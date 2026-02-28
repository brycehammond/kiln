using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEditor;

namespace Kiln.MCP.Editor
{
    [InitializeOnLoad]
    public static class MainThreadDispatcher
    {
        private static readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        static MainThreadDispatcher()
        {
            EditorApplication.update += ProcessQueue;
        }

        private static void ProcessQueue()
        {
            while (_queue.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[Kiln] MainThread error: {ex}");
                }
            }
        }

        public static void Enqueue(Action action)
        {
            _queue.Enqueue(action);
        }

        public static Task<T> RunOnMainThread<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            Enqueue(() =>
            {
                try
                {
                    tcs.SetResult(func());
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }

        public static Task RunOnMainThread(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();
            Enqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }
    }
}
