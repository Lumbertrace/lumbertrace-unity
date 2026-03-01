using System;
using UnityEngine;

namespace Lumbertrace.Unity
{
    public class LumbertraceLoggingSpammer : MonoBehaviour
    {
        [SerializeField] private float _timeBetweenLogsInSeconds = 0.2f;

         private float _logTimer;

        private void Update()
        {
            _logTimer += Time.deltaTime;
            if (_logTimer >= _timeBetweenLogsInSeconds)
            {
                EmitRandomLog();
                _logTimer = 0f;
            }
        }

        private void EmitRandomLog()
        {
            int logType = UnityEngine.Random.Range(0, 8);

            switch (logType)
            {
                case 1:
                    Debug.LogWarning($"[Lumbertrace] Warning: Something might be wrong at {DateTime.Now:HH:mm:ss.fff}");
                    break;
                case 2:
                    Debug.LogError($"[Lumbertrace] Error: Something failed at {DateTime.Now:HH:mm:ss.fff}");
                    break;
                default:
                    Debug.Log($"[Lumbertrace] Info log at {DateTime.Now:HH:mm:ss.fff}");
                    break;
            }
        }        
    }
}