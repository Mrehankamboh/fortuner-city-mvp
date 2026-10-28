using UnityEngine;
using FortunerCity.Core;

namespace FortunerCity.Vehicle
{
    /// <summary>
    /// Stable arcade drive — raycast ground stick (no WheelCollider launch).
    /// Works for cars and bikes (bikes only set the front-left / rear-left wheel poses).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerDriveController : MonoBehaviour
    {
        [Header("Wheels (visual poses: Pose → Spin)")]
        public Transform frontLeft;
        public Transform frontRight;
        public Transform rearLeft;
        public Transform rearRight;
        public float wheelRadius = 0.35f;

        [Header("Arcade Tuning")]
        public float accel = 28f;
        public float brakeForce = 40f;
        public float reverseAccel = 16f;
        public float maxSpeedKmh = 120f;
        public float maxSteerAngle = 42f;
        public float steerSpeed = 8f;
        public float highSpeedSteerFactor = 0.5f;
        public float turnResponsiveness = 2.1f;
        public float maxStepUp = 0.35f;      // curbs/sidewalks the car may climb
        public float groundProbeDown = 3f;
        public float rideHeight = 0f;        // root height above road (0 = root on road surface)
        public float groundStick = 25f;
        public LayerMask groundMask = ~0;

        [Header("Center of Mass")]
        public Vector3 centerOfMassOffset = new Vector3(0f, -0.2f, 0.05f);

        Rigidbody _rb;
        float _throttle;
        float _steer;
        float _steerAngle;
        bool _handbrake;
        bool _reverseIntent;
        bool _grounded;
        float _lastGroundY = VehicleGround.DefaultRoadY;

        public float SpeedKmh => _rb != null ? UnityCompat.GetVelocity(_rb).magnitude * 3.6f : 0f;
        public bool IsReverse => _reverseIntent && SpeedKmh < 8f;
        public float Throttle => _throttle;
        public bool Handbrake => _handbrake;
        public int displayGear;              // set by the engine sound's gearbox simulation
        public string GearLabel => IsReverse ? "R" : displayGear > 0 ? displayGear.ToString() : "D";

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.mass = 1500f;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            _rb.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX;
            UnityCompat.SetDamping(_rb, 0.15f, 2.5f);
            _rb.centerOfMass = centerOfMassOffset;

            // WheelColliders were launching the car — make sure none are active
            foreach (var wc in GetComponentsInChildren<WheelCollider>())
                wc.enabled = false;
        }

        public void SetInput(float throttle, float steer, bool handbrake)
        {
            _throttle = Mathf.Clamp(throttle, -1f, 1f);
            _steer = Mathf.Clamp(steer, -1f, 1f);
            _handbrake = handbrake;
        }

