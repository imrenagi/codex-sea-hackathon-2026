using System;
using System.Collections.Generic;
using UnityEngine;
namespace JalanKita
{
    [Serializable] public struct CityPoint { public float x, z; public Vector3 At(float y) => new(x,y,z); }
    [Serializable] public sealed class CityBuilding { public string osmId, name, heightBasis; public CityPoint[] points; public float height; }
    [Serializable] public sealed class CityRoad { public string osmId, name, highway, layer, bridge, tunnel, oneway, access; public CityPoint[] points; }
    [Serializable] public sealed class CityMapData
    {
        public string name, attribution, limitations;
        public double centerLat, centerLon;
        public CityBuilding[] buildings;
        public CityRoad[] roads;
        public static readonly string[] Ids = { "morrissey", "bundaran_hi", "sea_labs", "semanggi", "monas", "bundaran_senayan" };
        public static readonly string[] Names = { "Morrissey", "Bundaran HI", "Sea Labs", "Semanggi", "Monas", "Bundaran Senayan" };
        public static CityMapData Load(string id)
        {
            var asset = Resources.Load<TextAsset>("CityMaps/" + id);
            if (!asset) throw new InvalidOperationException("Data peta belum tersedia: " + id);
            var map = JsonUtility.FromJson<CityMapData>(asset.text);
            if (map?.buildings == null || map.roads == null) throw new InvalidOperationException("Data peta tidak valid: " + id);
            return map;
        }
    }
    public static class CityMapGeometry
    {
        public static int[] Triangulate(CityPoint[] points)
        {
            var result = new List<int>();
            if (points == null || points.Length < 3) return result.ToArray();
            int count = points.Length;
            if ((points[0].At(0)-points[count-1].At(0)).sqrMagnitude < .0001f) count--;
            var ring = new List<int>();
            float area = 0;
            for (int i=0;i<count;i++)
            {
                var a=points[i]; var b=points[(i+1)%count];
                area += a.x*b.z-b.x*a.z; ring.Add(i);
            }
            for (int i=ring.Count-1;i>=0 && ring.Count>2;i--)
            {
                int a=ring[(i+ring.Count-1)%ring.Count], b=ring[i], c=ring[(i+1)%ring.Count];
                if ((points[a].At(0)-points[b].At(0)).sqrMagnitude<.0001f || Mathf.Abs(Cross(points[a],points[b],points[c]))<.00001f)
                    ring.RemoveAt(i);
            }
            count=ring.Count;
            if (area < 0) ring.Reverse();
            int guard=count*count;
            while (ring.Count >= 3 && guard-- > 0)
            {
                bool clipped=false;
                for (int i=0;i<ring.Count;i++)
                {
                    int a=ring[(i+ring.Count-1)%ring.Count], b=ring[i], c=ring[(i+1)%ring.Count];
                    if (Cross(points[a],points[b],points[c]) <= .00001f) continue;
                    bool inside=false;
                    foreach (int p in ring)
                    {
                        if (p==a || p==b || p==c) continue;
                        if (Cross(points[a],points[b],points[p])>=0 && Cross(points[b],points[c],points[p])>=0 && Cross(points[c],points[a],points[p])>=0)
                        { inside=true; break; }
                    }
                    if (inside) continue;
                    result.Add(a); result.Add(c); result.Add(b); ring.RemoveAt(i); clipped=true; break;
                }
                if (!clipped) break;
            }
            // Malformed polygons render walls only, not a misleading partial roof.
            return result.Count == (count-2)*3 ? result.ToArray() : Array.Empty<int>();
        }
        static float Cross(CityPoint a,CityPoint b,CityPoint c) => (b.x-a.x)*(c.z-a.z)-(b.z-a.z)*(c.x-a.x);
    }
}
