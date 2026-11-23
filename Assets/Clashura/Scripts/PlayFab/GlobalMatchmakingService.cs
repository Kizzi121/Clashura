using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Clashura.PlayFab
{
    /// <summary>
    /// Thread-safe, atomic, authoritative Global Matchmaking Queue Service.
    /// Handles concurrent requests from multiple devices across networks, executes dynamic
    /// rating range expansion (±50 -> ±100 -> ±150), optimal ELO bipartite pairing for batches
    /// of simultaneous players (e.g. 6 players arriving at once), duplicate match prevention,
    /// reservation locks, disconnect prune, and authoritative ELO calculation (K=32).
    /// </summary>
    public static class GlobalMatchmakingService
    {
        private static readonly object _queueLock = new object();

        // Active queued tickets: ticketId -> MatchmakingTicketDto
        private static readonly ConcurrentDictionary<string, MatchmakingTicketDto> _tickets =
            new ConcurrentDictionary<string, MatchmakingTicketDto>(StringComparer.OrdinalIgnoreCase);

        // Active player map: playerId -> ticketId (prevents same player entering queue twice)
        private static readonly ConcurrentDictionary<string, string> _playerToTicket =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Active matches: matchId -> MatchRecord
        private static readonly ConcurrentDictionary<string, MatchRecord> _activeMatches =
            new ConcurrentDictionary<string, MatchRecord>(StringComparer.OrdinalIgnoreCase);

        // Processed match IDs (anti-tamper idempotency)
        private static readonly HashSet<string> _processedMatchIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Authoritative persistent profiles: playerId -> PlayFabPlayerProfile
        private static readonly ConcurrentDictionary<string, PlayFabPlayerProfile> _profiles =
            new ConcurrentDictionary<string, PlayFabPlayerProfile>(StringComparer.OrdinalIgnoreCase);

        public class MatchRecord
        {
            public string matchId;
            public string sessionName;
            public string hostPlayerId;
            public string hostPlayerName;
            public int hostRating;
            public string clientPlayerId;
            public string clientPlayerName;
            public int clientRating;
            public long createdTime;
            public bool isCompleted;
        }

        #region Profile Registration & Management

        public static PlayFabPlayerProfile GetOrCreateProfile(string playerId, string displayName, int defaultRating = PlayFabConfig.DefaultRating)
        {
            if (string.IsNullOrEmpty(playerId)) return null;

            return _profiles.GetOrAdd(playerId, id => new PlayFabPlayerProfile
            {
                playFabId = id,
                displayName = !string.IsNullOrEmpty(displayName) ? displayName : PlayFabConfig.DefaultDisplayName,
                rating = defaultRating,
                wins = 0,
                losses = 0,
                matchesPlayed = 0,
                lastActiveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });
        }

        public static void UpdateProfileRating(string playerId, int newRating, bool won)
        {
            if (_profiles.TryGetValue(playerId, out var profile))
            {
                profile.rating = Math.Max(PlayFabConfig.MinRating, newRating);
                profile.matchesPlayed++;
                if (won) profile.wins++;
                else profile.losses++;
                profile.lastActiveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }

        #endregion

        #region Enqueue & Ticket Management

        /// <summary>
        /// Atomically enqueues a player into the global matchmaking queue.
        /// Prevents duplicate queue entries: if player is already queued, returns their active ticket.
        /// </summary>
        public static MatchmakingTicketDto Enqueue(string playerId, string displayName, int rating)
        {
            lock (_queueLock)
            {
                PruneExpiredTickets();

                // 1. Check if player already has an active ticket
                if (_playerToTicket.TryGetValue(playerId, out string existingTicketId))
                {
                    if (_tickets.TryGetValue(existingTicketId, out var existingTicket))
                    {
                        if (existingTicket.status == "Queued" || existingTicket.status == "Reserved" || existingTicket.status == "Matched")
                        {
                            existingTicket.lastHeartbeatTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            existingTicket.playersInQueue = GetQueuedCount();
                            return existingTicket;
                        }
                    }
                }

                // 2. Register or fetch authoritative profile
                var profile = GetOrCreateProfile(playerId, displayName, rating);

                string ticketId = "TKT_" + Guid.NewGuid().ToString("N").Substring(0, 12);
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                var ticket = new MatchmakingTicketDto
                {
                    ticketId = ticketId,
                    playerId = profile.playFabId,
                    displayName = profile.displayName,
                    rating = profile.rating,
                    currentSearchRadius = PlayFabConfig.InitialRadius,
                    searchStartTime = now,
                    lastHeartbeatTime = now,
                    status = "Queued",
                    playersInQueue = GetQueuedCount() + 1
                };

                _tickets[ticketId] = ticket;
                _playerToTicket[profile.playFabId] = ticketId;

                Debug.Log($"[GlobalMatchmakingService] Enqueued '{ticket.displayName}' (Rating: {ticket.rating}, Ticket: {ticket.ticketId}). Queue size: {GetQueuedCount()}");

                // Immediately trigger batch evaluation to see if a suitable match can be formed
                EvaluateQueueBatch();

                ticket.playersInQueue = GetQueuedCount();
                return ticket;
            }
        }

        /// <summary>
        /// Polls the status of a ticket, refreshing heartbeat, updating dynamic search radius,
        /// and running batch evaluation.
        /// </summary>
        public static MatchmakingTicketDto Poll(string ticketId)
        {
            lock (_queueLock)
            {
                if (!_tickets.TryGetValue(ticketId, out var ticket))
                {
                    return null;
                }

                ticket.lastHeartbeatTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                if (ticket.status == "Matched" || ticket.status == "Canceled")
                {
                    ticket.playersInQueue = GetQueuedCount();
                    return ticket;
                }

                // Check 120s search timeout
                long elapsed = ticket.lastHeartbeatTime - ticket.searchStartTime;
                if (elapsed >= PlayFabConfig.MaxSearchDurationSeconds)
                {
                    ticket.status = "TimedOut";
                    _tickets.TryRemove(ticketId, out _);
                    _playerToTicket.TryRemove(ticket.playerId, out _);
                    Debug.Log($"[GlobalMatchmakingService] Ticket {ticketId} for '{ticket.displayName}' reached 120s timeout without opponent. Removed from queue.");
                    return ticket;
                }

                // Update dynamic search radius
                int steps = (int)(elapsed / PlayFabConfig.StepIntervalSeconds);
                int newRadius = Math.Min(PlayFabConfig.MaxRadius, PlayFabConfig.InitialRadius + (steps * PlayFabConfig.RadiusStep));
                ticket.currentSearchRadius = newRadius;

                // Evaluate batch
                EvaluateQueueBatch();

                ticket.playersInQueue = GetQueuedCount();
                return ticket;
            }
        }

        /// <summary>
        /// Cancels a matchmaking ticket atomically and frees the player.
        /// </summary>
        public static bool Cancel(string ticketId, string playerId = null)
        {
            lock (_queueLock)
            {
                if (_tickets.TryGetValue(ticketId, out var ticket))
                {
                    ticket.status = "Canceled";
                    _tickets.TryRemove(ticketId, out _);
                    _playerToTicket.TryRemove(ticket.playerId, out _);
                    Debug.Log($"[GlobalMatchmakingService] Canceled ticket {ticketId} for player '{ticket.displayName}'.");
                    return true;
                }
                return false;
            }
        }

        public static int GetQueuedCount()
        {
            return _tickets.Values.Count(t => t.status == "Queued");
        }

        public static QueueStatusDto GetQueueStatus()
        {
            return new QueueStatusDto
            {
                playersInQueue = GetQueuedCount(),
                activeMatches = _activeMatches.Count,
                serverTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        #endregion

        #region Batch Matchmaking Evaluation (Optimal ELO Pairing)

        /// <summary>
        /// Evaluates all queued players simultaneously.
        /// Handles multiple players arriving at once (e.g. 6 players: Commander 1250, Player B 1270,
        /// Player C 1230, Player D 900, Player E 920, Player F 880), finding optimal pairs that minimize
        /// ELO difference within each player's active search radius.
        /// Odd players or players without suitable opponents remain in queue.
        /// Atomically reserves both players before creating the match.
        /// </summary>
        public static void EvaluateQueueBatch()
        {
            lock (_queueLock)
            {
                PruneExpiredTickets();

                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // 1. Gather all currently "Queued" tickets and update their dynamic radius
                var queuedTickets = _tickets.Values
                    .Where(t => t.status == "Queued")
                    .OrderBy(t => t.rating) // Sort ascending by rating
                    .ToList();

                if (queuedTickets.Count < 2) return;

                foreach (var t in queuedTickets)
                {
                    long elapsed = now - t.searchStartTime;
                    int steps = (int)(elapsed / PlayFabConfig.StepIntervalSeconds);
                    t.currentSearchRadius = Math.Min(PlayFabConfig.MaxRadius, PlayFabConfig.InitialRadius + (steps * PlayFabConfig.RadiusStep));
                }

                var matchedInThisPass = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 2. Build candidate pairs where both players' search radii are satisfied
                var possiblePairs = new List<CandidatePair>();

                for (int i = 0; i < queuedTickets.Count; i++)
                {
                    for (int j = i + 1; j < queuedTickets.Count; j++)
                    {
                        var p1 = queuedTickets[i];
                        var p2 = queuedTickets[j];

                        // Prevent matching against own account
                        if (string.Equals(p1.playerId, p2.playerId, StringComparison.OrdinalIgnoreCase))
                            continue;

                        int diff = Math.Abs(p1.rating - p2.rating);
                        int allowedDiff = Math.Min(p1.currentSearchRadius, p2.currentSearchRadius);

                        if (diff <= allowedDiff)
                        {
                            possiblePairs.Add(new CandidatePair
                            {
                                p1 = p1,
                                p2 = p2,
                                ratingDiff = diff,
                                combinedWaitTime = (now - p1.searchStartTime) + (now - p2.searchStartTime)
                            });
                        }
                    }
                }

                // 3. Sort pairs: lowest rating difference first (best quality), then longest wait time
                // To support tie-breaking with random selection among equally suitable opponents:
                System.Random rng = new System.Random();
                var sortedPairs = possiblePairs
                    .OrderBy(p => p.ratingDiff)
                    .ThenByDescending(p => p.combinedWaitTime)
                    .ThenBy(_ => rng.Next())
                    .ToList();

                // 4. Greedily select non-overlapping pairs
                foreach (var pair in sortedPairs)
                {
                    if (matchedInThisPass.Contains(pair.p1.ticketId) || matchedInThisPass.Contains(pair.p2.ticketId))
                        continue;

                    // Atomic reservation
                    if (pair.p1.status != "Queued" || pair.p2.status != "Queued")
                        continue;

                    pair.p1.status = "Reserved";
                    pair.p2.status = "Reserved";
                    matchedInThisPass.Add(pair.p1.ticketId);
                    matchedInThisPass.Add(pair.p2.ticketId);

                    // Create match
                    CreateMatch(pair.p1, pair.p2);
                }
            }
        }

        private class CandidatePair
        {
            public MatchmakingTicketDto p1;
            public MatchmakingTicketDto p2;
            public int ratingDiff;
            public long combinedWaitTime;
        }

        private static void CreateMatch(MatchmakingTicketDto p1, MatchmakingTicketDto p2)
        {
            string matchId = "CLASH_MATCH_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            string sessionName = "CLASH_SESSION_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            // P1 is designated Host, P2 is Client
            p1.matchedMatchId = matchId;
            p1.matchedOpponentId = p2.playerId;
            p1.matchedOpponentName = p2.displayName;
            p1.matchedOpponentRating = p2.rating;
            p1.isHost = true;
            p1.sessionName = sessionName;
            p1.status = "Matched";

            p2.matchedMatchId = matchId;
            p2.matchedOpponentId = p1.playerId;
            p2.matchedOpponentName = p1.displayName;
            p2.matchedOpponentRating = p1.rating;
            p2.isHost = false;
            p2.sessionName = sessionName;
            p2.status = "Matched";

            var matchRecord = new MatchRecord
            {
                matchId = matchId,
                sessionName = sessionName,
                hostPlayerId = p1.playerId,
                hostPlayerName = p1.displayName,
                hostRating = p1.rating,
                clientPlayerId = p2.playerId,
                clientPlayerName = p2.displayName,
                clientRating = p2.rating,
                createdTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                isCompleted = false
            };

            _activeMatches[matchId] = matchRecord;

            // Remove from available queue mapping so neither player can be re-selected
            _playerToTicket.TryRemove(p1.playerId, out _);
            _playerToTicket.TryRemove(p2.playerId, out _);

            Debug.Log($"[GlobalMatchmakingService] MATCH CREATED! ID: {matchId} | Host: '{p1.displayName}' ({p1.rating}) vs Client: '{p2.displayName}' ({p2.rating}) | Delta: {Math.Abs(p1.rating - p2.rating)} ELO | Session: {sessionName}");
        }

        private static void PruneExpiredTickets()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (var kvp in _tickets)
            {
                var t = kvp.Value;
                if (t.status == "Queued")
                {
                    long totalDuration = now - t.searchStartTime;
                    long heartbeatInactive = now - t.lastHeartbeatTime;

                    if (totalDuration >= PlayFabConfig.MaxSearchDurationSeconds)
                    {
                        t.status = "TimedOut";
                        _tickets.TryRemove(kvp.Key, out _);
                        _playerToTicket.TryRemove(t.playerId, out _);
                        Debug.Log($"[GlobalMatchmakingService] Ticket {t.ticketId} for '{t.displayName}' reached 120s search duration without opponent.");
                    }
                    else if (heartbeatInactive > PlayFabConfig.HeartbeatTimeoutSeconds)
                    {
                        t.status = "Canceled";
                        _tickets.TryRemove(kvp.Key, out _);
                        _playerToTicket.TryRemove(t.playerId, out _);
                        Debug.Log($"[GlobalMatchmakingService] Pruned timed-out ticket {t.ticketId} for player '{t.displayName}'.");
                    }
                }
            }
        }

        #endregion

        #region Authoritative ELO Match Result Resolution

        /// <summary>
        /// Authoritatively resolves a completed match using the standard ELO formula (K=32).
        /// Prevents duplicate rating modifications via matchId idempotency.
        /// </summary>
        public static MatchResultResponseDto CompleteMatch(MatchResultReportDto report)
        {
            lock (_queueLock)
            {
                if (report == null || string.IsNullOrEmpty(report.matchId))
                {
                    return new MatchResultResponseDto { isVerified = false, status = "INVALID_REPORT" };
                }

                // Check duplicate processing
                if (_processedMatchIds.Contains(report.matchId))
                {
                    return new MatchResultResponseDto
                    {
                        matchId = report.matchId,
                        isVerified = true,
                        status = "ALREADY_PROCESSED"
                    };
                }

                if (!_activeMatches.TryGetValue(report.matchId, out var match))
                {
                    return new MatchResultResponseDto { matchId = report.matchId, isVerified = false, status = "MATCH_NOT_FOUND" };
                }

                match.isCompleted = true;
                _processedMatchIds.Add(report.matchId);

                // Fetch authoritative profiles
                var p1 = GetOrCreateProfile(match.hostPlayerId, match.hostPlayerName, match.hostRating);
                var p2 = GetOrCreateProfile(match.clientPlayerId, match.clientPlayerName, match.clientRating);

                bool hostWon = string.Equals(report.winnerPlayerId, match.hostPlayerId, StringComparison.OrdinalIgnoreCase);

                int winnerOld = hostWon ? p1.rating : p2.rating;
                int loserOld = hostWon ? p2.rating : p1.rating;
                string winnerId = hostWon ? p1.playFabId : p2.playFabId;
                string loserId = hostWon ? p2.playFabId : p1.playFabId;

                // ELO Calculation (K = 32)
                // Expected Score: E_W = 1 / (1 + 10^((R_L - R_W) / 400))
                double exponent = (loserOld - winnerOld) / 400.0;
                double expectedWinner = 1.0 / (1.0 + Math.Pow(10.0, exponent));
                double expectedLoser = 1.0 - expectedWinner;

                int winnerDelta = (int)Math.Round(PlayFabConfig.EloKFactor * (1.0 - expectedWinner));
                int loserDelta = (int)Math.Round(PlayFabConfig.EloKFactor * (0.0 - expectedLoser));

                // Minimum change of 1
                if (winnerDelta < 1) winnerDelta = 1;
                if (loserDelta > -1) loserDelta = -1;

                int winnerNew = Math.Max(PlayFabConfig.MinRating, winnerOld + winnerDelta);
                int loserNew = Math.Max(PlayFabConfig.MinRating, loserOld + loserDelta);

                // Update authoritative records
                UpdateProfileRating(winnerId, winnerNew, true);
                UpdateProfileRating(loserId, loserNew, false);

                Debug.Log($"[GlobalMatchmakingService] Match {report.matchId} Resolved! Winner: {winnerId} (+{winnerDelta} -> {winnerNew} ELO) | Loser: {loserId} ({loserDelta} -> {loserNew} ELO)");

                return new MatchResultResponseDto
                {
                    matchId = report.matchId,
                    winnerId = winnerId,
                    loserId = loserId,
                    winnerOldRating = winnerOld,
                    winnerNewRating = winnerNew,
                    winnerDelta = winnerDelta,
                    loserOldRating = loserOld,
                    loserNewRating = loserNew,
                    loserDelta = loserDelta,
                    isVerified = true,
                    status = "SUCCESS"
                };
            }
        }

        #endregion

        #region Reset / Cleanup (For Testing)

        public static void ResetState()
        {
            lock (_queueLock)
            {
                _tickets.Clear();
                _playerToTicket.Clear();
                _activeMatches.Clear();
                _processedMatchIds.Clear();
                _profiles.Clear();
            }
        }

        #endregion
    }
}
