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
        [Tooltip("The 'Player vs Player' button on the Main Menu (Custom Rooms).")]
        [SerializeField] private Button pvpButton;

        [Tooltip("The 'Random Match' button on the Main Menu (Skill-Based ELO Matchmaking).")]
        [SerializeField] private Button randomMatchButton;

        public Button PvpButton
        {
            get => pvpButton;
            set => pvpButton = value;
        }

        public Button RandomMatchButton
        {
            get => randomMatchButton;
            set => randomMatchButton = value;
        }

        [Header("Team Selection Warning Panel")]
        [Tooltip("The warning panel displayed when no team is selected.")]
        [SerializeField] private GameObject teamWarningPanel;

        [Tooltip("Warning text component: 'Please Select Your Team First!'")]
        [SerializeField] private TextMeshProUGUI warningMessageText;

        [Tooltip("The single action button to go to Character Selection.")]
        [SerializeField] private Button goToCharacterSelectionButton;

        public GameObject WarningPanel => teamWarningPanel;
        public TextMeshProUGUI WarningText => warningMessageText;
        public Button GoToCharacterSelectionButton => goToCharacterSelectionButton;

        [Header("Scene Configuration")]
        [SerializeField] private string matchmakingSceneName = "PvP_Matchmaking";
        [SerializeField] private string characterSelectionSceneName = "CharacterSelection";

        private const string RequiredWarningMessage = "Please Select Your Team First!";

        private void Awake()
        {
            if (pvpButton == null)
            {
                var pvpGo = GameObject.Find("Btn_PvP");
                if (pvpGo != null) pvpButton = pvpGo.GetComponent<Button>();
            }

            if (randomMatchButton == null)
            {
                var randomGo = GameObject.Find("Btn_RandomMatch");
                if (randomGo != null) randomMatchButton = randomGo.GetComponent<Button>();
            }

            if (teamWarningPanel != null)
            {
                teamWarningPanel.SetActive(false);
            }

            if (pvpButton != null)
            {
                pvpButton.onClick.AddListener(OnPlayerVsPlayerClicked);
            }

            if (randomMatchButton != null)
            {
                randomMatchButton.onClick.AddListener(OnRandomMatchClicked);
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

            if (randomMatchButton != null)
            {
                randomMatchButton.onClick.RemoveListener(OnRandomMatchClicked);
            }

            if (goToCharacterSelectionButton != null)
            {
                goToCharacterSelectionButton.onClick.RemoveListener(OnGoToCharacterSelectionClicked);
            }
        }

        /// <summary>
        /// Triggered when the player clicks "Player vs Player".
        /// Opens Custom 1v1 Rooms mode (Host / Search by custom room code).
        /// </summary>
        public void OnPlayerVsPlayerClicked()
        {
            bool hasTeam = TeamSelectionManager.HasSelectedTeam();

            if (!hasTeam)
            {
                ShowWarningPanel();
            }
            else
            {
                MatchmakingManager.EntryMode = MatchmakingEntryMode.CustomRooms;
                LoadMatchmakingScene();
            }
        }

        /// <summary>
        /// Triggered when the player clicks "Random Match".
        /// Opens Skill-Based ELO Matchmaking mode (±50 -> ±100 -> ±150).
        /// </summary>
        public void OnRandomMatchClicked()
        {
            bool hasTeam = TeamSelectionManager.HasSelectedTeam();

            if (!hasTeam)
            {
                ShowWarningPanel();
            }
            else
            {
                MatchmakingManager.EntryMode = MatchmakingEntryMode.RandomMatch;
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
            else if (Application.isPlaying)
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
            if (Application.isPlaying)
            {
                SceneManager.LoadScene(matchmakingSceneName);
            }
        }
    }
}
