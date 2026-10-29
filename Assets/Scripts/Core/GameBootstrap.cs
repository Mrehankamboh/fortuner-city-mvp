using UnityEngine;
using FortunerCity.City;
using FortunerCity.Vehicle;
using FortunerCity.CameraSys;
using FortunerCity.Traffic;
using FortunerCity.UI;
using FortunerCity.AudioSys;
using FortunerCity.People;
using FortunerCity.PlayerSys;
using FortunerCity.Weapons;

namespace FortunerCity.Core
{
    /// <summary>
    /// Runtime bootstrap: builds the city, the player's Fortuner, traffic (cars + bikes), people,
    /// camera, HUD, audio, the on-foot player with weapons, and two parked bikes near the start.
    /// Attach to empty scene (CityDrive) — Play to run.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Traffic")]
        public int trafficCount = 30;

        void Awake()
        {
            // Lighting
            if (RenderSettings.sun == null)
            {
                var lightGo = new GameObject("Sun");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.15f;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.shadows = LightShadows.Soft;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                RenderSettings.sun = light;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.5f, 0.55f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.55f, 0.62f, 0.7f);
            RenderSettings.fogDensity = 0.0016f;   // light haze so skyscrapers stay visible

            // City
            var city = CityBuilder.Build();

            // Player SUV
            var player = FortunerBuilder.Build(city.playerSpawn, city.playerSpawnRot);
            var drive = player.GetComponent<PlayerDriveController>();

            // Two parked bikes on the sidewalk right next to the start
            var side = city.playerSpawn + new Vector3(12f, 0f, CityBuilder.AvenueWidth * 0.5f - CityBuilder.AvenueMidLane + 2.2f);
            FortunerBuilder.BuildParkedBike(BikeBuilder.Commuter, BikeBuilder.Commuter.palette[0], side, Quaternion.Euler(0f, 60f, 0f));
            FortunerBuilder.BuildParkedBike(BikeBuilder.Sport, BikeBuilder.Sport.palette[0], side + new Vector3(3f, 0f, 0f), Quaternion.Euler(0f, 60f, 0f));

            // Input
            var inputGo = new GameObject("InputBridge");
            var bridge = inputGo.AddComponent<GameInputBridge>();
            bridge.drive = drive;

            // Camera
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                cam.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
            }
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 1500f;
            cam.fieldOfView = 65f;
            var chase = cam.gameObject.GetComponent<ChaseCamera>() ?? cam.gameObject.AddComponent<ChaseCamera>();
            chase.SetTarget(player.transform);
            cam.transform.position = player.transform.TransformPoint(chase.chaseOffset);
            cam.transform.LookAt(player.transform.position + Vector3.up * 1.2f);

            // Traffic (cars + motorbikes)
            TrafficSpawner.Spawn(city.trafficLoops, trafficCount);

            // HUD
            var hud = GameHud.Create(drive);

            // Audio
            VehicleAudio.Attach(player, drive);
            MusicPlayer.Create();   // background music made in code (M mutes)

            // People on the sidewalks and in the parks
            PedestrianSpawner.Spawn(city.root);

            // Get out (F), walk, carry weapons, and get into any car or bike
            var walker = PlayerWalker.Create();
            walker.gameObject.AddComponent<PlayerWeapons>().Init(walker.Rig, chase, hud);
            var switcher = new GameObject("PlayerSwitcher").AddComponent<PlayerSwitcher>();
            switcher.Init(drive, bridge, chase, hud, walker);

            Debug.Log("Fortuner City MVP ready — drive the Fortis XR (Fortuner-style SUV).");
        }
    }
}
