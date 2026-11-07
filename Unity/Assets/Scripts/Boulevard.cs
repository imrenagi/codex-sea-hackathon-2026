using UnityEngine;

namespace JalanKita
{
    // World-space geometry shared by the simulation, road mesh, and demonstration.
    public static class Boulevard
    {
        public const float HalfWidth = 10.8f, LaneWidth = 3.6f, RailwayZ = 800, RailLine = 790;
        public static Vector2 Center(int stage) => stage == 5 ? new(0, 480) : stage == 6 ? new(0, 640) : new(-180, 640);
        public static Vector2 Forward(int stage) => stage == 7 ? Vector2.left : Vector2.up;
        public static Vector2 Right(int stage) { var d = Forward(stage); return new Vector2(d.y, -d.x); }
        public static int Movement(int stage) => stage == 6 ? -1 : stage == 7 ? 1 : 0;
        public static float Lane(int stage) => stage == 5 ? -5.4f : stage == 6 ? -9 : -1.8f;
        public static Vector2 Exit(int stage) => Movement(stage) == 0 ? Forward(stage) : Right(stage) * Movement(stage);
        public static Vector2 Local(int stage, Vector2 point)
        {
            var delta = point - Center(stage);
            return new Vector2(Vector2.Dot(delta, Right(stage)), Vector2.Dot(delta, Forward(stage)));
        }
        public static Vector2 World(int stage, float lateral, float along) => Center(stage) + Right(stage) * lateral + Forward(stage) * along;
        public static Vector2[] Path(int stage)
        {
            if (stage == 8) return new[] { RouteStage.All[7].Checkpoint, new Vector2(-185.4f, 750), new Vector2(-185.4f, 870) };
            if (stage == 9) return new[] { RouteStage.All[8].Checkpoint, new Vector2(-185.4f, 930), new Vector2(-189, 950), new Vector2(-189, 1070) };
            var points = new System.Collections.Generic.List<Vector2> { RouteStage.All[stage - 1].Checkpoint };
            points.Add(World(stage, Lane(stage), -72));
            points.Add(World(stage, Lane(stage), -18));
            if (Movement(stage) != 0)
            {
                var a = World(stage, Lane(stage), -18);
                var b = Center(stage) + Exit(stage) * 18 + Forward(stage) * (Movement(stage) == -1 ? -9 : 1.8f);
                var c1 = a + Forward(stage) * (stage == 6 ? 7 : 16);
                var c2 = b - Exit(stage) * (stage == 6 ? 7 : 16);
                for (int i = 1; i <= 12; i++)
                {
                    float t = i / 12f, u = 1 - t;
                    points.Add(u*u*u*a + 3*u*u*t*c1 + 3*u*t*t*c2 + t*t*t*b);
                }
            }
            points.Add(RouteStage.All[stage].Checkpoint + Exit(stage) * 12);
            return points.ToArray();
        }
        public static Vector2 LookAhead(Vector2[] path, Vector2 position, float distance)
        {
            float best = float.PositiveInfinity, fraction = 0;
            int segment = 0;
            for (int i = 0; i < path.Length - 1; i++)
            {
                var delta = path[i+1] - path[i];
                float t = Mathf.Clamp01(Vector2.Dot(position - path[i], delta) / delta.sqrMagnitude);
                float error = (position - (path[i] + delta*t)).sqrMagnitude;
                if (error < best) { best = error; fraction = t; segment = i; }
            }
            var point = Vector2.Lerp(path[segment], path[segment+1], fraction);
            for (int i = segment; i < path.Length - 1; i++)
            {
                float length = Vector2.Distance(point, path[i+1]);
                if (length >= distance) return Vector2.MoveTowards(point, path[i+1], distance);
                distance -= length; point = path[i+1];
            }
            return point;
        }
    }

