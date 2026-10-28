using UnityEngine;
using UnityEngine.SceneManagement;

namespace FortunerCity.Core
{
    /// <summary>
    /// Ensures Boot/CityDrive work even if scene YAML has no script references yet.
    /// </summary>
    public static class AutoSceneStart
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnLoad()
        {
            var scene = SceneManager.GetActiveScene();
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
