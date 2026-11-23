using System;
using UnityEngine;

namespace Clashura.AI.Career
{
    /// <summary>
    /// Utility manager for querying difficulty tiers, display metadata, colors, and tags.
    /// </summary>
    public static class AIDifficultyManager
    {
        public static string GetDifficultyName(AIDifficulty difficulty)
        {
            return difficulty switch
            {
                AIDifficulty.Easy => "Easy",
                AIDifficulty.Medium => "Medium",
                AIDifficulty.Hard => "Hard",
                _ => "Medium"
            };
        }

        public static string GetDifficultyTierTag(AIDifficulty difficulty)
        {
            return difficulty switch
            {
                AIDifficulty.Easy => "[ NOVICE · TIER I ]",
                AIDifficulty.Medium => "[ BALANCED · TIER II ]",
                AIDifficulty.Hard => "[ EXPERT · TIER III ]",
                _ => "[ BALANCED · TIER II ]"
            };
        }

        public static Color GetDifficultyColor(AIDifficulty difficulty)
        {
            return difficulty switch
            {
                AIDifficulty.Easy => new Color(0.14f, 0.88f, 0.48f, 1f),    // Emerald
                AIDifficulty.Medium => new Color(0.98f, 0.80f, 0.22f, 1f),  // Celestial Gold
                AIDifficulty.Hard => new Color(0.96f, 0.32f, 0.32f, 1f),    // Blazing Crimson
                _ => Color.white
            };
        }

        public static int GetEstimatedAIRating(AIDifficulty difficulty)
        {
            return difficulty switch
            {
                AIDifficulty.Easy => 900,
                AIDifficulty.Medium => 1250,
                AIDifficulty.Hard => 1650,
                _ => 1250
            };
        }
    }
}
