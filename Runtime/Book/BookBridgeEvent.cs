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
        /// "RestartPage", "RestartBook" (the command that was received), "PageShown" (a page
        /// became current) or "LanguageChanged" (the language actually changed).
        /// </summary>
        public string action;

        /// <summary>Id the host attached to the command this event answers, or empty.</summary>
        public string requestId;

        public bool success;

        /// <summary>Why a command was rejected: no-next-page, no-previous-page, out-of-range,
        /// already-there, already-set, unsupported-language, loading, not-ready, bad-request.</summary>
        public string reason;

        /// <summary>Current page, 0-based (kept compatible with InteractiveBooks).</summary>
        public int currentPageIndex;

        /// <summary>Current page, 1-based.</summary>
        public int currentPage;

        public int pageCount;

        /// <summary>Language the book is showing when the event was raised.</summary>
        public string language;
    }
}
