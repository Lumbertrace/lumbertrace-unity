using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Lumbertrace.Unity
{
    public class LumbertraceInitializer : MonoBehaviour
    {
        [SerializeField] private string endpoint = "https://api.lumbertrace.co.uk";
        [SerializeField] private string wsEndpoint = "wss://api.lumbertrace.co.uk";
        [SerializeField] private string apiKey = "your-api-key-here";
        [SerializeField] private string projectId = "your-project-id-here";

        private CancellationTokenSource _cts;
        private LumbertraceAPI _apiInstance;
        private LumbertraceAPI _api;

        private async void Awake()
        {
            Debug.Log("[Lumbertrace] Attempting to create a new session...");
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