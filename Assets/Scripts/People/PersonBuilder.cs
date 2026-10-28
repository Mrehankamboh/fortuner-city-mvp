using System.Collections.Generic;
using UnityEngine;
using FortunerCity.Vehicle;

namespace FortunerCity.People
{
    public enum PersonProp { None, Phone, Bag }

    public struct PersonLook
    {
        public Color skin, hair, top, bottom, shoes, accent, vest;
        public bool kameez;       // long tunic to the knees (shalwar kameez)
        public bool female;
        public bool elderly;
        public bool longSleeves;
        public bool dupatta;      // scarf over the shoulders (accent colour)
        public bool headScarf;    // dupatta worn over the head
        public int facialHair;    // 0 clean-shaven, 1 moustache, 2 beard
        public bool cap;          // white prayer cap (topi)
        public bool waistcoat;    // sleeveless waistcoat over the kameez
        public PersonProp prop;
        public bool texting;      // phone held in front, head down (instead of at the ear)
        public float height;      // metres
        public float build;       // 0.9 slim … 1.15 heavy
    }

    public class PersonRig
    {
        public Transform root, body, head;
        public Transform leftArm, rightArm, leftForearm, rightForearm;
        public Transform leftLeg, rightLeg, leftShin, rightShin, leftFoot, rightFoot;
        public bool kameez, female, elderly, texting;
        public PersonProp prop;
        public bool riding;        // seated on a bike: knees up, hands on the bars
        public int weaponPose;     // 0 none, 1 pistol (one hand), 2 long gun (two hands)
        public float aimPitch;     // degrees, + = aim up
        public System.Action<bool> onStep;   // foot plant (true = running) — used for footstep sounds
    }

    /// <summary>
    /// People built in code (no downloaded models) with smooth-shaded anatomy: chest, waist and hips,
    /// sloping shoulders, muscle shapes on arms and calves, a modelled face (cheekbones, jaw, chin, nose,
    /// lips, eyes, brows, ears), mitten hands with thumbs and shaped shoes on a separate ankle joint.
    /// Root = feet, forward = +Z. Meshes are shared per body variant; colours come from materials.
    /// </summary>
    public static class PersonBuilder
    {
        // Torso submeshes
        const int Top = 0, Skin = 1, Bottom = 2, Accent = 3, Vest = 4, White = 5;
        // Head submeshes
        const int HSkin = 0, HHair = 1, HDark = 2, HWhite = 3, HAccent = 4, HLip = 5;
        const float NeckY = 1.50f;   // head pivot

        static Color C(float r, float g, float b) => new Color(r, g, b);
        static Vector4 R(float width, float depth, float y, float forward = 0f) => new Vector4(width, depth, y, forward);

        static readonly Color[] Skins = { C(0.87f, 0.72f, 0.58f), C(0.78f, 0.6f, 0.45f), C(0.66f, 0.47f, 0.33f), C(0.5f, 0.35f, 0.24f), C(0.4f, 0.27f, 0.19f) };
        static readonly Color[] DarkHair = { C(0.04f, 0.035f, 0.03f), C(0.12f, 0.08f, 0.05f), C(0.22f, 0.14f, 0.08f) };
        static readonly Color GreyHair = C(0.62f, 0.6f, 0.58f);
        static readonly Color[] Shirts =
        {
            C(0.92f, 0.92f, 0.9f), C(0.1f, 0.1f, 0.11f), C(0.14f, 0.2f, 0.36f), C(0.48f, 0.12f, 0.14f), C(0.35f, 0.4f, 0.24f),
            C(0.55f, 0.7f, 0.86f), C(0.78f, 0.7f, 0.56f), C(0.46f, 0.47f, 0.5f), C(0.18f, 0.32f, 0.22f)
        };
        static readonly Color[] KameezColors =
        {
            C(0.93f, 0.93f, 0.9f), C(0.72f, 0.66f, 0.55f), C(0.32f, 0.36f, 0.42f), C(0.2f, 0.22f, 0.26f), C(0.55f, 0.6f, 0.66f),  // men
            C(0.75f, 0.2f, 0.35f), C(0.9f, 0.62f, 0.2f), C(0.2f, 0.45f, 0.5f), C(0.55f, 0.3f, 0.6f), C(0.85f, 0.75f, 0.6f)   // women
        };
        static readonly Color[] Trousers = { C(0.17f, 0.24f, 0.4f), C(0.09f, 0.09f, 0.1f), C(0.58f, 0.51f, 0.39f), C(0.35f, 0.35f, 0.37f) };
        static readonly Color[] ShoeColors = { C(0.06f, 0.05f, 0.05f), C(0.32f, 0.2f, 0.11f), C(0.88f, 0.88f, 0.88f) };
        static readonly Color[] Scarves = { C(0.95f, 0.85f, 0.75f), C(0.85f, 0.25f, 0.3f), C(0.15f, 0.15f, 0.2f), C(0.3f, 0.55f, 0.45f), C(0.95f, 0.75f, 0.3f) };
        static readonly Color[] Vests = { C(0.12f, 0.12f, 0.14f), C(0.3f, 0.22f, 0.15f), C(0.22f, 0.25f, 0.3f) };

        static readonly Dictionary<string, Mesh> MeshCache = new Dictionary<string, Mesh>();
        static readonly Dictionary<Color, Material> Mats = new Dictionary<Color, Material>();

