# Game text and languages

English is the default language. Player-facing text is stored in `Unity/Assets/Resources/Localization/en.json` and read through `GameText` in the `JalanKita` namespace. Gameplay IDs, model/resource paths, save keys, legal citation identifiers, and proper names retain their existing values.

## Add or change copy

Find the text in `en.json`, then edit its `value`. Keep its `key` stable. New copy needs a new descriptive key and a call to the same API:

```csharp
AddLabel(parent, GameText.Get("home.subtitle"), "copy");
AddLabel(parent, GameText.Get("journal.checkpoint_summary", completed, total, vehicleName), "copy");
```

Parameterized catalogue values use .NET composite formatting. For example:

```json
{
  "key": "journal.checkpoint_summary",
  "value": "{0} of {1} checkpoints saved  ·  {2}"
}
```

A translation may reorder the numbered placeholders. Keep each argument's meaning and format, including formats such as `{0:00}`. Use `{{` and `}}` for literal braces. Prefer a complete sentence/template over concatenating translated word fragments.

The current catalogue covers menus, the home subtitle, mirrors, journal, options, controls, HUD, quizzes, missions, feedback, law explanations, vehicle names/descriptions, and source-authored scenery labels. Tutorial and example definitions carry explicit `tutorial.*` and `example.*` keys. Credits use `credits.*`. The larger `text.*` group contains the migrated existing copy; search by value to find a specific phrase.

## Add a language

1. Copy `en.json` to another JSON file in the same directory, for example `id.json` or `fr.json`.
2. Keep the same `entries` structure and stable keys; translate the values. The optional top-level `locale` field is documentary; the filename selects the language.
3. Preserve placeholders, keyboard key names, proper names, and legal reference identifiers. Keep short controls and HUD labels concise.
4. Call `GameText.SetLocale("id")` before constructing the game UI. No extra package, network access, or save migration is needed.

Missing entries fall back from a regional locale (such as `fr-ca`) to its base language (`fr`), then English. An unknown key displays the key itself so a missing reference remains visible. Invalid localized formatting falls back to the English template instead of interrupting play. Number formatting uses the selected locale when that culture is available.

`SetLocale` raises `LocaleChanged`. This change does not add a language picker or automatically rebuild an already-open screen. A future picker should change the locale and rebuild/reopen the current UI. Mission, vehicle, route, lesson, quiz, and tutorial definitions resolve their stored keys when read, so their text is not permanently captured in the first language loaded.

## UXML and scene text

Static UXML labels and tooltips use classes such as `i18n-text--text.incoming_call` and `i18n-tooltip--text.change_camera_c`. Their `text`/`tooltip` attributes are empty. `GameText.Apply(root)` fills these values when the UI starts; call it again when rebuilding a translated tree after a locale change. Existing element names and behavior remain unchanged.

TextMesh scenery labels are read from the catalogue when the scene is created. Recreate that scenery after a locale change. Baked lettering in FBX geometry, shop-sign texture atlases, and vehicle liveries is separate asset work: adding a JSON translation will not replace words embedded in those assets.

## Verification boundary

This pass uses source-level catalogue/reference and label inventories only. No Unity compilation, builds, tests, Play operations, reloads, or visual inspections were performed because the user is testing the running Unity project. Source synchronization does not prove that an already-running Play session has loaded the changed assembly; the user controls normal script reload and Play restart.
