using UnityEngine;

namespace JalanKita
{
    public sealed partial class District
    {
        bool homePanoramaActive;
        float homePanoramaAngle = 180f;

        // A single existing camera, with no extra render targets or world simulation.
        // Only the presentation moves: the UI and saved driving progress stay untouched.
        void PresentHomePanorama(float dt)
        {
            homePanoramaActive = true;
            // Three-minute orbit; clamp long frames to avoid a jump after focus returns.
            homePanoramaAngle = Mathf.Repeat(homePanoramaAngle + Mathf.Clamp(dt, 0f, .1f) * 2f, 360f);
            float angle = homePanoramaAngle * Mathf.Deg2Rad;
            var target = new Vector3(0f, 3f, 75f);
            var eye = target + new Vector3(Mathf.Sin(angle) * 24f, 22f, Mathf.Cos(angle) * 38f);
            camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye, Vector3.up));
            camera.fieldOfView = 58f;
        }
    }
}