        public static PersonLook RandomLook(System.Random rng)
        {
            bool female = rng.NextDouble() < 0.45;
            bool elderly = rng.NextDouble() < 0.15;
            bool kameez = female ? rng.NextDouble() < 0.8 : rng.NextDouble() < 0.45;
            var look = new PersonLook
            {
                female = female,
                elderly = elderly,
                kameez = kameez,
                skin = Skins[rng.Next(Skins.Length)],
                hair = elderly ? GreyHair : DarkHair[rng.Next(DarkHair.Length)],
                shoes = ShoeColors[rng.Next(ShoeColors.Length)],
                longSleeves = kameez || rng.NextDouble() < 0.4,
                height = (female ? 1.53f + (float)rng.NextDouble() * 0.17f : 1.64f + (float)rng.NextDouble() * 0.22f) - (elderly ? 0.03f : 0f),
                build = 0.92f + (float)rng.NextDouble() * (elderly ? 0.25f : 0.18f),
                vest = Vests[rng.Next(Vests.Length)],
            };
            if (kameez)
            {
                look.top = female ? KameezColors[5 + rng.Next(5)] : KameezColors[rng.Next(5)];
                look.bottom = rng.NextDouble() < 0.6 ? look.top : C(0.9f, 0.9f, 0.88f);   // matching or white shalwar
            }
            else
            {
                look.top = Shirts[rng.Next(Shirts.Length)];
                look.bottom = Trousers[rng.Next(Trousers.Length)];
            }
            look.accent = Scarves[rng.Next(Scarves.Length)];
            if (female)
            {
                look.dupatta = rng.NextDouble() < 0.75;
                look.headScarf = look.dupatta && rng.NextDouble() < 0.45;
            }
            else
            {
                double fh = rng.NextDouble();
                look.facialHair = elderly || fh < 0.35 ? 2 : fh < 0.6 ? 1 : 0;
                look.cap = kameez && rng.NextDouble() < (elderly ? 0.5 : 0.18);
                look.waistcoat = kameez && rng.NextDouble() < 0.28;
            }
            double p = rng.NextDouble();
            look.prop = p < 0.15 ? PersonProp.Phone : p < 0.3 ? PersonProp.Bag : PersonProp.None;
            look.texting = look.prop == PersonProp.Phone && rng.NextDouble() < 0.5;
            return look;
        }

        public static PersonRig Build(Transform parent, PersonLook look, string name)
        {
            var rig = new PersonRig
            {
                kameez = look.kameez, female = look.female, elderly = look.elderly,
                prop = look.prop, texting = look.texting
            };
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            rig.root = root;

            var model = new GameObject("Model").transform;
            model.SetParent(root, false);
            float s = look.height / 1.75f;
            model.localScale = new Vector3(s * look.build, s, s * look.build);
            rig.body = model;

            string g = look.female ? "F" : "M";
            var white = C(0.95f, 0.95f, 0.93f);
            var lip = new Color(look.skin.r * 0.8f, look.skin.g * 0.55f, look.skin.b * 0.55f);
            Part(model, "Body", Vector3.zero, Get($"torso{g}{look.kameez}{look.dupatta}{look.waistcoat}", () => BuildTorso(look)),
                new[] { Mat(look.top), Mat(look.skin), Mat(look.bottom), Mat(look.accent), Mat(look.vest), Mat(white) });
            rig.head = Part(model, "Head", new Vector3(0f, NeckY, 0f), Get($"head{g}{look.facialHair}{look.cap}{look.headScarf}", () => BuildHead(look)),
                new[] { Mat(look.skin), Mat(look.hair), Mat(C(0.05f, 0.04f, 0.04f)), Mat(white), Mat(look.accent), Mat(lip) });

            var armMats = new[] { Mat(look.top), Mat(look.skin) };
            var foreMats = new[] { Mat(look.longSleeves ? look.top : look.skin), Mat(look.skin) };
            float sx = look.female ? 0.19f : 0.212f;
            rig.leftArm = Part(model, "ArmL", new Vector3(-sx, 1.395f, 0f), Get("upperArm", BuildUpperArm), armMats);
            rig.rightArm = Part(model, "ArmR", new Vector3(sx, 1.395f, 0f), Get("upperArm", BuildUpperArm), armMats);
            rig.leftForearm = Part(rig.leftArm, "ForearmL", new Vector3(0f, -0.29f, 0f), Get("forearmL", () => BuildForearm(-1f)), foreMats);
            rig.rightForearm = Part(rig.rightArm, "ForearmR", new Vector3(0f, -0.29f, 0f), Get("forearmR", () => BuildForearm(1f)), foreMats);

            bool loose = look.kameez;   // shalwar legs are baggy
            var legMats = new[] { Mat(look.bottom), Mat(look.shoes) };
            var footMats = new[] { Mat(look.shoes) };
            rig.leftLeg = Part(model, "LegL", new Vector3(-0.09f, 0.90f, 0f), Get($"thigh{loose}", () => BuildThigh(loose)), legMats);
            rig.rightLeg = Part(model, "LegR", new Vector3(0.09f, 0.90f, 0f), Get($"thigh{loose}", () => BuildThigh(loose)), legMats);
            rig.leftShin = Part(rig.leftLeg, "ShinL", new Vector3(0f, -0.44f, 0f), Get($"shin{loose}", () => BuildShin(loose)), legMats);
            rig.rightShin = Part(rig.rightLeg, "ShinR", new Vector3(0f, -0.44f, 0f), Get($"shin{loose}", () => BuildShin(loose)), legMats);
            rig.leftFoot = Part(rig.leftShin, "FootL", new Vector3(0f, -0.40f, 0f), Get("foot", BuildFoot), footMats);
            rig.rightFoot = Part(rig.rightShin, "FootR", new Vector3(0f, -0.40f, 0f), Get("foot", BuildFoot), footMats);

            if (look.prop == PersonProp.Phone)
                Part(rig.rightForearm, "Phone", new Vector3(0f, -0.31f, 0.025f), Get("phone", BuildPhone), new[] { Mat(C(0.05f, 0.05f, 0.06f)) });
            else if (look.prop == PersonProp.Bag)
                Part(rig.rightForearm, "Bag", new Vector3(0f, -0.35f, 0f), Get("bag", BuildBag), new[] { Mat(C(0.92f, 0.9f, 0.85f)), Mat(C(0.55f, 0.35f, 0.2f)) });

            var anim = root.gameObject.AddComponent<PersonAnimator>();
            anim.rig = rig;
            return rig;
        }

