using System.Collections.Generic;
using UnityEngine;
using FortunerCity.Vehicle;

namespace FortunerCity.City
{
    public enum SignalState { Green, Yellow, Red }

    /// <summary>
    /// Working traffic signals at city junctions. Each approach gets a kerb pole (near-left corner, left-hand
    /// traffic) with a low head plus an overhead mast arm; stop lines are painted before the zebra.
    /// Cycle per junction: N-S green 14 s → yellow 3 s → all-red 2 s → E-W green → yellow → all-red.
    /// Traffic cars ask <see cref="MustStop"/> before entering a junction.
    /// </summary>
    public class TrafficSignals : MonoBehaviour
    {
        public const float GreenTime = 14f, YellowTime = 3f, AllRedTime = 2f;
        const float Phase = GreenTime + YellowTime + AllRedTime;
        const float Cycle = Phase * 2f;
        const float StopLineGap = 3.9f;   // from junction edge, just before the zebra crossing

        class Junction
        {
            public float offset;
            public readonly List<MeshRenderer> ns = new List<MeshRenderer>(), ew = new List<MeshRenderer>();
            public int nsState = -1, ewState = -1;
        }

        static TrafficSignals _instance;
        static readonly Dictionary<int, Junction> Junctions = new Dictionary<int, Junction>();
        static Material[][] _stateMats;
        static Material _pole;
        static Mesh _headMesh;

        static int Key(int i, int j) => i * 100 + j;

        /// <summary>Builds signals at every junction with 3+ arms. Poles/stop lines go under staticParent (batched).</summary>
        public static void Build(Transform cityRoot, Transform staticParent)
        {
            var go = new GameObject("TrafficSignals");
            go.transform.SetParent(cityRoot, false);
            _instance = go.AddComponent<TrafficSignals>();
            Junctions.Clear();
            EnsureAssets();

            int grid = CityBuilder.Grid;
            for (int i = 0; i <= grid; i++)
            {
                for (int j = 0; j <= grid; j++)
                {
                    var arms = new List<Vector3>();
                    if (i > 0) arms.Add(Vector3.right);      // travelling +X arrives from the west arm
                    if (i < grid) arms.Add(Vector3.left);
                    if (j > 0) arms.Add(Vector3.forward);
                    if (j < grid) arms.Add(Vector3.back);
                    if (arms.Count < 3) continue;            // plain corners have no signal

                    // offsets create a loose "green wave" instead of every light switching together
                    var jn = new Junction { offset = (i * 7f + j * 11f) % Cycle };
                    foreach (var d in arms) BuildApproach(jn, i, j, d, staticParent, go.transform);
                    Junctions[Key(i, j)] = jn;
                }
            }
            _instance.Refresh(true);
        }

