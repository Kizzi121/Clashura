using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Clashura.Elo
{
    /// <summary>
    /// Lightweight Authoritative HTTP REST Server using System.Net.HttpListener.
    /// Provides real online REST endpoints for matchmaking, profile storage, and match validation.
    /// Runs as an embedded background service inside Unity Master Client or Standalone processes.
    /// </summary>
    public class EloHttpServer : MonoBehaviour
    {
        private static EloHttpServer _instance;
        public static EloHttpServer Instance => _instance;

        [Header("Server Configuration")]
        [SerializeField] private int serverPort = 8088;
        [SerializeField] private bool autoStartOnPlay = true;

        private HttpListener _listener;
        private Thread _listenerThread;
        private bool _isRunning = false;
        private string _serverUrl;

        public bool IsRunning => _isRunning;
        public string ServerUrl => _serverUrl;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (autoStartOnPlay)
            {
                StartServer();
            }
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
            StopServer();
        }

        public void StartServer(int port = 8088)
        {
            if (_isRunning) return;

            int[] portsToTry = { port, 8089, 8090, 8091, 8092 };

            foreach (var testPort in portsToTry)
            {
                try
                {
                    _listener = new HttpListener();
                    _serverUrl = $"http://127.0.0.1:{testPort}/";
                    _listener.Prefixes.Add(_serverUrl);
                    _listener.Start();

                    _isRunning = true;
                    serverPort = testPort;

                    _listenerThread = new Thread(ListenLoop)
                    {
                        IsBackground = true,
                        Name = "EloHttpServer_Thread"
                    };
                    _listenerThread.Start();

                    Debug.Log($"[EloHttpServer] Authoritative REST Server STARTED on {_serverUrl}");
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[EloHttpServer] Port {testPort} unavailable ({ex.Message}), trying next...");
                    try { _listener?.Close(); } catch { }
                    _listener = null;
                }
            }

            Debug.LogError("[EloHttpServer] Could not bind HTTP Server to any test port. Using direct backend fallback.");
        }

        public void StopServer()
        {
            _isRunning = false;
            try
            {
                if (_listener != null && _listener.IsListening)
                {
                    _listener.Stop();
                    _listener.Close();
                }
            }
            catch { }
            _listener = null;

            try
            {
                if (_listenerThread != null && _listenerThread.IsAlive)
                {
                    _listenerThread.Abort();
                }
            }
            catch { }
            _listenerThread = null;

            Debug.Log("[EloHttpServer] REST Server stopped.");
        }

        private void ListenLoop()
        {
            while (_isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(ProcessRequest, context);
                }
                catch (HttpListenerException)
                {
                    // Listener stopped or aborted
                    break;
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Debug.LogWarning($"[EloHttpServer] Error accepting HTTP connection: {ex.Message}");
                    }
                }
            }
        }

        private void ProcessRequest(object state)
        {
            var context = (HttpListenerContext)state;
            var request = context.Request;
            var response = context.Response;

            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            string rawUrl = request.RawUrl ?? string.Empty;
            string path = request.Url.AbsolutePath.ToLowerInvariant();

            try
            {
                string responseJson = string.Empty;
                int statusCode = 200;

                // Route: GET /api/player?id=...
                if (path == "/api/player" && request.HttpMethod == "GET")
                {
                    string id = request.QueryString["id"];
                    var player = EloDatabase.GetPlayer(id);
                    responseJson = player != null ? JsonUtility.ToJson(player) : "{}";
                }
                // Route: POST /api/player/register
                else if (path == "/api/player/register" && request.HttpMethod == "POST")
                {
                    string body = ReadBody(request);
                    var payload = JsonUtility.FromJson<PlayerEloProfile>(body);
                    var player = EloServerService.GetOrRegisterPlayer(payload.playerId, payload.playerName, payload.rating);
                    responseJson = JsonUtility.ToJson(player);
                }
                // Route: POST /api/matchmaking/enqueue
                else if (path == "/api/matchmaking/enqueue" && request.HttpMethod == "POST")
                {
                    string body = ReadBody(request);
                    var payload = JsonUtility.FromJson<MatchmakingTicket>(body);
                    var ticket = EloServerService.EnqueueMatchmaking(payload.playerId, payload.playerName);
                    responseJson = JsonUtility.ToJson(ticket);
                }
                // Route: GET /api/matchmaking/poll?ticketId=...
                else if (path == "/api/matchmaking/poll" && request.HttpMethod == "GET")
                {
                    string ticketId = request.QueryString["ticketId"];
                    var ticket = EloServerService.PollMatchmaking(ticketId);
                    responseJson = ticket != null ? JsonUtility.ToJson(ticket) : "{}";
                }
                // Route: POST /api/matchmaking/cancel
                else if (path == "/api/matchmaking/cancel" && request.HttpMethod == "POST")
                {
                    string body = ReadBody(request);
                    var payload = JsonUtility.FromJson<MatchmakingTicket>(body);
                    bool canceled = EloServerService.CancelMatchmaking(payload.ticketId);
                    responseJson = $"{{\"success\": {canceled.ToString().ToLower()}}}";
                }
                // Route: POST /api/match/complete
                else if (path == "/api/match/complete" && request.HttpMethod == "POST")
                {
                    string body = ReadBody(request);
                    var report = JsonUtility.FromJson<MatchResultReport>(body);
                    var result = EloServerService.ProcessMatchResult(report);
                    responseJson = JsonUtility.ToJson(result);
                }
                // Route: GET /api/leaderboard
                else if (path == "/api/leaderboard" && request.HttpMethod == "GET")
                {
                    var leaders = EloDatabase.GetLeaderboard(10);
                    var schema = new DatabaseSchema { players = leaders };
                    responseJson = JsonUtility.ToJson(schema);
                }
                else
                {
                    statusCode = 404;
                    responseJson = "{\"error\": \"Endpoint not found\"}";
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseJson);
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.StatusCode = statusCode;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                byte[] buffer = Encoding.UTF8.GetBytes($"{{\"error\": \"{ex.Message}\"}}");
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            finally
            {
                try { response.OutputStream.Close(); } catch { }
            }
        }

        private static string ReadBody(HttpListenerRequest request)
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
