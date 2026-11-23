using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Clashura.UI.Navigation;

namespace Clashura.Multiplayer
{
    /// <summary>
    /// Controller for the Main Menu "Player vs Player" button and the Team Selection Warning Panel.
    /// Intercepts entry to PvP, validates that a 3-hero team is selected, and routes to PvP_Matchmaking.
    /// </summary>
    public class PvPMenuController : MonoBehaviour
    {
        [Header("Main Menu Controls")]
        [Tooltip("The 'Player vs Player' button on the Main Menu.")]
        [SerializeField] private Button pvpButton;

        [Header("Team Selection Warning Panel")]
        [Tooltip("The warning panel displayed when no team is selected.")]
        [SerializeField] private GameObject teamWarningPanel;

        [Tooltip("Warning text component: 'Please Select Your Team First!'")]
        [SerializeField] private TextMeshProUGUI warningMessageText;

        [Tooltip("The single action button to go to Character Selection.")]
        [SerializeField] private Button goToCharacterSelectionButton;

        [Header("Scene Configuration")]
        [SerializeField] private string matchmakingSceneName = "PvP_Matchmaking";
        [SerializeField] private string characterSelectionSceneName = "CharacterSelection";

        private const string RequiredWarningMessage = "Please Select Your Team First!";

        private void Awake()
        {
            if (teamWarningPanel != null)
            {
                teamWarningPanel.SetActive(false);
            }

            if (pvpButton != null)
            {
                pvpButton.onClick.AddListener(OnPlayerVsPlayerClicked);
            }

            if (goToCharacterSelectionButton != null)
            {
                goToCharacterSelectionButton.onClick.AddListener(OnGoToCharacterSelectionClicked);
            }
        }

        private void OnDestroy()
        {
            if (pvpButton != null)
            {
                pvpButton.onClick.RemoveListener(OnPlayerVsPlayerClicked);
            }

            if (goToCharacterSelectionButton != null)
            {
                goToCharacterSelectionButton.onClick.RemoveListener(OnGoToCharacterSelectionClicked);
            }
        }

        /// <summary>
        /// Triggered when the player clicks "Player vs Player".
        /// </summary>
        public void OnPlayerVsPlayerClicked()
        {
            bool hasTeam = TeamSelectionManager.HasSelectedTeam();

            if (!hasTeam)
            {
                // Show Warning Panel immediately
                ShowWarningPanel();
            }
            else
            {
                // Team is selected -> load PvP_Matchmaking scene
                LoadMatchmakingScene();
            }
        }

        private void ShowWarningPanel()
        {
            if (teamWarningPanel != null)
            {
                teamWarningPanel.SetActive(true);
            }

            if (warningMessageText != null)
            {
                warningMessageText.text = RequiredWarningMessage;
            }

            Debug.Log("[PvPMenuController] Team check failed: Displaying 'Please Select Your Team First!' panel.");
        }

        /// <summary>
        /// Triggered when the player clicks "Go to Character Selection" on the warning panel.
        /// </summary>
        public void OnGoToCharacterSelectionClicked()
        {
            if (teamWarningPanel != null)
            {
                teamWarningPanel.SetActive(false);
            }

            // If UINavigationController is active in the scene, switch to Character Selection screen
            if (UINavigationController.Instance != null)
            {
                UINavigationController.Instance.OpenCharacterSelection();
            }
            else
            {
                // Otherwise load the dedicated CharacterSelection scene
                SceneManager.LoadScene(characterSelectionSceneName);
            }

            Debug.Log("[PvPMenuController] Navigating player to Character Selection.");
        }

        private void LoadMatchmakingScene()
        {
            if (teamWarningPanel != null)
            {
                teamWarningPanel.SetActive(false);
            }

            Debug.Log($"[PvPMenuController] Team check passed. Loading scene: '{matchmakingSceneName}'.");
            SceneManager.LoadScene(matchmakingSceneName);
        }
    }
}
