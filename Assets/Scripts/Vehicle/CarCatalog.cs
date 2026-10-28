using System.Collections.Generic;
using UnityEngine;

namespace FortunerCity.Vehicle
{
    /// <summary>
    /// Car models inspired by real cars, built at their real dimensions.
    /// Generic "-style" designs: no manufacturer logos, badges or names on the cars.
    /// </summary>
    public static class CarCatalog
    {
        static Vector2 V(float z, float y) => new Vector2(z, y);
        static Color C(float r, float g, float b) => new Color(r, g, b);

        static readonly Color White = C(0.93f, 0.93f, 0.91f), Black = C(0.05f, 0.05f, 0.06f), Silver = C(0.66f, 0.67f, 0.69f),
            Grey = C(0.35f, 0.36f, 0.38f), Red = C(0.62f, 0.05f, 0.06f), Blue = C(0.08f, 0.2f, 0.45f);

        // ---------- Fortuner-style: 4.84 x 1.86 x 1.84 m, wb 2.75 m, 265/60 R18 ----------
        public static readonly CarSpec Fortuner = new CarSpec
        {
            name = "Fortuner", topSpeedKmh = 170f, accel = 28f, engine = EngineSound.Diesel4, length = 4.84f, frontAxle = 1.475f, rearAxle = -1.275f, halfTrack = 0.7725f,
            wheelRadius = 0.39f, tyreWidth = 0.265f, rimRatio = 0.63f, spokes = 6, twinSpokes = true,
            sill = 0.42f, belt = 1.13f, roof = 1.835f, wLow = 0.9275f, wBelt = 0.895f, wRoof = 0.70f, taper = 0.09f,
            rearUnderZ = -2.28f,
            top = new[]
            {
                V(2.30f, 0.44f), V(2.40f, 0.56f), V(2.42f, 0.80f), V(2.40f, 0.96f), V(2.32f, 1.04f), V(1.80f, 1.10f),
                V(1.25f, 1.14f), V(0.50f, 1.79f), V(0.30f, 1.835f), V(-1.90f, 1.825f), V(-2.20f, 1.80f),
                V(-2.33f, 1.72f), V(-2.40f, 1.30f), V(-2.42f, 1.05f), V(-2.41f, 0.70f), V(-2.40f, 0.52f)
            },
            windshieldA = 6, rearGlassA = 11,
            sideWindow = new[] { V(1.12f, 1.16f), V(0.52f, 1.74f), V(-1.92f, 1.75f), V(-2.20f, 1.70f), V(-2.28f, 1.40f), V(-2.26f, 1.16f) },
            pillars = new[] { 0.05f, -1.18f }, roofRails = true, sideSteps = true,
            plateFrontY = 0.575f, plateRearY = 0.93f,
            palette = new[] { White, Black, Silver, Grey, C(0.42f, 0.32f, 0.24f) },
            trafficWeight = 1.2f,
            details = d =>
            {
                d.Front(CarMat.Black, 0f, 0.79f, new Vector3(1.0f, 0.30f, 0.04f));
                d.HBars(CarMat.Chrome, 0.70f, 0.88f, 3, 1.02f, 0.022f);
                d.Front(CarMat.Chrome, 0f, 0.635f, new Vector3(1.05f, 0.03f, 0.05f));
                d.Front(CarMat.Chrome, 0f, 0.975f, new Vector3(1.0f, 0.04f, 0.06f));
                d.Headlights(0.62f, 0.92f, 0.40f, 0.10f, 14f);
                d.Front(CarMat.Black, 0.66f, 0.66f, new Vector3(0.22f, 0.11f, 0.07f), 14f);
                d.Front(CarMat.Light, 0.66f, 0.66f, new Vector3(0.11f, 0.05f, 0.03f), 14f, 0.04f);
                d.Front(CarMat.Silver, 0f, 0.47f, new Vector3(0.95f, 0.07f, 0.16f));
                d.Rear(CarMat.Tail, 0.66f, 1.18f, new Vector3(0.32f, 0.26f, 0.08f), -10f);
                d.Pair(CarMat.Tail, new Vector3(0.84f, 1.18f, -2.28f), new Vector3(0.05f, 0.24f, 0.22f));
                d.Rear(CarMat.Chrome, 0f, 1.17f, new Vector3(0.95f, 0.05f, 0.04f), 0f, 0.01f);
                d.Rear(CarMat.Black, 0f, 0.58f, new Vector3(1.5f, 0.10f, 0.08f));
                d.Rear(CarMat.Tail, 0.70f, 0.66f, new Vector3(0.12f, 0.03f, 0.03f), 0f, 0.04f);
                d.Box(CarMat.Paint, new Vector3(0f, 1.81f, -2.28f), new Vector3(1.25f, 0.05f, 0.22f));
                d.Box(CarMat.Chrome, new Vector3(-0.55f, 0.42f, -2.36f), new Vector3(0.08f, 0.08f, 0.14f));
            }
        };

