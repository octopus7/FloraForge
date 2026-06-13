using FloraForge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(FloraForgeVegetationGenerator))]
public sealed class FloraForgeVegetationGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Regenerate"))
            {
                foreach (var targetObject in targets)
                {
                    var generator = (FloraForgeVegetationGenerator)targetObject;
                    Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Regenerate FloraForge Vegetation");
                    generator.Regenerate();
                    EditorUtility.SetDirty(generator.gameObject);
                }
            }

            if (GUILayout.Button("Clear"))
            {
                foreach (var targetObject in targets)
                {
                    var generator = (FloraForgeVegetationGenerator)targetObject;
                    Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Clear FloraForge Vegetation");
                    generator.ClearGenerated();
                    EditorUtility.SetDirty(generator.gameObject);
                }
            }
        }
    }
}

public static class FloraForgeVegetationMenu
{
    private const string DemoScenePath = "Assets/FloraForge/Scenes/TavernVegetationDemo.unity";

    [MenuItem("Tools/FloraForge/Add Generator To Current Scene")]
    public static void AddGeneratorToCurrentScene()
    {
        CreateGeneratorInCurrentScene();
    }

    private static FloraForgeVegetationGenerator CreateGeneratorInCurrentScene()
    {
        var generatorObject = new GameObject("FloraForge Vegetation Generator");
        Undo.RegisterCreatedObjectUndo(generatorObject, "Add FloraForge Vegetation Generator");

        var generator = generatorObject.AddComponent<FloraForgeVegetationGenerator>();
        generator.seed = 49708;
        generator.climbingVineCount = 9;
        generator.hangingVineCount = 11;
        generator.shrubCount = 7;
        generator.wildflowerClumps = 5;
        generator.Regenerate();

        Selection.activeGameObject = generatorObject;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return generator;
    }

    [MenuItem("Tools/FloraForge/Create Tavern Vegetation Demo Scene")]
    public static void CreateDemoScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AddCameraAndLighting();

        var generator = CreateGeneratorInCurrentScene();
        generator.transform.position = Vector3.zero;
        generator.transform.rotation = Quaternion.identity;

        EnsureFolder("Assets", "FloraForge");
        EnsureFolder("Assets/FloraForge", "Scenes");

        EditorSceneManager.SaveScene(scene, DemoScenePath);
        EditorGUIUtility.PingObject(generator.gameObject);
        Selection.activeGameObject = generator.gameObject;
    }

    private static void AddCameraAndLighting()
    {
        var cameraObject = new GameObject("Preview Camera");
        var camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.transform.position = new Vector3(0.1f, 1.85f, -6.8f);
        camera.transform.rotation = Quaternion.LookRotation(new Vector3(-0.1f, -0.05f, 1.0f));
        camera.fieldOfView = 43.0f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100.0f;

        var sunObject = new GameObject("Warm Directional Light");
        var sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1.0f, 0.86f, 0.68f);
        sun.intensity = 0.9f;
        sun.transform.rotation = Quaternion.Euler(42.0f, -34.0f, 0.0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.38f, 0.4f, 0.36f);
    }

    private static void EnsureFolder(string parent, string child)
    {
        var path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
