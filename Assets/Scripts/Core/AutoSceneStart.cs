using UnityEngine;
using UnityEngine.SceneManagement;

namespace FortunerCity.Core
{
    /// <summary>
    /// Ensures Boot/CityDrive work even if scene YAML has no script references yet.
    /// Runs for the first scene AND every scene loaded later (Boot → CityDrive in builds, R to restart),
    /// because RuntimeInitializeOnLoadMethod alone only fires once at startup.
    /// </summary>
    public static class AutoSceneStart
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnLoad()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Ensure(SceneManager.GetActiveScene());
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Ensure(scene);

        static void Ensure(Scene scene)
        {
            if (!scene.IsValid()) return;

            if (scene.name == "Boot")
            {
                if (UnityCompat.FindAny<BootLoader>() == null)
                {
                    var go = new GameObject("BootLoader");
                    go.AddComponent<BootLoader>();
                }
                return;
            }

            if (scene.name == "CityDrive" || scene.name == "SampleScene")
            {
                if (UnityCompat.FindAny<GameBootstrap>() == null)
                {
                    var go = new GameObject("GameBootstrap");
                    go.AddComponent<GameBootstrap>();
                }
            }
        }
    }
}
