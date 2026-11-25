using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Clashura.AI.Career
{
    /// <summary>
    /// Master coordinator for the Unified AI Career Mode screen.
    /// Manages all 3 modular panels inside ONE single scene without scene reloads:
    /// - Panel 1: AI Career Menu (New Game, Continue, Confirmation Dialog)
    /// - Panel 2: Difficulty Selection (Better With AI: Easy, Medium, Hard)
    /// - Panel 3: 3v3 Versus Screen (Player Team vs permanent NOVA AI Team)
    /// </summary>
    public class AICareerMasterUI : MonoBehaviour
    {
        public enum PanelType
        {
            CareerMenu = 0,
            DifficultySelect = 1,
            AIVersus = 2
        }

        private static AICareerMasterUI _instance;
        public static AICareerMasterUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AICareerMasterUI>();
                }
                return _instance;
            }
        }

        [Header("Master Panels")]
        [SerializeField] private GameObject careerMenuPanel;
        [SerializeField] private GameObject difficultySelectPanel;
        [SerializeField] private GameObject versusScreenPanel;
        [SerializeField] private GameObject confirmationModalPanel;

        [Header("Sub-Controllers")]
        [SerializeField] private AICareerUI careerUI;
        [SerializeField] private BetterWithAIController difficultyUI;
        [SerializeField] private AIVSScreenController versusUI;

        [Header("Header / Mode Badge")]
        [SerializeField] private TextMeshProUGUI masterModeBadge;

        private PanelType _currentPanel = PanelType.CareerMenu;
        public PanelType CurrentPanel => _currentPanel;

        public GameObject CareerMenuPanel => careerMenuPanel;
        public GameObject DifficultySelectPanel => difficultySelectPanel;
        public GameObject VersusScreenPanel => versusScreenPanel;
        public GameObject ConfirmationModalPanel => confirmationModalPanel;

        public AICareerUI CareerUI => careerUI;
        public BetterWithAIController DifficultyUI => difficultyUI;
        public AIVSScreenController VersusUI => versusUI;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Start()
        {
            // Default to starting on Career Menu panel
            ShowCareerMenu();
        }

        /// <summary>
        /// Activates Panel 1: AI Career Menu (New Game / Continue / Confirmation Dialog).
        /// </summary>
        public void ShowCareerMenu()
        {
            _currentPanel = PanelType.CareerMenu;

            if (careerMenuPanel != null) careerMenuPanel.SetActive(true);
            if (difficultySelectPanel != null) difficultySelectPanel.SetActive(false);
            if (versusScreenPanel != null) versusScreenPanel.SetActive(false);
            if (confirmationModalPanel != null) confirmationModalPanel.SetActive(false);

            if (careerUI != null)
            {
                careerUI.RefreshCareerStatus();
            }

            if (masterModeBadge != null)
            {
                masterModeBadge.text = "AI CAREER · CAMPAIGN";
            }

            Debug.Log("[AICareerMasterUI] Seamlessly switched to Panel 1: AI Career Menu");
        }

        /// <summary>
        /// Activates Panel 2: Difficulty Selection (Better With AI: Easy, Medium, Hard).
        /// </summary>
        public void ShowDifficultySelection()
        {
            _currentPanel = PanelType.DifficultySelect;

            if (careerMenuPanel != null) careerMenuPanel.SetActive(false);
            if (difficultySelectPanel != null) difficultySelectPanel.SetActive(true);
            if (versusScreenPanel != null) versusScreenPanel.SetActive(false);
            if (confirmationModalPanel != null) confirmationModalPanel.SetActive(false);

            if (difficultyUI != null)
            {
                difficultyUI.ApplyDifficultySelection(BetterWithAIState.SelectedDifficulty, logMessage: false);
            }

            if (masterModeBadge != null)
            {
                masterModeBadge.text = "AI CAREER · DIFFICULTY";
            }

            Debug.Log("[AICareerMasterUI] Seamlessly switched to Panel 2: Difficulty Selection");
        }

        /// <summary>
        /// Activates Panel 3: 3v3 Versus Screen (Player Team vs NOVA AI Team).
        /// </summary>
        public void ShowVersusScreen(AIDifficulty? difficulty = null)
        {
            _currentPanel = PanelType.AIVersus;

            if (careerMenuPanel != null) careerMenuPanel.SetActive(false);
            if (difficultySelectPanel != null) difficultySelectPanel.SetActive(false);
            if (versusScreenPanel != null) versusScreenPanel.SetActive(true);
            if (confirmationModalPanel != null) confirmationModalPanel.SetActive(false);

            if (difficulty.HasValue)
            {
                BetterWithAIState.SelectedDifficulty = difficulty.Value;
            }

            if (versusUI != null)
            {
                versusUI.LoadAndDisplayMatchup();
            }

            if (masterModeBadge != null)
            {
                masterModeBadge.text = "3v3 AI ARENA · SHOWDOWN";
            }

            Debug.Log($"[AICareerMasterUI] Seamlessly switched to Panel 3: 3v3 Versus Screen (Difficulty: {BetterWithAIState.SelectedDifficulty})");
        }

        /// <summary>
        /// Returns to Main Menu scene.
        /// </summary>
        public void ReturnToMainMenu()
        {
            Debug.Log("[AICareerMasterUI] Returning to Main Menu...");
            SceneManager.LoadScene(AICareerManager.SceneMainMenu);
        }
    }
}
