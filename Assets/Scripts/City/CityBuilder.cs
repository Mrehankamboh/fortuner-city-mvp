using System.Collections.Generic;
using UnityEngine;
using FortunerCity.Traffic;

namespace FortunerCity.City
{
    /// <summary>
    /// City layout (8 x 8 blocks of 64 m):
    /// - avenues (26 m, 3 lanes each way + tree median) on every 4th line, streets (14 m) between
    /// - downtown core of glass skyscrapers, mid-rise offices/apartments around it,
    ///   Kenney CC0 houses in two corner suburbs, parks
    /// - working traffic signals (TrafficSignals) and an elevated ring motorway with ramps (MotorwayBuilder)
    /// Roads and buildings use procedural textures (CityArt) — nothing paid or downloaded.
    /// </summary>
    public static class CityBuilder
    {
        public struct CityResult
        {
            public Transform root;
            public Vector3 playerSpawn;
            public Quaternion playerSpawnRot;
            public List<WaypointPath> trafficLoops;
        }

        public const int Grid = 8;
        public const float BlockSize = 64f;
        public const float StreetWidth = 14f;
        public const float AvenueWidth = 26f;
        public const float MedianWidth = 4f;
        public const float RoadTop = 0.05f;      // top of road colliders
        public const float PadTop = 0.15f;       // top of block pads (sidewalk / kerb height)
        const float SidewalkDepth = 4f;
        const float KenneyScale = 5.5f;

        // Lane centre offsets from the road centre line (left-hand traffic uses the left lanes)
        public const float AvenueMidLane = 7.75f;
        public const float StreetLane = 3f;

        public static float Half => Grid * BlockSize * 0.5f;
        public static float Line(int i) => -Half + i * BlockSize;
        public static bool IsAvenue(int i) => i % 4 == 0;
        public static float Width(int i) => IsAvenue(i) ? AvenueWidth : StreetWidth;

        public enum District { Downtown, Midtown, Residential, Park }

        public static CityResult Build()
        {
            var root = new GameObject("City").transform;
            var roads = Child(root, "Roads");
            var blocks = Child(root, "Blocks");
            var buildings = Child(root, "Buildings");
            var kenney = Child(root, "KenneyProps");   // imported FBX meshes are not batchable
            var rng = new System.Random(42);
            bool kit = CityKitLoader.Ready;

            // Ground (grass) out past the motorway
            float g = Half + 200f;
            CityMesh.SpawnBox("Ground", root, new Vector3(g * 2f, 0.02f, g * 2f), new Vector3(0f, -0.02f, 0f),
                CityArt.Grass, CityArt.Grass, true, 0f, 8f);

            // Road segments between intersections + intersection pads
            for (int i = 0; i <= Grid; i++)
            {
                for (int j = 0; j < Grid; j++)
                {
                    float a = Line(j) + Width(j) * 0.5f;
                    float b = Line(j + 1) - Width(j + 1) * 0.5f;
                    RoadSegment(roads, kenney, new Vector3(Line(i), 0f, (a + b) * 0.5f), b - a, Width(i), IsAvenue(i), false, kit, rng);
                    RoadSegment(roads, kenney, new Vector3((a + b) * 0.5f, 0f, Line(i)), b - a, Width(i), IsAvenue(i), true, kit, rng);
                }
            }
            for (int i = 0; i <= Grid; i++)
                for (int j = 0; j <= Grid; j++)
                    Intersection(roads, i, j);

            // Working traffic signals + stop lines (poles batch with the roads; lamp heads stay dynamic)
            TrafficSignals.Build(root, roads);

            // Blocks
            for (int bx = 0; bx < Grid; bx++)
            {
                for (int bz = 0; bz < Grid; bz++)
                {
                    float x0 = Line(bx) + Width(bx) * 0.5f, x1 = Line(bx + 1) - Width(bx + 1) * 0.5f;
                    float z0 = Line(bz) + Width(bz) * 0.5f, z1 = Line(bz + 1) - Width(bz + 1) * 0.5f;
                    var center = new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f);
                    float sx = x1 - x0, sz = z1 - z0;
                    var district = Classify(bx, bz);

                    CityMesh.SpawnBox("Block", blocks, new Vector3(sx, PadTop, sz), center,
                        CityArt.Concrete, district == District.Park ? CityArt.Grass : CityArt.Paving, true);

                    var zone = new Rect(x0 + SidewalkDepth, z0 + SidewalkDepth, sx - SidewalkDepth * 2f, sz - SidewalkDepth * 2f);
                    switch (district)
                    {
                        case District.Downtown: Downtown(buildings, kenney, zone, bx == 3 && bz == 3, kit, rng); break;
                        case District.Midtown: Midtown(buildings, zone, rng); break;
                        case District.Residential: Residential(kenney, buildings, zone, kit, rng); break;
                        case District.Park: Park(blocks, kenney, zone, kit, rng); break;
                    }

                    StreetFurniture(kenney, x0, x1, z0, z1, kit);
                }
            }

