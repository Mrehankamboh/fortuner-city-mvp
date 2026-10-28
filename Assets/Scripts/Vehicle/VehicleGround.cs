using UnityEngine;

namespace FortunerCity.Vehicle
{
    /// <summary>
    /// Finds the drivable surface (road / sidewalk / ground) under a car.
    /// Ignores the car itself, other cars (anything with a Rigidbody), triggers and walls.
    /// </summary>
    public static class VehicleGround
    {
        public const float DefaultRoadY = 0.05f;   // top of CityBuilder road strips

        static readonly RaycastHit[] Hits = new RaycastHit[16];

        /// <param name="stepUp">How far above <paramref name="pos"/> a surface may be (curbs).</param>
        /// <param name="probeDown">How far below <paramref name="pos"/> to search.</param>
        public static bool TryGetHeight(Vector3 pos, Transform ignore, float stepUp, float probeDown, out float height, int mask = ~0)
        {
            height = 0f;
            Vector3 origin = pos + Vector3.up * stepUp;
            int n = Physics.RaycastNonAlloc(origin, Vector3.down, Hits, stepUp + probeDown, mask, QueryTriggerInteraction.Ignore);

            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var h = Hits[i];
                if (h.collider == null) continue;
                if (ignore != null && h.collider.transform.IsChildOf(ignore)) continue;
                if (h.collider.attachedRigidbody != null) continue;   // other cars
                if (h.normal.y < 0.6f) continue;                       // walls / steep faces
                if (h.distance < best)
                {
                    best = h.distance;
                    height = h.point.y;
                }
            }
            return best < float.MaxValue;
        }

        /// <summary>Surface height near pos.y (so a spawn on the elevated motorway lands on the deck, not the ground below).</summary>
        public static float SpawnHeight(Vector3 pos, Transform ignore = null)
        {
            return TryGetHeight(pos, ignore, 4f, 6f, out float y) ? y : pos.y;
        }
    }
}
