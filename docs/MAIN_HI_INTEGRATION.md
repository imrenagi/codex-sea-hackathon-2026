# Main integration: accepted HI and current RoadSense

This development milestone combines accepted checkpoint `d33141380d26de36b53f02a1861dfd25bfbe1cd0` with GitHub main `3c34d96d3974e8b9c4f7f93db47a9b4b2c985024`. The original dirty authoring workspace and accepted application remain untouched.

## Preserved behavior

- Current main: English localization, the ordered pedestrian-crossing / three-car priority tutorial, grouped pedestrians, finite crossing traffic, thumbnails, chooser UI, and the newer Avanza-inspired MPV.
- Accepted checkpoint: Bundaran HI as the fourth everyday scenario, actual enter/yield/circulate/exit objective, retry/result flow and isolated counters; normal S brake-then-reverse; Space brake-only; road-edge recovery; paused keyboard controls; independent practice saves; responsive HUD badge; market tree/pole/cable and access-road lamp clearance.
- Crossing traffic receives both player poses for relative-motion avoidance. All three pedestrians retain the accepted reverse-aware full-pose contact checks. Guided tutorial S remains brake-only, with ordinary controls restored afterward.
- HI keeps its tested forward mission traffic itinerary, sixteen circulating actors, real yield conflicts and physical contact prevention. The unsupported optional reverse through-itinerary remains excluded from this mission.
- Six city-map overview cards remain static previews. Monas gameplay and optional visual-v2 art are not added by this milestone. Existing source/provenance branches remain preserved.

## Verification

Unity 6000.3.27f1, warm isolated project:

- Compilation passed.
- `IntegrationChecks.RunSimulation` passed: migrations, seven vehicles, local scenarios and traffic, controller, ten-stage route, audio and map data.
- `HiMissionFlowChecks.Run` passed: real controller completion in both vehicle classes, live traffic yielding and completion, catalogue/brief/result/retry, pause, reverse, save isolation and listener lifecycle.
- `NpcRuleChecks.Run` passed, including a new positive-gap relative-motion regression for each of the three finite crossing cars.
- `ResultFlowChecks.Run` passed: actual home Play through the ordered crossing and three-car priority tutorial, fresh S/W release guards, brake-only guided waiting, restored normal rules, completion/chooser/replay, both save categories, bounded violation demonstration, paused mission picker, maps and listener lifecycle.

This is a source integration milestone. Native acceptance of the previously delivered d331 application does not certify this newer localized UI. The delivered application remains unchanged until a separately identified build is accepted.

## Next authorized milestone

The shared safety and pedestrian-path helpers are preserved on `b84b593ca93ba48d5c65f033d165dc944fbd2ad6`, not silently treated as integrated rules. Their map adapters, heading/contact enforcement, physical pedestrian snapshots, posted speed signs, warning-only overspeed, uncapped-by-road-limit propulsion and the confirmed HI coast/scrape smoothness fixes remain pending. Optional visual imports are deferred so they cannot replace the newer MPV or undo accepted clearance/lifecycle fixes.

Exact gate log SHA-256 values (copies retained in ignored `Unity/Builds/MainHiIntegrationEvidence`):

```json
{
  "roadsense-main-hi-compile.log": "a819e42643eaae9f7d26e551a75ddc09801803896ca3525c4aca386607426139",
  "roadsense-main-hi-simulation3.log": "80604c5bbb48501b9858bc73c86e7e894988651c1194f28a13a4135d4deb7f69",
  "roadsense-main-hi-flow.log": "533475392229cb1e99cbcf35a04b765634585de4c949be6336e20812b5be0ddd",
  "roadsense-main-hi-relative-npc.log": "7c5fe19608d56c41a035e9769422676b50b3775ec98a737bb8579b3bd82962aa",
  "roadsense-main-hi-result-flow.log": "476e71a7e91ec90bebc47fc93d417f87b8cfb981cca2fbc417fc8bdbe1320be7"
}
```
