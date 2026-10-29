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
            w.AddComponent<MeshFilter>().sharedMesh = Get($"{s.name}_wheel{tag}", () => BuildWheel(s, tyre, tag == "F"));
            var sh = ProceduralCar.Shared();
            w.AddComponent<MeshRenderer>().sharedMaterials = new[] { ProceduralCar.Rubber(), sh[(int)(s.style == BikeStyle.Sport ? CarMat.Dark : CarMat.Chrome)], sh[(int)CarMat.Silver] };
        }

        static Mesh Get(string key, System.Func<Mesh> build)
        {
            if (!Cache.TryGetValue(key, out var m)) Cache[key] = m = build();
            return m;
        }


        // ---------- geometry helpers ----------

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static Vector2 H(float w, float h) => new Vector2(w, h);
        static readonly float[] Sides = { -1f, 1f };

        /// <summary>Smooth loft along Z through rings (z, centre y, half width, half height): tanks, seats, fairings.</summary>
        static void Shell(MeshBuilder mb, CarMat m, float[] z, float[] cy, float[] hw, float[] hh, float squareness,
            float x = 0f, int seg = 20)
        {
            var path = new Vector3[z.Length];
            var half = new Vector2[z.Length];
            for (int i = 0; i < z.Length; i++) { path[i] = V(x, cy[i], z[i]); half[i] = H(hw[i], hh[i]); }
            mb.Sweep((int)m, path, half, seg, squareness, Vector3.right);
        }

        static void Pipe(MeshBuilder mb, CarMat m, float radius, params Vector3[] pts) => mb.Tube((int)m, pts, radius, 10);

        /// <summary>Points on an arc in the YZ plane; angles in degrees from +Z (forward) toward +Y (up).</summary>
        static Vector3[] Arc(Vector3 c, float radius, float a0, float a1, int n, float x = 0f)
        {
            var pts = new Vector3[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / (float)n) * Mathf.Deg2Rad;
                pts[i] = c + V(x, radius * Mathf.Sin(a), radius * Mathf.Cos(a));
            }
            return pts;
        }

        /// <summary>Thin curved shell around a wheel (mudguards).</summary>
        static void Fender(MeshBuilder mb, CarMat m, Vector3 axle, float radius, float a0, float a1, float halfWidth)
        {
            var path = Arc(axle, radius, a0, a1, 18);
            mb.Sweep((int)m, path, new[] { H(halfWidth, 0.006f) }, 16, 2.4f, Vector3.right);
        }

        /// <summary>Coil spring from a to b (shock absorbers).</summary>
        static void Spring(MeshBuilder mb, CarMat m, Vector3 a, Vector3 b, float coil, float wire, float turns)
        {
            Vector3 axis = b - a;
            Vector3 u = Vector3.right;
            Vector3 w = Vector3.Cross(axis.normalized, u);
            int n = Mathf.RoundToInt(turns * 10f);
            var pts = new Vector3[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n, ang = t * turns * Mathf.PI * 2f;
                pts[i] = a + axis * t + (u * Mathf.Cos(ang) + w * Mathf.Sin(ang)) * coil;
            }
            mb.Tube((int)m, pts, wire, 5);
        }

        /// <summary>Rear shock: chrome top cover, coil spring, damper body, mounting eyes.</summary>
        static void Shock(MeshBuilder mb, Vector3 top, Vector3 bottom, CarMat spring)
        {
            Pipe(mb, CarMat.Black, 0.013f, top, bottom);
            Pipe(mb, CarMat.Chrome, 0.026f, top + (bottom - top) * 0.05f, Vector3.Lerp(top, bottom, 0.32f));
            Spring(mb, spring, Vector3.Lerp(top, bottom, 0.34f), Vector3.Lerp(top, bottom, 0.94f), 0.025f, 0.0045f, 7f);
            mb.Tube((int)CarMat.Black, V(top.x - 0.015f, top.y, top.z), V(top.x + 0.015f, top.y, top.z), 0.016f, 8);
            mb.Tube((int)CarMat.Black, V(bottom.x - 0.015f, bottom.y, bottom.z), V(bottom.x + 0.015f, bottom.y, bottom.z), 0.016f, 8);
        }

        /// <summary>Round indicator on a short stalk pointing sideways.</summary>
        static void Indicator(MeshBuilder mb, Vector3 root, float side)
        {
            Vector3 tip = root + V(side * 0.1f, 0f, 0f);
            Pipe(mb, CarMat.Black, 0.008f, root, tip);
            mb.Sweep((int)CarMat.Amber, new[] { tip, tip + V(side * 0.025f, 0f, 0f), tip + V(side * 0.042f, 0f, 0f) },
                new[] { H(0.018f, 0.018f), H(0.024f, 0.024f), H(0.012f, 0.012f) }, 12, 2f, Vector3.forward);
        }

        /// <summary>Round mirror head on a stalk, glass facing the rider.</summary>
        static void Mirror(MeshBuilder mb, Vector3 root, Vector3 head, float w, float h, CarMat back)
        {
            Pipe(mb, CarMat.Black, 0.006f, root, Vector3.Lerp(root, head, 0.6f) + V(0f, 0.02f, 0f), head);
            mb.Sweep((int)back, new[] { head + V(0f, 0f, 0.035f), head + V(0f, 0f, 0.01f), head },
                new[] { H(w * 0.5f, h * 0.5f), H(w, h), H(w, h) }, 16, 2.4f, Vector3.right);
            mb.Sweep((int)CarMat.Glass, new[] { head + V(0f, 0f, -0.001f), head + V(0f, 0f, -0.004f) },
                new[] { H(w * 0.9f, h * 0.88f) }, 16, 2.4f, Vector3.right);
        }

        /// <summary>Drive chain looping round the gearbox sprocket and the rear sprocket.</summary>
        static void Chain(MeshBuilder mb, float x, Vector3 front, float rf, Vector3 rear, float rr)
        {
            var pts = new List<Vector3>();
            pts.AddRange(Arc(rear, rr, 90f, 270f, 12));
            pts.AddRange(Arc(front, rf, -90f, 90f, 8));
            pts.Add(pts[0]);
            for (int i = 0; i < pts.Count; i++) pts[i] = new Vector3(x, pts[i].y, pts[i].z);
            mb.Sweep((int)CarMat.Dark, pts, new[] { H(0.008f, 0.005f) }, 6, 3f, Vector3.right, false);
        }

        /// <summary>Thin annulus at x0 (brake discs, sprockets, drum plates), listed so normals face out.</summary>
        static void Disc(MeshBuilder mb, int sub, float x0, float inner, float outer, float halfThick, int seg = 32)
        {
            mb.LatheSmooth(sub, new[]
            {
                H(x0 - halfThick, inner), H(x0 - halfThick, outer), H(x0 + halfThick, outer), H(x0 + halfThick, inner), H(x0 - halfThick, inner)
            }, seg);
        }

        /// <summary>Pitch rotation built by hand (also runs in the offline mesh test harness).</summary>
        static Quaternion Pitch(float deg)
        {
            float h = deg * 0.5f * Mathf.Deg2Rad;
            return new Quaternion(Mathf.Sin(h), 0f, 0f, Mathf.Cos(h));
        }

        static Mesh BuildBody(BikeSpec s)
        {
            var mb = new MeshBuilder((int)CarMat.Count);
            if (s.style == BikeStyle.Commuter) CommuterBody(mb, s);
            else SportBody(mb, s);
            return mb.ToMesh(s.name + "_body");
        }

        /// <summary>
        /// 70cc commuter in the classic Pakistani style: tubular backbone frame, teardrop tank with chrome badge and
        /// knee rubbers, long flat seat, side covers, forward-tilted finned cylinder, chrome exhaust and twin chrome
        /// shocks, chrome carrier, half chain case, footrests, kick starter.
        /// </summary>
        static void CommuterBody(MeshBuilder mb, BikeSpec s)
        {
            float ra = s.rearAxle, r = s.wheelRadius;
            var head = V(0f, 0.92f, s.frontAxle - 0.22f);
            var rearAxle = V(0f, r, ra);

            // frame
            Pipe(mb, CarMat.Black, 0.03f, head, V(0f, 0.87f, 0.25f), V(0f, 0.81f, 0.06f), V(0f, 0.75f, -0.16f));
            Pipe(mb, CarMat.Black, 0.026f, head + V(0f, -0.05f, -0.01f), V(0f, 0.66f, 0.31f), V(0f, 0.52f, 0.27f));
            mb.Tube((int)CarMat.Black, V(0f, 0.92f, head.z + 0.02f), V(0f, 0.80f, head.z - 0.03f), 0.04f, 12);   // steering head
            foreach (float sx in Sides)
            {
                Pipe(mb, CarMat.Black, 0.016f, V(sx * 0.03f, 0.75f, -0.16f), V(sx * 0.09f, 0.75f, -0.30f), V(sx * 0.1f, 0.77f, -0.74f));
                Pipe(mb, CarMat.Black, 0.014f, V(sx * 0.09f, 0.47f, -0.08f), V(sx * 0.1f, 0.76f, -0.47f));
                Pipe(mb, CarMat.Black, 0.02f, V(sx * 0.1f, 0.42f, -0.06f), V(sx * 0.1f, r + 0.01f, ra));                     // swingarm
                Shock(mb, V(sx * 0.115f, 0.79f, -0.47f), V(sx * 0.115f, r + 0.035f, ra + 0.07f), CarMat.Chrome);
            }
            mb.Tube((int)CarMat.Black, V(-0.11f, 0.42f, -0.06f), V(0.11f, 0.42f, -0.06f), 0.022f, 10);
            mb.Tube((int)CarMat.Chrome, V(-0.12f, r, ra), V(0.12f, r, ra), 0.012f, 8);                              // axle

            // tank, badge, filler cap, knee rubbers
            Shell(mb, CarMat.Paint, new[] { 0.36f, 0.32f, 0.24f, 0.14f, 0.03f, -0.06f, -0.11f },
                new[] { 0.87f, 0.875f, 0.88f, 0.875f, 0.86f, 0.84f, 0.825f },
                new[] { 0.05f, 0.11f, 0.135f, 0.14f, 0.13f, 0.10f, 0.05f },
                new[] { 0.04f, 0.075f, 0.09f, 0.095f, 0.09f, 0.07f, 0.04f }, 2.6f, 0f, 24);
            foreach (float sx in Sides)
            {
                mb.Sweep((int)CarMat.Chrome, new[] { V(sx * 0.141f, 0.885f, 0.05f), V(sx * 0.141f, 0.888f, 0.22f) },
                    new[] { H(0.004f, 0.016f) }, 8, 4f, Vector3.right);
                mb.Sweep((int)CarMat.Black, new[] { V(sx * 0.128f, 0.835f, -0.04f), V(sx * 0.128f, 0.84f, 0.07f) },
                    new[] { H(0.012f, 0.035f) }, 10, 4f, Vector3.right);
            }
            mb.Sweep((int)CarMat.Chrome, new[] { V(0f, 0.962f, 0.19f), V(0f, 0.985f, 0.19f) }, new[] { H(0.033f, 0.033f) }, 14, 2f, Vector3.right);

            // long flat seat over the battery box and side covers
            Shell(mb, CarMat.Black, new[] { -0.06f, -0.12f, -0.25f, -0.40f, -0.55f, -0.64f, -0.67f },
                new[] { 0.79f, 0.777f, 0.772f, 0.776f, 0.785f, 0.79f, 0.79f },
                new[] { 0.06f, 0.12f, 0.135f, 0.135f, 0.13f, 0.11f, 0.06f },
                new[] { 0.025f, 0.045f, 0.05f, 0.05f, 0.05f, 0.045f, 0.03f }, 4f, 0f, 24);
            Shell(mb, CarMat.Black, new[] { -0.08f, -0.2f, -0.36f }, new[] { 0.63f, 0.63f, 0.64f },
                new[] { 0.09f, 0.1f, 0.09f }, new[] { 0.07f, 0.075f, 0.07f }, 4f);
            foreach (float sx in Sides)
                Shell(mb, CarMat.Paint, new[] { -0.06f, -0.15f, -0.3f, -0.4f }, new[] { 0.625f, 0.635f, 0.64f, 0.65f },
                    new[] { 0.012f, 0.018f, 0.018f, 0.012f }, new[] { 0.05f, 0.085f, 0.085f, 0.05f }, 3f, sx * 0.112f, 16);

            // rear mudguard, tail lamp, plate, indicators, chrome carrier
            Fender(mb, CarMat.Paint, rearAxle, r + 0.055f, 68f, 150f, 0.07f);
            Shell(mb, CarMat.Tail, new[] { -0.80f, -0.83f, -0.86f }, new[] { 0.72f, 0.72f, 0.72f },
                new[] { 0.055f, 0.06f, 0.05f }, new[] { 0.03f, 0.036f, 0.028f }, 4f, 0f, 16);
            mb.Box((int)CarMat.Plate, V(0f, 0.6f, ra - 0.31f), V(0.2f, 0.11f, 0.008f), Pitch(-18f));
            foreach (float sx in Sides) Indicator(mb, V(sx * 0.05f, 0.72f, -0.80f), sx);
            Pipe(mb, CarMat.Chrome, 0.011f, V(-0.1f, 0.835f, -0.56f), V(-0.11f, 0.835f, -0.80f), V(0.11f, 0.835f, -0.80f), V(0.1f, 0.835f, -0.56f));
            for (int i = 1; i <= 3; i++)
                mb.Tube((int)CarMat.Chrome, V(-0.105f, 0.835f, -0.56f - i * 0.06f), V(0.105f, 0.835f, -0.56f - i * 0.06f), 0.007f, 6);
            foreach (float sx in Sides)
                Pipe(mb, CarMat.Chrome, 0.009f, V(sx * 0.11f, 0.835f, -0.76f), V(sx * 0.1f, 0.765f, -0.70f));

            // engine: crankcase with side covers, finned cylinder tilted forward, head, plug, carburettor
            Shell(mb, CarMat.Silver, new[] { -0.13f, -0.05f, 0.06f, 0.15f }, new[] { 0.36f, 0.36f, 0.37f, 0.38f },
                new[] { 0.10f, 0.125f, 0.125f, 0.09f }, new[] { 0.07f, 0.10f, 0.10f, 0.07f }, 3f, 0f, 18);
            mb.Sweep((int)CarMat.Silver, new[] { V(0.1f, 0.36f, 0.0f), V(0.15f, 0.36f, 0.0f), V(0.162f, 0.36f, 0.0f) },
                new[] { H(0.08f, 0.08f), H(0.075f, 0.075f), H(0.05f, 0.05f) }, 18, 2f, Vector3.forward);
            mb.Sweep((int)CarMat.Silver, new[] { V(-0.1f, 0.37f, -0.02f), V(-0.145f, 0.37f, -0.02f), V(-0.155f, 0.37f, -0.02f) },
                new[] { H(0.065f, 0.065f), H(0.06f, 0.06f), H(0.04f, 0.04f) }, 18, 2f, Vector3.forward);
            var cylA = V(0f, 0.40f, 0.12f);
            var cylB = V(0f, 0.47f, 0.36f);
            Vector3 dir = (cylB - cylA).normalized;
            mb.Tube((int)CarMat.Silver, cylA, cylB, 0.045f, 14);
            for (int i = 0; i < 8; i++)
            {
                var c = Vector3.Lerp(cylA, cylB, 0.18f + i * 0.095f);
                float sz = i < 6 ? 0.085f : 0.075f;
                mb.Sweep((int)CarMat.Silver, new[] { c - dir * 0.0035f, c + dir * 0.0035f }, new[] { H(sz, sz * 0.92f) }, 14, 3.5f, Vector3.right);
            }
            mb.Sweep((int)CarMat.Silver, new[] { cylB, cylB + dir * 0.03f, cylB + dir * 0.05f },
                new[] { H(0.072f, 0.066f), H(0.07f, 0.064f), H(0.05f, 0.045f) }, 16, 3f, Vector3.right);
            Pipe(mb, CarMat.Black, 0.01f, cylB + V(0f, 0.05f, 0.0f), cylB + V(0f, 0.11f, -0.02f));
            Shell(mb, CarMat.Black, new[] { 0.02f, 0.08f, 0.12f }, new[] { 0.5f, 0.505f, 0.5f },
                new[] { 0.04f, 0.045f, 0.035f }, new[] { 0.03f, 0.035f, 0.03f }, 2.5f, 0.05f, 12);

            // chrome exhaust under the engine to a long muffler on the right, black heat shield
            Pipe(mb, CarMat.Chrome, 0.021f, V(0.02f, 0.42f, 0.37f), V(0.05f, 0.35f, 0.41f), V(0.09f, 0.27f, 0.36f),
                V(0.12f, 0.235f, 0.22f), V(0.14f, 0.25f, 0.0f), V(0.15f, 0.28f, -0.22f));
            mb.Sweep((int)CarMat.Chrome, new[] { V(0.15f, 0.28f, -0.22f), V(0.155f, 0.29f, -0.31f), V(0.16f, 0.31f, -0.55f), V(0.165f, 0.33f, -0.78f), V(0.165f, 0.335f, -0.82f) },
                new[] { H(0.022f, 0.022f), H(0.046f, 0.046f), H(0.05f, 0.05f), H(0.045f, 0.045f), H(0.03f, 0.03f) }, 16, 2f, Vector3.right);
            mb.Sweep((int)CarMat.Black, new[] { V(0.168f, 0.35f, -0.30f), V(0.172f, 0.355f, -0.48f) }, new[] { H(0.02f, 0.03f) }, 10, 3f, Vector3.right);
            Pipe(mb, CarMat.Dark, 0.016f, V(0.165f, 0.335f, -0.815f), V(0.165f, 0.337f, -0.835f));

            // drive chain on the left, half chain case over its top run
            Chain(mb, -0.095f, V(0f, 0.36f, -0.02f), 0.04f, rearAxle, 0.085f);
            mb.Sweep((int)CarMat.Black, new[] { V(-0.1f, 0.425f, 0.02f), V(-0.1f, 0.41f, -0.25f), V(-0.1f, 0.395f, -0.5f), V(-0.1f, 0.39f, -0.6f) },
                new[] { H(0.012f, 0.035f), H(0.012f, 0.04f), H(0.012f, 0.04f), H(0.012f, 0.03f) }, 10, 4f, Vector3.right);

            // controls: footrest bar with rubbers, pillion pegs, kick starter, gear and brake levers, side stand
            mb.Tube((int)CarMat.Chrome, V(-0.25f, 0.30f, -0.02f), V(0.25f, 0.30f, -0.02f), 0.011f, 8);
            foreach (float sx in Sides)
            {
                mb.Tube((int)CarMat.Black, V(sx * 0.15f, 0.30f, -0.02f), V(sx * 0.265f, 0.30f, -0.02f), 0.02f, 10);
                mb.Tube((int)CarMat.Black, V(sx * 0.12f, 0.42f, -0.42f), V(sx * 0.22f, 0.42f, -0.42f), 0.014f, 8);
            }
            Pipe(mb, CarMat.Chrome, 0.01f, V(0.16f, 0.4f, -0.08f), V(0.2f, 0.47f, -0.28f));
            mb.Tube((int)CarMat.Black, V(0.2f, 0.47f, -0.28f), V(0.26f, 0.47f, -0.28f), 0.014f, 8);
            Pipe(mb, CarMat.Black, 0.008f, V(-0.15f, 0.3f, 0.02f), V(-0.17f, 0.33f, 0.14f));
            Pipe(mb, CarMat.Black, 0.008f, V(0.15f, 0.3f, 0.02f), V(0.16f, 0.29f, 0.15f));
            Pipe(mb, CarMat.Black, 0.012f, V(-0.1f, 0.32f, -0.06f), V(-0.14f, 0.27f, -0.36f));
        }

        /// <summary>
        /// 600cc-class supersport: twin-spar aluminium frame, banana swingarm, full fairing with twin headlamps and
        /// screen (frame-mounted, it does not steer), sculpted tank, split seat with tail cowl, inline-four engine,
        /// four-into-one exhaust with a mid can, chain, rearsets.
        /// </summary>
        static void SportBody(MeshBuilder mb, BikeSpec s)
        {
            float ra = s.rearAxle, r = s.wheelRadius;
            var rearAxle = V(0f, r, ra);
            var head = V(0f, 0.86f, s.frontAxle - 0.22f);

            // twin-spar frame, subframe, banana swingarm
            foreach (float sx in Sides)
            {
                mb.Sweep((int)CarMat.Silver, new[] { V(sx * 0.06f, 0.88f, head.z - 0.01f), V(sx * 0.13f, 0.84f, 0.30f), V(sx * 0.15f, 0.74f, 0.05f), V(sx * 0.14f, 0.58f, -0.16f), V(sx * 0.12f, 0.46f, -0.22f) },
                    new[] { H(0.018f, 0.045f) }, 12, 4f, Vector3.right);
                Pipe(mb, CarMat.Dark, 0.012f, V(sx * 0.1f, 0.72f, -0.14f), V(sx * 0.055f, 0.86f, -0.68f));
                Pipe(mb, CarMat.Dark, 0.012f, V(sx * 0.1f, 0.55f, -0.2f), V(sx * 0.055f, 0.82f, -0.6f));
                mb.Sweep((int)CarMat.Silver, new[] { V(sx * 0.11f, 0.46f, -0.22f), V(sx * 0.12f, 0.42f, -0.45f), V(sx * 0.115f, r, ra) },
                    new[] { H(0.025f, 0.055f), H(0.025f, 0.05f), H(0.02f, 0.035f) }, 12, 4f, Vector3.right);
                mb.Box((int)CarMat.Dark, V(sx * 0.115f, r, ra), V(0.03f, 0.05f, 0.07f), Quaternion.identity);
            }
            mb.Tube((int)CarMat.Silver, V(-0.12f, 0.43f, -0.33f), V(0.12f, 0.43f, -0.33f), 0.02f, 10);
            mb.Tube((int)CarMat.Silver, V(-0.13f, r, ra), V(0.13f, r, ra), 0.012f, 8);
            mb.Tube((int)CarMat.Dark, V(0f, 0.88f, head.z + 0.02f), V(0f, 0.72f, head.z - 0.04f), 0.045f, 12);

            // inline-four engine with side covers
            Shell(mb, CarMat.Dark, new[] { -0.18f, -0.05f, 0.10f, 0.18f }, new[] { 0.40f, 0.40f, 0.42f, 0.45f },
                new[] { 0.14f, 0.17f, 0.17f, 0.14f }, new[] { 0.10f, 0.12f, 0.12f, 0.10f }, 3.5f, 0f, 18);
            Shell(mb, CarMat.Dark, new[] { 0.08f, 0.2f, 0.3f }, new[] { 0.52f, 0.6f, 0.66f },
                new[] { 0.15f, 0.16f, 0.14f }, new[] { 0.08f, 0.09f, 0.07f }, 4f, 0f, 16);
            mb.Sweep((int)CarMat.Silver, new[] { V(0.165f, 0.4f, -0.05f), V(0.19f, 0.4f, -0.05f), V(0.198f, 0.4f, -0.05f) },
                new[] { H(0.09f, 0.09f), H(0.085f, 0.085f), H(0.06f, 0.06f) }, 18, 2f, Vector3.forward);
            mb.Sweep((int)CarMat.Silver, new[] { V(-0.165f, 0.42f, 0.04f), V(-0.19f, 0.42f, 0.04f), V(-0.198f, 0.42f, 0.04f) },
                new[] { H(0.07f, 0.07f), H(0.065f, 0.065f), H(0.045f, 0.045f) }, 18, 2f, Vector3.forward);

            // four-into-one exhaust: headers sweep under the engine to a mid-mounted can on the right
            foreach (float hx in new[] { -0.09f, -0.03f, 0.03f, 0.09f })
                Pipe(mb, CarMat.Silver, 0.018f, V(hx, 0.6f, 0.33f), V(hx, 0.46f, 0.42f), V(hx * 0.9f, 0.31f, 0.38f), V(hx * 0.6f, 0.23f, 0.22f), V(hx * 0.3f, 0.215f, 0.05f));
            Pipe(mb, CarMat.Silver, 0.03f, V(0f, 0.215f, 0.06f), V(0.05f, 0.225f, -0.12f), V(0.12f, 0.30f, -0.32f));
            mb.Sweep((int)CarMat.Dark, new[] { V(0.12f, 0.30f, -0.31f), V(0.135f, 0.33f, -0.36f), V(0.15f, 0.40f, -0.52f), V(0.16f, 0.46f, -0.66f), V(0.162f, 0.465f, -0.68f) },
                new[] { H(0.03f, 0.03f), H(0.05f, 0.06f), H(0.06f, 0.07f), H(0.055f, 0.065f), H(0.04f, 0.045f) }, 16, 3f, Vector3.right);
            Pipe(mb, CarMat.Silver, 0.028f, V(0.162f, 0.465f, -0.675f), V(0.163f, 0.47f, -0.695f));

            // fairing: nose cowl, side panels (paint), belly pan; screen, twin headlamps, mirrors, dash
            Shell(mb, CarMat.Paint, new[] { 0.87f, 0.83f, 0.75f, 0.65f, 0.55f, 0.44f },
                new[] { 0.81f, 0.82f, 0.82f, 0.81f, 0.79f, 0.76f },
                new[] { 0.04f, 0.12f, 0.18f, 0.21f, 0.225f, 0.23f },
                new[] { 0.04f, 0.10f, 0.14f, 0.16f, 0.17f, 0.17f }, 2.4f, 0f, 24);
            Shell(mb, CarMat.Paint, new[] { 0.42f, 0.34f, 0.24f, 0.12f, 0.0f, -0.08f },
                new[] { 0.68f, 0.60f, 0.55f, 0.52f, 0.51f, 0.52f },
                new[] { 0.2f, 0.23f, 0.235f, 0.22f, 0.19f, 0.12f },
                new[] { 0.10f, 0.18f, 0.22f, 0.21f, 0.16f, 0.08f }, 2.6f, 0f, 24);
            Shell(mb, CarMat.Black, new[] { 0.30f, 0.18f, 0.02f, -0.10f }, new[] { 0.33f, 0.29f, 0.28f, 0.30f },
                new[] { 0.12f, 0.16f, 0.15f, 0.1f }, new[] { 0.04f, 0.06f, 0.06f, 0.04f }, 3f, 0f, 18);
            foreach (float sx in Sides)
            {
                mb.Sweep((int)CarMat.Black, new[] { V(sx * 0.232f, 0.6f, 0.30f), V(sx * 0.236f, 0.53f, 0.18f) }, new[] { H(0.006f, 0.022f) }, 8, 4f, Vector3.right);
                mb.Sweep((int)CarMat.Black, new[] { V(sx * 0.232f, 0.55f, 0.27f), V(sx * 0.236f, 0.48f, 0.15f) }, new[] { H(0.006f, 0.018f) }, 8, 4f, Vector3.right);
                mb.Sweep((int)CarMat.Black, new[] { V(sx * 0.07f, 0.785f, 0.79f), V(sx * 0.095f, 0.78f, 0.85f) },
                    new[] { H(0.05f, 0.032f), H(0.048f, 0.03f) }, 16, 2.6f, Vector3.right);
                mb.Sweep((int)CarMat.Light, new[] { V(sx * 0.095f, 0.78f, 0.85f), V(sx * 0.1f, 0.78f, 0.858f), V(sx * 0.102f, 0.78f, 0.861f) },
                    new[] { H(0.044f, 0.026f), H(0.036f, 0.02f), H(0.02f, 0.01f) }, 16, 2.6f, Vector3.right);
                Mirror(mb, V(sx * 0.2f, 0.88f, 0.6f), V(sx * 0.3f, 0.93f, 0.55f), 0.06f, 0.035f, CarMat.Paint);
                mb.Sweep((int)CarMat.Amber, new[] { V(sx * 0.33f, 0.93f, 0.575f), V(sx * 0.36f, 0.93f, 0.57f) }, new[] { H(0.008f, 0.012f) }, 8, 3f, Vector3.forward);
            }
            mb.Sweep((int)CarMat.Glass, new[] { V(0f, 0.92f, 0.77f), V(0f, 0.99f, 0.69f), V(0f, 1.045f, 0.60f) },
                new[] { H(0.12f, 0.006f), H(0.13f, 0.006f), H(0.11f, 0.006f) }, 16, 2.2f, Vector3.right);
            mb.Sweep((int)CarMat.Black, new[] { V(0f, 0.92f, 0.64f), V(0f, 0.95f, 0.61f) }, new[] { H(0.075f, 0.045f) }, 14, 3f, Vector3.right);
            mb.Sweep((int)CarMat.Glass, new[] { V(0f, 0.951f, 0.609f), V(0f, 0.953f, 0.607f) }, new[] { H(0.065f, 0.036f) }, 14, 3f, Vector3.right);

            // tank, rider seat, tail cowl with pillion pad, tail light, plate hanger, hugger
            Shell(mb, CarMat.Paint, new[] { 0.36f, 0.30f, 0.20f, 0.08f, -0.04f, -0.12f },
                new[] { 0.88f, 0.92f, 0.94f, 0.93f, 0.89f, 0.85f },
                new[] { 0.12f, 0.17f, 0.19f, 0.18f, 0.15f, 0.10f },
                new[] { 0.06f, 0.08f, 0.09f, 0.09f, 0.07f, 0.05f }, 2.6f, 0f, 24);
            mb.Sweep((int)CarMat.Silver, new[] { V(0f, 1.025f, 0.24f), V(0f, 1.04f, 0.24f) }, new[] { H(0.04f, 0.04f) }, 14, 2f, Vector3.right);
            Shell(mb, CarMat.Black, new[] { -0.08f, -0.14f, -0.26f, -0.38f, -0.44f }, new[] { 0.83f, 0.82f, 0.82f, 0.83f, 0.85f },
                new[] { 0.08f, 0.13f, 0.14f, 0.13f, 0.09f }, new[] { 0.03f, 0.04f, 0.045f, 0.045f, 0.035f }, 4f, 0f, 20);
            Shell(mb, CarMat.Paint, new[] { -0.36f, -0.46f, -0.58f, -0.70f, -0.80f, -0.86f },
                new[] { 0.80f, 0.84f, 0.88f, 0.91f, 0.93f, 0.94f },
                new[] { 0.13f, 0.12f, 0.10f, 0.075f, 0.05f, 0.03f },
                new[] { 0.07f, 0.08f, 0.07f, 0.06f, 0.045f, 0.03f }, 2.6f, 0f, 20);
            Shell(mb, CarMat.Black, new[] { -0.44f, -0.53f, -0.63f }, new[] { 0.90f, 0.925f, 0.945f },
                new[] { 0.08f, 0.085f, 0.065f }, new[] { 0.02f, 0.02f, 0.018f }, 4f, 0f, 16);
            mb.Sweep((int)CarMat.Tail, new[] { V(0f, 0.92f, -0.83f), V(0f, 0.935f, -0.875f) }, new[] { H(0.05f, 0.018f) }, 14, 4f, Vector3.right);
            Pipe(mb, CarMat.Dark, 0.012f, V(0f, 0.80f, -0.74f), V(0f, 0.7f, -0.84f), V(0f, 0.62f, -0.88f));
            mb.Box((int)CarMat.Plate, V(0f, 0.6f, -0.89f), V(0.18f, 0.1f, 0.006f), Pitch(-15f));
            foreach (float sx in Sides) Indicator(mb, V(sx * 0.02f, 0.7f, -0.84f), sx);
            Fender(mb, CarMat.Dark, rearAxle, r + 0.04f, 75f, 140f, 0.08f);

            // chain, rearsets, pillion pegs, gear lever, side stand
            Chain(mb, -0.11f, V(0f, 0.40f, -0.14f), 0.045f, rearAxle, 0.1f);
            foreach (float sx in Sides)
            {
                mb.Box((int)CarMat.Silver, V(sx * 0.15f, 0.44f, -0.2f), V(0.012f, 0.12f, 0.1f), Pitch(20f));
                mb.Tube((int)CarMat.Silver, V(sx * 0.155f, 0.40f, -0.18f), V(sx * 0.24f, 0.40f, -0.18f), 0.011f, 8);
                mb.Tube((int)CarMat.Silver, V(sx * 0.12f, 0.47f, -0.42f), V(sx * 0.21f, 0.47f, -0.42f), 0.01f, 8);
            }
            Pipe(mb, CarMat.Silver, 0.007f, V(-0.16f, 0.4f, -0.16f), V(-0.18f, 0.42f, -0.06f));
            Pipe(mb, CarMat.Dark, 0.012f, V(-0.12f, 0.33f, -0.12f), V(-0.16f, 0.27f, -0.4f));
        }

        // ---------- front (steers) ----------

        /// <summary>Front assembly in front-pose space (origin = front axle): forks, yokes, bars, mudguard, lamp.</summary>
        static Mesh BuildFront(BikeSpec s)
        {
            var mb = new MeshBuilder((int)CarMat.Count);
            FrontParts(mb, s);
            return mb.ToMesh(s.name + "_front");
        }

        static void FrontParts(MeshBuilder mb, BikeSpec s)
        {
            float r = s.wheelRadius;
            bool sport = s.style == BikeStyle.Sport;
            float topY = sport ? 0.62f : 0.66f;
            Vector3 Leg(float x, float t) => V(x, -0.02f + (topY + 0.02f) * t, -0.20f * t);

            foreach (float sx in Sides)
            {
                float x = sx * (sport ? 0.085f : 0.075f);
                if (sport)
                {
                    // upside-down fork: thin chrome sliders below, fat dark tubes above, axle clamp, radial calipers
                    mb.Tube((int)CarMat.Chrome, Leg(x, 0.02f), Leg(x, 0.5f), 0.024f, 12);
                    mb.Tube((int)CarMat.Dark, Leg(x, 0.42f), Leg(x, 1f), 0.033f, 14);
                    mb.Box((int)CarMat.Dark, Leg(x, 0.02f), V(0.05f, 0.08f, 0.07f), Quaternion.identity);
                    mb.Box((int)CarMat.Dark, V(sx * 0.072f, 0.12f, -0.085f), V(0.035f, 0.1f, 0.055f), Pitch(-30f));
                }
                else
                {
                    // telescopic fork: polished lower legs, painted upper covers, chrome headlamp brackets
                    mb.Tube((int)CarMat.Silver, Leg(x, 0f), Leg(x, 0.48f), 0.026f, 12);
                    mb.Tube((int)CarMat.Paint, Leg(x, 0.46f), Leg(x, 1f), 0.033f, 14);
                    mb.Tube((int)CarMat.Silver, Leg(x, -0.03f) + V(0f, 0f, 0.02f), Leg(x, 0.06f) + V(0f, 0f, 0.02f), 0.03f, 10);
                    Pipe(mb, CarMat.Chrome, 0.008f, Leg(x, 0.88f), V(sx * 0.068f, 0.56f, -0.06f));
                }
            }
            float yokeX = sport ? 0.12f : 0.105f;
            mb.Sweep((int)CarMat.Silver, new[] { Leg(-yokeX, 0.75f), Leg(yokeX, 0.75f) }, new[] { H(0.035f, 0.02f) }, 10, 4f, Vector3.forward);
            mb.Sweep((int)CarMat.Silver, new[] { Leg(-yokeX, 0.97f), Leg(yokeX, 0.97f) }, new[] { H(0.04f, 0.014f) }, 10, 4f, Vector3.forward);
            mb.Tube((int)CarMat.Chrome, V(-0.1f, 0f, 0f), V(0.1f, 0f, 0f), 0.011f, 8);   // axle

            if (sport)
            {
                Fender(mb, CarMat.Paint, Vector3.zero, r + 0.035f, 40f, 140f, 0.06f);
                // clip-on bars below the top yoke, grips, levers
                foreach (float sx in Sides)
                {
                    Pipe(mb, CarMat.Black, 0.012f, Leg(sx * 0.085f, 0.95f), V(sx * 0.3f, 0.61f, -0.225f));
                    mb.Tube((int)CarMat.Black, V(sx * 0.2f, 0.613f, -0.206f), V(sx * 0.31f, 0.61f, -0.226f), 0.017f, 10);
                    Pipe(mb, CarMat.Silver, 0.006f, V(sx * 0.17f, 0.618f, -0.19f), V(sx * 0.25f, 0.62f, -0.15f), V(sx * 0.3f, 0.615f, -0.16f));
                }
            }
            else
            {
                Fender(mb, CarMat.Paint, Vector3.zero, r + 0.045f, 28f, 160f, 0.065f);
                foreach (float sx in Sides)
                    Pipe(mb, CarMat.Chrome, 0.006f, Arc(Vector3.zero, r + 0.045f, 60f, 61f, 1, sx * 0.068f)[0], Leg(sx * 0.075f, 0.42f));

                // round chrome headlamp, glass lens, speedometer, front indicators
                mb.Sweep((int)CarMat.Chrome, new[] { V(0f, 0.56f, -0.10f), V(0f, 0.56f, -0.04f), V(0f, 0.56f, 0.02f), V(0f, 0.56f, 0.055f) },
                    new[] { H(0.05f, 0.05f), H(0.083f, 0.083f), H(0.09f, 0.09f), H(0.088f, 0.088f) }, 22, 2f, Vector3.right);
                mb.Sweep((int)CarMat.Light, new[] { V(0f, 0.56f, 0.055f), V(0f, 0.56f, 0.068f), V(0f, 0.56f, 0.074f) },
                    new[] { H(0.08f, 0.08f), H(0.072f, 0.072f), H(0.05f, 0.05f) }, 22, 2f, Vector3.right);
                mb.Sweep((int)CarMat.Black, new[] { V(0f, 0.655f, -0.1f), V(0f, 0.695f, -0.14f) }, new[] { H(0.045f, 0.045f) }, 16, 2f, Vector3.right);
                mb.Sweep((int)CarMat.Plate, new[] { V(0f, 0.696f, -0.141f), V(0f, 0.699f, -0.144f) }, new[] { H(0.037f, 0.037f) }, 16, 2f, Vector3.right);
                foreach (float sx in Sides) Indicator(mb, V(sx * 0.07f, 0.565f, -0.05f), sx);

                // swept-back chrome handlebar, grips, levers, switch pods, round mirrors
                Pipe(mb, CarMat.Chrome, 0.011f, V(-0.34f, 0.746f, -0.17f), V(-0.22f, 0.74f, -0.15f), V(-0.12f, 0.715f, -0.105f), V(0f, 0.705f, -0.095f),
                    V(0.12f, 0.715f, -0.105f), V(0.22f, 0.74f, -0.15f), V(0.34f, 0.746f, -0.17f));
                Pipe(mb, CarMat.Chrome, 0.014f, Leg(0f, 0.97f), V(0f, 0.705f, -0.095f));
                foreach (float sx in Sides)
                {
                    mb.Tube((int)CarMat.Black, V(sx * 0.25f, 0.742f, -0.155f), V(sx * 0.355f, 0.747f, -0.172f), 0.018f, 10);
                    Pipe(mb, CarMat.Chrome, 0.006f, V(sx * 0.2f, 0.748f, -0.14f), V(sx * 0.27f, 0.742f, -0.10f), V(sx * 0.33f, 0.738f, -0.115f));
                    mb.Sweep((int)CarMat.Black, new[] { V(sx * 0.205f, 0.742f, -0.148f), V(sx * 0.235f, 0.742f, -0.152f) }, new[] { H(0.022f, 0.022f) }, 10, 3f, Vector3.forward);
                    Mirror(mb, V(sx * 0.2f, 0.755f, -0.15f), V(sx * 0.27f, 0.95f, -0.14f), 0.05f, 0.04f, CarMat.Black);
                }
            }
        }

        // ---------- wheels ----------

        /// <summary>
        /// Wheel facing +X. Submeshes: 0 tyre (rounded profile), 1 rim + spokes (chrome laced wire spokes on the
        /// commuter, dark split-spoke alloy on the sports bike), 2 metal (drum hub, disc rotors, sprocket).
        /// </summary>
        static Mesh BuildWheel(BikeSpec s, float tyre, bool front)
        {
            var mb = new MeshBuilder(3);
            WheelParts(mb, s, tyre, front);
            return mb.ToMesh(s.name + "_wheel");
        }

        static void WheelParts(MeshBuilder mb, BikeSpec s, float tyre, bool front)
        {
            float r = s.wheelRadius, hw = tyre * 0.5f, hh = tyre * 0.475f, rr = r - hh * 2f;
            bool sport = s.style == BikeStyle.Sport;

            // tyre: rounded section from bead to bead
            var tyreProfile = new List<Vector2> { H(-hw * 0.8f, rr) };
            for (int k = 0; k <= 12; k++)
            {
                float phi = Mathf.Lerp(-110f, 110f, k / 12f) * Mathf.Deg2Rad;
                tyreProfile.Add(H(hw * Mathf.Sin(phi), r - hh + hh * Mathf.Cos(phi)));
            }
            tyreProfile.Add(H(hw * 0.8f, rr));
            mb.LatheSmooth(0, tyreProfile.ToArray(), 40);

            // rim channel (listed right flange → inside → left flange so normals face out)
            float rw = hw * 0.85f;
            mb.LatheSmooth(1, new[]
            {
                H(rw, rr + 0.008f), H(rw, rr - 0.012f), H(rw * 0.6f, rr - 0.024f), H(-rw * 0.6f, rr - 0.024f),
                H(-rw, rr - 0.012f), H(-rw, rr + 0.008f)
            }, 40);

            if (!sport)
            {
                // drum hub with its brake plate (left at the front, right at the back) and 36 laced spokes
                mb.LatheSmooth(2, new[] { H(-0.055f, 0.016f), H(-0.055f, 0.05f), H(-0.04f, 0.056f), H(0.04f, 0.056f), H(0.055f, 0.05f), H(0.055f, 0.016f) }, 20);
                Disc(mb, 2, front ? -0.062f : 0.062f, 0.016f, 0.074f, 0.007f, 24);
                for (int k = 0; k < 36; k++)
                {
                    float side = k % 2 == 0 ? 1f : -1f;
                    float a = k * 10f * Mathf.Deg2Rad;
                    float off = ((k / 2) % 2 == 0 ? 1f : -1f) * 26f * Mathf.Deg2Rad;
                    var hub = V(side * 0.042f, 0.052f * Mathf.Cos(a), 0.052f * Mathf.Sin(a));
                    var rim = V(side * 0.006f, (rr - 0.022f) * Mathf.Cos(a + off), (rr - 0.022f) * Mathf.Sin(a + off));
                    mb.Tube(1, new[] { hub, rim }, 0.0022f, 4, false);
                }
                if (!front) Disc(mb, 2, -0.09f, 0.03f, 0.085f, 0.003f);   // rear sprocket
            }
            else
            {
                // alloy: hub and three split spokes
                mb.LatheSmooth(1, new[] { H(-0.07f, 0.02f), H(-0.07f, 0.06f), H(0.07f, 0.06f), H(0.07f, 0.02f) }, 18);
                for (int k = 0; k < 3; k++)
                    foreach (float split in Sides)
                    {
                        float a = k * 120f * Mathf.Deg2Rad, b = (k * 120f + split * 14f) * Mathf.Deg2Rad, m = (a + b) * 0.5f;
                        mb.Sweep(1, new[]
                            {
                                V(0f, 0.055f * Mathf.Cos(a), 0.055f * Mathf.Sin(a)),
                                V(0f, 0.15f * Mathf.Cos(m), 0.15f * Mathf.Sin(m)),
                                V(0f, (rr - 0.018f) * Mathf.Cos(b), (rr - 0.018f) * Mathf.Sin(b))
                            },
                            new[] { H(0.02f, 0.012f), H(0.012f, 0.008f), H(0.014f, 0.009f) }, 8, 3f, Vector3.right);
                    }
                // disc rotors (two at the front, one at the back) on short carriers, rear sprocket
                foreach (float x0 in front ? new[] { -0.072f, 0.072f } : new[] { 0.075f })
                {
                    float outer = front ? 0.155f : 0.11f, inner = outer - 0.04f;
                    Disc(mb, 2, x0, inner, outer, 0.0025f, 36);
                    for (int k = 0; k < 5; k++)
                    {
                        float a = k * 72f * Mathf.Deg2Rad;
                        mb.Tube(2, V(x0 * 0.85f, 0.055f * Mathf.Cos(a), 0.055f * Mathf.Sin(a)), V(x0, inner * Mathf.Cos(a), inner * Mathf.Sin(a)), 0.008f, 5);
                    }
                }
                if (!front) Disc(mb, 2, -0.11f, 0.04f, 0.1f, 0.003f, 36);
            }
        }
    }
}
