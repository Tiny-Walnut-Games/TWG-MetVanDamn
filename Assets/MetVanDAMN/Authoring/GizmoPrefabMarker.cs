using UnityEngine;

namespace TinyWalnutGames.MetVanDAMN.Authoring
    {
    [DisallowMultipleComponent]
    public sealed class GizmoPrefabMarker : MonoBehaviour
        {
        [Tooltip("Registry key this gizmo represents.")]
        public string Key;

        [Tooltip("Shape used for gizmo drawing in the Scene view.")]
        public EcsPrefabRegistryAuthoring.GizmoShape Shape = EcsPrefabRegistryAuthoring.GizmoShape.Cube;

        [Tooltip("Uniform size used for gizmo drawing.")]
        public float Size = 1.0f;

        [Tooltip("Gizmo color in the Scene view.")]
        public Color Color = new(0.2f, 0.9f, 0.3f, 0.9f);

        private void OnDrawGizmos()
            {
			Color prev = Gizmos.color;
            Gizmos.color = Color;
			Vector3 pos = transform.position;

            switch (Shape)
                {
                case EcsPrefabRegistryAuthoring.GizmoShape.Cube:
                    Gizmos.DrawWireCube(pos, Vector3.one * Size);
                    break;
                case EcsPrefabRegistryAuthoring.GizmoShape.Sphere:
                    Gizmos.DrawWireSphere(pos, Size * 0.5f);
                    break;
                case EcsPrefabRegistryAuthoring.GizmoShape.Icon:
#if UNITY_EDITOR
                    // Use a simple crosshair-like icon using Handles so we don't depend on any asset path.
                    UnityEditor.Handles.color = Color;
                    float r = Size * 0.5f;
                    UnityEditor.Handles.DrawLine(pos + Vector3.left * r, pos + Vector3.right * r);
                    UnityEditor.Handles.DrawLine(pos + Vector3.forward * r, pos + Vector3.back * r);
                    UnityEditor.Handles.DrawLine(pos + Vector3.up * r, pos + Vector3.down * r);
#else
                    Gizmos.DrawWireSphere(pos, Size * 0.5f);
#endif
                    break;
                }

#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(Key))
                {
                UnityEditor.Handles.color = new Color(Color.r, Color.g, Color.b, 1f);
                UnityEditor.Handles.Label(pos + Vector3.up * (Size * 0.6f), Key);
                }
#endif

            Gizmos.color = prev;
            }
        }
    }
