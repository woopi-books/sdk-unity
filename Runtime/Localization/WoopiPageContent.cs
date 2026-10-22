using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Woopi.Core.Localization
{
    /// <summary>
    /// Downloads the localized images, audio and other media of a page ahead of time, in one language.
    /// Strings are small and preloaded at startup; media can be megabytes, so Woopi's group rules keep it in
    /// one Addressables group per page and language (see Woopi > Localization > Set up remote localization)
    /// and label every entry with its page. Call <see cref="PreloadAround"/> when the book shows a page, and
    /// again when the language changes, so the next page is already in the cache when the reader turns to it.
    /// </summary>
    public static class WoopiPageContent
    {
        /// <summary>Label prefix of the page an asset belongs to, set by the Woopi group rules.</summary>
        public const string PageLabelPrefix = "woopi-page-";

        // Unity Localization labels every localized entry with Locale-<code>.
        private const string LocaleLabelPrefix = "Locale-";

        /// <summary>The Addressables label of page <paramref name="page"/> (1-based).</summary>
        public static string PageLabel(int page) => PageLabelPrefix + page.ToString("00");

        /// <summary>
        /// Downloads page <paramref name="page"/>'s localized media in <paramref name="locale"/> (the selected
        /// language by default) into the Addressables cache. Does nothing when the page has none. Use with
        /// StartCoroutine or yield return.
        /// </summary>
        public static IEnumerator PreloadPage(int page, Locale locale = null)
        {
            if (locale == null)
            {
                yield return LocalizationSettings.InitializationOperation;
                locale = LocalizationSettings.SelectedLocale;
            }

            if (locale == null)
                yield break;

            var keys = new object[] { PageLabel(page), LocaleLabelPrefix + locale.Identifier.Code };
            var find = Addressables.LoadResourceLocationsAsync(keys, Addressables.MergeMode.Intersection);
            yield return find;

            if (find.Status == AsyncOperationStatus.Succeeded && find.Result.Count > 0)
            {
                var download = Addressables.DownloadDependenciesAsync(find.Result, false);
                yield return download;

                if (download.Status == AsyncOperationStatus.Failed)
                    Debug.LogWarning($"[WoopiPageContent] Could not download page {page} in {locale.Identifier.Code}: {download.OperationException?.Message}");

                Addressables.Release(download);
            }

            Addressables.Release(find);
        }

        /// <summary>
        /// Preloads <paramref name="page"/> first, then the pages within <paramref name="radius"/> of it, nearest
        /// first (page 2 with radius 1: 2, 1, 3). Pages below 1 are skipped; pages beyond the book's end have no
        /// content and cost one lookup.
        /// </summary>
        public static IEnumerator PreloadAround(int page, int radius = 1, Locale locale = null)
        {
            yield return PreloadPage(page, locale);

            for (var distance = 1; distance <= radius; distance++)
            {
                if (page - distance >= 1)
                    yield return PreloadPage(page - distance, locale);

                yield return PreloadPage(page + distance, locale);
            }
        }
    }
}
