using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Lumbertrace.Unity
{
    
    public static class Authentication
    {
        [Serializable]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public class AuthRequest
        {
            [SerializeField] private string projectId;
            [SerializeField] private string apiKey;
            [SerializeField] private string device;
            [SerializeField] private string platform;
            [SerializeField] private string version;

            public AuthRequest(string projectId, string apiKey, string device, string platform, string version)
            {
                this.projectId = projectId;
                this.apiKey = apiKey;
                this.device = device;
                this.platform = platform;
                this.version = version;
            }

            public static AuthRequest CreateFromClientDetails(string projectId, string apiKey, LumbertraceClientDetails clientDetails)
            {
                string device = clientDetails.Device;
                string version = clientDetails.Version;
                string platform = clientDetails.Platform;
                
                AuthRequest request = new AuthRequest(projectId, apiKey, device, platform, version);
                return request;
            }
        }
        
        public class AuthenticateResponse
        {
            public string SessionAuthToken { get;}
            
            public AuthenticateResponse(string sessionAuthToken)
            {
                SessionAuthToken = sessionAuthToken;
            }
        }
        
        

        internal static async Task<(bool success, AuthenticateResponse response)> AuthenticateAsync(
            string endpoint,
            string projectId,
            string apiKey,
            LumbertraceClientDetails clientDetails,
            CancellationToken ct = default)
        {
            string url = Path.Combine(endpoint, "api", "auth", "session");
            AuthRequest authRequest = AuthRequest.CreateFromClientDetails(projectId, apiKey, clientDetails);
            string json = JsonUtility.ToJson(authRequest);


            using var request = await UnityMainThread.Run(() =>
            {
                Debug.Log(json);
                
                var r = new UnityWebRequest(url, "POST");
                byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
                r.uploadHandler = new UploadHandlerRaw(jsonBytes);
                r.downloadHandler = new DownloadHandlerBuffer();
                r.SetRequestHeader("Content-Type", "application/json");
                return r;
            });

            try
            {
               Task<UnityWebRequest.Result> resultTask = await UnityMainThread.Run(async () =>
                {
                    await request.SendWebRequest();
                    return request.result;
                });
                
               var result = await resultTask;
               
                ct.ThrowIfCancellationRequested();
                

                if (result == UnityWebRequest.Result.Success)
                {
                    AuthenticateResponse response = await UnityMainThread.Run(() =>
                    {
                        string responseText = request.downloadHandler.text;
                        string token = TrimQuotes(responseText);

                        AuthenticateResponse authResponse = new AuthenticateResponse(token);
                        return authResponse;
                    });

                    return (true, response);
                }

                await UnityMainThread.Run(() =>
                {
                    Debug.LogError($"Lumbertrace.AuthenticateAsync failed: {request.responseCode} {request.error}");
                    return Task.CompletedTask;
                });
                return (false, null);
            }
            catch (OperationCanceledException)
            {
                return (false, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lumbertrace.AuthenticateAsync exception: {ex}");
                return (false, null);
            }
        }
        
        private static string TrimQuotes(string input)
        {
            if (input.StartsWith("\"") && input.EndsWith("\"") && input.Length >= 2)
                return input.Substring(1, input.Length - 2);
            return input;
        }
    }
}