using UnityEngine;
using FortunerCity.AudioSys;
using FortunerCity.Traffic;
using FortunerCity.Vehicle;

namespace FortunerCity.Weapons
{
    /// <summary>
    /// Bullet damage for any car or bike: grey smoke from the engine when hurt; when health runs out it is
    /// wrecked — dark smoke, a small engine fire, burnt paint, and it can no longer be driven.
    /// </summary>
    public class CarDamage : MonoBehaviour
    {
        public float health = 100f;
        public bool Wrecked { get; private set; }
        ParticleSystem _smoke, _fire;

        public static CarDamage On(GameObject car)
        {
            var d = car.GetComponent<CarDamage>();
            return d != null ? d : car.AddComponent<CarDamage>();
        }

        public void Hit(float damage)
        {
            if (Wrecked) return;
            health -= damage;
            if (health < 55f && _smoke == null)
                _smoke = Effects.Smoke(transform, EnginePoint(), new Color(0.75f, 0.75f, 0.75f, 0.45f), 10f);
            if (health <= 0f) Wreck();
        }

        Vector3 EnginePoint()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return new Vector3(0f, 1f, 1.5f);
            var info = GetComponent<VehicleInfo>();
            float z = info != null && info.isBike ? 0.1f : box.center.z + box.size.z * 0.32f;
            return new Vector3(0f, box.center.y + box.size.y * 0.3f, z);
        }

        void Wreck()
        {
            Wrecked = true;
            if (_smoke != null) Destroy(_smoke.gameObject);
            _smoke = Effects.Smoke(transform, EnginePoint(), new Color(0.12f, 0.12f, 0.12f, 0.6f), 18f);
            _fire = Effects.Fire(transform, EnginePoint());

            // AI traffic gives up (removing the component lets the cars behind drive round it)
            var traffic = GetComponent<TrafficCar>();
            if (traffic != null) { traffic.enabled = false; Destroy(traffic); }
            TrafficAudio.Remove(gameObject);

            var drive = GetComponent<PlayerDriveController>();
            if (drive != null) drive.maxSpeedKmh = 0f;
            var audio = GetComponent<VehicleAudio>();
            if (audio != null) audio.SetRunning(false);

            // burnt paint
            foreach (var r in GetComponentsInChildren<MeshRenderer>())
            {
                var mats = r.materials;
                for (int i = 0; i < mats.Length; i++) mats[i].color = mats[i].color * 0.25f;
                r.materials = mats;
            }

            var src = gameObject.AddComponent<AudioSource>();
            src.spatialBlend = 1f;
            src.maxDistance = 80f;
            src.PlayOneShot(SoundSynth.GunSound(SoundSynth.Gun.Shotgun), 1f);
        }
    }
}
