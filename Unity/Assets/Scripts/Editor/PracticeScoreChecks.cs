using System;
using UnityEditor;
namespace JalanKita.EditorTools
{
    public static class PracticeScoreChecks
    {
        [MenuItem("RoadSense/Verify Practice Ratings")]
        public static void Run()
        {
            var counts=new int[Lesson.All.Length];
            Require(PracticeRating.Stars(false,counts)==0,"Incomplete earns zero");
            Require(PracticeRating.Stars(true,counts)==3,"Clean completion earns three");
            counts[(int)Rule.ReturnLeft]=1;
            Require(PracticeRating.Stars(true,counts)==2,"Exercise-only correction earns two");
            counts[(int)Rule.Signal]=1;
            Require(PracticeRating.Stars(true,counts)==1,"Safety violation earns one");
            var baseline=(int[])counts.Clone();
            Require(PracticeRating.Total(PracticeRating.Since(counts,baseline))==0,"Fresh attempt baseline");
            counts[(int)Rule.Phone]++;
            Require(PracticeRating.Total(PracticeRating.Since(counts,baseline))==1,"Later violation counted");
            var save=new JourneySave();
            var run=new Journey(VehicleMode.Car,save,persistent:false,trafficSeed:1);
            Require(!run.Depart() && run.Failure==Rule.SeatBelt,"Preflight violation");
            run.ContinueAfterFailure();
            Require(!run.Failure.HasValue && run.MistakeCount==1 && run.Stage==0,"Continue retains mistake and safely resets");
            run.DriverGear=run.PassengerGear=true;
            Require(run.Depart() && run.Stage==1,"Can complete after recovering");
            UnityEngine.Debug.Log("Practice rating checks passed (no persistent writes).");
        }
        static void Require(bool condition,string label)
        { if(!condition) throw new InvalidOperationException(label); }
    }
}
