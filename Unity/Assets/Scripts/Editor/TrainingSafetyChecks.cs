using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace JalanKita.EditorTools
{
    public static class TrainingSafetyChecks
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static void Require(bool ok, string message)
        { if (!ok) throw new InvalidOperationException("TRAINING_SAFETY: " + message); }
        static void Pose(Journey run, Vector2 position, float heading, float speed)
        {
            typeof(Journey).GetProperty("Position").SetValue(run, position);
            typeof(Journey).GetProperty("Heading").SetValue(run, heading);
            typeof(Journey).GetProperty("Speed").SetValue(run, speed);
        }
        static Journey New(VehicleMode mode, int stage) => new(mode, new JourneySave { next = stage }, false, trafficSeed: 1);
        public static void Run()
        {
            DriveSafetyChecks.Run();
            foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            {
                var wrong = New(mode, 4);
                Pose(wrong, new Vector2(-2, 304), 180, 4);
                for (int i = 0; i < 45 && !wrong.Failure.HasValue; i++) wrong.Step(.02f, false, false, 0);
                Require(wrong.Failure == Rule.KeepLeft, "Wrong heading on correct village lane fails " + mode);
                int retainedMistakes = wrong.MistakeCount, retainedVariant = wrong.Save.variant;
                wrong.ContinueAfterFailure();
                Require(!wrong.Failure.HasValue && wrong.MistakeCount == retainedMistakes
                    && wrong.Save.variant == retainedVariant && wrong.Speed == 0,
                    "Safe continuation preserves counted violations and rating inputs " + mode);
                wrong.Retry();
                Require(!wrong.Safety.Failure.HasValue && wrong.Safety.WrongWayDuration == 0 && wrong.Speed == 0,
                    "Retry resets safety without changing seeded encounter " + mode);

                var reverse = New(mode, 4);
                Pose(reverse, new Vector2(-2, 305), 0, 0);
                for (int i = 0; i < 80; i++) reverse.Step(.02f, false, false, 0, true);
                Require(reverse.Speed < 0 && !reverse.Failure.HasValue, "Deliberate low reverse remains lawful " + mode);
                var paused = reverse.Position; float clock = reverse.ElapsedTime, speed = reverse.Speed;
                reverse.Step(0, true, false, 1);
                Require(reverse.Position == paused && reverse.ElapsedTime == clock && reverse.Speed == speed,
                    "Zero time freezes simulation and safety " + mode);

                var road = New(mode, 5);
                Pose(road, Boulevard.World(5, -5.4f, -60), 180, 4);
                for (int i = 0; i < 45 && !road.Failure.HasValue; i++) road.Step(.02f, false, false, 0);
                Require(road.Failure == Rule.KeepLeft, "Boulevard wrong heading fails " + mode);

                var speedRun = New(mode, 4);
                Pose(speedRun, new Vector2(-2, RouteStage.SchoolLimit + 1), 0, 11);
                for (int i = 0; i < 25; i++) speedRun.Step(.02f, true, false, 0);
                Require(speedRun.Safety.SpeedWarning && !speedRun.Failure.HasValue && speedRun.Kph > 30
                    && speedRun.Save.mistakes[(int)Rule.Speed] == 0, "Posted30 is warning only " + mode);
                var fastBoulevard = New(mode, 8);
                Pose(fastBoulevard, new Vector2(-185.4f, 715), 0, 15);
                for (int i = 0; i < 25; i++) fastBoulevard.Step(.02f, true, false, 0);
                Require(fastBoulevard.Safety.SpeedWarning && !fastBoulevard.Failure.HasValue
                    && fastBoulevard.Save.mistakes[(int)Rule.BoulevardSpeed] == 0, "Posted50 is warning only " + mode);

                var group = New(mode, 2);
                group.Step(.02f, false, true, 0);
                Require(group.PedestrianSnapshots.Count == 3, "All three rendered crossing people have snapshots " + mode);
                for (int i = 0; i < Journey.CrossingPedestrianCount; i++)
                    Require(group.PedestrianSnapshots[i].Position == group.CrossingPedestrian(i).Value,
                        "Crossing draw and contact positions agree " + mode + "/" + i);

                var market = new Journey(mode, new JourneySave { next = 3, checkpoint = 1 }, false,
                    mission: Mission.Scenarios[2], trafficSeed: 1);
                Pose(market, new Vector2(-5.8f, Mission.PickupPoint), 0, 0);
                market.Step(.02f, false, true, 0);
                Require(market.PedestrianSnapshots.Count == 8 && !market.Failure.HasValue,
                    "All seven market neighbours plus waiting passenger are physical " + mode);
                var waitingContact = new Journey(mode, new JourneySave { next = 3, checkpoint = 1 }, false,
                    mission: Mission.Scenarios[2], trafficSeed: 1);
                // The upright motorcycle has a real 3 cm clearance here; angle its body
                // toward the passenger to test contact without enlarging either footprint.
                Pose(waitingContact, new Vector2(-7.44f, Mission.PickupPoint), mode==VehicleMode.Motorcycle?45:0, 0);
                waitingContact.Step(.02f, false, true, 0);
                Require(waitingContact.Failure == Rule.Pedestrian,
                    "Actual market adapter catches waiting passenger body contact " + mode);
                market.Pickup();
                Require(market.IsBoarding, "Valid pickup begins boarding " + mode);
                market.Step(.02f, false, true, 0);
                Require(market.PedestrianSnapshots.Count == 7 && !market.Failure.HasValue,
                    "Only intentional boarding passenger excluded " + mode);

                // Relative sweep must catch a person crossing the complete body between frames,
                // while a nearby parallel walker remains safe; neither depends on proximity slowdown.
                var crossing = new[] { new DrivePedestrian(new Vector2(-3, 0), new Vector2(3, 0)) };
                Require(DriveSafetyState.PedestrianContact(Vector2.zero, Vector2.zero, 90, 90, mode, crossing),
                    "Moving person contacts stationary full body " + mode);
                var rear = new[] { new DrivePedestrian(new Vector2(0, -3), new Vector2(0, -3)) };
                Require(DriveSafetyState.PedestrianContact(Vector2.zero, new Vector2(0, -4), 0, 0, mode, rear),
                    "Rear swept contact while reversing " + mode);
                var safe = new[] { new DrivePedestrian(new Vector2(3, 0), new Vector2(3, 1)) };
                Require(!DriveSafetyState.PedestrianContact(Vector2.zero, Vector2.up, 0, 0, mode, safe),
                    "Parallel non-contact remains safe " + mode);
            }
            var paths = new List<DrivePedestrian>();
            foreach (var mission in Mission.Scenarios)
            {
                var run = new Journey(VehicleMode.Car, mission.NewSave(), false, mission, trafficSeed: 1);
                paths.Clear(); TrainingPedestrianPaths.Append(run, 2, 2, paths);
                foreach (var person in paths) Require(person.PreviousPosition == person.Position, "Paused ambient path is stable");
            }
            // Full-route coach covers lawful left/right junction transitions and true actor/priority hazards.
            Debug.Log("TRAINING_SAFETY_CHECKS_PASSED wrong-heading/retry, pause, reverse, 30/50 warning-only, grouped/ambient snapshots and boarding exemption");
        }
    }
}
