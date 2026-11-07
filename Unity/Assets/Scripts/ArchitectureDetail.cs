using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JalanKita
{
    public static class ArchitectureDetail
    {
        public static void Dress(GameObject building, StreetLook look)
        {
            // Split only the original atlas cells that benefit from real surface response.
            // The shop lettering, signs, openings and source FBX remain intact.
            foreach (var filter in building.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = filter.GetComponent<MeshRenderer>(); var original = filter.sharedMesh;
                if (!renderer || !original || !original.isReadable) continue;
                var materials = renderer.sharedMaterials.ToList();
                int atlas = materials.FindIndex(m => m && m.name.StartsWith("M_RukoTrim"));
                if (atlas < 0) continue;
                var copy = Object.Instantiate(original); var uv = copy.uv;
                var buckets = new List<List<int>>();
                for (int i = 0; i < copy.subMeshCount; i++) buckets.Add(i == atlas ? new List<int>() : copy.GetTriangles(i).ToList());
                var mapping = new Dictionary<int, int>();
                foreach (int cell in new[] { 1, 2, 3, 5 })
                {
                    mapping[cell] = materials.Count; buckets.Add(new List<int>());
                    materials.Add(cell switch
                    {
                        1 => look.Surface("Building concrete", "concrete_floor_02", "c9c8c1"),
                        2 => look.Surface("Roller shutter metal", null, "899295", .4f, .55f),
                        3 => look.Surface("Facade glazing", null, "788a91", .85f, .25f),
                        _ => look.Surface("Terracotta building roofs", "clay_roof_tiles", "c7b19b")
                    });
                }
                var triangles = original.GetTriangles(atlas);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var at = (uv[triangles[i]] + uv[triangles[i + 1]] + uv[triangles[i + 2]]) / 3;
                    int cell = Mathf.Clamp((int)(at.x * 4), 0, 3) + Mathf.Clamp((int)(at.y * 4), 0, 3) * 4;
                    int slot = mapping.TryGetValue(cell, out int target) ? target : atlas;
                    buckets[slot].AddRange(new[] { triangles[i], triangles[i + 1], triangles[i + 2] });
                }
                copy.subMeshCount = materials.Count;
                for (int i = 0; i < buckets.Count; i++) copy.SetTriangles(buckets[i], i);
                filter.sharedMesh = copy; renderer.sharedMaterials = materials.ToArray();
                foreach (int cell in new[] { 1, 5 }) StreetLook.ProjectSurface(filter, cell == 5 ? 1.6f : 3, mapping[cell]);
            }
            var renderers = building.GetComponentsInChildren<Renderer>(); if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            if (bounds.size.x < 7 || bounds.size.z > 8) return;
            var mesh = new StreetMesh();
            var plaster = look.Surface("Weathered wall base", "StreetLook/Surfaces/weathered_plaster", "bbb6a7");
            var concrete = look.Surface("Window surrounds", "plastered_wall", "bdbeb5");
            var frame = look.Surface("Anodised window frame", null, "555e5d", .3f, .5f);
            var glass = look.Surface("Side window glass", null, "6a7f83", .83f, .2f);
            var metal = look.Surface("Rainwater pipes", null, "898d87", .25f, .3f);
            var ac = look.Surface("AC housings", null, "c8c9bf", .23f, .15f);
            foreach (int side in new[] { -1, 1 })
            {
                float z = side < 0 ? bounds.min.z : bounds.max.z;
                mesh.Box(plaster, new Vector3(bounds.center.x, bounds.min.y + .38f, z), new Vector3(bounds.size.x - .15f, .76f, .10f));
                for (float x = bounds.min.x + 2; x < bounds.max.x - 1; x += 3.2f)
                    for (float y = 5.1f; y < bounds.max.y - 1.7f; y += 3.6f)
                    {
                        mesh.Box(concrete, new Vector3(x, y, z + side * .035f), new Vector3(1.46f, 1.85f, .15f));
                        mesh.Box(frame, new Vector3(x, y, z + side * .12f), new Vector3(1.29f, 1.67f, .045f));
                        mesh.Box(glass, new Vector3(x, y, z + side * .147f), new Vector3(1.13f, 1.51f, .025f));
                        mesh.Box(frame, new Vector3(x, y, z + side * .166f), new Vector3(.045f, 1.55f, .04f));
                        mesh.Box(concrete, new Vector3(x, y - .94f, z + side * .15f), new Vector3(1.57f, .09f, .38f));
                        mesh.Box(concrete, new Vector3(x, y + .95f, z + side * .15f), new Vector3(1.65f, .10f, .45f));
                    }
                float pipeX = bounds.min.x + .3f;
                mesh.Tube(metal, new Vector3(pipeX, .18f, z + side * .09f), new Vector3(pipeX, bounds.max.y - 1.5f, z + side * .09f), .045f, sides: 10);
                for (float y = 1; y < bounds.max.y - 1.5f; y += 2)
                    mesh.Box(frame, new Vector3(pipeX, y, z + side * .09f), new Vector3(.16f, .045f, .13f));
                float ax = bounds.center.x;
                mesh.Box(ac, new Vector3(ax, 3.25f, z + side * .24f), new Vector3(.88f, .57f, .42f));
                for (int bar = 0; bar < 12; bar++)
                    mesh.Box(frame, new Vector3(ax - .36f + bar * .065f, 3.25f, z + side * .46f), new Vector3(.023f, .43f, .02f));
            }
            // Mesh coordinates are world-space, then attached while preserving that transform.
            var root = new GameObject("Original facade detail").transform;
            root.SetParent(building.transform, true); mesh.Build(root);
        }

        public static void House(StreetMesh mesh, StreetLook look, int side, float z, float height)
        {
            float x = side * 9.9f;
            var tile = look.Surface("Clay roof tiles", "clay_roof_tiles", "b99b82");
            var metal = look.Surface("House gutter metal", null, "737871", .22f, .45f);
            var trim = look.Surface("House trim", "plastered_wall", "dedbd0");
            var weather = look.Surface("Weathered wall base", "StreetLook/Surfaces/weathered_plaster", "bbb6a7");
            // Narrow raised tile crests make the eaves and ridges read beyond a flat roof plane.
            foreach (int slope in new[] { -1, 1 })
            {
                for (float at = z - 3.25f; at < z + 3.35f; at += .27f)
                    mesh.Tube(tile, new Vector3(x + slope * .08f, height + 1.01f, at),
                        new Vector3(x + slope * 2.73f, height + .045f, at), .033f, .026f, 7);
                mesh.Tube(metal, new Vector3(x + slope * 2.76f, height + .025f, z - 3.4f),
                    new Vector3(x + slope * 2.76f, height + .025f, z + 3.4f), .065f, sides: 10);
            }
            mesh.Box(weather, new Vector3(side * 7.475f, .35f, z), new Vector3(.075f, .66f, 6.29f));
            mesh.Tube(metal, new Vector3(side * 7.18f, height, z + 3.12f), new Vector3(side * 7.18f, .15f, z + 3.12f), .043f, sides: 10);
            // Recess-like door panels and a shaded ventilation grille.
            var dark = look.Surface("Door panel inset", null, "484b40");
            foreach (float y in new[] { .58f, 1.55f })
                mesh.Box(dark, new Vector3(side * 7.435f, y, z - 1.65f), new Vector3(.035f, .68f, .68f));
            mesh.Box(trim, new Vector3(side * 7.43f, 2.36f, z - 1.65f), new Vector3(.10f, .21f, .92f));
            for (int bar = 0; bar < 7; bar++)
                mesh.Box(dark, new Vector3(side * 7.37f, 2.36f, z - 1.98f + bar * .11f), new Vector3(.04f, .13f, .043f));
        }
    }
}
