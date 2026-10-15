# Woopi book host integration

How a hosting page (e.g. `https://woopi.fun/book/<id>`) drives a Woopi book built with the Woopi WebGL
template, whether the book is embedded in an `<iframe>` or served top-level. For adding the bridge to a
Unity book, see [README.md](README.md).

## Start options
All optional. Anything missing falls back to the book's own default language, page 1, no content rewrite, and sound on at full volume.

| Argument | Meaning |
|---|---|
| `lang` / `language` | Language code, e.g. `es` |
| `page` | Opening page, 1-based |
| `content` | Base URL where the book's Addressables files are hosted, e.g. `https://cdn.example.com/books/my-book/1.2.0`. The book appends its build-target folder (`/WebGL/...`) |
| `muted` | Start muted: `1`/`true` or `0`/`false` |
| `volume` | Master volume from `0` to `1`, e.g. `0.5` (values outside are clamped) |
| `extra` / `x_<name>` | Book-specific settings the SDK does not interpret, passed to the book as text. Ask the book's developer which names it reads. In the URL: `x_theme=dark`; in `init`: `extra:{theme:"dark"}` |

| Source | Example |
|---|---|
| URL of the book page (also the iframe `src`) | `index.html?lang=es&page=5&muted=1&volume=0.5&x_theme=dark&content=https://cdn.example.com/books/my-book/1.2.0` |
| `init` message, sent before the book has started | `{source:"woopi-host", action:"init", language:"es", page:5, muted:true, volume:0.5, extra:{theme:"dark"}, content:"https://cdn.example.com/books/my-book/1.2.0"}` |

The `init` message wins over the URL (per option; `extra` entries are merged by name). If it arrives
after the book has started, `language`, `page`, `muted` and `volume` are applied as `SetLanguage`,
`GoToPage`, `SetMuted` and `SetVolume` instead, and `content` and `extra` are ignored (the content
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
| `SetMuted` | `true` or `false` | mutes / unmutes all book sound; works at any time |
| `SetVolume` | number from 0 to 1 | master volume; works at any time, independent of mute |
| `RestartPage` | | restarts the current page only; its state resets |
| `RestartBook` | | restarts the whole book at the opening page |

Add a `requestId` string to any command to correlate the reply: it is echoed in the resulting event.
Commands sent before the book is ready are queued and replayed. The book only accepts messages from
origins listed in `ALLOWED_ORIGINS` in the template.

**Book-specific commands.** A book can register commands of its own (for example `ShowHint`). Send them
the same way (`{ source: "woopi-host", action: "ShowHint", value: "3" }`). The `commands` list in the
`ready` event shows which ones a book accepts.

**Unknown commands.** A command the book does not accept, whether the name is wrong or the book was built
with an older SDK, is answered with a `book-event` with `success:false` and `reason:"unsupported-action"`.
It never fails silently. Check the `commands` list to avoid sending it at all.

## Events (book to host)
Received with a `message` listener on the host window:

```
{ source: "woopi-unity", type: "ready", detail }          // book is up; see "The ready event" below
{ source: "woopi-unity", type: "error", message }
{ source: "woopi-unity", type: "book-event", detail: {
    action, requestId, success, reason,
    currentPage /* 1-based */, currentPageIndex /* 0-based */, pageCount, language,
    muted, volume, data } }
```
When the host and the book are on the same page, you can instead listen for the `woopi-book-event`
window event or define `window.WoopiBook.onEvent(detail)`.

`detail.action` is the command that was received (`success:false` with a `reason` if it was refused), or:
- `PageShown`: a page became current. Also sent for navigation inside the book (the reader turning a
  page) and after `RestartPage`/`RestartBook`; `requestId` is set when a command caused it.
- `LanguageChanged`: the language changed, whether the host or the book did it. `language` is the new one.
- `AudioChanged`: mute or volume changed. `muted` and `volume` hold the new state.
- Any name the book chose itself (for example `ScoreChanged`): a book-defined event. `data` carries
  free-form text, often JSON, that only that book's documentation describes.

Reasons: `no-next-page`, `no-previous-page`, `out-of-range`, `already-there`, `already-set`,
`unsupported-language`, `unsupported-action`, `loading` (the target page is still being fetched),
`not-ready`, `bad-request`. A book-specific command may reject with its own reason.

## The ready event
`detail` of `ready` is the starting point for feature detection. Besides the usual fields (page, language,
muted, volume) it has:

| Field | Meaning |
|---|---|
| `protocolVersion` | Version of this message contract (currently `1`). It only changes if an existing command or event stops working the way described here. New commands and fields do not change it |
| `sdkVersion` | Woopi SDK version the book was built with, e.g. `"0.2.0"` |
| `commands` | Every command name the book accepts: the built-in ones plus any it registered |
| `capabilities` | Feature tags: `navigation`, `language`, `audio`, `restart`, plus any the book adds |

A `ready` without `commands` comes from a book built with SDK 0.1.x; it only understands `NextPage`,
`PreviousPage`, `GoToPage`, `GoToPageIndex`, `SetLanguage`, `RestartPage` and `RestartBook`.

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