        static void BuildApproach(Junction jn, int i, int j, Vector3 d, Transform poles, Transform heads)
        {
            bool ew = Mathf.Abs(d.x) > 0.5f;
            float wRoad = ew ? CityBuilder.Width(j) : CityBuilder.Width(i);     // road we arrive on
            float wCross = ew ? CityBuilder.Width(i) : CityBuilder.Width(j);    // road we cross
            bool avenue = wRoad > CityBuilder.StreetWidth + 1f;
            var c = new Vector3(CityBuilder.Line(i), 0f, CityBuilder.Line(j));
            var left = new Vector3(-d.z, 0f, d.x);                              // left-hand traffic keeps left

            // Pole on the near-left kerb corner
            Vector3 pole = c - d * (wCross * 0.5f + 1.2f) + left * (wRoad * 0.5f + 1.2f);
            pole.y = CityBuilder.PadTop;
            CityMesh.SpawnBox("SignalPole", poles, new Vector3(0.22f, 6.2f, 0.22f), pole, _pole, _pole, true);

            // Mast arm reaching over the lanes
            float arm = avenue ? 10.5f : 4.6f;
            Vector3 armMid = pole - left * (arm * 0.5f);
            var armSize = ew ? new Vector3(0.16f, 0.16f, arm) : new Vector3(arm, 0.16f, 0.16f);
            CityMesh.SpawnBox("SignalArm", poles, armSize, new Vector3(armMid.x, pole.y + 5.9f, armMid.z), _pole, _pole, false);

            var faceDrivers = Quaternion.LookRotation(-d, Vector3.up);
            var list = ew ? jn.ew : jn.ns;
            float[] headOffsets = avenue ? new[] { 6.45f, 10.0f } : new[] { 4.0f };
            foreach (float off in headOffsets)
            {
                Vector3 h = pole - left * off;
                CityMesh.SpawnBox("Hanger", poles, new Vector3(0.06f, 0.3f, 0.06f), new Vector3(h.x, pole.y + 5.6f, h.z), _pole, _pole, false);
                list.Add(Head(heads, new Vector3(h.x, pole.y + 5.05f, h.z), faceDrivers));
            }
            // Low head on the pole itself
            Vector3 low = pole - d * 0.25f;
            list.Add(Head(heads, new Vector3(low.x, pole.y + 3.0f, low.z), faceDrivers));

            // Stop line across the approach carriageway
            float laneOff = avenue ? CityBuilder.MedianWidth * 0.5f + (wRoad - CityBuilder.MedianWidth) * 0.25f : wRoad * 0.25f;
            float laneW = avenue ? (wRoad - CityBuilder.MedianWidth) * 0.5f - 0.6f : wRoad * 0.5f - 1.2f;
            Vector3 stop = c - d * (wCross * 0.5f + StopLineGap) + left * laneOff;
            stop.y = CityBuilder.RoadTop + 0.022f;
            CityMesh.SpawnQuad("StopLine", poles, CityArt.LinePaint, laneW, 0.45f, 1f, 1f, stop, Quaternion.LookRotation(d, Vector3.up));
        }

        static MeshRenderer Head(Transform parent, Vector3 pos, Quaternion rot)
        {
            var go = new GameObject("SignalHead");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, rot);
            go.AddComponent<MeshFilter>().sharedMesh = _headMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = _stateMats[(int)SignalState.Red];
            return r;
        }

        void Update() => Refresh(false);

        void Refresh(bool force)
        {
            float t = Time.time;
            foreach (var jn in Junctions.Values)
            {
                int ns = (int)StateOf(jn, true, t), ew = (int)StateOf(jn, false, t);
                if (force || ns != jn.nsState)
                {
                    foreach (var r in jn.ns) r.sharedMaterials = _stateMats[ns];
                    jn.nsState = ns;
                }
                if (force || ew != jn.ewState)
                {
                    foreach (var r in jn.ew) r.sharedMaterials = _stateMats[ew];
                    jn.ewState = ew;
                }
            }
        }

        static SignalState StateOf(Junction jn, bool northSouth, float time)
        {
            float t = Mathf.Repeat(time + jn.offset - (northSouth ? 0f : Phase), Cycle);
            if (t < GreenTime) return SignalState.Green;
            if (t < GreenTime + YellowTime) return SignalState.Yellow;
            return SignalState.Red;
        }

        /// <summary>
        /// Should a car at pos heading forward stop for the next signal? distance = metres from the car's
        /// front bumper to the stop line.
        /// </summary>
        public static bool MustStop(Vector3 pos, Vector3 forward, float halfLength, out float distance)
        {
            distance = 0f;
            if (_instance == null || pos.y > 3f) return false;      // motorway deck has no signals

            bool alongX = Mathf.Abs(forward.x) >= Mathf.Abs(forward.z);
            float sign = alongX ? Mathf.Sign(forward.x) : Mathf.Sign(forward.z);
            float along = alongX ? pos.x : pos.z, cross = alongX ? pos.z : pos.x;

            int road = Mathf.RoundToInt((cross + CityBuilder.Half) / CityBuilder.BlockSize);
            if (road < 0 || road > CityBuilder.Grid) return false;
            if (Mathf.Abs(cross - CityBuilder.Line(road)) > CityBuilder.Width(road) * 0.5f) return false;

            float front = along + sign * halfLength;
            int k = -1;
            float d = 0f;
            for (int s = 0; s <= CityBuilder.Grid; s++)
            {
                int n = sign > 0f ? s : CityBuilder.Grid - s;
                float stopLine = CityBuilder.Line(n) - sign * (CityBuilder.Width(n) * 0.5f + StopLineGap);
                d = (stopLine - front) * sign;
                if (d > -1f) { k = n; break; }
            }
            if (k < 0 || d > 30f) return false;

            int key = alongX ? Key(k, road) : Key(road, k);
            if (!Junctions.TryGetValue(key, out var jn)) return false;

            distance = d;
            var state = StateOf(jn, !alongX, Time.time);
            if (state == SignalState.Red) return d > -0.5f;
            if (state == SignalState.Yellow) return d > 6f;          // too close to stop safely → keep going
            return false;
        }

