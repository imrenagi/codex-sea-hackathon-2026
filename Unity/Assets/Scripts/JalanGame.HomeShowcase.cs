using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        CityMapPreview homeCity;
        int homeScene = -1;
        float homeSceneTime, homeOrbitTime;
        bool homeCityFailed, homeSopoFailed;
        Label homeSceneCaption;

        void PresentHomeShowcase(float dt)
        {
            dt = Mathf.Clamp(dt, 0, .1f);
            homeSceneTime += dt;
            homeOrbitTime += dt;
            if (homeScene < 0 || homeSceneTime >= 5f)
            {
                homeSceneTime = 0;
                homeScene = homeScene < 0 ? 1 : (homeScene + 1) % 3;
                ActivateHomeScene();
            }
            if (homeScene == 1 && homeCity != null) homeCity.PresentShowcase(homeOrbitTime);
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
            homeCity?.SetShowcaseVisible(false);
            if (jaksa != null) jaksa.Street.Root.SetActive(false);
            trainingRoot.SetActive(false);
            string caption;
            if (homeScene == 1 && !homeCityFailed)
            {
                try
                {
                    if (homeCity == null)
                    {
                        var map = CityMapData.Load("bundaran_hi");
                        // Keep only a small central subset, not six full city maps in memory.
                        map.buildings = map.buildings.OrderBy(b => b.points.Average(p => p.x*p.x+p.z*p.z)).Take(300).ToArray();
                        map.roads = map.roads.OrderBy(r => r.points.Min(p => p.x*p.x+p.z*p.z)).Take(250).ToArray();
                        homeCity = new CityMapPreview(transform, map);
                    }
                    homeCity.SetShowcaseVisible(true);
                }
                catch (Exception error) { homeCityFailed = true; Debug.LogWarning("Home city preview unavailable: " + error.Message); }
            }
            if (homeScene == 2 && !homeSopoFailed)
            {
                try { jaksa ??= new JaksaDrive(transform); jaksa.Street.Activate(); }
                catch (Exception error) { homeSopoFailed = true; Debug.LogWarning("Home Sopo Del preview unavailable: " + error.Message); }
            }
            if (homeScene == 1 && homeCity != null && !homeCityFailed)
                caption = "Bundaran HI · OSM preview · © OpenStreetMap contributors";
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

        void StopHomeShowcase(Page destination)
        {
            if (homeScene < 0 && homeCity == null) return;
            homeCity?.Dispose(); homeCity = null;
            if (jaksa != null && destination != Page.Jaksa) jaksa.Street.Root.SetActive(false);
            homeScene = -1; homeSceneTime = 0; homeSceneCaption = null;
            // Some callers already created their own scene before SetPage: don't override it.
            if (destination == Page.CityPreview) cityPreview?.SetShowcaseVisible(true);
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
