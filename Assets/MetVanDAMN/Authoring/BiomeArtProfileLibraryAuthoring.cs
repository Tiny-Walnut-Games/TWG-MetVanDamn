using Unity.Entities;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring
    {
    /// <summary>
    /// Authoring MonoBehaviour to reference a BiomeArtProfileLibrary in scene; baked into a singleton component.
    /// </summary>
    public class BiomeArtProfileLibraryAuthoring : MonoBehaviour
        {
        public BiomeArtProfileLibrary library;
        }

    public struct BiomeArtProfileLibraryRef : IComponentData
        {
        public UnityObjectRef<BiomeArtProfileLibrary> Library;
        }

    public class BiomeArtProfileLibraryBaker : Baker<BiomeArtProfileLibraryAuthoring>
        {
        public override void Bake(BiomeArtProfileLibraryAuthoring authoring)
            {
			Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new BiomeArtProfileLibraryRef
                {
                Library = new UnityObjectRef<BiomeArtProfileLibrary> { Value = authoring.library }
                });
            }
        }
    }
