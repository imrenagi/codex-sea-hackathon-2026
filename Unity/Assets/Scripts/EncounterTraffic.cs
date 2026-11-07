using UnityEngine;

namespace JalanKita
{
    public sealed partial class Journey
    {
        void CheckEncounterContacts(Vector2 oldPosition, float oldHeading,
            (Vector2? cross, Vector2? lane, Vector2? spur, Vector2? pickup) old)
        {
            bool Contact(TrafficChoice choice, Vector2? before, Vector2? after, float heading)
            {
                if (!after.HasValue) return false;
                var start = before.HasValue && Vector2.Distance(before.Value, after.Value) < 8 ? before.Value : after.Value;
                return TrafficFootprint.Sweeps(oldPosition, Position, oldHeading, Heading,
                    TrafficFootprint.PlayerHalfWidth(Mode), TrafficFootprint.PlayerHalfLength(Mode),
                    start, after.Value, heading, heading, choice.Vehicle.HalfWidth, choice.Vehicle.HalfLength);
            }
            if (Contact(Traffic.Cross, old.cross, CrossTraffic, CrossDirection * 90)
                || Contact(Traffic.Lane, old.lane, LaneTraffic, 0)
                || Contact(Traffic.Spur, old.spur, SpurTraffic, 180)
                || Contact(Traffic.Pickup, old.pickup, PickupTraffic, 0)) Fail(Rule.Collision);
        }

        Vector2? FollowEncounter(Vector2? before, Vector2? desired, TrafficChoice choice, Vector2 direction, float speed, float dt)
        {
            if (!desired.HasValue)
            {
                if (!before.HasValue || Vector2.Distance(before.Value, Position) > 12) return null;
                desired = before.Value + direction * speed * dt;
            }
            var target = desired.Value;
            if (!before.HasValue || Mathf.Abs(Vector2.Dot(target - before.Value, new Vector2(direction.y, -direction.x))) > 1
                || Vector2.Dot(target - before.Value, direction) < -8)
                return Vector2.Distance(target, Position) < 8 ? null : desired;
            var from = before.Value;
            float travel = Mathf.Min(speed * dt, Mathf.Max(0, Vector2.Dot(target - from, direction)));
            var right = new Vector2(direction.y, -direction.x);
            var delta = Position - from;
            float yaw = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            float angle = (Heading - yaw) * Mathf.Deg2Rad;
            float playerSide = Mathf.Abs(Mathf.Cos(angle)) * TrafficFootprint.PlayerHalfWidth(Mode)
                + Mathf.Abs(Mathf.Sin(angle)) * TrafficFootprint.PlayerHalfLength(Mode);
            float playerFront = Mathf.Abs(Mathf.Cos(angle)) * TrafficFootprint.PlayerHalfLength(Mode)
                + Mathf.Abs(Mathf.Sin(angle)) * TrafficFootprint.PlayerHalfWidth(Mode);
            float ahead = Vector2.Dot(delta, direction);
            if (ahead > 0 && Mathf.Abs(Vector2.Dot(delta, right)) < playerSide + choice.Vehicle.HalfWidth + .2f)
                travel = Mathf.Min(travel, Mathf.Max(0, ahead - playerFront - choice.Vehicle.HalfLength - 2));
            var next = from + direction * travel;
            if (TrafficFootprint.Sweeps(from, next, yaw, yaw, choice.Vehicle.HalfWidth, choice.Vehicle.HalfLength,
                Position, Position, Heading, Heading, TrafficFootprint.PlayerHalfWidth(Mode), TrafficFootprint.PlayerHalfLength(Mode), .1f))
                return from;
            return next;
        }
    }
}