            // Elevated ring motorway + ramps
            var motorwayLoops = MotorwayBuilder.Build(root, kenney, kit, rng);

            // Traffic: square loops on the outer avenue and two inner street rings, plus motorway
            var loops = new List<WaypointPath>
            {
                MakeLoop(root, "Loop_Avenue_CCW", Half - AvenueMidLane, RoadTop, false, 8f, 8f),
                MakeLoop(root, "Loop_Avenue_CW", Half + AvenueMidLane, RoadTop, true, 8f, 8f),
                MakeLoop(root, "Loop_Street128", Line(6) - StreetLane, RoadTop, false, 4f, 8f),
                MakeLoop(root, "Loop_Street64", Line(5) + StreetLane, RoadTop, true, 4f, 8f),
            };
            loops.AddRange(motorwayLoops);

            // Merge static geometry into big batches (procedural meshes only)
            StaticBatchingUtility.Combine(roads.gameObject);
            StaticBatchingUtility.Combine(blocks.gameObject);
            StaticBatchingUtility.Combine(buildings.gameObject);
            Physics.SyncTransforms();

            // Player: south outer avenue, middle lane, heading east (the wide SUV would scrape the kerb lane)
            var spawn = new Vector3(Line(0) + 35f, RoadTop, Line(0) + AvenueMidLane);
            return new CityResult
            {
                root = root,
                playerSpawn = spawn,
                playerSpawnRot = Quaternion.LookRotation(Vector3.right, Vector3.up),
                trafficLoops = loops
            };
        }

