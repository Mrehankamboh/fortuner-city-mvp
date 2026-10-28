using System.Collections.Generic;
using UnityEngine;
using FortunerCity.Traffic;

namespace FortunerCity.City
{
    /// <summary>
    /// Elevated ring motorway around the city: 3 lanes each way, concrete median and side barriers,
    /// piers, gantry signs, and one ramp per side linking a city street to the deck.
    /// Each side is built in a local frame (as the south side) and rotated 0/90/180/270°.
    /// </summary>
    public static class MotorwayBuilder
    {
        public const float Ring = 326f;          // centre line distance from the city centre
        public const float DeckTop = 9f;
        const float DeckWidth = 27f;              // 2 x 12.5 carriageway + 2 m median
        const float DeckDepth = 1.4f;
        const float Carriageway = 11f;            // lane-marked width (Road3 texture)
        const float MidLane = 7.5f;               // middle lane offset from the centre line
        const float RampWidth = 8f;
        const float RampRun = 150f;               // ~6% grade
        const float MergeLength = 40f;

        public static List<WaypointPath> Build(Transform cityRoot, Transform props, bool kit, System.Random rng)
        {
            var root = new GameObject("Motorway").transform;
            root.SetParent(cityRoot, false);

            for (int side = 0; side < 4; side++)
            {
                var frame = new GameObject("Side" + side).transform;
                frame.SetParent(root, false);
                frame.localRotation = Quaternion.Euler(0f, side * 90f, 0f);
                BuildSide(frame);
            }

            StaticBatchingUtility.Combine(root.gameObject);
            Physics.SyncTransforms();

            // Left-hand traffic: anticlockwise ring uses the inner carriageway, clockwise the outer
            return new List<WaypointPath>
            {
                CityBuilder.MakeLoop(cityRoot, "Loop_Motorway_CCW", Ring - MidLane, DeckTop, false, 6f, 10f, 1.8f),
                CityBuilder.MakeLoop(cityRoot, "Loop_Motorway_CW", Ring + MidLane, DeckTop, true, 6f, 10f, 1.8f),
            };
        }

