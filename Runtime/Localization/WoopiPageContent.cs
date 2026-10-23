using System.Collections;
using System.Collections.Generic;
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
    /// and label every entry with its page.
    ///
    /// Order matters. In a WebGL player a bundle that is being downloaded ahead of time and loaded for use at
    /// the same moment can fail (the browser aborts one of the two requests). So:
    /// <list type="bullet">
    /// <item>Before pointing localized references at a page, <c>yield return PreloadPage(page)</c> and only
    /// then set them. A call that finds the page already downloading waits for that download.</item>
    /// <item>Preload the neighbours with <see cref="PreloadNeighbours"/> after the page is showing.</item>
    /// <item>When the language changes, Unity Localization starts loading the current page at once, so only
    /// preload the neighbours then.</item>
    /// <item>Do not stop these coroutines (StopCoroutine, StopAllCoroutines) while they run.</item>
    /// </list>
    /// </summary>
    public static class WoopiPageContent
    {
        /// <summary>Label prefix of the page an asset belongs to, set by the Woopi group rules.</summary>
        public const string PageLabelPrefix = "woopi-page-";

        // Unity Localization labels every localized entry with Locale-<code>.
        private const string LocaleLabelPrefix = "Locale-";

        private static readonly HashSet<string> InFlight = new HashSet<string>();

        /// <summary>The Addressables label of page <paramref name="page"/> (1-based).</summary>
        public static string PageLabel(int page) => PageLabelPrefix + page.ToString("00");

        /// <summary>
        /// Downloads page <paramref name="page"/>'s localized media in <paramref name="locale"/> (the selected
        /// language by default) into the Addressables cache. Does nothing when the page has none or is already
        /// downloaded. If the same page is being downloaded already, waits for that instead. Use with
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

            var id = page + "/" + locale.Identifier.Code;
            if (InFlight.Contains(id))
            {
                while (InFlight.Contains(id))
                    yield return null;

                yield break;
            }

            InFlight.Add(id);
            try
            {
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
            finally
            {
                InFlight.Remove(id);
            }
        }

        /// <summary>
        /// Preloads the pages within <paramref name="radius"/> of <paramref name="page"/>, nearest first (page 2,
        /// radius 1: 1, then 3), not the page itself. Call it once <paramref name="page"/> is showing.
        /// Pages below 1 are skipped; pages beyond the book's end have no content and cost one lookup.
        /// </summary>
        public static IEnumerator PreloadNeighbours(int page, int radius = 1, Locale locale = null)
        {
            for (var distance = 1; distance <= radius; distance++)
            {
                if (page - distance >= 1)
                    yield return PreloadPage(page - distance, locale);

                yield return PreloadPage(page + distance, locale);
            }
        }
    }
}
