using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UjianSim.Game;

namespace JalanKita
{
    // Visual choices share their category's simulation and saves. Root always faces local +Z.
    public sealed class PlayerVehicleView
    {
        public readonly GameObject Root;
        public VehicleDefinition Definition { get; private set; }
        readonly VehicleDefinition fixedDefinition;
        readonly List<Pivot> wheels = new(), steering = new();
        readonly List<Renderer> heads = new(), brakes = new(), left = new(), right = new();
        readonly List<GameObject> occupants = new();
        readonly List<Material> ownedMaterials = new();
        readonly MaterialPropertyBlock properties = new();
        GameObject content;
        TrafficVehicleView modernCar;
        Light headlamp;
        float rolling, clock, previousSpeed, previousHeading, brakeUntil;
        Vector3 previousPosition;
        bool positioned;
        float bodyPitch, bodyRoll;

        sealed class Pivot
        {
            public Transform Part;
            public Quaternion Rest;
            public Vector3 Axis;
            public float Radius;
        }

        public PlayerVehicleView(Transform parent, VehicleDefinition definition = null)
        {
            Root = new GameObject("Selected player vehicle");
            Root.transform.SetParent(parent, false);
            fixedDefinition = definition;
        }

        public void Present(VehicleMode mode, Vector3 position, float headingDegrees, float speedMetresPerSecond,
            float dt, bool headlight, int indicator, bool passenger, bool visible, bool driverGear = true, bool passengerGear = true)
        {
            var definition = fixedDefinition ?? VehicleCatalogue.Selected(mode);
            if (definition.Mode != mode) throw new ArgumentException("Vehicle and lesson category differ");
            if (Definition != definition) Build(definition);
            Root.SetActive(visible);
            Root.transform.SetPositionAndRotation(position, Quaternion.Euler(0, headingDegrees, 0));
            dt = Mathf.Clamp(dt, 0, .1f);
            clock += dt;
            float distance = positioned ? Vector3.Distance(position, previousPosition) : 0;
            // Resets and checkpoint teleports must not spin wheels or trigger braking.
            bool teleport = !positioned || distance > Mathf.Max(2, Mathf.Abs(speedMetresPerSecond) * dt * 4);
            if (!teleport) rolling += speedMetresPerSecond < 0 ? -distance : distance;
            if (!teleport && dt > 0 && Mathf.Abs(previousSpeed) - Mathf.Abs(speedMetresPerSecond) > dt * 2) brakeUntil = clock + .35f;
            if (teleport) { rolling = 0; brakeUntil = 0; }
            float wheelbase = mode == VehicleMode.Car ? 2.5f : 1.255f;
            float turn = !teleport && dt > 0 && Mathf.Abs(speedMetresPerSecond) > .2f
                ? Mathf.Clamp(Mathf.Atan(wheelbase * Mathf.DeltaAngle(previousHeading, headingDegrees) * Mathf.Deg2Rad / dt / speedMetresPerSecond) * Mathf.Rad2Deg, -30, 30) : 0;
            float acceleration = !teleport && dt > 0 ? (speedMetresPerSecond - previousSpeed) / dt : 0;
            float lateral = !teleport && dt > 0 ? speedMetresPerSecond * Mathf.DeltaAngle(previousHeading, headingDegrees) * Mathf.Deg2Rad / dt : 0;
            float settle = teleport ? 1 : 1 - Mathf.Exp(-8 * dt);
            bodyPitch = Mathf.Lerp(bodyPitch, Mathf.Clamp(-acceleration * .2f, -1.1f, 1.7f), settle);
            bodyRoll = Mathf.Lerp(bodyRoll, mode == VehicleMode.Motorcycle
                ? Mathf.Clamp(-Mathf.Atan(lateral / 9.81f) * Mathf.Rad2Deg, -16, 16)
                : Mathf.Clamp(lateral * .45f, -2.4f, 2.4f), settle);
            previousPosition = position; previousSpeed = speedMetresPerSecond; previousHeading = headingDegrees; positioned = true;
            if (modernCar != null)
                modernCar.Present(position, headingDegrees, speedMetresPerSecond, dt, headlight, indicator, clock < brakeUntil, signedSpeed: true);
            Root.transform.rotation = Quaternion.Euler(0, headingDegrees, 0) * Quaternion.Euler(bodyPitch, 0, bodyRoll);
            foreach (var p in steering) p.Part.localRotation = Quaternion.AngleAxis(turn, p.Axis) * p.Rest;
            foreach (var p in wheels) p.Part.localRotation = Quaternion.AngleAxis(rolling / p.Radius * Mathf.Rad2Deg, p.Axis) * p.Rest;
            foreach (var p in occupants)
            {
                bool pillion = p.name.StartsWith("Pillion_");
                bool gear = pillion ? passengerGear : driverGear;
                bool safety = p.name.Contains("Helmet_SNI") || p.name.Contains("Strap_Buckled");
                bool hair = p.name.Contains("_Hair");
                p.SetActive((!pillion || passenger) && (safety ? gear : hair ? !gear : true));
            }
            bool blink = clock % .7f < .42f;
            Glow(heads, headlight ? new Color(1, .92f, .72f) * 1.8f : Color.black);
            Glow(brakes, new Color(1, .035f, .015f) * (clock < brakeUntil ? 2.8f : headlight ? .22f : 0));
            Glow(left, indicator < 0 && blink ? new Color(1, .28f, .01f) * 2.2f : Color.black);
            Glow(right, indicator > 0 && blink ? new Color(1, .28f, .01f) * 2.2f : Color.black);
            if (headlamp) headlamp.enabled = visible && headlight;
        }

