using System.Collections.Generic;
using UnityEngine;

namespace FloraForge
{
    [ExecuteAlways]
    public sealed class FloraForgeBushGenerator : MonoBehaviour
    {
        private const string GeneratedRootName = "__FloraForgeBushGenerated";
        private const int GeneratorVersion = 11;

        [Header("Leaf Assets")]
        public Mesh leafMesh;
        public Texture2D leafTexture;
        public Material leafMaterialOverride;

        [Header("Volume Source")]
        public Mesh volumeMesh;
        public bool showVolumeGuide;

        [Header("Bush Volume")]
        [Min(0.1f)] public float radius = 1.2f;
        [Min(0.1f)] public float height = 0.95f;
        [Min(0.1f)] public float depth = 0.85f;

        [Header("Future Placement")]
        public int seed = 190619;
        [Min(0)] public int targetLeafCount = 320;
        [Range(0.0f, 1.0f)] public float surfaceFullness = 0.72f;

        [Header("Leaf Shape")]
        [Range(0.05f, 0.6f)] public float leafDisplayLength = 0.22f;
        [Range(0.0f, 0.4f)] public float leafBendOutward = 0.12f;
        [Range(0.0f, 0.4f)] public float leafTipDroop = 0.08f;

        [Header("Leaf Clusters")]
        [Min(1)] public int minLeavesPerCluster = 5;
        [Min(1)] public int maxLeavesPerCluster = 10;
        [Range(0.0f, 0.35f)] public float clusterSpread = 0.14f;
        [Range(0.0f, 1.0f)] public float clusterFan = 0.62f;

        [Header("Layer Depth")]
        [Range(0.0f, 0.35f)] public float layerShellSeparation = 0.18f;
        [Range(0.0f, 0.35f)] public float layerFrontBackSeparation = 0.16f;
        [Range(0.0f, 0.25f)] public float layerHeightSeparation = 0.08f;

        [Header("Crown Fill")]
        [Range(0.0f, 0.5f)] public float crownFillFraction = 0.24f;
        [Range(0.05f, 0.75f)] public float crownFillRadius = 0.48f;

        [Header("Debug")]
        public bool debugSpectrumTint = true;

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
            leafDisplayLength = Mathf.Max(0.05f, leafDisplayLength);
            minLeavesPerCluster = Mathf.Max(1, minLeavesPerCluster);
            maxLeavesPerCluster = Mathf.Max(minLeavesPerCluster, maxLeavesPerCluster);
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

            if (showVolumeGuide && volumeMesh != null)
            {
                Gizmos.color = new Color(0.55f, 0.95f, 0.45f, 0.55f);
                Gizmos.DrawWireMesh(volumeMesh, Vector3.zero, Quaternion.identity, new Vector3(radius, height, depth));
            }

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
            var material = ResolveLeafMaterial();
            if (material == null)
            {
                Debug.LogWarning("Bush Generator needs a leaf material or a texture that can be assigned to a generated material.");
                return;
            }

            var mesh = BuildPreviewBushMesh();
            if (mesh == null)
            {
                return;
            }

            var meshObject = new GameObject("Bush Preview Mesh");
            meshObject.transform.SetParent(parent, false);
            meshObject.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = meshObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }

        private Material ResolveLeafMaterial()
        {
            var material = leafMaterialOverride;
            if (material == null)
            {
                var shader = Shader.Find("FloraForge/Bush Leaf Vertex Color");
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Lit");
                }

                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                if (shader == null)
                {
                    return null;
                }

                material = new Material(shader)
                {
                    name = "Generated Bush Leaf Material"
                };
            }

