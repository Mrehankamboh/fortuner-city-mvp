using UnityEngine;

namespace FortunerCity.City
{
    /// <summary>
    /// Procedural textures + shared materials for the city (generated in code — no downloads, no paid assets).
    /// Shared materials keep draw calls low and let static batching work.
    /// </summary>
    public static class CityArt
    {
        // Facade textures hold 8 bays x 8 floors
        public const float BayWidth = 3.2f;
        public const float FloorHeight = 3.5f;
        public const float FacadeTileU = BayWidth * 8f;
        public const float FacadeTileV = FloorHeight * 8f;
        public const float RoadTileLength = 12f;
        public const float StreetMarkWidth = 14f;      // Road2 texture spans a 14 m street
        public const float CarriagewayWidth = 11f;     // Road3 texture spans one 3-lane direction

        static Shader _shader;
        static Material _asphalt, _road2, _road3, _zebra, _paving, _concrete, _roof, _grass, _shopfront, _barrier, _signGreen, _leaves, _trunk, _water, _linePaint;
        static Material[] _glass, _office, _brick;

        public static Material Asphalt => _asphalt ??= Make(Noise(new Color(0.2f, 0.2f, 0.21f), 0.05f, 1), 0f, 0.2f);
        public static Material Road2 => _road2 ??= Make(RoadTex(false), 0f, 0.25f);
        public static Material Road3 => _road3 ??= Make(RoadTex(true), 0f, 0.25f);
        public static Material Zebra => _zebra ??= Make(ZebraTex(), 0f, 0.3f);
        public static Material Paving => _paving ??= Make(PavingTex(), 0f, 0.2f);
        public static Material Concrete => _concrete ??= Make(Noise(new Color(0.62f, 0.61f, 0.58f), 0.06f, 2), 0f, 0.15f);
        public static Material Roof => _roof ??= Make(Noise(new Color(0.3f, 0.3f, 0.31f), 0.08f, 3), 0f, 0.1f);
        public static Material Grass => _grass ??= Make(Noise(new Color(0.27f, 0.42f, 0.2f), 0.07f, 4), 0f, 0.1f);
        public static Material Shopfront => _shopfront ??= Make(ShopfrontTex(), 0.4f, 0.75f);
        public static Material Barrier => _barrier ??= Make(Noise(new Color(0.72f, 0.72f, 0.7f), 0.04f, 5), 0f, 0.2f);
        public static Material SignGreen => _signGreen ??= Make(Noise(new Color(0.05f, 0.38f, 0.18f), 0.02f, 6), 0.1f, 0.4f);
        public static Material Leaves => _leaves ??= Make(Noise(new Color(0.18f, 0.42f, 0.2f), 0.08f, 7), 0f, 0.15f);
        public static Material Trunk => _trunk ??= Make(Noise(new Color(0.33f, 0.22f, 0.13f), 0.05f, 8), 0f, 0.1f);
        public static Material Water => _water ??= Make(Noise(new Color(0.2f, 0.42f, 0.55f), 0.03f, 9), 0.3f, 0.9f);
        public static Material LinePaint => _linePaint ??= Make(Noise(new Color(0.88f, 0.88f, 0.86f), 0.03f, 10), 0f, 0.3f);

        public static Material RandomGlass(System.Random rng)
        {
            if (_glass == null)
            {
                _glass = new[]
                {
                    Make(GlassTex(new Color(0.32f, 0.48f, 0.62f), 11), 0.55f, 0.88f),  // blue
                    Make(GlassTex(new Color(0.28f, 0.52f, 0.52f), 12), 0.55f, 0.88f),  // teal
                    Make(GlassTex(new Color(0.22f, 0.25f, 0.29f), 13), 0.6f, 0.9f),    // dark grey
                    Make(GlassTex(new Color(0.48f, 0.4f, 0.3f), 14), 0.55f, 0.85f),    // bronze
                    Make(GlassTex(new Color(0.58f, 0.62f, 0.66f), 15), 0.6f, 0.9f),    // silver
                };
            }
            return _glass[rng.Next(_glass.Length)];
        }

