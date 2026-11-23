using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Clashura.AI.Career;

namespace Clashura.AI
{
    /// <summary>
    /// Master Controller for the "Better with AI" game mode screen.
    /// Manages the three difficulty tiers (Easy, Medium, Hard) with premier fighting-game UI aesthetics:
    /// - Dynamic card sprite switching (Normal vs Glowing Selected)
    /// - Character portrait lighting (Full brilliance when selected, ambient shadow when inactive)
    /// - Active selection pill badges and tactile scale highlights (1.025x)
    /// - Streamlined glowing status readout
    /// - Strict adherence: Stores selected difficulty without launching gameplay yet
    /// </summary>
    public class BetterWithAIController : MonoBehaviour
    {
        [Header("Header Elements")]
        [Tooltip("Screen title component displaying 'BETTER WITH AI'.")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Tooltip("Screen subtitle component displaying 'CHOOSE YOUR DIFFICULTY'.")]
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Tooltip("Back to Main Menu button.")]
        [SerializeField] private Button backButton;

        [Header("Difficulty Buttons (Vertical Hierarchy)")]
        [SerializeField] private Button easyButton;
        [SerializeField] private Button mediumButton;
        [SerializeField] private Button hardButton;

        [Header("Card Background Images")]
        [SerializeField] private Image easyCardImage;
        [SerializeField] private Image mediumCardImage;
        [SerializeField] private Image hardCardImage;

        [Header("Character Portrait Busts")]
        [SerializeField] private Image easyCharPortrait;
        [SerializeField] private Image mediumCharPortrait;
        [SerializeField] private Image hardCharPortrait;

        [Header("Card Normal & Selected Sprites")]
        [SerializeField] private Sprite easyNormalSprite;
        [SerializeField] private Sprite easySelectedSprite;
        [SerializeField] private Sprite mediumNormalSprite;
        [SerializeField] private Sprite mediumSelectedSprite;
        [SerializeField] private Sprite hardNormalSprite;
        [SerializeField] private Sprite hardSelectedSprite;

        [Header("Selection Badges ('[ SELECTED ]')")]
        [SerializeField] private GameObject easyActiveBadge;
        [SerializeField] private GameObject mediumActiveBadge;
        [SerializeField] private GameObject hardActiveBadge;

        [Header("Card RectTransforms for Scaling")]
        [SerializeField] private RectTransform easyCardRect;
        [SerializeField] private RectTransform mediumCardRect;
        [SerializeField] private RectTransform hardCardRect;

        [Header("Status Banner")]
        [SerializeField] private TextMeshProUGUI statusTitleText;

        [Header("Scene Navigation")]
        [SerializeField] private string careerMenuSceneName = "AICareerMenu";
        [SerializeField] private bool autoAdvanceToVersus = true;

        public AIDifficulty CurrentDifficulty => BetterWithAIState.SelectedDifficulty;

        private void Awake()
        {
            HookButtonEvents();
        }

        private void Start()
        {
            // Set required screen typography
            if (titleText != null)
            {
                titleText.text = "BETTER WITH AI";
            }
            if (subtitleText != null)
            {
                subtitleText.text = "CHOOSE YOUR DIFFICULTY";
            }

            // Restore previously stored difficulty from persistent state
            AIDifficulty storedDiff = BetterWithAIState.SelectedDifficulty;
            ApplyDifficultySelection(storedDiff, logMessage: false);
        }

        private void OnDestroy()
        {
            UnhookButtonEvents();
        }

        private void HookButtonEvents()
        {
            if (easyButton != null) easyButton.onClick.AddListener(OnEasyClicked);
            if (mediumButton != null) mediumButton.onClick.AddListener(OnMediumClicked);
            if (hardButton != null) hardButton.onClick.AddListener(OnHardClicked);
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        }

        private void UnhookButtonEvents()
        {
            if (easyButton != null) easyButton.onClick.RemoveListener(OnEasyClicked);
            if (mediumButton != null) mediumButton.onClick.RemoveListener(OnMediumClicked);
            if (hardButton != null) hardButton.onClick.RemoveListener(OnHardClicked);
            if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
        }

        #region Button Click Handlers

        public void OnEasyClicked()
        {
            ApplyDifficultySelection(AIDifficulty.Easy, logMessage: true);
            if (autoAdvanceToVersus)
            {
                StartCoroutine(TransitionToVersusRoutine(AIDifficulty.Easy));
            }
        }

