using System.Collections.Generic;
using FloraForge;
using UnityEditor;
using UnityEngine;

public static class FloraForgeBushAssetUtility
{
    public const string DefaultLeafMeshPath = "Assets/FloraForge/Meshes/BushDummyLeaf.asset";
    public const string DefaultLeafTexturePath = "Assets/FloraForge/Textures/BushDummyLeafTexture.asset";
    public const string DefaultLeafMaterialPath = "Assets/FloraForge/Materials/BushLeafMaterial.mat";
    public const string DefaultLeafShaderPath = "Assets/FloraForge/Shaders/FloraForgeBushLeaf.shader";
    private const string DefaultLeafMeshName = "BushDummyLeaf";
    private const string DefaultLeafTextureName = "BushDummyLeafTexture";
    private const string DefaultLeafMaterialName = "BushLeafMaterial";

    [MenuItem("Tools/FloraForge/Bush/Create Default Bush Assets")]
    public static void CreateDefaultBushAssets()
    {
        EnsureAssetFolders();

        var mesh = CreateOrUpdateDummyLeafMesh();
        var texture = CreateOrUpdateDummyLeafTexture();
        var shader = LoadBushLeafShader();
        CreateOrUpdateLeafMaterial(shader, texture);

        EditorUtility.SetDirty(mesh);
        EditorUtility.SetDirty(texture);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/FloraForge/Bush/Assign Default Assets To Selected Generator")]
    public static void AssignDefaultAssetsToSelectedGenerator()
    {
        var generator = Selection.activeGameObject == null
            ? null
            : Selection.activeGameObject.GetComponent<FloraForgeBushGenerator>();

        if (generator == null)
        {
            Debug.LogWarning("Select a FloraForgeBushGenerator GameObject first.");
            return;
        }

        AssignDefaultAssets(generator);
    }

    public static void AssignDefaultAssets(FloraForgeBushGenerator generator)
    {
        if (generator == null)
        {
            return;
        }

        CreateDefaultBushAssets();

        generator.leafMesh = AssetDatabase.LoadAssetAtPath<Mesh>(DefaultLeafMeshPath);
        generator.leafTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultLeafTexturePath);
        generator.leafMaterialOverride = AssetDatabase.LoadAssetAtPath<Material>(DefaultLeafMaterialPath);

        EditorUtility.SetDirty(generator);
    }

