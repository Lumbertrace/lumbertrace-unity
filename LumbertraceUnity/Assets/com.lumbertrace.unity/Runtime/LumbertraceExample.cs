using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Lumbertrace.Unity
{
    public class LumbertraceExample : MonoBehaviour
    {
        [SerializeField] private string endpoint = "https://api.lumbertrace.co.uk";
        [SerializeField] private string wsEndpoint = "wss://api.lumbertrace.co.uk";
        [SerializeField] private string apiKey = "your-api-key-here";
        [SerializeField] private string projectId = "your-project-id-here";

        private CancellationTokenSource _cts;
        private LumbertraceAPI _apiInstance;
        private LumbertraceAPI _api;
         private float _logTimer;

        private async void Awake()
        {
            Debug.Log("[Lumbertrace] Auto-starting log session...");
            _cts = new CancellationTokenSource();

            ILumbertraceConfig config = new LumberTraceConfig(projectId, apiKey, endpoint: endpoint, wsEndpoint: wsEndpoint);
            var clientDetails = LumbertraceClientDetails.CreateWithAutoFill();
            
            _api = await StartSession(config, clientDetails, _cts.Token);
        }

        private static async Task<LumbertraceAPI> StartSession(ILumbertraceConfig config,
            LumbertraceClientDetails clientDetails, CancellationToken token = default)
        {
            LumbertraceAPI api = null;
            
            try
            {
                api = await LumbertraceAPI.TryStartSessionAsync(config, clientDetails, token);

                if (token.IsCancellationRequested == true)
                {
                    api?.Dispose();
                    return null;
                }
                
                Debug.Log("[Lumbertrace] Log session started successfully.");
            }
            catch (TaskCanceledException)
            {
                Debug.Log("[Lumbertrace] Log session start was cancelled.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Lumbertrace] Failed to start log session: {ex}");
                throw;
            }
            
            return api;
        }


        private void Update()
        {
            if (_api == null) return;

            // Emit logs every 1 second
            _logTimer += Time.deltaTime;
            if (_logTimer >= 0.2f)
            {
                EmitRandomLog();
                _logTimer = 0f;
            }
        }

        private void EmitRandomLog()
        {
            int logType = UnityEngine.Random.Range(0, 3);

            switch (logType)
            {
                case 0:
                    Debug.Log($"[Lumbertrace] Info log at {DateTime.Now:HH:mm:ss.fff}");
                    break;
                case 1:
                    Debug.LogWarning($"[Lumbertrace] Warning: Something might be wrong at {DateTime.Now:HH:mm:ss.fff}");
                    break;
                case 2:
                    Debug.LogError($"[Lumbertrace] Error: Something failed at {DateTime.Now:HH:mm:ss.fff}");
                    break;
            }
        }        
        
        private void OnDestroy()
        {
            Debug.Log("[Lumbertrace] Shutting down log session...");
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _api?.Dispose();
        }
    }
}