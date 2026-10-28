#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FortunerCity.EditorTools
{
    /// <summary>
    /// Menu: Fortuner City / Build for Chrome (WebGL)
    /// Output: Builds/WebGL (play it with play-in-chrome.bat in the project folder).
    /// </summary>
    public static class BuildWebGL
    {
        const string OutputDir = "Builds/WebGL";

        [MenuItem("Fortuner City/Build for Chrome (WebGL)")]
        public static void Build()
        {
            RuntimeShaders.Ensure();

            // Uncompressed: runs from the simple local server without special HTTP headers
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.dataCaching = true;

            if (!Directory.Exists(OutputDir)) Directory.CreateDirectory(OutputDir);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Boot.unity", "Assets/Scenes/CityDrive.unity" },
                locationPathName = OutputDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log($"WebGL build OK → {Path.GetFullPath(OutputDir)}  — run play-in-chrome.bat to open it in Chrome.");
            else
                Debug.LogError($"WebGL build failed: {report.summary.result}");
        }
    }

    /// <summary>
    /// Everything in the game creates materials in code with Shader.Find("Standard"). Builds only ship
    /// shaders that some asset uses, so keep Standard (plus emission + instancing variants) alive via
    /// two small material assets in Resources, and keep the exponential-fog variants.
    /// </summary>
    public static class RuntimeShaders
    {
        const string Folder = "Assets/Resources/RuntimeShaders";

        public static void Ensure()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateFolder("Assets/Resources", "RuntimeShaders");
            }

            var standard = Shader.Find("Standard");
            CreateIfMissing($"{Folder}/StandardKeep.mat", standard, false);
            CreateIfMissing($"{Folder}/StandardEmissionKeep.mat", standard, true);

            // smoke / fire / tracers use the built-in particle shader in alpha-blend mode
            var particles = Shader.Find("Particles/Standard Unlit");
            if (particles != null && AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/ParticlesKeep.mat") == null)
            {
                var pm = new Material(particles);
                pm.EnableKeyword("_ALPHABLEND_ON");
                AssetDatabase.CreateAsset(pm, $"{Folder}/ParticlesKeep.mat");
            }

            var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            if (gs != null)
            {
                var so = new SerializedObject(gs);
                var strip = so.FindProperty("m_FogStripping");
                var exp2 = so.FindProperty("m_FogKeepExp2");
                if (strip != null) strip.intValue = 1;      // custom fog stripping
                if (exp2 != null) exp2.boolValue = true;    // keep the exp² fog used at runtime
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
        }

        static void CreateIfMissing(string path, Shader shader, bool emission)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var m = new Material(shader) { enableInstancing = true };
            if (emission)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.white);
            }
            AssetDatabase.CreateAsset(m, path);
        }
    }
}
#endif
