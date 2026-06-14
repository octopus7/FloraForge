using UnityEngine;

namespace FloraForge
{
    [ExecuteAlways]
    public sealed class FloraForgeBushGenerator : MonoBehaviour
    {
        private const string GeneratedRootName = "__FloraForgeBushGenerated";
        private const int GeneratorVersion = 1;

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
            CreateGeneratedRoot();
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
