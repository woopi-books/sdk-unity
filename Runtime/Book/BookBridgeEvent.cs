using System;

namespace Woopi.Core.Book
{
    /// <summary>
    /// One event sent to the host page (serialized with JsonUtility, so public fields).
    /// </summary>
    [Serializable]
    public struct BookBridgeEvent
    {
        /// <summary>
        /// "Ready", "NextPage", "PreviousPage", "GoToPage", "GoToPageIndex", "SetLanguage",
        /// "RestartPage", "RestartBook", "SetMuted", "SetVolume" (the command that was received),
        /// "PageShown" (a page became current), "LanguageChanged" (the language actually changed)
        /// or "AudioChanged" (mute or volume actually changed). A custom command echoes its own
        /// name, and a book-defined event (WoopiBookHost.Notify) uses the name the book chose.
        /// </summary>
        public string action;

        /// <summary>Id the host attached to the command this event answers, or empty.</summary>
        public string requestId;

        public bool success;

        /// <summary>Why a command was rejected: no-next-page, no-previous-page, out-of-range,
        /// already-there, already-set, unsupported-language, unsupported-action, loading, not-ready,
        /// bad-request (a custom handler may return its own reason).</summary>
        public string reason;

        /// <summary>Current page, 0-based (kept compatible with InteractiveBooks).</summary>
        public int currentPageIndex;

        /// <summary>Current page, 1-based.</summary>
        public int currentPage;

        public int pageCount;

        /// <summary>Language the book is showing when the event was raised.</summary>
        public string language;

        /// <summary>Whether the book's sound is muted when the event was raised.</summary>
        public bool muted;

        /// <summary>Master volume (0 to 1) when the event was raised.</summary>
        public float volume;

        /// <summary>Free-form text sent with a book-defined event (see WoopiBookHost.Notify), else empty.</summary>
        public string data;

        // Only filled in the "Ready" event: what the host can rely on.

        /// <summary>Message contract version (WoopiSdk.ProtocolVersion). 0 on events other than Ready.</summary>
        public int protocolVersion;

        /// <summary>Package version (WoopiSdk.Version). Empty on events other than Ready.</summary>
        public string sdkVersion;

        /// <summary>Command names this book accepts, built-in and registered. Only on Ready.</summary>
        public string[] commands;

        /// <summary>Feature tags: navigation, language, audio, restart plus any the adapter adds. Only on Ready.</summary>
        public string[] capabilities;
    }
}
