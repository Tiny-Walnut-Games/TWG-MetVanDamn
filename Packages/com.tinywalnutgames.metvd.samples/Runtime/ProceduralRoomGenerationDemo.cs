using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;

namespace TinyWalnutGames.MetVD.Samples
{
    /// <summary>
    /// Demonstration of the Procedural Room Generation Best Fit Matrix & Pipeline
    /// Shows how to create different types of rooms for various gameplay goals
    /// </summary>
    public class ProceduralRoomGenerationDemo : MonoBehaviour
    {
        [Header("Room Generation Settings")]
        [SerializeField] private int roomWidth = 16;
        [SerializeField] private int roomHeight = 12;
        [SerializeField] private RoomType roomType = RoomType.Normal;
        [SerializeField] private BiomeType targetBiome = BiomeType.SolarPlains;
        [SerializeField] private bool useSkillGates = true;
        [SerializeField] private bool enableSecrets = true;
        [SerializeField] private uint generationSeed = 12345;

        [Header("Available Player Skills")]
        [SerializeField] private bool hasJump = true;
        [SerializeField] private bool hasDoubleJump = false;
        [SerializeField] private bool hasWallJump = false;
        [SerializeField] private bool hasDash = false;
        [SerializeField] private bool hasGrapple = false;
        [SerializeField] private bool hasBomb = false;

        [Header("Jump Physics")]
        [SerializeField] private float maxJumpHeight = 4.0f;
        [SerializeField] private float maxJumpDistance = 6.0f;
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private float movementSpeed = 5.0f;

        [Header("Debug Visualization")]
        [SerializeField] private bool showJumpArcs = true;
        [SerializeField] private bool showSecretAreas = true;
        [SerializeField] private bool logGenerationSteps = true;

        private World _demoWorld;
        private EntityManager _entityManager;
        private Entity _demoRoomEntity;

        void Start()
        {
            InitializeDemoWorld();
            CreateDemoRoom();
        }

        void OnDestroy()
        {
            if (_demoWorld != null && _demoWorld.IsCreated)
            {
                _demoWorld.Dispose();
            }
        }

        /// <summary>
        /// Initialize the demo world with necessary systems
        /// </summary>
        private void InitializeDemoWorld()
        {
            _demoWorld = new World("ProceduralRoomGenerationDemo");
            _entityManager = _demoWorld.EntityManager;

            // Add the procedural room generation systems
            var systemGroup = _demoWorld.GetOrCreateSystemManaged<InitializationSystemGroup>();
            
            // Systems are manually added for this demonstration world
            // This provides complete control over the generation pipeline
            if (logGenerationSteps)
            {
                Debug.Log("Demo World initialized with procedural room generation systems");
            }
        }

        /// <summary>
        /// Create a demo room using the procedural generation pipeline
        /// </summary>
        public void CreateDemoRoom()
        {
            if (_entityManager == null) return;

            // Clean up previous room if it exists
            if (_demoRoomEntity != Entity.Null && _entityManager.Exists(_demoRoomEntity))
            {
                _entityManager.DestroyEntity(_demoRoomEntity);
            }

            // Create new room entity
            _demoRoomEntity = _entityManager.CreateEntity();

            // Add core room data
            var roomBounds = new RectInt(0, 0, roomWidth, roomHeight);
            var roomData = new RoomHierarchyData(roomBounds, roomType, true);
            var nodeId = new NodeId(1, 2, 1, new int2(0, 0));

            _entityManager.AddComponentData(_demoRoomEntity, roomData);
            _entityManager.AddComponentData(_demoRoomEntity, nodeId);

            // Create biome data
            var biome = CreateBiomeFromType(targetBiome);
            _entityManager.AddComponentData(_demoRoomEntity, biome);

            // Determine generator type based on Best Fit Matrix
            var generatorType = DetermineOptimalGenerator(roomType, roomBounds, targetBiome);
            
            // Build available skills mask
            var availableSkills = BuildSkillsMask();

            // Create room generation request
            var generationRequest = new RoomGenerationRequest(
                generatorType,
                targetBiome,
                biome.PrimaryPolarity,
                availableSkills,
                generationSeed
            );

            _entityManager.AddComponentData(_demoRoomEntity, generationRequest);

            // Add specialized components based on generator type
            AddGeneratorSpecificComponents(generatorType);

            // Add room management components
            _entityManager.AddComponentData(_demoRoomEntity, new RoomStateData(CalculateSecretCount()));
            _entityManager.AddComponentData(_demoRoomEntity, CreateNavigationData(roomBounds));
            _entityManager.AddBuffer<RoomFeatureElement>(_demoRoomEntity);

            if (logGenerationSteps)
            {
                Debug.Log($"Created demo room: {roomType} using {generatorType} generator in {targetBiome} biome");
                Debug.Log($"Room size: {roomWidth}x{roomHeight}, Skills: {availableSkills}");
            }
        }

