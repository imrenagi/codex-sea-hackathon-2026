# Reusable tutorial steps

`DrivingTutorial.cs` defines the configuration and runner. `DemoTutorialScenarios.cs` binds the approved pedestrian lesson and the next junction lesson to Pasar Bunga. `JalanGame.StartTutorial(TutorialScenario)` is the entry point after the host has loaded the scenario's map.

## Scenario binding

A `TutorialScenario` supplies:

- Stable `Id` and `MapId` identifiers.
- A `CreateJourney` factory, including its vehicle mode, starting stage and mission. Use a nonpersistent journey for a demo.
- An `IsComplete` predicate and localized title, initial instruction and completion-summary keys.
- Ordered `TutorialStep` definitions. The runner starts only the next pending step, and completes it once per run. Retry constructs a fresh runner and journey from the same scenario.

`MapId` identifies the binding; it does not load an arbitrary scene. The existing map host remains responsible for loading its road geometry before starting the scenario.

## Step configuration

| Field | Meaning |
| --- | --- |
| `Id` | Stable step identity within the scenario. |
| `Zone` | A `Rect` in `Journey.Position` coordinates: horizontal x, route z stored as y. |
| `TriggerWhen` | Additional stage, actor or rule-state condition. It must become true inside the zone before the hazard. |
| `Action` | Expected action: Brake (S), SignalLeft (Q), or SignalRight (E). |
| Instruction keys | Heading, requested action, braking, waiting and success text, resolved through `GameText.Get` when displayed. |
| `PromptSimulation` | `FreezeWorld` for a paused prompt, or `BrakeVehicle` to keep actors moving while awaiting input. |
| `WaitSimulation` | `BrakeVehicle` keeps braking while actors simulate. `FreezeWorld` freezes the scene; use it only when the completion condition can already be satisfied without moving actors. |
| `CompleteWhen` | Actual world/rule predicate. The runner additionally requires zero speed and a fresh W press before returning control. |
| `RuleId` | Existing `Rule` enum reference. `RuleReference` resolves the existing `Lesson` entry, including regulation, citation and source URL. |

The adapter never advances a guided mission or records a violation while a step owns control. `LearningControls` is cleared only when the safe condition is met and a fresh W press completes the step. The initial approach to the first lesson also receives instructional grace.

## Add a map or rule

1. Create a `TutorialScenario` for the map, with a journey factory and completion condition appropriate to that route.
2. Add ordered steps with zones that begin early enough to stop safely. Start actor-dependent steps at actor activation; waiting too long to trigger can let a slow approach miss the encounter.
3. Supply predicates using that map's real simulation state. Do not replace crossing clearance with a timer.
4. Reference existing `Rule` IDs. Add or correct legal material in the existing lesson/law catalog, rather than duplicating it in tutorial configuration.
5. Add the step's keys to `Resources/Localization/en.json`. Keys remain in configuration so a locale change does not require rebuilding step definitions.
6. Load the map through its host, then call `game.StartTutorial(scenario)`. Retry/replay keeps the injected scenario.

For a real turn, configure a signal step before its turn zone with `SignalLeft` or `SignalRight`, a matching localized Q/E instruction and a clearance predicate appropriate to that movement. Signal actions set the requested direction without toggling an already-correct indicator off. Pasar Bunga's current demo is straight, so it does not inject this step.

## Included rule bindings

- `pedestrian-crossing` → `Rule.Pedestrian`; completion is the end of the actual pedestrian actor.
- `main-road-junction` → `Rule.Junction`; completion reuses `DrivingCoach.TrafficComing`'s safe directional clearance check for `RouteStage.MainJunction`.

Both examples pause the world for S, then run the world with the vehicle braked while waiting. The existing compact top-left UI, Esc dismissal and fresh-input guard are shared by the runner adapter.

## Focused mistake examples

`IViolationExampleProvider.TryGetExample` maps a failed journey and its existing `Rule` to a `ViolationExample`. A descriptor supplies a section ID, detached start-snapshot factory, demonstration behavior, completion predicate and catalog reference. Inject another provider through `TutorialScenario.Examples` for another map.

`ViolationExampleSession` retains the exact failed journey as return/retry context and simulates only its separate, nonpersistent preview. Completion, Esc/Tab/R cancellation or focus loss returns to the same feedback. The learner's mistakes, checkpoint, position and save are unchanged; watching does not run tutorial steps or credits.

The default route provider supports:

| Failed rule/section | Example starts | Example ends |
| --- | --- | --- |
| Pedestrian, route or school crossing | 19 m before that crossing | Just beyond that crossing |
| Junction, Pasar Bunga | 20 m before its yield line | Just beyond the main-road junction |
| Stop or junction, stop-sign section | Before the stop line | Just beyond that junction |
| Signal, traffic-light section | Before the light's stop line | Just beyond that junction |

Unsupported sections show an explicit unavailable message beside the disabled example action. They never fall back to replaying the whole journey. `Journey.CreateSectionExample` deep-copies save state, preserves the encounter seed/variant, resets only the preview's maneuver state and positions it at the selected section.