        /// <summary>Seats a person on a bike: hips on the seat, knees up, hands on the bars.</summary>
        public static GameObject BuildRider(Transform seat, PersonLook look)
        {
            look.prop = PersonProp.None;
            look.texting = false;
            look.dupatta = false;
            look.headScarf = false;
            var rig = Build(seat, look, "Rider");
            rig.riding = true;
            float s = look.height / 1.75f;
            rig.root.localPosition = new Vector3(0f, -0.9f * s + 0.04f, -0.04f);
            rig.root.localRotation = Quaternion.identity;
            return rig.root.gameObject;
        }

        static Transform Part(Transform parent, string name, Vector3 pivot, Mesh mesh, Material[] mats)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pivot;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = mats;
            return go.transform;
        }

        static Mesh Get(string key, System.Func<Mesh> build)
        {
            if (!MeshCache.TryGetValue(key, out var m)) MeshCache[key] = m = build();
            return m;
        }

        static void Ball(MeshBuilder mb, int sub, Vector3 c, float r, int seg = 12)
        {
            var rings = new Vector4[7];
            for (int i = 0; i < 7; i++)
            {
                float t = Mathf.Lerp(Mathf.PI, 0f, i / 6f);
                rings[i] = R(r * Mathf.Sin(t), r * Mathf.Sin(t), -r * Mathf.Cos(t));
            }
            mb.SmoothLathe(sub, rings, seg, c, false, false);
        }

        static Vector4[] Scale(Vector4[] rings, float w, float d)
        {
            var o = new Vector4[rings.Length];
            for (int i = 0; i < rings.Length; i++) o[i] = new Vector4(rings[i].x * w, rings[i].y * d, rings[i].z, rings[i].w);
            return o;
        }

        static Vector4[] Concat(Vector4[] a, Vector4[] b)
        {
            var o = new Vector4[a.Length + b.Length];
            a.CopyTo(o, 0);
            b.CopyTo(o, a.Length);
            return o;
        }

        // ---------- torso ----------

        static readonly Vector4[] MaleChest =
        {
            R(0.15f, 0.097f, 1.02f, 0.004f), R(0.147f, 0.096f, 1.10f, 0.008f), R(0.163f, 0.106f, 1.19f, 0.014f),
            R(0.18f, 0.116f, 1.27f, 0.018f), R(0.19f, 0.112f, 1.34f, 0.01f), R(0.2f, 0.097f, 1.395f, 0f),
            R(0.168f, 0.078f, 1.435f, -0.005f), R(0.075f, 0.052f, 1.465f, 0f), R(0f, 0f, 1.47f, 0f)
        };

        static readonly Vector4[] FemaleChest =
        {
            R(0.15f, 0.1f, 1.02f, 0f), R(0.13f, 0.088f, 1.10f, 0.005f), R(0.148f, 0.1f, 1.19f, 0.012f),
            R(0.16f, 0.118f, 1.26f, 0.03f), R(0.166f, 0.112f, 1.32f, 0.022f), R(0.178f, 0.092f, 1.375f, 0.004f),
            R(0.15f, 0.07f, 1.425f, 0f), R(0.065f, 0.046f, 1.455f, 0f), R(0f, 0f, 1.46f, 0f)
        };