        public static Material RandomOffice(System.Random rng)
        {
            if (_office == null)
            {
                _office = new[]
                {
                    Make(OfficeTex(new Color(0.78f, 0.74f, 0.64f), 21), 0.05f, 0.35f),  // sandstone
                    Make(OfficeTex(new Color(0.66f, 0.66f, 0.66f), 22), 0.05f, 0.35f),  // concrete
                    Make(OfficeTex(new Color(0.88f, 0.87f, 0.84f), 23), 0.05f, 0.35f),  // white
                    Make(OfficeTex(new Color(0.55f, 0.5f, 0.45f), 24), 0.05f, 0.35f),   // taupe
                };
            }
            return _office[rng.Next(_office.Length)];
        }

        public static Material RandomBrick(System.Random rng)
        {
            if (_brick == null)
            {
                _brick = new[]
                {
                    Make(BrickTex(new Color(0.55f, 0.27f, 0.2f), 31), 0f, 0.2f),
                    Make(BrickTex(new Color(0.62f, 0.45f, 0.33f), 32), 0f, 0.2f),
                    Make(BrickTex(new Color(0.42f, 0.3f, 0.26f), 33), 0f, 0.2f),
                };
            }
            return _brick[rng.Next(_brick.Length)];
        }

        static Material Make(Texture2D tex, float metallic, float smoothness)
        {
            if (_shader == null) _shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(_shader) { mainTexture = tex, enableInstancing = true };
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        // ---------- texture generators ----------

        static Texture2D NewTex(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, true)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8
            };
        }

