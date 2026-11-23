using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Clashura.Elo
{
    /// <summary>
    /// Central Client Coordinator for ELO Rating, Profile Persistence, and Skill-Based Matchmaking.
    /// Interacts with the backend via REST API (or authoritative local server fallback)
    /// to retrieve ratings, coordinate dynamic search radius expansion, and process verified match results.
    /// </summary>
    public class EloManager : MonoBehaviour
    {
        private static EloManager _instance;
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

        public static EloManager Instance
        {
            get
            {
                if (_isShuttingDown) return null;
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<EloManager>();
                    if (_instance == null && Application.isPlaying && !_isShuttingDown)
                    {
                        GameObject go = new GameObject("[EloManager]");
                        _instance = go.AddComponent<EloManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Backend Configuration")]
        [SerializeField] private string restApiBaseUrl = "http://127.0.0.1:8088/api/";
        [SerializeField] private bool useDirectFallbackIfOffline = true;

        [Header("Local Player Rating Data")]
        [SerializeField] private PlayerEloProfile currentProfile;
        public PlayerEloProfile CurrentProfile => currentProfile;

        // Active Matchmaking Ticket
        private MatchmakingTicket _activeTicket;
        private Coroutine _pollCoroutine;
        private string _currentMatchId = string.Empty;

        public MatchmakingTicket ActiveTicket => _activeTicket;
        public string CurrentMatchId => _currentMatchId;

        // Events
        public event Action<PlayerEloProfile> OnProfileLoaded;
        public event Action<int, int, int> OnRatingUpdated; // oldRating, newRating, delta
        public event Action<MatchResultResponse> OnMatchResultReceived;
        public event Action<string, int> OnSearchRadiusExpanded; // statusMessage, radius

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureHttpServerExists();
            InitializeLocalPlayer();
        }

        private void Start()
        {
            FetchLatestRating();
        }

        private void OnApplicationQuit()
        {
            _isShuttingDown = true;
            if (_activeTicket != null && _activeTicket.status == "Queued")
            {
                CancelMatchmaking();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void EnsureHttpServerExists()
        {
            if (FindFirstObjectByType<EloHttpServer>() == null)
            {
                GameObject serverGo = new GameObject("[EloHttpServer]");
                serverGo.AddComponent<EloHttpServer>();
            }
        }

        #region Player Identity & Rating Retrieval

        /// <summary>
        /// Generates or retrieves the device/instance's persistent unique Player ID.
        /// Distinct between Editor and Standalone builds for multi-process testing on the same machine.
        /// </summary>
        public string GetLocalPlayerId()
        {
            string key = "CLASHURA_PLAYER_ID";
            if (!PlayerPrefs.HasKey(key))
            {
                string prefix = Application.isEditor ? "Player_Editor_" : "Player_Build_";
                string newId = prefix + Guid.NewGuid().ToString("N").Substring(0, 8);
                PlayerPrefs.SetString(key, newId);
                PlayerPrefs.Save();
            }
            return PlayerPrefs.GetString(key);
        }

        public string GetLocalPlayerName()
        {
            string defaultName = Application.isEditor ? "Champion Prime" : "Shadow Striker";
            return PlayerPrefs.GetString("CLASHURA_PLAYER_NAME", defaultName);
        }

        private void InitializeLocalPlayer()
        {
            string id = GetLocalPlayerId();
            string name = GetLocalPlayerName();
            int startingPoints = PlayerPrefs.GetInt("CLASHURA_PLAYER_POINTS", 1000);

            // Authoritatively load or register from database
            currentProfile = EloServerService.GetOrRegisterPlayer(id, name, startingPoints);
            Debug.Log($"[EloManager] Initialized Player '{currentProfile.playerName}' (ID: {id}) | Rating: {currentProfile.rating} | Wins: {currentProfile.wins} | Losses: {currentProfile.losses}");
            OnProfileLoaded?.Invoke(currentProfile);
        }

        /// <summary>
        /// Fetches the latest authoritative player rating from the online database.
        /// </summary>
        public void FetchLatestRating(Action<PlayerEloProfile> onComplete = null)
        {
            string playerId = GetLocalPlayerId();
            currentProfile = EloServerService.GetOrRegisterPlayer(playerId, GetLocalPlayerName());
            OnProfileLoaded?.Invoke(currentProfile);
            onComplete?.Invoke(currentProfile);
        }

        #endregion

        #region ELO Matchmaking Flow

        /// <summary>
        /// Starts Skill-Based ELO Matchmaking with dynamic range expansion (±50 -> ±100 -> ±150).
        /// </summary>
        public void StartMatchmaking(
            Action<MatchmakingTicket> onMatched,
            Action<string, int> onStatusUpdate,
            Action<string> onFailed)
        {
            if (_pollCoroutine != null)
            {
                StopCoroutine(_pollCoroutine);
            }

            // Always refresh rating before queuing
            FetchLatestRating();

            // Enqueue ticket authoritatively
            _activeTicket = EloServerService.EnqueueMatchmaking(currentProfile.playerId, currentProfile.playerName);

            _pollCoroutine = StartCoroutine(MatchmakingPollRoutine(onMatched, onStatusUpdate, onFailed));
        }

        private IEnumerator MatchmakingPollRoutine(
            Action<MatchmakingTicket> onMatched,
            Action<string, int> onStatusUpdate,
            Action<string> onFailed)
        {
            if (_activeTicket == null)
            {
                onFailed?.Invoke("Failed to generate matchmaking ticket.");
                yield break;
            }

            int lastReportedRadius = _activeTicket.currentSearchRadius;
            float maxTimeout = 45f;
            float elapsed = 0f;

            while (elapsed < maxTimeout)
            {
                yield return new WaitForSeconds(0.75f);
                elapsed += 0.75f;

                // Poll authoritative server
                _activeTicket = EloServerService.PollMatchmaking(_activeTicket.ticketId);

                if (_activeTicket == null)
                {
                    onFailed?.Invoke("Matchmaking session was lost.");
                    yield break;
                }

                if (_activeTicket.status == "Matched")
                {
                    _currentMatchId = _activeTicket.matchId;
                    Debug.Log($"[EloManager] Match pairing successful! Opponent: {_activeTicket.matchedOpponentName} (Rating: {_activeTicket.matchedOpponentRating}) | Session: {_activeTicket.matchedSessionName}");
                    onMatched?.Invoke(_activeTicket);
                    yield break;
                }

                if (_activeTicket.status == "Canceled")
                {
                    yield break;
                }

                // Check for dynamic range expansion update
                if (_activeTicket.currentSearchRadius != lastReportedRadius)
                {
                    lastReportedRadius = _activeTicket.currentSearchRadius;
                    string statusMsg = $"Expanding Search (±{lastReportedRadius} ELO)...";
                    onStatusUpdate?.Invoke(statusMsg, lastReportedRadius);
                    OnSearchRadiusExpanded?.Invoke(statusMsg, lastReportedRadius);
                }
            }

            // If timed out, cancel
            CancelMatchmaking();
            onFailed?.Invoke("Matchmaking search timed out. Expanding to global pool.");
        }

        public void CancelMatchmaking()
        {
            if (_pollCoroutine != null)
            {
                StopCoroutine(_pollCoroutine);
                _pollCoroutine = null;
            }

            if (_activeTicket != null)
            {
                EloServerService.CancelMatchmaking(_activeTicket.ticketId);
                _activeTicket = null;
            }

            Debug.Log("[EloManager] Canceled matchmaking ticket.");
        }

        public void SetCurrentMatchId(string matchId)
        {
            _currentMatchId = matchId;
        }

        #endregion

        #region Match Outcome & ELO Result Processing

        /// <summary>
        /// Reports match outcome to the authoritative backend.
        /// Validates match completion, calculates ELO ratings, updates database records, and returns verified rating delta.
        /// </summary>
        public void SubmitMatchResult(string winnerId, Action<MatchResultResponse> onComplete)
        {
            SubmitMatchResult(_currentMatchId, winnerId, onComplete);
        }

        public void SubmitMatchResult(string matchId, string winnerId, Action<MatchResultResponse> onComplete)
        {
            if (string.IsNullOrWhiteSpace(matchId))
            {
                Debug.LogWarning("[EloManager] Cannot report match without a valid Match ID.");
                onComplete?.Invoke(new MatchResultResponse { success = false, errorMessage = "MISSING_MATCH_ID" });
                return;
            }

            var report = new MatchResultReport
            {
                matchId = matchId,
                reportingPlayerId = GetLocalPlayerId(),
                winnerId = winnerId,
                isForfeit = false
            };

            var response = EloServerService.ProcessMatchResult(report);

            if (response != null && response.success)
            {
                int oldRating = currentProfile.rating;
                currentProfile.rating = response.localPlayerFinalRating;
                currentProfile.wins = response.localPlayerWins;
                currentProfile.losses = response.localPlayerLosses;
                currentProfile.matchesPlayed = response.localPlayerMatches;

                // Sync PlayerPrefs display points
                PlayerPrefs.SetInt("CLASHURA_PLAYER_POINTS", currentProfile.rating);
                PlayerPrefs.Save();

                OnRatingUpdated?.Invoke(oldRating, currentProfile.rating, response.localPlayerDelta);
                OnMatchResultReceived?.Invoke(response);
            }

            onComplete?.Invoke(response);
        }

        #endregion
    }
}
