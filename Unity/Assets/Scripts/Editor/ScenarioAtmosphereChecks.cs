using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JalanKita.EditorTools
{
    // Render representative gameplay and forecourt views without modifying the saved game scene or player saves.
    public static class ScenarioAtmosphereChecks
    {
        [MenuItem("Jalan Kita/Verify and capture scenario life")]
        public static void Run()
        {
            MissionChecks.Run();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Scenario atmosphere verification");
            var district = new District(root.transform);
            var camera = root.GetComponentInChildren<Camera>();
            string output = Path.GetFullPath("Builds/ScenarioPreview");
            Directory.CreateDirectory(output);
            float[] stops = { 43, 310, 251 };
            Vector3[] eyes = { new(-1, 2.7f, 43), new(-.8f, 2.7f, 310), new(-1.3f, 3.1f, 253) };
            Vector3[] looks = { new(-7, 1.2f, 52), new(-5.7f, 1.1f, 321), new(-8.4f, 1.1f, 266) };
            for (int i = 0; i < Mission.Scenarios.Length; i++)
            {
                var mission = Mission.Scenarios[i];
                var run = new Journey(VehicleMode.Motorcycle, mission.NewSave(), persistent: false, mission: mission);
                run.DriverGear = run.PassengerGear = run.Headlight = true;
                if (run.Stage == 0) run.Depart();
                for (int step = 0; step < 5000 && run.Position.y < stops[i] && !run.Failure.HasValue; step++)
                    DrivingCoach.Step(run, .02f);
                if (run.Failure.HasValue) throw new System.InvalidOperationException("Scenario capture failed: " + mission.Id);
                district.Snap(); district.Present(run, false, .02f);
                Capture(camera, Path.Combine(output, mission.Id + "-drive.png"));
                camera.transform.SetPositionAndRotation(eyes[i], Quaternion.LookRotation(looks[i] - eyes[i]));
                Capture(camera, Path.Combine(output, mission.Id + "-life.png"));
                if (!run.IsMarket) continue;
                for (int step = 0; step < 3000 && !run.PassengerPickedUp && !run.Failure.HasValue; step++)
                {
                    DrivingCoach.Step(run, .02f);
                    district.Present(run, false, .02f);
                }
                for (int step = 0; step < 55; step++)
                {
                    run.Step(.02f, false, true, 0);
                    district.Present(run, false, .02f);
                }
                Capture(camera, Path.Combine(output, "market-boarding.png"));
            }
            Debug.Log("Scenario NPC captures complete: " + output);
        }

        static void Capture(Camera camera, string path)
        {
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var previous = RenderTexture.active;
            bool async = ShaderUtil.allowAsyncCompilation;
            camera.targetTexture = target;
            ShaderUtil.allowAsyncCompilation = false;
            for (int i = 0; i < 4; i++) camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = previous;
            ShaderUtil.allowAsyncCompilation = async;
            Object.DestroyImmediate(texture); Object.DestroyImmediate(target);
        }
    }
}
