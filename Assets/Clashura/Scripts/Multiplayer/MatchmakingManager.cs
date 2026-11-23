using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Clashura.Multiplayer
{
    public enum MatchmakingState
    {
        Idle,
        Connecting,
        CreatingRoom,
        WaitingForOpponent,
        SearchingOpponent,
        OpponentFound,
        JoiningRoom,
        MatchStarting,
        Failed
    }

    /// <summary>
    /// Entry mode determined by Main Menu button selection:
    /// - CustomRooms: Player vs Player button -> 1v1 Custom Room lobby (Create Room / Search Room by custom code).
    /// - RandomMatch: Random Match button -> Skill-Based ELO Matchmaking queue.
    /// </summary>
    public enum MatchmakingEntryMode
    {
        CustomRooms,
        RandomMatch
    }

    /// <summary>
    /// Master coordinator for 1v1 Player vs Player Matchmaking.
    /// Drives room creation, session searching, opponent discovery, and gameplay scene transitions.
    /// </summary>
    public class MatchmakingManager : MonoBehaviour
    {
        private static MatchmakingManager _instance;
        private static bool _isShuttingDown = false;

        /// <summary>
        /// Global entry mode set by Main Menu (CustomRooms vs RandomMatch).
        /// </summary>
        public static MatchmakingEntryMode EntryMode { get; set; } = MatchmakingEntryMode.CustomRooms;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _isShuttingDown = false;
            _instance = null;
            EntryMode = MatchmakingEntryMode.CustomRooms;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterEditorLifecycle()
        {
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode ||
                    state == UnityEditor.PlayModeStateChange.ExitingEditMode)
                {
                    _isShuttingDown = true;
                }
                else if (state == UnityEditor.PlayModeStateChange.EnteredPlayMode ||
                         state == UnityEditor.PlayModeStateChange.EnteredEditMode)
                {
                    _isShuttingDown = false;
                }
            };
        }
