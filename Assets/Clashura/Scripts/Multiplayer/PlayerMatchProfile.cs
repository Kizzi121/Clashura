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
        public string playerName = "Warrior";
        public int playerLevel = 1;
        public int playerPoints = 1000;
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
        /// Creates a profile representing the local player from persistent settings and team selection.
        /// </summary>
        public static PlayerMatchProfile CreateLocalProfile(bool isHost = true)
        {
            string defaultName = isHost ? "Player 1" : "Player 2";
            string name = PlayerPrefs.GetString("CLASHURA_PLAYER_NAME", defaultName);
            int level = PlayerPrefs.GetInt("CLASHURA_PLAYER_LEVEL", isHost ? 25 : 28);
            int points = PlayerPrefs.GetInt("CLASHURA_PLAYER_POINTS", isHost ? 1450 : 1520);

            var team = TeamSelectionManager.GetSelectedTeam();
            if (team == null || team.Count == 0)
            {
                // Fallback default team
                team = isHost 
                    ? new List<string> { "clashura_char_01", "clashura_char_02", "clashura_char_03" }
                    : new List<string> { "clashura_char_04", "clashura_char_05", "clashura_char_06" };
            }

            return new PlayerMatchProfile(name, level, points, team);
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
