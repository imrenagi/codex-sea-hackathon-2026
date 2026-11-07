using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UjianSim.Game
{
    /// <summary>Accumulates boxes and quads into one mesh, with world-space UVs so textures tile evenly.</summary>
    public class MeshBuilder
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<Color> col = new List<Color>();
        readonly List<int> tri = new List<int>();
        readonly float uvScale;

        public MeshBuilder(float uvScale = 0.25f) { this.uvScale = uvScale; }

        public int Count => v.Count;

        void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color color)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (int k = 0; k < 4; k++) { n.Add(normal); col.Add(color); }
            // Project onto the face's dominant plane for UVs.
            foreach (var p in new[] { a, b, c, d })
            {
                var an = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
                Vector2 q = an.y >= an.x && an.y >= an.z ? new Vector2(p.x, p.z) : an.x >= an.z ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y);
                uv.Add(q * uvScale);
            }
            tri.Add(i); tri.Add(i + 1); tri.Add(i + 2);
            tri.Add(i); tri.Add(i + 2); tri.Add(i + 3);
        }

        /// <summary>Horizontal quad (facing up) covering [x0,x1] x [z0,z1] in Unity coordinates at height y.</summary>
        public void Quad(float x0, float x1, float z0, float z1, float y, Color? color = null)
        {
            var c = color ?? Color.white;
            Face(new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0), Vector3.up, c);
        }

        /// <summary>Axis-aligned box from min to max corners (Unity coordinates); bottom face omitted.</summary>
        public void Box(Vector3 min, Vector3 max, Color? color = null)
        {
            var c = color ?? Color.white;
            float x0 = min.x, y0 = min.y, z0 = min.z, x1 = max.x, y1 = max.y, z1 = max.z;
            Face(new Vector3(x0, y1, z0), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0), Vector3.up, c);
            Face(new Vector3(x0, y0, z1), new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), Vector3.forward, c);
            Face(new Vector3(x1, y0, z0), new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), Vector3.back, c);
            Face(new Vector3(x1, y0, z1), new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), Vector3.right, c);
            Face(new Vector3(x0, y0, z0), new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0), Vector3.left, c);
        }

        /// <summary>A box rotated about +y by yawDeg around its centre.</summary>
        public void OrientedBox(Vector3 center, Vector3 size, float yawDeg, Color? color = null)
        {
            var c = color ?? Color.white;
            var q = Quaternion.Euler(0, yawDeg, 0);
            Vector3 h = size * 0.5f;
            Vector3 P(float x, float y, float z) => center + q * new Vector3(x * h.x, y * h.y, z * h.z);
            Face(P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), q * Vector3.up, c);
            Face(P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), q * Vector3.forward, c);
            Face(P(1, -1, -1), P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), q * Vector3.back, c);
            Face(P(1, -1, 1), P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), q * Vector3.right, c);
            Face(P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), q * Vector3.left, c);
        }

        public GameObject Build(string name, Transform parent, Material material, bool shadows = true, bool collider = false)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetColors(col);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }
    }
}
