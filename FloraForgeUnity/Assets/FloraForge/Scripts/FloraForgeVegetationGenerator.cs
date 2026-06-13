using System;
using System.Collections.Generic;
using UnityEngine;

namespace FloraForge
{
    [ExecuteAlways]
    public sealed class FloraForgeVegetationGenerator : MonoBehaviour
    {
        private const string GeneratedRootName = "__FloraForgeGenerated";
        private const int GeneratorVersion = 14;

        [Header("Generation")]
        public int seed = 4751;
        [Range(0.25f, 2.0f)] public float overallScale = 1.0f;
        public bool autoRegenerate = true;
        public bool includeReferenceFrame = true;

        [Header("Vines")]
        [Range(1, 16)] public int climbingVineCount = 8;
        [Range(0, 20)] public int hangingVineCount = 9;
        [Range(0.01f, 0.1f)] public float vineThickness = 0.035f;
        [Range(0.0f, 1.0f)] public float leafDensity = 0.75f;
        [Range(0.0f, 1.0f)] public float flowerDensity = 0.45f;

        [Header("Shrubs")]
        [Range(0, 12)] public int shrubCount = 6;
        [Range(0.25f, 1.8f)] public float shrubRadius = 0.9f;
        [Range(0.0f, 1.0f)] public float shrubFlowerRatio = 0.55f;

        [Header("Wildflowers")]
        [Range(0, 12)] public int wildflowerClumps = 5;
        [Range(0.15f, 1.0f)] public float wildflowerHeight = 0.55f;

        [Header("Grass")]
        [Range(0, 48)] public int grassClumpCount = 28;
        [Range(0.0f, 1.5f)] public float grassDensity = 1.0f;

        [SerializeField, HideInInspector] private int generatedVersion;
        private bool regenerateQueued;

        private void OnEnable()
        {
            if (!Application.isPlaying && autoRegenerate && (generatedVersion != GeneratorVersion || transform.Find(GeneratedRootName) == null))
            {
                QueueRegenerate();
            }
        }

        private void OnValidate()
        {
            if (autoRegenerate)
            {
                QueueRegenerate();
            }
        }

        public void QueueRegenerate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (regenerateQueued)
                {
                    return;
                }

                regenerateQueued = true;
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this == null)
                    {
                        return;
                    }

                    regenerateQueued = false;
                    Regenerate();
                };
                return;
            }
#endif

            Regenerate();
        }

        public void Regenerate()
        {
            ClearGenerated();
            generatedVersion = GeneratorVersion;

            UnityEngine.Random.InitState(seed);
            var rng = new System.Random(seed);
            var materials = new FloraMaterials();

            var root = new GameObject(GeneratedRootName);
            root.transform.SetParent(transform, false);
            root.transform.localScale = Vector3.one * Mathf.Max(0.05f, overallScale);

            if (includeReferenceFrame)
            {
                CreateReferenceFrame(root.transform, materials);
            }

            CreateGrass(root.transform, rng, materials);
            CreateClimbingVines(root.transform, rng, materials);
            CreateHangingVines(root.transform, rng, materials);
            CreateShrubs(root.transform, rng, materials);
            CreateWildflowers(root.transform, rng, materials);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.EditorUtility.SetDirty(gameObject);
            }