        /// <summary>Local frame: deck runs along X at z = -Ring; +Z points into the city.</summary>
        static void BuildSide(Transform f)
        {
            float z = -Ring;
            float hw = DeckWidth * 0.5f;
            float deckLen = 2f * Ring;                         // from corner centre to corner centre
            float inner = z + hw, outer = z - hw;
            var along = Quaternion.Euler(0f, 90f, 0f);          // quads: length along local X

            // Deck covers this side plus the corner square at its -X end (each corner owned once)
            CityMesh.SpawnBox("Deck", f, new Vector3(deckLen, DeckDepth, DeckWidth),
                new Vector3(-hw, DeckTop - DeckDepth, z), CityArt.Concrete, CityArt.Asphalt, true);

            // Lane markings between corners
            float laneLen = deckLen - DeckWidth;
            float y = DeckTop + 0.012f;
            float cOff = 1f + 0.75f + Carriageway * 0.5f;      // half-median + shoulder + half-carriageway
            CityMesh.SpawnQuad("LanesInner", f, CityArt.Road3, Carriageway, laneLen, Carriageway, CityArt.RoadTileLength,
                new Vector3(0f, y, z + cOff), along, true);
            CityMesh.SpawnQuad("LanesOuter", f, CityArt.Road3, Carriageway, laneLen, Carriageway, CityArt.RoadTileLength,
                new Vector3(0f, y, z - cOff), along);

            // Median barrier (meets the next side's median at the corner centres)
            CityMesh.SpawnBox("Median", f, new Vector3(deckLen, 0.9f, 0.6f), new Vector3(0f, DeckTop, z), CityArt.Barrier, CityArt.Barrier, true);

            // Outer barrier full length; inner barrier between corners with a gap where the ramp merges
            CityMesh.SpawnBox("BarrierOuter", f, new Vector3(deckLen + DeckWidth, 1.1f, 0.5f),
                new Vector3(0f, DeckTop, outer + 0.25f), CityArt.Barrier, CityArt.Barrier, true);

            float connectorX = CityBuilder.Line(1);                       // street that leads to the ramp
            float rampStart = connectorX + CityBuilder.StreetWidth * 0.5f;
            float rampTop = rampStart + RampRun;
            float mergeEnd = rampTop + MergeLength;
            float innerStart = -Ring + hw, innerEnd = Ring - hw;
            BarrierX(f, innerStart, rampTop - 3f, inner - 0.25f);
            BarrierX(f, mergeEnd, innerEnd, inner - 0.25f);

            // Piers every 30 m
            for (float x = -Ring + 15f; x < Ring - 10f; x += 30f)
            {
                float colH = DeckTop - DeckDepth - 1.2f;
                CityMesh.SpawnBox("Pier", f, new Vector3(2f, colH, 2f), new Vector3(x, 0f, z - 7f), CityArt.Concrete, CityArt.Concrete, true);
                CityMesh.SpawnBox("Pier", f, new Vector3(2f, colH, 2f), new Vector3(x, 0f, z + 7f), CityArt.Concrete, CityArt.Concrete, true);
                CityMesh.SpawnBox("PierCap", f, new Vector3(2.5f, 1.2f, DeckWidth - 3f), new Vector3(x, colH, z), CityArt.Concrete, CityArt.Concrete, false);
            }

            // Lamp posts on the median every 50 m
            for (float x = -Ring + 25f; x < Ring - 20f; x += 50f)
            {
                CityMesh.SpawnBox("LampPost", f, new Vector3(0.3f, 10f, 0.3f), new Vector3(x, DeckTop + 0.9f, z), CityArt.Barrier, CityArt.Barrier, false);
                CityMesh.SpawnBox("LampArm", f, new Vector3(0.25f, 0.25f, 9f), new Vector3(x, DeckTop + 10.6f, z), CityArt.Barrier, CityArt.Barrier, false);
            }

            // Gantry sign over both carriageways
            float gx = Ring * 0.45f;
            CityMesh.SpawnBox("GantryPostIn", f, new Vector3(0.6f, 7.5f, 0.6f), new Vector3(gx, DeckTop + 1.1f, inner - 0.25f), CityArt.Barrier, CityArt.Barrier, false);
            CityMesh.SpawnBox("GantryPostOut", f, new Vector3(0.6f, 7.5f, 0.6f), new Vector3(gx, DeckTop + 1.1f, outer + 0.25f), CityArt.Barrier, CityArt.Barrier, false);
            CityMesh.SpawnBox("GantryBeam", f, new Vector3(0.6f, 0.6f, DeckWidth), new Vector3(gx, DeckTop + 8.6f, z), CityArt.Barrier, CityArt.Barrier, false);
            CityMesh.SpawnBox("SignIn", f, new Vector3(0.3f, 3f, 9f), new Vector3(gx + 0.5f, DeckTop + 5.8f, z + cOff), CityArt.SignGreen, CityArt.SignGreen, false);
            CityMesh.SpawnBox("SignOut", f, new Vector3(0.3f, 3f, 9f), new Vector3(gx - 0.5f, DeckTop + 5.8f, z - cOff), CityArt.SignGreen, CityArt.SignGreen, false);

            BuildRamp(f, connectorX, rampStart, rampTop, mergeEnd, inner);
        }

        static void BarrierX(Transform f, float x0, float x1, float zPos)
        {
            if (x1 - x0 < 0.5f) return;
            CityMesh.SpawnBox("BarrierInner", f, new Vector3(x1 - x0, 1.1f, 0.5f), new Vector3((x0 + x1) * 0.5f, DeckTop, zPos),
                CityArt.Barrier, CityArt.Barrier, true);
        }

