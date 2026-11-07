using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace JalanKita
{
    // Original branch structure and clustered crowns, with separate far foliage.
    public static class NaturalTree
    {
        public static GameObject Create(Transform parent, Vector3 p, float height, int seed, Material bark, Material leaf)
        {
            var root = new GameObject("Original tropical shade tree"); root.transform.SetParent(parent, false); root.transform.position = p;
            var lods = new LOD[2];
            for (int level = 0; level < 2; level++)
            {
                var rng = new System.Random(seed); float R(float a, float b) => Mathf.Lerp(a, b, (float)rng.NextDouble());
                var mesh = new StreetMesh(); var branch = new Vector3(.17f, height * .48f, -.12f);
                mesh.Tube(bark, Vector3.zero, new Vector3(-.09f, height * .25f, .06f), .23f, .17f, level == 0 ? 12 : 7);
                mesh.Tube(bark, new Vector3(-.09f, height * .25f, .06f), branch, .17f, .11f, level == 0 ? 12 : 7);
                // Lopsided, overlapping crown lobes prevent the repeated flat umbrella silhouette.
                for (int b = 0; b < 9; b++)
                {
                    float angle = b * 2.39996f + R(-.2f, .2f), radius = R(1.45f, 3.05f) * height / 8;
                    var end = new Vector3(Mathf.Cos(angle) * radius, height * R(.69f, .94f), Mathf.Sin(angle) * radius);
                    var middle = Vector3.Lerp(branch, end, .52f) - Vector3.up * .32f;
                    mesh.Tube(bark, branch, middle, .095f, .055f, level == 0 ? 9 : 6);
                    mesh.Tube(bark, middle, end, .055f, .014f, level == 0 ? 8 : 5);
                    float crownRadius = R(.85f, 1.3f);
                    for (int k = 0; k < 30; k++)
                    {
                        float azimuth = R(0, Mathf.PI * 2), az = R(-1, 1), r = Mathf.Pow(R(0, 1), .333f) * crownRadius;
                        var local = new Vector3(Mathf.Cos(azimuth) * Mathf.Sqrt(1 - az * az), az * 1.2f, Mathf.Sin(azimuth) * Mathf.Sqrt(1 - az * az)) * r;
                        var point = end + local;
                        var rotation = Quaternion.Euler(R(-65, 65), R(0, 360), R(-60, 60));
                        float size = R(.8f, 1.35f) * (level == 0 ? 1 : 1.4f);
                        if (level == 1 && k % 3 != 0) continue;
                        var u = rotation * Vector3.right * size * .5f; var v = rotation * Vector3.up * size * .5f;
                        mesh.Quad(leaf, point - u - v, point + u - v, point + u + v, point - u + v, atlas: new Rect(0, 0, 1, 1));
                    }
                }
                var group = new GameObject("Tree LOD " + level).transform; group.SetParent(root.transform, false); mesh.Build(group);
                var renderers = group.GetComponentsInChildren<Renderer>();
                if (level == 1) foreach (var renderer in renderers) renderer.shadowCastingMode = ShadowCastingMode.Off;
                lods[level] = new LOD(level == 0 ? .10f : .012f, renderers);
            }
            var lod = root.AddComponent<LODGroup>(); lod.SetLODs(lods); lod.RecalculateBounds(); return root;
        }
    }
}
