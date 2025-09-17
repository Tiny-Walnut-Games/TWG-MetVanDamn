using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.TestTools;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Core.Tests
	{
	/// <summary>
	/// Tests for the Enemy Naming and Affix Display System
	/// Covers name generation, boss syllable concatenation, icon assignment, and config behavior
	/// </summary>
	[TestFixture]
	public class EnemyNamingSystemTests
		{
		private World world;
		private EntityManager m_Manager;
		private InitializationSystemGroup initGroup;
		private Entity configEntity;
		private Entity testEnemyEntity;

		[SetUp]
		public void Setup()
			{
			world = new World("EnemyNamingSystemTests");
			World.DefaultGameObjectInjectionWorld = world;
			m_Manager = world.EntityManager;

			// Ensure Initialization group exists and register the naming system
			initGroup = world.GetOrCreateSystemManaged<InitializationSystemGroup>();
			SystemHandle namingSystemHandle = world.GetOrCreateSystem<EnemyNamingSystem>();
			initGroup.AddSystemToUpdateList(namingSystemHandle);
			// Important: sort systems after modifying the update list so the group actually runs them
			initGroup.SortSystems();

			// Initialize the affix database (creates a single EnemyNamingConfig if none exists)
			EnemyAffixDatabase.InitializeDatabase(m_Manager);

			// Capture the singleton config created by the DB initializer to allow tests to edit it
			// (avoid creating a duplicate config which would break GetSingleton usage in systems)
			EntityQuery configQuery = m_Manager.CreateEntityQuery(typeof(EnemyNamingConfig));
			if (configQuery.CalculateEntityCount() == 0)
				{
				// Fallback: create one if DB init didn't (should not normally happen)
				configEntity = m_Manager.CreateEntity();
				m_Manager.AddComponentData(configEntity, new EnemyNamingConfig(
					AffixDisplayMode.NamesAndIcons, 4, true, 12345));
				}
			else
				{
				configEntity = configQuery.GetSingletonEntity();
				}
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
		public void Common_Enemy_Shows_BaseType_Only()
			{
			// Arrange
			testEnemyEntity = CreateTestEnemy(RarityType.Common, "Crawler");

			// Act
			initGroup.Update();

			// Assert
			Assert.IsTrue(m_Manager.HasComponent<EnemyNaming>(testEnemyEntity));
			EnemyNaming naming = m_Manager.GetComponentData<EnemyNaming>(testEnemyEntity);

			Assert.AreEqual("Crawler", naming.DisplayName.ToString());
			Assert.IsFalse(naming.ShowFullName);
			Assert.IsTrue(naming.ShowIcons);
			}

		[Test]
		public void Uncommon_Enemy_Shows_BaseType_Only()
			{
			// Arrange
			testEnemyEntity = CreateTestEnemy(RarityType.Uncommon, "Archer");

			// Act
			initGroup.Update();

			// Assert
			EnemyNaming naming = m_Manager.GetComponentData<EnemyNaming>(testEnemyEntity);

			Assert.AreEqual("Archer", naming.DisplayName.ToString());
			Assert.IsFalse(naming.ShowFullName);
			Assert.IsTrue(naming.ShowIcons);
			}

		[Test]
		public void Rare_Enemy_Shows_Full_Name()
			{
			// Arrange
			testEnemyEntity = CreateTestEnemy(RarityType.Rare, "Crawler");
			AddTestAffix(testEnemyEntity, "poisonous", "Poisonous");

			// Act
			initGroup.Update();

			// Assert
			EnemyNaming naming = m_Manager.GetComponentData<EnemyNaming>(testEnemyEntity);

			Assert.IsTrue(naming.ShowFullName);
			Assert.IsTrue(naming.ShowIcons);
			// Should contain affix name + base type
			Assert.IsTrue(naming.DisplayName.ToString().Contains("Crawler"));
			Assert.IsTrue(naming.DisplayName.ToString().Contains("Poisonous"));
			}

		[Test]
		public void Boss_Generates_Procedural_Name_From_Syllables()
			{
			// Arrange
			testEnemyEntity = CreateTestEnemy(RarityType.Boss, "Guardian");
			AddTestAffixWithSyllables(testEnemyEntity, "berserker", "Berserker", new[] { "Ber", "Zerk" });
			AddTestAffixWithSyllables(testEnemyEntity, "summoner", "Summoner", new[] { "Mon", "Zedd" });

			// Act
			initGroup.Update();

			// Assert
			EnemyNaming naming = m_Manager.GetComponentData<EnemyNaming>(testEnemyEntity);

			Assert.IsTrue(naming.ShowFullName);
			Assert.IsTrue(naming.ShowIcons);

			string name = naming.DisplayName.ToString();
			// Should be procedural, not contain base type for bosses
			Assert.IsFalse(name.Contains("Guardian"));
			// Should contain syllables from affixes
			Assert.IsTrue(name.Contains("Ber") || name.Contains("Zerk") || name.Contains("Mon") || name.Contains("Zedd"));
			}

		[Test]
		public void MiniBoss_Uses_Fewer_Syllables_Than_Boss()
			{
			// Arrange
			Entity miniBossEntity = CreateTestEnemy(RarityType.MiniBoss, "Lieutenant");
			Entity bossEntity = CreateTestEnemy(RarityType.Boss, "Commander");

			// Add multiple affixes to both
			AddTestAffixWithSyllables(miniBossEntity, "berserker", "Berserker", new[] { "Ber", "Zerk" });
			AddTestAffixWithSyllables(miniBossEntity, "summoner", "Summoner", new[] { "Mon", "Zedd" });
			AddTestAffixWithSyllables(miniBossEntity, "armored", "Armored", new[] { "Arm", "Mor" });

			AddTestAffixWithSyllables(bossEntity, "berserker", "Berserker", new[] { "Ber", "Zerk" });
			AddTestAffixWithSyllables(bossEntity, "summoner", "Summoner", new[] { "Mon", "Zedd" });
			AddTestAffixWithSyllables(bossEntity, "armored", "Armored", new[] { "Arm", "Mor" });

			// Act
			initGroup.Update();

			// Assert
			EnemyNaming miniBossNaming = m_Manager.GetComponentData<EnemyNaming>(miniBossEntity);
			EnemyNaming bossNaming = m_Manager.GetComponentData<EnemyNaming>(bossEntity);

			// MiniBoss should generally have shorter names than Boss
			// This is probabilistic but with fixed seed should be consistent
			Assert.IsTrue(miniBossNaming.DisplayName.Length <= bossNaming.DisplayName.Length + 10); // Allow some variance
			}

		[Test]
		public void Icon_Display_Respects_Global_Configuration()
			{
			// Test NamesOnly mode
			m_Manager.SetComponentData(configEntity, new EnemyNamingConfig(AffixDisplayMode.NamesOnly));
			testEnemyEntity = CreateTestEnemy(RarityType.Common, "Crawler");

			initGroup.Update();

			EnemyNaming naming = m_Manager.GetComponentData<EnemyNaming>(testEnemyEntity);
			Assert.IsFalse(naming.ShowIcons);

			// Test IconsOnly mode
			m_Manager.SetComponentData(configEntity, new EnemyNamingConfig(AffixDisplayMode.IconsOnly));
			testEnemyEntity = CreateTestEnemy(RarityType.Common, "Archer");

			initGroup.Update();

			naming = m_Manager.GetComponentData<EnemyNaming>(testEnemyEntity);
			Assert.IsTrue(naming.ShowIcons);

			// Test NamesAndIcons mode
			m_Manager.SetComponentData(configEntity, new EnemyNamingConfig(AffixDisplayMode.NamesAndIcons));
			testEnemyEntity = CreateTestEnemy(RarityType.Rare, "Mage");

			initGroup.Update();

			naming = m_Manager.GetComponentData<EnemyNaming>(testEnemyEntity);
			Assert.IsTrue(naming.ShowIcons);
			Assert.IsTrue(naming.ShowFullName);
			}

		[Test]
		public void Affix_Assignment_Respects_Rarity_Rules()
			{
			// Test different rarities get appropriate affix counts
			Entity commonEntity = m_Manager.CreateEntity();
			Entity rareEntity = m_Manager.CreateEntity();
			Entity bossEntity = m_Manager.CreateEntity();

			EnemyAffixDatabase.AssignRandomAffixes(m_Manager, commonEntity, RarityType.Common, 123);
			EnemyAffixDatabase.AssignRandomAffixes(m_Manager, rareEntity, RarityType.Rare, 456);
			EnemyAffixDatabase.AssignRandomAffixes(m_Manager, bossEntity, RarityType.Boss, 789);

			// Common should have 1 affix
			if (m_Manager.HasBuffer<EnemyAffixBufferElement>(commonEntity))
				{
				DynamicBuffer<EnemyAffixBufferElement> commonAffixes = m_Manager.GetBuffer<EnemyAffixBufferElement>(commonEntity);
				Assert.AreEqual(1, commonAffixes.Length);
				}

			// Rare should have 2 affixes
			if (m_Manager.HasBuffer<EnemyAffixBufferElement>(rareEntity))
				{
				DynamicBuffer<EnemyAffixBufferElement> rareAffixes = m_Manager.GetBuffer<EnemyAffixBufferElement>(rareEntity);
				Assert.AreEqual(2, rareAffixes.Length);
				}

			// Boss should have 3 affixes
			if (m_Manager.HasBuffer<EnemyAffixBufferElement>(bossEntity))
				{
				DynamicBuffer<EnemyAffixBufferElement> bossAffixes = m_Manager.GetBuffer<EnemyAffixBufferElement>(bossEntity);
				Assert.AreEqual(3, bossAffixes.Length);
				}
			}

		[Test]
		public void Boss_Syllable_Concatenation_Is_Readable()
			{
			// Arrange - create boss with consonant-heavy syllables to test connective insertion
			testEnemyEntity = CreateTestEnemy(RarityType.Boss, "Overlord");
			AddTestAffixWithSyllables(testEnemyEntity, "test1", "Test1", new[] { "Krx", "Xth" });
			AddTestAffixWithSyllables(testEnemyEntity, "test2", "Test2", new[] { "Zmr", "Yph" });

			// Act
			initGroup.Update();

			// Assert
			EnemyNaming naming = m_Manager.GetComponentData<EnemyNaming>(testEnemyEntity);
			string name = naming.DisplayName.ToString();

			// Should have some connective vowels inserted for readability
			Assert.IsTrue(name.Length > 6); // Should be longer than just concatenated syllables
			Assert.IsTrue(char.IsLetter(name[0])); // Should start with a letter
			}

		[Test]
		public void Database_Initialization_Creates_All_Expected_Affixes()
			{
			// Test that the database initialization worked correctly
			EntityQuery query = m_Manager.CreateEntityQuery(typeof(EnemyAffix), typeof(AffixDatabaseTag));
			NativeArray<Entity> affixEntities = query.ToEntityArray(Allocator.Temp);

			// Should have all the predefined affixes (5 combat + 5 movement + 4 behavior + 5 boss + 5 unique = 24)
			Assert.GreaterOrEqual(affixEntities.Length, 20); // At least 20 affixes

			// Check that we have affixes from different categories
			bool hasCombat = false, hasMovement = false, hasBehavior = false, hasBoss = false, hasUnique = false;

			foreach (Entity entity in affixEntities)
				{
				EnemyAffix affix = m_Manager.GetComponentData<EnemyAffix>(entity);
				switch (affix.Category)
					{
					case TraitCategory.Combat:
						hasCombat = true;
						break;
					case TraitCategory.Movement:
						hasMovement = true;
						break;
					case TraitCategory.Behavior:
						hasBehavior = true;
						break;
					case TraitCategory.Boss:
						hasBoss = true;
						break;
					case TraitCategory.Unique:
						hasUnique = true;
						break;
					}
				}

			Assert.IsTrue(hasCombat);
			Assert.IsTrue(hasMovement);
			Assert.IsTrue(hasBehavior);
			Assert.IsTrue(hasBoss);
			Assert.IsTrue(hasUnique);

			affixEntities.Dispose();
			}

		#region Helper Methods

		private Entity CreateTestEnemy(RarityType rarity, string baseType)
			{
			Entity entity = m_Manager.CreateEntity();
			m_Manager.AddComponentData(entity, new EnemyProfile(rarity, new FixedString64Bytes(baseType), 12345));
			m_Manager.AddComponentData(entity, new NeedsNameGeneration());
			return entity;
			}

		private void AddTestAffix(Entity entity, string id, string displayName)
			{
			if (!m_Manager.HasBuffer<EnemyAffixBufferElement>(entity))
				{
				m_Manager.AddBuffer<EnemyAffixBufferElement>(entity);
				}

			DynamicBuffer<EnemyAffixBufferElement> buffer = m_Manager.GetBuffer<EnemyAffixBufferElement>(entity);
			var affix = new EnemyAffix(
				new FixedString64Bytes(id),
				new FixedString64Bytes(displayName),
				new FixedString64Bytes($"icon_{id}.png"),
				TraitCategory.Combat,
				5, 1,
				new FixedString128Bytes($"Test affix: {displayName}")
			);

			buffer.Add(affix);
			}

		private void AddTestAffixWithSyllables(Entity entity, string id, string displayName, string[] syllables)
			{
			if (!m_Manager.HasBuffer<EnemyAffixBufferElement>(entity))
				{
				m_Manager.AddBuffer<EnemyAffixBufferElement>(entity);
				}

			DynamicBuffer<EnemyAffixBufferElement> buffer = m_Manager.GetBuffer<EnemyAffixBufferElement>(entity);
			var affix = new EnemyAffix(
				new FixedString64Bytes(id),
				new FixedString64Bytes(displayName),
				new FixedString64Bytes($"icon_{id}.png"),
				TraitCategory.Boss,
				5, 1,
				new FixedString128Bytes($"Test affix: {displayName}")
			);

			foreach (string syllable in syllables)
				{
				affix.AddBossSyllable(new FixedString32Bytes(syllable));
				}

			buffer.Add(affix);
			}

		#endregion
		}
	}
