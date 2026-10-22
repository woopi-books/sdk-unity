using UnityEngine;
using UnityEngine.UIElements;

namespace Woopi.Core.UI
{
    /// <summary>
    /// Base class for scripts that drive a runtime UI Toolkit panel on their own GameObject, on every Unity version
    /// the SDK supports. Unity 6.5 added <c>PanelRenderer</c> as the replacement for <c>UIDocument</c>; this uses
    /// <c>PanelRenderer</c> where it exists (chosen at compile time with <c>UNITY_6000_5_OR_NEWER</c>) and <c>UIDocument</c> before that, so the same script compiles and works on both.
    ///
    /// Override <see cref="OnPanelReady"/> to find and wire your elements. The panel can hand over a new root more than
    /// once (asset reloads, enable / disable), so it is called every time and <see cref="OnPanelLost"/> before the
    /// next one: do all element wiring there and undo it there.
    ///
    /// Do not define <c>LateUpdate</c> in a derived class without calling <c>base.LateUpdate()</c>: on UIDocument it
    /// is how the new root is noticed.
    /// </summary>
#if UNITY_6000_5_OR_NEWER
    [RequireComponent(typeof(PanelRenderer))]
#else
    [RequireComponent(typeof(UIDocument))]
#endif
    public abstract class WoopiPanelBehaviour : MonoBehaviour
    {
#if UNITY_6000_5_OR_NEWER
        private PanelRenderer _panel;
        private int _version = -1;
#else
        private UIDocument _panel;
#endif
        private VisualElement _root;

        /// <summary>True when this Unity version uses PanelRenderer, false when it uses UIDocument.</summary>
        public static bool UsesPanelRenderer =>
#if UNITY_6000_5_OR_NEWER
            true;
#else
            false;
#endif

        /// <summary>The panel's current root element, or null before the panel is ready.</summary>
        protected VisualElement PanelRoot => _root;

        /// <summary>Panel settings (theme, scaling) of the panel on this GameObject.</summary>
        protected PanelSettings PanelSettings
        {
            get => _panel != null ? _panel.panelSettings : null;
            set { if (_panel != null) _panel.panelSettings = value; }
        }

        /// <summary>The UXML shown by the panel on this GameObject.</summary>
        protected VisualTreeAsset VisualTree
        {
            get => _panel != null ? _panel.visualTreeAsset : null;
            set { if (_panel != null) _panel.visualTreeAsset = value; }
        }

        /// <summary>
        /// Adds the right panel component for this Unity version (PanelRenderer or UIDocument) to a GameObject. For editor
        /// scripts that build scenes. Its panel settings and UXML are the serialized fields
        /// <c>m_PanelSettings</c> and <c>sourceAsset</c>, the same on both components.
        /// </summary>
        public static Component AddPanelComponent(GameObject target)
        {
#if UNITY_6000_5_OR_NEWER
            return target.AddComponent<PanelRenderer>();
#else
            return target.AddComponent<UIDocument>();
#endif
        }

        /// <summary>
        /// Call from a derived Awake, before anything uses the panel. Adds the panel component if the GameObject does not
        /// have one for this Unity version, e.g. a scene saved on an older or newer Unity that carries the other one.
        /// </summary>
        protected void BindPanel()
        {
            if (_panel != null)
                return;

#if UNITY_6000_5_OR_NEWER
            _panel = GetComponent<PanelRenderer>();
            if (_panel == null)
                _panel = gameObject.AddComponent<PanelRenderer>();
#else
            _panel = GetComponent<UIDocument>();
            if (_panel == null)
                _panel = gameObject.AddComponent<UIDocument>();
#endif
        }

        protected virtual void OnEnable()
        {
            BindPanel();
#if UNITY_6000_5_OR_NEWER
            _panel.RegisterUIReloadCallback((PanelRenderer.VersionedUIReloadCallback)OnUIReload);
#endif
        }

        protected virtual void OnDisable()
        {
#if UNITY_6000_5_OR_NEWER
            if (_panel != null)
                _panel.UnregisterUIReloadCallback((PanelRenderer.VersionedUIReloadCallback)OnUIReload);
            _version = -1;
#endif
            SetRoot(null);
        }

#if UNITY_6000_5_OR_NEWER
        private void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            if (version == _version)
                return;

            _version = version;
            SetRoot(rootElement);
        }
#else
        // UIDocument has no reload callback: it builds a new root element whenever it rebuilds, so watch for it.
        protected virtual void LateUpdate()
        {
            if (_panel != null && _panel.rootVisualElement != _root)
                SetRoot(_panel.rootVisualElement);
        }
#endif

        private void SetRoot(VisualElement root)
        {
            if (root == _root)
                return;

            if (_root != null)
                OnPanelLost();

            _root = root;

            if (_root != null)
                OnPanelReady(_root);
        }

        /// <summary>The panel handed over a root element: find your elements and wire them.</summary>
        protected abstract void OnPanelReady(VisualElement root);

        /// <summary>The root is going away or being replaced: unwire your elements and forget them.</summary>
        protected abstract void OnPanelLost();
    }
}
