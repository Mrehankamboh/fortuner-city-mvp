using System.Collections.Generic;
using UnityEngine;

namespace FortunerCity.Vehicle
{
    /// <summary>
    /// Small helper for building flat-shaded runtime meshes with submeshes.
    /// Triangles are wound automatically from an "outward" hint, so callers never worry about winding.
    /// </summary>
    public class MeshBuilder
    {
        readonly List<Vector3> _verts = new List<Vector3>();
        readonly List<int>[] _subs;

        public MeshBuilder(int submeshes)
        {
            _subs = new List<int>[submeshes];
            for (int i = 0; i < submeshes; i++) _subs[i] = new List<int>();
        }

        public void Tri(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f) { var t = b; b = c; c = t; }
            int i = _verts.Count;
            _verts.Add(a); _verts.Add(b); _verts.Add(c);
            _subs[sub].Add(i); _subs[sub].Add(i + 1); _subs[sub].Add(i + 2);
        }

        public void Quad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            Tri(sub, a, b, c, outward);
            Tri(sub, a, c, d, outward);
        }

        /// <summary>Shared vertex for smooth-shaded surfaces (normals are averaged across triangles).</summary>
        public int AddVertex(Vector3 v)
        {
            _verts.Add(v);
            return _verts.Count - 1;
        }

        /// <summary>Triangle on shared vertices, wound to face <paramref name="outward"/>.</summary>
        public void TriIdx(int sub, int a, int b, int c, Vector3 outward)
        {
            Vector3 pa = _verts[a], pb = _verts[b], pc = _verts[c];
            if (Vector3.Dot(Vector3.Cross(pb - pa, pc - pa), outward) < 0f) { int t = b; b = c; c = t; }
            _subs[sub].Add(a); _subs[sub].Add(b); _subs[sub].Add(c);
        }

        /// <summary>
        /// Smooth surface of revolution around local Y. Each ring = (half-width x, half-depth z, height y,
        /// forward offset of the ring centre) so profiles can bulge forward (chest, chin) or back (calf).
        /// </summary>
        public void SmoothLathe(int sub, Vector4[] rings, int seg, Vector3 c, bool capBottom = true, bool capTop = true)
        {
            var idx = new int[rings.Length, seg];
            for (int i = 0; i < rings.Length; i++)
                for (int j = 0; j < seg; j++)
                {
                    float a = j / (float)seg * Mathf.PI * 2f;
                    var r = rings[i];
                    idx[i, j] = AddVertex(c + new Vector3(r.x * Mathf.Cos(a), r.z, r.y * Mathf.Sin(a) + r.w));
                }
            for (int i = 0; i < rings.Length - 1; i++)
            {
                Vector3 axis = c + new Vector3(0f, 0f, (rings[i].w + rings[i + 1].w) * 0.5f);
                for (int j = 0; j < seg; j++)
                {
                    int j1 = (j + 1) % seg;
                    int a = idx[i, j], b = idx[i, j1], cc = idx[i + 1, j1], d = idx[i + 1, j];
                    Vector3 mid = (_verts[a] + _verts[b] + _verts[cc] + _verts[d]) * 0.25f;
                    Vector3 outward = mid - new Vector3(axis.x, mid.y, axis.z);
                    TriIdx(sub, a, b, cc, outward);
                    TriIdx(sub, a, cc, d, outward);
                }
            }
            if (capBottom && rings[0].x > 1e-4f) Cap(sub, idx, 0, rings[0], c, seg, Vector3.down);
            int last = rings.Length - 1;
            if (capTop && rings[last].x > 1e-4f) Cap(sub, idx, last, rings[last], c, seg, Vector3.up);
        }

        void Cap(int sub, int[,] idx, int ring, Vector4 r, Vector3 c, int seg, Vector3 dir)
        {
            int center = AddVertex(c + new Vector3(0f, r.z, r.w));
            for (int j = 0; j < seg; j++)
                TriIdx(sub, center, idx[ring, j], idx[ring, (j + 1) % seg], dir);
        }

        /// <summary>Oriented box (used for alloy spokes).</summary>
        public void Box(int sub, Vector3 center, Vector3 size, Quaternion rot)
        {
            Vector3 hx = rot * new Vector3(size.x * 0.5f, 0f, 0f);
            Vector3 hy = rot * new Vector3(0f, size.y * 0.5f, 0f);
            Vector3 hz = rot * new Vector3(0f, 0f, size.z * 0.5f);
            void Face(Vector3 n, Vector3 u, Vector3 v)
            {
                Vector3 c = center + n;
                Quad(sub, c - u - v, c - u + v, c + u + v, c + u - v, n);
            }
            Face(hx, hy, hz); Face(-hx, hy, hz);
            Face(hy, hx, hz); Face(-hy, hx, hz);
            Face(hz, hx, hy); Face(-hz, hx, hy);
        }

        /// <summary>Revolve a (x = along axle, y = radius) profile around the X axis.</summary>
        public void Lathe(int sub, Vector2[] profile, int segments)
        {
            for (int i = 0; i < profile.Length - 1; i++)
            {
                Vector2 p = profile[i], q = profile[i + 1];
                Vector2 d = q - p;
                Vector2 n2 = new Vector2(-d.y, d.x);   // outward side for profiles listed inner → over → outer
                for (int j = 0; j < segments; j++)
                {
                    float a0 = j / (float)segments * Mathf.PI * 2f, a1 = (j + 1) / (float)segments * Mathf.PI * 2f;
                    float am = (a0 + a1) * 0.5f;
                    Vector3 P(Vector2 v, float a) => new Vector3(v.x, v.y * Mathf.Cos(a), v.y * Mathf.Sin(a));
                    var outward = new Vector3(n2.x, n2.y * Mathf.Cos(am), n2.y * Mathf.Sin(am));
                    Quad(sub, P(p, a0), P(p, a1), P(q, a1), P(q, a0), outward);
                }
            }
        }

        /// <summary>
        /// Lathe around X that is smooth around the axle but keeps hard edges between profile segments
        /// (tyres, rims, discs, headlamp shells).
        /// </summary>
        public void LatheSmooth(int sub, Vector2[] profile, int segments, Vector3 offset = default)
        {
            for (int i = 0; i < profile.Length - 1; i++)
            {
                Vector2 p = profile[i], q = profile[i + 1];
                Vector2 d = q - p;
                Vector2 n2 = new Vector2(-d.y, d.x);
                var a = new int[segments];
                var b = new int[segments];
                for (int j = 0; j < segments; j++)
                {
                    float ang = j / (float)segments * Mathf.PI * 2f;
                    float c = Mathf.Cos(ang), s = Mathf.Sin(ang);
                    a[j] = AddVertex(offset + new Vector3(p.x, p.y * c, p.y * s));
                    b[j] = AddVertex(offset + new Vector3(q.x, q.y * c, q.y * s));
                }
                for (int j = 0; j < segments; j++)
                {
                    int j1 = (j + 1) % segments;
                    float am = (j + 0.5f) / segments * Mathf.PI * 2f;
                    var outward = new Vector3(n2.x, n2.y * Mathf.Cos(am), n2.y * Mathf.Sin(am));
                    TriIdx(sub, a[j], a[j1], b[j1], outward);
                    TriIdx(sub, a[j], b[j1], b[j], outward);
                }
            }
        }

        /// <summary>
        /// Smooth loft along a path (tubes, tanks, seats, fairings, fenders). Every path point gets a superellipse
        /// ring: half.x across <paramref name="side"/>, half.y across the other axis; squareness 2 = ellipse,
        /// 4+ = rounded box. Rings are parallel-transported so bent tubes never twist. Ends get flat caps.
        /// </summary>
        public void Sweep(int sub, IList<Vector3> path, IList<Vector2> half, int seg, float squareness = 2f,
            Vector3? side = null, bool caps = true)
        {
            int n = path.Count;
            if (n < 2) return;
            var idx = new int[n, seg];
            var frameX = new Vector3[n];
            var frameY = new Vector3[n];
            Vector3 prevX = side ?? Vector3.right;
            Vector3 prevT = Vector3.zero;
            float e = 2f / squareness;
            for (int i = 0; i < n; i++)
            {
                Vector3 t = (path[Mathf.Min(i + 1, n - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                if (i > 0) prevX = RotateBetween(prevX, prevT, t);   // turn the frame with the path (no flips at sharp bends)
                prevT = t;
                Vector3 x = prevX - t * Vector3.Dot(prevX, t);
                if (x.sqrMagnitude < 1e-6f)
                {
                    x = Vector3.Cross(t, Mathf.Abs(t.y) < 0.9f ? Vector3.up : Vector3.forward);
                }
                x.Normalize();
                prevX = x;
                Vector3 y = Vector3.Cross(t, x);
                frameX[i] = x;
                frameY[i] = y;
                Vector2 h = half[Mathf.Min(i, half.Count - 1)];
                for (int j = 0; j < seg; j++)
                {
                    float a = j / (float)seg * Mathf.PI * 2f;
                    float c = Mathf.Cos(a), s = Mathf.Sin(a);
                    float px = h.x * Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), e);
                    float py = h.y * Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), e);
                    idx[i, j] = AddVertex(path[i] + x * px + y * py);
                }
            }
            for (int i = 0; i < n - 1; i++)
            {
                Vector3 axis = (path[i] + path[i + 1]) * 0.5f;
                for (int j = 0; j < seg; j++)
                {
                    int j1 = (j + 1) % seg;
                    int a = idx[i, j], b = idx[i, j1], c = idx[i + 1, j1], d = idx[i + 1, j];
                    Vector3 mid = (_verts[a] + _verts[b] + _verts[c] + _verts[d]) * 0.25f;
                    TriIdx(sub, a, b, c, mid - axis);
                    TriIdx(sub, a, c, d, mid - axis);
                }
            }
            if (!caps) return;
            for (int end = 0; end < 2; end++)
            {
                int i = end == 0 ? 0 : n - 1;
                Vector2 h = half[Mathf.Min(i, half.Count - 1)];
                if (h.x < 1e-4f || h.y < 1e-4f) continue;
                Vector3 dir = end == 0 ? path[0] - path[1] : path[n - 1] - path[n - 2];
                int center = AddVertex(path[i]);
                var ring = new int[seg];
                for (int j = 0; j < seg; j++) ring[j] = AddVertex(_verts[idx[i, j]]);   // flat cap: own vertices
                for (int j = 0; j < seg; j++)
                    TriIdx(sub, center, ring[j], ring[(j + 1) % seg], dir);
            }
        }

        /// <summary>Rotates v by the rotation that takes direction a onto direction b (Rodrigues).</summary>
        static Vector3 RotateBetween(Vector3 v, Vector3 a, Vector3 b)
        {
            Vector3 k = Vector3.Cross(a, b);
            float sin = k.magnitude, cos = Vector3.Dot(a, b);
            if (sin < 1e-6f) return v;
            k /= sin;
            return v * cos + Vector3.Cross(k, v) * sin + k * (Vector3.Dot(k, v) * (1f - cos));
        }

        /// <summary>Round tube along a polyline (frames, pipes, bars, spokes).</summary>
        public void Tube(int sub, IList<Vector3> path, float radius, int seg = 8, bool caps = true) =>
            Sweep(sub, path, new[] { new Vector2(radius, radius) }, seg, 2f, null, caps);

        public void Tube(int sub, Vector3 a, Vector3 b, float radius, int seg = 8) =>
            Tube(sub, new[] { a, b }, radius, seg);

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (_verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(_verts);
            mesh.subMeshCount = _subs.Length;
            for (int i = 0; i < _subs.Length; i++) mesh.SetTriangles(_subs[i], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Ear-clipping triangulation of a simple polygon (any winding). Returns index triples.</summary>
        public static List<int> Triangulate(IList<Vector2> poly)
        {
            var result = new List<int>();
            var idx = new List<int>();
            float area = 0f;
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % poly.Count];
                area += a.x * b.y - b.x * a.y;
            }
            for (int i = 0; i < poly.Count; i++) idx.Add(area > 0f ? i : poly.Count - 1 - i);

            int guard = 0;
            while (idx.Count > 3 && guard++ < 20000)
            {
                bool clipped = false;
                int n = idx.Count;
                for (int i = 0; i < n; i++)
                {
                    int ia = idx[(i - 1 + n) % n], ib = idx[i], ic = idx[(i + 1) % n];
                    Vector2 a = poly[ia], b = poly[ib], c = poly[ic];
                    if (Cross(b - a, c - b) <= 1e-7f) continue;          // reflex or flat
                    bool inside = false;
                    for (int k = 0; k < n && !inside; k++)
                    {
                        int ik = idx[k];
                        if (ik == ia || ik == ib || ik == ic) continue;
                        inside = InTri(poly[ik], a, b, c);
                    }
                    if (inside) continue;
                    result.Add(ia); result.Add(ib); result.Add(ic);
                    idx.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped)
                {
                    // only degenerate (collinear) corners left: drop the flattest one
                    int best = 0; float bestCross = float.MaxValue;
                    for (int i = 0; i < n; i++)
                    {
                        Vector2 a = poly[idx[(i - 1 + n) % n]], b = poly[idx[i]], c = poly[idx[(i + 1) % n]];
                        float cr = Mathf.Abs(Cross(b - a, c - b));
                        if (cr < bestCross) { bestCross = cr; best = i; }
                    }
                    idx.RemoveAt(best);
                }
            }
            if (idx.Count == 3) { result.Add(idx[0]); result.Add(idx[1]); result.Add(idx[2]); }
            return result;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(b - a, p - a), d2 = Cross(c - b, p - b), d3 = Cross(a - c, p - c);
            return d1 > 1e-7f && d2 > 1e-7f && d3 > 1e-7f;
        }
    }
}
