using UnityEngine;

namespace JalanKita
{
    // Original Indonesian residential details; all solids remain outside the lesson road.
    // This is frontage dressing, not a narrower collision corridor or a new scored hazard.
    public static class GangStreetDetails
    {
        public static void Build(Transform parent, StreetLook look)
        {
            var root = new GameObject("Kenanga residential frontages").transform;
            root.SetParent(parent, false);
            var mesh = new StreetMesh();
            var roof = look.Surface("Clay roof tiles", "clay_roof_tiles", "b99b82");
            var trim = look.Surface("House trim", "plastered_wall", "dedbd0");
            var concrete = look.Surface("Courtyard concrete", "concrete_floor_02", "bdbab0");
            var gate = look.Surface("Residential gates", null, "42534f", .25f, .35f);
            var wood = look.Surface("Timber doors", null, "655449");
            var glass = look.Surface("Residential windows", null, "344a52", .48f);
            var wire = look.Surface("Utility cable", null, "252c2d");
            var pot = look.Surface("Terracotta pots", "plastered_wall", "a7775c");
            var soil = look.Surface("Pot soil", null, "3d4130");
            var foliage = look.Surface("Tree foliage", null, "c5cfad", .05f);
            string[] walls = { "cec9b7", "b8c5b5", "c3bbb0", "b7c4c5", "d0bea4" };
            foreach (int side in new[] { -1, 1 })
            {
                int index = 0;
                for (float z = -20; z < 24; z += 7)
                {
                    if (Mathf.RoundToInt(z + 20) % 28 == 14) continue;
                    var wall = look.Surface("Residential plaster " + index, "plastered_wall", walls[index % walls.Length]);
                    float x = side * 9.9f, y = index % 2 == 0 ? 3.05f : 3.4f;
                    mesh.Box(wall, new Vector3(x, y / 2, z), new Vector3(4.8f, y, 6.35f));
                    mesh.Box(concrete, new Vector3(side * 7.05f, .11f, z), new Vector3(1.3f, .22f, 6.35f));
                    mesh.Box(trim, new Vector3(side * 7.45f, y - .1f, z), new Vector3(.2f, .23f, 6.5f));
                    // Two textured roof slopes and ridge. Geometry has actual thickness.
                    foreach (int slope in new[] { -1, 1 })
                        mesh.Box(roof, new Vector3(x + slope * 1.35f, y + .48f, z),
                            new Vector3(2.93f, .12f, 6.8f), Quaternion.Euler(0, 0, slope * -20));
                    mesh.Tube(roof, new Vector3(x, y + .99f, z - 3.4f), new Vector3(x, y + .99f, z + 3.4f), .105f, sides: 8);
                    // Road-facing door, windows, jambs, and a shallow porch canopy.
                    mesh.Box(wood, new Vector3(side * 7.48f, 1.09f, z - 1.65f), new Vector3(.07f, 2.12f, .95f));
                    mesh.Box(gate, new Vector3(side * 7.42f, 1.07f, z - 1.29f), new Vector3(.05f, .17f, .05f));
                    foreach (float offset in new[] { .1f, 1.65f })
                    {
                        mesh.Box(trim, new Vector3(side * 7.43f, 1.65f, z + offset), new Vector3(.13f, 1.45f, 1.15f));
                        mesh.Box(glass, new Vector3(side * 7.35f, 1.65f, z + offset), new Vector3(.04f, 1.25f, .96f));
                        mesh.Box(gate, new Vector3(side * 7.31f, 1.65f, z + offset), new Vector3(.04f, 1.27f, .045f));
                    }
                    mesh.Box(roof, new Vector3(side * 6.98f, 2.55f, z - 1.55f), new Vector3(1.15f, .09f, 1.7f), Quaternion.Euler(0, 0, side * 8));
                    // Low boundary and slatted gate: avoid the existing resident at x=6.5,z=17.
                    float gx = side * (side > 0 && z == 15 ? 6.95f : 6.58f);
                    foreach (float dz in new[] { -3.04f, -.3f, 3.04f })
                        mesh.Box(trim, new Vector3(gx, .84f, z + dz), new Vector3(.25f, 1.68f, .25f));
                    mesh.Box(wall, new Vector3(gx, .38f, z + 1.35f), new Vector3(.18f, .76f, 3.05f));
                    for (float dz = -2.85f; dz < 2.9f; dz += .18f)
                    {
                        float height = dz < -.4f ? 1.35f : .72f;
                        mesh.Box(gate, new Vector3(gx, dz < -.4f ? .8f : 1.15f, z + dz), new Vector3(.055f, height, .035f));
                    }
                    foreach (float h in new[] { .2f, 1.48f })
                        mesh.Box(gate, new Vector3(gx, h, z - 1.65f), new Vector3(.07f, .045f, 2.5f));
                    for (int p = 0; p < 2; p++)
                    {
                        var at = new Vector3(side * 7.04f, .23f, z + .65f + p * 1.3f);
                        mesh.Tube(pot, at, at + Vector3.up * .36f, .17f, .24f, 10);
                        mesh.Box(soil, at + Vector3.up * .34f, new Vector3(.31f, .035f, .31f));
                        for (int leaf = 0; leaf < 5; leaf++)
                        {
                            var q = Quaternion.Euler(-12 + leaf * 8, leaf * 72, 0);
                            var u = q * Vector3.right * .28f; var v = q * Vector3.up * .36f;
                            var c = at + Vector3.up * .68f;
                            mesh.Quad(foliage, c - u - v, c + u - v, c + u + v, c - u + v, atlas: new Rect(0, 0, 1, 1));
                        }
                    }
                    ArchitectureDetail.House(mesh, look, side, z, y);
                    index++;
                }
                // Cables terminate at poles and service connections rather than floating ends.
                foreach (float z in new[] { -23f, -1f, 23f })
                {
                    mesh.Tube(concrete, new Vector3(side * 6.4f, 0, z), new Vector3(side * 6.4f, 6.3f, z), .1f, .065f, 10);
                    mesh.Box(gate, new Vector3(side * 6.4f, 5.8f, z), new Vector3(.8f, .08f, .1f));
                    float houseZ = z == 23 ? 15 : z == -1 ? 1 : -20;
                    Cable(mesh, wire, new Vector3(side * 6.4f, 5.5f, z), new Vector3(side * 7.5f, 2.9f, houseZ), .16f);
                }
                foreach (var span in new[] { new Vector2(-23, -1), new Vector2(-1, 23) })
                    for (int cable = 0; cable < 3; cable++)
                        Cable(mesh, wire, new Vector3(side * 6.4f + (cable - 1) * .28f, 5.85f, span.x),
                            new Vector3(side * 6.4f + (cable - 1) * .28f, 5.85f, span.y), .55f + cable * .11f);
            }
            Cable(mesh, wire, new Vector3(-6.4f, 5.7f, 23), new Vector3(6.4f, 5.7f, 23), .6f);
            mesh.Build(root);
            // Parked within private frontage pockets, beyond the sidewalk and scored corridor.
            foreach (int side in new[] { -1, 1 })
            {
                var scooter = new TrafficVehicleView(root, "Resident's parked scooter");
                scooter.Select(new TrafficChoice(TrafficCatalogue.All[side < 0 ? 0 : 1], 1));
                scooter.Present(new Vector3(side * 7.55f, .12f, -6), side * 18, 0, 0, false);
                scooter.Root.transform.rotation *= Quaternion.Euler(0, 0, -6);
            }
        }

        static void Cable(StreetMesh mesh, Material material, Vector3 start, Vector3 end, float sag)
        {
            var previous = start;
            for (int i = 1; i <= 12; i++)
            {
                float t = i / 12f;
                var next = Vector3.Lerp(start, end, t) - Vector3.up * (4 * t * (1 - t) * sag);
                mesh.Tube(material, previous, next, .014f, sides: 5); previous = next;
            }
        }
    }
}
