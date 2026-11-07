using UnityEngine;
using UnityEngine.Rendering;

namespace JalanKita
{
    public static class FidelityLighting
    {
        // One directional source represents the sun. The ambient probe represents
        // diffuse sky/bounce light only; it cannot cast a second set of shadows.
        public static void ConfigureSun(Light sun)
        {
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(52, -36, 0);
            sun.color = new Color(1, .975f, .94f);
            sun.intensity = 2.2f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .95f;
            sun.shadowBias = .035f;
            sun.shadowNormalBias = .25f;
        }

        public static void Activate(Light sun)
        {
            ConfigureSun(sun);
            RenderSettings.sun = sun;
            ApplyAmbient();
        }

        public static void ApplyAmbient(float strength = 1)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.78f, .84f, .92f) * strength;
            RenderSettings.ambientEquatorColor = new Color(.56f, .59f, .62f) * strength;
            RenderSettings.ambientGroundColor = new Color(.37f, .36f, .32f) * strength;
            var ambient = new SphericalHarmonicsL2();
            ambient.AddAmbientLight(new Color(.42f, .46f, .52f) * strength);
            ambient.AddDirectionalLight(Vector3.up, new Color(.40f, .46f, .55f), .35f * strength);
            RenderSettings.ambientProbe = ambient;
            // One cubemap rendered from our own street; no per-car capture or realtime probe cost.
            var reflection = Resources.Load<Cubemap>("StreetLook/StreetReflection");
            if (!reflection) return;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = reflection;
            RenderSettings.reflectionIntensity = 1;
        }
    }
}
