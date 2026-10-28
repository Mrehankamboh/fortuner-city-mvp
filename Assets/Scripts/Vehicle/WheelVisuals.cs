using UnityEngine;

namespace FortunerCity.Vehicle
{
    /// <summary>
    /// Optional: kept for compatibility. Arcade drive spins wheels itself.
    /// </summary>
    public class WheelVisuals : MonoBehaviour
    {
        [System.Serializable]
        public struct WheelPair
        {
            public WheelCollider collider;
            public Transform mesh;
        }

        public WheelPair[] wheels;
    }
}
