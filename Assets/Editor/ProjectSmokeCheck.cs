#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FortunerCity.EditorTools
{
    public static class ProjectSmokeCheck
    {
        [MenuItem("Fortuner City/Smoke Check (Validate Scripts)")]
        public static void Validate()
        {
            int ok = 0;
            void Need(System.Type t)
            {
                if (t == null) Debug.LogError("Missing type");
                else { ok++; Debug.Log($"OK: {t.Name}"); }
            }

            Need(typeof(FortunerCity.Core.GameBootstrap));
            Need(typeof(FortunerCity.Vehicle.PlayerDriveController));
            Need(typeof(FortunerCity.Vehicle.FortunerBuilder));
            Need(typeof(FortunerCity.City.CityBuilder));
            Need(typeof(FortunerCity.Traffic.TrafficCar));
            Need(typeof(FortunerCity.CameraSys.ChaseCamera));
            Need(typeof(FortunerCity.UI.GameHud));
            Need(typeof(FortunerCity.AudioSys.VehicleAudio));

            EditorUtility.DisplayDialog(
                "Fortuner City Smoke Check",
                $"Validated {ok} core types.\nOpen CityDrive and press Play to drive.\nBuild via Fortuner City → Build Windows Standalone.",
                "OK");
        }
    }
}
#endif
