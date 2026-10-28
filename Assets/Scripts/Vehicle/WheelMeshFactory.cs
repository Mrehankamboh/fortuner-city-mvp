using UnityEngine;

namespace FortunerCity.Vehicle
{
    /// <summary>
    /// Truly round tires using Unity Cylinder (circle faces ±axle).
    /// Hierarchy: Pose → Spin → Tire/Rim (spin rotates around local X).
    /// </summary>
    public static class WheelMeshFactory
    {
        public const float DefaultRadius = 0.38f;
        public const float DefaultWidth = 0.28f;

        public static Transform Create(Transform parent, string name, Vector3 localPos, float radius = DefaultRadius, float width = DefaultWidth)
        {
            // Pose: position + steer (Y)
            var pose = new GameObject("WheelPose_" + name).transform;
            pose.SetParent(parent, false);
            pose.localPosition = localPos;
            pose.localRotation = Quaternion.identity;

            // Spin: rolls around local X (axle)
            var spin = new GameObject("Spin").transform;
            spin.SetParent(pose, false);
            spin.localPosition = Vector3.zero;
            spin.localRotation = Quaternion.identity;

            // Unity Cylinder: height = Y, circular cross-section in XZ (default radius 0.5, height 2).
            // Rotate 90° on Z → axle along X, circular faces point left/right = ROUND when viewed from car side.
            float diam = radius * 2f;
            // scale: (diam, width/2, diam) because mesh radius 0.5 → diam/1 = 2*radius, height 2 → width/2
            var tireScale = new Vector3(diam, width * 0.5f, diam);

            var tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tire.name = "Tire";
            tire.transform.SetParent(spin, false);
            tire.transform.localPosition = Vector3.zero;
            tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            tire.transform.localScale = tireScale;
            Object.Destroy(tire.GetComponent<Collider>());
            ApplyMat(tire, new Color(0.04f, 0.04f, 0.04f), 0.05f, 0.2f);

            // Rubber sidewall lip (slightly wider, thinner) — reinforces round silhouette
            var lip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lip.name = "Lip";
            lip.transform.SetParent(spin, false);
            lip.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            lip.transform.localScale = new Vector3(diam * 1.04f, width * 0.18f, diam * 1.04f);
            Object.Destroy(lip.GetComponent<Collider>());
            ApplyMat(lip, new Color(0.08f, 0.08f, 0.08f), 0.05f, 0.25f);

            // Rim (silver circle)
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim";
            rim.transform.SetParent(spin, false);
            rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rim.transform.localScale = new Vector3(diam * 0.68f, width * 0.52f, diam * 0.68f);
            Object.Destroy(rim.GetComponent<Collider>());
            ApplyMat(rim, new Color(0.78f, 0.8f, 0.85f), 0.75f, 0.7f);

            // Hub center
            var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "Hub";
            hub.transform.SetParent(spin, false);
            hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            hub.transform.localScale = new Vector3(diam * 0.22f, width * 0.56f, diam * 0.22f);
            Object.Destroy(hub.GetComponent<Collider>());
            ApplyMat(hub, new Color(0.18f, 0.18f, 0.2f), 0.4f, 0.45f);

            return pose;
        }

        public static WheelCollider AddCollider(Transform poseRoot, float radius = DefaultRadius)
        {
            var col = poseRoot.gameObject.AddComponent<WheelCollider>();
            col.radius = radius;
            col.enabled = false;
            return col;
        }

        /// <summary>Find Spin child under a wheel pose (for drive script).</summary>
        public static Transform GetSpin(Transform pose)
        {
            if (pose == null) return null;
            var s = pose.Find("Spin");
            return s != null ? s : (pose.childCount > 0 ? pose.GetChild(0) : null);
        }

        static void ApplyMat(GameObject go, Color c, float metallic, float gloss)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", gloss);
            r.sharedMaterial = mat;
        }
    }
}