        // ---------- Haval H6-style crossover: 4.65 x 1.89 x 1.72 m, wb 2.74 m, 225/55 R19 ----------
        public static readonly CarSpec Haval = new CarSpec
        {
            name = "HavalStyle", topSpeedKmh = 180f, accel = 25f, engine = EngineSound.Petrol4, length = 4.65f, frontAxle = 1.405f, rearAxle = -1.335f, halfTrack = 0.80f,
            wheelRadius = 0.36f, tyreWidth = 0.235f, rimRatio = 0.67f, spokes = 5, twinSpokes = true,
            sill = 0.36f, belt = 1.02f, roof = 1.724f, wLow = 0.943f, wBelt = 0.90f, wRoof = 0.68f, taper = 0.10f,
            rearUnderZ = -2.18f,
            top = new[]
            {
                V(2.22f, 0.38f), V(2.32f, 0.50f), V(2.325f, 0.72f), V(2.30f, 0.88f), V(2.18f, 0.97f), V(1.60f, 1.00f),
                V(1.05f, 1.04f), V(0.25f, 1.68f), V(0.0f, 1.724f), V(-1.20f, 1.71f), V(-1.85f, 1.64f),
                V(-2.15f, 1.52f), V(-2.30f, 1.20f), V(-2.325f, 1.00f), V(-2.32f, 0.75f), V(-2.30f, 0.50f)
            },
            windshieldA = 6, rearGlassA = 11,
            sideWindow = new[] { V(0.95f, 1.06f), V(0.28f, 1.62f), V(-1.15f, 1.66f), V(-1.80f, 1.58f), V(-2.08f, 1.45f), V(-2.12f, 1.06f) },
            pillars = new[] { -0.38f }, roofRails = true,
            plateFrontY = 0.52f, plateRearY = 0.86f,
            palette = new[] { White, Grey, Silver, Black, C(0.15f, 0.3f, 0.5f) },
            details = d =>
            {
                // big grille with chrome dot matrix
                d.Front(CarMat.Black, 0f, 0.70f, new Vector3(1.30f, 0.40f, 0.04f));
                d.DotGrid(CarMat.Chrome, 0.70f, 1.20f, 0.34f, 11, 4, 0.035f);
                d.Front(CarMat.Chrome, 0f, 0.91f, new Vector3(1.32f, 0.025f, 0.05f), 0f, 0.01f);
                d.Headlights(0.68f, 0.87f, 0.40f, 0.07f, 16f);
                d.Front(CarMat.Light, 0.82f, 0.58f, new Vector3(0.03f, 0.18f, 0.05f), 16f, 0.02f);    // vertical fog LEDs
                d.Front(CarMat.Black, 0f, 0.46f, new Vector3(1.10f, 0.08f, 0.06f));
                d.Front(CarMat.Silver, 0f, 0.41f, new Vector3(0.90f, 0.03f, 0.08f), 0f, 0.01f);
                // full-width tail light bar
                d.Rear(CarMat.Tail, 0f, 1.10f, new Vector3(1.60f, 0.05f, 0.05f), 0f, 0.01f);
                d.Rear(CarMat.Tail, 0.68f, 1.06f, new Vector3(0.32f, 0.11f, 0.07f), -12f);
                d.Rear(CarMat.Black, 0f, 0.47f, new Vector3(1.55f, 0.10f, 0.07f));
                d.Rear(CarMat.Silver, 0f, 0.42f, new Vector3(1.0f, 0.03f, 0.08f), 0f, 0.01f);
                d.Box(CarMat.Paint, new Vector3(0f, 1.66f, -1.98f), new Vector3(1.15f, 0.04f, 0.20f), new Vector3(-12f, 0f, 0f));
            }
        };

