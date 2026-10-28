#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FortunerCity.EditorTools
{
    /// <summary>
    /// CLI: Unity.exe -projectPath ... -executeMethod FortunerCity.EditorTools.LaunchPlay.Run
    /// </summary>
    public static class LaunchPlay
    {
        public static void Run()
        {
            const string scene = "Assets/Scenes/CityDrive.unity";
            if (!System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "../", scene)))
            {
                // path relative to project
            }

            var scenePath = "Assets/Scenes/CityDrive.unity";
            if (!System.IO.File.Exists(
                    System.IO.Path.GetFullPath(
                        System.IO.Path.Combine(Application.dataPath, "..", scenePath))))
            {
                Debug.LogError("CityDrive scene missing: " + scenePath);
                return;
            }

            EditorSceneManager.OpenScene(scenePath);
            EditorApplication.delayCall += () =>
            {
                EditorApplication.isPlaying = true;
                Debug.Log("Fortuner City MVP: entering Play Mode");
            };
        }
    }
}
#endif