        void FixedUpdate()
        {
            StickToGround();

            float speed = SpeedKmh;
            float steerFactor = Mathf.Lerp(1f, highSpeedSteerFactor, Mathf.InverseLerp(0f, maxSpeedKmh, speed));
            float targetSteer = _steer * maxSteerAngle * steerFactor;
            _steerAngle = Mathf.MoveTowards(_steerAngle, targetSteer, steerSpeed * maxSteerAngle * Time.fixedDeltaTime);

            if (_grounded)
            {
                // No turning while stopped — only steer when moving
                if (speed >= 2.5f)
                {
                    float turnMul = Mathf.Lerp(1.25f, 0.8f, Mathf.InverseLerp(0f, 80f, speed));
                    float yaw = _steerAngle * turnResponsiveness * turnMul * Time.fixedDeltaTime;
                    transform.Rotate(0f, yaw, 0f, Space.World);
                }

                Vector3 vel = UnityCompat.GetVelocity(_rb);
                Vector3 flatVel = Vector3.ProjectOnPlane(vel, Vector3.up);
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

                if (_throttle < -0.1f && speed < 5f)
                    _reverseIntent = true;
                else if (_throttle > 0.1f)
                    _reverseIntent = false;

                if (_handbrake)
                {
                    flatVel *= 0.88f;
                }
                else if (_reverseIntent)
                {
                    flatVel += forward * (_throttle * reverseAccel * Time.fixedDeltaTime);
                }
                else if (_throttle >= 0f)
                {
                    if (speed < maxSpeedKmh)
                        flatVel += forward * (_throttle * accel * Time.fixedDeltaTime);
                }
                else
                {
                    flatVel = Vector3.MoveTowards(flatVel, Vector3.zero, brakeForce * Time.fixedDeltaTime);
                }

                // Snap velocity into turn so corner "cuts" instead of sliding straight
                if (flatVel.magnitude > 0.4f)
                {
                    Vector3 desiredDir = _reverseIntent ? -forward : forward;
                    float align = _handbrake ? 4f : 14f;
                    flatVel = Vector3.Lerp(flatVel, desiredDir * flatVel.magnitude, align * Time.fixedDeltaTime);
                }

                float maxMs = maxSpeedKmh / 3.6f;
                if (flatVel.magnitude > maxMs)
                    flatVel = flatVel.normalized * maxMs;

                UnityCompat.SetVelocity(_rb, new Vector3(flatVel.x, UnityCompat.GetVelocity(_rb).y, flatVel.z));
            }

            // Hard safety: never allow upward launch
            var v = UnityCompat.GetVelocity(_rb);
            if (v.y > 0.5f) v.y = 0f;
            if (!_grounded && transform.position.y > _lastGroundY + rideHeight + 2f)
            {
                // Somehow airborne (pushed by a collision) — put it back on the road
                var p = _rb.position;
                p.y = _lastGroundY + rideHeight;
                _rb.position = p;
                v.y = 0f;
            }
            UnityCompat.SetVelocity(_rb, v);
            _rb.angularVelocity = new Vector3(0f, _rb.angularVelocity.y, 0f);
        }

        void StickToGround()
        {
            Vector3 basePos = _rb.position - Vector3.up * rideHeight;
            if (VehicleGround.TryGetHeight(basePos, transform, maxStepUp, groundProbeDown, out float groundY, groundMask))
            {
                _grounded = true;
                _lastGroundY = groundY;
                float targetY = groundY + rideHeight;
                Vector3 pos = _rb.position;
                // Snap down instantly (never hover), ease up over curbs
                pos.y = pos.y > targetY ? targetY : Mathf.Lerp(pos.y, targetY, Mathf.Clamp01(groundStick * Time.fixedDeltaTime));
                _rb.position = pos;

                var v = UnityCompat.GetVelocity(_rb);
                v.y = 0f;
                UnityCompat.SetVelocity(_rb, v);
                _rb.useGravity = false;

                Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
                if (fwd.sqrMagnitude > 0.001f)
                {
                    Quaternion upright = Quaternion.LookRotation(fwd.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, upright, 10f * Time.fixedDeltaTime);
                }
            }
            else
            {
                _grounded = false;
                _rb.useGravity = true;
            }
        }

        void LateUpdate()
        {
            // Roll visual wheels at the real rate for their radius
            float speedMs = SpeedKmh / 3.6f * (_reverseIntent ? -1f : 1f);
            float spin = speedMs / Mathf.Max(0.05f, wheelRadius) * Mathf.Rad2Deg * Time.deltaTime;
            SpinVisual(frontLeft, spin, _steerAngle);
            SpinVisual(frontRight, spin, _steerAngle);
            SpinVisual(rearLeft, spin, 0f);
            SpinVisual(rearRight, spin, 0f);
        }

        void SpinVisual(Transform pose, float spinDeg, float steer)
        {
            if (pose == null) return;
            // Steer on pose Y only
            pose.localRotation = Quaternion.Euler(0f, steer, 0f);
            // Roll around axle (local X) on Spin child — keeps tire circular
            var spin = WheelMeshFactory.GetSpin(pose);
            if (spin != null)
                spin.Rotate(spinDeg, 0f, 0f, Space.Self);
        }
    }
}
