using System.Collections.Generic;
using UnityEngine;
using FortunerCity.Vehicle;

namespace FortunerCity.City
{
    /// <summary>
    /// Centre line for swept road geometry: points with cumulative length s and turning angle θ (radians,
    /// positive when turning toward the lateral side). Lateral = horizontal, to the left of travel (+Z when heading +X).
    /// </summary>
    public class SweepPath
    {
        public readonly List<Vector3> pos = new List<Vector3>();
        public readonly List<float> s = new List<float>();
        public readonly List<float> theta = new List<float>();

        public float Length => s.Count == 0 ? 0f : s[s.Count - 1];

        public void Add(Vector3 p, float th = 0f)
        {
            s.Add(pos.Count == 0 ? 0f : s[s.Count - 1] + Vector3.Distance(pos[pos.Count - 1], p));
            pos.Add(p);
            theta.Add(th);
        }

        public static SweepPath Polyline(params Vector3[] pts)
        {
            var p = new SweepPath();
            foreach (var v in pts) p.Add(v);
            return p;
        }

        public struct Sample
        {
            public Vector3 pos, tangent, lateral;
            public float s, theta;
        }

        int Segment(float at)
        {
            for (int i = 0; i < s.Count - 2; i++)
                if (at < s[i + 1]) return i;
            return s.Count - 2;
        }

        Vector3 SegDir(int i) => (pos[i + 1] - pos[i]).normalized;

        static Vector3 Lateral(Vector3 tangent)
        {
            var flat = new Vector3(tangent.x, 0f, tangent.z);
            return Vector3.Cross(flat.normalized, Vector3.up);
        }

        public Sample At(float at)
        {
            at = Mathf.Clamp(at, 0f, Length);
            int i = Segment(at);
            float len = s[i + 1] - s[i];
            float t = len > 1e-5f ? (at - s[i]) / len : 0f;
            Vector3 tan = SegDir(i);
            // at a path vertex use the averaged direction so neighbouring pieces meet without a crack
            if (t < 1e-4f && i > 0) tan = (SegDir(i - 1) + tan).normalized;
            else if (t > 1f - 1e-4f && i + 1 < s.Count - 1) tan = (tan + SegDir(i + 1)).normalized;
            return new Sample
            {
                pos = Vector3.Lerp(pos[i], pos[i + 1], t),
                tangent = tan,
                lateral = Lateral(tan),
                s = at,
                theta = Mathf.Lerp(theta[i], theta[i + 1], t)
            };
        }

        /// <summary>Samples from s0 to s1: both ends plus every path vertex in between.</summary>
        public List<Sample> Samples(float s0, float s1)
        {
            var list = new List<Sample> { At(s0) };
            for (int i = 0; i < s.Count; i++)
                if (s[i] > s0 + 1e-3f && s[i] < s1 - 1e-3f) list.Add(At(s[i]));
            list.Add(At(s1));
            return list;
        }
    }

    /// <summary>
    /// Mesh made by sweeping (d = lateral, y = up) profiles along a <see cref="SweepPath"/>.
    /// Hard edges between profile segments, smooth along the path (curves stay round).
    /// </summary>
    public class SweepMesh
    {
        public delegate Vector2 UvFn(float d, float y, float profileLength, float s, float theta);

        readonly List<Vector3> _v = new List<Vector3>();
        readonly List<Vector3> _n = new List<Vector3>();
        readonly List<Vector2> _uv = new List<Vector2>();
        readonly List<int>[] _subs;

        public SweepMesh(int submeshes = 1)
        {
            _subs = new List<int>[submeshes];
            for (int i = 0; i < submeshes; i++) _subs[i] = new List<int>();
        }

        int Vert(Vector3 p, Vector3 n, Vector2 uv)
        {
            _v.Add(p); _n.Add(n); _uv.Add(uv);
            return _v.Count - 1;
        }