        void Build(VehicleDefinition definition)
        {
            if (content) { content.SetActive(false); Models.Kill(content); }
            foreach (var m in ownedMaterials) Models.Kill(m);
            ownedMaterials.Clear(); wheels.Clear(); steering.Clear(); heads.Clear(); brakes.Clear(); left.Clear(); right.Clear(); occupants.Clear();
            Definition = definition; positioned = false;
            modernCar = null; bodyPitch = bodyRoll = 0;
            content = new GameObject(definition.Label); content.transform.SetParent(Root.transform, false);
            if (definition.Model.StartsWith("Traffic/"))
            {
                modernCar = new TrafficVehicleView(content.transform, "Detailed player car");
                string id = definition.Model.Substring("Traffic/".Length);
                modernCar.Select(new TrafficChoice(TrafficCatalogue.All.First(v => v.Id == id), id == "family_mpv" ? 0 : 1));
            }
            else if (definition.Beat) BuildBeat(); else BuildOriginal();
            var indicators = left.Concat(right).Distinct().ToArray(); left.Clear(); right.Clear();
            foreach (var renderer in indicators)
            {
                var mesh = renderer.GetComponent<MeshFilter>();
                var center = mesh ? renderer.transform.TransformPoint(mesh.sharedMesh.bounds.center) : renderer.bounds.center;
                (Root.transform.InverseTransformPoint(center).x < 0 ? left : right).Add(renderer);
            }
            // URP removes _EMISSION from the black body atlas. Lamp-only copies retain the
            // shader variant; property blocks can then switch their physical lenses on/off.
            var lampMaterials = new Dictionary<Material, Material>();
            foreach (var lamp in heads.Concat(brakes).Concat(left).Concat(right).Distinct())
            {
                var slots = lamp.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    var source = slots[i];
                    if (!lampMaterials.TryGetValue(source, out var lit))
                    {
                        lit = new Material(source) { name = source.name + " lamp", globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive };
                        lit.EnableKeyword("_EMISSION"); lit.SetColor("_EmissionColor", Color.white);
                        lampMaterials[source] = lit; ownedMaterials.Add(lit);
                    }
                    slots[i] = lit;
                }
                lamp.sharedMaterials = slots;
            }
            if (definition.Mode == VehicleMode.Motorcycle)
            {
                headlamp = new GameObject("Working scooter headlight").AddComponent<Light>();
                headlamp.transform.SetParent(content.transform, false);
                headlamp.transform.localPosition = new Vector3(0, definition.Beat ? .74f : 1.04f, .72f);
                headlamp.type = LightType.Spot; headlamp.spotAngle = 56; headlamp.range = 12;
                headlamp.intensity = 3; headlamp.color = new Color(1, .94f, .8f); headlamp.shadows = LightShadows.None;
            }
        }

        GameObject Imported(string path, Transform parent)
        {
            var prefab = Resources.Load<GameObject>(path);
            if (!prefab) throw new InvalidOperationException("Missing player vehicle " + path);
            var basis = new GameObject("Forward-axis adapter"); basis.transform.SetParent(parent, false);
            // Both exports use Blender +Y forward; preserve their imported axis transform beneath this adapter.
            basis.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var model = UnityEngine.Object.Instantiate(prefab, basis.transform);
            model.name = prefab.name;
            return model;
        }

        void BuildBeat()
        {
            var levels = new List<Renderer[]>();
            for (int level = 0; level < 2; level++)
            {
                var model = Imported("Vehicles/" + Definition.Model + "/" + (level == 0 ? "Body" : "Body_LOD1"), content.transform);
                var atlas = Resources.Load<Material>("Vehicles/" + Definition.Model + "/Atlas");
                var lens = Resources.Load<Material>("Vehicles/" + Definition.Model + "/Lens");
                if (!atlas || !lens) throw new InvalidOperationException("Run VehicleChecks.Prepare to create vehicle materials");
                var renderers = model.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                    r.sharedMaterials = r.sharedMaterials.Select(m => m && m.name.StartsWith("Optical_Lens") ? lens : atlas).ToArray();
                AddPivot(model.transform, "Steering_Pivot", steering, Vector3.up, 1);
                AddPivot(model.transform, "Front_Wheel_Pivot", wheels, Vector3.right, .2498f);
                AddPivot(model.transform, "Rear_Wheel_Pivot", wheels, Vector3.right, .2588f);
                AddLamps(model.transform, "Headlight", heads); AddLamps(model.transform, "Brake_Light", brakes);
                AddLamps(model.transform, "Indicator_Front_L", left); AddLamps(model.transform, "Indicator_Rear_L", left);
                AddLamps(model.transform, "Indicator_Front_R", right); AddLamps(model.transform, "Indicator_Rear_R", right);
                levels.Add(renderers);
            }
            var riders = Imported("Models/Motorcycle", content.transform);
            ownedMaterials.AddRange(MaterialLibrary.Apply(riders).Values);
            riders.transform.parent.localPosition = new Vector3(0, -.11f, .01f);
            foreach (var r in riders.GetComponentsInChildren<Renderer>(true))
            {
                bool occupant = r.name.StartsWith("Rider_") || r.name.StartsWith("Pillion_");
                r.gameObject.SetActive(occupant && HelmetPart(r.name));
                if (occupant && !r.name.Contains("Helmet_Catok") && !r.name.Contains("Strap_Open")) occupants.Add(r.gameObject);
            }
            var riderRenderers = riders.GetComponentsInChildren<Renderer>().Where(r => r.gameObject.activeSelf).ToArray();
            var group = content.AddComponent<LODGroup>();
            group.SetLODs(new[] { new LOD(.16f, levels[0].Concat(riderRenderers).ToArray()), new LOD(.015f, levels[1].Concat(riderRenderers).ToArray()) });
            group.RecalculateBounds();
        }

