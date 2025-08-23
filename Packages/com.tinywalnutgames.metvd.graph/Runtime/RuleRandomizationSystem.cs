using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Authoring;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Component to store randomized world rules
    /// Generated based on RandomizationMode and world capabilities
    /// </summary>
    public struct WorldRuleSet : IComponentData
    {
        /// <summary>
        /// Randomized biome polarity assignments
        /// </summary>
        public Polarity BiomePolarityMask;
        
        /// <summary>
        /// Available upgrade abilities in this world
        /// </summary>
        public uint AvailableUpgradesMask;
        
        /// <summary>
        /// Whether upgrade rules have been randomized
        /// </summary>
        public bool UpgradesRandomized;
        
        /// <summary>
        /// Random seed used for rule generation
        /// </summary>
        public uint RuleSeed;

        public WorldRuleSet(Polarity biomePolarityMask, uint availableUpgradesMask, bool upgradesRandomized, uint ruleSeed)
        {
            BiomePolarityMask = biomePolarityMask;
            AvailableUpgradesMask = availableUpgradesMask;
            UpgradesRandomized = upgradesRandomized;
            RuleSeed = ruleSeed;
        }
    }

    /// <summary>
    /// Tag component indicating that rule randomization has been completed
    /// </summary>
    public struct RuleRandomizationDoneTag : IComponentData
    {
        public RandomizationMode Mode;
        public uint GeneratedRuleCount;

        public RuleRandomizationDoneTag(RandomizationMode mode, uint ruleCount = 0)
        {
            Mode = mode;
            GeneratedRuleCount = ruleCount;
        }
    }

    /// <summary>
    /// System responsible for adaptive rule randomization
    /// Operates after district layout to ensure world capabilities are considered
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ConnectionBuilderSystem))]
    public partial struct RuleRandomizationSystem : ISystem
    {
        private EntityQuery _worldConfigQuery;
        private EntityQuery _layoutDoneQuery;
        private EntityQuery _rulesDoneQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _worldConfigQuery = state.GetEntityQuery(ComponentType.ReadOnly<WorldConfiguration>());
            _layoutDoneQuery = state.GetEntityQuery(ComponentType.ReadOnly<DistrictLayoutDoneTag>());
            _rulesDoneQuery = state.GetEntityQuery(ComponentType.ReadOnly<RuleRandomizationDoneTag>());

            state.RequireForUpdate(_worldConfigQuery);
            state.RequireForUpdate(_layoutDoneQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Skip if rules already generated
            if (!_rulesDoneQuery.IsEmptyIgnoreFilter) return;

            // Wait for district layout to complete
            if (_layoutDoneQuery.IsEmptyIgnoreFilter) return;

            var worldConfig = _worldConfigQuery.GetSingleton<WorldConfiguration>();
            var layoutDone = _layoutDoneQuery.GetSingleton<DistrictLayoutDoneTag>();

            // Generate rules based on randomization mode
            var random = new Unity.Mathematics.Random((uint)(worldConfig.Seed + 42));
            var ruleSet = GenerateWorldRules(worldConfig.RandomizationMode, layoutDone.DistrictCount, ref random);

            // Create singleton rule set entity
            var ruleEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(ruleEntity, ruleSet);

            // Mark rules as done
            var doneEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(doneEntity, new RuleRandomizationDoneTag(worldConfig.RandomizationMode, 1));
        }

        /// <summary>
        /// Generate world rules based on randomization mode
        /// </summary>
        [BurstCompile]
        private static WorldRuleSet GenerateWorldRules(RandomizationMode mode, int districtCount, ref Unity.Mathematics.Random random)
        {
            switch (mode)
            {
                case RandomizationMode.None:
                    return ApplyCuratedRules(ref random);
                
                case RandomizationMode.Partial:
                    return ApplyPartialRandomization(districtCount, ref random);
                
                case RandomizationMode.Full:
                    return ApplyFullRandomization(districtCount, ref random);
                
                default:
                    return ApplyCuratedRules(ref random);
            }
        }

        /// <summary>
        /// Apply curated rules with no randomization
        /// </summary>
        [BurstCompile]
        private static WorldRuleSet ApplyCuratedRules(ref Unity.Mathematics.Random random)
        {
            // Use balanced curated polarity distribution
            var curatedPolarity = Polarity.Sun | Polarity.Moon | Polarity.Heat | Polarity.Cold;
            
            // Standard upgrade set ensuring reachability
            var curatedUpgrades = 0u;
            curatedUpgrades |= 1u << 0; // Jump upgrade
            curatedUpgrades |= 1u << 1; // Double jump
            curatedUpgrades |= 1u << 2; // Dash
            curatedUpgrades |= 1u << 3; // Wall jump
            
            return new WorldRuleSet(curatedPolarity, curatedUpgrades, false, random.NextUInt());
        }

        /// <summary>
        /// Apply partial randomization - randomize biome polarities, keep upgrades fixed
        /// </summary>
        [BurstCompile]
        private static WorldRuleSet ApplyPartialRandomization(int districtCount, ref Unity.Mathematics.Random random)
        {
            // Randomize biome polarities but ensure at least 2 are present
            var availablePolarities = new[]
            {
                Polarity.Sun, Polarity.Moon, Polarity.Heat, Polarity.Cold,
                Polarity.Earth, Polarity.Wind, Polarity.Life, Polarity.Tech
            };

            var randomizedPolarity = Polarity.None;
            int polarityCount = math.max(2, random.NextInt(2, math.min(6, availablePolarities.Length)));
            
            // Shuffle and select polarities
            for (int i = 0; i < polarityCount; i++)
            {
                int randomIndex = random.NextInt(0, availablePolarities.Length);
                randomizedPolarity |= availablePolarities[randomIndex];
            }

            // Keep curated upgrades for reachability
            var curatedUpgrades = 0u;
            curatedUpgrades |= 1u << 0; // Jump upgrade
            curatedUpgrades |= 1u << 1; // Double jump  
            curatedUpgrades |= 1u << 2; // Dash
            curatedUpgrades |= 1u << 3; // Wall jump

            return new WorldRuleSet(randomizedPolarity, curatedUpgrades, false, random.NextUInt());
        }

        /// <summary>
        /// Apply full randomization - randomize everything with reachability guards
        /// </summary>
        [BurstCompile]
        private static WorldRuleSet ApplyFullRandomization(int districtCount, ref Unity.Mathematics.Random random)
        {
            // Randomize biome polarities
            var availablePolarities = new[]
            {
                Polarity.Sun, Polarity.Moon, Polarity.Heat, Polarity.Cold,
                Polarity.Earth, Polarity.Wind, Polarity.Life, Polarity.Tech
            };

            var randomizedPolarity = Polarity.None;
            int polarityCount = math.max(2, random.NextInt(2, availablePolarities.Length));
            
            for (int i = 0; i < polarityCount; i++)
            {
                int randomIndex = random.NextInt(0, availablePolarities.Length);
                randomizedPolarity |= availablePolarities[randomIndex];
            }

            // Randomize upgrades but ensure minimum set for reachability
            var randomizedUpgrades = 0u;
            
            // Always include basic movement (reachability guard)
            randomizedUpgrades |= 1u << 0; // Jump upgrade (essential)
            
            // Randomly add other upgrades based on district count
            var maxUpgrades = math.min(8, math.max(3, districtCount / 2));
            var upgradeCount = random.NextInt(2, maxUpgrades); // At least 2, including guaranteed jump
            
            for (int i = 1; i < upgradeCount && i < 8; i++)
            {
                if (random.NextFloat() > 0.3f) // 70% chance for each additional upgrade
                {
                    randomizedUpgrades |= 1u << i;
                }
            }

            // Ensure at least one traversal upgrade exists for complex worlds
            if (districtCount > 5)
            {
                randomizedUpgrades |= 1u << 2; // Dash
            }

            return new WorldRuleSet(randomizedPolarity, randomizedUpgrades, true, random.NextUInt());
        }
    }
}