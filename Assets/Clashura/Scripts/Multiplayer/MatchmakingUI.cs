using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Clashura.Multiplayer
{
    /// <summary>
    /// User Interface controller for the PvP_Matchmaking scene.
    /// Manages two-step matchmaking flow:
    /// 1. Initial screen displays CREATE ROOM vs SEARCH ROOM selection.
    /// 2. Clicking either option opens its dedicated panel with input fields and actions.
    /// </summary>
    public class MatchmakingUI : MonoBehaviour
    {
        [Header("Title & Info")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI roomNameText;

        [Header("Flow Panels")]
        [Tooltip("The initial panel displaying the 2 main choices: CREATE ROOM vs SEARCH ROOM")]
        [SerializeField] private GameObject modeSelectPanel;

        [Tooltip("Panel with input field and button to host a custom room")]
        [SerializeField] private GameObject createRoomPanel;

        [Tooltip("Panel with input field and button to search/join a custom room")]
        [SerializeField] private GameObject searchRoomPanel;

        [Header("Mode Selection Buttons")]
        [SerializeField] private Button chooseCreateButton;
        [SerializeField] private Button chooseSearchButton;

        [Header("Create Room Panel Elements")]
        [SerializeField] private TMP_InputField createRoomInputField;
        [SerializeField] private Button hostRoomActionButton;
        [SerializeField] private Button backFromCreateButton;

        [Header("Search Room Panel Elements")]
        [SerializeField] private TMP_InputField searchRoomInputField;
        [SerializeField] private Button joinRoomActionButton;
        [SerializeField] private Button backFromSearchButton;

        [Header("Common Navigation & Action Buttons")]
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button backToMenuButton;

        [Header("Versus Matchup Screen")]
        [Tooltip("The dramatic Versus Matchup panel that displays both players and selected teams")]
        [SerializeField] private VersusMatchupUI versusMatchupUI;
        [SerializeField] private GameObject matchmakingCard;

        [Header("Status & Indicators")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private Image loadingSpinnerIcon;
        [SerializeField] private float spinnerSpeed = 240f;

        [Header("Scene Navigation")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private bool _isSubscribed = false;

        private void Awake()
        {
            HookButtons();
        }

        private void OnEnable()
        {
            SubscribeManagerEvents();
        }

        private void OnDisable()
        {
            UnsubscribeManagerEvents();
        }

        private void Start()
        {
            SubscribeManagerEvents();
            ShowModeSelection();
        }

        private void SubscribeManagerEvents()
        {
            if (_isSubscribed) return;
            if (MatchmakingManager.Instance != null)
            {
                MatchmakingManager.Instance.OnStateChanged += HandleStateChanged;
                MatchmakingManager.Instance.OnStatusMessageChanged += HandleStatusMessageChanged;
                MatchmakingManager.Instance.OnRoomCreated += HandleRoomCreated;
                _isSubscribed = true;
            }
        }

        private void UnsubscribeManagerEvents()
        {
            if (!_isSubscribed) return;
            if (MatchmakingManager.Instance != null)
            {
                MatchmakingManager.Instance.OnStateChanged -= HandleStateChanged;
                MatchmakingManager.Instance.OnStatusMessageChanged -= HandleStatusMessageChanged;
                MatchmakingManager.Instance.OnRoomCreated -= HandleRoomCreated;
            }
            _isSubscribed = false;
        }

        private void Update()
        {
            // Spin the loading icon when active
            if (loadingIndicator != null && loadingIndicator.activeSelf && loadingSpinnerIcon != null)
            {
                loadingSpinnerIcon.transform.Rotate(0f, 0f, -spinnerSpeed * Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeManagerEvents();
            UnhookButtons();
        }

        private void HookButtons()
        {
            if (chooseCreateButton != null) chooseCreateButton.onClick.AddListener(OnChooseCreateModeClicked);
            if (chooseSearchButton != null) chooseSearchButton.onClick.AddListener(OnChooseSearchModeClicked);

            if (hostRoomActionButton != null) hostRoomActionButton.onClick.AddListener(OnHostRoomClicked);
            if (backFromCreateButton != null) backFromCreateButton.onClick.AddListener(OnBackToModeSelectionClicked);

            if (joinRoomActionButton != null) joinRoomActionButton.onClick.AddListener(OnJoinRoomClicked);
            if (backFromSearchButton != null) backFromSearchButton.onClick.AddListener(OnBackToModeSelectionClicked);

            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
            if (backToMenuButton != null) backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        }

        private void UnhookButtons()
        {
            if (chooseCreateButton != null) chooseCreateButton.onClick.RemoveListener(OnChooseCreateModeClicked);
            if (chooseSearchButton != null) chooseSearchButton.onClick.RemoveListener(OnChooseSearchModeClicked);

            if (hostRoomActionButton != null) hostRoomActionButton.onClick.RemoveListener(OnHostRoomClicked);
            if (backFromCreateButton != null) backFromCreateButton.onClick.RemoveListener(OnBackToModeSelectionClicked);

            if (joinRoomActionButton != null) joinRoomActionButton.onClick.RemoveListener(OnJoinRoomClicked);
            if (backFromSearchButton != null) backFromSearchButton.onClick.RemoveListener(OnBackToModeSelectionClicked);

            if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancelClicked);
            if (backToMenuButton != null) backToMenuButton.onClick.RemoveListener(OnBackToMenuClicked);
        }

        #region Navigation & Panel Switching

        /// <summary>
        /// Displays the initial 2-button choice screen: CREATE ROOM vs SEARCH ROOM.
        /// </summary>
        public void ShowModeSelection()
        {
            if (versusMatchupUI != null) versusMatchupUI.HideMatchup();
            if (matchmakingCard != null) matchmakingCard.SetActive(true);
            if (backToMenuButton != null) backToMenuButton.gameObject.SetActive(true);

            if (modeSelectPanel != null) modeSelectPanel.SetActive(true);
            if (createRoomPanel != null) createRoomPanel.SetActive(false);
            if (searchRoomPanel != null) searchRoomPanel.SetActive(false);

            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
            if (roomNameText != null) roomNameText.gameObject.SetActive(false);
            ShowLoading(false);

            if (titleText != null)
            {
                titleText.gameObject.SetActive(true);
                titleText.text = "PLAYER VS PLAYER";
            }
            if (subtitleText != null)
            {
                subtitleText.gameObject.SetActive(true);
                subtitleText.text = "1V1 ONLINE MATCHMAKING · CHOOSE MODE";
            }
            SetStatus("Choose an option to create or search for a 1v1 battle arena.", new Color(0.85f, 0.90f, 0.98f));
            SetButtonsInteractable(true);
        }

        /// <summary>
        /// Displays the dramatic Versus (VS) screen between both players.
        /// </summary>
        public void ShowVersusMatchup(PlayerMatchProfile p1, PlayerMatchProfile p2, string roomName, Action onComplete)
        {
            if (matchmakingCard != null) matchmakingCard.SetActive(false);
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
            if (backToMenuButton != null) backToMenuButton.gameObject.SetActive(false);
            if (titleText != null) titleText.gameObject.SetActive(false);
            if (subtitleText != null) subtitleText.gameObject.SetActive(false);

            if (versusMatchupUI != null)
            {
                versusMatchupUI.ShowMatchup(p1, p2, roomName, onComplete);
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private void OnChooseCreateModeClicked()
        {
            if (modeSelectPanel != null) modeSelectPanel.SetActive(false);
            if (createRoomPanel != null) createRoomPanel.SetActive(true);
            if (searchRoomPanel != null) searchRoomPanel.SetActive(false);

            if (subtitleText != null) subtitleText.text = "HOST 1V1 ARENA · CREATE ROOM";
            SetStatus("Enter your custom room name and click Host Room.", new Color(1.0f, 0.85f, 0.30f));
            SetButtonsInteractable(true);
        }

        private void OnChooseSearchModeClicked()
        {
            if (modeSelectPanel != null) modeSelectPanel.SetActive(false);
            if (createRoomPanel != null) createRoomPanel.SetActive(false);
            if (searchRoomPanel != null) searchRoomPanel.SetActive(true);

            if (subtitleText != null) subtitleText.text = "SEARCH 1V1 ARENA · JOIN ROOM";
            SetStatus("Enter the target room name and click Join Room.", new Color(0.40f, 0.85f, 1.0f));
            SetButtonsInteractable(true);
        }

        private void OnBackToModeSelectionClicked()
        {
            if (MatchmakingManager.Instance != null && MatchmakingManager.Instance.IsBusy)
            {
                MatchmakingManager.Instance.CancelMatchmaking();
            }
            ShowModeSelection();
        }

        private void OnBackToMenuClicked()
        {
            if (MatchmakingManager.Instance != null && MatchmakingManager.Instance.IsBusy)
            {
                MatchmakingManager.Instance.CancelMatchmaking();
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }

        #endregion

        #region Matchmaking Action Handlers

        private void OnHostRoomClicked()
        {
            string roomName = createRoomInputField != null ? createRoomInputField.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(roomName))
            {
                SetStatus("Please enter a Room Name to host!", new Color(1.0f, 0.40f, 0.40f));
                return;
            }

            if (MatchmakingManager.Instance != null)
            {
                SetButtonsInteractable(false);
                MatchmakingManager.Instance.CreateRoom(roomName);
            }
        }

        private void OnJoinRoomClicked()
        {
            string roomName = searchRoomInputField != null ? searchRoomInputField.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(roomName))
            {
                SetStatus("Please enter a Room Name to search!", new Color(1.0f, 0.40f, 0.40f));
                return;
            }

            if (MatchmakingManager.Instance != null)
            {
                SetButtonsInteractable(false);
                MatchmakingManager.Instance.SearchMatch(roomName);
            }
        }

        private void OnCancelClicked()
        {
            if (MatchmakingManager.Instance != null)
            {
                MatchmakingManager.Instance.CancelMatchmaking();
            }

            SetButtonsInteractable(true);
            ShowLoading(false);
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
            if (roomNameText != null) roomNameText.gameObject.SetActive(false);

            if (createRoomPanel != null && createRoomPanel.activeSelf)
            {
                SetStatus("Room hosting canceled. Ready to host again or return to mode select.");
            }
            else if (searchRoomPanel != null && searchRoomPanel.activeSelf)
            {
                SetStatus("Room search canceled. Ready to search again or return to mode select.");
            }
            else
            {
                ShowModeSelection();
            }
        }

        #endregion

        #region State & Event Handlers

        private void HandleStateChanged(MatchmakingState state, string message)
        {
            switch (state)
            {
                case MatchmakingState.Idle:
                    SetButtonsInteractable(true);
                    ShowLoading(false);
                    if (cancelButton != null) cancelButton.gameObject.SetActive(false);
                    break;

                case MatchmakingState.Connecting:
                case MatchmakingState.CreatingRoom:
                case MatchmakingState.SearchingOpponent:
                case MatchmakingState.JoiningRoom:
                    SetButtonsInteractable(false);
                    ShowLoading(true);
                    SetStatus(message, new Color(1.0f, 0.85f, 0.30f)); // Gold status
                    if (cancelButton != null) cancelButton.gameObject.SetActive(true);
                    break;

                case MatchmakingState.WaitingForOpponent:
                    SetButtonsInteractable(false);
                    ShowLoading(true);
                    SetStatus(message, new Color(0.40f, 0.85f, 1.0f)); // Cyan waiting
                    if (cancelButton != null) cancelButton.gameObject.SetActive(true);
                    break;

                case MatchmakingState.OpponentFound:
                    ShowLoading(false);
                    SetStatus(message, new Color(0.20f, 0.95f, 0.40f)); // Green found
                    break;

                case MatchmakingState.MatchStarting:
                    ShowLoading(false);
                    SetStatus(message, new Color(0.20f, 0.95f, 0.40f)); // Emerald starting
                    if (cancelButton != null) cancelButton.gameObject.SetActive(false);

                    if (versusMatchupUI != null)
                    {
                        var p1 = PhotonFusionManager.Instance != null ? PhotonFusionManager.Instance.GetHostProfile() : PlayerMatchProfile.CreateLocalProfile(true);
                        var p2 = PhotonFusionManager.Instance != null ? PhotonFusionManager.Instance.GetClientProfile() : PlayerMatchProfile.CreateRivalPreviewProfile();
                        string rName = PhotonFusionManager.Instance != null ? PhotonFusionManager.Instance.CurrentSessionName : "Arena";
                        ShowVersusMatchup(p1, p2, rName, null);
                    }
                    break;

                case MatchmakingState.Failed:
                    ShowLoading(false);
                    SetStatus(message, new Color(1.0f, 0.35f, 0.35f)); // Red error
                    SetButtonsInteractable(true);
                    if (cancelButton != null) cancelButton.gameObject.SetActive(false);
                    break;
            }
        }

        private void HandleStatusMessageChanged(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                SetStatus(message);
            }
        }

        private void HandleRoomCreated(string roomName)
        {
            if (roomNameText != null)
            {
                roomNameText.text = $"ROOM NAME: {roomName}";
                roomNameText.gameObject.SetActive(true);
            }
        }

        #endregion

        #region UI Presentation Helpers

        public void SetButtonsInteractable(bool interactable)
        {
            if (chooseCreateButton != null) chooseCreateButton.interactable = interactable;
            if (chooseSearchButton != null) chooseSearchButton.interactable = interactable;

            if (hostRoomActionButton != null) hostRoomActionButton.interactable = interactable;
            if (backFromCreateButton != null) backFromCreateButton.interactable = interactable;

            if (joinRoomActionButton != null) joinRoomActionButton.interactable = interactable;
            if (backFromSearchButton != null) backFromSearchButton.interactable = interactable;

            if (createRoomInputField != null) createRoomInputField.interactable = interactable;
            if (searchRoomInputField != null) searchRoomInputField.interactable = interactable;
        }

        public void SetStatus(string message, Color? color = null)
        {
            if (statusText != null)
            {
                statusText.text = message;
                if (color.HasValue)
                {
                    statusText.color = color.Value;
                }
            }
        }

        public void ShowLoading(bool show)
        {
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(show);
            }
        }

        #endregion
    }
}
