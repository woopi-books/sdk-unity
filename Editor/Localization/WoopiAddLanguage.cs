using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Localization;
using UnityEditor.Localization.Addressables;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;

namespace Woopi.Core.Editor.Localization
{
    /// <summary>
    /// Adds a language to the book: the Locale, an empty table for it in every string and asset table,
    /// and its own remote Addressables groups. Menu: Woopi > Localization > Add language.
    /// Entries the translator has not filled in fall back to the project's default language.
    /// </summary>
    public sealed class WoopiAddLanguage : EditorWindow
    {
        private const string LocaleFolder = "Assets/Localization/Locales";

        private string _code = "";
        private string _status;
        private MessageType _statusType = MessageType.None;

        [MenuItem("Woopi/Localization/Add language...")]
        private static void Open() => GetWindow<WoopiAddLanguage>(true, "Add language", true).minSize = new Vector2(380, 150);

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Enter the language code, e.g. es, fr, pt-BR. It creates the Locale and a table for it in every " +
                "string and asset table, all in groups of their own. Set up remote localization must have run once.",
                MessageType.Info);

            _code = EditorGUILayout.TextField("Language code", _code);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_code)))
            {
                if (GUILayout.Button("Add language"))
                    _statusType = TryAdd(_code.Trim(), out _status) ? MessageType.Info : MessageType.Error;
            }

            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, _statusType);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Languages in the project", EditorStyles.boldLabel);
            foreach (var locale in LocalizationEditorSettings.GetLocales())
                EditorGUILayout.LabelField($"{locale.Identifier.Code}   {locale.LocaleName}");
        }

        /// <summary>Adds the language <paramref name="code"/>. On failure nothing is changed and <paramref name="message"/> says why.</summary>
        public static bool TryAdd(string code, out string message)
        {
            LocaleIdentifier id;
            try
            {
                id = new LocaleIdentifier(code);
            }
            catch (Exception)
            {
                message = $"'{code}' is not a language code.";
                return false;
            }

            if (id.CultureInfo == null)
            {
                message = $"'{code}' is not a known culture. Use a code like es or pt-BR.";
                return false;
            }

            if (LocalizationEditorSettings.GetLocale(id) != null)
            {
                message = $"The project already has {id.Code}.";
                return false;
            }

            var locales = LocalizationEditorSettings.GetLocales();
            if (locales.Count == 0)
            {
                message = "The project has no default language yet. Create one first (Localization Settings).";
                return false;
            }

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                message = "Could not create Addressables settings.";
                return false;
            }

            // Without the Woopi rules the new groups would be local and end up inside the player.
            if (!EditorUtility.IsPersistent(AddressableGroupRules.Instance))
                WoopiLocalizationSetup.Run();

            var fallback = locales[0];
            var locale = Locale.CreateLocale(id);
            locale.Metadata.AddMetadata(new FallbackLocale(fallback));

            Directory.CreateDirectory(LocaleFolder);
            var path = AssetDatabase.GenerateUniqueAssetPath($"{LocaleFolder}/{Sanitize(locale.LocaleName)}.asset");
            AssetDatabase.CreateAsset(locale, path);
            LocalizationEditorSettings.AddLocale(locale);

            var tables = 0;
            foreach (LocalizationTableCollection collection in LocalizationEditorSettings.GetStringTableCollections()
                         .Cast<LocalizationTableCollection>()
                         .Concat(LocalizationEditorSettings.GetAssetTableCollections()))
            {
                if (collection.ContainsTable(id))
                    continue;

                collection.AddNewTable(id);
                tables++;
            }

            WoopiLocalizationSetup.PreloadStringTables();

            // Groups made by the steps above start with the local paths.
            WoopiLocalizationSetup.ApplyRemotePaths(settings);

            AssetDatabase.SaveAssets();
            message = $"Added {locale.LocaleName} ({id.Code}) with {tables} table(s); missing entries fall back to " +
                      $"{fallback.LocaleName}. Fill in the translations, then use Build language pack.";
            Debug.Log($"[Woopi] {message}");
            return true;
        }

        private static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
