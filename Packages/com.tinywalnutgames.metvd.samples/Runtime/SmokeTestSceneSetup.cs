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

		/// <summary>
		/// Set up world context for tests without running full setup. Allows individual method testing.
		/// </summary>
		public void SetupTestContext(World world)
			{
			DefaultWorld = world;
			EntityManager = world.EntityManager;
			_hasSetup = true;
			// Don't call SetupSmokeTestWorld() - let tests call individual methods
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

			// ✅ NEW: Create visual representations after entities are created
			if (Application.isPlaying)
				{
				CreateVisualRepresentations();
				}

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
			// Idempotence guard: if districts already exist, skip
			using (EntityQuery existingDistricts = EntityManager.CreateEntityQuery(typeof(DistrictTag)))
				{
				if (existingDistricts.CalculateEntityCount() > 0)
					{
					if (logGenerationSteps)
						Debug.Log("⚠️ CreateDistrictEntities skipped (districts already present)");
					return;
					}
				}
			// Use targetSectorCount to determine how many districts to create
			int actualDistrictCount = math.min(targetSectorCount, 24); // Reasonable upper limit
			int gridSize = (int)math.ceil(math.sqrt(actualDistrictCount));

			Entity hubEntity = EntityManager.CreateEntity();
			EntityManager.SetName(hubEntity, "HubDistrict");
			// Tag as district (rooms won't get this)
			EntityManager.AddComponentData(hubEntity, new DistrictTag());

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

			// Create rooms for hub district
			CreateRoomsForDistrict(hubEntity, int2.zero, 0);

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
					EntityManager.AddComponentData(districtEntity, new DistrictTag());

					var coordinates = new int2(x * 10, y * 10);
					EntityManager.AddComponentData(districtEntity, new NodeId
						{
						Coordinates = coordinates,
						Level = (byte)(math.abs(x) + math.abs(y)),
						_value = (uint)districtId,
						ParentId = 0
						});

					EntityManager.AddComponentData(districtEntity, new WfcState());
					EntityManager.AddBuffer<WfcCandidateBufferElement>(districtEntity);
					EntityManager.AddBuffer<ConnectionBufferElement>(districtEntity);
					EntityManager.AddComponentData(districtEntity, new SectorRefinementData(0.3f));
					EntityManager.AddBuffer<GateConditionBufferElement>(districtEntity);

					// Create rooms for this district
					CreateRoomsForDistrict(districtEntity, coordinates, districtId);

					districtId++;
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
			// Idempotence guard: if polarity fields already exist, skip
			using (EntityQuery existingFields = EntityManager.CreateEntityQuery(typeof(PolarityFieldData)))
				{
				if (existingFields.CalculateEntityCount() >= 4) // expected full set
					{
					if (logGenerationSteps)
						Debug.Log("⚠️ CreateBiomeFieldEntities skipped (polarity fields already present)");
					return;
					}
				}
			CreatePolarityField(Polarity.Sun, new float2(15, 15), "SunField");
			CreatePolarityField(Polarity.Moon, new float2(-15, -15), "MoonField");
			CreatePolarityField(Polarity.Heat, new float2(15, -15), "HeatField");
			CreatePolarityField(Polarity.Cold, new float2(-15, 15), "ColdField");
			}

		/// <summary>
		/// Creates room entities within a district for demonstration purposes
		/// Each district gets 2-6 rooms arranged in a small grid pattern
		/// </summary>
		private void CreateRoomsForDistrict(Entity districtEntity, int2 districtCenter, int districtId)
			{
			var random = new Unity.Mathematics.Random(worldSeed + (uint)districtId + 1000u);
			int roomCount = random.NextInt(2, 7); // 2-6 rooms per district

			// Arrange rooms in a small grid around the district center
			int roomGridSize = (int)math.ceil(math.sqrt(roomCount));
			float roomSpacing = 3f; // Spacing between rooms
			float halfSpread = (roomGridSize - 1) * roomSpacing * 0.5f;

			int roomsCreated = 0;
			for (int x = 0; x < roomGridSize && roomsCreated < roomCount; x++)
				{
				for (int y = 0; y < roomGridSize && roomsCreated < roomCount; y++)
					{
					Entity roomEntity = EntityManager.CreateEntity();
					string roomName = $"Room_{districtId}_{roomsCreated}";
					EntityManager.SetName(roomEntity, roomName);

					// Position room relative to district center
					float2 roomOffset = new float2(
						(x * roomSpacing) - halfSpread,
						(y * roomSpacing) - halfSpread);
					int2 roomCoordinates = districtCenter + (int2)roomOffset;

					EntityManager.AddComponentData(roomEntity, new NodeId
						{
						Coordinates = roomCoordinates,
						Level = 1, // Rooms are level 1 (districts are level 0)
						_value = (uint)(districtId * 1000 + roomsCreated), // Unique room ID
						ParentId = (uint)districtId // Reference parent district
						});

					EntityManager.AddComponentData(roomEntity, new WfcState());
					EntityManager.AddBuffer<WfcCandidateBufferElement>(roomEntity);
					EntityManager.AddBuffer<ConnectionBufferElement>(roomEntity);

					// Add room-specific component for visual identification
					EntityManager.AddComponentData(roomEntity, new RoomData
						{
						RoomType = (RoomType)(roomsCreated % 4), // Cycle through room types
						Size = random.NextFloat(1.5f, 3.5f),
						DistrictId = (uint)districtId
						});

					roomsCreated++;
					}
				}

			if (logGenerationSteps)
				{
				Debug.Log($"Created {roomsCreated} rooms for district {districtId}");
				}
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

		/// <summary>
		/// Creates visual GameObjects to represent the ECS entities in the scene
		/// This bridges the gap between ECS data and visible scene objects
		/// </summary>
		private void CreateVisualRepresentations()
			{
			if (logGenerationSteps)
				{
				Debug.Log("🎨 Creating visual representations for districts, rooms, and polarity fields...");
				}

			CreateDistrictVisuals();
			CreateRoomVisuals();
			CreatePolarityFieldVisuals();

			if (logGenerationSteps)
				{
				Debug.Log("✨ Visual representations created successfully!");
				}
			}

		private void CreateDistrictVisuals()
			{
			using EntityQuery districtQuery = EntityManager.CreateEntityQuery(typeof(NodeId));
			Unity.Collections.NativeArray<Entity> entities = districtQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

			foreach (Entity entity in entities)
				{
				NodeId nodeId = EntityManager.GetComponentData<NodeId>(entity);
				string entityName = EntityManager.GetName(entity);

				// Skip rooms (Level 1) - only show districts (Level 0)
				if (nodeId.Level != 0) continue;

				// Create a visual cube for each district
				GameObject districtVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
				districtVisual.name = $"Visual_{entityName}";
				districtVisual.transform.position = new Vector3(nodeId.Coordinates.x, 1, nodeId.Coordinates.y);
				districtVisual.transform.localScale = Vector3.one * 2f;

				// Color based on whether it's the hub or regular district
				Renderer renderer = districtVisual.GetComponent<Renderer>();
				if (entityName.Contains("Hub"))
					{
					renderer.material.color = Color.yellow; // Hub = yellow
					districtVisual.transform.localScale = Vector3.one * 3f; // Larger for hub
					}
				else
					{
					renderer.material.color = Color.cyan; // Districts = cyan
					}

				// Parent to this component's GameObject for organization
				districtVisual.transform.SetParent(transform);
				}

			entities.Dispose();
			}

		private void CreateRoomVisuals()
			{
			using EntityQuery roomQuery = EntityManager.CreateEntityQuery(typeof(NodeId), typeof(RoomData));
			Unity.Collections.NativeArray<Entity> entities = roomQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

			foreach (Entity entity in entities)
				{
				NodeId nodeId = EntityManager.GetComponentData<NodeId>(entity);
				RoomData roomData = EntityManager.GetComponentData<RoomData>(entity);
				string entityName = EntityManager.GetName(entity);

				// Create different shapes based on room type
				GameObject roomVisual = roomData.RoomType switch
					{
						RoomType.Chamber => GameObject.CreatePrimitive(PrimitiveType.Cube),
						RoomType.Corridor => GameObject.CreatePrimitive(PrimitiveType.Capsule),
						RoomType.Hub => GameObject.CreatePrimitive(PrimitiveType.Sphere),
						RoomType.Specialty => GameObject.CreatePrimitive(PrimitiveType.Cylinder),
						_ => GameObject.CreatePrimitive(PrimitiveType.Cube)
						};

				roomVisual.name = $"Visual_{entityName}";
				roomVisual.transform.position = new Vector3(nodeId.Coordinates.x, 0.5f, nodeId.Coordinates.y);
				roomVisual.transform.localScale = Vector3.one * (roomData.Size * 0.5f); // Scale based on room size

				// Color based on room type
				Renderer renderer = roomVisual.GetComponent<Renderer>();
				renderer.material.color = roomData.RoomType switch
					{
						RoomType.Chamber => Color.green,
						RoomType.Corridor => Color.gray,
						RoomType.Hub => Color.magenta,
						RoomType.Specialty => Color.red,
						_ => Color.white
						};

				// Make rooms slightly transparent to distinguish from districts
				Color color = renderer.material.color;
				renderer.material.color = new Color(color.r, color.g, color.b, 0.8f);

				// Parent to this component's GameObject for organization
				roomVisual.transform.SetParent(transform);
				}

			entities.Dispose();
			}

		private void CreatePolarityFieldVisuals()
			{
			using EntityQuery polarityQuery = EntityManager.CreateEntityQuery(typeof(PolarityFieldData));
			Unity.Collections.NativeArray<Entity> entities = polarityQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

			foreach (Entity entity in entities)
				{
				PolarityFieldData fieldData = EntityManager.GetComponentData<PolarityFieldData>(entity);
				string entityName = EntityManager.GetName(entity);

				// Create a visual sphere for each polarity field
				GameObject fieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				fieldVisual.name = $"Visual_{entityName}";
				fieldVisual.transform.position = new Vector3(fieldData.Center.x, 0.5f, fieldData.Center.y);
				fieldVisual.transform.localScale = Vector3.one * (fieldData.Radius / 5f); // Scale based on radius

				// Color based on polarity type
				Renderer renderer = fieldVisual.GetComponent<Renderer>();
				renderer.material.color = fieldData.Polarity switch
					{
						Polarity.Sun => Color.yellow,
						Polarity.Moon => new Color(0.7f, 0.7f, 1f), // Light blue
						Polarity.Heat => Color.red,
						Polarity.Cold => Color.blue,
						_ => Color.white
						};

				// Make it semi-transparent to show field nature
				renderer.material.color = new Color(renderer.material.color.r, renderer.material.color.g, renderer.material.color.b, 0.6f);

				// Parent to this component's GameObject for organization
				fieldVisual.transform.SetParent(transform);
				}

			entities.Dispose();
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
	/// Tag component identifying district (level 0) entities so tests can distinguish
	/// them from room (child) entities that also have NodeId.
	/// </summary>
	public struct DistrictTag : IComponentData { }

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

	/// <summary>
	/// Room data component for visual identification and debugging
	/// Helps distinguish different room types in the generated world
	/// </summary>
	public struct RoomData : IComponentData
		{
		public RoomType RoomType;
		public float Size;
		public uint DistrictId;
		}

	/// <summary>
	/// Types of rooms that can be generated for visual variety
	/// </summary>
	public enum RoomType : byte
		{
		Chamber = 0,
		Corridor = 1,
		Hub = 2,
		Specialty = 3
		}
	}
