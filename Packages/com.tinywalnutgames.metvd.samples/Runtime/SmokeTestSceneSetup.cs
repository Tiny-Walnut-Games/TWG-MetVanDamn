using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Shared; // ✅ ADD: Use shared namespace for WorldSeed and related components
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Samples
	{
	/// <summary>
	/// Smoke test scene setup for MetVanDAMN engine
	/// Provides immediate "hit Play -> see map" experience for validation
	/// </summary>
	public class SmokeTestSceneSetup : MonoBehaviour
		{
		[Header("World Generation Parameters")]
		[SerializeField] private uint worldSeed = 42;
		[SerializeField] private int2 worldSize = new(50, 50);
		[SerializeField] private int targetSectorCount = 5;
		[SerializeField] private float biomeTransitionRadius = 10.0f;

		[Header("Debug Visualization")]
		[SerializeField] private bool enableDebugVisualization = false;
		[SerializeField] private bool logGenerationSteps = true;

		// Make these settable for tests/integration
		public EntityManager EntityManager { get; private set; }
		public World DefaultWorld { get; private set; }
		[SerializeField] private bool createdFallbackWorld = false; // ✅ Track if we created a fallback world for cleanup

		private bool _hasSetup = false;

		private void Awake()
			{
			UnityEngine.Debug.Log("🌐 MetVanDAMN Smoke Test: Awake - initializing world setup");
			UnityEngine.Debug.Log($"🌐 _hasSetup = {_hasSetup}, DefaultWorld = {(DefaultWorld?.Name ?? "null")}");
			}
		private void Start()
			{
			UnityEngine.Debug.Log("🌐 MetVanDAMN Smoke Test: Start - initializing world setup");
			UnityEngine.Debug.Log($"🌐 _hasSetup = {_hasSetup}, DefaultWorld = {(DefaultWorld?.Name ?? "null")}");
			// Only run setup if not already forced by test
			if (!_hasSetup)
				{
				UnityEngine.Debug.Log("🌐 MetVanDAMN Smoke Test: Calling SetupSmokeTestWorld from Start");
				SetupSmokeTestWorld();
				}
			else
				{
				UnityEngine.Debug.Log("🌐 MetVanDAMN Smoke Test: Skipping SetupSmokeTestWorld - already setup");
				}

			UnityEngine.Debug.Log("🌐 MetVanDAMN Smoke Test: End of Start. SetupSmokeTestWorld should have been called.");
			}

		private void Update()
			{
			// Periodically re-draw bounds if debug visualization enabled (consumes field each frame)
			if (enableDebugVisualization && Time.frameCount % 120 == 0)
				{
				DebugDrawBounds();
				}
			}

#if UNITY_EDITOR
		private void OnValidate()
			{
			// Respond immediately in editor when toggled so value is meaningfully used
			if (Application.isPlaying == false && enableDebugVisualization)
				{
				DebugDrawBounds();
				}
			}
#endif

		/// <summary>
		/// Forcibly set the world and run setup. Use in tests/integration to guarantee correct world.
		/// </summary>
		public void ForceSetup(World world)
		{
			DefaultWorld = world;
			EntityManager = world.EntityManager;
			_hasSetup = true;
			SetupSmokeTestWorld();
		}

		private void SetupSmokeTestWorld()
			{
			// ✅ PHASE 1: EXPLICIT WORLD INJECTION - No fallback world creation in tests
			// If we have an explicitly injected test world, use it directly
			if (DefaultWorld != null && DefaultWorld.IsCreated)
				{
				if (logGenerationSteps)
					{
					Debug.Log($"🎯 Using explicitly injected world: {DefaultWorld.Name}");
					}
				}
			else
				{
				// Original logic for normal gameplay scenarios
				DefaultWorld = World.DefaultGameObjectInjectionWorld;

				if (DefaultWorld == null)
					{
					if (logGenerationSteps)
						{
						Debug.LogWarning("⚠️ DefaultGameObjectInjectionWorld is null - creating fallback world for testing/standalone scenarios");
						}

					// Create a fallback world if default injection world is not available (testing scenarios)
					DefaultWorld = new World("SmokeTest_FallbackWorld");
					createdFallbackWorld = true; // Track for cleanup
					}
				}

			EntityManager = DefaultWorld.EntityManager;

			// ✅ PHASE 1 VALIDATION: Log which world we're actually using
			if (logGenerationSteps)
				{
				Debug.Log($"🧬 Entity creation will use world: {DefaultWorld.Name} (IsCreated: {DefaultWorld.IsCreated})");
				Debug.Log("🚀 MetVanDAMN Smoke Test: Starting world generation...");
				}

			CreateWorldConfiguration();
			CreateDistrictEntities();
			CreateBiomeFieldEntities();

			if (enableDebugVisualization)
				{
				DebugDrawBounds();
				}

			// ✅ PHASE 1 CONFIRMATION: Log entity counts in the world we used
			if (logGenerationSteps)
				{
				using EntityQuery seedQuery = EntityManager.CreateEntityQuery(typeof(WorldSeed));
				using EntityQuery districtQuery = EntityManager.CreateEntityQuery(typeof(NodeId));
				using EntityQuery polarityQuery = EntityManager.CreateEntityQuery(typeof(PolarityFieldData));

				Debug.Log($"✅ MetVanDAMN Smoke Test: World setup complete with seed {worldSeed}");
				Debug.Log($"   World: {DefaultWorld.Name}");
				Debug.Log($"   World size: {worldSize.x}x{worldSize.y}");
				Debug.Log($"   Target sectors: {targetSectorCount}");
				Debug.Log($"   Entities created: {seedQuery.CalculateEntityCount()} seeds, {districtQuery.CalculateEntityCount()} districts, {polarityQuery.CalculateEntityCount()} polarity fields");
				Debug.Log("   Systems will begin generation on next frame.");
				}
			}

		private void CreateWorldConfiguration()
			{
			Entity configEntity = EntityManager.CreateEntity();
			EntityManager.SetName(configEntity, "WorldConfiguration");

			// ✅ FIX: Use shared WorldSeed component
			EntityManager.AddComponentData(configEntity, new WorldSeed { Value = worldSeed });
			EntityManager.AddComponentData(configEntity, new WorldBounds
				{
				Min = new int2(-worldSize.x / 2, -worldSize.y / 2),
				Max = new int2(worldSize.x / 2, worldSize.y / 2)
				});

			// ✅ FIX: Use shared WorldGenerationConfig component
			EntityManager.AddComponentData(configEntity, new WorldGenerationConfig
				{
				WorldSeed = worldSeed, // Add explicit seed to config too
				TargetSectorCount = targetSectorCount,
				MaxDistrictCount = targetSectorCount * 4, // Allow room for subdivision
				BiomeTransitionRadius = biomeTransitionRadius
				});
			}

		private void CreateDistrictEntities()
			{
			// Use targetSectorCount to determine how many districts to create
			int actualDistrictCount = math.min(targetSectorCount, 24); // Reasonable upper limit
			int gridSize = (int)math.ceil(math.sqrt(actualDistrictCount));

			Entity hubEntity = EntityManager.CreateEntity();
			EntityManager.SetName(hubEntity, "HubDistrict");

			EntityManager.AddComponentData(hubEntity, new NodeId
				{
				Coordinates = int2.zero,
				Level = 0,
				_value = 0,
				ParentId = 0
				});

			EntityManager.AddComponentData(hubEntity, new WfcState());
			EntityManager.AddBuffer<WfcCandidateBufferElement>(hubEntity);
			EntityManager.AddBuffer<ConnectionBufferElement>(hubEntity);

			int districtId = 1;
			int districtsCreated = 0;
			int halfGrid = gridSize / 2;

			for (int x = -halfGrid; x <= halfGrid && districtsCreated < actualDistrictCount; x++)
				{
				for (int y = -halfGrid; y <= halfGrid && districtsCreated < actualDistrictCount; y++)
					{
					if (x == 0 && y == 0)
						{
						continue; // Skip hub position
						}

					Entity districtEntity = EntityManager.CreateEntity();
					EntityManager.SetName(districtEntity, $"District_{x}_{y}");

					EntityManager.AddComponentData(districtEntity, new NodeId
						{
						Coordinates = new int2(x * 10, y * 10),
						Level = (byte)(math.abs(x) + math.abs(y)),
						_value = (uint)districtId++,
						ParentId = 0
						});

					EntityManager.AddComponentData(districtEntity, new WfcState());
					EntityManager.AddBuffer<WfcCandidateBufferElement>(districtEntity);
					EntityManager.AddBuffer<ConnectionBufferElement>(districtEntity);
					EntityManager.AddComponentData(districtEntity, new SectorRefinementData(0.3f));
					EntityManager.AddBuffer<GateConditionBufferElement>(districtEntity);

					districtsCreated++;
					}
				}

			if (logGenerationSteps)
				{
				Debug.Log($"Created {districtsCreated} districts based on targetSectorCount ({targetSectorCount})");
				}
			}

		private void CreateBiomeFieldEntities()
			{
			CreatePolarityField(Polarity.Sun, new float2(15, 15), "SunField");
			CreatePolarityField(Polarity.Moon, new float2(-15, -15), "MoonField");
			CreatePolarityField(Polarity.Heat, new float2(15, -15), "HeatField");
			CreatePolarityField(Polarity.Cold, new float2(-15, 15), "ColdField");
			}

		private void CreatePolarityField(Polarity polarity, float2 center, string name)
			{
			Entity fieldEntity = EntityManager.CreateEntity();
			EntityManager.SetName(fieldEntity, name);

			EntityManager.AddComponentData(fieldEntity, new PolarityFieldData
				{
				Polarity = polarity,
				Center = center,
				Radius = biomeTransitionRadius,
				Strength = 0.8f
				});
			}

		private void DebugDrawBounds()
			{
			var color = new Color(0.2f, 0.9f, 0.4f, 0.6f);
			var half = new float3(worldSize.x * 0.5f, 0, worldSize.y * 0.5f);
			Debug.DrawLine(new Vector3(-half.x, 0, -half.z), new Vector3(half.x, 0, -half.z), color, 1f);
			Debug.DrawLine(new Vector3(half.x, 0, -half.z), new Vector3(half.x, 0, half.z), color, 1f);
			Debug.DrawLine(new Vector3(half.x, 0, half.z), new Vector3(-half.x, 0, half.z), color, 1f);
			Debug.DrawLine(new Vector3(-half.x, 0, half.z), new Vector3(-half.x, 0, -half.z), color, 1f);
			}

		private void OnDestroy()
			{
			// ✅ FIX: Proper cleanup of fallback world to prevent memory leaks
			if (createdFallbackWorld && DefaultWorld != null && DefaultWorld.IsCreated)
				{
				if (logGenerationSteps)
					{
					Debug.Log("🧹 Disposing fallback world created for testing/standalone scenario");
					}
				DefaultWorld.Dispose();
				}

			if (logGenerationSteps)
				{
				Debug.Log("🔚 MetVanDAMN Smoke Test: Scene cleanup complete");
				}
			}
		}

	// ✅ REMOVE: Duplicate WorldSeed definition - using the shared one instead
	// ✅ REMOVE: Duplicate WorldBounds definition - using the shared one instead  
	// ✅ REMOVE: Duplicate WorldGenerationConfig definition - using the shared one instead

	/// <summary>
	/// Local polarity field data component for biome field creation
	/// (This stays here as it's specific to the scene setup demo)
	/// </summary>
	public struct PolarityFieldData : IComponentData
		{
		public Polarity Polarity;
		public float2 Center;
		public float Radius;
		public float Strength;
		}
	}
