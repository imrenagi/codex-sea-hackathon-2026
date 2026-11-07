using System;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JalanKita.EditorTools
{
    public static class ReverseMotionChecks
    {
        static void Require(bool ok,string message)
        {if(!ok)throw new InvalidOperationException("REVERSE: "+message);}
        static void Set(object target,string property,object value)=>target.GetType().GetProperty(property).SetValue(target,value);

        public static void Run()
        {
            using var preferences=new IntegrationChecks.SavedPreferences();
            RunMath();
            foreach(VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            {
                foreach(var mission in Mission.Tests.Concat(Mission.Scenarios))
                    CheckJourney(new Journey(mode,mission.NewSave(),false,mission,173));
                for(int stage=1;stage<10;stage++)
                    CheckJourney(new Journey(mode,new JourneySave{next=stage},false,trafficSeed:173));
                CheckRules(mode);
                CheckFiniteTraining(mode);
            }
            CheckExploration();
            Debug.Log("REVERSE_CONTROLLERS_PASSED all seven models, both steering controllers, eight activities, nine road checkpoints, stationary rules, boarding, finite boundaries and rear traffic.");
        }

        public static void RunMath()
        {
            foreach(VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            {
                var motion=new RoadVehicleMotion();float speed=4,heading=0;
                bool stopped=false,reversed=false;
                for(int i=0;i<220;i++)
                {
                    motion.Step(.02f,mode,false,false,0,12,ref speed,ref heading,true);
                    if(speed==0)stopped=true;
                    if(speed<0){Require(stopped,"Direction reversed before stopping");reversed=true;}
                    Require(speed>=-RoadVehicleMotion.ReverseLimit(mode)-.001f,"Reverse limit");
                }
                Require(reversed&&speed<-.5f,"Reverse request did not engage");
                stopped=false;bool forward=false;
                for(int i=0;i<150;i++)
                {
                    motion.Step(.02f,mode,true,false,0,12,ref speed,ref heading);
                    if(speed==0)stopped=true;
                    if(speed>0){Require(stopped,"Forward engaged before reverse stopped");forward=true;}
                }
                Require(forward,"Forward request cannot leave reverse");
                for(int i=0;i<250;i++)
                {motion.Step(.02f,mode,false,true,1,12,ref speed,ref heading);Require(speed>=0,"Brake-only entered reverse");}
                float stoppedHeading=heading;
                for(int i=0;i<100;i++)motion.Step(.02f,mode,false,true,-1,12,ref speed,ref heading,true);
                Require(speed==0&&Mathf.Abs(Mathf.DeltaAngle(stoppedHeading,heading))<.001f,"Space does not dominate reverse / stationary pivot");
                motion.Reset();speed=-1;heading=0;
                for(int i=0;i<20;i++)motion.Step(.02f,mode,false,false,1,12,ref speed,ref heading,true);
                Require(heading<0,"Reverse steering yaw must invert");
                float width=TrafficFootprint.PlayerHalfWidth(mode),length=TrafficFootprint.PlayerHalfLength(mode);
                Require(TrafficFootprint.SweptOverlaps(new Vector2(0,8),0,new Vector2(0,-8),0,width,length,Vector2.zero,0,.9f,2),"Rear swept collision missed");
                float corner=mode==VehicleMode.Car?1.3f:.6f;
                var obstacle=new Vector2(corner,corner);
                Require(!TrafficFootprint.Overlaps(Vector2.zero,0,width,length,obstacle,0,.1f,.1f)
                    &&!TrafficFootprint.Overlaps(Vector2.zero,90,width,length,obstacle,0,.1f,.1f)
                    &&TrafficFootprint.SweptOverlaps(Vector2.zero,0,Vector2.zero,90,width,length,obstacle,0,.1f,.1f),"Rotating side sweep missed between endpoints");
                foreach(int side in new[]{-1,1})
                {
                    RoadBoundary.Frame Road(Vector2 p)=>new(new Vector2(.3f*p.y,p.y),new Vector2(.3f,1),3.25f/Mathf.Sqrt(1.09f));
                    var frame=Road(new Vector2(9,30));heading=frame.Heading-side*65;speed=0;motion.Reset();
                    var position=RoadBoundary.Fit(Road,mode,new Vector2(9,30)+frame.Right*side*3,heading);
                    var initial=position;
                    for(int i=0;i<300;i++)
                    {
                        motion.Step(.02f,mode,false,false,i<100?side:-side*.35f,12,ref speed,ref heading,true);
                        position=RoadBoundary.Move(Road,mode,position,.02f,ref speed,ref heading,out _);
                        Require(RoadBoundary.Contains(Road,mode,position,heading),"Reverse edge footprint escaped");
                    }
                    Require(Vector2.Distance(initial,position)>3&&speed<-.5f,"Reverse edge stuck");
                }
            }
            Debug.Log("REVERSE_MATH_PASSED stop-before-direction-change, brake-only, signed steering, reverse edge recovery and full rear/side sweeps.");
        }

        static void CheckJourney(Journey run)
        {
            if(run.Stage==0){run.DriverGear=run.PassengerGear=run.Headlight=true;run.Depart();if(run.Finished)return;}
            var start=run.Position;
            for(int i=0;i<75;i++)run.Step(.02f,false,false,0,true);
            Require(!run.Failure.HasValue&&run.Speed<-.5f&&Vector2.Distance(start,run.Position)>.5f,
                "Journey reverse "+run.Mode+"/"+(run.Activity?.Id??"route")+"/"+run.Stage+": "+run.Failure);
            for(int i=0;i<100;i++)run.Step(.02f,false,true,0);
            var stopped=run.Position;float heading=run.Heading;
            for(int i=0;i<100;i++)run.Step(.02f,false,true,1);
            Require(run.Speed==0&&run.Position==stopped&&run.Heading==heading,"Coach brake semantics changed");
            run.Retry();Require(run.Speed==0&&run.Position==start,"Reverse retry did not restore spawn");
        }

        static void CheckRules(VehicleMode mode)
        {
            var market=new Journey(mode,Mission.Scenarios[2].NewSave(),false,Mission.Scenarios[2],173);
            Set(market,"Position",new Vector2(-5.8f,Mission.PickupPoint));Set(market,"Speed",-1f);
            Require(!market.CanPickup,"Reverse counted as stopped for pickup");
            market.Pickup();Require(market.Failure==Rule.PickupPlace,"Moving reverse pickup accepted");
            var school=new Journey(mode,Mission.Scenarios[1].NewSave(),false,Mission.Scenarios[1],173);
            Set(school,"Position",new Vector2(-5.8f,340));Set(school,"Speed",-1f);Set(school,"PhoneRinging",true);
            school.ResolvePhone(true);Require(school.Failure==Rule.Phone,"Moving reverse phone answer accepted");
            var stop=new Journey(mode,new JourneySave{next=1},false,trafficSeed:173);
            Set(stop,"Position",new Vector2(RouteStage.LeftLane,27.5f));Set(stop,"Speed",-1f);
            stop.Step(.02f,false,false,0,true);Require(!stop.FullStop,"Reverse counted as full STOP");
            market=new Journey(mode,Mission.Scenarios[2].NewSave(),false,Mission.Scenarios[2],173);
            Set(market,"Position",new Vector2(-5.8f,Mission.PickupPoint));market.Pickup();
            var waiting=market.Position;
            for(int i=0;i<80;i++)market.Step(.02f,false,false,1,true);
            Require(market.IsBoarding&&market.Position==waiting&&market.Speed==0,"Reverse moved vehicle during boarding");
        }

        static void CheckFiniteTraining(VehicleMode mode)
        {
            var run=new Journey(mode,new JourneySave{next=1},false,trafficSeed:173);
            for(int i=0;i<1200&&!run.Failure.HasValue;i++)run.Step(.02f,false,false,0,true);
            Require(run.Failure==Rule.OffRoad&&run.Position.y-RoadBoundary.HalfLength(mode)>=-28,"Reverse escaped village's rendered start");
            run.Retry();Require(!run.Failure.HasValue&&run.Speed==0&&run.Position.y==-12,"Finite-road retry");
            run=new Journey(mode,new JourneySave{next=7},false,trafficSeed:173);
            Set(run,"Position",new Vector2(81,631));Set(run,"Heading",270f);
            for(int i=0;i<300&&!run.Failure.HasValue;i++)run.Step(.02f,false,false,0,true);
            Require(run.Failure==Rule.OffRoad&&run.Position.x+RoadBoundary.HalfLength(mode)<=85,"Reverse escaped boulevard's finite end");
        }

        static void CheckExploration()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var drive=new JaksaDrive(new GameObject("Reverse exploration verification").transform);
            foreach(var choice in VehicleCatalogue.All)
            {
                VehicleCatalogue.Select(choice);drive.Reset(choice.Mode);
                var initial=drive.Position;
                for(int i=0;i<100;i++)drive.Step(.02f,false,false,0,true);
                Require(drive.Speed<-.5f&&drive.Position.y<initial.y-.5f,"Reverse from exploration spawn "+choice.Id);
                for(int i=0;i<100;i++)drive.Step(.02f,false,true,1);
                var held=drive.Position;float yaw=drive.Heading;
                for(int i=0;i<100;i++)drive.Step(.02f,false,true,-1,true);
                Require(drive.Position==held&&drive.Heading==yaw&&drive.Speed==0,"Exploration Space must hold pose "+choice.Id);
                drive.Reset(choice.Mode);
                for(int i=0;i<drive.Traffic.Actors.Length;i++)
                {
                    var a=drive.Traffic.Actors[i];a.Z=i==0?0:70+i*20;a.Speed=a.DesiredSpeed=0;a.Direction=1;
                }
                for(int i=0;i<400;i++)
                {
                    drive.Step(.02f,false,false,0,true);
                    foreach(var a in drive.Traffic.Actors)
                        Require(!TrafficFootprint.Overlaps(drive.Position,drive.Heading,TrafficFootprint.PlayerHalfWidth(choice.Mode),TrafficFootprint.PlayerHalfLength(choice.Mode),
                            a.Position,a.Heading,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,0),"Reverse overlaps NPC "+choice.Id);
                }
                Require(drive.TrafficContact&&Mathf.Abs(drive.Speed)<.1f,"Rear NPC did not block "+choice.Id);
                float blocked=drive.Position.y;
                for(int i=0;i<150;i++)drive.Step(.02f,true,false,0);
                Require(drive.Position.y>blocked+3,"Cannot drive away from rear obstacle "+choice.Id);
            }
        }
    }
}
