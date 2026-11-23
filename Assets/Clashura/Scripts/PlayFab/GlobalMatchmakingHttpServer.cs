using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Clashura.PlayFab
{
    /// <summary>
    /// Lightweight, asynchronous, multi-threaded HTTP REST Server for Global Real-Time Matchmaking.
    /// Exposes endpoints for any device across the network/LAN/internet to enqueue, poll, cancel,
    /// and submit authoritative match results.
    /// </summary>
    public class GlobalMatchmakingHttpServer : MonoBehaviour
    {
        private static GlobalMatchmakingHttpServer _instance;
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

        public static GlobalMatchmakingHttpServer Instance
        {
            get
            {
                if (_isShuttingDown) return null;
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GlobalMatchmakingHttpServer>();
                    if (_instance == null && Application.isPlaying && !_isShuttingDown)
                    {
                        GameObject go = new GameObject("[GlobalMatchmakingHttpServer]");
                        _instance = go.AddComponent<GlobalMatchmakingHttpServer>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Configuration")]
        [SerializeField] private int port = 7865;
        [SerializeField] private bool isRunning = false;

        private HttpListener _listener;
        private Thread _listenerThread;
        private volatile bool _stopRequested = false;

        public bool IsRunning => isRunning;
        public int Port => port;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            StartServer();
        }

        public void StartServer()
        {
            if (isRunning) return;

            try
            {
                _listener = new HttpListener();
                // Attempt binding to all interfaces and localhost
                _listener.Prefixes.Add($"http://localhost:{port}/");
                _listener.Prefixes.Add($"http://127.0.0.1:{port}/");

                _stopRequested = false;
                _listener.Start();
                isRunning = true;

                _listenerThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "Clashura_Matchmaking_HttpServer"
                };
                _listenerThread.Start();

                Debug.Log($"[GlobalMatchmakingHttpServer] Server started on port {port}. Ready for cross-device matchmaking.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GlobalMatchmakingHttpServer] Could not bind port {port}: {ex.Message}. (May already be running in another process, which is expected for multi-client testing).");
            }
        }

        private void ListenLoop()
        {
            while (!_stopRequested && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => ProcessRequest(context));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!_stopRequested)
                    {
                        Debug.LogWarning($"[GlobalMatchmakingHttpServer] Listener error: {ex.Message}");
                    }
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            // CORS headers for cross-device and web requests
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, X-Authentication");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            try
            {
                string rawPath = request.Url.AbsolutePath.ToLowerInvariant();
                string query = request.Url.Query;

                if (request.HttpMethod == "POST" && rawPath == "/api/matchmaking/enqueue")
                {
                    string body = ReadBody(request);
                    var reqTicket = JsonUtility.FromJson<MatchmakingTicketDto>(body);
                    var ticket = GlobalMatchmakingService.Enqueue(reqTicket.playerId, reqTicket.displayName, reqTicket.rating);
                    WriteJsonResponse(response, 200, JsonUtility.ToJson(ticket));
                }
                else if (request.HttpMethod == "GET" && rawPath == "/api/matchmaking/poll")
                {
                    string ticketId = GetQueryParam(query, "ticketId");
                    var ticket = GlobalMatchmakingService.Poll(ticketId);
                    if (ticket != null)
                    {
                        WriteJsonResponse(response, 200, JsonUtility.ToJson(ticket));
                    }
                    else
                    {
                        WriteJsonResponse(response, 404, "{\"error\":\"Ticket not found\"}");
                    }
                }
                else if (request.HttpMethod == "POST" && rawPath == "/api/matchmaking/cancel")
                {
                    string ticketId = GetQueryParam(query, "ticketId");
                    if (string.IsNullOrEmpty(ticketId))
                    {
                        string body = ReadBody(request);
                        var dto = JsonUtility.FromJson<MatchmakingTicketDto>(body);
                        ticketId = dto?.ticketId;
                    }

                    bool canceled = GlobalMatchmakingService.Cancel(ticketId);
                    WriteJsonResponse(response, 200, $"{{\"canceled\":{canceled.ToString().ToLower()}}}");
                }
                else if (request.HttpMethod == "GET" && rawPath == "/api/matchmaking/queue_status")
                {
                    var status = GlobalMatchmakingService.GetQueueStatus();
                    WriteJsonResponse(response, 200, JsonUtility.ToJson(status));
                }
                else if (request.HttpMethod == "POST" && rawPath == "/api/match/complete")
                {
                    string body = ReadBody(request);
                    var report = JsonUtility.FromJson<MatchResultReportDto>(body);
                    var result = GlobalMatchmakingService.CompleteMatch(report);
                    WriteJsonResponse(response, 200, JsonUtility.ToJson(result));
                }
                else if (request.HttpMethod == "GET" && rawPath == "/api/player/profile")
                {
                    string playFabId = GetQueryParam(query, "playFabId");
                    var profile = GlobalMatchmakingService.GetOrCreateProfile(playFabId, null);
                    WriteJsonResponse(response, 200, JsonUtility.ToJson(profile));
                }
                else
                {
                    WriteJsonResponse(response, 404, "{\"error\":\"Endpoint not found\"}");
                }
            }
            catch (Exception ex)
            {
                WriteJsonResponse(response, 500, $"{{\"error\":\"{ex.Message}\"}}");
            }
        }

        private string ReadBody(HttpListenerRequest request)
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                return reader.ReadToEnd();
            }
        }

        private void WriteJsonResponse(HttpListenerResponse response, int statusCode, string json)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = bytes.Length;
            using (var output = response.OutputStream)
            {
                output.Write(bytes, 0, bytes.Length);
            }
        }

        private string GetQueryParam(string query, string paramName)
        {
            if (string.IsNullOrEmpty(query)) return string.Empty;
            if (query.StartsWith("?")) query = query.Substring(1);

            var pairs = query.Split('&');
            foreach (var pair in pairs)
            {
                var kv = pair.Split('=');
                if (kv.Length == 2 && string.Equals(kv[0], paramName, StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(kv[1]);
                }
            }
            return string.Empty;
        }

        private void OnDestroy()
        {
            StopServer();
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            _isShuttingDown = true;
            StopServer();
        }

        public void StopServer()
        {
            _stopRequested = true;
            if (_listener != null)
            {
                try
                {
                    _listener.Stop();
                    _listener.Close();
                }
                catch { }
                _listener = null;
            }
            isRunning = false;
        }
    }
}
