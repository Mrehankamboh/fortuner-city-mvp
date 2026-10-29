using System.Collections.Generic;
using UnityEngine;
using FortunerCity.Traffic;
using FortunerCity.Vehicle;

namespace FortunerCity.City
{
    /// <summary>
    /// Elevated ring motorway around the city, built like a real one:
    /// - curved corners (70 m radius) instead of square turns
    /// - box-girder concrete deck on hammerhead piers
    /// - per direction: median shoulder, three 3.65 m lanes, edge lines, rumble strip and a hard shoulder
    /// - New Jersey concrete barriers (double-sided in the median, single-sided at the edges)
    /// - twin-arm lamp posts on the median, truss gantries with green direction signs, speed-limit signs
    /// - one on-ramp per side with a smooth vertical curve and a tapered merge
    /// Each side owns its straight plus the corner at its -X end, built in a local frame rotated 0/90/180/270°.
    /// </summary>
    public static class MotorwayBuilder
    {
        public const float Ring = 326f;              // centre line distance from the city centre
        public const float DeckTop = 9f;
        public const float CornerRadius = 70f;       // centre line radius of the corner curves
        const float MedianHalf = 0.3f;                // half the median barrier base
        const float RoadOuter = MedianHalf + CityArt.MotorwayWidth;   // 15.25 m: edge barrier road face
        const float DeckHalf = 16f;
        const float GirderDepth = 2.3f;
        const float MidLane = MedianHalf + 1f + 3.65f * 1.5f;         // middle lane centre (6.775 m)
        const float RampHalf = 4f;
        const float RampRun = 150f;
        const float MergeLength = 40f;
        const float BarrierTop = 1.07f;

        static float Straight => Ring - CornerRadius;                  // straight runs from -Straight to +Straight
        static float ArcLength => CornerRadius * Mathf.PI * 0.5f;
        static float SideLength => ArcLength + 2f * Straight;

        // ---------- cross sections (d = lateral toward the city, y = up from the deck top) ----------

        static readonly Vector2[] DeckProfile =
        {
            new Vector2(-5.5f, -GirderDepth), new Vector2(5.5f, -GirderDepth), new Vector2(7.5f, -0.6f), new Vector2(DeckHalf, -0.35f),
            new Vector2(DeckHalf, 0f), new Vector2(-DeckHalf, 0f), new Vector2(-DeckHalf, -0.35f), new Vector2(-7.5f, -0.6f)
        };

        /// <summary>Single-sided New Jersey barrier: road face at d = 0, body toward +d.</summary>
        static readonly Vector2[] EdgeBarrier =
        {
            new Vector2(0.42f, 0f), new Vector2(0.42f, BarrierTop), new Vector2(0.24f, BarrierTop),
            new Vector2(0.18f, 0.33f), new Vector2(0f, 0.075f), new Vector2(0f, 0f)
        };

        static readonly Vector2[] MedianBarrier =
        {
            new Vector2(0.3f, 0f), new Vector2(0.3f, 0.075f), new Vector2(0.12f, 0.33f), new Vector2(0.075f, BarrierTop),
            new Vector2(-0.075f, BarrierTop), new Vector2(-0.12f, 0.33f), new Vector2(-0.3f, 0.075f), new Vector2(-0.3f, 0f)
        };

        static readonly Vector2[] RoadStrip = { new Vector2(RoadOuter, 0.008f), new Vector2(MedianHalf, 0.008f) };

        static readonly Vector2[] RampProfile =
        {
            new Vector2(-2.6f, -1.1f), new Vector2(2.6f, -1.1f), new Vector2(3.4f, -0.35f), new Vector2(RampHalf, -0.3f),
            new Vector2(RampHalf, 0f), new Vector2(-RampHalf, 0f), new Vector2(-RampHalf, -0.3f), new Vector2(-3.4f, -0.35f)
        };

