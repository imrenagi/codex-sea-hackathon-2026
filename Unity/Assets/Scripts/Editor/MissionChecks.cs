using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JalanKita.EditorTools
{
    public static class MissionChecks
    {
        public static void VerifyAndBuildMac()
        {
            Run();
            ProjectSetup.BuildMac();
        }

        [MenuItem("Jalan Kita/Verify Missions")]
        public static void Run()
        {
            JourneyChecks.Run();
            foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            {
                foreach (var mission in Mission.Tests.Concat(Mission.Scenarios))
                    for (int variant = 0; variant < 2; variant++) VerifyCompletion(mode, mission, variant);
                VerifySchoolRetry(mode);
                VerifyMarketRetry(mode);
                VerifyBoarding(mode);
                VerifyMarketMistakes(mode);
                VerifySaveIsolation(mode);
            }
            Debug.Log("Mission checks passed: all 8 activities, both vehicles, both traffic variants, school/market failures, encounter retries, save isolation and legacy save migration.");
        }

        static Journey Create(VehicleMode mode, Mission mission, int variant = 0)
        {
            var save = mission.NewSave();
            save.variant = variant;
            return new Journey(mode, save, persistent: false, mission: mission);
        }

        static void VerifyCompletion(VehicleMode mode, Mission mission, int variant)
        {
            var run = Create(mode, mission, variant);
            if (run.Stage == 0)
            {
                run.DriverGear = run.PassengerGear = run.Headlight = true;
                Require(run.Depart(), "Prepared departure");
            }
            for (int i = 0; i < 9000 && !run.Finished && !run.Failure.HasValue; i++) DrivingCoach.Step(run, .02f);
            Require(run.Finished && !run.Failure.HasValue,
                $"{mission.Id}/{mode}/{variant}: {run.Failure} at {run.Position}, checkpoint {run.Save.checkpoint}, passenger {run.PassengerPickedUp}");
            Require(run.Save.next == mission.LastStage + 1 && run.Save.completions == 1 && run.Save.bestMistakes == 0,
                "Bounded completion and best result");
            var position = run.Position;
            run.Step(.02f, true, false, 0);
            Require(run.Position == position && run.Save.completions == 1, "Completed missions must not continue or score twice");
        }

        static void VerifySchoolRetry(VehicleMode mode)
        {
            var run = Create(mode, Mission.Scenarios[1]);
            for (int i = 0; i < 2000 && !run.PhoneRinging && !run.Failure.HasValue; i++) DrivingCoach.Step(run, .02f);
            Require(run.PhoneRinging && run.Save.checkpoint >= 1, "School scenario should reach phone after crossing");
            // Continue to the second checkpoint without answering the phone.
            for (int i = 0; i < 300 && run.Save.checkpoint < 2; i++) run.Step(.02f, run.Speed < 7, false, 0);
            run.ResolvePhone(true);
            Require(run.Failure == Rule.Phone, "School phone while moving");
            run.Retry();
            Require(run.Position.y == Mission.SchoolPhoneCheckpoint && run.Save.checkpoint == 2 && run.Save.attempts == 2,
                "School retry should retain the completed crossing");
            for (int i = 0; i < 2000 && !run.Finished && !run.Failure.HasValue; i++) DrivingCoach.Step(run, .02f);
            Require(run.Finished && run.Save.bestMistakes == 1, "School retry can complete with learning history");

            run = Create(mode, Mission.Scenarios[1]);
            for (int i = 0; i < 1800 && !run.Failure.HasValue && !run.Finished; i++)
                run.Step(.02f, run.Speed < 7.7f, run.Speed > 7.8f, 0);
            Require(run.Failure == Rule.Pedestrian, "Ignoring the school crossing must fail");
            int previousDirection = run.PedestrianDirection;
            run.Retry();
            run.Step(.02f, false, true, 0);
            Require(run.PedestrianDirection == -previousDirection, "A crossing retry changes pedestrian direction");
            var focused = Create(mode, Mission.Tests[4]);
            for (int i = 0; i < 500; i++) DrivingCoach.Step(focused, .02f);
            Require(!focused.Pedestrian.HasValue, "School scenario must not add a pedestrian to the focused speed test");
        }

        static void VerifyMarketRetry(VehicleMode mode)
        {
            var run = Create(mode, Mission.Scenarios[2]);
            for (int i = 0; i < 4000 && !run.PassengerPickedUp && !run.Failure.HasValue; i++) DrivingCoach.Step(run, .02f);
            Require(run.PassengerPickedUp && run.Save.checkpoint == 2, "Market pickup checkpoint");
            run.Retry();
            Require(run.PassengerPickedUp && run.Position.x < -5 && run.Position.y == Mission.PickupPoint,
                "Retry retains the passenger and starts safely in the bay");
            for (int i = 0; i < 4000 && !run.Finished && !run.Failure.HasValue; i++) DrivingCoach.Step(run, .02f);
            Require(run.Finished && !run.Failure.HasValue, "Can rejoin traffic after checkpoint retry");
        }

        static void VerifyMarketMistakes(VehicleMode mode)
        {
            var mission = Mission.Scenarios[2];
            var run = Create(mode, mission);
            run.Pickup();
            Require(run.Failure == Rule.PickupPlace, "Cannot collect passenger from the starting position");
            run = Create(mode, mission);
            for (int i = 0; i < 3000 && !run.Failure.HasValue && !run.Finished; i++)
                run.Step(.02f, run.Speed < 5, run.Speed > 5.2f, 0);
            Require(run.Failure == Rule.PickupIncomplete, "Cannot finish without collecting passenger");
            run = Create(mode, mission);
            for (int i = 0; i < 2000 && run.Position.y < 233; i++) run.Step(.02f, run.Speed < 4, run.Speed > 4.1f, 0);
            for (int i = 0; i < 160 && !run.Failure.HasValue; i++) run.Step(.02f, false, true, 0);
            Require(run.Failure == Rule.NoStopping, "Cannot wait for pickup in the signed restriction");
            run = Create(mode, mission);
            for (int i = 0; i < 2000 && run.Position.y < 251; i++) run.Step(.02f, run.Speed < 4, run.Speed > 4.1f, 0);
            for (int i = 0; i < 200 && !run.Failure.HasValue; i++) run.Step(.02f, run.Speed < 4, false, -1);
            Require(run.Failure == Rule.Indicator, "Bay entry needs an indicator");
            var save = mission.NewSave();
            save.checkpoint = 2;
            run = new Journey(mode, save, persistent: false, mission: mission);
            for (int i = 0; i < 160; i++) run.Step(.02f, false, true, 0);
            run.ToggleIndicator(1);
            for (int i = 0; i < 200 && !run.Failure.HasValue; i++) run.Step(.02f, true, false, 1);
            Require(run.Failure == Rule.Gap, "Cannot pull out of the bay into approaching traffic");
        }

        static void VerifyBoarding(VehicleMode mode)
        {
            var run = Create(mode, Mission.Scenarios[2]);
            for (int i = 0; i < 4000 && !run.PassengerPickedUp && !run.Failure.HasValue; i++)
                DrivingCoach.Step(run, .02f);
            Require(run.IsBoarding && run.BoardingProgress < 1, "Pickup starts a visible boarding interval");
            var atPickup = run.Position;
            for (int i = 0; i < 45; i++) run.Step(.02f, true, false, 1);
            Require(run.Position == atPickup && run.Speed == 0 && run.IsBoarding,
                "Throttle and steering cannot move the vehicle during boarding");
            for (int i = 0; i < 80; i++) run.Step(.02f, false, true, 0);
            Require(!run.IsBoarding && run.BoardingProgress == 1 && !run.Failure.HasValue,
                "Boarding completes without changing the scored pickup");
            run.Retry();
            Require(run.PassengerPickedUp && !run.IsBoarding && run.BoardingProgress == 1,
                "A pickup checkpoint resumes with the passenger already aboard");
        }

        static void VerifySaveIsolation(VehicleMode mode)
        {
            var mission = Mission.Scenarios[1];
            string mainKey = JourneySave.LegacyKey(mode), currentKey = JourneySave.Key(mode), missionKey = JourneySave.Key(mode, mission);
            string oldCurrent = PlayerPrefs.GetString(currentKey, null);
            string oldMain = PlayerPrefs.GetString(mainKey, null), oldMission = PlayerPrefs.GetString(missionKey, null);
            try
            {
                PlayerPrefs.DeleteKey(currentKey);
                // A save from the original 17-rule build must survive appended mission rules.
                var legacy = new JourneySave { next = 3, mistakes = new int[17], before = 2 };
                legacy.mistakes[(int)Rule.Stop] = 2;
                PlayerPrefs.SetString(mainKey, JsonUtility.ToJson(legacy));
                string original = PlayerPrefs.GetString(mainKey);
                var loaded = JourneySave.Load(mode);
                Require(loaded.next == 3 && loaded.before == 2 && loaded.mistakes.Length == Lesson.All.Length
                    && loaded.mistakes[(int)Rule.Stop] == 2, "Legacy save migration");
                var save = mission.NewSave();
                save.checkpoint = 2;
                var run = new Journey(mode, save, mission: mission);
                run.Step(.02f, true, false, 0);
                run.ResolvePhone(true);
                save.Store(mode, mission);
                Require(PlayerPrefs.GetString(mainKey) == original, "Mission writes must not touch the continuous journey");
                var resumed = new Journey(mode, JourneySave.Load(mode, mission), persistent: false, mission: mission);
                Require(resumed.Stage == 4 && resumed.Position.y == Mission.SchoolPhoneCheckpoint, "Mission resume uses its own encounter checkpoint");
                run = new Journey(mode, Mission.Scenarios[0].NewSave(), mission: Mission.Scenarios[0], persistent: false);
                run.Depart();
                var retry = new Journey(mode, run.Save, mission: run.Activity, persistent: false);
                Require(retry.Save.attempts == 2 && !retry.Save.retryPending, "Resuming after failure counts a new attempt");
                Require(JourneySave.Key(mode, Mission.Tests[4]) != missionKey, "Tests and scenarios use different save keys");
                Require(JourneySave.Key(mode == VehicleMode.Car ? VehicleMode.Motorcycle : VehicleMode.Car, mission) != missionKey,
                    "Vehicle results must be independent");
            }
            finally
            {
                Restore(mainKey, oldMain); Restore(currentKey, oldCurrent); Restore(missionKey, oldMission); PlayerPrefs.Save();
            }
        }

        static void Restore(string key, string value)
        {
            if (value == null) PlayerPrefs.DeleteKey(key);
            else PlayerPrefs.SetString(key, value);
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
