# Woopi example book

The smallest complete book wired to the Woopi SDK: one core scene, ten dummy page scenes, and an
adapter. Use it as the reference for structure; see the Woopi SDK README (Package Manager > Woopi SDK > View
documentation) for the SDK itself.

## Run it
0. Once after importing: menu **Woopi > Example > Build sample book**. It generates the ten page scenes,
   the core scene and the Addressables groups from the art, and is safe to re-run. Requires Addressables
   and the URP 2D renderer. The imported folder ships without scenes on purpose, so the project
   gets no Addressables groups until you build the sample.
1. Open `Scenes/ExampleCore.unity` and press Play. The book opens on page 1 in English.
2. Drive it like the host page does. With the Editor in Play mode, from any test script:
   ```csharp
   var host = GameObject.Find("WoopiBookHost");
   host.SendMessage("GoToPage", "5");
   host.SendMessage("SetLanguage", "es");   // the title changes language mid-book
   host.SendMessage("RestartPage", "");
   host.SendMessage("RestartBook", "");
   ```
   Every command logs a `[WebBookBridge] {...}` event, which is what the host page would receive.
3. Try start arguments in a WebGL build with `index.html?lang=es&page=5`.

You can also open any `ExamplePage_NN` scene directly: `ExampleCore` is opened alongside it, and
pressing Play starts the book on that page with its neighbours loaded.

To remove the sample again, delete the imported folder and the `Woopi Example ...` groups in the
Addressables Groups window.

## What is in it

```
Art/                    three placeholder images shared by every page
Scenes/ExampleCore.unity   (generated) the scene that stays loaded: camera, light, the host object
Scenes/Pages/              (generated) ExamplePage_01 ... ExamplePage_10: one scene per page, no camera
DevNavigation/             UI Toolkit assets of the Editor-only page navigation panel
Scripts/
  ExampleBookAdapter.cs    the Woopi integration (the part to copy and adapt)
  ExampleDevNavigation.cs  Editor-only previous / next / jump panel
  ExampleBookLoader.cs     this book's own page loading
  ExampleLanguage.cs       this book's own language handling
  ExamplePageTitle.cs      a page element that follows the language
Editor/ExampleBookSetup.cs generates the scenes and Addressables groups
```

### The core scene
One GameObject, `WoopiBookHost`, carries four components:

| Component | Belongs to | Role |
|---|---|---|
| `WoopiBookHost` | SDK | The object the host page talks to |
| `ExampleBookAdapter` | this book | Implements `IBookAdapter`; the only bridge to the SDK |
| `ExampleBookLoader` | this book | Loads and unloads page scenes |
| `ExampleLanguage` | this book | Current language, switching, change event |

The host's *Adapter Behaviour* field points at the adapter, and the adapter points at the loader and
language service. Nothing starts the book except the host calling `ExampleBookAdapter.Initialize`.

### Editor-only navigation panel
In Play mode a small panel (previous, next, page label, jump-to-page field) lets you move through the
book without a host page. It is a development tool only: the platform provides the real controls, so
it must not ship. Its GameObject, `DevNavigation (Editor only)`, uses the built-in **EditorOnly** tag,
which makes Unity strip it, and the assets only it references, from every player build, including
development builds. Keep that tag on any such tool in your own book. It drives the loader directly; use
the `SendMessage` commands above when you want to exercise the host contract.

### The adapter
`ExampleBookAdapter` is about 50 lines and only maps calls:
- `Initialize(args)` sets the language (the requested one, otherwise the default), then opens the
  requested page (otherwise page 1) and finishes when that page is shown.
- `GoTo`, `Next`, `Previous`, `RestartPage`, `RestartBook` forward to the loader.
- `PageChanged` and `LanguageChanged` forward the loader's and language service's events, so the host
  is told about every change, including ones the book makes itself.

### Pages and loading
- Each page is an Addressable scene (`ExamplePage_NN`) in its own group, loaded additively. Pages have
  no camera or audio listener; the core scene owns them.
- The current page and its neighbours are kept loaded (neighbours hidden); the rest are unloaded.
  Moving to a neighbour is instant. Jumping further loads the page.
- The three images are used by every page, so they sit in one shared group and the page groups only
  contain their scenes. An asset can be in one group only, so art shared across pages must not be
  placed per page. Real books with unique art per page put the art in that page's group.
- Groups use local build and load paths so the sample runs anywhere. For a real book, point them at
  your remote path (see the SDK README for loading and hosting guidance).

### Language
`ExampleLanguage` supports English and Spanish with one string, so the sample stays dependency-free.
A real book would use its localization system behind the same members the adapter needs: current and
default language, a way to switch, and a change event.

## Making your own book from it
1. Copy this folder and rename the namespace and types.
2. Replace the page scenes with your content; keep one Addressable scene per page.
3. Replace `ExampleLanguage` with your localization and update the adapter's language members.
4. Keep `ExampleBookAdapter` as the single point of contact with the SDK.
5. Do not add navigation buttons or HUD; the platform provides them.
