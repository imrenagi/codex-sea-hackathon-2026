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
                VerifySignalTransitions(game);
                VerifyDrivingKeyboard(game);
                JakartaIntegrationChecks.VerifyUI(game);
                VerifyMissionPickerModal(game);
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
                    Call(game, "JumpToMission", 1);
                    Call(game, "RefreshHud");
                    var picker = root.Q<Button>("missionPicker");
                    Require(picker.text.Contains(RouteStage.All[1].Title), "Current mission title is visible");
                    using (var open = NavigationSubmitEvent.GetPooled()) { open.target=picker; picker.SendEvent(open); }
                    Require(!root.Q("missionPickerPopup").ClassListContains("hidden"), "Mission title opens picker");
                    // Exercise the actual Button activation handler, including keyboard support.
                    var button = root.Q<Button>("mission" + (stage+1).ToString("00"));
                    Require(button != null && button.focusable, "Mission selector is a button");
                    Require(button.text.Contains(RouteStage.All[stage].Title) && button.text.StartsWith((stage+1).ToString("00")), "Every choice includes mission number and title");
                    button.Focus();
                    using (var submit = NavigationSubmitEvent.GetPooled()) { submit.target=button; button.SendEvent(submit); }
                    var run = (Journey)Field(game, "journey");
                    Require(root.Q("missionPickerPopup").ClassListContains("hidden"), "Selecting a mission closes picker");
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

        static void VerifyMissionPickerModal(JalanGame game)
        {
            var root = game.GetComponent<UIDocument>().rootVisualElement;
            var pageType = typeof(JalanGame).GetNestedType("Page", BindingFlags.NonPublic);
            foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            {
                string key = JourneySave.Key(mode), saved = PlayerPrefs.GetString(key);
                bool existed = PlayerPrefs.HasKey(key);
                var run = new Journey(mode, new JourneySave { next = 1 }, persistent: false, trafficSeed: 1);
                for (int i = 0; i < 20; i++) run.Step(.02f, true, false, 0);
                Require(run.Speed > 0, "Picker pause fixture begins in motion");
                Set(game, "mode", mode);
                Set(game, "journey", run);
                Set(game, "learner", null);
                Set(game, "pitchDemo", false);
                Call(game, "SetPage", Enum.Parse(pageType, "Drive"));
                Call(game, "RefreshHud");
                Call(game, "OpenMissionPicker");
                Require(Page(game) == "MissionPicker" && !root.Q("missionPickerPopup").ClassListContains("hidden"),
                    "Opening picker enters modal state");
                var selected = root.Q<Button>("mission02");
                Require(root.focusController.focusedElement == selected, "Picker focuses current titled mission");
                using (var move = NavigationMoveEvent.GetPooled(Vector2.down))
                {
                    move.target = selected; selected.SendEvent(move);
                    Require(!move.isImmediatePropagationStopped, "Picker navigation is not consumed by driving guard");
                }
                var position = run.Position;
                float heading = run.Heading, speed = run.Speed, elapsed = run.ElapsedTime;
                for (int i = 0; i < 20; i++) Call(game, "FixedUpdate");
                Require(run.Position == position && run.Heading == heading && run.Speed == speed && run.ElapsedTime == elapsed,
                    "Picker freezes vehicle and simulation clock even after navigation");
                Call(game, "CloseMissionPicker"); // Escape and P use this same close transition.
                Require(Page(game) == "Drive" && root.Q("missionPickerPopup").ClassListContains("hidden")
                    && root.focusController.focusedElement == null, "Closing picker resumes without retained UI focus");
                var progress = root.Q<Button>("progress");
                progress.Focus();
                using (var brake = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Space, character = ' ' }))
                {
                    brake.target = progress; progress.SendEvent(brake);
                    Require(brake.isImmediatePropagationStopped && Page(game) == "Drive", "Space driving guard restored after picker close");
                }
                Call(game, "OpenMissionPicker");
                var choice = root.Q<Button>("mission07");
                choice.Focus();
                using (var submit = NavigationSubmitEvent.GetPooled()) { submit.target = choice; choice.SendEvent(submit); }
                var chosen = (Journey)Field(game, "journey");
                Require(Page(game) == "Drive" && chosen.Stage == 6 && (int)Field(game, "practiceStart") == 6
                    && root.Q("missionPickerPopup").ClassListContains("hidden"), "Enter selects existing isolated checkpoint practice");
                Require(PlayerPrefs.HasKey(key) == existed && PlayerPrefs.GetString(key) == saved, "Picker practice preserves saved progress");
                Set(game, "pitchDemo", true);
                Call(game, "OpenMissionPicker");
                Require(Page(game) == "Drive", "Pitch demo cannot open route picker");
                Set(game, "pitchDemo", false);
            }
            Debug.Log("MISSION_PICKER_MODAL_PASSED both modes: focus/nav, frozen pose/time, close/brake guard, Enter practice and save isolation.");
        }

        static void VerifyDrivingKeyboard(JalanGame game)
        {
            var root = game.GetComponent<UIDocument>().rootVisualElement;
            var progress = root.Q<Button>("progress");
            var pageType = typeof(JalanGame).GetNestedType("Page", BindingFlags.NonPublic);
            Set(game, "learner", null);
            Set(game, "pitchDemo", false);
            Set(game, "journey", new Journey(VehicleMode.Car, new JourneySave { next = 1 }, persistent: false));
            foreach (string active in new[] { "Drive", "Jaksa" })
            {
                progress.Focus();
                Call(game, "SetPage", Enum.Parse(pageType, active));
                Require(root.focusController.focusedElement == null, "Entering driving clears old UI focus");
                progress.Focus(); // Reproduce retained HUD focus even if another UI path reintroduces it.
                using (var move = NavigationMoveEvent.GetPooled(Vector2.up))
                {
                    move.target = progress;
                    progress.SendEvent(move);
                    Require(move.isImmediatePropagationStopped && root.focusController.focusedElement == progress, "Driving W/S/arrows cannot navigate HUD");
                }
                using (var key = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Space, character = ' ' }))
                {
                    key.target = progress;
                    progress.SendEvent(key);
                    Require(key.isImmediatePropagationStopped && Page(game) == active,
                        "Driving Space key cannot activate focused pause button");
                }
            }
            Call(game, "SetPage", Enum.Parse(pageType, "Drive"));
            Call(game, "ShowProgress");
            var resume = root.Q("dialog").Query<Button>().ToList().Find(b => b.text == "Kembali berkendara");
            Require(resume != null, "Paused journey exposes resume button");
            resume.Focus();
            using (var move = NavigationMoveEvent.GetPooled(Vector2.up))
            {
                move.target = resume;
                resume.SendEvent(move);
                Require(!move.isPropagationStopped, "Paused keyboard navigation is available");
            }
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = resume;
                resume.SendEvent(submit);
            }
            Require(Page(game) == "Drive", "Paused Enter submission resumes driving");
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

        static void VerifySignalTransitions(JalanGame game)
        {
            var root = game.GetComponent<UIDocument>().rootVisualElement;
            var district = (District)Field(game, "district");
            var run = new Journey(VehicleMode.Car, new JourneySave { next = 1 }, persistent: false, trafficSeed: 1);
            typeof(Journey).GetProperty("Position").SetValue(run, new Vector2(-2, 72));
            Set(game, "journey", run);
            Call(game, "SetPage", Enum.Parse(typeof(JalanGame).GetNestedType("Page", BindingFlags.NonPublic), "Drive"));
            district.Present(run, false, .02f, true);
            Call(game, "RefreshHud");
            Require(!root.Q("signalPanel").ClassListContains("hidden"), "Training signal appears near intersection");
            Call(game, "ShowMenu");
            Require(root.Q("signalPanel").ClassListContains("hidden"), "Leaving training hides signal panel");
            Call(game, "StartJaksa");
            Call(game, "RefreshJaksaHud");
            Require(root.Q("signalPanel").ClassListContains("hidden"), "Exploration does not show stale training signal");
            Call(game, "ShowMenu");
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
