using System.Collections.Generic;
using UnityEngine;
using FortunerCity.City;
using FortunerCity.PlayerSys;

namespace FortunerCity.People
{
    /// <summary>
    /// City pedestrian. Walks round its block on the sidewalk; at corners it may wait at the zebra
    /// crossing until cars on that road have a red light, then cross to the next block. Sometimes stops
    /// to look at shop windows, keeps a little personal space, and jumps aside from the player's car.
    /// Park visitors use a simple loop. No collider: people never block traffic physics.
    /// </summary>
    public class Pedestrian : MonoBehaviour
    {
        enum State { Walk, Pause, WaitToCross, Cross, Flee, Down }

        const float Inset = 2f;            // middle of the 4 m sidewalk = zebra crossing line
        const float CrossSpeed = 1.6f;
        static readonly List<Pedestrian> All = new List<Pedestrian>();

        // city mode: block (bx, bz), corner 0=SW 1=SE 2=NE 3=NW, walking direction +1 / -1
        int _bx, _bz, _corner, _dir;
        bool _cityMode;
        List<Vector3> _loop;               // park mode
        int _loopTarget;

        State _state;
        Vector3 _target;
        float _timer, _walkSpeed, _curSpeed;
        public float speedFactor = 1f;
        int _crossI, _crossJ, _nextBx, _nextBz, _nextCorner;
        bool _crossNs;
        Vector3 _waitFacing;
        PersonAnimator _anim;
        System.Random _rng;
        Vector3 _fallDir;
        float _fleeTimer, _downTimer;


        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>Gunfire: everyone within radius runs away from the sound for a few seconds.</summary>
        public static void Panic(Vector3 source, float radius)
        {
            foreach (var p in All)
            {
                if (p._state == State.Down) continue;
                Vector3 away = p.transform.position - source;
                away.y = 0f;
                if (away.magnitude > radius) continue;
                if (p._state == State.Cross) continue;              // already hurrying across the road
                // run along the sidewalk, in whichever direction leads away from the shot
                if (p._cityMode && Vector3.Dot(away, p._target - p.transform.position) < 0f)
                {
                    p._dir = -p._dir;
                    p._corner = (p._corner + p._dir + 4) % 4;
                    p._target = CornerPos(p._bx, p._bz, p._corner);
                }
                p._fleeTimer = 6f + (float)p._rng.NextDouble() * 4f;
                p._state = State.Flee;
            }
        }

        /// <summary>Hit by a bullet: falls over (no gore) and is removed after a while.</summary>
        public void Shot(Vector3 bulletDir)
        {
            if (_state == State.Down) return;
            _state = State.Down;
            _downTimer = 0f;
            bulletDir.y = 0f;
            _fallDir = bulletDir.sqrMagnitude > 0.01f ? bulletDir.normalized : -transform.forward;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            if (_anim != null) _anim.speed = 0f;
        }

        public void InitCity(int bx, int bz, int corner, int dir, float speed, int seed)
        {
            _cityMode = true;
            _bx = bx; _bz = bz; _corner = corner; _dir = dir;
            _walkSpeed = speed * speedFactor;
            _rng = new System.Random(seed);
            _anim = GetComponent<PersonAnimator>();
            _state = State.Walk;
            _target = CornerPos(_bx, _bz, _corner);
        }

        public void InitLoop(List<Vector3> loop, int target, float speed, int seed)
        {
            _cityMode = false;
            _loop = loop;
            _loopTarget = target % loop.Count;
            _walkSpeed = speed * speedFactor;
            _rng = new System.Random(seed);
            _anim = GetComponent<PersonAnimator>();
            _state = State.Walk;
            _target = loop[_loopTarget];
        }

