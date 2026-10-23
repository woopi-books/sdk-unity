using System.IO;
using UnityEditor;
using UnityEngine;

namespace Woopi.Samples.Localization.Editor
{
    /// <summary>
    /// The sample ships without its tables, art and scene, which Woopi > Example > Build localization sample
    /// generates. The first time the Editor starts with the sample in the project and no scene, this offers to
    /// run it, so importing the sample is enough. "Not now" asks again next session; "Don't ask again" stops
    /// for this sample. The menu item and the README step stay as the fallback.
    /// </summary>
    [InitializeOnLoad]
    internal static class LocalizationSampleImportPrompt
    {
        private static readonly string SkipKey = "Woopi.Samples.Localization.SkipImportPrompt." + LocalizationSampleSetup.ScenePath;
        private static readonly string AskedKey = "Woopi.Samples.Localization.ImportPromptAsked." + LocalizationSampleSetup.ScenePath;

        static LocalizationSampleImportPrompt()
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

            if (File.Exists(LocalizationSampleSetup.ScenePath))
                return;

            SessionState.SetBool(AskedKey, true);
            var answer = EditorUtility.DisplayDialogComplex(
                "Woopi localization sample",
                "The localization sample's tables, media and scene have not been built in this project yet. " +
                "Build them now? This also sets up remote localization (Addressables groups and rules). " +
                "You can do it later with Woopi > Example > Build localization sample.",
                "Build now", "Not now", "Don't ask again");

            switch (answer)
            {
                case 0:
                    LocalizationSampleSetup.Build();
                    break;
                case 2:
                    EditorPrefs.SetBool(SkipKey, true);
                    break;
            }
        }
    }
}
