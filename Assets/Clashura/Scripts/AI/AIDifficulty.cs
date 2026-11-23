using System;
using UnityEngine;

namespace Clashura.AI
{
    /// <summary>
    /// Supported AI difficulty tiers for the "Better with AI" game mode.
    /// </summary>
    public enum AIDifficulty
    {
        Easy = 0,
        Medium = 1,
        Hard = 2
    }

    /// <summary>
    /// Persistent state and metadata helper for Better with AI mode.
    /// Stores the selected difficulty in PlayerPrefs and provides helper methods
    /// for UI labels, color accents, and descriptions.
    /// </summary>
    public static class BetterWithAIState
    {
        public const string SelectedDifficultyPrefKey = "CLASHURA_AI_DIFFICULTY";

        /// <summary>
        /// Authoritative player-selected AI difficulty.
        /// Persisted across application restarts via PlayerPrefs.
        /// </summary>
        public static AIDifficulty SelectedDifficulty
        {
            get
            {
                int val = PlayerPrefs.GetInt(SelectedDifficultyPrefKey, (int)AIDifficulty.Medium);
                return (AIDifficulty)Mathf.Clamp(val, (int)AIDifficulty.Easy, (int)AIDifficulty.Hard);
            }
            set
            {
                PlayerPrefs.SetInt(SelectedDifficultyPrefKey, (int)value);
                PlayerPrefs.Save();
                OnDifficultyChanged?.Invoke(value);
            }
        }

        public static event Action<AIDifficulty> OnDifficultyChanged;

        public static string GetDifficultyName(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy: return "EASY";
                case AIDifficulty.Medium: return "MEDIUM";
                case AIDifficulty.Hard: return "HARD";
                default: return "MEDIUM";
            }
        }

        public static string GetDifficultyTag(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy: return "NOVICE · TRAINING";
                case AIDifficulty.Medium: return "BALANCED · STANDARD";
                case AIDifficulty.Hard: return "EXPERT · MASTER";
                default: return "STANDARD";
            }
        }

        public static string GetDifficultyDescription(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    return "Novice AI · Slower reaction times & forgiving attack windows.";
                case AIDifficulty.Medium:
                    return "Balanced AI · Standard combat tempo, tactical spacing & blocking.";
                case AIDifficulty.Hard:
                    return "Relentless AI · Frame-perfect parries, ruthless combos & aggressive pressure.";
                default:
                    return "Standard combat tempo and tactical spacing.";
            }
        }

        public static Color GetDifficultyColor(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    return new Color(0.13f, 0.77f, 0.37f, 1.0f); // Emerald #22C55E
                case AIDifficulty.Medium:
                    return new Color(0.96f, 0.62f, 0.04f, 1.0f); // Amber Gold #F59E0B
                case AIDifficulty.Hard:
                    return new Color(0.94f, 0.27f, 0.27f, 1.0f); // Crimson #EF4444
                default:
                    return Color.white;
            }
        }
    }
}
