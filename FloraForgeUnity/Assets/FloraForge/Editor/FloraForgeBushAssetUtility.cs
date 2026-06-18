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
    public const string DefaultVolumeMeshPath = "Assets/FloraForge/Meshes/BushDummyVolume.asset";
    private const string DefaultLeafMeshName = "BushDummyLeaf";
    private const string DefaultLeafTextureName = "BushDummyLeafTexture";
    private const string DefaultLeafMaterialName = "BushLeafMaterial";
    private const string DefaultVolumeMeshName = "BushDummyVolume";

    [MenuItem("Tools/FloraForge/Bush/Create Default Bush Assets")]
    public static void CreateDefaultBushAssets()
    {
        EnsureAssetFolders();

        var mesh = CreateOrUpdateDummyLeafMesh();
        var volumeMesh = CreateOrUpdateDummyVolumeMesh();
        var texture = CreateOrUpdateDummyLeafTexture();
        var shader = LoadBushLeafShader();
        CreateOrUpdateLeafMaterial(shader, texture);

        EditorUtility.SetDirty(mesh);
        EditorUtility.SetDirty(volumeMesh);
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
        generator.volumeMesh = AssetDatabase.LoadAssetAtPath<Mesh>(DefaultVolumeMeshPath);

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

    private static Mesh CreateOrUpdateDummyVolumeMesh()
    {
        const int latitudeSegments = 8;
        const int longitudeSegments = 18;
        const float halfWidth = 1.0f;
        const float halfDepth = 1.0f;
        const float height = 1.0f;
        const float roundness = 0.36f;

        var vertices = new List<Vector3>((latitudeSegments + 1) * longitudeSegments);
        var normals = new List<Vector3>(vertices.Capacity);
        var uvs = new List<Vector2>(vertices.Capacity);
        var triangles = new List<int>(latitudeSegments * longitudeSegments * 6);

        for (var y = 0; y <= latitudeSegments; y++)
        {
            var v = y / (float)latitudeSegments;
            var yPos = Mathf.Lerp(0.0f, height, v);
            var dome = Mathf.Sqrt(Mathf.Max(0.0f, 1.0f - Mathf.Pow((v - 0.42f) / 0.72f, 2.0f)));
            var squareBlend = Mathf.SmoothStep(0.0f, 1.0f, dome);
            var rowWidth = Mathf.Lerp(0.42f, halfWidth, squareBlend);
            var rowDepth = Mathf.Lerp(0.34f, halfDepth, squareBlend);
            rowWidth *= Mathf.Lerp(0.78f, 1.0f, Mathf.Sin(v * Mathf.PI));
            rowDepth *= Mathf.Lerp(0.72f, 1.0f, Mathf.Sin(v * Mathf.PI));

            for (var x = 0; x < longitudeSegments; x++)
            {
                var u = x / (float)longitudeSegments;
                var angle = u * Mathf.PI * 2.0f;
                var circle = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var roundedSquare = RoundedSquareDirection(circle, roundness);
                var position = new Vector3(roundedSquare.x * rowWidth, yPos, roundedSquare.y * rowDepth);
                var normal = new Vector3(position.x / Mathf.Max(0.001f, halfWidth), Mathf.Lerp(-0.15f, 0.85f, v), position.z / Mathf.Max(0.001f, halfDepth)).normalized;
                vertices.Add(position);
                normals.Add(normal);
                uvs.Add(new Vector2(u, v));
            }
        }

        var bottomCenterIndex = vertices.Count;
        vertices.Add(new Vector3(0.0f, 0.0f, 0.0f));
        normals.Add(Vector3.down);
        uvs.Add(new Vector2(0.5f, 0.0f));

        var topCenterIndex = vertices.Count;
        vertices.Add(new Vector3(0.0f, height, 0.0f));
        normals.Add(Vector3.up);
        uvs.Add(new Vector2(0.5f, 1.0f));

        for (var y = 0; y < latitudeSegments; y++)
        {
            for (var x = 0; x < longitudeSegments; x++)
            {
                var nextX = (x + 1) % longitudeSegments;
                var a = y * longitudeSegments + x;
                var b = y * longitudeSegments + nextX;
                var c = (y + 1) * longitudeSegments + x;
                var d = (y + 1) * longitudeSegments + nextX;
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);
                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(d);
            }
        }

        for (var x = 0; x < longitudeSegments; x++)
        {
            var nextX = (x + 1) % longitudeSegments;
            triangles.Add(bottomCenterIndex);
            triangles.Add(nextX);
            triangles.Add(x);

            var topA = latitudeSegments * longitudeSegments + x;
            var topB = latitudeSegments * longitudeSegments + nextX;
            triangles.Add(topA);
            triangles.Add(topB);
            triangles.Add(topCenterIndex);
        }

        var mesh = new Mesh { name = DefaultVolumeMeshName };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        AssetDatabase.DeleteAsset(DefaultVolumeMeshPath);
        AssetDatabase.CreateAsset(mesh, DefaultVolumeMeshPath);
        EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssetIfDirty(mesh);
        AssetDatabase.ImportAsset(DefaultVolumeMeshPath, ImportAssetOptions.ForceSynchronousImport);
        return mesh;
    }

    private static Vector2 RoundedSquareDirection(Vector2 direction, float roundness)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            return Vector2.right;
        }

        direction.Normalize();
        var maxAxis = Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));
        var square = direction / Mathf.Max(0.0001f, maxAxis);
        return Vector2.Lerp(square, direction, Mathf.Clamp01(roundness));
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
        material.SetFloat("_DebugVertexColor", 1.0f);
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
