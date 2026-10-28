using UnityEngine;
using FortunerCity.Vehicle;

namespace FortunerCity.Core
{
    /// <summary>
    /// Legacy Input Manager — compiles without Input System package.
    /// </summary>
    public class GameInputBridge : MonoBehaviour
    {
        public PlayerDriveController drive;

        void Update()
        {
            if (drive == null) return;
            if (Time.timeScale <= 0f) return;

            float throttle = 0f;
            float steer = 0f;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) throttle += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) throttle -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) steer -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) steer += 1f;

            bool handbrake = Input.GetKey(KeyCode.Space);
            drive.SetInput(throttle, steer, handbrake);
        }
    }
}