        static Mesh BuildTorso(PersonLook look)
        {
            var mb = new MeshBuilder(6);
            bool f = look.female;
            var id = Quaternion.identity;
            const int Seg = 20;
            var chest = f ? FemaleChest : MaleChest;

            if (look.kameez)
            {
                // Tunic from the knees to the neck, flaring at the hem
                var skirt = f
                    ? new[] { R(0.245f, 0.16f, 0.50f), R(0.225f, 0.15f, 0.62f), R(0.205f, 0.135f, 0.75f), R(0.185f, 0.122f, 0.88f, -0.01f), R(0.165f, 0.11f, 0.97f, -0.004f) }
                    : new[] { R(0.232f, 0.155f, 0.52f), R(0.215f, 0.142f, 0.62f), R(0.2f, 0.13f, 0.75f), R(0.18f, 0.118f, 0.88f, -0.005f), R(0.17f, 0.11f, 0.97f) };
                mb.SmoothLathe(Top, Concat(skirt, chest), Seg, Vector3.zero);
                mb.Box(White, new Vector3(0f, 1.32f, f ? 0.138f : 0.124f), new Vector3(0.022f, 0.2f, 0.006f), Quaternion.Euler(-6f, 0f, 0f));   // placket
                if (look.waistcoat)
                {
                    var vest = new[]
                    {
                        R(0.162f, 0.107f, 1.0f, 0.004f), R(0.159f, 0.106f, 1.10f, 0.008f), R(0.175f, 0.116f, 1.19f, 0.014f),
                        R(0.192f, 0.126f, 1.27f, 0.018f), R(0.202f, 0.122f, 1.34f, 0.01f), R(0.212f, 0.107f, 1.395f, 0f)
                    };
                    mb.SmoothLathe(Vest, vest, Seg, Vector3.zero, false, false);
                    mb.Box(Top, new Vector3(0f, 1.2f, 0.133f), new Vector3(0.07f, 0.4f, 0.006f), id);    // kameez in the vest opening
                }
            }
            else
            {
                var hips = f
                    ? new[] { R(0.14f, 0.1f, 0.84f), R(0.172f, 0.118f, 0.88f, -0.012f), R(0.178f, 0.12f, 0.925f, -0.018f), R(0.165f, 0.11f, 0.97f, -0.008f) }
                    : new[] { R(0.13f, 0.095f, 0.84f), R(0.155f, 0.108f, 0.875f, -0.004f), R(0.163f, 0.112f, 0.92f, -0.012f), R(0.158f, 0.106f, 0.97f, -0.004f) };
                mb.SmoothLathe(Bottom, hips, Seg, Vector3.zero);
                var waist = f ? new[] { R(0.168f, 0.11f, 0.94f, -0.01f) } : new[] { R(0.155f, 0.102f, 0.95f) };
                mb.SmoothLathe(Top, Concat(waist, chest), Seg, Vector3.zero);
                if (!f)
                {
                    mb.Box(Top, new Vector3(0.045f, 1.452f, 0.052f), new Vector3(0.07f, 0.03f, 0.02f), Quaternion.Euler(0f, -25f, 0f));   // collar
                    mb.Box(Top, new Vector3(-0.045f, 1.452f, 0.052f), new Vector3(0.07f, 0.03f, 0.02f), Quaternion.Euler(0f, 25f, 0f));
                }
            }

            // shoulder caps hide the joint when the arms swing
            float sx = f ? 0.19f : 0.212f;
            Ball(mb, Top, new Vector3(sx, 1.395f, 0f), 0.056f);
            Ball(mb, Top, new Vector3(-sx, 1.395f, 0f), 0.056f);

            // neck
            mb.SmoothLathe(Skin, new[] { R(0.047f, 0.047f, 1.43f, 0.005f), R(0.043f, 0.045f, 1.5f, 0.008f), R(0.045f, 0.047f, 1.56f, 0.01f) }, 12, Vector3.zero);

            if (look.dupatta)
            {
                // scarf: loop across the chest and down the back
                mb.Box(Accent, new Vector3(0f, 1.36f, 0.115f), new Vector3(0.32f, 0.07f, 0.04f), Quaternion.Euler(-14f, 0f, 0f));
                mb.Box(Accent, new Vector3(0f, 1.18f, -0.1f), new Vector3(0.32f, 0.5f, 0.03f), id);
                mb.Box(Accent, new Vector3(0.15f, 1.12f, 0.1f), new Vector3(0.06f, 0.42f, 0.03f), id);
            }
            return mb.ToMesh("PersonTorso");
        }

        // ---------- head ----------

        static readonly Vector4[] Skull =
        {
            R(0f, 0f, 0.036f, 0.052f), R(0.032f, 0.026f, 0.042f, 0.056f), R(0.058f, 0.062f, 0.062f, 0.034f),
            R(0.072f, 0.084f, 0.088f, 0.016f), R(0.082f, 0.097f, 0.118f, 0.006f), R(0.088f, 0.103f, 0.15f),
            R(0.087f, 0.104f, 0.178f, -0.005f), R(0.078f, 0.097f, 0.208f, -0.01f), R(0.056f, 0.072f, 0.234f, -0.014f),
            R(0.022f, 0.028f, 0.251f, -0.015f), R(0f, 0f, 0.255f, -0.015f)
        };

