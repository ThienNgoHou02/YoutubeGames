using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Utilities
{
    public static class UnityEventExtension
    {
        public static void AddListenerOnce(this UnityEvent unityEvent, UnityAction call)
        {
            unityEvent.RemoveListener(call);
            unityEvent.AddListener(call);
        }
        public static void AddListenerOnce<T>(this UnityEvent<T> unityEvent, UnityAction<T> call)
        {
            unityEvent.RemoveListener(call);
            unityEvent.AddListener(call);
        }
        public static void AddListenerOnce<T1, T2>(this UnityEvent<T1, T2> unityEvent, UnityAction<T1, T2> call)
        {
            unityEvent.RemoveListener(call);
            unityEvent.AddListener(call);
        }
        public static void AddListenerOnce<T1, T2, T3>(this UnityEvent<T1, T2, T3> unityEvent, UnityAction<T1, T2, T3> call)
        {
            unityEvent.RemoveListener(call);
            unityEvent.AddListener(call);
        }
    }
}