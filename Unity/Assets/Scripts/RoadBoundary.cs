using System;
using UnityEngine;

namespace JalanKita
{
    // Road containment is independent of actor collisions. Callers still sweep the
    // returned pose against traffic before accepting either its position or yaw.
    public static class RoadBoundary
    {
        public readonly struct Frame
        {
            public readonly Vector2 Center, Forward;
            public readonly float HalfWidth;
            public Frame(Vector2 center, Vector2 forward, float halfWidth)
            { Center = center; Forward = forward.normalized; HalfWidth = halfWidth; }
            public Vector2 Right => new(Forward.y, -Forward.x);
            public float Heading => Mathf.Atan2(Forward.x, Forward.y) * Mathf.Rad2Deg;
        }

        // Include the visible mirrors/handlebars as well as the collision body.
        public static float HalfWidth(VehicleMode mode) => mode == VehicleMode.Car ? 1f : .45f;
        public static float HalfLength(VehicleMode mode) => mode == VehicleMode.Car ? 2.05f : 1.05f;
        const float Clearance = .12f;

        public static bool Contains(Func<Vector2, Frame> road, VehicleMode mode, Vector2 position, float heading)
        {
            float angle = heading * Mathf.Deg2Rad;
            Vector2 forward = new(Mathf.Sin(angle), Mathf.Cos(angle)), right = new(forward.y, -forward.x);
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                {
                    var point = position + right * (x * HalfWidth(mode)) + forward * (z * HalfLength(mode));
                    var frame = road(point);
                    if (Mathf.Abs(Vector2.Dot(point - frame.Center, frame.Right)) > frame.HalfWidth - Clearance + .002f)
                        return false;
                }
            return true;
        }

        public static Vector2 Fit(Func<Vector2, Frame> road, VehicleMode mode, Vector2 position, float heading)
        {
            float angle = heading * Mathf.Deg2Rad;
            Vector2 forward = new(Mathf.Sin(angle), Mathf.Cos(angle)), right = new(forward.y, -forward.x);
            for (int pass = 0; pass < 8; pass++)
            {
                bool changed = false;
                for (int x = -1; x <= 1; x++)
                    for (int z = -1; z <= 1; z++)
                    {
                        var point = position + right * (x * HalfWidth(mode)) + forward * (z * HalfLength(mode));
                        var frame = road(point);
                        float lateral = Vector2.Dot(point - frame.Center, frame.Right);
                        float limit = Mathf.Max(0, frame.HalfWidth - Clearance);
                        float excess = lateral - Mathf.Clamp(lateral, -limit, limit);
                        if (Mathf.Abs(excess) < .0001f) continue;
                        position -= frame.Right * excess;
                        changed = true;
                    }
                if (!changed) break;
            }
            return position;
        }

        public static Vector2 Move(Func<Vector2, Frame> road, VehicleMode mode, Vector2 from,
            float dt, ref float speed, ref float heading, out bool contact)
        {
            float angle = heading * Mathf.Deg2Rad;
            Vector2 forward = new(Mathf.Sin(angle), Mathf.Cos(angle));
            float direction = speed < 0 ? -1 : 1;
            var travelForward = forward * direction;
            float distance = Mathf.Abs(speed) * Mathf.Max(0, dt);
            var next = from + travelForward * distance;
            contact = !Contains(road, mode, next, heading);
            if (!contact) return next;
            if (distance <= .000001f) return Fit(road, mode, next, heading);

            var frame = road(from);
            float alignment = Vector2.Dot(travelForward, frame.Forward);
            var tangent = frame.Forward * (alignment < 0 ? -1 : 1);
            float tangentHeading = Mathf.Atan2(tangent.x, tangent.y) * Mathf.Rad2Deg + (direction < 0 ? 180 : 0);
            // A glancing contact retains forward travel. Even a nose-on approach
            // gets a small tangent crawl so steering can recover without reverse.
            // Yaw correction is distance-based: a stopped car never spins in place.
            speed *= Mathf.Exp(-3 * (1 - Mathf.Abs(alignment)) * Mathf.Max(0, dt));
            distance = Mathf.Abs(speed) * Mathf.Max(0, dt);
            heading = Mathf.MoveTowardsAngle(heading, tangentHeading, Mathf.Min(distance * 35, 70 * Mathf.Max(0, dt)));
            next = from + tangent * (distance * Mathf.Max(.25f, Mathf.Abs(alignment)));
            return Fit(road, mode, next, heading);
        }
    }
}
