namespace Woopi.Core.Book
{
    /// <summary>
    /// What the host can tell a book at startup. Anything left empty means "the book's default":
    /// its own default language, page 1, and no injected content location.
    /// </summary>
    public readonly struct BookStartArgs
    {
        /// <summary>Language code such as "es" or "en-US", or null for the book's default.</summary>
        public string Language { get; }

        /// <summary>Opening page, 1-based, or 0 for the start of the book.</summary>
        public int StartPage { get; }

        /// <summary>
        /// Base URL the book's Addressables content is hosted at, or null. Applied by
        /// WoopiContentUrl (Woopi.Core.Addressables) before the first load.
        /// </summary>
        public string ContentUrl { get; }

        public bool HasLanguage => !string.IsNullOrWhiteSpace(Language);
        public bool HasStartPage => StartPage > 0;
        public bool HasContentUrl => !string.IsNullOrWhiteSpace(ContentUrl);

        public BookStartArgs(string language, int startPage, string contentUrl = null)
        {
            Language = string.IsNullOrWhiteSpace(language) ? null : language.Trim();
            StartPage = startPage > 0 ? startPage : 0;
            ContentUrl = string.IsNullOrWhiteSpace(contentUrl) ? null : contentUrl.Trim();
        }

        public override string ToString() =>
            $"lang={(HasLanguage ? Language : "<default>")}, page={(HasStartPage ? StartPage : 1)}, " +
            $"content={(HasContentUrl ? ContentUrl : "<none>")}";
    }
}
