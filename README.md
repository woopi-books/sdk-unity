# Woopi SDK: book host bridge

What a Unity book needs to be driven by the Woopi host page: next/previous/go-to page, language
changes at any time, restart page / restart book, and a default language, opening page and content
location passed in at startup. The host page side of the contract is in
[Documentation~/host-integration.md](Documentation~/host-integration.md).

This SDK is published so developers can build books for the [Woopi](https://woopi.fun) platform.
It is released under the [MIT license](LICENSE.md).

## Install

Package Manager > **+** > **Add package from git URL**:

```
https://github.com/woopifun/sdk-unity.git#v0.1.0
```

The part after `#` is the release tag; always pin one. A private
repository needs Git credentials on your machine. Requires Unity 6000.0 or newer. Addressables is
optional but needed for content hosting (below) and for the sample.

To try the sample: Package Manager > Woopi SDK > **Samples** > **Example book** > **Import**. It is not
imported by default.

## What you get

| Piece | Where | Role |
|---|---|---|
| `IBookAdapter` | `Runtime/Book` | The only thing your book implements |
| `WoopiBookHost` | `Runtime/Book` | Component the host page talks to; validates commands and reports results |
| `BookInitOptions` / `BookInitOptionsResolver` | `Runtime/Book` | Everything the host passes at startup (language, opening page, content URL, mute, volume) |
| `WoopiAudio` | `Runtime/Book` | The host-controlled master sound state (mute, volume) |
| `BookCommand`, `IBookCapabilities`, `WoopiSdk` | `Runtime/Book`, `Runtime` | Custom commands, optional capability tags, version info for the `ready` event |
| `WebBookBridge` + `WoopiBookBridge.jslib` | `Runtime/Book`, `Runtime/Plugins/WebGL` | Unity to host events |
| `WoopiContentUrl` | `Runtime/Addressables` | Injects the content URL at runtime (needs Addressables) |
| WebGL template **Woopi** | `WebGLTemplate~/index.html` | Browser side: postMessage in, events out |
| `WebGLTemplateSync` | `Editor` | Copies the template to `Assets/WebGLTemplates/Woopi` |

Pages are **1-based** everywhere in the SDK.

## Setup

1. Install the package. The WebGL template is copied to `Assets/WebGLTemplates/Woopi` and selected
   automatically when none is chosen (menu `Woopi > WebGL > Sync template` to force it). Never edit
   that copy; it is overwritten from the package.
2. Add `Woopi.Core` to your assembly definition references if your code is in its own assembly.
3. Write the adapter (below).
4. In the scene that stays loaded for the whole book, create a GameObject with `WoopiBookHost` and your
   adapter component, and assign the adapter to the host's *Adapter Behaviour* field. The host renames
   its GameObject to `WoopiBookHost`; the host page addresses it by that name.
5. Make sure nothing else opens the first page. The adapter does it.
6. Build for WebGL with the **Woopi** template.

## No navigation UI inside the book

The platform provides the book controls (next, previous, page jump, language picker, restart) and
drives the book through the host page. The book must not ship its own HUD or buttons for any of these.

- Do not add next/previous buttons, page counters, page-jump fields, language switchers or restart
  buttons to the book's scenes. Two sets of controls would fight each other and the platform's
  would no longer be the single source of truth.
- Do not rely on a development navigation UI in builds. If you keep one for testing in the Editor, make
  it Editor-only (or behind a development build flag) so it never ships.
- Show state from the host's events, not from the book's own UI: the platform learns the current page,
  page count and language from `book-event` messages, so the book has nothing to display itself.
- Story interactions that turn the page (for example the reader tapping a character to move on) are
  fine, because they are part of the content. They must go through the same page loader the adapter
  wraps so `PageChanged` fires and the host stays in sync.
- Keep the screen area for the content. The platform's controls are drawn by the host page around or
  over the canvas, so do not reserve space or place important content where the platform's controls
  will sit.

## The adapter

Implement `IBookAdapter` on a MonoBehaviour that wraps your page loading and language selection.

```csharp
public sealed class MyBookAdapter : MonoBehaviour, IBookAdapter
{
    public int PageCount => ...;              // total pages
    public int CurrentPage => ...;            // 1-based, 0 until the first page is shown
    public bool IsLoading => ...;             // reader is waiting on a page
    public string CurrentLanguage => ...;     // e.g. "es"
    public string DefaultLanguage => ...;     // the book's own default

    public event Action<int> PageChanged;     // a page became current (also after RestartPage)
    public event Action<string> LanguageChanged; // language changed, by host OR by the book

    public IEnumerator Initialize(BookInitOptions options) { ... }
    public void GoTo(int page) { ... }
    public void Next() { ... }
    public void Previous() { ... }
    public bool SetLanguage(string code) { ... }   // false if unsupported
    public void RestartPage() { ... }
    public void RestartBook() { ... }
}
```

Rules the host relies on:

- **`Initialize`** applies the language first (`options.HasLanguage ? options.Language : DefaultLanguage`),
  then opens `options.HasStartPage ? options.StartPage : 1`, and only finishes once that first page is
  shown. The host sends the `ready` event afterwards. Keep `options` if `RestartBook` should return to
  the opening page. Sound needs nothing here (see Sound below).
- **Do not start the book yourself** in `Start()` or `Awake()`; the host decides when.
- **`PageChanged`** fires whenever the shown page changes, including in-book navigation (a "turn page"
  button) and a restart of the same page. It is what produces the `PageShown` event.
- **`LanguageChanged`** fires for every change, including ones the book makes itself, so the host is
  always told.
- **`SetLanguage`** accepts regional codes (`es-AR`) and may fall back to the base language (`es`).
  Return `false` for anything unsupported and leave the language unchanged.
- **`RestartPage`** returns the current page to its initial state and keeps the reader on it.
  **`RestartBook`** drops all page state and reopens the opening page. State is reset only by these two,
  never when a page is merely revisited.
- Calls from the host are already validated (range, "already there", "still loading"); the adapter can
  assume `GoTo` gets a valid page.

## Start options

All optional. Missing means the default language, page 1, no content rewrite, sound on at full volume.

- Page URL: `index.html?lang=es&page=5&muted=1&volume=0.5&content=https://cdn.example.com/books/my-book/1.2.0`
- Host `init` message, sent before the book has started.

`BookInitOptionsResolver` reads them; adapters only see `BookInitOptions`. Future startup settings are
added to that one type, so your adapter signature does not change.

## Sound

The host page controls mute and volume, at start and at any time with `SetMuted` / `SetVolume`.
`WoopiBookHost` applies them to `AudioListener.volume`, so every `AudioSource` follows with no code in
your book. That setting is global: do not set `AudioListener.volume` yourself.

- Do not add a mute button or volume slider to the book; the platform owns them.
- If your book routes sound through its own system (an `AudioMixer`, an audio manager), untick
  *Apply Audio To Listener* on `WoopiBookHost` and follow `WoopiAudio` yourself: read
  `WoopiAudio.EffectiveVolume` (0 while muted) and listen to `WoopiAudio.Changed`.
- Browsers only start audio after the reader interacts with the page; the SDK does not work around that.

## Book-specific settings, commands and events

The SDK's own contract stays small and stable. Anything particular to your book goes through these
extension points, which need no SDK change.

**Settings at startup.** `options.Extra` holds any extra values the host passed (`x_name=value` in the
page URL, or an `extra` object in the `init` message), as text with case-insensitive names:

```csharp
var theme = options.GetExtra("theme", "light");
```

**Commands from the host.** Register them in `Awake` or `Start`, before the book is ready; return null
when handled or a short reason to reject:

```csharp
host.RegisterCommand("ShowHint", cmd =>
{
    if (!cmd.HasNumber) return "bad-request";
    hints.Show(cmd.Number);
    return null;
});
```

**Events to the host.** `host.Notify("ScoreChanged", json)` sends a `book-event` with that name and
`data`. Names use letters, digits and `_`, and cannot reuse a built-in or reserved name (`GoToPage`,
`Ready`, `PageShown`, and so on). Tell the platform team which commands, events and `extra` names your
book supports; the platform cannot guess them.

**Capabilities.** To advertise a feature tag in the `ready` event, implement `IBookCapabilities` on the
adapter and return its tags.

## Keeping your adapter compiling

`IBookAdapter` is the small required core and is not expected to change. New SDK abilities arrive as
optional interfaces (like `IBookCapabilities`), new `BookInitOptions` properties, or new built-in
commands, so a book keeps building and working when the SDK is updated. The host page checks the
`ready` event's `protocolVersion`, `commands` and `capabilities` before relying on a feature, and a
command a book does not support is answered with `unsupported-action`.

## Testing

- **Editor:** enter Play mode and drive the host from an eval/test script:
  `GameObject.Find("WoopiBookHost").SendMessage("GoToPage", "5")`. Payloads are always one string:
  `""`, a bare value (`"5"`, `"es"`), or JSON with a request id (`{"id":"r1","index":5}`,
  `{"id":"r1","value":"es"}`). Events are logged as `[WebBookBridge] {...}`.
- **Browser:** serve the build from an origin listed in `ALLOWED_ORIGINS` in the template (edit it for
  your deployments), embed it in a test page, and exercise the commands in
  [Documentation~/host-integration.md](Documentation~/host-integration.md).
- Check: no arguments opens the default language on page 1; `?lang=es&page=5` opens Spanish on page 5;
  switching language mid-book changes visible text and raises `LanguageChanged`; restarting a page
  resets only that page.

## Content hosting (CDN)

You do not need a CDN URL to build. Build the book once with a placeholder, and the platform tells the
book where its files are hosted when it starts.

1. Menu **Woopi > Addressables > Use content placeholder**. It sets the active profile's remote load
   path to `https://woopi-content.invalid/[BuildTarget]`, turns on the remote catalog and WebGL data
   caching. Point your page groups' build and load paths at the **Remote** variables.
2. Build the Addressables content and the WebGL player in the same session, then upload the whole
   build output folder (`ServerData/<BuildTarget>` plus the catalog files in it) to the location the
   platform gives you. Keep the `WebGL/` folder name.
3. At runtime the platform passes the base URL as the `content` start argument, for example
   `?content=https://cdn.example.com/books/my-book/1.2.0`. `WoopiContentUrl` rewrites the placeholder
   in every Addressables location (the catalog and the bundles) before the first load, so the files are
   fetched from `<content>/WebGL/<file>`. This needs `com.unity.addressables`; without it the class is
   not compiled.

Requirements on the hosting side:

- **Stable URL per book version.** The browser caches downloaded bundles by their full URL. Changing the
  host or path of the same bundles forces a full re-download, so use one base URL per released version.
- **CORS** for `GET` and `HEAD` from the book's origin.
- **Caching headers:** bundles have content hashes in their names, so serve them with long-lived
  immutable caching. Serve the catalog and its `.hash` file so that they are revalidated on every load
  (`no-cache` with `ETag`), otherwise updates are not picked up.
- **HTTPS**, like the page that embeds the book.

Notes:

- The `content` argument must be known when the book boots. The URL form (`?content=`) is reliable. An
  `init` message works only if it arrives before the book boots; a later one is ignored with a console
  warning.
- Without a `content` argument (Editor, local builds) nothing is rewritten, so Editor play modes and
  locally hosted builds keep working.
- A book built with the placeholder but started without `content` fails with a clear
  `Could not resolve host: woopi-content.invalid` error.
- To change content without a new player build, rebuild the content and upload it to the same location:
  the player checks the remote catalog on every start and loads the new one.

## Best practices: Addressables loading for WebGL

WebGL has one thread, a fixed memory budget and a download for every byte, so the loading structure of
a book matters more than on other platforms.

**Split content so a page is a download unit**
- One Addressable scene per page (or per small group of pages), additively loaded, in its own group.
  A page is then the unit of download, caching, memory and restart: unloading and reloading its scene
  is also the cleanest way to implement `RestartPage`.
- Keep a **persistent shell scene** (camera, light, UI, host, adapter) in the player build and keep it
  small. Everything else is remote content. A tiny initial download gets the reader to the first page
  sooner.
- Put assets used by many pages (shared prefabs, fonts, common sounds) in a **shared group** that the
  page groups depend on, so they are not duplicated into every page bundle. Check for duplicated
  dependencies with the Addressables Analyze window ("Check Duplicate Bundle Dependencies").
- Group by "loaded together", not by file type. Avoid a single group holding all textures or all audio.

**Load only what the reader can reach next**
- Keep a bounded window of resident pages: the current page plus a few on each side. Pages in the window
  are loaded but hidden, so turning to them is instant; pages outside it are unloaded and their
  handles released.
- Load the page the reader is on first and block only on that. Prefetch neighbours in the background,
  nearest first, one at a time, so prefetching cannot starve a load the reader is waiting on.
- Do not lock the UI for background prefetching; only for the page the reader is actually waiting on.
  This is what `IBookAdapter.IsLoading` means.
- A navigation started while an older one is still loading should supersede it (a generation counter
  works well) instead of queueing behind it.
- Jumping far outside the window will load; show feedback rather than keeping a wider window warm.

**Release correctly**
- Unload with `Addressables.UnloadSceneAsync(handle, autoReleaseHandle: true)`. Unloading the scene
  alone leaves the bundle referenced and its memory resident.
- Never call `Addressables.Release` twice on a handle and never use a handle after releasing it.
- Wait for the unload to finish before reloading the same address (needed for `RestartPage`).

**Keep WebGL memory in check**
- Memory only grows in a WebGL tab; peak usage is what matters. Release before prefetch so the resident
  set is the window size and never briefly the old window plus the new one.
- Compress textures for WebGL and cap sizes per page; a single oversized texture can exceed the budget
  on mobile browsers. Prefer streamed audio for long clips.
- Set an explicit memory size in Player Settings and test on a low-end phone, not only on desktop.

**Hosting and caching**
- Use a remote catalog and a remote load path for the page groups; keep the build and the remote
  content versioned together (build content first, then the player).
- Serve bundles compressed (Brotli or gzip, with the right `Content-Encoding`) and with long cache
  lifetimes: bundle names include a content hash, so they can be cached immutably. Keep the catalog
  short-lived so updates are picked up.
- The CDN must allow cross-origin `GET` and `HEAD` from the book's origin (CORS), or bundles will fail
  with an opaque network error.
- Enable WebGL data caching so downloaded bundles survive page reloads.
- Never load a bundle by blocking (`WaitForCompletion`) on WebGL; the main thread is the only thread and
  a blocking wait on a download freezes or deadlocks the tab. Always yield on the handle.

**Make loading failures visible**
- Check `handle.Status` after every load, log the `OperationException`, and release the failed handle.
- A page that fails to load must not leave the book stuck in `IsLoading`: clear the loading state on
  failure and keep the reader on the current page.
- Log load timings during development; they are the quickest way to find which page group is too heavy.
