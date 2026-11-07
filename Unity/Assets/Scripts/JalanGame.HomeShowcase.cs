using System;
using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        LandmarkEnvironment homeLandmark;
        Camera homeLandmarkCamera;
        int homeScene = -1;
        float homeSceneTime, homeOrbitTime;
        bool homeCityFailed, homeSopoFailed;
        Label homeSceneCaption;

        void PresentHomeShowcase(float dt)
        {
            dt = Mathf.Clamp(dt, 0, .1f);
            homeSceneTime += dt;
            homeOrbitTime += dt;
            if (homeScene < 0 || homeSceneTime >= 6f)
            {
                homeSceneTime = 0;
                homeScene = homeScene < 0 ? 2 : (homeScene + 2) % 3;
                ActivateHomeScene();
            }
            if (homeScene == 1 && homeLandmarkCamera && !homeCityFailed)
            {
                float angle = (homeOrbitTime * 3f + 180f) * Mathf.Deg2Rad;
                var target = homeLandmark.LandmarkTarget;
                var eye = target + new Vector3(Mathf.Sin(angle)*150, 95, Mathf.Cos(angle)*150);
                homeLandmarkCamera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target-eye));
                homeLandmark.Water.Present(homeLandmarkCamera, homeOrbitTime);
            }
            else if (homeScene == 2 && jaksa != null && !homeSopoFailed)
            {
                // Presentation only: never step/reset the user's free-drive simulation.
                float angle = (homeOrbitTime * 3f + 180f) * Mathf.Deg2Rad;
                var target = new Vector3(jaksa.Street.RoadX(120), 5, 120);
                var eye = target + new Vector3(Mathf.Sin(angle)*35, 28, Mathf.Cos(angle)*45);
                jaksa.Camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target-eye));
                jaksa.Camera.fieldOfView = 58;
            }
            else district.Present(journey, false, dt, false, 0, true);
        }

        void ActivateHomeScene()
        {
            if (homeLandmark != null && homeLandmark.Root) homeLandmark.Root.SetActive(false);
            if (jaksa != null) jaksa.Street.Root.SetActive(false);
            trainingRoot.SetActive(false);
            string caption;
            if (homeScene == 1 && !homeCityFailed)
            {
                try
                {
                    // Use the same complete landmark scenery as the HI mission, without
                    // constructing a second driving simulation, traffic or mirror cameras.
                    // Keep it cached while this game exists; cycling only toggles visibility.
                    if (homeLandmark == null)
                    {
                        homeLandmark = new LandmarkEnvironment(transform, JakartaPlace.BundaranHI);
                        homeLandmarkCamera = new GameObject("Home HI panorama camera").AddComponent<Camera>();
                        homeLandmarkCamera.transform.SetParent(homeLandmark.Root.transform, false);
                        homeLandmarkCamera.clearFlags = CameraClearFlags.Skybox;
                        homeLandmarkCamera.fieldOfView = 58;
                        homeLandmarkCamera.nearClipPlane = .3f;
                        homeLandmarkCamera.farClipPlane = 1500;
                        homeLandmarkCamera.allowHDR = true;
                        homeLandmarkCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                    }
                    homeLandmark.Activate();
                    homeLandmark.Water.SetAudio(0, false);
                }
                catch (Exception error) { homeCityFailed = true; Debug.LogWarning("Home city preview unavailable: " + error.Message); }
            }
            if (homeScene == 2 && !homeSopoFailed)
            {
                try { jaksa ??= new JaksaDrive(transform); jaksa.Street.Activate(); }
                catch (Exception error) { homeSopoFailed = true; Debug.LogWarning("Home Sopo Del preview unavailable: " + error.Message); }
            }
            if (homeScene == 1 && homeLandmarkCamera && !homeCityFailed)
                caption = "Bundaran HI";
            else if (homeScene == 2 && jaksa != null && !homeSopoFailed)
                caption = "Sopo Del · Mega Kuningan";
            else
            {
                trainingRoot.SetActive(true);
                district.RestoreAtmosphere();
                caption = "Kampung Kenanga";
            }
            if (homeSceneCaption == null || homeSceneCaption.parent == null)
                homeSceneCaption = AddLabel(dialog, caption, "home-tagline home-scene-caption");
            else homeSceneCaption.text = caption;
        }

        void StopHomeShowcase(Page destination, bool tearingDown = false)
        {
            if (homeScene < 0) return;
            if (homeLandmark != null && homeLandmark.Root) homeLandmark.Root.SetActive(false);
            if (jaksa != null && jaksa.Street.Root && destination != Page.Jaksa) jaksa.Street.Root.SetActive(false);
            homeScene = -1; homeSceneTime = 0; homeSceneCaption = null;
            // Scene objects may already be destroyed during domain reload/OnDestroy.
            if (tearingDown) return;
            // Some callers already created their own scene before SetPage: don't override it.
            if (destination == Page.CityPreview) cityPreview?.SetShowcaseVisible(true);
            else if (destination == Page.Jaksa && hiActive && hi != null) hi.Environment.Activate();
            else if (destination == Page.Jaksa && jaksa != null) jaksa.Street.Activate();
            if (destination != Page.CityPreview && destination != Page.Jaksa)
            {
                trainingRoot.SetActive(true);
                district.RestoreAtmosphere();
                district.Snap();
            }
        }
    }
}
