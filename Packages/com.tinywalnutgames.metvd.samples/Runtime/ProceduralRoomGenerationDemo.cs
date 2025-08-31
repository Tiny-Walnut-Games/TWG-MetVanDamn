using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Samples
	{
	/// <summary>
	/// Demonstration of the Procedural Room Generation Best Fit Matrix & Pipeline
	/// Shows how to create different types of rooms for various gameplay goals
	/// </summary>
	public class ProceduralRoomGenerationDemo : MonoBehaviour
		{
		[Header("Room Generation Settings")]
		[SerializeField] private readonly int roomWidth = 16;
		[SerializeField] private readonly int roomHeight = 12;
		[SerializeField] private RoomType roomType = RoomType.Normal;
		[SerializeField] private BiomeType targetBiome = BiomeType.SolarPlains;
		[SerializeField] private readonly bool useSkillGates = true;
		[SerializeField] private readonly bool enableSecrets = true;
		[SerializeField] private uint generationSeed = 12345;

		[Header("Available Player Skills")]
		[SerializeField] private readonly bool hasJump = true;
		[SerializeField] private readonly bool hasDoubleJump = false;
		[SerializeField] private readonly bool hasWallJump = false;
		[SerializeField] private readonly bool hasDash = false;
		[SerializeField] private readonly bool hasGrapple = false;
		[SerializeField] private readonly bool hasBomb = false;

		[Header("Jump Physics")]
		[SerializeField] private readonly float maxJumpHeight = 4.0f;
		[SerializeField] private readonly float maxJumpDistance = 6.0f;
		[SerializeField] private readonly float gravity = 9.81f;
		[SerializeField] private readonly float movementSpeed = 5.0f;

		[Header("Debug Visualization")]
		[SerializeField] private readonly bool showJumpArcs = true;
		[SerializeField] private readonly bool showSecretAreas = true;
		[SerializeField] private readonly bool logGenerationSteps = true;

		private World _demoWorld;
		private EntityManager _entityManager;
		private Entity _demoRoomEntity;

		private void Start ()
			{
			this.InitializeDemoWorld();
			this.CreateDemoRoom();
			}

		private void OnDestroy ()
			{
			if (this._demoWorld != null && this._demoWorld.IsCreated)
				{
				this._demoWorld.Dispose();
				}
			}

		/// <summary>
		/// Initialize the demo world with necessary systems
		/// </summary>
		private void InitializeDemoWorld ()
			{
			this._demoWorld = new World("ProceduralRoomGenerationDemo");
			this._entityManager = this._demoWorld.EntityManager;

			// Add the procedural room generation systems
			InitializationSystemGroup systemGroup = this._demoWorld.GetOrCreateSystemManaged<InitializationSystemGroup>();

			// Systems are manually added for this demonstration world
			// This provides complete control over the generation pipeline
			if (this.logGenerationSteps)
				{
				Debug.Log("Demo World initialized with procedural room generation systems");
				}
			}

		/// <summary>
		/// Create a demo room using the procedural generation pipeline
		/// </summary>
		public void CreateDemoRoom ()
			{
			if (this._entityManager == null)
				{
				return;
				}

			// Clean up previous room if it exists
			if (this._demoRoomEntity != Entity.Null && this._entityManager.Exists(this._demoRoomEntity))
				{
				this._entityManager.DestroyEntity(this._demoRoomEntity);
				}

			// Create new room entity
			this._demoRoomEntity = this._entityManager.CreateEntity();

			// Add core room data
			var roomBounds = new RectInt(0, 0, this.roomWidth, this.roomHeight);
			var roomData = new RoomHierarchyData(roomBounds, this.roomType, true);
			var nodeId = new NodeId(1, 2, 1, new int2(0, 0));

			this._entityManager.AddComponentData(this._demoRoomEntity, roomData);
			this._entityManager.AddComponentData(this._demoRoomEntity, nodeId);

			// Create biome data
			Core.Biome biome = this.CreateBiomeFromType(this.targetBiome);
			this._entityManager.AddComponentData(this._demoRoomEntity, biome);

			// Determine generator type based on Best Fit Matrix
			RoomGeneratorType generatorType = this.DetermineOptimalGenerator(this.roomType, roomBounds, this.targetBiome);

			// Build available skills mask
			Ability availableSkills = this.BuildSkillsMask();

			// Create room generation request
			var generationRequest = new RoomGenerationRequest(
				generatorType,
				this.targetBiome,
				biome.PrimaryPolarity,
				availableSkills,
				this.generationSeed
			);

			this._entityManager.AddComponentData(this._demoRoomEntity, generationRequest);

			// Add specialized components based on generator type
			this.AddGeneratorSpecificComponents(generatorType);

			// Add room management components
			this._entityManager.AddComponentData(this._demoRoomEntity, new RoomStateData(this.CalculateSecretCount()));
			this._entityManager.AddComponentData(this._demoRoomEntity, this.CreateNavigationData(roomBounds));
			this._entityManager.AddBuffer<RoomFeatureElement>(this._demoRoomEntity);

			if (this.logGenerationSteps)
				{
				Debug.Log($"Created demo room: {this.roomType} using {generatorType} generator in {this.targetBiome} biome");
				Debug.Log($"Room size: {this.roomWidth}x{this.roomHeight}, Skills: {availableSkills}");
				}
			}

		/// <summary>
		/// Determine the optimal generator based on the Best Fit Matrix
		/// </summary>
		private RoomGeneratorType DetermineOptimalGenerator (RoomType roomType, RectInt bounds, BiomeType biome)
			{
			float aspectRatio = (float)bounds.width / bounds.height;

			// Apply Best Fit Matrix logic
			return roomType switch
				{
					RoomType.Boss when this.useSkillGates => RoomGeneratorType.PatternDrivenModular,
					RoomType.Treasure => RoomGeneratorType.ParametricChallenge,
					RoomType.Save or RoomType.Shop or RoomType.Hub => RoomGeneratorType.WeightedTilePrefab,
					_ when IsSkyBiome(biome) => RoomGeneratorType.LayeredPlatformCloud,
					_ when IsTerrainBiome(biome) => RoomGeneratorType.BiomeWeightedHeightmap,
					_ when aspectRatio > 1.5f => RoomGeneratorType.LinearBranchingCorridor,
					_ when aspectRatio < 0.67f => RoomGeneratorType.StackedSegment,
					_ => RoomGeneratorType.WeightedTilePrefab
					};
			}

		/// <summary>
		/// Build skills mask from UI checkboxes
		/// </summary>
		private Ability BuildSkillsMask ()
			{
			Ability skills = Ability.None;

			if (this.hasJump)
				{
				skills |= Ability.Jump;
				}

			if (this.hasDoubleJump)
				{
				skills |= Ability.DoubleJump;
				}

			if (this.hasWallJump)
				{
				skills |= Ability.WallJump;
				}

			if (this.hasDash)
				{
				skills |= Ability.Dash;
				}

			if (this.hasGrapple)
				{
				skills |= Ability.Grapple;
				}

			if (this.hasBomb)
				{
				skills |= Ability.Bomb;
				}

			return skills;
			}

		/// <summary>
		/// Create biome data from biome type
		/// </summary>
		private Core.Biome CreateBiomeFromType (BiomeType biomeType)
			{
			Polarity polarity = biomeType switch
				{
					BiomeType.SolarPlains => Polarity.Sun,
					BiomeType.ShadowRealms => Polarity.Moon,
					BiomeType.VolcanicCore => Polarity.Heat,
					BiomeType.FrozenWastes => Polarity.Cold,
					BiomeType.SkyGardens => Polarity.Wind,
					BiomeType.PowerPlant => Polarity.Tech,
					_ => Polarity.None
					};

			return new Core.Biome(biomeType, polarity, 1.0f, Polarity.None, 1.0f);
			}

		/// <summary>
		/// Add components specific to the selected generator type
		/// </summary>
		private void AddGeneratorSpecificComponents (RoomGeneratorType generatorType)
			{
			switch (generatorType)
				{
				case RoomGeneratorType.PatternDrivenModular:
					this._entityManager.AddBuffer<RoomPatternElement>(this._demoRoomEntity);
					this._entityManager.AddBuffer<RoomModuleElement>(this._demoRoomEntity);
					break;

				case RoomGeneratorType.ParametricChallenge:
					var jumpPhysics = new JumpPhysicsData(
						this.maxJumpHeight, this.maxJumpDistance, this.gravity, this.movementSpeed,
						this.hasDoubleJump, this.hasWallJump, this.hasDash
					);
					this._entityManager.AddComponentData(this._demoRoomEntity, jumpPhysics);
					this._entityManager.AddComponentData(this._demoRoomEntity, new JumpArcValidation(false, 0, 0));
					this._entityManager.AddBuffer<JumpConnectionElement>(this._demoRoomEntity);
					break;

				case RoomGeneratorType.WeightedTilePrefab:
					if (this.enableSecrets)
						{
						var secretConfig = new SecretAreaConfig(
							0.15f, new int2(2, 2), new int2(4, 4),
							this.hasBomb ? Ability.Bomb : Ability.None, true, true
						);
						this._entityManager.AddComponentData(this._demoRoomEntity, secretConfig);
						}
					break;
				case RoomGeneratorType.VerticalSegment:
					break;
				case RoomGeneratorType.HorizontalCorridor:
					break;
				case RoomGeneratorType.BiomeWeightedTerrain:
					break;
				case RoomGeneratorType.SkyBiomePlatform:
					break;
				case RoomGeneratorType.LinearBranchingCorridor:
					break;
				case RoomGeneratorType.StackedSegment:
					break;
				case RoomGeneratorType.LayeredPlatformCloud:
					break;
				case RoomGeneratorType.BiomeWeightedHeightmap:
					break;
				default:
					break;
				}
			}

		/// <summary>
		/// Create navigation data for the room
		/// </summary>
		private RoomNavigationData CreateNavigationData (RectInt bounds)
			{
			var primaryEntrance = new int2(bounds.x + 1, bounds.y + 1);
			bool isCriticalPath = this.roomType == RoomType.Boss || this.roomType == RoomType.Entrance || this.roomType == RoomType.Exit;
			float traversalTime = this.CalculateTraversalTime(bounds);

			return new RoomNavigationData(primaryEntrance, isCriticalPath, traversalTime);
			}

		/// <summary>
		/// Calculate expected traversal time based on room size and type
		/// </summary>
		private float CalculateTraversalTime (RectInt bounds)
			{
			float baseTime = (bounds.width + bounds.height) * 0.5f;

			return this.roomType switch
				{
					RoomType.Boss => baseTime * 3.0f,      // Boss fights take longer
					RoomType.Treasure => baseTime * 2.0f,  // Puzzle rooms take longer
					RoomType.Save => baseTime * 0.5f,      // Safe rooms are quick
					_ => baseTime
					};
			}

		/// <summary>
		/// Calculate number of secrets based on room type and settings
		/// </summary>
		private int CalculateSecretCount ()
			{
			if (!this.enableSecrets)
				{
				return 0;
				}

			int area = this.roomWidth * this.roomHeight;
			return this.roomType switch
				{
					RoomType.Treasure => math.max(2, area / 20),
					RoomType.Normal => area / 40,
					RoomType.Boss => 1,
					_ => 0
					};
			}

		/// <summary>
		/// Helper methods for biome classification
		/// </summary>
		private static bool IsSkyBiome (BiomeType biome)
			{
			return biome == BiomeType.SkyGardens || biome == BiomeType.PlasmaFields;
			}

		private static bool IsTerrainBiome (BiomeType biome)
			{
			return biome == BiomeType.SolarPlains || biome == BiomeType.FrozenWastes;
			}

		/// <summary>
		/// Manual trigger for regenerating the room (for testing)
		/// </summary>
		[ContextMenu("Regenerate Room")]
		public void RegenerateRoom ()
			{
			this.generationSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
			this.CreateDemoRoom();
			}

		/// <summary>
		/// Demonstrate different generator types
		/// </summary>
		[ContextMenu("Demo All Generator Types")]
		public void DemoAllGeneratorTypes ()
			{
			RoomType originalType = this.roomType;
			BiomeType originalBiome = this.targetBiome;

			this.StartCoroutine(this.DemoGeneratorSequence(originalType, originalBiome));
			}

		private System.Collections.IEnumerator DemoGeneratorSequence (RoomType originalType, BiomeType originalBiome)
			{
			(RoomType, BiomeType, string) [ ] generatorTypes = new [ ]
			{
				(RoomType.Boss, BiomeType.VolcanicCore, "Pattern-Driven Modular - Skill Challenges"),
				(RoomType.Treasure, BiomeType.CrystalCaverns, "Parametric Challenge - Jump Testing"),
				(RoomType.Normal, BiomeType.HubArea, "Weighted Tile/Prefab - Standard Platforming"),
				(RoomType.Normal, BiomeType.SkyGardens, "Layered Platform/Cloud - Sky Biome"),
				(RoomType.Normal, BiomeType.SolarPlains, "Biome-Weighted Heightmap - Terrain")
			};

			foreach ((RoomType type, BiomeType biome, string description) in generatorTypes)
				{
				this.roomType = type;
				this.targetBiome = biome;
				this.CreateDemoRoom();

				Debug.Log($"Generated: {description}");
				yield return new WaitForSeconds(2.0f);
				}

			// Restore original settings
			this.roomType = originalType;
			this.targetBiome = originalBiome;
			this.CreateDemoRoom();
			}

		/// <summary>
		/// Visualize jump arcs and room features in the scene view
		/// </summary>
		private void OnDrawGizmos ()
			{
			if (!Application.isPlaying || this._entityManager == null || !this._entityManager.Exists(this._demoRoomEntity))
				{
				return;
				}

			this.DrawRoomBounds();

			if (this.showJumpArcs)
				{
				this.DrawJumpArcs();
				}

			if (this.showSecretAreas)
				{
				this.DrawSecretAreas();
				}
			}

		private void DrawRoomBounds ()
			{
			Gizmos.color = Color.white;
			var bounds = new Vector3(this.roomWidth, this.roomHeight, 1);
			Gizmos.DrawWireCube(this.transform.position + bounds * 0.5f, bounds);
			}

		private void DrawJumpArcs ()
			{
			if (!this._entityManager.HasBuffer<JumpConnectionElement>(this._demoRoomEntity))
				{
				return;
				}

			DynamicBuffer<JumpConnectionElement> connections = this._entityManager.GetBuffer<JumpConnectionElement>(this._demoRoomEntity);
			Gizmos.color = Color.green;

			foreach (JumpConnectionElement connection in connections)
				{
				var from = new Vector3(connection.FromPosition.x, connection.FromPosition.y, 0);
				var to = new Vector3(connection.ToPosition.x, connection.ToPosition.y, 0);

				Gizmos.DrawLine(this.transform.position + from, this.transform.position + to);
				Gizmos.DrawSphere(this.transform.position + from, 0.2f);
				Gizmos.DrawSphere(this.transform.position + to, 0.2f);
				}
			}

		private void DrawSecretAreas ()
			{
			if (!this._entityManager.HasBuffer<RoomFeatureElement>(this._demoRoomEntity))
				{
				return;
				}

			DynamicBuffer<RoomFeatureElement> features = this._entityManager.GetBuffer<RoomFeatureElement>(this._demoRoomEntity);

			foreach (RoomFeatureElement feature in features)
				{
				if (feature.Type == RoomFeatureType.Secret)
					{
					Gizmos.color = Color.yellow;
					var pos = new Vector3(feature.Position.x, feature.Position.y, 0);
					Gizmos.DrawCube(this.transform.position + pos, Vector3.one * 0.8f);
					}
				}
			}
		}
	}
