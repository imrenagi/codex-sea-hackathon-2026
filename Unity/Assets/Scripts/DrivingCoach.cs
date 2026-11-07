using UnityEngine;
using static JalanKita.RouteStage;

namespace JalanKita
{
    public static class DrivingCoach
    {
        public static void Step(Journey run, float dt)
        {
            if (run.Finished || run.Failure.HasValue) return;
            if (run.Stage >= 5) { DriveBoulevard(run, dt); return; }
            if (run.IsMarket) { MarketStep(run, dt); return; }
            float cruise = 9.2f;
            float targetX = LeftLane;
            float stopAt = float.PositiveInfinity;

            switch (run.Stage)
            {
                case 1:
                    if (!run.FullStop || (run.Front < StopJunction - 4.3f && TrafficComing(run, StopJunction)))
                        stopAt = StopLine;
                    else if (run.Front < LightLine && run.LightPhase != 2)
                        stopAt = LightLine;
                    break;
                case 2:
                    if (run.Front < Zebra - 2 && run.Pedestrian.HasValue)
                        stopAt = Zebra - 2;
                    else if (run.Front < MainJunction - 4.3f && TrafficComing(run, MainJunction))
                        stopAt = YieldLine;
                    break;
                case 3:
                    cruise = 4.85f;
                    float z = run.Position.y;
                    float gap = run.LaneTraffic.HasValue ? run.LaneTraffic.Value.y - z : float.PositiveInfinity;
                    bool goRight = !run.ChangedRight && z >= OneWayStart + 6 && gap > 9;
                    bool stayRight = run.ChangedRight && !run.ReturnedLeft && z < 249;
                    if (goRight || stayRight)
                    {
                        if (run.Indicator != 1) run.ToggleIndicator(1);
                        targetX = RightLane;
                    }
                    else if (run.ChangedRight && !run.ReturnedLeft && run.Indicator != -1)
                        run.ToggleIndicator(-1);
                    else if (run.ReturnedLeft && run.Indicator != 0)
                        run.ToggleIndicator(run.Indicator);
                    break;
                case 4:
                    cruise = 7.7f;
                    if (run.IsSchool && run.Front < run.CrossingZebra - 2 && run.Pedestrian.HasValue)
                        stopAt = run.CrossingZebra - 2;
                    if (run.PhoneRinging) run.ResolvePhone(false);
                    break;
            }

            float remaining = Mathf.Max(0, stopAt - 1.15f - run.Front);
            float desired = Mathf.Min(cruise, Mathf.Sqrt(2 * 5.1f * remaining));
            float steer = run.UsesRouteSteering
                ? VehicleSteering.Pursue(run, new Vector2(targetX, run.Position.y + Mathf.Max(4, 3 + .6f * run.Speed)))
                : RoadVehicleMotion.TrackLane(run.Mode, run.Speed, run.Heading, run.Position.x, targetX);
            run.Step(dt, run.Speed < desired - .14f, run.Speed > desired + .12f, steer);
        }

        static void MarketStep(Journey run, float dt)
        {
            float targetX = LeftLane;
            float desired = 4.3f;
            if (!run.PassengerPickedUp)
            {
                if (run.Position.y >= Mission.PickupStart + 1)
                {
                    targetX = -5.8f;
                    if (run.Indicator != -1) run.ToggleIndicator(-1);
                    float remaining = Mathf.Max(0, Mission.PickupPoint - 1.15f - run.Front);
                    desired = Mathf.Min(desired, Mathf.Sqrt(2 * 4 * remaining));
                }
                if (run.CanPickup) run.Pickup();
            }
            if (run.PassengerPickedUp)
            {
                if (run.Indicator != 1) run.ToggleIndicator(1);
                if (!run.PickupTraffic.HasValue || run.PickupTraffic.Value.y < run.Position.y + 12)
                {
                    targetX = -5.8f;
                    desired = 0;
                }
                else targetX = LeftLane;
                // Once back in traffic, keep going even after the approaching car leaves the scene.
                if (run.Position.x >= -4.25f) { targetX = LeftLane; desired = 4.3f; }
            }
            float steer = run.UsesRouteSteering
                ? VehicleSteering.Pursue(run, new Vector2(targetX, run.Position.y + Mathf.Max(4, 3 + .6f * run.Speed)))
                : RoadVehicleMotion.TrackLane(run.Mode, run.Speed, run.Heading, run.Position.x, targetX);
            run.Step(dt, run.Speed < desired - .14f, run.Speed > desired + .12f, steer);
        }

        static readonly Vector2[][] boulevardPaths = new Vector2[5][];

        static void DriveBoulevard(Journey run, float dt)
        {
            int stage = run.Stage;
            if (run.Finished) return;
            var path = boulevardPaths[stage - 5] ??= Boulevard.Path(stage);
            float cruise = stage <= 7 ? 5.5f : 8;
            float remaining = float.PositiveInfinity;
            int signal = stage == 6 ? -1 : stage == 7 ? 1 : stage == 5 || stage == 8 ? -1 : run.Position.y > 922 ? -1 : 0;
            if (stage <= 7)
            {
                float nose = Boulevard.Local(stage, run.Nose).y;
                float exit = Vector2.Dot(run.Position - Boulevard.Center(stage), Boulevard.Exit(stage));
                if (nose < run.JunctionStopLine && (!run.MovementGreen || run.BoulevardPedestrian.HasValue))
                    remaining = run.JunctionStopLine - 1.2f - nose;
                if (exit > 22) signal = 0;
            }
            else if (stage == 8 && run.Nose.y < Boulevard.RailLine && run.RailWarning)
                remaining = Boulevard.RailLine - 1.2f - run.Nose.y;
            else if (stage == 9 && !run.AmbulancePassed)
                remaining = 1038 - run.Nose.y;
            if (run.Indicator != signal)
            {
                if (signal == 0) { if (run.Indicator != 0) run.ToggleIndicator(run.Indicator); }
                else run.ToggleIndicator(signal);
            }
            if (run.CityTraffic != null)
            {
                Vector2 forward=new(Mathf.Sin(run.Heading*Mathf.Deg2Rad),Mathf.Cos(run.Heading*Mathf.Deg2Rad));
                remaining=Mathf.Min(remaining,run.CityTraffic.GapAhead(run.Position,forward,run.Mode==VehicleMode.Car?2.1f:1)-3);
            }
            float desired = Mathf.Min(cruise, Mathf.Sqrt(2 * 5.1f * Mathf.Max(0, remaining)));
            var target = Boulevard.LookAhead(path, run.Position, 4);
            float steer = VehicleSteering.Pursue(run, target);
            run.Step(dt, run.Speed < desired - .14f, run.Speed > desired + .12f, steer);
        }

        static bool TrafficComing(Journey run, float junction)
        {
            if (!run.CrossTraffic.HasValue || run.CrossTraffic.Value.y != junction) return false;
            return (run.Position.x - run.CrossTraffic.Value.x) * run.CrossDirection > -4.3f;
        }
    }
}
