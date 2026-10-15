using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Woopi.Core.Addressables;

namespace Woopi.Core.Editor.Addressables
{
    /// <summary>
    /// Sets the project's Addressables up for Woopi hosting: remote groups load from the placeholder
    /// URL, which the platform replaces at runtime through the <c>content</c> start argument
    /// (see <see cref="WoopiContentUrl"/>). Menu: Woopi > Addressables > Use content placeholder.
    /// </summary>
    internal static class WoopiAddressablesSetup
    {
        [MenuItem("Woopi/Addressables/Use content placeholder")]
        internal static void UsePlaceholder()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("[Woopi] Could not create Addressables settings.");
                return;
            }

            // The remote catalog is what lets the platform publish content updates without a new player.
            settings.BuildRemoteCatalog = true;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);

            settings.profileSettings.SetValue(
                settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath, WoopiContentUrl.LoadPath);

            // Keeps downloaded bundles across page reloads.
            PlayerSettings.WebGL.dataCaching = true;

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Woopi] Remote load path is now '{WoopiContentUrl.LoadPath}'. Point your page groups at " +
                      "the remote build and load paths, build the content, and upload the build output folder. " +
                      "The platform supplies the real base URL at runtime.");
        }
    }
}
