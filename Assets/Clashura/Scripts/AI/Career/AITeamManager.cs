using System;
using System.Collections.Generic;
using UnityEngine;
using Clashura.Characters.Data;
using Clashura.Characters.Managers;
using Clashura.Multiplayer;

namespace Clashura.AI.Career
{
    /// <summary>
    /// Coordinates the 3v3 team composition for AI Career Mode:
    /// - Player Team: Exactly 3 authentic characters retrieved from existing TeamSelectionManager.
    /// - AI Team: Exactly 3 randomly selected characters from the difficulty tier (Opponent NOVA).
    /// Total: 6 Characters.
    /// </summary>
    public static class AITeamManager
    {
        public const int RequiredTeamSize = 3;

        /// <summary>
        /// Retrieves the player's authentic 3 selected character IDs from the existing character selection system.
        /// If fewer than 3 are selected, falls back to the top unlocked heroes while logging a notice.
        /// </summary>
        public static List<string> GetPlayerTeamIds(CharacterDatabaseSO masterDb = null)
        {
            List<string> selected = TeamSelectionManager.GetSelectedTeam();

            if (selected == null || selected.Count < RequiredTeamSize)
            {
                Debug.LogWarning($"[AITeamManager] Player has only {selected?.Count ?? 0} characters selected in TeamSelectionManager. Checking fallback defaults...");
                if (selected == null) selected = new List<string>();

                // Fallback: fill up to 3 using canonical IDs
                string[] fallbacks = { "clashura_char_01", "clashura_char_02", "clashura_char_03" };
                for (int i = 0; i < fallbacks.Length && selected.Count < RequiredTeamSize; i++)
                {
                    if (!selected.Contains(fallbacks[i]))
                    {
                        selected.Add(fallbacks[i]);
                    }
                }
            }

            return selected;
        }

        /// <summary>
        /// Retrieves the CharacterDataSO objects for the player's team.
        /// </summary>
        public static List<CharacterDataSO> GetPlayerTeamData(CharacterDatabaseSO masterDb)
        {
            var ids = GetPlayerTeamIds(masterDb);
            var result = new List<CharacterDataSO>();

            if (masterDb == null) return result;

            foreach (var id in ids)
            {
                var charData = masterDb.GetCharacterById(id);
                if (charData != null)
                {
                    result.Add(charData);
                }
            }

            return result;
        }

        /// <summary>
        /// Resolves CharacterDataSO objects for the AI team given their character IDs.
        /// </summary>
        public static List<CharacterDataSO> ResolveAITeamData(List<string> aiIds, CharacterDatabaseSO masterDb)
        {
            var result = new List<CharacterDataSO>();
            if (aiIds == null || masterDb == null) return result;

            foreach (var id in aiIds)
            {
                var charData = masterDb.GetCharacterById(id);
                if (charData != null)
                {
                    result.Add(charData);
                }
            }

            return result;
        }
    }
}
