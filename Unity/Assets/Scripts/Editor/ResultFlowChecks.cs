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
        const string ContinuationOnly = Running + ".ContinuationOnly";
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

        public static void RunContinuation()
        {
            SessionState.SetBool(ContinuationOnly, true);
            EditorSceneManager.OpenScene(ProjectSetup.ScenePath);
            SessionState.SetBool(Running, true);
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("RoadSense/Verify Result Screens (Play mode)")]
        public static void RunInOpenEditor()
        {
            var game = UnityEngine.Object.FindFirstObjectByType<JalanGame>();
            if (!EditorApplication.isPlaying || !game) throw new InvalidOperationException("Enter Play mode first.");
            game.enabled = false;
            try
            {
                foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode))) { Verify(game, mode); VerifyMissionSelection(game, mode); }
                VerifyArrowDirections();
                IntegrationChecks.VerifyUI(game);
                VerifyMirrorsAndLook(game);
                SignalChecks.Verify(game);
                VerifySignalTransitions(game);
                VerifyDrivingKeyboard(game);
                VerifyControls(game);
                VerifyMissionPickerModal(game);
                Debug.Log("Result flow checks passed: both vehicles, checkpoints, paused mission picker, saves, pitch, maps, keyboard, controls and mirrors.");
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
                if (SessionState.GetBool(ContinuationOnly, false))
                {
                    SessionState.SetBool(ContinuationOnly, false);
                    using var preferences = new IntegrationChecks.SavedPreferences();
                    game.enabled = false;
                    foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode))) Verify(game, mode);
                    Debug.Log("JOURNEY_CONTINUATION_PASSED: all ten mission buttons, both vehicles, final quiz, pause and retry.");
                }
                else RunInOpenEditor();
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
                Require(root.Q<Label>(className: "stamp")?.text == GameText.Get("text.mission_complete_405e76"), "Success banner");
                Call(game, "FixedUpdate");
                Require(run.Position == position && run.Speed == speed, "Pause preserves driving state");
                var next = root.Q<Button>("continue-mission");
                Require(next != null && next.ClassListContains("primary"), "Completion exposes primary continuation button");
                Require(next.text == (run.Finished ? GameText.Get("text.final_quiz_5f0543")
                    : GameText.Get("mission.continue_next", completed + 1)), "Continuation names the next mission or final quiz");
                if (!run.Finished)
                    Require(root.Query<Label>().ToList().Exists(label => label.text ==
                        GameText.Get("mission.up_next", completed + 1, RouteStage.All[completed].Title)), "Completion previews the next task");
                Submit(next);
                AcknowledgeControls(game);
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
            Require(root.Q<Label>(className: "stamp")?.text == GameText.Get("text.violation"), "Violation banner");
            Require(root.Query<VisualElement>(className: "fold").ToList().Count == 2, "Explanation and legal folds");
            Call(game, "Retry");
            AcknowledgeControls(game);
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
                    AcknowledgeControls(game);
                    Call(game, "ShowProgress");
                    var fold = root.Query<VisualElement>(className: "fold").ToList().Find(f =>
                        f.Q<Label>(className: "fold-title")?.text == GameText.Get("text.practice_from_a_checkpoint"));
                    Require(fold != null, "Pause contains checkpoint picker");
                    Submit(fold.Q<Button>(className: "fold-head"));
                    Require(fold.ClassListContains("open"), "Checkpoint picker opens from pause");
                    string prefix = (stage + 1).ToString("00") + " · ";
                    var button = fold.Query<Button>().ToList().Find(b => b.text.StartsWith(prefix));
                    Require(button != null && button.focusable && button.text.Contains(RouteStage.All[stage].Title), "Practice choice has mission number and title");
                    Submit(button);
                    AcknowledgeControls(game);
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
                    Require(root.Q<Label>(className:"checkpoint-saved").text == GameText.Get("text.practice_complete_your_journey_progress_stays_saved"), "Practice never claims skipped missions saved");
                    Call(game, "ContinueCheckpoint");
                    AcknowledgeControls(game);
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
                Require(Page(game) == "Progress", "Opening picker pauses the game");
                var selected = root.Q("dialog").Query<Button>().ToList().Find(b => b.text.StartsWith("02 · "));
                Require(root.focusController.focusedElement == selected, "Picker focuses current titled mission");
                Call(game, "FocusPage", Enum.Parse(pageType, "Progress"));
                Require(root.focusController.focusedElement == selected, "Delayed generic page focus preserves explicit mission choice");
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
                Require(Page(game) == "Drive" && (bool)Field(game, "inputReleaseRequired")
                    && root.focusController.focusedElement == null, "Closing picker resumes with input release and no retained UI focus");
                var progress = root.Q<Button>("progress");
                progress.Focus();
                using (var brake = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Space, character = ' ' }))
                {
                    brake.target = progress; progress.SendEvent(brake);
                    Require(brake.isImmediatePropagationStopped && Page(game) == "Drive", "Space driving guard restored after picker close");
                }
                Call(game, "OpenMissionPicker");
                var choice = root.Q("dialog").Query<Button>().ToList().Find(b => b.text.StartsWith("07 · "));
                choice.Focus();
                using (var submit = NavigationSubmitEvent.GetPooled()) { submit.target = choice; choice.SendEvent(submit); }
                AcknowledgeControls(game);
                var chosen = (Journey)Field(game, "journey");
                Require(Page(game) == "Drive" && chosen.Stage == 6 && (int)Field(game, "practiceStart") == 6,
                    "Enter selects existing isolated checkpoint practice");
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
            var resume = root.Q("dialog").Query<Button>().ToList().Find(b => b.text == GameText.Get("text.resume_driving"));
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

        static void VerifyControls(JalanGame game)
        {
            using var preferences = new IntegrationChecks.SavedPreferences();
            var root = game.GetComponent<UIDocument>().rootVisualElement;
            var pageType = typeof(JalanGame).GetNestedType("Page", BindingFlags.NonPublic);
            Call(game, "ShowMenu");
            var home = root.Q("dialog").Query<Button>().ToList();
            Require(home.Count == 3 && home[0].text == GameText.Get("home.play")
                && home[1].text == GameText.Get("home.options") && home[2].text == GameText.Get("home.exit"),
                "Native home has exactly Play / Options / Exit");
            Submit(home[1]);
            Require(Page(game) == "Options", "Options route");
            var guide = root.Q("dialog").Query<Button>().ToList().Find(b => b.text == GameText.Get("controls.open"));
            Require(guide != null, "Options exposes manual controls");
            Submit(guide);
            Require(Page(game) == "Controls", "Options opens manual controls");
            Submit(root.Q<Button>(className: "tutorial-start"));
            Require(Page(game) == "Options", "Manual controls returns to Options");
            const string onboardingKey = "RoadSense.Onboarding.MovementBrake.v1";
            const string completedKey = "RoadSense.Tutorial.pasar-bunga-right-of-way.Completed.v1";
            PlayerPrefs.DeleteKey(onboardingKey); PlayerPrefs.DeleteKey(completedKey);
            Set(game, "demoFinishedAt", -1d); Set(game, "completedTutorialScenarioId", null);
            var saved = new JourneySave { next = 4, trafficSeed = 173, before = 2 };
            saved.Store(VehicleMode.Car);
            string saveKey = JourneySave.Key(VehicleMode.Car), original = PlayerPrefs.GetString(saveKey);
            Call(game, "ShowMenu");
            Submit(root.Q("dialog").Query<Button>().ToList().Find(b => b.text == GameText.Get("home.play")));
            Require(Page(game) == "Drive", "First real Play starts contextual driving tutorial");
            var run = (Journey)Field(game, "journey");
            var tutorial = (TutorialRunner)Field(game, "demoTutorial");
            Require(run.Mode == VehicleMode.Car && run.LearningControls && (bool)Field(game, "inputReleaseRequired"),
                "Safe car tutorial owns input and scoring");
            var position = run.Position; float elapsed = run.ElapsedTime;
            Call(game, "FixedUpdate");
            Require(run.Position == position && run.ElapsedTime == elapsed, "Held entry input cannot move tutorial");
            Set(game, "inputReleaseRequired", false);
            int pedestrians = 0, cars = 0;
            void ObserveActors()
            {
                for (int i = 0; i < Journey.CrossingPedestrianCount; i++)
                    if (run.CrossingPedestrian(i).HasValue) pedestrians |= 1 << i;
                for (int i = 0; i < Journey.MainRoadTrafficCount; i++)
                    if (run.MainRoadTraffic(i).HasValue) cars |= 1 << i;
            }
            foreach (string expected in new[] { "pedestrian-crossing", "main-road-junction" })
            {
                for (int i = 0; i < 2500 && !tutorial.IsActive && !run.Failure.HasValue; i++)
                {
                    run.Step(.02f, true, false, 0); ObserveActors();
                    Call(game, "BeginDemoTutorialStep");
                }
                Require(tutorial.IsActive && tutorial.Active.Id == expected && Page(game) == "TutorialBrake"
                    && !run.Failure.HasValue, "Ordered tutorial prompt: " + expected);
                position = run.Position; elapsed = run.ElapsedTime;
                Call(game, "FixedUpdate");
                Require(run.Position == position && run.ElapsedTime == elapsed, "Prompt freezes simulation: " + expected);
                Require(!tutorial.TryAction(run, true, true), "Held S cannot dismiss new prompt");
                tutorial.TryAction(run, false, false);
                Require(tutorial.TryAction(run, true, true), "Fresh S accepts guided brake");
                Call(game, "EnterDemoTutorialWaiting");
                Require(tutorial.TryContinue(run, true, true) == null, "W cannot bypass required wait");
                for (int i = 0; i < 2500 && tutorial.Phase != TutorialPhase.Ready; i++)
                {
                    Call(game, "FixedUpdate"); ObserveActors();
                    Require(run.Speed >= 0 && !run.Failure.HasValue, "Guided brake never reverses or penalizes learner");
                }
                Require(tutorial.Phase == TutorialPhase.Ready && run.Speed == 0
                    && tutorial.Active.CompleteWhen(run), "Actual hazard clears before continuation: " + expected);
                Require(tutorial.TryContinue(run, true, true) == null, "Held W cannot skip ready transition");
                tutorial.TryContinue(run, false, false);
                var completed = tutorial.TryContinue(run, true, true);
                Require(completed != null && completed.Id == expected && !run.LearningControls,
                    "Fresh W restores normal driving rules: " + expected);
            }
            Require(tutorial.CompletedCount == 2 && pedestrians == 7 && cars == 7,
                "Tutorial encounters all three pedestrians and all three main-road cars");
            for (int i = 0; i < 10000 && run.Stage == 2 && !run.Failure.HasValue; i++) DrivingCoach.Step(run, .02f);
            Require(run.Stage == 3 && !run.Failure.HasValue, "Guided tutorial completes its bounded route");
            Call(game, "AnnounceCheckpoint");
            var actions = root.Q("dialog").Query<Button>().ToList();
            var choose = actions.Find(b => b.text == GameText.Get("navigation.explore_modes"));
            Require(choose != null && actions.Exists(b => b.text == GameText.Get("demo.replay")),
                "Completion exposes journey chooser and fresh replay");
            Submit(choose);
            Require(Page(game) == "PlayChooser" && PlayerPrefs.GetInt(completedKey) == 1
                && PlayerPrefs.GetInt(onboardingKey) == 1, "Completed tutorial persists completion and reaches chooser");
            Require(PlayerPrefs.GetString(saveKey) == original, "Tutorial preserves existing route save byte-for-byte");
            Call(game, "ShowMenu");
            Submit(root.Q("dialog").Query<Button>().ToList().Find(b => b.text == GameText.Get("home.play")));
            Require(Page(game) == "PlayChooser", "Returning Play reaches saved journeys and modes");
            foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            {
                Set(game, "mode", mode); Set(game, "pitchDemo", false); Set(game, "demoTutorial", null);
                Set(game, "journey", new Journey(mode, new JourneySave { next = 1 }, false, trafficSeed: 173));
                Call(game, "Retry");
                Require(Page(game) == "Drive" && (bool)Field(game, "inputReleaseRequired"), "Retry retains release latch");
                Call(game, "ShowProgress");
                Call(game, "ShowControls", Enum.Parse(pageType, "Progress"), Enum.Parse(pageType, "Drive"), false);
                Submit(root.Q<Button>(className: "tutorial-start"));
                Require(Page(game) == "Progress", "Manual guide returns to pause");
                Call(game, "StartJaksa");
                Require(Page(game) == "Jaksa" && (bool)Field(game, "inputReleaseRequired"), "Exploration retains release latch");
                Call(game, "ShowJaksaPause");
                Call(game, "ShowControls", Enum.Parse(pageType, "JaksaPause"), Enum.Parse(pageType, "Jaksa"), false);
                Submit(root.Q<Button>(className: "tutorial-start"));
                Require(Page(game) == "JaksaPause", "Exploration guide returns to pause");
            }
            Debug.Log("ROADSENSE_ONBOARDING_PASSED actual Play, ordered crossing/junction with three actors each, fresh-key brake/wait/continue, returning chooser, immutable saves, help/pause/input gates.");
        }

        internal static void AcknowledgeControls(JalanGame game)
        {
            if (Page(game) != "Controls") return;
            var root = game.GetComponent<UIDocument>().rootVisualElement;
            var start = root.Q<Button>(className: "tutorial-start");
            Require(start != null, "Controls exposes a focused continue action");
            var run = (Journey)Field(game, "journey");
            var position = run.Position;
            Call(game, "FixedUpdate");
            Require(run.Position == position, "Tutorial freezes simulation");
            Submit(start);
            Require(Page(game) != "Controls" && (bool)Field(game, "inputReleaseRequired"), "Accepting guide requires release before driving");
        }

        static void Submit(Button button)
        {
            button.Focus();
            using var submit = NavigationSubmitEvent.GetPooled();
            submit.target = button;
            button.SendEvent(submit);
        }

        static object Field(JalanGame game, string name) => typeof(JalanGame).GetField(name, Private).GetValue(game);
        static void Set(JalanGame game, string name, object value) => typeof(JalanGame).GetField(name, Private).SetValue(game, value);
        static string Page(JalanGame game) => Field(game, "page").ToString();
        static void Call(JalanGame game, string name, params object[] args)
        {
            var method = Array.Find(typeof(JalanGame).GetMethods(Private),
                m => m.Name == name && m.GetParameters().Length == args.Length);
            if (method == null) throw new MissingMethodException(typeof(JalanGame).FullName, name);
            method.Invoke(game, args);
        }
        static void Require(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException(message);
        }
    }
}