        // ---------- Lamborghini-style supercar: 4.52 x 1.93 x 1.165 m, wb 2.62 m, R20 ----------
        public static readonly CarSpec Lambo = new CarSpec
        {
            name = "SupercarStyle", topSpeedKmh = 300f, accel = 42f, engine = EngineSound.V10, length = 4.52f, frontAxle = 1.23f, rearAxle = -1.39f, halfTrack = 0.815f,
            wheelRadius = 0.34f, tyreWidth = 0.29f, rimRatio = 0.76f, spokes = 5, twinSpokes = true, rimMat = CarMat.Dark,
            caliper = new Color(0.9f, 0.75f, 0.05f), archExtra = 0.06f,
            sill = 0.14f, belt = 0.86f, roof = 1.165f, wLow = 0.965f, wBelt = 0.80f, wRoof = 0.60f, taper = 0.16f, taperLength = 0.6f,
            rearUnderZ = -2.00f,
            top = new[]
            {
                V(2.12f, 0.16f), V(2.24f, 0.24f), V(2.26f, 0.40f), V(2.18f, 0.55f), V(1.60f, 0.75f), V(1.05f, 0.84f),
                V(0.90f, 0.89f), V(-0.10f, 1.155f), V(-0.35f, 1.165f), V(-0.90f, 1.10f), V(-1.60f, 0.98f),
                V(-2.10f, 0.90f), V(-2.24f, 0.88f), V(-2.26f, 0.75f), V(-2.25f, 0.50f), V(-2.20f, 0.30f), V(-2.10f, 0.17f)
            },
            windshieldA = 6, rearGlassA = 8,
            sideWindow = new[] { V(0.75f, 0.90f), V(-0.10f, 1.12f), V(-0.55f, 1.11f), V(-0.95f, 0.93f), V(-0.90f, 0.90f) },
            chromeBelt = false,
            plateFrontY = 0.26f, plateRearY = 0.62f,
            palette = new[] { C(0.95f, 0.45f, 0.02f), C(0.95f, 0.78f, 0.05f), C(0.35f, 0.75f, 0.1f), Black, White },
            trafficWeight = 0.35f,
            details = d =>
            {
                // no grille: big angular black intakes + Y-shaped headlights
                d.Front(CarMat.Black, 0.55f, 0.30f, new Vector3(0.55f, 0.14f, 0.06f), 10f);
                d.Front(CarMat.Black, 0f, 0.22f, new Vector3(1.70f, 0.04f, 0.10f));                       // splitter
                d.Front(CarMat.Light, 0.62f, 0.51f, new Vector3(0.42f, 0.04f, 0.08f), 24f);
                d.Front(CarMat.Light, 0.70f, 0.47f, new Vector3(0.20f, 0.03f, 0.06f), 24f, 0.01f, 35f);
                d.Front(CarMat.Black, 0.60f, 0.47f, new Vector3(0.30f, 0.06f, 0.06f), 24f, -0.01f);
                // side intakes and skirts
                d.Side(CarMat.Black, 0.55f, -0.85f, new Vector3(0.04f, 0.26f, 0.60f));
                d.Side(CarMat.Black, 0.20f, 0.0f, new Vector3(0.06f, 0.08f, 1.70f));
                // engine-cover louvres
                for (float z = -1.0f; z >= -1.5f; z -= 0.1f)
                {
                    float y = 1.10f - (-0.90f - z) / 0.70f * 0.12f + 0.012f;
                    d.Box(CarMat.Black, new Vector3(0f, y, z), new Vector3(0.90f, 0.012f, 0.05f), new Vector3(-10f, 0f, 0f));
                }
                // rear: thin lights, mesh panel, quad exhaust, diffuser, lip spoiler
                d.Rear(CarMat.Tail, 0.62f, 0.78f, new Vector3(0.42f, 0.04f, 0.06f), -15f, 0.01f);
                d.Rear(CarMat.Black, 0f, 0.55f, new Vector3(1.40f, 0.24f, 0.05f));
                d.Rear(CarMat.Chrome, 0.12f, 0.38f, new Vector3(0.10f, 0.08f, 0.10f), 0f, 0.02f);
                d.Rear(CarMat.Black, 0f, 0.22f, new Vector3(1.50f, 0.12f, 0.10f));
                d.Box(CarMat.Paint, new Vector3(0f, 0.91f, -2.20f), new Vector3(1.50f, 0.04f, 0.16f), new Vector3(-8f, 0f, 0f));
            }
        };

