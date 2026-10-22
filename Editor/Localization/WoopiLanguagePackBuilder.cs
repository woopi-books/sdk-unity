using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;

namespace Woopi.Core.Editor.Localization
{
    /// <summary>
    /// Publishes one language without rebuilding the player. It runs an Addressables content update build
    /// against the <c>addressables_content_state.bin</c> saved by the release build, then copies the
    /// language's bundles and the new catalog into <c>&lt;output&gt;/&lt;code&gt;/&lt;BuildTarget&gt;/</c>.
    /// Upload that folder's files into the book's content folder (same base URL as the release).
    /// Menu: Woopi > Localization > Build language pack.
    /// </summary>
    public sealed class WoopiLanguagePackBuilder : EditorWindow
    {
        private const string OutputKey = "Woopi.LanguagePack.Output";

        private int _localeIndex;
        private string _statePath;
        private string _output;
        private string _status;
        private MessageType _statusType = MessageType.None;

        [MenuItem("Woopi/Localization/Build language pack...")]
        private static void Open() => GetWindow<WoopiLanguagePackBuilder>(true, "Build language pack", true).minSize = new Vector2(460, 220);

        private void OnEnable()
        {
            _output = EditorPrefs.GetString(OutputKey, "LanguagePacks");
            _statePath ??= DefaultStatePath();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Builds only what a new or updated language needs, on top of the release the players already run. " +
                "Needs the addressables_content_state.bin saved when that release was built.",
                MessageType.Info);

            var locales = LocalizationEditorSettings.GetLocales();
            if (locales.Count == 0)
            {
                EditorGUILayout.HelpBox("The project has no languages.", MessageType.Warning);
                return;
            }

            _localeIndex = Mathf.Clamp(_localeIndex, 0, locales.Count - 1);
            _localeIndex = EditorGUILayout.Popup("Language", _localeIndex,
                locales.Select(l => $"{l.Identifier.Code}  {l.LocaleName}").ToArray());

            using (new EditorGUILayout.HorizontalScope())
            {
                _statePath = EditorGUILayout.TextField("Release state file", _statePath);
                if (GUILayout.Button("...", GUILayout.Width(28)))
                {
                    var picked = EditorUtility.OpenFilePanel("addressables_content_state.bin",
                        Path.GetDirectoryName(_statePath) ?? "", "bin");
                    if (!string.IsNullOrEmpty(picked))
                        _statePath = picked;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _output = EditorGUILayout.TextField("Output folder", _output);
                if (GUILayout.Button("...", GUILayout.Width(28)))
                {
                    var picked = EditorUtility.OpenFolderPanel("Language pack output", _output, "");
                    if (!string.IsNullOrEmpty(picked))
                        _output = picked;
                }
            }

            if (GUILayout.Button("Build language pack"))
            {
                EditorPrefs.SetString(OutputKey, _output);
                _statusType = TryBuild(locales[_localeIndex], _statePath, _output, out _status)
                    ? MessageType.Info
                    : MessageType.Error;
            }

            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, _statusType);
        }

        private static string DefaultStatePath()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            return settings == null ? "" : ContentUpdateScript.GetContentStateDataPath(false, settings);
        }

        /// <summary>
        /// Builds the content update and copies the files for <paramref name="locale"/> to
        /// <paramref name="outputRoot"/>. On failure nothing is copied and <paramref name="message"/> says why.
        /// </summary>
        public static bool TryBuild(Locale locale, string statePath, string outputRoot, out string message)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                message = "The project has no Addressables settings.";
                return false;
            }

            if (!File.Exists(statePath))
            {
                message = $"State file not found: {statePath}. Use the one saved by the release build.";
                return false;
            }

            var groups = settings.groups
                .Where(g => WoopiLocalizationSetup.IsLocalizationGroup(g) && g.Name.EndsWith("-" + locale.LocaleName, System.StringComparison.Ordinal))
                .ToList();
            if (groups.Count == 0)
            {
                message = $"No Addressables groups for {locale.LocaleName}. Add the language with Add language.";
                return false;
            }

            var local = groups.FirstOrDefault(g =>
            {
                var schema = g.GetSchema<BundledAssetGroupSchema>();
                return schema != null && !WoopiLocalizationSetup.IsRemote(settings, schema);
            });
            if (local != null)
            {
                message = $"Group '{local.Name}' is not remote. Run Set up remote localization.";
                return false;
            }

            var result = ContentUpdateScript.BuildContentUpdate(settings, statePath);
            if (result == null || !string.IsNullOrEmpty(result.Error))
            {
                message = $"The content update build failed: {result?.Error ?? "invalid state file"}";
                return false;
            }

            var catalogDir = settings.profileSettings.EvaluateString(
                settings.activeProfileId, settings.RemoteCatalogBuildPath.GetValue(settings));
            var target = Path.GetFileName(catalogDir.TrimEnd('/', '\\'));
            var destination = Path.Combine(outputRoot, locale.Identifier.Code, target);
            if (Directory.Exists(destination))
                Directory.Delete(destination, true);
            Directory.CreateDirectory(destination);

            var files = 0;
            long bytes = 0;
            void Copy(string source)
            {
                var to = Path.Combine(destination, Path.GetFileName(source));
                File.Copy(source, to, true);
                files++;
                bytes += new FileInfo(to).Length;
            }

            foreach (var bundle in result.AssetBundleBuildResults.Where(b => b.SourceAssetGroup != null && groups.Contains(b.SourceAssetGroup)))
                Copy(bundle.FilePath);

            foreach (var catalog in Directory.GetFiles(catalogDir, "catalog_*"))
                Copy(catalog);

            message = $"{files} file(s), {bytes / 1024f:0.#} KB in {destination}. Upload them into the book's content " +
                      $"folder next to the release's {target}/ files, then reload the book.";
            Debug.Log($"[Woopi] Language pack {locale.Identifier.Code}: {message}");
            return true;
        }
    }
}