        public void OnMediumClicked()
        {
            ApplyDifficultySelection(AIDifficulty.Medium, logMessage: true);
            if (autoAdvanceToVersus)
            {
                StartCoroutine(TransitionToVersusRoutine(AIDifficulty.Medium));
            }
        }

        public void OnHardClicked()
        {
            ApplyDifficultySelection(AIDifficulty.Hard, logMessage: true);
            if (autoAdvanceToVersus)
            {
                StartCoroutine(TransitionToVersusRoutine(AIDifficulty.Hard));
            }
        }

        private IEnumerator TransitionToVersusRoutine(AIDifficulty difficulty)
        {
            yield return new WaitForSeconds(0.25f);
            AICareerManager.Instance.StartNewCareerWithDifficulty(difficulty);
        }

        public void OnBackClicked()
        {
            Debug.Log($"[BetterWithAIController] Returning to {careerMenuSceneName}...");
            SceneManager.LoadScene(careerMenuSceneName);
        }

        #endregion

        /// <summary>
        /// Stores the selected difficulty and updates visual indicators across the UI.
        /// Strictly does not launch gameplay at this stage.
        /// </summary>
        public void ApplyDifficultySelection(AIDifficulty difficulty, bool logMessage = true)
        {
            BetterWithAIState.SelectedDifficulty = difficulty;

            if (logMessage)
            {
                Debug.Log($"[BetterWithAIController] AI Difficulty set to: {difficulty} (Stored in PlayerPrefs: {BetterWithAIState.SelectedDifficultyPrefKey})");
            }

            // 1. Update Card Sprites (Normal vs Glowing Selected)
            if (easyCardImage != null)
            {
                easyCardImage.sprite = (difficulty == AIDifficulty.Easy) ? easySelectedSprite : easyNormalSprite;
            }
            if (mediumCardImage != null)
            {
                mediumCardImage.sprite = (difficulty == AIDifficulty.Medium) ? mediumSelectedSprite : mediumNormalSprite;
            }
            if (hardCardImage != null)
            {
                hardCardImage.sprite = (difficulty == AIDifficulty.Hard) ? hardSelectedSprite : hardNormalSprite;
            }

            // 2. Character Portrait Lighting (Active = Brilliant, Inactive = Shadow)
            Color activeCharColor = Color.white;
            Color shadowCharColor = new Color(0.70f, 0.75f, 0.85f, 0.55f);

            if (easyCharPortrait != null)
            {
                easyCharPortrait.color = (difficulty == AIDifficulty.Easy) ? activeCharColor : shadowCharColor;
            }
            if (mediumCharPortrait != null)
            {
                mediumCharPortrait.color = (difficulty == AIDifficulty.Medium) ? activeCharColor : shadowCharColor;
            }
            if (hardCharPortrait != null)
            {
                hardCharPortrait.color = (difficulty == AIDifficulty.Hard) ? activeCharColor : shadowCharColor;
            }

            // 3. Toggle Active Badges
            if (easyActiveBadge != null) easyActiveBadge.SetActive(difficulty == AIDifficulty.Easy);
            if (mediumActiveBadge != null) mediumActiveBadge.SetActive(difficulty == AIDifficulty.Medium);
            if (hardActiveBadge != null) hardActiveBadge.SetActive(difficulty == AIDifficulty.Hard);

            // 4. Tactile Card Scales
            float selScale = 1.025f;
            float normScale = 1.0f;
            if (easyCardRect != null) easyCardRect.localScale = (difficulty == AIDifficulty.Easy) ? Vector3.one * selScale : Vector3.one * normScale;
            if (mediumCardRect != null) mediumCardRect.localScale = (difficulty == AIDifficulty.Medium) ? Vector3.one * selScale : Vector3.one * normScale;
            if (hardCardRect != null) hardCardRect.localScale = (difficulty == AIDifficulty.Hard) ? Vector3.one * selScale : Vector3.one * normScale;

            // 5. Update Streamlined Status Banner
            Color accentColor = BetterWithAIState.GetDifficultyColor(difficulty);
            string hexColor = ColorUtility.ToHtmlStringRGB(accentColor);
            string diffName = BetterWithAIState.GetDifficultyName(difficulty);
            string diffTag = BetterWithAIState.GetDifficultyTag(difficulty);

            if (statusTitleText != null)
            {
                statusTitleText.text = $"• CURRENT SELECTION: <color=#{hexColor}>{diffName}</color> ({diffTag}) •";
            }
        }
    }
}