        /// <summary>Head in neck-pivot space (y up from the neck joint, +z = face).</summary>
        static Mesh BuildHead(PersonLook look)
        {
            var mb = new MeshBuilder(6);
            bool f = look.female;
            var id = Quaternion.identity;
            float w = f ? 0.94f : 1f, d = f ? 0.96f : 1f;
            mb.SmoothLathe(HSkin, Scale(Skull, w, d), 20, Vector3.zero, false, false);

            // nose (bridge + tip + nostrils), ears
            mb.Box(HSkin, new Vector3(0f, 0.136f, 0.104f * d), new Vector3(0.02f, 0.042f, 0.03f), Quaternion.Euler(-20f, 0f, 0f));
            mb.Box(HSkin, new Vector3(0f, 0.116f, 0.113f * d), new Vector3(0.03f, 0.022f, 0.026f), id);
            mb.Box(HSkin, new Vector3(0f, 0.107f, 0.106f * d), new Vector3(0.036f, 0.012f, 0.02f), id);
            for (int side = -1; side <= 1; side += 2)
            {
                mb.Box(HSkin, new Vector3(side * 0.086f * w, 0.142f, -0.005f), new Vector3(0.018f, 0.055f, 0.034f), Quaternion.Euler(0f, side * 12f, 0f));
                // eyes: white + dark iris, brow above
                mb.Box(HWhite, new Vector3(side * 0.032f * w, 0.152f, 0.101f * d), new Vector3(0.03f, 0.015f, 0.008f), id);
                mb.Box(HDark, new Vector3(side * 0.032f * w, 0.152f, 0.106f * d), new Vector3(0.012f, 0.012f, 0.004f), id);
                mb.Box(HHair, new Vector3(side * 0.033f * w, 0.172f, 0.103f * d), new Vector3(0.034f, 0.008f, 0.01f), Quaternion.Euler(0f, 0f, side * -6f));
            }

            // lips + mouth line
            float lipZ = (look.facialHair == 2 ? 0.106f : 0.099f) * d;
            mb.Box(HLip, new Vector3(0f, 0.094f, lipZ), new Vector3(0.042f, 0.008f, 0.012f), id);
            mb.Box(HLip, new Vector3(0f, 0.083f, lipZ - 0.002f), new Vector3(0.038f, 0.011f, 0.013f), id);
            mb.Box(HDark, new Vector3(0f, 0.089f, lipZ + 0.004f), new Vector3(0.034f, 0.003f, 0.006f), id);

            // hair: smooth cap with a hairline above the brows, fuller at the back
            var cap = new[] { R(0.093f, 0.111f, 0.165f, -0.004f), R(0.093f, 0.11f, 0.18f, -0.006f), R(0.083f, 0.103f, 0.21f, -0.011f), R(0.06f, 0.077f, 0.237f, -0.015f), R(0.024f, 0.03f, 0.256f, -0.016f), R(0f, 0f, 0.26f, -0.016f) };
            mb.SmoothLathe(HHair, Scale(cap, w, d), 20, Vector3.zero, false, false);
            mb.Box(HHair, new Vector3(0f, 0.15f, -0.078f), new Vector3(0.17f * w, 0.11f, 0.06f), id);
            mb.Box(HHair, new Vector3(0.088f * w, 0.168f, -0.012f), new Vector3(0.02f, 0.05f, 0.07f), id);
            mb.Box(HHair, new Vector3(-0.088f * w, 0.168f, -0.012f), new Vector3(0.02f, 0.05f, 0.07f), id);
            if (f && !look.headScarf)
            {
                mb.Box(HHair, new Vector3(0f, -0.03f, -0.085f), new Vector3(0.19f, 0.3f, 0.06f), id);          // long hair down the back
                mb.Box(HHair, new Vector3(0.085f, 0.075f, -0.01f), new Vector3(0.025f, 0.16f, 0.1f), id);
                mb.Box(HHair, new Vector3(-0.085f, 0.075f, -0.01f), new Vector3(0.025f, 0.16f, 0.1f), id);
            }

            if (look.facialHair == 2)
                mb.SmoothLathe(HHair, new[] { R(0f, 0f, 0.03f, 0.054f), R(0.036f, 0.03f, 0.038f, 0.059f), R(0.064f, 0.068f, 0.06f, 0.036f), R(0.078f, 0.092f, 0.088f, 0.018f), R(0.088f, 0.104f, 0.118f, 0.008f) }, 18, Vector3.zero, false, false);
            if (look.facialHair >= 1)
                mb.Box(HHair, new Vector3(0f, 0.101f, look.facialHair == 2 ? 0.112f : 0.108f), new Vector3(0.05f, 0.011f, 0.014f), id);   // moustache

            if (look.cap)
                mb.SmoothLathe(HWhite, new[] { R(0.097f, 0.112f, 0.2f, -0.008f), R(0.097f, 0.112f, 0.245f, -0.01f), R(0.088f, 0.103f, 0.255f, -0.012f), R(0f, 0f, 0.258f, -0.012f) }, 20, Vector3.zero, false, false);

            if (look.headScarf)
            {
                mb.SmoothLathe(HAccent, new[] { R(0.1f, 0.118f, 0.155f, -0.006f), R(0.1f, 0.118f, 0.18f, -0.008f), R(0.09f, 0.11f, 0.212f, -0.012f), R(0.064f, 0.082f, 0.24f, -0.016f), R(0.026f, 0.033f, 0.26f, -0.017f), R(0f, 0f, 0.264f, -0.017f) }, 20, Vector3.zero, false, false);
                mb.Box(HAccent, new Vector3(0f, 0.08f, -0.06f), new Vector3(0.22f, 0.24f, 0.12f), id);       // wraps behind the head
                mb.Box(HAccent, new Vector3(0.096f, 0.1f, 0.015f), new Vector3(0.025f, 0.2f, 0.13f), id);
                mb.Box(HAccent, new Vector3(-0.096f, 0.1f, 0.015f), new Vector3(0.025f, 0.2f, 0.13f), id);
            }
            return mb.ToMesh("PersonHead");
        }

