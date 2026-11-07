using System;
using System.Collections.Generic;
using UnityEngine;
namespace JalanKita
{
    public enum JakartaPlace { BundaranHI, Monas }
    [Serializable] public sealed class LandmarkMap
    {
        public string id,source;
        public double originLatitude,originLongitude;
        public float roadWidth;
        public Vector2[] route;
        public JaksaStreet.Building[] buildings;
        public JaksaStreet.Road[] roads;
    }
    // A continuous closed road, separate from the original Sopo Del street controller.
    public sealed class LandmarkRoute
    {
        public readonly Vector2[] Points;
        public readonly float[] Distances;
        public readonly bool Closed;
        public float Length=>Distances[^1];
        public LandmarkRoute(Vector2[] knots,bool closed=true)
        {
            Closed=closed;
            var points=new List<Vector2>();
            for(int i=0;i<(closed?knots.Length:knots.Length-1);i++)
            {
                Vector2 Knot(int n)=>knots[closed?(n+knots.Length)%knots.Length:Mathf.Clamp(n,0,knots.Length-1)];
                var a=Knot(i-1);var b=Knot(i);var c=Knot(i+1);var d=Knot(i+2);
                int steps=Mathf.Max(2,Mathf.CeilToInt(Vector2.Distance(b,c)/2));
                for(int j=0;j<steps;j++)
                {
                    float t=j/(float)steps;
                    points.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));
                }
            }
            points.Add(closed?points[0]:knots[^1]);Points=points.ToArray();Distances=new float[Points.Length];
            for(int i=1;i<Points.Length;i++)Distances[i]=Distances[i-1]+Vector2.Distance(Points[i-1],Points[i]);
        }
        public Vector2 At(float distance,float lateral=0)
        {
            distance=Closed?Mathf.Repeat(distance,Length):Mathf.Clamp(distance,0,Length);int i=Array.BinarySearch(Distances,distance);if(i<0)i=~i;i=Mathf.Clamp(i,1,Points.Length-1);
            Vector2 tangent=(Points[i]-Points[i-1]).normalized;
            return Vector2.Lerp(Points[i-1],Points[i],Mathf.InverseLerp(Distances[i-1],Distances[i],distance))+new Vector2(tangent.y,-tangent.x)*lateral;
        }
        public float Heading(float s)
        {var d=At(s+1)-At(s-1);return Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg;}
        public float Nearest(Vector2 p,out float lateral)
            =>Nearest(p,out lateral,out _,out _);
        public float Nearest(Vector2 p,out float lateral,out Vector2 center,out Vector2 forward)
        {
            float best=float.MaxValue,result=0;Vector2 closest=default,dir=default;
            for(int i=1;i<Points.Length;i++)
            {
                Vector2 d=Points[i]-Points[i-1];float t=Mathf.Clamp01(Vector2.Dot(p-Points[i-1],d)/d.sqrMagnitude);
                Vector2 q=Points[i-1]+d*t;float ds=(p-q).sqrMagnitude;
                if(ds>=best)continue;best=ds;closest=q;dir=d.normalized;result=Mathf.Lerp(Distances[i-1],Distances[i],t);
            }
            center=closest;forward=dir;
            lateral=Vector2.Dot(p-closest,new Vector2(dir.y,-dir.x));return result;
        }
    }
}
