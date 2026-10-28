using UnityEngine;
using FortunerCity.AudioSys;
using FortunerCity.People;

namespace FortunerCity.PlayerSys
{
    /// <summary>
    /// The driver on foot. W jogs, Shift+W sprints, Ctrl+W walks; A/D turn (or side-step while a gun is
    /// out — the mouse turns then); Space jumps. Footstep sounds follow the animation. Uses a
    /// CharacterController so kerbs, cars and buildings block him naturally.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerWalker : MonoBehaviour
    {
        public float walkSpeed = 1.8f;
        public float jogSpeed = 4.2f;
        public float sprintSpeed = 7.5f;
        public float turnSpeed = 170f;
        public float jumpSpeed = 4.5f;
        public float gravity = -18f;
        public bool armed;

        /// <summary>True while sprinting (the camera widens a little).</summary>
        public static bool Sprinting { get; private set; }

        /// <summary>Driver: white shalwar kameez, brown sandals (also used for the rider on bikes).</summary>
        public static readonly PersonLook DriverLook = new PersonLook
        {
            skin = new Color(0.76f, 0.57f, 0.42f), hair = new Color(0.05f, 0.04f, 0.035f),
            top = new Color(0.94f, 0.94f, 0.92f), bottom = new Color(0.94f, 0.94f, 0.92f),
            shoes = new Color(0.25f, 0.15f, 0.08f), accent = Color.white, vest = new Color(0.12f, 0.12f, 0.14f),
            kameez = true, longSleeves = true, facialHair = 1, height = 1.78f, build = 1.02f
        };

        public PersonRig Rig { get; private set; }
        CharacterController _cc;
        PersonAnimator _anim;
        AudioSource _feet;
        float _vy, _speed;
        int _stepVariant;

        public static PlayerWalker Create()
        {
            var go = new GameObject("Player_OnFoot");
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.75f;
            cc.radius = 0.28f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.4f;
            cc.slopeLimit = 50f;

            var rig = PersonBuilder.Build(go.transform, DriverLook, "Driver");
            var walker = go.AddComponent<PlayerWalker>();
            walker._cc = cc;
            walker.Rig = rig;
            walker._anim = rig.root.GetComponent<PersonAnimator>();
            walker._feet = go.AddComponent<AudioSource>();
            walker._feet.spatialBlend = 0.3f;
            walker._feet.playOnAwake = false;
            rig.onStep = walker.Footstep;
            go.SetActive(false);
            return walker;
        }

        public void Teleport(Vector3 pos, Quaternion rot)
        {
            _cc.enabled = false;
            transform.SetPositionAndRotation(pos, rot);
            _cc.enabled = true;
            _vy = 0f;
            _speed = 0f;
        }

        void OnDisable() => Sprinting = false;

        void Update()
        {
            if (Time.timeScale <= 0f) return;
            float dt = Time.deltaTime;

            float side = 0f, fwd = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) side -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) side += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) fwd += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) fwd -= 1f;
            bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool slow = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            // target pace: jog by default, sprint with Shift, walk with Ctrl; slower backwards / with a gun out
            float pace = slow ? walkSpeed : sprint ? sprintSpeed : jogSpeed;
            if (fwd < 0f) pace = Mathf.Min(pace, jogSpeed * 0.6f);
            if (armed) pace = Mathf.Min(pace, slow ? walkSpeed : jogSpeed * 0.85f);

            Vector3 dir;
            if (armed)
            {
                dir = transform.forward * fwd + transform.right * side;   // the mouse turns, A/D side-step
                if (dir.sqrMagnitude > 1f) dir.Normalize();
            }
            else
            {
                transform.Rotate(0f, side * turnSpeed * dt * (sprint ? 0.75f : 1f), 0f);
                dir = transform.forward * fwd;
            }

            // smooth acceleration so starting / stopping isn't instant
            float target = dir.sqrMagnitude > 0.01f ? pace : 0f;
            _speed = Mathf.MoveTowards(_speed, target, (target > _speed ? 9f : 14f) * dt);
            Sprinting = sprint && fwd > 0f && _speed > jogSpeed + 0.5f;
            Vector3 move = dir.sqrMagnitude > 0.01f ? dir.normalized * _speed : transform.forward * 0f;

            if (_cc.isGrounded)
            {
                _vy = -2f;
                if (Input.GetKeyDown(KeyCode.Space)) _vy = jumpSpeed;
            }
            else _vy += gravity * dt;

            _cc.Move((move + Vector3.up * _vy) * dt);
            if (_anim != null) _anim.speed = _cc.isGrounded ? move.magnitude : move.magnitude * 0.5f;
        }

        void Footstep(bool running)
        {
            if (_feet == null || !_cc.isGrounded) return;
            _stepVariant = (_stepVariant + 1 + Random.Range(0, 2)) % 4;
            _feet.pitch = Random.Range(0.92f, 1.08f);
            _feet.PlayOneShot(SoundSynth.Footstep(_stepVariant, running), running ? 0.55f : 0.35f);
        }
    }
}
