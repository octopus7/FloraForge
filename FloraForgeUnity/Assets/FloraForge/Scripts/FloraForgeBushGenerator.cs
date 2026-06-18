using System.Collections.Generic;
using UnityEngine;

namespace FloraForge
{
    [ExecuteAlways]
    public sealed class FloraForgeBushGenerator : MonoBehaviour
    {
        private const string GeneratedRootName = "__FloraForgeBushGenerated";
        private const int GeneratorVersion = 3;

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
            var normalizedLeafHeight = 0.26f;
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
                var radius01 = Mathf.Pow(Random01(random), 0.46f);
                var ringNoise = RandomRange(random, 0.72f, 1.08f);
                var localX = Mathf.Cos(angle) * radius * radius01 * ringNoise;
                var localZ = Mathf.Sin(angle) * depth * radius01 * RandomRange(random, 0.72f, 1.1f);
                var centerFactor = 1.0f - radius01;
                var localY = height * Mathf.Lerp(0.32f, 0.82f, Mathf.Pow(centerFactor, 0.55f));
                localY += RandomRange(random, -height * 0.055f, height * 0.055f);
                localY = Mathf.Clamp(localY, height * 0.22f, height * 0.9f);

                var radial = new Vector3(
                    localX / Mathf.Max(0.001f, radius),
                    0.0f,
                    localZ / Mathf.Max(0.001f, depth));
                if (radial.sqrMagnitude < 0.001f)
                {
                    radial = RandomHorizontal(random);
                }

                radial.Normalize();

                var spread = Mathf.Lerp(0.42f, 1.05f, radius01);
                var droop = Mathf.Lerp(0.1f, 0.72f, radius01);
                var lift = centerFactor * 0.18f;
                var leafUp = (radial * spread + Vector3.down * droop + Vector3.up * lift).normalized;

                var normalSeed = Vector3.Lerp(Vector3.back, radial + Vector3.up * 0.45f, 0.42f);
                normalSeed += RandomHorizontal(random) * 0.16f;
                var leafForward = Vector3.ProjectOnPlane(normalSeed, leafUp);
                if (leafForward.sqrMagnitude < 0.001f)
                {
                    leafForward = Vector3.ProjectOnPlane(Vector3.back, leafUp);
                }

                leafForward.Normalize();
                var leafRight = Vector3.Cross(leafUp, leafForward);
                if (leafRight.sqrMagnitude < 0.001f)
                {
                    leafRight = Vector3.right;
                }

                leafRight.Normalize();
                leafForward = Vector3.Cross(leafRight, leafUp).normalized;

                var roll = RandomRange(random, -24.0f, 24.0f) * Mathf.Deg2Rad;
                var rolledRight = leafRight * Mathf.Cos(roll) + leafForward * Mathf.Sin(roll);
                var rolledForward = Vector3.Cross(rolledRight.normalized, leafUp).normalized;

                var scale = baseScale * RandomRange(random, 0.66f, 1.05f);
                var position = new Vector3(localX, localY, localZ) - leafUp * sourceBounds.min.y * scale;

                var topFactor = Mathf.Clamp01(localY / Mathf.Max(0.001f, height));
                var frontFactor = Mathf.InverseLerp(depth, -depth, localZ);
                var outerFactor = Mathf.Clamp01(radius01);
                var depthShade = Mathf.Clamp01(0.28f + topFactor * 0.28f + frontFactor * 0.23f + outerFactor * 0.16f);
                var tint = ChooseLeafTint(depthShade, random);

                var vertexOffset = vertices.Count;
                for (var v = 0; v < sourceVertices.Length; v++)
                {
                    var sourceVertex = sourceVertices[v];
                    vertices.Add(position
                        + rolledRight * (sourceVertex.x * scale)
                        + leafUp * (sourceVertex.y * scale)
                        + rolledForward * (sourceVertex.z * scale));

                    if (sourceNormals != null && sourceNormals.Length == sourceVertices.Length)
                    {
                        var sourceNormal = sourceNormals[v];
                        normals.Add((rolledRight * sourceNormal.x + leafUp * sourceNormal.y + rolledForward * sourceNormal.z).normalized);
                    }
                    else
                    {
                        normals.Add(rolledForward);
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

        private static Vector3 RandomHorizontal(System.Random random)
        {
            var angle = RandomRange(random, 0.0f, Mathf.PI * 2.0f);
            return new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle));
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
