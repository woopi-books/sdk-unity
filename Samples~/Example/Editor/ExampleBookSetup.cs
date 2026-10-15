using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Woopi.Core.Book;

namespace Woopi.Examples.SimpleBook.Editor
{
    /// <summary>
    /// Rebuilds the sample from its art: ten dummy page scenes, the core scene with the Woopi host,
    /// and the Addressables groups. Menu: Woopi > Example > Build sample book. Safe to re-run.
    /// </summary>
    internal static class ExampleBookSetup
    {
        private const int PageCount = 10;
        private static string Root => ExamplePaths.Root;
        private static string ArtRoot => Root + "/Art";
        private static string SceneRoot => Root + "/Scenes";
        private static string PageSceneRoot => SceneRoot + "/Pages";
        private static string CoreScenePath => SceneRoot + "/ExampleCore.unity";
        private static string DevNavRoot => Root + "/DevNavigation";
        private const string SharedGroupName = "Woopi Example Shared";

        private static readonly string[] Art =
        {
            ArtRoot + "/example_bg.png",
            ArtRoot + "/example_art_a.png",
            ArtRoot + "/example_art_b.png",
        };

        private static string PageName(int page) => $"ExamplePage_{page:00}";
        private static string PageScenePath(int page) => $"{PageSceneRoot}/{PageName(page)}.unity";

        [MenuItem("Woopi/Example/Build sample book")]
        internal static void Build()
        {
            ConfigureTextures();
            Directory.CreateDirectory(PageSceneRoot);

            for (var page = 1; page <= PageCount; page++)
                BuildPageScene(page);

            BuildCoreScene();
            WireAddressables();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ExampleBookSetup] Built {PageCount} pages + core. Open {CoreScenePath} and press Play.");
        }

        // Pages are viewed head-on at about 1:1, so no mipmaps; compressed to keep bundles small.
        private static void ConfigureTextures()
        {
            foreach (var path in Art)
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
        }

        // A page scene has no camera and no AudioListener: the core scene owns the single instance.
        private static void BuildPageScene(int page)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var root = new GameObject(PageName(page));
            SceneManager.MoveGameObjectToScene(root, scene);

            AddSprite(root.transform, "Background", Art[0], new Vector3(0f, 0f, 0f), 0.9f, 0);
            AddSprite(root.transform, "Art_A", Art[1], new Vector3(-5.2f, -1.4f, 0f), 0.45f, 10);
            AddSprite(root.transform, "Art_B", Art[2], new Vector3(5.2f, -1.4f, 0f), 0.45f, 10);
            AddTitle(root.transform, page);

            EditorSceneManager.SaveScene(scene, PageScenePath(page));
            EditorSceneManager.CloseScene(scene, removeScene: true);
        }

        private static void AddSprite(
            Transform parent, string name, string path, Vector3 position, float scale, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * scale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            renderer.sortingOrder = order;
        }

        private static void AddTitle(Transform parent, int page)
        {
            var go = new GameObject("Title");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 3.2f, 0f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var text = go.AddComponent<TextMesh>();
            text.font = font;
            text.fontSize = 120;
            text.characterSize = 0.1f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.black;
            text.text = $"Page {page}";

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.sortingOrder = 20;

            var title = go.AddComponent<ExamplePageTitle>();
            var serialized = new SerializedObject(title);
            serialized.FindProperty("page").intValue = page;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // The core scene stays loaded for the whole book: camera, light and the Woopi host.
        private static void BuildCoreScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.white;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var lightObject = new GameObject("Global Light 2D");
            lightObject.AddComponent<Light2D>().lightType = Light2D.LightType.Global;

            // One object carries the SDK host plus this book's adapter, loader and language service.
            var hostObject = new GameObject(WoopiBookHost.HostObjectName);
            var language = hostObject.AddComponent<ExampleLanguage>();
            var loader = hostObject.AddComponent<ExampleBookLoader>();
            var adapter = hostObject.AddComponent<ExampleBookAdapter>();
            var host = hostObject.AddComponent<WoopiBookHost>();

            var adapterSo = new SerializedObject(adapter);
            adapterSo.FindProperty("loader").objectReferenceValue = loader;
            adapterSo.FindProperty("language").objectReferenceValue = language;
            adapterSo.ApplyModifiedPropertiesWithoutUndo();

            var hostSo = new SerializedObject(host);
            hostSo.FindProperty("adapterBehaviour").objectReferenceValue = adapter;
            hostSo.ApplyModifiedPropertiesWithoutUndo();

            AddDevNavigation(loader);

            EditorSceneManager.SaveScene(scene, CoreScenePath);
        }

        // Editor-only page navigation for trying the book without a host page. The "EditorOnly" tag
        // makes Unity strip it, and the assets only it uses, from every player build.
        private static void AddDevNavigation(ExampleBookLoader loader)
        {
            var settingsPath = DevNavRoot + "/DevPanelSettings.asset";
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(settingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, settingsPath);
            }

            panelSettings.themeStyleSheet =
                AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(DevNavRoot + "/DevTheme.tss");
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            EditorUtility.SetDirty(panelSettings);

            var navObject = new GameObject("DevNavigation (Editor only)") { tag = "EditorOnly" };
            var panel = navObject.AddComponent<PanelRenderer>();
            var navigation = navObject.AddComponent<ExampleDevNavigation>();

            var panelSo = new SerializedObject(panel);
            panelSo.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
            panelSo.FindProperty("sourceAsset").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DevNavRoot + "/DevNav.uxml");
            panelSo.ApplyModifiedPropertiesWithoutUndo();

            var navSo = new SerializedObject(navigation);
            navSo.FindProperty("loader").objectReferenceValue = loader;
            navSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // Art shared by every page lives in one shared group; each page group holds only its scene
        // and depends on it. An asset can sit in a single group, so shared art must not be per page.
        private static void WireAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var shared = GetOrCreateGroup(settings, SharedGroupName);
            for (var i = 0; i < Art.Length; i++)
                AddEntry(settings, shared, Art[i], Path.GetFileNameWithoutExtension(Art[i]));

            for (var page = 1; page <= PageCount; page++)
            {
                var group = GetOrCreateGroup(settings, $"Woopi Example Page {page:00}");
                AddEntry(settings, group, PageScenePath(page), PageName(page));
            }
        }

        private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string name)
        {
            var group = settings.FindGroup(name) ?? settings.CreateGroup(
                name, false, false, false, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

            var bundled = group.GetSchema<BundledAssetGroupSchema>() ?? group.AddSchema<BundledAssetGroupSchema>();
            // Local paths keep the sample self-contained; a real book points these at its CDN.
            bundled.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            bundled.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            bundled.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            // LZ4 decompresses in chunks; LZMA would stall the single WebGL thread.
            bundled.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            bundled.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.AppendHash;
            return group;
        }

        private static void AddEntry(
            AddressableAssetSettings settings, AddressableAssetGroup group, string path, string address)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"[ExampleBookSetup] Missing asset at {path}.");
                return;
            }

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
        }
    }
}
