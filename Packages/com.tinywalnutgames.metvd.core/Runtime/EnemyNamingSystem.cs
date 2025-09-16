using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Core
	{
	/// <summary>
	/// System that generates enemy names based on rarity and affixes
	/// Handles both text names and icon-based display according to rarity rules
	/// </summary>
	[BurstCompile]
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	public partial struct EnemyNamingSystem : ISystem
		{
		private struct PendingNaming
			{
			public Entity Entity;
			public EnemyNaming Naming;
			}

		[BurstCompile]
		public void OnCreate(ref SystemState state)
			{
			state.RequireForUpdate<EnemyNamingConfig>();
			state.RequireForUpdate<NeedsNameGeneration>();
			}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
			{
			var namingConfig = SystemAPI.GetSingleton<EnemyNamingConfig>();

			// Lookups for safe read access during iteration (no EntityManager calls inside loop)
			var affixLookupRO = SystemAPI.GetBufferLookup<EnemyAffixBufferElement>(true);

			// Collect results to apply after iteration to avoid structural changes during iteration
			var toApply = new Unity.Collections.NativeList<PendingNaming>(Allocator.Temp);

			// Process entities that need name generation
			foreach (var (profile, entity) in SystemAPI.Query<RefRO<EnemyProfile>>()
				.WithAll<NeedsNameGeneration>()
				.WithEntityAccess())
				{
				var naming = GenerateEnemyName(in affixLookupRO, entity, in profile.ValueRO, in namingConfig);
				toApply.Add(new PendingNaming { Entity = entity, Naming = naming });
				}

			// Apply structural changes after enumeration
			var em = state.EntityManager;
			for (int i = 0; i < toApply.Length; i++)
				{
				var item = toApply[i];
				if (!em.HasComponent<EnemyNaming>(item.Entity))
					{
					em.AddComponentData(item.Entity, item.Naming);
					}
				else
					{
					em.SetComponentData(item.Entity, item.Naming);
					}
				if (em.HasComponent<NeedsNameGeneration>(item.Entity))
					{
					em.RemoveComponent<NeedsNameGeneration>(item.Entity);
					}
				}

			toApply.Dispose();
			}

		/// <summary>
		/// Generate name and display settings for an enemy based on its profile and affixes
		/// </summary>
		private static EnemyNaming GenerateEnemyName(in BufferLookup<EnemyAffixBufferElement> affixLookupRO,
											  Entity entity, in EnemyProfile profile, in EnemyNamingConfig config)
			{
			var random = Unity.Mathematics.Random.CreateFromIndex(profile.GenerationSeed);

			// Determine display behavior based on rarity
			bool showFullName = ShouldShowFullName(profile.Rarity);
			bool showIcons = ShouldShowIcons(profile.Rarity, config.GlobalDisplayMode);

			FixedString128Bytes displayName;

			if (showFullName)
				{
				displayName = GenerateFullName(in affixLookupRO, entity, in profile, in config, random);
				}
			else
				{
				displayName = profile.BaseType;
				}

			// Create the naming component
			return new EnemyNaming(displayName, showFullName, showIcons, config.GlobalDisplayMode);
			}

		/// <summary>
		/// Determine if an enemy should show its full name based on rarity
		/// </summary>
		private static bool ShouldShowFullName(RarityType rarity)
			{
			return rarity switch
				{
					RarityType.Common => false,
					RarityType.Uncommon => false,
					RarityType.Rare => true,
					RarityType.Unique => true,
					RarityType.MiniBoss => true,
					RarityType.Boss => true,
					RarityType.FinalBoss => true,
					_ => false
					};
			}

		/// <summary>
		/// Determine if an enemy should show affix icons
		/// </summary>
		private static bool ShouldShowIcons(RarityType rarity, AffixDisplayMode globalMode)
			{
			if (globalMode == AffixDisplayMode.NamesOnly)
				{
				return false;
				}

			return true; // All rarities show icons unless globally disabled
			}

		/// <summary>
		/// Generate a full name including prefixes and suffixes from affixes
		/// </summary>
		private static FixedString128Bytes GenerateFullName(in BufferLookup<EnemyAffixBufferElement> affixLookupRO,
									   Entity entity, in EnemyProfile profile,
									   in EnemyNamingConfig config, Unity.Mathematics.Random random)
			{
			// Check if entity has affixes
			if (!affixLookupRO.HasBuffer(entity))
				{
				return profile.BaseType;
				}

			var affixBuffer = affixLookupRO[entity];

			if (affixBuffer.Length == 0)
				{
				return profile.BaseType;
				}

			// For bosses, use procedural syllable-based names
			if (IsBossType(profile.Rarity) && config.UseProceduralBossNames)
				{
				return GenerateProceduralBossName(affixBuffer, profile, random);
				}

			// For rares/uniques, use traditional prefix + base + suffix format
			return GenerateAffixedName(affixBuffer, profile.BaseType, random);
			}

		/// <summary>
		/// Check if rarity type is considered a boss
		/// </summary>
		private static bool IsBossType(RarityType rarity)
			{
			return rarity is RarityType.MiniBoss or RarityType.Boss or RarityType.FinalBoss;
			}

		/// <summary>
		/// Generate a procedural boss name using affix syllables
		/// </summary>
		private static FixedString128Bytes GenerateProceduralBossName(
			DynamicBuffer<EnemyAffixBufferElement> affixes, EnemyProfile profile, Unity.Mathematics.Random random)
			{
			var nameBuilder = new FixedString128Bytes();
			int syllablesUsed = 0;
			int maxSyllables = GetMaxSyllablesForRarity(profile.Rarity);

			// Concatenate syllables from affixes
			foreach (var affixElement in affixes)
				{
				if (syllablesUsed >= maxSyllables)
					{
					break;
					}

				var syllable = affixElement.Value.GetRandomBossSyllable(random.NextUInt());
				if (syllable.Length > 0)
					{
					if (nameBuilder.Length > 0 && ShouldInsertConnective(nameBuilder, syllable))
						{
						nameBuilder.Append('a'); // Simple connective vowel
						}
					nameBuilder.Append(syllable);
					syllablesUsed++;
					}
				}

			// If no syllables were found, fall back to base type
			if (nameBuilder.Length == 0)
				{
				nameBuilder = profile.BaseType;
				}

			return nameBuilder;
			}

		/// <summary>
		/// Get maximum number of syllables for boss name based on rarity
		/// </summary>
		private static int GetMaxSyllablesForRarity(RarityType rarity)
			{
			return rarity switch
				{
					RarityType.MiniBoss => 2,
					RarityType.Boss => 3,
					RarityType.FinalBoss => 4,
					_ => 2
					};
			}

		/// <summary>
		/// Determine if a connective vowel should be inserted between syllables
		/// </summary>
		private static bool ShouldInsertConnective(FixedString128Bytes currentName, FixedString32Bytes nextSyllable)
			{
			if (currentName.Length == 0 || nextSyllable.Length == 0)
				{
				return false;
				}

			// Get last character of current name and first character of next syllable
			byte lastChar = currentName[^1];
			byte firstChar = nextSyllable[0];

			// Insert connective if both are consonants (simple heuristic)
			return !IsVowel(lastChar) && !IsVowel(firstChar);
			}

		/// <summary>
		/// Simple vowel check for readability improvement
		/// </summary>
		private static bool IsVowel(byte character)
			{
			return character is (byte)'a' or (byte)'e' or (byte)'i' or (byte)'o' or (byte)'u' or
								(byte)'A' or (byte)'E' or (byte)'I' or (byte)'O' or (byte)'U';
			}

		/// <summary>
		/// Generate traditional affix-based name (prefix + base + suffix)
		/// </summary>
		private static FixedString128Bytes GenerateAffixedName(DynamicBuffer<EnemyAffixBufferElement> affixes,
															  FixedString64Bytes baseType, Unity.Mathematics.Random random)
			{
			var nameBuilder = new FixedString128Bytes();

			// Select prefix and suffix from available affixes
			var prefix = SelectAffixForPosition(affixes, true, random);
			var suffix = SelectAffixForPosition(affixes, false, random);

			// Build name: prefix + base + suffix
			if (prefix.Length > 0)
				{
				nameBuilder.Append(prefix);
				nameBuilder.Append(' ');
				}

			nameBuilder.Append(baseType);

			if (suffix.Length > 0)
				{
				// Burst-safe append of separator without using managed string
				nameBuilder.Append(' ');
				nameBuilder.Append('o');
				nameBuilder.Append('f');
				nameBuilder.Append(' ');
				nameBuilder.Append(suffix);
				}

			return nameBuilder;
			}

		/// <summary>
		/// Select an affix name for use as prefix or suffix
		/// </summary>
		private static FixedString64Bytes SelectAffixForPosition(DynamicBuffer<EnemyAffixBufferElement> affixes,
															   bool isPrefix, Unity.Mathematics.Random random)
			{
			if (affixes.Length == 0)
				{
				return new FixedString64Bytes();
				}

			// For simplicity, randomly select from available affixes
			// In a more complex system, you might prefer certain affixes for prefixes vs suffixes
			int index = random.NextInt(0, affixes.Length);
			return affixes[index].Value.DisplayName;
			}
		}
	}