        // Direction signs per side: [side] = { inner carriageway (anticlockwise traffic), outer (clockwise) }
        static readonly string[][][] SignText =
        {
            new[] { new[] { "^ ISLAMABAD 24 KM", "LAHORE 9 KM" }, new[] { "^ SIALKOT 27 KM", "GUJRANWALA 8 KM" } },
            new[] { new[] { "^ PESHAWAR 41 KM", "MARDAN 17 KM" }, new[] { "^ MULTAN 33 KM", "SAHIWAL 12 KM" } },
            new[] { new[] { "^ KARACHI 58 KM", "HYDERABAD 21 KM" }, new[] { "^ QUETTA 46 KM", "SUKKUR 19 KM" } },
            new[] { new[] { "^ FAISALABAD 31 KM", "JHANG 14 KM" }, new[] { "^ MURREE 22 KM", "TAXILA 7 KM" } },
        };

        public static List<WaypointPath> Build(Transform cityRoot, Transform props, bool kit, System.Random rng)
        {
            var root = new GameObject("Motorway").transform;
            root.SetParent(cityRoot, false);

            var path = SidePath();
            for (int side = 0; side < 4; side++)
            {
                var frame = new GameObject("Side" + side).transform;
                frame.SetParent(root, false);
                frame.localRotation = Quaternion.Euler(0f, side * 90f, 0f);
                // order of this side along the direction of travel (0 → rotation 270° → 180° → 90°)
                BuildSide(frame, path, (4 - side) % 4, side);
            }

            StaticBatchingUtility.Combine(root.gameObject);
            Physics.SyncTransforms();

            // Left-hand traffic: anticlockwise ring uses the inner carriageway, clockwise the outer. The corner radius
            // matches the deck curve at each lane's offset so cars follow the bend.
            return new List<WaypointPath>
            {
                CityBuilder.MakeLoop(cityRoot, "Loop_Motorway_CCW", Ring - MidLane, DeckTop, false, CornerRadius - MidLane, 12f, 1.8f),
                CityBuilder.MakeLoop(cityRoot, "Loop_Motorway_CW", Ring + MidLane, DeckTop, true, CornerRadius + MidLane, 12f, 1.8f),
            };
        }

        /// <summary>
        /// Local frame centre line at deck height: the corner arc at the -X end (from the west side's straight)
        /// then the straight along z = -Ring heading +X. Lateral (+d) points into the city.
        /// </summary>
        static SweepPath SidePath()
        {
            var p = new SweepPath();
            float c = Straight;
            const int steps = 40;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps * Mathf.PI * 0.5f;
                float phi = Mathf.PI + t;
                p.Add(new Vector3(-c + CornerRadius * Mathf.Cos(phi), DeckTop, -c + CornerRadius * Mathf.Sin(phi)), t);
            }
            // straight in ~24 m pieces: keeps collider triangles well shaped
            int pieces = Mathf.CeilToInt(2f * c / 24f);
            for (int i = 1; i <= pieces; i++)
                p.Add(new Vector3(Mathf.Lerp(-c, c, i / (float)pieces), DeckTop, -Ring), Mathf.PI * 0.5f);
            return p;
        }

        /// <summary>Distance along the side path of a point on its straight.</summary>
        static float SOnStraight(float x) => ArcLength + x + Straight;

        /// <summary>
        /// Texture v that runs continuously round the whole ring at lateral offset d: the tile length is nudged so
        /// a whole number of tiles fits the ring at that offset (no seams in the dashes).
        /// </summary>
        static float RingV(float d, float s, float theta, int order, float tile)
        {
            float sideLen = SideLength - d * Mathf.PI * 0.5f;
            float ringLen = sideLen * 4f;
            float t = ringLen / Mathf.Max(1f, Mathf.Round(ringLen / tile));
            return (order * sideLen + s - d * theta) / t;
        }

