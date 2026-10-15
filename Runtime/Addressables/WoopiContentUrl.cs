using System;
using UnityEngine;
using Woopi.Core.Book;

namespace Woopi.Core.Addressables
{
    /// <summary>
    /// Lets a book be built once and hosted anywhere. Books build their remote Addressables groups
    /// with the load path <see cref="LoadPath"/> (a placeholder host that never resolves). At runtime
    /// the platform passes the real base URL as the <c>content</c> start argument and this class
    /// rewrites every Addressables location that starts with the placeholder, both the remote
    /// catalog and the bundles.
    ///
    /// The rewrite is installed automatically before the first scene loads, so it is in place before
    /// Addressables initializes. Installing it later still loads bundles, but the remote catalog
    /// check would hit the placeholder and a catalog update would be missed.
    ///
    /// Nothing is installed when no <c>content</c> argument is given (for example in the Editor),
    /// so locally built or Editor-simulated content keeps working.
    /// </summary>
    public static class WoopiContentUrl
    {
        /// <summary>Host part of the placeholder. Never resolves, so a missing rewrite fails loudly.</summary>
        public const string Placeholder = "https://woopi-content.invalid";

        /// <summary>Remote load path to give the Addressables profile: placeholder plus build target.</summary>
        public const string LoadPath = Placeholder + "/[BuildTarget]";

        private static Func<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation, string> _previous;
        private static bool _installed;

        /// <summary>The base URL currently injected, or null.</summary>
        public static string Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInstall()
        {
            // Read without consuming: the host page's init message may still be on its way.
            var args = BookStartArgsResolver.Resolve(consume: false);
            if (args.HasContentUrl)
                Install(args.ContentUrl);
        }

        /// <summary>
        /// Points every placeholder location at <paramref name="baseUrl"/> (for example
        /// <c>https://cdn.example.com/books/my-book/1.2.0</c>). The build-target folder stays part
        /// of the URL, so the uploaded folder keeps its <c>WebGL/</c> directory. Calling it again
        /// replaces the previous base URL.
        /// </summary>
        public static void Install(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return;

            Current = baseUrl.Trim().TrimEnd('/');

            if (!_installed)
            {
                _installed = true;
                _previous = UnityEngine.AddressableAssets.Addressables.InternalIdTransformFunc;
                UnityEngine.AddressableAssets.Addressables.InternalIdTransformFunc = location =>
                {
                    var id = _previous != null ? _previous(location) : location.InternalId;
                    return Rewrite(id, Current);
                };
            }

            Debug.Log($"[WoopiContentUrl] Content is served from {Current}.");
        }

        /// <summary>Replaces the placeholder prefix of <paramref name="internalId"/>; other ids pass through.</summary>
        public static string Rewrite(string internalId, string baseUrl)
        {
            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(internalId)
                || !internalId.StartsWith(Placeholder, StringComparison.Ordinal))
                return internalId;

            return baseUrl.TrimEnd('/') + internalId.Substring(Placeholder.Length);
        }
    }
}
