using System;
using System.Collections;

namespace Woopi.Core.Book
{
    /// <summary>
    /// The only thing a book implements to be driven by the host page. <see cref="WoopiBookHost"/>
    /// owns the JS contract (payloads, request ids, events); the adapter just maps it onto however
    /// that book loads pages and picks languages.
    ///
    /// Pages are 1-based throughout.
    /// </summary>
    public interface IBookAdapter
    {
        int PageCount { get; }

        /// <summary>The page being read, 1-based. 0 until the book has opened its first page.</summary>
        int CurrentPage { get; }

        /// <summary>True while the reader is waiting on a page that is not yet loaded.</summary>
        bool IsLoading { get; }

        /// <summary>Language code of the language the book is currently showing, e.g. "es".</summary>
        string CurrentLanguage { get; }

        /// <summary>The book's own default language, used when the host does not pass one.</summary>
        string DefaultLanguage { get; }

        /// <summary>Raised when a page becomes the current page (also after a page reload).</summary>
        event Action<int> PageChanged;

        /// <summary>
        /// Raised after the language changed, whether the host asked for it or the book did itself,
        /// with the new language code.
        /// </summary>
        event Action<string> LanguageChanged;

        /// <summary>
        /// Opens the book. Missing arguments in <paramref name="args"/> mean "use the book's
        /// default" (see <see cref="DefaultLanguage"/>, page 1). Finishes once the first page is shown.
        /// </summary>
        IEnumerator Initialize(BookStartArgs args);

        void GoTo(int page);
        void Next();
        void Previous();

        /// <summary>Returns false when the book has no language matching <paramref name="code"/>.</summary>
        bool SetLanguage(string code);

        /// <summary>Restarts the current page only; the book stays on it.</summary>
        void RestartPage();

        /// <summary>Restarts the whole book at the opening page.</summary>
        void RestartBook();
    }
}
