using System;
using System.Collections.Generic;
using UnityEngine;

namespace FloraForge
{
    [ExecuteAlways]
    public sealed class FloraForgeVegetationGenerator : MonoBehaviour
    {
        private const string GeneratedRootName = "__FloraForgeGenerated";

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

        private bool regenerateQueued;

        private void OnEnable()
        {
            if (!Application.isPlaying && autoRegenerate && transform.Find(GeneratedRootName) == null)
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

            CreateClimbingVines(root.transform, rng, materials);
            CreateHangingVines(root.transform, rng, materials);
            CreateShrubs(root.transform, rng, materials);
            CreateWildflowers(root.transform, rng, materials);
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
                CreateTube(parent, "Climbing Vine", path, vineThickness * RandomRange(rng, 0.75f, 1.25f), 7, materials.Vine);
                ScatterLeaves(parent, path, Mathf.RoundToInt(RandomRange(rng, 16, 28) * leafDensity), RandomRange(rng, 0.12f, 0.2f), rng, materials);
                ScatterFlowers(parent, path, Mathf.RoundToInt(RandomRange(rng, 2, 7) * flowerDensity), rng, materials.FlowerPink, materials.FlowerPurple);
            }
        }

        private void CreateHangingVines(Transform parent, System.Random rng, FloraMaterials materials)
        {
            for (var i = 0; i < hangingVineCount; i++)
            {
                var start = new Vector3(RandomRange(rng, -3.3f, 3.45f), RandomRange(rng, 2.85f, 3.42f), RandomRange(rng, -0.38f, -0.16f));
                var end = start + new Vector3(RandomRange(rng, -0.25f, 0.25f), -RandomRange(rng, 0.7f, 1.7f), RandomRange(rng, -0.08f, 0.1f));
                var path = BuildVinePath(start, end, RandomRange(rng, 0.12f, 0.35f), 10, rng);
                CreateTube(parent, "Hanging Vine", path, vineThickness * RandomRange(rng, 0.5f, 0.95f), 6, materials.Vine);
                ScatterLeaves(parent, path, Mathf.RoundToInt(RandomRange(rng, 8, 17) * leafDensity), RandomRange(rng, 0.1f, 0.17f), rng, materials);

                if (Random01(rng) < flowerDensity)
                {
                    ScatterFlowers(parent, path, RandomRangeInt(rng, 1, 3), rng, materials.FlowerPink, materials.FlowerPurple);
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
                    CreateTube(parent, "Shrub Stem", path, vineThickness * RandomRange(rng, 0.45f, 0.8f), 5, materials.Vine);
                    ScatterLeaves(parent, path, RandomRangeInt(rng, 3, 8), RandomRange(rng, 0.14f, 0.25f), rng, materials);

                    if (Random01(rng) < shrubFlowerRatio)
                    {
                        CreateFlower(parent, end, RandomRange(rng, 0.08f, 0.14f), rng, Random01(rng) < 0.5f ? materials.FlowerPink : materials.FlowerPurple);
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
                    CreateTube(parent, "Wildflower Stem", path, vineThickness * 0.34f, 5, materials.Vine);
                    CreateLeafCard(parent, Vector3.Lerp(start, end, 0.45f), Vector3.up, RandomSigned(rng) * Vector3.right, RandomRange(rng, 0.08f, 0.13f), Random01(rng) < 0.5f ? materials.LeafA : materials.LeafB);
                    CreateFlower(parent, end, RandomRange(rng, 0.05f, 0.09f), rng, materials.FlowerYellow);
                }
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
                var side = Vector3.Cross(tangent, Vector3.up);
                if (side.sqrMagnitude < 0.001f)
                {
                    side = Vector3.right;
                }

                side.Normalize();
                side *= RandomSigned(rng);
                var normal = Vector3.Lerp(Vector3.up, tangent.normalized, 0.25f).normalized;
                CreateLeafCard(parent, sample + side * RandomRange(rng, 0.03f, 0.08f), normal, side, size * RandomRange(rng, 0.7f, 1.35f), Random01(rng) < 0.55f ? materials.LeafA : materials.LeafB);
            }
        }

        private static void ScatterFlowers(Transform parent, IReadOnlyList<Vector3> path, int count, System.Random rng, Material primary, Material secondary)
        {
            if (count <= 0 || path.Count < 2)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var position = SamplePath(path, RandomRange(rng, 0.18f, 0.98f), out _) + RandomXZ(rng, 0.08f) + Vector3.up * RandomRange(rng, 0.0f, 0.08f);
                CreateFlower(parent, position, RandomRange(rng, 0.08f, 0.15f), rng, Random01(rng) < 0.6f ? primary : secondary);
            }
        }

