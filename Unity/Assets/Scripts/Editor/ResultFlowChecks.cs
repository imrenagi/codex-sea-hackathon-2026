using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita.EditorTools
{
    [InitializeOnLoad]
    public static class ResultFlowChecks
    {
        const string Running = "JalanKita.ResultFlowChecks";
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        static ResultFlowChecks()
        {
            if (SessionState.GetBool(Running, false)) EditorApplication.update += AwaitGame;
        }

        public static void Run()
        {
            JourneyChecks.Run();
            EditorSceneManager.OpenScene(ProjectSetup.ScenePath);
            SessionState.SetBool(Running, true);
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("Jalan Kita/Verify Result Screens (Play mode)")]
        public static void RunInOpenEditor()
        {
            var game = UnityEngine.Object.FindFirstObjectByType<JalanGame>();
            if (!EditorApplication.isPlaying || !game) throw new InvalidOperationException("Enter Play mode first.");
            game.enabled = false;
            try
            {
                foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode))) { Verify(game, mode); VerifyMissionSelection(game, mode); }
                Debug.Log("Result screens passed: both vehicles, all ten cards, pause/resume and checkpoint retry.");
            }
            finally { game.enabled = true; Call(game, "ShowMenu"); }
        }

        static void AwaitGame()
        {
            if (!EditorApplication.isPlaying) return;
            var game = UnityEngine.Object.FindFirstObjectByType<JalanGame>();
            if (!game || Field(game, "journey") == null) return;
            EditorApplication.update -= AwaitGame;
            SessionState.SetBool(Running, false);
            try
            {
                game.enabled = false;
                foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode))) { Verify(game, mode); VerifyMissionSelection(game, mode); }
                VerifyArrowDirections();
                IntegrationChecks.VerifyUI(game);
                VerifyMirrorsAndLook(game);
                SignalChecks.Verify(game);
                Debug.Log("Result flow checks passed: both vehicles, all ten completion cards, mission selection, saved progress, arrow directions, continuous resume, final quiz and checkpoint retry.");
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        static void Verify(JalanGame game, VehicleMode mode)
        {
            Set(game, "practiceStart", -1);
            var run = new Journey(mode, new JourneySave(), persistent: false, trafficSeed: 1);
            Set(game, "mode", mode);
            Set(game, "journey", run);
            run.DriverGear = run.PassengerGear = run.Headlight = true;
            Require(run.Depart(), "Departure");
            var root = game.GetComponent<UIDocument>().rootVisualElement;
            for (int completed = 1; completed <= RouteStage.All.Length; completed++)
            {
                Require(run.Stage == completed, "Checkpoint sequence");
                var position = run.Position;
                float speed = run.Speed;
                Call(game, "AnnounceCheckpoint");
                Require(Page(game) == "Checkpoint", "Completion page");
                Require(root.Q<Label>(className: "stamp")?.text == "MISI SELESAI!", "Success banner");
                Call(game, "FixedUpdate");
                Require(run.Position == position && run.Speed == speed, "Pause preserves driving state");
                Call(game, "ContinueCheckpoint");
                Require(run.Position == position && run.Speed == speed, "Continue never teleports or resets speed");
                Require(Page(game) == (run.Finished ? "After" : "Drive"), "Final completion opens quiz");
                if (run.Finished) break;
                for (int frame = 0; frame < 9000 && run.Stage == completed && !run.Failure.HasValue; frame++)
                    DrivingCoach.Step(run, .02f);
                Require(!run.Failure.HasValue, "Route remains driveable");
            }

            run = new Journey(mode, new JourneySave { next = 1 }, persistent: false, trafficSeed: 1);
            Set(game, "journey", run);
            for (int frame = 0; frame < 1000 && !run.Failure.HasValue; frame++)
                run.Step(.02f, true, false, 0);
            Require(run.Failure == Rule.Stop, "STOP violation");
            Call(game, "ShowLesson", Rule.Stop);
            Require(root.Q<Label>(className: "stamp")?.text == "PELANGGARAN!", "Violation banner");
            Require(root.Query<VisualElement>(className: "fold").ToList().Count == 2, "Explanation and legal folds");
            Call(game, "Retry");
            Require(Page(game) == "Drive" && run.Stage == 1 && run.Speed == 0
                && run.Position.y == RouteStage.All[0].End, "Retry uses the saved checkpoint");
            Require(!root.Q("dialog").ClassListContains("result-layout"), "Result styling is cleared");
        }

        static void VerifyMissionSelection(JalanGame game, VehicleMode mode)
        {
            string key = JourneySave.Key(mode), backup = PlayerPrefs.GetString(key);
            bool existed = PlayerPrefs.HasKey(key);
            try
            {
                var saved = new JourneySave { next = 3, before = 2 };
                saved.mistakes[(int)Rule.Stop] = 2;
                saved.Store(mode);
                string original = PlayerPrefs.GetString(key);
                Set(game, "mode", mode);
                var root = game.GetComponent<UIDocument>().rootVisualElement;
                for (int stage = 0; stage < RouteStage.All.Length; stage++)
                {
                    // Exercise the actual Button activation handler, including keyboard support.
                    var button = root.Q<Button>("mission" + (stage+1).ToString("00"));
                    Require(button != null && button.focusable, "Mission selector is a button");
                    button.Focus();
                    using (var submit = NavigationSubmitEvent.GetPooled()) { submit.target=button; button.SendEvent(submit); }
                    var run = (Journey)Field(game, "journey");
                    var start = RouteStage.All[Mathf.Max(0, stage-1)];
                    Require(run.Stage == stage && run.Position == start.Checkpoint && run.Heading == start.Heading
                        && run.Speed == 0 && !run.Failure.HasValue, $"Selected mission {stage} resets to its start pose: got {run.Stage}, {run.Position}, {run.Heading}, page {Page(game)}");
                    Require(Page(game) == (stage == 0 ? "Preflight" : "Drive"), "Selected mission opens correct screen");
                    if (stage == 0)
                    {
                        run.DriverGear = run.PassengerGear = run.Headlight = true;
                        Require(run.Depart(), "Selected preflight works");
                    }
                    else
                    {
                        for (int i = 0; i < 12000 && run.Stage == stage && !run.Failure.HasValue; i++) DrivingCoach.Step(run,.02f);
                        Require(run.Stage == stage+1 && !run.Failure.HasValue, "Selected mission can complete");
                    }
                    Call(game, "AnnounceCheckpoint");
                    Require(root.Q<Label>(className:"checkpoint-saved").text.Contains("Latihan"), "Practice never claims skipped missions saved");
                    Call(game, "ContinueCheckpoint");
                    Require(Page(game) == (run.Finished ? "Progress" : "Drive"), "Practice finishes without overwriting quiz");
                    Require(PlayerPrefs.GetString(key) == original, "Jump and completion preserve saved journey");
                }
                Call(game,"BackToMenu");
                Call(game,"Resume");
                Require(((Journey)Field(game,"journey")).Stage == 3 && (int)Field(game,"practiceStart") == -1,
                    "Normal journey resumes after practice");
            }
            finally
            {
                if (existed) PlayerPrefs.SetString(key,backup); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                Set(game,"practiceStart",-1);
            }
        }

        static void VerifyArrowDirections()
        {
            // Check imported mesh geometry using each rendered marking's actual wrapper rotation.
            var markings = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            int checkedCount = 0;
            foreach (var marking in markings)
            {
                if (!marking.name.StartsWith("LaneArrow_") || marking.parent == null || marking.parent.name != "Kampung Kenanga") continue;
                var model = UjianSim.Game.Models.Prefab(marking.name);
                var probe = UnityEngine.Object.Instantiate(model);
                try
                {
                    probe.transform.SetParent(null);
                    float heading = marking.position.x > 5 ? 180 : 0;
                    if (marking.position.z > 390)
                    {
                        Vector2 point = new(marking.position.x,marking.position.z);
                        Vector2 center = Boulevard.Center(5);
                        for (int stage=6;stage<=7;stage++)
                            if ((point-Boulevard.Center(stage)).sqrMagnitude < (point-center).sqrMagnitude) center=Boulevard.Center(stage);
                        var toward = center-point;
                        heading = Mathf.Round(Mathf.Atan2(toward.x,toward.y)*Mathf.Rad2Deg/90)*90;
                    }
                    var trafficFrame = Quaternion.Inverse(Quaternion.Euler(0,heading,0));
                    var relative = new System.Collections.Generic.List<Vector3>();
                    foreach (var mesh in probe.GetComponentsInChildren<MeshFilter>())
                        foreach (var v in mesh.sharedMesh.vertices) relative.Add(trafficFrame * (marking.rotation * (mesh.transform.TransformPoint(v)-probe.transform.position)));
                    if (marking.name.EndsWith("Left")) Require(relative.Exists(v=>v.x < -1), "Left arrow points left with corrected wrapper");
                    else if (marking.name.EndsWith("Right")) Require(relative.Exists(v=>v.x > 1), "Right arrow points right with corrected wrapper");
                    else
                    {
                        float tipWidth=0;
                        foreach (var v in relative) if (v.z > 2.4f) tipWidth=Mathf.Max(tipWidth,Mathf.Abs(v.x));
                        Require(relative.Exists(v=>v.z > 2.4f) && tipWidth < .05f,"Straight arrow tip faces forward");
                    }
                    checkedCount++;
                }
                finally { UnityEngine.Object.DestroyImmediate(probe); }
            }
            Require(checkedCount >= 36, "All junction arrow models inspected");
        }

        static void VerifyMirrorsAndLook(JalanGame game)
        {
            var district=(District)Field(game,"district");
            var root=game.GetComponent<UIDocument>().rootVisualElement;
            Require(root.Q("mirrors").Query<Image>().ToList().Count==3,"Three live mirror views are bound to HUD");
            Require(root.Q<Button>("lookLeft")!=null && root.Q<Button>("lookRight")!=null,"Look buttons available");
            foreach(VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            {
                var run=new Journey(mode,new JourneySave { next=5 },persistent:false,trafficSeed:1);
                district.Snap();
                district.Present(run,false,1,true,-1);
                Require(Camera.main.transform.forward.x < -.8f,"Z looks left without steering the vehicle");
                district.Present(run,false,1,true,1);
                Require(Camera.main.transform.forward.x > .8f && run.Heading==0,"X looks right without steering the vehicle");
                district.Present(run,false,1,true,0);
                Require(Camera.main.transform.forward.z > .8f,"Releasing look returns forward");
                foreach(var mirror in district.Mirrors.Cameras)
                    Require(mirror.targetTexture!=null && mirror.transform.forward.z < -.8f,"Mirrors face behind the vehicle");
                district.Present(run,false,.02f,false);
                foreach(var mirror in district.Mirrors.Cameras) Require(!mirror.enabled,"Paused mirrors stop rendering");
            }
            Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"Only the driving camera has an audio listener");
        }

        static object Field(JalanGame game, string name) => typeof(JalanGame).GetField(name, Private).GetValue(game);
        static void Set(JalanGame game, string name, object value) => typeof(JalanGame).GetField(name, Private).SetValue(game, value);
        static string Page(JalanGame game) => Field(game, "page").ToString();
        static void Call(JalanGame game, string name, params object[] args) => typeof(JalanGame).GetMethod(name, Private).Invoke(game, args);
        static void Require(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException(message);
        }
    }
}
