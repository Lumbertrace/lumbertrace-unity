using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Lumbertrace.Unity
{
    public class LumbertraceAutoStarter : MonoBehaviour
    {
        [SerializeField] private string endpoint = "https://your-lumbertrace-endpoint.com";
        [SerializeField] private string wsEndpoint = "ws://your-lumbertrace-endpoint.com";
        [SerializeField] private string apiKey = "your-api-key-here";
        [SerializeField] private string projectId = "your-project-id-here";

        private CancellationTokenSource _cts;
        private LumbertraceAPI _apiInstance;
        private LumbertraceAPI _api;

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
                api = await LumbertraceAPI.TryStartLogSessionAsync(config, clientDetails, token);

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
            }
            
            return api;
        }


        private void Update()
        {
            Debug.Log("[Lumbertrace] Auto-starting log session message.");
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