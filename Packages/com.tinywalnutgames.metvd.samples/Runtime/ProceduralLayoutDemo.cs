using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVD.Shared;

namespace TinyWalnutGames.MetVD.Samples
{
    /// <summary>
    /// Demonstration component for procedural world layout system
    /// Provides runtime validation and debugging of the complete pipeline
    /// </summary>
    public class ProceduralLayoutDemo : MonoBehaviour
    {
        [Header("Demo Configuration")]
        [SerializeField] private bool enableAutoDemo = true;
        [SerializeField] private float demoUpdateInterval = 2.0f;
        
        [Header("Layout Parameters")]
        [SerializeField] private int districtCount = 5;
        [SerializeField] private int2 worldSize = new(32, 32);
        [SerializeField] private RandomizationMode randomizationMode = RandomizationMode.Partial;
        
        [Header("Debug Output")]
        [SerializeField] private bool logLayoutProgress = true;
        [SerializeField] private bool logRuleGeneration = true;
        [SerializeField] private bool logHierarchyCreation = true;

        private EntityManager _entityManager;
        private float _lastUpdateTime;
        private bool _demoStarted = false;
        private int _currentStage = 0;

        private void Start()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("ProceduralLayoutDemo: Default World not available!");
                return;
            }
            
            _entityManager = world.EntityManager;

