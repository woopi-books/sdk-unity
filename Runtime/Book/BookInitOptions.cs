using System;
using System.Collections.Generic;

namespace Woopi.Core.Book
{
    /// <summary>
    /// Everything the host can tell a book at startup. Anything left out means "the book's
    /// default": its own default language, page 1, no injected content location, sound on at full
    /// volume. New startup settings are added here, so adapters keep one parameter.
    /// </summary>
    public readonly struct BookInitOptions
    {
        private static readonly IReadOnlyDictionary<string, string> NoExtra =
            new Dictionary<string, string>(0);

        private readonly IReadOnlyDictionary<string, string> _extra;

        /// <summary>Language code such as "es" or "en-US", or null for the book's default.</summary>
        public string Language { get; }

        /// <summary>Opening page, 1-based, or 0 for the start of the book.</summary>
        public int StartPage { get; }

        /// <summary>
        /// Base URL the book's Addressables content is hosted at, or null. Applied by
        /// WoopiContentUrl (Woopi.Core.Addressables) before the first load.
        /// </summary>
        public string ContentUrl { get; }

        /// <summary>Start muted, or null when the host did not say (the book starts unmuted).</summary>
        public bool? Muted { get; }

        /// <summary>Master volume from 0 to 1, or null when the host did not say (full volume).</summary>
        public float? Volume { get; }

        /// <summary>
        /// Book-specific settings the SDK does not know about, as text. The host passes them as an
        /// <c>extra</c> object in the <c>init</c> message or as <c>x_name=value</c> in the page URL.
        /// Never null; keys are case-insensitive.
        /// </summary>
        public IReadOnlyDictionary<string, string> Extra => _extra ?? NoExtra;

        public bool HasLanguage => !string.IsNullOrWhiteSpace(Language);
        public bool HasStartPage => StartPage > 0;
        public bool HasContentUrl => !string.IsNullOrWhiteSpace(ContentUrl);

        /// <summary>The muted state to start with: the host's choice, else unmuted.</summary>
        public bool StartMuted => Muted ?? false;

        /// <summary>The volume to start with: the host's choice, else 1.</summary>
        public float StartVolume => Volume ?? 1f;

        public BookInitOptions(string language, int startPage, string contentUrl = null,
            bool? muted = null, float? volume = null,
            IReadOnlyDictionary<string, string> extra = null)
        {
            Language = string.IsNullOrWhiteSpace(language) ? null : language.Trim();
            StartPage = startPage > 0 ? startPage : 0;
            ContentUrl = string.IsNullOrWhiteSpace(contentUrl) ? null : contentUrl.Trim();
            Muted = muted;
            Volume = volume.HasValue ? UnityEngine.Mathf.Clamp01(volume.Value) : (float?)null;

            if (extra == null || extra.Count == 0)
            {
                _extra = null;
            }
            else
            {
                var copy = new Dictionary<string, string>(extra.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var pair in extra)
                    copy[pair.Key] = pair.Value ?? "";
                _extra = copy;
            }
        }

        /// <summary>The extra setting <paramref name="key"/>, or <paramref name="fallback"/> when absent.</summary>
        public string GetExtra(string key, string fallback = null) =>
            key != null && Extra.TryGetValue(key, out var value) ? value : fallback;

        public override string ToString() =>
            $"lang={(HasLanguage ? Language : "<default>")}, page={(HasStartPage ? StartPage : 1)}, " +
            $"content={(HasContentUrl ? ContentUrl : "<none>")}, muted={StartMuted}, volume={StartVolume:0.##}, " +
            $"extra={Extra.Count}";
    }
}
