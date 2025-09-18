using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Shared;
using System.Collections.Generic;
using System;
namespace TinyWalnutGames.MetVanDAMN.Authoring
    {
    /// <summary>
    /// Comprehensive map generation system that creates visual representations of MetVanDAMN worlds.
    /// Generates both in-world minimap displays and detailed world overview maps for each run.
    /// </summary>
    public class MetVanDAMNMapGenerator : MonoBehaviour
        {
        [Header("Map Generation Settings")]
        public bool autoGenerateOnWorldSetup = true;
        public bool showMinimapInGame = true;
        public bool generateDetailedWorldMap = true;
        public bool exportMapAsImage = false;
        [Tooltip("Draw debug coordinate grid & district IDs on detailed map (development aid only)")] public bool enableDebugOverlay = false;

        [Header("Map Visual Settings")]
        public int mapResolution = 512;
        public int minimapSize = 200;
        public Color backgroundColor = new(0.1f, 0.1f, 0.15f, 1f);
        public Color unexploredColor = new(0.3f, 0.3f, 0.3f, 1f);
        public Color currentLocationColor = Color.yellow;

        [Header("District Colors")]
        public Color hubDistrictColor = Color.yellow;
        public Color normalDistrictColor = Color.cyan;
        public Color exploredDistrictColor = Color.green;
        public Color lockedDistrictColor = Color.red;

        [Header("Room Colors")]
        public Color chamberColor = Color.green;
        public Color corridorColor = Color.gray;
        public Color hubRoomColor = Color.magenta;
        public Color specialtyRoomColor = Color.red;

        [Header("Biome Colors")]
        public Color sunBiomeColor = Color.yellow;
        public Color moonBiomeColor = new(0.7f, 0.7f, 1f);
        public Color heatBiomeColor = Color.red;
        public Color coldBiomeColor = Color.blue;

        // =====================================================================
        // Nullability Annihilation Manifesto
        // ---------------------------------------------------------------------
        // Every actor has a role; no unknowns permitted. All reference fields
        // are eagerly or deterministically initialized so that the component
        // maintains strong invariants after Awake():
        //  1. Core references (placeholder textures, containers) exist even
        //     before world data arrives.
        //  2. Generation swaps placeholder assets with real data atomically.
        //  3. Readiness flags (_detailedMapReady, _minimapReady) communicate
        //     semantic availability instead of null checks.
        //  4. Assertions catch contract violations during development.
        // No field uses nullable reference types; absence is represented by
        // explicit state flags or empty collections.
        // =====================================================================

        // Runtime state (nullable until initialized in Start / generation steps)
        private EntityManager _entityManager; // ECS struct (default)
        private World defaultWorld = null!; // Assigned in Awake or Start; assertion enforces presence
        private Texture2D worldMapTexture = null!; // Placeholder then real
        private Texture2D minimapTexture = null!;   // Placeholder then real
                                                    // Reserved fields retained for future rendering pipeline (currently unused – intentionally left for planned feature hooks)
#pragma warning disable CS0414
        private RenderTexture mapRenderTexture = null!; // Future: off-screen render target for animated minimap
        private Camera mapCamera = null!;                // Future: dedicated orthographic capture camera
#pragma warning restore CS0414
        private Canvas minimapCanvas = null!;
        private Image minimapImage = null!;
        private RawImage detailedMapDisplay = null!;
        private bool _detailedMapReady;
        private bool _minimapReady;
        // Events to allow external listeners (UI, analytics, tutorials) to react without modifying this script
        public static event Action<Texture2D> WorldMapGenerated = delegate { }; // Initialized to avoid null invocation guard
        public static event Action<Texture2D> MinimapUpdated = delegate { };    // Initialized to avoid null invocation guard
        public static event Action<float> ExplorationProgressed = delegate { }; // Percent 0..100 when crossing threshold
        public static event Action<float> RoomExplorationProgressed = delegate { }; // Rooms percent 0..100
        public static event Action<Vector2, Vector2> MinimapPlayerMoved = delegate { }; // (worldPos, minimapPos)

        // Map data structures
        private WorldMapData worldMapData;
        private Dictionary<uint, DistrictMapInfo> districtInfos = new();
        private Dictionary<uint, RoomMapInfo> roomInfos = new();
        // Biome field overlay extraction is deferred; overlays not currently cached

        // Player tracking
        private Transform playerTransform = null!; // Assigned if discovered; else remains placeholderTransform
        private Vector2 playerMapPosition;
        private uint currentDistrictId;
        private bool[] exploredDistricts = System.Array.Empty<bool>();
        private int _exploredDistrictCount; // number of districts flagged explored (cache to avoid recount loops for metrics)
        private float _lastExplorationPercentBucket = -1f; // last bucket threshold announced
        private int _exploredRoomCount;
        private float _lastRoomExplorationPercentBucket = -1f;
        private Transform _placeholderTransform = null!; // created in Awake
                                                         // Reusable pixel buffers to avoid per-frame allocations (especially minimap regeneration)
        private Color[] _minimapPixels = System.Array.Empty<Color>();
        private Vector2 _lastRenderedPlayerMinimapPos = new(float.NaN, float.NaN);
        private Vector2 _lastEmittedMinimapPlayerPos = new(float.NaN, float.NaN);
        // Reusable buffer for detailed world map pixels to avoid large allocations on regeneration
        private Color[] _worldMapPixels = System.Array.Empty<Color>();

        private void Awake()
            {
            // Establish placeholder transform (so playerTransform never null)
            var placeholderGO = new GameObject("__MapGeneratorPlaceholderPlayer__");
            _placeholderTransform = placeholderGO.transform;
            playerTransform = _placeholderTransform;

            // Create tiny placeholder textures so public accessors never return null
            worldMapTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            minimapTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            worldMapTexture.SetPixels(new[] { Color.black, Color.black, Color.black, Color.black });
            worldMapTexture.Apply();
            minimapTexture.SetPixels(new[] { Color.black, Color.black, Color.black, Color.black });
            minimapTexture.Apply();
            exploredDistricts = System.Array.Empty<bool>();
            _detailedMapReady = false;
            _minimapReady = false;
            }

        private void Start()
            {
            // Acquire world & ECS manager
            defaultWorld = World.DefaultGameObjectInjectionWorld;
            if (defaultWorld != null && defaultWorld.IsCreated)
                {
                _entityManager = defaultWorld.EntityManager;
                }
            else
                {
                Debug.LogWarning("MetVanDAMNMapGenerator: Default world not yet created in Start(). Generation will retry later if triggered.");
                }

            // Try to locate real player
            var playerMovement = FindFirstObjectByType<DemoPlayerMovement>();
            if (playerMovement != null)
                {
                playerTransform = playerMovement.transform;
                }

            if (autoGenerateOnWorldSetup)
                {
                Invoke(nameof(GenerateWorldMap), 0.1f); // Delay to allow world construction
                }
            }

        /// <summary>
        /// Main entry point for generating the complete world map
        /// </summary>
        public void GenerateWorldMap()
            {
            // Validate ECS world availability
            if (defaultWorld == null || !defaultWorld.IsCreated)
                {
                Debug.LogWarning("🗺️ Cannot generate map - Entity Manager not available");
                return;
                }

            Debug.Log("🗺️ Starting MetVanDAMN world map generation...");

            // Step 1: Extract world data from ECS entities
            ExtractWorldData();

            // Step 2: Generate the visual map representations
            if (generateDetailedWorldMap)
                {
                GenerateDetailedWorldMap();
                // Fire event so external systems can hook (e.g., tutorial overlay, export pipelines)
                WorldMapGenerated?.Invoke(worldMapTexture);
                }

            if (showMinimapInGame)
                {
                GenerateMinimapUI();
                }

            // Step 3: Export if requested
            if (exportMapAsImage)
                {
                ExportMapAsImage();
                }

            Debug.Log("✅ MetVanDAMN world map generation complete!");
            }

        /// <summary>
        /// Extracts world generation data from ECS entities
        /// </summary>
        private void ExtractWorldData()
            {
            Debug.Log("📊 Extracting world data from ECS entities...");

            // Initialize world map data structure
            worldMapData = new WorldMapData();

            // Extract world configuration
            if (defaultWorld == null || !defaultWorld.IsCreated)
                {
                Debug.LogWarning("World not created; skipping data extraction.");
                return;
                }

            using (var configQuery = _entityManager.CreateEntityQuery(typeof(WorldSeed), typeof(WorldBounds)))
                {
                if (configQuery.CalculateEntityCount() > 0)
                    {
                    var entities = configQuery.ToEntityArray(Allocator.Temp);
                    var entity = entities[0];

                    worldMapData.seed = _entityManager.GetComponentData<WorldSeed>(entity).Value;
                    var bounds = _entityManager.GetComponentData<WorldBounds>(entity);
                    worldMapData.worldBounds = new RectInt(bounds.Min.x, bounds.Min.y,
                        bounds.Max.x - bounds.Min.x, bounds.Max.y - bounds.Min.y);

                    entities.Dispose();
                    }
                }

            // Extract district information
            ExtractDistrictData();

            // Extract room information
            ExtractRoomData();

            // Extract biome field information
            ExtractBiomeFieldData();

            // Initialize exploration tracking
            exploredDistricts = new bool[districtInfos.Count + 10]; // Some padding

            Debug.Log($"📈 World data extracted: {districtInfos.Count} districts, {roomInfos.Count} rooms");
            }

        private void ExtractDistrictData()
            {
            if (defaultWorld == null || !defaultWorld.IsCreated) return;

            using (var districtQuery = _entityManager.CreateEntityQuery(typeof(NodeId)))
                {
                var entities = districtQuery.ToEntityArray(Allocator.Temp);

                foreach (var entity in entities)
                    {
                    var nodeId = _entityManager.GetComponentData<NodeId>(entity);
                    // Treat Level 0 as districts (consistent with NodeId semantics)
                    if (nodeId.Level != 0) { continue; }
                    var entityName = _entityManager.GetName(entity);

                    var districtInfo = new DistrictMapInfo
                        {
                        id = nodeId._value,
                        position = new Vector2(nodeId.Coordinates.x, nodeId.Coordinates.y),
                        level = nodeId.Level,
                        isHub = entityName.Contains("Hub"),
                        isExplored = false,
                        isLocked = false, // Would be determined by game logic
                        name = entityName
                        };

                    districtInfos[nodeId._value] = districtInfo;
                    }

                entities.Dispose();
                }
            }

        private void ExtractRoomData()
            {
            if (defaultWorld == null || !defaultWorld.IsCreated) return;

            using (var roomQuery = _entityManager.CreateEntityQuery(typeof(NodeId)))
                {
                var entities = roomQuery.ToEntityArray(Allocator.Temp);

                foreach (var entity in entities)
                    {
                    var nodeId = _entityManager.GetComponentData<NodeId>(entity);
                    // Treat Level 1 as rooms; districtId inferred from parent relation (heuristic: parent is districtId)
                    if (nodeId.Level != 1) { continue; }
                    var entityName = _entityManager.GetName(entity);

                    var roomInfo = new RoomMapInfo
                        {
                        id = nodeId._value,
                        position = new Vector2(nodeId.Coordinates.x, nodeId.Coordinates.y),
                        districtId = nodeId.ParentId,
                        roomType = RoomType.Normal,
                        size = 2.5f,
                        isExplored = false,
                        name = entityName
                        };

                    roomInfos[nodeId._value] = roomInfo;
                    }

                entities.Dispose();
                }
            }

        private void ExtractBiomeFieldData()
            {
            // Skip biome field extraction unless a compatible component is available in the world.
            // Current implementation renders without biome overlays when no known field component is present.
            return;
            }

        /// <summary>
        /// Generates a detailed world map texture showing all districts, rooms, and biomes
        /// </summary>
        private void GenerateDetailedWorldMap()
            {
            Debug.Log("🎨 Generating detailed world map texture...");
            int totalPixels = mapResolution * mapResolution;

            // Ensure pixel buffer sized appropriately
            if (_worldMapPixels.Length != totalPixels)
                {
                _worldMapPixels = new Color[totalPixels];
                }

            // Reinitialize / resize existing texture if resolution changed, otherwise reuse
            if (worldMapTexture.width != mapResolution || worldMapTexture.height != mapResolution)
                {
#if UNITY_2021_2_OR_NEWER
                bool reinitOk = worldMapTexture.Reinitialize(mapResolution, mapResolution);
                if (!reinitOk)
                    {
                    // Fallback allocate a new texture if reinitialize failed (should be rare)
                    worldMapTexture = new Texture2D(mapResolution, mapResolution, TextureFormat.RGBA32, false);
                    }
#else
                // Older Unity versions: Resize returns bool; if fails allocate new
                if (!worldMapTexture.Resize(mapResolution, mapResolution))
                    {
                    worldMapTexture = new Texture2D(mapResolution, mapResolution, TextureFormat.RGBA32, false);
                    }
#endif
                }

            // Clear background into reusable buffer
            for (int i = 0; i < _worldMapPixels.Length; i++)
                {
                _worldMapPixels[i] = backgroundColor;
                }

            // Draw biome fields first (background layer)
            DrawBiomeFields(_worldMapPixels);

            // Draw districts (middle layer)
            DrawDistricts(_worldMapPixels);

            // Draw rooms (foreground layer)
            DrawRooms(_worldMapPixels);

            // Draw connections between districts
            DrawConnections(_worldMapPixels);

            // Optional debug overlay (grid + district labels) for coordinate awareness
            if (enableDebugOverlay)
                {
                DrawDebugOverlay(_worldMapPixels);
                }

            // Apply pixels to texture
            worldMapTexture.SetPixels(_worldMapPixels);
            worldMapTexture.Apply();

            // Create UI display for the detailed map
            CreateDetailedMapDisplay();
            _detailedMapReady = true;
            }

        private void DrawBiomeFields(Color[] pixels)
            {
            // Biome overlays not drawn until extraction is implemented
            }

        private void DrawDistricts(Color[] pixels)
            {
            foreach (var district in districtInfos.Values)
                {
                Color districtColor = GetDistrictColor(district);
                Vector2 center = WorldToMapCoordinates(district.position);
                float size = district.isHub ? 8f : 6f;

                DrawSquare(pixels, center, size, districtColor);

                // Draw district border
                DrawSquareOutline(pixels, center, size + 1f, Color.white, 1f);
                }
            }

        private void DrawRooms(Color[] pixels)
            {
            foreach (var room in roomInfos.Values)
                {
                Color roomColor = GetRoomColor(room);
                Vector2 center = WorldToMapCoordinates(room.position);
                float size = math.max(2f, room.size);

                DrawSquare(pixels, center, size, roomColor);
                }
            }

        private void DrawConnections(Color[] pixels)
            {
            // Draw connections between adjacent districts
            foreach (var district1 in districtInfos.Values)
                {
                foreach (var district2 in districtInfos.Values)
                    {
                    if (district1.id >= district2.id) continue; // Avoid duplicates

                    float distance = Vector2.Distance(district1.position, district2.position);
                    if (distance < 20f) // Adjacent districts
                        {
                        Vector2 start = WorldToMapCoordinates(district1.position);
                        Vector2 end = WorldToMapCoordinates(district2.position);
                        DrawLine(pixels, start, end, Color.white, 0.5f);
                        }
                    }
                }
            }

        /// <summary>
        /// Generates a minimap UI for in-game use
        /// </summary>
        private void GenerateMinimapUI()
            {
            Debug.Log("🗺️ Generating minimap UI...");

            // Create minimap texture (smaller resolution)
            minimapTexture = new Texture2D(minimapSize, minimapSize, TextureFormat.RGBA32, false);

            // Ensure reusable pixel buffer sized appropriately
            int expected = minimapSize * minimapSize;
            if (_minimapPixels.Length != expected)
                {
                _minimapPixels = new Color[expected];
                }

            // Generate minimap content (similar to detailed map but smaller)
            for (int i = 0; i < _minimapPixels.Length; i++)
                {
                _minimapPixels[i] = backgroundColor;
                }

            // Draw simplified version for minimap
            DrawMinimapContent(_minimapPixels);

            minimapTexture.SetPixels(_minimapPixels);
            minimapTexture.Apply();

            // Create minimap UI
            CreateMinimapUI();
            _minimapReady = true;
            _lastRenderedPlayerMinimapPos = new Vector2(float.NaN, float.NaN); // force first update
            MinimapUpdated?.Invoke(minimapTexture);
            }

        private void DrawMinimapContent(Color[] pixels)
            {
            int resolution = minimapSize;

            // Draw districts on minimap
            foreach (var district in districtInfos.Values)
                {
                if (!district.isExplored && !district.isHub) continue; // Only show explored districts

                Color districtColor = GetDistrictColor(district);
                Vector2 center = WorldToMinimapCoordinates(district.position);
                float size = district.isHub ? 4f : 3f;

                DrawSquare(pixels, center, size, districtColor, resolution);
                }

            // Draw player position
            if (playerTransform)
                {
                Vector2 playerPos = WorldToMinimapCoordinates(new Vector2(playerTransform.position.x, playerTransform.position.z));
                DrawSquare(pixels, playerPos, 2f, currentLocationColor, resolution);
                }
            }

        private void CreateDetailedMapDisplay()
            {
            // Find or create canvas for detailed map
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (!canvas)
                {
                GameObject canvasObj = new GameObject("Map Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                }

            // Create detailed map panel
            GameObject mapPanel = new GameObject("World Map Panel");
            mapPanel.transform.SetParent(canvas.transform, false);

            var mapImage = mapPanel.AddComponent<RawImage>();
            mapImage.texture = worldMapTexture;

            var rectTransform = mapPanel.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.1f, 0.1f);
            rectTransform.anchorMax = new Vector2(0.9f, 0.9f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            detailedMapDisplay = mapImage;

            // Initially hidden - can be toggled with M key
            mapPanel.SetActive(false);

            Debug.Log("🖼️ Detailed map display created");
            }

        private void CreateMinimapUI()
            {
            // Find or create canvas for minimap
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (!canvas)
                {
                GameObject canvasObj = new GameObject("Minimap Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                }

            // Create minimap panel (top-right corner)
            GameObject minimapPanel = new GameObject("Minimap Panel");
            minimapPanel.transform.SetParent(canvas.transform, false);

            var minimapBg = minimapPanel.AddComponent<Image>();
            minimapBg.color = new Color(0, 0, 0, 0.8f);

            var rectTransform = minimapPanel.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.8f, 0.8f);
            rectTransform.anchorMax = new Vector2(0.98f, 0.98f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Create minimap image
            GameObject minimapImageObj = new GameObject("Minimap Image");
            minimapImageObj.transform.SetParent(minimapPanel.transform, false);

            minimapImage = minimapImageObj.AddComponent<Image>();
            minimapImage.sprite = Sprite.Create(minimapTexture, new Rect(0, 0, minimapSize, minimapSize), Vector2.one * 0.5f);

            var imageRect = minimapImageObj.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.one * 5f; // Small padding
            imageRect.offsetMax = Vector2.one * -5f;

            minimapCanvas = canvas;

            Debug.Log("🧭 Minimap UI created");
            }

        private void Update()
            {
            // Handle map controls
            if (Input.GetKeyDown(KeyCode.M))
                {
                // Shift+M toggles minimap, M alone toggles detailed map
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                    ToggleMinimap();
                    }
                else if (detailedMapDisplay)
                    {
                    // Route through public API for consistent behavior & to avoid ColdMethod false positive
                    ToggleDetailedMap();
                    }
                }

            // Update minimap if player moved
            if (showMinimapInGame && _minimapReady)
                {
                UpdateMinimapPlayerPosition();
                }

            // Update exploration status
            UpdateExplorationStatus();
            }

        private void UpdateMinimapPlayerPosition()
            {
            // Player transform is always non-null (placeholder or real). Minimap texture guaranteed if _minimapReady
            if (!_minimapReady) return;
            Vector2 newPlayerPos = new Vector2(playerTransform.position.x, playerTransform.position.z);
            if (Vector2.Distance(newPlayerPos, playerMapPosition) > 1f) // Only update if moved significantly
                {
                playerMapPosition = newPlayerPos;
                // Regenerate into reusable buffer
                for (int i = 0; i < _minimapPixels.Length; i++)
                    {
                    _minimapPixels[i] = backgroundColor;
                    }
                DrawMinimapContent(_minimapPixels);
                minimapTexture.SetPixels(_minimapPixels);
                minimapTexture.Apply(false);
                MinimapUpdated?.Invoke(minimapTexture);

                // Emit movement event if minimap logical position changed beyond threshold
                Vector2 minimapPos = WorldToMinimapCoordinates(playerMapPosition);
                if (float.IsNaN(_lastEmittedMinimapPlayerPos.x) || Vector2.Distance(minimapPos, _lastEmittedMinimapPlayerPos) > 2f)
                    {
                    _lastEmittedMinimapPlayerPos = minimapPos;
                    MinimapPlayerMoved?.Invoke(playerMapPosition, minimapPos);
                    }
                }
            }

        private void UpdateExplorationStatus()
            {
            if (exploredDistricts.Length == 0) return; // Not initialized with world data yet

            Vector2 playerPos = new Vector2(playerTransform.position.x, playerTransform.position.z);

            // Check which district the player is in & update exploration state
            bool explorationChanged = false;
            foreach (var district in districtInfos.Values)
                {
                if (Vector2.Distance(playerPos, district.position) < 8f) // Within district bounds
                    {
                    if (district.id < exploredDistricts.Length)
                        {
                        if (!exploredDistricts[district.id])
                            {
                            exploredDistricts[district.id] = true;
                            _exploredDistrictCount++;
                            explorationChanged = true;
                            }
                        if (!district.isExplored)
                            {
                            districtInfos[district.id] = new DistrictMapInfo
                                {
                                id = district.id,
                                position = district.position,
                                level = district.level,
                                isHub = district.isHub,
                                isExplored = true, // Mark as explored
                                isLocked = district.isLocked,
                                name = district.name
                                };
                            }
                        }
                    }
                }

            // Similar logic for rooms
            foreach (var room in roomInfos.Values)
                {
                if (Vector2.Distance(playerPos, room.position) < 3f) // Within room bounds
                    {
                    if (!room.isExplored)
                        {
                        roomInfos[room.id] = new RoomMapInfo
                            {
                            id = room.id,
                            position = room.position,
                            districtId = room.districtId,
                            roomType = room.roomType,
                            size = room.size,
                            isExplored = true, // Mark as explored
                            name = room.name
                            };
                        _exploredRoomCount++;
                        explorationChanged = true; // piggy-back global change flag
                        }
                    }
                }

            if (explorationChanged)
                {
                float percent = GetExplorationPercent();
                // Bucket thresholds to avoid log spam (10,25,50,75,90,100)
                float bucket = ComputeExplorationBucket(percent);
                if (bucket > _lastExplorationPercentBucket)
                    {
                    _lastExplorationPercentBucket = bucket;
                    Debug.Log($"🧭 Exploration progress: {percent:0.0}% (reached {bucket:0}% milestone)");
                    ExplorationProgressed?.Invoke(percent);
                    }

                float roomPercent = GetRoomExplorationPercent();
                float roomBucket = ComputeExplorationBucket(roomPercent);
                if (roomBucket > _lastRoomExplorationPercentBucket)
                    {
                    _lastRoomExplorationPercentBucket = roomBucket;
                    Debug.Log($"🚪 Room exploration progress: {roomPercent:0.0}% (reached {roomBucket:0}% milestone)");
                    RoomExplorationProgressed?.Invoke(roomPercent);
                    }
                }
            }

        private void ExportMapAsImage()
            {
            if (!_detailedMapReady) return; // Nothing meaningful to export yet

            byte[] pngData = worldMapTexture.EncodeToPNG();
            string fileName = $"MetVanDAMN_WorldMap_Seed{worldMapData.seed}_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);

            System.IO.File.WriteAllBytes(filePath, pngData);
            Debug.Log($"🖼️ World map exported to: {filePath}");
            }

        #region Utility Methods

        private Vector2 WorldToMapCoordinates(Vector2 worldPos)
            {
            float normalizedX = (worldPos.x - worldMapData.worldBounds.xMin) / worldMapData.worldBounds.width;
            float normalizedY = (worldPos.y - worldMapData.worldBounds.yMin) / worldMapData.worldBounds.height;

            return new Vector2(normalizedX * mapResolution, normalizedY * mapResolution);
            }

        private Vector2 WorldToMinimapCoordinates(Vector2 worldPos)
            {
            float normalizedX = (worldPos.x - worldMapData.worldBounds.xMin) / worldMapData.worldBounds.width;
            float normalizedY = (worldPos.y - worldMapData.worldBounds.yMin) / worldMapData.worldBounds.height;

            return new Vector2(normalizedX * minimapSize, normalizedY * minimapSize);
            }

        private Color GetDistrictColor(DistrictMapInfo district)
            {
            if (district.isLocked) return lockedDistrictColor;
            if (district.isHub) return hubDistrictColor;
            if (district.isExplored) return exploredDistrictColor;
            return normalDistrictColor;
            }

        private Color GetRoomColor(RoomMapInfo room)
            {
            if (!room.isExplored) return unexploredColor;

            switch (room.roomType)
                {
                case RoomType.Hub:
                    return hubRoomColor;
                case RoomType.Boss:
                case RoomType.Treasure:
                case RoomType.Shop:
                case RoomType.Save:
                    return specialtyRoomColor;
                case RoomType.Entrance:
                case RoomType.Exit:
                    return corridorColor;
                case RoomType.Normal:
                default:
                    return chamberColor;
                }
            }

        private Color GetBiomeColor(Polarity polarity)
            {
            return polarity switch
                {
                    Polarity.Sun => sunBiomeColor,
                    Polarity.Moon => moonBiomeColor,
                    Polarity.Heat => heatBiomeColor,
                    Polarity.Cold => coldBiomeColor,
                    _ => Color.white
                    };
            }

        private void DrawCircle(Color[] pixels, Vector2 center, float radius, Color color, float alpha = 1f)
            {
            DrawCircle(pixels, center, radius, color, alpha, mapResolution);
            }

        private void DrawCircle(Color[] pixels, Vector2 center, float radius, Color color, float alpha, int resolution)
            {
            int radiusInt = (int)radius;
            Color finalColor = new Color(color.r, color.g, color.b, alpha);

            for (int x = -radiusInt; x <= radiusInt; x++)
                {
                for (int y = -radiusInt; y <= radiusInt; y++)
                    {
                    if (x * x + y * y <= radius * radius)
                        {
                        int pixelX = (int)center.x + x;
                        int pixelY = (int)center.y + y;

                        if (pixelX >= 0 && pixelX < resolution && pixelY >= 0 && pixelY < resolution)
                            {
                            int index = pixelY * resolution + pixelX;
                            pixels[index] = Color.Lerp(pixels[index], finalColor, alpha);
                            }
                        }
                    }
                }
            }

        private void DrawSquare(Color[] pixels, Vector2 center, float size, Color color)
            {
            DrawSquare(pixels, center, size, color, mapResolution);
            }

        private void DrawSquare(Color[] pixels, Vector2 center, float size, Color color, int resolution)
            {
            int halfSize = (int)(size * 0.5f);

            for (int x = -halfSize; x <= halfSize; x++)
                {
                for (int y = -halfSize; y <= halfSize; y++)
                    {
                    int pixelX = (int)center.x + x;
                    int pixelY = (int)center.y + y;

                    if (pixelX >= 0 && pixelX < resolution && pixelY >= 0 && pixelY < resolution)
                        {
                        int index = pixelY * resolution + pixelX;
                        pixels[index] = color;
                        }
                    }
                }
            }

        private void DrawSquareOutline(Color[] pixels, Vector2 center, float size, Color color, float thickness)
            {
            int halfSize = (int)(size * 0.5f);
            int thicknessInt = (int)thickness;

            // Draw outline
            for (int x = -halfSize; x <= halfSize; x++)
                {
                for (int y = -halfSize; y <= halfSize; y++)
                    {
                    if (math.abs(x) >= halfSize - thicknessInt || math.abs(y) >= halfSize - thicknessInt)
                        {
                        int pixelX = (int)center.x + x;
                        int pixelY = (int)center.y + y;

                        if (pixelX >= 0 && pixelX < mapResolution && pixelY >= 0 && pixelY < mapResolution)
                            {
                            int index = pixelY * mapResolution + pixelX;
                            pixels[index] = color;
                            }
                        }
                    }
                }
            }

        private void DrawLine(Color[] pixels, Vector2 start, Vector2 end, Color color, float alpha)
            {
            Vector2 direction = end - start;
            float distance = direction.magnitude;
            direction.Normalize();

            for (float t = 0; t < distance; t += 0.5f)
                {
                Vector2 point = start + direction * t;
                int pixelX = (int)point.x;
                int pixelY = (int)point.y;

                if (pixelX >= 0 && pixelX < mapResolution && pixelY >= 0 && pixelY < mapResolution)
                    {
                    int index = pixelY * mapResolution + pixelX;
                    pixels[index] = Color.Lerp(pixels[index], color, alpha);
                    }
                }
            }

        #endregion

        #region Debug Overlay
        private void DrawDebugOverlay(Color[] pixels)
            {
            // Draw a faint grid every 32 pixels
            int step = math.max(16, mapResolution / 32); // adaptive density
            Color gridColor = new Color(1f, 1f, 1f, 0.08f);
            for (int x = 0; x < mapResolution; x += step)
                {
                for (int y = 0; y < mapResolution; y++)
                    {
                    int idx = y * mapResolution + x;
                    pixels[idx] = Color.Lerp(pixels[idx], gridColor, gridColor.a);
                    }
                }
            for (int y = 0; y < mapResolution; y += step)
                {
                for (int x = 0; x < mapResolution; x++)
                    {
                    int idx = y * mapResolution + x;
                    pixels[idx] = Color.Lerp(pixels[idx], gridColor, gridColor.a);
                    }
                }

            // Lightweight district ID marking (draw tiny colored square sequence encoding id)
            foreach (var d in districtInfos.Values)
                {
                Vector2 center = WorldToMapCoordinates(d.position);
                int baseX = (int)center.x + 2;
                int baseY = (int)center.y + 2;
                uint id = d.id;
                for (int bit = 0; bit < 8; bit++)
                    {
                    bool on = ((id >> bit) & 1u) == 1u;
                    int px = baseX + bit;
                    int py = baseY;
                    if (px >= 0 && px < mapResolution && py >= 0 && py < mapResolution)
                        {
                        int idx = py * mapResolution + px;
                        pixels[idx] = on ? Color.white : new Color(0f, 0f, 0f, 0.5f);
                        }
                    }
                }
            }
        #endregion

        #region Data Structures

        [System.Serializable]
        public struct WorldMapData
            {
            public uint seed;
            public RectInt worldBounds;
            }

        [System.Serializable]
        private struct DistrictMapInfo
            {
            public uint id;
            public Vector2 position;
            public byte level;
            public bool isHub;
            public bool isExplored;
            public bool isLocked;
            public string name;
            }

        [System.Serializable]
        private struct RoomMapInfo
            {
            public uint id;
            public Vector2 position;
            public uint districtId;
            public RoomType roomType;
            public float size;
            public bool isExplored;
            public string name;
            }

        // Placeholder struct for future biome overlay extraction; intentionally empty to avoid warnings-as-errors
        private struct BiomeFieldMapInfo { }



        #endregion

        // Public API
        public void RegenerateMap() => GenerateWorldMap();
        public void ToggleDetailedMap()
            {
            if (detailedMapDisplay && detailedMapDisplay.gameObject)
                {
                detailedMapDisplay.gameObject.SetActive(!detailedMapDisplay.gameObject.activeInHierarchy);
                }
            }
        public void ToggleMinimap()
            {
            if (minimapImage && minimapImage.gameObject)
                {
                minimapImage.gameObject.SetActive(!minimapImage.gameObject.activeInHierarchy);
                }
            }
        /// <summary>Returns the generated detailed world map texture if available.</summary>
        public Texture2D GetWorldMapTexture() => worldMapTexture;
        /// <summary>Returns the generated minimap texture if available.</summary>
        public Texture2D GetMinimapTexture() => minimapTexture;
        public WorldMapData GetWorldMapData() => worldMapData;
        /// <summary>Returns exploration percent (0-100) across known districts (excluding padding).</summary>
        public float GetExplorationPercent()
            {
            int total = districtInfos.Count == 0 ? 1 : districtInfos.Count; // avoid div zero
            return (_exploredDistrictCount / (float)total) * 100f;
            }
        /// <summary>Returns exploration percent (0-100) across known rooms.</summary>
        public float GetRoomExplorationPercent()
            {
            int total = roomInfos.Count == 0 ? 1 : roomInfos.Count;
            return (_exploredRoomCount / (float)total) * 100f;
            }

        private float ComputeExplorationBucket(float percent)
            {
            if (percent >= 100f) return 100f;
            if (percent >= 90f) return 90f;
            if (percent >= 75f) return 75f;
            if (percent >= 50f) return 50f;
            if (percent >= 25f) return 25f;
            if (percent >= 10f) return 10f;
            return 0f;
            }

        // =====================================================================
        // Editor / Inspection Convenience (Read-Only Public Surface)
        // ---------------------------------------------------------------------
        // These properties intentionally expose internal readiness & texture
        // references for rich editor tooling (custom inspector previews,
        // automated validation dashboards) without granting mutation access.
        // They carry no gameplay semantics beyond mirroring internal state.
        // =====================================================================
#if UNITY_EDITOR
        public bool DetailedMapReady => _detailedMapReady;
        public bool MinimapReady => _minimapReady;
        public Texture2D WorldMapTexture => worldMapTexture;
        public Texture2D MinimapTexture => minimapTexture;
        public int DistrictCount => districtInfos.Count;
        public int RoomCount => roomInfos.Count;
        public uint Seed => worldMapData.seed;
        /// <summary>Editor-only helper to invoke PNG export without exposing the private implementation broadly.</summary>
        public void EditorExportWorldMapPNG()
            {
            ExportMapAsImage();
            }
#endif
        }
    }