        static void BuildSide(Transform f, SweepPath path, int order, int sideIndex)
        {
            float L = path.Length;
            float connectorX = CityBuilder.Line(1);                       // street that leads to the ramp
            float rampStart = connectorX + CityBuilder.StreetWidth * 0.5f;
            float rampTop = rampStart + RampRun;
            float mergeEnd = rampTop + MergeLength;
            float gapStart = SOnStraight(rampTop - 3f), gapEnd = SOnStraight(mergeEnd);

            // Deck (box girder) and road surfaces
            var deck = new SweepMesh();
            deck.Extrude(0, path, 0f, L, DeckProfile, true, false,
                (d, y, pl, s, th) => new Vector2(pl / 4f, RingV(d, s, th, order, 4f)));
            deck.Spawn("Deck", f, new[] { CityArt.Concrete }, true);

            var road = new SweepMesh();
            SweepMesh.UvFn roadUv = (d, y, pl, s, th) => new Vector2((Mathf.Abs(d) - MedianHalf) / CityArt.MotorwayWidth,
                RingV(d, s, th, order, CityArt.RoadTileLength));
            road.Extrude(0, path, 0f, L, RoadStrip, false, false, roadUv);
            road.Extrude(0, path, 0f, L, RoadStrip, false, true, roadUv);
            road.Spawn("Carriageways", f, new[] { CityArt.Motorway }, false);

            // Concrete barriers: median, outer edge, inner edge with a gap where the ramp merges
            SweepMesh.UvFn barrierUv = (d, y, pl, s, th) => new Vector2(pl / 1.5f, (s - d * th) / 3f);
            var barriers = new SweepMesh();
            barriers.Extrude(0, path, 0f, L, MedianBarrier, false, false, barrierUv);
            barriers.Extrude(0, path, 0f, L, EdgeBarrier, false, true, barrierUv, -RoadOuter);
            barriers.Extrude(0, path, 0f, gapStart, EdgeBarrier, false, false, barrierUv, RoadOuter);
            barriers.Cap(0, path, gapStart, EdgeBarrier, false, true, RoadOuter);
            barriers.Extrude(0, path, gapEnd, L, EdgeBarrier, false, false, barrierUv, RoadOuter);
            barriers.Spawn("Barriers", f, new[] { CityArt.Barrier }, true);

            // Hammerhead piers under the girder
            for (float at = 18f; at < L - 4f; at += 36f)
            {
                var sm = path.At(at);
                var pier = CityMesh.Spawn("Pier", f, PierMesh(), new[] { CityArt.Concrete },
                    new Vector3(sm.pos.x, 0f, sm.pos.z), Quaternion.LookRotation(sm.tangent), false);
                var col = pier.AddComponent<BoxCollider>();
                float colTop = DeckTop - GirderDepth;
                col.center = new Vector3(0f, colTop * 0.5f, 0f);
                col.size = new Vector3(3.8f, colTop, 1.8f);
            }

            // Twin-arm lamp posts standing on the median barrier
            float gantryX = Ring * 0.45f;
            float gantryS = SOnStraight(gantryX);
            var lampMats = new[] { CityArt.Steel, ProceduralCar.Shared()[(int)CarMat.Light] };
            for (float at = 12f; at < L - 6f; at += 45f)
            {
                if (Mathf.Abs(at - gantryS) < 10f) continue;
                var sm = path.At(at);
                CityMesh.Spawn("LampPost", f, LampMesh(), lampMats, sm.pos + Vector3.up * BarrierTop,
                    Quaternion.LookRotation(sm.tangent), false);
            }

            BuildGantry(f, gantryX, SignText[sideIndex]);
            SpeedSign(f, new Vector3(mergeEnd + 40f, DeckTop, -Ring + RoadOuter + 0.21f), true);
            SpeedSign(f, new Vector3(-60f, DeckTop, -Ring - RoadOuter - 0.21f), false);

            BuildRamp(f, connectorX, rampStart, rampTop, mergeEnd);
        }

        // ---------- gantry and signs ----------

        static Mesh _gantry;

        /// <summary>Steel box-truss gantry (local: X along the road, Z across), posts standing on the edge barriers.</summary>
        static Mesh GantryMesh()
        {
            if (_gantry != null) return _gantry;
            var mb = new MeshBuilder(1);
            GantryParts(mb);
            return _gantry = mb.ToMesh("Gantry");
        }

