using UnityEngine;

namespace FortunerCity.Vehicle
{
    public enum EngineSound { Petrol4, Diesel4, V8, V10, Single, SportBike }

    /// <summary>
    /// Per-vehicle data that survives a driver change (traffic → player → parked):
    /// top speed, acceleration, engine sound and, for bikes, the lean pivot and seat.
    /// </summary>
    public class VehicleInfo : MonoBehaviour
    {
        public string model;
        public bool isBike;
        public float topSpeedKmh = 170f;
        public float accel = 28f;
        public float maxSteer = 42f;
        public float turnResponsiveness = 2.1f;
        public EngineSound engine = EngineSound.Petrol4;
        public int gears = 6;

        [Header("Bikes")]
        public Transform lean;     // visual pivot that tilts into corners
        public Transform seat;     // rider anchor (feet level of the seated rider)
        public GameObject rider;   // current rider model

        public void ApplyTo(PlayerDriveController d)
        {
            d.maxSpeedKmh = topSpeedKmh;
            d.accel = accel;
            d.maxSteerAngle = maxSteer;
            d.turnResponsiveness = turnResponsiveness;
        }
    }

    /// <summary>Tilts a bike into turns from its real yaw rate and speed (lean ≈ atan(v·ω / g)).</summary>
    public class BikeLean : MonoBehaviour
    {
        public Transform lean;
        float _lastYaw, _angle;
        Vector3 _lastPos;

        void Start()
        {
            _lastYaw = transform.eulerAngles.y;
            _lastPos = transform.position;
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            float yaw = transform.eulerAngles.y;
            float yawRate = Mathf.DeltaAngle(_lastYaw, yaw) * Mathf.Deg2Rad / dt;
            Vector3 d = transform.position - _lastPos;
            d.y = 0f;
            float speed = d.magnitude / dt;
            _lastYaw = yaw;
            _lastPos = transform.position;

            float target = -Mathf.Atan(speed * yawRate / 9.81f) * Mathf.Rad2Deg;
            target = Mathf.Clamp(target, -38f, 38f);
            _angle = Mathf.Lerp(_angle, target, 1f - Mathf.Exp(-6f * dt));
            if (lean != null) lean.localRotation = Quaternion.Euler(0f, 0f, _angle);
        }
    }
}
