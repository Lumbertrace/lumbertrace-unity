using System;
using UnityEngine;

namespace Lumbertrace.Unity
{
    public interface ILumbertraceConfig
    {
        public const string DefaultEndpoint = "http://localhost:5214"; 
        public const string DefaultWsEndpoint = "ws://localhost:5214"; 
        
        public string Endpoint { get; }
        public string WsEndpoint { get; }
        public string ApiKey { get; }
        public string ProjectId { get; }
    }
    
    [Serializable]
    public class LumberTraceConfig : ILumbertraceConfig
    {
        [SerializeField] private string _apiKey;
        [SerializeField] private string _projectId;
        [SerializeField] private string _endpoint;
        [SerializeField] private string _wsEndpoint;
        public string ApiKey => _apiKey;
        public string ProjectId => _projectId;
        public string Endpoint => _endpoint;
        public string WsEndpoint => _wsEndpoint;
        
        public LumberTraceConfig(
            string projectId, 
            string apiKey, 
            string endpoint = ILumbertraceConfig.DefaultEndpoint,
            string wsEndpoint = ILumbertraceConfig.DefaultWsEndpoint
            )
        {
            _apiKey = apiKey;
            _projectId = projectId;
            _endpoint = endpoint;
            _wsEndpoint = wsEndpoint;
        }
    }
    
    [CreateAssetMenu(menuName = "Lumbertrace/Configuration", fileName = "LumbertraceConfig", order = 0)]
    public class LumbertraceConfigScriptableObject : ScriptableObject, ILumbertraceConfig
    {
        [SerializeField] private string _apiKey;
        [SerializeField] private string _projectId;
        [SerializeField] private string _endpoint;
        [SerializeField] private string _wsEndpoint;
        
        public string WsEndpoint => _wsEndpoint;
        public string ApiKey => _apiKey;
        public string ProjectId => _projectId;
        public string Endpoint => _endpoint;
    }
}