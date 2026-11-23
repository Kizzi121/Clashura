using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clashura.AI.Career
{
    /// <summary>
    /// Persistent data structure representing an active AI Career profile.
    /// Strictly isolated from PlayerPrefs accounts, PlayFab records, and multiplayer ratings.
    /// </summary>
    [Serializable]
    public class AICareerData
    {
        public bool hasActiveCareer = false;
        public int difficulty = (int)AIDifficulty.Medium; // 0=Easy, 1=Medium, 2=Hard
        public int careerLevel = 1;
        public int completedMatches = 0;
        public long saveTimestamp = 0;
        public List<string> playerCharacterIds = new List<string>();
        public List<string> aiCharacterIds = new List<string>();
        public string aiDisplayName = "NOVA";

        public AIDifficulty DifficultyEnum => (AIDifficulty)Mathf.Clamp(difficulty, 0, 2);

        public AICareerData()
        {
            hasActiveCareer = false;
            difficulty = (int)AIDifficulty.Medium;
            careerLevel = 1;
            completedMatches = 0;
            saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            playerCharacterIds = new List<string>();
            aiCharacterIds = new List<string>();
            aiDisplayName = "NOVA";
        }
    }

    /// <summary>
    /// Dedicated save manager for the AI Career Mode.
    /// Manages loading, saving, and safely resetting career progress without affecting
    /// PlayFab player profiles, player display name, rating points, or existing character unlocks.
    /// </summary>
    public static class AICareerSaveManager
    {
        public const string CareerSaveKey = "CLASHURA_AI_CAREER_SAVE";

        /// <summary>
        /// Checks whether a valid, active AI Career save exists.
        /// </summary>
        public static bool HasSavedCareer()
        {
            if (!PlayerPrefs.HasKey(CareerSaveKey)) return false;

            string json = PlayerPrefs.GetString(CareerSaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                var data = JsonUtility.FromJson<AICareerData>(json);
                return data != null && data.hasActiveCareer;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AICareerSaveManager] Error checking saved career: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads the existing AI Career data from PlayerPrefs.
        /// </summary>
        public static AICareerData LoadCareer()
        {
            if (!PlayerPrefs.HasKey(CareerSaveKey))
            {
                return new AICareerData();
            }

            try
            {
                string json = PlayerPrefs.GetString(CareerSaveKey, string.Empty);
                if (string.IsNullOrWhiteSpace(json)) return new AICareerData();

                var data = JsonUtility.FromJson<AICareerData>(json);
                return data ?? new AICareerData();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AICareerSaveManager] Failed to load career data: {ex.Message}");
                return new AICareerData();
            }
        }

        /// <summary>
        /// Saves AI Career data to PlayerPrefs.
        /// </summary>
        public static void SaveCareer(AICareerData data)
        {
            if (data == null)
            {
                Debug.LogError("[AICareerSaveManager] Cannot save null career data.");
                return;
            }

            try
            {
                data.hasActiveCareer = true;
                data.aiDisplayName = "NOVA"; // Enforce permanent AI display name
                data.saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                string json = JsonUtility.ToJson(data);
                PlayerPrefs.SetString(CareerSaveKey, json);
                PlayerPrefs.Save();
                Debug.Log($"[AICareerSaveManager] Saved AI Career (Difficulty: {data.DifficultyEnum}, Level: {data.careerLevel}, PlayerTeam: {data.playerCharacterIds.Count}, AITeam: {data.aiCharacterIds.Count})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AICareerSaveManager] Error saving career data: {ex.Message}");
            }
        }

        /// <summary>
        /// Resets and clears ONLY the AI Career save data.
        /// Strictly preserves PlayFab accounts, player name, player rating points, and character data.
        /// </summary>
        public static void ClearCareer()
        {
            if (PlayerPrefs.HasKey(CareerSaveKey))
            {
                PlayerPrefs.DeleteKey(CareerSaveKey);
                PlayerPrefs.Save();
                Debug.Log("[AICareerSaveManager] AI Career save cleared. All player identity, PlayFab and rating data preserved.");
            }
        }
    }
}
