using System.Collections.Generic;
using UnityEngine;

namespace FortunerCity.Vehicle
{
    public enum BikeStyle { Commuter, Sport }

    public class BikeSpec
    {
        public string name;
        public BikeStyle style;
        public float frontAxle, rearAxle, wheelRadius, frontTyre, rearTyre;
        public float seatHeight;
        public float topSpeedKmh, accel;
        public EngineSound engine;
        public int gears;
        public Color[] palette;
        public float trafficWeight = 1f;
    }

    /// <summary>
    /// Procedural motorbikes at real size: a CD70-style 70cc commuter (Pakistan's everyday bike) and a
    /// 600cc-class sports bike. No brand logos. Root = road, forward = +Z.
    /// Hierarchy: root → Lean (tilts in corners) → body, front pose (steers: fork + bars + wheel), rear pose.
    /// </summary>
    public static class BikeBuilder
    {
        static Color C(float r, float g, float b) => new Color(r, g, b);

        public static readonly BikeSpec Commuter = new BikeSpec
        {
            name = "CommuterBike70", style = BikeStyle.Commuter, frontAxle = 0.61f, rearAxle = -0.59f,
            wheelRadius = 0.30f, frontTyre = 0.065f, rearTyre = 0.075f, seatHeight = 0.77f,
            topSpeedKmh = 85f, accel = 11f, engine = EngineSound.Single, gears = 4,
            palette = new[] { C(0.78f, 0.06f, 0.06f), C(0.08f, 0.08f, 0.09f), C(0.1f, 0.2f, 0.55f) },
            trafficWeight = 3f
        };

        public static readonly BikeSpec Sport = new BikeSpec
        {
            name = "SportBike600", style = BikeStyle.Sport, frontAxle = 0.70f, rearAxle = -0.70f,
            wheelRadius = 0.31f, frontTyre = 0.12f, rearTyre = 0.18f, seatHeight = 0.83f,
            topSpeedKmh = 250f, accel = 38f, engine = EngineSound.SportBike, gears = 6,
            palette = new[] { C(0.05f, 0.2f, 0.6f), C(0.1f, 0.6f, 0.15f), C(0.08f, 0.08f, 0.09f), C(0.85f, 0.1f, 0.1f) },
            trafficWeight = 1f
        };

        public static readonly BikeSpec[] All = { Commuter, Sport };

        static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        public static BikeSpec RandomTraffic(System.Random rng, out Color paint)
        {
            float total = 0f;
            foreach (var s in All) total += s.trafficWeight;
            float pick = (float)rng.NextDouble() * total;
            var chosen = All[0];
            foreach (var s in All) { pick -= s.trafficWeight; if (pick <= 0f) { chosen = s; break; } }
            paint = chosen.palette[rng.Next(chosen.palette.Length)];
            return chosen;
        }

