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
        [SerializeField] private TextMeshProUGUI playerRatingBadgeText;

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
        private float _searchDurationRemaining = 120f;
        private bool _isCountdownActive = false;

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

            if (MatchmakingManager.EntryMode == MatchmakingEntryMode.RandomMatch)
            {
                StartRandomEloMatchmaking();
            }
            else
            {
                ShowModeSelection();
            }
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

            if (PlayFab.PlayFabAuthService.Instance != null)
            {
                PlayFab.PlayFabAuthService.Instance.OnProfileLoaded += HandlePlayFabProfileLoaded;
                PlayFab.PlayFabAuthService.Instance.OnRatingUpdated += HandlePlayFabRatingUpdated;
                if (PlayFab.PlayFabAuthService.Instance.CurrentProfile != null)
                {
                    UpdatePlayFabRatingDisplay(PlayFab.PlayFabAuthService.Instance.CurrentProfile);
                }
            }

            if (PlayFab.PlayFabMatchmakingClient.Instance != null)
            {
                PlayFab.PlayFabMatchmakingClient.Instance.OnTicketUpdated += HandleTicketUpdated;
            }

            if (Elo.EloManager.Instance != null)
            {
                Elo.EloManager.Instance.OnProfileLoaded += HandleProfileLoaded;
                Elo.EloManager.Instance.OnRatingUpdated += HandleRatingUpdated;
                if (PlayFab.PlayFabAuthService.Instance == null || PlayFab.PlayFabAuthService.Instance.CurrentProfile == null)
                {
                    UpdateRatingDisplay(Elo.EloManager.Instance.CurrentProfile);
                }
            }
        }

        private void UnsubscribeManagerEvents()
        {
            if (!_isSubscribed) return;
            if (MatchmakingManager.HasInstance)
            {
                MatchmakingManager.Instance.OnStateChanged -= HandleStateChanged;
                MatchmakingManager.Instance.OnStatusMessageChanged -= HandleStatusMessageChanged;
                MatchmakingManager.Instance.OnRoomCreated -= HandleRoomCreated;
            }

            if (PlayFab.PlayFabAuthService.HasInstance)
            {
                PlayFab.PlayFabAuthService.Instance.OnProfileLoaded -= HandlePlayFabProfileLoaded;
                PlayFab.PlayFabAuthService.Instance.OnRatingUpdated -= HandlePlayFabRatingUpdated;
            }

            if (PlayFab.PlayFabMatchmakingClient.HasInstance)
            {
                PlayFab.PlayFabMatchmakingClient.Instance.OnTicketUpdated -= HandleTicketUpdated;
            }

            if (Elo.EloManager.Instance != null)
            {
                Elo.EloManager.Instance.OnProfileLoaded -= HandleProfileLoaded;
                Elo.EloManager.Instance.OnRatingUpdated -= HandleRatingUpdated;
            }
            _isSubscribed = false;
        }

        private void HandlePlayFabProfileLoaded(PlayFab.PlayFabPlayerProfile profile)
        {
            UpdatePlayFabRatingDisplay(profile);
        }

        private void HandlePlayFabRatingUpdated(int oldRating, int newRating, int delta)
        {
            if (PlayFab.PlayFabAuthService.HasInstance)
            {
                UpdatePlayFabRatingDisplay(PlayFab.PlayFabAuthService.Instance.CurrentProfile);
            }
        }

        private void HandleTicketUpdated(PlayFab.MatchmakingTicketDto ticket)
        {
            if (ticket != null && playerRatingBadgeText != null)
            {
                playerRatingBadgeText.text = $"YOUR NAME: {ticket.displayName} · YOUR RATING: {ticket.rating} ELO · PLAYERS IN QUEUE: {ticket.playersInQueue}";
            }
        }

        public void UpdatePlayFabRatingDisplay(PlayFab.PlayFabPlayerProfile profile, int queueCount = 1)
        {
            if (playerRatingBadgeText != null && profile != null)
            {
                playerRatingBadgeText.text = $"YOUR NAME: {profile.displayName} · YOUR RATING: {profile.rating} ELO · PLAYERS IN QUEUE: {queueCount}";
                playerRatingBadgeText.gameObject.SetActive(true);
            }
        }

        private void HandleProfileLoaded(Elo.PlayerEloProfile profile)
        {
            UpdateRatingDisplay(profile);
        }

        private void HandleRatingUpdated(int oldRating, int newRating, int delta)
        {
            if (Elo.EloManager.Instance != null)
            {
                UpdateRatingDisplay(Elo.EloManager.Instance.CurrentProfile);
            }
        }

        public void UpdateRatingDisplay(Elo.PlayerEloProfile profile)
        {
            if (playerRatingBadgeText != null && profile != null)
            {
                playerRatingBadgeText.text = $"YOUR RATING: {profile.rating} ELO · {profile.wins}W - {profile.losses}L";
                playerRatingBadgeText.gameObject.SetActive(true);
            }
        }

        private void Update()
        {
            // Spin the loading icon when active
            if (loadingIndicator != null && loadingIndicator.activeSelf && loadingSpinnerIcon != null)
            {
                loadingSpinnerIcon.transform.Rotate(0f, 0f, -spinnerSpeed * Time.deltaTime);
            }

            if (_isCountdownActive)
            {
                _searchDurationRemaining -= Time.deltaTime;
                if (_searchDurationRemaining <= 0f)
                {
                    _searchDurationRemaining = 0f;
                    _isCountdownActive = false;
                }

                int minutes = (int)(_searchDurationRemaining / 60f);
                int seconds = (int)(_searchDurationRemaining % 60f);
                string timeStr = $"{minutes:D2}:{seconds:D2}";

                if (subtitleText != null && MatchmakingManager.EntryMode == MatchmakingEntryMode.RandomMatch)
                {
                    subtitleText.text = $"SEARCHING FOR OPPONENT · TIME: {timeStr}";
                }
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

            if (PlayFab.PlayFabAuthService.HasInstance && PlayFab.PlayFabAuthService.Instance.CurrentProfile != null)
            {
                UpdatePlayFabRatingDisplay(PlayFab.PlayFabAuthService.Instance.CurrentProfile);
            }
            else if (Elo.EloManager.Instance != null && Elo.EloManager.Instance.CurrentProfile != null)
            {
                UpdateRatingDisplay(Elo.EloManager.Instance.CurrentProfile);
            }
        }

        /// <summary>
        /// Launches direct Skill-Based ELO Matchmaking for the Main Menu "Random Match" button.
        /// Bypasses the room creation selection and searches dynamically (±50 -> ±100 -> ±150 ELO).
        /// </summary>
        public void StartRandomEloMatchmaking()
        {
            if (versusMatchupUI != null) versusMatchupUI.HideMatchup();
            if (matchmakingCard != null) matchmakingCard.SetActive(true);
            if (backToMenuButton != null) backToMenuButton.gameObject.SetActive(true);

            if (modeSelectPanel != null) modeSelectPanel.SetActive(false);
            if (createRoomPanel != null) createRoomPanel.SetActive(false);
            if (searchRoomPanel != null) searchRoomPanel.SetActive(false);

            if (cancelButton != null) cancelButton.gameObject.SetActive(true);
            if (roomNameText != null) roomNameText.gameObject.SetActive(false);

            if (titleText != null)
            {
                titleText.gameObject.SetActive(true);
                titleText.text = "RANDOM MATCH";
            }
            if (subtitleText != null)
            {
                subtitleText.gameObject.SetActive(true);
                subtitleText.text = "SKILL-BASED ELO MATCHMAKING";
            }

            if (PlayFab.PlayFabAuthService.HasInstance && PlayFab.PlayFabAuthService.Instance.CurrentProfile != null)
            {
                UpdatePlayFabRatingDisplay(PlayFab.PlayFabAuthService.Instance.CurrentProfile);
            }
            else if (Elo.EloManager.Instance != null && Elo.EloManager.Instance.CurrentProfile != null)
            {
                UpdateRatingDisplay(Elo.EloManager.Instance.CurrentProfile);
            }

            SetStatus("Connecting to matchmaking queue...", new Color(1.0f, 0.85f, 0.30f));
            ShowLoading(true);

            if (MatchmakingManager.Instance != null)
            {
                MatchmakingManager.Instance.StartEloSkillMatchmaking();
            }
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

            if (subtitleText != null) subtitleText.text = "SEARCH 1V1 ARENA · SKILL-BASED ELO OR ROOM NAME";
            SetStatus("Click Join to find an opponent by ELO rating (±50 ELO), or enter a custom room name.", new Color(0.40f, 0.85f, 1.0f));
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
            else if (MatchmakingManager.EntryMode == MatchmakingEntryMode.RandomMatch)
            {
                SetStatus("Matchmaking canceled. Choose an option or return to Main Menu.", new Color(1.0f, 0.85f, 0.30f));
                ShowModeSelection();
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
                    _isCountdownActive = false;
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
                    if (cancelButton != null)
                    {
                        cancelButton.gameObject.SetActive(true);
                        var btnTxt = cancelButton.GetComponentInChildren<TextMeshProUGUI>();
                        if (btnTxt != null)
                        {
                            btnTxt.text = MatchmakingManager.EntryMode == MatchmakingEntryMode.RandomMatch 
                                ? "CANCEL SEARCH" 
                                : "CANCEL MATCHMAKING";
                        }
                    }
                    if (state == MatchmakingState.SearchingOpponent && !_isCountdownActive)
                    {
                        _searchDurationRemaining = PlayFab.PlayFabConfig.MaxSearchDurationSeconds;
                        _isCountdownActive = true;
                    }
                    break;

                case MatchmakingState.WaitingForOpponent:
                    SetButtonsInteractable(false);
                    ShowLoading(true);
                    SetStatus(message, new Color(0.40f, 0.85f, 1.0f)); // Cyan waiting
                    if (cancelButton != null) cancelButton.gameObject.SetActive(true);
                    break;

                case MatchmakingState.OpponentFound:
                    _isCountdownActive = false;
                    ShowLoading(false);
                    if (subtitleText != null && MatchmakingManager.EntryMode == MatchmakingEntryMode.RandomMatch)
                    {
                        subtitleText.text = "OPPONENT FOUND!";
                    }
                    SetStatus(message, new Color(0.20f, 0.95f, 0.40f)); // Green found
                    break;

                case MatchmakingState.MatchStarting:
                    _isCountdownActive = false;
                    ShowLoading(false);
                    SetStatus(message, new Color(0.20f, 0.95f, 0.40f)); // Emerald starting
                    if (cancelButton != null) cancelButton.gameObject.SetActive(false);

                    if (versusMatchupUI != null)
                    {
                        var p1 = PhotonFusionManager.Instance != null ? PhotonFusionManager.Instance.GetHostProfile() : PlayerMatchProfile.CreateLocalProfile(true);
                        var p2 = PhotonFusionManager.Instance != null ? PhotonFusionManager.Instance.GetClientProfile() : PlayerMatchProfile.CreateRivalPreviewProfile();

                        var pfTicket = PlayFab.PlayFabMatchmakingClient.HasInstance ? PlayFab.PlayFabMatchmakingClient.Instance.CurrentTicket : null;
                        if (pfTicket != null && pfTicket.status == "Matched")
                        {
                            if (p1 != null)
                            {
                                p1.playerName = pfTicket.displayName;
                                p1.playerPoints = pfTicket.rating;
                            }
                            if (p2 != null)
                            {
                                p2.playerName = pfTicket.matchedOpponentName;
                                p2.playerPoints = pfTicket.matchedOpponentRating;
                            }
                        }

                        string rName = PhotonFusionManager.Instance != null ? PhotonFusionManager.Instance.CurrentSessionName : "Arena";
                        ShowVersusMatchup(p1, p2, rName, null);
                    }
                    break;

                case MatchmakingState.Failed:
                    _isCountdownActive = false;
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
