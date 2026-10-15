using System;
using UnityEngine;

namespace Woopi.Examples.SimpleBook
{
    /// <summary>
    /// A deliberately tiny language service: two languages, one string. A real book would use Unity
    /// Localization (or similar) behind the same four things the adapter needs: the current and
    /// default language, a way to switch, and a change notification.
    /// </summary>
    public sealed class ExampleLanguage : MonoBehaviour
    {
        public const string English = "en";
        public const string Spanish = "es";

        public string DefaultLanguage => English;
        public string CurrentLanguage { get; private set; } = English;

        /// <summary>Raised with the new language code after every change.</summary>
        public event Action<string> Changed;

        /// <summary>
        /// Accepts "es", "es-AR", "ES"... Returns false for a language the book does not have, and
        /// leaves the current one untouched.
        /// </summary>
        public bool Set(string code)
        {
            var language = Normalize(code);
            if (language == null)
                return false;

            if (language != CurrentLanguage)
            {
                CurrentLanguage = language;
                Changed?.Invoke(language);
            }

            return true;
        }

        public string PageTitle(int page) => CurrentLanguage == Spanish ? $"Página {page}" : $"Page {page}";

        private static string Normalize(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            // Regional codes fall back to the base language.
            var dash = code.IndexOfAny(new[] { '-', '_' });
            var language = (dash > 0 ? code.Substring(0, dash) : code).Trim().ToLowerInvariant();
            return language == English || language == Spanish ? language : null;
        }
    }
}
