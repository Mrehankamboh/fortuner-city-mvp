using System.Collections.Generic;
using UnityEngine;

namespace FortunerCity.Vehicle
{
    public enum CarMat { Paint, Black, Glass, Chrome, Light, Tail, Plate, Dark, Silver, Count }

    /// <summary>
    /// Real-world description of one car model. Body = side profile extruded across the width with a
    /// shoulder line (belt) and narrower glasshouse; details (grille, lights…) come from a delegate.
    /// Coordinates: z forward (body spans ±length/2), y up from the road, x right.
    /// </summary>
    public class CarSpec
    {
        public string name;
        public float length;
        public float frontAxle, rearAxle, halfTrack;
        public float wheelRadius, tyreWidth, rimRatio = 0.64f;
        public int spokes = 6;
        public bool twinSpokes = true;
        public CarMat rimMat = CarMat.Silver;
        public Color caliper = new Color(0.7f, 0.05f, 0.05f);
        public float sill, belt, roof, archExtra = 0.08f;
        public float wLow, wBelt, wRoof, taper = 0.09f, taperLength = 0.30f;
        public float rearUnderZ;
        /// <summary>Front underside → nose → hood → windshield → roof → tailgate → rear lower corner.</summary>
        public Vector2[] top;
        public int windshieldA, rearGlassA;      // glass lies on top edge (A, A+1)
        public Vector2[] sideWindow;
        public float[] pillars = new float[0];   // blacked-out pillar centres (z); [0] = B-pillar
        public bool roofRails, sideSteps, chromeBelt = true;
        public float plateFrontY, plateRearY;
        public Color[] palette;
        public float trafficWeight = 1f;
        public float topSpeedKmh = 175f;
        public float accel = 26f;
        public EngineSound engine = EngineSound.Petrol4;
        public System.Action<CarDetailer> details;

        public float HalfLength => length * 0.5f;
        public float ArchR => wheelRadius + archExtra;

        public float Taper(float z) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(HalfLength - taperLength, HalfLength + 0.01f, Mathf.Abs(z))) * taper;

        public float WLow(float y, float z) =>
            (y < sill + 0.13f ? Mathf.Lerp(wLow - 0.045f, wLow, Mathf.InverseLerp(sill, sill + 0.13f, y)) : wLow) - Taper(z);

        public float WUp(float y, float z) =>
            Mathf.Lerp(wBelt, wRoof, Mathf.InverseLerp(belt, roof, y)) - Taper(z) * 0.6f;

