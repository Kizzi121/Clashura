using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Clashura.Characters.Data;
using Clashura.Characters.Managers;
using Clashura.Multiplayer;

namespace Clashura.AI.Career
{
    /// <summary>
    /// Master controller for the 3v3 Player vs AI Versus Screen (AIVersusScreen.unity).
    /// Displays:
    /// - Left: Real Player Display Name, Rating, Level, and 3 Authentic Selected Characters
    /// - Center: Grand VS Emblem, Difficulty Tier Badge, Match Title, and Ready Countdown
    /// - Right: Permanent Opponent "NOVA", Rating, Level, and 3 Randomly Selected AI Characters
    /// </summary>
    public class AIVSScreenController : MonoBehaviour
    {
        [Header("Databases")]
        [SerializeField] private CharacterDatabaseSO masterDatabase;
        [SerializeField] private AICharacterDatabase aiDatabase;

        [Header("Center VS Elements")]
        [SerializeField] private TextMeshProUGUI matchTitleText;
        [SerializeField] private TextMeshProUGUI arenaNameText;
        [SerializeField] private TextMeshProUGUI difficultyTierText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private RectTransform vsBadgeRect;
        [SerializeField] private Image vsGlowImage;

        [Header("Player (Left Side)")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI playerLevelText;
        [SerializeField] private TextMeshProUGUI playerRatingText;
        [SerializeField] private TextMeshProUGUI playerTagText;
        [SerializeField] private Image[] playerHeroPortraits = new Image[3];
        [SerializeField] private TextMeshProUGUI[] playerHeroNames = new TextMeshProUGUI[3];
        [SerializeField] private TextMeshProUGUI[] playerHeroRoles = new TextMeshProUGUI[3];
        [SerializeField] private GameObject[] playerHeroSlots = new GameObject[3];

        [Header("AI Opponent (Right Side - NOVA)")]
        [SerializeField] private TextMeshProUGUI aiNameText;
        [SerializeField] private TextMeshProUGUI aiLevelText;
        [SerializeField] private TextMeshProUGUI aiRatingText;
        [SerializeField] private TextMeshProUGUI aiTagText;
        [SerializeField] private Image[] aiHeroPortraits = new Image[3];
        [SerializeField] private TextMeshProUGUI[] aiHeroNames = new TextMeshProUGUI[3];
        [SerializeField] private TextMeshProUGUI[] aiHeroRoles = new TextMeshProUGUI[3];
        [SerializeField] private GameObject[] aiHeroSlots = new GameObject[3];

        [Header("Action Buttons")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button startMatchButton;

        private AIDifficulty _currentDifficulty = AIDifficulty.Medium;
        private List<string> _playerTeamIds = new List<string>();
        private List<string> _aiTeamIds = new List<string>();

        public string DisplayedPlayerName => playerNameText != null ? playerNameText.text : string.Empty;
        public string DisplayedAIName => aiNameText != null ? aiNameText.text : string.Empty;
        public IReadOnlyList<string> CurrentPlayerTeamIds => _playerTeamIds;
        public IReadOnlyList<string> CurrentAITeamIds => _aiTeamIds;
        public AIDifficulty CurrentDifficulty => _currentDifficulty;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            if (startMatchButton != null) startMatchButton.onClick.AddListener(OnStartMatchClicked);
        }

        private void Start()
        {
            InitializeDatabases();
            LoadAndDisplayMatchup();
        }

        private void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
            if (startMatchButton != null) startMatchButton.onClick.RemoveListener(OnStartMatchClicked);
        }

        private void InitializeDatabases()
        {
            if (masterDatabase == null)
            {
                masterDatabase = Resources.Load<CharacterDatabaseSO>("CharacterDatabase");
                if (masterDatabase == null)
                {
#if UNITY_EDITOR
                    masterDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>("Assets/Clashura/Data/Characters/ScriptableObjects/CharacterDatabase.asset");
#endif
                }
            }

            if (aiDatabase == null)
            {
                aiDatabase = AICharacterDatabase.Instance;
                if (aiDatabase == null)
                {
                    aiDatabase = FindFirstObjectByType<AICharacterDatabase>();
                }
            }
        }

        /// <summary>
        /// Reads active career data or current state, resolves characters, and populates the 3v3 matchup.
        /// </summary>
        public void LoadAndDisplayMatchup()
        {
            AICareerData career = AICareerSaveManager.LoadCareer();

            _currentDifficulty = career.hasActiveCareer ? career.DifficultyEnum : BetterWithAIState.SelectedDifficulty;

            // 1. Resolve Player Team (3 Characters)
            if (career.hasActiveCareer && career.playerCharacterIds != null && career.playerCharacterIds.Count == 3)
            {
                _playerTeamIds = new List<string>(career.playerCharacterIds);
            }
            else
            {
                _playerTeamIds = AITeamManager.GetPlayerTeamIds(masterDatabase);
            }

            // 2. Resolve AI Team (3 Characters from selected difficulty)
            if (career.hasActiveCareer && career.aiCharacterIds != null && career.aiCharacterIds.Count == 3)
            {
                _aiTeamIds = new List<string>(career.aiCharacterIds);
            }
            else
            {
                var randomAI = AIRandomSelector.SelectRandomAITeam(_currentDifficulty, aiDatabase);
                _aiTeamIds = new List<string>();
                foreach (var entry in randomAI)
                {
                    _aiTeamIds.Add(entry.characterId);
                }

                // Persist new matchup
                career.hasActiveCareer = true;
                career.difficulty = (int)_currentDifficulty;
                career.playerCharacterIds = _playerTeamIds;
                career.aiCharacterIds = _aiTeamIds;
                career.aiDisplayName = AIOpponentController.FixedAIDisplayName;
                AICareerSaveManager.SaveCareer(career);
            }

            // 3. Populate Header & Center VS
            PopulateCenterVS(career);

            // 4. Populate Player 1 Side
            PopulatePlayerSide();

            // 5. Populate AI Opponent Side (NOVA)
            PopulateAISide(career);
        }

