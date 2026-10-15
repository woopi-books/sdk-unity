using UnityEngine;

namespace Woopi.Examples.SimpleBook
{
    /// <summary>
    /// A page title that follows the book's language. Lives in a page scene, finds the language
    /// service in the core scene, and refreshes when the language changes or the page is shown.
    /// </summary>
    [RequireComponent(typeof(TextMesh))]
    public sealed class ExamplePageTitle : MonoBehaviour
    {
        [SerializeField, Min(1)] private int page = 1;

        private TextMesh _text;
        private ExampleLanguage _language;

        private void OnEnable()
        {
            _text = GetComponent<TextMesh>();
            _language = FindAnyObjectByType<ExampleLanguage>();
            if (_language == null)
                return;

            _language.Changed += OnLanguageChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (_language != null)
                _language.Changed -= OnLanguageChanged;
        }

        private void OnLanguageChanged(string language) => Refresh();

        private void Refresh() => _text.text = _language.PageTitle(page);
    }
}
