using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;
using Woopi.Core.Editor.Localization;

namespace Woopi.Samples.Localization.Editor
{
    /// <summary>
    /// Builds the sample: English and Spanish, one string table and one asset table (a picture and a
    /// voice clip per language), and the scene. Menu: Woopi > Example > Build localization sample.
    /// Safe to re-run. Add further languages afterwards with Woopi > Localization > Add language.
    /// </summary>
    internal static class LocalizationSampleSetup
    {
        private const string StringTableName = "WoopiSample";
        private const string AssetTableName = "WoopiSampleAssets";
        private const string ProjectFolder = "Assets/Localization";

        private static readonly string Root = ResolveRoot();
        private static string ArtRoot => Root + "/Art";
        private static string TableRoot => Root + "/Tables";
        private static string ScenePath => Root + "/Scenes/LocalizationSample.unity";

        private static readonly Dictionary<string, (string title, string body)> Texts = new Dictionary<string, (string, string)>
        {
            ["en"] = ("Hello, reader!", "This text, the picture and the voice all come from the language you picked."),
            ["es"] = ("¡Hola, lector!", "Este texto, la imagen y la voz vienen del idioma que elegiste."),
        };

        private static readonly Dictionary<string, int> VoiceHertz = new Dictionary<string, int> { ["en"] = 440, ["es"] = 660 };

        [MenuItem("Woopi/Example/Build localization sample")]
        internal static void Build()
        {
            WoopiLocalizationSetup.Run();
            EnsureSettings();
            EnsureLanguages();

            // Media lives in Art/Page_01 ... Page_03: the Woopi group rules read the page from the folder name
            // and give each page and language its own Addressables group.
            var media = new Dictionary<(string code, int page), (Sprite sprite, AudioClip clip)>();
            for (var page = 1; page <= LocalizedContentDemo.PageCount; page++)
            {
                var folder = $"{ArtRoot}/Page_{page:00}";
                Directory.CreateDirectory(folder);
                media[("en", page)] = (
                    MakePicture(folder, "picture_en", new Color(0.15f, 0.35f, 0.8f), Color.white, false, page),
                    MakeVoice(folder, "voice_en", VoiceHertz["en"] + page * 110));
                media[("es", page)] = (
                    MakePicture(folder, "picture_es", new Color(0.8f, 0.15f, 0.15f), new Color(0.98f, 0.8f, 0.1f), true, page),
                    MakeVoice(folder, "voice_es", VoiceHertz["es"] + page * 110));
            }

            Directory.CreateDirectory(TableRoot);
            FillStrings();
            FillAssets(media);

            // Groups created by the steps above start with the local paths.
            WoopiLocalizationSetup.ApplyRemotePaths(AddressableAssetSettingsDefaultObject.GetSettings(true));

            BuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[LocalizationSampleSetup] Built. Open {ScenePath} and press Play; use the language buttons.");
        }

        private static void EnsureSettings()
        {
            if (LocalizationEditorSettings.ActiveLocalizationSettings != null)
                return;

            Directory.CreateDirectory(ProjectFolder);
            var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            settings.name = "Default Localization Settings";
            AssetDatabase.CreateAsset(settings, ProjectFolder + "/Localization Settings.asset");
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
        }

        private static void EnsureLanguages()
        {
            if (LocalizationEditorSettings.GetLocales().Count == 0)
            {
                Directory.CreateDirectory(ProjectFolder + "/Locales");
                var english = Locale.CreateLocale("en");
                AssetDatabase.CreateAsset(english, ProjectFolder + "/Locales/English (en).asset");
                LocalizationEditorSettings.AddLocale(english);
            }

            if (LocalizationEditorSettings.GetLocale(new LocaleIdentifier("es")) == null)
            {
                if (!WoopiAddLanguage.TryAdd("es", out var message))
                    Debug.LogError($"[LocalizationSampleSetup] {message}");
            }
        }

        private static void FillStrings()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(StringTableName)
                             ?? LocalizationEditorSettings.CreateStringTableCollection(StringTableName, TableRoot);

            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                var table = collection.GetTable(locale.Identifier) as StringTable
                            ?? collection.AddNewTable(locale.Identifier) as StringTable;

                // Only the languages the sample ships get text; any language added later falls back to English.
                if (table == null || !Texts.TryGetValue(locale.Identifier.Code, out var text))
                    continue;

