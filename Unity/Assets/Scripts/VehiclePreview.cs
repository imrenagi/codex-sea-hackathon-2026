using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UjianSim.Game;

namespace JalanKita
{
    public sealed class VehiclePreview : IDisposable
    {
        const int Layer = 31;
        readonly GameObject root;
        readonly PlayerVehicleView vehicle;
        public readonly Camera Camera;
        public readonly RenderTexture Texture;
        public float Angle { get; private set; } = 145;
        public bool AutoRotate { get; set; } = true;
        public VehicleDefinition Definition => vehicle.Definition;
        public bool Visible => root.activeSelf;
        VehicleDefinition framed;
        readonly Vector3 origin = new(0, -1000, 0);

        public VehiclePreview(Transform parent)
        {
            root = new GameObject("Vehicle showroom");
            root.transform.SetParent(parent, false);
            root.transform.position = origin;
            vehicle = new PlayerVehicleView(root.transform);
            Texture = new RenderTexture(768, 768, 24) { name = "Vehicle 360 preview", antiAliasing = 2 };
            Texture.Create();
            Camera = new GameObject("Showroom camera").AddComponent<Camera>();
            Camera.transform.SetParent(root.transform, false);
            Camera.targetTexture = Texture; Camera.cullingMask = 1 << Layer;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = new Color(.075f, .09f, .11f);
            Camera.fieldOfView = 34; Camera.nearClipPlane = .1f; Camera.farClipPlane = 30;
            Camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            AddLight("Key", new Vector3(35, -35, 0), 2.4f, new Color(1, .93f, .84f));
            AddLight("Fill", new Vector3(25, 140, 0), 1.4f, new Color(.72f, .84f, 1));
            SetVisible(false);
        }
        void AddLight(string name, Vector3 rotation, float intensity, Color color)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(root.transform, false);
            light.transform.rotation = Quaternion.Euler(rotation);
            light.type = LightType.Directional; light.intensity = intensity; light.color = color;
            light.cullingMask = 1 << Layer; light.shadows = LightShadows.None;
        }
        public void SetVisible(bool visible) { root.SetActive(visible); Camera.enabled = visible; }
        public void Rotate(float degrees) { Angle = Mathf.Repeat(Angle + degrees, 360); AutoRotate = false; }
        public void SetAngle(float degrees) { Angle = Mathf.Repeat(degrees, 360); AutoRotate = false; }
        public void Present(VehicleMode mode, float dt)
        {
            if (!Visible) return;
            if (AutoRotate) Angle = Mathf.Repeat(Angle + Mathf.Clamp(dt, 0, .1f) * 18, 360);
            vehicle.Present(mode, origin, Angle, 0, 0, false, 0, false, true);
            if (framed != Definition)
            {
                framed = Definition;
                foreach (var node in vehicle.Root.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = Layer;
                foreach (var light in vehicle.Root.GetComponentsInChildren<Light>(true)) light.cullingMask = 1 << Layer;
                // Fixed sphere framing keeps every side in view throughout a full turn.
                float radius = mode == VehicleMode.Car ? 2.6f : 1.6f;
                var target = origin + Vector3.up * (mode == VehicleMode.Car ? .8f : .95f);
                float distance = radius / Mathf.Sin(Camera.fieldOfView * .5f * Mathf.Deg2Rad);
                Camera.transform.position = target + new Vector3(0, .25f, -1).normalized * distance;
                Camera.transform.LookAt(target);
            }
        }
        public void Dispose()
        {
            Camera.targetTexture = null;
            Texture.Release(); Models.Kill(Texture);
            vehicle.Dispose(); Models.Kill(root);
        }
    }
}
