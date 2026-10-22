using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization.Addressables;

namespace Woopi.Core.Editor.Localization
{
    /// <summary>
    /// Keeps groups created after Set up remote localization remote. Unity Localization creates a group the
    /// first time content for a new page or language is added (a French image in Page_04, say), and it starts
    /// with the local paths, which would put that content inside the player. Only acts in projects that ran
    /// Set up remote localization (their rules use <see cref="WoopiPageGroupResolver"/>).
    /// </summary>
    [InitializeOnLoad]
    internal static class WoopiLocalizationGroupWatcher
    {
        static WoopiLocalizationGroupWatcher()
        {
            AddressableAssetSettings.OnModificationGlobal += OnModification;
        }

        private static void OnModification(AddressableAssetSettings settings, AddressableAssetSettings.ModificationEvent evt, object data)
        {
            if (evt != AddressableAssetSettings.ModificationEvent.GroupAdded)
                return;

            if (!(data is AddressableAssetGroup group) || !WoopiLocalizationSetup.IsLocalizationGroup(group))
                return;

            // Schemas are still being added while the event fires.
            EditorApplication.delayCall += () =>
            {
                if (settings != null && AddressableGroupRules.Instance.AssetResolver is WoopiPageGroupResolver)
                    WoopiLocalizationSetup.ApplyRemotePaths(settings);
            };
        }
    }
}