        public static Vector3 CornerPos(int bx, int bz, int corner)
        {
            float x0 = CityBuilder.Line(bx) + CityBuilder.Width(bx) * 0.5f + Inset;
            float x1 = CityBuilder.Line(bx + 1) - CityBuilder.Width(bx + 1) * 0.5f - Inset;
            float z0 = CityBuilder.Line(bz) + CityBuilder.Width(bz) * 0.5f + Inset;
            float z1 = CityBuilder.Line(bz + 1) - CityBuilder.Width(bz + 1) * 0.5f - Inset;
            float y = CityBuilder.PadTop;
            switch (corner)
            {
                case 0: return new Vector3(x0, y, z0);
                case 1: return new Vector3(x1, y, z0);
                case 2: return new Vector3(x1, y, z1);
                default: return new Vector3(x0, y, z1);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 pos = transform.position;
            Vector3 move = Vector3.zero;
            Vector3 face = transform.forward;

            if (_state == State.Down)
            {
                // tip over backwards along the bullet's direction, lie still, then disappear
                _downTimer += dt;
                float tip = Mathf.Clamp01(_downTimer / 0.45f);
                transform.rotation = Quaternion.LookRotation(-_fallDir, Vector3.up) * Quaternion.Euler(-90f * tip, 0f, 0f);
                if (_downTimer > 25f) Destroy(gameObject);
                return;
            }

            switch (_state)
            {
                case State.Flee:
                {
                    _fleeTimer -= dt;
                    Vector3 to = _target - pos;
                    to.y = 0f;
                    if (to.magnitude < 0.35f) Arrive();
                    else { move = to.normalized * 5f; face = to.normalized; }
                    if (_fleeTimer <= 0f) _state = State.Walk;
                    break;
                }
                case State.Walk:
                case State.Cross:
                {
                    Vector3 to = _target - pos;
                    to.y = 0f;
                    if (to.magnitude < 0.35f) { Arrive(); break; }
                    float spd = _state == State.Cross ? CrossSpeed : _walkSpeed;
                    move = to.normalized * spd;
                    face = to.normalized;
                    break;
                }
                case State.Pause:
                    _timer -= dt;
                    face = _waitFacing;
                    if (_timer <= 0f) _state = State.Walk;
                    break;
                case State.WaitToCross:
                {
                    face = _waitFacing;
                    float width = _crossNs ? CityBuilder.Width(_crossI) : CityBuilder.Width(_crossJ);
                    float needed = (width + Inset * 2f) / CrossSpeed + 1.5f;
                    if (TrafficSignals.PedestrianCanCross(_crossI, _crossJ, _crossNs, needed))
                    {
                        _state = State.Cross;
                        _target = CornerPos(_nextBx, _nextBz, _nextCorner);
                    }
                    break;
                }
            }

            // ease into and out of walking instead of starting / stopping instantly
            float desired = move.magnitude;
            _curSpeed = Mathf.MoveTowards(_curSpeed, desired, (desired > _curSpeed ? 2.5f : 4f) * dt);
            move = desired > 0.001f ? move / desired * _curSpeed : transform.forward * _curSpeed;

            // personal space: drift away from people who are too close (not while crossing)
            if (_state != State.Cross)
            {
                foreach (var other in All)
                {
                    if (other == this) continue;
                    Vector3 d = pos - other.transform.position;
                    d.y = 0f;
                    float m = d.magnitude;
                    if (m > 0.001f && m < 0.75f) move += d / m * (0.75f - m) * 2.5f;
                }
            }

            // jump aside when the player's car comes at us
            var car = PlayerSwitcher.ActiveCar;
            if (car != null && PlayerSwitcher.ActiveCarSpeed > 2f)
            {
                Vector3 rel = pos - car.position;
                rel.y = 0f;
                float ahead = Vector3.Dot(rel, car.forward);
                float side = Vector3.Dot(rel, car.right);
                if (ahead > -2.5f && ahead < 10f && Mathf.Abs(side) < 2.6f)
                {
                    move = car.right * (side >= 0f ? 1f : -1f) * 4.5f;
                    face = move.normalized;
                }
            }

            transform.position = pos + move * dt;
            if (face.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face, Vector3.up), 6f * dt);
            if (_anim != null) _anim.speed = move.magnitude;
        }

        void Arrive()
        {
            if (!_cityMode)
            {
                _loopTarget = (_loopTarget + 1) % _loop.Count;
                _target = _loop[_loopTarget];
                if (_rng.NextDouble() < 0.2) StartPause(1.5f, 4f, -transform.forward);
                return;
            }

            if (_state == State.Flee)
            {
                // keep running round the block, no stopping or crossing
                _corner = (_corner + _dir + 4) % 4;
                _target = CornerPos(_bx, _bz, _corner);
                return;
            }

            if (_state == State.Cross)
            {
                // reached the far side: carry on round the new block
                _bx = _nextBx; _bz = _nextBz; _corner = _nextCorner;
                _state = State.Walk;
            }
            else if (_rng.NextDouble() < 0.4 && TryPlanCrossing())
            {
                _state = State.WaitToCross;
                return;
            }

            _corner = (_corner + _dir + 4) % 4;
            _target = CornerPos(_bx, _bz, _corner);

            // sometimes stop on the way to look at a shop window (face the building)
            if (_rng.NextDouble() < 0.15)
            {
                Vector3 center = (CornerPos(_bx, _bz, 0) + CornerPos(_bx, _bz, 2)) * 0.5f;
                StartPause(2f, 6f, (center - transform.position).normalized);
            }
        }

        void StartPause(float min, float max, Vector3 facing)
        {
            _state = State.Pause;
            _timer = min + (float)_rng.NextDouble() * (max - min);
            facing.y = 0f;
            _waitFacing = facing.sqrMagnitude > 0.01f ? facing.normalized : transform.forward;
        }

