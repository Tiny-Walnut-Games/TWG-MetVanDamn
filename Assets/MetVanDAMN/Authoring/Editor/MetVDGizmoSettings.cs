#if UNITY_EDITOR
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring.Editor
    {
    /// <summary>
    /// Unified gizmo & biome grid settings (merged duplicate definitions).
    /// ScriptableObject auto-created at a stable path for tooling & drawers.
    /// </summary>
    [CreateAssetMenu(fileName = "MetVDGizmoSettings", menuName = "MetVanDAMN/Debug/Gizmo Settings", order = 10)]
    public sealed class MetVDGizmoSettings : ScriptableObject
        {
        private static MetVDGizmoSettings _instance = null!; // set via lazy load
        public static MetVDGizmoSettings Instance => _instance != null ? _instance : (_instance = LoadOrCreate());

        // ---------------- Grid Alignment ----------------
        [Header("Grid Mapping (Authoring -> Preview)")]
        [Tooltip("World units per district grid cell.")]
        public float gridCellSize = 8f; // prefer smaller default from gizmo drawer duplicate
        [Tooltip("Offset applied to all grid-mapped gizmos.")] public Vector3 gridOriginOffset = Vector3.zero;
        [Tooltip("If true, gizmos use (gridCoordinates * cellSize) positioning.")] public bool useGridCoordinatesForGizmos = true;
        [Tooltip("If true, a utility button can snap transform positions to grid when pressed.")] public bool enableTransformSnapUtility = true;
        [Tooltip("When snapping transforms, also resize districtSize to match cell size")] public bool adaptDistrictSizeToCell = true;

        // ---------------- District Visualization ----------------
        [Header("District Bounds Visuals")] public Color districtColor = new(0.2f, 0.8f, 1f, 0.35f);
        public Color districtOutline = new(0.1f, 0.6f, 0.9f, 1f);
        public Color placedDistrictColor = new(0.1f, 0.9f, 0.8f, 0.35f);
        public Vector2 districtSize = new(6, 6);

        // ---------------- Connections ----------------
        [Header("Connection Visuals")] public Color connectionColor = new(0.9f, 0.9f, 0.3f, 1f);
        public Color oneWayColor = new(1f, 0.5f, 0.2f, 1f);
        public float connectionArrowSize = 0.75f;
        public float connectionWidth = 2f;

        // ---------------- Biome Fields ----------------
        [Header("Biome Field Visuals")] public Color biomePrimary = new(0.3f, 1f, 0.3f, 0.25f);
        public Color biomeSecondary = new(1f, 0.3f, 0.9f, 0.2f);
        public float biomeRadius = 4f;
        [Tooltip("Fallback tint for unspecified biome visuals.")] public Color biomeFallbackTint = new(0.25f, 0.9f, 0.65f, 0.35f);

        // ---------------- Labels ----------------
        [Header("Labels")] public Color labelColor = Color.white;
        public int labelFontSize = 11;

        // ---------------- Utility ----------------
        [Header("Utility")] public bool drawInEditMode = true;
        public bool drawInPlayMode = true;

        private const string DefaultAssetPath = "Assets/MetVanDAMN/Authoring/Editor/MetVDGizmoSettings.asset";

        private static MetVDGizmoSettings LoadOrCreate()
            {
            var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<MetVDGizmoSettings>(DefaultAssetPath);
            if (loaded != null) return loaded;
            var inst = CreateInstance<MetVDGizmoSettings>();
            var folder = System.IO.Path.GetDirectoryName(DefaultAssetPath);
            if (!string.IsNullOrEmpty(folder) && !System.IO.Directory.Exists(folder))
                {
                System.IO.Directory.CreateDirectory(folder);
                }
            UnityEditor.AssetDatabase.CreateAsset(inst, DefaultAssetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            return inst;
            }
        }
    }
#endif
