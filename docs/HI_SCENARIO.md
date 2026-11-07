# Bundaran HI scenario

Based on the accepted RoadSense `e242d9725d4c0d63513952aec480d4aab683b2a3` checkpoint. No newer upstream main merge is included.

Open **Play → Mode lainnya → Misi sehari-hari → Melintasi Bundaran HI**. The existing vehicle choice, briefing, HUD, keyboard controls, pause and result styles are retained. Approach from Sudirman, yield to a conflicting stream, circulate clockwise, then leave toward Thamrin. A clear gap does not require an arbitrary stop. Indicators remain guidance. Reverse cannot skip objective gates.

Only HI is exposed. The six static map previews and Sopo Del remain available as before. Monas geometry shared in donor modules is not a new playable entry; the separate visual-v2 work is not included.

Results use `RoadSense.Mission.HI.v1.Car` / `RoadSense.Mission.HI.v1.Motorcycle`, with attempts, completions and failures. Existing journey and scenario saves are not changed. A new attempt starts from the approach; mid-mission progress is not promised to persist.

Provenance: six Landmark runtime modules and JakartaLandmarks resources were extracted from committed donor `5e10f2b4c95a931f6fdaf8a1ccfd2c3a7f9200ad`, never its dirty integration tree. Objective/checks originated at `bab3ac8251ad7879bc3efcacbc5d65ed5c4cd347`. The adapter is new. The through-traffic exit connector was corrected to join a forward outgoing-road station: its old centerline link doubled back across circulating lanes and deadlocked two NPCs. Physical overlap and yielding protections remain active. This bounded mission stages four forward Sudirman–Thamrin access actors and all sixteen circulating actors. The optional reverse-direction access itinerary is not spawned: its southern exit connector has not passed the same body-clearance gate. Both mapped approaches and their scenery remain.

Targeted verification: `JalanKita.EditorTools.HiObjectiveChecks.Run` covers safe/unsafe ordered gates, backwards shortcuts, pause/reset, swept contact blocking and the through-route road envelope. `JalanKita.EditorTools.HiMissionFlowChecks.Run` covers actual UI buttons, both vehicle modes' controller completion, result/retry, pause/reverse, save isolation, audio listener lifecycle and one default live-traffic completion. This is a bounded new-scenario gate, not a repeat of the complete prior regression matrix. Native acceptance is recorded adjacent to the immutable build.
