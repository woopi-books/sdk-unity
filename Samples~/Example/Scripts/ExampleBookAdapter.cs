using System;
using System.Collections;
using UnityEngine;
using Woopi.Core.Book;

namespace Woopi.Examples.SimpleBook
{
    /// <summary>
    /// The whole Woopi integration for this book: it maps the book's own loader and language service
    /// onto <see cref="IBookAdapter"/>. <see cref="WoopiBookHost"/> does everything else (payloads,
    /// validation, request ids, events to the host page).
    /// </summary>
    public sealed class ExampleBookAdapter : MonoBehaviour, IBookAdapter
    {
        [SerializeField] private ExampleBookLoader loader;
        [SerializeField] private ExampleLanguage language;

        private BookStartArgs _args;

        public int PageCount => loader.PageCount;
        public int CurrentPage => loader.CurrentPage;
        public bool IsLoading => loader.IsLoading;
        public string CurrentLanguage => language.CurrentLanguage;
        public string DefaultLanguage => language.DefaultLanguage;

        public event Action<int> PageChanged
        {
            add => loader.PageChanged += value;
            remove => loader.PageChanged -= value;
        }

        public event Action<string> LanguageChanged
        {
            add => language.Changed += value;
            remove => language.Changed -= value;
        }

        public IEnumerator Initialize(BookStartArgs args)
        {
            _args = args;

            // Language first, so the first page already opens in it. An unknown or missing
            // language falls back to the book's default.
            if (!args.HasLanguage || !language.Set(args.Language))
                language.Set(language.DefaultLanguage);

            // Finishes once the first page is shown; the host sends "ready" after that.
            yield return loader.Begin(args.StartPage);
        }

        public void GoTo(int page) => loader.GoTo(page);
        public void Next() => loader.Next();
        public void Previous() => loader.Previous();
        public bool SetLanguage(string code) => language.Set(code);
        public void RestartPage() => loader.RestartPage();
        public void RestartBook() => loader.RestartBook(_args.StartPage);
    }
}
