using System;
using UnityEngine;

namespace JalanKita.EditorTools
{
    public static class BoulevardTrafficChecks
    {
        public static void Run()
        {
            foreach(VehicleMode mode in Enum.GetValues(typeof(VehicleMode))) for(int seed=1;seed<=12;seed++)
            {
                var run=new Journey(mode,new JourneySave { next=5 },persistent:false,trafficSeed:seed);
                var traffic=run.CityTraffic;
                bool car=false,motorcycle=false,moving=false;
                foreach(var n in traffic.Vehicles) { car|=!n.Motorcycle; motorcycle|=n.Motorcycle; }
                Require(car&&motorcycle,"Mixed car and motorcycle traffic");
                for(int frame=0;frame<30000 && !run.Finished && !run.Failure.HasValue;frame++)
                {
                    DrivingCoach.Step(run,.02f);
                    if(frame%100==0)
                    {
                        foreach(var n in traffic.Vehicles) moving|=n.Speed>1;
                        Require(!traffic.Collides(run),$"Traffic overlaps player: {mode}, seed {seed}, stage {run.Stage}, at {run.Position}");
                    }
                }
                Require(!run.Failure.HasValue && run.Finished,$"Traffic route: {mode}, seed {seed}, stage {run.Stage}, {run.Position}, failure {run.Failure}");
                Require(ReferenceEquals(traffic,run.CityTraffic)&&moving,"Traffic survives checkpoints and moves");
            }
            VerifyFollowing();
            Debug.Log("Traffic checks passed: 12 seeds per vehicle, all boulevard/rail/ambulance checkpoints, following and collision detection.");
        }
        static void VerifyFollowing()
        {
            var run=new Journey(VehicleMode.Car,new JourneySave { next=5 },persistent:false);
            var traffic=run.CityTraffic;
            var n=traffic.Vehicles[0];
            n.Position=run.Position-Vector2.up*12; n.Direction=Vector2.up; n.Speed=10;
            for(int i=0;i<200;i++) traffic.Step(.02f,run);
            Require(!traffic.Collides(run) && n.Speed<.1f,"NPC brakes behind a stationary player");
            n.Position=run.Position;
            Require(traffic.Collides(run),"Vehicle contact is detected");
            run.Step(.02f,false,false,0);
            Require(run.Failure==Rule.Collision,"Player contact triggers collision feedback");
            run.Retry();
            Require(!ReferenceEquals(traffic,run.CityTraffic) && !run.CityTraffic.Collides(run),"Retry reseeds traffic clear of checkpoint");
        }
        static void Require(bool valid,string reason) { if(!valid) throw new InvalidOperationException(reason); }
    }
}