        public float W(float y, float z) => y > belt ? WUp(y, z) : WLow(y, z);
    }

    /// <summary>Helpers the per-car detail delegates use to place trim on the body surface.</summary>
    public class CarDetailer
    {
        readonly MeshBuilder _mb;
        public readonly CarSpec spec;

        internal CarDetailer(MeshBuilder mb, CarSpec s) { _mb = mb; spec = s; }

        public void Box(CarMat m, Vector3 pos, Vector3 size, Vector3 euler = default) =>
            _mb.Box((int)m, pos, size, Quaternion.Euler(euler));

        /// <summary>Right-hand part plus its mirror (x, yaw and roll mirrored).</summary>
        public void Pair(CarMat m, Vector3 pos, Vector3 size, Vector3 euler = default)
        {
            Box(m, pos, size, euler);
            Box(m, new Vector3(-pos.x, pos.y, pos.z), size, new Vector3(euler.x, -euler.y, -euler.z));
        }

        /// <summary>Z of the front face at height y.</summary>
        public float FrontZ(float y)
        {
            float best = 0f;
            var t = spec.top;
            for (int i = 0; i < t.Length - 1; i++)
            {
                Vector2 a = t[i], b = t[i + 1];
                if (a.x <= 0f || b.x <= 0f) continue;
                if ((y - a.y) * (y - b.y) > 0f) continue;
                float k = Mathf.Abs(b.y - a.y) < 1e-5f ? 0f : (y - a.y) / (b.y - a.y);
                best = Mathf.Max(best, Mathf.Lerp(a.x, b.x, k));
            }
            return best;
        }

        /// <summary>Z of the rear face at height y.</summary>
        public float RearZ(float y)
        {
            float best = 0f;
            var t = spec.top;
            for (int i = 0; i < t.Length - 1; i++)
            {
                Vector2 a = t[i], b = t[i + 1];
                if (a.x >= 0f || b.x >= 0f) continue;
                if ((y - a.y) * (y - b.y) > 0f) continue;
                float k = Mathf.Abs(b.y - a.y) < 1e-5f ? 0f : (y - a.y) / (b.y - a.y);
                best = Mathf.Min(best, Mathf.Lerp(a.x, b.x, k));
            }
            return best;
        }

        public void Front(CarMat m, float x, float y, Vector3 size, float yaw = 0f, float push = 0f, float roll = 0f)
        {
            var pos = new Vector3(x, y, FrontZ(y) + size.z * 0.25f + push);
            if (Mathf.Abs(x) < 1e-4f) Box(m, pos, size, new Vector3(0f, yaw, roll));
            else Pair(m, pos, size, new Vector3(0f, yaw, roll));
        }

        public void Rear(CarMat m, float x, float y, Vector3 size, float yaw = 0f, float push = 0f, float roll = 0f)
        {
            var pos = new Vector3(x, y, RearZ(y) - size.z * 0.25f - push);
            if (Mathf.Abs(x) < 1e-4f) Box(m, pos, size, new Vector3(0f, yaw, roll));
            else Pair(m, pos, size, new Vector3(0f, yaw, roll));
        }

        /// <summary>Mirrored part on the body side at (y, z).</summary>
        public void Side(CarMat m, float y, float z, Vector3 size, float push = 0f)
        {
            Pair(m, new Vector3(spec.W(y, z) + size.x * 0.25f + push, y, z), size);
        }

        public void Headlights(float x, float y, float w, float h, float yaw)
        {
            Front(CarMat.Light, x, y, new Vector3(w, h, 0.10f), yaw);
            Front(CarMat.Chrome, x - w * 0.15f, y, new Vector3(w * 0.28f, h * 0.6f, 0.08f), yaw, 0.03f);
            Front(CarMat.Light, x, y - h * 0.62f, new Vector3(w * 0.85f, 0.022f, 0.06f), yaw, 0.01f);
        }

        public void HBars(CarMat m, float y0, float y1, int count, float width, float thick)
        {
            for (int i = 0; i < count; i++)
            {
                float y = count == 1 ? (y0 + y1) * 0.5f : Mathf.Lerp(y0, y1, i / (float)(count - 1));
                Front(m, 0f, y, new Vector3(width, thick, 0.05f), 0f, 0.015f);
            }
        }

        public void VBars(CarMat m, float y, float height, float width, int count, float thick)
        {
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, (i + 0.5f) / count);
                Box(m, new Vector3(x, y, FrontZ(y) + 0.03f), new Vector3(thick, height, 0.05f));
            }
        }

        public void DotGrid(CarMat m, float y, float width, float height, int cols, int rows, float dot)
        {
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, (c + 0.5f) / cols);
                    float yy = Mathf.Lerp(y - height * 0.5f, y + height * 0.5f, (r + 0.5f) / rows);
                    float stagger = (r % 2 == 0) ? 0f : width / cols * 0.5f;
                    if (c == cols - 1 && stagger > 0f) continue;
                    Box(m, new Vector3(x + stagger, yy, FrontZ(yy) + 0.035f), new Vector3(dot, dot, 0.03f), new Vector3(0f, 0f, 45f));
                }
        }
    }

    /// <summary>Builds a car (body mesh with all trim merged + 4 spinning wheels) from a CarSpec.</summary>
    public static class ProceduralCar
    {
        static readonly Dictionary<string, Mesh> BodyCache = new Dictionary<string, Mesh>();
        static readonly Dictionary<string, Mesh> WheelCache = new Dictionary<string, Mesh>();
        static readonly Dictionary<Color, Material> PaintCache = new Dictionary<Color, Material>();
        static readonly Dictionary<Color, Material> CaliperCache = new Dictionary<Color, Material>();
        static Material[] _shared;
        static Material _rubber;

        struct P
        {
            public Vector2 p;      // (z, y)
            public bool black;     // material of the edge starting here
            public P(Vector2 p, bool black) { this.p = p; this.black = black; }
        }

        public static KenneyCarRig.Result Build(Transform root, CarSpec spec, Color paint)
        {
            var r = new KenneyCarRig.Result();
            var body = new GameObject(spec.name);
            body.transform.SetParent(root, false);
            r.visual = body.transform;

            if (!BodyCache.TryGetValue(spec.name, out var mesh))
                BodyCache[spec.name] = mesh = BuildBody(spec);
            body.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mats = (Material[])Shared().Clone();
            mats[(int)CarMat.Paint] = Paint(paint);
            body.AddComponent<MeshRenderer>().sharedMaterials = mats;

            float y = spec.wheelRadius;
            r.frontLeft = Wheel(root, spec, "FL", new Vector3(-spec.halfTrack, y, spec.frontAxle), false);
            r.frontRight = Wheel(root, spec, "FR", new Vector3(spec.halfTrack, y, spec.frontAxle), true);
            r.rearLeft = Wheel(root, spec, "RL", new Vector3(-spec.halfTrack, y, spec.rearAxle), false);
            r.rearRight = Wheel(root, spec, "RR", new Vector3(spec.halfTrack, y, spec.rearAxle), true);
            r.wheels.Add(r.frontLeft); r.wheels.Add(r.frontRight); r.wheels.Add(r.rearLeft); r.wheels.Add(r.rearRight);
            r.wheelRadius = spec.wheelRadius;
            r.bodyBounds = new Bounds(new Vector3(0f, (spec.sill + spec.roof) * 0.5f, 0f),
                new Vector3(spec.wLow * 2f, spec.roof - spec.sill, spec.length));

            var info = root.gameObject.AddComponent<VehicleInfo>();
            info.model = spec.name;
            info.topSpeedKmh = spec.topSpeedKmh;
            info.accel = spec.accel;
            info.engine = spec.engine;
            info.gears = spec.engine == EngineSound.V10 ? 7 : 6;
            return r;
        }

        // ---------------- body ----------------

        static List<P> Profile(CarSpec s)
        {
            var l = new List<P> { new P(new Vector2(s.rearUnderZ, s.sill), true) };
            AddArch(l, s, s.rearAxle);
            AddArch(l, s, s.frontAxle);          // edge between the arches = black sill
            foreach (var p in s.top) l.Add(new P(p, false));
            return l;
        }

        static void AddArch(List<P> l, CarSpec s, float za)
        {
            float d = Mathf.Asin(Mathf.Clamp((s.sill - s.wheelRadius) / s.ArchR, -1f, 1f));
            const int Steps = 16;
            for (int i = 0; i <= Steps; i++)
            {
                float t = Mathf.Lerp(Mathf.PI - d, d, i / (float)Steps);
                l.Add(new P(new Vector2(za + s.ArchR * Mathf.Cos(t), s.wheelRadius + s.ArchR * Mathf.Sin(t)), true));
            }
        }

        static Mesh BuildBody(CarSpec s)
        {
            const int Paint = (int)CarMat.Paint, Black = (int)CarMat.Black, Glass = (int)CarMat.Glass;
            var mb = new MeshBuilder((int)CarMat.Count);

            // Profile with the two shoulder-line crossings inserted
            var src = Profile(s);
            var pts = new List<P>();
            int cutA = -1, cutB = -1;
            for (int i = 0; i < src.Count; i++)
            {
                P a = src[i], b = src[(i + 1) % src.Count];
                pts.Add(a);
                if ((a.p.y - s.belt) * (b.p.y - s.belt) < 0f)
                {
                    float t = (s.belt - a.p.y) / (b.p.y - a.p.y);
                    if (cutA < 0) cutA = pts.Count; else cutB = pts.Count;
                    pts.Add(new P(new Vector2(Mathf.Lerp(a.p.x, b.p.x, t), s.belt), a.black));
                }
            }

            // Perimeter skin: bumpers, hood, windshield, roof, tailgate, arch linings, sills
            int n = pts.Count;
            for (int i = 0; i < n; i++)
            {
                P a = pts[i], b = pts[(i + 1) % n];
                bool upper = (a.p.y + b.p.y) * 0.5f > s.belt;
                float wa = upper ? s.WUp(a.p.y, a.p.x) : s.WLow(a.p.y, a.p.x);
                float wb = upper ? s.WUp(b.p.y, b.p.x) : s.WLow(b.p.y, b.p.x);
                Vector2 d = b.p - a.p;
                var outward = new Vector3(0f, -d.x, d.y);   // profile is anticlockwise in (z, y)
                mb.Quad(a.black ? Black : Paint,
                    new Vector3(-wa, a.p.y, a.p.x), new Vector3(wa, a.p.y, a.p.x),
                    new Vector3(wb, b.p.y, b.p.x), new Vector3(-wb, b.p.y, b.p.x), outward);
            }

            // Side panels: lower body and glasshouse separately so the shoulder line stays crisp
            var lower = new List<Vector2>();
            var upperPoly = new List<Vector2>();
            foreach (var p in pts)
            {
                if (p.p.y <= s.belt + 1e-4f) lower.Add(p.p);
                if (p.p.y >= s.belt - 1e-4f) upperPoly.Add(p.p);
            }
            SidePanels(mb, Paint, lower, q => s.WLow(q.y, q.x));
            SidePanels(mb, Paint, upperPoly, q => s.WUp(q.y, q.x));

            if (cutA >= 0 && cutB >= 0)
            {
                Vector2 ca = pts[cutA].p, cb = pts[cutB].p;
                for (int sg = -1; sg <= 1; sg += 2)
                    mb.Quad(Paint,
                        new Vector3(sg * s.WUp(s.belt, ca.x), s.belt, ca.x), new Vector3(sg * s.WLow(s.belt, ca.x), s.belt, ca.x),
                        new Vector3(sg * s.WLow(s.belt, cb.x), s.belt, cb.x), new Vector3(sg * s.WUp(s.belt, cb.x), s.belt, cb.x), Vector3.up);
            }

            // Wheel-well liners just inside the tyres
            float liner = s.halfTrack - s.tyreWidth * 0.5f - 0.03f;
            foreach (float za in new[] { s.rearAxle, s.frontAxle })
            {
                var arch = new List<P>();
                AddArch(arch, s, za);
                var c = new Vector2(za, s.sill);
                for (int sg = -1; sg <= 1; sg += 2)
                    for (int i = 0; i < arch.Count - 1; i++)
                        mb.Tri(Black, new Vector3(sg * liner, c.y, c.x),
                            new Vector3(sg * liner, arch[i].p.y, arch[i].p.x),
                            new Vector3(sg * liner, arch[i + 1].p.y, arch[i + 1].p.x), new Vector3(sg, 0f, 0f));
            }

            // Glass
            var win = s.sideWindow;
            var tris = MeshBuilder.Triangulate(win);
            for (int sg = -1; sg <= 1; sg += 2)
            {
                Vector3 V(Vector2 q) => new Vector3(sg * (s.WUp(q.y, q.x) + 0.006f), q.y, q.x);
                for (int i = 0; i < tris.Count; i += 3)
                    mb.Tri(Glass, V(win[tris[i]]), V(win[tris[i + 1]]), V(win[tris[i + 2]]), new Vector3(sg, 0f, 0f));
            }
            GlassOnEdge(mb, s, s.top[s.windshieldA], s.top[s.windshieldA + 1]);
            GlassOnEdge(mb, s, s.top[s.rearGlassA], s.top[s.rearGlassA + 1]);

            var det = new CarDetailer(mb, s);
            CommonDetails(det);
            s.details?.Invoke(det);
            return mb.ToMesh(s.name);
        }

        static void SidePanels(MeshBuilder mb, int sub, List<Vector2> poly, System.Func<Vector2, float> width)
        {
            var tris = MeshBuilder.Triangulate(poly);
            for (int sg = -1; sg <= 1; sg += 2)
            {
                Vector3 V(Vector2 q) => new Vector3(sg * width(q), q.y, q.x);
                for (int i = 0; i < tris.Count; i += 3)
                    mb.Tri(sub, V(poly[tris[i]]), V(poly[tris[i + 1]]), V(poly[tris[i + 2]]), new Vector3(sg, 0f, 0f));
            }
        }

        static void GlassOnEdge(MeshBuilder mb, CarSpec s, Vector2 a, Vector2 b)
        {
            Vector2 d = b - a;
            Vector3 n = new Vector3(0f, -d.x, d.y).normalized;
            Vector2 p0 = Vector2.Lerp(a, b, 0.06f), p1 = Vector2.Lerp(a, b, 0.94f);
            float w0 = s.WUp(Mathf.Max(p0.y, s.belt), p0.x) - 0.07f, w1 = s.WUp(Mathf.Max(p1.y, s.belt), p1.x) - 0.07f;
            Vector3 o = n * 0.006f;
            mb.Quad((int)CarMat.Glass,
                new Vector3(-w0, p0.y, p0.x) + o, new Vector3(w0, p0.y, p0.x) + o,
                new Vector3(w1, p1.y, p1.x) + o, new Vector3(-w1, p1.y, p1.x) + o, n);
        }

        /// <summary>Trim every car has: plates, mirrors, pillars, handles, shut lines, steps, rails.</summary>
        static void CommonDetails(CarDetailer d)
        {
            var s = d.spec;
            d.Front(CarMat.Plate, 0f, s.plateFrontY, new Vector3(0.50f, 0.12f, 0.02f), 0f, 0.01f);
            d.Rear(CarMat.Plate, 0f, s.plateRearY, new Vector3(0.50f, 0.13f, 0.02f), 0f, 0.01f);

            // Mirrors at the windshield base
            float mz = s.top[s.windshieldA].x - 0.14f, my = s.belt + 0.08f;
            float mx = s.WUp(my, mz);
            d.Pair(CarMat.Paint, new Vector3(mx + 0.13f, my, mz), new Vector3(0.20f, 0.13f, 0.09f));
            d.Pair(CarMat.Black, new Vector3(mx + 0.04f, my - 0.03f, mz + 0.02f), new Vector3(0.09f, 0.05f, 0.06f));

            // Window frame chrome and blacked-out pillars
            float wTop = float.MinValue, wFront = float.MinValue, wRear = float.MaxValue;
            foreach (var p in s.sideWindow) { wTop = Mathf.Max(wTop, p.y); wFront = Mathf.Max(wFront, p.x); wRear = Mathf.Min(wRear, p.x); }
            float wBottom = s.belt + 0.03f;
            if (s.chromeBelt)
            {
                float cz = (wFront + wRear) * 0.5f;
                d.Pair(CarMat.Chrome, new Vector3(s.WUp(wBottom, cz) + 0.008f, wBottom, cz), new Vector3(0.012f, 0.022f, wFront - wRear));
            }
            foreach (float pz in s.pillars)
            {
                float ym = (wBottom + wTop) * 0.5f;
                d.Pair(CarMat.Black, new Vector3(s.WUp(ym, pz) + 0.01f, ym, pz), new Vector3(0.012f, wTop - wBottom, 0.11f));
            }

            // Door shut lines + handles (cars with a B-pillar)
            if (s.pillars.Length > 0)
            {
                float b = s.pillars[0];
                float doorH = s.belt - s.sill - 0.14f, doorY = s.sill + 0.08f + doorH * 0.5f;
                float frontEdge = s.top[s.windshieldA].x - 0.12f;
                float rearEdge = s.pillars.Length > 1 ? s.pillars[1] : wRear + 0.1f;
                foreach (float z in new[] { frontEdge, b, rearEdge })
                    d.Pair(CarMat.Dark, new Vector3(s.WLow(doorY, z) + 0.002f, doorY, z), new Vector3(0.004f, doorH, 0.012f));
                float hy = s.belt - 0.09f;
                foreach (float z in new[] { b + 0.35f, rearEdge + 0.35f })
                    d.Pair(CarMat.Chrome, new Vector3(s.WLow(hy, z) + 0.012f, hy, z), new Vector3(0.025f, 0.035f, 0.14f));
            }

            float frontArchRear = s.frontAxle - s.ArchR, rearArchFront = s.rearAxle + s.ArchR;
            if (s.sideSteps)
            {
                float cz = (frontArchRear + rearArchFront) * 0.5f, len = frontArchRear - rearArchFront - 0.05f;
                d.Pair(CarMat.Black, new Vector3(s.wLow + 0.03f, s.sill - 0.02f, cz), new Vector3(0.16f, 0.05f, len));
                d.Pair(CarMat.Chrome, new Vector3(s.wLow + 0.05f, s.sill + 0.008f, cz), new Vector3(0.10f, 0.008f, len - 0.1f));
            }

            if (s.roofRails)
            {
                float rz0 = float.MaxValue, rz1 = float.MinValue;
                foreach (var p in s.top) if (p.y > s.roof - 0.03f) { rz0 = Mathf.Min(rz0, p.x); rz1 = Mathf.Max(rz1, p.x); }
                float cz = (rz0 + rz1) * 0.5f, len = (rz1 - rz0) * 0.9f;
                d.Pair(CarMat.Silver, new Vector3(s.wRoof - 0.08f, s.roof + 0.03f, cz), new Vector3(0.05f, 0.05f, len));
            }
        }

        // ---------------- wheels ----------------

        static Transform Wheel(Transform root, CarSpec s, string name, Vector3 pos, bool right)
        {
            var pose = new GameObject("WheelPose_" + name).transform;
            pose.SetParent(root, false);
            pose.localPosition = pos;
            var spin = new GameObject("Spin").transform;
            spin.SetParent(pose, false);

            var wheel = new GameObject("Wheel");
            wheel.transform.SetParent(spin, false);
            wheel.transform.localRotation = right ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);   // alloy face outward
            if (!WheelCache.TryGetValue(s.name, out var mesh))
                WheelCache[s.name] = mesh = BuildWheel(s);
            wheel.AddComponent<MeshFilter>().sharedMesh = mesh;
            var sh = Shared();
            wheel.AddComponent<MeshRenderer>().sharedMaterials = new[] { Rubber(), sh[(int)s.rimMat], sh[(int)CarMat.Dark] };

            // Brake caliper on the pose: steers with the wheel but doesn't spin
            var cal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(cal.GetComponent<Collider>());
            cal.name = "Caliper";
            cal.transform.SetParent(pose, false);
            float rr = s.wheelRadius * s.rimRatio;
            cal.transform.localPosition = new Vector3((right ? 1f : -1f) * s.tyreWidth * 0.18f, rr * 0.45f, -rr * 0.35f);
            cal.transform.localScale = new Vector3(0.04f, rr * 0.5f, rr * 0.4f);
            cal.GetComponent<Renderer>().sharedMaterial = Caliper(s.caliper);
            return pose;
        }

        static Mesh BuildWheel(CarSpec s)
        {
            const int Rubber = 0, RimSub = 1, Dark = 2;
            var mb = new MeshBuilder(3);
            float r = s.wheelRadius, hw = s.tyreWidth * 0.5f, rr = r * s.rimRatio, side = r - rr;
            mb.Lathe(Rubber, new[]
            {
                new Vector2(-hw * 0.94f, rr), new Vector2(-hw, rr + side * 0.3f), new Vector2(-hw * 0.985f, rr + side * 0.7f),
                new Vector2(-hw * 0.88f, r * 0.97f), new Vector2(-hw * 0.69f, r), new Vector2(hw * 0.69f, r),
                new Vector2(hw * 0.88f, r * 0.97f), new Vector2(hw * 0.985f, rr + side * 0.7f), new Vector2(hw, rr + side * 0.3f),
                new Vector2(hw * 0.94f, rr)
            }, 40);
            mb.Lathe(Rubber, new[] { new Vector2(-hw * 0.92f, 0f), new Vector2(-hw * 0.92f, rr) }, 24);
            mb.Lathe(RimSub, new[] { new Vector2(hw * 0.96f, rr + 0.005f), new Vector2(hw * 0.92f, rr - 0.03f) }, 40);
            mb.Lathe(Dark, new[] { new Vector2(hw * 0.92f, rr - 0.03f), new Vector2(hw * 0.5f, rr - 0.03f), new Vector2(hw * 0.5f, 0f) }, 40);
            mb.Lathe(RimSub, new[] { new Vector2(hw * 0.5f, 0.065f), new Vector2(hw * 0.75f, 0.055f), new Vector2(hw * 0.8f, 0f) }, 24);

            float inner = 0.06f, outer = rr - 0.035f, len = outer - inner;
            for (int k = 0; k < s.spokes; k++)
            {
                var rot = Quaternion.AngleAxis(k * 360f / s.spokes, Vector3.right);
                if (s.twinSpokes)
                {
                    for (int sg = -1; sg <= 1; sg += 2)
                        mb.Box(RimSub, new Vector3(hw * 0.7f, 0f, 0f) + rot * new Vector3(0f, inner + len * 0.5f, sg * 0.022f),
                            new Vector3(hw * 0.3f, len, 0.022f), rot);
                }
                else
                {
                    mb.Box(RimSub, new Vector3(hw * 0.7f, 0f, 0f) + rot * new Vector3(0f, inner + len * 0.5f, 0f),
                        new Vector3(hw * 0.3f, len, 0.04f), rot);
                }
            }
            return mb.ToMesh(s.name + "_Wheel");
        }

        // ---------------- materials ----------------

        internal static Material[] Shared()
        {
            if (_shared != null) return _shared;
            _shared = new Material[(int)CarMat.Count];
            _shared[(int)CarMat.Paint] = Make(Color.white, 0.35f, 0.88f);
            _shared[(int)CarMat.Black] = Make(new Color(0.04f, 0.04f, 0.045f), 0f, 0.35f);
            _shared[(int)CarMat.Glass] = Make(new Color(0.04f, 0.06f, 0.08f), 0.8f, 0.95f);
            _shared[(int)CarMat.Chrome] = Make(new Color(0.9f, 0.9f, 0.92f), 1f, 0.95f);
            _shared[(int)CarMat.Light] = Make(new Color(0.95f, 0.96f, 1f), 0.2f, 0.95f, new Color(0.6f, 0.62f, 0.65f));
            _shared[(int)CarMat.Tail] = Make(new Color(0.6f, 0.02f, 0.02f), 0.2f, 0.9f, new Color(0.35f, 0f, 0f));
            _shared[(int)CarMat.Plate] = Make(new Color(0.95f, 0.95f, 0.92f), 0f, 0.4f);
            _shared[(int)CarMat.Dark] = Make(new Color(0.12f, 0.12f, 0.13f), 0.5f, 0.5f);
            _shared[(int)CarMat.Silver] = Make(new Color(0.72f, 0.73f, 0.75f), 0.85f, 0.75f);
            return _shared;
        }

        internal static Material Rubber() => _rubber != null ? _rubber : (_rubber = Make(new Color(0.05f, 0.05f, 0.05f), 0f, 0.15f));

        internal static Material Paint(Color c)
        {
            if (!PaintCache.TryGetValue(c, out var m))
                PaintCache[c] = m = Make(c, 0.4f, 0.86f);
            return m;
        }

        internal static Material Caliper(Color c)
        {
            if (!CaliperCache.TryGetValue(c, out var m))
                CaliperCache[c] = m = Make(c, 0.3f, 0.6f);
            return m;
        }

        static Material Make(Color c, float metallic, float smoothness, Color? emission = null)
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader) { color = c, enableInstancing = true };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
            }
            return mat;
        }
    }
}
