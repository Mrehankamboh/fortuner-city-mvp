using UnityEngine;

namespace FortunerCity.Vehicle
{
    /// <summary>
    /// Player car: real-size procedural Fortuner-style SUV (CarCatalog.Fortuner) + arcade drive.
    /// </summary>
    public static class FortunerBuilder
    {
        public static Color PlayerPaint = new Color(0.93f, 0.93f, 0.91f);   // pearl white

        public static GameObject Build(Vector3 position, Quaternion rotation)
        {
            // Build at the origin, then place: root sits ON the road, tyres touch root Y=0
            var root = new GameObject("Player_Fortuner");
            var model = ProceduralCar.Build(root.transform, CarCatalog.Fortuner, PlayerPaint);
            root.tag = "PlayerCar";

            var spawn = new Vector3(position.x, VehicleGround.SpawnHeight(position), position.z);
            root.transform.SetPositionAndRotation(spawn, rotation);

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 2100f;   // ~kerb weight of a diesel Fortuner
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var drive = root.AddComponent<PlayerDriveController>();
            drive.frontLeft = model.frontLeft;
            drive.frontRight = model.frontRight;
            drive.rearLeft = model.rearLeft;
            drive.rearRight = model.rearRight;
            drive.wheelRadius = model.wheelRadius;
            drive.rideHeight = 0f;
            drive.centerOfMassOffset = new Vector3(0f, 0.6f, 0.1f);
            rb.centerOfMass = drive.centerOfMassOffset;
            root.GetComponent<VehicleInfo>().ApplyTo(drive);

            // Body-only collider kept above kerbs so it never lifts the car off the road
            KenneyCarRig.AddBodyCollider(root, model.bodyBounds, 0.3f);
            return root;
        }

        /// <summary>A parked, enterable bike (kinematic until someone rides it).</summary>
        public static GameObject BuildParkedBike(BikeSpec spec, Color paint, Vector3 position, Quaternion rotation)
        {
            var root = new GameObject("Parked_" + spec.name);
            var model = BikeBuilder.Build(root.transform, spec, paint);
            var spawn = new Vector3(position.x, VehicleGround.SpawnHeight(position), position.z);
            root.transform.SetPositionAndRotation(spawn, rotation);
            KenneyCarRig.AddBodyCollider(root, model.bodyBounds, 0.25f);

            var rb = root.AddComponent<Rigidbody>();
            var drive = root.AddComponent<PlayerDriveController>();
            drive.frontLeft = model.frontLeft;
            drive.rearLeft = model.rearLeft;
            drive.wheelRadius = model.wheelRadius;
            drive.rideHeight = 0f;
            drive.centerOfMassOffset = new Vector3(0f, 0.5f, 0f);
            root.GetComponent<VehicleInfo>().ApplyTo(drive);
            drive.enabled = false;
            rb.isKinematic = true;
            return root;
        }
    }
}
