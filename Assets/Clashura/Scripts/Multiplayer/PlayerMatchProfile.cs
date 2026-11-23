using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clashura.Multiplayer
{
    /// <summary>
    /// Holds match-relevant player data: Name, Level, Rating/Points, and Selected Team character IDs.
    /// Synchronized across network instances during PvP matchmaking.
    /// </summary>
    [Serializable]
    public class PlayerMatchProfile
    {
        public string playerId = string.Empty;
        public string playerName = "Warrior";
        public int playerLevel = 1;
        public int playerPoints = 1000;
        public int wins = 0;
        public int losses = 0;
        public string currentMatchId = string.Empty;
        public List<string> teamCharacterIds = new List<string>();

        public PlayerMatchProfile()
        {
        }

        public PlayerMatchProfile(string name, int level, int points, List<string> team)
        {
            playerName = name;
            playerLevel = level;
            playerPoints = points;
            teamCharacterIds = team != null ? new List<string>(team) : new List<string>();
        }

        /// <summary>
        /// Creates a profile representing the local player from persistent settings, database ELO, and team selection.
        /// </summary>
        public static PlayerMatchProfile CreateLocalProfile(bool isHost = true)
        {
            string id = isHost ? "Host_01" : "Client_02";
            string defaultName = isHost ? PlayFab.PlayFabConfig.DefaultDisplayName : "Player 2";
            string name = PlayerPrefs.GetString("CLASHURA_PLAYER_NAME", defaultName);
            int points = PlayFab.PlayFabConfig.DefaultRating;
            int wins = 0;
            int losses = 0;

            if (PlayFab.PlayFabAuthService.HasInstance && PlayFab.PlayFabAuthService.Instance.CurrentProfile != null)
            {
                var pf = PlayFab.PlayFabAuthService.Instance.CurrentProfile;
                id = pf.playFabId;
                name = pf.displayName;
                points = pf.rating;
                wins = pf.wins;
                losses = pf.losses;
            }
            else if (Elo.EloManager.Instance != null && Elo.EloManager.Instance.CurrentProfile != null)
            {
                id = Elo.EloManager.Instance.GetLocalPlayerId();
                name = Elo.EloManager.Instance.GetLocalPlayerName();
                points = Elo.EloManager.Instance.CurrentProfile.rating;
                wins = Elo.EloManager.Instance.CurrentProfile.wins;
                losses = Elo.EloManager.Instance.CurrentProfile.losses;
            }
            else
            {
                points = PlayerPrefs.GetInt("CLASHURA_PLAYER_POINTS", 1000);
            }

            int level = PlayerPrefs.GetInt("CLASHURA_PLAYER_LEVEL", isHost ? 25 : 28);

            var team = TeamSelectionManager.GetSelectedTeam();
            if (team == null || team.Count == 0)
            {
                // Fallback default team
                team = isHost 
                    ? new List<string> { "clashura_char_01", "clashura_char_02", "clashura_char_03" }
                    : new List<string> { "clashura_char_04", "clashura_char_05", "clashura_char_06" };
            }

            var profile = new PlayerMatchProfile(name, level, points, team);
            profile.playerId = id;
            if (Elo.EloManager.Instance != null && Elo.EloManager.Instance.CurrentProfile != null)
            {
                profile.wins = Elo.EloManager.Instance.CurrentProfile.wins;
                profile.losses = Elo.EloManager.Instance.CurrentProfile.losses;
            }
            if (Elo.EloManager.Instance != null)
            {
                profile.currentMatchId = Elo.EloManager.Instance.CurrentMatchId;
            }
            return profile;
        }

        /// <summary>
        /// Creates a default rival profile for local/offline preview testing.
        /// </summary>
        public static PlayerMatchProfile CreateRivalPreviewProfile()
        {
            return new PlayerMatchProfile(
                "Shadow Rival", 
                30, 
                1560, 
                new List<string> { "clashura_char_04", "clashura_char_02", "clashura_char_06" }
            );
        }
    }
}