            ApplyLeafTexture(material);
            return material;
        }

        private void ApplyLeafTexture(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (leafTexture != null && material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", leafTexture);
            }

            if (leafTexture != null && material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", leafTexture);
            }

            if (material.HasProperty("_DebugVertexColor"))
            {
                material.SetFloat("_DebugVertexColor", debugSpectrumTint ? 1.0f : 0.0f);
            }

#if UNITY_EDITOR
            if (leafMaterialOverride != null && material == leafMaterialOverride)
            {
                UnityEditor.EditorUtility.SetDirty(material);
            }
#endif
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
            var sourceMinY = sourceBounds.min.y;
            var sourceMaxY = sourceBounds.max.y;
            var volumeSamples = BuildVolumeSamples();
            var baseScale = leafDisplayLength / sourceHeight;
            if (sourceWidth * baseScale > leafDisplayLength * 0.82f)
            {
                baseScale *= leafDisplayLength * 0.82f / (sourceWidth * baseScale);
            }

            var random = new System.Random(seed);
            var count = Mathf.Clamp(targetLeafCount, 1, 20000);
            var fullnessSpread = Mathf.Lerp(0.72f, 1.08f, surfaceFullness);
            var vertices = new List<Vector3>(sourceVertices.Length * count);
            var normals = new List<Vector3>(sourceVertices.Length * count);
            var uvs = new List<Vector2>(sourceVertices.Length * count);
            var colors = new List<Color>(sourceVertices.Length * count);
            var triangles = new List<int>(sourceTriangles.Length * count);

            var leaf = 0;
            while (leaf < count)
            {
                var crownCluster = Random01(random) < crownFillFraction;
                var layerRoll = Random01(random);
                var layer = crownCluster ? 2 : layerRoll < 0.32f ? 0 : layerRoll < 0.78f ? 1 : 2;
                var clusterSize = Mathf.Min(count - leaf, random.Next(minLeavesPerCluster, maxLeavesPerCluster + 1));
                var clusterSample = crownCluster
                    ? SampleCrownTop(random)
                    : volumeSamples.Count > 0
                    ? SampleVolume(volumeSamples, layer, random)
                    : SampleFallbackDome(layer, random);

                var clusterRadial = new Vector3(clusterSample.Normal.x, 0.0f, clusterSample.Normal.z);
                if (clusterRadial.sqrMagnitude < 0.001f)
                {
                    clusterRadial = new Vector3(
                        clusterSample.Position.x / Mathf.Max(0.001f, radius),
                        0.0f,
                        clusterSample.Position.z / Mathf.Max(0.001f, depth));
                }

                if (clusterRadial.sqrMagnitude < 0.001f)
                {
                    clusterRadial = RandomHorizontal(random);
                }

                clusterRadial.Normalize();

                var clusterTangent = Vector3.Cross(Vector3.up, clusterRadial);
                if (clusterTangent.sqrMagnitude < 0.001f)
                {
                    clusterTangent = RandomHorizontal(random);
                }

                clusterTangent.Normalize();

                var clusterNormal = clusterSample.Normal.sqrMagnitude > 0.001f ? clusterSample.Normal.normalized : Vector3.up;
                var layerDepth = layer == 0 ? -1.0f : layer == 1 ? 0.0f : 1.0f;
                var layerZBias = layer == 0 ? depth * layerFrontBackSeparation : layer == 2 ? -depth * layerFrontBackSeparation : 0.0f;
                var layerYBias = layer == 0 ? -height * layerHeightSeparation : layer == 2 ? height * layerHeightSeparation : 0.0f;
                var layerShellBias = clusterNormal * (layerDepth * layerShellSeparation * Mathf.Max(radius, depth));
                var clusterBase = clusterSample.Position
                    + layerShellBias
                    + new Vector3(0.0f, layerYBias, layerZBias)
                    - clusterRadial * RandomRange(random, 0.015f, 0.055f) * Mathf.Max(radius, depth);
                var branchLean = crownCluster
                    ? (clusterRadial * RandomRange(random, 0.52f, 0.9f) + Vector3.up * RandomRange(random, 0.08f, 0.28f)).normalized
                    : (clusterRadial * RandomRange(random, 0.45f, 0.85f) + Vector3.up * RandomRange(random, -0.04f, 0.18f)).normalized;
                var fanStart = crownCluster ? RandomRange(random, -0.5f, 0.05f) : RandomRange(random, -0.75f, 0.25f);
                var fanStep = clusterSize <= 1 ? 0.0f : 1.0f / (clusterSize - 1);

                for (var clusterLeaf = 0; clusterLeaf < clusterSize; clusterLeaf++, leaf++)
                {
                    var fan01 = clusterSize <= 1 ? 0.5f : clusterLeaf * fanStep;
                    var fanOffset = (fanStart + fan01) * clusterFan;
                    var sideOffset = fanOffset * clusterSpread * fullnessSpread * Mathf.Max(radius, depth);
                    var forwardOffset = (crownCluster ? 0.04f : 0.18f + fan01 * 0.82f) * clusterSpread * fullnessSpread * Mathf.Max(radius, depth);
                    var randomOffset = RandomRange(random, -0.025f, 0.025f) * Mathf.Max(radius, depth);
                    var sampledCenter = clusterBase
                        + clusterRadial * forwardOffset
                        + clusterTangent * sideOffset
                        + clusterNormal * randomOffset;

                    var localX = sampledCenter.x;
                    var localY = sampledCenter.y;
                    var localZ = sampledCenter.z;
                    var radius01 = Mathf.Clamp01(new Vector2(localX / Mathf.Max(0.001f, radius), localZ / Mathf.Max(0.001f, depth)).magnitude);
                    var radial = (clusterRadial + clusterTangent * fanOffset * 0.38f).normalized;
                    var tangentAround = Vector3.Cross(Vector3.up, radial);
                    if (tangentAround.sqrMagnitude < 0.001f)
                    {
                        tangentAround = clusterTangent;
                    }

                    tangentAround.Normalize();

                    var radialFlow = Mathf.Lerp(0.24f, 0.62f, radius01);
                    var swirl = fanOffset * 0.42f + RandomRange(random, -0.12f, 0.12f);
                    var surfaceNormal = (clusterNormal + radial * 0.16f).normalized;
                    var leafUp = (branchLean * 0.45f + radial * radialFlow + tangentAround * swirl + Vector3.up * RandomRange(random, -0.04f, 0.12f)).normalized;

                    var normalSeed = Vector3.Lerp(surfaceNormal, Vector3.up, 0.28f);
                    normalSeed += tangentAround * fanOffset * 0.28f;
                    var leafForward = Vector3.ProjectOnPlane(normalSeed, leafUp);
                    if (leafForward.sqrMagnitude < 0.001f)
                    {
                        leafForward = Vector3.ProjectOnPlane(Vector3.up, leafUp);
                    }

                    leafForward.Normalize();
                    var leafRight = Vector3.Cross(leafUp, leafForward);
                    if (leafRight.sqrMagnitude < 0.001f)
                    {
                        leafRight = tangentAround;
                    }

                    leafRight.Normalize();
                    leafForward = Vector3.Cross(leafRight, leafUp).normalized;

                    var roll = (fanOffset * 18.0f + RandomRange(random, -12.0f, 12.0f)) * Mathf.Deg2Rad;
                    var rolledRight = leafRight * Mathf.Cos(roll) + leafForward * Mathf.Sin(roll);
                    var rolledForward = Vector3.Cross(rolledRight.normalized, leafUp).normalized;

                    var layerScale = layer == 0 ? RandomRange(random, 0.95f, 1.22f) : layer == 1 ? RandomRange(random, 0.78f, 1.05f) : RandomRange(random, 0.56f, 0.84f);
                    var scale = baseScale * layerScale * Mathf.Lerp(0.82f, 1.08f, fan01);
                    var shellOffset = layer == 0 ? -0.04f : layer == 1 ? 0.0f : 0.04f;
                    var position = sampledCenter + surfaceNormal * (shellOffset * Mathf.Max(radius, depth)) - leafUp * (sourceBounds.center.y * scale);

                    var topFactor = Mathf.Clamp01(localY / Mathf.Max(0.001f, height));
                    var frontFactor = Mathf.InverseLerp(depth, -depth, localZ);
                    var outerFactor = Mathf.Clamp01(radius01);
                    var depthShade = Mathf.Clamp01(0.22f + topFactor * 0.26f + frontFactor * 0.28f + outerFactor * 0.16f + layer * 0.08f);
                    var tint = ChooseLeafTint(depthShade, layer, random);
                    var bendDirection = Vector3.Lerp(surfaceNormal, radial, 0.68f).normalized;

                    var vertexOffset = vertices.Count;
                    for (var v = 0; v < sourceVertices.Length; v++)
                    {
                        var sourceVertex = sourceVertices[v];
                        var length01 = Mathf.InverseLerp(sourceMinY, sourceMaxY, sourceVertex.y);
                        var bendProfile = Mathf.Pow(Mathf.Clamp01(length01), 1.65f);
                        var sideProfile = Mathf.Abs(sourceVertex.x) / Mathf.Max(0.001f, sourceWidth * 0.5f);
                        var cupProfile = Mathf.Sin(Mathf.Clamp01(length01) * Mathf.PI) * sideProfile * sideProfile;
                        var bendOffset = bendDirection * (leafBendOutward * bendProfile * scale);
                        bendOffset += Vector3.down * (leafTipDroop * bendProfile * scale);
                        bendOffset -= rolledForward * (0.035f * cupProfile * scale);

                        vertices.Add(position
                            + rolledRight * (sourceVertex.x * scale)
                            + leafUp * (sourceVertex.y * scale)
                            + rolledForward * (sourceVertex.z * scale)
                            + bendOffset);

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

        private List<VolumeSample> BuildVolumeSamples()
        {
            var samples = new List<VolumeSample>();
            if (volumeMesh == null)
            {
                return samples;
            }

            var volumeVertices = volumeMesh.vertices;
            var volumeNormals = volumeMesh.normals;
            var volumeTriangles = volumeMesh.triangles;
            if (volumeVertices == null || volumeVertices.Length == 0 || volumeTriangles == null || volumeTriangles.Length == 0)
            {
                return samples;
            }

            var scale = new Vector3(radius, height, depth);
            for (var i = 0; i < volumeTriangles.Length; i += 3)
            {
                var ia = volumeTriangles[i];
                var ib = volumeTriangles[i + 1];
                var ic = volumeTriangles[i + 2];
                var a = Vector3.Scale(volumeVertices[ia], scale);
                var b = Vector3.Scale(volumeVertices[ib], scale);
                var c = Vector3.Scale(volumeVertices[ic], scale);
                var normal = Vector3.Cross(b - a, c - a);
                var area = normal.magnitude * 0.5f;
                if (area <= 0.000001f)
                {
                    continue;
                }

                normal.Normalize();
                if (volumeNormals != null && volumeNormals.Length == volumeVertices.Length)
                {
                    var na = Vector3.Scale(volumeNormals[ia], new Vector3(1.0f / Mathf.Max(0.001f, radius), 1.0f / Mathf.Max(0.001f, height), 1.0f / Mathf.Max(0.001f, depth))).normalized;
                    var nb = Vector3.Scale(volumeNormals[ib], new Vector3(1.0f / Mathf.Max(0.001f, radius), 1.0f / Mathf.Max(0.001f, height), 1.0f / Mathf.Max(0.001f, depth))).normalized;
                    var nc = Vector3.Scale(volumeNormals[ic], new Vector3(1.0f / Mathf.Max(0.001f, radius), 1.0f / Mathf.Max(0.001f, height), 1.0f / Mathf.Max(0.001f, depth))).normalized;
                    normal = (na + nb + nc).normalized;
                }

                if (normal.y < -0.35f)
                {
                    continue;
                }

                samples.Add(new VolumeSample(a, b, c, normal, area));
            }

            return samples;
        }

        private VolumeSamplePoint SampleVolume(IReadOnlyList<VolumeSample> samples, int layer, System.Random random)
        {
            var totalArea = 0.0f;
            for (var i = 0; i < samples.Count; i++)
            {
                var weight = samples[i].Area * LayerSampleWeight(samples[i], layer);
                totalArea += weight;
            }

            var pick = RandomRange(random, 0.0f, Mathf.Max(0.0001f, totalArea));
            var accum = 0.0f;
            var selected = samples[0];
            for (var i = 0; i < samples.Count; i++)
            {
                accum += samples[i].Area * LayerSampleWeight(samples[i], layer);
                if (accum >= pick)
                {
                    selected = samples[i];
                    break;
                }
            }

            var r1 = Mathf.Sqrt(Random01(random));
            var r2 = Random01(random);
            var u = 1.0f - r1;
            var v = r1 * (1.0f - r2);
            var w = r1 * r2;
            var position = selected.A * u + selected.B * v + selected.C * w;
            position += selected.Normal * RandomRange(random, -0.025f, 0.025f) * Mathf.Max(radius, depth);
            return new VolumeSamplePoint(position, selected.Normal);
        }

        private float LayerSampleWeight(VolumeSample sample, int layer)
        {
            var center = (sample.A + sample.B + sample.C) / 3.0f;
            var top = Mathf.Clamp01(center.y / Mathf.Max(0.001f, height));
            var front = Mathf.InverseLerp(depth * 0.65f, -depth * 0.65f, center.z);

            if (layer == 0)
            {
                var backBias = Mathf.Lerp(0.18f, 1.65f, 1.0f - front);
                var lowerBias = Mathf.Lerp(1.45f, 0.42f, top);
                return backBias * lowerBias;
            }

            if (layer == 2)
            {
                var frontBias = Mathf.Lerp(0.22f, 1.8f, front);
                var upperBias = Mathf.Lerp(0.55f, 1.55f, top);
                return frontBias * upperBias;
            }

            var middleDepth = 1.0f - Mathf.Abs(front - 0.5f) * 1.15f;
            var middleHeight = 1.0f - Mathf.Abs(top - 0.55f) * 0.85f;
            return Mathf.Max(0.25f, middleDepth * middleHeight);
        }

        private VolumeSamplePoint SampleCrownTop(System.Random random)
        {
            var angle = RandomRange(random, 0.0f, Mathf.PI * 2.0f);
            var radius01 = Mathf.Sqrt(Random01(random)) * crownFillRadius;
            var localX = Mathf.Cos(angle) * radius * radius01 * RandomRange(random, 0.72f, 1.08f);
            var localZ = Mathf.Sin(angle) * depth * radius01 * RandomRange(random, 0.72f, 1.08f);
            var localY = height * RandomRange(random, 0.82f, 0.98f);

            var radial = new Vector3(
                localX / Mathf.Max(0.001f, radius),
                0.0f,
                localZ / Mathf.Max(0.001f, depth));
            if (radial.sqrMagnitude < 0.001f)
            {
                radial = RandomHorizontal(random);
            }

            radial.Normalize();
            var normal = (Vector3.up * 0.82f + radial * 0.28f).normalized;
            return new VolumeSamplePoint(new Vector3(localX, localY, localZ), normal);
        }

        private VolumeSamplePoint SampleFallbackDome(int layer, System.Random random)
        {
            var angle = RandomRange(random, 0.0f, Mathf.PI * 2.0f);
            var radius01 = layer == 0
                ? RandomRange(random, 0.22f, 0.88f)
                : layer == 1
                    ? RandomRange(random, 0.32f, 1.0f)
                    : RandomRange(random, 0.12f, 0.86f);
            radius01 = Mathf.Pow(radius01, layer == 2 ? 0.78f : 0.92f);

            var localX = Mathf.Cos(angle) * radius * radius01 * RandomRange(random, 0.82f, 1.08f);
            var localZ = Mathf.Sin(angle) * depth * radius01 * RandomRange(random, 0.82f, 1.08f);
            if (layer == 0)
            {
                localZ += depth * RandomRange(random, 0.06f, 0.22f);
            }
            else if (layer == 2)
            {
                localZ -= depth * RandomRange(random, 0.08f, 0.28f);
            }

            localZ = Mathf.Clamp(localZ, -depth * 1.02f, depth * 1.02f);
            var dome = Mathf.Sqrt(Mathf.Max(0.0f, 1.0f - radius01 * radius01));
            var layerHeightOffset = layer == 0 ? -0.08f : layer == 2 ? 0.08f : 0.0f;
            var localY = height * (0.14f + Mathf.Pow(dome, 0.72f) * 0.62f + layerHeightOffset);
            localY += RandomRange(random, -height * 0.075f, height * 0.065f);
            localY = Mathf.Clamp(localY, height * 0.1f, height * 0.82f);
            var position = new Vector3(localX, localY, localZ);
            var normal = new Vector3(
                localX / Mathf.Max(0.001f, radius * radius),
                (localY - height * 0.08f) / Mathf.Max(0.001f, height * height),
                localZ / Mathf.Max(0.001f, depth * depth)).normalized;
            return new VolumeSamplePoint(position, normal);
        }

        private Color ChooseLeafTint(float depthShade, int layer, System.Random random)
        {
            if (debugSpectrumTint)
            {
                var layerBase = layer == 0 ? 0.0f : layer == 1 ? 0.34f : 0.66f;
                var hue = Mathf.Repeat(layerBase + depthShade * 0.16f, 1.0f);
                return Color.HSVToRGB(hue, 1.0f, 1.0f);
            }

            var shadow = new Color(0.22f, 0.42f, 0.34f, 1.0f);
            var mid = new Color(0.55f, 0.78f, 0.28f, 1.0f);
            var light = new Color(0.86f, 0.98f, 0.34f, 1.0f);
            var color = depthShade < 0.62f
                ? Color.Lerp(shadow, mid, Mathf.InverseLerp(0.32f, 0.62f, depthShade))
                : Color.Lerp(mid, light, Mathf.InverseLerp(0.62f, 1.0f, depthShade));

            if (layer == 0)
            {
                color = Color.Lerp(color, shadow, 0.42f);
            }
            else if (layer == 2)
            {
                color = Color.Lerp(color, light, 0.36f);
            }

            var variation = RandomRange(random, 0.88f, 1.12f);
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

        private readonly struct VolumeSample
        {
            public readonly Vector3 A;
            public readonly Vector3 B;
            public readonly Vector3 C;
            public readonly Vector3 Normal;
            public readonly float Area;

            public VolumeSample(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, float area)
            {
                A = a;
                B = b;
                C = c;
                Normal = normal;
                Area = area;
            }
        }

        private readonly struct VolumeSamplePoint
        {
            public readonly Vector3 Position;
            public readonly Vector3 Normal;

            public VolumeSamplePoint(Vector3 position, Vector3 normal)
            {
                Position = position;
                Normal = normal;
            }
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