        public static KenneyCarRig.Result Build(Transform root, BikeSpec spec, Color paint)
        {
            var r = new KenneyCarRig.Result();
            var lean = new GameObject("Lean").transform;
            lean.SetParent(root, false);
            r.visual = lean;

            var mats = (Material[])ProceduralCar.Shared().Clone();
            mats[(int)CarMat.Paint] = ProceduralCar.Paint(paint);

            var body = new GameObject(spec.name);
            body.transform.SetParent(lean, false);
            body.AddComponent<MeshFilter>().sharedMesh = Get(spec.name + "_body", () => BuildBody(spec));
            body.AddComponent<MeshRenderer>().sharedMaterials = mats;

            // Front: pose steers fork, bars and lamp with the wheel; only the wheel mesh spins
            var front = Pose(lean, "WheelPose_F", new Vector3(0f, spec.wheelRadius, spec.frontAxle));
            var forkGo = new GameObject("Fork");
            forkGo.transform.SetParent(front, false);
            forkGo.AddComponent<MeshFilter>().sharedMesh = Get(spec.name + "_front", () => BuildFront(spec));
            forkGo.AddComponent<MeshRenderer>().sharedMaterials = mats;
            Wheel(front, spec, spec.frontTyre, "F");

            var rear = Pose(lean, "WheelPose_R", new Vector3(0f, spec.wheelRadius, spec.rearAxle));
            Wheel(rear, spec, spec.rearTyre, "R");

            r.frontLeft = front;
            r.rearLeft = rear;
            r.wheels.Add(front);
            r.wheels.Add(rear);
            r.wheelRadius = spec.wheelRadius;
            float len = spec.frontAxle - spec.rearAxle + spec.wheelRadius * 2f + 0.15f;
            r.bodyBounds = new Bounds(new Vector3(0f, 0.68f, 0f), new Vector3(0.62f, 0.95f, len));

            var info = root.gameObject.AddComponent<VehicleInfo>();
            info.model = spec.name;
            info.isBike = true;
            info.topSpeedKmh = spec.topSpeedKmh;
            info.accel = spec.accel;
            info.engine = spec.engine;
            info.gears = spec.gears;
            info.maxSteer = 26f;
            info.turnResponsiveness = 3.2f;
            info.lean = lean;
            var seat = new GameObject("Seat").transform;
            seat.SetParent(lean, false);
            seat.localPosition = new Vector3(0f, spec.seatHeight, spec.style == BikeStyle.Sport ? -0.22f : -0.18f);
            info.seat = seat;

            root.gameObject.AddComponent<BikeLean>().lean = lean;
            return r;
        }

        static Transform Pose(Transform parent, string name, Vector3 pos)
        {
            var p = new GameObject(name).transform;
            p.SetParent(parent, false);
            p.localPosition = pos;
            return p;
        }

        static void Wheel(Transform pose, BikeSpec s, float tyre, string tag)
        {
            var spin = new GameObject("Spin").transform;
            spin.SetParent(pose, false);
            var w = new GameObject("Wheel");
            w.transform.SetParent(spin, false);
            w.AddComponent<MeshFilter>().sharedMesh = Get($"{s.name}_wheel{tag}", () => BuildWheel(s, tyre));
            var sh = ProceduralCar.Shared();
            w.AddComponent<MeshRenderer>().sharedMaterials = new[] { ProceduralCar.Rubber(), sh[(int)(s.style == BikeStyle.Sport ? CarMat.Dark : CarMat.Chrome)], sh[(int)CarMat.Dark] };
        }

        static Mesh Get(string key, System.Func<Mesh> build)
        {
            if (!Cache.TryGetValue(key, out var m)) Cache[key] = m = build();
            return m;
        }

        // ---------- geometry ----------

        static void Tube(MeshBuilder mb, CarMat m, Vector3 a, Vector3 b, float thick)
        {
            Vector3 d = b - a;
            var rot = Quaternion.FromToRotation(Vector3.up, d.normalized);
            mb.Box((int)m, (a + b) * 0.5f, new Vector3(thick, d.magnitude, thick), rot);
        }

        static void Box(MeshBuilder mb, CarMat m, Vector3 c, Vector3 size, float pitch = 0f) =>
            mb.Box((int)m, c, size, Quaternion.Euler(pitch, 0f, 0f));