        static Transform Child(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        public static District Classify(int bx, int bz)
        {
            if ((bx == 2 && bz == 5) || (bx == 1 && bz == 2) || (bx == 6 && bz == 4)) return District.Park;
            if ((bx <= 1 && bz <= 1) || (bx >= 6 && bz >= 6)) return District.Residential;
            if (bx >= 2 && bx <= 5 && bz >= 2 && bz <= 5) return District.Downtown;
            return District.Midtown;
        }

        // ---------- roads ----------

        /// <summary>Road between two intersections. Local frame: width along X, length along Z.</summary>
        public static void RoadSegment(Transform parent, Transform props, Vector3 center, float length, float width,
            bool avenue, bool eastWest, bool kit, System.Random rng)
        {
            var seg = new GameObject(avenue ? "Avenue" : "Street").transform;
            seg.SetParent(parent, false);
            seg.localPosition = center;
            seg.localRotation = eastWest ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;

            CityMesh.SpawnBox("Base", seg, new Vector3(width, RoadTop, length), Vector3.zero, CityArt.Asphalt, CityArt.Asphalt, true);
            seg.GetChild(0).tag = "Road";

            float y = RoadTop + 0.012f;
            if (!avenue)
            {
                CityMesh.SpawnQuad("Lanes", seg, CityArt.Road2, width, length, width, CityArt.RoadTileLength,
                    new Vector3(0f, y, 0f), Quaternion.identity);
                return;
            }

            float cw = (width - MedianWidth) * 0.5f;
            float off = MedianWidth * 0.5f + cw * 0.5f;
            // Road3 texture has the median edge at u=0, so the left carriageway is flipped
            CityMesh.SpawnQuad("LanesL", seg, CityArt.Road3, cw, length, cw, CityArt.RoadTileLength, new Vector3(-off, y, 0f), Quaternion.identity, true);
            CityMesh.SpawnQuad("LanesR", seg, CityArt.Road3, cw, length, cw, CityArt.RoadTileLength, new Vector3(off, y, 0f), Quaternion.identity);

            // Raised tree median (stops short of the intersections)
            float medLen = Mathf.Max(2f, length - 4f);
            CityMesh.SpawnBox("Median", seg, new Vector3(MedianWidth, 0.2f, medLen), new Vector3(0f, RoadTop, 0f),
                CityArt.Concrete, CityArt.Grass, true);
            for (float z = -medLen * 0.5f + 6f; z < medLen * 0.5f - 4f; z += 12f)
            {
                Vector3 world = seg.TransformPoint(new Vector3(0f, RoadTop + 0.2f, z));
                PlaceTree(props, world, kit, rng, 0.8f, false);
            }
        }

        static void Intersection(Transform parent, int i, int j)
        {
            float x = Line(i), z = Line(j), wi = Width(i), wj = Width(j);
            CityMesh.SpawnBox("Intersection", parent, new Vector3(wi, RoadTop, wj), new Vector3(x, 0f, z),
                CityArt.Asphalt, CityArt.Asphalt, true);

            // Zebra crossings on each arm that exists
            float y = RoadTop + 0.02f;
            if (j < Grid) Crosswalk(parent, new Vector3(x, y, z + wj * 0.5f + 2f), wi, false);
            if (j > 0) Crosswalk(parent, new Vector3(x, y, z - wj * 0.5f - 2f), wi, false);
            if (i < Grid) Crosswalk(parent, new Vector3(x + wi * 0.5f + 2f, y, z), wj, true);
            if (i > 0) Crosswalk(parent, new Vector3(x - wi * 0.5f - 2f, y, z), wj, true);
        }

        static void Crosswalk(Transform parent, Vector3 pos, float roadWidth, bool eastWest)
        {
            var rot = eastWest ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            CityMesh.SpawnQuad("Crosswalk", parent, CityArt.Zebra, roadWidth - 1f, 3f, 1f, 3f, pos, rot);
        }

        // ---------- districts ----------

        static void Downtown(Transform parent, Transform props, Rect zone, bool landmarkBlock, bool kit, System.Random rng)
        {
            float lw = zone.width * 0.5f, ld = zone.height * 0.5f;
            for (int ix = 0; ix < 2; ix++)
            {
                for (int iz = 0; iz < 2; iz++)
                {
                    var c = new Vector3(zone.x + lw * (ix + 0.5f), PadTop, zone.y + ld * (iz + 0.5f));
                    bool landmark = landmarkBlock && ix == 1 && iz == 1;   // corner nearest the city centre
                    if (!landmark && rng.NextDouble() < 0.12)
                    {
                        // small plaza with trees
                        for (int k = 0; k < 4; k++)
                            PlaceTree(props, c + new Vector3((k % 2 - 0.5f) * lw * 0.5f, 0f, (k / 2 - 0.5f) * ld * 0.5f), kit, rng, 1f, true);
                        continue;
                    }
                    float d = Mathf.Clamp01(new Vector2(c.x, c.z).magnitude / (BlockSize * 2.6f));
                    float h = Mathf.Lerp(200f, 75f, d) * (0.6f + (float)rng.NextDouble() * 0.55f);
                    if (landmark) h = 280f;
                    CityBuildings.Tower(parent, c, lw - 2f, ld - 2f, h, rng);
                }
            }
        }

        static void Midtown(Transform parent, Rect zone, System.Random rng)
        {
            bool wide = zone.width >= zone.height;
            int nx = wide ? 3 : 2, nz = wide ? 2 : 3;
            if (rng.NextDouble() < 0.4) { nx = 2; nz = 2; }
            float lw = zone.width / nx, ld = zone.height / nz;
            for (int ix = 0; ix < nx; ix++)
            {
                for (int iz = 0; iz < nz; iz++)
                {
                    var c = new Vector3(zone.x + lw * (ix + 0.5f), PadTop, zone.y + ld * (iz + 0.5f));
                    float h = 14f + (float)rng.NextDouble() * 38f;
                    CityBuildings.Midrise(parent, c, lw - 1.5f, ld - 1.5f, h, rng);
                }
            }
        }

        static void Residential(Transform props, Transform fallbackParent, Rect zone, bool kit, System.Random rng)
        {
            float lw = zone.width * 0.5f, ld = zone.height * 0.5f;
            for (int ix = 0; ix < 2; ix++)
            {
                for (int iz = 0; iz < 2; iz++)
                {
                    var c = new Vector3(zone.x + lw * (ix + 0.5f), PadTop, zone.y + ld * (iz + 0.5f));
                    var prefab = kit ? CityKitLoader.Building(rng) : null;
                    if (prefab != null)
                    {
                        var go = Object.Instantiate(prefab, props);
                        go.name = "House_" + prefab.name;
                        go.transform.position = c;
                        // face the nearer street on the X side
                        go.transform.rotation = Quaternion.Euler(0f, ix == 0 ? 270f : 90f, 0f);
                        go.transform.localScale = Vector3.one * KenneyScale;
                        EnsureCollider(go);
                    }
                    else
                    {
                        CityBuildings.Midrise(fallbackParent, c, lw - 6f, ld - 6f, 8f, rng);
                    }
                    PlaceTree(props, c + new Vector3((ix - 0.5f) * lw * 0.7f, 0f, (iz - 0.5f) * ld * 0.7f), kit, rng, 1f, true);
                }
            }
        }

        static void Park(Transform parent, Transform props, Rect zone, bool kit, System.Random rng)
        {
            var c = new Vector3(zone.center.x, PadTop, zone.center.y);
            // cross paths + fountain
            CityMesh.SpawnQuad("PathNS", parent, CityArt.Paving, 4f, zone.height + SidewalkDepth * 2f, 4f, 4f, c + Vector3.up * 0.01f, Quaternion.identity);
            CityMesh.SpawnQuad("PathEW", parent, CityArt.Paving, 4f, zone.width + SidewalkDepth * 2f, 4f, 4f, c + Vector3.up * 0.012f, Quaternion.Euler(0f, 90f, 0f));
            CityMesh.SpawnBox("Fountain", parent, new Vector3(9f, 0.7f, 9f), c, CityArt.Concrete, CityArt.Water, true);
            for (int k = 0; k < 14; k++)
            {
                float px = zone.x + (float)rng.NextDouble() * zone.width;
                float pz = zone.y + (float)rng.NextDouble() * zone.height;
                if (Mathf.Abs(px - c.x) < 4f || Mathf.Abs(pz - c.z) < 4f) continue;   // keep paths clear
                PlaceTree(props, new Vector3(px, PadTop, pz), kit, rng, 0.9f + (float)rng.NextDouble() * 0.5f, true);
            }
        }

        static void StreetFurniture(Transform props, float x0, float x1, float z0, float z1, bool kit)
        {
            // Street lamps mid-way along each kerb (block corners hold the traffic-signal poles)
            float cx = (x0 + x1) * 0.5f, cz = (z0 + z1) * 0.5f;
            var spots = new[]
            {
                new Vector3(cx, PadTop, z0 + 1.0f), new Vector3(cx, PadTop, z1 - 1.0f),
                new Vector3(x0 + 1.0f, PadTop, cz), new Vector3(x1 - 1.0f, PadTop, cz)
            };
            float[] yaws = { 180f, 0f, 270f, 90f };   // lamp heads lean over the road
            for (int k = 0; k < spots.Length; k++)
            {
                var light = kit ? CityKitLoader.StreetLight() : null;
                if (light != null) PlaceProp(props, light, spots[k], KenneyScale, yaws[k], 0.4f);
                else CityMesh.SpawnBox("Lamp", props, new Vector3(0.25f, 7f, 0.25f), spots[k], CityArt.Barrier, CityArt.Barrier, true);
            }
        }

        // ---------- props ----------

        static void PlaceTree(Transform parent, Vector3 pos, bool kit, System.Random rng, float scale, bool collider)
        {
            var prefab = kit ? CityKitLoader.Tree(rng) : null;
            if (prefab != null)
                PlaceProp(parent, prefab, pos, KenneyScale * scale * (0.85f + (float)rng.NextDouble() * 0.3f), rng.Next(0, 360), collider ? 0.5f : 0f);
            else
                CityBuildings.Tree(parent, pos, scale * 1.4f);
        }

        /// <param name="colliderWidth">World-space pole/trunk width; 0 = no collider.</param>
        public static void PlaceProp(Transform parent, GameObject prefab, Vector3 pos, float scale, float yaw, float colliderWidth)
        {
            if (prefab == null) return;
            var go = Object.Instantiate(prefab, parent);
            go.name = prefab.name;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            foreach (var c in go.GetComponentsInChildren<Collider>())
                Object.Destroy(c);
            if (colliderWidth <= 0f) return;
            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 1.5f / scale, 0f);
            box.size = new Vector3(colliderWidth, 3f, colliderWidth) / scale;
        }

