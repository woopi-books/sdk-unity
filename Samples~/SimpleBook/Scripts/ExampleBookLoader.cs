using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Woopi.Examples.SimpleBook
{
    /// <summary>
    /// The book's own page loading, deliberately small. Each page is an Addressable scene loaded
    /// additively. The current page plus <see cref="prefetchRadius"/> neighbours stay resident (the
    /// neighbours hidden); everything else is unloaded. Nothing here knows about the host page:
    /// <see cref="ExampleBookAdapter"/> is the only bridge to the Woopi SDK.
    /// </summary>
    public sealed class ExampleBookLoader : MonoBehaviour
    {
        [SerializeField, Min(1)] private int pageCount = 10;
        [SerializeField, Min(0)] private int prefetchRadius = 1;

        private readonly Dictionary<int, AsyncOperationHandle<SceneInstance>> _resident = new();
        private readonly HashSet<int> _loading = new();

        // Bumped on every navigation so a slower, older one notices it was superseded.
        private int _generation;

        public event Action<int> PageChanged;

        /// <summary>True while the reader is waiting on a page fetch; false when it settles.</summary>
        public event Action<bool> LoadingChanged;

        public int PageCount => pageCount;
        public int CurrentPage { get; private set; }
        public bool IsLoading { get; private set; }

        public static string PageAddress(int page) => $"ExamplePage_{page:00}";

        /// <summary>Opens the first page; finishes once it is shown. 0 means page 1.</summary>
        public IEnumerator Begin(int page)
        {
            var initialPage = page > 0 ? Mathf.Clamp(page, 1, pageCount) : 1;
#if UNITY_EDITOR
            // Pressing Play with a page scene open (ExampleCore is opened alongside it by
            // ExampleCoreEditModeLoader) starts the book on that page, like a build that had
            // navigated there. Only when the host did not ask for a page.
            if (page <= 0)
                yield return ResolveEditorStartPageRoutine(found => initialPage = found);
#endif

            var generation = ++_generation;
            yield return ShowRoutine(initialPage, generation);
            StartCoroutine(WarmWindowRoutine(generation));
        }

#if UNITY_EDITOR
        /// <summary>
        /// Looks for an "ExamplePage_NN" scene opened directly in the Editor (not through
        /// Addressables) and reports its page number. The scene is unloaded first so the normal load
        /// path brings in the Addressables instance instead of leaving a duplicate.
        /// </summary>
        private IEnumerator ResolveEditorStartPageRoutine(Action<int> onResolved)
        {
            const string prefix = "ExamplePage_";
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.name.StartsWith(prefix, StringComparison.Ordinal)
                    || !int.TryParse(scene.name.Substring(prefix.Length), out var found)
                    || found < 1 || found > pageCount)
                    continue;

                yield return SceneManager.UnloadSceneAsync(scene);
                onResolved(found);
                yield break;
            }
        }
#endif

        public void Next() => GoTo(CurrentPage + 1);
        public void Previous() => GoTo(CurrentPage - 1);

        public void GoTo(int page)
        {
            page = Mathf.Clamp(page, 1, pageCount);
            if (page != CurrentPage)
                StartCoroutine(NavigateRoutine(page));
        }

        /// <summary>Unloads and reloads the current page, so everything on it starts over.</summary>
        public void RestartPage()
        {
            if (CurrentPage >= 1)
                StartCoroutine(RestartPageRoutine());
        }

        /// <summary>Drops every page and reopens the book at <paramref name="page"/>.</summary>
        public void RestartBook(int page) => StartCoroutine(RestartBookRoutine(page));

        private IEnumerator NavigateRoutine(int page)
        {
            var generation = ++_generation;
            yield return ShowRoutine(page, generation);
            yield return WarmWindowRoutine(generation);
        }

        private IEnumerator RestartPageRoutine()
        {
            var generation = ++_generation;
            var page = CurrentPage;

            yield return UnloadRoutine(page);
            yield return ShowRoutine(page, generation);
            yield return WarmWindowRoutine(generation);
        }

        private IEnumerator RestartBookRoutine(int page)
        {
            var generation = ++_generation;
            foreach (var resident in new List<int>(_resident.Keys))
                yield return UnloadRoutine(resident);

            CurrentPage = 0;
            yield return ShowRoutine(page > 0 ? Mathf.Clamp(page, 1, pageCount) : 1, generation);
            yield return WarmWindowRoutine(generation);
        }

        /// <summary>Fetches the page if needed and makes it the current, visible page.</summary>
        private IEnumerator ShowRoutine(int page, int generation)
        {
            if (!_resident.ContainsKey(page))
            {
                SetLoading(true);
                yield return LoadPageRoutine(page);
                SetLoading(false);
            }

            // A newer navigation started meanwhile; it owns the state now.
            if (generation != _generation || !_resident.ContainsKey(page))
                yield break;

            CurrentPage = page;
            foreach (var resident in _resident.Keys)
                SetVisible(resident, resident == page);

            PageChanged?.Invoke(page);
        }

        private IEnumerator WarmWindowRoutine(int generation)
        {
            // Free first, then warm, so the resident set never spans two windows.
            foreach (var page in new List<int>(_resident.Keys))
            {
                if (Mathf.Abs(page - CurrentPage) > prefetchRadius)
                    yield return UnloadRoutine(page);
            }

            for (var distance = 1; distance <= prefetchRadius; distance++)
            {
                foreach (var page in new[] { CurrentPage + distance, CurrentPage - distance })
                {
                    if (generation != _generation)
                        yield break;

                    if (page >= 1 && page <= pageCount && !_resident.ContainsKey(page))
                        yield return LoadPageRoutine(page); // one at a time, nearest first
                }
            }
        }

        private IEnumerator LoadPageRoutine(int page)
        {
            if (_resident.ContainsKey(page))
                yield break;

            if (_loading.Contains(page))
            {
                while (_loading.Contains(page))
                    yield return null;
                yield break;
            }

            _loading.Add(page);
            var handle = Addressables.LoadSceneAsync(PageAddress(page), LoadSceneMode.Additive);
            yield return handle; // never WaitForCompletion on WebGL
            _loading.Remove(page);

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[ExampleBookLoader] Failed to load '{PageAddress(page)}': " +
                               $"{handle.OperationException}");
                Addressables.Release(handle);
                yield break;
            }

            _resident[page] = handle;
            // A freshly loaded page stays hidden unless it is the one being read.
            SetVisible(page, page == CurrentPage);
        }

        private IEnumerator UnloadRoutine(int page)
        {
            if (!_resident.TryGetValue(page, out var handle))
                yield break;

            _resident.Remove(page);
            // autoReleaseHandle also releases the bundle reference; unloading the scene alone would not.
            yield return Addressables.UnloadSceneAsync(handle, autoReleaseHandle: true);
        }

        private void SetVisible(int page, bool visible)
        {
            if (!_resident.TryGetValue(page, out var handle))
                return;

            var scene = handle.Result.Scene;
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            foreach (var root in scene.GetRootGameObjects())
                root.SetActive(visible);
        }

        private void SetLoading(bool value)
        {
            if (IsLoading == value)
                return;

            IsLoading = value;
            LoadingChanged?.Invoke(value);
        }

        private void OnDestroy()
        {
            foreach (var handle in _resident.Values)
                Addressables.UnloadSceneAsync(handle, autoReleaseHandle: true);

            _resident.Clear();
        }
    }
}
