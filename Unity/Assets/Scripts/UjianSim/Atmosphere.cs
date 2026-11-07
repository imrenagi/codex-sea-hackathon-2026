using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UjianSim.Game
{
    /// <summary>
    /// Sun, sky, haze, post-processing and weather. Times of day: siang (midday), sore (late afternoon), malam (night).
    /// Rain adds falling streaks and darker, wetter roads; it is ready for the Ps. 107 (1) headlight lesson.
    /// </summary>
    public class Atmosphere
    {
        public enum TimeOfDay { Siang, Sore, Malam }
        public TimeOfDay Time { get; private set; } = TimeOfDay.Sore;
        public bool Rain { get; private set; }
        /// <summary>0 by day, 1 at night: how strongly street lamps and shop signs glow.</summary>
        public float Night => Time == TimeOfDay.Malam ? 1f : Time == TimeOfDay.Sore ? 0.25f : 0f;

        readonly UnityEngine.Light sun;
        readonly Material sky;
        readonly Volume volume;
        readonly ColorAdjustments color;
        readonly ParticleSystem rain;

        public Atmosphere(Transform parent, Camera cam)
        {
            var root = new GameObject("Atmosphere").transform;
            root.SetParent(parent, false);
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(root, false);
            sun = sunGo.AddComponent<UnityEngine.Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            RenderSettings.sun = sun;

            sky = new Material(Shader.Find("Skybox/Procedural"));
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;

            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            cam.allowHDR = true;

            var vgo = new GameObject("PostProcess");
            vgo.transform.SetParent(root, false);
            volume = vgo.AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.1f);
            bloom.intensity.Override(0.45f);
            bloom.scatter.Override(0.6f);
            color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.15f);
            color.contrast.Override(8f);
            color.saturation.Override(6f);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.18f);
            var ao = profile.Add<WhiteBalance>(true);
            ao.temperature.Override(6f);

            var rgo = new GameObject("Rain");
            rgo.transform.SetParent(cam.transform, false);
            rgo.transform.localPosition = new Vector3(0f, 8f, 6f);
            rain = rgo.AddComponent<ParticleSystem>();
            var main = rain.main;
            main.startLifetime = 1.0f;
            main.startSpeed = 22f;
            main.startSize3D = true;
            main.startSizeX = 0.015f;
            main.startSizeY = 0.6f;
            main.startSizeZ = 0.015f;
            main.maxParticles = 6000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.8f, 0.85f, 0.9f, 0.35f);
            var shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(40f, 1f, 40f);
            shape.rotation = new Vector3(90f, 0f, 0f);
            var em = rain.emission;
            em.rateOverTime = 4500f;
            var pr = rgo.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Stretch;
            pr.velocityScale = 0.04f;
            pr.lengthScale = 1f;
            pr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            pr.sharedMaterial.SetColor("_BaseColor", new Color(0.8f, 0.85f, 0.9f, 0.35f));
            rain.Stop();

            Apply(TimeOfDay.Sore, false);
        }

        public void Activate()
        {
            RenderSettings.sun = sun;
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            Apply(Time, Rain);
        }

        public void Apply(TimeOfDay time, bool rainOn)
        {
            Time = time;
            Rain = rainOn;
            switch (time)
            {
                case TimeOfDay.Siang:
                    sun.transform.rotation = Quaternion.Euler(62f, -35f, 0f);
                    sun.color = new Color(1f, 0.96f, 0.9f);
                    sun.intensity = 1.6f;
                    sky.SetFloat("_AtmosphereThickness", 1.0f);
                    sky.SetColor("_SkyTint", new Color(0.55f, 0.62f, 0.72f));
                    sky.SetFloat("_Exposure", 1.25f);
                    RenderSettings.fogColor = new Color(0.72f, 0.76f, 0.8f);
                    RenderSettings.fogDensity = 0.0035f;
                    break;
                case TimeOfDay.Sore:
                    sun.transform.rotation = Quaternion.Euler(24f, -60f, 0f);
                    sun.color = new Color(1f, 0.82f, 0.62f);
                    sun.intensity = 1.45f;
                    sky.SetFloat("_AtmosphereThickness", 1.35f);
                    sky.SetColor("_SkyTint", new Color(0.6f, 0.55f, 0.5f));
                    sky.SetFloat("_Exposure", 1.15f);
                    RenderSettings.fogColor = new Color(0.82f, 0.74f, 0.64f);
                    RenderSettings.fogDensity = 0.0045f;
                    break;
                default:
                    sun.transform.rotation = Quaternion.Euler(35f, 120f, 0f);
                    sun.color = new Color(0.55f, 0.65f, 0.9f);
                    sun.intensity = 0.3f;
                    sky.SetFloat("_AtmosphereThickness", 0.6f);
                    sky.SetColor("_SkyTint", new Color(0.1f, 0.12f, 0.2f));
                    sky.SetFloat("_Exposure", 0.25f);
                    RenderSettings.fogColor = new Color(0.06f, 0.07f, 0.1f);
                    RenderSettings.fogDensity = 0.006f;
                    break;
            }
            if (rainOn)
            {
                sun.intensity *= 0.45f;
                RenderSettings.fogDensity *= 2.2f;
                RenderSettings.fogColor *= 0.8f;
                rain.Play();
            }
            else rain.Stop();
            color.saturation.Override(rainOn ? -12f : 6f);
            RenderSettings.ambientIntensity = time == TimeOfDay.Malam ? 0.8f : 1f;
            DynamicGI.UpdateEnvironment();
        }
    }
}
