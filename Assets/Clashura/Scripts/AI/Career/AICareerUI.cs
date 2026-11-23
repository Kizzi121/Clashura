using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Clashura.AI.Career
{
    /// <summary>
    /// Controller for the AI Career Menu screen (AICareerMenu.unity).
    /// Manages:
    /// - NEW GAME button with conditional confirmation popup
    /// - CONTINUE button with save-state validation
    /// - Confirmation Dialog: "Are you sure you want to clear your previous progress?" (YES/NO)
    /// - Navigation back to Main Menu
    /// </summary>
    public class AICareerUI : MonoBehaviour
    {
        [Header("Menu Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button backButton;

        [Header("Continue Button Visual State")]
        [SerializeField] private CanvasGroup continueCanvasGroup;
        [SerializeField] private TextMeshProUGUI continueSubtitleText;

        [Header("Confirmation Panel")]
        [SerializeField] private GameObject confirmationPanelRoot;
        [SerializeField] private TextMeshProUGUI confirmationPromptText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        [Header("Status Feedback")]
        [SerializeField] private TextMeshProUGUI statusBannerText;

        private void Awake()
        {
            HookEvents();
        }

        private void Start()
        {
            RefreshCareerStatus();
        }

        private void OnDestroy()
        {
            UnhookEvents();
        }

        private void HookEvents()
        {
            if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClicked);
            if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);

            if (confirmYesButton != null) confirmYesButton.onClick.AddListener(OnConfirmYesClicked);
            if (confirmNoButton != null) confirmNoButton.onClick.AddListener(OnConfirmNoClicked);
        }

        private void UnhookEvents()
        {
            if (newGameButton != null) newGameButton.onClick.RemoveListener(OnNewGameClicked);
            if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
            if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);

            if (confirmYesButton != null) confirmYesButton.onClick.RemoveListener(OnConfirmYesClicked);
            if (confirmNoButton != null) confirmNoButton.onClick.RemoveListener(OnConfirmNoClicked);
        }

        /// <summary>
        /// Updates the visibility, interactability, and labels of the Continue button and status readout.
        /// </summary>
        public void RefreshCareerStatus()
        {
            bool hasCareer = AICareerSaveManager.HasSavedCareer();

            if (continueButton != null)
            {
                continueButton.interactable = hasCareer;
            }

            if (continueCanvasGroup != null)
            {
                continueCanvasGroup.alpha = hasCareer ? 1f : 0.45f;
            }

            if (continueSubtitleText != null)
            {
                if (hasCareer)
                {
                    var data = AICareerSaveManager.LoadCareer();
                    continueSubtitleText.text = $"RESUME · {data.DifficultyEnum.ToString().ToUpper()} (LVL {data.careerLevel})";
                    continueSubtitleText.color = AIDifficultyManager.GetDifficultyColor(data.DifficultyEnum);
                }
                else
                {
                    continueSubtitleText.text = "NO SAVED CAREER AVAILABLE";
                    continueSubtitleText.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                }
            }

            if (statusBannerText != null)
            {
                if (hasCareer)
                {
                    var data = AICareerSaveManager.LoadCareer();
                    statusBannerText.text = $"• ACTIVE CAREER: LEVEL {data.careerLevel} ({data.DifficultyEnum.ToString().ToUpper()}) •";
                }
                else
                {
                    statusBannerText.text = "• SELECT 'NEW GAME' TO START YOUR AI CAREER •";
                }
            }

            // Hide confirmation panel by default
            if (confirmationPanelRoot != null)
            {
                confirmationPanelRoot.SetActive(false);
            }
        }

        #region Button Handlers

        public void OnNewGameClicked()
        {
            bool hasCareer = AICareerSaveManager.HasSavedCareer();

            if (hasCareer)
            {
                // Open Confirmation Dialog
                ShowConfirmationDialog(true);
            }
            else
            {
                // Start a fresh career directly by proceeding to Difficulty Selection
                ProceedToDifficultySelection();
            }
        }

        public void OnContinueClicked()
        {
            if (!AICareerSaveManager.HasSavedCareer())
            {
                Debug.LogWarning("[AICareerUI] Continue clicked but no saved career exists.");
                if (statusBannerText != null)
                {
                    statusBannerText.text = "• NO SAVED CAREER FOUND. PLEASE START A NEW GAME •";
                }
                return;
            }

            // Continue the saved career
            bool success = AICareerManager.Instance.ContinueCareer();
            if (!success)
            {
                // Fallback to loading AIVersusScreen
                SceneManager.LoadScene(AICareerManager.SceneAIVersus);
            }
        }

        public void OnBackClicked()
        {
            Debug.Log("[AICareerUI] Returning to Main Menu...");
            SceneManager.LoadScene(AICareerManager.SceneMainMenu);
        }

        public void OnConfirmYesClicked()
        {
            Debug.Log("[AICareerUI] Confirmation YES clicked. Clearing previous AI Career...");
            // Clear ONLY previous AI Career progress
            AICareerSaveManager.ClearCareer();

            // Close dialog
            ShowConfirmationDialog(false);

            // Proceed to Difficulty Selection
            ProceedToDifficultySelection();
        }

        public void OnConfirmNoClicked()
        {
            Debug.Log("[AICareerUI] Confirmation NO clicked. Retaining previous AI Career progress.");
            // Close dialog without clearing anything
            ShowConfirmationDialog(false);
            RefreshCareerStatus();
        }

        #endregion

        private void ShowConfirmationDialog(bool show)
        {
            if (confirmationPanelRoot != null)
            {
                confirmationPanelRoot.SetActive(show);
                if (show)
                {
                    confirmationPanelRoot.transform.SetAsLastSibling();
                    if (confirmationPromptText != null)
                    {
                        confirmationPromptText.text = "Are you sure you want to clear your previous progress?";
                    }
                }
            }
        }

        private void ProceedToDifficultySelection()
        {
            Debug.Log("[AICareerUI] Loading Difficulty Selection (BetterWithAI)...");
            SceneManager.LoadScene(AICareerManager.SceneBetterWithAI);
        }
    }
}
