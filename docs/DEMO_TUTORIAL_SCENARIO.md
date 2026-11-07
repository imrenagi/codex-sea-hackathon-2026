# Tutorial inside the demo

Play starts the Pasar Bunga demo with a car. Instructions use the approved compact, dark, translucent box at the top left. The road and pedestrians stay visible. Each step shows one instruction at a time.

## Pedestrian crossing

1. **Press [W] to start driving.** This initial hint is small, regular text slightly below the screen center, with no opaque panel; it disappears on W. Initial guidance does not record penalties or advance the mission.
2. At the crossing's activation zone, before the zebra line, the world pauses: **Pedestrian crossing ahead — Press [S] to brake**.
3. Release S if already held, then press it. The world resumes and the car brakes automatically. Throttle and steering cannot bypass the step.
4. **Wait for the pedestrian to finish crossing.** The car stays stopped while the pedestrian keeps walking.
5. Only after the car is fully stopped and the pedestrian actor has finished does **Press [W] to continue** appear. A fresh W press resumes normal driving; holding W cannot advance the lesson.

## Junction and cross traffic

6. Continue straight toward the main road. At the cross-traffic activation zone, 20 m before the yield line, the world pauses: **Junction ahead · yield to cross traffic — Press [S] to brake**.
7. A fresh S press resumes the world with automatic braking. **Stop before the junction** remains visible while slowing down.
8. **Wait for cross traffic to pass.** The car stays stopped while cross traffic moves. The step uses the same directional clearance predicate as the existing driving coach, not a delay or countdown.
9. Once fully stopped and cross traffic has cleared the existing safe threshold, **Press [W] to continue** appears. A fresh W press restores normal rules.
10. Continue straight through the junction to finish the demo. This route does not require a turn, so it does not introduce an artificial Q/E signal step.

## Controls and boundaries

- Esc on either paused tutorial prompt closes only that prompt, applies the safe action and resumes the guarded waiting phase. It does not navigate to the menu or bypass clearance.
- Esc/Tab while driving opens pause. Focus loss also pauses driving. Returning requires releasing controls; fresh-action detection survives pauses.
- R/replay restarts the same scenario with a new journey and resets every step.
- During each guided step, penalties and mission/checkpoint advancement are disabled. Normal rules return after the fresh W press. Demo journeys are nonpersistent.
- Tutorial instructions suppress competing objective, toast and control-hint text. Success feedback temporarily replaces the objective and wraps within a dark box.
- See Example starts immediately before the failed maneuver, ends just beyond it, and returns to the same feedback with the original failure and save unchanged. It never repeats unrelated onboarding or the whole demo.

Source changes are synced without controlling Unity or Play Mode. The user controls reloading and live verification; no automated gameplay test, build or explicit compile was run.

See [Reusable tutorial steps](TUTORIAL_STEPS.md) for map/scenario injection, localization keys and rule references.
