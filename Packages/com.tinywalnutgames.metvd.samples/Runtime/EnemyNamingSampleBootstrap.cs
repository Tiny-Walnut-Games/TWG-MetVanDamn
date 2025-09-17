using Unity.Entities;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;
using Unity.Collections;

namespace TinyWalnutGames.MetVD.Samples
	{
	/// <summary>
	/// Sample demonstrating how to use the Enemy Naming and Affix Display System
	/// Shows creation of enemies with different rarities and affix configurations
	/// </summary>
	public class EnemyNamingSampleBootstrap : MonoBehaviour
		{
		[Header("Sample Configuration")]
		[SerializeField] private bool initializeOnStart = true;
		[SerializeField] private bool createSampleEnemies = true;
		[SerializeField] private AffixDisplayMode displayMode = AffixDisplayMode.NamesAndIcons;

		[Header("Sample Enemy Types")]
		[SerializeField] private int commonEnemyCount = 3;
		[SerializeField] private int rareEnemyCount = 2;
		[SerializeField] private int bossCount = 1;

		private EntityManager entityManager;

		private void Start()
			{
			if (initializeOnStart)
				{
				InitializeNamingSystem();
				}

			if (createSampleEnemies)
				{
				CreateSampleEnemies();
				}
			}

		/// <summary>
		/// Initialize the enemy naming system
		/// </summary>
		[ContextMenu("Initialize Naming System")]
		public void InitializeNamingSystem()
			{
			entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

			// Initialize the affix database
			EnemyAffixDatabase.InitializeDatabase(entityManager);

			// Set global configuration
			EntityQuery query = entityManager.CreateEntityQuery(typeof(EnemyNamingConfig));
			NativeArray<Entity> configEntities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

			if (configEntities.Length > 0)
				{
				EnemyNamingConfig config = entityManager.GetComponentData<EnemyNamingConfig>(configEntities[0]);
				config.GlobalDisplayMode = displayMode;
				entityManager.SetComponentData(configEntities[0], config);
				}

			configEntities.Dispose();

			Debug.Log("🎯 Enemy Naming System initialized with affix database");
			}

		/// <summary>
		/// Create sample enemies to demonstrate the naming system
		/// </summary>
		[ContextMenu("Create Sample Enemies")]
		public void CreateSampleEnemies()
			{
			if (entityManager == default)
				{
				entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
				}

			uint seedCounter = 1000;

			// Create common enemies
			for (int i = 0; i < commonEnemyCount; i++)
				{
				CreateSampleEnemy(RarityType.Common, GetRandomBaseType(), seedCounter++);
				}

			// Create rare enemies
			for (int i = 0; i < rareEnemyCount; i++)
				{
				CreateSampleEnemy(RarityType.Rare, GetRandomBaseType(), seedCounter++);
				}

			// Create bosses
			for (int i = 0; i < bossCount; i++)
				{
				CreateSampleEnemy(RarityType.Boss, GetRandomBossType(), seedCounter++);
				}

			Debug.Log($"✨ Created {commonEnemyCount + rareEnemyCount + bossCount} sample enemies");
			}

		/// <summary>
		/// Create a single sample enemy
		/// </summary>
		private void CreateSampleEnemy(RarityType rarity, string baseType, uint seed)
			{
			Entity enemyEntity = entityManager.CreateEntity();

			// Add enemy profile
			entityManager.AddComponentData(enemyEntity, new EnemyProfile(rarity, baseType, seed));

			// Assign random affixes based on rarity
			EnemyAffixDatabase.AssignRandomAffixes(entityManager, enemyEntity, rarity, seed);

			// Mark for name generation
			entityManager.AddComponentData(enemyEntity, new NeedsNameGeneration());

			// Add some additional components for demo purposes
			entityManager.AddComponentData(enemyEntity, new EnemyMeleeTag());

			// Set name for debugging
			entityManager.SetName(enemyEntity, $"Enemy_{rarity}_{baseType}_{seed}");

			Debug.Log($"📝 Created {rarity} enemy '{baseType}' with seed {seed}");
			}

		/// <summary>
		/// Get a random base enemy type name
		/// </summary>
		private string GetRandomBaseType()
			{
			string[] baseTypes = { "Crawler", "Archer", "Mage", "Scout", "Warrior", "Guardian", "Sentinel" };
			return baseTypes[Random.Range(0, baseTypes.Length)];
			}

		/// <summary>
		/// Get a random boss type name
		/// </summary>
		private string GetRandomBossType()
			{
			string[] bossTypes = { "Overlord", "Champion", "Warden", "Titan", "Ancient", "Harbinger" };
			return bossTypes[Random.Range(0, bossTypes.Length)];
			}

		/// <summary>
		/// Display information about all named enemies
		/// </summary>
		[ContextMenu("Display Enemy Names")]
		public void DisplayEnemyNames()
			{
			if (entityManager == default)
				{
				entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
				}

			EntityQuery query = entityManager.CreateEntityQuery(typeof(EnemyProfile), typeof(EnemyNaming));
			NativeArray<Entity> entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

			Debug.Log($"🎭 Displaying names for {entities.Length} enemies:");

			foreach (Entity entity in entities)
				{
				EnemyProfile profile = entityManager.GetComponentData<EnemyProfile>(entity);
				EnemyNaming naming = entityManager.GetComponentData<EnemyNaming>(entity);

				string affixInfo = "";
				if (entityManager.HasBuffer<EnemyAffixBufferElement>(entity))
					{
					DynamicBuffer<EnemyAffixBufferElement> affixes = entityManager.GetBuffer<EnemyAffixBufferElement>(entity);
					affixInfo = $" (Affixes: {affixes.Length})";
					}

				Debug.Log($"  {profile.Rarity}: '{naming.DisplayName}' " +
						 $"[Base: {profile.BaseType}] " +
						 $"[FullName: {naming.ShowFullName}] " +
						 $"[Icons: {naming.ShowIcons}]" +
						 affixInfo);
				}

			entities.Dispose();
			}

		/// <summary>
		/// Change display mode and regenerate names
		/// </summary>
		[ContextMenu("Toggle Display Mode")]
		public void ToggleDisplayMode()
			{
			displayMode = displayMode switch
				{
					AffixDisplayMode.NamesOnly => AffixDisplayMode.IconsOnly,
					AffixDisplayMode.IconsOnly => AffixDisplayMode.NamesAndIcons,
					AffixDisplayMode.NamesAndIcons => AffixDisplayMode.NamesOnly,
					_ => AffixDisplayMode.NamesAndIcons
					};

			// Update global configuration
			EntityQuery query = entityManager.CreateEntityQuery(typeof(EnemyNamingConfig));
			NativeArray<Entity> configEntities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

			if (configEntities.Length > 0)
				{
				EnemyNamingConfig config = entityManager.GetComponentData<EnemyNamingConfig>(configEntities[0]);
				config.GlobalDisplayMode = displayMode;
				entityManager.SetComponentData(configEntities[0], config);
				}

			configEntities.Dispose();

			Debug.Log($"🔄 Display mode changed to: {displayMode}");
			}

		/// <summary>
		/// Create a specific example from the problem statement
		/// </summary>
		[ContextMenu("Create Problem Statement Examples")]
		public void CreateProblemStatementExamples()
			{
			if (entityManager == default)
				{
				entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
				}

			// Example: Common "Crawler" + [Shuffler icon]
			Entity commonCrawler = CreateExampleEnemy(RarityType.Common, "Crawler", new[] { "shuffling" });

			// Example: Uncommon "Archer" + [Teleport icon] [Poison icon]
			Entity uncommonArcher = CreateExampleEnemy(RarityType.Uncommon, "Archer", new[] { "teleporting", "poisonous" });

			// Example: Rare "Venomous Archer of Fury" + [Poison icon] [Berserker icon]
			Entity rareArcher = CreateExampleEnemy(RarityType.Rare, "Archer", new[] { "poisonous", "berserker" });

			// Example: Boss with Berserker + Summoner + should create "Bermonzedd" or similar
			Entity boss = CreateExampleEnemy(RarityType.Boss, "Guardian", new[] { "berserker", "summoner" });

			Debug.Log("📚 Created problem statement examples:");
			Debug.Log("  - Common Crawler with Shuffling");
			Debug.Log("  - Uncommon Archer with Teleporting + Poisonous");
			Debug.Log("  - Rare Archer with Poisonous + Berserker");
			Debug.Log("  - Boss with Berserker + Summoner (procedural name)");
			}

		/// <summary>
		/// Create an enemy with specific affixes for demonstration
		/// </summary>
		private Entity CreateExampleEnemy(RarityType rarity, string baseType, string[] affixIds)
			{
			Entity enemyEntity = entityManager.CreateEntity();

			// Add enemy profile
			entityManager.AddComponentData(enemyEntity, new EnemyProfile(rarity, baseType, 99999));

			// Add specific affixes
			DynamicBuffer<EnemyAffixBufferElement> affixBuffer = entityManager.AddBuffer<EnemyAffixBufferElement>(enemyEntity);
			EntityQuery affixQuery = entityManager.CreateEntityQuery(typeof(EnemyAffix), typeof(AffixDatabaseTag));
			NativeArray<Entity> affixEntities = affixQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

			foreach (string affixId in affixIds)
				{
				foreach (Entity affixEntity in affixEntities)
					{
					EnemyAffix affix = entityManager.GetComponentData<EnemyAffix>(affixEntity);
					if (affix.Id.ToString() == affixId)
						{
						affixBuffer.Add(affix);
						break;
						}
					}
				}

			affixEntities.Dispose();

			// Mark for name generation
			entityManager.AddComponentData(enemyEntity, new NeedsNameGeneration());

			// Set name for debugging
			entityManager.SetName(enemyEntity, $"Example_{rarity}_{baseType}");

			return enemyEntity;
			}
		}
	}
