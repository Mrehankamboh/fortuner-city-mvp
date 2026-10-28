#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FortunerCity.EditorTools
{
    /// <summary>
    /// Menu: Fortuner City / Build Windows Standalone
    /// Output: Builds/Windows/FortunerCityMVP.exe
    /// </summary>
    public static class BuildWindows
    {
        const string OutputDir = "Builds/Windows";
        const string ExeName = "FortunerCityMVP.exe";

        [MenuItem("Fortuner City/Build Windows Standalone")]
        public static void Build()
        {
            if (!Directory.Exists(OutputDir))
                Directory.CreateDirectory(OutputDir);

            var scenes = new[]
            {
                "Assets/Scenes/Boot.unity",
                "Assets/Scenes/CityDrive.unity",
            };

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(OutputDir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log($"Windows build OK → {Path.GetFullPath(Path.Combine(OutputDir, ExeName))}");
            else
                Debug.LogError($"Windows build failed: {report.summary.result}");
        }

        [MenuItem("Fortuner City/Open CityDrive Scene")]
        public static void OpenCity()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/CityDrive.unity");
        }
    }
}
#endif
