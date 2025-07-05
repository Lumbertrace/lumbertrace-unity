using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

namespace Lumbertrace.Unity
{
    public class LumbertraceAPI : IDisposable
    {
        private static readonly TimeSpan TimeBetweenSend = TimeSpan.FromSeconds(0.25);
        private static readonly TimeSpan TimeBetweenReconnectAttempts = TimeSpan.FromSeconds(2.0);
        
        [Serializable]
        private class LogEntry
        {
            // ReSharper disable once InconsistentNaming
            [SerializeField]
            private string message;
            // ReSharper disable once InconsistentNaming
            [SerializeField]
            private string stacktrace;
            // ReSharper disable once InconsistentNaming
            [SerializeField]
            private string logType;
            // ReSharper disable once InconsistentNaming
            [SerializeField]
            private long time;

            public LogEntry(string message, string stacktrace, LogType type, DateTime time)
            {
                this.message = message;
                this.stacktrace = stacktrace;
                this.logType = type.ToString();
                this.time = time.Ticks;
            }
        }
        
        private CancellationTokenSource _cts = new CancellationTokenSource();
        
        private ClientWebSocket _webSocket;
        private Task _sendLoopTask;
        private readonly ConcurrentQueue<LogEntry> _logQueue = new ConcurrentQueue<LogEntry>();

        private LumbertraceAPI()
        {
            UnityMainThread.Run(() =>
            {
                Application.logMessageReceived += HandleLog;
                return Task.CompletedTask;
            });
        }
        
        
        public static async System.Threading.Tasks.Task<LumbertraceAPI> TryStartLogSessionAsync(ILumbertraceConfig config,
            LumbertraceClientDetails clientDetails, CancellationToken ct = default)
        {
            var api = new LumbertraceAPI();

            try
            {
                (bool authenticated, Authentication.AuthenticateResponse authResponse) = await Authentication.AuthenticateAsync(config.Endpoint, config.ProjectId, config.ApiKey, clientDetails, ct);

                ct.ThrowIfCancellationRequested();

                if (authenticated == false)
                {
                    Debug.LogError("Failed to authenticate with Lumbertrace API.");
                    return null;
                }

                string token = authResponse.SessionAuthToken;
                string baseUrl = Path.Combine(config.WsEndpoint, "api", "ws", "logs");
                string fullUrl = $"{baseUrl}?token={Uri.EscapeDataString(token)}";
                Uri wsUri = new Uri(fullUrl);
                await api.ConnectToSessionAsync(wsUri, ct);
                ct.ThrowIfCancellationRequested();
                
                api.StartSendLogTask(wsUri, ct);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"{nameof(LumbertraceAPI)} was cancelled while attempting to connect to the Lumbertrace API.");
                api.Dispose();
                throw;
            }
            catch
            {
                api.Dispose();     
                throw;
            }

            return api;
        }

        private void StartSendLogTask(Uri wsUri, CancellationToken ct)
        {
            Debug.Assert(_sendLoopTask == null, "SendLogTask already in progress");
            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
             _sendLoopTask = Task.Run(() => SendLoopAsync(wsUri, cts.Token), cts.Token);
        }

        private async Task ConnectToSessionAsync(Uri wsUri, CancellationToken ct)
        {
            _webSocket = new ClientWebSocket();

            try
            {
                Debug.Log($"Connecting to Lumbertrace WebSocket at {wsUri}");
                await _webSocket.ConnectAsync(wsUri, ct).ConfigureAwait(false);
                Debug.Log("Connected to Lumbertrace WebSocket session.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to connect to Lumbertrace WebSocket: {ex.Message}");
                throw;
            }
        }

        
        private async Task SendLoopAsync(Uri wsUri, CancellationToken ct)
        {
            try
            {
                while (ct.IsCancellationRequested == false && _webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    while (_logQueue.TryDequeue(out var log))
                    {
                        string logAsJson = JsonUtility.ToJson(log);
                        var bytes = Encoding.UTF8.GetBytes(logAsJson);
                        var segment = new ArraySegment<byte>(bytes);

                        try
                        {
                            await _webSocket.SendAsync(segment, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
                        }
                        catch (WebSocketException ex)
                        {
                            Debug.LogWarning($"WebSocket send failed, will attempt reconnect: {ex.Message}");
                            _logQueue.Enqueue(log);
                            await AttemptReconnectAsync(wsUri, ct);
                            break;
                        }
                    }

                    await Task.Delay(TimeBetweenSend, ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lumbertrace SendLoop exception: {ex}");
            }
        }
        
        
        private async Task AttemptReconnectAsync(Uri wsUri, CancellationToken ct)
        {
            Debug.Log("Attempting Lumbertrace WebSocket reconnect...");


            while (ct.IsCancellationRequested == false)
            {
                try
                {
                    DisconnectWebSocket();
                    _webSocket = new ClientWebSocket();
                    await _webSocket.ConnectAsync(wsUri, ct).ConfigureAwait(false);

                    if (_webSocket.State == WebSocketState.Open)
                    {
                        Debug.Log("Reconnected to Lumbertrace WebSocket.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Reconnect attempt failed: {ex.Message}");
                }

                await Task.Delay(TimeBetweenReconnectAttempts, ct);
            }
        }

        private void DisconnectWebSocket()
        {
            if (_webSocket == null)
            {
                return;
            }
            
            try
            {
                _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait(1000);
            }
            catch { /* Ignore */ }
            finally
            {
                _webSocket.Dispose();
                _webSocket = null;
            }
        }

        
        private void HandleLog(string message, string stacktrace, LogType type)
        {
            LogEntry entry = new(message, stacktrace, type, DateTime.UtcNow);
            _logQueue.Enqueue(entry);
        }

        private void ShutdownApi()
        {
            DisconnectWebSocket();
        }

        public void Dispose()
        {
            UnityMainThread.Run(() =>
            {
                Application.logMessageReceived -= HandleLog;
                return Task.CompletedTask;
            });
            
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            
            ShutdownApi();
        }
    }
}
