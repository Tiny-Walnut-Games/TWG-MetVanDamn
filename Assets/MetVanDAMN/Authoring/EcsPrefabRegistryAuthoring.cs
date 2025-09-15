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