        // ---------- Range Rover-style luxury SUV: 5.05 x 2.05 x 1.87 m, wb 3.0 m, 285/40 R22 ----------
        public static readonly CarSpec RangeRover = new CarSpec
        {
            name = "LuxurySuvStyle", topSpeedKmh = 230f, accel = 31f, engine = EngineSound.V8, length = 5.05f, frontAxle = 1.595f, rearAxle = -1.405f, halfTrack = 0.845f,
            wheelRadius = 0.39f, tyreWidth = 0.285f, rimRatio = 0.72f, spokes = 10, twinSpokes = false, rimMat = CarMat.Dark,
            sill = 0.40f, belt = 1.12f, roof = 1.87f, wLow = 1.0235f, wBelt = 0.985f, wRoof = 0.80f, taper = 0.07f,
            rearUnderZ = -2.38f,
            top = new[]
            {
                V(2.42f, 0.42f), V(2.50f, 0.55f), V(2.525f, 0.80f), V(2.50f, 1.00f), V(2.40f, 1.07f), V(1.80f, 1.10f),
                V(1.30f, 1.11f), V(1.22f, 1.15f), V(0.45f, 1.83f), V(0.20f, 1.87f), V(-2.20f, 1.86f), V(-2.40f, 1.82f),
                V(-2.48f, 1.70f), V(-2.52f, 1.35f), V(-2.525f, 1.05f), V(-2.515f, 0.75f), V(-2.48f, 0.50f)
            },
            windshieldA = 7, rearGlassA = 12,
            sideWindow = new[] { V(1.10f, 1.16f), V(0.48f, 1.76f), V(-2.22f, 1.78f), V(-2.40f, 1.70f), V(-2.44f, 1.16f) },
            pillars = new[] { 0.0f, -1.30f }, chromeBelt = false,
            plateFrontY = 0.62f, plateRearY = 0.90f,
            palette = new[] { Black, White, C(0.55f, 0.56f, 0.55f), C(0.12f, 0.22f, 0.16f), C(0.25f, 0.27f, 0.3f) },
            trafficWeight = 0.8f,
            details = d =>
            {
                // slim mesh grille, super-slim headlights, big lower intake
                d.Front(CarMat.Dark, 0f, 0.86f, new Vector3(1.0f, 0.20f, 0.04f));
                d.DotGrid(CarMat.Silver, 0.86f, 0.92f, 0.16f, 14, 3, 0.022f);
                d.Headlights(0.66f, 0.97f, 0.46f, 0.06f, 8f);
                d.Front(CarMat.Black, 0f, 0.56f, new Vector3(1.30f, 0.18f, 0.05f));
                d.DotGrid(CarMat.Dark, 0.56f, 1.2f, 0.14f, 16, 2, 0.02f);
                // fender vents + lower body silver strip
                d.Side(CarMat.Dark, 0.92f, 1.95f, new Vector3(0.012f, 0.06f, 0.30f));
                d.Side(CarMat.Silver, 0.52f, 0.10f, new Vector3(0.012f, 0.04f, 2.6f));
                // rear: gloss-black band with slim vertical lamps at the corners
                d.Rear(CarMat.Black, 0f, 1.30f, new Vector3(1.55f, 0.10f, 0.04f));
                d.Rear(CarMat.Tail, 0.86f, 1.30f, new Vector3(0.10f, 0.42f, 0.06f), 0f, 0.01f);
                d.Rear(CarMat.Silver, 0f, 0.48f, new Vector3(1.1f, 0.05f, 0.08f));
                d.Box(CarMat.Black, new Vector3(0f, 1.85f, -2.38f), new Vector3(1.4f, 0.04f, 0.18f));
            }
        };

