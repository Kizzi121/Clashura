using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Clashura.AI.Career
{
    /// <summary>
    /// Runtime random selector that samples EXACTLY THREE unique characters from a given 6-character difficulty pool.
    /// Strictly guarantees:
    /// - Exactly 3 characters selected (never all 6).
    /// - Zero duplicate characters.
    /// - Only characters from the selected difficulty tier (no mixing between tiers).
    /// - Unbiased Fisher-Yates shuffle sampling without replacement.
    /// </summary>
    public static class AIRandomSelector
    {
        public const int RequiredSelectionCount = 3;

        /// <summary>
        /// Randomly selects exactly 3 unique characters from the 6 characters belonging to the specified difficulty tier.
        /// </summary>
        public static List<AICharacterEntry> SelectRandomAITeam(AIDifficulty difficulty, AICharacterDatabase database = null)
        {
            if (database == null)
            {
                database = AICharacterDatabase.Instance;
                if (database == null)
                {
                    database = UnityEngine.Object.FindFirstObjectByType<AICharacterDatabase>();
                }
            }

            if (database == null)
            {
                Debug.LogError("[AIRandomSelector] AICharacterDatabase instance not found!");
                return new List<AICharacterEntry>();
            }

            var pool = database.GetCharactersForDifficulty(difficulty);
            if (pool == null || pool.Count == 0)
            {
                Debug.LogError($"[AIRandomSelector] No AI characters found for difficulty {difficulty}!");
                return new List<AICharacterEntry>();
            }

            return SampleThreeUniqueCharacters(pool);
        }

        /// <summary>
        /// Performs Fisher-Yates shuffle and samples exactly 3 unique characters without replacement.
        /// </summary>
        public static List<AICharacterEntry> SampleThreeUniqueCharacters(IReadOnlyList<AICharacterEntry> sourceList)
        {
            if (sourceList == null || sourceList.Count == 0)
            {
                return new List<AICharacterEntry>();
            }

            // Copy to working list to avoid mutating the master database
            List<AICharacterEntry> copy = new List<AICharacterEntry>(sourceList);

            // True Fisher-Yates shuffle
            for (int i = 0; i < copy.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, copy.Count);
                var temp = copy[i];
                copy[i] = copy[randomIndex];
                copy[randomIndex] = temp;
            }

            // Take exactly 3 elements
            int takeCount = Mathf.Min(RequiredSelectionCount, copy.Count);
            var selected = copy.Take(takeCount).ToList();

            Debug.Log($"[AIRandomSelector] Randomly selected {selected.Count} unique AI characters: {string.Join(", ", selected.Select(s => $"{s.placeholderName} ({s.characterName})"))}");
            return selected;
        }

        /// <summary>
        /// Validates that a list of selected character entries contains exactly 3 unique character IDs.
        /// </summary>
        public static bool ValidateThreeUnique(IReadOnlyList<AICharacterEntry> list)
        {
            if (list == null || list.Count != RequiredSelectionCount) return false;

            var idSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in list)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.characterId)) return false;
                if (!idSet.Add(item.characterId))
                {
                    return false; // Duplicate detected
                }
            }

            return idSet.Count == RequiredSelectionCount;
        }
    }
}
