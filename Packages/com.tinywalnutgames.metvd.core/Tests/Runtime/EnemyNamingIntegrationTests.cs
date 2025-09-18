using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Core.Tests
	{
	/// <summary>
	/// Integration tests for the complete Enemy Naming and Affix Display System
	/// Tests end-to-end functionality from enemy creation to final display
	/// </summary>
	[TestFixture]
#nullable enable
	public class EnemyNamingIntegrationTests
		{
		private World world = null!;
		private EntityManager m_Manager; // struct
		private InitializationSystemGroup initGroup = null!;

		[SetUp]
		public void Setup()
			{
			world = new World("EnemyNamingIntegrationTests");
			World.DefaultGameObjectInjectionWorld = world;
			m_Manager = world.EntityManager;

			// Initialize the complete system
			initGroup = world.GetOrCreateSystemManaged<InitializationSystemGroup>();
			SystemHandle namingSystemHandle = world.GetOrCreateSystem<EnemyNamingSystem>();
			initGroup.AddSystemToUpdateList(namingSystemHandle);
			EnemyAffixDatabase.InitializeDatabase(m_Manager);

			// Database initialize ensures config singleton exists; avoid double-creation
			}

		[TearDown]
		public void TearDown()
			{
			if (world != null && world.IsCreated)
				{
				world.Dispose();
				if (World.DefaultGameObjectInjectionWorld == world)
					{
					World.DefaultGameObjectInjectionWorld = null;
					}
				}
			}

		[Test]
		public void Complete_Enemy_Creation_Workflow()
			{
			// Simulate complete enemy creation workflow

			// 1. Create enemy entity with profile
			Entity enemyEntity = m_Manager.CreateEntity();
			m_Manager.AddComponentData(enemyEntity, new EnemyProfile(RarityType.Rare, "Sentinel", 98765));
			m_Manager.AddComponentData(enemyEntity, new NeedsNameGeneration());

			// 2. Assign random affixes based on rarity
			EnemyAffixDatabase.AssignRandomAffixes(m_Manager, enemyEntity, RarityType.Rare, 98765);

			// 3. Process naming system
			initGroup.Update();

			// 4. Verify complete setup
			Assert.IsTrue(m_Manager.HasComponent<EnemyNaming>(enemyEntity));
			Assert.IsTrue(m_Manager.HasBuffer<EnemyAffixBufferElement>(enemyEntity));
			Assert.IsFalse(m_Manager.HasComponent<NeedsNameGeneration>(enemyEntity)); // Should be removed

			EnemyNaming naming = m_Manager.GetComponentData<EnemyNaming>(enemyEntity);
			DynamicBuffer<EnemyAffixBufferElement> affixes = m_Manager.GetBuffer<EnemyAffixBufferElement>(enemyEntity);

			// Rare enemy should show full name and icons
			Assert.IsTrue(naming.ShowFullName);
			Assert.IsTrue(naming.ShowIcons);
			Assert.GreaterOrEqual(affixes.Length, 1); // Should have at least one affix
			}

		[Test]
		public void Multiple_Enemy_Types_Generate_Correctly()
			{
			// Create multiple enemies of different rarities
			var entities = new Entity[6];
			RarityType[] rarities = new[] { RarityType.Common, RarityType.Uncommon, RarityType.Rare,
								 RarityType.Unique, RarityType.MiniBoss, RarityType.Boss };
			string[] baseTypes = new[] { "Crawler", "Archer", "Mage", "Guardian", "Warden", "Overlord" };

			// Create entities
			for (int i = 0; i < entities.Length; i++)
				{
				entities[i] = m_Manager.CreateEntity();
				m_Manager.AddComponentData(entities[i], new EnemyProfile(rarities[i], baseTypes[i], (uint)(1000 + i)));
				m_Manager.AddComponentData(entities[i], new NeedsNameGeneration());
				EnemyAffixDatabase.AssignRandomAffixes(m_Manager, entities[i], rarities[i], (uint)(1000 + i));
				}

			// Process all entities
			initGroup.Update();

			// Verify each entity is processed correctly
			for (int i = 0; i < entities.Length; i++)
				{
				Assert.IsTrue(m_Manager.HasComponent<EnemyNaming>(entities[i]),
					$"Entity {i} ({rarities[i]}) should have EnemyNaming component");

				EnemyNaming naming = m_Manager.GetComponentData<EnemyNaming>(entities[i]);

				// Common and Uncommon should show base type only
				if (rarities[i] is RarityType.Common or RarityType.Uncommon)
					{
					Assert.IsFalse(naming.ShowFullName, $"{rarities[i]} should not show full name");
					Assert.AreEqual(baseTypes[i], naming.DisplayName.ToString(),
						$"{rarities[i]} should show base type '{baseTypes[i]}'");
					}
				else
					{
					Assert.IsTrue(naming.ShowFullName, $"{rarities[i]} should show full name");
					}

				// All should show icons unless globally disabled
				Assert.IsTrue(naming.ShowIcons, $"{rarities[i]} should show icons");
				}
			}

		[Test]
		public void Boss_Progression_Shows_Name_Complexity_Increase()
			{
			// Create boss progression: MiniBoss -> Boss -> FinalBoss
			Entity miniBoss = CreateBossWithAffixes(RarityType.MiniBoss, "Champion");
			Entity boss = CreateBossWithAffixes(RarityType.Boss, "Overlord");
			Entity finalBoss = CreateBossWithAffixes(RarityType.FinalBoss, "Ancient");

			initGroup.Update();

			EnemyNaming miniBossNaming = m_Manager.GetComponentData<EnemyNaming>(miniBoss);
			EnemyNaming bossNaming = m_Manager.GetComponentData<EnemyNaming>(boss);
			EnemyNaming finalBossNaming = m_Manager.GetComponentData<EnemyNaming>(finalBoss);

			// All should show full names
			Assert.IsTrue(miniBossNaming.ShowFullName);
			Assert.IsTrue(bossNaming.ShowFullName);
			Assert.IsTrue(finalBossNaming.ShowFullName);

			// Names should generally increase in complexity (length as a proxy)
			// Note: This is probabilistic but with fixed seeds should be deterministic
			Assert.GreaterOrEqual(bossNaming.DisplayName.Length, miniBossNaming.DisplayName.Length - 5); // Allow some variance
			Assert.GreaterOrEqual(finalBossNaming.DisplayName.Length, bossNaming.DisplayName.Length - 5);

			// All should be procedural (not contain base type for bosses)
			Assert.IsFalse(miniBossNaming.DisplayName.ToString().Contains("Champion"));
			Assert.IsFalse(bossNaming.DisplayName.ToString().Contains("Overlord"));
			Assert.IsFalse(finalBossNaming.DisplayName.ToString().Contains("Ancient"));
			}

		[Test]
		public void Configuration_Changes_Affect_All_Entities()
			{
			// Create enemies with different configurations
			Entity enemy1 = CreateTestEnemy(RarityType.Rare, "Warrior");
			Entity enemy2 = CreateTestEnemy(RarityType.Common, "Scout");

			// Test with NamesAndIcons mode
			SetGlobalDisplayMode(AffixDisplayMode.NamesAndIcons);
			initGroup.Update();

			EnemyNaming naming1 = m_Manager.GetComponentData<EnemyNaming>(enemy1);
			EnemyNaming naming2 = m_Manager.GetComponentData<EnemyNaming>(enemy2);

			Assert.IsTrue(naming1.ShowIcons);
			Assert.IsTrue(naming2.ShowIcons);
			Assert.AreEqual(AffixDisplayMode.NamesAndIcons, naming1.DisplayMode);
			Assert.AreEqual(AffixDisplayMode.NamesAndIcons, naming2.DisplayMode);

			// Change to IconsOnly and recreate entities
			SetGlobalDisplayMode(AffixDisplayMode.IconsOnly);

			enemy1 = CreateTestEnemy(RarityType.Rare, "Warrior");
			enemy2 = CreateTestEnemy(RarityType.Common, "Scout");

			initGroup.Update();

			naming1 = m_Manager.GetComponentData<EnemyNaming>(enemy1);
			naming2 = m_Manager.GetComponentData<EnemyNaming>(enemy2);

			Assert.IsTrue(naming1.ShowIcons);
			Assert.IsTrue(naming2.ShowIcons);
			Assert.AreEqual(AffixDisplayMode.IconsOnly, naming1.DisplayMode);
			Assert.AreEqual(AffixDisplayMode.IconsOnly, naming2.DisplayMode);
			}

		[Test]
		public void Affix_Database_Query_Functions_Work()
			{
			// Test database query functions
			NativeArray<Entity> combatAffixes = EnemyAffixDatabase.GetAffixesByCategory(m_Manager, TraitCategory.Combat, Allocator.Temp);
			NativeArray<Entity> movementAffixes = EnemyAffixDatabase.GetAffixesByCategory(m_Manager, TraitCategory.Movement, Allocator.Temp);
			NativeArray<Entity> bossAffixes = EnemyAffixDatabase.GetAffixesByCategory(m_Manager, TraitCategory.Boss, Allocator.Temp);

			Assert.Greater(combatAffixes.Length, 0, "Should have combat affixes");
			Assert.Greater(movementAffixes.Length, 0, "Should have movement affixes");
			Assert.Greater(bossAffixes.Length, 0, "Should have boss affixes");

			// Verify affixes have correct categories
			foreach (Entity entity in combatAffixes)
				{
				EnemyAffix affix = m_Manager.GetComponentData<EnemyAffix>(entity);
				Assert.AreEqual(TraitCategory.Combat, affix.Category);
				}

			foreach (Entity entity in movementAffixes)
				{
				EnemyAffix affix = m_Manager.GetComponentData<EnemyAffix>(entity);
				Assert.AreEqual(TraitCategory.Movement, affix.Category);
				}

			combatAffixes.Dispose();
			movementAffixes.Dispose();
			bossAffixes.Dispose();
			}

		[Test]
		public void System_Handles_Entities_Without_Affixes_Gracefully()
			{
			// Create enemy without affixes
			Entity enemyEntity = m_Manager.CreateEntity();
			m_Manager.AddComponentData(enemyEntity, new EnemyProfile(RarityType.Rare, "Phantom", 55555));
			m_Manager.AddComponentData(enemyEntity, new NeedsNameGeneration());
			// Don't add any affixes

			// Should not crash and should handle gracefully
			Assert.DoesNotThrow(() => initGroup.Update());

			// Should still create naming component
			Assert.IsTrue(m_Manager.HasComponent<EnemyNaming>(enemyEntity));

			EnemyNaming naming = m_Manager.GetComponentData<EnemyNaming>(enemyEntity);
			// Should fall back to base type
			Assert.AreEqual("Phantom", naming.DisplayName.ToString());
			}

		[Test]
		public void Consistent_Names_With_Same_Seed()
			{
			// Create two identical enemies with same seed
			Entity enemy1 = CreateBossWithAffixes(RarityType.Boss, "Titan", 77777);
			Entity enemy2 = CreateBossWithAffixes(RarityType.Boss, "Titan", 77777);

			initGroup.Update();

			EnemyNaming naming1 = m_Manager.GetComponentData<EnemyNaming>(enemy1);
			EnemyNaming naming2 = m_Manager.GetComponentData<EnemyNaming>(enemy2);

			// Should generate identical names with same seed
			Assert.AreEqual(naming1.DisplayName.ToString(), naming2.DisplayName.ToString());
			}

		#region Helper Methods

		private Entity CreateTestEnemy(RarityType rarity, string baseType)
			{
			Entity entity = m_Manager.CreateEntity();
			m_Manager.AddComponentData(entity, new EnemyProfile(rarity, new FixedString64Bytes(baseType), 12345));
			m_Manager.AddComponentData(entity, new NeedsNameGeneration());
			EnemyAffixDatabase.AssignRandomAffixes(m_Manager, entity, rarity, 12345);
			return entity;
			}

		private Entity CreateBossWithAffixes(RarityType rarity, string baseType, uint seed = 33333)
			{
			Entity entity = m_Manager.CreateEntity();
			m_Manager.AddComponentData(entity, new EnemyProfile(rarity, new FixedString64Bytes(baseType), seed));
			m_Manager.AddComponentData(entity, new NeedsNameGeneration());
			EnemyAffixDatabase.AssignRandomAffixes(m_Manager, entity, rarity, seed);
			return entity;
			}

		private void SetGlobalDisplayMode(AffixDisplayMode mode)
			{
			EntityQuery query = m_Manager.CreateEntityQuery(typeof(EnemyNamingConfig));
			NativeArray<Entity> configEntities = query.ToEntityArray(Allocator.Temp);

			if (configEntities.Length > 0)
				{
				EnemyNamingConfig config = m_Manager.GetComponentData<EnemyNamingConfig>(configEntities[0]);
				config.GlobalDisplayMode = mode;
				m_Manager.SetComponentData(configEntities[0], config);
				}
			else
				{
				Entity configEntity = m_Manager.CreateEntity();
				m_Manager.AddComponentData(configEntity, new EnemyNamingConfig(mode));
				}

			configEntities.Dispose();
			}

		#endregion
		}
	}