        // ---------- limbs ----------

        static Mesh BuildUpperArm()
        {
            var mb = new MeshBuilder(2);
            mb.SmoothLathe(0, new[]
            {
                R(0f, 0f, -0.30f), R(0.038f, 0.04f, -0.30f), R(0.042f, 0.044f, -0.24f, 0.003f), R(0.047f, 0.05f, -0.15f, 0.006f),
                R(0.05f, 0.05f, -0.06f), R(0.052f, 0.05f, -0.02f), R(0.045f, 0.045f, 0.01f), R(0f, 0f, 0.025f)
            }, 12, Vector3.zero);
            return mb.ToMesh("PersonUpperArm");
        }

        static Mesh BuildForearm(float side)
        {
            var mb = new MeshBuilder(2);
            Ball(mb, 0, Vector3.zero, 0.043f);                                                           // elbow
            mb.SmoothLathe(0, new[] { R(0f, 0f, -0.255f), R(0.028f, 0.023f, -0.255f), R(0.033f, 0.028f, -0.2f), R(0.04f, 0.036f, -0.1f), R(0.043f, 0.04f, -0.03f), R(0f, 0f, 0f) }, 12, Vector3.zero);
            // mitten hand, palm facing the body, fingers together
            mb.SmoothLathe(1, new[] { R(0f, 0f, -0.375f, 0.004f), R(0.012f, 0.03f, -0.37f, 0.006f), R(0.016f, 0.042f, -0.34f, 0.006f), R(0.018f, 0.044f, -0.30f, 0.004f), R(0.016f, 0.032f, -0.265f), R(0.014f, 0.025f, -0.25f) }, 12, Vector3.zero);
            mb.Box(1, new Vector3(side * 0.006f, -0.29f, 0.036f), new Vector3(0.016f, 0.045f, 0.016f), Quaternion.Euler(25f, 0f, 0f));   // thumb
            return mb.ToMesh("PersonForearm");
        }

        static Mesh BuildThigh(bool loose)
        {
            float k = loose ? 1.3f : 1f;
            var mb = new MeshBuilder(2);
            mb.SmoothLathe(0, new[]
            {
                R(0f, 0f, -0.45f), R(0.05f * k, 0.053f * k, -0.45f), R(0.058f * k, 0.063f * k, -0.36f, 0.004f),
                R(0.07f * k, 0.076f * k, -0.2f, 0.008f), R(0.08f * k, 0.083f * k, -0.07f), R(0.072f, 0.074f, 0f), R(0f, 0f, 0.03f)
            }, 12, Vector3.zero);
            return mb.ToMesh("PersonThigh");
        }

        static Mesh BuildShin(bool loose)
        {
            float k = loose ? 1.35f : 1f;
            float calf = loose ? 0.05f : 0.057f;
            var mb = new MeshBuilder(2);
            Ball(mb, 0, Vector3.zero, 0.054f * k);                                                       // knee
            mb.SmoothLathe(0, new[]
            {
                R(0f, 0f, -0.40f), R(0.033f * k, 0.035f * k, -0.40f), R(0.037f * k, 0.042f * k, -0.33f, -0.004f),
                R(0.048f * k, calf * k, -0.2f, -0.01f), R(0.05f * k, 0.053f * k, -0.08f, -0.004f), R(0.048f * k, 0.05f * k, 0f), R(0f, 0f, 0.01f)
            }, 12, Vector3.zero);
            return mb.ToMesh("PersonShin");
        }

        /// <summary>Shoe in ankle-pivot space: rounded toe, heel behind the ankle, sole on the ground.</summary>
        static Mesh BuildFoot()
        {
            var mb = new MeshBuilder(1);
            mb.SmoothLathe(0, new[]
            {
                R(0f, 0f, -0.06f, 0.035f), R(0.045f, 0.112f, -0.06f, 0.035f), R(0.048f, 0.125f, -0.045f, 0.04f),
                R(0.047f, 0.122f, -0.02f, 0.038f), R(0.04f, 0.095f, 0f, 0.02f), R(0.032f, 0.055f, 0.018f), R(0f, 0f, 0.025f)
            }, 14, Vector3.zero);
            return mb.ToMesh("PersonFoot");
        }

        static Mesh BuildPhone()
        {
            var mb = new MeshBuilder(1);
            mb.Box(0, Vector3.zero, new Vector3(0.012f, 0.14f, 0.07f), Quaternion.identity);
            return mb.ToMesh("Phone");
        }

        static Mesh BuildBag()
        {
            var mb = new MeshBuilder(2);
            mb.Box(0, new Vector3(0f, -0.14f, 0f), new Vector3(0.24f, 0.28f, 0.1f), Quaternion.identity);
            mb.Box(1, new Vector3(0f, 0.02f, 0f), new Vector3(0.012f, 0.06f, 0.012f), Quaternion.identity);
            return mb.ToMesh("ShoppingBag");
        }

