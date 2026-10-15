using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Woopi.Core.Editor
{
    /// <summary>
    /// Warns when <see cref="WoopiSdk.Version"/> (reported to the host page) differs from the version
    /// in package.json, so a release is never tagged with a stale constant.
    /// </summary>
    [InitializeOnLoad]
    internal static class WoopiVersionCheck
    {
        static WoopiVersionCheck()
        {
            var package = PackageInfo.FindForAssembly(typeof(WoopiSdk).Assembly);
            if (package != null && package.version != WoopiSdk.Version)
            {
                Debug.LogWarning(
                    $"[Woopi SDK] WoopiSdk.Version is '{WoopiSdk.Version}' but package.json says " +
                    $"'{package.version}'. Update Runtime/WoopiSdk.cs before tagging a release.");
            }
        }
    }
}
