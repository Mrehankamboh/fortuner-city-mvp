using UnityEngine;

namespace FortunerCity.CameraSys
{
    /// <summary>
    /// Chase camera for cars/bikes (C toggles the driver's-eye view), a follow camera on foot, and an
    /// over-the-shoulder aim camera while a gun is out (mouse pitch in aimPitch, crosshair = screen centre).
    /// </summary>
    public class ChaseCamera : MonoBehaviour
    {
        public Transform target;
        // Tuned for the real-size Fortuner (4.8 m long, 1.84 m tall)
        public Vector3 chaseOffset = new Vector3(0f, 2.7f, -7.2f);
        public Vector3 hoodOffset = new Vector3(0f, 1.42f, 0.35f);   // driver eye height, hood visible ahead
        public Vector3 footOffset = new Vector3(0f, 2.1f, -4.3f);    // follow cam when walking
        public Vector3 aimOffset = new Vector3(0.6f, 0.15f, -2.4f);  // from the head pivot, over the right shoulder
        public float followSmooth = 8f;
        public float lookSmooth = 10f;
        public float lookAhead = 6f;
        public bool onFoot;
        public bool armed;
        public float aimPitch;                                       // degrees, + = up

        bool _hood;
        Camera _cam;

        /// <summary>Driver's-eye view inside the vehicle (engine sound is muffled by the cabin).</summary>
        public bool HoodView => _hood && !onFoot;

        public void SetTarget(Transform t) => target = t;

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.C) && !onFoot)
                _hood = !_hood;

            // widen the view a little while sprinting so the speed is felt
            if (_cam == null) _cam = GetComponent<Camera>();
            if (_cam != null)
            {
                float fov = onFoot && !armed && FortunerCity.PlayerSys.PlayerWalker.Sprinting ? 73f : 65f;
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, fov, 1f - Mathf.Exp(-5f * dt));
            }

            if (onFoot && armed)
            {
                var rot = Quaternion.Euler(-aimPitch, target.eulerAngles.y, 0f);
                Vector3 pivot = target.position + Vector3.up * 1.6f;
                Vector3 desired = pivot + rot * aimOffset;
                transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSmooth * 2f * dt));
                transform.rotation = rot;
            }
            else if (onFoot)
            {
                Vector3 desired = target.TransformPoint(footOffset);
                Vector3 lookAt = target.position + target.forward * 2f + Vector3.up * 1.4f;
                transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSmooth * dt));
                var rot = Quaternion.LookRotation((lookAt - transform.position).normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, 1f - Mathf.Exp(-lookSmooth * dt));
            }
            else if (_hood)
            {
                Vector3 desired = target.TransformPoint(hoodOffset);
                Vector3 lookAt = target.position + target.forward * 12f + Vector3.up * 1.2f;
                transform.position = desired;
                transform.rotation = Quaternion.LookRotation((lookAt - transform.position).normalized, Vector3.up);
            }
            else
            {
                Vector3 desired = target.TransformPoint(chaseOffset);
                Vector3 lookAt = target.position + target.forward * lookAhead + Vector3.up * 1.2f;
                transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSmooth * dt));
                var rot = Quaternion.LookRotation((lookAt - transform.position).normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, 1f - Mathf.Exp(-lookSmooth * dt));
            }
        }
    }
}
