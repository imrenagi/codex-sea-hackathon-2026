using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace JalanKita
{
    // A low, damped chase view keeps signs and the stopping distance visible.
    public sealed class DrivingCamera
    {
        float yaw, headYaw;
        public static void Configure(Camera camera)
        {
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 60;
            camera.nearClipPlane = .08f;
            camera.farClipPlane = 360;
            camera.allowHDR = true;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }

        public void Present(Camera camera, Vector3 position, float heading, float speed, bool motorcycle,
            bool hood, float dt, bool snap, float lookInput = 0)
        {
            float blend = snap ? 1 : 1 - Mathf.Exp(-7 * Mathf.Max(0, dt));
            yaw = snap ? heading : Mathf.LerpAngle(yaw, heading, 1 - Mathf.Exp(-4.5f * Mathf.Max(0, dt)));
            var rotation = Quaternion.Euler(0, yaw, 0);
            var eye = position + rotation * (hood ? new Vector3(0, motorcycle ? 1.52f : 1.4f, .65f)
                : new Vector3(0, motorcycle ? 2.65f : 2.85f, motorcycle ? -5.7f : -6.4f));
            var look = position + rotation * new Vector3(0, hood ? 1.36f : 1.22f, hood ? 30 : 13);
            headYaw = Mathf.MoveTowards(snap ? 0 : headYaw, lookInput * 80, Mathf.Max(0, dt) * 320);
            var view = Quaternion.LookRotation(look - eye);
            if (Mathf.Abs(headYaw) > .1f)
            {
                float turn = headYaw / 80;
                if (hood)
                {
                    eye += rotation * new Vector3(turn * .08f, 0, Mathf.Abs(turn) * .06f);
                    view = rotation * Quaternion.Euler(4, headYaw, 0);
                }
                else
                {
                    var head = position + Vector3.up * (motorcycle ? 2 : 2.3f) - rotation * Vector3.forward * (motorcycle ? .6f : 1.2f);
                    eye = Vector3.Lerp(eye, head, Mathf.Abs(turn));
                    view = rotation * Quaternion.Euler(Mathf.Lerp(12, 6, Mathf.Abs(turn)), headYaw, 0);
                }
            }
            camera.transform.SetPositionAndRotation(Vector3.Lerp(camera.transform.position, eye, blend),
                Quaternion.Slerp(camera.transform.rotation, view, blend));
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, Mathf.Abs(headYaw) > .1f ? 70 : (hood ? 64 : 59) + Mathf.Clamp01(speed / 17) * 3, blend);
        }
    }
}
