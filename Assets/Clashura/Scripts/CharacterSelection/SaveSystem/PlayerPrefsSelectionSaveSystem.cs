using System;
using UnityEngine;

namespace Clashura.CharacterSelection.SaveSystem
{
    /// <summary>
    /// Default persistent save implementation using JSON serialization stored in Unity PlayerPrefs.
    /// </summary>
    public class PlayerPrefsSelectionSaveSystem : ISelectionSaveSystem
    {
        private const string KeyPrefix = "CLASHURA_SELECTION_";

        public void SaveSelection(PlayerSelectionData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.PlayerId))
            {
                Debug.LogError("[SaveSystem] Cannot save null or invalid selection data.");
                return;
            }

            try
            {
                data.SaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string json = JsonUtility.ToJson(data);
                string key = GetKey(data.PlayerId);
                PlayerPrefs.SetString(key, json);
                PlayerPrefs.Save();
                Debug.Log($"[SaveSystem] Successfully saved character selection for {data.PlayerId}: {string.Join(", ", data.SelectedCharacterIds)}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Error saving selection data: {ex.Message}");
            }
        }

        public PlayerSelectionData LoadSelection(string playerId)
        {
            string key = GetKey(playerId);
            if (!PlayerPrefs.HasKey(key))
            {
                Debug.Log($"[SaveSystem] No saved selection found for {playerId}. Returning default.");
                return new PlayerSelectionData(playerId, null);
            }

            try
            {
                string json = PlayerPrefs.GetString(key);
                var data = JsonUtility.FromJson<PlayerSelectionData>(json);
                if (data != null)
                {
                    return data;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to parse saved selection data for {playerId}: {ex.Message}");
            }

            return new PlayerSelectionData(playerId, null);
        }

        public bool HasSavedSelection(string playerId)
        {
            return PlayerPrefs.HasKey(GetKey(playerId));
        }

        public void DeleteSelection(string playerId)
        {
            string key = GetKey(playerId);
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                Debug.Log($"[SaveSystem] Cleared selection data for {playerId}.");
            }
        }

        private static string GetKey(string playerId)
        {
            return KeyPrefix + (string.IsNullOrWhiteSpace(playerId) ? "Player_Local" : playerId.Trim());
        }
    }
}
