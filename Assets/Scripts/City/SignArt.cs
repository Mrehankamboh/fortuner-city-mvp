using System.Collections.Generic;
using UnityEngine;

namespace FortunerCity.City
{
    /// <summary>
    /// Motorway signs drawn in code: a small stroke font (A-Z, 0-9, arrows) rasterised with anti-aliasing into
    /// green direction boards and speed-limit discs. No font files or downloads.
    /// </summary>
    public static class SignArt
    {
        // Glyphs on a 4 x 6 grid, polylines separated by '|'
        static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            ['A'] = "0,0 2,6 4,0|0.8,2.2 3.2,2.2",
            ['B'] = "0,0 0,6 3,6 4,5.2 4,3.8 3,3 0,3|3,3 4,2.2 4,0.8 3,0 0,0",
            ['C'] = "4,5 3,6 1,6 0,5 0,1 1,0 3,0 4,1",
            ['D'] = "0,0 0,6 2.5,6 4,4.5 4,1.5 2.5,0 0,0",
            ['E'] = "4,6 0,6 0,0 4,0|0,3 3,3",
            ['F'] = "4,6 0,6 0,0|0,3 3,3",
            ['G'] = "4,5 3,6 1,6 0,5 0,1 1,0 3,0 4,1 4,2.8 2.2,2.8",
            ['H'] = "0,0 0,6|4,0 4,6|0,3 4,3",
            ['I'] = "2,0 2,6|1,6 3,6|1,0 3,0",
            ['J'] = "4,6 4,1 3,0 1,0 0,1",
            ['K'] = "0,0 0,6|4,6 0,2|1.4,3.4 4,0",
            ['L'] = "0,6 0,0 4,0",
            ['M'] = "0,0 0,6 2,3 4,6 4,0",
            ['N'] = "0,0 0,6 4,0 4,6",
            ['O'] = "1,0 0,1 0,5 1,6 3,6 4,5 4,1 3,0 1,0",
            ['P'] = "0,0 0,6 3,6 4,5 4,4 3,3 0,3",
            ['Q'] = "1,0 0,1 0,5 1,6 3,6 4,5 4,1 3,0 1,0|2.5,1.5 4,0",
            ['R'] = "0,0 0,6 3,6 4,5 4,4 3,3 0,3|2,3 4,0",
            ['S'] = "4,5 3,6 1,6 0,5 0,4 1,3 3,3 4,2 4,1 3,0 1,0 0,1",
            ['T'] = "0,6 4,6|2,6 2,0",
            ['U'] = "0,6 0,1 1,0 3,0 4,1 4,6",
            ['V'] = "0,6 2,0 4,6",
            ['W'] = "0,6 1,0 2,4 3,0 4,6",
            ['X'] = "0,0 4,6|0,6 4,0",
            ['Y'] = "0,6 2,3 4,6|2,3 2,0",
            ['Z'] = "0,6 4,6 0,0 4,0",
            ['0'] = "1,0 0,1 0,5 1,6 3,6 4,5 4,1 3,0 1,0",
            ['1'] = "1,5 2,6 2,0|1,0 3,0",
            ['2'] = "0,5 1,6 3,6 4,5 4,4 0,0 4,0",
            ['3'] = "0,5 1,6 3,6 4,5 4,4 3,3 1.5,3|3,3 4,2 4,1 3,0 1,0 0,1",
            ['4'] = "3,0 3,6 0,2 4,2",
            ['5'] = "4,6 0,6 0,3.4 3,3.4 4,2.4 4,1 3,0 1,0 0,1",
            ['6'] = "4,5 3,6 1,6 0,5 0,1 1,0 3,0 4,1 4,2.4 3,3.4 1,3.4 0,2.4",
            ['7'] = "0,6 4,6 1.5,0",
            ['8'] = "1,3 0,4 0,5 1,6 3,6 4,5 4,4 3,3 1,3 0,2 0,1 1,0 3,0 4,1 4,2 3,3",
            ['9'] = "0,1 1,0 3,0 4,1 4,5 3,6 1,6 0,5 0,3.6 1,2.6 3,2.6 4,3.6",
            ['-'] = "1,3 3,3",
            ['.'] = "2,0 2,0.15",
            ['^'] = "2,0 2,6|0.3,4.2 2,6 3.7,4.2",
            ['>'] = "0,3 4,3|2.2,4.8 4,3 2.2,1.2",
        };

