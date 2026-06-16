using System.Collections.Generic;
using UnityEngine;

namespace FloraForge
{
    [ExecuteAlways]
    public sealed class FloraForgeBushGenerator : MonoBehaviour
    {
        private const string GeneratedRootName = "__FloraForgeBushGenerated";
        private const int GeneratorVersion = 2;

        [Header("Leaf Assets")]
        public Mesh leafMesh;
        public Texture2D leafTexture;
        public Material leafMaterialOverride;

        [Header("Bush Volume")]
        [Min(0.1f)] public float radius = 1.2f;
        [Min(0.1f)] public float height = 0.95f;
        [Min(0.1f)] public float depth = 0.85f;

        [Header("Future Placement")]
        public int seed = 190619;
        [Min(0)] public int targetLeafCount = 320;
        [Range(0.0f, 1.0f)] public float surfaceFullness = 0.72f;

        [SerializeField, HideInInspector] private int generatedVersion;

        public bool HasLeafSource => leafMesh != null && (leafMaterialOverride != null || leafTexture != null);

        public void Regenerate()
        {
            ClearGenerated();
            generatedVersion = GeneratorVersion;
            var root = CreateGeneratedRoot();

            if (HasLeafSource)
            {
                CreatePreviewBush(root);
            }
        }

        public void ClearGenerated()
        {
            var generated = transform.Find(GeneratedRootName);
            if (generated == null)
            {
                return;
            }

            DestroyUnityObject(generated.gameObject);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying && generatedVersion != GeneratorVersion)
            {
                Regenerate();
            }
        }

        private void OnValidate()
        {
            radius = Mathf.Max(0.1f, radius);
            height = Mathf.Max(0.1f, height);
            depth = Mathf.Max(0.1f, depth);
            targetLeafCount = Mathf.Max(0, targetLeafCount);
        }

        private void OnDrawGizmosSelected()
        {
            var oldMatrix = Gizmos.matrix;
            var oldColor = Gizmos.color;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.35f, 0.8f, 0.25f, 0.7f);
            Gizmos.DrawWireCube(new Vector3(0.0f, height * 0.5f, 0.0f), new Vector3(radius * 2.0f, height, depth * 2.0f));

            Gizmos.color = new Color(0.9f, 0.95f, 0.25f, 0.45f);
            Gizmos.DrawWireSphere(new Vector3(0.0f, height * 0.52f, 0.0f), Mathf.Max(radius, depth));

