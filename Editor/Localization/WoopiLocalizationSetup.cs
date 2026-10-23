using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Localization;
using UnityEditor.Localization.Addressables;
using UnityEngine;
using UnityEngine.Localization;
using Woopi.Core.Editor.Addressables;
using Object = UnityEngine.Object;

namespace Woopi.Core.Editor.Localization
{
    /// <summary>
    /// Prepares Unity Localization so every language lives in its own remote Addressables groups and
    /// can be published without rebuilding the player (a "language pack", see
    /// <see cref="WoopiLanguagePackBuilder"/>). Menu: Woopi > Localization > Set up remote localization.
    /// Safe to re-run; it also fixes groups created since the last run.
    /// </summary>
    public static class WoopiLocalizationSetup
    {
        /// <summary>Every group Unity Localization creates starts with this; so do the groups of a language pack.</summary>
        public const string GroupPrefix = "Localization-";

        private const string RulesFolder = "Assets/Localization";
        private const string RulesPath = RulesFolder + "/Woopi Addressable Group Rules.asset";

        [MenuItem("Woopi/Localization/Set up remote localization")]
        public static void Run()
        {
            WoopiAddressablesSetup.UsePlaceholder();

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
                return;

            // The player must look for a newer catalog on startup, or a new language is never seen.
            settings.DisableCatalogUpdateOnStartup = false;

            var rules = EnsureRules();

            // One group per language, including the Locale asset itself, so adding a language only adds groups.
            foreach (var locale in LocalizationEditorSettings.GetLocales())
                rules.LocaleResolver.AddToGroup(locale, new[] { locale.Identifier }, settings, false);

            RegroupAssets(settings, rules);
            RemoveEmptyGroups(settings);
            PreloadStringTables();
            var changed = ApplyRemotePaths(settings);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Woopi] Remote localization is set up ({changed} group(s) switched to the remote paths). " +
                      "Text is preloaded at startup; localized media goes in one group per page and language " +
                      "(keep it in folders named Page_01, Page_02, ...). Add languages with Woopi > Localization > Add language.");
        }

        /// <summary>
        /// Moves every localized asset into the group of its page and language. Runs after the rules change
        /// and whenever the setup is re-run, so assets added since land in the right place.
        /// </summary>
        private static void RegroupAssets(AddressableAssetSettings settings, AddressableGroupRules rules)
        {
            var languagesByAsset = new Dictionary<string, List<LocaleIdentifier>>();
            foreach (var collection in LocalizationEditorSettings.GetAssetTableCollections())
            {
                foreach (var table in collection.AssetTables)
                {
                    if (table == null)
                        continue;

                    foreach (var entry in table.Values)
                    {
                        if (string.IsNullOrEmpty(entry.Guid))
                            continue;

                        if (!languagesByAsset.TryGetValue(entry.Guid, out var languages))
                            languagesByAsset[entry.Guid] = languages = new List<LocaleIdentifier>();

                        if (!languages.Contains(table.LocaleIdentifier))
                            languages.Add(table.LocaleIdentifier);
                    }
                }
            }

            foreach (var pair in languagesByAsset)
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(pair.Key));
                if (asset != null)
                    rules.AssetResolver.AddToGroup(asset, pair.Value, settings, false);
            }
        }

        /// <summary>Drops localization groups that regrouping emptied; Unity Localization recreates them on demand.</summary>
        private static void RemoveEmptyGroups(AddressableAssetSettings settings)
        {
            for (var i = settings.groups.Count - 1; i >= 0; i--)
            {
                var group = settings.groups[i];
                if (IsLocalizationGroup(group) && group.entries.Count == 0)
                    settings.RemoveGroup(group);
            }
        }

        /// <summary>
        /// Text is small and every page needs it, so string tables load with the book. Asset tables are not
        /// flagged: preloading one loads every asset in it, which is exactly what per-page loading avoids.
        /// </summary>
        public static void PreloadStringTables()
        {
            foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
            {
                foreach (var table in collection.StringTables)
                {
                    if (table != null)
                        LocalizationEditorSettings.SetPreloadTableFlag(table, true);
                }
            }
        }

        /// <summary>The group rules asset the project uses, created with Woopi's per-language layout when missing.</summary>
        private static AddressableGroupRules EnsureRules()
        {
            var rules = AddressableGroupRules.Instance;
            if (rules == null || !EditorUtility.IsPersistent(rules))
            {
                Directory.CreateDirectory(RulesFolder);
                rules = AssetDatabase.LoadAssetAtPath<AddressableGroupRules>(RulesPath);
                if (rules == null)
                {
                    rules = ScriptableObject.CreateInstance<AddressableGroupRules>();
                    AssetDatabase.CreateAsset(rules, RulesPath);
                }

                AddressableGroupRules.Instance = rules;
            }

            // Tables and localized assets already default to one group per language.
            rules.LocaleResolver = new GroupResolver("Localization-Locale-{LocaleName}", "Localization-Locales");

            // Media goes in a group per page and language; text and the tables stay in one group per language.
            if (!(rules.AssetResolver is WoopiPageGroupResolver))
                rules.AssetResolver = new WoopiPageGroupResolver();
            EditorUtility.SetDirty(rules);
            return rules;
        }

        /// <summary>
        /// Points every localization group at the remote build and load paths. Groups are created with the
        /// local paths, so this runs after anything that can create one. Returns how many groups changed.
        /// </summary>
        public static int ApplyRemotePaths(AddressableAssetSettings settings)
        {
            var changed = 0;
            foreach (var group in settings.groups)
            {
                if (IsLocalizationGroup(group) && MakeRemote(settings, group))
                    changed++;
            }

            return changed;
        }

        /// <summary>Switches one group to the remote paths. Returns false when it was already remote or has no bundle schema.</summary>
        public static bool MakeRemote(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema == null || IsRemote(settings, schema))
                return false;

            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            EditorUtility.SetDirty(schema);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupSchemaModified, group, true, true);
            return true;
        }

        public static bool IsLocalizationGroup(AddressableAssetGroup group) =>
            group != null && group.Name.StartsWith(GroupPrefix, StringComparison.Ordinal);

        public static bool IsRemote(AddressableAssetSettings settings, BundledAssetGroupSchema schema) =>
            schema.BuildPath.GetName(settings) == AddressableAssetSettings.kRemoteBuildPath &&
            schema.LoadPath.GetName(settings) == AddressableAssetSettings.kRemoteLoadPath;
    }
}
