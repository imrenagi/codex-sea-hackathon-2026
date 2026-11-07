using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace JalanKita
{
    // Geometry is combined per material and 25 m cell, keeping the district inexpensive to draw
    // while allowing Unity to cull distant blocks. UVs are in metres, not stretched per object.
    public sealed class StreetMesh
    {
        sealed class Buffer
        {
            public readonly List<Vector3> vertices = new();
            public readonly List<Vector2> uv = new();
            public readonly List<int> triangles = new();
        }
        readonly Dictionary<(Material, int), Buffer> buffers = new();

        public void Quad(Material material, Vector3 a, Vector3 b, Vector3 c, Vector3 d,
            float repeat = 1, Rect? atlas = null, bool flipU = false)
        {
            var key = (material, Mathf.FloorToInt((a.z + b.z + c.z + d.z) / 100));
            if (!buffers.TryGetValue(key, out var mesh)) buffers.Add(key, mesh = new Buffer());
            int n = mesh.vertices.Count;
            mesh.vertices.AddRange(new[] { a, b, c, d });
            Rect r = atlas ?? new Rect(0, 0, Vector3.Distance(a, b) / repeat, Vector3.Distance(b, c) / repeat);
            float left = flipU ? r.xMax : r.xMin, right = flipU ? r.xMin : r.xMax;
            mesh.uv.AddRange(new[] { new Vector2(left, r.yMin), new Vector2(right, r.yMin),
                new Vector2(right, r.yMax), new Vector2(left, r.yMax) });
            mesh.triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
        }

        public void Box(Material m, Vector3 p, Vector3 size, Quaternion rotation = default, float repeat = 2)
        {
            if (rotation == default) rotation = Quaternion.identity;
            Vector3 V(float x, float y, float z) => p + rotation * Vector3.Scale(new Vector3(x, y, z), size * .5f);
            Quad(m, V(-1,-1,1), V(1,-1,1), V(1,1,1), V(-1,1,1), repeat);
            Quad(m, V(1,-1,-1), V(-1,-1,-1), V(-1,1,-1), V(1,1,-1), repeat);
            Quad(m, V(1,-1,1), V(1,-1,-1), V(1,1,-1), V(1,1,1), repeat);
            Quad(m, V(-1,-1,-1), V(-1,-1,1), V(-1,1,1), V(-1,1,-1), repeat);
            Quad(m, V(-1,1,1), V(1,1,1), V(1,1,-1), V(-1,1,-1), repeat);
            Quad(m, V(-1,-1,-1), V(1,-1,-1), V(1,-1,1), V(-1,-1,1), repeat);
        }

        public void Tube(Material m, Vector3 a, Vector3 b, float radius, float endRadius = -1, int sides = 7)
        {
            if (endRadius < 0) endRadius = radius;
            Vector3 axis = (b - a).normalized;
            Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.y) > .9f ? Vector3.right : Vector3.up).normalized;
            Vector3 v = Vector3.Cross(axis, u);
            for (int i = 0; i < sides; i++)
            {
                float t = i * Mathf.PI * 2 / sides, t2 = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 q = u * Mathf.Cos(t) + v * Mathf.Sin(t), r = u * Mathf.Cos(t2) + v * Mathf.Sin(t2);
                Quad(m, a + q * radius, a + r * radius, b + r * endRadius, b + q * endRadius, .8f);
            }
        }

        public void Build(Transform parent)
        {
            foreach (var pair in buffers)
            {
                var data = pair.Value;
                var mesh = new Mesh { name = pair.Key.Item1.name + " / cell " + pair.Key.Item2,
                    indexFormat = data.vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(data.vertices); mesh.SetUVs(0, data.uv); mesh.SetTriangles(data.triangles, 0);
                mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
                var go = new GameObject(mesh.name);
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = pair.Key.Item1;
                renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
            }
            buffers.Clear();
        }
    }
}
