using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Clashura.PlayFab
{
    /// <summary>
    /// Authenticates the local device with PlayFab, retrieves authoritative player statistics,
    /// handles initial migration from PlayerPrefs, and prevents overwriting existing profile data.
    /// </summary>
    public class PlayFabAuthService : MonoBehaviour
    {
        private static PlayFabAuthService _instance;
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

        public static PlayFabAuthService Instance
        {
            get
            {
                if (_isShuttingDown) return null;
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PlayFabAuthService>();
                    if (_instance == null && Application.isPlaying && !_isShuttingDown)
                    {
                        GameObject go = new GameObject("[PlayFabAuthService]");
                        _instance = go.AddComponent<PlayFabAuthService>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("State")]
        [SerializeField] private bool isAuthenticated = false;
        [SerializeField] private string sessionTicket = string.Empty;
        [SerializeField] private PlayFabPlayerProfile currentProfile;

        // Account override for multi-device/multi-account testing on one machine
        private static string _overrideCustomId = null;
        private static string _overrideDisplayName = null;
        private static int? _overrideRating = null;

        public bool IsAuthenticated => isAuthenticated;
        public string SessionTicket => sessionTicket;
        public PlayFabPlayerProfile CurrentProfile => currentProfile;

        public event Action<PlayFabPlayerProfile> OnProfileLoaded;
        public event Action<int, int, int> OnRatingUpdated; // oldRating, newRating, delta

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Sets an account override for testing multiple distinct accounts on the same machine.
        /// </summary>
        public static void SetAccountOverride(string customId, string displayName, int? rating = null)
        {
            _overrideCustomId = customId;
            _overrideDisplayName = displayName;
            _overrideRating = rating;
        }

        public static void ClearAccountOverride()
        {
            _overrideCustomId = null;
            _overrideDisplayName = null;
            _overrideRating = null;
        }

        /// <summary>
        /// Generates or retrieves a unique persistent device ID for this physical device.
        /// Distinct between Editor and Standalone builds so two local instances get distinct accounts.
        /// </summary>
        public string GetDeviceCustomId()
        {
            if (!string.IsNullOrEmpty(_overrideCustomId))
            {
                return _overrideCustomId;
            }

            string key = "CLASHURA_DEVICE_CUSTOM_ID";
            if (!PlayerPrefs.HasKey(key))
            {
                string deviceId = SystemInfo.deviceUniqueIdentifier;
                if (string.IsNullOrEmpty(deviceId) || deviceId == SystemInfo.unsupportedIdentifier)
                {
                    deviceId = Guid.NewGuid().ToString("N");
                }
                if (Application.isEditor)
                {
                    deviceId += "_Editor";
                }
                PlayerPrefs.SetString(key, deviceId);
                PlayerPrefs.Save();
            }
            return PlayerPrefs.GetString(key);
        }

        /// <summary>
        /// Authenticates the player through PlayFab using LoginWithCustomID.
        /// </summary>
        public void Authenticate(Action<bool, PlayFabPlayerProfile> onComplete = null)
        {
            StartCoroutine(AuthenticateRoutine(onComplete));
        }

        private IEnumerator AuthenticateRoutine(Action<bool, PlayFabPlayerProfile> onComplete)
        {
            string customId = GetDeviceCustomId();
            string titleId = PlayFabConfig.TitleId;

            Debug.Log($"[PlayFabAuthService] Authenticating device '{customId}' with PlayFab Title '{titleId}'...");

            // Construct PlayFab Login request
            string url = $"https://{titleId}.playfabapi.com/Client/LoginWithCustomID";
            string jsonBody = $"{{\"TitleId\":\"{titleId}\",\"CustomId\":\"{customId}\",\"CreateAccount\":true}}";

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 8;

                yield return req.SendWebRequest();

                bool loginParsed = false;
                string playFabId = string.Empty;

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string responseJson = req.downloadHandler.text;
                        var baseResp = JsonUtility.FromJson<PlayFabBaseResponse<PlayFabLoginResponseData>>(responseJson);

                        if (baseResp != null && baseResp.data != null)
                        {
                            sessionTicket = baseResp.data.SessionTicket;
                            playFabId = baseResp.data.PlayFabId;
                            loginParsed = true;
                            Debug.Log($"[PlayFabAuthService] PlayFab Login Success! PlayFabId: {playFabId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[PlayFabAuthService] Failed parsing PlayFab login response: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[PlayFabAuthService] PlayFab Login endpoint returned {req.responseCode}: {req.error}. Using fallback profile.");
                }

                if (loginParsed)
                {
                    // Fetch authoritative user profile and data outside try block
                    yield return FetchUserDataRoutine(playFabId, onComplete);
                    yield break;
                }
            }

            // Fallback: If PlayFab cloud is offline or Title ID is not yet provisioned on Azure,
            // maintain a local authoritative profile with device-unique PlayFab ID contract
            EnsureFallbackProfile(customId);
            isAuthenticated = true;
            OnProfileLoaded?.Invoke(currentProfile);
            onComplete?.Invoke(true, currentProfile);
        }

        private IEnumerator FetchUserDataRoutine(string playFabId, Action<bool, PlayFabPlayerProfile> onComplete)
        {
            string titleId = PlayFabConfig.TitleId;
            string url = $"https://{titleId}.playfabapi.com/Client/GetUserData";
            string jsonBody = "{}";

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("X-Authentication", sessionTicket);
                req.timeout = 8;

                yield return req.SendWebRequest();

                bool hasExistingData = false;
                string displayName = !string.IsNullOrEmpty(_overrideDisplayName) 
                    ? _overrideDisplayName 
                    : PlayerPrefs.GetString("CLASHURA_PLAYER_NAME", PlayFabConfig.DefaultDisplayName);

                int rating = _overrideRating.HasValue 
                    ? _overrideRating.Value 
                    : PlayerPrefs.GetInt("CLASHURA_PLAYER_POINTS", PlayFabConfig.DefaultRating);

                int wins = PlayerPrefs.GetInt("CLASHURA_WINS", 0);
                int losses = PlayerPrefs.GetInt("CLASHURA_LOSSES", 0);
                int matchesPlayed = 0;

                if (req.result == UnityWebRequest.Result.Success)
                {
                    string text = req.downloadHandler.text;
                    // Check if existing data keys are present in PlayFab
                    if (text.Contains(PlayFabConfig.KeyRating))
                    {
                        hasExistingData = true;
                        rating = ExtractIntFromJson(text, PlayFabConfig.KeyRating, rating);
                        wins = ExtractIntFromJson(text, PlayFabConfig.KeyWins, wins);
                        losses = ExtractIntFromJson(text, PlayFabConfig.KeyLosses, losses);
                        matchesPlayed = ExtractIntFromJson(text, PlayFabConfig.KeyMatchesPlayed, wins + losses);

                        if (text.Contains(PlayFabConfig.KeyDisplayName))
                        {
                            string remoteName = ExtractStringFromJson(text, PlayFabConfig.KeyDisplayName, displayName);
                            if (!string.IsNullOrEmpty(remoteName) && !remoteName.StartsWith("Player_"))
                            {
                                displayName = remoteName;
                            }
                        }

                        // Sanitize generated placeholders to preserve original Commander / 1250 identity
                        if (string.IsNullOrEmpty(displayName) || displayName.StartsWith("Player_"))
                        {
                            displayName = PlayFabConfig.DefaultDisplayName; // "Commander"
                            yield return UpdateUserDataRoutine(rating, wins, losses, matchesPlayed, displayName);
                        }

                        if (rating == 1000)
                        {
                            rating = PlayFabConfig.DefaultRating; // 1250
                            yield return UpdateUserDataRoutine(rating, wins, losses, matchesPlayed, displayName);
                        }

                        Debug.Log($"[PlayFabAuthService] Authoritative PlayFab Identity: '{displayName}', Rating: {rating}, {wins}W - {losses}L");
                    }
                }

                // If brand new account on PlayFab, migrate from local PlayerPrefs preserving original identity
                if (!hasExistingData)
                {
                    if (string.IsNullOrEmpty(displayName) || displayName.StartsWith("Player_"))
                    {
                        displayName = PlayFabConfig.DefaultDisplayName; // "Commander"
                        rating = PlayFabConfig.DefaultRating; // 1250
                    }

                    Debug.Log($"[PlayFabAuthService] Migrating original player identity '{displayName}' ({rating} ELO) to PlayFab.");

                    // Cache locally as mirror
                    PlayerPrefs.SetString("CLASHURA_PLAYER_NAME", displayName);
                    PlayerPrefs.SetInt("CLASHURA_PLAYER_POINTS", rating);
                    PlayerPrefs.Save();

                    // Save initial migration data to PlayFab
                    yield return UpdateUserDataRoutine(rating, wins, losses, matchesPlayed, displayName);
                }

                // Ensure local PlayerPrefs cache matches authoritative PlayFab identity
                PlayerPrefs.SetString("CLASHURA_PLAYER_NAME", displayName);
                PlayerPrefs.SetInt("CLASHURA_PLAYER_POINTS", rating);
                PlayerPrefs.Save();

                currentProfile = new PlayFabPlayerProfile
                {
                    playFabId = playFabId,
                    displayName = displayName,
                    rating = rating,
                    wins = wins,
                    losses = losses,
                    matchesPlayed = matchesPlayed,
                    lastActiveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                isAuthenticated = true;
                OnProfileLoaded?.Invoke(currentProfile);
                onComplete?.Invoke(true, currentProfile);
            }
        }

        private IEnumerator UpdateUserDataRoutine(int rating, int wins, int losses, int matchesPlayed, string displayName)
        {
            string titleId = PlayFabConfig.TitleId;
            string url = $"https://{titleId}.playfabapi.com/Client/UpdateUserData";
            string jsonBody = $"{{\"Data\":{{\"{PlayFabConfig.KeyRating}\":\"{rating}\",\"{PlayFabConfig.KeyWins}\":\"{wins}\",\"{PlayFabConfig.KeyLosses}\":\"{losses}\",\"{PlayFabConfig.KeyMatchesPlayed}\":\"{matchesPlayed}\",\"{PlayFabConfig.KeyDisplayName}\":\"{displayName}\"}}}}";

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("X-Authentication", sessionTicket);
                req.timeout = 8;
                yield return req.SendWebRequest();
            }
        }

        private void EnsureFallbackProfile(string customId)
        {
            string playFabId = "PF_" + customId.GetHashCode().ToString("X8");
            string name = !string.IsNullOrEmpty(_overrideDisplayName) 
                ? _overrideDisplayName 
                : PlayerPrefs.GetString("CLASHURA_PLAYER_NAME", PlayFabConfig.DefaultDisplayName);

            if (string.IsNullOrEmpty(name) || name.StartsWith("Player_"))
            {
                name = PlayFabConfig.DefaultDisplayName; // "Commander"
            }

            int rating = _overrideRating.HasValue 
                ? _overrideRating.Value 
                : PlayerPrefs.GetInt("CLASHURA_PLAYER_POINTS", PlayFabConfig.DefaultRating);

            if (rating == 1000)
            {
                rating = PlayFabConfig.DefaultRating; // 1250
            }

            int wins = PlayerPrefs.GetInt("CLASHURA_WINS", 0);
            int losses = PlayerPrefs.GetInt("CLASHURA_LOSSES", 0);

            // Ensure PlayerPrefs preserves this original identity
            PlayerPrefs.SetString("CLASHURA_PLAYER_NAME", name);
            PlayerPrefs.SetInt("CLASHURA_PLAYER_POINTS", rating);
            PlayerPrefs.Save();

            currentProfile = new PlayFabPlayerProfile
            {
                playFabId = playFabId,
                displayName = name,
                rating = rating,
                wins = wins,
                losses = losses,
                matchesPlayed = wins + losses,
                lastActiveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        /// <summary>
        /// Updates the local player profile after an authoritative match result from the backend.
        /// </summary>
        public void ApplyMatchResult(int newRating, int delta, bool won)
        {
            if (currentProfile == null) return;

            int oldRating = currentProfile.rating;
            currentProfile.rating = newRating;
            currentProfile.matchesPlayed++;
            if (won) currentProfile.wins++;
            else currentProfile.losses++;
            currentProfile.lastActiveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Persist to local PlayerPrefs as migration mirror
            PlayerPrefs.SetInt("CLASHURA_PLAYER_POINTS", newRating);
            PlayerPrefs.SetInt("CLASHURA_WINS", currentProfile.wins);
            PlayerPrefs.SetInt("CLASHURA_LOSSES", currentProfile.losses);
            PlayerPrefs.Save();

            // Also update PlayFab UserData if connected
            if (!string.IsNullOrEmpty(sessionTicket))
            {
                StartCoroutine(UpdateUserDataRoutine(currentProfile.rating, currentProfile.wins, currentProfile.losses, currentProfile.matchesPlayed, currentProfile.displayName));
            }

            OnRatingUpdated?.Invoke(oldRating, newRating, delta);
            OnProfileLoaded?.Invoke(currentProfile);
        }

        private int ExtractIntFromJson(string json, string key, int defaultVal)
        {
            try
            {
                int keyIdx = json.IndexOf($"\"{key}\"");
                if (keyIdx == -1) return defaultVal;
                int valIdx = json.IndexOf("\"Value\":\"", keyIdx);
                if (valIdx == -1) return defaultVal;
                int start = valIdx + 9;
                int end = json.IndexOf("\"", start);
                string strVal = json.Substring(start, end - start);
                return int.TryParse(strVal, out int result) ? result : defaultVal;
            }
            catch
            {
                return defaultVal;
            }
        }

        private string ExtractStringFromJson(string json, string key, string defaultVal)
        {
            try
            {
                int keyIdx = json.IndexOf($"\"{key}\"");
                if (keyIdx == -1) return defaultVal;
                int valIdx = json.IndexOf("\"Value\":\"", keyIdx);
                if (valIdx == -1) return defaultVal;
                int start = valIdx + 9;
                int end = json.IndexOf("\"", start);
                string strVal = json.Substring(start, end - start);
                return !string.IsNullOrEmpty(strVal) ? strVal : defaultVal;
            }
            catch
            {
                return defaultVal;
            }
        }

        private void OnApplicationQuit()
        {
            _isShuttingDown = true;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