                Set(table, "title", text.title);
                Set(table, "body", text.body);
                EditorUtility.SetDirty(table);
            }

            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);
        }

        private static void Set(StringTable table, string key, string value)
        {
            var entry = table.GetEntry(key);
            if (entry == null)
                table.AddEntry(key, value);
            else
                entry.Value = value;
        }

        private static void FillAssets(Dictionary<(string code, int page), (Sprite sprite, AudioClip clip)> media)
        {
            var collection = LocalizationEditorSettings.GetAssetTableCollection(AssetTableName)
                             ?? LocalizationEditorSettings.CreateAssetTableCollection(AssetTableName, TableRoot);

            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                for (var page = 1; page <= LocalizedContentDemo.PageCount; page++)
                {
                    if (!media.TryGetValue((locale.Identifier.Code, page), out var content))
                        continue;

                    collection.AddAssetToTable(locale.Identifier, $"picture_{page}", content.sprite);
                    collection.AddAssetToTable(locale.Identifier, $"voice_{page}", content.clip);
                }
            }

            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);
        }

        // --- generated art and audio, so the sample needs no binary files ---------------------------

        private static Sprite MakePicture(string folder, string name, Color main, Color accent, bool bands, int page)
        {
            const int w = 256, h = 160;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    // Spanish: horizontal bands. English: a diagonal stripe. One dark square per page number.
                    var accented = bands ? y > h / 4 && y < h * 3 / 4 : Mathf.Abs(x - y * w / h) < 18;
                    var mark = y >= h - 32 && y < h - 12 && x >= 12 && x < 12 + page * 28 && (x - 12) % 28 < 20;
                    texture.SetPixel(x, y, mark ? new Color(0.1f, 0.12f, 0.18f) : accented ? accent : main);
                }
            }

            var path = $"{folder}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static AudioClip MakeVoice(string folder, string name, int hertz)
        {
            const int rate = 22050;
            var samples = rate / 2;
            var path = $"{folder}/{name}.wav";

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + samples * 2);
                writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(rate);
                writer.Write(rate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(samples * 2);

                for (var i = 0; i < samples; i++)
                {
                    var fade = Mathf.Min(1f, Mathf.Min(i, samples - i) / (rate * 0.05f));
                    writer.Write((short)(Mathf.Sin(2f * Mathf.PI * hertz * i / rate) * 0.4f * fade * short.MaxValue));
                }

                writer.Flush();
                File.WriteAllBytes(path, stream.ToArray());
            }

            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // --- scene ---------------------------------------------------------------------------------

        private static void BuildScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.93f, 0.94f, 0.96f);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            var content = NewUi("Content", canvasObject.transform);
            Stretch(content);
            var column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.childAlignment = TextAnchor.MiddleCenter;
            column.spacing = 20;
            column.childControlWidth = false;
            column.childControlHeight = false;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;

            var titleText = MakeText(content, "Title", 54, FontStyle.Bold, 900, 80);
            var picture = NewUi("Picture", content);
            picture.sizeDelta = new Vector2(320, 200);
            var pictureImage = picture.gameObject.AddComponent<Image>();
            pictureImage.preserveAspect = true;
            var bodyText = MakeText(content, "Body", 28, FontStyle.Normal, 900, 90);
            var play = MakeButton(content, "Play voice", "Play voice", 220, 56);

            var pager = NewUi("Pager", content);
            pager.sizeDelta = new Vector2(900, 60);
            var pagerLayout = pager.gameObject.AddComponent<HorizontalLayoutGroup>();
            pagerLayout.childAlignment = TextAnchor.MiddleCenter;
            pagerLayout.spacing = 12;
            pagerLayout.childControlWidth = false;
            pagerLayout.childControlHeight = false;
            pagerLayout.childForceExpandWidth = false;
            pagerLayout.childForceExpandHeight = false;
            var previous = MakeButton(pager, "Previous", "<", 80, 56);
            var pageText = MakeText(pager, "Page", 28, FontStyle.Normal, 120, 56);
            var next = MakeButton(pager, "Next", ">", 80, 56);

            var row = NewUi("Languages", content);
            row.sizeDelta = new Vector2(900, 60);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.spacing = 12;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            var template = MakeButton(row, "LanguageButtonTemplate", "Language", 200, 56);

            var demo = canvasObject.AddComponent<LocalizedContentDemo>();
            var source = canvasObject.AddComponent<AudioSource>();
            source.playOnAwake = false;

            SetField(demo, "titleText", titleText);
            SetField(demo, "bodyText", bodyText);
            SetField(demo, "pictureImage", pictureImage);
            SetField(demo, "audioSource", source);
            GetField<LocalizedString>(demo, "title").SetReference(StringTableName, "title");
            GetField<LocalizedString>(demo, "body").SetReference(StringTableName, "body");
            SetField(demo, "pageText", pageText);
            GetField<LocalizedSprite>(demo, "picture").SetReference(AssetTableName, "picture_1");
            GetField<LocalizedAudioClip>(demo, "voice").SetReference(AssetTableName, "voice_1");
            UnityEventTools.AddVoidPersistentListener(play.onClick, demo.PlayVoice);
            UnityEventTools.AddVoidPersistentListener(previous.onClick, demo.PreviousPage);
            UnityEventTools.AddVoidPersistentListener(next.onClick, demo.NextPage);

            var menu = canvasObject.AddComponent<LanguageMenu>();
            SetField(menu, "template", template);
            SetField(menu, "container", row);

            EditorUtility.SetDirty(demo);
            EditorUtility.SetDirty(menu);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static RectTransform NewUi(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Text MakeText(Transform parent, string name, int size, FontStyle style, float width, float height)
        {
            var rect = NewUi(name, parent);
            rect.sizeDelta = new Vector2(width, height);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.1f, 0.12f, 0.18f);
            text.raycastTarget = false;
            return text;
        }

        private static Button MakeButton(Transform parent, string name, string label, float width, float height)
        {
            var rect = NewUi(name, parent);
            rect.sizeDelta = new Vector2(width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.2f, 0.4f, 0.85f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = MakeText(rect, "Label", 26, FontStyle.Normal, width, height);
            text.color = Color.white;
            text.text = label;
            Stretch(text.rectTransform);
            return button;
        }

        private static T GetField<T>(object target, string name) where T : class =>
            (T)Field(target, name).GetValue(target);

        private static void SetField(object target, string name, object value) =>
            Field(target, name).SetValue(target, value);

        private static FieldInfo Field(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new MissingFieldException(target.GetType().Name, name);

        /// <summary>The sample is imported to Assets/Samples/Woopi SDK/&lt;version&gt;/Localization, so the root is worked out from this file.</summary>
        private static string ResolveRoot([CallerFilePath] string thisFile = "")
        {
            var path = thisFile.Replace('\\', '/');
            var assets = path.IndexOf("/Assets/", StringComparison.Ordinal);
            if (assets >= 0)
                path = path.Substring(assets + 1);

            // <root>/Editor/LocalizationSampleSetup.cs -> <root>
            return Path.GetDirectoryName(Path.GetDirectoryName(path)).Replace('\\', '/');
        }
    }
}
