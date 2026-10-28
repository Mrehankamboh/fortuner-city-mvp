using UnityEngine;
using FortunerCity.AudioSys;
using FortunerCity.CameraSys;
using FortunerCity.Core;
using FortunerCity.Traffic;
using FortunerCity.UI;
using FortunerCity.Vehicle;

namespace FortunerCity.PlayerSys
{
    /// <summary>
    /// F key: get out of the car (when slow), walk around, and get into ANY car — your own parked car or
    /// any traffic car, which then becomes drivable. Cars you leave stay parked where you left them.
    /// </summary>
    public class PlayerSwitcher : MonoBehaviour
    {
        public static Transform ActiveCar;        // car the player is driving (null on foot)
        public static float ActiveCarSpeed;       // m/s

        const float MaxExitSpeedKmh = 12f;
        const float EnterRange = 3f;

        PlayerDriveController _drive;
        GameInputBridge _bridge;
        ChaseCamera _chase;
        GameHud _hud;
        PlayerWalker _walker;

        public void Init(PlayerDriveController drive, GameInputBridge bridge, ChaseCamera chase, GameHud hud, PlayerWalker walker)
        {
            _drive = drive;
            _bridge = bridge;
            _chase = chase;
            _hud = hud;
            _walker = walker;
        }

        void Update()
        {
            ActiveCar = _drive != null ? _drive.transform : null;
            ActiveCarSpeed = _drive != null ? _drive.SpeedKmh / 3.6f : 0f;
            if (Time.timeScale <= 0f) return;

            bool pressed = Input.GetKeyDown(KeyCode.F);
            if (_drive != null)
            {
                bool slow = _drive.SpeedKmh < MaxExitSpeedKmh;
                _hud.SetContextHint(slow ? "F  Get out of the car" : "Slow down to get out");
                if (pressed && slow) ExitCar();
            }
            else
            {
                var near = FindNearestCar();
                _hud.SetContextHint(near != null ? "F  Get in this car" : "");
                if (pressed && near != null) EnterCar(near);
            }
        }

        void ExitCar()
        {
            var car = _drive;
            car.SetInput(0f, 0f, true);
            Park(car.gameObject);
            car.enabled = false;
            var audio = car.GetComponent<VehicleAudio>();
            if (audio != null) audio.SetRunning(false);
            var info = car.GetComponent<VehicleInfo>();
            if (info != null && info.rider != null) { Destroy(info.rider); info.rider = null; }   // step off the bike

            // Right-hand-drive car (left-hand traffic): driver's door is on the right
            var box = car.GetComponent<BoxCollider>();
            float half = box != null ? box.size.x * 0.5f : 1f;
            Vector3 door = car.transform.TransformPoint(new Vector3(half + 0.7f, 0f, 0.3f));
            Vector3 face = car.transform.right;
            if (Blocked(door))
            {
                door = car.transform.TransformPoint(new Vector3(-(half + 0.7f), 0f, 0.3f));
                face = -car.transform.right;
            }
            door.y = VehicleGround.SpawnHeight(door, car.transform) + 0.05f;

            _walker.gameObject.SetActive(true);
            _walker.Teleport(door, Quaternion.LookRotation(face, Vector3.up));

            _drive = null;
            _bridge.drive = null;
            _chase.SetTarget(_walker.transform);
            _chase.onFoot = true;
            _hud.SetDrive(null);
        }

        void EnterCar(GameObject car)
        {
            var drive = car.GetComponent<PlayerDriveController>();
            var traffic = car.GetComponent<TrafficCar>();
            if (traffic != null) drive = TakeOverTrafficCar(car, traffic);
            if (drive == null) return;

            var damage = car.GetComponent<FortunerCity.Weapons.CarDamage>();
            if (damage != null && damage.Wrecked) return;            // burnt-out wrecks can't be driven

            var rb = car.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            drive.enabled = true;
            car.tag = "PlayerCar";
            var info = car.GetComponent<VehicleInfo>();
            if (info != null)
            {
                info.ApplyTo(drive);
                if (info.isBike)
                {
                    // the player sits on the bike (replaces any AI rider)
                    if (info.rider != null) Destroy(info.rider);
                    info.rider = FortunerCity.People.PersonBuilder.BuildRider(info.seat, PlayerWalker.DriverLook);
                }
            }
            TrafficAudio.Remove(car);
            var audio = car.GetComponent<VehicleAudio>();
            if (audio == null) VehicleAudio.Attach(car, drive);
            else audio.SetRunning(true);

            _walker.gameObject.SetActive(false);
            _drive = drive;
            _bridge.drive = drive;
            _chase.SetTarget(car.transform);
            _chase.onFoot = false;
            _hud.SetDrive(drive);
        }

        /// <summary>Turns an AI traffic car into a player-drivable car (same body, wheels and collider).</summary>
        static PlayerDriveController TakeOverTrafficCar(GameObject car, TrafficCar traffic)
        {
            var wheels = traffic.wheels;
            float radius = traffic.wheelRadius;
            traffic.enabled = false;
            Destroy(traffic);

            var rb = car.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            var drive = car.AddComponent<PlayerDriveController>();
            if (wheels != null && wheels.Count >= 4)
            {
                drive.frontLeft = wheels[0];
                drive.frontRight = wheels[1];
                drive.rearLeft = wheels[2];
                drive.rearRight = wheels[3];
            }
            else if (wheels != null && wheels.Count == 2)
            {
                drive.frontLeft = wheels[0];   // bike: front, rear
                drive.rearLeft = wheels[1];
            }
            drive.wheelRadius = radius;
            drive.rideHeight = 0f;
            drive.centerOfMassOffset = new Vector3(0f, 0.55f, 0.1f);
            rb.centerOfMass = drive.centerOfMassOffset;
            return drive;
        }

        /// <summary>Parked cars become kinematic so they stay exactly where they were left.</summary>
        static void Park(GameObject car)
        {
            var rb = car.GetComponent<Rigidbody>();
            if (rb == null) return;
            UnityCompat.SetVelocity(rb, Vector3.zero);
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        GameObject FindNearestCar()
        {
            Vector3 p = _walker.transform.position + Vector3.up * 0.9f;
            GameObject best = null;
            float bestDist = EnterRange;
            foreach (var col in Physics.OverlapSphere(p, EnterRange, ~0, QueryTriggerInteraction.Ignore))
            {
                var rb = col.attachedRigidbody;
                if (rb == null) continue;
                var go = rb.gameObject;
                if (go.GetComponent<PlayerDriveController>() == null && go.GetComponent<TrafficCar>() == null) continue;
                float d = Vector3.Distance(p, col.ClosestPoint(p));
                if (d < bestDist) { bestDist = d; best = go; }
            }
            return best;
        }

        static bool Blocked(Vector3 spot)
        {
            return Physics.CheckCapsule(spot + Vector3.up * 0.6f, spot + Vector3.up * 1.5f, 0.3f, ~0, QueryTriggerInteraction.Ignore);
        }
    }
}
