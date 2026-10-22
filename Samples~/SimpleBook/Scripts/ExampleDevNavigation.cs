using UnityEngine;
using UnityEngine.UIElements;

namespace Woopi.Examples.SimpleBook
{
    /// <summary>
    /// DEVELOPMENT TOOL, EDITOR ONLY. A previous/next/jump panel for trying the book in the Editor
    /// without a host page. The platform provides the real controls and drives the book through the
    /// Woopi host, so this must never ship: its GameObject is tagged "EditorOnly", which makes Unity
    /// strip it, and the assets only it references, from every player build. Keep that tag on it.
    ///
    /// It talks to the book's loader directly. Use <see cref="Woopi.Core.Book.WoopiBookHost"/>
    /// commands (SendMessage) when you want to exercise the host contract itself.
    ///
    /// The panel's root element arrives through a reload callback that can fire more than once, so the
    /// element wiring is rebuilt every time.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public sealed class ExampleDevNavigation : MonoBehaviour
    {
        [SerializeField] private ExampleBookLoader loader;

        private PanelRenderer _renderer;
        private int _uiVersion = -1;
        private Button _prevButton;
        private Button _nextButton;
        private Button _jumpButton;
        private Label _pageLabel;
        private IntegerField _jumpField;

        private void OnEnable()
        {
            if (loader == null)
                loader = FindAnyObjectByType<ExampleBookLoader>();

            _renderer = GetComponent<PanelRenderer>();
            _renderer.RegisterUIReloadCallback((PanelRenderer.VersionedUIReloadCallback)OnUIReload);

            if (loader == null)
                return;

            loader.PageChanged += OnPageChanged;
            loader.LoadingChanged += OnLoadingChanged;
        }

        private void OnDisable()
        {
            if (_renderer != null)
                _renderer.UnregisterUIReloadCallback((PanelRenderer.VersionedUIReloadCallback)OnUIReload);

            _uiVersion = -1;
            DetachElements();

            if (loader == null)
                return;

            loader.PageChanged -= OnPageChanged;
            loader.LoadingChanged -= OnLoadingChanged;
        }

        private void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            if (version == _uiVersion)
                return;

            _uiVersion = version;
            DetachElements();

            _prevButton = rootElement.Q<Button>("prevButton");
            _nextButton = rootElement.Q<Button>("nextButton");
            _jumpButton = rootElement.Q<Button>("jumpButton");
            _pageLabel = rootElement.Q<Label>("pageLabel");
            _jumpField = rootElement.Q<IntegerField>("jumpField");

            _prevButton.clicked += OnPrevious;
            _nextButton.clicked += OnNext;
            _jumpButton.clicked += OnJump;
            _jumpField.RegisterCallback<KeyDownEvent>(OnJumpFieldKeyDown);

            Refresh();
        }

        private void DetachElements()
        {
            if (_prevButton != null) _prevButton.clicked -= OnPrevious;
            if (_nextButton != null) _nextButton.clicked -= OnNext;
            if (_jumpButton != null) _jumpButton.clicked -= OnJump;
            _jumpField?.UnregisterCallback<KeyDownEvent>(OnJumpFieldKeyDown);

            _prevButton = null;
            _nextButton = null;
            _jumpButton = null;
            _pageLabel = null;
            _jumpField = null;
        }

        private void OnPrevious() => loader?.Previous();
        private void OnNext() => loader?.Next();

        private void OnJump()
        {
            if (loader == null || _jumpField == null)
                return;

            var page = Mathf.Clamp(_jumpField.value, 1, loader.PageCount);
            _jumpField.SetValueWithoutNotify(page);
            loader.GoTo(page);
        }

        private void OnJumpFieldKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                return;

            OnJump();
            evt.StopPropagation();
        }

        private void OnPageChanged(int page) => Refresh();
        private void OnLoadingChanged(bool loading) => Refresh();

        private void Refresh()
        {
            // The loader can raise events before the panel has handed over its tree.
            if (loader == null || _pageLabel == null)
                return;

            var page = loader.CurrentPage;
            _pageLabel.text = $"{page} / {loader.PageCount}";
            _jumpField.SetValueWithoutNotify(Mathf.Max(page, 1));

            // Lock everything while a page is being fetched.
            _prevButton.SetEnabled(!loader.IsLoading && page > 1);
            _nextButton.SetEnabled(!loader.IsLoading && page < loader.PageCount);
            _jumpButton.SetEnabled(!loader.IsLoading);
            _jumpField.SetEnabled(!loader.IsLoading);
        }
    }
}
