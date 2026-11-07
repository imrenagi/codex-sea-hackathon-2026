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
        Bounds modelBounds;
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
            // Only an actual model change resumes rotation after manual interaction.
            // Reopening the chooser or clicking the same model preserves the stopped angle.
            if (framed != VehicleCatalogue.Selected(mode)) AutoRotate = true;
            if (AutoRotate) Angle = Mathf.Repeat(Angle + Mathf.Clamp(dt, 0, .1f) * 18, 360);
            vehicle.Present(mode, origin, Angle, 0, 0, false, 0, false, true);
            if (framed != Definition)
            {
                framed = Definition;
                foreach (var node in vehicle.Root.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = Layer;
                foreach (var light in vehicle.Root.GetComponentsInChildren<Light>(true)) light.cullingMask = 1 << Layer;
                // Normalize presentation size from actual imported geometry, never
                // change model scale (the two detailed cars differ from legacy cars).
                bool first = true;
                var vehicleTransform = vehicle.Root.transform;
                foreach (var mesh in vehicle.Root.GetComponentsInChildren<MeshFilter>())
                {
                    if (!mesh.sharedMesh || !mesh.gameObject.activeInHierarchy) continue;
                    var bounds = mesh.sharedMesh.bounds;
                    for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                    {
                        var point = vehicleTransform.InverseTransformPoint(mesh.transform.TransformPoint(
                            bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z))));
                        if (first) { modelBounds = new Bounds(point, Vector3.zero); first = false; }
                        else modelBounds.Encapsulate(point);
                    }
                }
                if (first) modelBounds = new Bounds(Vector3.up, Vector3.one * 2);

            }
            FrameModel();
        }
        void FrameModel()
        {
            var target = vehicle.Root.transform.TransformPoint(modelBounds.center);
            var direction = new Vector3(0, .25f, -1).normalized;
            var rotation = Quaternion.LookRotation(-direction);
            var inverse = Quaternion.Inverse(rotation);
            float tanV = Mathf.Tan(Camera.fieldOfView * .5f * Mathf.Deg2Rad);
            float tanH = tanV * Texture.width / Texture.height;
            float distance = .2f;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                var corner = inverse * vehicle.Root.transform.TransformVector(
                    Vector3.Scale(modelBounds.extents, new Vector3(x, y, z)));
                distance = Mathf.Max(distance, Mathf.Abs(corner.x) / tanH - corner.z,
                    Mathf.Abs(corner.y) / tanV - corner.z);
            }
            distance *= 1.06f;
            Camera.farClipPlane = Mathf.Max(30, distance * 4);
            Camera.transform.SetPositionAndRotation(target + direction * distance, rotation);
        }
        public void Dispose()
        {
            Camera.targetTexture = null;
            Texture.Release(); Models.Kill(Texture);
            vehicle.Dispose(); Models.Kill(root);
        }
    }
}
