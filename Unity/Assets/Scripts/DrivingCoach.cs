using UnityEngine;
using static JalanKita.RouteStage;

namespace JalanKita
{
    public static class DrivingCoach
    {
        public static void Step(Journey run, float dt)
        {
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
                    if (run.PhoneRinging) run.ResolvePhone(false);
                    break;
            }

            float remaining = Mathf.Max(0, stopAt - 1.15f - run.Front);
            float desired = Mathf.Min(cruise, Mathf.Sqrt(2 * 5.1f * remaining));
            float steer = Mathf.Clamp((targetX - run.Position.x) * .48f, -1, 1);
            run.Step(dt, run.Speed < desired - .14f, run.Speed > desired + .12f, steer);
        }

        static bool TrafficComing(Journey run, float junction)
        {
            if (!run.CrossTraffic.HasValue || run.CrossTraffic.Value.y != junction) return false;
            return (run.Position.x - run.CrossTraffic.Value.x) * run.CrossDirection > -4.3f;
        }
    }
}