        private void PopulateCenterVS(AICareerData career)
        {
            if (matchTitleText != null)
            {
                matchTitleText.text = "AI CAREER 3v3 SHOWDOWN";
            }

            if (arenaNameText != null)
            {
                arenaNameText.text = "ARENA: CELESTIAL STADIUM";
            }

            if (difficultyTierText != null)
            {
                string diffName = AIDifficultyManager.GetDifficultyName(_currentDifficulty).ToUpper();
                string diffTag = AIDifficultyManager.GetDifficultyTierTag(_currentDifficulty);
                difficultyTierText.text = $"{diffName} · {diffTag}";
                difficultyTierText.color = AIDifficultyManager.GetDifficultyColor(_currentDifficulty);
            }

            if (countdownText != null)
            {
                countdownText.text = "COMBATANTS READY";
            }
        }

        private void PopulatePlayerSide()
        {
            // Authoritative player identity
            var localProfile = PlayerMatchProfile.CreateLocalProfile(isHost: true);

            if (playerNameText != null)
            {
                playerNameText.text = localProfile.playerName;
            }

            if (playerLevelText != null)
            {
                playerLevelText.text = $"LVL {localProfile.playerLevel}";
            }

            if (playerRatingText != null)
            {
                playerRatingText.text = $"{localProfile.playerPoints} PTS";
            }

            if (playerTagText != null)
            {
                playerTagText.text = "[ PLAYER 1 · COMMANDER ]";
            }

            // Populate 3 Player Characters
            for (int i = 0; i < 3; i++)
            {
                if (i < _playerTeamIds.Count && masterDatabase != null)
                {
                    string charId = _playerTeamIds[i];
                    var charData = masterDatabase.GetCharacterById(charId);

                    if (playerHeroNames[i] != null)
                    {
                        playerHeroNames[i].text = charData != null ? charData.CharacterName.ToUpper() : $"HERO 0{i + 1}";
                    }

                    if (playerHeroRoles[i] != null)
                    {
                        playerHeroRoles[i].text = charData != null ? charData.Role.ToString().ToUpper() : "VANGUARD";
                    }

                    if (playerHeroPortraits[i] != null && charData != null && charData.Portrait != null)
                    {
                        playerHeroPortraits[i].sprite = charData.Portrait;
                        playerHeroPortraits[i].color = Color.white;
                    }

                    if (playerHeroSlots[i] != null)
                    {
                        playerHeroSlots[i].SetActive(true);
                    }
                }
            }
        }

        private void PopulateAISide(AICareerData career)
        {
            // Authoritative AI Profile - ALWAYS NOVA
            var aiProfile = AIOpponentController.CreateNOVAProfile(_currentDifficulty, _aiTeamIds);

            if (aiNameText != null)
            {
                aiNameText.text = AIOpponentController.FixedAIDisplayName; // STRICT REQUIREMENT: NOVA
            }

            if (aiLevelText != null)
            {
                aiLevelText.text = $"LVL {aiProfile.playerLevel}";
            }

            if (aiRatingText != null)
            {
                aiRatingText.text = $"{aiProfile.playerPoints} PTS";
            }

            if (aiTagText != null)
            {
                aiTagText.text = $"[ {AIOpponentController.OpponentSubtitle} ]";
                aiTagText.color = AIDifficultyManager.GetDifficultyColor(_currentDifficulty);
            }

            // Populate 3 AI Characters
            for (int i = 0; i < 3; i++)
            {
                if (i < _aiTeamIds.Count && masterDatabase != null)
                {
                    string charId = _aiTeamIds[i];
                    var charData = masterDatabase.GetCharacterById(charId);

                    if (aiHeroNames[i] != null)
                    {
                        aiHeroNames[i].text = charData != null ? charData.CharacterName.ToUpper() : $"AI COMBATANT 0{i + 1}";
                    }

                    if (aiHeroRoles[i] != null)
                    {
                        aiHeroRoles[i].text = charData != null ? charData.Role.ToString().ToUpper() : "TACTICAL";
                    }

                    if (aiHeroPortraits[i] != null && charData != null && charData.Portrait != null)
                    {
                        aiHeroPortraits[i].sprite = charData.Portrait;
                        aiHeroPortraits[i].color = Color.white;
                    }

                    if (aiHeroSlots[i] != null)
                    {
                        aiHeroSlots[i].SetActive(true);
                    }
                }
            }
        }

        #region Buttons

        public void OnBackClicked()
        {
            Debug.Log("[AIVSScreenController] Returning to AI Career Menu...");
            SceneManager.LoadScene(AICareerManager.SceneAICareerMenu);
        }

        public void OnStartMatchClicked()
        {
            Debug.Log("[AIVSScreenController] Match start requested! (Stage 1 Complete - Awaiting AI Combat Logic)");
            if (countdownText != null)
            {
                countdownText.text = "MATCH READY - COMBAT SIMULATION INITIALIZED";
            }
        }

        #endregion
    }
}