        public static bool HasSignal(int i, int j) => Junctions.ContainsKey(Key(i, j));

        /// <summary>
        /// Can a pedestrian start crossing at junction (i, j)? crossingNsRoad = walking across the road that
        /// runs north-south (so N-S cars must be held at red for at least <paramref name="seconds"/> more).
        /// </summary>
        public static bool PedestrianCanCross(int i, int j, bool crossingNsRoad, float seconds)
        {
            if (!Junctions.TryGetValue(Key(i, j), out var jn)) return false;
            float t = Mathf.Repeat(Time.time + jn.offset - (crossingNsRoad ? 0f : Phase), Cycle);
            bool redForCars = t >= GreenTime + YellowTime + 0.5f;
            float redLeft = Cycle - t;
            return redForCars && redLeft >= seconds;
        }

        // ---------------- assets ----------------

        static void EnsureAssets()
        {
            if (_headMesh != null) return;

            var housing = Make(new Color(0.07f, 0.07f, 0.08f), 0.2f, 0.4f);
            _pole = Make(new Color(0.28f, 0.29f, 0.3f), 0.5f, 0.45f);
            var redOn = Make(new Color(1f, 0.12f, 0.08f), 0f, 0.9f, new Color(2.2f, 0.2f, 0.1f));
            var yellowOn = Make(new Color(1f, 0.75f, 0.1f), 0f, 0.9f, new Color(2.2f, 1.4f, 0.1f));
            var greenOn = Make(new Color(0.15f, 1f, 0.45f), 0f, 0.9f, new Color(0.2f, 2.2f, 0.7f));
            var redOff = Make(new Color(0.18f, 0.04f, 0.04f), 0f, 0.8f);
            var yellowOff = Make(new Color(0.18f, 0.14f, 0.04f), 0f, 0.8f);
            var greenOff = Make(new Color(0.04f, 0.15f, 0.08f), 0f, 0.8f);
            _stateMats = new Material[3][];
            _stateMats[(int)SignalState.Green] = new[] { housing, redOff, yellowOff, greenOn };
            _stateMats[(int)SignalState.Yellow] = new[] { housing, redOff, yellowOn, greenOff };
            _stateMats[(int)SignalState.Red] = new[] { housing, redOn, yellowOff, greenOff };

            // Head: housing + backplate + visors (submesh 0), lamps red/yellow/green (1..3). Faces +Z.
            var mb = new MeshBuilder(4);
            mb.Box(0, Vector3.zero, new Vector3(0.36f, 0.98f, 0.26f), Quaternion.identity);
            mb.Box(0, new Vector3(0f, 0f, -0.145f), new Vector3(0.62f, 1.22f, 0.03f), Quaternion.identity);
            float[] ys = { 0.31f, 0f, -0.31f };
            for (int k = 0; k < 3; k++)
            {
                mb.Box(k + 1, new Vector3(0f, ys[k], 0.14f), new Vector3(0.24f, 0.24f, 0.04f), Quaternion.identity);
                mb.Box(0, new Vector3(0f, ys[k] + 0.135f, 0.20f), new Vector3(0.28f, 0.025f, 0.14f), Quaternion.identity);
            }
            _headMesh = mb.ToMesh("SignalHead");
        }

        static Material Make(Color c, float metallic, float smoothness, Color? emission = null)
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(shader) { color = c, enableInstancing = true };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
            }
            return m;
        }
    }
}
