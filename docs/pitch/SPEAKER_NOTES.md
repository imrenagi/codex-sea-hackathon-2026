# RoadSense: six-minute English speaker notes

Team: **We Will Handle**. Total allocation: **360 seconds**, including **90 seconds of live demonstration**. Start a visible six-minute timer. Each segment has a hard end time; any spare time is breathing and transition time inside that segment. These are rehearsal targets, not a claim that any speaking pace will land exactly on six minutes.

| Slide | Segment | Start | End | Seconds |
|---|---|---|---|---|
| 1 | Introduction | 00:00 | 00:25 | 25 |
| 2 | The learning gap | 00:25 | 01:05 | 40 |
| 3 | The learning loop | 01:05 | 01:50 | 45 |
| 4 | Live demonstration, including app transitions | 01:50 | 03:20 | 90 |
| 5 | Indonesian context | 03:20 | 04:05 | 45 |
| 6 | Current prototype | 04:05 | 04:50 | 45 |
| 7 | Next validation | 04:50 | 05:35 | 45 |
| 8 | Closing | 05:35 | 06:00 | 25 |

## 1. Introduction — 00:00–00:25

“We are We Will Handle, and this is RoadSense: a playable space for practicing road decisions in an Indonesian setting. Our starting point is a familiar moment: someone steps onto a crossing. You know the rule, but you still need to notice them, brake, and choose when to move again.”

The cover is labeled concept artwork. The actual prototype appears on slide 4 and in the live demonstration.

## 2. The learning gap — 00:25–01:05

“At a crossing, the question is not only whether you can remember a rule. It is whether you can recognize the situation and apply it while controlling a vehicle. The pedestrian may still be moving. You may be approaching too quickly. You need to decide whether to wait. RoadSense explores how we can give learners a place to rehearse that decision, see what happened, and try again before facing the same kind of moment on a real road.”

Do not introduce accident statistics or claim that this prototype reduces crashes. This slide frames the learning problem rather than reporting research findings.

## 3. The learning loop — 01:05–01:50

“The experience begins with a road situation and a choice. When the player makes a mistake, feedback explains the action and shows the relevant rule citation. The player can then watch a driving example and retry. That connection matters: the next attempt comes with something specific to change. We have implemented this loop in the prototype. Today, we are demonstrating the interaction. We still need to evaluate how well it helps people learn and whether they can apply the lesson in a different situation.”

Point along the diagram once, then follow the return connection. Switch to the live application at **01:50**.

## 4. Live demo — 01:50–03:20

**Goal:** show the first crossing tutorial clearly, then show feedback and the example if time allows. All transitions are inside the 90-second slot. The application interface is currently Indonesian; narrate it in English. The PDF does not claim an implemented English interface.

| Pitch clock | Operator action and English narration |
|---|---|
| 01:50–02:05 | Switch to the prepared app and select Play. Play opens the car demo directly. “The demo begins in a separate session, so this practice does not replace saved journey progress.” Press W to approach the first crossing. |
| 02:05–02:25 | At the crossing, the simulation pauses and asks for braking. Press S. “The prompt appears at the decision point. I brake, and the vehicle stays stopped while the pedestrian crosses.” Do not pretend that the crossing is clear before the pedestrian actually finishes. |
| 02:25–02:40 | Wait for the pedestrian to clear. Release W if it is held, then press W again only when prompted. “Continuing requires a fresh input after the crossing is clear. Now normal driving rules resume.” |
| 02:40–03:08 | If the next junction and feedback can be reached in time, demonstrate the feedback, its rule reference, and the existing driving example. “This feedback explains the mistake. The example shows the action to try next.” Use D on feedback for the example or activate its visible example control. R retries. Describe only what actually appears. |
| 03:08–03:20 | Return to the PDF. “The same structure supports another attempt: act, understand, and retry.” If the latter interaction was not shown, say: “We have shown the crossing tutorial; feedback and the driving example are also available in the prototype.” Advance at **03:20** regardless of progress. |

Rehearse on the presenting hardware before the pitch. The 90-second slot is a presentation plan, not a recorded timing result. If input stalls, focus the app once. If the runtime cannot proceed, return to the PDF and state what was not demonstrated. Use a backup recording only if a real recording of the intended build exists, and identify it as a recording. Never imply a completed run or mistake count that the audience has not seen.

## 5. Indonesian context — 03:20–04:05

“We built the scenarios around everyday Indonesian journeys: shopping in a village, passing a school crossing, and picking up a market passenger. The vehicle choice also changes preparation. A car requires a seat belt. A motorcycle requires a helmet and headlight. These details give the practice a recognizable setting and a reason for the player to pay attention to their vehicle. The illustration represents that context; the live application is the playable prototype you just saw.”

This slide uses labeled concept artwork. Do not describe the artwork as a screenshot or a promise of production graphics.

## 6. Current prototype — 04:05–04:50

“The current prototype includes five short skill tests, three everyday missions, and a ten-stage journey. The journey covers preparation, signs and signals, priority, lane changes, school-zone decisions, junctions, railway gates, and an ambulance encounter. Car and motorcycle progress remain separate. Short sessions let the player focus on a particular situation, while the longer journey connects those situations. These numbers describe implemented content. They are not user counts, completion results, or evidence of learning impact.”

Keep this focused on playable learning content. The optional map overviews are not driving simulations and are not part of this pitch.

## 7. Next validation — 04:50–05:35

“Our next question is whether practice changes the next decision. We have not measured that yet. We plan to observe first-time players, check whether they understand the cues and controls, then compare their decisions in a new scenario after using the feedback and retry loop. That will help us separate following a prompt from understanding a situation. We would use those observations to improve the interaction before making claims about learning outcomes.”

The diagram is a proposed evaluation sequence, not an experiment already completed. There are no invented outcome figures or partner commitments.

## 8. Closing — 05:35–06:00

“RoadSense gives a road decision room to be practiced, explained, and tried again. We have a playable prototype, familiar Indonesian scenarios, and a clear next step: testing what people understand and carry into the next attempt. We are We Will Handle. Thank you, and we invite you to try RoadSense.”

Finish at **06:00**. Keep questions outside the six-minute pitch.

## Content and image sources

- Product scope: `README.md`, “Experiences,” “Saved progress,” and “Controls,” inspected on 2026-11-07.
- Current first-crossing flow: `docs/DEMO_TUTORIAL_SCENARIO.md`, inspected on 2026-11-07. This takes precedence over older demo entry instructions in other documents.
- Earlier demo narrative: `docs/ROAD_SENSE_DEMO.md`; its outdated first-entry sequence was not reused.
- Slide 4 image: existing `assets/gameplay-existing.png`, placed through a text-free crop of the road and vehicle. It is an earlier capture, not evidence of the latest tutorial UI.
- Generated artwork: built-in image generation, three distinct assets. Full prompts and provenance are in `assets/PROVENANCE.json`. Artwork on slides 1, 2, 5, and 8 is visibly labeled. The cover background intentionally returns on the closing slide.
- No external statistical, medical, legal-effectiveness, traction, or learning-impact claims are used.