        /// <summary>
        /// Determine the optimal generator based on the Best Fit Matrix
        /// </summary>
        private RoomGeneratorType DetermineOptimalGenerator(RoomType roomType, RectInt bounds, BiomeType biome)
        {
            var aspectRatio = (float)bounds.width / bounds.height;

            // Apply Best Fit Matrix logic
            return roomType switch
            {
                RoomType.Boss when useSkillGates => RoomGeneratorType.PatternDrivenModular,
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
        private Ability BuildSkillsMask()
        {
            var skills = Ability.None;
            
            if (hasJump) skills |= Ability.Jump;
            if (hasDoubleJump) skills |= Ability.DoubleJump;
            if (hasWallJump) skills |= Ability.WallJump;
            if (hasDash) skills |= Ability.Dash;
            if (hasGrapple) skills |= Ability.Grapple;
            if (hasBomb) skills |= Ability.Bomb;

            return skills;
        }

        /// <summary>
        /// Create biome data from biome type
        /// </summary>
        private Core.Biome CreateBiomeFromType(BiomeType biomeType)
        {
            var polarity = biomeType switch
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
        private void AddGeneratorSpecificComponents(RoomGeneratorType generatorType)
        {
            switch (generatorType)
            {
                case RoomGeneratorType.PatternDrivenModular:
                    _entityManager.AddBuffer<RoomPatternElement>(_demoRoomEntity);
                    _entityManager.AddBuffer<RoomModuleElement>(_demoRoomEntity);
                    break;

                case RoomGeneratorType.ParametricChallenge:
                    var jumpPhysics = new JumpPhysicsData(
                        maxJumpHeight, maxJumpDistance, gravity, movementSpeed,
                        hasDoubleJump, hasWallJump, hasDash
                    );
                    _entityManager.AddComponentData(_demoRoomEntity, jumpPhysics);
                    _entityManager.AddComponentData(_demoRoomEntity, new JumpArcValidation(false, 0, 0));
                    _entityManager.AddBuffer<JumpConnectionElement>(_demoRoomEntity);
                    break;

                case RoomGeneratorType.WeightedTilePrefab:
                    if (enableSecrets)
                    {
                        var secretConfig = new SecretAreaConfig(
                            0.15f, new int2(2, 2), new int2(4, 4),
                            hasBomb ? Ability.Bomb : Ability.None, true, true
                        );
                        _entityManager.AddComponentData(_demoRoomEntity, secretConfig);
                    }
                    break;
            }
        }

        /// <summary>
        /// Create navigation data for the room
        /// </summary>
        private RoomNavigationData CreateNavigationData(RectInt bounds)
        {
            var primaryEntrance = new int2(bounds.x + 1, bounds.y + 1);
            var isCriticalPath = roomType == RoomType.Boss || roomType == RoomType.Entrance || roomType == RoomType.Exit;
            var traversalTime = CalculateTraversalTime(bounds);

            return new RoomNavigationData(primaryEntrance, isCriticalPath, traversalTime);
        }

        /// <summary>
        /// Calculate expected traversal time based on room size and type
        /// </summary>
        private float CalculateTraversalTime(RectInt bounds)
        {
            var baseTime = (bounds.width + bounds.height) * 0.5f;
            
            return roomType switch
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
        private int CalculateSecretCount()
        {
            if (!enableSecrets) return 0;
            
            var area = roomWidth * roomHeight;
            return roomType switch
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
        private static bool IsSkyBiome(BiomeType biome) => 
            biome == BiomeType.SkyGardens || biome == BiomeType.PlasmaFields;

        private static bool IsTerrainBiome(BiomeType biome) => 
            biome == BiomeType.SolarPlains || biome == BiomeType.FrozenWastes;

        /// <summary>
        /// Manual trigger for regenerating the room (for testing)
        /// </summary>
        [ContextMenu("Regenerate Room")]
        public void RegenerateRoom()
        {
            generationSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
            CreateDemoRoom();
        }

        /// <summary>
        /// Demonstrate different generator types
        /// </summary>
        [ContextMenu("Demo All Generator Types")]
        public void DemoAllGeneratorTypes()
        {
            var originalType = roomType;
            var originalBiome = targetBiome;

            StartCoroutine(DemoGeneratorSequence(originalType, originalBiome));
        }

        private System.Collections.IEnumerator DemoGeneratorSequence(RoomType originalType, BiomeType originalBiome)
        {
            var generatorTypes = new[]
            {
                (RoomType.Boss, BiomeType.VolcanicCore, "Pattern-Driven Modular - Skill Challenges"),
                (RoomType.Treasure, BiomeType.CrystalCaverns, "Parametric Challenge - Jump Testing"),
                (RoomType.Normal, BiomeType.HubArea, "Weighted Tile/Prefab - Standard Platforming"),
                (RoomType.Normal, BiomeType.SkyGardens, "Layered Platform/Cloud - Sky Biome"),
                (RoomType.Normal, BiomeType.SolarPlains, "Biome-Weighted Heightmap - Terrain")
            };

            foreach (var (type, biome, description) in generatorTypes)
            {
                roomType = type;
                targetBiome = biome;
                CreateDemoRoom();
                
                Debug.Log($"Generated: {description}");
                yield return new WaitForSeconds(2.0f);
            }

            // Restore original settings
            roomType = originalType;
            targetBiome = originalBiome;
            CreateDemoRoom();
        }

        /// <summary>
        /// Visualize jump arcs and room features in the scene view
        /// </summary>
        void OnDrawGizmos()
        {
            if (!Application.isPlaying || _entityManager == null || !_entityManager.Exists(_demoRoomEntity))
                return;

            DrawRoomBounds();
            
            if (showJumpArcs)
                DrawJumpArcs();
                
            if (showSecretAreas)
                DrawSecretAreas();
        }

        private void DrawRoomBounds()
        {
            Gizmos.color = Color.white;
            var bounds = new Vector3(roomWidth, roomHeight, 1);
            Gizmos.DrawWireCube(transform.position + bounds * 0.5f, bounds);
        }

        private void DrawJumpArcs()
        {
            if (!_entityManager.HasBuffer<JumpConnectionElement>(_demoRoomEntity))
                return;

            var connections = _entityManager.GetBuffer<JumpConnectionElement>(_demoRoomEntity);
            Gizmos.color = Color.green;
            
            foreach (var connection in connections)
            {
                var from = new Vector3(connection.FromPosition.x, connection.FromPosition.y, 0);
                var to = new Vector3(connection.ToPosition.x, connection.ToPosition.y, 0);
                
                Gizmos.DrawLine(transform.position + from, transform.position + to);
                Gizmos.DrawSphere(transform.position + from, 0.2f);
                Gizmos.DrawSphere(transform.position + to, 0.2f);
            }
        }

        private void DrawSecretAreas()
        {
            if (!_entityManager.HasBuffer<RoomFeatureElement>(_demoRoomEntity))
                return;

            var features = _entityManager.GetBuffer<RoomFeatureElement>(_demoRoomEntity);
            
            foreach (var feature in features)
            {
                if (feature.Type == RoomFeatureType.Secret)
                {
                    Gizmos.color = Color.yellow;
                    var pos = new Vector3(feature.Position.x, feature.Position.y, 0);
                    Gizmos.DrawCube(transform.position + pos, Vector3.one * 0.8f);
                }
            }
        }
    }
}