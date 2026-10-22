# Changelog

## Unreleased

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
