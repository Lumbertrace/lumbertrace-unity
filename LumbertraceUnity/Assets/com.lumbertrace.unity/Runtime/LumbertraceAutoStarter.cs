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

        private void Awake()
        {
            Debug.Log("[Lumbertrace] Auto-starting log session...");
            _cts = new CancellationTokenSource();

            ILumbertraceConfig config = new LumberTraceConfig(projectId, apiKey, endpoint: endpoint, wsEndpoint: wsEndpoint);
            var clientDetails = LumbertraceClientDetails.CreateWithAutoFill();
            
            Task.Run(async () =>
            {
                try
                {
                    await LumbertraceAPI.TryStartLogSessionAsync(config, clientDetails, _cts.Token);
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
            });
        }

        private void OnDestroy()
        {
            Debug.Log("[Lumbertrace] Shutting down log session...");
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}