        // ---------- Jaecoo J7-style crossover: 4.50 x 1.87 x 1.68 m, wb 2.67 m, R19 ----------
        public static readonly CarSpec Jaecoo = new CarSpec
        {
            name = "UrbanSuvStyle", topSpeedKmh = 175f, accel = 24f, engine = EngineSound.Petrol4, length = 4.50f, frontAxle = 1.37f, rearAxle = -1.302f, halfTrack = 0.79f,
            wheelRadius = 0.355f, tyreWidth = 0.235f, rimRatio = 0.68f, spokes = 5, twinSpokes = false,
            sill = 0.36f, belt = 1.02f, roof = 1.68f, wLow = 0.9325f, wBelt = 0.90f, wRoof = 0.70f, taper = 0.08f,
            rearUnderZ = -2.10f,
            top = new[]
            {
                V(2.15f, 0.38f), V(2.24f, 0.50f), V(2.25f, 0.78f), V(2.22f, 0.93f), V(2.12f, 0.99f), V(1.50f, 1.005f),
                V(1.10f, 1.01f), V(1.02f, 1.05f), V(0.30f, 1.64f), V(0.05f, 1.68f), V(-1.70f, 1.665f), V(-1.98f, 1.62f),
                V(-2.12f, 1.52f), V(-2.22f, 1.18f), V(-2.25f, 0.98f), V(-2.24f, 0.70f), V(-2.20f, 0.48f)
            },
            windshieldA = 7, rearGlassA = 12,
            sideWindow = new[] { V(0.92f, 1.06f), V(0.33f, 1.58f), V(-1.70f, 1.61f), V(-1.97f, 1.55f), V(-2.10f, 1.42f), V(-2.12f, 1.06f) },
            pillars = new[] { -0.40f, -1.45f }, roofRails = true, chromeBelt = false,
            plateFrontY = 0.50f, plateRearY = 0.84f,
            palette = new[] { White, Grey, C(0.33f, 0.36f, 0.27f), Black, C(0.1f, 0.25f, 0.42f) },
            details = d =>
            {
                // tall vertical-bar grille
                d.Front(CarMat.Black, 0f, 0.74f, new Vector3(1.10f, 0.38f, 0.04f));
                d.VBars(CarMat.Chrome, 0.74f, 0.34f, 1.04f, 14, 0.018f);
                d.Headlights(0.66f, 0.91f, 0.38f, 0.07f, 12f);
                d.Front(CarMat.Black, 0f, 0.47f, new Vector3(1.20f, 0.09f, 0.06f));
                d.Front(CarMat.Silver, 0f, 0.42f, new Vector3(0.85f, 0.03f, 0.08f), 0f, 0.01f);
                // full-width tail bar + corner units
                d.Rear(CarMat.Tail, 0f, 1.08f, new Vector3(1.55f, 0.045f, 0.05f), 0f, 0.01f);
                d.Rear(CarMat.Tail, 0.66f, 1.04f, new Vector3(0.30f, 0.10f, 0.07f), -10f);
                d.Rear(CarMat.Black, 0f, 0.48f, new Vector3(1.50f, 0.10f, 0.07f));
                d.Rear(CarMat.Silver, 0f, 0.43f, new Vector3(0.9f, 0.03f, 0.08f), 0f, 0.01f);
            }
        };