        static void GantryParts(MeshBuilder mb)
        {
            float post = RoadOuter + 0.25f, span = DeckHalf - 0.1f;
            float yLow = BarrierTop + 6.0f, yHigh = BarrierTop + 7.0f;
            foreach (float z in new[] { -post, post })
                mb.Tube(0, new Vector3(0f, BarrierTop, z), new Vector3(0f, yHigh + 0.2f, z), 0.24f, 14);
            foreach (float x in new[] { -0.45f, 0.45f })
                foreach (float y in new[] { yLow, yHigh })
                    mb.Tube(0, new Vector3(x, y, -span), new Vector3(x, y, span), 0.08f, 8);
            int bays = 20;
            for (int i = 0; i <= bays; i++)
            {
                float z = Mathf.Lerp(-span, span, i / (float)bays);
                float zn = Mathf.Lerp(-span, span, (i + 1) / (float)bays);
                foreach (float x in new[] { -0.45f, 0.45f })
                {
                    mb.Tube(0, new Vector3(x, yLow, z), new Vector3(x, yHigh, z), 0.045f, 6);
                    if (i < bays) mb.Tube(0, new Vector3(x, i % 2 == 0 ? yLow : yHigh, z), new Vector3(x, i % 2 == 0 ? yHigh : yLow, zn), 0.04f, 6);
                }
                mb.Tube(0, new Vector3(-0.45f, yHigh, z), new Vector3(0.45f, yHigh, z), 0.04f, 6);
                mb.Tube(0, new Vector3(-0.45f, yLow, z), new Vector3(0.45f, yLow, z), 0.04f, 6);
            }
        }

        static void BuildGantry(Transform f, float x, string[][] text)
        {
            CityMesh.Spawn("Gantry", f, GantryMesh(), new[] { CityArt.Steel }, new Vector3(x, DeckTop, -Ring), Quaternion.identity, false);
            const float w = 9f, h = 3f;
            var board = SignArt.Board(w, h, 0.12f);
            float y = DeckTop + BarrierTop + 6.5f;
            float laneMid = MedianHalf + 1f + 3.65f * 1.5f;
            // inner carriageway traffic heads +X: its board faces -X; outer traffic heads -X
            CityMesh.Spawn("SignInner", f, board, new[] { SignArt.DirectionSign(text[0], w / h), CityArt.Steel },
                new Vector3(x - 0.62f, y, -Ring + laneMid), Quaternion.Euler(0f, -90f, 0f), false);
            CityMesh.Spawn("SignOuter", f, board, new[] { SignArt.DirectionSign(text[1], w / h), CityArt.Steel },
                new Vector3(x + 0.62f, y, -Ring - laneMid), Quaternion.Euler(0f, 90f, 0f), false);
        }

        static Mesh _disc;

        /// <summary>120 km/h sign on a short post on top of an edge barrier.</summary>
        static void SpeedSign(Transform f, Vector3 basePos, bool facesMinusX)
        {
            var top = basePos + Vector3.up * BarrierTop;
            CityMesh.SpawnBox("SignPost", f, new Vector3(0.08f, 1.9f, 0.08f), top, CityArt.Steel, CityArt.Steel, false);
            _disc ??= SignArt.Disc(0.45f, 0.04f);
            CityMesh.Spawn("SpeedLimit", f, _disc, new[] { SignArt.SpeedLimit120, CityArt.Steel },
                top + new Vector3(facesMinusX ? -0.06f : 0.06f, 1.9f, 0f), Quaternion.Euler(0f, facesMinusX ? -90f : 90f, 0f), false);
        }

        // ---------- piers and lamps ----------

        static Mesh _pier, _lamp;

        /// <summary>Oval column flaring into a hammerhead under the box girder (local Z = along the road).</summary>
        static Mesh PierMesh()
        {
            if (_pier != null) return _pier;
            float top = DeckTop - GirderDepth;
            _pier = PierShape(top, 1.9f, 0.9f, 5.3f, 1.05f, "Pier");
            return _pier;
        }

        static Mesh PierShape(float top, float colW, float colD, float headW, float headD, string name)
        {
            var mb = new MeshBuilder(1);
            PierParts(mb, top, colW, colD, headW, headD);
            return mb.ToMesh(name);
        }

