using System;
using System.Reflection;
using UnityEngine;

namespace JalanKita.EditorTools
{
    public static class PedestrianChecks
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static void Set(Journey run, string name, object value) => typeof(Journey).GetProperty(name).SetValue(run, value);
        static void Field(Journey run, string name, object value) => typeof(Journey).GetField(name, Private).SetValue(run, value);
        static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        public static void Run()
        {
            foreach (int direction in new[] {-1, 1})
            {
                var axis = Vector2.right * direction;
                Require(PedestrianClearance.Blocks(axis * 2, axis, Vector2.zero, Vector2.right, 3.6f), "Pedestrian inside lane margin blocks");
                Require(!PedestrianClearance.Blocks(axis * 3, axis, Vector2.zero, Vector2.right, 3.6f), "Pedestrian walking away clears lane before road edge");
                Require(PedestrianClearance.Blocks(-axis * 5, axis, Vector2.zero, Vector2.right, 3.6f), "Incoming adjacent lane remains protected");
            }
            foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            {
                foreach (int direction in new[] {-1, 1}) foreach (bool clear in new[] {false, true})
                {
                    var save = new JourneySave { next = 2 };
                    save.mistakes[(int)Rule.Pedestrian] = direction == 1 ? 0 : 1;
                    var run = new Journey(mode, save, persistent: false, trafficSeed: 1);
                    float pedestrianX = clear ? (direction == 1 ? 2 : -5.5f) : -2.15f;
                    float clock = .65f + (pedestrianX * direction + 5.6f) / 1.83f;
                    Field(run, "zebraTime", clock - .02f);
                    Set(run, "Position", new Vector2(-2.15f, RouteStage.Zebra - 2 - (mode == VehicleMode.Car ? 1.95f : 1) - .02f));
                    Set(run, "Speed", 2f);
                    run.Step(.02f, true, false, 0);
                    Require(run.Pedestrian.HasValue, "Clearance fixture still has a pedestrian on the road");
                    Require(clear ? !run.Failure.HasValue : run.Failure == Rule.Pedestrian, $"Small crossing {mode}/{direction}/clear={clear}: {run.Failure}");
                }
                for (int stage = 5; stage <= 7; stage++) foreach (bool clear in new[] {false, true})
                {
                    var run = new Journey(mode, new JourneySave { next = stage }, persistent: false, trafficSeed: 1);
                    var axis = stage == 5 ? Boulevard.Right(stage) : Boulevard.Forward(stage);
                    var crossing = run.BoulevardCrossingPoint;
                    var exit = Boulevard.Exit(stage);
                    Set(run, "Position", crossing - exit * 2);
                    Set(run, "Heading", Mathf.Atan2(exit.x, exit.y) * Mathf.Rad2Deg);
                    crossing = run.BoulevardCrossingPoint;
                    var pedestrian = crossing + axis * (clear ? 3.1f : 0);
                    var local = Boulevard.Local(stage, pedestrian);
                    float walk = ((stage == 5 ? local.x : local.y) + 12) / 1.85f;
                    Field(run, "boulevardTime", walk + (stage == 6 ? 2 : 14));
                    Field(run, "enteredJunction", true);
                    typeof(Journey).GetMethod("StepBigJunction", Private).Invoke(run, new object[] {0f, run.Nose});
                    Require(run.BoulevardPedestrian.HasValue, "Big crossing fixture keeps pedestrian visible");
                    Require(clear ? !run.Failure.HasValue : run.Failure == Rule.Pedestrian, $"Big crossing {mode}/{stage}/clear={clear}: {run.Failure}");
                }
                Require(PedestrianClearance.Touches(Vector2.zero, Vector2.zero, 90, mode), "Pedestrian contact remains a violation");
                var coach = new Journey(mode, new JourneySave { next = 2 }, persistent: false, trafficSeed: 1);
                bool crossedWithVisiblePedestrian = false;
                for (int i = 0; i < 4000 && coach.Stage == 2 && !coach.Failure.HasValue; i++)
                {
                    DrivingCoach.Step(coach, .02f);
                    crossedWithVisiblePedestrian |= coach.Front > RouteStage.Zebra - 2 && coach.Pedestrian.HasValue;
                }
                Require(!coach.Failure.HasValue && crossedWithVisiblePedestrian, "Coach resumes before pedestrian leaves the road: " + mode);
            }
            Debug.Log("Pedestrian checks passed: both directions, both vehicles, all junction movements and early coach release.");
        }
    }
}
