using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clashura.Elo
{
    /// <summary>
    /// Persistent player rating profile stored in the database.
    /// Tracks unique player identification, current skill points (ELO), and career statistics.
    /// </summary>
    [Serializable]
    public class PlayerEloProfile
    {
        public string playerId = string.Empty;
        public string playerName = "Warrior";
        public int rating = 1000;
        public int wins = 0;
        public int losses = 0;
        public int matchesPlayed = 0;
        public long lastActiveTimestamp = 0;

        public float WinRate => matchesPlayed > 0 ? (float)wins / matchesPlayed * 100f : 0f;

        public PlayerEloProfile()
        {
            lastActiveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public PlayerEloProfile(string id, string name, int initialRating = 1000)
        {
            playerId = id;
            playerName = name;
            rating = initialRating;
            wins = 0;
            losses = 0;
            matchesPlayed = 0;
            lastActiveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    /// <summary>
    /// Matchmaking ticket enqueued in the matchmaking pool.
    /// Tracks dynamic search range expansion (±50 -> ±100 -> ±150).
    /// </summary>
    [Serializable]
    public class MatchmakingTicket
    {
        public string ticketId = string.Empty;
        public string playerId = string.Empty;
        public string playerName = string.Empty;
        public int playerRating = 1000;
        public int currentSearchRadius = 50;
        public long searchStartTime = 0;
        public string status = "Queued"; // Queued, Matched, Expired, Canceled

        // Match assignment (populated once paired)
        public string matchId = string.Empty;
        public string matchedSessionName = string.Empty;
        public string matchedOpponentId = string.Empty;
        public string matchedOpponentName = string.Empty;
        public int matchedOpponentRating = 1000;
        public bool isHost = false;

        public MatchmakingTicket()
        {
            ticketId = Guid.NewGuid().ToString("N");
            searchStartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    /// <summary>
    /// Authoritative record of an ELO match stored in the database.
    /// Guaranteed unique per match to prevent duplicate reward exploits.
    /// </summary>
    [Serializable]
    public class EloMatchRecord
    {
        public string matchId = string.Empty;
        public string sessionName = string.Empty;
        public string player1Id = string.Empty;
        public string player2Id = string.Empty;
        public string player1Name = string.Empty;
        public string player2Name = string.Empty;
        public int p1InitialRating = 1000;
        public int p2InitialRating = 1000;

        public string status = "InProgress"; // InProgress, Completed, Abandoned
        public string winnerId = string.Empty;
        public string loserId = string.Empty;

        public int p1RatingDelta = 0;
        public int p2RatingDelta = 0;
        public int p1FinalRating = 1000;
        public int p2FinalRating = 1000;

        public bool isProcessed = false;
        public long createdTimestamp = 0;
        public long completedTimestamp = 0;

        public EloMatchRecord()
        {
            createdTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    /// <summary>
    /// Payload submitted when reporting a completed match.
    /// </summary>
    [Serializable]
    public class MatchResultReport
    {
        public string matchId = string.Empty;
        public string reportingPlayerId = string.Empty;
        public string winnerId = string.Empty;
        public bool isForfeit = false;
    }

    /// <summary>
    /// Validated response returned by backend after processing a match outcome.
    /// </summary>
    [Serializable]
    public class MatchResultResponse
    {
        public bool success = false;
        public string errorMessage = string.Empty;
        public string matchId = string.Empty;
        public string winnerId = string.Empty;
        public string loserId = string.Empty;

        public int player1Delta = 0;
        public int player2Delta = 0;
        public int player1FinalRating = 0;
        public int player2FinalRating = 0;

        // Contextualized fields for local player
        public int localPlayerDelta = 0;
        public int localPlayerFinalRating = 0;
        public int localPlayerWins = 0;
        public int localPlayerLosses = 0;
        public int localPlayerMatches = 0;
        public bool isLocalWinner = false;
    }

    /// <summary>
    /// Serializable root schema for the persistent JSON database.
    /// </summary>
    [Serializable]
    public class DatabaseSchema
    {
        public List<PlayerEloProfile> players = new List<PlayerEloProfile>();
        public List<MatchmakingTicket> activeTickets = new List<MatchmakingTicket>();
        public List<EloMatchRecord> matches = new List<EloMatchRecord>();
        public List<string> processedMatchIds = new List<string>();
        public long lastUpdated = 0;
    }
}
