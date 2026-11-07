# Rule guide artwork

Original RoadSense UI pictograms, created for this repository. No downloaded art, external fonts, SVG importer, or additional package is required. These are instructional illustrations, not exact regulatory sign reproductions.

## Source and usage

`Unity/Assets/Scripts/JalanGame.RuleGuide.cs` is the editable vector asset source. `RuleGuideIcon(Rule)` draws a scalable UI Toolkit Painter2D pictogram. Used beside every Rules to Watch action in `JalanGame.SelectionBrowser.cs` and in the clickable Before You Drive equipment cards in `JalanGame.cs`. Artwork ignores pointer events so the full card remains clickable. Descriptive text remains the source of truth; never communicate a rule through color or imagery alone.

## Style

Normalized 0–1 square coordinates; 46 px rule icons and 54 px equipment illustrations. Rounded strokes, simple silhouettes, warm gold `#edc86d` on the dark interface; ready equipment changes to green `#9bdbb3` and explicitly says Ready. Keep critical detail inside 12–88% of the canvas. Stroke width scales with the icon. Styling lives under `.rule-guide-*` and `.equipment-guide*` in Jalan.uss.

## Rule mapping

- SeatBelt: person with diagonal belt and buckle.
- Helmet: helmet profile, visor, and chin strap.
- Headlight: lamp with three light beams.
- Stop: octagonal stop cue; Signal / RightArrow: traffic light / turn arrow.
- Pedestrian: person above crossing stripes.
- Indicator / TurnSignal / ReturnLeft: opposing turn arrows.
- NoEntry / NoStopping / Phone: prohibition ring with bar / slash / phone.
- Speed / BoulevardSpeed: speedometer. The adjacent rule text supplies the actual limit.
- Gap / Collision: separated vehicles.
- PickupPlace / PickupIncomplete: vehicle and person with pickup arrow.
- Railway: rails, sleepers, and crossing barrier.
- Emergency: ambulance with medical cross and warning lamp.
- StopBox: marked waiting box and car behind the stop line.
- Junction: T-junction with approaching arrow.
- KeepLeft / MovementLane / OffRoad: lane boundaries and left-side travel arrow.

## Extend and verify

Add an explicit switch case for every new Rule. Reuse a pictogram only for related concepts; retain distinct action text. Localize card copy in `Resources/Localization/en.json`. Do not use emoji or glyph fonts. Check car and motorcycle preflight, toggle each card on/off, inspect Rules to Watch at compact and normal sizes, and confirm empty rule lists remain hidden. Do not change journey validation or legal citations when changing artwork.

## Concise rule summaries

Selector summaries use `guide.rule.<Rule>` localization keys, normal capitalization, and bold gold primary highlights for actions and subjects. UI Toolkit rich text is explicitly enabled. Full Lesson.Action remains available in the tooltip; citations and gameplay validation are unchanged. Translate whole strings while retaining balanced bold/color tags.
