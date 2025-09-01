using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

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
			_worldConfigQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<WorldConfiguration>()
				.Build(ref state);
			_layoutDoneQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<DistrictLayoutDoneTag>()
				.Build(ref state);
			_rulesDoneQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RuleRandomizationDoneTag>()
				.Build(ref state);

			state.RequireForUpdate(_worldConfigQuery);
			state.RequireForUpdate(_layoutDoneQuery);
			}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
			{
			// Skip if rules already generated
			if (!_rulesDoneQuery.IsEmptyIgnoreFilter)
				{
				return;
				}

			// Wait for district layout to complete
			if (_layoutDoneQuery.IsEmptyIgnoreFilter)
				{
				return;
				}

			WorldConfiguration worldConfig = _worldConfigQuery.GetSingleton<WorldConfiguration>();
			DistrictLayoutDoneTag layoutDone = _layoutDoneQuery.GetSingleton<DistrictLayoutDoneTag>();

			// Generate rules based on randomization mode
			var random = new Random((uint)(worldConfig.Seed + 42));
			WorldRuleSet ruleSet = GenerateWorldRules(worldConfig.RandomizationMode, layoutDone.DistrictCount, ref random);

			// Create singleton rule set entity
			Entity ruleEntity = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponentData(ruleEntity, ruleSet);

			// Mark rules as done
			Entity doneEntity = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponentData(doneEntity, new RuleRandomizationDoneTag(worldConfig.RandomizationMode, 1));
			}

		/// <summary>
		/// Generate world rules based on randomization mode
		/// </summary>
		private static WorldRuleSet GenerateWorldRules(RandomizationMode mode, int districtCount, ref Random random)
			{
			return mode switch
				{
					RandomizationMode.None => ApplyCuratedRules(ref random),
					RandomizationMode.Partial => ApplyPartialRandomization(ref random),
					RandomizationMode.Full => ApplyFullRandomization(districtCount, ref random),
					_ => ApplyCuratedRules(ref random),
					};
			}

		/// <summary>
		/// Apply curated rules with no randomization
		/// </summary>
		private static WorldRuleSet ApplyCuratedRules(ref Random random)
			{
			// Use balanced curated polarity distribution
			Polarity curatedPolarity = Polarity.Sun | Polarity.Moon | Polarity.Heat | Polarity.Cold;

			// Standard upgrade set ensuring reachability
			uint curatedUpgrades = 0u;
			curatedUpgrades |= 1u << 0; // Jump upgrade
			curatedUpgrades |= 1u << 1; // Double jump
			curatedUpgrades |= 1u << 2; // Dash
			curatedUpgrades |= 1u << 3; // Wall jump

			return new WorldRuleSet(curatedPolarity, curatedUpgrades, false, random.NextUInt());
			}

		// Helper to pick one polarity by index (avoids managed arrays)
		private static Polarity PickPolarity(int index)
			{
			return index switch
				{
					0 => Polarity.Sun,
					1 => Polarity.Moon,
					2 => Polarity.Heat,
					3 => Polarity.Cold,
					4 => Polarity.Earth,
					5 => Polarity.Wind,
					6 => Polarity.Life,
					_ => Polarity.Tech,
					};
			}

		/// <summary>
		/// Apply partial randomization - randomize biome polarities, keep upgrades fixed
		/// </summary>
		private static WorldRuleSet ApplyPartialRandomization(ref Random random)
			{
			// Randomize biome polarities but ensure at least 2 are present
			Polarity randomizedPolarity = Polarity.None;
			int polarityCount = math.max(2, random.NextInt(2, math.min(6, 8))); // 8 possible

			// Shuffle and select polarities
			for (int i = 0; i < polarityCount; i++)
				{
				int randomIndex = random.NextInt(0, 8);
				randomizedPolarity |= PickPolarity(randomIndex);
				}

			// Keep curated upgrades for reachability
			uint curatedUpgrades = 0u;
			curatedUpgrades |= 1u << 0; // Jump upgrade
			curatedUpgrades |= 1u << 1; // Double jump  
			curatedUpgrades |= 1u << 2; // Dash
			curatedUpgrades |= 1u << 3; // Wall jump

			return new WorldRuleSet(randomizedPolarity, curatedUpgrades, false, random.NextUInt());
			}

		/// <summary>
		/// Apply full randomization - randomize everything with reachability guards
		/// </summary>
		private static WorldRuleSet ApplyFullRandomization(int districtCount, ref Random random)
			{
			// Randomize biome polarities
			Polarity randomizedPolarity = Polarity.None;
			int polarityCount = math.max(2, random.NextInt(2, 8));

			for (int i = 0; i < polarityCount; i++)
				{
				int randomIndex = random.NextInt(0, 8);
				randomizedPolarity |= PickPolarity(randomIndex);
				}

			// Randomize upgrades but ensure minimum set for reachability
			uint randomizedUpgrades = 0u;

			// Always include basic movement (reachability guard)
			randomizedUpgrades |= 1u << 0; // Jump upgrade (essential)

			// Randomly add other upgrades based on district count
			int maxUpgrades = math.min(8, math.max(3, districtCount / 2));
			int upgradeCount = random.NextInt(2, maxUpgrades); // At least 2, including guaranteed jump

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