        private static void CreateTube(Transform parent, string name, IReadOnlyList<Vector3> points, float radius, int radialSegments, Material material)
        {
            if (points.Count < 2)
            {
                return;
            }

            var vertices = new Vector3[points.Count * radialSegments];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[(points.Count - 1) * radialSegments * 6];
            var tri = 0;

            for (var i = 0; i < points.Count; i++)
            {
                var tangent = GetPathTangent(points, i);
                var rotation = RotationAlong(tangent);
                var ringRadius = radius * Mathf.Lerp(1.15f, 0.65f, i / (float)(points.Count - 1));

                for (var r = 0; r < radialSegments; r++)
                {
                    var angle = r / (float)radialSegments * Mathf.PI * 2.0f;
                    var offset = rotation * new Vector3(Mathf.Cos(angle) * ringRadius, Mathf.Sin(angle) * ringRadius, 0.0f);
                    var vertexIndex = i * radialSegments + r;
                    vertices[vertexIndex] = points[i] + offset;
                    uvs[vertexIndex] = new Vector2(r / (float)radialSegments, i / (float)(points.Count - 1));
                }
            }

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

        private static void CreateLeafCard(Transform parent, Vector3 center, Vector3 up, Vector3 side, float size, Material material)
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

            var width = size * 0.55f;
            var vertices = new[]
            {
                center - side * width * 0.45f - up * size * 0.18f,
                center + up * size,
                center + side * width * 0.45f - up * size * 0.18f,
                center - up * size * 0.42f
            };

            var mesh = new Mesh { name = "Leaf Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 2, 1, 0, 3, 2, 0 };
            mesh.uv = new[] { new Vector2(0.0f, 0.0f), new Vector2(0.5f, 1.0f), new Vector2(1.0f, 0.0f), new Vector2(0.5f, 0.0f) };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject("Leaf");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void CreateFlower(Transform parent, Vector3 center, float size, System.Random rng, Material material)
        {
            const int petalCount = 5;
            for (var i = 0; i < petalCount; i++)
            {
                var angle = (i / (float)petalCount) * Mathf.PI * 2.0f + RandomRange(rng, -0.12f, 0.12f);
                var radial = new Vector3(Mathf.Cos(angle), RandomRange(rng, 0.1f, 0.35f), Mathf.Sin(angle)).normalized;
                var side = Vector3.Cross(radial, Vector3.up);
                if (side.sqrMagnitude < 0.001f)
                {
                    side = Vector3.right;
                }

                CreatePetal(parent, center + radial * size * 0.42f, radial, side.normalized, size * RandomRange(rng, 0.75f, 1.15f), material);
            }
        }

        private static void CreatePetal(Transform parent, Vector3 center, Vector3 outDir, Vector3 side, float size, Material material)
        {
            outDir.Normalize();
            side.Normalize();

            var vertices = new[]
            {
                center - side * size * 0.25f,
                center + outDir * size,
                center + side * size * 0.25f,
                center - outDir * size * 0.28f
            };

            var mesh = new Mesh { name = "Petal Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 2, 1, 0, 3, 2, 0 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject("Flower Petal");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
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
            public readonly Material Vine = Create("FloraForge Vine", new Color(0.18f, 0.28f, 0.12f));
            public readonly Material LeafA = Create("FloraForge Leaf A", new Color(0.18f, 0.38f, 0.17f));
            public readonly Material LeafB = Create("FloraForge Leaf B", new Color(0.11f, 0.25f, 0.12f));
            public readonly Material FlowerPink = Create("FloraForge Flower Pink", new Color(0.95f, 0.28f, 0.58f));
            public readonly Material FlowerPurple = Create("FloraForge Flower Purple", new Color(0.62f, 0.22f, 0.78f));
            public readonly Material FlowerYellow = Create("FloraForge Flower Yellow", new Color(0.96f, 0.82f, 0.18f));
            public readonly Material Wood = Create("FloraForge Wood", new Color(0.38f, 0.24f, 0.16f));
            public readonly Material WoodDark = Create("FloraForge Dark Wood", new Color(0.25f, 0.16f, 0.11f));
            public readonly Material Wall = Create("FloraForge Wall", new Color(0.36f, 0.31f, 0.25f));
            public readonly Material Ground = Create("FloraForge Ground", new Color(0.18f, 0.23f, 0.12f));
            public readonly Material Pot = Create("FloraForge Pot", new Color(0.62f, 0.34f, 0.2f));

            private static Material Create(string name, Color color)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                var material = new Material(shader)
                {
                    name = name,
                    color = color,
                    enableInstancing = true
                };

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }

                if (material.HasProperty("_Smoothness"))
                {
                    material.SetFloat("_Smoothness", 0.18f);
                }

                return material;
            }
        }
    }
}