#endif
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

        private void CreateReferenceFrame(Transform parent, FloraMaterials materials)
        {
            CreateBox(parent, "Ground", new Vector3(0.0f, -0.04f, -0.45f), new Vector3(8.0f, 0.08f, 3.2f), Quaternion.identity, materials.Ground);
            CreateBox(parent, "Back Wall", new Vector3(0.0f, 1.55f, 0.32f), new Vector3(7.2f, 3.1f, 0.12f), Quaternion.identity, materials.Wall);

            CreateBox(parent, "Left Post", new Vector3(-3.25f, 1.65f, -0.02f), new Vector3(0.34f, 3.3f, 0.34f), Quaternion.Euler(0, 0, -3), materials.WoodDark);
            CreateBox(parent, "Center Post", new Vector3(-0.15f, 1.72f, -0.04f), new Vector3(0.32f, 3.45f, 0.32f), Quaternion.Euler(0, 0, 2), materials.Wood);
            CreateBox(parent, "Right Post", new Vector3(3.15f, 1.7f, -0.02f), new Vector3(0.36f, 3.4f, 0.36f), Quaternion.Euler(0, 0, 4), materials.WoodDark);
            CreateBox(parent, "Top Beam", new Vector3(0.0f, 3.22f, -0.04f), new Vector3(7.2f, 0.28f, 0.38f), Quaternion.Euler(0, 0, 1), materials.WoodDark);
            CreateBox(parent, "Balcony Rail", new Vector3(0.5f, 2.28f, -0.28f), new Vector3(5.3f, 0.18f, 0.24f), Quaternion.Euler(0, 0, -1), materials.Wood);
            CreateBox(parent, "Lower Rail", new Vector3(1.7f, 1.08f, -0.35f), new Vector3(3.6f, 0.22f, 0.24f), Quaternion.identity, materials.WoodDark);
            CreateBox(parent, "Diagonal Brace A", new Vector3(-1.85f, 2.65f, -0.02f), new Vector3(2.35f, 0.2f, 0.25f), Quaternion.Euler(0, 0, 28), materials.Wood);
            CreateBox(parent, "Diagonal Brace B", new Vector3(1.85f, 2.62f, -0.02f), new Vector3(2.1f, 0.2f, 0.25f), Quaternion.Euler(0, 0, -31), materials.Wood);

            CreateBox(parent, "Step 1", new Vector3(-0.65f, 0.08f, -0.78f), new Vector3(1.55f, 0.18f, 0.62f), Quaternion.identity, materials.Wood);
            CreateBox(parent, "Step 2", new Vector3(-0.65f, 0.28f, -0.52f), new Vector3(1.25f, 0.18f, 0.54f), Quaternion.identity, materials.WoodDark);
            CreateBox(parent, "Crate", new Vector3(1.55f, 0.36f, -0.85f), new Vector3(0.8f, 0.72f, 0.72f), Quaternion.Euler(0, 12, 0), materials.Wood);
            CreateCylinder(parent, "Clay Pot", new Vector3(2.3f, 0.28f, -0.58f), new Vector3(0.42f, 0.42f, 0.42f), materials.Pot);
        }

        private void CreateClimbingVines(Transform parent, System.Random rng, FloraMaterials materials)
        {
            var anchors = new[]
            {
                new Vector3(-3.35f, 0.0f, -0.22f),
                new Vector3(-1.15f, 0.0f, -0.2f),
                new Vector3(-0.18f, 0.0f, -0.25f),
                new Vector3(2.65f, 0.0f, -0.22f),
                new Vector3(3.25f, 0.0f, -0.18f)
            };

            for (var i = 0; i < climbingVineCount; i++)
            {
                var start = anchors[i % anchors.Length] + RandomXZ(rng, 0.18f);
                var end = start + new Vector3(RandomRange(rng, -0.65f, 0.65f), RandomRange(rng, 2.35f, 3.55f), RandomRange(rng, -0.08f, 0.16f));
                if (i % 3 == 2)
                {
                    end.x += RandomRange(rng, -1.2f, 1.2f);
                    end.y = RandomRange(rng, 2.5f, 3.25f);
                }

                var path = BuildVinePath(start, end, RandomRange(rng, 0.25f, 0.6f), 14, rng);
                CreateTube(parent, "Climbing Vine", path, vineThickness * RandomRange(rng, 0.5f, 0.78f), 7, materials.Vine, 1.0f, 0.42f, 0.08f);
                ScatterLeaves(parent, path, Mathf.RoundToInt(RandomRange(rng, 16, 28) * leafDensity), RandomRange(rng, 0.12f, 0.2f), rng, materials);
                ScatterFlowers(parent, path, Mathf.RoundToInt(RandomRange(rng, 2, 7) * flowerDensity), rng, materials, materials.FlowerPink, materials.FlowerPurple);
            }
        }

        private void CreateHangingVines(Transform parent, System.Random rng, FloraMaterials materials)
        {
            for (var i = 0; i < hangingVineCount; i++)
            {
                var start = new Vector3(RandomRange(rng, -3.3f, 3.45f), RandomRange(rng, 2.85f, 3.42f), RandomRange(rng, -0.38f, -0.16f));
                var end = start + new Vector3(RandomRange(rng, -0.25f, 0.25f), -RandomRange(rng, 0.7f, 1.7f), RandomRange(rng, -0.08f, 0.1f));
                var path = BuildVinePath(start, end, RandomRange(rng, 0.12f, 0.35f), 10, rng);
                CreateTube(parent, "Hanging Vine", path, vineThickness * RandomRange(rng, 0.34f, 0.58f), 6, materials.Vine, 0.95f, 0.2f, 0.04f);
                ScatterLeaves(parent, path, Mathf.RoundToInt(RandomRange(rng, 8, 17) * leafDensity), RandomRange(rng, 0.1f, 0.17f), rng, materials);

                if (Random01(rng) < flowerDensity)
                {
                    ScatterFlowers(parent, path, RandomRangeInt(rng, 1, 3), rng, materials, materials.FlowerPink, materials.FlowerPurple);
                }
            }
        }

        private void CreateShrubs(Transform parent, System.Random rng, FloraMaterials materials)
        {
            for (var i = 0; i < shrubCount; i++)
            {
                var x = Mathf.Lerp(-3.35f, 3.35f, shrubCount <= 1 ? 0.5f : i / (float)(shrubCount - 1));
                x += RandomRange(rng, -0.35f, 0.35f);
                var center = new Vector3(x, 0.02f, RandomRange(rng, -1.35f, -0.55f));
                var stemCount = RandomRangeInt(rng, 9, 16);

                for (var s = 0; s < stemCount; s++)
                {
                    var angle = RandomRange(rng, 0.0f, Mathf.PI * 2.0f);
                    var length = RandomRange(rng, 0.35f, 1.05f) * shrubRadius;
                    var end = center + new Vector3(Mathf.Cos(angle) * length * 0.55f, RandomRange(rng, 0.3f, 0.9f), Mathf.Sin(angle) * length * 0.35f);
                    var path = BuildVinePath(center + RandomXZ(rng, 0.08f), end, RandomRange(rng, 0.05f, 0.18f), 5, rng);
                    CreateTube(parent, "Shrub Stem", path, vineThickness * RandomRange(rng, 0.24f, 0.42f), 5, materials.Vine, 0.95f, 0.24f, 0.05f);
                    ScatterLeaves(parent, path, RandomRangeInt(rng, 3, 8), RandomRange(rng, 0.14f, 0.25f), rng, materials);

                    if (Random01(rng) < shrubFlowerRatio)
                    {
                        CreateFlower(parent, end, RandomRange(rng, 0.08f, 0.14f), rng, materials, Random01(rng) < 0.5f ? materials.FlowerPink : materials.FlowerPurple);
                    }
                }
            }
        }

        private void CreateWildflowers(Transform parent, System.Random rng, FloraMaterials materials)
        {
            for (var i = 0; i < wildflowerClumps; i++)
            {
                var center = new Vector3(RandomRange(rng, -2.4f, 2.7f), 0.02f, RandomRange(rng, -1.25f, -0.42f));
                var count = RandomRangeInt(rng, 7, 15);

                for (var s = 0; s < count; s++)
                {
                    var start = center + RandomXZ(rng, 0.2f);
                    var end = start + new Vector3(RandomRange(rng, -0.06f, 0.06f), RandomRange(rng, 0.25f, wildflowerHeight), RandomRange(rng, -0.04f, 0.04f));
                    var path = BuildVinePath(start, end, 0.04f, 3, rng);
                    var stemTangent = (end - start).normalized;
                    var leafNormal = RandomRadialAround(stemTangent, rng);
                    var leafUp = (Vector3.up * RandomRange(rng, 0.45f, 0.75f) + leafNormal * RandomRange(rng, 0.35f, 0.65f)).normalized;
                    var leafSide = Vector3.Cross(leafUp, leafNormal);
                    if (leafSide.sqrMagnitude < 0.001f)
                    {
                        leafSide = Vector3.right;
                    }

                    leafSide.Normalize();
                    CreateTube(parent, "Wildflower Stem", path, vineThickness * RandomRange(rng, 0.12f, 0.2f), 5, materials.Vine, 0.9f, 0.36f, 0.03f);
                    CreateLeafCard(parent, Vector3.Lerp(start, end, 0.45f) + leafNormal * RandomRange(rng, 0.015f, 0.04f), leafUp, leafSide, RandomRange(rng, 0.08f, 0.13f), rng, materials);
                    CreateFlower(parent, end, RandomRange(rng, 0.05f, 0.09f), rng, materials, materials.FlowerYellow);
                }
            }
        }

        private void CreateGrass(Transform parent, System.Random rng, FloraMaterials materials)
        {
            if (grassClumpCount <= 0 || grassDensity <= 0.0f)
            {
                return;
            }

            var effectiveClumpCount = Mathf.CeilToInt(grassClumpCount * 1.25f);
            for (var i = 0; i < effectiveClumpCount; i++)
            {
                var center = new Vector3(
                    RandomRange(rng, -3.45f, 3.45f),
                    0.02f,
                    RandomRange(rng, -1.75f, -0.45f));
                var clumpRadius = RandomRange(rng, 0.22f, 0.46f);
                var bladeCount = Mathf.RoundToInt(RandomRange(rng, 16, 34) * grassDensity);
                CreateGrassClump(parent, center, clumpRadius, bladeCount, rng, materials.Grass);
            }
        }

        private static List<Vector3> BuildVinePath(Vector3 start, Vector3 end, float sway, int segments, System.Random rng)
        {
            var points = new List<Vector3>(segments + 1);
            var lateral = Vector3.Cross((end - start).normalized, Vector3.up);
            if (lateral.sqrMagnitude < 0.001f)
            {
                lateral = Vector3.right;
            }

            lateral.Normalize();
            lateral *= RandomSigned(rng) * sway;
            var depth = Vector3.forward * RandomRange(rng, -sway * 0.35f, sway * 0.35f);

            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var bend = Mathf.Sin(t * Mathf.PI);
                var ripple = Mathf.Sin((t * 3.0f + RandomRange(rng, -0.2f, 0.2f)) * Mathf.PI) * sway * 0.18f;
                points.Add(Vector3.Lerp(start, end, t) + lateral * bend + depth * bend + Vector3.right * ripple);
            }

            return points;
        }

        private static void ScatterLeaves(Transform parent, IReadOnlyList<Vector3> path, int count, float size, System.Random rng, FloraMaterials materials)
        {
            if (count <= 0 || path.Count < 2)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var t = RandomRange(rng, 0.06f, 0.96f);
                var sample = SamplePath(path, t, out var tangent);
                var tangentDirection = tangent.sqrMagnitude < 0.001f ? Vector3.up : tangent.normalized;
                var leafNormal = RandomRadialAround(tangentDirection, rng);
                var leafUp = (leafNormal * RandomRange(rng, 0.55f, 0.9f) + Vector3.up * RandomRange(rng, 0.25f, 0.55f) + tangentDirection * RandomRange(rng, 0.05f, 0.22f)).normalized;
                var side = Vector3.Cross(leafUp, leafNormal);
                if (side.sqrMagnitude < 0.001f)
                {
                    side = Vector3.right;
                }

                side.Normalize();
                CreateLeafCard(parent, sample + leafNormal * RandomRange(rng, 0.03f, 0.08f), leafUp, side, size * RandomRange(rng, 0.7f, 1.35f), rng, materials);
            }
        }

        private static void ScatterFlowers(Transform parent, IReadOnlyList<Vector3> path, int count, System.Random rng, FloraMaterials materials, Material primary, Material secondary)
        {
            if (count <= 0 || path.Count < 2)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var position = SamplePath(path, RandomRange(rng, 0.18f, 0.98f), out _) + RandomXZ(rng, 0.08f) + Vector3.up * RandomRange(rng, 0.0f, 0.08f);
                CreateFlower(parent, position, RandomRange(rng, 0.08f, 0.15f), rng, materials, Random01(rng) < 0.6f ? primary : secondary);
            }
        }

        private static void CreateGrassClump(Transform parent, Vector3 center, float radius, int bladeCount, System.Random rng, Material material)
        {
            if (bladeCount <= 0)
            {
                return;
            }

            const int segments = 4;
            var frontVertices = new List<Vector3>(bladeCount * (segments + 1) * 2);
            var uvs = new List<Vector2>(frontVertices.Capacity);
            var triangles = new List<int>(bladeCount * segments * 6);

            for (var blade = 0; blade < bladeCount; blade++)
            {
                var angle = RandomRange(rng, 0.0f, Mathf.PI * 2.0f);
                var distance = Mathf.Sqrt(Random01(rng)) * radius;
                var basePoint = center + new Vector3(Mathf.Cos(angle) * distance, 0.0f, Mathf.Sin(angle) * distance * 0.58f);
                var yaw = RandomRange(rng, 0.0f, Mathf.PI * 2.0f);
                var side = new Vector3(Mathf.Cos(yaw), 0.0f, Mathf.Sin(yaw)).normalized;
                var bendDirection = new Vector3(-side.z, 0.0f, side.x).normalized;
                bendDirection = (bendDirection + RandomXZ(rng, 0.6f)).normalized;

                var height = RandomRange(rng, 0.2f, 0.52f);
                var width = RandomRange(rng, 0.026f, 0.055f);
                var bend = RandomRange(rng, 0.025f, 0.15f);
                var twist = RandomSigned(rng) * RandomRange(rng, 0.0f, 0.06f);
                var startIndex = frontVertices.Count;

                for (var y = 0; y <= segments; y++)
                {
                    var t = y / (float)segments;
                    var taper = Mathf.Pow(1.0f - t, 1.35f);
                    var bladeSide = (side + bendDirection * twist * t).normalized;
                    var rowCenter = basePoint + Vector3.up * (height * t);
                    rowCenter += bendDirection * (bend * t * t);
                    rowCenter += side * (Mathf.Sin(t * Mathf.PI * 1.35f + blade) * width * 0.16f);
                    var halfWidth = width * Mathf.Max(0.035f, taper) * 0.5f;

                    frontVertices.Add(rowCenter - bladeSide * halfWidth);
                    frontVertices.Add(rowCenter + bladeSide * halfWidth);
                    uvs.Add(new Vector2(0.0f, t));
                    uvs.Add(new Vector2(1.0f, t));
                }

                for (var y = 0; y < segments; y++)
                {
                    var a = startIndex + y * 2;
                    var b = a + 1;
                    var c = a + 2;
                    var d = a + 3;

                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }

            var mesh = CreateTwoSidedMesh("Grass Clump Mesh", frontVertices.ToArray(), uvs.ToArray(), triangles.ToArray());

            var go = new GameObject("Grass Clump");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void CreateTube(Transform parent, string name, IReadOnlyList<Vector3> points, float radius, int radialSegments, Material material, float baseScale = 1.0f, float tipScale = 0.42f, float midBulge = 0.06f)
        {
            if (points.Count < 2)
            {
                return;
            }

            var ringVertexCount = points.Count * radialSegments;
            var startCenterIndex = ringVertexCount;
            var endCenterIndex = ringVertexCount + 1;
            var vertices = new Vector3[ringVertexCount + 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[(points.Count - 1) * radialSegments * 6 + radialSegments * 6];
            var tri = 0;

            for (var i = 0; i < points.Count; i++)
            {
                var t = i / (float)(points.Count - 1);
                var tangent = GetPathTangent(points, i);
                var rotation = RotationAlong(tangent);
                var taper = Mathf.Lerp(baseScale, tipScale, Mathf.SmoothStep(0.0f, 1.0f, t));
                var organicRipple = Mathf.Sin((t * 4.0f + radius * 31.0f) * Mathf.PI) * 0.025f;
                var ringRadius = radius * Mathf.Max(0.12f, taper + Mathf.Sin(t * Mathf.PI) * midBulge + organicRipple);

                for (var r = 0; r < radialSegments; r++)
                {
                    var angle = r / (float)radialSegments * Mathf.PI * 2.0f;
                    var offset = rotation * new Vector3(Mathf.Cos(angle) * ringRadius, Mathf.Sin(angle) * ringRadius, 0.0f);
                    var vertexIndex = i * radialSegments + r;
                    vertices[vertexIndex] = points[i] + offset;
                    uvs[vertexIndex] = new Vector2(r / (float)radialSegments, t);
                }
            }

            vertices[startCenterIndex] = points[0];
            vertices[endCenterIndex] = points[points.Count - 1];
            uvs[startCenterIndex] = new Vector2(0.5f, 0.0f);
            uvs[endCenterIndex] = new Vector2(0.5f, 1.0f);

            for (var i = 0; i < points.Count - 1; i++)
            {
                for (var r = 0; r < radialSegments; r++)
                {
                    var nextR = (r + 1) % radialSegments;
                    var a = i * radialSegments + r;
                    var b = i * radialSegments + nextR;
                    var c = (i + 1) * radialSegments + r;
                    var d = (i + 1) * radialSegments + nextR;

                    triangles[tri++] = a;
                    triangles[tri++] = c;
                    triangles[tri++] = b;
                    triangles[tri++] = b;
                    triangles[tri++] = c;
                    triangles[tri++] = d;
                }
            }

            for (var r = 0; r < radialSegments; r++)
            {
                var nextR = (r + 1) % radialSegments;
                triangles[tri++] = startCenterIndex;
                triangles[tri++] = nextR;
                triangles[tri++] = r;

                var endA = (points.Count - 1) * radialSegments + r;
                var endB = (points.Count - 1) * radialSegments + nextR;
                triangles[tri++] = endCenterIndex;
                triangles[tri++] = endA;
                triangles[tri++] = endB;
            }

            var mesh = new Mesh { name = name + " Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void CreateLeafCard(Transform parent, Vector3 center, Vector3 up, Vector3 side, float size, System.Random rng, FloraMaterials materials)
        {
            if (side.sqrMagnitude < 0.001f)
            {
                side = Vector3.right;
            }

            if (up.sqrMagnitude < 0.001f)
            {
                up = Vector3.up;
            }

            side.Normalize();
            up.Normalize();
            var surfaceNormal = Vector3.Cross(side, up);
            if (surfaceNormal.sqrMagnitude < 0.001f)
            {
                surfaceNormal = Vector3.forward;
            }

            surfaceNormal.Normalize();
            side = Vector3.Cross(up, surfaceNormal).normalized;

            var leafSize = size * 0.62f;
            var style = RandomRangeInt(rng, 0, 3);
            var material = materials.GetLeafMaterial(style, Random01(rng) < 0.5f);
            var totalLength = leafSize * RandomRange(rng, 1.28f, 1.62f);
            var maxWidth = leafSize * RandomRange(rng, 0.45f, 0.68f);
            if (style == 1)
            {
                maxWidth *= 1.22f;
            }
            else if (style == 2)
            {
                maxWidth *= 0.72f;
                totalLength *= 1.18f;
            }

            var basePoint = center - up * totalLength * 0.36f;
            const int rows = 10;
            const int cols = 9;
            var vertices = new Vector3[(rows + 1) * cols];
            var uvs = new Vector2[vertices.Length];
            var triangles = new List<int>(rows * (cols - 1) * 6);

            for (var y = 0; y <= rows; y++)
            {
                var t = y / (float)rows;
                var bodyMask = LeafBodyMask(t);
                var rowWidth = LeafWidthProfile(t, style, maxWidth);
                var edgeOffset = LeafEdgeOffset(t, y, style, leafSize);
                var rowCenter = basePoint + up * (totalLength * t);
                rowCenter += surfaceNormal * (bodyMask * leafSize * 0.08f + t * t * leafSize * 0.025f);

                for (var x = 0; x < cols; x++)
                {
                    var u = x / (float)(cols - 1);
                    var lateral = (u - 0.5f) * 2.0f;
                    var edgeAmount = Mathf.Abs(lateral);
                    var shapedWidth = rowWidth + (edgeAmount > 0.84f ? edgeOffset : 0.0f);
                    var cup = -edgeAmount * edgeAmount * bodyMask * leafSize * 0.06f;
                    var asymmetry = Mathf.Sin((t * 6.0f + lateral * 1.7f) * Mathf.PI) * leafSize * 0.015f;
                    var index = y * cols + x;
                    vertices[index] = rowCenter + side * (lateral * shapedWidth + asymmetry) + surfaceNormal * cup;
                    uvs[index] = new Vector2(u, t);
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

            var mesh = CreateTwoSidedMesh("Detailed Leaf Mesh", vertices, uvs, triangles.ToArray());

            var go = new GameObject("Leaf");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static float LeafWidthProfile(float t, int style, float maxWidth)
        {
            t = Mathf.Clamp01(t);
            var baseProfile = LeafBodyMask(t);
            if (style == 1)
            {
                var shoulder = LeafBodyMask(Mathf.Clamp01(t * 1.25f));
                return maxWidth * Mathf.Pow(Mathf.Max(baseProfile, shoulder * 0.82f), 0.62f) * Mathf.Lerp(0.72f, 1.0f, t);
            }

            if (style == 2)
            {
                return maxWidth * Mathf.Pow(baseProfile, 0.95f);
            }

            return maxWidth * Mathf.Pow(baseProfile, 0.72f);
        }

        private static float LeafEdgeOffset(float t, int row, int style, float size)
        {
            var bodyMask = LeafBodyMask(t);
            var alternating = row % 2 == 0 ? 1.0f : -1.0f;
            if (style == 1)
            {
                var lowerLobe = Mathf.Clamp01(1.0f - Mathf.Abs(t - 0.32f) / 0.16f);
                var upperLobe = Mathf.Clamp01(1.0f - Mathf.Abs(t - 0.62f) / 0.2f);
                return size * (lowerLobe * 0.1f + upperLobe * 0.055f + alternating * bodyMask * 0.018f);
            }

            if (style == 2)
            {
                return size * alternating * bodyMask * 0.014f;
            }

            return size * alternating * bodyMask * 0.035f;
        }

        private static float LeafBodyMask(float t)
        {
            return Mathf.Max(0.0f, Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI));
        }

        private static void CreateFlower(Transform parent, Vector3 center, float size, System.Random rng, FloraMaterials materials, Material petalMaterial)
        {
            var head = new GameObject("Flower Head");
            head.transform.SetParent(parent, false);

            var normal = (Vector3.up + RandomXZ(rng, 0.45f)).normalized;
            var forward = Vector3.ProjectOnPlane(new Vector3(RandomRange(rng, -1.0f, 1.0f), 0.0f, RandomRange(rng, -1.0f, 1.0f)), normal);
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.ProjectOnPlane(Vector3.forward, normal);
            }

            forward.Normalize();
            var side = Vector3.Cross(normal, forward).normalized;
            var petalCount = RandomRangeInt(rng, 6, 9);

            for (var i = 0; i < petalCount; i++)
            {
                var angle = (i / (float)petalCount) * Mathf.PI * 2.0f + RandomRange(rng, -0.1f, 0.1f);
                var radial = (forward * Mathf.Cos(angle) + side * Mathf.Sin(angle)).normalized;
                CreatePetal(head.transform, center, radial, normal, size * RandomRange(rng, 0.95f, 1.25f), RandomRange(rng, 0.42f, 0.58f), petalMaterial);
            }

            var innerCount = Mathf.Max(4, petalCount - 2);
            for (var i = 0; i < innerCount; i++)
            {
                var angle = ((i + 0.5f) / innerCount) * Mathf.PI * 2.0f + RandomRange(rng, -0.08f, 0.08f);
                var radial = (forward * Mathf.Cos(angle) + side * Mathf.Sin(angle)).normalized;
                CreatePetal(head.transform, center + normal * size * 0.03f, radial, normal, size * RandomRange(rng, 0.55f, 0.75f), RandomRange(rng, 0.32f, 0.45f), petalMaterial);
            }

            CreateFlowerDisc(head.transform, center + normal * size * 0.08f, normal, forward, size * 0.28f, materials.FlowerCenter);
            CreateSepals(head.transform, center - normal * size * 0.05f, normal, forward, side, size * 0.48f, materials.Sepal);
        }

        private static void CreatePetal(Transform parent, Vector3 center, Vector3 outDir, Vector3 normal, float length, float widthScale, Material material)
        {
            outDir.Normalize();
            normal.Normalize();
            var side = Vector3.Cross(normal, outDir).normalized;
            const int lengthSegments = 5;
            const int widthSegments = 3;
            var vertices = new Vector3[(lengthSegments + 1) * widthSegments];
            var uvs = new Vector2[vertices.Length];
            var triangles = new List<int>(lengthSegments * (widthSegments - 1) * 6);

            for (var y = 0; y <= lengthSegments; y++)
            {
                var t = y / (float)lengthSegments;
                var taper = Mathf.Sin(t * Mathf.PI);
                var baseWidth = length * widthScale * Mathf.Lerp(0.18f, 1.0f, taper);
                var cupping = Mathf.Sin(t * Mathf.PI) * length * 0.16f;
                var tipLift = t * t * length * 0.18f;
                var rowCenter = center + outDir * (length * (0.16f + t * 0.84f)) + normal * (cupping + tipLift);

                for (var x = 0; x < widthSegments; x++)
                {
                    var u = x / (float)(widthSegments - 1);
                    var lateral = (u - 0.5f) * baseWidth;
                    var crossCup = -Mathf.Abs(u - 0.5f) * length * 0.06f * Mathf.Sin(t * Mathf.PI);
                    var index = y * widthSegments + x;
                    vertices[index] = rowCenter + side * lateral + normal * crossCup;
                    uvs[index] = new Vector2(u, t);
                }
            }

            for (var y = 0; y < lengthSegments; y++)
            {
                for (var x = 0; x < widthSegments - 1; x++)
                {
                    var a = y * widthSegments + x;
                    var b = y * widthSegments + x + 1;
                    var c = (y + 1) * widthSegments + x;
                    var d = (y + 1) * widthSegments + x + 1;

                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }

            var mesh = CreateTwoSidedMesh("Curved Petal Mesh", vertices, uvs, triangles.ToArray());

            var go = new GameObject("Flower Petal");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void CreateFlowerDisc(Transform parent, Vector3 center, Vector3 normal, Vector3 forward, float radius, Material material)
        {
            normal.Normalize();
            forward = Vector3.ProjectOnPlane(forward, normal).normalized;
            var side = Vector3.Cross(normal, forward).normalized;
            const int segments = 12;
            var vertices = new Vector3[segments + 1];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * 3];
            vertices[0] = center;
            uvs[0] = new Vector2(0.5f, 0.5f);

            for (var i = 0; i < segments; i++)
            {
                var angle = i / (float)segments * Mathf.PI * 2.0f;
                var unit = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vertices[i + 1] = center + (forward * unit.x + side * unit.y) * radius + normal * Mathf.Sin(angle * 3.0f) * radius * 0.05f;
                uvs[i + 1] = unit * 0.5f + Vector2.one * 0.5f;
            }

            var tri = 0;
            for (var i = 0; i < segments; i++)
            {
                var next = i == segments - 1 ? 1 : i + 2;
                triangles[tri++] = 0;
                triangles[tri++] = i + 1;
                triangles[tri++] = next;
            }

            var mesh = CreateTwoSidedMesh("Flower Center Mesh", vertices, uvs, triangles);

            var go = new GameObject("Flower Center");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void CreateSepals(Transform parent, Vector3 center, Vector3 normal, Vector3 forward, Vector3 side, float size, Material material)
        {
            const int sepalCount = 5;
            for (var i = 0; i < sepalCount; i++)
            {
                var angle = i / (float)sepalCount * Mathf.PI * 2.0f;
                var radial = (forward * Mathf.Cos(angle) + side * Mathf.Sin(angle)).normalized;
                CreatePetal(parent, center - normal * size * 0.08f, radial, -normal, size * 0.55f, 0.22f, material);
            }
        }

        private static Mesh CreateTwoSidedMesh(string name, Vector3[] frontVertices, Vector2[] frontUvs, int[] frontTriangles)
        {
            var vertexCount = frontVertices.Length;
            var vertices = new Vector3[vertexCount * 2];
            var uvs = new Vector2[vertexCount * 2];
            var triangles = new int[frontTriangles.Length * 2];

            Array.Copy(frontVertices, 0, vertices, 0, vertexCount);
            Array.Copy(frontVertices, 0, vertices, vertexCount, vertexCount);
            Array.Copy(frontUvs, 0, uvs, 0, vertexCount);
            Array.Copy(frontUvs, 0, uvs, vertexCount, vertexCount);
            Array.Copy(frontTriangles, 0, triangles, 0, frontTriangles.Length);

            for (var i = 0; i < frontTriangles.Length; i += 3)
            {
                var target = frontTriangles.Length + i;
                triangles[target] = frontTriangles[i] + vertexCount;
                triangles[target + 1] = frontTriangles[i + 2] + vertexCount;
                triangles[target + 2] = frontTriangles[i + 1] + vertexCount;
            }

            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 SamplePath(IReadOnlyList<Vector3> path, float t, out Vector3 tangent)
        {
            t = Mathf.Clamp01(t);
            var scaled = t * (path.Count - 1);
            var index = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, path.Count - 2);
            var localT = scaled - index;
            tangent = (path[index + 1] - path[index]).normalized;
            return Vector3.Lerp(path[index], path[index + 1], localT);
        }

        private static Vector3 GetPathTangent(IReadOnlyList<Vector3> points, int index)
        {
            if (index == 0)
            {
                return (points[1] - points[0]).normalized;
            }

            if (index == points.Count - 1)
            {
                return (points[index] - points[index - 1]).normalized;
            }

            return (points[index + 1] - points[index - 1]).normalized;
        }

        private static Quaternion RotationAlong(Vector3 tangent)
        {
            if (tangent.sqrMagnitude < 0.0001f)
            {
                return Quaternion.identity;
            }

            tangent.Normalize();
            var up = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > 0.92f ? Vector3.forward : Vector3.up;
            return Quaternion.LookRotation(tangent, up);
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            RemoveCollider(go);
            return go;
        }

        private static GameObject CreateCylinder(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            RemoveCollider(go);
            return go;
        }

        private static void RemoveCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyUnityObject(collider);
            }
        }

        private static void DestroyUnityObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(target);
                return;
            }
#endif

            UnityEngine.Object.Destroy(target);
        }

        private static Vector3 RandomXZ(System.Random rng, float radius)
        {
            var angle = RandomRange(rng, 0.0f, Mathf.PI * 2.0f);
            var distance = RandomRange(rng, 0.0f, radius);
            return new Vector3(Mathf.Cos(angle) * distance, 0.0f, Mathf.Sin(angle) * distance);
        }

        private static Vector3 RandomRadialAround(Vector3 axis, System.Random rng)
        {
            if (axis.sqrMagnitude < 0.001f)
            {
                axis = Vector3.up;
            }

            axis.Normalize();
            var reference = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.86f ? Vector3.forward : Vector3.up;
            var radialA = Vector3.Cross(axis, reference);
            if (radialA.sqrMagnitude < 0.001f)
            {
                radialA = Vector3.right;
            }

            radialA.Normalize();
            var radialB = Vector3.Cross(axis, radialA).normalized;
            var angle = RandomRange(rng, 0.0f, Mathf.PI * 2.0f);
            return (radialA * Mathf.Cos(angle) + radialB * Mathf.Sin(angle)).normalized;
        }

        private static float RandomRange(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        private static int RandomRangeInt(System.Random rng, int minInclusive, int maxExclusive)
        {
            return rng.Next(minInclusive, maxExclusive);
        }

        private static float Random01(System.Random rng)
        {
            return (float)rng.NextDouble();
        }

        private static float RandomSigned(System.Random rng)
        {
            return Random01(rng) < 0.5f ? -1.0f : 1.0f;
        }

        private sealed class FloraMaterials
        {
            private const string LeafOvateTexturePath = "Assets/FloraForge/Textures/leaf_surface_ovate_uv.png";
            private const string LeafLobedTexturePath = "Assets/FloraForge/Textures/leaf_surface_lobed_uv.png";
            private const string LeafLanceolateTexturePath = "Assets/FloraForge/Textures/leaf_surface_lanceolate_uv.png";
            private const string FlowerPetalPinkTexturePath = "Assets/FloraForge/Textures/flower_petal_pink_uv.png";
            private const string FlowerPetalPurpleTexturePath = "Assets/FloraForge/Textures/flower_petal_purple_uv.png";
            private const string FlowerPetalYellowTexturePath = "Assets/FloraForge/Textures/flower_petal_yellow_uv.png";
            private const string FlowerCenterTexturePath = "Assets/FloraForge/Textures/flower_center_uv.png";

            public readonly Material Vine = Create("FloraForge Vine", new Color(0.18f, 0.28f, 0.12f));
            public readonly Material Grass = Create("FloraForge Grass", Color.white, CreateGrassGradientTexture(), true);
            public readonly Material LeafOvateA = Create("FloraForge Leaf Ovate A", new Color(0.78f, 0.96f, 0.72f), LoadTexture(LeafOvateTexturePath));
            public readonly Material LeafOvateB = Create("FloraForge Leaf Ovate B", new Color(0.62f, 0.82f, 0.58f), LoadTexture(LeafOvateTexturePath));
            public readonly Material LeafLobedA = Create("FloraForge Leaf Lobed A", new Color(0.78f, 0.94f, 0.7f), LoadTexture(LeafLobedTexturePath));
            public readonly Material LeafLobedB = Create("FloraForge Leaf Lobed B", new Color(0.58f, 0.78f, 0.54f), LoadTexture(LeafLobedTexturePath));
            public readonly Material LeafLanceolateA = Create("FloraForge Leaf Lanceolate A", new Color(0.72f, 0.9f, 0.66f), LoadTexture(LeafLanceolateTexturePath));
            public readonly Material LeafLanceolateB = Create("FloraForge Leaf Lanceolate B", new Color(0.52f, 0.72f, 0.5f), LoadTexture(LeafLanceolateTexturePath));
            public readonly Material FlowerPink = Create("FloraForge Flower Pink", new Color(0.95f, 0.28f, 0.58f), LoadTexture(FlowerPetalPinkTexturePath), true);
            public readonly Material FlowerPurple = Create("FloraForge Flower Purple", new Color(0.62f, 0.22f, 0.78f), LoadTexture(FlowerPetalPurpleTexturePath), true);
            public readonly Material FlowerYellow = Create("FloraForge Flower Yellow", new Color(0.96f, 0.82f, 0.18f), LoadTexture(FlowerPetalYellowTexturePath), true);
            public readonly Material FlowerCenter = Create("FloraForge Flower Center", new Color(0.95f, 0.68f, 0.16f), LoadTexture(FlowerCenterTexturePath), true);
            public readonly Material Sepal = Create("FloraForge Sepal", new Color(0.18f, 0.42f, 0.16f));
            public readonly Material Wood = Create("FloraForge Wood", new Color(0.38f, 0.24f, 0.16f));
            public readonly Material WoodDark = Create("FloraForge Dark Wood", new Color(0.25f, 0.16f, 0.11f));
            public readonly Material Wall = Create("FloraForge Wall", new Color(0.36f, 0.31f, 0.25f));
            public readonly Material Ground = Create("FloraForge Ground", new Color(0.18f, 0.23f, 0.12f));
            public readonly Material Pot = Create("FloraForge Pot", new Color(0.62f, 0.34f, 0.2f));

            public Material GetLeafMaterial(int style, bool alternate)
            {
                if (style == 1)
                {
                    return alternate ? LeafLobedB : LeafLobedA;
                }

                if (style == 2)
                {
                    return alternate ? LeafLanceolateB : LeafLanceolateA;
                }

                return alternate ? LeafOvateB : LeafOvateA;
            }

            private static Material Create(string name, Color color, Texture texture = null, bool preserveTextureColor = false)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                var materialColor = texture != null && preserveTextureColor ? Color.white : color;
                var material = new Material(shader)
                {
                    name = name,
                    color = materialColor,
                    enableInstancing = true
                };

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", materialColor);
                }

                if (texture != null)
                {
                    texture.wrapMode = TextureWrapMode.Clamp;
                    texture.filterMode = FilterMode.Bilinear;

                    if (material.HasProperty("_BaseMap"))
                    {
                        material.SetTexture("_BaseMap", texture);
                    }

                    if (material.HasProperty("_MainTex"))
                    {
                        material.SetTexture("_MainTex", texture);
                    }
                }

                if (material.HasProperty("_Smoothness"))
                {
                    material.SetFloat("_Smoothness", 0.18f);
                }

                return material;
            }

            private static Texture2D LoadTexture(string assetPath)
            {
#if UNITY_EDITOR
                var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (texture == null)
                {
                    UnityEditor.AssetDatabase.ImportAsset(assetPath, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
                    texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                }

                return texture;
#else
                return null;
#endif
            }

            private static Texture2D CreateGrassGradientTexture()
            {
                const int width = 16;
                const int height = 96;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = "FloraForge Grass Gradient",
                    hideFlags = HideFlags.DontSave
                };

                var rootColor = new Color(0.08f, 0.2f, 0.07f);
                var bodyColor = new Color(0.22f, 0.42f, 0.12f);
                var tipColor = new Color(0.48f, 0.66f, 0.25f);

                for (var y = 0; y < height; y++)
                {
                    var v = y / (float)(height - 1);
                    var baseColor = v < 0.58f
                        ? Color.Lerp(rootColor, bodyColor, v / 0.58f)
                        : Color.Lerp(bodyColor, tipColor, (v - 0.58f) / 0.42f);

                    for (var x = 0; x < width; x++)
                    {
                        var u = x / (float)(width - 1);
                        var lateral = Mathf.Abs(u - 0.5f) * 2.0f;
                        var centerHighlight = (1.0f - lateral) * Mathf.Sin(v * Mathf.PI) * 0.12f;
                        var edgeShade = lateral * 0.1f;
                        var noise = (Mathf.PerlinNoise(u * 7.0f, v * 16.0f) - 0.5f) * 0.06f;
                        var color = baseColor;
                        color.r = Mathf.Clamp01(color.r + centerHighlight + noise - edgeShade * 0.45f);
                        color.g = Mathf.Clamp01(color.g + centerHighlight + noise - edgeShade * 0.25f);
                        color.b = Mathf.Clamp01(color.b + noise - edgeShade * 0.35f);
                        color.a = 1.0f;
                        texture.SetPixel(x, y, color);
                    }
                }

                texture.Apply(false, true);
                return texture;
            }
        }
    }
}
