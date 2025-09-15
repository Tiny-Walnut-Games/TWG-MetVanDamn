using Unity.Collections;
using Unity.Entities;

namespace TinyWalnutGames.MetVD.Core
    {
    /// Tag component for the ECS prefab registry singleton
    public struct EcsPrefabRegistry : IComponentData { }

    /// Buffer entries mapping action keys to entity prefabs
    public struct EcsPrefabEntry : IBufferElementData
        {
        public FixedString64Bytes Key;
        public Entity Prefab;
        }
    }
