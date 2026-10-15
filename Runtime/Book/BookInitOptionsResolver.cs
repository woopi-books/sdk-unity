using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Woopi.Core.Book
{
    /// <summary>
    /// The one place that works out the startup options. Order of precedence, per option:
    /// 1. what the host page handed the WebGL template (an <c>init</c> message, or the page's
    ///    own <c>?lang=&amp;page=&amp;content=&amp;muted=&amp;volume=</c> query, which the template also reads),
    /// 2. the query string of <see cref="Application.absoluteURL"/> (for builds without the
    ///    Woopi template),
    /// 3. nothing, which the book turns into its defaults.
    /// </summary>
    public static class BookInitOptionsResolver
    {
        public const string LanguageParameter = "lang";
        public const string PageParameter = "page";
        public const string ContentParameter = "content";
        public const string MutedParameter = "muted";
        public const string VolumeParameter = "volume";

        /// <summary>URL parameters starting with this become <see cref="BookInitOptions.Extra"/> entries.</summary>
        public const string ExtraPrefix = "x_";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string WoopiBook_GetStartArgs(int consume);
#endif

        // JsonUtility leaves missing fields at their initial value, so -1 means "not given".
        [Serializable]
        private class RawOptions
        {
            public string lang;
            public int page;
            public string content;
            public int muted = -1;
            public float volume = -1f;
            public ExtraPair[] extra;
        }

        [Serializable]
        private class ExtraPair
        {
            public string k;
            public string v;
        }

        /// <param name="consume">
        /// True when the book is about to start with these options. A host <c>init</c> message that
        /// arrives after that is applied as ordinary commands instead. Pass false to peek.
        /// </param>
        public static BookInitOptions Resolve(bool consume = true)
        {
            var fromHost = ReadFromHost(consume);
            var fromUrl = ReadFromUrl(Application.absoluteURL);

            return new BookInitOptions(
                fromHost.HasLanguage ? fromHost.Language : fromUrl.Language,
                fromHost.HasStartPage ? fromHost.StartPage : fromUrl.StartPage,
                fromHost.HasContentUrl ? fromHost.ContentUrl : fromUrl.ContentUrl,
                fromHost.Muted ?? fromUrl.Muted,
                fromHost.Volume ?? fromUrl.Volume,
                Merge(fromUrl.Extra, fromHost.Extra));
        }

        private static BookInitOptions ReadFromHost(bool consume)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                var json = WoopiBook_GetStartArgs(consume ? 1 : 0);
                if (!string.IsNullOrEmpty(json))
                {
                    var raw = JsonUtility.FromJson<RawOptions>(json);
                    return new BookInitOptions(raw.lang, raw.page, raw.content,
                        raw.muted < 0 ? (bool?)null : raw.muted != 0,
                        raw.volume < 0f ? (float?)null : raw.volume,
                        ToDictionary(raw.extra));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BookInitOptionsResolver] Could not read host options: {e.Message}");
            }
#endif
            return default;
        }

        internal static BookInitOptions ReadFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return default;

            var queryStart = url.IndexOf('?');
            if (queryStart < 0)
                return default;

            var query = url.Substring(queryStart + 1);
            var fragmentStart = query.IndexOf('#');
            if (fragmentStart >= 0)
                query = query.Substring(0, fragmentStart);

            string lang = null;
            string content = null;
            bool? muted = null;
            float? volume = null;
            Dictionary<string, string> extra = null;
            var page = 0;
            foreach (var pair in query.Split('&'))
            {
                var separator = pair.IndexOf('=');
                if (separator <= 0)
                    continue;

                var name = Uri.UnescapeDataString(pair.Substring(0, separator));
                var value = Uri.UnescapeDataString(pair.Substring(separator + 1));

                if (string.Equals(name, LanguageParameter, StringComparison.OrdinalIgnoreCase))
                    lang = value;
                else if (string.Equals(name, PageParameter, StringComparison.OrdinalIgnoreCase))
                    int.TryParse(value, out page);
                else if (string.Equals(name, ContentParameter, StringComparison.OrdinalIgnoreCase))
                    content = value;
                else if (string.Equals(name, MutedParameter, StringComparison.OrdinalIgnoreCase))
                    muted = TryParseBool(value);
                else if (string.Equals(name, VolumeParameter, StringComparison.OrdinalIgnoreCase))
                    volume = TryParseVolume(value);
                else if (name.Length > ExtraPrefix.Length
                         && name.StartsWith(ExtraPrefix, StringComparison.OrdinalIgnoreCase))
                    (extra ??= new Dictionary<string, string>())[name.Substring(ExtraPrefix.Length)] = value;
            }

            return new BookInitOptions(lang, page, content, muted, volume, extra);
        }

        private static Dictionary<string, string> ToDictionary(ExtraPair[] pairs)
        {
            if (pairs == null || pairs.Length == 0)
                return null;

            var result = new Dictionary<string, string>(pairs.Length);
            foreach (var pair in pairs)
            {
                if (!string.IsNullOrEmpty(pair?.k))
                    result[pair.k] = pair.v ?? "";
            }

            return result;
        }

        /// <summary>Entries of <paramref name="over"/> win over the same keys in <paramref name="under"/>.</summary>
        private static Dictionary<string, string> Merge(
            IReadOnlyDictionary<string, string> under, IReadOnlyDictionary<string, string> over)
        {
            if (under.Count == 0 && over.Count == 0)
                return null;

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in under)
                result[pair.Key] = pair.Value;
            foreach (var pair in over)
                result[pair.Key] = pair.Value;
            return result;
        }

        /// <summary>Accepts true/false, 1/0, yes/no, on/off. Null when it is none of those.</summary>
        internal static bool? TryParseBool(string text)
        {
            switch ((text ?? "").Trim().ToLowerInvariant())
            {
                case "true": case "1": case "yes": case "on": return true;
                case "false": case "0": case "no": case "off": return false;
                default: return null;
            }
        }

        /// <summary>A number with a '.' decimal separator, or null when it is not a number.</summary>
        internal static float? TryParseVolume(string text)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                   && !float.IsNaN(v) && !float.IsInfinity(v)
                ? v
                : (float?)null;
        }
    }
}
