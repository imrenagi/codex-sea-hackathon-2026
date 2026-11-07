using System;
using UnityEngine;

namespace JalanKita
{
    public sealed class TrafficDefinition
    {
        public readonly string Id;
        public readonly bool Motorcycle;
        public readonly float Radius, Wheelbase, Scale, HalfWidth, HalfLength;
        public readonly string[] Colors;
        public TrafficDefinition(string id, bool motorcycle, float radius, float wheelbase, float width, float length, params string[] colors)
        {
            Id=id; Motorcycle=motorcycle; Radius=radius; Wheelbase=wheelbase; Colors=colors;
            Scale=motorcycle?1:Mathf.Min(1,1.95f/width,3.9f/length);
            HalfWidth=width*Scale*.5f; HalfLength=length*Scale*.5f;
        }
    }
    public sealed class TrafficChoice
    {
        public readonly TrafficDefinition Vehicle;
        public readonly int Color;
        public TrafficChoice(TrafficDefinition vehicle,int color){Vehicle=vehicle;Color=Math.Abs(color)%vehicle.Colors.Length;}
    }
    public static class TrafficCatalogue
    {
        public static readonly TrafficDefinition[] All={
            new("sport_scooter",true,.258f,1.277f,.72f,1.866f,"245DA5","A92936"),
            new("retro_scooter",true,.245f,1.25f,.694f,1.828f,"67B7AA","B66C4A"),
            new("maxi_scooter",true,.263f,1.35f,.72f,1.951f,"546679","D5DBD9"),
            new("city_hatchback",false,.293f,2.405f,2.10f,3.957f,"E3B534","C74F39"),
            new("family_mpv",false,.32f,2.75f,2.15f,4.542f,"DCE1DD","254859"),
            new("utility_pickup",false,.288f,2.205f,2.095f,4.297f,"A3B1B4","42685A")
        };
        public static int Seed(string id,int variant,VehicleMode mode)
        {
            unchecked {int h=1937;foreach(char c in id??"journey")h=h*31+c;return h^(variant*7919)^((int)mode*127);}
        }
    }
    public sealed class TrafficRoster
    {
        public readonly TrafficChoice[] Choices;
        public TrafficChoice Cross=>Choices[0]; public TrafficChoice Lane=>Choices[1];
        public TrafficChoice Spur=>Choices[2]; public TrafficChoice Pickup=>Choices[3];
        public TrafficRoster(int seed)
        {
            var rng=new System.Random(seed);var indices=new[]{0,1,2,3,4,5};
            for(int i=5;i>0;i--){int j=rng.Next(i+1);(indices[i],indices[j])=(indices[j],indices[i]);}
            Choices=new TrafficChoice[6];
            for(int i=0;i<6;i++)Choices[i]=new TrafficChoice(TrafficCatalogue.All[indices[i]],rng.Next(2));
        }
    }
    public static class TrafficFootprint
    {
        public static float PlayerHalfWidth(VehicleMode mode)=>mode==VehicleMode.Car?.95f:.37f;
        public static float PlayerHalfLength(VehicleMode mode)=>mode==VehicleMode.Car?1.95f:1.02f;
        public static bool SweptOverlaps(Vector2 from,float fromYaw,Vector2 to,float toYaw,float width,float length,
            Vector2 obstacle,float obstacleYaw,float obstacleWidth,float obstacleLength,float margin=.03f)
        {
            float turn=Mathf.DeltaAngle(fromYaw,toYaw);
            int samples=Mathf.Max(1,Mathf.CeilToInt((Vector2.Distance(from,to)+Mathf.Abs(turn)*Mathf.Deg2Rad*
                new Vector2(width,length).magnitude)/.1f));
            for(int i=0;i<=samples;i++)
                if(Overlaps(Vector2.Lerp(from,to,(float)i/samples),fromYaw+turn*i/samples,width,length,
                    obstacle,obstacleYaw,obstacleWidth,obstacleLength,margin))return true;
            return false;
        }
        public static bool Sweeps(Vector2 fromA, Vector2 toA, float yawFromA, float yawToA, float widthA, float lengthA,
            Vector2 fromB, Vector2 toB, float yawFromB, float yawToB, float widthB, float lengthB, float margin = .03f)
        {
            float reach = new Vector2(widthA,lengthA).magnitude + new Vector2(widthB,lengthB).magnitude + margin;
            if (Mathf.Min(fromA.x,toA.x) - Mathf.Max(fromB.x,toB.x) > reach
                || Mathf.Min(fromB.x,toB.x) - Mathf.Max(fromA.x,toA.x) > reach
                || Mathf.Min(fromA.y,toA.y) - Mathf.Max(fromB.y,toB.y) > reach
                || Mathf.Min(fromB.y,toB.y) - Mathf.Max(fromA.y,toA.y) > reach) return false;
            float turnA = Mathf.DeltaAngle(yawFromA, yawToA), turnB = Mathf.DeltaAngle(yawFromB, yawToB);
            float distance = ((toA - fromA) - (toB - fromB)).magnitude
                + Mathf.Abs(turnA) * Mathf.Deg2Rad * new Vector2(widthA, lengthA).magnitude
                + Mathf.Abs(turnB) * Mathf.Deg2Rad * new Vector2(widthB, lengthB).magnitude;
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / .15f));
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                if (Overlaps(Vector2.Lerp(fromA, toA, t), yawFromA + turnA * t, widthA, lengthA,
                    Vector2.Lerp(fromB, toB, t), yawFromB + turnB * t, widthB, lengthB, margin)) return true;
            }
            return false;
        }
        public static bool Overlaps(Vector2 a,float yawA,float widthA,float lengthA,Vector2 b,float yawB,float widthB,float lengthB,float margin=.03f)
        {
            float reach=widthA+lengthA+widthB+lengthB+margin;
            if (Mathf.Abs(a.x-b.x)>reach || Mathf.Abs(a.y-b.y)>reach) return false;
            float aa=yawA*Mathf.Deg2Rad,bb=yawB*Mathf.Deg2Rad;
            Vector2 af=new(Mathf.Sin(aa),Mathf.Cos(aa)),ar=new(af.y,-af.x);
            Vector2 bf=new(Mathf.Sin(bb),Mathf.Cos(bb)),br=new(bf.y,-bf.x),d=b-a;
            bool Axis(Vector2 axis)=>Mathf.Abs(Vector2.Dot(d,axis))<
                Mathf.Abs(Vector2.Dot(ar,axis))*widthA+Mathf.Abs(Vector2.Dot(af,axis))*lengthA+
                Mathf.Abs(Vector2.Dot(br,axis))*widthB+Mathf.Abs(Vector2.Dot(bf,axis))*lengthB+margin;
            return Axis(ar)&&Axis(af)&&Axis(br)&&Axis(bf);
        }
    }
}
