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
            Entity entity = GetEntity(TransformUsageFlags.None);
            
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
            {
                return BiomeType.Unknown;
            }

            string lowerName = profileName.ToLower();
            
            // 🔥 FIXED: Complete mapping for all 27 biome types
            
            // Light-aligned biomes
            if (lowerName.Contains("solar") || lowerName.Contains("sun") || lowerName.Contains("plain"))
            {
                return BiomeType.SolarPlains;
            }

            if (lowerName.Contains("crystal") && lowerName.Contains("cavern"))
            {
                return BiomeType.CrystalCaverns;
            }

            if (lowerName.Contains("sky") || lowerName.Contains("garden") || lowerName.Contains("floating"))
            {
                return BiomeType.SkyGardens;
            }

            // Dark-aligned biomes
            if (lowerName.Contains("shadow") || lowerName.Contains("realm"))
            {
                return BiomeType.ShadowRealms;
            }

            if (lowerName.Contains("underwater") || lowerName.Contains("deep") && lowerName.Contains("water"))
            {
                return BiomeType.DeepUnderwater;
            }

            if (lowerName.Contains("void") || lowerName.Contains("chamber"))
            {
                return BiomeType.VoidChambers;
            }

            // Hazard/Energy biomes
            if (lowerName.Contains("volcanic") && lowerName.Contains("core"))
            {
                return BiomeType.VolcanicCore;
            }

            if (lowerName.Contains("power") || lowerName.Contains("plant") || lowerName.Contains("facility"))
            {
                return BiomeType.PowerPlant;
            }

            if (lowerName.Contains("plasma") || lowerName.Contains("field") || lowerName.Contains("energy"))
            {
                return BiomeType.PlasmaFields;
            }

            // Ice/Crystal biomes
            if (lowerName.Contains("frozen") || lowerName.Contains("waste"))
            {
                return BiomeType.FrozenWastes;
            }

            if (lowerName.Contains("ice") && lowerName.Contains("catacomb"))
            {
                return BiomeType.IceCatacombs;
            }

            if (lowerName.Contains("cryogenic") || lowerName.Contains("lab") || lowerName.Contains("cryo"))
            {
                return BiomeType.CryogenicLabs;
            }

            if (lowerName.Contains("icy") && lowerName.Contains("canyon"))
            {
                return BiomeType.IcyCanyon;
            }

            if (lowerName.Contains("tundra") || lowerName.Contains("arctic"))
            {
                return BiomeType.Tundra;
            }

            // Earth/Nature biomes
            if (lowerName.Contains("forest") || lowerName.Contains("wood") || lowerName.Contains("tree"))
            {
                return BiomeType.Forest;
            }

            if (lowerName.Contains("mountain") || lowerName.Contains("peak") || lowerName.Contains("cliff"))
            {
                return BiomeType.Mountains;
            }

            if (lowerName.Contains("desert") || lowerName.Contains("sand") || lowerName.Contains("dune"))
            {
                return BiomeType.Desert;
            }

            // Water biomes
            if (lowerName.Contains("ocean") || lowerName.Contains("sea") || lowerName.Contains("aquatic"))
            {
                return BiomeType.Ocean;
            }

            // Space biomes
            if (lowerName.Contains("cosmic") || lowerName.Contains("space") || lowerName.Contains("stellar"))
            {
                return BiomeType.Cosmic;
            }

            // Crystal biomes (general crystal, not caverns)
            if (lowerName.Contains("crystal") && !lowerName.Contains("cavern"))
            {
                return BiomeType.Crystal;
            }

            // Ruins/Ancient biomes
            if (lowerName.Contains("ancient") && lowerName.Contains("ruin"))
            {
                return BiomeType.AncientRuins;
            }

            if (lowerName.Contains("ruin") || lowerName.Contains("temple") || lowerName.Contains("artifact"))
            {
                return BiomeType.Ruins;
            }

            // Volcanic/Fire biomes
            if (lowerName.Contains("volcanic") && !lowerName.Contains("core"))
            {
                return BiomeType.Volcanic;
            }

            if (lowerName.Contains("hell") || lowerName.Contains("inferno") || lowerName.Contains("demon"))
            {
                return BiomeType.Hell;
            }

            // Neutral/Mixed biomes
            if (lowerName.Contains("hub") || lowerName.Contains("central") || lowerName.Contains("main"))
            {
                return BiomeType.HubArea;
            }

            if (lowerName.Contains("transition") || lowerName.Contains("blend") || lowerName.Contains("border"))
            {
                return BiomeType.TransitionZone;
            }

            // Additional keyword fallbacks for broader matching
            if (lowerName.Contains("fire") || lowerName.Contains("lava") || lowerName.Contains("magma"))
            {
                return BiomeType.VolcanicCore;
            }

            if (lowerName.Contains("ice") || lowerName.Contains("cold") || lowerName.Contains("freeze"))
            {
                return BiomeType.FrozenWastes;
            }

            if (lowerName.Contains("water") && !lowerName.Contains("deep") && !lowerName.Contains("underwater"))
            {
                return BiomeType.Ocean;
            }

            if (lowerName.Contains("tech") || lowerName.Contains("machine") || lowerName.Contains("robot"))
            {
                return BiomeType.PowerPlant;
            }

            return BiomeType.Unknown;
        }
    }
}
