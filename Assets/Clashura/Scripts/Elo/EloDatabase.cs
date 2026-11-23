using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Clashura.Elo
{
    /// <summary>
    /// Thread-safe and cross-process persistent database for player ELO ratings, active tickets, and matches.
    /// Stored locally in %LocalAppData%/Clashura/Database/elo_database.json to preserve player career statistics
    /// across game sessions, device reboots, and between concurrent Editor and Standalone processes.
    /// </summary>
    public static class EloDatabase
    {
        private static readonly object _lock = new object();

        public static string DatabaseDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Clashura", "Database");

        public static string DatabaseFilePath =>
            Path.Combine(DatabaseDirectory, "elo_database.json");

        static EloDatabase()
        {
            EnsureDirectoryExists();
        }

        private static void EnsureDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(DatabaseDirectory))
                {
                    Directory.CreateDirectory(DatabaseDirectory);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EloDatabase] Failed to create database directory: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads the entire persistent database with file-sharing retries.
        /// </summary>
        public static DatabaseSchema Load()
        {
            lock (_lock)
            {
                EnsureDirectoryExists();
                string path = DatabaseFilePath;

                if (!File.Exists(path))
                {
                    var freshDb = CreateInitialDatabase();
                    Save(freshDb);
                    return freshDb;
                }

                for (int attempt = 0; attempt < 5; attempt++)
                {
                    try
                    {
                        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(fs))
                        {
                            string json = reader.ReadToEnd();
                            if (string.IsNullOrWhiteSpace(json))
                            {
                                return CreateInitialDatabase();
                            }
                            var db = JsonUtility.FromJson<DatabaseSchema>(json);
                            return db ?? CreateInitialDatabase();
                        }
                    }
                    catch (IOException)
                    {
                        System.Threading.Thread.Sleep(25);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[EloDatabase] Parse error, creating fallback: {ex.Message}");
                        return CreateInitialDatabase();
                    }
                }

                return CreateInitialDatabase();
            }
        }

        /// <summary>
        /// Saves the database atomically to disk.
        /// </summary>
        public static bool Save(DatabaseSchema schema)
        {
            if (schema == null) return false;

            lock (_lock)
            {
                EnsureDirectoryExists();
                string path = DatabaseFilePath;
                schema.lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string json = JsonUtility.ToJson(schema, true);

                for (int attempt = 0; attempt < 5; attempt++)
                {
                    try
                    {
                        // Write to temporary file first, then replace to prevent file corruption
                        string tempPath = path + ".tmp";
                        File.WriteAllText(tempPath, json);
                        File.Copy(tempPath, path, true);
                        try { File.Delete(tempPath); } catch { }
                        return true;
                    }
                    catch (IOException)
                    {
                        System.Threading.Thread.Sleep(25);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[EloDatabase] Save error on attempt {attempt}: {ex.Message}");
                    }
                }

                return false;
            }
        }

        private static DatabaseSchema CreateInitialDatabase()
        {
            var db = new DatabaseSchema();
            // Seed sample players for dynamic matchmaking matching if empty
            db.players.Add(new PlayerEloProfile("Player_Host_01", "Aethelgard Master", 1020) { wins = 12, losses = 8, matchesPlayed = 20 });
            db.players.Add(new PlayerEloProfile("Player_Client_02", "Shadow Zephyr", 980) { wins = 10, losses = 10, matchesPlayed = 20 });
            db.players.Add(new PlayerEloProfile("Player_Rival_03", "Valkira Champion", 1050) { wins = 15, losses = 5, matchesPlayed = 20 });
            db.players.Add(new PlayerEloProfile("Player_Novice_04", "Iron Guard", 920) { wins = 5, losses = 15, matchesPlayed = 20 });
            db.lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return db;
        }

        #region Player Operations

        public static PlayerEloProfile GetPlayer(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return null;
            var db = Load();
            return db.players.FirstOrDefault(p => string.Equals(p.playerId, playerId, StringComparison.OrdinalIgnoreCase));
        }

        public static PlayerEloProfile GetOrCreatePlayer(string playerId, string defaultName = "Warrior", int defaultRating = 1000)
        {
            if (string.IsNullOrWhiteSpace(playerId)) playerId = "Player_Local";

            var db = Load();
            var existing = db.players.FirstOrDefault(p => string.Equals(p.playerId, playerId, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(defaultName) && existing.playerName == "Warrior")
                {
                    existing.playerName = defaultName;
                    Save(db);
                }
                return existing;
            }

            var newPlayer = new PlayerEloProfile(playerId, defaultName, defaultRating);
            db.players.Add(newPlayer);
            Save(db);
            Debug.Log($"[EloDatabase] Registered new player profile: '{newPlayer.playerName}' (ID: {playerId}) with initial rating {defaultRating}.");
            return newPlayer;
        }

        public static bool UpdatePlayer(PlayerEloProfile updated)
        {
            if (updated == null || string.IsNullOrWhiteSpace(updated.playerId)) return false;
            var db = Load();
            int idx = db.players.FindIndex(p => string.Equals(p.playerId, updated.playerId, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                db.players[idx] = updated;
            }
            else
            {
                db.players.Add(updated);
            }
            return Save(db);
        }

        public static List<PlayerEloProfile> GetLeaderboard(int topCount = 10)
        {
            var db = Load();
            return db.players.OrderByDescending(p => p.rating).Take(topCount).ToList();
        }

        #endregion

        #region Matchmaking Tickets

        public static void EnqueueTicket(MatchmakingTicket ticket)
        {
            if (ticket == null || string.IsNullOrWhiteSpace(ticket.ticketId)) return;
            var db = Load();
            // Remove existing tickets for same player to avoid duplicates
            db.activeTickets.RemoveAll(t => string.Equals(t.playerId, ticket.playerId, StringComparison.OrdinalIgnoreCase));
            db.activeTickets.Add(ticket);
            Save(db);
        }

        public static MatchmakingTicket GetTicket(string ticketId)
        {
            if (string.IsNullOrWhiteSpace(ticketId)) return null;
            var db = Load();
            return db.activeTickets.FirstOrDefault(t => t.ticketId == ticketId);
        }

        public static MatchmakingTicket GetPlayerActiveTicket(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return null;
            var db = Load();
            return db.activeTickets.FirstOrDefault(t => string.Equals(t.playerId, playerId, StringComparison.OrdinalIgnoreCase));
        }

        public static void UpdateTicket(MatchmakingTicket ticket)
        {
            if (ticket == null || string.IsNullOrWhiteSpace(ticket.ticketId)) return;
            var db = Load();
            int idx = db.activeTickets.FindIndex(t => t.ticketId == ticket.ticketId);
            if (idx >= 0)
            {
                db.activeTickets[idx] = ticket;
                Save(db);
            }
        }

        public static void RemoveTicket(string ticketId)
        {
            if (string.IsNullOrWhiteSpace(ticketId)) return;
            var db = Load();
            int removed = db.activeTickets.RemoveAll(t => t.ticketId == ticketId);
            if (removed > 0)
            {
                Save(db);
            }
        }

        public static void CleanupExpiredTickets(int maxAgeSeconds = 60)
        {
            var db = Load();
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int countBefore = db.activeTickets.Count;
            db.activeTickets.RemoveAll(t => (now - t.searchStartTime) > maxAgeSeconds && t.status != "Matched");
            if (db.activeTickets.Count != countBefore)
            {
                Save(db);
            }
        }

        #endregion

        #region Matches & Idempotency

        public static void SaveMatch(EloMatchRecord match)
        {
            if (match == null || string.IsNullOrWhiteSpace(match.matchId)) return;
            var db = Load();
            int idx = db.matches.FindIndex(m => m.matchId == match.matchId);
            if (idx >= 0)
            {
                db.matches[idx] = match;
            }
            else
            {
                db.matches.Add(match);
            }
            Save(db);
        }

        public static EloMatchRecord GetMatch(string matchId)
        {
            if (string.IsNullOrWhiteSpace(matchId)) return null;
            var db = Load();
            return db.matches.FirstOrDefault(m => m.matchId == matchId);
        }

        public static bool IsMatchProcessed(string matchId)
        {
            if (string.IsNullOrWhiteSpace(matchId)) return false;
            var db = Load();
            return db.processedMatchIds.Contains(matchId);
        }

        public static void MarkMatchProcessed(string matchId)
        {
            if (string.IsNullOrWhiteSpace(matchId)) return;
            var db = Load();
            if (!db.processedMatchIds.Contains(matchId))
            {
                db.processedMatchIds.Add(matchId);
                Save(db);
            }
        }

        #endregion
    }
}
