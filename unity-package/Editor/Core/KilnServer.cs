using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Kiln.MCP.Editor
{
    [InitializeOnLoad]
    public class KilnServer
    {
        private static KilnServer _instance;
        private HttpListener _httpListener;
        private CancellationTokenSource _cts;
        private readonly MessageRouter _router;
        private WebSocket _activeClient;
        private const int Port = 8091;
        private const int MaxMessageSize = 1024 * 1024; // 1MB

        public static KilnServer Instance => _instance;
        public bool IsRunning { get; private set; }
        public bool HasClient => _activeClient?.State == WebSocketState.Open;

        static KilnServer()
        {
            _instance = new KilnServer();
            _instance.Start();

            EditorApplication.quitting += () => _instance?.Stop();
            AssemblyReloadEvents.beforeAssemblyReload += () => _instance?.Stop();
            AssemblyReloadEvents.afterAssemblyReload += () =>
            {
                _instance = new KilnServer();
                _instance.Start();
            };
        }

        private KilnServer()
        {
            _router = new MessageRouter();
            RegisterTools();
        }

        private void RegisterTools()
        {
            _router.RegisterTool(new CreateGameObjectTool());
            _router.RegisterTool(new DescribeSceneTool());
            _router.RegisterTool(new ExplainErrorTool());
            _router.RegisterTool(new CreateScriptTool());
            _router.RegisterTool(new ReadScriptTool());
            _router.RegisterTool(new GetProjectSummaryTool());
            _router.RegisterTool(new SaveTool());
            _router.RegisterTool(new ListSavesTool());
            _router.RegisterTool(new LoadSaveTool());
            _router.RegisterTool(new ImportAssetTool());
            _router.RegisterTool(new ScreenshotTool());
            _router.RegisterTool(new EnterPlayModeTool());
            _router.RegisterTool(new ExitPlayModeTool());
            _router.RegisterTool(new EditScriptTool());
            _router.RegisterTool(new FocusGameObjectTool());
            _router.RegisterTool(new SetTransformTool());
            _router.RegisterTool(new ExplainSceneTool());
            _router.RegisterTool(new BuildProjectTool());
        }

        public void Start()
        {
            if (IsRunning) return;

            try
            {
                _cts = new CancellationTokenSource();
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://localhost:{Port}/");
                _httpListener.Start();
                IsRunning = true;

                Debug.Log($"[Kiln] WebSocket server started on port {Port}");
                _ = AcceptConnectionsAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Kiln] Failed to start server: {ex.Message}");
                IsRunning = false;
            }
        }

        public void Stop()
        {
            if (!IsRunning) return;

            try
            {
                _cts?.Cancel();

                if (_activeClient?.State == WebSocketState.Open)
                {
                    _activeClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None).Wait(1000);
                }

                _httpListener?.Stop();
                _httpListener?.Close();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Kiln] Error during shutdown: {ex.Message}");
            }
            finally
            {
                IsRunning = false;
                _activeClient = null;
                Debug.Log("[Kiln] Server stopped");
            }
        }

        private async Task AcceptConnectionsAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();

                    if (!context.Request.IsWebSocketRequest)
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                        continue;
                    }

                    var wsContext = await context.AcceptWebSocketAsync(null);
                    _activeClient = wsContext.WebSocket;
                    Debug.Log("[Kiln] MCP client connected");

                    await HandleClientAsync(wsContext.WebSocket, ct);
                }
                catch (ObjectDisposedException) { break; }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    if (!ct.IsCancellationRequested)
                        Debug.LogError($"[Kiln] Connection error: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(WebSocket ws, CancellationToken ct)
        {
            var buffer = new byte[MaxMessageSize];

            try
            {
                while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var result = await ReceiveFullMessage(ws, buffer, ct);
                    if (result == null) break;

                    var message = Encoding.UTF8.GetString(result.Value.buffer, 0, result.Value.count);
                    var response = await _router.RouteMessage(message);

                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    await ws.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, ct);
                }
            }
            catch (WebSocketException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[Kiln] Client handler error: {ex.Message}");
            }
            finally
            {
                _activeClient = null;
                Debug.Log("[Kiln] MCP client disconnected");
            }
        }

        private async Task<(byte[] buffer, int count)?> ReceiveFullMessage(WebSocket ws, byte[] buffer, CancellationToken ct)
        {
            var totalBytes = 0;
            WebSocketReceiveResult result;

            do
            {
                var segment = new ArraySegment<byte>(buffer, totalBytes, buffer.Length - totalBytes);
                result = await ws.ReceiveAsync(segment, ct);

                if (result.MessageType == WebSocketMessageType.Close)
                    return null;

                totalBytes += result.Count;
            } while (!result.EndOfMessage);

            return (buffer, totalBytes);
        }
    }
}