    private static Mesh CreateOrUpdateDummyLeafMesh()
    {
        const int rows = 9;
        const int cols = 5;
        const float length = 0.44f;
        const float maxHalfWidth = 0.125f;

        var vertices = new Vector3[(rows + 1) * cols];
        var normals = new Vector3[vertices.Length];
        var uvs = new Vector2[vertices.Length];
        var colors = new Color[vertices.Length];
        var triangles = new List<int>(rows * (cols - 1) * 6);

        for (var y = 0; y <= rows; y++)
        {
            var t = y / (float)rows;
            var body = Mathf.Pow(Mathf.Max(0.0f, Mathf.Sin(t * Mathf.PI)), 0.58f);
            var asymmetry = Mathf.Lerp(0.76f, 1.08f, Mathf.SmoothStep(0.0f, 1.0f, 1.0f - Mathf.Abs(t - 0.46f) * 1.85f));
            var halfWidth = maxHalfWidth * body * asymmetry;
            var centerY = t * length;
            var centerZ = Mathf.Sin(t * Mathf.PI) * 0.026f;

            for (var x = 0; x < cols; x++)
            {
                var u = x / (float)(cols - 1);
                var lateral = (u - 0.5f) * 2.0f;
                var edgeCup = -Mathf.Abs(lateral) * Mathf.Abs(lateral) * body * 0.012f;
                var index = y * cols + x;
                vertices[index] = new Vector3(lateral * halfWidth, centerY, centerZ + edgeCup);
                normals[index] = Vector3.forward;
                uvs[index] = new Vector2(u, t);
                colors[index] = Color.white;
            }
        }

        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < cols - 1; x++)
            {
                var a = y * cols + x;
                var b = y * cols + x + 1;
                var c = (y + 1) * cols + x;
                var d = (y + 1) * cols + x + 1;

                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);
                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(d);
            }
        }

        var mesh = new Mesh
        {
            name = DefaultLeafMeshName
        };

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        if (mesh.vertexCount == 0 || mesh.GetIndexCount(0) == 0)
        {
            Debug.LogError("Failed to build BushDummyLeaf mesh data.");
            return mesh;
        }

        // Recreate this generated asset after mesh data is complete. Creating an empty Mesh asset
        // first can leave a zero-vertex asset behind if Unity reloads while this menu item runs.
        AssetDatabase.DeleteAsset(DefaultLeafMeshPath);
        AssetDatabase.CreateAsset(mesh, DefaultLeafMeshPath);
        EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssetIfDirty(mesh);
        AssetDatabase.ImportAsset(DefaultLeafMeshPath, ImportAssetOptions.ForceSynchronousImport);

        var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(DefaultLeafMeshPath);
        if (savedMesh == null || savedMesh.vertexCount == 0)
        {
            Debug.LogError("BushDummyLeaf asset was created without vertex data.");
        }

        return mesh;
    }

    private static Texture2D CreateOrUpdateDummyLeafTexture()
    {
        const int width = 64;
        const int height = 128;
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultLeafTexturePath);
        if (texture == null)
        {
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = DefaultLeafTextureName
            };
            AssetDatabase.CreateAsset(texture, DefaultLeafTexturePath);
        }

        texture.name = DefaultLeafTextureName;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        var pixels = new Color[texture.width * texture.height];
        for (var y = 0; y < texture.height; y++)
        {
            var v = y / (float)(texture.height - 1);
            for (var x = 0; x < texture.width; x++)
            {
                var u = x / (float)(texture.width - 1);
                var centerDistance = Mathf.Abs(u - 0.48f);
                var verticalLight = Mathf.SmoothStep(0.0f, 1.0f, v);
                var vein = 1.0f - Mathf.SmoothStep(0.0f, 0.045f, centerDistance);
                var edgeDarken = Mathf.SmoothStep(0.38f, 0.5f, Mathf.Abs(u - 0.5f));
                var highlight = Mathf.Clamp01((1.0f - centerDistance * 2.2f) * verticalLight);

                var dark = new Color(0.12f, 0.28f, 0.09f, 1.0f);
                var mid = new Color(0.34f, 0.58f, 0.18f, 1.0f);
                var light = new Color(0.72f, 0.86f, 0.28f, 1.0f);
                var color = Color.Lerp(dark, mid, verticalLight * 0.8f);
                color = Color.Lerp(color, light, highlight * 0.34f);
                color = Color.Lerp(color, new Color(0.18f, 0.36f, 0.1f, 1.0f), edgeDarken * 0.45f);
                color = Color.Lerp(color, new Color(0.58f, 0.78f, 0.2f, 1.0f), vein * 0.22f);
                pixels[y * texture.width + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static Material CreateOrUpdateLeafMaterial(Shader shader, Texture2D texture)
    {
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(DefaultLeafMaterialPath);
        if (material == null)
        {
            material = new Material(shader)
            {
                name = DefaultLeafMaterialName
            };
            AssetDatabase.CreateAsset(material, DefaultLeafMaterialPath);
        }
        else if (shader != null)
        {
            material.shader = shader;
        }

        material.name = DefaultLeafMaterialName;
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_ShadowMultiplier", 0.64f);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Shader LoadBushLeafShader()
    {
        AssetDatabase.ImportAsset(DefaultLeafShaderPath, ImportAssetOptions.ForceSynchronousImport);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(DefaultLeafShaderPath);
        if (shader == null)
        {
            shader = Shader.Find("FloraForge/Bush Leaf Vertex Color");
        }

        if (shader == null)
        {
            Debug.LogWarning("FloraForge bush leaf shader was not found. The default material will use Unity's fallback shader.");
        }

        return shader;
    }

    private static void EnsureAssetFolders()
    {
        EnsureFolder("Assets", "FloraForge");
        EnsureFolder("Assets/FloraForge", "Meshes");
        EnsureFolder("Assets/FloraForge", "Materials");
        EnsureFolder("Assets/FloraForge", "Shaders");
        EnsureFolder("Assets/FloraForge", "Textures");
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