#endif

        public static bool HasInstance => _instance != null && !_isShuttingDown;

        public static MatchmakingManager Instance
        {
            get
            {
                if (_isShuttingDown) return null;
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<MatchmakingManager>();
                    if (_instance == null && Application.isPlaying && !_isShuttingDown)
                    {
                        GameObject go = new GameObject("[MatchmakingManager]");
                        _instance = go.AddComponent<MatchmakingManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Scene Configuration")]
        [Tooltip("Name of the battle arena gameplay scene to load when both players are connected.")]
        [SerializeField] private string gameplaySceneName = "SampleScene";

        [Header("Search Settings")]
        [Tooltip("Interval in seconds between session search retries when no room is found.")]
        [SerializeField] private float searchRetryInterval = 1.0f;
        [SerializeField] private float maxSearchTimeout = 30.0f;

        // Current state
        [SerializeField] private MatchmakingState currentState = MatchmakingState.Idle;
        private Coroutine _searchCoroutine;
        private bool _isTransitioning = false;

        public MatchmakingState CurrentState => currentState;
        public bool IsBusy => currentState != MatchmakingState.Idle && currentState != MatchmakingState.Failed;

        // Status messages required by specification
        public const string MsgConnecting = "Connecting to Photon...";
        public const string MsgCreatingRoom = "Creating Room...";
        public const string MsgRoomCreated = "Room Created! Waiting for Opponent...";
        public const string MsgSearchingOpponent = "Searching for Opponent...";
        public const string MsgOpponentFound = "Opponent Found!";
        public const string MsgJoiningRoom = "Joining Room...";
        public const string MsgMatchStarting = "Match Starting...";
        public const string MsgConnectionFailed = "Connection Failed. Please Try Again.";

        // Events
        public event Action<MatchmakingState, string> OnStateChanged;
        public event Action<string> OnStatusMessageChanged;
        public event Action<string> OnRoomCreated;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            // Subscribe to PhotonFusionManager events
            if (PhotonFusionManager.Instance != null)
            {
                PhotonFusionManager.Instance.OnMatchReady += HandleMatchReady;
                PhotonFusionManager.Instance.OnConnectionFailed += HandleConnectionFailed;
            }

            SetState(MatchmakingState.Idle, string.Empty);
        }

        private void OnApplicationQuit()
        {
            _isShuttingDown = true;
        }

        private void OnDestroy()
        {
            if (!_isShuttingDown && PhotonFusionManager.Instance != null)
            {
                PhotonFusionManager.Instance.OnMatchReady -= HandleMatchReady;
                PhotonFusionManager.Instance.OnConnectionFailed -= HandleConnectionFailed;
            }

            StopAllCoroutines();
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #region Create Room Flow

        /// <summary>
        /// Initiates room creation with custom room name chosen by the host:
        /// Connects to Photon, creates session with host-specified name, and waits for opponent.
        /// </summary>
        public void CreateRoom(string customRoomName = null)
        {
            if (IsBusy)
            {
                Debug.LogWarning("[MatchmakingManager] Matchmaking already in progress. Ignoring duplicate click.");
                return;
            }

            StartCoroutine(CreateRoomRoutine(customRoomName));
        }

        private IEnumerator CreateRoomRoutine(string customRoomName)
        {
            SetState(MatchmakingState.Connecting, MsgConnecting);

            bool connectSuccess = false;
            string connectErr = string.Empty;

            PhotonFusionManager.Instance.ConnectToPhoton((success, error) =>
            {
                connectSuccess = success;
                connectErr = error;
            });

            yield return new WaitUntil(() => PhotonFusionManager.Instance.IsConnected || !string.IsNullOrEmpty(connectErr));

            if (!connectSuccess && !PhotonFusionManager.Instance.IsConnected)
            {
                SetState(MatchmakingState.Failed, MsgConnectionFailed);
                yield break;
            }

            // Connecting done -> Creating room
            SetState(MatchmakingState.CreatingRoom, MsgCreatingRoom);

            // Use host-specified room name or sensible fallback
            string roomName = string.IsNullOrWhiteSpace(customRoomName)
                ? $"CLASH_{UnityEngine.Random.Range(1000, 9999)}"
                : customRoomName.Trim();

            bool createSuccess = false;
            string createErr = string.Empty;

            PhotonFusionManager.Instance.CreateSession(roomName, (success, error) =>
            {
                createSuccess = success;
                createErr = error;
            });

            yield return new WaitUntil(() => createSuccess || !string.IsNullOrEmpty(createErr));

            if (!createSuccess)
            {
                if (createErr == "ROOM_NAME_TAKEN")
                {
                    SetState(MatchmakingState.Failed, $"Room '{roomName}' already exists! Please choose another name.");
                }
                else
                {
                    SetState(MatchmakingState.Failed, MsgConnectionFailed);
                }
                yield break;
            }

            // Room created successfully -> Waiting for opponent
            SetState(MatchmakingState.WaitingForOpponent, MsgRoomCreated);
            OnRoomCreated?.Invoke(roomName);
        }

        #endregion

        #region Search Match Flow

        /// <summary>
        /// Initiates search for room by specific name chosen by client, or runs ELO Skill-Based Matchmaking if empty.
        /// </summary>
        public void SearchMatch(string targetRoomName = null)
        {
            if (string.IsNullOrWhiteSpace(targetRoomName))
            {
                StartEloSkillMatchmaking();
                return;
            }

            if (IsBusy)
            {
                Debug.LogWarning("[MatchmakingManager] Matchmaking already in progress. Ignoring duplicate click.");
                return;
            }

            if (_searchCoroutine != null)
            {
                StopCoroutine(_searchCoroutine);
            }
            _searchCoroutine = StartCoroutine(SearchMatchRoutine(targetRoomName));
        }

        /// <summary>
        /// Initiates Skill-Based Matchmaking using the standard ELO rating algorithm.
        /// Evaluates database ratings, applies dynamic range expansion (±50 -> ±100 -> ±150),
        /// pairs players authoritatively, and binds to Photon Fusion 2 session.
        /// </summary>
        public void StartEloSkillMatchmaking()
        {
            if (IsBusy)
            {
                Debug.LogWarning("[MatchmakingManager] Matchmaking already in progress. Ignoring duplicate click.");
                return;
            }

            if (_searchCoroutine != null)
            {
                StopCoroutine(_searchCoroutine);
            }
            _searchCoroutine = StartCoroutine(EloSkillMatchmakingRoutine());
        }

        private IEnumerator EloSkillMatchmakingRoutine()
        {
            SetState(MatchmakingState.Connecting, MsgConnecting);

            // 1. Connect to Photon Fusion 2 Cloud
            bool connectSuccess = false;
            string connectErr = string.Empty;

            PhotonFusionManager.Instance.ConnectToPhoton((success, error) =>
            {
                connectSuccess = success;
                connectErr = error;
            });

            yield return new WaitUntil(() => PhotonFusionManager.Instance.IsConnected || !string.IsNullOrEmpty(connectErr));

            if (!connectSuccess && !PhotonFusionManager.Instance.IsConnected)
            {
                SetState(MatchmakingState.Failed, MsgConnectionFailed);
                yield break;
            }

            // 2. Authenticate through PlayFab
            var auth = PlayFab.PlayFabAuthService.Instance;
            if (!auth.IsAuthenticated || auth.CurrentProfile == null)
            {
                bool authComplete = false;
                auth.Authenticate((success, profile) =>
                {
                    authComplete = true;
                });

                yield return new WaitUntil(() => authComplete);
            }

            var profile = auth.CurrentProfile;
            int myRating = profile != null ? profile.rating : 1000;
            string myName = profile != null ? profile.displayName : "Player";

            string initialStatus = $"Searching for Opponent...\nYour Name: {myName} · Rating: {myRating} ELO (±50)\nPlayers in Queue: 1";
            SetState(MatchmakingState.SearchingOpponent, initialStatus);

            // 3. Submit Matchmaking Ticket to Global Real-Time Matchmaking Queue
            var client = PlayFab.PlayFabMatchmakingClient.Instance;
            bool matchComplete = false;
            bool matchFailed = false;
            string failReason = string.Empty;
            PlayFab.MatchmakingTicketDto matchedTicket = null;

            Action<PlayFab.MatchmakingTicketDto> onUpdated = (ticket) =>
            {
                if (currentState == MatchmakingState.SearchingOpponent)
                {
                    SetState(MatchmakingState.SearchingOpponent,
                        $"Searching for Opponent...\nYour Name: {myName} · Rating: {myRating} ELO (±{ticket.currentSearchRadius})\nPlayers in Queue: {ticket.playersInQueue}");
                }
            };

            Action<PlayFab.MatchmakingTicketDto> onMatched = (ticket) =>
            {
                matchedTicket = ticket;
                matchComplete = true;
            };

            client.OnTicketUpdated += onUpdated;
            client.StartMatchmaking(onMatched);

            yield return new WaitUntil(() => matchComplete || matchFailed || currentState != MatchmakingState.SearchingOpponent);

            client.OnTicketUpdated -= onUpdated;

            if (matchFailed || currentState != MatchmakingState.SearchingOpponent && !matchComplete)
            {
                if (matchFailed)
                {
                    SetState(MatchmakingState.Failed, string.IsNullOrEmpty(failReason) ? MsgConnectionFailed : failReason);
                }
                yield break;
            }

            if (matchedTicket != null)
            {
                // Step 1: Opponent Found!
                string foundMsg = $"Opponent Found!\nOpponent: {matchedTicket.matchedOpponentName} (Rating: {matchedTicket.matchedOpponentRating} ELO)\nConnecting to Match...";
                SetState(MatchmakingState.OpponentFound, foundMsg);
                yield return new WaitForSeconds(0.8f);

                // Step 2: Joining / Creating Room
                SetState(MatchmakingState.JoiningRoom, "Connecting to Match...");

                bool sessionReady = false;
                string sessionErr = string.Empty;

                if (matchedTicket.isHost)
                {
                    // Host creates the matched Fusion 2 session
                    PhotonFusionManager.Instance.CreateSession(matchedTicket.sessionName, (success, err) =>
                    {
                        sessionReady = success;
                        sessionErr = err;
                    });
                }
                else
                {
                    // Client joins the matched Fusion 2 session (allowing brief delay for host creation)
                    yield return new WaitForSeconds(0.4f);
                    PhotonFusionManager.Instance.SearchAndJoinSession(matchedTicket.sessionName, (success, err) =>
                    {
                        sessionReady = success;
                        sessionErr = err;
                    });
                }

                yield return new WaitUntil(() => sessionReady || !string.IsNullOrEmpty(sessionErr) || PhotonFusionManager.Instance.CurrentPlayerCount >= 2);

                // Step 3: Match Starting
                HandleMatchReady();
            }
        }

        private IEnumerator SearchMatchRoutine(string targetRoomName)
        {
            SetState(MatchmakingState.Connecting, MsgConnecting);

            bool connectSuccess = false;
            string connectErr = string.Empty;

            PhotonFusionManager.Instance.ConnectToPhoton((success, error) =>
            {
                connectSuccess = success;
                connectErr = error;
            });

            yield return new WaitUntil(() => PhotonFusionManager.Instance.IsConnected || !string.IsNullOrEmpty(connectErr));

            if (!connectSuccess && !PhotonFusionManager.Instance.IsConnected)
            {
                SetState(MatchmakingState.Failed, MsgConnectionFailed);
                yield break;
            }

            string searchKey = string.IsNullOrWhiteSpace(targetRoomName) ? null : targetRoomName.Trim();
            string searchStatus = string.IsNullOrEmpty(searchKey) 
                ? MsgSearchingOpponent 
                : $"Searching for Room '{searchKey}'...";

            // Begin Searching Loop
            SetState(MatchmakingState.SearchingOpponent, searchStatus);
            float elapsedTime = 0f;

            while (elapsedTime < maxSearchTimeout && currentState == MatchmakingState.SearchingOpponent)
            {
                bool searchFinished = false;
                bool joinSuccess = false;
                string searchResult = string.Empty;

                PhotonFusionManager.Instance.SearchAndJoinSession(searchKey, (success, result) =>
                {
                    searchFinished = true;
                    joinSuccess = success;
                    searchResult = result;
                });

                yield return new WaitUntil(() => searchFinished);

                if (joinSuccess)
                {
                    // Step 1: Opponent Found!
                    SetState(MatchmakingState.OpponentFound, MsgOpponentFound);
                    yield return new WaitForSeconds(0.6f);

                    // Step 2: Joining Room...
                    SetState(MatchmakingState.JoiningRoom, MsgJoiningRoom);
                    yield return new WaitForSeconds(0.6f);

                    // Step 3: Match Starting
                    HandleMatchReady();
                    yield break;
                }

                if (searchResult == "ROOM_FULL")
                {
                    SetState(MatchmakingState.Failed, $"Room '{searchKey}' is full (2/2 players).");
                    yield break;
                }

                // If room not found yet, keep searching until maxSearchTimeout
                yield return new WaitForSeconds(searchRetryInterval);
                elapsedTime += searchRetryInterval;
            }

            if (currentState == MatchmakingState.SearchingOpponent)
            {
                Debug.LogWarning("[MatchmakingManager] Search match timed out.");
                if (!string.IsNullOrEmpty(searchKey))
                {
                    SetState(MatchmakingState.Failed, $"Room '{searchKey}' not found. Please verify the room name.");
                }
                else
                {
                    SetState(MatchmakingState.Failed, MsgConnectionFailed);
                }
            }
        }

        #endregion

        #region Match Starting Flow

        private void HandleMatchReady()
        {
            if (_isTransitioning) return;
            StartCoroutine(MatchStartingRoutine());
        }

        private IEnumerator MatchStartingRoutine()
        {
            _isTransitioning = true;
            SetState(MatchmakingState.MatchStarting, MsgMatchStarting);

            // Allow 4.5 seconds for the full dramatic Versus (VS) matchup presentation and countdown
            yield return new WaitForSeconds(4.5f);

            Debug.Log($"[MatchmakingManager] Both players connected! Loading gameplay scene: '{gameplaySceneName}'...");
            if (Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
            }
            else
            {
                Debug.LogWarning($"[MatchmakingManager] Gameplay scene '{gameplaySceneName}' not in Build Settings! Loading default arena.");
                SceneManager.LoadScene(0); // Return to default scene or current
            }
        }

        #endregion

        #region Helpers & Cancellation

        public void CancelMatchmaking()
        {
            if (_searchCoroutine != null)
            {
                StopCoroutine(_searchCoroutine);
                _searchCoroutine = null;
            }

            _isTransitioning = false;
            if (PlayFab.PlayFabMatchmakingClient.HasInstance)
            {
                PlayFab.PlayFabMatchmakingClient.Instance.CancelMatchmaking();
            }
            if (Elo.EloManager.Instance != null)
            {
                Elo.EloManager.Instance.CancelMatchmaking();
            }
            if (PhotonFusionManager.Instance != null)
            {
                PhotonFusionManager.Instance.LeaveSession();
            }
            SetState(MatchmakingState.Idle, string.Empty);
            Debug.Log("[MatchmakingManager] Matchmaking canceled by player.");
        }

        private void HandleConnectionFailed(string error)
        {
            SetState(MatchmakingState.Failed, MsgConnectionFailed);
        }

        private void SetState(MatchmakingState newState, string statusMsg)
        {
            currentState = newState;
            OnStateChanged?.Invoke(newState, statusMsg);
            OnStatusMessageChanged?.Invoke(statusMsg);
            Debug.Log($"[MatchmakingManager] State: {newState} | Message: \"{statusMsg}\"");
        }

        #endregion
    }
}
