using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    [CreateAssetMenu(fileName = "MetVDGizmoSettings", menuName = "MetVanDAMN/Debug/Gizmo Settings", order = 10)]
    public class MetVDGizmoSettings : ScriptableObject
    {
        [Header("District Bounds Visuals")] public Color districtColor = new Color(0.2f,0.8f,1f,0.35f);
        public Color districtOutline = new Color(0.1f,0.6f,0.9f,1f);
        public Vector2 districtSize = new(6,6);

        [Header("Connection Visuals")] public Color connectionColor = new(0.9f,0.9f,0.3f,1f);
        public Color oneWayColor = new(1f,0.5f,0.2f,1f);
        public float connectionArrowSize = 0.75f;
        public float connectionWidth = 2f;

        [Header("Biome Field Visuals")] public Color biomePrimary = new(0.3f,1f,0.3f,0.25f);
        public Color biomeSecondary = new(1f,0.3f,0.9f,0.2f);
        public float biomeRadius = 4f;

        [Header("Labels")] public Color labelColor = Color.white;
        public int labelFontSize = 11;

        [Header("Grid Mapping (Authoring -> Preview)")]
        [Tooltip("World units per district grid cell.")] public float gridCellSize = 8f;
        [Tooltip("Offset applied to all grid-mapped gizmos.")] public Vector3 gridOriginOffset = Vector3.zero;
        [Tooltip("If true, gizmos are drawn at (gridCoordinates * cellSize) instead of raw transform.position.")] public bool useGridCoordinatesForGizmos = true;
        [Tooltip("If true, a utility button can snap transform positions to grid when pressed (no automatic move).")] public bool enableTransformSnapUtility = true;
        [Tooltip("When snapping transforms, also resize districtSize to match cell size")] public bool adaptDistrictSizeToCell = true;

        [Header("Utility")] public bool drawInEditMode = true;
        public bool drawInPlayMode = true;
    }
}