        static Mesh BuildBody(BikeSpec s)
        {
            var mb = new MeshBuilder((int)CarMat.Count);
            float fa = s.frontAxle, ra = s.rearAxle, r = s.wheelRadius;
            var head = new Vector3(0f, s.style == BikeStyle.Sport ? 0.86f : 0.92f, fa - 0.22f);    // steering head

            if (s.style == BikeStyle.Commuter)
            {
                // CD70-style: tubular underbone, teardrop tank, long flat seat, chrome carrier, upright engine
                Tube(mb, CarMat.Black, head, new Vector3(0f, 0.52f, 0.05f), 0.05f);                 // down tube
                Tube(mb, CarMat.Black, head, new Vector3(0f, 0.74f, -0.25f), 0.045f);               // backbone
                Tube(mb, CarMat.Black, new Vector3(0.1f, 0.42f, -0.05f), new Vector3(0.1f, r, ra), 0.035f);   // swingarm
                Tube(mb, CarMat.Black, new Vector3(-0.1f, 0.42f, -0.05f), new Vector3(-0.1f, r, ra), 0.035f);
                Tube(mb, CarMat.Chrome, new Vector3(0.11f, 0.76f, -0.45f), new Vector3(0.11f, r + 0.02f, ra), 0.035f);  // shocks
                Tube(mb, CarMat.Chrome, new Vector3(-0.11f, 0.76f, -0.45f), new Vector3(-0.11f, r + 0.02f, ra), 0.035f);
                Box(mb, CarMat.Paint, new Vector3(0f, 0.80f, 0.12f), new Vector3(0.26f, 0.17f, 0.40f), -8f);   // tank
                Box(mb, CarMat.Chrome, new Vector3(0f, 0.89f, 0.14f), new Vector3(0.08f, 0.02f, 0.06f));       // filler cap
                Box(mb, CarMat.Black, new Vector3(0f, 0.80f, -0.30f), new Vector3(0.25f, 0.09f, 0.58f));       // seat
                Box(mb, CarMat.Paint, new Vector3(0f, 0.66f, -0.22f), new Vector3(0.22f, 0.16f, 0.36f));       // side covers
                Box(mb, CarMat.Paint, new Vector3(0f, r + 0.33f, ra - 0.04f), new Vector3(0.14f, 0.03f, 0.5f), -15f);  // rear fender
                Box(mb, CarMat.Chrome, new Vector3(0f, 0.86f, -0.66f), new Vector3(0.24f, 0.02f, 0.30f));      // carrier rack
                Box(mb, CarMat.Tail, new Vector3(0f, 0.74f, -0.86f), new Vector3(0.1f, 0.06f, 0.05f));
                Box(mb, CarMat.Plate, new Vector3(0f, 0.62f, -0.88f), new Vector3(0.18f, 0.1f, 0.01f));
                // engine: crankcase + forward-tilted finned cylinder
                Box(mb, CarMat.Silver, new Vector3(0f, 0.38f, 0.0f), new Vector3(0.26f, 0.20f, 0.30f));
                for (int i = 0; i < 5; i++)
                    Box(mb, CarMat.Silver, new Vector3(0f, 0.47f + i * 0.025f, 0.16f + i * 0.02f), new Vector3(0.17f, 0.012f, 0.14f), -60f);
                Tube(mb, CarMat.Chrome, new Vector3(0.05f, 0.40f, 0.22f), new Vector3(0.14f, 0.30f, -0.15f), 0.04f);   // exhaust
                Box(mb, CarMat.Chrome, new Vector3(0.14f, 0.31f, -0.45f), new Vector3(0.08f, 0.08f, 0.5f), 4f);        // muffler
                Box(mb, CarMat.Black, new Vector3(-0.1f, 0.40f, -0.32f), new Vector3(0.03f, 0.08f, 0.55f));            // chain guard
                Box(mb, CarMat.Black, new Vector3(0f, 0.33f, -0.06f), new Vector3(0.52f, 0.025f, 0.03f));              // footrests
            }
            else
            {
                // Sports bike: twin-spar frame under bodywork, full fairing, stepped seat, tail cowl
                Tube(mb, CarMat.Dark, head, new Vector3(0.12f, 0.55f, -0.25f), 0.06f);
                Tube(mb, CarMat.Dark, head, new Vector3(-0.12f, 0.55f, -0.25f), 0.06f);
                Tube(mb, CarMat.Dark, new Vector3(0.11f, 0.5f, -0.2f), new Vector3(0.11f, r, ra), 0.06f);       // swingarm
                Tube(mb, CarMat.Dark, new Vector3(-0.11f, 0.5f, -0.2f), new Vector3(-0.11f, r, ra), 0.06f);
                Box(mb, CarMat.Paint, new Vector3(0f, 0.84f, 0.10f), new Vector3(0.34f, 0.2f, 0.42f), -10f);   // tank
                Box(mb, CarMat.Paint, new Vector3(0f, 0.62f, 0.28f), new Vector3(0.40f, 0.42f, 0.42f), -20f);   // side fairing
                Box(mb, CarMat.Paint, new Vector3(0f, 0.38f, 0.10f), new Vector3(0.32f, 0.16f, 0.55f));        // belly pan
                Box(mb, CarMat.Black, new Vector3(0f, 0.82f, -0.25f), new Vector3(0.26f, 0.07f, 0.34f));        // rider seat
                Box(mb, CarMat.Paint, new Vector3(0f, 0.92f, -0.58f), new Vector3(0.22f, 0.12f, 0.40f), 12f);   // tail cowl
                Box(mb, CarMat.Tail, new Vector3(0f, 0.95f, -0.80f), new Vector3(0.14f, 0.04f, 0.04f));
                Box(mb, CarMat.Plate, new Vector3(0f, 0.72f, -0.88f), new Vector3(0.18f, 0.1f, 0.01f), 30f);
                Box(mb, CarMat.Dark, new Vector3(0f, 0.46f, -0.05f), new Vector3(0.28f, 0.22f, 0.30f));         // engine
                Tube(mb, CarMat.Silver, new Vector3(0.1f, 0.35f, -0.1f), new Vector3(0.16f, 0.48f, -0.62f), 0.07f);  // exhaust can
                Box(mb, CarMat.Black, new Vector3(0f, 0.36f, -0.12f), new Vector3(0.50f, 0.025f, 0.03f));        // rearsets
            }
            return mb.ToMesh(s.name + "_body");
        }

