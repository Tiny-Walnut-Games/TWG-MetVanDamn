using Unity.Collections;
using Unity.Entities;

namespace TinyWalnutGames.MetVD.Core
	{
	/// <summary>
	/// Static database for enemy affixes with predefined data from the specification
	/// Creates affix entities with all required data for name generation and display
	/// </summary>
	public static class EnemyAffixDatabase
		{
		/// <summary>
		/// Initialize the affix database with predefined affixes
		/// </summary>
		public static void InitializeDatabase(EntityManager entityManager)
			{
			// Create config entity
			var configEntity = entityManager.CreateEntity();
			entityManager.AddComponentData(configEntity, new EnemyNamingConfig());

			// Create database entity
			var databaseEntity = entityManager.CreateEntity();
			entityManager.AddComponentData(databaseEntity, new AffixDatabaseTag());

			// Add all predefined affixes
			CreateCombatAffixes(entityManager);
			CreateMovementAffixes(entityManager);
			CreateBehaviorAffixes(entityManager);
			CreateBossAffixes(entityManager);
			CreateUniqueAffixes(entityManager);
			}

		/// <summary>
		/// Create combat modifier affixes
		/// </summary>
		private static void CreateCombatAffixes(EntityManager entityManager)
			{
			CreateAffix(entityManager, "berserker", "Berserker", "icon_berserker.png", 
					   TraitCategory.Combat, 5, 3, 
					   "Gains attack speed/damage over time in combat",
					   new[] { "Ber", "Zerk" });

			CreateAffix(entityManager, "armored", "Armored", "icon_armor.png", 
					   TraitCategory.Combat, 5, 3, 
					   "Reduces incoming damage",
					   new[] { "Arm", "Mor" });

			CreateAffix(entityManager, "regenerator", "Regenerator", "icon_regen.png", 
					   TraitCategory.Combat, 4, 2, 
					   "Recovers health over time",
					   new[] { "Re", "Gen" });

			CreateAffix(entityManager, "poisonous", "Poisonous", "icon_poison.png", 
					   TraitCategory.Combat, 4, 2, 
					   "Attacks apply poison damage over time",
					   new[] { "Ven", "Ox" });

			CreateAffix(entityManager, "explosive", "Explosive", "icon_explosive.png", 
					   TraitCategory.Combat, 3, 2, 
					   "Explodes on death",
					   new[] { "Ex", "Plos" });
			}

		/// <summary>
		/// Create movement modifier affixes
		/// </summary>
		private static void CreateMovementAffixes(EntityManager entityManager)
			{
			CreateAffix(entityManager, "teleporting", "Teleporting", "icon_teleport.png", 
					   TraitCategory.Movement, 4, 2, 
					   "Can blink short distances",
					   new[] { "Tel", "Port" });

			CreateAffix(entityManager, "sprinting", "Sprinting", "icon_sprint.png", 
					   TraitCategory.Movement, 5, 1, 
					   "Moves faster than normal",
					   new[] { "Spr", "Int" });

			CreateAffix(entityManager, "shuffling", "Shuffling", "icon_shuffle.png", 
					   TraitCategory.Movement, 5, 1, 
					   "Moves slowly and erratically",
					   new[] { "Shuf", "Ling" });

			CreateAffix(entityManager, "flying", "Flying", "icon_flying.png", 
					   TraitCategory.Movement, 3, 2, 
					   "Can move over obstacles",
					   new[] { "Aero", "Wing" });

			CreateAffix(entityManager, "burrowing", "Burrowing", "icon_burrow.png", 
					   TraitCategory.Movement, 3, 2, 
					   "Can tunnel underground",
					   new[] { "Bur", "Row" });
			}

		/// <summary>
		/// Create behavior modifier affixes
		/// </summary>
		private static void CreateBehaviorAffixes(EntityManager entityManager)
			{
			CreateAffix(entityManager, "pack_hunter", "Pack Hunter", "icon_pack.png", 
					   TraitCategory.Behavior, 4, 1, 
					   "Gains bonuses near allies",
					   new[] { "Pack", "Hun" });

			CreateAffix(entityManager, "ambusher", "Ambusher", "icon_ambush.png", 
					   TraitCategory.Behavior, 4, 1, 
					   "Prefers surprise attacks",
					   new[] { "Amb", "Ush" });

			CreateAffix(entityManager, "cowardly", "Cowardly", "icon_coward.png", 
					   TraitCategory.Behavior, 3, 0, 
					   "Avoids direct combat",
					   new[] { "Cow", "Ard" });

			CreateAffix(entityManager, "patrol", "Patrol", "icon_patrol.png", 
					   TraitCategory.Behavior, 3, 0, 
					   "Moves along set routes",
					   new[] { "Pat", "Rol" });
			}

		/// <summary>
		/// Create boss-only modifier affixes
		/// </summary>
		private static void CreateBossAffixes(EntityManager entityManager)
			{
			CreateAffix(entityManager, "summoner", "Summoner", "icon_summon.png", 
					   TraitCategory.Boss, 4, 3, 
					   "Summons additional enemies",
					   new[] { "Mon", "Zedd" });

			CreateAffix(entityManager, "arena_shaper", "Arena Shaper", "icon_arena.png", 
					   TraitCategory.Boss, 3, 2, 
					   "Alters arena layout mid-fight",
					   new[] { "Are", "Sha" });

			CreateAffix(entityManager, "trap_layer", "Trap Layer", "icon_trap.png", 
					   TraitCategory.Boss, 3, 2, 
					   "Places traps during combat",
					   new[] { "Trap", "Lay" });

			CreateAffix(entityManager, "meteor_slam", "Meteor Slam", "icon_meteor.png", 
					   TraitCategory.Boss, 2, 3, 
					   "Calls down meteors",
					   new[] { "Met", "Slam" });

			CreateAffix(entityManager, "gravity_shift", "Gravity Shift", "icon_gravity.png", 
					   TraitCategory.Boss, 2, 3, 
					   "Alters gravity in arena",
					   new[] { "Grav", "Ity" });
			}

		/// <summary>
		/// Create unique/named modifier affixes
		/// </summary>
		private static void CreateUniqueAffixes(EntityManager entityManager)
			{
			CreateAffix(entityManager, "eternal_flame", "Eternal Flame", "icon_flame.png", 
					   TraitCategory.Unique, 1, 3, 
					   "Constant fire aura that damages nearby players",
					   new[] { "Eter", "Flam" });

			CreateAffix(entityManager, "void_touched", "Void-Touched", "icon_void.png", 
					   TraitCategory.Unique, 1, 3, 
					   "Gains powers from the void, unpredictable attacks",
					   new[] { "Void", "Tuch" });

			CreateAffix(entityManager, "frostbound", "Frostbound", "icon_frost.png", 
					   TraitCategory.Unique, 1, 2, 
					   "Freezes terrain and slows enemies",
					   new[] { "Fros", "Boun" });

			CreateAffix(entityManager, "stormcaller", "Stormcaller", "icon_storm.png", 
					   TraitCategory.Unique, 1, 2, 
					   "Summons lightning strikes periodically",
					   new[] { "Stor", "Call" });

			CreateAffix(entityManager, "soulrender", "Soulrender", "icon_soul.png", 
					   TraitCategory.Unique, 1, 3, 
					   "Drains life from players to heal itself",
					   new[] { "Soul", "Ren" });
			}

		/// <summary>
		/// Helper method to create a single affix entity
		/// </summary>
		private static void CreateAffix(EntityManager entityManager, string id, string displayName, string iconRef,
									   TraitCategory category, byte weight, byte toxicityScore, string description,
									   string[] bossSyllables)
			{
			var entity = entityManager.CreateEntity();

			var affix = new EnemyAffix(
				new FixedString64Bytes(id),
				new FixedString64Bytes(displayName),
				new FixedString64Bytes(iconRef),
				category,
				weight,
				toxicityScore,
				new FixedString128Bytes(description)
			);

			// Add boss syllables
			foreach (var syllable in bossSyllables)
				{
				affix.AddBossSyllable(new FixedString32Bytes(syllable));
				}

			entityManager.AddComponentData(entity, affix);
			entityManager.AddComponentData(entity, new AffixDatabaseTag());
			}

		/// <summary>
		/// Get all affixes of a specific category
		/// </summary>
		public static NativeArray<Entity> GetAffixesByCategory(EntityManager entityManager, TraitCategory category, Allocator allocator)
			{
			var query = entityManager.CreateEntityQuery(typeof(EnemyAffix), typeof(AffixDatabaseTag));
			var entities = query.ToEntityArray(allocator);
			var results = new NativeList<Entity>(allocator);

			foreach (var entity in entities)
				{
				var affix = entityManager.GetComponentData<EnemyAffix>(entity);
				if (affix.Category == category)
					{
					results.Add(entity);
					}
				}

			entities.Dispose();
			return results.AsArray();
			}

		/// <summary>
		/// Generate random affixes for an enemy based on rarity
		/// </summary>
		public static void AssignRandomAffixes(EntityManager entityManager, Entity enemyEntity, RarityType rarity, uint seed)
			{
			var random = Unity.Mathematics.Random.CreateFromIndex(seed);
			int affixCount = GetAffixCountForRarity(rarity);

			if (affixCount == 0)
				{
				return;
				}

			// Get all available affixes
			var query = entityManager.CreateEntityQuery(typeof(EnemyAffix), typeof(AffixDatabaseTag));
			var affixEntities = query.ToEntityArray(Allocator.Temp);

			if (affixEntities.Length == 0)
				{
				affixEntities.Dispose();
				return;
				}

			// Create affix buffer on enemy
			var affixBuffer = entityManager.AddBuffer<EnemyAffixBufferElement>(enemyEntity);

			// Select random affixes
			for (int i = 0; i < affixCount && i < affixEntities.Length; i++)
				{
				int randomIndex = random.NextInt(0, affixEntities.Length);
				var affixEntity = affixEntities[randomIndex];
				var affix = entityManager.GetComponentData<EnemyAffix>(affixEntity);

				// Check if we already have this affix
				bool alreadyHasAffix = false;
				foreach (var existingAffix in affixBuffer)
					{
					if (existingAffix.Value.Id.Equals(affix.Id))
						{
						alreadyHasAffix = true;
						break;
						}
					}

				if (!alreadyHasAffix)
					{
					affixBuffer.Add(affix);
					}
				}

			affixEntities.Dispose();
			}

		/// <summary>
		/// Get the number of affixes an enemy should have based on rarity
		/// </summary>
		private static int GetAffixCountForRarity(RarityType rarity)
			{
			return rarity switch
				{
					RarityType.Common => 1,
					RarityType.Uncommon => 2,
					RarityType.Rare => 2,
					RarityType.Unique => 3,
					RarityType.MiniBoss => 2,
					RarityType.Boss => 3,
					RarityType.FinalBoss => 4,
					_ => 1
				};
			}
		}
	}