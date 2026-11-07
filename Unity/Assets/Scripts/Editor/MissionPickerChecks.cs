using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita.EditorTools
{
    [InitializeOnLoad]
    public static class MissionPickerChecks
    {
        const string Running = "JalanKita.MissionPickerChecks";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static JalanGame game;
        static Journey run;
        static District district;
        static double began;
        static int shot;
        static bool captured;
        static MissionPickerChecks() { if (SessionState.GetBool(Running, false)) EditorApplication.update += Poll; }
        public static void Run()
        {
            EditorSceneManager.OpenScene(ProjectSetup.ScenePath);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            SessionState.SetBool(Running, true);
            EditorApplication.EnterPlaymode();
        }
        static void Poll()
        {
            if (!EditorApplication.isPlaying) return;
            try
            {
                if (!game)
                {
                    game = UnityEngine.Object.FindFirstObjectByType<JalanGame>();
                    if (!game || typeof(JalanGame).GetField("journey", Private).GetValue(game) == null) { game = null; return; }
                    game.enabled = false;
                    run = new Journey(VehicleMode.Car, new JourneySave { next = 6 }, persistent: false, trafficSeed: 1);
                    typeof(JalanGame).GetField("journey", Private).SetValue(game, run);
                    typeof(JalanGame).GetMethod("SetPage", Private).Invoke(game, new[] {Enum.Parse(typeof(JalanGame).GetNestedType("Page", BindingFlags.NonPublic), "Drive")});
                    district = (District)typeof(JalanGame).GetField("district", Private).GetValue(game);
                    district.Snap();
                    Directory.CreateDirectory("Builds/mission-picker-review");
                    began = EditorApplication.timeSinceStartup;
                }
                district.Present(run, false, .02f, true);
                typeof(JalanGame).GetMethod("RefreshHud", Private).Invoke(game, null);
                var root = game.GetComponent<UIDocument>().rootVisualElement;
                if (shot > 0) root.Q("missionPickerPopup").RemoveFromClassList("hidden");
                var scroll = root.Q<ScrollView>("missionPickerScroll");
                if (shot == 2) scroll.scrollOffset = new Vector2(0, 10000);
                if (!captured && EditorApplication.timeSinceStartup - began > 2)
                {
                    if (shot > 0)
                    {
                        var popup = root.Q("missionPickerPopup");
                        if (popup.worldBound.xMin < 0 || popup.worldBound.yMax > root.worldBound.yMax)
                            throw new InvalidOperationException("Mission popup must fit the viewport");
                        if (shot == 2 && !scroll.contentViewport.worldBound.Overlaps(root.Q("mission10").worldBound))
                            throw new InvalidOperationException("Last mission must be reachable by scrolling");
                    }
                    ScreenCapture.CaptureScreenshot("Builds/mission-picker-review/" + new[] {"current-title", "choices", "last-choices"}[shot] + ".png");
                    captured = true;
                }
                if (EditorApplication.timeSinceStartup - began < 3) return;
                shot++; captured = false; began = EditorApplication.timeSinceStartup;
                if (shot < 3) return;
                Debug.Log("Mission picker visual checks passed: current title, scrollable choices, last mission reachable.");
                Finish(0);
            }
            catch (Exception e) { Debug.LogException(e); Finish(1); }
        }
        static void Finish(int code) { SessionState.SetBool(Running, false); EditorApplication.update -= Poll; EditorApplication.Exit(code); }
    }
}
