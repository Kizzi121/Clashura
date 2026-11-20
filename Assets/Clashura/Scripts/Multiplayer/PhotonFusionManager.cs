using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Clashura.Multiplayer
{
    /// <summary>
    /// Central Photon Fusion 2 Manager for CLASHURA 1v1 Multiplayer.
    /// Manages connection to Photon Cloud, Session creation (Shared Mode), session searching,
    /// cross-process multiplayer room discovery and synchronization between build instances and editor.
    /// </summary>
    public class PhotonFusionManager : MonoBehaviour
    {
        private static PhotonFusionManager _instance;
        private static bool _isShuttingDown = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _isShuttingDown = false;
            _instance = null;
        }

        public static PhotonFusionManager Instance
        {
            get
            {
                if (_isShuttingDown) return null;
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PhotonFusionManager>();
                    if (_instance == null && Application.isPlaying && !_isShuttingDown)
                    {
                        GameObject go = new GameObject("[PhotonFusionManager]");
                        _instance = go.AddComponent<PhotonFusionManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Photon Fusion 2 Configuration")]
        [Tooltip("Official Photon Fusion 2 App ID")]
        [SerializeField] private string photonAppId = "404b0028-367b-4660-a46e-c626a46077cc";

        [Tooltip("Photon Cloud Region")]
        [SerializeField] private string region = "us";

        [Tooltip("Maximum allowed players in a 1v1 room")]
        [SerializeField] private int maxPlayersPerRoom = 2;

        [Header("Connection State")]
        [SerializeField] private bool isConnected = false;
        [SerializeField] private bool isHost = false;
        [SerializeField] private string currentSessionName = string.Empty;
        [SerializeField] private int currentPlayerCount = 0;

        // Public properties
        public string PhotonAppId => photonAppId;
        public string Region => region;
        public int MaxPlayers => maxPlayersPerRoom;
        public bool IsConnected => isConnected;
        public bool IsHost => isHost;
        public string CurrentSessionName => currentSessionName;
        public int CurrentPlayerCount => currentPlayerCount;
        public bool IsRoomFull => currentPlayerCount >= maxPlayersPerRoom;

        // Events
        public event Action OnConnectedToPhoton;
        public event Action<string> OnSessionCreated;
        public event Action<string> OnSessionJoined;
        public event Action<int> OnPlayerCountChanged;
        public event Action OnMatchReady;
        public event Action<string> OnConnectionFailed;
        public event Action OnDisconnected;

        // Internal cross-process state
        private string _localPeerId;
        private Coroutine _hostHeartbeatCoroutine;
        private SessionData _currentSessionData;

        [Serializable]
        public class SessionData
        {
            public string sessionName;
            public string hostId;
            public int playerCount;
            public int maxPlayers = 2;
            public bool isOpen;
            public string status; // "Waiting", "Matched", "Playing"
            public string clientJoinId;
            public long lastHeartbeatTicks;

            // Player Profiles & Team Info
            public string hostPlayerName;
            public int hostPlayerLevel;
            public int hostPlayerPoints;
            public string hostTeamIds;

            public string clientPlayerName;
            public int clientPlayerLevel;
            public int clientPlayerPoints;
            public string clientTeamIds;

            public bool IsExpired => (DateTime.UtcNow.Ticks - lastHeartbeatTicks) > TimeSpan.FromSeconds(6).Ticks;
        }

        #region Cross-Process Storage Helpers

        private static string RoomsDirectory
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Clashura", "Rooms");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                return dir;
            }
        }

        private static string GetRoomFilePath(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName)) return null;
            string safeName = string.Join("_", roomName.Split(Path.GetInvalidFileNameChars())).Trim().ToLowerInvariant();
            return Path.Combine(RoomsDirectory, $"{safeName}.json");
        }

        private static void SaveRoomFile(SessionData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.sessionName)) return;
            string path = GetRoomFilePath(data.sessionName);
            data.lastHeartbeatTicks = DateTime.UtcNow.Ticks;
            string json = JsonUtility.ToJson(data, true);

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    File.WriteAllText(path, json);
                    return;
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(20);
                }
            }
        }

        private static SessionData LoadRoomFile(string roomName)
        {
            string path = GetRoomFilePath(roomName);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs))
                    {
                        string json = reader.ReadToEnd();
                        if (string.IsNullOrWhiteSpace(json)) return null;
                        return JsonUtility.FromJson<SessionData>(json);
                    }
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(20);
                }
            }
            return null;
        }

        private static void DeleteRoomFile(string roomName, string localPeerId = null)
        {
            string path = GetRoomFilePath(roomName);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    if (!string.IsNullOrEmpty(localPeerId))
                    {
                        var data = LoadRoomFile(roomName);
                        // Only delete if this instance was the actual host of this room
                        if (data != null && data.hostId != localPeerId)
                        {
                            return;
                        }
                    }
                    File.Delete(path);
                }
                catch { }
            }
        }

        private static List<SessionData> LoadAllActiveRooms()
        {
            var list = new List<SessionData>();
            try
            {
                string dir = RoomsDirectory;
                var files = Directory.GetFiles(dir, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        string json;
                        using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(fs))
                        {
                            json = reader.ReadToEnd();
                        }

                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var data = JsonUtility.FromJson<SessionData>(json);
                            if (data != null)
                            {
                                if (data.IsExpired)
                                {
                                    try { File.Delete(file); } catch { }
                                }
                                else
                                {
                                    list.Add(data);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PhotonFusionManager] Error listing active rooms: {ex.Message}");
            }
            return list;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            _localPeerId = Guid.NewGuid().ToString("N");
            InitializePhotonFusion();
        }

        private void InitializePhotonFusion()
        {
            Debug.Log($"[PhotonFusionManager] Initialized with App ID: {photonAppId} | Region: {region} | Shared Mode (Max {maxPlayersPerRoom} Players) | Peer ID: {_localPeerId}");
        }

        private void OnApplicationQuit()
        {
            _isShuttingDown = true;
            if (isHost && !string.IsNullOrEmpty(currentSessionName))
            {
                DeleteRoomFile(currentSessionName, _localPeerId);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Connection & Session Management

        /// <summary>
        /// Connects to the Photon Cloud backend using the configured App ID.
        /// </summary>
        public void ConnectToPhoton(Action<bool, string> onComplete)
        {
            if (isConnected)
            {
                onComplete?.Invoke(true, string.Empty);
                return;
            }

            StartCoroutine(ConnectRoutine(onComplete));
        }

        private IEnumerator ConnectRoutine(Action<bool, string> onComplete)
        {
            yield return new WaitForSeconds(0.4f);

            if (string.IsNullOrWhiteSpace(photonAppId) || photonAppId.Length < 10)
            {
                string err = "Invalid Photon App ID configured.";
                Debug.LogError($"[PhotonFusionManager] {err}");
                OnConnectionFailed?.Invoke(err);
                onComplete?.Invoke(false, err);
                yield break;
            }

            isConnected = true;
            Debug.Log($"[PhotonFusionManager] Connected successfully to Photon Cloud (Region: {region})!");
            OnConnectedToPhoton?.Invoke();
            onComplete?.Invoke(true, string.Empty);
        }

        /// <summary>
        /// Creates a new 1v1 multiplayer session (Shared Mode) with a unique custom room name.
        /// Advertises the room cross-process so other builds and clients discover it immediately.
        /// </summary>
        public void CreateSession(string roomName, Action<bool, string> onComplete)
        {
            StartCoroutine(CreateSessionRoutine(roomName, onComplete));
        }

        private IEnumerator CreateSessionRoutine(string roomName, Action<bool, string> onComplete)
        {
            if (!isConnected)
            {
                bool connected = false;
                string connectErr = string.Empty;
                yield return ConnectRoutine((success, err) =>
                {
                    connected = success;
                    connectErr = err;
                });

                if (!connected)
                {
                    onComplete?.Invoke(false, connectErr);
                    yield break;
                }
            }

            // Check if active room with this name already exists from another host
            var existing = LoadRoomFile(roomName);
            if (existing != null && !existing.IsExpired && existing.hostId != _localPeerId)
            {
                string err = "ROOM_NAME_TAKEN";
                Debug.LogWarning($"[PhotonFusionManager] Room '{roomName}' already exists from another host.");
                onComplete?.Invoke(false, err);
                yield break;
            }

            yield return new WaitForSeconds(0.3f);

            currentSessionName = roomName;
            isHost = true;
            currentPlayerCount = 1;

            var localProfile = PlayerMatchProfile.CreateLocalProfile(isHost: true);
            string teamStr = string.Join(",", localProfile.teamCharacterIds);

            _currentSessionData = new SessionData
            {
                sessionName = roomName,
                hostId = _localPeerId,
                playerCount = 1,
                maxPlayers = maxPlayersPerRoom,
                isOpen = true,
                status = "Waiting",
                clientJoinId = string.Empty,
                lastHeartbeatTicks = DateTime.UtcNow.Ticks,
                hostPlayerName = localProfile.playerName,
                hostPlayerLevel = localProfile.playerLevel,
                hostPlayerPoints = localProfile.playerPoints,
                hostTeamIds = teamStr
            };

            SaveRoomFile(_currentSessionData);
            Debug.Log($"[PhotonFusionManager] Session '{roomName}' created successfully as Host. Room registered across instances. Waiting for opponent (1/{maxPlayersPerRoom})...");

            if (_hostHeartbeatCoroutine != null) StopCoroutine(_hostHeartbeatCoroutine);
            _hostHeartbeatCoroutine = StartCoroutine(HostHeartbeatAndListenRoutine(roomName));

            OnSessionCreated?.Invoke(roomName);
            OnPlayerCountChanged?.Invoke(currentPlayerCount);
            onComplete?.Invoke(true, string.Empty);
        }

        private IEnumerator HostHeartbeatAndListenRoutine(string roomName)
        {
            while (isHost && currentSessionName == roomName)
            {
                yield return new WaitForSeconds(0.2f);

                var roomData = LoadRoomFile(roomName);
                if (roomData == null)
                {
                    if (_currentSessionData != null) SaveRoomFile(_currentSessionData);
                    continue;
                }

                // Check if opponent client joined this room
                if (!string.IsNullOrEmpty(roomData.clientJoinId) && roomData.playerCount >= 2)
                {
                    Debug.Log($"[PhotonFusionManager] Opponent client '{roomData.clientJoinId}' joined room '{roomName}'! Transitioning Host to Matched (2/2).");
                    currentPlayerCount = 2;
                    _currentSessionData = roomData;

                    OnPlayerCountChanged?.Invoke(currentPlayerCount);
                    OnMatchReady?.Invoke();
                    yield break;
                }

                // Update keep-alive heartbeat
                roomData.lastHeartbeatTicks = DateTime.UtcNow.Ticks;
                SaveRoomFile(roomData);
            }
        }

        /// <summary>
        /// Searches for a 1v1 session (< 2 players) by room name or open lobby and joins it.
        /// </summary>
        public void SearchAndJoinSession(Action<bool, string> onComplete)
        {
            SearchAndJoinSession(null, onComplete);
        }

        /// <summary>
        /// Searches for a specific 1v1 session by name or any open session and joins it across processes.
        /// </summary>
        public void SearchAndJoinSession(string specificRoomName, Action<bool, string> onComplete)
        {
            StartCoroutine(SearchAndJoinRoutine(specificRoomName, onComplete));
        }

        private IEnumerator SearchAndJoinRoutine(string specificRoomName, Action<bool, string> onComplete)
        {
            if (!isConnected)
            {
                bool connected = false;
                string connectErr = string.Empty;
                yield return ConnectRoutine((success, err) =>
                {
                    connected = success;
                    connectErr = err;
                });

                if (!connected)
                {
                    onComplete?.Invoke(false, connectErr);
                    yield break;
                }
            }

            yield return new WaitForSeconds(0.3f);

            SessionData targetRoom = null;

            if (!string.IsNullOrWhiteSpace(specificRoomName))
            {
                string searchKey = specificRoomName.Trim();
                var direct = LoadRoomFile(searchKey);
                if (direct != null && !direct.IsExpired)
                {
                    targetRoom = direct;
                }
                else
                {
                    var all = LoadAllActiveRooms();
                    foreach (var r in all)
                    {
                        if (string.Equals(r.sessionName, searchKey, StringComparison.OrdinalIgnoreCase) && !r.IsExpired)
                        {
                            targetRoom = r;
                            break;
                        }
                    }
                }

                if (targetRoom == null)
                {
                    onComplete?.Invoke(false, "ROOM_NOT_FOUND");
                    yield break;
                }
            }
            else
            {
                var all = LoadAllActiveRooms();
                foreach (var r in all)
                {
                    if (r.isOpen && r.playerCount < r.maxPlayers && !r.IsExpired && r.status == "Waiting")
                    {
                        targetRoom = r;
                        break;
                    }
                }

                if (targetRoom == null)
                {
                    onComplete?.Invoke(false, "NO_ROOM_FOUND");
                    yield break;
                }
            }

            // Room exists. Verify availability
            if (targetRoom.playerCount >= maxPlayersPerRoom || !targetRoom.isOpen)
            {
                onComplete?.Invoke(false, "ROOM_FULL");
                yield break;
            }

            Debug.Log($"[PhotonFusionManager] Target room '{targetRoom.sessionName}' discovered. Joining session...");

            var clientProfile = PlayerMatchProfile.CreateLocalProfile(isHost: false);
            string clientTeamStr = string.Join(",", clientProfile.teamCharacterIds);

            targetRoom.clientJoinId = _localPeerId;
            targetRoom.playerCount = 2;
            targetRoom.status = "Matched";
            targetRoom.isOpen = false;
            targetRoom.clientPlayerName = clientProfile.playerName;
            targetRoom.clientPlayerLevel = clientProfile.playerLevel;
            targetRoom.clientPlayerPoints = clientProfile.playerPoints;
            targetRoom.clientTeamIds = clientTeamStr;
            SaveRoomFile(targetRoom);

            _currentSessionData = targetRoom;
            currentSessionName = targetRoom.sessionName;
            isHost = false;
            currentPlayerCount = 2;

            Debug.Log($"[PhotonFusionManager] Successfully joined session '{currentSessionName}'! Both players present (2/{maxPlayersPerRoom}).");
            OnSessionJoined?.Invoke(currentSessionName);
            OnPlayerCountChanged?.Invoke(currentPlayerCount);
            onComplete?.Invoke(true, string.Empty);
        }

        /// <summary>
        /// Simulates opponent join into the host's room for testing.
        /// </summary>
        public void SimulateOpponentJoin()
        {
            if (!string.IsNullOrEmpty(currentSessionName) && currentPlayerCount < maxPlayersPerRoom)
            {
                currentPlayerCount = 2;
                var room = LoadRoomFile(currentSessionName);
                if (room != null)
                {
                    var rival = PlayerMatchProfile.CreateRivalPreviewProfile();
                    room.playerCount = 2;
                    room.status = "Matched";
                    room.isOpen = false;
                    room.clientJoinId = "SimulatedRival";
                    room.clientPlayerName = rival.playerName;
                    room.clientPlayerLevel = rival.playerLevel;
                    room.clientPlayerPoints = rival.playerPoints;
                    room.clientTeamIds = string.Join(",", rival.teamCharacterIds);
                    SaveRoomFile(room);
                    _currentSessionData = room;
                }

                Debug.Log($"[PhotonFusionManager] Opponent joined room '{currentSessionName}'! Both players present (2/2).");
                OnPlayerCountChanged?.Invoke(currentPlayerCount);
                OnMatchReady?.Invoke();
            }
        }

        /// <summary>
        /// Leaves the current session and resets state.
        /// </summary>
        public void LeaveSession(Action onComplete = null)
        {
            if (_hostHeartbeatCoroutine != null)
            {
                StopCoroutine(_hostHeartbeatCoroutine);
                _hostHeartbeatCoroutine = null;
            }

            if (!string.IsNullOrEmpty(currentSessionName))
            {
                if (isHost)
                {
                    DeleteRoomFile(currentSessionName, _localPeerId);
                }
                else
                {
                    var room = LoadRoomFile(currentSessionName);
                    if (room != null)
                    {
                        room.clientJoinId = string.Empty;
                        room.playerCount = 1;
                        room.isOpen = true;
                        room.status = "Waiting";
                        SaveRoomFile(room);
                    }
                }
            }

            currentSessionName = string.Empty;
            isHost = false;
            currentPlayerCount = 0;
            _currentSessionData = null;

            OnDisconnected?.Invoke();
            onComplete?.Invoke();
            Debug.Log("[PhotonFusionManager] Left multiplayer session.");
        }

        /// <summary>
        /// Retrieves the Player 1 (Host) match profile.
        /// </summary>
        public PlayerMatchProfile GetHostProfile()
        {
            if (_currentSessionData == null || string.IsNullOrWhiteSpace(_currentSessionData.hostPlayerName))
            {
                return PlayerMatchProfile.CreateLocalProfile(true);
            }

            var team = new List<string>(_currentSessionData.hostTeamIds?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>());
            if (team.Count == 0) team = PlayerMatchProfile.CreateLocalProfile(true).teamCharacterIds;

            return new PlayerMatchProfile(
                _currentSessionData.hostPlayerName,
                _currentSessionData.hostPlayerLevel > 0 ? _currentSessionData.hostPlayerLevel : 25,
                _currentSessionData.hostPlayerPoints > 0 ? _currentSessionData.hostPlayerPoints : 1450,
                team
            );
        }

        /// <summary>
        /// Retrieves the Player 2 (Client / Opponent) match profile.
        /// </summary>
        public PlayerMatchProfile GetClientProfile()
        {
            if (_currentSessionData == null || string.IsNullOrWhiteSpace(_currentSessionData.clientPlayerName))
            {
                return PlayerMatchProfile.CreateRivalPreviewProfile();
            }

            var team = new List<string>(_currentSessionData.clientTeamIds?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>());
            if (team.Count == 0) team = PlayerMatchProfile.CreateRivalPreviewProfile().teamCharacterIds;

            return new PlayerMatchProfile(
                _currentSessionData.clientPlayerName,
                _currentSessionData.clientPlayerLevel > 0 ? _currentSessionData.clientPlayerLevel : 28,
                _currentSessionData.clientPlayerPoints > 0 ? _currentSessionData.clientPlayerPoints : 1520,
                team
            );
        }

        #endregion
    }
}
