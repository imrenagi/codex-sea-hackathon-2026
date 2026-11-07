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
        // Propulsion is limited by engine/drag balance, independently of posted road limits.
        public static float FreeSpeed(VehicleMode mode) => float.PositiveInfinity;
        public static float ReverseLimit(VehicleMode mode) => mode == VehicleMode.Car ? 2.2f : 1.5f;
        public static float SteeringLimit(float speed, VehicleMode mode) =>
            Mathf.Lerp(mode == VehicleMode.Car ? 32 : 24, mode == VehicleMode.Car ? 11 : 8, Mathf.Clamp01(Mathf.Abs(speed) / 17));

        // Reverse is an explicit request. Existing brake-only coach callers never
        // enter reverse, and a direction change always reaches zero first.
        public static float AdvanceSpeed(float dt, VehicleMode mode, bool throttle, bool brake, bool reverse,
            float speedLimit, float speed, float brakeDeceleration = 8.4f)
        {
            if (dt <= 0) return speed;
            dt = Mathf.Min(dt, .05f);
            if (brake || throttle && reverse || reverse && speed > 0 || throttle && speed < 0)
                return Mathf.MoveTowards(speed, 0, brakeDeceleration * dt);
            if (reverse)
            {
                float target = -Mathf.Min(ReverseLimit(mode), Mathf.Max(0, speedLimit));
                return Mathf.MoveTowards(speed, target, (speed < target ? 8.4f : 1.6f) * dt);
            }
            if (speed < 0) return Mathf.MoveTowards(speed, 0, 1.2f * dt);
            float resistance = .55f + speed * speed * .004f;
            float acceleration = ((mode == VehicleMode.Car ? 3.3f : 3.8f) + .55f) / (1 + speed / 24f);
            // A finite limit is supplied only by an actual obstacle/following gap.
            // Releasing the accelerator uses rolling/air resistance, never an invented zero limit.
            if (speed > speedLimit) return Mathf.MoveTowards(speed, speedLimit, 6 * dt);
            return Mathf.Min(speedLimit, Mathf.Max(0, speed + ((throttle ? acceleration : 0) - resistance) * dt));
        }

        public void Step(float dt, VehicleMode mode, bool throttle, bool brake, float input,
            float speedLimit, ref float speed, ref float heading, bool reverse = false)
        {
            if (dt <= 0) return;
            dt = Mathf.Min(dt, .05f);
            speed = AdvanceSpeed(dt, mode, throttle, brake, reverse, speedLimit, speed);
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
