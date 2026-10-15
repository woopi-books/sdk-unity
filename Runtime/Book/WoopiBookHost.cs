using System;
using System.Collections;
using UnityEngine;

namespace Woopi.Core.Book
{
    /// <summary>
    /// The book's door to the host page. Drop it on a GameObject in the persistent scene, point it
    /// at the book's <see cref="IBookAdapter"/> and the host page can drive the book through
    /// <c>unityInstance.SendMessage("WoopiBookHost", action, payload)</c> (the Woopi WebGL
    /// template does that for postMessage commands). See the package documentation (Documentation~/host-integration.md).
    ///
    /// Every entry point takes ONE string payload; the template always sends one, even if empty:
    ///   ""                       no argument
    ///   "4" / "es"               a bare value (page number, language code)
    ///   {"id":"r1","index":4}    request id plus page number
    ///   {"id":"r1","value":"es"} request id plus a value
    /// The id is echoed in the resulting <see cref="BookBridgeEvent"/>.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class WoopiBookHost : MonoBehaviour
    {
        /// <summary>Name the host page addresses with SendMessage; enforced at runtime.</summary>
        public const string HostObjectName = "WoopiBookHost";

        [Tooltip("Component implementing IBookAdapter for this book.")]
        [SerializeField] private MonoBehaviour adapterBehaviour;

        private IBookAdapter _adapter;
        private bool _ready;
        private string _pendingPageRequestId = "";
        private string _pendingLanguageRequestId = "";

        public bool IsReady => _ready;

        [Serializable]
        private class Payload
        {
            public string id;
            public int index;
            public string value;
        }

        private readonly struct Request
        {
            public readonly string Id;
            public readonly string Text;
            public readonly int Number;
            public readonly bool HasNumber;

            public Request(string id, string text, int number, bool hasNumber)
            {
                Id = id ?? "";
                Text = text ?? "";
                Number = number;
                HasNumber = hasNumber;
            }
        }

        private void Awake()
        {
            gameObject.name = HostObjectName;
            _adapter = adapterBehaviour as IBookAdapter;
            if (_adapter == null)
                Debug.LogError("[WoopiBookHost] No IBookAdapter assigned.", this);
        }

        private IEnumerator Start()
        {
            if (_adapter == null)
                yield break;

            var args = BookStartArgsResolver.Resolve();
            Debug.Log($"[WoopiBookHost] Starting book with {args}.", this);
            yield return _adapter.Initialize(args);

            _adapter.PageChanged += OnPageChanged;
            _adapter.LanguageChanged += OnLanguageChanged;

            _ready = true;
            Emit("Ready", "", true, "");
        }

        private void OnDestroy()
        {
            if (_adapter == null)
                return;

            _adapter.PageChanged -= OnPageChanged;
            _adapter.LanguageChanged -= OnLanguageChanged;
        }

        // ---------------------------------------------------------------- host entry points

        public void NextPage(string payload)
        {
            var r = Parse(payload);
            if (!Accept("NextPage", r))
                return;

            if (_adapter.CurrentPage >= _adapter.PageCount)
                Reject("NextPage", r, "no-next-page");
            else if (_adapter.IsLoading)
                Reject("NextPage", r, "loading");
            else
                AcceptNavigation("NextPage", r, _adapter.Next);
        }

        public void PreviousPage(string payload)
        {
            var r = Parse(payload);
            if (!Accept("PreviousPage", r))
                return;

            if (_adapter.CurrentPage <= 1)
                Reject("PreviousPage", r, "no-previous-page");
            else if (_adapter.IsLoading)
                Reject("PreviousPage", r, "loading");
            else
                AcceptNavigation("PreviousPage", r, _adapter.Previous);
        }

        /// <summary>1-based page number.</summary>
        public void GoToPage(string payload) => GoToInternal("GoToPage", payload, indexBase: 1);

        /// <summary>0-based page index (InteractiveBooks compatibility).</summary>
        public void GoToPageIndex(string payload) => GoToInternal("GoToPageIndex", payload, indexBase: 0);

        public void SetLanguage(string payload)
        {
            var r = Parse(payload);
            if (!Accept("SetLanguage", r))
                return;

            if (string.IsNullOrWhiteSpace(r.Text))
            {
                Reject("SetLanguage", r, "bad-request");
                return;
            }

            var before = _adapter.CurrentLanguage;
            if (!_adapter.SetLanguage(r.Text))
            {
                Reject("SetLanguage", r, "unsupported-language");
                return;
            }

            if (string.Equals(before, _adapter.CurrentLanguage, StringComparison.OrdinalIgnoreCase))
            {
                Reject("SetLanguage", r, "already-set");
                return;
            }

            _pendingLanguageRequestId = r.Id;
            Emit("SetLanguage", r.Id, true, "");
        }

        /// <summary>Restarts the current page only (its state resets); the book stays on it.</summary>
        public void RestartPage(string payload)
        {
            var r = Parse(payload);
            if (!Accept("RestartPage", r))
                return;

            if (_adapter.CurrentPage < 1)
            {
                Reject("RestartPage", r, "not-ready");
                return;
            }

            AcceptNavigation("RestartPage", r, _adapter.RestartPage);
        }

        /// <summary>Restarts the whole book at the opening page.</summary>
        public void RestartBook(string payload)
        {
            var r = Parse(payload);
            if (!Accept("RestartBook", r))
                return;

            AcceptNavigation("RestartBook", r, _adapter.RestartBook);
        }

        // ---------------------------------------------------------------- internals

        private void GoToInternal(string action, string payload, int indexBase)
        {
            var r = Parse(payload);
            if (!Accept(action, r))
                return;

            if (!r.HasNumber)
            {
                Reject(action, r, "bad-request");
                return;
            }

            var page = r.Number + (1 - indexBase);
            if (page < 1 || page > _adapter.PageCount)
                Reject(action, r, "out-of-range");
            else if (page == _adapter.CurrentPage)
                Reject(action, r, "already-there");
            else if (_adapter.IsLoading)
                Reject(action, r, "loading");
            else
                AcceptNavigation(action, r, () => _adapter.GoTo(page));
        }

        /// <summary>Common gate: the book must be up and the adapter present.</summary>
        private bool Accept(string action, Request r)
        {
            if (_adapter != null && _ready)
                return true;

            Reject(action, r, "not-ready");
            return false;
        }

        private void AcceptNavigation(string action, Request r, Action navigate)
        {
            _pendingPageRequestId = r.Id;
            Emit(action, r.Id, true, "");
            navigate();
        }

        private void Reject(string action, Request r, string reason) => Emit(action, r.Id, false, reason);

        private void OnPageChanged(int page)
        {
            var id = _pendingPageRequestId;
            _pendingPageRequestId = "";
            Emit("PageShown", id, true, "");
        }

        private void OnLanguageChanged(string language)
        {
            var id = _pendingLanguageRequestId;
            _pendingLanguageRequestId = "";
            Emit("LanguageChanged", id, true, "");
        }

        private void Emit(string action, string requestId, bool success, string reason)
        {
            var page = _adapter?.CurrentPage ?? 0;
            WebBookBridge.Emit(new BookBridgeEvent
            {
                action = action,
                requestId = requestId ?? "",
                success = success,
                reason = reason ?? "",
                currentPage = page,
                currentPageIndex = Mathf.Max(0, page - 1),
                pageCount = _adapter?.PageCount ?? 0,
                language = _adapter?.CurrentLanguage ?? "",
            });
        }

        private static Request Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new Request("", "", 0, false);

            raw = raw.Trim();
            if (raw[0] == '{')
            {
                try
                {
                    var p = JsonUtility.FromJson<Payload>(raw);
                    var text = p.value ?? "";
                    var number = p.index;
                    if (number == 0 && int.TryParse(text, out var fromText))
                        number = fromText;
                    return new Request(p.id, text, number, true);
                }
                catch (ArgumentException)
                {
                    return new Request("", "", 0, false);
                }
            }

            return new Request("", raw, int.TryParse(raw, out var n) ? n : 0, int.TryParse(raw, out _));
        }
    }
}
