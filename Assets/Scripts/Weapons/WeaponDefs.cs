using System.Collections.Generic;
using UnityEngine;
using FortunerCity.AudioSys;
using FortunerCity.Vehicle;

namespace FortunerCity.Weapons
{
    public class WeaponDef
    {
        public string name;
        public int pose;              // 1 one-hand pistol, 2 two-hand long gun
        public float damage;
        public float fireInterval;    // seconds between shots
        public bool automatic;
        public int magSize, startReserve;
        public int pellets = 1;
        public float spread;          // degrees
        public float range = 250f;
        public float recoil;          // camera kick in degrees
        public float reloadTime;
        public SoundSynth.Gun sound;
        public Vector3 muzzle;        // in weapon space (+Z = barrel)
        public System.Func<Mesh> model;
    }

    /// <summary>
    /// Generic look-alike guns built in code (no brand marks): a polymer service pistol (Glock-style),
    /// an AK-style rifle with wooden furniture and a pump shotgun. Weapon space: origin at the grip,
    /// +Z = barrel, +Y = up. Submeshes: 0 black metal, 1 polymer, 2 wood.
    /// </summary>
    public static class WeaponDefs
    {
        static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();
        static Material[] _mats;

        public static readonly WeaponDef Pistol = new WeaponDef
        {
            name = "Pistol", pose = 1, damage = 25f, fireInterval = 0.18f, automatic = false,
            magSize = 17, startReserve = 85, spread = 0.6f, recoil = 1.6f, reloadTime = 1.2f,
            sound = SoundSynth.Gun.Pistol, muzzle = new Vector3(0f, 0.035f, 0.17f), model = () => Get("pistol", BuildPistol)
        };

        public static readonly WeaponDef Rifle = new WeaponDef
        {
            name = "Rifle", pose = 2, damage = 34f, fireInterval = 0.1f, automatic = true,
            magSize = 30, startReserve = 150, spread = 1.2f, recoil = 1.1f, reloadTime = 1.9f,
            sound = SoundSynth.Gun.Rifle, muzzle = new Vector3(0f, 0.06f, 0.66f), model = () => Get("rifle", BuildRifle)
        };

        public static readonly WeaponDef Shotgun = new WeaponDef
        {
            name = "Shotgun", pose = 2, damage = 14f, fireInterval = 0.85f, automatic = false,
            magSize = 8, startReserve = 40, pellets = 9, spread = 5f, range = 60f, recoil = 5f, reloadTime = 2.4f,
            sound = SoundSynth.Gun.Shotgun, muzzle = new Vector3(0f, 0.06f, 0.78f), model = () => Get("shotgun", BuildShotgun)
        };

        public static readonly WeaponDef[] All = { Pistol, Rifle, Shotgun };

        public static Material[] Materials()
        {
            if (_mats != null) return _mats;
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            Material M(Color c, float metal, float gloss)
            {
                var m = new Material(shader) { color = c };
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", gloss);
                return m;
            }
            _mats = new[]
            {
                M(new Color(0.08f, 0.08f, 0.09f), 0.8f, 0.55f),    // blued steel
                M(new Color(0.12f, 0.12f, 0.12f), 0.1f, 0.35f),    // polymer
                M(new Color(0.42f, 0.22f, 0.1f), 0f, 0.45f),       // wood
            };
            return _mats;
        }

        static Mesh Get(string key, System.Func<Mesh> build)
        {
            if (!Cache.TryGetValue(key, out var m)) Cache[key] = m = build();
            return m;
        }

        static void B(MeshBuilder mb, int sub, float x, float y, float z, float sx, float sy, float sz, float pitch = 0f) =>
            mb.Box(sub, new Vector3(x, y, z), new Vector3(sx, sy, sz), Quaternion.Euler(pitch, 0f, 0f));

        static Mesh BuildPistol()
        {
            var mb = new MeshBuilder(3);
            B(mb, 0, 0f, 0.035f, 0.06f, 0.03f, 0.033f, 0.19f);              // slide
            B(mb, 0, 0f, 0.035f, 0.157f, 0.012f, 0.012f, 0.006f);           // muzzle face
            B(mb, 1, 0f, 0.008f, 0.05f, 0.028f, 0.022f, 0.16f);             // frame + rail
            B(mb, 1, 0f, -0.045f, -0.005f, 0.03f, 0.11f, 0.05f, -18f);      // grip
            B(mb, 1, 0f, -0.012f, 0.05f, 0.008f, 0.03f, 0.05f);             // trigger guard
            B(mb, 0, 0f, 0.055f, 0.145f, 0.005f, 0.008f, 0.006f);           // front sight
            B(mb, 0, 0f, 0.055f, -0.025f, 0.02f, 0.008f, 0.008f);           // rear sight
            return mb.ToMesh("Pistol");
        }

        static Mesh BuildRifle()
        {
            var mb = new MeshBuilder(3);
            B(mb, 0, 0f, 0.04f, 0.10f, 0.05f, 0.075f, 0.36f);               // receiver
            B(mb, 0, 0f, 0.085f, 0.10f, 0.045f, 0.015f, 0.34f);             // dust cover
            B(mb, 2, 0f, 0.05f, 0.38f, 0.055f, 0.06f, 0.20f);               // wooden handguard
            B(mb, 0, 0f, 0.085f, 0.40f, 0.024f, 0.024f, 0.24f);             // gas tube
            B(mb, 0, 0f, 0.06f, 0.55f, 0.02f, 0.02f, 0.26f);                // barrel
            B(mb, 0, 0f, 0.06f, 0.66f, 0.03f, 0.03f, 0.05f);                // muzzle brake
            B(mb, 0, 0f, 0.1f, 0.6f, 0.01f, 0.05f, 0.015f);                 // front sight post
            B(mb, 2, 0f, -0.04f, -0.01f, 0.035f, 0.1f, 0.05f, -20f);        // pistol grip
            B(mb, 2, 0f, 0.02f, -0.26f, 0.045f, 0.08f, 0.32f, 6f);          // stock
            B(mb, 0, 0f, -0.03f, 0.18f, 0.035f, 0.1f, 0.06f, 8f);           // curved magazine (3 segments)
            B(mb, 0, 0f, -0.11f, 0.205f, 0.035f, 0.08f, 0.06f, 20f);
            B(mb, 0, 0f, -0.18f, 0.24f, 0.035f, 0.07f, 0.06f, 32f);
            return mb.ToMesh("Rifle");
        }

        static Mesh BuildShotgun()
        {
            var mb = new MeshBuilder(3);
            B(mb, 0, 0f, 0.04f, 0.08f, 0.045f, 0.07f, 0.24f);               // receiver
            B(mb, 0, 0f, 0.06f, 0.48f, 0.026f, 0.026f, 0.62f);              // barrel
            B(mb, 0, 0f, 0.025f, 0.40f, 0.024f, 0.024f, 0.46f);             // magazine tube
            B(mb, 2, 0f, 0.025f, 0.36f, 0.05f, 0.05f, 0.2f);                // pump fore-end
            B(mb, 2, 0f, -0.04f, -0.01f, 0.035f, 0.1f, 0.05f, -20f);        // grip
            B(mb, 2, 0f, 0.0f, -0.25f, 0.045f, 0.09f, 0.34f, 8f);           // stock
            B(mb, 0, 0f, 0.08f, 0.77f, 0.008f, 0.012f, 0.01f);              // bead sight
            return mb.ToMesh("Shotgun");
        }
    }
}
