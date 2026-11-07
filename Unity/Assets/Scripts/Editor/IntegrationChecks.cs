using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita.EditorTools
{
    public static class IntegrationChecks
    {
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException("INTEGRATION: " + message); }
        static object Field(JalanGame game, string name) => typeof(JalanGame).GetField(name, Private).GetValue(game);
        static void Set(JalanGame game, string name, object value) => typeof(JalanGame).GetField(name, Private).SetValue(game, value);
        static void Call(JalanGame game, string name, params object[] args) => typeof(JalanGame).GetMethod(name, Private).Invoke(game, args);
        static string Page(JalanGame game) => Field(game, "page").ToString();

        public sealed class SavedPreferences : IDisposable
        {
            readonly Dictionary<string, string> values = new();
            public SavedPreferences()
            {
                foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
                {
                    foreach (var mission in new Mission[] { null }.Concat(Mission.Tests).Concat(Mission.Scenarios))
                    {
                        Remember(JourneySave.Key(mode, mission)); Remember(JourneySave.LegacyKey(mode, mission));
                    }
                    Remember("JalanKita.Vehicle." + mode);
                }
            }
            void Remember(string key) => values[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
            public void Dispose()
            {
                foreach (var pair in values)
                    if (pair.Value == null) PlayerPrefs.DeleteKey(pair.Key); else PlayerPrefs.SetString(pair.Key, pair.Value);
                PlayerPrefs.Save();
            }
        }

        [MenuItem("Jalan Kita/Verify combined simulation")]
        public static void RunSimulation()
        {
            using var preferences = new SavedPreferences();
            VerifyMigration();
            StreetLookChecks.MotionChecks();
            TrafficChecks.Run(); // Includes vehicle, missions, full route, steering, sound and exploration checks.
            CityMapChecks.Run();
            VerifyCrossingGeometry();
            foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode))) VerifyVillagePhone(mode);
            Require(VehicleCatalogue.All.Length == 7, "All seven selectable vehicles retained");
            Require(VehicleCatalogue.All.Where(v => v.Beat).All(v => v.Label.Contains("motor bebek") && !v.Label.Contains("BeAT")), "Requested visible vehicle wording");
            Debug.Log("INTEGRATED_SIMULATION_PASSED migration, seven vehicles, local missions/traffic/controller, ten-stage route, audio and six map datasets.");
        }

        public static void VerifyMigration()
        {
            using var preferences = new SavedPreferences();
            foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            foreach (int count in new[] { 16, 17, 18, 19, 23 })
            foreach (var mission in new[] { (Mission)null, Mission.Scenarios[1], Mission.Scenarios[2] })
            {
                string oldKey = JourneySave.LegacyKey(mode, mission), key = JourneySave.Key(mode, mission);
                PlayerPrefs.DeleteKey(key);
                var legacy = new JourneySave { next = mission?.FirstStage ?? 5, schema = 0, before = 2, after = 1,
                    checkpoint = 2, variant = 3, attempts = 4, completions = 2, bestMistakes = 3, retryPending = true, trafficSeed = 9126,
                    mistakes = Enumerable.Range(0, count).Select(i => 100 + i).ToArray() };
                string original = JsonUtility.ToJson(legacy);
                PlayerPrefs.SetString(oldKey, original);
                var loaded = JourneySave.Load(mode, mission);
                Require(loaded != null && loaded.mistakes.Length == 26 && loaded.schema == 2, "Schema migration " + count);
                for (int i = 0; i < count; i++)
                {
                    int target = count == 23 && i >= 16 ? i + 3 : i;
                    Require(loaded.mistakes[target] == 100 + i, "Exact rule migration " + count + "/" + i);
                }
                if (count == 23) for (int i = 16; i < 19; i++) Require(loaded.mistakes[i] == 0, "No false local violations");
                Require(loaded.checkpoint == 2 && loaded.variant == 3 && loaded.attempts == 4 && loaded.completions == 2
                    && loaded.bestMistakes == 3 && loaded.retryPending && loaded.trafficSeed == 9126, "Mission bookkeeping survives migration");
                Require(!PlayerPrefs.HasKey(key) && PlayerPrefs.GetString(oldKey) == original, "Read-only load preserves original keys");
                if (mission == null) Require(loaded.next == 5 && loaded.after == -1 && loaded.legacyAfter == 1, "Old completed route extends without losing previous quiz");
                loaded.Store(mode, mission);
                Require(JourneySave.Load(mode, mission).mistakes.SequenceEqual(loaded.mistakes), "v2 roundtrip");
                Require(PlayerPrefs.GetString(oldKey) == original, "Explicit v2 save preserves legacy raw JSON");
            }
            Debug.Log("INTEGRATED_SAVE_MIGRATION_PASSED 30 mode/schema/mission combinations; exact rule counters and immutable legacy values.");
        }

        static void VerifyCrossingGeometry()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            var host = new GameObject("Combined crossing checks");
            try
            {
                new District(host.transform);
                var ramps = host.GetComponentsInChildren<MeshFilter>().Where(m => m.name == "Landasan penyeberangan").ToArray();
                Require(ramps.Length == 2, "Both sides have crossing landings");
                foreach (var ramp in ramps)
                {
                    Require(ramp.GetComponent<Renderer>().sharedMaterial != null, "Landing material exists with local StreetLook paving");
                    var vertices = ramp.sharedMesh.vertices.Select(ramp.transform.TransformPoint).ToArray();
                    Require(vertices.All(v => Mathf.Abs(v.x) >= 4.66f && Mathf.Abs(v.x) <= 5.51f), "Ramp uses retained sidewalk width");
                    Require(vertices.Min(v => v.y) < .01f && vertices.Max(v => v.y) >= .12f, "Ramp joins street to sidewalk");
                }
                Require(host.GetComponentsInChildren<Transform>().Any(t => t.name.StartsWith("Pasar Bunga ·")), "Flower market frontage retained");
                Require(host.GetComponentsInChildren<Transform>().Any(t => t.name == "Teluk jemput pasar"), "Separate passenger market retained");
                Require(host.GetComponentsInChildren<Transform>().Any(t => t.name == "Pola taktil penyeberangan"), "Tactile paving retained");
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
            Debug.Log("INTEGRATED_CROSSING_GEOMETRY_PASSED landing materials/dimensions, tactile paving, two distinct market areas.");
        }

        static void VerifyVillagePhone(VehicleMode mode)
        {
            var run = new Journey(mode, Mission.Tests[4].NewSave(), persistent: false, mission: Mission.Tests[4]);
            Require(!run.UsesRouteSteering, "Local skill retains RoadVehicleMotion");
            for (int i = 0; i < 700 && run.Position.y < RouteStage.PhoneStart + 4.2f; i++)
                run.Step(.02f, run.Speed < 7.4f, run.Speed > 7.7f, 0);
            for (int i = 0; i < 500 && run.Position.y < RouteStage.PhoneEnd - 8 && !run.Failure.HasValue; i++)
            {
                float steer = RoadVehicleMotion.TrackLane(run.Mode, run.Speed, run.Heading, run.Position.x, -5.7f);
                run.Step(.02f, run.Speed < 4.8f, run.Speed > 5, steer);
            }
            for (int i = 0; i < 150 && run.Speed > 0; i++)
                run.Step(.02f, false, true, RoadVehicleMotion.TrackLane(run.Mode, run.Speed, run.Heading, run.Position.x, -5.7f));
            Require(run.PhoneRinging && !run.Failure.HasValue && run.Speed == 0 && run.Position.x < -4.6f
                && run.Position.x > -6.8f, "Local controller aligns and stops in phone bay " + mode);
            run.ResolvePhone(true);
            Require(!run.PhoneRinging && !run.Failure.HasValue, "Safe local phone response");
        }

        // Executed by ResultFlowChecks after its full-route and practice UI assertions.
        public static void VerifyUI(JalanGame game)
        {
            using var preferences = new SavedPreferences();
            var root = game.GetComponent<UIDocument>().rootVisualElement;
            foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            {
                Set(game, "mode", mode);
                var saved = new JourneySave { next = 3, before = 2, trafficSeed = 12345 };
                saved.mistakes[(int)Rule.Stop] = 2; saved.Store(mode);
                string key = JourneySave.Key(mode), original = PlayerPrefs.GetString(key);
                Call(game, "ShowMenu"); Call(game, "ShowCatalogue", MissionKind.Scenario);
                Require(PlayerPrefs.GetString(key) == original, "Browsing menus preserves full journey");
                var activity = Mission.Scenarios[2];
                Call(game, "StartMission", activity, null, false);
                Call(game, "RefreshHud");
                var run = (Journey)Field(game, "journey");
                var button = root.Q<Button>("mission01");
                Require(!button.enabledSelf && !button.focusable, "Scenario step is not practice navigation");
                Call(game, "JumpToMission", 0);
                Require(ReferenceEquals(run, Field(game, "journey")), "Scenario handler guards practice entry");
                string missionKey = JourneySave.Key(mode, activity), missionValue = PlayerPrefs.GetString(missionKey);
                Call(game, "ShowMenu"); Call(game, "StartPitchDemo");
                run = (Journey)Field(game, "journey");
                Require(run.Stage == 2 && run.Activity == null && run.UsesRouteSteering, "Pitch starts its isolated route encounter");
                for (int i = 0; i < 1500 && !run.Failure.HasValue; i++) run.Step(.02f, true, false, 0);
                Require(run.Failure == Rule.Pedestrian, "Pitch reproducible pedestrian violation");
                Call(game, "ShowLesson", run.Failure.Value);
                Require(root.Query<Label>(className: "rule-source").ToList().Any(x => x.text.Contains("Pasal")), "Legal citation visible without opening fold");
                Call(game, "StartDemonstration");
                var demo = (Journey)Field(game, "journey");
                for (int i = 0; i < 10000 && demo.Stage == 2 && !demo.Failure.HasValue; i++) DrivingCoach.Step(demo, .02f);
                Require(demo.Stage == 3 && !demo.Failure.HasValue, "Pitch demonstration completes");
                Call(game, "EndDemonstration");
                Require(ReferenceEquals(run, Field(game, "journey")) && run.MistakeCount == 1, "Demo restores actual learner mistake count");
                Call(game, "Retry");
                for (int i = 0; i < 10000 && run.Stage == 2 && !run.Failure.HasValue; i++) DrivingCoach.Step(run, .02f);
                Require(run.Stage == 3 && !run.Failure.HasValue, "Pitch retry completes");
                Call(game, "AnnounceCheckpoint");
                Require(Page(game) == "Checkpoint" && root.Q<Label>(className: "stamp").text == "DEMO SELESAI!", "Pitch completion stays bounded");
                Call(game, "Retry");
                Require(((Journey)Field(game, "journey")).Stage == 2 && ((Journey)Field(game, "journey")).MistakeCount == 0, "Pitch replay is a fresh isolated session");
                Require(PlayerPrefs.GetString(key) == original && PlayerPrefs.GetString(missionKey) == missionValue, "Pitch failure/demo/retry/completion/replay preserve both save categories");
                Call(game, "ShowMenu"); Call(game, "Resume");
                Require(((Journey)Field(game, "journey")).Stage == 3, "Main journey resumes after pitch");
            }
            Call(game, "ShowMenu"); Require(Listeners() == 1, "One menu audio listener");
            Call(game, "StartJaksa"); Require(Listeners() == 1, "One exploration audio listener");
            Call(game, "ShowMenu"); Require(Listeners() == 1, "Exploration listener disabled on exit");
            for (int i = 0; i < CityMapData.Ids.Length; i++)
            {
                Call(game, "OpenCityMap", i);
                Require(Page(game) == "CityPreview" && Listeners() == 1, "Map " + i + " has one active listener");
                var preview = Field(game, "cityPreview");
                Require(preview != null, "Map preview created");
                Call(game, "ShowCityMaps"); Require(Listeners() == 1, "Map listener restored on exit");
            }
            Call(game, "ShowMenu"); Call(game, "LateUpdate");
            Require(RenderSettings.sun && RenderSettings.sun.isActiveAndEnabled, "Menu lighting restored after maps");
            Debug.Log("INTEGRATED_UI_FLOW_PASSED scenario-step guard, isolated pitch/demo/retry/replay, saves, six maps, scene lighting and audio listener lifecycle.");
        }
        static int Listeners() => UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l => l.isActiveAndEnabled);
    }
}
