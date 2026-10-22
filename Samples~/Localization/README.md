# Woopi localization sample

Text, an image and an audio clip that change with the book's language, and a language menu that lists
whatever languages the book has. It also sets up the project so that **a language can be published later
without rebuilding the player**. Requires Unity Localization, Addressables and the Input System.

## Run it
0. Once after importing: the Editor offers to build the sample the next time it is idle. If you chose Not now
   (or Don't ask again), run it yourself with menu **Woopi > Example > Build localization sample**. It runs
   **Woopi > Localization > Set up remote localization**, creates English and Spanish, the string table
   `WoopiSample` and the asset table `WoopiSampleAssets` (a picture and a voice clip per language,
   generated so the sample ships no binaries) and the scene. Safe to re-run.
1. Open `Scenes/LocalizationSample.unity` and press Play. Use the language buttons; the text, the picture
   and the voice (Play voice) change with the language. The `<` and `>` buttons turn between three pages.
2. With a `lang` start argument (`index.html?lang=es` in a WebGL build) the book opens in that language.

## How content is grouped (and why)
- **Text:** all string tables of a language share one group, `Localization-String-Tables-<Language>`, and are
  flagged to preload, so every text of the book is ready from the start. It is small.
- **Images and audio:** one group per page and language, `Localization-Assets-Page03-Spanish`, and every entry
  is labelled `woopi-page-03` and `Locale-es`. They are megabytes, so they load only when needed.
  The page comes from the folder name: keep a page's localized media under a folder like `Art/Page_03/`
  (also `page3`, `Page 03`). Media outside any page folder falls back to one group per language.
- **Preload:** `WoopiPageContent.PreloadAround(page)` downloads the current page and its neighbours (page 2:
  pages 2, 1 and 3) in the current language. `LocalizedContentDemo` calls it when the page or the language
  changes. A reader on Spanish never downloads English, and never downloads page 9 while on page 2.
- **Asset tables** are not flagged to preload: Unity would then load every asset in them.
- Media used by several languages (the same image for all) goes to a shared group instead of a page group.

## Add a language
1. **Woopi > Localization > Add language**, enter a code such as `fr`. It creates the Locale, an empty table
   for it in every string and asset table, and its own remote Addressables groups
   (`Localization-String-Tables-French`, and a `Localization-Assets-PageNN-French` per page once it has media).
2. Translate in **Window > Asset Management > Localization Tables**. Entries left empty show the default
   language (English), so a half-translated language still works. Put the French picture and voice of each
   page in the `WoopiSampleAssets` table, using files under that page's folder (`Art/Page_02/picture_fr.png`).
   Their groups are created, labelled and made remote automatically.
3. Run the book again: the menu shows a French button. Nothing in the code changed.

## Publish a language without rebuilding the player
1. When you build a release (Addressables > Build > New Build), keep the `addressables_content_state.bin`
   it saves next to the Addressables settings (default `Assets/AddressableAssetsData/<Platform>/`). Keep one
   per released version.
2. **Woopi > Localization > Build language pack**, pick the language and that state file. It runs a content
   update build and copies the language's bundles, the small language-neutral ones (shared assets and table
   data, which change whenever a language is added) and the new catalog to `<output>/<code>/<BuildTarget>/`.
   Bundles of the other languages are not touched.
3. Upload those files into the book's content folder, next to the release's `<BuildTarget>/` files, and
   reload the book. The player finds the new catalog on startup and lists the new language.

The host page has to serve the catalog's `.hash` file without caching, or players keep the old catalog.

## What is in it
```
Scripts/
  LocalizedContentDemo.cs   localized text, image and audio over three pages; preloads the neighbour pages
  LanguageMenu.cs           one button per available language; reads the host's lang start argument
Editor/
  LocalizationSampleSetup.cs   builds the tables, the generated art and audio, and the scene
```
Generated when you build the sample (not part of the package): `Art/`, `Tables/`, `Scenes/`.

To remove the sample, delete its folder, the `Localization-*` groups in the Addressables Groups window and,
if nothing else uses them, `Assets/Localization`.
