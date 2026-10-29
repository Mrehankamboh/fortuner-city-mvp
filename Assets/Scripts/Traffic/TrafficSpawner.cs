using System.Collections.Generic;
using UnityEngine;
using FortunerCity.People;
using FortunerCity.Vehicle;

namespace FortunerCity.Traffic
{
    public static class TrafficSpawner
    {
        const double BikeShare = 0.25;   // a quarter of the traffic are motorbikes (with riders)

        public static List<GameObject> Spawn(List<WaypointPath> loops, int totalCars = 12)
        {
            var list = new List<GameObject>();
            if (loops == null || loops.Count == 0) return list;

            // share the cars out by loop length so long roads aren't empty and short ones aren't jammed
            var lengths = new float[loops.Count];
            float sum = 0f;
            for (int li = 0; li < loops.Count; li++) { lengths[li] = LoopLength(loops[li]); sum += lengths[li]; }
            int spawned = 0;
            var rng = new System.Random(7);

            for (int li = 0; li < loops.Count; li++)
            {
                var path = loops[li];
                int count = li == loops.Count - 1 ? totalCars - spawned : Mathf.RoundToInt(totalCars * lengths[li] / Mathf.Max(1f, sum));
                count = Mathf.Min(count, path.points.Count, Mathf.FloorToInt(lengths[li] / MinGap));
                if (count <= 0) continue;
                for (int i = 0; i < count; i++)
                {
                    int idx = (i * path.points.Count / count) % path.points.Count;
                    Vector3 wp = path.GetPoint(idx);
                    // bikes stay off the motorway
                    bool bike = path.speedScale <= 1f && rng.NextDouble() < BikeShare;
                    var car = bike ? BuildBike(wp, rng, out var model) : BuildNpc(wp, rng, out model);
                    var tc = car.AddComponent<TrafficCar>();
                    tc.wheels = model.wheels;
                    tc.wheelRadius = model.wheelRadius;
                    tc.halfLength = model.bodyBounds.extents.z;
                    float baseSpeed = 9f + (float)rng.NextDouble() * 7f;
                    tc.Init(path, baseSpeed * path.speedScale * (bike ? 0.85f : 1f), idx);
                    Vector3 next = path.GetPoint(path.NextIndex(idx));
                    Vector3 dir = next - car.transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.01f)
                        car.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    list.Add(car);
                    spawned++;
                }
            }

            return list;
        }

        const float MinGap = 25f;   // metres between spawned cars on one loop

        static float LoopLength(WaypointPath p)
        {
            float len = 0f;
            for (int i = 0; i < p.points.Count; i++)
                len += Vector3.Distance(p.GetPoint(i), p.GetPoint(p.NextIndex(i)));
            return len;
        }

        /// <summary>Random real-size procedural car (Haval / Range Rover / Land Cruiser… styles) in a factory colour.</summary>
        static GameObject BuildNpc(Vector3 roadPos, System.Random rng, out KenneyCarRig.Result model)
        {
            var spec = CarCatalog.RandomTraffic(rng, out Color paint);
            // Build at the origin, then place: root = road surface, tyres touch root Y=0
            var root = new GameObject("NPC_" + spec.name);
            root.tag = "TrafficCar";
            model = ProceduralCar.Build(root.transform, spec, paint);
            KenneyCarRig.AddBodyCollider(root, model.bodyBounds, 0.3f);
            root.transform.position = new Vector3(roadPos.x, VehicleGround.SpawnHeight(roadPos, root.transform), roadPos.z);
            return root;
        }

        /// <summary>Commuter or sports bike with a seated rider.</summary>
        static GameObject BuildBike(Vector3 roadPos, System.Random rng, out KenneyCarRig.Result model)
        {
            var spec = BikeBuilder.RandomTraffic(rng, out Color paint);
            var root = new GameObject("NPC_" + spec.name);
            root.tag = "TrafficCar";
            model = BikeBuilder.Build(root.transform, spec, paint);
            KenneyCarRig.AddBodyCollider(root, model.bodyBounds, 0.25f);

            var info = root.GetComponent<VehicleInfo>();
            var look = PersonBuilder.RandomLook(rng);
            look.female = false;
            look.kameez = rng.NextDouble() < 0.6;
            info.rider = PersonBuilder.BuildRider(info.seat, look);

            root.transform.position = new Vector3(roadPos.x, VehicleGround.SpawnHeight(roadPos, root.transform), roadPos.z);
            return root;
        }
    }
}