        /// <summary>Front assembly in front-pose space (origin = front axle): fork, bars, lamp, fender.</summary>
        static Mesh BuildFront(BikeSpec s)
        {
            var mb = new MeshBuilder((int)CarMat.Count);
            float r = s.wheelRadius;
            bool sport = s.style == BikeStyle.Sport;
            Vector3 top = new Vector3(0f, sport ? 0.62f : 0.66f, -0.20f);
            Tube(mb, sport ? CarMat.Silver : CarMat.Chrome, new Vector3(0.08f, 0f, 0f), top + new Vector3(0.08f, 0f, 0f), 0.04f);   // fork legs
            Tube(mb, sport ? CarMat.Silver : CarMat.Chrome, new Vector3(-0.08f, 0f, 0f), top + new Vector3(-0.08f, 0f, 0f), 0.04f);
            Box(mb, sport ? CarMat.Dark : CarMat.Paint, new Vector3(0f, r + 0.06f, 0.02f), new Vector3(0.12f, 0.03f, 0.42f), 8f);   // fender

            if (sport)
            {
                Box(mb, CarMat.Paint, top + new Vector3(0f, -0.05f, 0.30f), new Vector3(0.36f, 0.26f, 0.22f), -30f);   // nose fairing
                Box(mb, CarMat.Light, top + new Vector3(0.08f, -0.08f, 0.42f), new Vector3(0.09f, 0.04f, 0.03f), -30f); // twin headlights
                Box(mb, CarMat.Light, top + new Vector3(-0.08f, -0.08f, 0.42f), new Vector3(0.09f, 0.04f, 0.03f), -30f);
                Box(mb, CarMat.Glass, top + new Vector3(0f, 0.12f, 0.26f), new Vector3(0.26f, 0.16f, 0.01f), -40f);     // screen
                Box(mb, CarMat.Black, top + new Vector3(0f, 0.02f, 0.02f), new Vector3(0.52f, 0.03f, 0.03f));         // clip-ons
            }
            else
            {
                Box(mb, CarMat.Chrome, top + new Vector3(0f, 0.08f, 0.04f), new Vector3(0.62f, 0.025f, 0.025f));      // handlebar
                Box(mb, CarMat.Black, top + new Vector3(0.29f, 0.08f, 0.04f), new Vector3(0.1f, 0.035f, 0.035f));     // grips
                Box(mb, CarMat.Black, top + new Vector3(-0.29f, 0.08f, 0.04f), new Vector3(0.1f, 0.035f, 0.035f));
                Box(mb, CarMat.Chrome, top + new Vector3(0f, -0.06f, 0.14f), new Vector3(0.17f, 0.17f, 0.12f));       // round headlamp shell
                Box(mb, CarMat.Light, top + new Vector3(0f, -0.06f, 0.205f), new Vector3(0.14f, 0.14f, 0.01f));
                Box(mb, CarMat.Black, top + new Vector3(0f, 0.04f, 0.06f), new Vector3(0.09f, 0.05f, 0.05f));         // speedo
                Box(mb, CarMat.Chrome, top + new Vector3(0.24f, 0.18f, 0.02f), new Vector3(0.012f, 0.14f, 0.012f));   // mirrors
                Box(mb, CarMat.Chrome, top + new Vector3(-0.24f, 0.18f, 0.02f), new Vector3(0.012f, 0.14f, 0.012f));
                Box(mb, CarMat.Glass, top + new Vector3(0.24f, 0.26f, 0.02f), new Vector3(0.07f, 0.05f, 0.01f));
                Box(mb, CarMat.Glass, top + new Vector3(-0.24f, 0.26f, 0.02f), new Vector3(0.07f, 0.05f, 0.01f));
            }
            return mb.ToMesh(s.name + "_front");
        }