    public sealed partial class Journey
    {
        float boulevardTime = -1, railTime = -1, emergencyTime = -1, emergencyBlocked;
        int approachLane = -1;
        bool enteredJunction;
        public bool AmbulancePassed { get; private set; }
        public Vector2? Ambulance { get; private set; }
        public Vector2? BoulevardPedestrian { get; private set; }
        public float BoulevardPedestrianHeading => Stage == 5 ? 90 : Stage == 6 ? 0 : 270;
        public float CrossingClock => boulevardTime;
        public int MainPhase => boulevardTime < 0 || boulevardTime % 42 < (Stage == 5 ? 16 : Stage == 6 ? 2 : 5) ? 0
            : boulevardTime % 42 < (Stage == 7 ? 9 : 29) ? 2 : boulevardTime % 42 < (Stage == 7 ? 12 : 32) ? 1 : 0;
        public int ArrowPhase => boulevardTime >= 0 && boulevardTime % 42 >= 14 && boulevardTime % 42 < 34 ? 2 : 0;
        public bool MovementGreen => Stage == 7 ? ArrowPhase == 2 : MainPhase == 2;
        public float JunctionStopLine => Mode == VehicleMode.Car ? -26 : -18;
        public bool RailWarning => railTime >= 0 && railTime % 58 < 22;
        public float BarrierDown
        {
            get
            {
                if (!RailWarning) return 0;
                float t = railTime % 58;
                return t < 6 ? Mathf.Clamp01((t - 3) / 3) : t < 19 ? 1 : 1 - (t - 19) / 3;
            }
        }
        public float TrainX => -180 + ((railTime < 0 ? -11 : railTime % 58 - 11) * 16 - 13) * TrainDirection;
        public int TrainDirection => (Save.mistakes[(int)Rule.Railway] + Mathf.Max(0, Mathf.FloorToInt(railTime / 58))) % 2 == 0 ? 1 : -1;
        public bool TrainVisible => railTime >= 0 && railTime % 58 < 26;
        public float AmbulanceGap => Ambulance.HasValue ? Position.y - Ambulance.Value.y : float.PositiveInfinity;
        public Vector2 Nose => Position + new Vector2(Mathf.Sin(Heading * Mathf.Deg2Rad), Mathf.Cos(Heading * Mathf.Deg2Rad)) * (Mode == VehicleMode.Car ? 1.95f : 1);

        void ResetBoulevard()
        {
            boulevardTime = railTime = emergencyTime = -1;
            emergencyBlocked = 0;
            approachLane = -1;
            enteredJunction = AmbulancePassed = false;
            Ambulance = BoulevardPedestrian = null;
        }

        void StepBoulevard(float dt, bool throttle, bool brake, float steer, bool reverse = false)
        {
            var oldNose = Nose;
            Speed = reverse || Speed < 0 ? RoadVehicleMotion.AdvanceSpeed(dt, Mode, throttle, brake, reverse, 16.1f, Speed)
                : Mathf.MoveTowards(Speed, brake ? 0 : throttle ? 16.1f : 0, dt * (brake ? 10.5f : throttle ? 2.85f : 1.7f));
            Heading = steering.Step(Mode, Speed, Heading, steer, dt);
            Position += new Vector2(Mathf.Sin(Heading*Mathf.Deg2Rad), Mathf.Cos(Heading*Mathf.Deg2Rad)) * Speed * dt;
            if (RejectReverseOffRoad()) return;
            rightSignalAge = Indicator == 1 ? rightSignalAge + dt : 0;
            leftSignalAge = Indicator == -1 ? leftSignalAge + dt : 0;
            if (Mode == VehicleMode.Motorcycle && !Headlight && Kph >= 3) Fail(Rule.Headlight);
            overSpeed = Kph > 50.75f ? overSpeed + dt : 0;
            if (overSpeed > .6f) Fail(Rule.BoulevardSpeed);
            if (Stage <= 7) StepBigJunction(dt, oldNose);
            else StepRailAndEmergency(dt, oldNose);
        }

        void StepBigJunction(float dt, Vector2 oldNose)
        {
            int stage = Stage;
            var p = Boulevard.Local(stage, Position);
            var n = Boulevard.Local(stage, Nose);
            var before = Boulevard.Local(stage, oldNose);
            Tick(ref boulevardTime, dt, p.y > -68);
            float t = boulevardTime < 0 ? -1 : boulevardTime % 42;
            float walk = stage == 7 || stage == 5 ? t - 14 : t - 2;
            BoulevardPedestrian = walk >= 0 && walk < 13
                ? stage == 5 ? Boulevard.World(stage, -12 + walk * 1.85f, -14)
                : Boulevard.World(stage, stage == 6 ? -14 : 14, -12 + walk * 1.85f) : null;
            StepCityTraffic(dt);
            float exit = Vector2.Dot(Position - Boulevard.Center(stage), Boulevard.Exit(stage));
            float outgoingLane = Vector2.Dot(Position - Boulevard.Center(stage), new Vector2(Boulevard.Exit(stage).y, -Boulevard.Exit(stage).x));
            bool inside = Mathf.Abs(p.x) <= 17 && Mathf.Abs(p.y) <= 17;
            bool approach = p.y < -11 && p.x >= -10.4f && p.x <= 10.4f;
            bool leaving = exit > 11 && outgoingLane >= -10.4f && outgoingLane <= 10.4f;
            if (!inside && !approach && !leaving) Fail(Rule.OffRoad);
            if ((approach && p.x > .2f || leaving && outgoingLane > .2f) && Mathf.Abs(Speed) > 1)
            { wrongWay += dt; if (wrongWay > .6f) Fail(Rule.KeepLeft); }
            else wrongWay = 0;

            if (approach)
            {
                int current = Mathf.Clamp(Mathf.FloorToInt((p.x + 10.8f) / 3.6f), 0, 2);
                if (approachLane >= 0 && current != approachLane)
                {
                    if (p.y > -56 && p.y < -26) Fail(Rule.MovementLane);
                    if ((current > approachLane ? rightSignalAge : leftSignalAge) < .28f) Fail(Rule.Indicator);
                }
                approachLane = current;
                if (p.y > -56 && p.y < -26 && Mathf.Abs(p.x - Boulevard.Lane(stage)) > 1.7f) Fail(Rule.MovementLane);
            }
            if (before.y < JunctionStopLine && n.y >= JunctionStopLine)
            {
                if (!MovementGreen) Fail(stage == 7 ? Rule.RightArrow : Mode == VehicleMode.Car ? Rule.StopBox : Rule.Signal);
                if (stage != 5 && (stage == 6 ? leftSignalAge : rightSignalAge) < .3f) Fail(Rule.TurnSignal);
                enteredJunction = true;
            }
            // Do not permit cutting across the junction from an unrelated approach.
            if (!enteredJunction && p.y > -11) Fail(Rule.MovementLane);
            if (BoulevardPedestrian.HasValue)
            {
                bool crossing = stage == 5 ? n.y >= -16 && p.y <= -11
                    : Vector2.Dot(Nose - Boulevard.Center(stage), Boulevard.Exit(stage)) >= 11 && exit <= 17;
                if (crossing && BoulevardPedestrianBlocksPath || TouchesPedestrian(BoulevardPedestrian.Value)) Fail(Rule.Pedestrian);
            }
            if (exit > 20 && Mathf.Abs(outgoingLane - Boulevard.Lane(stage)) > 1.8f) Fail(Rule.MovementLane);
            if (exit >= Vector2.Dot(RouteStage.All[stage].Checkpoint - Boulevard.Center(stage), Boulevard.Exit(stage)) && enteredJunction && !Failure.HasValue) Advance();
        }

