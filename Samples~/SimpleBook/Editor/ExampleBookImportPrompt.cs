using System.IO;
using UnityEditor;
using UnityEngine;

namespace Woopi.Examples.SimpleBook.Editor
{
    /// <summary>
    /// The sample ships without its scenes and Addressables groups, which Woopi > Example > Build sample book
    /// generates. The first time the Editor starts with the sample in the project and no scenes, this offers
    /// to run it, so importing the sample is enough. "Not now" asks again next session; "Don't ask again"
    /// stops for this sample. The menu item and the README step stay as the fallback.
    /// </summary>
    [InitializeOnLoad]
    internal static class ExampleBookImportPrompt
    {
        private static readonly string SkipKey = "Woopi.Examples.SimpleBook.SkipImportPrompt." + ExamplePaths.Root;
        private static readonly string AskedKey = "Woopi.Examples.SimpleBook.ImportPromptAsked." + ExamplePaths.Root;

        static ExampleBookImportPrompt()
        {
            EditorApplication.update += Check;
        }

        private static void Check()
        {
            // Wait until the Editor is idle: a dialog during an import or a compile is lost or misleading.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorApplication.update -= Check;

            if (Application.isBatchMode || EditorPrefs.GetBool(SkipKey, false) || SessionState.GetBool(AskedKey, false))
                return;

            if (File.Exists(ExamplePaths.CoreScenePath))
                return;

            SessionState.SetBool(AskedKey, true);
            var answer = EditorUtility.DisplayDialogComplex(
                "Woopi example book",
                "The example book's scenes and Addressables groups have not been built in this project yet. " +
                "Build them now? You can do it later with Woopi > Example > Build sample book.",
                "Build now", "Not now", "Don't ask again");

            switch (answer)
            {
                case 0:
                    ExampleBookSetup.Build();
                    break;
                case 2:
                    EditorPrefs.SetBool(SkipKey, true);
                    break;
            }
        }
    }
}