        static void PierParts(MeshBuilder mb, float top, float colW, float colD, float headW, float headD)
        {
            float flare = Mathf.Min(2.2f, top * 0.4f);
            mb.SmoothLathe(0, new[]
            {
                new Vector4(colW * 1.08f, colD * 1.1f, 0f, 0f),
                new Vector4(colW, colD, Mathf.Min(0.4f, top * 0.1f), 0f),
                new Vector4(colW, colD, top - flare, 0f),
                new Vector4(Mathf.Lerp(colW, headW, 0.15f), Mathf.Lerp(colD, headD, 0.3f), top - flare * 0.68f, 0f),
                new Vector4(Mathf.Lerp(colW, headW, 0.45f), Mathf.Lerp(colD, headD, 0.7f), top - flare * 0.38f, 0f),
                new Vector4(Mathf.Lerp(colW, headW, 0.85f), headD, top - flare * 0.12f, 0f),
                new Vector4(headW, headD, top, 0f),
            }, 24, Vector3.zero, false, true);
        }

        /// <summary>10.6 m tapered steel pole with two arms reaching over both carriageways (local X = across).</summary>
        static Mesh LampMesh()
        {
            if (_lamp != null) return _lamp;
            var mb = new MeshBuilder(2);
            LampParts(mb);
            return _lamp = mb.ToMesh("MotorwayLamp");
        }

        static void LampParts(MeshBuilder mb)
        {
            mb.SmoothLathe(0, new[]
            {
                new Vector4(0.22f, 0.22f, 0f, 0f), new Vector4(0.22f, 0.22f, 0.04f, 0f), new Vector4(0.12f, 0.12f, 0.06f, 0f),
                new Vector4(0.1f, 0.1f, 1.2f, 0f), new Vector4(0.065f, 0.065f, 10.2f, 0f), new Vector4(0.06f, 0.06f, 10.4f, 0f)
            }, 12, Vector3.zero, false, true);
            foreach (float s in new[] { -1f, 1f })
            {
                mb.Tube(0, new[] { new Vector3(0f, 9.8f, 0f), new Vector3(s * 0.7f, 10.25f, 0f), new Vector3(s * 1.6f, 10.5f, 0f), new Vector3(s * 2.6f, 10.6f, 0f) }, 0.055f, 8);
                mb.Sweep(0, new[] { new Vector3(s * 2.4f, 10.6f, 0f), new Vector3(s * 2.9f, 10.62f, 0f), new Vector3(s * 3.3f, 10.6f, 0f) },
                    new[] { new Vector2(0.15f, 0.07f), new Vector2(0.17f, 0.08f), new Vector2(0.12f, 0.05f) }, 12, 3f, Vector3.forward);
                mb.Box(1, new Vector3(s * 2.88f, 10.535f, 0f), new Vector3(0.66f, 0.012f, 0.24f), Quaternion.identity);
            }
        }

        // ---------- on-ramp ----------

        /// <summary>
        /// Ramp centre line beside the deck's inner edge: sag curve off the street, steady climb, crest curve onto
        /// deck level, then the flat merge.
        /// </summary>
        static SweepPath RampPath(float rampStart, float mergeEnd, float z)
        {
            const float Vc = 20f;
            float y0 = CityBuilder.RoadTop;
            float g = (DeckTop - y0) / (RampRun - Vc);
            var p = new SweepPath();
            for (float x = rampStart; ; x += 5f)
            {
                float xc = Mathf.Min(x, mergeEnd);
                float u = xc - rampStart, y;
                if (u <= Vc) y = y0 + g * u * u / (2f * Vc);
                else if (u <= RampRun - Vc) y = y0 + g * Vc * 0.5f + g * (u - Vc);
                else if (u <= RampRun)
                {
                    float w = u - (RampRun - Vc);
                    y = y0 + g * Vc * 0.5f + g * (RampRun - 2f * Vc) + g * w - g * w * w / (2f * Vc);
                }
                else y = DeckTop;
                p.Add(new Vector3(xc, y, z));
                if (xc >= mergeEnd) break;
            }
            return p;
        }

        static float SAtX(SweepPath p, float x)
        {
            for (int i = 0; i < p.pos.Count - 1; i++)
                if (x <= p.pos[i + 1].x)
                {
                    float t = Mathf.InverseLerp(p.pos[i].x, p.pos[i + 1].x, x);
                    return Mathf.Lerp(p.s[i], p.s[i + 1], t);
                }
            return p.Length;
        }