        void StepRailAndEmergency(float dt, Vector2 oldNose)
        {
            float x = Position.x + 180;
            if (Mathf.Abs(x) > 10.4f || Position.y < 700 || Position.y > 1160) Fail(Rule.OffRoad);
            if (x > .2f && Mathf.Abs(Speed) > 1) { wrongWay += dt; if (wrongWay > .6f) Fail(Rule.KeepLeft); }
            else wrongWay = 0;
            int current = Mathf.Clamp(Mathf.FloorToInt((x + 10.8f) / 3.6f), 0, 2);
            if (approachLane >= 0 && current != approachLane && (current > approachLane ? rightSignalAge : leftSignalAge) < .28f)
                Fail(Rule.Indicator);
            if (approachLane >= 0 && current != approachLane && current == 1 && Ambulance.HasValue
                && Mathf.Abs(AmbulanceGap) < Mathf.Max(8, (16.1f - Speed) * 2 + 4)) Fail(Rule.Gap);
            approachLane = current;
            if (Stage == 8)
            {
                Tick(ref railTime, dt, Position.y >= 745);
                StepCityTraffic(dt);
                if (RailWarning && oldNose.y < Boulevard.RailLine && Nose.y >= Boulevard.RailLine) Fail(Rule.Railway);
                if (RailWarning && Nose.y >= 797 && Position.y <= 804) Fail(Rule.Railway);
                if (Position.y >= 850 && !Failure.HasValue) Advance();
                return;
            }
            StepCityTraffic(dt);
            if (Mathf.Abs(Position.x + 189) < 1.8f && Mathf.Abs(Position.y - 914) < 4.6f) Fail(Rule.Collision);
            if (emergencyTime < 0 && Position.y >= 924)
            {
                emergencyTime = 0;
                Ambulance = new Vector2(-185.4f, Position.y - 70);
            }
            if (Ambulance.HasValue)
            {
                emergencyTime += dt;
                var ambulance = Ambulance.Value;
                bool blocked = Mathf.Abs(Position.x - ambulance.x) < 1.8f && AmbulanceGap > -4 && AmbulanceGap < 35;
                emergencyBlocked = blocked ? emergencyBlocked + dt : 0;
                if (emergencyBlocked >= 3) Fail(Rule.Emergency);
                float next = ambulance.y + 16.1f * dt;
                next = Mathf.Min(next, ambulance.y + Mathf.Max(0, CityTraffic.GapAhead(ambulance,Vector2.up,2.2f)-2));
                if (blocked) next = Mathf.Min(next, Position.y - 6);
                Ambulance = new Vector2(ambulance.x, next);
                if (Mathf.Abs(Position.x - ambulance.x) < 1.8f && Mathf.Abs(Position.y - ambulance.y) < 4) Fail(Rule.Collision);
                if (ambulance.y > Position.y + 8) AmbulancePassed = true;
            }
            if (Position.y >= 1040 && AmbulancePassed && !Failure.HasValue) Advance();
        }
    }
}
