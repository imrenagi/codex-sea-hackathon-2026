using System;
using System.Reflection;
using UnityEngine;
namespace JalanKita.EditorTools
{
    public static class ExploreSafetyChecks
    {
        static void Require(bool ok,string message){if(!ok)throw new Exception("EXPLORE_SAFETY: "+message);}
        static void Pose(object drive,Vector2 position,float heading,float speed)
        {
            var type=drive.GetType();type.GetProperty("Position").SetValue(drive,position);
            type.GetProperty("Heading").SetValue(drive,heading);type.GetProperty("Speed").SetValue(drive,speed);
        }
        public static void Run()
        {
            DriveSafetyChecks.Run();RoadBoundaryChecks.Run();
            foreach(VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            {
                float velocity=0;
                for(int i=0;i<1500;i++)velocity=RoadVehicleMotion.AdvanceSpeed(.02f,mode,true,false,false,RoadVehicleMotion.FreeSpeed(mode),velocity);
                Require(velocity*3.6f>55,"Free propulsion can exceed all posted limits: "+mode+" "+velocity*3.6f);
                float coast=30/3.6f,brake=coast;
                for(int i=0;i<25;i++){coast=RoadVehicleMotion.AdvanceSpeed(.02f,mode,false,false,false,float.PositiveInfinity,coast);brake=RoadVehicleMotion.AdvanceSpeed(.02f,mode,false,true,false,float.PositiveInfinity,brake);}
                Require(coast*3.6f>28 && brake*3.6f<16,"Coast uses resistance, Space uses actual braking "+mode);
                foreach(int side in new[]{-1,1})foreach(float angle in new[]{5f,15f})foreach(float kph in new[]{10f,20f,30f})
                {
                    RoadBoundary.Frame Frame(Vector2 p)=>new(new Vector2(0,p.y),Vector2.up,3.25f);
                    float heading=side*angle,speed=kph/3.6f;
                    var p=RoadBoundary.Fit(Frame,mode,new Vector2(side*3,0),heading);
                    float before=speed,yaw=heading;
                    p=RoadBoundary.Move(Frame,mode,p,.02f,ref speed,ref heading,out bool contact);
                    Require(contact&&RoadBoundary.Contains(Frame,mode,p,heading),"Scrape contained");
                    Require(before-speed<.1f&&Mathf.Abs(Mathf.DeltaAngle(yaw,heading))<=1.401f,"Shallow scrape has bounded speed/yaw correction");
                }
                var root=new GameObject("Exploration safety fixture");
                try
                {
                    var sopo=new JaksaDrive(root.transform);sopo.Reset(mode);
                    float z=20;float yaw=Mathf.Atan2(sopo.Street.RoadX(z+.1f)-sopo.Street.RoadX(z-.1f),.2f)*Mathf.Rad2Deg;
                    Pose(sopo,new Vector2(sopo.Street.RoadX(z)-1.48f,z),yaw+180,3);
                    for(int i=0;i<40;i++)sopo.Step(.02f,false,false,0);
                    Require(sopo.Safety.Failure==Rule.KeepLeft,"Sopo wrong heading in left lane fails "+mode);
                    sopo.Reset(mode);Pose(sopo,new Vector2(sopo.Street.RoadX(z)+1.48f,z),yaw+180,3);
                    for(int i=0;i<40;i++)sopo.Step(.02f,false,false,0);
                    Require(!sopo.Safety.Failure.HasValue,"Sopo right lane permits opposite direction "+mode);
                    sopo.Reset(mode);Pose(sopo,new Vector2(sopo.Street.RoadX(z)-1.48f,z),yaw,35/3.6f);
                    for(int i=0;i<25;i++)sopo.Step(.02f,false,false,0);
                    Require(sopo.Safety.SpeedWarning&&!sopo.Safety.Failure.HasValue&&sopo.Speed*3.6f>30,"Actual Sopo30 warning does not cap/fail "+mode);
                    sopo.Reset(mode);for(int i=0;i<70;i++)sopo.Step(.02f,false,false,0,true);
                    Require(sopo.Speed<0&&!sopo.Safety.Failure.HasValue,"Sopo deliberate reverse remains lawful "+mode);
                    var position=sopo.Position;sopo.Step(0,true,false,1);Require(sopo.Position==position,"Pause does not move Sopo");
                    sopo.Mirrors.Dispose();sopo.Street.Root.SetActive(false);
                    var hi=new LandmarkDrive(root.transform,JakartaPlace.BundaranHI,true);hi.Reset(mode);hi.TrafficEnabled=false;
                    var entry=hi.Environment.Network.Branches[0].Route;
                    Pose(hi,entry.At(65,-2),entry.Heading(65),30/3.6f);
                    for(int i=0;i<25;i++)hi.Step(.02f,false,false,0);
                    Require(hi.Speed*3.6f>28&&!hi.Safety.Failure.HasValue,"Actual HI throttle release coasts "+mode+" "+hi.Speed*3.6f+" "+hi.Safety.Failure);
                    hi.Reset(mode);hi.TrafficEnabled=false;
                    Pose(hi,hi.Environment.Route.At(100,-1.75f),hi.Environment.Route.Heading(100)+180,3);
                    for(int i=0;i<40;i++)hi.Step(.02f,false,false,0);
                    Require(hi.Safety.Failure==Rule.KeepLeft,"HI opposite circulation fails "+mode);
                    hi.Reset(mode);hi.TrafficEnabled=false;
                    Pose(hi,entry.At(65,-2),entry.Heading(65),45/3.6f);
                    for(int i=0;i<25;i++)hi.Step(.02f,false,false,0);
                    Require(hi.Safety.SpeedWarning&&!hi.Safety.Failure.HasValue,"HI40 overspeed only warns "+mode);
                    hi.Reset(mode);for(int i=0;i<70;i++)hi.Step(.02f,false,false,0,true);
                    Require(hi.Speed<0&&!hi.Safety.Failure.HasValue,"HI reverse lawful "+mode);
                    foreach(bool reversing in new[]{false,true})
                    {
                        hi.Reset(mode);hi.TrafficEnabled=true;
                        float station=100;var a=hi.Traffic[0];float direction=reversing?-1:1;
                        a.S=station+direction*(TrafficFootprint.PlayerHalfLength(mode)+a.Choice.Vehicle.HalfLength+.15f);
                        a.Lane=-5.15f;a.Speed=a.Target=0;a.Position=hi.Environment.Route.At(a.S,a.Lane);a.Heading=hi.Environment.Route.Heading(a.S);
                        Pose(hi,hi.Environment.Route.At(station,-5.15f),hi.Environment.Route.Heading(station),reversing?-RoadVehicleMotion.ReverseLimit(mode):3);
                        for(int i=0;i<8&&!hi.Safety.Failure.HasValue;i++)hi.Step(.02f,!reversing,false,0,reversing);
                        Require(hi.Safety.Failure==Rule.Collision,"Actual HI attempted full-pose actor contact fails, reverse="+reversing+" "+mode+" "+hi.Safety.Failure);
                    }
                    int thirty=0,forty=0;foreach(var text in root.GetComponentsInChildren<TextMesh>(true)){if(text.text=="30")thirty++;if(text.text=="40")forty++;}
                    Require(thirty>=4&&forty>=8,"Posted signs and painted speeds match Sopo30 and HI40");
                    hi.Mirrors.Dispose();
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
                Debug.Log("EXPLORE_SAFETY_MODE_PASS "+mode+" coast="+coast*3.6f+" free="+velocity*3.6f);
            }
            Debug.Log("EXPLORE_SAFETY_PASSED actual Sopo/HI direction, legal lane/reverse/pause, free propulsion, coast/brake, shallow scrape/edge recovery, warning-only speed and road signs");
        }
    }
}
