using System.IO;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace Woopi.Core.Editor
{
    /// <summary>
    /// Unity only picks WebGL templates up from Assets/WebGLTemplates, but the Woopi template ships
    /// inside the package (WebGLTemplate~, which Unity does not import). This copies it to
    /// Assets/WebGLTemplates/Woopi when it differs, and selects it for WebGL builds when no template
    /// has been chosen yet. Never edit the copy in Assets: it is overwritten from the package.
    /// </summary>
    [InitializeOnLoad]
    public static class WebGLTemplateSync
    {
        private const string TargetDir = "Assets/WebGLTemplates/Woopi";
        private const string TemplateId = "PROJECT:Woopi";

        static WebGLTemplateSync() => EditorApplication.delayCall += () => Sync(false);

        [MenuItem("Woopi/WebGL/Sync template")]
        private static void SyncMenu() => Sync(true);

        private static void Sync(bool log)
        {
            var package = PackageInfo.FindForAssembly(typeof(WebGLTemplateSync).Assembly);
            if (package == null)
                return;

            var sourceDir = Path.Combine(package.resolvedPath, "WebGLTemplate~");
            if (!Directory.Exists(sourceDir))
                return;

            var changed = false;
            foreach (var source in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(sourceDir, source);
                var target = Path.Combine(TargetDir, relative);
                if (File.Exists(target) && File.ReadAllText(target) == File.ReadAllText(source))
                    continue;

                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(source, target, true);
                changed = true;
            }

            if (changed)
                AssetDatabase.Refresh();

            if (string.IsNullOrEmpty(PlayerSettings.WebGL.template)
                || PlayerSettings.WebGL.template == "APPLICATION:Default")
                PlayerSettings.WebGL.template = TemplateId;

            if (log || changed)
                Debug.Log($"[Woopi] WebGL template {(changed ? "synced to" : "already up to date in")} {TargetDir}.");
        }
    }
}
