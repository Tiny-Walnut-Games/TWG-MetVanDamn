using Unity.Entities;
using Unity.Mathematics;
using Shared = TinyWalnutGames.MetVD.Shared;

namespace TinyWalnutGames.MetVD.Core
    {
    /// <summary>
    /// Compatibility bridge that ensures a legacy Shared.WorldConfiguration singleton exists
    /// by deriving it from either the new Data-suffixed components or the legacy Shared components.
    /// This unblocks older systems/tests that still query Shared.WorldConfiguration.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct WorldConfigurationCompatibilityBridgeSystem : ISystem
        {
        public void OnCreate(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
            {
            var em = state.EntityManager;

            // Try to get existing shared configuration
            bool hasCfg = SystemAPI.TryGetSingletonEntity<Shared.WorldConfiguration>(out var cfgEntity);
            Shared.WorldConfiguration cfg = default;
            if (hasCfg)
                {
                cfg = em.GetComponentData<Shared.WorldConfiguration>(cfgEntity);
                }

            bool updated = false;

            // Derive from legacy Shared components when present
            if (SystemAPI.TryGetSingleton<Shared.WorldGenerationConfig>(out var gen))
                {
                cfg.TargetSectors = gen.TargetSectorCount;
                cfg.Seed = unchecked((int)gen.WorldSeed);
                updated = true;
                }
            if (SystemAPI.TryGetSingleton<Shared.WorldSeed>(out var seed))
                {
                cfg.Seed = unchecked((int)seed.Value);
                updated = true;
                }
            if (SystemAPI.TryGetSingleton<Shared.WorldBounds>(out var bounds))
                {
                int2 size = bounds.Max - bounds.Min + new int2(1, 1); // assume inclusive bounds
                cfg.WorldSize = math.max(new int2(1, 1), size);
                updated = true;
                }

            // Derive from new Data components when present
            if (SystemAPI.TryGetSingleton<WorldGenerationConfigData>(out var genData))
                {
                if (genData.TargetSectorCount > 0)
                    cfg.TargetSectors = genData.TargetSectorCount;
                updated = true;
                }
            if (SystemAPI.TryGetSingleton<WorldSeedData>(out var seedData))
                {
                cfg.Seed = unchecked((int)seedData.Value);
                updated = true;
                }
            if (SystemAPI.TryGetSingleton<WorldBoundsData>(out var boundsData))
                {
                int w = math.max(1, (int)math.round(boundsData.Extents.x * 2f));
                int h = math.max(1, (int)math.round(boundsData.Extents.y * 2f));
                cfg.WorldSize = new int2(w, h);
                updated = true;
                }

            // Defaults if nothing provided elsewhere
            if (!hasCfg && !updated)
                {
                // No sources found; leave early (nothing to do)
                return;
                }

            // Ensure reasonable defaults for fields not covered by sources
            // Flow defaults to GridFirstLegacy (0); RandomizationMode defaults to None (0)

            if (!hasCfg)
                {
                var e = em.CreateEntity();
                em.AddComponentData(e, cfg);
                }
            else if (updated)
                {
                em.SetComponentData(cfgEntity, cfg);
                }
            }
        }
    }
