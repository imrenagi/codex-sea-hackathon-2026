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
            if (!asset) throw new InvalidOperationException("Map data unavailable: " + id);
            var map = JsonUtility.FromJson<CityMapData>(asset.text);
            if (map?.buildings == null || map.roads == null) throw new InvalidOperationException("Invalid map data: " + id);
            if (string.IsNullOrWhiteSpace(map.attribution)) throw new InvalidOperationException("Map attribution unavailable: " + id);
            foreach (var building in map.buildings)
            {
                if (building == null || !Finite(building.height) || building.height <= 0)
                    throw new InvalidOperationException("Invalid building height: " + id);
                CheckPoints(building.points, 3, id + " / building " + building.osmId);
            }
            foreach (var road in map.roads)
            {
                if (road == null) throw new InvalidOperationException("Invalid road: " + id);
                CheckPoints(road.points, 2, id + " / road " + road.osmId);
                if (float.TryParse(road.layer, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float layer) && (!Finite(layer) || Mathf.Abs(layer) > 1000))
                    throw new InvalidOperationException("Invalid road elevation: " + id + " / " + road.osmId);
            }
            return map;
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static void CheckPoints(CityPoint[] points, int minimum, string label)
        {
            if (points == null || points.Length < minimum)
                throw new InvalidOperationException("Incomplete map geometry: " + label);
            foreach (var point in points)
                if (!Finite(point.x) || !Finite(point.z) || Mathf.Abs(point.x) > 1000000 || Mathf.Abs(point.z) > 1000000)
                    throw new InvalidOperationException("Invalid map coordinates:" + label);
        }
    }
    public static class CityMapGeometry
    {
        public static int[] Triangulate(CityPoint[] points)
        {
            var result = new List<int>();
            if (points == null || points.Length < 3) return result.ToArray();
            foreach (var point in points)
                if (float.IsNaN(point.x) || float.IsInfinity(point.x) || float.IsNaN(point.z) || float.IsInfinity(point.z))
                    return Array.Empty<int>();
            int count = points.Length;
            if ((points[0].At(0)-points[count-1].At(0)).sqrMagnitude < .0001f) count--;
            var ring = new List<int>();
            for (int i=0;i<count;i++) ring.Add(i);
            bool changed;
            do
            {
                changed = false;
                for (int i=ring.Count-1;i>=0 && ring.Count>2;i--)
                {
                    int a=ring[(i+ring.Count-1)%ring.Count], b=ring[i], c=ring[(i+1)%ring.Count];
                    if ((points[a].At(0)-points[b].At(0)).sqrMagnitude<.0001f || Math.Abs(Cross(points[a],points[b],points[c]))<.00001)
                    { ring.RemoveAt(i); changed = true; }
                }
            }
            while (changed && ring.Count > 2);
            count=ring.Count;
            if (count < 3) return Array.Empty<int>();
            // A local origin preserves the winding of small footprints far from zero.
            double area = 0;
            for (int i=0;i<count;i++)
                area += Cross(points[ring[0]], points[ring[i]], points[ring[(i+1)%count]]);
            if (Math.Abs(area) < .00001) return Array.Empty<int>();
            for (int i=0;i<count;i++)
                for (int j=i+1;j<count;j++)
                {
                    if (j == i+1 || (i == 0 && j == count-1)) continue;
                    if (Intersects(points[ring[i]], points[ring[(i+1)%count]], points[ring[j]], points[ring[(j+1)%count]]))
                        return Array.Empty<int>();
                }
            if (area < 0) ring.Reverse();
            while (ring.Count >= 3)
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
        static double Cross(CityPoint a,CityPoint b,CityPoint c) =>
            ((double)b.x-a.x)*((double)c.z-a.z)-((double)b.z-a.z)*((double)c.x-a.x);
        static bool Intersects(CityPoint a, CityPoint b, CityPoint c, CityPoint d)
        {
            if (Math.Max(a.x,b.x) < Math.Min(c.x,d.x) || Math.Max(c.x,d.x) < Math.Min(a.x,b.x)
                || Math.Max(a.z,b.z) < Math.Min(c.z,d.z) || Math.Max(c.z,d.z) < Math.Min(a.z,b.z)) return false;
            double ac=Cross(a,b,c), ad=Cross(a,b,d), ca=Cross(c,d,a), cb=Cross(c,d,b);
            bool Between(CityPoint p, CityPoint q, CityPoint r) => r.x >= Math.Min(p.x,q.x)
                && r.x <= Math.Max(p.x,q.x) && r.z >= Math.Min(p.z,q.z) && r.z <= Math.Max(p.z,q.z);
            if (Math.Abs(ac) < .00001 && Between(a,b,c) || Math.Abs(ad) < .00001 && Between(a,b,d)
                || Math.Abs(ca) < .00001 && Between(c,d,a) || Math.Abs(cb) < .00001 && Between(c,d,b)) return true;
            return (ac > 0) != (ad > 0) && (ca > 0) != (cb > 0);
        }
    }
}
