using UnityEngine;
using Unity.Entities;
using TinyWalnutGames.MetVD.Core;

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
            
            // Add BiomeArtProfileReference component
            AddComponent(entity, new BiomeArtProfileReference
            {
                ProfileRef = new UnityObjectRef<BiomeArtProfile> { Value = authoring.artProfile },
                IsApplied = false,
                ProjectionType = authoring.projectionType
            });
            
            // Auto-configure or use override biome type
            BiomeType biomeType = authoring.biomeTypeOverride;
            if (authoring.autoConfigureBiomeType && authoring.artProfile != null)
            {
                biomeType = InferBiomeTypeFromProfileName(authoring.artProfile.biomeName);
            }
            
            // Add or update Biome component if it doesn't exist
            if (!HasComponent<Biome>(entity))
            {
                AddComponent(entity, new Biome(biomeType, Polarity.None));
            }
        }
        
        private BiomeType InferBiomeTypeFromProfileName(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
                return BiomeType.Unknown;
                
            string lowerName = profileName.ToLower();
            
            // Simple name matching - could be expanded with more sophisticated logic
            if (lowerName.Contains("solar") || lowerName.Contains("sun"))
                return BiomeType.SolarPlains;
            else if (lowerName.Contains("crystal"))
                return BiomeType.CrystalCaverns;
            else if (lowerName.Contains("sky") || lowerName.Contains("garden"))
                return BiomeType.SkyGardens;
            else if (lowerName.Contains("shadow"))
                return BiomeType.ShadowRealms;
            else if (lowerName.Contains("underwater") || lowerName.Contains("ocean"))
                return BiomeType.DeepUnderwater;
            else if (lowerName.Contains("void"))
                return BiomeType.VoidChambers;
            else if (lowerName.Contains("volcanic") || lowerName.Contains("volcano"))
                return BiomeType.VolcanicCore;
            else if (lowerName.Contains("power") || lowerName.Contains("plant"))
                return BiomeType.PowerPlant;
            else if (lowerName.Contains("plasma"))
                return BiomeType.PlasmaFields;
            else if (lowerName.Contains("frozen") || lowerName.Contains("ice"))
                return BiomeType.FrozenWastes;
            else if (lowerName.Contains("catacomb"))
                return BiomeType.IceCatacombs;
            else if (lowerName.Contains("cryogenic") || lowerName.Contains("lab"))
                return BiomeType.CryogenicLabs;
            else if (lowerName.Contains("hub"))
                return BiomeType.HubArea;
            else if (lowerName.Contains("transition"))
                return BiomeType.TransitionZone;
            else if (lowerName.Contains("ancient") || lowerName.Contains("ruin"))
                return BiomeType.AncientRuins;
            else
                return BiomeType.Unknown;
        }
    }
}