        /// <summary>Wheel facing +X: tyre, rim (chrome wire-spoke look on the commuter, alloy on the sports bike), hub.</summary>
        static Mesh BuildWheel(BikeSpec s, float tyre)
        {
            var mb = new MeshBuilder(3);
            float r = s.wheelRadius, hw = tyre * 0.5f, rr = r - tyre * 0.95f;
            mb.Lathe(0, new[]
            {
                new Vector2(-hw * 0.8f, rr), new Vector2(-hw, rr + (r - rr) * 0.4f), new Vector2(-hw * 0.75f, r * 0.98f),
                new Vector2(0f, r), new Vector2(hw * 0.75f, r * 0.98f), new Vector2(hw, rr + (r - rr) * 0.4f), new Vector2(hw * 0.8f, rr)
            }, 32);
            mb.Lathe(1, new[] { new Vector2(hw * 0.8f, rr), new Vector2(hw * 0.6f, rr - 0.02f), new Vector2(-hw * 0.6f, rr - 0.02f), new Vector2(-hw * 0.8f, rr) }, 32); // rim
            mb.Lathe(2, new[] { new Vector2(-0.06f, 0f), new Vector2(-0.06f, 0.07f), new Vector2(0.06f, 0.07f), new Vector2(0.06f, 0f) }, 16);   // hub / drum
            int spokes = s.style == BikeStyle.Sport ? 5 : 18;
            float thick = s.style == BikeStyle.Sport ? 0.035f : 0.004f;
            for (int k = 0; k < spokes; k++)
            {
                var rot = Quaternion.AngleAxis(k * 360f / spokes, Vector3.right);
                float side = s.style == BikeStyle.Sport ? 0f : (k % 2 == 0 ? 0.03f : -0.03f);
                mb.Box(1, new Vector3(side, 0f, 0f) + rot * new Vector3(0f, (0.07f + rr) * 0.5f, 0f),
                    new Vector3(thick, rr - 0.07f, s.style == BikeStyle.Sport ? 0.03f : 0.004f), rot);
            }
            return mb.ToMesh(s.name + "_wheel");
        }
    }
}
