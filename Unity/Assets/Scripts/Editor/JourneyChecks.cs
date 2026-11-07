using System;
using UnityEditor;
using UnityEngine;

namespace JalanKita.EditorTools
{
    public static class JourneyChecks
    {
        [MenuItem("Jalan Kita/Verify Checkpoints")]
        public static void Run()
        {
            SteeringChecks.Run();
            SoundChecks.Run();
            foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            {
                VerifyContinuousRoute(mode, 0);
                VerifyContinuousRoute(mode, 1);
                VerifyRetries(mode);
                BoulevardChecks.Run(mode);
            }
            Debug.Log("Jalan Kita checks passed for both vehicles, both traffic variants, all ten checkpoints, and checkpoint retry/persistence.");
        }

        static void VerifyContinuousRoute(VehicleMode mode, int variant)
        {
            var save = new JourneySave();
            save.mistakes[(int)Rule.Pedestrian] = variant;
            save.mistakes[(int)Rule.Junction] = variant;
            save.mistakes[(int)Rule.Railway] = variant;
            var drive = new Journey(mode, save, persistent: false);
            drive.DriverGear = drive.PassengerGear = drive.Headlight = true;
            Require(drive.Depart(), "Preflight");
            for (int i = 0; i < 30000 && !drive.Finished; i++)
            {
                var previous = drive.Position;
                int stage = drive.Stage;
                DrivingCoach.Step(drive, .02f);
                Require(!drive.Failure.HasValue, $"Coach {mode}, variant {variant}, stage {drive.Stage}, at {drive.Position}: {drive.Failure}");
                if (drive.Stage != stage)
                    Require(Vector2.Distance(previous, drive.Position) < .5f && drive.Speed > 0,
                        $"Checkpoint {drive.Stage} did not preserve motion");
            }
            Require(drive.Finished, $"Route stuck: {mode}, variant {variant}, stage {drive.Stage}, at {drive.Position}");
        }

        static void VerifyRetries(VehicleMode mode)
        {
            string key = JourneySave.Key(mode);
            bool existed = PlayerPrefs.HasKey(key);
            string backup = PlayerPrefs.GetString(key);
            try
            {
                var drive = new Journey(mode, new JourneySave());
                Require(!drive.Depart() && drive.Failure == (mode == VehicleMode.Car ? Rule.SeatBelt : Rule.Helmet),
                    "Missing gear should stop departure");
                drive.Retry();
                drive.DriverGear = drive.PassengerGear = true;
                if (mode == VehicleMode.Motorcycle)
                {
                    Require(!drive.Depart() && drive.Failure == Rule.Headlight, "Motorcycle headlight");
                    drive.Retry();
                    drive.DriverGear = drive.PassengerGear = drive.Headlight = true;
                }
                Require(drive.Depart(), "Departure with gear");
                Require(JourneySave.Load(mode)?.next == 1, "Departure checkpoint persisted");
                DriveUntilFailure(drive, Rule.Stop);
                drive.Retry();
                Require(drive.Stage == 1 && drive.Position.y == RouteStage.All[0].End && drive.Speed == 0,
                    "STOP retry should return to the departure checkpoint");
                Require(JourneySave.Load(mode)?.mistakes[(int)Rule.Stop] == 1, "STOP mistake persisted");
                for (int i = 0; i < 1600 && !drive.Failure.HasValue && drive.Position.y < 51; i++)
                    DrivingCoach.Step(drive, .02f);
                DriveUntilFailure(drive, Rule.Signal);

                drive = new Journey(mode, new JourneySave { next = 2 }, persistent: false);
                DriveUntilFailure(drive, Rule.Pedestrian);
                drive.Retry();
                Require(drive.Position.y == RouteStage.All[1].End && drive.Stage == 2, "Zebra retry preserves checkpoint 02");

                drive = new Journey(mode, new JourneySave { next = 3 }, persistent: false);
                DriveUntilFailure(drive, Rule.ReturnLeft);
                drive.Retry();
                for (int i = 0; i < 500 && !drive.Failure.HasValue; i++)
                    drive.Step(.02f, true, false, drive.Position.y >= RouteStage.OneWayStart + 1 ? 1 : 0);
                Require(drive.Failure == Rule.Indicator, "Detect lane change without indicator");

                drive = new Journey(mode, new JourneySave { next = 4 }, persistent: false);
                DriveUntilFailure(drive, Rule.Speed);
                drive.Retry();
                for (int i = 0; i < 500 && !drive.PhoneRinging; i++)
                    DrivingCoach.Step(drive, .02f);
                Require(drive.PhoneRinging, "Phone should ring in the school zone");
                drive.ResolvePhone(true);
                Require(drive.Failure == Rule.Phone, "Answering while driving should trigger feedback");
                drive.Retry();
                Require(drive.Position.y == RouteStage.All[3].End && !drive.PhoneRinging, "Retry should reset the school-zone events");
                VerifySafeCall(drive);
            }
            finally
            {
                if (existed) PlayerPrefs.SetString(key, backup);
                else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        static void VerifySafeCall(Journey run)
        {
            for (int i = 0; i < 700 && run.Position.y < RouteStage.PhoneStart + 4.2f; i++)
                Cruise(run, 0);
            for (int i = 0; i < 200 && run.Position.x > -5.3f; i++)
                Cruise(run, VehicleSteering.Pursue(run, new Vector2(-6, run.Position.y + 6)));
            for (int i = 0; i < 150 && run.Speed > 0; i++)
                run.Step(.02f, false, true, VehicleSteering.Pursue(run, new Vector2(-6, run.Position.y + 4)));
            Require(run.PhoneRinging && !run.Failure.HasValue, $"Stopped in the bay with phone ringing: {run.Mode}, {run.Position}, {run.Failure}");
            run.ResolvePhone(true);
            Require(!run.PhoneRinging && !run.Failure.HasValue, "Answering while stopped in the bay should pass");
        }

        static void Cruise(Journey run, float steer) =>
            run.Step(.02f, run.Speed < 7.4f, run.Speed > 7.7f, steer);

        static void DriveUntilFailure(Journey run, Rule expected)
        {
            for (int i = 0; i < 1800 && !run.Failure.HasValue && !run.Finished; i++)
                run.Step(.02f, true, false, 0);
            Require(run.Failure == expected, $"Expected {expected}, saw {run.Failure} at {run.Position}");
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