        // ---------- Land Cruiser 300-style: 4.99 x 1.98 x 1.95 m, wb 2.85 m, 265/55 R20 ----------
        public static readonly CarSpec LandCruiser = new CarSpec
        {
            name = "FullSizeSuvStyle", topSpeedKmh = 200f, accel = 27f, engine = EngineSound.V8, length = 4.985f, frontAxle = 1.5425f, rearAxle = -1.3075f, halfTrack = 0.83f,
            wheelRadius = 0.40f, tyreWidth = 0.265f, rimRatio = 0.62f, spokes = 6, twinSpokes = false,
            sill = 0.46f, belt = 1.20f, roof = 1.945f, wLow = 0.99f, wBelt = 0.96f, wRoof = 0.80f, taper = 0.06f,
            rearUnderZ = -2.35f,
            top = new[]
            {
                V(2.39f, 0.48f), V(2.47f, 0.60f), V(2.4925f, 0.85f), V(2.48f, 1.05f), V(2.40f, 1.13f), V(1.80f, 1.16f),
                V(1.35f, 1.18f), V(1.27f, 1.22f), V(0.62f, 1.89f), V(0.40f, 1.945f), V(-2.25f, 1.935f), V(-2.40f, 1.90f),
                V(-2.46f, 1.78f), V(-2.49f, 1.40f), V(-2.4925f, 1.12f), V(-2.48f, 0.80f), V(-2.45f, 0.55f)
            },
            windshieldA = 7, rearGlassA = 12,
            sideWindow = new[] { V(1.15f, 1.24f), V(0.64f, 1.82f), V(-2.25f, 1.84f), V(-2.40f, 1.78f), V(-2.44f, 1.24f) },
            pillars = new[] { 0.05f, -1.25f }, roofRails = true, sideSteps = true,
            plateFrontY = 0.62f, plateRearY = 0.95f,
            palette = new[] { White, Black, Silver, Grey, C(0.62f, 0.56f, 0.45f) },
            trafficWeight = 1.1f,
            details = d =>
            {
                // massive chrome grille with thick bars
                d.Front(CarMat.Chrome, 0f, 0.88f, new Vector3(1.30f, 0.44f, 0.04f));
                d.Front(CarMat.Black, 0f, 0.88f, new Vector3(1.20f, 0.36f, 0.04f), 0f, 0.01f);
                d.HBars(CarMat.Chrome, 0.76f, 1.00f, 4, 1.18f, 0.035f);
                d.Headlights(0.74f, 0.96f, 0.34f, 0.14f, 6f);
                d.Front(CarMat.Black, 0f, 0.60f, new Vector3(1.60f, 0.14f, 0.07f));
                d.Front(CarMat.Light, 0.70f, 0.62f, new Vector3(0.12f, 0.05f, 0.03f), 0f, 0.04f);
                d.Front(CarMat.Silver, 0f, 0.52f, new Vector3(1.0f, 0.06f, 0.14f));
                // rear: L-shaped lamps, chrome garnish, black bumper
                d.Rear(CarMat.Tail, 0.80f, 1.25f, new Vector3(0.22f, 0.30f, 0.07f), 0f, 0.01f);
                d.Rear(CarMat.Tail, 0.62f, 1.14f, new Vector3(0.20f, 0.07f, 0.06f), 0f, 0.01f);
                d.Rear(CarMat.Chrome, 0f, 1.20f, new Vector3(0.95f, 0.05f, 0.04f), 0f, 0.01f);
                d.Rear(CarMat.Black, 0f, 0.62f, new Vector3(1.75f, 0.12f, 0.08f));
                d.Box(CarMat.Paint, new Vector3(0f, 1.93f, -2.36f), new Vector3(1.4f, 0.05f, 0.20f));
            }
        };