        void Tri(int sub, int a, int b, int c, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(_v[b] - _v[a], _v[c] - _v[a]), outward) < 0f) { int t = b; b = c; c = t; }
            _subs[sub].Add(a); _subs[sub].Add(b); _subs[sub].Add(c);
        }

        static List<Vector2> Prepare(IList<Vector2> profile, bool mirror)
        {
            var prof = new List<Vector2>(profile);
            if (mirror)
            {
                for (int i = 0; i < prof.Count; i++) prof[i] = new Vector2(-prof[i].x, prof[i].y);
                prof.Reverse();
            }
            return prof;
        }

        /// <summary>
        /// Sweep a profile between s0 and s1. List profile points so the visible side is on the right of
        /// travel in the (d right, y up) plane — closed profiles counter-clockwise. mirror flips d.
        /// </summary>
        public void Extrude(int sub, SweepPath path, float s0, float s1, IList<Vector2> profile, bool closed, bool mirror,
            UvFn uv, float dOffset = 0f)
        {
            var prof = Prepare(profile, mirror);
            for (int i = 0; i < prof.Count; i++) prof[i] += new Vector2(dOffset, 0f);
            var samples = path.Samples(s0, s1);
            int segs = closed ? prof.Count : prof.Count - 1;
            float plen = 0f;
            for (int k = 0; k < segs; k++)
            {
                Vector2 a = prof[k], b = prof[(k + 1) % prof.Count];
                Vector2 dir = b - a;
                Vector2 n2 = new Vector2(dir.y, -dir.x).normalized;
                float la = plen, lb = plen + dir.magnitude;
                plen = lb;
                int first = _v.Count;
                foreach (var sm in samples)
                {
                    Vector3 nrm = (sm.lateral * n2.x + Vector3.up * n2.y).normalized;
                    Vert(sm.pos + sm.lateral * a.x + Vector3.up * a.y, nrm, uv(a.x, a.y, la, sm.s, sm.theta));
                    Vert(sm.pos + sm.lateral * b.x + Vector3.up * b.y, nrm, uv(b.x, b.y, lb, sm.s, sm.theta));
                }
                for (int i = 0; i < samples.Count - 1; i++)
                {
                    int a0 = first + i * 2, b0 = a0 + 1, a1 = a0 + 2, b1 = a0 + 3;
                    Vector3 outward = _n[a0] + _n[a1];
                    Tri(sub, a0, b0, b1, outward);
                    Tri(sub, a0, b1, a1, outward);
                }
            }
        }

        /// <summary>Flat end cap: the profile polygon (closed along its first-last edge) at path position s.</summary>
        public void Cap(int sub, SweepPath path, float s, IList<Vector2> profile, bool mirror, bool facingForward, float dOffset = 0f)
        {
            var prof = Prepare(profile, mirror);
            var sm = path.At(s);
            Vector3 n = facingForward ? sm.tangent : -sm.tangent;
            var ids = new int[prof.Count];
            for (int i = 0; i < prof.Count; i++)
            {
                Vector2 p = prof[i] + new Vector2(dOffset, 0f);
                ids[i] = Vert(sm.pos + sm.lateral * p.x + Vector3.up * p.y, n, new Vector2(p.x, p.y) * 0.5f);
            }
            var tris = MeshBuilder.Triangulate(prof);
            for (int i = 0; i < tris.Count; i += 3) Tri(sub, ids[tris[i]], ids[tris[i + 1]], ids[tris[i + 2]], n);
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (_v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(_v);
            mesh.SetNormals(_n);
            mesh.SetUVs(0, _uv);
            mesh.subMeshCount = _subs.Length;
            for (int i = 0; i < _subs.Length; i++) mesh.SetTriangles(_subs[i], i);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Adds a GameObject with this mesh (and an exact MeshCollider when asked).</summary>
        public GameObject Spawn(string name, Transform parent, Material[] mats, bool collider)
        {
            var mesh = ToMesh(name);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = mats;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }
    }
}
