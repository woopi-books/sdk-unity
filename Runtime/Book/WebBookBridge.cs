using System.Runtime.InteropServices;
using UnityEngine;

namespace Woopi.Core.Book
{
    /// <summary>Unity to host page. Outside a WebGL build the event is just logged.</summary>
    public static class WebBookBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void WoopiBook_Emit(string json);
#endif

        public static void Emit(BookBridgeEvent evt)
        {
            var json = JsonUtility.ToJson(evt);
#if UNITY_WEBGL && !UNITY_EDITOR
            WoopiBook_Emit(json);
#else
            Debug.Log($"[WebBookBridge] {json}");
#endif
        }
    }
}
