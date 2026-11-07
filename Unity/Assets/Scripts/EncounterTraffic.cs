using UnityEngine;

namespace JalanKita
{
    public sealed partial class Journey
    {
        public const int MainRoadTrafficCount = 3;
        readonly Vector2?[] mainRoadTraffic = new Vector2?[MainRoadTrafficCount];
        readonly Vector2?[] previousMainRoadTraffic = new Vector2?[MainRoadTrafficCount];
        readonly bool[] mainRoadTrafficFinished = new bool[MainRoadTrafficCount];

        public Vector2? MainRoadTraffic(int index) => mainRoadTraffic[index];
        public TrafficChoice MainRoadTrafficChoice(int index)
        {
            // Reuse the saved roster's three car types and paints, without allocations.
            foreach (var choice in Traffic.Choices)
                if (!choice.Vehicle.Motorcycle && index-- == 0) return choice;
            return Traffic.Cross;
        }

        public bool MainRoadTrafficComing
        {
            get
            {
                if (priorityTime < 0) return false;
                for (int i = 0; i < MainRoadTrafficCount; i++)
                {
                    var position = mainRoadTraffic[i];
                    // A scheduled car still counts even before it enters the scene.
                    if (!position.HasValue && !mainRoadTrafficFinished[i]) return true;
                    if (!position.HasValue) continue;
                    float clearance = Mathf.Max(4.3f, MainRoadTrafficChoice(i).Vehicle.HalfLength
                        + TrafficFootprint.PlayerHalfWidth(Mode) + 1);
                    if ((Position.x - position.Value.x) * CrossDirection > -clearance) return true;
                }
                return false;
            }
        }

        void ResetMainRoadTraffic()
        {
            for (int i = 0; i < MainRoadTrafficCount; i++)
            {
                mainRoadTraffic[i] = previousMainRoadTraffic[i] = null;
                mainRoadTrafficFinished[i] = false;
            }
        }

        void UpdateMainRoadTraffic(float dt)
        {
            var direction = Vector2.right * CrossDirection;
            for (int i = 0; i < MainRoadTrafficCount; i++)
            {
                var before = mainRoadTraffic[i];
                previousMainRoadTraffic[i] = before;
                if (priorityTime < i * .75f || mainRoadTrafficFinished[i]) continue;
                var choice = MainRoadTrafficChoice(i);
                float speed = 11.2f - i * .4f; // 37–40 km/h; finite, gently staggered flow.
                if (!before.HasValue)
                {
                    // If the lead car was held up, postpone this departure rather
                    // than materializing the next car inside its rear bumper.
                    if (i > 0)
                    {
                        var ahead = mainRoadTraffic[i - 1];
                        if (!ahead.HasValue && !mainRoadTrafficFinished[i - 1]) continue;
                        float spawnGap = choice.Vehicle.HalfLength + MainRoadTrafficChoice(i - 1).Vehicle.HalfLength + 2.5f;
                        if (ahead.HasValue && Vector2.Dot(ahead.Value, direction) + 30 < spawnGap) continue;
                    }
                    mainRoadTraffic[i] = FollowEncounter(null, new Vector2(-30 * CrossDirection, RouteStage.MainJunction),
                        choice, direction, speed, dt);
                    continue;
                }
                float along = Vector2.Dot(before.Value, direction);
                if (along > 40)
                {
                    mainRoadTraffic[i] = null;
                    mainRoadTrafficFinished[i] = true;
                    continue;
                }
                float target = along + speed * dt;
                if (i > 0 && mainRoadTraffic[i - 1].HasValue)
                {
                    float leader = Vector2.Dot(mainRoadTraffic[i - 1].Value, direction);
                    float gap = choice.Vehicle.HalfLength + MainRoadTrafficChoice(i - 1).Vehicle.HalfLength + 2.5f;
                    target = Mathf.Min(target, leader - gap);
                }
                // Keep the existing swept player avoidance, plus a real bumper gap
                // behind the preceding car when the player holds up the procession.
                var desired = new Vector2(Mathf.Max(along, target) * CrossDirection, RouteStage.MainJunction);
                mainRoadTraffic[i] = FollowEncounter(before, desired, choice, direction, speed, dt);
            }
            CrossTraffic = mainRoadTraffic[0]; // Legacy single-actor observation.
        }

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
            if (Stage == 2)
                for (int i = 0; i < MainRoadTrafficCount; i++)
                    if (Contact(MainRoadTrafficChoice(i), previousMainRoadTraffic[i], mainRoadTraffic[i], CrossDirection * 90))
                        Fail(Rule.Collision);
            if ((Stage != 2 && Contact(Traffic.Cross, old.cross, CrossTraffic, CrossDirection * 90))
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
