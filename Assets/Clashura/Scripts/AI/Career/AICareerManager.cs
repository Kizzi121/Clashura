using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Clashura.Characters.Managers;
using Clashura.Multiplayer;

namespace Clashura.AI.Career
{
    /// <summary>
    /// Master coordinator for AI Career Mode across scenes:
    /// - Orchestrates New Game / Continue flow
    /// - Manages Career Save/Load states
    /// - Coordinates 3v3 team generation (Player 3 vs NOVA 3)
    /// - Handles scene transitions
    /// </summary>
    public class AICareerManager : MonoBehaviour
    {
        public const string SceneMainMenu = "MainMenu";
        public const string SceneAICareerMenu = "AICareerMenu";
        public const string SceneBetterWithAI = "BetterWithAI";
        public const string SceneAIVersus = "AIVersusScreen";

        private static AICareerManager _instance;
        private static bool _isShuttingDown = false;

        public static bool HasInstance => _instance != null && !_isShuttingDown;

        public static AICareerManager Instance
        {
            get
            {
                if (_isShuttingDown) return null;
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AICareerManager>();
                    if (_instance == null && Application.isPlaying && !_isShuttingDown)
                    {
                        GameObject go = new GameObject("[AICareerManager]");
                        _instance = go.AddComponent<AICareerManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Active Career Session")]
        [SerializeField] private AICareerData activeCareer;

        public AICareerData ActiveCareer => activeCareer;
        public bool HasActiveCareer => activeCareer != null && activeCareer.hasActiveCareer;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Load existing career if available
            if (AICareerSaveManager.HasSavedCareer())
            {
                activeCareer = AICareerSaveManager.LoadCareer();
            }
            else
            {
                activeCareer = new AICareerData();
            }
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
        /// Prepares career data for a fresh start without triggering a scene reload.
        /// Used by unified multi-panel UI.
        /// </summary>
        public void PrepareNewCareerData(AIDifficulty difficulty)
        {
            Debug.Log($"[AICareerManager] Preparing New AI Career data for difficulty: {difficulty}...");

            // 1. Reset old career
            AICareerSaveManager.ClearCareer();

            // 2. Select exactly 3 AI characters
            var randomAIEntries = AIRandomSelector.SelectRandomAITeam(difficulty);
            List<string> aiIds = randomAIEntries.Select(e => e.characterId).ToList();

            // 3. Read Player's 3 characters
            List<string> playerIds = AITeamManager.GetPlayerTeamIds();

            // 4. Build and save new career data
            activeCareer = new AICareerData
            {
                hasActiveCareer = true,
                difficulty = (int)difficulty,
                careerLevel = 1,
                completedMatches = 0,
                aiDisplayName = AIOpponentController.FixedAIDisplayName,
                playerCharacterIds = playerIds,
                aiCharacterIds = aiIds,
                saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            AICareerSaveManager.SaveCareer(activeCareer);
            BetterWithAIState.SelectedDifficulty = difficulty;
        }

        /// <summary>
        /// Prepares career data for continue without triggering a scene reload.
        /// Used by unified multi-panel UI.
        /// </summary>
        public bool PrepareContinueCareerData()
        {
            if (!AICareerSaveManager.HasSavedCareer())
            {
                Debug.LogWarning("[AICareerManager] No saved AI Career found to continue!");
                return false;
            }

            activeCareer = AICareerSaveManager.LoadCareer();
            Debug.Log($"[AICareerManager] Continuing AI Career data: Level {activeCareer.careerLevel}, Difficulty {activeCareer.DifficultyEnum}");

            // Ensure player team is populated
            if (activeCareer.playerCharacterIds == null || activeCareer.playerCharacterIds.Count < 3)
            {
                activeCareer.playerCharacterIds = AITeamManager.GetPlayerTeamIds();
            }

            // Ensure AI team has 3 characters
            if (activeCareer.aiCharacterIds == null || activeCareer.aiCharacterIds.Count < 3)
            {
                var aiList = AIRandomSelector.SelectRandomAITeam(activeCareer.DifficultyEnum);
                activeCareer.aiCharacterIds = aiList.Select(e => e.characterId).ToList();
                AICareerSaveManager.SaveCareer(activeCareer);
            }

            BetterWithAIState.SelectedDifficulty = activeCareer.DifficultyEnum;
            return true;
        }

        /// <summary>
        /// Starts a completely new AI Career with the specified difficulty:
        /// 1. Resets old AI career progress.
        /// 2. Samples exactly 3 random AI characters from the selected difficulty tier.
        /// 3. Reads player's authentic 3 characters.
        /// 4. Saves career data persistently.
        /// 5. Transitions to 3v3 Versus Screen.
        /// </summary>
        public void StartNewCareerWithDifficulty(AIDifficulty difficulty)
        {
            PrepareNewCareerData(difficulty);
            LoadAIVersusScreen();
        }

        /// <summary>
        /// Continues the saved AI Career:
        /// 1. Loads saved difficulty, progress, and character teams.
        /// 2. If AI team is empty, generates random 3 characters from saved difficulty.
        /// 3. Transitions to 3v3 Versus Screen without restarting.
        /// </summary>
        public bool ContinueCareer()
        {
            bool prepared = PrepareContinueCareerData();
            if (!prepared) return false;

            LoadAIVersusScreen();
            return true;
        }

        #region Navigation Helpers

        public void LoadAICareerMenu()
        {
            if (AICareerMasterUI.Instance != null)
            {
                AICareerMasterUI.Instance.ShowCareerMenu();
            }
            else
            {
                SceneManager.LoadScene(SceneAICareerMenu);
            }
        }

        public void LoadDifficultySelection()
        {
            if (AICareerMasterUI.Instance != null)
            {
                AICareerMasterUI.Instance.ShowDifficultySelection();
            }
            else
            {
                SceneManager.LoadScene(SceneAICareerMenu);
            }
        }

        public void LoadAIVersusScreen()
        {
            if (AICareerMasterUI.Instance != null)
            {
                AICareerMasterUI.Instance.ShowVersusScreen();
            }
            else
            {
                SceneManager.LoadScene(SceneAICareerMenu);
            }
        }

        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene(SceneMainMenu);
        }

        #endregion
    }
}
