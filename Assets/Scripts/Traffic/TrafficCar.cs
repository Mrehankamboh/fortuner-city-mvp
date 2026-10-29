using System.Collections.Generic;
using UnityEngine;
using FortunerCity.Vehicle;

namespace FortunerCity.Traffic
{
    /// <summary>
    /// Kinematic NPC car / bike following a waypoint loop. Stops for vehicles and the walking player,
    /// obeys traffic signals, and drives round cars the player left in the lane.
    /// Root stays on the road surface (tyres touch root Y=0).
    /// </summary>
    public class TrafficCar : MonoBehaviour
    {
        public WaypointPath path;
        public float speed = 12f;
        public float turnSpeed = 3.5f;
        public float waypointReach = 3f;
        public float stopDistance = 8f;
        public LayerMask obstacleMask = ~0;

        [Header("Visual wheels (Pose → Spin)")]
        public List<Transform> wheels = new List<Transform>();
        public float wheelRadius = 0.32f;
        public float halfLength = 2.3f;   // centre → front bumper, for stop lines

        const float OvertakeOffset = 3.5f;   // one lane to the right (towards the centre in left-hand traffic)

        int _index;
        Rigidbody _rb;
        float _currentSpeed;
        float _laneShift, _laneShiftTarget, _blockedTime, _clearTime;
        Renderer[] _renderers;
        float _cullTimer;
        bool _shown = true;
        const float DrawDistance = 280f;   // cars further than this are not drawn (keeps heavy traffic cheap)

        public void Init(WaypointPath p, float spd, int startIndex)
        {
            path = p;
            speed = spd;
            _index = startIndex;
            _rb = GetComponent<Rigidbody>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.useGravity = false;
            tag = "TrafficCar";
            _renderers = GetComponentsInChildren<Renderer>(true);
            _cullTimer = Random.value * 0.25f;
        }

        void UpdateCulling(float dt)
        {
            _cullTimer -= dt;
            if (_cullTimer > 0f) return;
            _cullTimer = 0.25f;
            var cam = Camera.main;
            bool show = cam == null || (cam.transform.position - transform.position).sqrMagnitude < DrawDistance * DrawDistance;
            if (show != _shown) SetShown(show);
        }

        void SetShown(bool show)
        {
            _shown = show;
            if (_renderers == null) return;
            foreach (var r in _renderers) if (r != null) r.enabled = show;
        }

        void OnDisable()
        {
            if (!_shown) SetShown(true);   // taken over by the player or wrecked: always drawn again
        }

        void Update()
        {
            if (path == null || path.points.Count == 0) return;
            UpdateCulling(Time.deltaTime);
            float dt = Time.deltaTime;

            // Stop for vehicles and the walking player ahead — not barriers or lamp posts on corners
            float moveSpeed = speed;
            float lookAhead = Mathf.Max(stopDistance, speed * 0.6f);
            bool parkedAhead = false;
            if (Physics.Raycast(transform.position + Vector3.up * 0.8f, transform.forward, out RaycastHit hit, lookAhead, obstacleMask, QueryTriggerInteraction.Ignore)
                && !hit.collider.transform.IsChildOf(transform))
            {
                var rb = hit.collider.attachedRigidbody;
                if (rb != null || hit.collider is CharacterController)
                {
                    moveSpeed = 0f;
                    // A parked / standing car that isn't AI traffic (e.g. one the player left) can be driven around
                    parkedAhead = rb != null && rb.GetComponent<TrafficCar>() == null
                                  && FortunerCity.Core.UnityCompat.GetVelocity(rb).magnitude < 0.5f;
                }
            }
            UpdateOvertake(parkedAhead, dt);

            // Traffic signals: ease to a halt at the stop line on red (or yellow when there is room)
            if (FortunerCity.City.TrafficSignals.MustStop(transform.position, transform.forward, halfLength, out float toLine))
                moveSpeed = Mathf.Min(moveSpeed, Mathf.Max(0f, toLine - 0.5f) * 1.5f);
            _currentSpeed = moveSpeed;

            Vector3 target = path.GetPoint(_index);
            if (Mathf.Abs(_laneShift) > 0.01f)
            {
                // keep following the loop, just one lane over
                int prev = (_index - 1 + path.points.Count) % path.points.Count;
                Vector3 segDir = target - path.GetPoint(prev);
                segDir.y = 0f;
                if (segDir.sqrMagnitude > 0.01f)
                    target += Vector3.Cross(Vector3.up, segDir.normalized) * _laneShift;
            }
            Vector3 flatTarget = new Vector3(target.x, transform.position.y, target.z);
            Vector3 to = flatTarget - transform.position;
            float dist = to.magnitude;

            if (dist < waypointReach)
                _index = path.NextIndex(_index);

            if (to.sqrMagnitude > 0.01f)
            {
                Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * dt);
            }

            Vector3 next = transform.position + transform.forward * (moveSpeed * dt);
            // Follow the actual road surface instead of a fixed height
            if (VehicleGround.TryGetHeight(next, transform, 0.35f, 2f, out float groundY))
                next.y = groundY;
            transform.position = next;

            SpinWheels();
        }

        /// <summary>
        /// After waiting 1.5 s behind a parked car, slide one lane over (only if that lane is clear ahead),
        /// pass it, then slide back once the original lane has been clear for a few seconds.
        /// </summary>
        void UpdateOvertake(bool parkedAhead, float dt)
        {
            _blockedTime = parkedAhead ? _blockedTime + dt : 0f;
            if (_laneShiftTarget == 0f && _blockedTime > 1.5f)
            {
                Vector3 right = Vector3.Cross(Vector3.up, transform.forward).normalized;
                Vector3 from = transform.position + right * OvertakeOffset + Vector3.up * 0.8f;
                bool laneBusy = Physics.Raycast(from, transform.forward, out RaycastHit h, 25f, obstacleMask, QueryTriggerInteraction.Ignore)
                                && h.collider.attachedRigidbody != null;
                if (!laneBusy) _laneShiftTarget = OvertakeOffset;
            }

            if (_laneShiftTarget != 0f)
            {
                _clearTime = parkedAhead ? 0f : _clearTime + dt;
                if (_clearTime > 3f) { _laneShiftTarget = 0f; _clearTime = 0f; }
            }

            // Slide sideways directly so a stopped car can still change lane
            float newShift = Mathf.MoveTowards(_laneShift, _laneShiftTarget, 2.5f * dt);
            if (!Mathf.Approximately(newShift, _laneShift))
            {
                Vector3 right = Vector3.Cross(Vector3.up, transform.forward).normalized;
                transform.position += right * (newShift - _laneShift);
                _laneShift = newShift;
            }
        }

        void SpinWheels()
        {
            float deg = _currentSpeed / Mathf.Max(0.05f, wheelRadius) * Mathf.Rad2Deg * Time.deltaTime;
            foreach (var pose in wheels)
            {
                var spin = WheelMeshFactory.GetSpin(pose);
                if (spin != null) spin.Rotate(deg, 0f, 0f, Space.Self);
            }
        }
    }
}
