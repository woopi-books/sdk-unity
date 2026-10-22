using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using Woopi.Core.Localization;

namespace Woopi.Samples.Localization
{
    /// <summary>
    /// The three kinds of localized content a book has: text, an image and an audio clip. Each is a
    /// Localized reference (table + entry), so nothing here knows which languages exist: when the
    /// selected language changes, or a new language arrives from a language pack, the references
    /// resolve to that language's content and the handlers below run.
    ///
    /// The book has <see cref="PageCount"/> pages. Text is preloaded with the book; each page's image and
    /// voice sit in their own Addressables group per language. Turning to a page first downloads it through
    /// <see cref="WoopiPageContent"/> and only then points the image and voice at it (a page must not be
    /// downloaded ahead of time and loaded for use at the same moment, or WebGL can abort one of the two).
    /// Once it shows, the neighbouring pages are downloaded in the background.
    /// </summary>
    [AddComponentMenu("Woopi/Samples/Localized Content Demo")]
    public sealed class LocalizedContentDemo : MonoBehaviour
    {
        public const int PageCount = 3;

        [SerializeField] private LocalizedString title = new LocalizedString();
        [SerializeField] private LocalizedString body = new LocalizedString();
        [SerializeField] private LocalizedSprite picture = new LocalizedSprite();
        [SerializeField] private LocalizedAudioClip voice = new LocalizedAudioClip();

        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text pageText;
        [SerializeField] private Image pictureImage;
        [SerializeField] private AudioSource audioSource;

        private AudioClip _voiceClip;
        private int _page = 1;
        private bool _mediaSubscribed;

        private void OnEnable()
        {
            // Text is preloaded with the book, so it can follow the language straight away.
            title.StringChanged += OnTitle;
            body.StringChanged += OnBody;
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

            StartCoroutine(ShowPage(_page));
        }

        private void OnDisable()
        {
            title.StringChanged -= OnTitle;
            body.StringChanged -= OnBody;
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

            if (_mediaSubscribed)
            {
                _mediaSubscribed = false;
                picture.AssetChanged -= OnPicture;
                voice.AssetChanged -= OnVoice;
            }
        }

        public void NextPage() => StartCoroutine(ShowPage(Mathf.Min(PageCount, _page + 1)));
        public void PreviousPage() => StartCoroutine(ShowPage(Mathf.Max(1, _page - 1)));

        /// <summary>Plays the voice clip of the current page and language. Hooked to the button.</summary>
        public void PlayVoice()
        {
            if (_voiceClip == null)
                return;

            audioSource.Stop();
            audioSource.clip = _voiceClip;
            audioSource.Play();
        }

        private IEnumerator ShowPage(int page)
        {
            _page = page;
            pageText.text = $"{page} / {PageCount}";

            // Download the page first; pointing the references at it starts the real load.
            yield return WoopiPageContent.PreloadPage(page);
            if (_page != page)
                yield break; // the reader has already moved on

            if (!_mediaSubscribed)
            {
                _mediaSubscribed = true;
                picture.AssetChanged += OnPicture;
                voice.AssetChanged += OnVoice;
            }

            // The entries of one page are named by its number; the same keys exist in every language.
            picture.SetReference(picture.TableReference, $"picture_{page}");
            voice.SetReference(voice.TableReference, $"voice_{page}");

            yield return WoopiPageContent.PreloadNeighbours(page);
        }

        // New language: Unity Localization is already loading the current page in it, so fetch only the neighbours.
        private void OnLocaleChanged(Locale locale) => StartCoroutine(WoopiPageContent.PreloadNeighbours(_page, 1, locale));

        private void OnTitle(string value) => titleText.text = value;
        private void OnBody(string value) => bodyText.text = value;
        private void OnPicture(Sprite value) => pictureImage.sprite = value;
        private void OnVoice(AudioClip value) => _voiceClip = value;
    }
}
