using System;
using UnityEngine;

namespace JalanKita.EditorTools
{
    public static class BoulevardChecks
    {
        static Journey Start(VehicleMode mode,int stage) => new(mode,new JourneySave { next=stage },persistent:false);
        static void Require(bool valid,string reason) { if(!valid) throw new InvalidOperationException(reason); }
        public static void Run(VehicleMode mode)
        {
            for(int stage=5;stage<10;stage++)
            {
                var run=Start(mode,stage);
                var checkpoint=RouteStage.All[stage-1];
                Require(run.Position==checkpoint.Checkpoint && run.Heading==checkpoint.Heading,"Checkpoint pose including heading");
                for(int i=0;i<12000 && run.Stage==stage && !run.Failure.HasValue;i++) DrivingCoach.Step(run,.02f);
                Require(!run.Failure.HasValue && run.Stage==stage+1,$"Extended retry {mode} stage {stage} at {run.Position} heading {run.Heading}: {run.Failure}");
            }
            // Straight through a turn lane cannot complete the junction.
            var wrong=Start(mode,5);
            for(int i=0;i<1500 && !wrong.Failure.HasValue;i++) wrong.Step(.02f,true,false,0);
            Require(wrong.Failure==Rule.MovementLane,"Wrong approach lane must fail");
            // Rail warnings begin from the approach, so racing the crossing is not a bypass.
            var rail=Start(mode,8);
            for(int i=0;i<1800 && !rail.Failure.HasValue;i++) rail.Step(.02f,rail.Speed<10,rail.Speed>10.2f,0);
            Require(rail.Failure==Rule.Railway,"Unsafe railway approach must fail");
            rail.Retry();
            Require(!rail.RailWarning && !rail.TrainVisible && rail.Position==RouteStage.All[7].Checkpoint,"Rail retry resets schedule and location");
            // Staying in the ambulance's lane must fail, even when the player stops.
            var emergency=Start(mode,9);
            for(int i=0;i<4000 && !emergency.Failure.HasValue;i++)
            {
                float desired=emergency.Position.y<928?8:0;
                emergency.Step(.02f,emergency.Speed<desired-.1f,emergency.Speed>desired+.1f,0);
            }
            Require(emergency.Failure==Rule.Emergency,"Blocked ambulance must fail");
            emergency.Retry();
            Require(!emergency.Ambulance.HasValue && !emergency.AmbulancePassed,"Ambulance retry resets encounter");
            VerifyJunctionViolation(mode,6,Rule.TurnSignal);
            VerifyJunctionViolation(mode,7,Rule.RightArrow);
            VerifyJunctionViolation(mode,5,Rule.Pedestrian);
            VerifyJunctionViolation(mode,5,mode==VehicleMode.Car?Rule.StopBox:Rule.Signal);
            VerifySaveUpgrade(mode);
        }

        static void VerifyJunctionViolation(VehicleMode mode,int stage,Rule expected)
        {
            var run=Start(mode,stage);
            bool injected=false;
            for(int i=0;i<8000 && !run.Failure.HasValue && run.Stage==stage;i++)
            {
                float local=Boulevard.Local(stage,run.Nose).y;
                bool ready=local>run.JunctionStopLine-2.5f;
                bool target=expected==Rule.TurnSignal ? run.MovementGreen && !run.BoulevardPedestrian.HasValue
                    : expected==Rule.Pedestrian ? run.MovementGreen && run.BoulevardPedestrian.HasValue
                    : expected==Rule.RightArrow ? run.ArrowPhase==0 : !run.MovementGreen;
                if(ready && target || injected)
                {
                    injected=true;
                    if(expected==Rule.TurnSignal && run.Indicator!=0) run.ToggleIndicator(run.Indicator);
                    // Drive the chosen lane straight across its stop line; turning is checked at entry.
                    run.Step(.02f,run.Speed<4,run.Speed>4.2f,0);
                }
                else if (ready) run.Step(.02f,false,true,0);
                else DrivingCoach.Step(run,.02f);
            }
            Require(run.Failure==expected,$"{mode}: expected {expected}, got {run.Failure}, stage {run.Stage}, {run.Position}, clock {run.CrossingClock}");
        }

        static void VerifySaveUpgrade(VehicleMode mode)
        {
            string key=JourneySave.LegacyKey(mode), current=JourneySave.Key(mode), currentBackup=PlayerPrefs.GetString(current);
            bool hadCurrent=PlayerPrefs.HasKey(current);
            string backup=PlayerPrefs.GetString(key);
            bool existed=PlayerPrefs.HasKey(key);
            try
            {
                PlayerPrefs.DeleteKey(current);
                var old=new JourneySave { next=5, before=3, after=4, mistakes=new int[16] };
                old.mistakes[3]=2;
                PlayerPrefs.SetString(key,JsonUtility.ToJson(old));
                var upgraded=JourneySave.Load(mode);
                Require(upgraded!=null && upgraded.next==5 && upgraded.before==3 && upgraded.after==-1
                    && upgraded.mistakes.Length==Lesson.All.Length && upgraded.mistakes[3]==2,"Old completed save must resume new lessons without losing mistakes");
            }
            finally { if(hadCurrent) PlayerPrefs.SetString(current,currentBackup); else PlayerPrefs.DeleteKey(current); if(existed) PlayerPrefs.SetString(key,backup); else PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
        }
    }
}
