using UnityEngine;

namespace JalanKita
{
    // Deterministic low-speed bicycle model authored for Jalan Kita.
    // Steering recentres the wheels, never the heading of a stationary vehicle.
    public sealed class RoadVehicleMotion
    {
        public float SteeringAngle { get; private set; }
        public void Reset() => SteeringAngle = 0;
        public static float Wheelbase(VehicleMode mode) => mode == VehicleMode.Car ? 2.5f : 1.28f;
        public static float SteeringLimit(float speed, VehicleMode mode) =>
            Mathf.Lerp(mode == VehicleMode.Car ? 32 : 24, mode == VehicleMode.Car ? 11 : 8, Mathf.Clamp01(speed / 17));

        public void Step(float dt, VehicleMode mode, bool throttle, bool brake, float input,
            float speedLimit, ref float speed, ref float heading)
        {
            if (dt <= 0) return;
            dt = Mathf.Min(dt, .05f);
            float acceleration = mode == VehicleMode.Car ? 2.85f : 3.3f;
            acceleration *= Mathf.Lerp(1, .58f, Mathf.Clamp01(speed / Mathf.Max(1, speedLimit)));
            float resistance = .55f + speed * speed * .004f;
            speed = Mathf.MoveTowards(speed, throttle && !brake ? speedLimit : 0,
                (brake ? 8.4f : speed > speedLimit ? 6 : throttle ? acceleration : resistance) * dt);
            float limit = SteeringLimit(speed, mode);
            SteeringAngle = Mathf.MoveTowards(SteeringAngle, Mathf.Clamp(input, -1, 1) * limit,
                (Mathf.Abs(input) < .01f ? 115 : 90) * dt);
            heading += speed / Wheelbase(mode) * Mathf.Tan(SteeringAngle * Mathf.Deg2Rad) * Mathf.Rad2Deg * dt;
            heading = Mathf.DeltaAngle(0, heading);
        }

        public static float TrackLane(VehicleMode mode, float speed, float heading, float x, float targetX, float roadHeading = 0)
        {
            float desiredHeading = roadHeading + Mathf.Clamp(Mathf.Atan2(targetX - x, Mathf.Max(4, speed * .9f)) * Mathf.Rad2Deg, -28, 28);
            float yawRate = Mathf.DeltaAngle(heading, desiredHeading) * Mathf.Deg2Rad * 2.5f;
            float wheelAngle = Mathf.Atan(Wheelbase(mode) * yawRate / Mathf.Max(2, speed)) * Mathf.Rad2Deg;
            return Mathf.Clamp(wheelAngle / SteeringLimit(speed, mode), -1, 1);
        }
    }
}