        /// <summary>Connector street from the outer avenue to a ramp that climbs beside the deck and merges in.</summary>
        static void BuildRamp(Transform f, float connectorX, float rampStart, float rampTop, float mergeEnd, float deckInner)
        {
            float rampZ = deckInner + RampWidth * 0.5f;                     // ramp runs alongside the deck's inner edge
            float avenueEdge = -(CityBuilder.Half + CityBuilder.AvenueWidth * 0.5f);
            float connectorEnd = deckInner;
            float connLen = avenueEdge - connectorEnd;

            // Ground connector (local Z is its length, so no rotation)
            CityBuilder.RoadSegment(f, f, new Vector3(connectorX, 0f, (avenueEdge + connectorEnd) * 0.5f), connLen,
                CityBuilder.StreetWidth, false, false, false, null);

            // Sloped ramp: parent pivots about Z so its +X climbs to the deck
            float rise = DeckTop - CityBuilder.RoadTop;
            float slopeLen = Mathf.Sqrt(RampRun * RampRun + rise * rise);
            float angle = Mathf.Atan2(rise, RampRun) * Mathf.Rad2Deg;
            var ramp = new GameObject("Ramp").transform;
            ramp.SetParent(f, false);
            ramp.localPosition = new Vector3(rampStart + RampRun * 0.5f, CityBuilder.RoadTop + rise * 0.5f, rampZ);
            ramp.localRotation = Quaternion.Euler(0f, 0f, angle);

            CityMesh.SpawnBox("RampDeck", ramp, new Vector3(slopeLen, 0.8f, RampWidth), new Vector3(0f, -0.8f, 0f),
                CityArt.Concrete, CityArt.Asphalt, true);
            // Guard rails (start a little up the ramp so cars can turn in from the connector)
            float railLen = slopeLen - 12f;
            CityMesh.SpawnBox("RampRailIn", ramp, new Vector3(railLen, 1.1f, 0.4f), new Vector3(6f, 0f, RampWidth * 0.5f - 0.2f), CityArt.Barrier, CityArt.Barrier, true);
            CityMesh.SpawnBox("RampRailDeck", ramp, new Vector3(railLen, 1.1f, 0.4f), new Vector3(6f, 0f, -RampWidth * 0.5f + 0.2f), CityArt.Barrier, CityArt.Barrier, true);

            // Ramp piers
            for (float x = rampStart + 30f; x < rampTop - 5f; x += 25f)
            {
                float h = CityBuilder.RoadTop + rise * (x - rampStart) / RampRun - 0.8f;
                if (h < 1.5f) continue;
                CityMesh.SpawnBox("RampPier", f, new Vector3(1.6f, h, 1.6f), new Vector3(x, 0f, rampZ), CityArt.Concrete, CityArt.Concrete, true);
            }

            // Flat merge lane at deck height, open on the deck side
            CityMesh.SpawnBox("MergeDeck", f, new Vector3(mergeEnd - rampTop, DeckDepth, RampWidth),
                new Vector3((rampTop + mergeEnd) * 0.5f, DeckTop - DeckDepth, rampZ), CityArt.Concrete, CityArt.Asphalt, true);
            CityMesh.SpawnBox("MergeRail", f, new Vector3(mergeEnd - rampTop, 1.1f, 0.4f),
                new Vector3((rampTop + mergeEnd) * 0.5f, DeckTop, rampZ + RampWidth * 0.5f - 0.2f), CityArt.Barrier, CityArt.Barrier, true);
            CityMesh.SpawnBox("MergeEnd", f, new Vector3(0.4f, 1.1f, RampWidth),
                new Vector3(mergeEnd - 0.2f, DeckTop, rampZ), CityArt.Barrier, CityArt.Barrier, true);
            CityMesh.SpawnBox("MergePier", f, new Vector3(1.6f, DeckTop - DeckDepth, 1.6f),
                new Vector3((rampTop + mergeEnd) * 0.5f, 0f, rampZ), CityArt.Concrete, CityArt.Concrete, true);
        }
    }
}
