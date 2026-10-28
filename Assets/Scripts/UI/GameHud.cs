using UnityEngine;
using UnityEngine.UI;
using FortunerCity.Vehicle;

namespace FortunerCity.UI
{
    /// <summary>Speed / gear, control hints, context prompt ("F  Get in"), crosshair + ammo, pause menu.</summary>
    public class GameHud : MonoBehaviour
    {
        public PlayerDriveController drive;
        Text _speed;
        Text _gear;
        Text _hint;
        Text _context;
        Text _crosshair;
        Text _ammo;
        GameObject _pausePanel;
        bool _paused;

        const string DriveHelp = "WASD drive  ·  Space handbrake  ·  H horn  ·  C camera  ·  F get out  ·  Esc pause";
        const string WalkHelp = "W run  ·  Shift+W sprint  ·  Ctrl+W walk  ·  A/D turn  ·  Space jump  ·  F get in  ·  1 pistol  2 rifle  3 shotgun  ·  Esc pause";
        const string AimHelp = "Mouse aim  ·  Left click fire  ·  R reload  ·  A/D side-step  ·  1/2/3 switch  ·  Q holster";

        public static GameHud Create(PlayerDriveController drive)
        {
            var canvasGo = new GameObject("HUD_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var hud = canvasGo.AddComponent<GameHud>();
            hud.drive = drive;
            hud.BuildUi(canvasGo.transform);
            return hud;
        }

        public void SetDrive(PlayerDriveController d)
        {
            drive = d;
            if (_hint != null) _hint.text = d != null ? DriveHelp : WalkHelp;
        }

        /// <summary>Big prompt above the controls line (e.g. "F  Get in this car").</summary>
        public void SetContextHint(string text)
        {
            if (_context != null && _context.text != text) _context.text = text;
        }

        public void SetWeapon(string name, int mag, int reserve, bool reloading)
        {
            if (_crosshair != null) _crosshair.enabled = true;
            if (_ammo != null) _ammo.text = reloading ? $"{name}   reloading…" : $"{name}   {mag} / {reserve}";
            if (_hint != null) _hint.text = AimHelp;
        }

        public void HideWeapon()
        {
            if (_crosshair != null) _crosshair.enabled = false;
            if (_ammo != null) _ammo.text = "";
            if (_hint != null) _hint.text = drive != null ? DriveHelp : WalkHelp;
        }

        void BuildUi(Transform parent)
        {
            _speed = MakeText(parent, "Speed", new Vector2(0.02f, 0.92f), new Vector2(0.28f, 0.98f), 28, TextAnchor.UpperLeft);
            _gear = MakeText(parent, "Gear", new Vector2(0.02f, 0.84f), new Vector2(0.15f, 0.92f), 22, TextAnchor.UpperLeft);
            _hint = MakeText(parent, "Hint", new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.1f), 16, TextAnchor.LowerCenter);
            _hint.text = DriveHelp;
            _hint.color = new Color(1f, 1f, 1f, 0.75f);
            _context = MakeText(parent, "Context", new Vector2(0.25f, 0.1f), new Vector2(0.75f, 0.17f), 24, TextAnchor.LowerCenter);
            _context.color = new Color(1f, 0.85f, 0.3f);
            _crosshair = MakeText(parent, "Crosshair", new Vector2(0.45f, 0.45f), new Vector2(0.55f, 0.55f), 30, TextAnchor.MiddleCenter);
            _crosshair.text = "+";
            _crosshair.enabled = false;
            _ammo = MakeText(parent, "Ammo", new Vector2(0.7f, 0.9f), new Vector2(0.98f, 0.98f), 24, TextAnchor.UpperRight);

            _pausePanel = new GameObject("PausePanel");
            _pausePanel.transform.SetParent(parent, false);
            var rt = _pausePanel.AddComponent<RectTransform>();
            Stretch(rt);
            var img = _pausePanel.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.65f);

            var pauseText = MakeText(_pausePanel.transform, "Paused", new Vector2(0.3f, 0.55f), new Vector2(0.7f, 0.7f), 36, TextAnchor.MiddleCenter);
            pauseText.text = "PAUSED";
            var sub = MakeText(_pausePanel.transform, "Sub", new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.55f), 18, TextAnchor.MiddleCenter);
            sub.text = "Esc resume  ·  R restart";
            _pausePanel.SetActive(false);
        }

        static Font _font;

        // Unity 6 removed built-in Arial.ttf (throws); LegacyRuntime.ttf replaces it
        static Font HudFont(int size)
        {
            if (_font != null) return _font;
            try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch (System.ArgumentException) { _font = null; }
            if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", size);
            return _font;
        }

        Text MakeText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int size, TextAnchor align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<Text>();
            t.font = HudFont(size);
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.text = "";
            return t;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void Update()
        {
            if (drive != null)
            {
                if (_speed != null) _speed.text = $"{drive.SpeedKmh:0} km/h";
                if (_gear != null) _gear.text = $"Gear {drive.GearLabel}";
            }
            else
            {
                if (_speed != null) _speed.text = "On foot";
                if (_gear != null) _gear.text = "";
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                SetPaused(!_paused);

            if (_paused && Input.GetKeyDown(KeyCode.R))
            {
                Time.timeScale = 1f;
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    string.IsNullOrEmpty(scene.name) ? "CityDrive" : scene.name);
            }
        }

        void OnGUI()
        {
            // Fallback HUD if uGUI Text failed to init
            if (_speed != null || drive == null) return;
            GUI.Label(new Rect(16, 16, 280, 28), $"{drive.SpeedKmh:0} km/h  ·  Gear {drive.GearLabel}");
            GUI.Label(new Rect(16, Screen.height - 36, 520, 24), "WASD drive · Space handbrake · C camera · Esc pause");
        }

        void SetPaused(bool p)
        {
            _paused = p;
            Time.timeScale = p ? 0f : 1f;
            if (_pausePanel) _pausePanel.SetActive(p);
        }
    }
}
