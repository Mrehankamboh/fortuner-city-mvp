using UnityEngine;

namespace FortunerCity.City
{
    /// <summary>Procedural skyscrapers, mid-rise blocks and trees built from CityMesh boxes.</summary>
    public static class CityBuildings
    {
        static float SnapBays(float metres) => Mathf.Max(2f, Mathf.Round(metres / CityArt.BayWidth)) * CityArt.BayWidth;
        static float SnapFloors(float metres) => Mathf.Max(1f, Mathf.Round(metres / CityArt.FloorHeight)) * CityArt.FloorHeight;

        /// <summary>Glass tower: shop podium, 1–3 setback tiers, rooftop plant room, antenna on supertalls.</summary>
        public static void Tower(Transform parent, Vector3 basePos, float lotW, float lotD, float height, System.Random rng)
        {
            var t = new GameObject("Tower").transform;
            t.SetParent(parent, false);
            t.localPosition = basePos;

            var facade = CityArt.RandomGlass(rng);
            float podiumH = CityArt.FloorHeight * 2f;
            float pw = SnapBays(lotW), pd = SnapBays(lotD);
            CityMesh.SpawnBox("Podium", t, new Vector3(pw, podiumH, pd), Vector3.zero, CityArt.Shopfront, CityArt.Roof, true);

            int tiers = height > 130f ? 3 : (height > 75f ? 2 : 1);
            if (rng.NextDouble() < 0.3) tiers = 1;
            float[] frac = tiers == 3 ? new[] { 0.55f, 0.28f, 0.17f } : tiers == 2 ? new[] { 0.68f, 0.32f } : new[] { 1f };

            float w = lotW * 0.84f, d = lotD * 0.84f;
            float y = podiumH;
            float remaining = height - podiumH;
            for (int i = 0; i < tiers; i++)
            {
                float th = SnapFloors(remaining * frac[i]);
                float sw = SnapBays(w), sd = SnapBays(d);
                CityMesh.SpawnBox("Tier" + i, t, new Vector3(sw, th, sd), new Vector3(0f, y, 0f), facade, CityArt.Roof, i == 0, y);
                y += th;
                w *= 0.78f;
                d *= 0.78f;
            }

            // Rooftop plant room
            CityMesh.SpawnBox("PlantRoom", t, new Vector3(Mathf.Max(4f, w * 0.7f), 5f, Mathf.Max(4f, d * 0.7f)),
                new Vector3(0f, y, 0f), CityArt.Concrete, CityArt.Roof, false);
            y += 5f;

            if (height > 150f)
                CityMesh.SpawnBox("Antenna", t, new Vector3(0.8f, height * 0.12f, 0.8f), new Vector3(0f, y, 0f),
                    CityArt.Barrier, CityArt.Barrier, false);
        }

        /// <summary>Mid-rise office / apartment block with a shop ground floor.</summary>
        public static void Midrise(Transform parent, Vector3 basePos, float lotW, float lotD, float height, System.Random rng)
        {
            var t = new GameObject("Midrise").transform;
            t.SetParent(parent, false);
            t.localPosition = basePos;

            double pick = rng.NextDouble();
            Material facade = pick < 0.45 ? CityArt.RandomOffice(rng) : pick < 0.8 ? CityArt.RandomBrick(rng) : CityArt.RandomGlass(rng);

            float w = SnapBays(lotW), d = SnapBays(lotD);
            float ground = CityArt.FloorHeight * 1.3f;
            float body = SnapFloors(Mathf.Max(CityArt.FloorHeight * 2f, height - ground));
            CityMesh.SpawnBox("Shops", t, new Vector3(w, ground, d), Vector3.zero, CityArt.Shopfront, CityArt.Roof, true);
            CityMesh.SpawnBox("Body", t, new Vector3(w, body, d), new Vector3(0f, ground, 0f), facade, CityArt.Roof, true, ground);

            // Parapet cap + small rooftop unit
            float top = ground + body;
            CityMesh.SpawnBox("Parapet", t, new Vector3(w + 0.4f, 0.8f, d + 0.4f), new Vector3(0f, top, 0f), CityArt.Concrete, CityArt.Roof, false);
            if (rng.NextDouble() < 0.7)
                CityMesh.SpawnBox("RoofUnit", t, new Vector3(w * 0.3f, 3f, d * 0.25f),
                    new Vector3((float)(rng.NextDouble() - 0.5) * w * 0.4f, top + 0.8f, (float)(rng.NextDouble() - 0.5) * d * 0.4f),
                    CityArt.Concrete, CityArt.Roof, false);
        }

        /// <summary>Simple low-poly tree (used when the Kenney kit is not imported, and in parks/medians).</summary>
        public static void Tree(Transform parent, Vector3 pos, float scale)
        {
            var tree = new GameObject("Tree").transform;
            tree.SetParent(parent, false);
            tree.localPosition = pos;
            CityMesh.SpawnBox("Trunk", tree, new Vector3(0.35f, 2.2f, 0.35f) * scale, Vector3.zero, CityArt.Trunk, CityArt.Trunk, false);
            CityMesh.SpawnBox("Crown", tree, new Vector3(2.6f, 2.4f, 2.6f) * scale, new Vector3(0f, 2f * scale, 0f), CityArt.Leaves, CityArt.Leaves, false);
            CityMesh.SpawnBox("CrownTop", tree, new Vector3(1.7f, 1.2f, 1.7f) * scale, new Vector3(0f, 4.4f * scale, 0f), CityArt.Leaves, CityArt.Leaves, false);
        }
    }
}
