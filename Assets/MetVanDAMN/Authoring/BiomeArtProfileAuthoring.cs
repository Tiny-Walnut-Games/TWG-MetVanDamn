using UnityEngine;
using Unity.Entities;
using TinyWalnutGames.MetVD.Core; // Provides BiomeType, Polarity, Biome struct
// using TinyWalnutGames.MetVD.Biome; // Removed to avoid 'Biome' namespace/type ambiguity

// Alias to disambiguate biome component explicitly (in case other namespaces introduce Biome symbol)
using CoreBiome = TinyWalnutGames.MetVD.Core.Biome;

namespace TinyWalnutGames.MetVD.Authoring
{
    /// <summary>
    /// Authoring component for linking BiomeArtProfile assets to biome entities
    /// Place this component on GameObjects that represent biomes in the scene
    /// </summary>
    public class BiomeArtProfileAuthoring : MonoBehaviour
    {
        [Header("Biome Art Configuration")]
        [Tooltip("The BiomeArtProfile asset to use for this biome")]
        public BiomeArtProfile artProfile;
        
        [Tooltip("Projection type for tilemap generation")]
        public ProjectionType projectionType = ProjectionType.TopDown;
        
        [Header("Auto-Configuration")]
        [Tooltip("Automatically configure biome type based on art profile name")]
        public bool autoConfigureBiomeType = true;
        
        [Tooltip("Override biome type (if auto-configuration is disabled)")]
        public BiomeType biomeTypeOverride = BiomeType.Unknown;
    }

    /// <summary>
    /// Baker for converting BiomeArtProfileAuthoring to ECS components
    /// </summary>
    public class BiomeArtProfileBaker : Baker<BiomeArtProfileAuthoring>
    {
        public override void Bake(BiomeArtProfileAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            
            AddComponent(entity, new BiomeArtProfileReference
            {
                ProfileRef = new UnityObjectRef<BiomeArtProfile> { Value = authoring.artProfile },
                IsApplied = false,
                ProjectionType = authoring.projectionType
            });
            
            BiomeType biomeType = authoring.biomeTypeOverride;
            if (authoring.autoConfigureBiomeType && authoring.artProfile != null)
            {
                biomeType = InferBiomeTypeFromProfileName(authoring.artProfile.biomeName);
            }
            
            // Safe add/update of biome component without relying on HasComponent (not exposed in some Baker API versions)
            var biomeComponent = new CoreBiome(biomeType, Polarity.None);
            try
            {
                AddComponent(entity, biomeComponent);
            }
            catch (System.InvalidOperationException)
            {
                // Component likely already added by another baker; attempt to update
                try
                {
                    SetComponent(entity, biomeComponent);
                }
                catch
                {
                    // Swallow if SetComponent not available in this Entities version
                }
            }
        }
        
        private BiomeType InferBiomeTypeFromProfileName(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
                return BiomeType.Unknown;
                
            string lowerName = profileName.ToLower();
            
            if (lowerName.Contains("solar") || lowerName.Contains("sun")) return BiomeType.SolarPlains;
            if (lowerName.Contains("crystal")) return BiomeType.CrystalCaverns;
            if (lowerName.Contains("sky") || lowerName.Contains("garden")) return BiomeType.SkyGardens;
            if (lowerName.Contains("shadow")) return BiomeType.ShadowRealms;
            if (lowerName.Contains("underwater") || lowerName.Contains("ocean")) return BiomeType.DeepUnderwater;
            if (lowerName.Contains("void")) return BiomeType.VoidChambers;
            if (lowerName.Contains("volcanic") || lowerName.Contains("volcano")) return BiomeType.VolcanicCore;
            if (lowerName.Contains("power") || lowerName.Contains("plant")) return BiomeType.PowerPlant;
            if (lowerName.Contains("plasma")) return BiomeType.PlasmaFields;
            if (lowerName.Contains("frozen") || lowerName.Contains("ice")) return BiomeType.FrozenWastes;
            if (lowerName.Contains("catacomb")) return BiomeType.IceCatacombs;
            if (lowerName.Contains("cryogenic") || lowerName.Contains("lab")) return BiomeType.CryogenicLabs;
            if (lowerName.Contains("hub")) return BiomeType.HubArea;
            if (lowerName.Contains("transition")) return BiomeType.TransitionZone;
            if (lowerName.Contains("ancient") || lowerName.Contains("ruin")) return BiomeType.AncientRuins;
            return BiomeType.Unknown;
        }
    }
}
