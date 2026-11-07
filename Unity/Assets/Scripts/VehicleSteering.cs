using UnityEngine;

namespace JalanKita
{
    // Ported from Ujian SIM's VehicleModel and GameController.ReadControls.
    // This world uses degrees clockwise from +Z; the source uses radians left from -Z.
    public sealed class VehicleSteering
    {
        public float Input { get; private set; }
        public float Angle { get; private set; }
        public float YawRate { get; private set; }
        public static float Wheelbase(VehicleMode mode) => mode == VehicleMode.Car ? 2.6f : 1.35f;
        public static float Available(VehicleMode mode, float speed) =>
            (mode == VehicleMode.Car ? .6f : .55f) / (1 + Mathf.Abs(speed) / (mode == VehicleMode.Car ? 12 : 8));

        public float Step(VehicleMode mode, float speed, float heading, float target, float dt)
        {
            target = Mathf.Clamp(target, -1, 1);
            Input = Mathf.MoveTowards(Input, target, dt * (target == 0 ? 5 : 3));
            Angle = Input * Available(mode, speed);
            YawRate = speed / Wheelbase(mode) * Mathf.Tan(Angle);
            return Mathf.Repeat(heading + YawRate * dt * Mathf.Rad2Deg, 360);
        }

        public void Reset() { Input = Angle = YawRate = 0; }

        // Ujian SIM's pure-pursuit controller, used only by demonstrations.
        public static float Pursue(Journey run, Vector2 target)
        {
            var delta = target - run.Position;
            float alpha = Mathf.DeltaAngle(run.Heading, Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float wheel = Mathf.Atan(2 * Wheelbase(run.Mode) * Mathf.Sin(alpha) / Mathf.Max(1, delta.magnitude));
            return Mathf.Clamp(wheel / Available(run.Mode, run.Speed), -1, 1);
        }
    }
}