        static Texture2D Finish(Texture2D t, Color[] px)
        {
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int n = x * 374761393 + y * 668265263 + seed * 1442695041;
                n = (n ^ (n >> 13)) * 1274126177;
                return ((n ^ (n >> 16)) & 0xffff) / 65535f;
            }
        }

        static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

        static Texture2D Noise(Color baseColor, float amount, int seed)
        {
            const int S = 64;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                    px[y * S + x] = Shade(baseColor, 1f + (Hash(x, y, seed) - 0.5f) * amount * 4f);
            return Finish(NewTex(S, S), px);
        }

        static bool Band(float u, float start, float width) => u >= start && u < start + width;

        /// <summary>u = across the road (metres), v = along (one 12 m tile).</summary>
        static Texture2D RoadTex(bool oneWay)
        {
            const int W = 256, H = 256;
            var px = new Color[W * H];
            float widthM = oneWay ? CarriagewayWidth : StreetMarkWidth;
            var white = new Color(0.9f, 0.9f, 0.88f);
            var yellow = new Color(0.92f, 0.75f, 0.2f);
            for (int y = 0; y < H; y++)
            {
                float v = (y + 0.5f) / H;
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W * widthM;
                    float n = (Hash(x, y, 40) - 0.5f) * 0.06f;
                    var c = new Color(0.2f + n, 0.2f + n, 0.215f + n);
                    Color? paint = null;
                    if (oneWay)
                    {
                        // median side (u=0) solid yellow, kerb side solid white, 2 dashed lane dividers
                        if (Band(u, 0.3f, 0.15f)) paint = yellow;
                        else if (Band(u, widthM - 0.45f, 0.15f)) paint = white;
                        else if ((Band(u, 3.925f, 0.15f) || Band(u, 7.425f, 0.15f)) && v < 0.35f) paint = white;
                    }
                    else
                    {
                        // two-way street: edge lines + dashed centre line
                        if (Band(u, 1.0f, 0.15f) || Band(u, widthM - 1.15f, 0.15f)) paint = white;
                        else if (Band(u, widthM * 0.5f - 0.075f, 0.15f) && v < 0.45f) paint = white;
                    }
                    if (paint.HasValue)
                        c = Color.Lerp(paint.Value, c, 0.15f + Hash(x / 3, y / 3, 41) * 0.2f);
                    px[y * W + x] = c;
                }
            }
            return Finish(NewTex(W, H), px);
        }

        /// <summary>One 1 m stripe period across u (white half / asphalt half).</summary>
        static Texture2D ZebraTex()
        {
            const int S = 64;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float n = (Hash(x, y, 50) - 0.5f) * 0.06f;
                    px[y * S + x] = x < S / 2 ? new Color(0.88f + n, 0.88f + n, 0.86f + n) : new Color(0.2f + n, 0.2f + n, 0.215f + n);
                }
            return Finish(NewTex(S, S), px);
        }

        /// <summary>4 x 4 paving slabs (texture covers 4 m).</summary>
        static Texture2D PavingTex()
        {
            const int S = 128, T = 32;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    bool grout = x % T == 0 || y % T == 0;
                    float slab = (Hash(x / T, y / T, 60) - 0.5f) * 0.08f;
                    float n = (Hash(x, y, 61) - 0.5f) * 0.05f;
                    float g = grout ? 0.45f : 0.66f + slab + n;
                    px[y * S + x] = new Color(g, g * 0.99f, g * 0.96f);
                }
            return Finish(NewTex(S, S), px);
        }

        const int Bay = 32;   // pixels per bay / floor

        static Texture2D GlassTex(Color glass, int seed)
        {
            const int S = Bay * 8;
            var px = new Color[S * S];
            var frame = new Color(0.16f, 0.17f, 0.18f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int bx = x / Bay, fy = y / Bay, lx = x % Bay, ly = y % Bay;
                    Color c;
                    if (lx < 2 || ly < 2) c = frame;                              // mullions
                    else if (ly < 8) c = Shade(glass, 0.55f);                     // spandrel between floors
                    else
                    {
                        float var = (Hash(bx, fy, seed) - 0.5f) * 0.18f;
                        float sky = (ly - 8) / 24f * 0.14f;                       // lighter at the top of each pane
                        c = Shade(glass, 1f + var + sky);
                        if (Hash(bx, fy, seed + 7) > 0.9f) c = Color.Lerp(c, new Color(0.85f, 0.82f, 0.72f), 0.35f); // blinds
                    }
                    px[y * S + x] = c;
                }
            return Finish(NewTex(S, S), px);
        }

        static Texture2D OfficeTex(Color wall, int seed)
        {
            const int S = Bay * 8;
            var px = new Color[S * S];
            var glass = new Color(0.22f, 0.3f, 0.38f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int bx = x / Bay, fy = y / Bay, lx = x % Bay, ly = y % Bay;
                    float n = (Hash(x, y, seed) - 0.5f) * 0.05f;
                    Color c = Shade(wall, 1f + n);
                    if (lx >= 6 && lx < 26 && ly >= 9 && ly < 27)
                    {
                        float var = (Hash(bx, fy, seed + 3) - 0.5f) * 0.25f;
                        c = Shade(glass, 1f + var + (ly - 9) / 18f * 0.2f);
                        if (lx == 16) c = Shade(wall, 0.7f);                      // window divider
                    }
                    else if (ly == 8 && lx >= 5 && lx < 27) c = Shade(wall, 1.12f); // sill
                    px[y * S + x] = c;
                }
            return Finish(NewTex(S, S), px);
        }

        static Texture2D BrickTex(Color brick, int seed)
        {
            const int S = Bay * 8;
            var px = new Color[S * S];
            var glass = new Color(0.2f, 0.26f, 0.32f);
            var frameC = new Color(0.85f, 0.85f, 0.82f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int bx = x / Bay, fy = y / Bay, lx = x % Bay, ly = y % Bay;
                    int row = y / 4;
                    bool mortar = y % 4 == 0 || (x + (row % 2) * 4) % 8 == 0;
                    float n = (Hash(x / 8, row, seed) - 0.5f) * 0.18f;
                    Color c = mortar ? new Color(0.62f, 0.6f, 0.56f) : Shade(brick, 1f + n);
                    if (lx >= 8 && lx < 24 && ly >= 9 && ly < 26)
                    {
                        bool frame = lx == 8 || lx == 23 || ly == 9 || ly == 25;
                        c = frame ? frameC : Shade(glass, 1f + (Hash(bx, fy, seed + 5) - 0.5f) * 0.3f);
                    }
                    px[y * S + x] = c;
                }
            return Finish(NewTex(S, S), px);
        }

        static Texture2D ShopfrontTex()
        {
            const int S = Bay * 8;
            var px = new Color[S * S];
            var glass = new Color(0.16f, 0.22f, 0.26f);
            var frame = new Color(0.12f, 0.12f, 0.13f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int bx = x / Bay, lx = x % Bay, ly = y % Bay;
                    Color c;
                    if (ly < 3) c = new Color(0.35f, 0.35f, 0.36f);                // plinth
                    else if (lx < 2 || ly > 28) c = frame;
                    else c = Shade(glass, 1f + (Hash(bx, y / Bay, 70) - 0.5f) * 0.2f + (ly - 3) / 26f * 0.25f);
                    px[y * S + x] = c;
                }
            return Finish(NewTex(S, S), px);
        }
    }
}
