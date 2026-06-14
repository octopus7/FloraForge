using FloraForge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(FloraForgeBushGenerator))]
public sealed class FloraForgeBushGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var generator = (FloraForgeBushGenerator)target;
        if (!generator.HasLeafSource)
        {
            EditorGUILayout.HelpBox("Leaf mesh and leaf texture/material slots are reserved for the next implementation step.", MessageType.Info);
        }

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create Scaffold Root"))
            {
                foreach (var targetObject in targets)
                {
                    var bushGenerator = (FloraForgeBushGenerator)targetObject;
                    Undo.RegisterFullObjectHierarchyUndo(bushGenerator.gameObject, "Create Bush Scaffold Root");
                    bushGenerator.Regenerate();
                    EditorUtility.SetDirty(bushGenerator.gameObject);
                }
            }

            if (GUILayout.Button("Clear"))
            {
                foreach (var targetObject in targets)
                {
                    var bushGenerator = (FloraForgeBushGenerator)targetObject;
                    Undo.RegisterFullObjectHierarchyUndo(bushGenerator.gameObject, "Clear Bush Scaffold Root");
                    bushGenerator.ClearGenerated();
                    EditorUtility.SetDirty(bushGenerator.gameObject);
                }
            }
        }
    }
}

public static class FloraForgeBushWorkbenchMenu
{
    private const string BushScenePath = "Assets/FloraForge/Scenes/BushGenerationWorkbench.unity";

    [MenuItem("Tools/FloraForge/Bush/Add Bush Generator To Current Scene")]
    public static void AddBushGeneratorToCurrentScene()
    {
        CreateBushGeneratorInCurrentScene();
    }

    [MenuItem("Tools/FloraForge/Bush/Create Bush Workbench Scene")]
    public static void CreateBushWorkbenchScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AddCameraAndLighting();
        AddNeutralGround();

        var generator = CreateBushGeneratorInCurrentScene();
        generator.transform.position = Vector3.zero;
        generator.transform.rotation = Quaternion.identity;
        generator.Regenerate();

        EnsureFolder("Assets", "FloraForge");
        EnsureFolder("Assets/FloraForge", "Scenes");

        EditorSceneManager.SaveScene(scene, BushScenePath);
        EditorGUIUtility.PingObject(generator.gameObject);
        Selection.activeGameObject = generator.gameObject;
    }

    private static FloraForgeBushGenerator CreateBushGeneratorInCurrentScene()
    {
        var generatorObject = new GameObject("FloraForge Bush Generator");
        Undo.RegisterCreatedObjectUndo(generatorObject, "Add FloraForge Bush Generator");

        var generator = generatorObject.AddComponent<FloraForgeBushGenerator>();
        Selection.activeGameObject = generatorObject;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return generator;
    }

    private static void AddCameraAndLighting()
    {
        var cameraObject = new GameObject("Bush Preview Camera");
        var camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.transform.position = new Vector3(0.0f, 1.45f, -4.6f);
        camera.transform.rotation = Quaternion.LookRotation(new Vector3(0.0f, -0.12f, 1.0f));
        camera.fieldOfView = 38.0f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100.0f;

        var sunObject = new GameObject("Soft Directional Light");
        var sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1.0f, 0.92f, 0.78f);
        sun.intensity = 0.75f;
        sun.transform.rotation = Quaternion.Euler(44.0f, -28.0f, 0.0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.45f, 0.4f);
    }

    private static void AddNeutralGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Neutral Ground Plane";
        ground.transform.position = new Vector3(0.0f, -0.035f, 0.0f);
        ground.transform.localScale = new Vector3(4.2f, 0.06f, 3.2f);

        var renderer = ground.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = CreateSceneMaterial("Bush Workbench Ground", new Color(0.18f, 0.22f, 0.16f));

        var collider = ground.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static Material CreateSceneMaterial(string name, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader)
        {
            name = name,
            color = color
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        return material;
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
