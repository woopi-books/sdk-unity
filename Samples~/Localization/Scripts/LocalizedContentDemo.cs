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
    /// voice sit in their own Addressables group per language, so turning to a page asks
    /// <see cref="WoopiPageContent"/> to download that page and its neighbours in the current language.
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

        private void OnEnable()
        {
            // Subscribing starts the load; the handlers also run again on every language change.
            title.StringChanged += OnTitle;
            body.StringChanged += OnBody;
            picture.AssetChanged += OnPicture;
            voice.AssetChanged += OnVoice;
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            ShowPage(_page);
        }

        private void OnDisable()
        {
            title.StringChanged -= OnTitle;
            body.StringChanged -= OnBody;
            picture.AssetChanged -= OnPicture;
            voice.AssetChanged -= OnVoice;
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        public void NextPage() => ShowPage(Mathf.Min(PageCount, _page + 1));
        public void PreviousPage() => ShowPage(Mathf.Max(1, _page - 1));

        /// <summary>Plays the voice clip of the current page and language. Hooked to the button.</summary>
        public void PlayVoice()
        {
            if (_voiceClip == null)
                return;

            audioSource.Stop();
            audioSource.clip = _voiceClip;
            audioSource.Play();
        }

        private void ShowPage(int page)
        {
            _page = page;
            pageText.text = $"{page} / {PageCount}";

            // The entries of one page are named by its number; the same keys exist in every language.
            picture.SetReference(picture.TableReference, $"picture_{page}");
            voice.SetReference(voice.TableReference, $"voice_{page}");
            Preload();
        }

        // New language: the current page and its neighbours must be fetched in it.
        private void OnLocaleChanged(Locale locale) => Preload();

        private void Preload()
        {
            StopAllCoroutines();
            StartCoroutine(WoopiPageContent.PreloadAround(_page));
        }

        private void OnTitle(string value) => titleText.text = value;
        private void OnBody(string value) => bodyText.text = value;
        private void OnPicture(Sprite value) => pictureImage.sprite = value;
        private void OnVoice(AudioClip value) => _voiceClip = value;
    }
}
