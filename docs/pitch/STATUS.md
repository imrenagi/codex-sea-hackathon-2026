# RoadSense pitch production

Updated on 7 November 2026 to follow `PITCH.md`. Eight 16:9 slides preserve the existing charcoal, cream, and red design, typography, and original artwork.

## Deliverables

- `RoadSense-Pitch-Draft.pdf`: presentation PDF aligned with the current pitch.
- `PITCH.md`: authoritative spoken text, mission cues, and claim notes. The deck update leaves this file unchanged.
- `SPEAKER_NOTES.md`: slide-to-script timing, operator notes, sources, and a copy of the current pitch. The generator refreshes it from `PITCH.md` on each build.
- `build_deck.py` and `template.py`: editable slide text, layout, and drawing source.
- `assets/`: preserved artwork, earlier gameplay capture, and provenance.
- Desktop copy: `~/Desktop/RoadSense-Pitch.pdf`.

## Slide sequence

| Slide | Topic | Pitch clock |
|---|---|---|
| 1 | Road frustration | 0:00-0:35 |
| 2 | The testing gap | 0:35-1:25 |
| 3 | Rp450 trillion national cost | 1:25-1:45 |
| 4 | Illustrative 1% saving and RoadSense | 1:45-2:15 |
| 5 | Live crossing and junction demo | 2:15-4:20 |
| 6 | Codex contribution | 4:20-4:50 |
| 7 | Proposed licensing and driver-partner uses | 4:50-5:35 |
| 8 | Closing | 5:35-5:45 |

Hold the final slide through the 15-second buffer. The demo lasts 125 seconds. Timing is a rehearsal plan, not a recorded result.

The national loss uses the 2020 baseline from Perpres 1/2022. The Rp4.5 trillion figure is an illustrative reduction in national economic losses, not measured product impact. Licensing requirements and Gojek/Grab use are proposals. Slides 3 and 4 include source links and the relevant qualifications.

Generated artwork remains labelled. Slide 5 uses a labelled crop from an earlier real prototype capture. No generated UI, invented task capture, or learner result has been added. The script requests a genuine Codex task capture; prepare it for display with slide 6.

The current tutorial documentation differs from the deliberate-violation sequence in `PITCH.md`. `SPEAKER_NOTES.md` flags this for rehearsal without changing the requested script or game behavior.

## Rebuild

Use Python with `reportlab`, `pymupdf`, and `pillow`. Fonts are Arial and Georgia from the macOS supplemental font directory.

```bash
python docs/pitch/build_deck.py --desktop
```

The generator renders every page and writes a contact sheet, extracted text, source-script hash, and page-boundary report to the operating system's temporary directory under `roadsense-pitch-review/`. No game builds or runtime tests are part of this document workflow.
