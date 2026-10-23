using UnityEngine;
using UnityEngine.UIElements;
using Woopi.Core.UI;

namespace Woopi.Core.SafeZone
{
    /// <summary>
    /// Draws the page-turn edge zones, the reader dock's bottom hotzone and the safe area between them over the Game view, so you can see while
    /// designing where double taps turn the page (see <see cref="WoopiSafeZone"/>). It also shows while editing, not
    /// only in Play mode.
    ///
    /// A design aid for the Editor only: add it with GameObject > Woopi > Safe Zone Overlay. That creates the object
    /// tagged "EditorOnly", so Unity strips it, and the assets only it uses, from every player build. Keep the tag. For a
    /// version that ships in builds, make your own copy of this script and its UXML and USS.
    ///
    /// The zones are measured against the whole screen (the browser canvas), which is the camera's view unless the book
    /// letterboxes its camera.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Woopi/Safe Zone Overlay")]
    public sealed class WoopiSafeZoneOverlay : WoopiPanelBehaviour
    {
        [Tooltip("Show the zones.")]
        [SerializeField] private bool visible = true;

        [Tooltip("Draw order of the overlay's panel. Raise it to sit above the book's own UI.")]
        [SerializeField] private int sortingOrder = 1000;

        private VisualElement _zoneLeft;
        private VisualElement _zoneRight;
        private VisualElement _dockZone;

        /// <summary>Shows or hides the overlay.</summary>
        public bool Visible
        {
            get => visible;
            set
            {
                visible = value;
                ApplyVisibility();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ApplySortingOrder();
        }

        protected override void OnPanelReady(VisualElement root)
        {
            _zoneLeft = root.Q("zoneLeft");
            _zoneRight = root.Q("zoneRight");
            _dockZone = root.Q("dockZone");
            root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            ApplyVisibility();
            LayoutZones(root.resolvedStyle.width);
        }

        protected override void OnPanelLost()
        {
            PanelRoot?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _zoneLeft = null;
            _zoneRight = null;
            _dockZone = null;
        }

        // The panel's width is the Game view's width in the Editor and the canvas in a build, in screen pixels
        // (the panel settings scale one to one), so it also follows the Game view being resized while editing.
        private void OnGeometryChanged(GeometryChangedEvent evt) => LayoutZones(evt.newRect.width);

        private void LayoutZones(float panelWidth)
        {
            if (_zoneLeft == null || _zoneRight == null || float.IsNaN(panelWidth) || panelWidth <= 0f)
                return;

            var zone = WoopiSafeZone.GetEdgeZoneWidth(panelWidth);
            _zoneLeft.style.width = zone;
            _zoneRight.style.width = zone;

            if (_dockZone != null)
                _dockZone.style.height = WoopiSafeZone.GetDockHotzoneHeight();
        }

        private void ApplyVisibility()
        {
            if (PanelRoot != null)
                PanelRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplySortingOrder()
        {
#if UNITY_6000_5_OR_NEWER
            GetComponent<PanelRenderer>().sortingOrder = sortingOrder;
#else
            GetComponent<UIDocument>().sortingOrder = sortingOrder;
#endif
        }

#if UNITY_EDITOR
        private const string AssetFolder = "Packages/com.woopi.sdk/Runtime/SafeZone/UI/";

        private void OnValidate()
        {
            ApplyVisibility();
            if (isActiveAndEnabled)
                ApplySortingOrder();
        }

        // Runs when the component is added with the Inspector's Add Component button (and from its Reset menu).
        private void Reset() => UnityEditor.EditorApplication.delayCall += SetUpInEditor;

        /// <summary>
        /// Editor only. Tags the object "EditorOnly" so player builds strip it, and fills in the panel's layout and
        /// settings so it shows in the Game view without Play mode. The GameObject menu calls it; Reset does too.
        /// </summary>
        public void SetUpInEditor()
        {
            if (this == null)
                return;

            gameObject.tag = "EditorOnly";

            BindPanel();
            var panel = GetComponent(UsesPanelRenderer ? typeof(PanelRenderer) : typeof(UIDocument));
            var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetFolder + "WoopiSafeZonePanelSettings.asset");
            var tree = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetFolder + "WoopiSafeZone.uxml");
            if (panel == null || settings == null || tree == null)
            {
                Debug.LogWarning("[Woopi] Safe Zone Overlay: could not find its panel settings or UXML under " + AssetFolder);
                return;
            }

            var so = new UnityEditor.SerializedObject(panel);
            so.FindProperty("m_PanelSettings").objectReferenceValue = settings;
            so.FindProperty("sourceAsset").objectReferenceValue = tree;
            so.ApplyModifiedProperties();
        }
#endif
    }
}
