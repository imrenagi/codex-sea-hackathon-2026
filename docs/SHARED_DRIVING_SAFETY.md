# Shared driving safety and posted speed feedback

Scope: all playable Journey/Boulevard activities, Sopo Del and the Bundaran HI mission. Static city previews are not scored. This update builds on published main `278961054f3ec53b0750957bec571b67d56c75bc`, preserving its selector, stars, retry scoring, home panorama and vehicle assets.

## Rules and simulation

- Sustained travel against the permitted local lane direction fails after a short turn/transition grace. Sopo is two-way: the right half permits the opposite road tangent. HI accepts the mapped branch or ring tangent at legal joins. Low-speed deliberate reverse remains allowed; reverse never bypasses body contacts.
- Full-pose vehicle contact and authoritative pedestrian contact remain violations. Automatic following slowdown and curb correction do not count as a collision. Rejected road positions cannot create a pedestrian/direction failure from a pose that was never accepted.
- Existing training intersection/traffic-light/railway/ambulance and HI yield facts retain their existing mission behavior. Sopo's finite road slice has no authored interactive junction-priority encounter; none is invented for the static surrounding road scenery.
- Crossing groups use all three pedestrians and relative old/new poses. Village/school/market draw and contact positions share 22 authored ambient paths across those scenarios; the market waiting passenger is included and only the intentional boarding/attached state is excluded. Sopo's four sidewalk people, HI's twenty ring walkers and sixteen branch walkers use the same simulation position functions for drawing and contacts. Sidewalk people are not newly scripted to cross traffic.
- Pause freezes simulation clocks; retry resets transient safety state. Existing save keys and counted violations remain intact, including current main's continue-after-failure score semantics.

## Speed and handling

Posted limits are 30 km/h in the existing school zone, 50 on the boulevard, 30 in Sopo and 40 at HI. Existing training signs remain; Sopo/HI now show roadside signs and painted entry speeds. HUD feedback samples the same limits.

Overspeed is warning-only with onset/clear hysteresis. It does not fail the attempt or add a speed mistake/star penalty. Previous saved speed mistakes are preserved as historical progress. Posted limits do not cap the player vehicle: propulsion and rolling/air resistance determine speed; actual obstacle gaps still impose safe following/braking constraints. AI traffic keeps its existing safe cruise behavior.

Releasing throttle at HI now coasts rather than requesting a zero speed cap. A 30 km/h fixture retains 28.53 km/h after half a second; Space still brakes strongly. Curb scraping no longer instantly clips speed to 16.2 km/h or applies a large yaw jump. Shallow contacts retain smooth tangential movement, with whole-body road containment and forward/reverse recovery retained.

## Verification status

- Shared state and actual training gates: direction, lawful reverse, pause/retry, retained score, 30/50 warning-only behavior, grouped/ambient contacts and intentional boarding exemption passed.
- Actual Sopo/HI controller gate: legal and wrong-way direction, reverse/pause, Sopo30 and HI40 warning-only behavior, coast/brake, forward/rear attempted vehicle contacts, visible numeric sign/paint objects, and seven-choice boundary recovery passed.
- Shallow 5°/15° scrapes at 10/20/30 km/h remain contained with bounded per-step speed/yaw corrections.
- Final existing affected simulation and HI flow results are recorded with the source milestone/build manifest. Native display/handling acceptance belongs to the separately identified final application; prior builds do not certify this update.

Actual HI live-traffic completion passed in80.02 simulation seconds, with540 yield frames including115 pedestrian-yield frames. The safe test driver now yields to ring walker19 crossing the northern exit instead of driving into it; the real pedestrian collision detector remains unchanged. Existing full simulation also passed.

Gate log SHA-256 values:

```json
{
  "roadsense-shared-training-safety3.log": "7e7fa297afd0517f42e9203fea00b29facae75526a0a1a792cd9f2b47f3919f5",
  "roadsense-explore-safety2.log": "09c1f976653dda8ca37d9a776b7d9e24b9287ce6bdde687661db2c62868cd9c1",
  "roadsense-shared-safety-simulation.log": "5228a9fe69dbd824218c03dde84d03dfe32d85dfd53b912776c210e80144430e",
  "roadsense-shared-safety-hi-flow2.log": "6a908b0b18380edbd368e533027501320ab164ddb694fe1e2a5570e73d1168d3"
}
```
