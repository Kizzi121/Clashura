using UnityEngine;
using Clashura.CharacterSelection.Managers;
using Clashura.CharacterSelection.UI;

namespace Clashura.UI.Navigation
{
    /// <summary>
    /// Master Screen Navigation Controller for CLASHURA.
    /// Orchestrates seamless panel switching between Main Menu and Character Selection,
    /// preserving selection states, handling the Back button, and managing application exit.
    /// </summary>
    public class UINavigationController : MonoBehaviour
    {
        private static UINavigationController _instance;
        public static UINavigationController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<UINavigationController>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public enum ScreenState
        {
            MainMenu,
            CharacterSelection
        }

        [Header("UI Panels")]
        [Tooltip("The root GameObject containing the Main Menu UI.")]
        [SerializeField] private GameObject mainMenuPanel;

        [Tooltip("The root GameObject containing the Character Selection UI.")]
        [SerializeField] private GameObject characterSelectionPanel;

        [Header("System References")]
        [SerializeField] private CharacterSelectionManager selectionManager;
        [SerializeField] private CharacterSelectionUI selectionUI;

        [Header("Initial State")]
        [SerializeField] private ScreenState startingScreen = ScreenState.MainMenu;

        public ScreenState CurrentScreen { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (selectionUI != null)
            {
                selectionUI.OnBackRequested += OpenMainMenu;
            }
        }

        private void Start()
        {
            if (startingScreen == ScreenState.MainMenu)
            {
                OpenMainMenu();
            }
            else
            {
                OpenCharacterSelection();
            }
        }

        private void OnDestroy()
        {
            if (selectionUI != null)
            {
                selectionUI.OnBackRequested -= OpenMainMenu;
            }
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Shows the Main Menu panel and hides Character Selection.
        /// Preserves all previously selected characters.
        /// </summary>
        public void OpenMainMenu()
        {
            CurrentScreen = ScreenState.MainMenu;

            if (characterSelectionPanel != null)
            {
                characterSelectionPanel.SetActive(false);
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the Main Menu panel and activates the Character Selection panel.
        /// Refreshes the Character Selection UI view while retaining existing selections.
        /// </summary>
        public void OpenCharacterSelection()
        {
            CurrentScreen = ScreenState.CharacterSelection;

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            if (characterSelectionPanel != null)
            {
                characterSelectionPanel.SetActive(true);
            }

            if (selectionUI != null)
            {
                selectionUI.RefreshView();
            }
        }

        /// <summary>
        /// Handles application quit across desktop and mobile platforms.
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[UINavigationController] Exiting CLASHURA application...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
