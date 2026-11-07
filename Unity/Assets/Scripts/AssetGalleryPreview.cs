using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UjianSim.Game;

namespace JalanKita
{
    // An isolated presentation stage. Only actual Resources/Models FBX assets enter it.
    public sealed class AssetGalleryPreview : IDisposable
    {
        readonly int layer;
        readonly GameObject root;
        readonly Camera camera;
        readonly Vector3 origin;
        GameObject model;
        Dictionary<string, Material> ownedMaterials;
        public readonly RenderTexture Texture;
        float yaw, pitch, distance, fitDistance, radius;
        Vector3 center, pan, extents;
        bool autoRotate;
        public AssetGalleryPreview(Transform parent, int width, int height, float depth, int layer = 31)
        {
            this.layer = layer;
            origin = new Vector3(0, depth, 0);
            root = new GameObject("Asset gallery stage");
            root.transform.SetParent(parent, false); root.transform.position = origin;
            Texture = new RenderTexture(width, height, 24) { name = "Asset gallery preview", antiAliasing = 1 };
            Texture.Create();
            camera = new GameObject("Asset gallery camera").AddComponent<Camera>();
            camera.transform.SetParent(root.transform, false);
            camera.targetTexture = Texture; camera.cullingMask = 1 << layer;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.055f, .075f, .085f);
            camera.fieldOfView = 34; camera.nearClipPlane = .01f;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            Light("Gallery key", new Vector3(35, -35, 0), 2.2f, new Color(1, .94f, .84f));
            Light("Gallery fill", new Vector3(20, 145, 0), 1.2f, new Color(.72f, .84f, 1));
        }
        void Light(string name, Vector3 rotation, float intensity, Color color)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(root.transform, false); light.transform.rotation = Quaternion.Euler(rotation);
            light.type = LightType.Directional; light.intensity = intensity; light.color = color;
            light.cullingMask = 1 << layer; light.shadows = LightShadows.None;
        }
        void ClearModel()
        {
            if (model) { model.SetActive(false); Models.Kill(model); }
            if (ownedMaterials != null) foreach (var material in ownedMaterials.Values) Models.Kill(material);
            ownedMaterials = null;
        }
        public void Select(string name)
        {
            ClearModel();
            model = Models.Spawn(name, root.transform, out ownedMaterials, false);
            foreach (var node in model.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = layer;
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = new Bounds(origin, Vector3.one);
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            }
            center = bounds.center;
            radius = Mathf.Max(.1f, bounds.extents.magnitude);
            extents = bounds.extents;
            Reset();
            camera.farClipPlane = Mathf.Max(40, radius * 40);
        }
        // Fit projected bounding-box corners rather than an oversized sphere. This
        // fills the frame for long cars and tall buildings without clipping either.
        float FitDistance()
        {
            var inverse = Quaternion.Inverse(Quaternion.Euler(pitch, yaw, 0));
            float tanV = Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad);
            float tanH = tanV * Texture.width / Texture.height;
            float fit = .1f;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                var corner = inverse * Vector3.Scale(extents, new Vector3(x, y, z));
                fit = Mathf.Max(fit, Mathf.Abs(corner.x) / tanH - corner.z,
                    Mathf.Abs(corner.y) / tanV - corner.z);
            }
            return fit * 1.06f;
        }
        public void Reset() { yaw = 145; pitch = 16; fitDistance = FitDistance(); distance = fitDistance; pan = Vector3.zero; autoRotate = true; Present(0); }
        public void Orbit(float x, float y)
        {
            autoRotate = false;
            float zoom = distance / Mathf.Max(.1f, fitDistance);
            yaw += x; pitch = Mathf.Clamp(pitch + y, -70, 80);
            fitDistance = FitDistance(); distance = fitDistance * zoom; Present(0);
        }
        public void Pan(float x, float y)
        {
            autoRotate = false;
            pan += (camera.transform.right * -x + camera.transform.up * y) * distance * .0015f;
            pan = Vector3.ClampMagnitude(pan, radius * 2); Present(0);
        }
        public void Zoom(float delta) { autoRotate = false; distance = Mathf.Clamp(distance * Mathf.Exp(delta * .035f), fitDistance * .25f, fitDistance * 3); Present(0); }
        public void Present(float dt)
        {
            if (autoRotate)
            {
                yaw += Mathf.Clamp(dt, 0, .1f) * 12;
                fitDistance = FitDistance(); distance = fitDistance;
            }
            var target = center + pan;
            camera.transform.position = target + Quaternion.Euler(pitch, yaw, 0) * Vector3.back * distance;
            camera.transform.LookAt(target);
        }
        public void Dispose()
        {
            ClearModel(); camera.targetTexture = null;
            Texture.Release(); Models.Kill(Texture); root.SetActive(false); Models.Kill(root);
        }
    }
}