        void BuildOriginal()
        {
            var model = Imported("Models/" + Definition.Model, content.transform);
            var materialMap = MaterialLibrary.Apply(model);
            foreach (var material in materialMap.Values) ownedMaterials.Add(material);
            foreach (var part in model.GetComponentsInChildren<Transform>(true))
            {
                if (!HelmetPart(part.name)) part.gameObject.SetActive(false);
                if ((part.name.StartsWith("Rider_") || part.name.StartsWith("Pillion_"))
                    && !part.name.Contains("Helmet_Catok") && !part.name.Contains("Strap_Open")) occupants.Add(part.gameObject);
            }
            AddLamps(model.transform, "Lamp_Head", heads); AddLamps(model.transform, "Lamp_Brake", brakes);
            AddLamps(model.transform, "Lamp_Ind_L", left); AddLamps(model.transform, "Lamp_Ind_R", right);
            if (Definition.Mode == VehicleMode.Car)
            {
                // Keep cosmetic car choices within the existing car lesson/collision footprint.
                var renderers = model.GetComponentsInChildren<Renderer>();
                var envelope = renderers[0].bounds;
                foreach (var renderer in renderers) envelope.Encapsulate(renderer.bounds);
                float fit = Mathf.Min(1, 3.9f / envelope.size.z, 1.95f / envelope.size.x);
                model.transform.parent.localScale = Vector3.one * fit;
                foreach (string name in new[] { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" })
                {
                    var wheel = Find(model.transform, name);
                    if (name.StartsWith("Wheel_F") && wheel)
                    {
                        var yaw = new GameObject(name + " steering").transform;
                        yaw.SetParent(wheel.parent, false); yaw.position = wheel.position; yaw.rotation = Root.transform.rotation;
                        wheel.SetParent(yaw, true);
                        steering.Add(new Pivot { Part = yaw, Rest = yaw.localRotation,
                            Axis = yaw.parent.InverseTransformDirection(Root.transform.up), Radius = 1 });
                    }
                    AddPivot(model.transform, name, wheels, Vector3.right, .31f);
                }
            }
            else
            {
                // The original asset exports these parts flat; restore its documented steering hierarchy.
                var pivot = Find(model.transform, "Steer_Pivot");
                foreach (string name in new[] { "Steer_Handlebars", "Lamp_Head", "Wheel_F" })
                {
                    var part = Find(model.transform, name);
                    if (pivot && part) part.SetParent(pivot, true);
                }
                AddPivot(model.transform, "Wheel_F", wheels, Vector3.right, .28f);
                AddPivot(model.transform, "Wheel_R", wheels, Vector3.right, .28f);
                AddPivot(model.transform, "Steer_Pivot", steering, Vector3.up, 1);
            }
        }

        static bool HelmetPart(string name) => !name.Contains("Helmet_Catok") && !name.Contains("Strap_Open") && !name.Contains("_Hair");
        static Transform Find(Transform parent, string name) => parent.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == name || t.name == name + "_Export" || t.name.StartsWith(name + "."));

        void AddPivot(Transform root, string name, List<Pivot> list, Vector3 rootAxis, float radius)
        {
            var part = Find(root, name);
            if (!part) throw new InvalidOperationException("Vehicle pivot missing: " + name);
            list.Add(new Pivot { Part = part, Rest = part.localRotation,
                Axis = part.parent.InverseTransformDirection(Root.transform.TransformDirection(rootAxis)), Radius = radius });
        }

        static void AddLamps(Transform root, string name, List<Renderer> list)
        {
            var part = Find(root, name);
            if (part) list.AddRange(part.GetComponentsInChildren<Renderer>(true));
        }

        void Glow(List<Renderer> lamps, Color color)
        {
            foreach (var lamp in lamps)
            {
                properties.Clear(); properties.SetColor("_EmissionColor", color * (Definition.Beat ? 6 : 1)); lamp.SetPropertyBlock(properties);
            }
        }
    }
}
