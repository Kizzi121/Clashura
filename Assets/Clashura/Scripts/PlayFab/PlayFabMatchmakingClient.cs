using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Clashura.PlayFab
{
    /// <summary>
    /// Client-side manager connecting Unity 6 UI to the PlayFab authenticated profile
    /// and Global Real-Time Matchmaking Queue backend.
    /// Handles asynchronous ticket submission, periodic polling, dynamic radius display,
    /// and match completion reporting.
    /// </summary>
    public class PlayFabMatchmakingClient : MonoBehaviour
    {
        private static PlayFabMatchmakingClient _instance;
        private static bool _isShuttingDown = false;

        public static bool HasInstance => _instance != null && !_isShuttingDown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _isShuttingDown = false;
            _instance = null;
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

        public static PlayFabMatchmakingClient Instance
        {
            get
            {
                if (_isShuttingDown) return null;
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PlayFabMatchmakingClient>();
                    if (_instance == null && Application.isPlaying && !_isShuttingDown)
                    {
                        GameObject go = new GameObject("[PlayFabMatchmakingClient]");
                        _instance = go.AddComponent<PlayFabMatchmakingClient>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("State")]
        [SerializeField] private bool isSearching = false;
        [SerializeField] private MatchmakingTicketDto currentTicket;

        private Coroutine _pollCoroutine;

        public bool IsSearching => isSearching;
        public MatchmakingTicketDto CurrentTicket => currentTicket;

        // Events
        public event Action<MatchmakingTicketDto> OnTicketEnqueued;
        public event Action<MatchmakingTicketDto> OnTicketUpdated;
        public event Action<MatchmakingTicketDto> OnMatchFound;
        public event Action OnMatchmakingCanceled;
        public event Action OnMatchmakingTimedOut;
        public event Action<string> OnMatchmakingFailed;
        public event Action<MatchResultResponseDto> OnMatchResultProcessed;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (!_isShuttingDown && Application.isPlaying)
            {
                // Ensure Auth service and local server exist
                var auth = PlayFabAuthService.Instance;
                var server = GlobalMatchmakingHttpServer.Instance;
            }
        }

        /// <summary>
        /// Begins the matchmaking flow: authenticates via PlayFab, obtains authoritative profile,
        /// submits queue ticket to the backend, and starts polling.
        /// </summary>
        public void StartMatchmaking(Action<MatchmakingTicketDto> onMatched = null)
        {
            if (isSearching)
            {
                Debug.LogWarning("[PlayFabMatchmakingClient] Already searching for match.");
                return;
            }

            StartCoroutine(StartMatchmakingRoutine(onMatched));
        }

        private IEnumerator StartMatchmakingRoutine(Action<MatchmakingTicketDto> onMatched)
        {
            isSearching = true;

            // 1. Ensure authenticated through PlayFab
            var auth = PlayFabAuthService.Instance;
            if (!auth.IsAuthenticated || auth.CurrentProfile == null)
            {
                bool authComplete = false;
                auth.Authenticate((success, profile) =>
                {
                    authComplete = true;
                });

                while (!authComplete)
                {
                    yield return null;
                }
            }

            var profile = auth.CurrentProfile;
            Debug.Log($"[PlayFabMatchmakingClient] Submitting queue ticket for '{profile.displayName}' (PlayFab ID: {profile.playFabId}, Rating: {profile.rating})...");

            // 2. Submit Enqueue Request to backend
            string url = $"{PlayFabConfig.BackendServerUrl}/api/matchmaking/enqueue";
            string jsonBody = $"{{\"playerId\":\"{profile.playFabId}\",\"displayName\":\"{profile.displayName}\",\"rating\":{profile.rating}}}";

            MatchmakingTicketDto ticket = null;

            // Try HTTP request first (works across devices/networks)
            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 5;

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    ticket = JsonUtility.FromJson<MatchmakingTicketDto>(req.downloadHandler.text);
                }
                else
                {
                    // Fallback to in-process GlobalMatchmakingService (for local editor host)
                    Debug.Log($"[PlayFabMatchmakingClient] HTTP enqueue fallback to in-memory service: {req.error}");
                    ticket = GlobalMatchmakingService.Enqueue(profile.playFabId, profile.displayName, profile.rating);
                }
            }

            if (ticket == null)
            {
                isSearching = false;
                OnMatchmakingFailed?.Invoke("Failed to submit matchmaking ticket to backend.");
                yield break;
            }

            currentTicket = ticket;
            OnTicketEnqueued?.Invoke(ticket);

            // If immediately matched on enqueue
            if (ticket.status == "Matched")
            {
                isSearching = false;
                OnMatchFound?.Invoke(ticket);
                onMatched?.Invoke(ticket);
                yield break;
            }

            // 3. Start Polling Loop
            _pollCoroutine = StartCoroutine(PollRoutine(onMatched));
        }

        private IEnumerator PollRoutine(Action<MatchmakingTicketDto> onMatched)
        {
            while (isSearching && currentTicket != null)
            {
                yield return new WaitForSeconds(1.0f);

                string url = $"{PlayFabConfig.BackendServerUrl}/api/matchmaking/poll?ticketId={currentTicket.ticketId}";
                MatchmakingTicketDto updated = null;

                using (UnityWebRequest req = UnityWebRequest.Get(url))
                {
                    req.timeout = 4;
                    yield return req.SendWebRequest();

                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        updated = JsonUtility.FromJson<MatchmakingTicketDto>(req.downloadHandler.text);
                    }
                    else
                    {
                        // In-memory fallback
                        updated = GlobalMatchmakingService.Poll(currentTicket.ticketId);
                    }
                }

                if (updated != null)
                {
                    currentTicket = updated;
                    OnTicketUpdated?.Invoke(currentTicket);

                    if (currentTicket.status == "Matched")
                    {
                        isSearching = false;
                        Debug.Log($"[PlayFabMatchmakingClient] MATCH FOUND! Session: {currentTicket.sessionName} | Opponent: '{currentTicket.matchedOpponentName}' ({currentTicket.matchedOpponentRating})");
                        OnMatchFound?.Invoke(currentTicket);
                        onMatched?.Invoke(currentTicket);
                        yield break;
                    }
                    else if (currentTicket.status == "TimedOut")
                    {
                        isSearching = false;
                        Debug.Log("[PlayFabMatchmakingClient] Matchmaking search timed out after 120s.");
                        OnMatchmakingTimedOut?.Invoke();
                        OnMatchmakingFailed?.Invoke(PlayFabConfig.MsgTimeout);
                        yield break;
                    }
                    else if (currentTicket.status == "Canceled")
                    {
                        isSearching = false;
                        OnMatchmakingCanceled?.Invoke();
                        yield break;
                    }
                }
            }
        }

        /// <summary>
        /// Cancels the active matchmaking search.
        /// </summary>
        public void CancelMatchmaking()
        {
            if (!isSearching && currentTicket == null) return;

            isSearching = false;
            if (_pollCoroutine != null)
            {
                StopCoroutine(_pollCoroutine);
                _pollCoroutine = null;
            }

            if (currentTicket != null)
            {
                StartCoroutine(CancelRoutine(currentTicket.ticketId));
                currentTicket = null;
            }

            OnMatchmakingCanceled?.Invoke();
        }

        private IEnumerator CancelRoutine(string ticketId)
        {
            string url = $"{PlayFabConfig.BackendServerUrl}/api/matchmaking/cancel?ticketId={ticketId}";
            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                req.downloadHandler = new DownloadHandlerBuffer();
                req.timeout = 4;
                yield return req.SendWebRequest();
            }
            GlobalMatchmakingService.Cancel(ticketId);
        }

        /// <summary>
        /// Submits match outcome to trusted backend for authoritative ELO calculation.
        /// </summary>
        public void SubmitMatchResult(string matchId, string winnerPlayerId, string loserPlayerId, Action<MatchResultResponseDto> onComplete = null)
        {
            StartCoroutine(SubmitMatchResultRoutine(matchId, winnerPlayerId, loserPlayerId, onComplete));
        }

        private IEnumerator SubmitMatchResultRoutine(string matchId, string winnerPlayerId, string loserPlayerId, Action<MatchResultResponseDto> onComplete)
        {
            string url = $"{PlayFabConfig.BackendServerUrl}/api/match/complete";
            string jsonBody = $"{{\"matchId\":\"{matchId}\",\"winnerPlayerId\":\"{winnerPlayerId}\",\"loserPlayerId\":\"{loserPlayerId}\"}}";

            MatchResultResponseDto result = null;

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 5;

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    result = JsonUtility.FromJson<MatchResultResponseDto>(req.downloadHandler.text);
                }
                else
                {
                    var report = new MatchResultReportDto
                    {
                        matchId = matchId,
                        winnerPlayerId = winnerPlayerId,
                        loserPlayerId = loserPlayerId
                    };
                    result = GlobalMatchmakingService.CompleteMatch(report);
                }
            }

            if (result != null && result.isVerified)
            {
                // Update local profile with authoritative delta
                var auth = PlayFabAuthService.Instance;
                if (auth != null && auth.CurrentProfile != null)
                {
                    bool isWinner = string.Equals(auth.CurrentProfile.playFabId, winnerPlayerId, StringComparison.OrdinalIgnoreCase);
                    int newRating = isWinner ? result.winnerNewRating : result.loserNewRating;
                    int delta = isWinner ? result.winnerDelta : result.loserDelta;
                    auth.ApplyMatchResult(newRating, delta, isWinner);
                }

                OnMatchResultProcessed?.Invoke(result);
                onComplete?.Invoke(result);
            }
            else
            {
                Debug.LogWarning($"[PlayFabMatchmakingClient] Match outcome report failed or unverified.");
                onComplete?.Invoke(result);
            }
        }

        private void OnApplicationQuit()
        {
            _isShuttingDown = true;
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
