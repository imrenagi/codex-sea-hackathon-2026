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
                foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode))) Verify(game, mode);
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
                foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode))) Verify(game, mode);
                Debug.Log("Result flow checks passed: both vehicles, all ten completion cards, frozen position and speed, continuous resume, final quiz, violation and checkpoint retry.");
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
            var run = new Journey(mode, new JourneySave(), persistent: false);
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

            run = new Journey(mode, new JourneySave { next = 1 }, persistent: false);
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
