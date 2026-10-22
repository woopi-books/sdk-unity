using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Woopi.Examples.SimpleBook.Editor
{
    /// <summary>
    /// When a page scene of the sample is opened on its own, opens ExampleCore alongside it, so
    /// pressing Play starts the sample book (host, camera, loader) on that page. Page scenes have no
    /// camera of their own. It also checks right before entering Play mode, in case the scene was
    /// opened while the Editor was busy.
    /// </summary>
    [InitializeOnLoad]
    internal static class ExampleCoreEditModeLoader
    {
        private static string CoreScenePath => ExamplePaths.CoreScenePath;
        private static string PageScenePrefix => ExamplePaths.PageSceneRoot + "/";

        static ExampleCoreEditModeLoader()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            // React only to a scene swap, never to our own additive open.
            if (mode != OpenSceneMode.Single || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!scene.path.StartsWith(PageScenePrefix))
                return;

            // The open has to wait until Unity finishes settling the new scene setup.
            EditorApplication.delayCall += EnsureCoreOpen;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                EnsureCoreOpen();
        }

        private static void EnsureCoreOpen()
        {
            if (!SamplePageIsOpen() || IsOpen(CoreScenePath))
                return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CoreScenePath) == null)
                return;

            EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Additive);
        }

        private static bool SamplePageIsOpen()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).path.StartsWith(PageScenePrefix))
                    return true;
            }

            return false;
        }

        private static bool IsOpen(string path)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).path == path)
                    return true;
            }

            return false;
        }
    }
}