        static void EnsureCollider(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>())
                Object.Destroy(c);
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return;
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            var col = go.AddComponent<BoxCollider>();
            Vector3 size = go.transform.InverseTransformVector(b.size);
            col.center = go.transform.InverseTransformPoint(b.center);
            col.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }

        // ---------- traffic ----------

        /// <summary>Square loop with rounded corners. Points run counter-clockwise (seen from above) unless clockwise.</summary>
        public static WaypointPath MakeLoop(Transform parent, string name, float half, float y, bool clockwise,
            float cornerRadius, float spacing, float speedScale = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var path = go.AddComponent<WaypointPath>();
            path.speedScale = speedScale;

            var corners = new[] { new Vector2(-half, -half), new Vector2(half, -half), new Vector2(half, half), new Vector2(-half, half) };
            var pts = new List<Vector3>();
            for (int k = 0; k < 4; k++)
            {
                Vector2 c = corners[k], next = corners[(k + 1) % 4], after = corners[(k + 2) % 4];
                Vector2 dir = (next - c).normalized, dirNext = (after - next).normalized;
                Vector2 start = c + dir * cornerRadius, end = next - dir * cornerRadius;
                int n = Mathf.Max(2, Mathf.RoundToInt((end - start).magnitude / spacing));
                for (int s = 0; s < n; s++)
                {
                    var p = Vector2.Lerp(start, end, s / (float)n);
                    pts.Add(new Vector3(p.x, y, p.y));
                }
                // arc around the next corner
                Vector2 arcCenter = end + dirNext * cornerRadius;
                int arcN = Mathf.Max(6, Mathf.RoundToInt(cornerRadius * Mathf.PI * 0.5f / spacing));
                for (int s = 0; s < arcN; s++)
                {
                    float t = s / (float)arcN * Mathf.PI * 0.5f;
                    var p = arcCenter - dirNext * (cornerRadius * Mathf.Cos(t)) + dir * (cornerRadius * Mathf.Sin(t));
                    pts.Add(new Vector3(p.x, y, p.y));
                }
            }
            if (clockwise) pts.Reverse();
            path.BuildFromPositions(pts);
            path.loop = true;
            return path;
        }
    }
}
