using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace Lumbertrace.Unity
{
    public static class UnityWebRequestAwaitableExtensions
    {
        public static async Awaitable<bool> SendWebRequestAwaitable(this UnityWebRequest request, CancellationToken cancellationToken = default)
        {
            var operation = request.SendWebRequest();

            while (operation.isDone == false && cancellationToken.IsCancellationRequested == false)
            {
                await Awaitable.NextFrameAsync(cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested == true)
            {
                request.Abort();
                return false;
            }
            
            return (operation.webRequest.result == UnityWebRequest.Result.Success);
        }
    }
}