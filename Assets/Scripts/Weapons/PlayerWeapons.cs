using System.Collections.Generic;
using UnityEngine;
using FortunerCity.AudioSys;
using FortunerCity.CameraSys;
using FortunerCity.People;
using FortunerCity.Traffic;
using FortunerCity.UI;
using FortunerCity.Vehicle;

namespace FortunerCity.Weapons
{
    /// <summary>
    /// On-foot weapons: 1 pistol, 2 rifle, 3 shotgun, Q holster. While armed the mouse aims
    /// (over-the-shoulder camera + crosshair), left click fires, R reloads. Hit-scan from the crosshair;
    /// tracers from the muzzle. Shots damage cars, knock people down and make everyone nearby flee.
    /// </summary>
    public class PlayerWeapons : MonoBehaviour
    {
        public float mouseSensitivity = 2.2f;

        PersonRig _rig;
        ChaseCamera _chase;
        GameHud _hud;
        FortunerCity.PlayerSys.PlayerWalker _walker;
        int _current = -1;
        readonly int[] _mag = new int[3], _reserve = new int[3];
        GameObject _model;
        Transform _muzzle;
        Light _flash;
        float _nextFire, _reloadEnd;
        bool _reloading;
        AudioSource _audio;

        public bool Armed => _current >= 0;
        WeaponDef Def => WeaponDefs.All[_current];

        public void Init(PersonRig rig, ChaseCamera chase, GameHud hud)
        {
            _rig = rig;
            _chase = chase;
            _hud = hud;
            _walker = GetComponent<FortunerCity.PlayerSys.PlayerWalker>();
            for (int i = 0; i < 3; i++)
            {
                _mag[i] = WeaponDefs.All[i].magSize;
                _reserve[i] = WeaponDefs.All[i].startReserve;
            }
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 0.2f;
            _audio.playOnAwake = false;
        }

        void OnDisable()
        {
            // getting into a vehicle puts the gun away
            if (_rig != null) Holster();
        }

        void Update()
        {
            if (_rig == null) return;
            if (Time.timeScale <= 0f)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) Equip(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Equip(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Equip(2);
            if (Input.GetKeyDown(KeyCode.Q)) Holster();
            if (!Armed) return;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // mouse aim: yaw turns the body, pitch tilts the camera and arms
            transform.Rotate(0f, Input.GetAxis("Mouse X") * mouseSensitivity, 0f);
            _chase.aimPitch = Mathf.Clamp(_chase.aimPitch + Input.GetAxis("Mouse Y") * mouseSensitivity, -35f, 55f);
            _rig.aimPitch = _chase.aimPitch;

            if (_reloading && Time.time >= _reloadEnd) FinishReload();
            if (Input.GetKeyDown(KeyCode.R)) StartReload();

            bool trigger = Def.automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
            if (trigger && !_reloading && Time.time >= _nextFire)
            {
                if (_mag[_current] > 0) Fire();
                else
                {
                    _audio.PlayOneShot(SoundSynth.GunSound(SoundSynth.Gun.Click), 0.8f);
                    _nextFire = Time.time + 0.3f;
                    StartReload();
                }
            }
            _hud.SetWeapon(Def.name, _mag[_current], _reserve[_current], _reloading);
        }

        void Equip(int index)
        {
            if (_current == index) return;
            RemoveModel();
            _current = index;
            _reloading = false;
            var def = Def;

            _model = new GameObject(def.name);
            _model.transform.SetParent(_rig.rightForearm, false);
            _model.transform.localPosition = new Vector3(0f, -0.31f, 0f);
            _model.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);    // barrel along the forearm
            _model.AddComponent<MeshFilter>().sharedMesh = def.model();
            _model.AddComponent<MeshRenderer>().sharedMaterials = WeaponDefs.Materials();

            _muzzle = new GameObject("Muzzle").transform;
            _muzzle.SetParent(_model.transform, false);
            _muzzle.localPosition = def.muzzle;
            _flash = _muzzle.gameObject.AddComponent<Light>();
            _flash.type = LightType.Point;
            _flash.color = new Color(1f, 0.75f, 0.4f);
            _flash.range = 5f;
            _flash.intensity = 4f;
            _flash.enabled = false;
            _muzzle.gameObject.AddComponent<Effects.AutoDisable>().enabled = false;

            _rig.weaponPose = def.pose;
            if (_walker != null) _walker.armed = true;
            _chase.armed = true;
            _audio.PlayOneShot(SoundSynth.GunSound(SoundSynth.Gun.Click), 0.6f);
        }