        const float Advance = 5.6f;      // glyph width 4 + spacing
        const float Stroke = 0.48f;      // half stroke width in grid units

        static readonly Dictionary<char, List<Vector2[]>> Parsed = new Dictionary<char, List<Vector2[]>>();

        static List<Vector2[]> Strokes(char ch)
        {
            if (Parsed.TryGetValue(ch, out var list)) return list;
            list = new List<Vector2[]>();
            if (Glyphs.TryGetValue(ch, out var src))
            {
                foreach (var line in src.Split('|'))
                {
                    var pts = line.Split(' ');
                    var arr = new Vector2[pts.Length];
                    for (int i = 0; i < pts.Length; i++)
                    {
                        var xy = pts[i].Split(',');
                        arr[i] = new Vector2(float.Parse(xy[0], System.Globalization.CultureInfo.InvariantCulture),
                            float.Parse(xy[1], System.Globalization.CultureInfo.InvariantCulture));
                    }
                    list.Add(arr);
                }
            }
            Parsed[ch] = list;
            return list;
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>Draws text with its baseline-left corner at (x, y) pixels; unit = pixels per grid unit.</summary>
        static void Text(Color[] px, int w, int h, string text, float x, float y, float unit, Color ink)
        {
            float pad = Stroke + 1f / unit;
            for (int ci = 0; ci < text.Length; ci++)
            {
                var strokes = Strokes(char.ToUpperInvariant(text[ci]));
                float gx = x + ci * Advance * unit;
                if (strokes.Count == 0) continue;
                int x0 = Mathf.Max(0, Mathf.FloorToInt(gx - pad * unit)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt(gx + (4f + pad) * unit));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(y - pad * unit)), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(y + (6f + pad) * unit));
                for (int py = y0; py <= y1; py++)
                    for (int pxl = x0; pxl <= x1; pxl++)
                    {
                        var g = new Vector2((pxl + 0.5f - gx) / unit, (py + 0.5f - y) / unit);
                        float best = 99f;
                        foreach (var line in strokes)
                            for (int k = 0; k < line.Length - 1; k++)
                                best = Mathf.Min(best, SegDist(g, line[k], line[k + 1]));
                        float cover = Mathf.Clamp01((Stroke - best) * unit + 0.5f);
                        if (cover > 0f) px[py * w + pxl] = Color.Lerp(px[py * w + pxl], ink, cover);
                    }
            }
        }

        static Texture2D Tex(int w, int h, Color[] px)
        {
            var t = new Texture2D(w, h, TextureFormat.RGB24, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8
            };
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        static Material Make(Texture2D tex)
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(shader) { mainTexture = tex };
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.35f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.35f);
            // retro-reflective sheeting: a little self-light keeps the white legend readable in shadow
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", tex);
            m.SetColor("_EmissionColor", new Color(0.18f, 0.18f, 0.18f));
            return m;
        }

        static readonly Color Green = new Color(0.02f, 0.33f, 0.16f);
        static readonly Color White = new Color(0.95f, 0.95f, 0.93f);

        /// <summary>Green direction board: white border and one destination per line (e.g. "^ LAHORE 18 KM").</summary>
        public static Material DirectionSign(string[] lines, float aspect)
        {
            var px = DirectionPixels(lines, aspect, out int w, out int h);
            return Make(Tex(w, h, px));
        }

        static Color[] DirectionPixels(string[] lines, float aspect, out int w, out int h)
        {
            h = 256;
            w = Mathf.RoundToInt(h * aspect);
            var px = new Color[w * h];
            float border = h * 0.045f, edge = h * 0.02f, radius = h * 0.08f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // rounded white border inset from the edge
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - w * 0.5f) - (w * 0.5f - edge - radius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - h * 0.5f) - (h * 0.5f - edge - radius), 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;          // < 0 inside the border's outer edge
                    float n = (Hash(x, y) - 0.5f) * 0.02f;
                    var green = new Color(Green.r + n, Green.g + n, Green.b + n);
                    px[y * w + x] = d < -border * 0.6f ? green : d < 0f ? White : green;
                }

            int maxChars = 1;
            foreach (var l in lines) maxChars = Mathf.Max(maxChars, l.Length);
            float marginX = h * 0.16f, marginY = h * 0.16f;
            float unitH = (h - marginY * 2f) / (lines.Length * 6f + (lines.Length - 1) * 3f);
            float unitW = (w - marginX * 2f) / (maxChars * Advance - 1.6f);
            float unit = Mathf.Min(unitH, unitW);
            float lineStep = 9f * unit;
            float top = h * 0.5f + (lines.Length * 6f + (lines.Length - 1) * 3f) * unit * 0.5f;
            for (int i = 0; i < lines.Length; i++)
                Text(px, w, h, lines[i], marginX, top - 6f * unit - i * lineStep, unit, White);
            return px;
        }

        static Material _speed120;

        /// <summary>Round speed-limit sign: red ring, white disc, black numbers.</summary>
        public static Material SpeedLimit120 => _speed120 ??= SpeedLimit("120");

        static Material SpeedLimit(string text) => Make(Tex(256, 256, SpeedLimitPixels(text)));

        static Color[] SpeedLimitPixels(string text)
        {
            const int S = 256;
            var px = new Color[S * S];
            var red = new Color(0.78f, 0.06f, 0.06f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float r = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(S * 0.5f, S * 0.5f)) / (S * 0.5f);
                    px[y * S + x] = r > 0.97f ? new Color(0.6f, 0.6f, 0.6f) : r > 0.76f ? red : White;
                }
            float unit = S * 0.6f / (text.Length * Advance - 1.6f);
            float width = (text.Length * Advance - 1.6f) * unit;
            Text(px, S, S, text, S * 0.5f - width * 0.5f, S * 0.5f - 3f * unit, unit, new Color(0.05f, 0.05f, 0.05f));
            return px;
        }

        static float Hash(int x, int y)
        {
            unchecked
            {
                int n = x * 374761393 + y * 668265263;
                n = (n ^ (n >> 13)) * 1274126177;
                return ((n ^ (n >> 16)) & 0xffff) / 65535f;
            }
        }

        // ---------- meshes ----------

        /// <summary>
        /// Sign board centred at the origin, face toward +Z (submesh 0, reads correctly from the front),
        /// back and edges = submesh 1 (grey metal).
        /// </summary>
        public static Mesh Board(float width, float height, float depth)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>();
            var face = new List<int>(); var rest = new List<int>();
            float hx = width * 0.5f, hy = height * 0.5f, hz = depth * 0.5f;
            void Quad(List<int> sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 nrm, bool uvFace)
            {
                int i = v.Count;
                foreach (var p in new[] { a, b, c, d })
                {
                    v.Add(p); n.Add(nrm);
                    uv.Add(uvFace ? new Vector2(0.5f - p.x / width, 0.5f + p.y / height) : new Vector2(p.x, p.y));
                }
                bool flip = Vector3.Dot(Vector3.Cross(b - a, c - a), nrm) < 0f;
                if (!flip) { sub.Add(i); sub.Add(i + 1); sub.Add(i + 2); sub.Add(i); sub.Add(i + 2); sub.Add(i + 3); }
                else { sub.Add(i); sub.Add(i + 2); sub.Add(i + 1); sub.Add(i); sub.Add(i + 3); sub.Add(i + 2); }
            }
            Quad(face, new Vector3(-hx, -hy, hz), new Vector3(hx, -hy, hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz), Vector3.forward, true);
            Quad(rest, new Vector3(-hx, -hy, -hz), new Vector3(hx, -hy, -hz), new Vector3(hx, hy, -hz), new Vector3(-hx, hy, -hz), Vector3.back, false);
            Quad(rest, new Vector3(-hx, hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz), Vector3.up, false);
            Quad(rest, new Vector3(-hx, -hy, -hz), new Vector3(hx, -hy, -hz), new Vector3(hx, -hy, hz), new Vector3(-hx, -hy, hz), Vector3.down, false);
            Quad(rest, new Vector3(hx, -hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, hy, hz), new Vector3(hx, -hy, hz), Vector3.right, false);
            Quad(rest, new Vector3(-hx, -hy, -hz), new Vector3(-hx, hy, -hz), new Vector3(-hx, hy, hz), new Vector3(-hx, -hy, hz), Vector3.left, false);
            return Finish("SignBoard", v, n, uv, face, rest);
        }

        /// <summary>Round sign (disc) facing +Z, same submesh layout as <see cref="Board"/>.</summary>
        public static Mesh Disc(float radius, float depth, int seg = 32)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>();
            var face = new List<int>(); var rest = new List<int>();
            float hz = depth * 0.5f;
            int fc = v.Count; v.Add(new Vector3(0f, 0f, hz)); n.Add(Vector3.forward); uv.Add(new Vector2(0.5f, 0.5f));
            int bc = v.Count; v.Add(new Vector3(0f, 0f, -hz)); n.Add(Vector3.back); uv.Add(Vector2.zero);
            for (int j = 0; j < seg; j++)
            {
                float a0 = j / (float)seg * Mathf.PI * 2f, a1 = (j + 1) / (float)seg * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * radius, p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * radius;
                int i = v.Count;
                v.Add(p0 + Vector3.forward * hz); n.Add(Vector3.forward); uv.Add(new Vector2(0.5f - p0.x / (2f * radius), 0.5f + p0.y / (2f * radius)));
                v.Add(p1 + Vector3.forward * hz); n.Add(Vector3.forward); uv.Add(new Vector2(0.5f - p1.x / (2f * radius), 0.5f + p1.y / (2f * radius)));
                face.Add(fc); face.Add(i + 1); face.Add(i);          // clockwise seen from +Z = front face
                v.Add(p0 - Vector3.forward * hz); n.Add(Vector3.back); uv.Add(Vector2.zero);
                v.Add(p1 - Vector3.forward * hz); n.Add(Vector3.back); uv.Add(Vector2.zero);
                rest.Add(bc); rest.Add(i + 2); rest.Add(i + 3);
                Vector3 rn = (p0 + p1).normalized;
                int k = v.Count;
                v.Add(p0 + Vector3.forward * hz); v.Add(p1 + Vector3.forward * hz); v.Add(p1 - Vector3.forward * hz); v.Add(p0 - Vector3.forward * hz);
                for (int q = 0; q < 4; q++) { n.Add(rn); uv.Add(Vector2.zero); }
                rest.Add(k); rest.Add(k + 2); rest.Add(k + 1); rest.Add(k); rest.Add(k + 3); rest.Add(k + 2);
            }
            var mesh = Finish("SignDisc", v, n, uv, face, rest);
            FixWinding(mesh);
            return mesh;
        }

        static Mesh Finish(string name, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> face, List<int> rest)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(face, 0);
            mesh.SetTriangles(rest, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Makes every triangle face along its vertex normals (so hand-built discs never render inside out).</summary>
        static void FixWinding(Mesh mesh)
        {
            var v = mesh.vertices; var n = mesh.normals;
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var t = mesh.GetTriangles(s);
                for (int i = 0; i < t.Length; i += 3)
                {
                    Vector3 nrm = n[t[i]] + n[t[i + 1]] + n[t[i + 2]];
                    if (Vector3.Dot(Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]), nrm) < 0f)
                    { int tmp = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = tmp; }
                }
                mesh.SetTriangles(t, s);
            }
        }
    }
}
