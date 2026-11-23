using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Clashura.UI.Navigation;

namespace Clashura.UI.MainMenu
{
    /// <summary>
    /// Controller for the CLASHURA Main Menu screen.
    /// Manages the five fighting-game menu buttons, connects to the Character Selection panel,
    /// and handles visual-only placeholder actions.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Menu Buttons")]
        [Tooltip("Button 1: Future private multiplayer lobby / room codes (Visual only for now).")]
        [SerializeField] private Button pvpButton;

        [Tooltip("Button: Opens Better with AI game mode.")]
        [SerializeField] private Button playWithAIButton;

        [Tooltip("Button 2: Future worldwide random matchmaking (Visual only for now).")]
        [SerializeField] private Button randomMatchButton;

        public Button PlayWithAIButton
        {
            get => playWithAIButton;
            set => playWithAIButton = value;
        }

        [Tooltip("Button 3: Opens the Character Selection panel.")]
        [SerializeField] private Button characterSelectionButton;

        [Tooltip("Button 4: Future game settings (Visual only for now).")]
        [SerializeField] private Button settingsButton;

        [Tooltip("Button 5: Exits the application.")]
        [SerializeField] private Button exitButton;

        [Header("Feedback Toast (Optional)")]
        [SerializeField] private GameObject feedbackBanner;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private float feedbackDuration = 2.5f;
        private float _feedbackTimer = 0f;

        private void Awake()
        {
            HookButtons();
        }

        private void Start()
        {
            UpdatePlayerBadgeDisplay();
            if (Clashura.PlayFab.PlayFabAuthService.HasInstance)
            {
                Clashura.PlayFab.PlayFabAuthService.Instance.OnProfileLoaded += HandleProfileLoaded;
            }
            else if (Application.isPlaying)
            {
                var auth = Clashura.PlayFab.PlayFabAuthService.Instance;
                if (auth != null)
                {
                    auth.OnProfileLoaded += HandleProfileLoaded;
                }
            }
        }

        private void OnDestroy()
        {
            if (Clashura.PlayFab.PlayFabAuthService.HasInstance)
            {
                Clashura.PlayFab.PlayFabAuthService.Instance.OnProfileLoaded -= HandleProfileLoaded;
            }

            if (playWithAIButton != null)
            {
                playWithAIButton.onClick.RemoveListener(OnPlayWithAIClicked);
            }
        }

        private void HandleProfileLoaded(Clashura.PlayFab.PlayFabPlayerProfile profile)
        {
            UpdatePlayerBadgeDisplay();
        }

        public void UpdatePlayerBadgeDisplay()
        {
            var badgeTitle = GameObject.Find("MasterCanvas/MainMenuPanel/SafeArea/PlayerProfileBadge/Title")?.GetComponent<TextMeshProUGUI>();
            var badgeValue = GameObject.Find("MasterCanvas/MainMenuPanel/SafeArea/PlayerProfileBadge/Value")?.GetComponent<TextMeshProUGUI>();

            string name = Clashura.PlayFab.PlayFabConfig.DefaultDisplayName;
            int rating = Clashura.PlayFab.PlayFabConfig.DefaultRating;

            if (Clashura.PlayFab.PlayFabAuthService.HasInstance && Clashura.PlayFab.PlayFabAuthService.Instance.CurrentProfile != null)
            {
                name = Clashura.PlayFab.PlayFabAuthService.Instance.CurrentProfile.displayName;
                rating = Clashura.PlayFab.PlayFabAuthService.Instance.CurrentProfile.rating;
            }
            else
            {
                name = PlayerPrefs.GetString("CLASHURA_PLAYER_NAME", name);
                rating = PlayerPrefs.GetInt("CLASHURA_PLAYER_POINTS", rating);
            }

            if (badgeTitle != null) badgeTitle.text = name.ToUpper();
            if (badgeValue != null) badgeValue.text = $"Lv. 12  ·  {rating:N0}";
        }

        private void Update()
        {
            if (_feedbackTimer > 0f)
            {
                _feedbackTimer -= Time.deltaTime;
                if (_feedbackTimer <= 0f && feedbackBanner != null)
                {
                    feedbackBanner.SetActive(false);
                }
            }
        }

        private void HookButtons()
        {
            // Button 1: PLAYER VS PLAYER is handled by PvPMenuController for 1v1 custom rooms
            // Button 2: RANDOM MATCH is handled by PvPMenuController for Skill-Based ELO Matchmaking
            // Both buttons perform team selection validation and route to their respective matchmaking flow.

            // New Button: PLAY WITH AI (Opens the BetterWithAI scene)
            if (playWithAIButton != null)
            {
                playWithAIButton.onClick.AddListener(OnPlayWithAIClicked);
            }

            // Button 3: CHARACTER SELECTION (Opens Character Selection panel)
            if (characterSelectionButton != null)
            {
                characterSelectionButton.onClick.AddListener(() =>
                {
                    if (UINavigationController.Instance != null)
                    {
                        UINavigationController.Instance.OpenCharacterSelection();
                    }
                    else
                    {
                        Debug.LogWarning("[MainMenuController] UINavigationController.Instance is null!");
                    }
                });
            }

            // Button 4: SETTINGS (Opens the Settings panel)
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(() =>
                {
                    if (UINavigationController.Instance != null)
                    {
                        UINavigationController.Instance.OpenSettings();
                    }
                    else
                    {
                        Debug.LogWarning("[MainMenuController] UINavigationController.Instance is null!");
                    }
                });
            }

            // Button 5: EXIT GAME (Quit application)
            if (exitButton != null)
            {
                exitButton.onClick.AddListener(() =>
                {
                    if (UINavigationController.Instance != null)
                    {
                        UINavigationController.Instance.QuitGame();
                    }
                    else
                    {
#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
                    }
                });
            }
        }

        private void OnPlayWithAIClicked()
        {
            Debug.Log("[MainMenuController] 'Play with AI' clicked. Opening AICareerMenu scene...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("AICareerMenu");
        }

        private void ShowFeedback(string message)
        {
            if (feedbackBanner != null && feedbackText != null)
            {
                feedbackText.text = message;
                feedbackBanner.SetActive(true);
                _feedbackTimer = feedbackDuration;
            }
            else
            {
                Debug.Log($"[MainMenu] {message}");
            }
        }
    }
}
