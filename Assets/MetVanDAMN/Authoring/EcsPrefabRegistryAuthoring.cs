using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring
    {
    [DisallowMultipleComponent]
    public sealed class EcsPrefabRegistryAuthoring : MonoBehaviour
        {
        [Serializable]
        public struct Entry
            {
            public string Key;
            public GameObject Prefab;
            }

        [Tooltip("Action key to prefab mappings for ECS instantiation.")]
        public List<Entry> Entries = new();

        [Header("Gizmo Prefab Auto-Generation (Editor)")]
        [Tooltip("When enabled, you can generate lightweight gizmo prefabs for keys that don’t have a prefab yet.")]
        public bool EnableGizmoPrefabGeneration = false;

        public enum GizmoShape { Cube, Sphere, Icon }

        [Tooltip("Gizmo shape for auto-generated debug prefabs.")]
        public GizmoShape GeneratedGizmoShape = GizmoShape.Cube;

        [Tooltip("Uniform gizmo size for auto-generated debug prefabs.")]
        public float GeneratedGizmoSize = 1.0f;

        [Tooltip("Color for auto-generated gizmo prefabs.")]
        public Color GeneratedGizmoColor = new(0.2f, 0.9f, 0.3f, 0.9f);

        [Tooltip("Folder to save generated gizmo prefabs.")]
        public string GeneratedPrefabsFolder = "Assets/MetVanDAMN/Debug/GizmoPrefabs";

        [Tooltip("If true, will replace existing prefab assignments with generated gizmo prefabs.")]
        public bool OverwriteExistingWithGizmos = false;

        public class Baker : Baker<EcsPrefabRegistryAuthoring>
            {
            public override void Bake(EcsPrefabRegistryAuthoring authoring)
                {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<EcsPrefabRegistry>(entity);
                var buffer = AddBuffer<EcsPrefabEntry>(entity);
                if (authoring.Entries == null) return;
                foreach (var e in authoring.Entries)
                    {
                    if (string.IsNullOrWhiteSpace(e.Key) || e.Prefab == null) continue;
                    var prefabEntity = GetEntity(e.Prefab, TransformUsageFlags.None);
                    buffer.Add(new EcsPrefabEntry
                        {
                        Key = new Unity.Collections.FixedString64Bytes(e.Key),
                        Prefab = prefabEntity
                        });
                    }
                }
            }
        }
    }
