# RoadSense pitch production

English editorial redesign, 2026-11-07. Eight 16:9 slides, a six-minute pitch plan including a 90-second live demo, and three bespoke generated road-world illustrations.

## Deliverables

- `RoadSense-Pitch-Draft.pdf`: presentation PDF, with all visible slide text in English.
- `SPEAKER_NOTES.md`: English script, operator cues, exact 360-second allocation, source notes, and an honest fallback.
- `build_deck.py` and `template.py`: editable text, layout, and native diagram source.
- `assets/generated/`: reusable original generated artwork.
- `assets/PROVENANCE.json`: exact generation prompts and source records.
- `assets/gameplay-existing.png`: preserved earlier prototype screenshot; only its text-free road/vehicle region appears in the deck.
- Desktop copy: `~/Desktop/RoadSense-Pitch.pdf`.

Generated artwork is labeled as concept artwork. The PDF does not fabricate an English product interface. The live application's Indonesian UI is handled with English narration. Content counts describe implemented scope, not validated learning outcomes.

## Rebuild

Use Python with `reportlab`, `pymupdf`, and `pillow`. The generator embeds Arial and Georgia from macOS's supplemental font directory.

```bash
python docs/pitch/build_deck.py --desktop
```

The local environment used for this build is `/private/tmp/roadsense-pitch-venv/bin/python`. The generator renders every page and produces a contact sheet, extracted text, and page-boundary report under the operating system's temporary directory in `roadsense-pitch-review/`. No game tests or game builds are part of this document workflow.

## Design guidance consulted

The online search identified Anthropic's presentation skill. Its primary source was inspected directly:

- https://github.com/anthropics/skills/blob/main/skills/pptx/SKILL.md
- Discovery listing: https://www.skills.sh/anthropics/skills/pptx

Applied guidance: vary compositions, establish strong type hierarchy, use deliberate contrast and relevant imagery, and render every page for visual review. No global skill installation was needed. Local PDF, presentation, and image-generation guidance informed the production workflow.
