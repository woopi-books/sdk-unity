using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using Woopi.Core.Book;

namespace Woopi.Samples.Localization
{
    /// <summary>
    /// One button per language the book knows. The list comes from Unity Localization at runtime, so a
    /// language published later as a language pack shows up here without rebuilding the player.
    /// A <c>lang</c> start argument from the host (<see cref="BookInitOptions.Language"/>) picks the
    /// opening language.
    /// </summary>
    [AddComponentMenu("Woopi/Samples/Language Menu")]
    public sealed class LanguageMenu : MonoBehaviour
    {
        [SerializeField] private Button template;
        [SerializeField] private Transform container;

        private IEnumerator Start()
        {
            template.gameObject.SetActive(false);

            // Locales are read from the Addressables catalog, so wait for it (including a remote update).
            yield return LocalizationSettings.InitializationOperation;

            foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
                AddButton(locale);

            var options = BookInitOptionsResolver.Resolve(consume: false);
            if (options.HasLanguage)
                Select(options.Language);
        }

        /// <summary>Selects a language by code. "es-AR" falls back to "es". Returns false when the book has no such language.</summary>
        public bool Select(string code)
        {
            var locale = Find(code);
            if (locale == null)
                return false;

            LocalizationSettings.SelectedLocale = locale;
            return true;
        }

        private static Locale Find(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var exact = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code.Trim()));
            if (exact != null)
                return exact;

            // Regional codes fall back to the base language.
            var dash = code.IndexOfAny(new[] { '-', '_' });
            return dash > 0
                ? LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code.Substring(0, dash)))
                : null;
        }

        private void AddButton(Locale locale)
        {
            var button = Instantiate(template, container);
            button.gameObject.name = locale.Identifier.Code;
            button.GetComponentInChildren<Text>().text = locale.LocaleName;
            button.onClick.AddListener(() => LocalizationSettings.SelectedLocale = locale);
            button.gameObject.SetActive(true);
        }
    }
}
