# Changelog

## Unreleased

- Safe zone: `WoopiSafeZone` (the page-turn zones of the WebGL template and the reader dock's bottom hotzone as screen and world rectangles, `IsInDockHotzone`) and `WoopiSafeZoneOverlay`
  (Editor-only UI Toolkit overlay that draws them in the Game view, in edit mode and in Play mode; GameObject > Woopi > Safe Zone Overlay,
  tagged `EditorOnly`). `WoopiBook_GetDevicePixelRatio` in the WebGL plugin.
- UI Toolkit panels work on every supported Unity version: `WoopiPanelBehaviour` drives a `PanelRenderer` on Unity 6.5 and later (chosen with `UNITY_6000_5_OR_NEWER`) and a `UIDocument` before that. The safe zone overlay and the SimpleBook dev navigation use it; `ExampleDevNavigation` no longer fails to compile on Unity 6.0 to 6.4.
- Sample: Interactions (tap, multi-tap, drag, hold and swipe, one scene each).
- Localization tooling (Editor, needs Unity Localization and Addressables): Woopi > Localization > Set up remote localization (remote Addressables groups: all text of a language in one group and preloaded, images and audio in one group per page and language, labelled `woopi-page-NN`, with the page read from a `Page_NN` folder name; Locale included), Add language (Locale, tables and groups; untranslated entries fall back to the default language) and Build language pack (content update build that outputs only one language's bundles plus the new catalog, to upload without rebuilding the player).
- `WoopiPageContent` (Woopi.Core.Localization): `PreloadPage` / `PreloadAround` download a page's localized media, and its neighbours', in the current language ahead of time.
- Sample: Localization (localized text, image and audio over three pages, a language menu built from the available locales, `lang` start argument).
- WebGL template: the Woopi platform deployments (dev and prod on AWS Amplify, `www.woopi.fun`)
  and the local development hosts are allowed origins. Without them a book loaded in the platform
  never received commands or sent events.

## 0.2.0
- Sound: the host can start the book muted and/or at a given volume (`muted`, `volume` in the `init` message or page URL) and change both at any time with the `SetMuted` and `SetVolume` commands. `AudioChanged` is sent whenever either changes; every event now also carries `muted` and `volume`.
- `WoopiBookHost` applies the sound to `AudioListener.volume` by default (switch off with "Apply Audio To Listener" for books that route sound themselves; read `WoopiAudio` instead).
- **Breaking:** `BookStartArgs` is now `BookInitOptions` and `BookStartArgsResolver` is `BookInitOptionsResolver`. `IBookAdapter.Initialize` takes a `BookInitOptions`. Rename the type in your adapter; nothing else changes.
- WebGL template: `init` accepts `muted` and `volume`; numeric and boolean command values are passed through unchanged.
- Handshake: the `ready` event now carries `protocolVersion`, `sdkVersion`, `commands` (every accepted command) and `capabilities` (feature tags; books add their own through the optional `IBookCapabilities`). A command the book does not accept, or a book built with an older SDK, is answered with `unsupported-action` instead of being dropped.
- Book-specific startup settings: `BookInitOptions.Extra` / `GetExtra`, filled from `x_<name>` URL parameters or the `extra` object of the `init` message.
- Book-specific commands and events: `WoopiBookHost.RegisterCommand`, `UnregisterCommand` and `Notify`; `BookBridgeEvent` has a `data` field. The template routes unknown-to-it command names through a single `Command` entry point, so no template change is needed per book.
- `WoopiSdk` (`Version`, `ProtocolVersion`); the Editor warns if `WoopiSdk.Version` does not match package.json.

## 0.1.0
- Book host bridge: `IBookAdapter`, `WoopiBookHost`, start arguments (language, opening page, content URL).
- Commands: next / previous page, go to page, set language, restart page, restart book. Events: ready, page shown, language changed, rejections with a reason.
- WebGL template `Woopi` and the `WoopiBookBridge.jslib` plugin.
- Optional Addressables support: runtime content URL injection so books are built once with a placeholder URL.
- Sample: Example book.
- MIT license.
