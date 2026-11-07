using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JalanKita.EditorTools
{
    public static class ReverseVisualChecks
    {
        static void Require(bool ok, string message)
        { if (!ok) throw new InvalidOperationException("REVERSE_VISUAL: " + message); }
        static string Name(Transform t) => t.name.Replace("_Export", "");
        static Transform[] Wheels(GameObject root) => root.GetComponentsInChildren<Transform>(true)
            .Where(t => t.gameObject.activeInHierarchy && t.GetComponentsInChildren<Renderer>().Any())
            .Where(t => new[] { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR", "Wheel_F", "Wheel_R", "Wheel_Front", "Wheel_Rear", "Front_Wheel_Pivot", "Rear_Wheel_Pivot" }.Contains(Name(t))).ToArray();
        static Transform[] Steering(GameObject root) => root.GetComponentsInChildren<Transform>(true)
            .Where(t => t.gameObject.activeInHierarchy && t.GetComponentsInChildren<Renderer>().Any())
            .Where(t => new[] { "Steering_FL", "Steering_FR", "Steering_Pivot", "Steer_Pivot", "Wheel_FL steering", "Wheel_FR steering" }.Contains(Name(t))).ToArray();
        static bool Braking(GameObject root)
        {
            var lamp = root.GetComponentsInChildren<Transform>(true)
                .First(t => new[] { "TailLights", "Brake_Light", "Lamp_Brake" }.Contains(Name(t)))
                .GetComponentInChildren<Renderer>();
            var block = new MaterialPropertyBlock(); lamp.GetPropertyBlock(block);
            return block.isEmpty ? lamp.sharedMaterial.name == "BrakeOn" : block.GetColor("_EmissionColor").r > 2;
        }

        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            foreach (var definition in VehicleCatalogue.All)
            {
                var host = new GameObject("Reverse player " + definition.Id);
                var view = new PlayerVehicleView(host.transform, definition);
                void Present(Vector3 p, float yaw, float speed, float dt) => view.Present(definition.Mode, p, yaw, speed, dt, true, 0, true, true);
                Present(Vector3.zero, 0, 0, 0);
                var wheels = Wheels(host); var rest = wheels.Select(t => t.localRotation).ToArray();
                Require(wheels.Length >= 2, definition.Id + " missing wheel pivots");
                Present(Vector3.forward * .1f, 0, 1, .1f);
                Require(wheels.Select((t, i) => Quaternion.Angle(t.localRotation, rest[i])).All(a => a > 2), definition.Id + " forward wheels did not roll");
                Present(Vector3.zero, 0, -1, .1f);
                Require(wheels.Select((t, i) => Quaternion.Angle(t.localRotation, rest[i])).All(a => a < .1f), definition.Id + " reverse did not undo equal forward wheel travel");
                Present(Vector3.back * .2f, 0, -2, .1f);
                Require(!Braking(host), definition.Id + " reverse acceleration falsely lights brakes");
                Present(Vector3.back * .21f, 0, -.1f, .1f);
                Require(Braking(host), definition.Id + " reverse deceleration failed to light brakes");
                UnityEngine.Object.DestroyImmediate(host);

                var forwardHost = new GameObject("Forward turn " + definition.Id);
                var reverseHost = new GameObject("Reverse turn " + definition.Id);
                var f = new PlayerVehicleView(forwardHost.transform, definition);
                var r = new PlayerVehicleView(reverseHost.transform, definition);
                f.Present(definition.Mode, Vector3.zero, 0, 1, 0, true, 0, true, true);
                r.Present(definition.Mode, Vector3.zero, 0, -1, 0, true, 0, true, true);
                var steer = Steering(forwardHost); var steerRest = steer.Select(t => t.localRotation).ToArray();
                Require(steer.Length > 0, definition.Id + " missing steering pivots");
                f.Present(definition.Mode, Vector3.forward * .1f, 1, 1, .1f, true, 0, true, true);
                r.Present(definition.Mode, Vector3.back * .1f, -1, -1, .1f, true, 0, true, true);
                var reverseSteer = Steering(reverseHost);
                Require(steer.Length == reverseSteer.Length, definition.Id + " steering count changed");
                Require(steer.Select((t, i) => Quaternion.Angle(t.localRotation, steerRest[i])).All(a => a > 1), definition.Id + " steering remained straight");
                Require(steer.Select((t, i) => Quaternion.Angle(t.localRotation, reverseSteer[i].localRotation)).All(a => a < .1f), definition.Id + " reverse steering direction differs from equivalent forward turn");
                UnityEngine.Object.DestroyImmediate(forwardHost); UnityEngine.Object.DestroyImmediate(reverseHost);
            }
            foreach (var definition in TrafficCatalogue.All)
            {
                var host = new GameObject("NPC sentinel " + definition.Id);
                var inferred = new TrafficVehicleView(host.transform, "Legacy negative sentinel");
                var explicitSpeed = new TrafficVehicleView(host.transform, "Explicit positive NPC speed");
                inferred.Select(new TrafficChoice(definition, 0)); explicitSpeed.Select(new TrafficChoice(definition, 0));
                inferred.Present(Vector3.zero, 0, -1, 0); explicitSpeed.Present(Vector3.zero, 0, 0, 0);
                for (int i = 1; i <= 3; i++)
                {
                    var p = Vector3.forward * (i * .1f);
                    inferred.Present(p, i, -1, .1f); explicitSpeed.Present(p, i, 1, .1f);
                    var wheels = Wheels(inferred.Root); var other = Wheels(explicitSpeed.Root);
                    Require(wheels.Select((t, j) => Quaternion.Angle(t.localRotation, other[j].localRotation)).All(a => a < .1f), definition.Id + " legacy NPC wheel sentinel changed");
                    var steer = Steering(inferred.Root); var otherSteer = Steering(explicitSpeed.Root);
                    Require(steer.Select((t, j) => Quaternion.Angle(t.localRotation, otherSteer[j].localRotation)).All(a => a < .1f), definition.Id + " legacy NPC steering sentinel changed");
                    Require(Braking(inferred.Root) == Braking(explicitSpeed.Root), definition.Id + " legacy NPC brake sentinel changed");
                }
                UnityEngine.Object.DestroyImmediate(host);
            }
            Debug.Log("REVERSE_VISUAL_PASSED all seven players: both LOD wheel directions, reverse steering, reverse acceleration/deceleration lamps; all six NPC negative-speed sentinel callers preserved.");
        }
    }
}