            if (enableAutoDemo)
            {
                StartDemo();
            }
        }

        private void Update()
        {
            if (!enableAutoDemo || !_demoStarted)
            {
                return;
            }

            if (Time.time - _lastUpdateTime > demoUpdateInterval)
            {
                CheckDemoProgress();
                _lastUpdateTime = Time.time;
            }
        }

        [ContextMenu("Start Procedural Layout Demo")]
        public void StartDemo()
        {
            Debug.Log("🧠 Starting Procedural World Layout Demo");
            
            // Clear existing demo entities
            ClearDemoEntities();
            
            // Create world configuration
            CreateWorldConfiguration();
            
            // Create unplaced districts
            CreateUnplacedDistricts();
            
            _demoStarted = true;
            _currentStage = 0;
            _lastUpdateTime = Time.time;
            
            Debug.Log($"Demo started with {districtCount} districts, world size {worldSize}, mode {randomizationMode}");
        }

        [ContextMenu("Check Current Demo Status")]
        public void CheckDemoProgress()
        {
            if (_entityManager == null)
            {
                return;
            }

            switch (_currentStage)
            {
                case 0:
                    CheckDistrictLayoutStage();
                    break;
                case 1:
                    CheckConnectionBuildingStage();
                    break;
                case 2:
                    CheckRuleRandomizationStage();
                    break;
                case 3:
                    CheckHierarchyStage();
                    break;
                case 4:
                    CheckWfcIntegrationStage();
                    break;
                default:
                    if (logLayoutProgress)
                    {
                        Debug.Log("🎉 Procedural Layout Demo Complete!");
                    }

                    break;
            }
        }

        private void CheckDistrictLayoutStage()
        {
            using EntityQuery layoutDoneQuery = _entityManager.CreateEntityQuery(typeof(DistrictLayoutDoneTag));
            
            if (layoutDoneQuery.CalculateEntityCount() > 0)
            {
                DistrictLayoutDoneTag layoutDone = layoutDoneQuery.GetSingleton<DistrictLayoutDoneTag>();
                if (logLayoutProgress)
                {
                    Debug.Log($"✅ Stage 1 Complete: District Layout ({layoutDone.DistrictCount} districts placed)");
                }

                // Check actual district positions
                LogDistrictPositions();
                _currentStage = 1;
            }
            else if (logLayoutProgress)
            {
                Debug.Log("⏳ Stage 1: Waiting for District Layout System...");
            }
        }

        private void CheckConnectionBuildingStage()
        {
            using EntityQuery layoutDoneQuery = _entityManager.CreateEntityQuery(typeof(DistrictLayoutDoneTag));
            
            if (layoutDoneQuery.CalculateEntityCount() > 0)
            {
                DistrictLayoutDoneTag layoutDone = layoutDoneQuery.GetSingleton<DistrictLayoutDoneTag>();
                if (layoutDone.ConnectionCount > 0)
                {
                    if (logLayoutProgress)
                    {
                        Debug.Log($"✅ Stage 2 Complete: Connection Building ({layoutDone.ConnectionCount} connections created)");
                    }

                    LogConnectionGraph();
                    _currentStage = 2;
                }
                else if (logLayoutProgress)
                {
                    Debug.Log("⏳ Stage 2: Waiting for Connection Builder System...");
                }
            }
        }

        private void CheckRuleRandomizationStage()
        {
            using EntityQuery rulesDoneQuery = _entityManager.CreateEntityQuery(typeof(RuleRandomizationDoneTag));
            using EntityQuery ruleSetQuery = _entityManager.CreateEntityQuery(typeof(WorldRuleSet));
            
            if (rulesDoneQuery.CalculateEntityCount() > 0 && ruleSetQuery.CalculateEntityCount() > 0)
            {
                RuleRandomizationDoneTag rulesDone = rulesDoneQuery.GetSingleton<RuleRandomizationDoneTag>();
                WorldRuleSet ruleSet = ruleSetQuery.GetSingleton<WorldRuleSet>();
                
                if (logRuleGeneration)
                {
                    Debug.Log($"✅ Stage 3 Complete: Rule Randomization (Mode: {rulesDone.Mode})");
                    Debug.Log($"   Biome Polarities: {ruleSet.BiomePolarityMask}");
                    Debug.Log($"   Available Upgrades: 0x{ruleSet.AvailableUpgradesMask:X}");
                    Debug.Log($"   Upgrades Randomized: {ruleSet.UpgradesRandomized}");
                }
                
                _currentStage = 3;
            }
            else if (logRuleGeneration)
            {
                Debug.Log("⏳ Stage 3: Waiting for Rule Randomization System...");
            }
        }

        private void CheckHierarchyStage()
        {
            using EntityQuery sectorQuery = _entityManager.CreateEntityQuery(typeof(SectorHierarchyData));
            using EntityQuery roomQuery = _entityManager.CreateEntityQuery(typeof(RoomHierarchyData));
            
            int sectorCount = sectorQuery.CalculateEntityCount();
            int roomCount = roomQuery.CalculateEntityCount();
            
            if (sectorCount > 0 || roomCount > 0)
            {
                if (logHierarchyCreation)
                {
                    Debug.Log($"✅ Stage 4 Complete: Hierarchy Creation ({sectorCount} sectors, {roomCount} rooms)");
                }

                _currentStage = 4;
            }
            else if (logHierarchyCreation)
            {
                Debug.Log("⏳ Stage 4: Waiting for Sector/Room Hierarchy System...");
            }
        }

        private void CheckWfcIntegrationStage()
        {
            using EntityQuery wfcQuery = _entityManager.CreateEntityQuery(typeof(WfcState));

            Unity.Collections.NativeArray<WfcState> wfcEntities = wfcQuery.ToComponentDataArray<WfcState>(Unity.Collections.Allocator.Temp);
            
            int inProgressCount = 0;
            int completedCount = 0;
            
            for (int i = 0; i < wfcEntities.Length; i++)
            {
                WfcState wfcState = wfcEntities[i];
                if (wfcState.State == WfcGenerationState.InProgress)
                {
                    inProgressCount++;
                }
                else if (wfcState.State == WfcGenerationState.Completed)
                {
                    completedCount++;
                }
            }
            
            wfcEntities.Dispose();
            
            if (completedCount > 0 || inProgressCount > 0)
            {
                if (logLayoutProgress)
                {
                    Debug.Log($"✅ Stage 5: WFC Integration Active ({inProgressCount} in progress, {completedCount} completed)");
                }

                _currentStage = 5;
            }
            else if (logLayoutProgress)
            {
                Debug.Log("⏳ Stage 5: Waiting for WFC System to start...");
            }
        }

        private void CreateWorldConfiguration()
        {
            Entity worldConfigEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(worldConfigEntity, new WorldConfiguration
            {
                Seed = UnityEngine.Random.Range(1, 999999),
                WorldSize = worldSize,
                TargetSectors = districtCount * 2,
                RandomizationMode = randomizationMode
            });
        }

        private void CreateUnplacedDistricts()
        {
            for (int i = 0; i < districtCount; i++)
            {
                Entity districtEntity = _entityManager.CreateEntity();
                
                // Create unplaced district (coordinates 0,0, level 0)
                _entityManager.AddComponentData(districtEntity, new NodeId((uint)(i + 1), 0, 0, new int2(0, 0)));
                _entityManager.AddComponentData(districtEntity, new WfcState(WfcGenerationState.Initialized));

                // Add sector hierarchy data
                uint sectorSeed = (uint)(i * 12345 + 6789);
                _entityManager.AddComponentData(districtEntity, new SectorHierarchyData(
                    new int2(6, 6), // 6x6 local grid
                    UnityEngine.Random.Range(2, 5), // 2-4 sectors per district
                    sectorSeed
                ));
                
                // Add connection buffer (empty initially)
                _entityManager.AddBuffer<ConnectionBufferElement>(districtEntity);
                
                // Add WFC candidate buffer (empty initially) 
                _entityManager.AddBuffer<WfcCandidateBufferElement>(districtEntity);
            }
        }

        private void LogDistrictPositions()
        {
            using EntityQuery nodeQuery = _entityManager.CreateEntityQuery(typeof(NodeId));
            Unity.Collections.NativeArray<NodeId> nodeIds = nodeQuery.ToComponentDataArray<NodeId>(Unity.Collections.Allocator.Temp);
            
            Debug.Log("📍 District Positions:");
            foreach (NodeId nodeId in nodeIds)
            {
                if (nodeId.Level == 0) // Districts only
                {
                    Debug.Log($"   District {nodeId._value}: ({nodeId.Coordinates.x}, {nodeId.Coordinates.y})");
                }
            }
            
            nodeIds.Dispose();
        }

        private void LogConnectionGraph()
        {
            using EntityQuery connectionQuery = _entityManager.CreateEntityQuery(typeof(ConnectionBufferElement));
            Unity.Collections.NativeArray<Entity> entities = connectionQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            
            Debug.Log("🔗 Connection Graph:");
            int totalConnections = 0;
            
            foreach (Entity entity in entities)
            {
                DynamicBuffer<ConnectionBufferElement> connectionBuffer = _entityManager.GetBuffer<ConnectionBufferElement>(entity);
                NodeId nodeId = _entityManager.GetComponentData<NodeId>(entity);
                
                if (nodeId.Level == 0 && connectionBuffer.Length > 0) // Districts only
                {
                    Debug.Log($"   District {nodeId._value} -> {connectionBuffer.Length} connections");
                    totalConnections += connectionBuffer.Length;
                }
            }
            
            Debug.Log($"   Total connections: {totalConnections}");
            entities.Dispose();
        }

        private void ClearDemoEntities()
        {
            // Clear existing demo entities to start fresh
            using EntityQuery allEntitiesQuery = _entityManager.CreateEntityQuery(typeof(Entity));
            Unity.Collections.NativeArray<Entity> allEntities = allEntitiesQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            
            foreach (Entity entity in allEntities)
            {
                // Only clear entities with our components (avoid clearing system entities)
                if (_entityManager.HasComponent<NodeId>(entity) ||
                    _entityManager.HasComponent<WorldConfiguration>(entity) ||
                    _entityManager.HasComponent<DistrictLayoutDoneTag>(entity) ||
                    _entityManager.HasComponent<RuleRandomizationDoneTag>(entity))
                {
                    _entityManager.DestroyEntity(entity);
                }
            }
            
            allEntities.Dispose();
        }

        private void OnGUI()
        {
            if (!enableAutoDemo)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10, Screen.height - 150, 400, 140));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Procedural Layout Demo", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            
            if (_demoStarted)
            {
                string[] stageNames = { "District Layout", "Connection Building", "Rule Randomization", "Hierarchy Creation", "WFC Integration", "Complete" };
                int displayStage = math.min(_currentStage, stageNames.Length - 1);
                
                GUILayout.Label($"Current Stage: {stageNames[displayStage]}");
                GUILayout.Label($"Districts: {districtCount} | World: {worldSize} | Mode: {randomizationMode}");
            }
            else
            {
                GUILayout.Label("Demo not started");
            }
            
            if (GUILayout.Button("Restart Demo"))
            {
                StartDemo();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}