        public void Holster()
        {
            RemoveModel();
            _current = -1;
            _reloading = false;
            if (_rig != null) { _rig.weaponPose = 0; _rig.aimPitch = 0f; }
            if (_walker != null) _walker.armed = false;
            if (_chase != null) { _chase.armed = false; _chase.aimPitch = 0f; }
            if (_hud != null) _hud.HideWeapon();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void RemoveModel()
        {
            if (_model != null) Destroy(_model);
            _model = null;
            _muzzle = null;
        }

        void StartReload()
        {
            var def = Def;
            if (_reloading || _mag[_current] >= def.magSize || _reserve[_current] <= 0) return;
            _reloading = true;
            _reloadEnd = Time.time + def.reloadTime;
            _audio.PlayOneShot(SoundSynth.GunSound(SoundSynth.Gun.Reload), 0.8f);
        }

        void FinishReload()
        {
            _reloading = false;
            int take = Mathf.Min(Def.magSize - _mag[_current], _reserve[_current]);
            _mag[_current] += take;
            _reserve[_current] -= take;
        }

        void Fire()
        {
            var def = Def;
            _mag[_current]--;
            _nextFire = Time.time + def.fireInterval;

            var cam = Camera.main;
            Ray aim = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            // ignore anything between the camera and the player (we shoot from the player, not the camera)
            float skip = Vector3.Dot(transform.position + Vector3.up * 1.5f - aim.origin, aim.direction);

            for (int p = 0; p < def.pellets; p++)
            {
                Vector3 dir = Quaternion.AngleAxis(Random.Range(-def.spread, def.spread), cam.transform.right)
                              * Quaternion.AngleAxis(Random.Range(-def.spread, def.spread), cam.transform.up) * aim.direction;
                Vector3 end = aim.origin + dir * def.range;
                var hits = Physics.RaycastAll(aim.origin, dir, def.range, ~0, QueryTriggerInteraction.Collide);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    if (hit.distance < skip || hit.collider.transform.IsChildOf(transform)) continue;
                    var ped = hit.collider.GetComponentInParent<Pedestrian>();
                    if (hit.collider.isTrigger && ped == null) continue;
                    end = hit.point;
                    Apply(hit, ped, dir, def);
                    break;
                }
                if (_muzzle != null) Effects.Tracer(_muzzle.position, end);
            }

            if (_flash != null) _muzzle.GetComponent<Effects.AutoDisable>().Arm(0.05f);
            _audio.PlayOneShot(SoundSynth.GunSound(def.sound), 1f);
            _chase.aimPitch = Mathf.Clamp(_chase.aimPitch + def.recoil, -35f, 55f);
            Pedestrian.Panic(transform.position, 45f);
        }

        static void Apply(RaycastHit hit, Pedestrian ped, Vector3 dir, WeaponDef def)
        {
            if (ped != null) { ped.Shot(dir); return; }

            var rb = hit.collider.attachedRigidbody;
            bool vehicle = rb != null && (rb.GetComponent<PlayerDriveController>() != null || rb.GetComponent<TrafficCar>() != null);
            if (vehicle)
            {
                CarDamage.On(rb.gameObject).Hit(def.damage);
                Effects.Impact(hit.point, hit.normal, true);
                Effects.BulletHole(hit.point, hit.normal, hit.collider.transform);
                return;
            }
            Effects.Impact(hit.point, hit.normal, false);
            Effects.BulletHole(hit.point, hit.normal, null);
        }
    }
}
