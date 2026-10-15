using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Woopi.Core.Book
{
    /// <summary>
    /// The one place that works out the startup arguments. Order of precedence:
    /// 1. what the host page handed the WebGL template (an <c>init</c> message, or the page's
    ///    own <c>?lang=&amp;page=&amp;content=</c> query, which the template also reads),
    /// 2. the query string of <see cref="Application.absoluteURL"/> (for builds without the
    ///    Woopi template),
    /// 3. nothing, which the book turns into its defaults.
    /// </summary>
    public static class BookStartArgsResolver
    {
        public const string LanguageParameter = "lang";
        public const string PageParameter = "page";
        public const string ContentParameter = "content";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string WoopiBook_GetStartArgs(int consume);
#endif

        [Serializable]
        private class RawArgs
        {
            public string lang;
            public int page;
            public string content;
        }

        /// <param name="consume">
        /// True when the book is about to start with these arguments. A host <c>init</c> message that
        /// arrives after that is applied as ordinary commands instead. Pass false to peek.
        /// </param>
        public static BookStartArgs Resolve(bool consume = true)
        {
            var fromHost = ReadFromHost(consume);
            var fromUrl = ReadFromUrl(Application.absoluteURL);

            return new BookStartArgs(
                fromHost.HasLanguage ? fromHost.Language : fromUrl.Language,
                fromHost.HasStartPage ? fromHost.StartPage : fromUrl.StartPage,
                fromHost.HasContentUrl ? fromHost.ContentUrl : fromUrl.ContentUrl);
        }

        private static BookStartArgs ReadFromHost(bool consume)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                var json = WoopiBook_GetStartArgs(consume ? 1 : 0);
                if (!string.IsNullOrEmpty(json))
                {
                    var raw = JsonUtility.FromJson<RawArgs>(json);
                    return new BookStartArgs(raw.lang, raw.page, raw.content);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BookStartArgsResolver] Could not read host args: {e.Message}");
            }
#endif
            return default;
        }

        internal static BookStartArgs ReadFromUrl(string url)
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
            }

            return new BookStartArgs(lang, page, content);
        }
    }
}
