using System;
using System.Collections.Generic;
using UnityEngine;
using Clashura.CharacterSelection.SaveSystem;

namespace Clashura.Multiplayer
{
    /// <summary>
    /// Central persistent manager for checking and managing player team selection.
    /// Preserves selected team across scene transitions and integrates with PlayerPrefs.
    /// </summary>
    public class TeamSelectionManager : MonoBehaviour
    {
        public const int RequiredTeamSize = 3;
        private const string PrefsKey = "CLASHURA_SELECTION_Player_Local";

        private static TeamSelectionManager _instance;
        private static bool _isShuttingDown = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _isShuttingDown = false;
            _instance = null;
        }

        public static TeamSelectionManager Instance
        {
            get
            {
                if (_isShuttingDown) return null;
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<TeamSelectionManager>();
                    if (_instance == null && Application.isPlaying && !_isShuttingDown)
                    {
                        GameObject go = new GameObject("[TeamSelectionManager]");
                        _instance = go.AddComponent<TeamSelectionManager>();
                    }
                }
                return _instance;
            }
        }

        private List<string> _cachedCharacterIds = new List<string>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCachedTeam();
        }

        private void OnApplicationQuit()
        {
            _isShuttingDown = true;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Checks whether the local player has selected a complete and valid team.
        /// </summary>
        public static bool HasSelectedTeam()
        {
            if (!PlayerPrefs.HasKey(PrefsKey))
            {
                return false;
            }

            try
            {
                string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
                if (string.IsNullOrWhiteSpace(json)) return false;

                var data = JsonUtility.FromJson<PlayerSelectionData>(json);
                return data != null && 
                       data.SelectedCharacterIds != null && 
                       data.SelectedCharacterIds.Count >= RequiredTeamSize;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TeamSelectionManager] Error checking team selection: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Retrieves the saved list of selected character IDs.
        /// </summary>
        public static List<string> GetSelectedTeam()
        {
            if (!PlayerPrefs.HasKey(PrefsKey))
            {
                return new List<string>();
            }

            try
            {
                string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
                var data = JsonUtility.FromJson<PlayerSelectionData>(json);
                return data?.SelectedCharacterIds ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets the current number of selected characters.
        /// </summary>
        public static int GetSelectedCount()
        {
            return GetSelectedTeam().Count;
        }

        /// <summary>
        /// Saves a selected team to PlayerPrefs and caches it.
        /// </summary>
        public static void SaveTeam(List<string> characterIds)
        {
            if (characterIds == null || characterIds.Count == 0)
            {
                ClearTeam();
                return;
            }

            var data = new PlayerSelectionData("Player_Local", characterIds);
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.Save();

            if (Instance != null)
            {
                Instance._cachedCharacterIds = new List<string>(characterIds);
            }

            Debug.Log($"[TeamSelectionManager] Successfully saved team of {characterIds.Count} heroes.");
        }

        /// <summary>
        /// Clears the saved team selection.
        /// </summary>
        public static void ClearTeam()
        {
            if (PlayerPrefs.HasKey(PrefsKey))
            {
                PlayerPrefs.DeleteKey(PrefsKey);
                PlayerPrefs.Save();
            }

            if (Instance != null)
            {
                Instance._cachedCharacterIds.Clear();
            }

            Debug.Log("[TeamSelectionManager] Cleared team selection.");
        }

        private void LoadCachedTeam()
        {
            _cachedCharacterIds = GetSelectedTeam();
        }
    }
}
