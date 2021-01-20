using System;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.QuickSearch
{
    static class Dispatcher
    {
        private static readonly Queue<Action> s_ExecutionQueue = new Queue<Action>();

        public static void Enqueue(Action action)
        {
            lock (s_ExecutionQueue)
            {
                s_ExecutionQueue.Enqueue(action);
                EditorApplication.update -= Update;
                EditorApplication.update += Update;
            }
        }

        static void Update()
        {
            lock (s_ExecutionQueue)
            {
                while (s_ExecutionQueue.Count > 0)
                    s_ExecutionQueue.Dequeue().Invoke();

                if (s_ExecutionQueue.Count == 0)
                    EditorApplication.update -= Update;
            }
        }
    }
}
