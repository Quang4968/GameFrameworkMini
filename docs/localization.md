# Localization

Key -> value text per language, synced from one Google Sheet, with a per-language font swap. Read before
touching any player-facing text, a `LocalizedText`, the sheet or the localization fonts.

## Summary

- **Every player-facing string is a key**: static labels carry a `LocalizedText` with a key, code writes
  through `label.Localize(key, args)`, and text fields on settings assets and steps hold keys. A raw
  `label.text = "..."` stays in the source language forever and skips the font swap.
- **Every TMP label needs a `LocalizedText`**, even numbers (empty key = font only): a language whose
  profile swaps fonts leaves a label without the component on the old font.
- Fonts go by **slot id + style name**: the source language's profile tells which slot ("Main") and style an
  authored font/material is; the current language's profile draws the same slot and style. Renaming a slot
  or style in one profile only silently drops that language back to the authored font or default material.
- Lookup is **current language -> source language -> the key itself**: a missing key shows as
  `popup_lose.title` on screen, never blank. A value that is not a key yet (old data) also shows as written.
- The **sheet is the source of truth**: `Data/Localization/<code>.json` is rewritten whole by the Sync
  button of `LocalizationSettings.asset`; hand edits to the JSON are lost on the next sync.
- One service for the whole app, held by `LocalizationProvider` and registered in every scope by
  `RegisterLocalization`; `LocalizedText` needs no injection. Every scene scope (and `SplashBootstrap`)
  must point at the **same** `LocalizationSettings.asset`, or the language flips between scenes.
- A label code restyles must go through `LocalizedText.SetAuthoredMaterial`, or the font profile maps the
  wrong preset and the next language switch reverts the restyle.
- A label that **code writes into has no key** (the extractor leaves it empty): a key there overwrites
  the code's text on every language switch.
- Localized labels with words **auto-size** from their authored size down to authored x `minFontScale`
  (settings, 0.6): keep the label's rect inside its art (a banner title spans the ribbon body, not its tails),
  or a long translation fits the rect and still spills over the art.
- Player choice is saved under `Game.Language`; first launch follows the device language
  (`LanguageEntry.deviceLanguages`), else the source language. The game's Settings popup carries
  the language selector (one flag per language, switching `ILocalizationService`).

**Start from**: `Game/Views/Localization/LocalizedText.cs`, `Game/Services/PlayerPrefsLocalizationService.cs`,
`Game/Data/LocalizationSettingsSO.cs`.

## Status in the framework

The system, its tools and `LocalizationSettings.asset` (source language `en`, table from
`tools/localization/localization-template.csv`) are wired into Splash, Boot, Home and MainGame. No prefab or
scene label carries a `LocalizedText` yet and the views still write text with `.text`: a game localizes its UI
with Keys > Extract, converts code-written strings to `label.Localize(...)`, and adds a language box to its Settings popup.

## Sheet format

One tab, first row `key | note | en | <code> | ...`. Every header after `key` except `note`/`comment` (or one
starting with `#`) is a language code; a row whose key starts with `#` is a section separator. An empty cell
means "not translated" and falls back to the source language. Placeholders are `{0}`, `{1}`; rich text tags
are kept. Template: `tools/localization/localization-template.csv`.

Keys are `<screen>.<element>` (`popup_settings.music_row`), shared ones `common.*`.

## Sync

```
Google Sheet --CSV export (Anyone with the link: Viewer)--> LocalizationSyncPanel
   --LocalizationCsv.Parse--> one <code>.json per column --> LocalizationSettings.languages[].table
```

1. Import the CSV template into Google Sheets (File > Import), share it "Anyone with the link: Viewer".
2. Put the Sheet ID (between `/d/` and `/edit`) and the tab `gid` into `LocalizationSettings.asset`.
3. Sync From Google Sheet in the asset's inspector (or the Settings & Sync tab of `Game/Localization`). The
   report lists duplicate keys, empty cells and placeholder mismatches; a new language column is added to
   the settings asset to finish by hand (display name, device languages).
4. Keys tab > Validate lists keys missing from the source table (LocalizedText, settings assets, steps), any
   scene scope not pointing at `LocalizationSettings.asset`, and profiles missing a slot or style.

## Fonts

```
FontProfile_en                      FontProfile_<code>
  Main:    LilitaOne                  Main:    <font with the glyphs>
    Default, Outline-Orange, ...         Default, Outline-Orange, ... (copies on its atlas)
```

A language without a profile keeps the authored fonts. `PlayerPrefsLocalizationService.TryResolveFont` looks
the label's authored font up in the source profile and its material up in that slot's styles, then takes the
same slot id and style from the current profile; a missing style falls back to the font's default material.

`Game/Localization` > Fonts:
- **Source language**: "Add / Replace Slot" (id + font) reads every `<Font>-<Style>.mat` beside the font
  (the default material is style `Default`); "Rescan" after adding a preset.
- **Other language**: pick a font per source slot, then "Create Materials + Fill Profile": every style is
  cloned onto that font's atlas (SDF thickness rescaled by the two fonts' padding/point-size ratio, optional
  outline scale and face dilate for a thin font) and the slot is written into the language's profile, created
  beside the settings asset when missing. Re-running overwrites the same files (GUIDs kept). A second slot
  cloning into the same font gets its source font's name in the file names.
- Check the language's glyphs with `TMP_FontAsset.HasCharacters` before shipping: a missing glyph renders as
  a box, and a fallback font has no matching presets.

## New UI

- Static text: `Game/Localization` > Keys > Extract From Prefabs / Open Scene (adds `LocalizedText`, keys
  static labels, writes a `key,note,en` CSV to paste into the sheet), or add `LocalizedText` and pick the key
  in the inspector.
- Extract also turns on auto size for the labels it keys; Keys > **Fit Localized Labels** does it for every
  prefab and the open scene (number-only labels and labels already auto-sized are left alone).
- Code text: `label.Localize("screen.key", args)`; the label gets the component on first use.
- Language changes re-render every `LocalizedText`; text code wrote with `.text` does not refresh.

## Adding a language

1. Add the column to the sheet, sync: the new language is appended to `LocalizationSettings.asset`; set its
   display name and device languages.
2. Fonts tab: pick a font per slot if the authored fonts lack its glyphs, Create Materials + Fill Profile.
3. Add a flag for it (language code, flag sprite, tick) in the Settings language box.

## Testing the first launch

`Game/Localization` > Debug: tick **Simulate** and pick one of the shipped languages (Editor only, kept in
EditorPrefs; builds always read the real device language through `PlayerPrefsLocalizationService.DeviceLanguage`),
then **Reset + Play From Splash**. "Next launch opens in" previews the pick: saved choice, else the language
claiming the device language, else the source language. The first launch saves nothing; only a selector press
writes `Game.Language`. While playing, the tab switches language live.

## Test

- `Tests/EditMode/Core/GameLogic/Localization/LocalizationTableTests.cs`: fallback chain, safe format, switch.
- `Tests/EditMode/Core/GameLogic/Localization/LocalizationCsvTests.cs`: quoting, comments, issues report.
