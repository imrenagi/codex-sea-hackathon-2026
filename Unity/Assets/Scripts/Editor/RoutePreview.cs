using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JalanKita.EditorTools
{
    [InitializeOnLoad]
    public static class RoutePreview
    {
        static Journey previewRun;
        const string StageKey="JalanKita.PreviewStage";
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static RoutePreview() => EditorApplication.update += AwaitGame;
        [MenuItem("RoadSense/Preview/06 - Lurus jalan besar")]
        static void Straight() => Open(5);
        [MenuItem("RoadSense/Preview/07 - Belok kiri dan pejalan kaki")]
        static void Left() => Open(6);
        [MenuItem("RoadSense/Preview/08 - Panah belok kanan")]
        static void Right() => Open(7);
        [MenuItem("RoadSense/Preview/09 - Perlintasan kereta")]
        static void Rail() => Open(8);
        [MenuItem("RoadSense/Preview/10 - Ambulans")]
        static void Emergency() => Open(9);
        [MenuItem("RoadSense/Preview/Maju ke titik tantangan")]
        static void Approach()
        {
            if (!EditorApplication.isPlaying) return;
            var game=UnityEngine.Object.FindFirstObjectByType<JalanGame>();
            if (!game) return;
            var run=(Journey)typeof(JalanGame).GetField("journey",Private).GetValue(game);
            if (run==null || !ReferenceEquals(run,previewRun) || run.Stage<5 || run.Finished) return;
            int stage=run.Stage;
            for(int i=0;i<4000 && run.Stage==stage && !run.Failure.HasValue;i++)
            {
                DrivingCoach.Step(run,.02f);
                if (stage<=7 && run.CrossingClock>5 && run.Speed<.15f) break;
                if (stage==8 && run.RailWarning && run.BarrierDown>.9f && run.Speed<.15f) break;
                if (stage==9 && run.Ambulance.HasValue && run.AmbulanceGap<25) break;
            }
            ((District)typeof(JalanGame).GetField("district",Private).GetValue(game)).Snap();
        }

        static void Open(int stage)
        {
            if(!EditorApplication.isPlaying && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetInt(StageKey,stage);
            if(!EditorApplication.isPlaying)
            {
                EditorSceneManager.OpenScene(ProjectSetup.ScenePath);
                EditorApplication.EnterPlaymode();
            }
        }
        static void AwaitGame()
        {
            int stage=SessionState.GetInt(StageKey,-1);
            if(stage<0 || !EditorApplication.isPlaying) return;
            var game=UnityEngine.Object.FindFirstObjectByType<JalanGame>();
            if(!game || typeof(JalanGame).GetField("journey",Private).GetValue(game)==null) return;
            SessionState.EraseInt(StageKey);
            var type=typeof(JalanGame);
            type.GetField("mode",Private).SetValue(game,VehicleMode.Car);
            type.GetField("learner",Private).SetValue(game,null);
            previewRun=new Journey(VehicleMode.Car,new JourneySave { next=stage },persistent:false);
            type.GetField("journey",Private).SetValue(game,previewRun);
            var page=type.GetNestedType("Page",BindingFlags.NonPublic);
            type.GetMethod("SetPage",Private).Invoke(game,new[] { Enum.Parse(page,"Drive") });
            ((District)type.GetField("district",Private).GetValue(game)).Snap();
        }
    }
}