        static Material Mat(Color c)
        {
            if (Mats.TryGetValue(c, out var m)) return m;
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(shader) { color = c, enableInstancing = true };
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.22f);
            Mats[c] = m;
            return m;
        }
    }

    /// <summary>
    /// Walk / jog / sprint from movement speed, with a personal style per person: stride, arm swing,
    /// bounce, hip sway (more for women), toe-out, posture (elderly stoop, slower and shorter steps).
    /// Heel-strike → toe-off foot roll, weight shifting side to side, shoulders counter-rotating while the
    /// head stays steady. Standing: breathing, weight shifts, glances around. Texters look down at the phone.
    /// Fires onStep at each foot plant.
    /// </summary>
    public class PersonAnimator : MonoBehaviour
    {
        public PersonRig rig;
        public float speed;      // m/s, set by whoever moves the person
        float _phase, _swing, _breath;
        float _lookYaw, _lookTarget, _lookTimer;
        Vector3 _bodyBase;
        bool _init;
        float _lastSin;
        // personal gait
        float _strideMul, _armMul, _bounceMul, _hipSway, _stoop, _toeOut, _elbowBase;

        void Init()
        {
            _bodyBase = rig.body.localPosition;
            var r = new System.Random(Random.Range(0, int.MaxValue));
            float Rnd() => (float)r.NextDouble();
            _strideMul = 0.9f + 0.2f * Rnd();
            _armMul = 0.65f + 0.55f * Rnd();
            _bounceMul = 0.7f + 0.6f * Rnd();
            _hipSway = rig.female ? 0.9f + 0.4f * Rnd() : 0.25f + 0.3f * Rnd();
            _stoop = rig.elderly ? 9f + 6f * Rnd() : Rnd() * 3f;
            _toeOut = 3f + 7f * Rnd();
            _elbowBase = 8f + 12f * Rnd();
            if (rig.elderly) { _armMul *= 0.6f; _strideMul *= 0.8f; _bounceMul *= 0.6f; }
            _breath = Rnd() * 6f;
            _lookTimer = 1f + 3f * Rnd();
            _init = true;
        }

        void LateUpdate()
        {
            if (rig == null) return;
            float dt = Time.deltaTime;
            if (!_init) Init();

            if (rig.riding)
            {
                // seated on a bike: thighs forward, shins down to the footpegs, arms reaching for the bars
                rig.leftLeg.localRotation = Quaternion.Euler(-78f, 0f, -6f);
                rig.rightLeg.localRotation = Quaternion.Euler(-78f, 0f, 6f);
                rig.leftShin.localRotation = Quaternion.Euler(85f, 0f, 0f);
                rig.rightShin.localRotation = Quaternion.Euler(85f, 0f, 0f);
                rig.leftFoot.localRotation = rig.rightFoot.localRotation = Quaternion.Euler(-7f, 0f, 0f);
                rig.leftArm.localRotation = Quaternion.Euler(-62f, 0f, -12f);
                rig.rightArm.localRotation = Quaternion.Euler(-62f, 0f, 12f);
                rig.leftForearm.localRotation = Quaternion.Euler(-22f, 0f, 0f);
                rig.rightForearm.localRotation = Quaternion.Euler(-22f, 0f, 0f);
                rig.body.localPosition = _bodyBase;
                rig.body.localRotation = Quaternion.identity;
                if (rig.head != null) rig.head.localRotation = Quaternion.Euler(8f, 0f, 0f);
                return;
            }

            float s = Mathf.Clamp(speed, 0f, 9f);
            float run = Mathf.InverseLerp(2.2f, 7f, s);
            float targetSwing = s < 0.05f ? 0f : Mathf.Lerp(24f, 55f, run) * (rig.kameez ? 0.8f : 1f) * (rig.elderly ? 0.75f : 1f);
            _swing = Mathf.MoveTowards(_swing, targetSwing, 180f * dt);
            float stride = Mathf.Lerp(1.35f, 2.8f, run) * _strideMul;
            _phase += s / stride * Mathf.PI * 2f * dt;

            float sin = Mathf.Sin(_phase), cos = Mathf.Cos(_phase);
            float amount = _swing / 55f;
            float knee = Mathf.Lerp(45f, 110f, run) * amount;

            // foot plant whenever the legs pass each other
            if (s > 0.3f && Mathf.Sign(sin) != Mathf.Sign(_lastSin)) rig.onStep?.Invoke(run > 0.35f);
            _lastSin = sin;

            // body posture: lean into a sprint, elderly stoop; legs compensate so the feet stay under the hips
            float bodyPitch = Mathf.Lerp(0f, 14f, run) * amount + _stoop;
            float legComp = -_stoop - Mathf.Lerp(0f, 14f, run) * amount * 0.5f;

            // legs: thigh swing (+ knee lift when sprinting), knee bend on the forward swing, toe-out
            float thighL = sin * _swing - run * 8f * Mathf.Max(0f, -cos) + legComp;
            float thighR = -sin * _swing - run * 8f * Mathf.Max(0f, cos) + legComp;
            float shinL = Mathf.Max(0f, -cos) * knee + 4f * amount + (rig.elderly ? 6f : 0f);
            float shinR = Mathf.Max(0f, cos) * knee + 4f * amount + (rig.elderly ? 6f : 0f);
            rig.leftLeg.localRotation = Quaternion.Euler(thighL, -_toeOut, 0f);
            rig.rightLeg.localRotation = Quaternion.Euler(thighR, _toeOut, 0f);
            rig.leftShin.localRotation = Quaternion.Euler(shinL, 0f, 0f);
            rig.rightShin.localRotation = Quaternion.Euler(shinR, 0f, 0f);

            // feet: stay flat to the ground, roll from heel strike (toes up) to toe-off (toes down)
            rig.leftFoot.localRotation = Quaternion.Euler(-(thighL + shinL + bodyPitch) + Roll(thighL - legComp, s), 0f, 0f);
            rig.rightFoot.localRotation = Quaternion.Euler(-(thighR + shinR + bodyPitch) + Roll(thighR - legComp, s), 0f, 0f);

            // standing: slow breathing and weight shifts
            _breath += dt;
            float idle = 1f - Mathf.Clamp01(s / 0.5f);
            float breathe = Mathf.Sin(_breath * 1.7f) * idle;

            // arms swing opposite the legs, elbows bend more as speed rises
            float armSwing = _swing * 0.8f * _armMul;
            float elbow = Mathf.Lerp(_elbowBase, 90f, run) * Mathf.Max(amount, 0.3f) + idle * 6f;
            rig.leftArm.localRotation = Quaternion.Euler(-sin * armSwing + breathe * 1.5f, 0f, -4f - run * 6f);
            rig.leftForearm.localRotation = Quaternion.Euler(-elbow, 0f, 0f);
            if (rig.prop == PersonProp.Phone && rig.texting)
            {
                // reading the phone held in front of the chest
                rig.rightArm.localRotation = Quaternion.Euler(-18f, 0f, 10f);
                rig.rightForearm.localRotation = Quaternion.Euler(-95f, 0f, 0f);
                rig.leftArm.localRotation = Quaternion.Euler(-14f, 0f, -14f);
                rig.leftForearm.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            }
            else if (rig.prop == PersonProp.Phone)
            {
                rig.rightArm.localRotation = Quaternion.Euler(-25f, 0f, 30f);       // phone at the ear
                rig.rightForearm.localRotation = Quaternion.Euler(-145f, 0f, 0f);
            }
            else
            {
                float carry = rig.prop == PersonProp.Bag ? 0.4f : 1f;              // bag swings less
                rig.rightArm.localRotation = Quaternion.Euler(sin * armSwing * carry + breathe * 1.5f, 0f, 4f + run * 6f);
                rig.rightForearm.localRotation = Quaternion.Euler(-elbow * carry, 0f, 0f);
            }

            // holding a gun: arms point along the aim, legs keep walking
            if (rig.weaponPose > 0)
            {
                float up = -88f - rig.aimPitch + bodyPitch;
                rig.rightArm.localRotation = Quaternion.Euler(up, 0f, rig.weaponPose == 2 ? -8f : 0f);
                rig.rightForearm.localRotation = Quaternion.Euler(rig.weaponPose == 2 ? -12f : 0f, 0f, 0f);
                if (rig.weaponPose == 2)
                {
                    rig.leftArm.localRotation = Quaternion.Euler(up + 8f, 0f, 32f);     // support hand on the fore-end
                    rig.leftForearm.localRotation = Quaternion.Euler(-28f, 0f, 0f);
                }
                else
                {
                    rig.leftArm.localRotation = Quaternion.Euler(up + 4f, 0f, 28f);     // two-handed pistol grip
                    rig.leftForearm.localRotation = Quaternion.Euler(-35f, 0f, 0f);
                }
            }

            // head: glance around now and then, steady against the shoulder twist, eyes level despite stoop
            _lookTimer -= dt;
            if (_lookTimer <= 0f)
            {
                _lookTarget = Random.value < 0.5f ? 0f : Random.Range(-40f, 40f) * (1f - run);
                _lookTimer = Random.Range(1.5f, 5f);
            }
            if (rig.weaponPose > 0 || rig.texting) _lookTarget = 0f;
            _lookYaw = Mathf.MoveTowards(_lookYaw, _lookTarget, 90f * dt);
            float bodyYaw = rig.weaponPose > 0 ? 0f : sin * 4f * amount;
            float headPitch = rig.weaponPose > 0 ? -rig.aimPitch * 0.5f : rig.texting ? 28f - bodyPitch * 0.5f : -bodyPitch * 0.7f - run * 4f;
            if (rig.head != null)
                rig.head.localRotation = Quaternion.Euler(headPitch, _lookYaw - bodyYaw * 0.8f, 0f);

            // body: bounce, side-to-side weight shift with a little hip roll, breathing rise
            float bob = Mathf.Abs(sin) * Mathf.Lerp(0.025f, 0.07f, run) * amount * _bounceMul + breathe * 0.004f;
            float shift = cos * 0.012f * (0.4f + _hipSway) * amount + Mathf.Sin(_breath * 0.35f) * 0.01f * idle;
            rig.body.localPosition = _bodyBase + new Vector3(shift, bob, 0f);
            rig.body.localRotation = Quaternion.Euler(bodyPitch, bodyYaw, cos * 2.2f * _hipSway * amount);
        }

        /// <summary>Foot roll relative to flat: toes up as the heel lands, toes down as the foot pushes off.</summary>
        static float Roll(float legAngle, float speed)
        {
            if (speed < 0.05f) return 0f;
            return Mathf.Clamp(legAngle < 0f ? legAngle * 0.55f : legAngle * 0.85f, -18f, 30f);
        }
    }
}
