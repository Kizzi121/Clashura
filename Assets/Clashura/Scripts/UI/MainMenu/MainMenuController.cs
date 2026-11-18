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

        [Tooltip("Button 2: Future worldwide random matchmaking (Visual only for now).")]
        [SerializeField] private Button randomMatchButton;

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
            // Button 1: PLAYER VS PLAYER (Visual only for now)
            if (pvpButton != null)
            {
                pvpButton.onClick.AddListener(() =>
                {
                    ShowFeedback("Player vs Player: Coming soon in multiplayer update!");
                });
            }

            // Button 2: RANDOM MATCH (Visual only for now)
            if (randomMatchButton != null)
            {
                randomMatchButton.onClick.AddListener(() =>
                {
                    ShowFeedback("Random Match: Matchmaking coming soon!");
                });
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

            // Button 4: SETTINGS (Visual only for now)
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(() =>
                {
                    ShowFeedback("Settings: Coming soon!");
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