            Gizmos.matrix = oldMatrix;
            Gizmos.color = oldColor;
        }

        private Transform CreateGeneratedRoot()
        {
            var generated = new GameObject(GeneratedRootName);
            generated.transform.SetParent(transform, false);
            generated.transform.localPosition = Vector3.zero;
            generated.transform.localRotation = Quaternion.identity;
            generated.transform.localScale = Vector3.one;
            return generated.transform;
        }

        private void CreatePreviewBush(Transform parent)
        {
            var mesh = BuildPreviewBushMesh();
            if (mesh == null)
            {
                return;
            }

            var meshObject = new GameObject("Bush Preview Mesh");
            meshObject.transform.SetParent(parent, false);
            meshObject.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = meshObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = leafMaterialOverride;
        }

        private Mesh BuildPreviewBushMesh()
        {
            if (leafMesh == null)
            {
                return null;
            }

            var sourceVertices = leafMesh.vertices;
            var sourceNormals = leafMesh.normals;
            var sourceUvs = leafMesh.uv;
            var sourceTriangles = leafMesh.triangles;
            if (sourceVertices == null || sourceVertices.Length == 0 || sourceTriangles == null || sourceTriangles.Length == 0)
            {
                Debug.LogWarning("Bush Generator needs a leaf mesh with vertices and triangles.");
                return null;
            }

            var sourceBounds = leafMesh.bounds;
            var sourceHeight = Mathf.Max(0.001f, sourceBounds.size.y);
            var sourceWidth = Mathf.Max(0.001f, sourceBounds.size.x);
            var normalizedLeafHeight = 0.34f;
            var baseScale = normalizedLeafHeight / sourceHeight;
            if (sourceWidth * baseScale > 0.22f)
            {
                baseScale *= 0.22f / (sourceWidth * baseScale);
            }

            var random = new System.Random(seed);
            var count = Mathf.Clamp(targetLeafCount, 1, 1200);
            var vertices = new List<Vector3>(sourceVertices.Length * count);
            var normals = new List<Vector3>(sourceVertices.Length * count);
            var uvs = new List<Vector2>(sourceVertices.Length * count);
            var colors = new List<Color>(sourceVertices.Length * count);
            var triangles = new List<int>(sourceTriangles.Length * count);

            for (var leaf = 0; leaf < count; leaf++)
            {
                var angle = RandomRange(random, 0.0f, Mathf.PI * 2.0f);
                var radius01 = Mathf.Sqrt(Random01(random));
                var ringNoise = RandomRange(random, 0.78f, 1.08f);
                var localX = Mathf.Cos(angle) * radius * radius01 * ringNoise;
                var localZ = Mathf.Sin(angle) * depth * radius01 * RandomRange(random, 0.78f, 1.12f);
                var centerFactor = 1.0f - radius01;
                var localY = Mathf.Max(0.02f, centerFactor * height + RandomRange(random, -height * 0.08f, height * 0.08f));
                var position = new Vector3(localX, localY, localZ);

                var outward = new Vector3(
                    localX / Mathf.Max(0.001f, radius),
                    Mathf.Lerp(0.18f, 0.72f, centerFactor),
                    localZ / Mathf.Max(0.001f, depth));
                if (outward.sqrMagnitude < 0.001f)
                {
                    outward = Vector3.up;
                }

                outward.Normalize();

                var yaw = Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg + RandomRange(random, -42.0f, 42.0f);
                var pitch = RandomRange(random, 10.0f, 35.0f);
                var roll = RandomRange(random, -20.0f, 20.0f);
                var rotation = Quaternion.Euler(pitch, yaw, roll);
                var scale = baseScale * RandomRange(random, 0.72f, 1.18f);
                var matrix = Matrix4x4.TRS(position, rotation, Vector3.one * scale);

                var topFactor = Mathf.Clamp01(localY / Mathf.Max(0.001f, height));
                var frontFactor = Mathf.InverseLerp(depth, -depth, localZ);
                var outerFactor = Mathf.Clamp01(radius01);
                var depthShade = Mathf.Clamp01(0.32f + topFactor * 0.34f + frontFactor * 0.24f + outerFactor * 0.14f);
                var tint = ChooseLeafTint(depthShade, random);

                var vertexOffset = vertices.Count;
                for (var v = 0; v < sourceVertices.Length; v++)
                {
                    vertices.Add(matrix.MultiplyPoint3x4(sourceVertices[v]));

                    if (sourceNormals != null && sourceNormals.Length == sourceVertices.Length)
                    {
                        normals.Add(matrix.MultiplyVector(sourceNormals[v]).normalized);
                    }
                    else
                    {
                        normals.Add(rotation * Vector3.forward);
                    }

                    uvs.Add(sourceUvs != null && sourceUvs.Length == sourceVertices.Length ? sourceUvs[v] : Vector2.zero);
                    colors.Add(new Color(tint.r, tint.g, tint.b, depthShade));
                }

                for (var t = 0; t < sourceTriangles.Length; t++)
                {
                    triangles.Add(vertexOffset + sourceTriangles[t]);
                }
            }

            var mesh = new Mesh
            {
                name = "Generated Bush Preview Mesh",
                indexFormat = vertices.Count > 65535
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Color ChooseLeafTint(float depthShade, System.Random random)
        {
            var shadow = new Color(0.34f, 0.58f, 0.42f, 1.0f);
            var mid = new Color(0.62f, 0.84f, 0.36f, 1.0f);
            var light = new Color(0.9f, 1.0f, 0.42f, 1.0f);
            var color = depthShade < 0.62f
                ? Color.Lerp(shadow, mid, Mathf.InverseLerp(0.32f, 0.62f, depthShade))
                : Color.Lerp(mid, light, Mathf.InverseLerp(0.62f, 1.0f, depthShade));

            var variation = RandomRange(random, 0.9f, 1.1f);
            return new Color(color.r * variation, color.g * variation, color.b * variation, 1.0f);
        }

        private static float Random01(System.Random random)
        {
            return (float)random.NextDouble();
        }

        private static float RandomRange(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        private static void DestroyUnityObject(Object target)
        {
            if (target == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(target);
                return;
            }
#endif

            Object.Destroy(target);
        }
    }
}
