# RoadSense: restrained street palette

The home screen is an **Experience** surface; options, help and feedback are **Operate** surfaces. This is a scoped refinement of the existing game, preserving RoadSense, We Will Handle, the live Indonesian street, current navigation and the approved upper-left tutorial.

## Research and interpretation

- [Gran Turismo 7's official manual](https://www.gran-turismo.com/us/gt7/manual/online/) separates its title screen, world navigation and settings. RoadSense's interpretation is a focused title composition with Play, Options and Exit, keeping configuration and journeys behind their existing actions.
- [Rockstar's official Red Dead Redemption 2 screenshots](https://www.rockstargames.com/newswire/article/9k1248838o1a1k/Screens-From-Red-Dead-Redemption-2) are an environmental art reference, not a palette to copy. RoadSense retains its own Indonesian street and uses weathered canvas, plaster and painted shopfront tones instead of arbitrary saturated hues.
- [Forza Motorsport's accessibility guidance](https://forza.net/news/forza-motorsport-accessibility) treats interface contrast and distinguishable world colors as functional concerns. The refinement reduces decorative contrast while retaining readable text, keyboard focus, traffic signal colors and pedestrian visibility.

The palette below is an authored RoadSense interpretation, not sampled colors or an assertion about those games' design systems.

## Palette and hierarchy

| Role | Color | Use |
| --- | --- | --- |
| Warm ink | `#191e22` | Dialogue and feedback surfaces |
| Raised charcoal | `#252c30` | Secondary controls and expanded content |
| Parchment | `#eee9df` | Main type |
| Warm secondary | `#c6c2b8` | Supporting text |
| Muted brass | `#c4aa7c` | Primary action and focus |
| Terracotta light | `#e6a59a` | Violation label |
| Sage light | `#a5c3ac` | Success label |

The home loses its enclosing card. The existing street remains the backdrop under a readable dark wash, with a larger wordmark, generous separation and a narrow action column. Play is the sole filled accent; Options and Exit have quieter treatment. The existing font asset is retained.

Results use one bounded dark surface, a modest severity label, a clear instruction, then the existing explanation and collapsible legal details. The recovery footer and scroll containers remain intact. Options, help, keycaps, vehicle selection and mission panels share the palette; the bright white violation stamp and keycaps are removed.

World changes are restricted to `District.cs` building fallback colors and awnings/shop boards. Existing textured plaster, lighting, grading, road markings, vehicles, pedestrian materials, signal emissions and red/amber/green UI states are unchanged. Seeded random draws remain in the same sequence so building dressing is stable.

## Review limits

Inspected historical `1280x720-home.png` and `1024x600-options.png` captures under `/private/tmp/roadsense-runtime-evidence/`; they predate these edits and the English localization. Reviewed source for palette, hierarchy, focus, responsive overrides, preserved tutorial rules and bounded scrolling. No new runtime captures, Unity actions, compilation, builds or tests were performed because the user is actively testing. Current runtime layout and appearance are therefore unverified. The Impeccable web detector is not applicable to Unity USS.
