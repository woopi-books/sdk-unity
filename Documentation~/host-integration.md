# Woopi book host integration

How a hosting page (e.g. `https://woopi.fun/book/<id>`) drives a Woopi book built with the Woopi WebGL
template, whether the book is embedded in an `<iframe>` or served top-level. For adding the bridge to a
Unity book, see [README.md](README.md).

## Start arguments
All optional. Anything missing falls back to the book's own default language, page 1, and no content rewrite.

| Argument | Meaning |
|---|---|
| `lang` / `language` | Language code, e.g. `es` |
| `page` | Opening page, 1-based |
| `content` | Base URL where the book's Addressables files are hosted, e.g. `https://cdn.example.com/books/my-book/1.2.0`. The book appends its build-target folder (`/WebGL/...`) |

| Source | Example |
|---|---|
| URL of the book page (also the iframe `src`) | `index.html?lang=es&page=5&content=https://cdn.example.com/books/my-book/1.2.0` |
| `init` message, sent before the book has started | `{source:"woopi-host", action:"init", language:"es", page:5, content:"https://cdn.example.com/books/my-book/1.2.0"}` |

The `init` message wins over the URL. If it arrives after the book has started, `language` and `page`
are applied as a `SetLanguage` plus a `GoToPage` instead, and `content` is ignored (the content
location is needed before the book's first load, so pass it in the URL, which is always in time).

The content URL must stay the same for a given book version: the browser caches downloaded files by
their full URL. The content host must allow CORS for `GET` and `HEAD` from the book's origin.

## Commands (host to book)
`iframe.contentWindow.postMessage({ source: "woopi-host", action, value?, requestId? }, bookOrigin)`

| action | value | effect |
|---|---|---|
| `NextPage`, `PreviousPage` | | one page forward / back |
| `GoToPage` | page, 1-based | |
| `GoToPageIndex` | index, 0-based | |
| `SetLanguage` | language code, e.g. `"es"` | works at any time, also mid-book |
| `RestartPage` | | restarts the current page only; its state resets |
| `RestartBook` | | restarts the whole book at the opening page |

Add a `requestId` string to any command to correlate the reply: it is echoed in the resulting event.
Commands sent before the book is ready are queued and replayed. The book only accepts messages from
origins listed in `ALLOWED_ORIGINS` in the template.

## Events (book to host)
Received with a `message` listener on the host window:

```
{ source: "woopi-unity", type: "ready", detail }          // book is up; detail has page and language
{ source: "woopi-unity", type: "error", message }
{ source: "woopi-unity", type: "book-event", detail: {
    action, requestId, success, reason,
    currentPage /* 1-based */, currentPageIndex /* 0-based */, pageCount, language } }
```
When the host and the book are on the same page, you can instead listen for the `woopi-book-event`
window event or define `window.WoopiBook.onEvent(detail)`.

`detail.action` is the command that was received (`success:false` with a `reason` if it was refused), or:
- `PageShown`: a page became current. Also sent for navigation inside the book (the reader turning a
  page) and after `RestartPage`/`RestartBook`; `requestId` is set when a command caused it.
- `LanguageChanged`: the language changed, whether the host or the book did it. `language` is the new one.

Reasons: `no-next-page`, `no-previous-page`, `out-of-range`, `already-there`, `already-set`,
`unsupported-language`, `loading` (the target page is still being fetched), `not-ready`, `bad-request`.

## Example
```js
const frame = document.querySelector("iframe");
const bookOrigin = "https://books.example";

window.addEventListener("message", (e) => {
  if (e.origin !== bookOrigin || e.data?.source !== "woopi-unity") return;
  console.log(e.data.type, e.data.detail);
});

frame.src = bookOrigin + "/my-book/index.html?lang=en&page=1";

// Commands can be sent as soon as the frame exists; the book queues them until it is ready.
frame.addEventListener("load", () => {
  frame.contentWindow.postMessage(
    { source: "woopi-host", action: "SetLanguage", value: "es", requestId: "lang-1" }, bookOrigin);
});
```
