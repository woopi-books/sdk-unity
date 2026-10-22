using UnityEditor;
using UnityEngine;
using Woopi.Core.SafeZone;

namespace Woopi.Core.Editor
{
    /// <summary>
    /// GameObject menu entry that adds the safe zone overlay to the open scene. The overlay sets itself up (EditorOnly
    /// tag, panel settings, layout) here, and also when the component is added with the Inspector.
    /// </summary>
    internal static class SafeZoneMenu
    {
        [MenuItem("GameObject/Woopi/Safe Zone Overlay", false, 10)]
        private static void Create(MenuCommand command)
        {
            var go = new GameObject("Woopi Safe Zone Overlay (Editor only)");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            go.AddComponent<WoopiSafeZoneOverlay>().SetUpInEditor();
            Undo.RegisterCreatedObjectUndo(go, "Create Woopi Safe Zone Overlay");
            Selection.activeGameObject = go;
        }
    }
}
