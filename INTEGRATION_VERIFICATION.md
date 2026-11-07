# Verified development-source milestone

Pinned latest main:9a757d86ef57ee6f0c3ca7de5c5de01084d32ef3. Candidate includes the final four motor-bebek FBXs and six bounded follow-on branches. All source changes were made in the isolated durable checkout.

- Unity compile: PASS, no C# errors.
- IntegrationChecks.RunSimulation: PASS, including both traffic suites,24seededboulevard runs,64seededlocalactivities,30migration fixtures,7playerchoices, controller/vehicle/collision/audio/map/landing checks.
- ResultFlowChecks.Run: PASS, including fullroute cards/practice/arrows, mirrors/look/pause, pitch/demo/retry/replay, save isolation, scenario-step guards and sixmap/listener/lighting transitions.
- Nativecore menu/selection/render smoke positive on earlier immutableee140d8; finalnative acceptance of this expanded source remains assigned to independent QA. No claim of controlled benchmark or speaker listening-quality certification.

Commands: use Unity6000.3.27f1 with `-batchmode -nographics -quit -executeMethod JalanKita.EditorTools.IntegrationChecks.RunSimulation`; for `ResultFlowChecks.Run`, omit `-quit` because the Playmode runner exits itself. `DrivingViewChecks.Run` additionally requires real graphics/audio and remains a finalQA gate.

Full logs are retained in ignored Unity/Builds/IntegrationEvidence. Their SHA-256 values:

{
  "jalan-latestmain-simulation.log": "5a1bad54c1fd6d68b664a4b6de40c0a98a82f7cbc469f348aa1f2031feea1bb7",
  "jalan-latestmain-flows.log": "6bd9f5caefd675b9c1194cb63b4c790ad95f488d5e429a03086798c1e63908a2"
}