        /// <summary>Connector street from the outer avenue to a ramp that climbs beside the deck and merges in.</summary>
        static void BuildRamp(Transform f, float connectorX, float rampStart, float rampTop, float mergeEnd)
        {
            float deckInner = -Ring + DeckHalf;
            float rampZ = deckInner + RampHalf;
            float avenueEdge = -(CityBuilder.Half + CityBuilder.AvenueWidth * 0.5f);
            float connLen = avenueEdge - deckInner;

            // Ground connector (local Z is its length, so no rotation)
            CityBuilder.RoadSegment(f, f, new Vector3(connectorX, 0f, (avenueEdge + deckInner) * 0.5f), connLen,
                CityBuilder.StreetWidth, false, false, false, null);

            var path = RampPath(rampStart, mergeEnd, rampZ);
            float L = path.Length, sTop = SAtX(path, rampTop), sOpen = SAtX(path, rampTop - 3f);

            var slab = new SweepMesh();
            slab.Extrude(0, path, 0f, L, RampProfile, true, false, (d, y, pl, s, th) => new Vector2(pl / 4f, s / 4f));
            slab.Cap(0, path, 0f, RampProfile, false, false);
            slab.Cap(0, path, L, RampProfile, false, true);
            slab.Spawn("RampDeck", f, new[] { CityArt.Concrete }, true);

            var surface = new SweepMesh();
            surface.Extrude(0, path, 0f, L, new[] { new Vector2(RampHalf - 0.42f, 0.008f), new Vector2(-RampHalf + 0.42f, 0.008f) },
                false, false, (d, y, pl, s, th) => new Vector2(d / 4f, s / 4f));
            surface.Spawn("RampSurface", f, new[] { CityArt.Asphalt }, false);

            var lines = new SweepMesh();
            foreach (float e in new[] { -3.2f, 3.2f })
                lines.Extrude(0, path, 2f, sTop, new[] { new Vector2(e + 0.075f, 0.012f), new Vector2(e - 0.075f, 0.012f) },
                    false, false, (d, y, pl, s, th) => new Vector2(d, s / 4f));
            lines.Spawn("RampLines", f, new[] { CityArt.LinePaint }, false);

            // Barriers start a little up the ramp so cars can turn in from the connector street
            SweepMesh.UvFn uv = (d, y, pl, s, th) => new Vector2(pl / 1.5f, s / 3f);
            var rails = new SweepMesh();
            rails.Extrude(0, path, 12f, sTop, EdgeBarrier, false, false, uv, RampHalf - 0.42f);
            rails.Cap(0, path, 12f, EdgeBarrier, false, false, RampHalf - 0.42f);
            rails.Extrude(0, path, 12f, sOpen, EdgeBarrier, false, true, uv, -RampHalf + 0.42f);
            rails.Cap(0, path, 12f, EdgeBarrier, true, false, -RampHalf + 0.42f);
            rails.Cap(0, path, sOpen, EdgeBarrier, true, true, -RampHalf + 0.42f);
            // Taper: the outer rail angles in to meet the deck's edge barrier, squeezing the merge lane to nothing
            var taper = SweepPath.Polyline(new Vector3(rampTop, DeckTop, rampZ + RampHalf - 0.42f),
                new Vector3(mergeEnd, DeckTop, -Ring + RoadOuter));
            rails.Extrude(0, taper, 0f, taper.Length, EdgeBarrier, false, false, uv);
            rails.Spawn("RampBarriers", f, new[] { CityArt.Barrier }, true);

            // Piers wherever the ramp is high enough to need one
            for (float at = 30f; at < L - 4f; at += 24f)
            {
                var sm = path.At(at);
                float top = sm.pos.y - 1.1f;
                if (top < 2f) continue;
                var rp = CityMesh.Spawn("RampPier", f, PierShape(top, 0.7f, 0.5f, 2.5f, 0.6f, "RampPier"), new[] { CityArt.Concrete },
                    new Vector3(sm.pos.x, 0f, sm.pos.z), Quaternion.LookRotation(sm.tangent), false);
                var col = rp.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, top * 0.5f, 0f);
                col.size = new Vector3(1.4f, top, 1f);
            }
        }
    }
}
