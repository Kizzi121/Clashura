using UnityEngine;

namespace Clashura.PlayFab
{
    /// <summary>
    /// Configuration settings for PlayFab authentication, trusted backend queue, and ELO matchmaking.
    /// </summary>
    public static class PlayFabConfig
    {
        // PlayFab Title Configuration
        public static string TitleId = "1224D";

        // Global Matchmaking Backend Configuration
        // Can be pointed to a LAN IP (e.g. "http://192.168.1.50:7865") or remote cloud server
        public static string BackendServerUrl = "http://localhost:7865";

        // Player Defaults (Preserves original game identity)
        public const string DefaultDisplayName = "Commander";
        public const int DefaultRating = 1250;

        // Matchmaking Queue Configuration
        public const string QueueName = "Clashura1v1Queue";
        public const int InitialRadius = 100; // ±100 points initial proximity
        public const int MaxSearchDurationSeconds = 120; // 2-minute search timeout
        public const string MsgTimeout = "Sorry, no opponent is currently available. Please try again.";

        public const int RadiusStep = 50;
        public const int StepIntervalSeconds = 30; // Expands gradually within 120s
        public const int MaxRadius = 200;
        public const int EloKFactor = 32;
        public const int HeartbeatTimeoutSeconds = 25;
        public const int MinRating = 100;

        // Player Data Keys in PlayFab UserData
        public const string KeyRating = "Rating";
        public const string KeyWins = "Wins";
        public const string KeyLosses = "Losses";
        public const string KeyMatchesPlayed = "MatchesPlayed";
        public const string KeyDisplayName = "DisplayName";
    }
}