        // ---------- MG HS-style crossover: 4.57 x 1.88 x 1.66 m, wb 2.72 m, R18 ----------
        public static readonly CarSpec MG = new CarSpec
        {
            name = "SportCrossoverStyle", topSpeedKmh = 185f, accel = 25f, engine = EngineSound.Petrol4, length = 4.574f, frontAxle = 1.387f, rearAxle = -1.333f, halfTrack = 0.79f,
            wheelRadius = 0.355f, tyreWidth = 0.235f, rimRatio = 0.66f, spokes = 5, twinSpokes = true,
            sill = 0.35f, belt = 1.00f, roof = 1.664f, wLow = 0.938f, wBelt = 0.89f, wRoof = 0.66f, taper = 0.11f,
            rearUnderZ = -2.12f,
            top = new[]
            {
                V(2.18f, 0.37f), V(2.27f, 0.48f), V(2.287f, 0.68f), V(2.25f, 0.84f), V(2.12f, 0.93f), V(1.50f, 0.97f),
                V(1.02f, 0.99f), V(0.95f, 1.03f), V(0.15f, 1.64f), V(-0.10f, 1.664f), V(-1.25f, 1.645f), V(-1.80f, 1.56f),
                V(-2.10f, 1.44f), V(-2.24f, 1.15f), V(-2.287f, 0.95f), V(-2.28f, 0.68f), V(-2.24f, 0.46f)
            },
            windshieldA = 7, rearGlassA = 12,
            sideWindow = new[] { V(0.85f, 1.04f), V(0.18f, 1.58f), V(-1.25f, 1.60f), V(-1.78f, 1.51f), V(-2.02f, 1.38f), V(-2.06f, 1.04f) },
            pillars = new[] { -0.42f },
            plateFrontY = 0.48f, plateRearY = 0.82f,
            palette = new[] { Red, White, Blue, Grey, Black },
            details = d =>
            {
                // star-pattern grille, swept headlights
                d.Front(CarMat.Black, 0f, 0.68f, new Vector3(1.15f, 0.34f, 0.04f));
                d.DotGrid(CarMat.Chrome, 0.68f, 1.05f, 0.28f, 9, 4, 0.04f);
                d.Front(CarMat.Chrome, 0f, 0.86f, new Vector3(1.10f, 0.03f, 0.05f), 0f, 0.01f);
                d.Headlights(0.66f, 0.83f, 0.42f, 0.08f, 20f);
                d.Front(CarMat.Black, 0.70f, 0.52f, new Vector3(0.22f, 0.10f, 0.06f), 15f);
                d.Front(CarMat.Chrome, 0.70f, 0.52f, new Vector3(0.24f, 0.015f, 0.07f), 15f, 0.005f);
                d.Front(CarMat.Black, 0f, 0.44f, new Vector3(0.95f, 0.07f, 0.06f));
                // rear
                d.Rear(CarMat.Tail, 0.64f, 1.05f, new Vector3(0.40f, 0.10f, 0.07f), -18f);
                d.Rear(CarMat.Chrome, 0f, 1.03f, new Vector3(0.70f, 0.04f, 0.04f), 0f, 0.01f);
                d.Rear(CarMat.Black, 0f, 0.47f, new Vector3(1.45f, 0.10f, 0.07f));
                d.Rear(CarMat.Chrome, 0.45f, 0.42f, new Vector3(0.14f, 0.05f, 0.06f), 0f, 0.02f);
                d.Box(CarMat.Paint, new Vector3(0f, 1.52f, -1.98f), new Vector3(1.10f, 0.04f, 0.20f), new Vector3(-15f, 0f, 0f));
            }
        };

        public static readonly CarSpec[] All = { Fortuner, Haval, Lambo, RangeRover, Jaecoo, LandCruiser, MG };

        /// <summary>Random model for traffic (weighted) and a random factory colour.</summary>
        public static CarSpec RandomTraffic(System.Random rng, out Color paint)
        {
            float total = 0f;
            foreach (var s in All) total += s.trafficWeight;
            float pick = (float)rng.NextDouble() * total;
            CarSpec chosen = All[0];
            foreach (var s in All)
            {
                pick -= s.trafficWeight;
                if (pick <= 0f) { chosen = s; break; }
            }
            paint = chosen.palette[rng.Next(chosen.palette.Length)];
            return chosen;
        }
    }
}
