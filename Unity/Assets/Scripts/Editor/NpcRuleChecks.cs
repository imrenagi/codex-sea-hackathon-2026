using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace JalanKita.EditorTools
{
    public static class NpcRuleChecks
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException("NPC: " + message); }
        static void Set(Journey run, string field, object value) => typeof(Journey).GetField(field, Private).SetValue(run, value);
        static Journey Scene(int stage, float clock)
        {
            var run = new Journey(VehicleMode.Car, new JourneySave { next = stage }, false, trafficSeed: 1);
            typeof(Journey).GetProperty("Position").SetValue(run, new Vector2(100, 100));
            Set(run, "boulevardTime", clock);
            ((List<BoulevardTraffic.Vehicle>)run.CityTraffic.Vehicles).Clear();
            return run;
        }
        static BoulevardTraffic.Vehicle Car(Journey run, int road, Vector2 position, Vector2 direction, bool motorcycle = false)
        {
            var n = new BoulevardTraffic.Vehicle { Id = run.CityTraffic.Vehicles.Count, Road = road, Position = position,
                Direction = direction, Along = road == 0 ? position.y - 390 : road == 3 ? position.y - 565 : position.x + (road == 2 ? 260 : 85),
                Cruise = 11, Motorcycle = motorcycle };
            ((List<BoulevardTraffic.Vehicle>)run.CityTraffic.Vehicles).Add(n);
            return n;
        }
        public static void Run()
        {
            foreach (bool motorcycle in new[] {false, true}) foreach (var direction in new[] {Vector2.up,Vector2.down,Vector2.left,Vector2.right})
            {
                var run=Scene(5,12);
                var start=Boulevard.Center(5)-direction*42+new Vector2(direction.y,-direction.x)*-5.4f;
                var n=Car(run,direction.y!=0?0:1,start,direction,motorcycle);
                for(int i=0;i<500;i++) run.CityTraffic.Step(.02f,run);
                float stop=motorcycle?-18:-26;
                float Front()=>Vector2.Dot(n.Position-Boulevard.Center(5),direction)+n.HalfLength;
                Require(Front()<=stop+.02f && n.Speed<.05f,"Red light and category stop line on every approach");
                Set(run,"boulevardTime",direction.y!=0?22f:3f);
                for(int i=0;i<200;i++) run.CityTraffic.Step(.02f,run);
                Require(Front()>stop+1,"Green releases waiting traffic");
            }
            {
                var run=Scene(7,16); var n=Car(run,2,new Vector2(-138,634.6f),Vector2.left);
                for(int i=0;i<500;i++)run.CityTraffic.Step(.02f,run);
                Require(n.Position.x-n.HalfLength>=-154-.02f,"Protected right arrow does not release straight NPC traffic");
            }
            {
                var run=Scene(5,22); var n=Car(run,0,new Vector2(-5.4f,438),Vector2.up);
                var blocking=Car(run,0,new Vector2(-5.4f,501),Vector2.up); blocking.Cruise=0;
                for(int i=0;i<500;i++)run.CityTraffic.Step(.02f,run);
                Require(n.Position.y+n.HalfLength<454,"Do not block intersection when exit is occupied");
            }

            {
                var run=Scene(5,22); var n=Car(run,0,new Vector2(-5.4f,450),Vector2.up);
                typeof(Journey).GetProperty("BoulevardPedestrian").SetValue(run,new Vector2(-8,466));
                for(int i=0;i<500;i++) run.CityTraffic.Step(.02f,run);
                Require(n.Position.y+n.HalfLength<465,"Incoming pedestrian has priority");
                typeof(Journey).GetProperty("BoulevardPedestrian").SetValue(run,new Vector2(2,466));
                for(int i=0;i<200;i++) run.CityTraffic.Step(.02f,run);
                Require(n.Position.y+n.HalfLength>467,"NPC proceeds after pedestrian clears own lane");
            }
            foreach(int direction in new[]{-1,1})
            {
                var run=Scene(8,0); Set(run,"railTime",8f);
                var n=Car(run,3,new Vector2(-180-direction*5.4f,direction>0?770:830),Vector2.up*direction);
                for(int i=0;i<500;i++)run.CityTraffic.Step(.02f,run);
                Require(direction>0?n.Position.y+n.HalfLength<=790.02f:n.Position.y-n.HalfLength>=809.98f,$"Rail barriers protect both directions: {direction}, {n.Position}, speed {n.Speed}");
            }
            {
                var run=Scene(5,22); var a=Car(run,0,new Vector2(-5.4f,440),Vector2.up);
                var b=Car(run,0,new Vector2(-5.4f,452),Vector2.up); b.Cruise=0;
                for(int i=0;i<500;i++)run.CityTraffic.Step(.02f,run);
                Require(a.Position.y+a.HalfLength<b.Position.y-b.HalfLength && a.Speed<.05f,"NPC follows stopped traffic without overlap");
                Require(TrafficFootprint.Sweeps(new Vector2(-10,0),new Vector2(10,0),90,90,.95f,1.95f,
                    new Vector2(0,-10),new Vector2(0,10),0,0,.95f,1.95f),"Fast crossing trajectories detect contact between frames");
                Require(!TrafficFootprint.Sweeps(Vector2.zero,Vector2.up*20,0,0,.95f,1.95f,
                    new Vector2(4,0),new Vector2(4,20),0,0,.95f,1.95f),"Adjacent lanes do not collide");
            }
            {
                var run=Scene(9,0); var n=Car(run,3,new Vector2(-185.4f,970),Vector2.up);
                typeof(Journey).GetProperty("Ambulance").SetValue(run,new Vector2(-185.4f,930));
                for(int i=0;i<250;i++)run.CityTraffic.Step(.02f,run);
                Require(n.Offset<-8.8f,"NPC yields left for ambulance when adjacent lane is clear");
                Require(n.Speed<=50/3.6f,"NPC respects boulevard speed limit");
            }
            {
                var run=Scene(9,0); var n=Car(run,3,new Vector2(-185.4f,900),Vector2.up);
                typeof(Journey).GetProperty("Ambulance").SetValue(run,new Vector2(-185.4f,860));
                for(int i=0;i<750;i++)
                {
                    if(i==400) typeof(Journey).GetProperty("Ambulance").SetValue(run,new Vector2(-185.4f,960));
                    run.CityTraffic.Step(.02f,run);
                    Require(!TrafficFootprint.Overlaps(n.Position,n.Heading,n.HalfWidth,n.HalfLength,new Vector2(-189,914),0,.95f,2.3f),
                        "NPC yields without clipping the parked vehicle");
                }
                Require(n.Position.y>920,"NPC rejoins safely after the ambulance passes");
            }
            {
                var run=Scene(9,0); var n=Car(run,3,new Vector2(-185.4f,970),Vector2.up); n.Cruise=100;
                for(int i=0;i<400;i++)
                { run.CityTraffic.Step(.02f,run); Require(n.Speed<=50/3.6f+.01f,"Speed cap applies to excessive cruise requests"); }
                Require(n.Speed>13,"Speed cap test reaches boulevard cruising speed");
            }
            Debug.Log("NPC_RULE_CHECKS_PASSED stop lines, lights, pedestrian clearance, railway, following, ambulance yielding and swept collisions.");
        }
    }
}
