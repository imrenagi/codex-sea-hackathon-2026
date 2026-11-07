using System;
using UnityEditor;
using UnityEngine;

namespace JalanKita.EditorTools
{
    public static class CityMapChecks
    {
        [MenuItem("RoadSense/Verify City Maps")]
        public static void Run()
        {
            var square = new[] { new CityPoint { x = 0, z = 0 }, new CityPoint { x = 10, z = 0 },
                new CityPoint { x = 10, z = 10 }, new CityPoint { x = 0, z = 10 } };
            Require(CityMapGeometry.Triangulate(square).Length == 6, "Square roof");
            Array.Reverse(square);
            Require(CityMapGeometry.Triangulate(square).Length == 6, "Reversed roof");
            Require(CityMapGeometry.Triangulate(Array.Empty<CityPoint>()).Length == 0, "Empty polygon");
            var concave = new[] { new CityPoint { x=0,z=0 }, new CityPoint { x=4,z=0 },
                new CityPoint { x=4,z=4 }, new CityPoint { x=2,z=2 }, new CityPoint { x=0,z=4 } };
            Require(CityMapGeometry.Triangulate(concave).Length == 9, "Concave roof");
            var clipped=CityMapPreview.Clip(new[] { new Vector3(-2000,0,-100),new Vector3(2000,0,-100),
                new Vector3(2000,0,100),new Vector3(-2000,0,100) });
            Require(clipped.Length == 4, "Cross-boundary road clipping");
            foreach (var point in clipped) Require(Mathf.Abs(point.x)<=864.01f && Mathf.Abs(point.z)<=735.01f,"Clipped bounds");
            var boundary=CityMapPreview.Clip(new[] { new Vector3(-900,0,0),new Vector3(-864,0,10),new Vector3(-800,0,0) });
            Require(boundary.Length == 3,"Boundary vertices must not duplicate");
            var indices=CityMapGeometry.Triangulate(square);
            for (int i=0;i<indices.Length;i+=3)
                Require(Vector3.Cross(square[indices[i+1]].At(0)-square[indices[i]].At(0),square[indices[i+2]].At(0)-square[indices[i]].At(0)).y>0,"Roof normals face up");
            foreach (var id in CityMapData.Ids)
            {
                var map = CityMapData.Load(id);
                Require(map.buildings.Length > 0 && map.roads.Length > 0, id + " geometry missing");
                Require(!string.IsNullOrEmpty(map.attribution), id + " attribution missing");
                foreach (var b in map.buildings) Require(b.points.Length >= 3 && b.height > 0, id + " invalid building");
            }
            Debug.Log("City map checks passed: six datasets, roof winding, empty geometry and attribution.");
        }
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
