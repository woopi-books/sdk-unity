using System;
using System.Collections;
using System.Collections.Generic;
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

        [Tooltip("Apply the host's mute and volume to AudioListener.volume (covers every AudioSource). " +
                 "Turn off if the book routes sound through its own system and reads WoopiAudio instead.")]
        [SerializeField] private bool applyAudioToListener = true;

        private IBookAdapter _adapter;
        private bool _ready;
        private string _pendingPageRequestId = "";
        private string _pendingLanguageRequestId = "";
        private string _pendingAudioRequestId = "";

        /// <summary>Commands every book accepts; custom ones are added with <see cref="RegisterCommand"/>.</summary>
        private static readonly string[] BuiltInCommands =
        {
            "NextPage", "PreviousPage", "GoToPage", "GoToPageIndex", "SetLanguage",
            "SetMuted", "SetVolume", "RestartPage", "RestartBook",
        };

        // Names a custom command or event may not use: they already mean something to the host page.
        private static readonly string[] ReservedNames =
        {
            "Command", "init", "Ready", "PageShown", "LanguageChanged", "AudioChanged",
        };

        private readonly Dictionary<string, Func<BookCommand, string>> _customCommands =
            new Dictionary<string, Func<BookCommand, string>>(StringComparer.Ordinal);

        public bool IsReady => _ready;

        [Serializable]
        private class Payload
        {
            public string action;
            public string id;
            public int index;
            public string value;
        }

        private readonly struct Request
        {
            public readonly string Action;
            public readonly string Id;
            public readonly string Text;
            public readonly int Number;
            public readonly bool HasNumber;

            public Request(string id, string text, int number, bool hasNumber, string action = "")
            {
                Action = action ?? "";
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

            WoopiAudio.Changed += OnAudioChanged;
        }

        private IEnumerator Start()
        {
            if (_adapter == null)
                yield break;

            var options = BookInitOptionsResolver.Resolve();
            Debug.Log($"[WoopiBookHost] Starting book with {options}.", this);

            // Sound is set before anything loads, so the first page never plays at the wrong level.
            WoopiAudio.Set(options.StartMuted, options.StartVolume);
            ApplyAudio();

            yield return _adapter.Initialize(options);

            _adapter.PageChanged += OnPageChanged;
            _adapter.LanguageChanged += OnLanguageChanged;

            _ready = true;
            var ready = BuildEvent("Ready", "", true, "");
            ready.protocolVersion = WoopiSdk.ProtocolVersion;
            ready.sdkVersion = WoopiSdk.Version;
            ready.commands = CommandNames();
            ready.capabilities = CapabilityNames();
            WebBookBridge.Emit(ready);
        }

        private void OnDestroy()
        {
            WoopiAudio.Changed -= OnAudioChanged;

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

        /// <summary>Payload: true/false (or 1/0).</summary>
        public void SetMuted(string payload)
        {
            var r = Parse(payload);
            if (!Accept("SetMuted", r))
                return;

            var muted = BookInitOptionsResolver.TryParseBool(r.Text);
            if (muted == null)
            {
                Reject("SetMuted", r, "bad-request");
                return;
            }

            if (muted.Value == WoopiAudio.Muted)
            {
                Reject("SetMuted", r, "already-set");
                return;
            }

            _pendingAudioRequestId = r.Id;
            Emit("SetMuted", r.Id, true, "");
            WoopiAudio.SetMuted(muted.Value);
        }

        /// <summary>Payload: a number from 0 to 1 (with '.' as the decimal separator).</summary>
        public void SetVolume(string payload)
        {
            var r = Parse(payload);
            if (!Accept("SetVolume", r))
                return;

            var volume = BookInitOptionsResolver.TryParseVolume(r.Text);
            if (volume == null)
            {
                Reject("SetVolume", r, "bad-request");
                return;
            }

            if (volume.Value < 0f || volume.Value > 1f)
            {
                Reject("SetVolume", r, "out-of-range");
                return;
            }

            if (Mathf.Approximately(volume.Value, WoopiAudio.Volume))
            {
                Reject("SetVolume", r, "already-set");
                return;
            }

            _pendingAudioRequestId = r.Id;
            Emit("SetVolume", r.Id, true, "");
            WoopiAudio.SetVolume(volume.Value);
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

        // ---------------------------------------------------------------- custom commands and events

        /// <summary>
        /// Lets the book handle a command of its own from the host page, for example
        /// <c>{action:"ShowHint", value:"3"}</c>. The handler returns null when it handled the command,
        /// or a short reason (such as "bad-request") to reject it; the host page gets the matching
        /// event either way. Register before the book is ready (in Awake or Start): the host page
        /// learns the accepted command names from the <c>ready</c> event.
        /// </summary>
        public void RegisterCommand(string action, Func<BookCommand, string> handler)
        {
            if (!IsCustomName(action))
                throw new ArgumentException(
                    $"'{action}' is not a valid custom command name (letters, digits and _, starting " +
                    "with a letter, and not a built-in or reserved name).", nameof(action));

            _customCommands[action] = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public void UnregisterCommand(string action) => _customCommands.Remove(action ?? "");

        /// <summary>
        /// Sends a book-defined event to the host page (<c>book-event</c> with this action and
        /// <paramref name="data"/>). Use it for things only this book knows, such as "ScoreChanged".
        /// </summary>
        public void Notify(string action, string data = "")
        {
            if (!IsCustomName(action))
            {
                Debug.LogError($"[WoopiBookHost] '{action}' is not a valid custom event name.", this);
                return;
            }

            var evt = BuildEvent(action, "", true, "");
            evt.data = data ?? "";
            WebBookBridge.Emit(evt);
        }

        /// <summary>
        /// The one entry point for custom commands. The template sends every command it has no
        /// dedicated entry point for as <c>{"action":"ShowHint","id":"r1","value":"3"}</c>.
        /// </summary>
        public void Command(string payload)
        {
            var r = Parse(payload);
            var label = string.IsNullOrEmpty(r.Action) ? "Command" : r.Action;
            if (!Accept(label, r))
                return;

            if (string.IsNullOrEmpty(r.Action) || !_customCommands.TryGetValue(r.Action, out var handler))
            {
                Reject(label, r, "unsupported-action");
                return;
            }

            string reason;
            try
            {
                reason = handler(new BookCommand(r.Action, r.Id, r.Text));
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
                reason = "error";
            }

            Emit(r.Action, r.Id, string.IsNullOrEmpty(reason), reason ?? "");
        }

        private string[] CommandNames()
        {
            var names = new List<string>(BuiltInCommands);
            names.AddRange(_customCommands.Keys);
            return names.ToArray();
        }

        private string[] CapabilityNames()
        {
            var names = new List<string> { "navigation", "language", "audio", "restart" };
            if (_adapter is IBookCapabilities extra && extra.Capabilities != null)
            {
                foreach (var tag in extra.Capabilities)
                {
                    if (!string.IsNullOrWhiteSpace(tag) && !names.Contains(tag))
                        names.Add(tag);
                }
            }

            return names.ToArray();
        }

        /// <summary>A name the book may use for its own commands and events.</summary>
        private static bool IsCustomName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length > 64
                || Array.IndexOf(BuiltInCommands, name) >= 0 || Array.IndexOf(ReservedNames, name) >= 0)
                return false;

            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                var letter = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
                var other = (c >= '0' && c <= '9') || c == '_';
                if (!(letter || (i > 0 && other)))
                    return false;
            }

            return true;
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

        private void OnAudioChanged()
        {
            ApplyAudio();

            var id = _pendingAudioRequestId;
            _pendingAudioRequestId = "";
            if (_ready)
                Emit("AudioChanged", id, true, "");
        }

        private void ApplyAudio()
        {
            if (applyAudioToListener)
                AudioListener.volume = WoopiAudio.EffectiveVolume;
        }

        private void Emit(string action, string requestId, bool success, string reason) =>
            WebBookBridge.Emit(BuildEvent(action, requestId, success, reason));

        private BookBridgeEvent BuildEvent(string action, string requestId, bool success, string reason)
        {
            var page = _adapter?.CurrentPage ?? 0;
            return new BookBridgeEvent
            {
                action = action,
                requestId = requestId ?? "",
                success = success,
                reason = reason ?? "",
                currentPage = page,
                currentPageIndex = Mathf.Max(0, page - 1),
                pageCount = _adapter?.PageCount ?? 0,
                language = _adapter?.CurrentLanguage ?? "",
                muted = WoopiAudio.Muted,
                volume = WoopiAudio.Volume,
                data = "",
                sdkVersion = "",
            };
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
                    return new Request(p.id, text, number, true, p.action);
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