        /// <summary>Pick one of the two zebra crossings at this corner (only at signalled junctions).</summary>
        bool TryPlanCrossing()
        {
            int grid = CityBuilder.Grid;
            // junction next to each corner
            int ji = (_corner == 1 || _corner == 2) ? _bx + 1 : _bx;
            int jj = (_corner == 2 || _corner == 3) ? _bz + 1 : _bz;
            if (!TrafficSignals.HasSignal(ji, jj)) return false;

            // option A: cross east/west (over the N-S road); option B: north/south (over the E-W road)
            bool east = _corner == 1 || _corner == 2, north = _corner == 2 || _corner == 3;
            int ax = _bx + (east ? 1 : -1), bz = _bz + (north ? 1 : -1);
            bool canA = ax >= 0 && ax < grid, canB = bz >= 0 && bz < grid;
            if (!canA && !canB) return false;
            bool useA = canA && (!canB || _rng.NextDouble() < 0.5);

            _crossI = ji; _crossJ = jj;
            _crossNs = useA;
            if (useA)
            {
                _nextBx = ax; _nextBz = _bz;
                _nextCorner = _corner == 1 ? 0 : _corner == 2 ? 3 : _corner == 0 ? 1 : 2;
                _waitFacing = east ? Vector3.right : Vector3.left;
            }
            else
            {
                _nextBx = _bx; _nextBz = bz;
                _nextCorner = _corner == 3 ? 0 : _corner == 2 ? 1 : _corner == 0 ? 3 : 2;
                _waitFacing = north ? Vector3.forward : Vector3.back;
            }
            return true;
        }
    }

    /// <summary>Places people on block sidewalks (busier downtown) and around park fountains.</summary>
    public static class PedestrianSpawner
    {
        public static void Spawn(Transform cityRoot, int seed = 99)
        {
            var parent = new GameObject("People").transform;
            parent.SetParent(cityRoot, false);
            var rng = new System.Random(seed);
            float y = CityBuilder.PadTop;

            for (int bx = 0; bx < CityBuilder.Grid; bx++)
            {
                for (int bz = 0; bz < CityBuilder.Grid; bz++)
                {
                    var district = CityBuilder.Classify(bx, bz);
                    int count = district == CityBuilder.District.Downtown ? 2 : 1;
                    for (int k = 0; k < count; k++)
                    {
                        int corner = rng.Next(4), dir = rng.NextDouble() < 0.5 ? 1 : -1;
                        int next = (corner + dir + 4) % 4;
                        Vector3 start = Vector3.Lerp(Pedestrian.CornerPos(bx, bz, corner), Pedestrian.CornerPos(bx, bz, next), (float)rng.NextDouble());
                        var ped = Make(parent, start, rng);
                        ped.InitCity(bx, bz, next, dir, Speed(rng), rng.Next());
                    }

                    if (district == CityBuilder.District.Park)
                    {
                        float x0 = CityBuilder.Line(bx) + CityBuilder.Width(bx) * 0.5f, x1 = CityBuilder.Line(bx + 1) - CityBuilder.Width(bx + 1) * 0.5f;
                        float z0 = CityBuilder.Line(bz) + CityBuilder.Width(bz) * 0.5f, z1 = CityBuilder.Line(bz + 1) - CityBuilder.Width(bz + 1) * 0.5f;
                        float cx = (x0 + x1) * 0.5f, cz = (z0 + z1) * 0.5f, r = 7f;
                        var loop = new List<Vector3>
                        {
                            new Vector3(cx - r, y, cz - r), new Vector3(cx + r, y, cz - r),
                            new Vector3(cx + r, y, cz + r), new Vector3(cx - r, y, cz + r)
                        };
                        for (int k = 0; k < 3; k++)
                        {
                            var route = new List<Vector3>(loop);
                            if (rng.NextDouble() < 0.5) route.Reverse();
                            int edge = rng.Next(4);
                            var ped = Make(parent, Vector3.Lerp(route[edge], route[(edge + 1) % 4], (float)rng.NextDouble()), rng);
                            ped.InitLoop(route, edge + 1, Speed(rng) * 0.8f, rng.Next());
                        }
                    }
                }
            }
        }

        static float Speed(System.Random rng) => 1.05f + (float)rng.NextDouble() * 0.55f;

        static Pedestrian Make(Transform parent, Vector3 pos, System.Random rng)
        {
            var look = PersonBuilder.RandomLook(rng);
            var rig = PersonBuilder.Build(parent, look, look.female ? "Woman" : "Man");
            rig.root.position = pos;
            // trigger only: bullets can hit people, but they never block cars or the player
            var col = rig.root.gameObject.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 0.9f, 0f);
            col.height = 1.75f;
            col.radius = 0.28f;
            var ped = rig.root.gameObject.AddComponent<Pedestrian>();
            ped.speedFactor = look.elderly ? 0.7f : 1f;   // older people walk slower
            return ped;
        }
    }
}
