using System.IO;
using System.Runtime.CompilerServices;

namespace Woopi.Examples.SimpleBook.Editor
{
    /// <summary>
    /// Where the sample lives in the project. The sample is imported from the Package Manager into
    /// Assets/Samples/Woopi SDK/&lt;version&gt;/Example, so paths cannot be hard-coded: they are worked out
    /// from this file's own location.
    /// </summary>
    internal static class ExamplePaths
    {
        /// <summary>Project-relative folder of the sample, e.g. Assets/Samples/Woopi SDK/0.1.0/Example.</summary>
        public static readonly string Root = ResolveRoot();

        public static string SceneRoot => Root + "/Scenes";
        public static string PageSceneRoot => SceneRoot + "/Pages";
        public static string CoreScenePath => SceneRoot + "/ExampleCore.unity";

        private static string ResolveRoot([CallerFilePath] string thisFile = "")
        {
            var path = thisFile.Replace('\\', '/');
            var assets = path.IndexOf("/Assets/", System.StringComparison.Ordinal);
            if (assets >= 0)
                path = path.Substring(assets + 1);

            // <root>/Editor/ExamplePaths.cs -> <root>
            return Path.GetDirectoryName(Path.GetDirectoryName(path)).Replace('\\', '/');
        }
    }
}
