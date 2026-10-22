using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization;
using UnityEditor.Localization.Addressables;
using UnityEngine;
using UnityEngine.Localization;
using Woopi.Core.Localization;
using Object = UnityEngine.Object;

namespace Woopi.Core.Editor.Localization
{
    /// <summary>
    /// Puts each localized asset into a group of its own page and language, e.g.
    /// <c>Localization-Assets-Page03-Spanish</c>, and labels it <c>woopi-page-03</c>. The page is read from the
    /// asset's path: any folder or file name with <c>page</c> and a number (<c>Art/Page_03/cat_es.png</c>,
    /// <c>page3</c>, <c>Page 03</c>). Assets without a page, and assets shared by several languages, keep the
    /// default Unity Localization groups. Group names always end with <c>-&lt;language&gt;</c>, which is how
    /// Build language pack finds a language's groups.
    /// </summary>
    [Serializable]
    public sealed class WoopiPageGroupResolver : GroupResolver
    {
        [SerializeField] private string m_PagePattern = @"(?i)page[ _\-]?(\d+)";

        public WoopiPageGroupResolver() : base("Localization-Assets-{LocaleName}", "Localization-Assets-Shared")
        {
        }

        /// <summary>Regular expression applied to the asset path; group 1 is the page number.</summary>
        public string PagePattern { get => m_PagePattern; set => m_PagePattern = value; }

        public bool TryGetPage(Object asset, out int page)
        {
            page = 0;
            if (asset == null)
                return false;

            var path = AssetDatabase.GetAssetPath(asset);
            var match = string.IsNullOrEmpty(path) ? Match.Empty : Regex.Match(path, m_PagePattern);
            return match.Success && match.Groups.Count > 1 && int.TryParse(match.Groups[1].Value, out page) && page > 0;
        }

        public override string GetExpectedGroupName(IList<LocaleIdentifier> locales, Object asset, AddressableAssetSettings aaSettings)
        {
            if (locales != null && locales.Count == 1 && TryGetPage(asset, out var page))
            {
                var locale = LocalizationEditorSettings.GetLocale(locales[0]);
                if (locale != null)
                    return $"Localization-Assets-Page{page:00}-{locale.LocaleName}";
            }

            return base.GetExpectedGroupName(locales, asset, aaSettings);
        }

        public override AddressableAssetEntry AddToGroup(Object asset, IList<LocaleIdentifier> locales, AddressableAssetSettings aaSettings, bool createUndo)
        {
            var entry = base.AddToGroup(asset, locales, aaSettings, createUndo);

            // The runtime downloads a page by this label (WoopiPageContent).
            if (entry != null && TryGetPage(asset, out var page))
                entry.SetLabel(WoopiPageContent.PageLabel(page), true, true);

            return entry;
        }
    }
}
