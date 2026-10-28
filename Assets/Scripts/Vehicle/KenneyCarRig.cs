using System.Collections.Generic;
using UnityEngine;

namespace FortunerCity.Vehicle
{
    /// <summary>
    /// Sets up a Kenney Car Kit model (body + its own wheel children) under a car root:
    /// - lowest point of the tyres sits exactly at root Y = 0 (root = road surface)
    /// - each wheel is wrapped as Pose (steer, Y) → Spin (roll, X) around its true centre
    /// Scale keeps Y == Z so tyres stay perfectly round; X may differ (narrower body).
    /// </summary>
    public static class KenneyCarRig
    {
        public class Result
        {
            public Transform visual;
            public Transform frontLeft, frontRight, rearLeft, rearRight;
            public readonly List<Transform> wheels = new List<Transform>();
            public float wheelRadius;
            public Bounds bodyBounds;   // root space, wheels excluded

            public bool HasFourWheels => frontLeft && frontRight && rearLeft && rearRight;
        }

        /// <summary>Root must be at origin with identity rotation while rigging.</summary>
        public static Result Build(GameObject prefab, Transform root, Vector3 scale)
        {
            var r = new Result();
            var visual = Object.Instantiate(prefab, root);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = scale;
            foreach (var col in visual.GetComponentsInChildren<Collider>())
                Object.Destroy(col);
            r.visual = visual.transform;

            // Collect wheel nodes (skip spare tyre "wheel-back" — it has no left/right)
            var wheelNodes = new List<Transform>();
            foreach (var t in visual.GetComponentsInChildren<Transform>())
            {
                string n = t.name.ToLowerInvariant();
                if (n.StartsWith("wheel") && (n.Contains("left") || n.Contains("right")))
                    wheelNodes.Add(t);
            }

            // Put the lowest tyre point on Y = 0 (whole model if it has no wheels)
            float minY;
            if (wheelNodes.Count > 0)
            {
                minY = float.MaxValue;
                foreach (var w in wheelNodes)
                    if (TryBounds(w, root, null, out Bounds wb)) minY = Mathf.Min(minY, wb.min.y);
            }
            else
            {
                minY = TryBounds(visual.transform, root, null, out Bounds all) ? all.min.y : 0f;
            }
            if (minY < float.MaxValue)
                visual.transform.localPosition = new Vector3(0f, -minY, 0f);

            // Wrap wheels: Pose (unscaled, under root) → Spin → original wheel mesh
            float radiusSum = 0f;
            foreach (var w in wheelNodes)
            {
                if (!TryBounds(w, root, null, out Bounds wb)) continue;
                var pose = new GameObject("WheelPose_" + w.name).transform;
                pose.SetParent(root, false);
                pose.localPosition = wb.center;
                pose.localRotation = Quaternion.identity;

                var spin = new GameObject("Spin").transform;
                spin.SetParent(pose, false);
                w.SetParent(spin, true);

                radiusSum += wb.extents.y;
                r.wheels.Add(pose);

                bool left = wb.center.x < 0f;
                bool front = w.name.ToLowerInvariant().Contains("front") || (!w.name.ToLowerInvariant().Contains("back") && wb.center.z > 0f);
                if (front && left && r.frontLeft == null) r.frontLeft = pose;
                else if (front && !left && r.frontRight == null) r.frontRight = pose;
                else if (!front && left && r.rearLeft == null) r.rearLeft = pose;
                else if (!front && !left && r.rearRight == null) r.rearRight = pose;
            }
            r.wheelRadius = r.wheels.Count > 0 ? radiusSum / r.wheels.Count : 0.35f;

            // Body bounds without wheels (for the collider)
            var wheelSet = new HashSet<Transform>(r.wheels);
            if (!TryBounds(visual.transform, root, wheelSet, out r.bodyBounds))
                r.bodyBounds = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(1.9f, 1.4f, 4.6f));
            return r;
        }

        /// <summary>Box collider that hugs the body but stays clear of the road.</summary>
        public static BoxCollider AddBodyCollider(GameObject root, Bounds body, float minClearance)
        {
            var box = root.AddComponent<BoxCollider>();
            float bottom = Mathf.Max(body.min.y, minClearance);
            float top = Mathf.Max(body.max.y, bottom + 0.5f);
            box.center = new Vector3(body.center.x, (bottom + top) * 0.5f, body.center.z);
            box.size = new Vector3(body.size.x * 0.92f, top - bottom, body.size.z * 0.96f);
            return box;
        }

        static bool TryBounds(Transform src, Transform space, HashSet<Transform> exclude, out Bounds b)
        {
            b = default;
            bool any = false;
            foreach (var rend in src.GetComponentsInChildren<Renderer>())
            {
                if (exclude != null && IsUnder(rend.transform, exclude)) continue;
                var wb = rend.bounds;
                // root is axis-aligned at origin during rigging, so world AABB == root-space AABB
                var lb = new Bounds(space.InverseTransformPoint(wb.center), wb.size);
                if (!any) { b = lb; any = true; }
                else b.Encapsulate(lb);
            }
            return any;
        }

        static bool IsUnder(Transform t, HashSet<Transform> set)
        {
            for (var p = t; p != null; p = p.parent)
                if (set.Contains(p)) return true;
            return false;
        }
    }
}
