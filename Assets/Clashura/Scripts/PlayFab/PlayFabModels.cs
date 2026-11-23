using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clashura.PlayFab
{
    /// <summary>
    /// PlayFab Authoritative Player Profile containing persistent account statistics and ELO rating.
    /// </summary>
    [Serializable]
    public class PlayFabPlayerProfile
    {
        public string playFabId;
        public string displayName;
        public int rating = 1000;
        public int wins = 0;
        public int losses = 0;
        public int matchesPlayed = 0;
        public long lastActiveTimestamp;

        public PlayFabPlayerProfile Clone()
        {
            return new PlayFabPlayerProfile
            {
                playFabId = this.playFabId,
                displayName = this.displayName,
                rating = this.rating,
                wins = this.wins,
                losses = this.losses,
                matchesPlayed = this.matchesPlayed,
                lastActiveTimestamp = this.lastActiveTimestamp
            };
        }
    }

    /// <summary>
    /// Data Transfer Object representing an active entry in the global matchmaking queue.
    /// </summary>
    [Serializable]
    public class MatchmakingTicketDto
    {
        public string ticketId;
        public string playerId;
        public string displayName;
        public int rating;
        public int currentSearchRadius;
        public long searchStartTime;
        public string status; // "Queued", "Reserved", "Matched", "Canceled"

        // Match resolution fields once matched
        public string matchedMatchId;
        public string matchedOpponentId;
        public string matchedOpponentName;
        public int matchedOpponentRating;
        public bool isHost;
        public string sessionName;

        // Queue diagnostics
        public int playersInQueue;
        public long lastHeartbeatTime;
    }

    /// <summary>
    /// Submission payload for match outcome reporting.
    /// </summary>
    [Serializable]
    public class MatchResultReportDto
    {
        public string matchId;
        public string winnerPlayerId;
        public string loserPlayerId;
    }

    /// <summary>
    /// Verified result returned by backend after authoritative ELO calculation.
    /// </summary>
    [Serializable]
    public class MatchResultResponseDto
    {
        public string matchId;
        public string winnerId;
        public string loserId;
        public int winnerOldRating;
        public int winnerNewRating;
        public int winnerDelta;
        public int loserOldRating;
        public int loserNewRating;
        public int loserDelta;
        public bool isVerified;
        public string status;
    }

    /// <summary>
    /// Global Queue Status snapshot.
    /// </summary>
    [Serializable]
    public class QueueStatusDto
    {
        public int playersInQueue;
        public int activeMatches;
        public long serverTime;
    }

    #region PlayFab REST DTOs

    [Serializable]
    public class PlayFabLoginCustomIdRequest
    {
        public string TitleId;
        public string CustomId;
        public bool CreateAccount = true;
    }

    [Serializable]
    public class PlayFabLoginResponseData
    {
        public string SessionTicket;
        public string PlayFabId;
        public string EntityToken;
    }

    [Serializable]
    public class PlayFabBaseResponse<T>
    {
        public int code;
        public string status;
        public T data;
        public string errorMessage;
    }

    [Serializable]
    public class PlayFabUpdateUserDataRequest
    {
        public Dictionary<string, string> Data;
    }

    [Serializable]
    public class PlayFabUserDataRecord
    {
        public string Value;
        public string LastUpdated;
    }

    #endregion
}
