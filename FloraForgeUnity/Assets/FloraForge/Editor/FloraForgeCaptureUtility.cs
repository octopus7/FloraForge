using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FloraForge
{
    public static class FloraForgeCaptureUtility
    {
        private const string DemoScenePath = "Assets/FloraForge/Scenes/TavernVegetationDemo.unity";

        [MenuItem("Tools/FloraForge/Capture Demo Views")]
        public static void CaptureDemoScene()
        {
            var outputDirectory = GetArgument("-floraCaptureDir");
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                outputDirectory = DefaultOutputDirectory;
            }

            CaptureDemoScene(outputDirectory);

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        public static string DefaultOutputDirectory
        {
            get
            {
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
                return Path.Combine(projectRoot, "Temp", "FloraForgeCaptures");
            }
        }

        public static string RequestPath
        {
            get
            {
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
                return Path.Combine(projectRoot, "Temp", "flora-capture-request.txt");
            }
        }

        public static void CaptureDemoScene(string outputDirectory)
        {
            Debug.Log($"FloraForge capture started: {outputDirectory}");

            Directory.CreateDirectory(outputDirectory);
            EditorSceneManager.OpenScene(DemoScenePath);

            var generator = UnityEngine.Object.FindFirstObjectByType<FloraForgeVegetationGenerator>();
            if (generator != null)
            {
                generator.Regenerate();
            }

            CaptureView(
                Path.Combine(outputDirectory, "flora-overview.png"),
                new Vector3(0.0f, 1.75f, -5.0f),
                new Vector3(0.0f, 1.35f, -0.65f),
                42.0f,
                1600,
                1100);

            CaptureView(
                Path.Combine(outputDirectory, "flora-vines.png"),
                new Vector3(2.6f, 1.25f, -3.2f),
                new Vector3(2.65f, 1.45f, -0.25f),
                34.0f,
                1400,
                1100);

            CaptureView(
                Path.Combine(outputDirectory, "flora-ground.png"),
                new Vector3(1.2f, 0.75f, -2.8f),
                new Vector3(1.15f, 0.32f, -0.95f),
                32.0f,
                1400,
                1000);

            AssetDatabase.Refresh();
            Debug.Log($"FloraForge capture finished: {outputDirectory}");
        }

        private static void CaptureView(string outputPath, Vector3 cameraPosition, Vector3 target, float fieldOfView, int width, int height)
        {
            var cameraObject = new GameObject("FloraForge Capture Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = cameraPosition;
            camera.transform.rotation = Quaternion.LookRotation(target - cameraPosition, Vector3.up);
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 60.0f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.52f, 0.6f, 0.67f);
            camera.allowHDR = false;
            camera.allowMSAA = true;

            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4
            };
            var previousActive = RenderTexture.active;

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                var image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply(false);

                File.WriteAllBytes(outputPath, image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static string GetArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }

    [InitializeOnLoad]
    internal static class FloraForgeCaptureRequestWatcher
    {
        private static DateTime lastProcessedRequestTime;
        private static double nextCheckTime;

        static FloraForgeCaptureRequestWatcher()
        {
            EditorApplication.update += CheckForCaptureRequest;
        }

        private static void CheckForCaptureRequest()
        {
            if (EditorApplication.timeSinceStartup < nextCheckTime)
            {
                return;
            }

            nextCheckTime = EditorApplication.timeSinceStartup + 1.0;
            var requestPath = FloraForgeCaptureUtility.RequestPath;
            if (!File.Exists(requestPath))
            {
                return;
            }

            var writeTime = File.GetLastWriteTimeUtc(requestPath);
            if (writeTime <= lastProcessedRequestTime)
            {
                return;
            }

            lastProcessedRequestTime = writeTime;
            try
            {
                FloraForgeCaptureUtility.CaptureDemoScene(FloraForgeCaptureUtility.DefaultOutputDirectory);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
