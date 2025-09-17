using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Shared;
using TinyWalnutGames.MetVD.Samples;
using System.Collections.Generic;

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
        
        [Header("Map Visual Settings")]
        public int mapResolution = 512;
        public int minimapSize = 200;
        public Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
        public Color unexploredColor = new Color(0.3f, 0.3f, 0.3f, 1f);
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
        public Color moonBiomeColor = new Color(0.7f, 0.7f, 1f);
        public Color heatBiomeColor = Color.red;
        public Color coldBiomeColor = Color.blue;
        
        // Runtime state
        private EntityManager entityManager;
        private World defaultWorld;
        private Texture2D worldMapTexture;
        private Texture2D minimapTexture;
        private RenderTexture mapRenderTexture;
        private Camera mapCamera;
        private Canvas minimapCanvas;
        private Image minimapImage;
        private RawImage detailedMapDisplay;
        
        // Map data structures
        private WorldMapData worldMapData;
        private Dictionary<uint, DistrictMapInfo> districtInfos = new Dictionary<uint, DistrictMapInfo>();
        private Dictionary<uint, RoomMapInfo> roomInfos = new Dictionary<uint, RoomMapInfo>();
        private List<BiomeFieldMapInfo> biomeFields = new List<BiomeFieldMapInfo>();
        
        // Player tracking
        private Transform playerTransform;
        private Vector2 playerMapPosition;
        private uint currentDistrictId;
        private bool[] exploredDistricts;
        
        private void Start()
        {
            // Find the default world and entity manager
            defaultWorld = World.DefaultGameObjectInjectionWorld;
            if (defaultWorld != null)
            {
                entityManager = defaultWorld.EntityManager;
            }
            
            // Find player for tracking
            var playerMovement = FindObjectOfType<DemoPlayerMovement>();
            if (playerMovement)
            {
                playerTransform = playerMovement.transform;
            }
            
            if (autoGenerateOnWorldSetup)
            {
                // Wait a frame for world generation to complete, then generate map
                Invoke(nameof(GenerateWorldMap), 0.1f);
            }
        }
        
        /// <summary>
        /// Main entry point for generating the complete world map
        /// </summary>
        public void GenerateWorldMap()
        {
            if (entityManager == null || !defaultWorld.IsCreated)
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
            using (var configQuery = entityManager.CreateEntityQuery(typeof(WorldSeed), typeof(WorldBounds)))
            {
                if (configQuery.CalculateEntityCount() > 0)
                {
                    var entities = configQuery.ToEntityArray(Allocator.Temp);
                    var entity = entities[0];
                    
                    worldMapData.seed = entityManager.GetComponentData<WorldSeed>(entity).Value;
                    var bounds = entityManager.GetComponentData<WorldBounds>(entity);
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
            
            Debug.Log($"📈 World data extracted: {districtInfos.Count} districts, {roomInfos.Count} rooms, {biomeFields.Count} biome fields");
        }
        
        private void ExtractDistrictData()
        {
            using (var districtQuery = entityManager.CreateEntityQuery(typeof(NodeId), typeof(DistrictTag)))
            {
                var entities = districtQuery.ToEntityArray(Allocator.Temp);
                
                foreach (var entity in entities)
                {
                    var nodeId = entityManager.GetComponentData<NodeId>(entity);
                    var entityName = entityManager.GetName(entity);
                    
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
            using (var roomQuery = entityManager.CreateEntityQuery(typeof(NodeId), typeof(RoomData)))
            {
                var entities = roomQuery.ToEntityArray(Allocator.Temp);
                
                foreach (var entity in entities)
                {
                    var nodeId = entityManager.GetComponentData<NodeId>(entity);
                    var roomData = entityManager.GetComponentData<RoomData>(entity);
                    var entityName = entityManager.GetName(entity);
                    
                    var roomInfo = new RoomMapInfo
                    {
                        id = nodeId._value,
                        position = new Vector2(nodeId.Coordinates.x, nodeId.Coordinates.y),
                        districtId = roomData.DistrictId,
                        roomType = roomData.RoomType,
                        size = roomData.Size,
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
            using (var biomeQuery = entityManager.CreateEntityQuery(typeof(PolarityFieldData)))
            {
                var entities = biomeQuery.ToEntityArray(Allocator.Temp);
                
                foreach (var entity in entities)
                {
                    var fieldData = entityManager.GetComponentData<PolarityFieldData>(entity);
                    var entityName = entityManager.GetName(entity);
                    
                    var biomeInfo = new BiomeFieldMapInfo
                    {
                        polarity = fieldData.Polarity,
                        center = fieldData.Center,
                        radius = fieldData.Radius,
                        strength = fieldData.Strength,
                        name = entityName
                    };
                    
                    biomeFields.Add(biomeInfo);
                }
                
                entities.Dispose();
            }
        }
        
        /// <summary>
        /// Generates a detailed world map texture showing all districts, rooms, and biomes
        /// </summary>
        private void GenerateDetailedWorldMap()
        {
            Debug.Log("🎨 Generating detailed world map texture...");
            
            // Create map texture
            worldMapTexture = new Texture2D(mapResolution, mapResolution, TextureFormat.RGBA32, false);
            
            // Clear background
            Color[] pixels = new Color[mapResolution * mapResolution];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = backgroundColor;
            }
            
            // Draw biome fields first (background layer)
            DrawBiomeFields(pixels);
            
            // Draw districts (middle layer)
            DrawDistricts(pixels);
            
            // Draw rooms (foreground layer)
            DrawRooms(pixels);
            
            // Draw connections between districts
            DrawConnections(pixels);
            
            // Apply pixels to texture
            worldMapTexture.SetPixels(pixels);
            worldMapTexture.Apply();
            
            // Create UI display for the detailed map
            CreateDetailedMapDisplay();
        }
        
        private void DrawBiomeFields(Color[] pixels)
        {
            foreach (var biomeField in biomeFields)
            {
                Color biomeColor = GetBiomeColor(biomeField.polarity);
                Vector2 center = WorldToMapCoordinates(biomeField.center);
                float radiusInPixels = (biomeField.radius / worldMapData.worldBounds.width) * mapResolution;
                
                DrawCircle(pixels, center, radiusInPixels, biomeColor, 0.3f); // Semi-transparent
            }
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
            
            // Generate minimap content (similar to detailed map but smaller)
            Color[] minimapPixels = new Color[minimapSize * minimapSize];
            for (int i = 0; i < minimapPixels.Length; i++)
            {
                minimapPixels[i] = backgroundColor;
            }
            
            // Draw simplified version for minimap
            DrawMinimapContent(minimapPixels);
            
            minimapTexture.SetPixels(minimapPixels);
            minimapTexture.Apply();
            
            // Create minimap UI
            CreateMinimapUI();
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
            Canvas canvas = FindObjectOfType<Canvas>();
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
            Canvas canvas = FindObjectOfType<Canvas>();
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
            if (Input.GetKeyDown(KeyCode.M) && detailedMapDisplay)
            {
                detailedMapDisplay.gameObject.SetActive(!detailedMapDisplay.gameObject.activeInHierarchy);
            }
            
            // Update minimap if player moved
            if (showMinimapInGame && playerTransform && minimapImage)
            {
                UpdateMinimapPlayerPosition();
            }
            
            // Update exploration status
            UpdateExplorationStatus();
        }
        
        private void UpdateMinimapPlayerPosition()
        {
            Vector2 newPlayerPos = new Vector2(playerTransform.position.x, playerTransform.position.z);
            if (Vector2.Distance(newPlayerPos, playerMapPosition) > 1f) // Only update if moved significantly
            {
                playerMapPosition = newPlayerPos;
                
                // Regenerate minimap with new player position
                Color[] minimapPixels = new Color[minimapSize * minimapSize];
                for (int i = 0; i < minimapPixels.Length; i++)
                {
                    minimapPixels[i] = backgroundColor;
                }
                
                DrawMinimapContent(minimapPixels);
                minimapTexture.SetPixels(minimapPixels);
                minimapTexture.Apply();
            }
        }
        
        private void UpdateExplorationStatus()
        {
            if (!playerTransform) return;
            
            Vector2 playerPos = new Vector2(playerTransform.position.x, playerTransform.position.z);
            
            // Check which district the player is in
            foreach (var district in districtInfos.Values)
            {
                if (Vector2.Distance(playerPos, district.position) < 8f) // Within district bounds
                {
                    if (district.id < exploredDistricts.Length)
                    {
                        exploredDistricts[district.id] = true;
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
            
            // Similar logic for rooms
            foreach (var room in roomInfos.Values)
            {
                if (Vector2.Distance(playerPos, room.position) < 3f) // Within room bounds
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
                }
            }
        }
        
        private void ExportMapAsImage()
        {
            if (worldMapTexture == null) return;
            
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
            
            return room.roomType switch
            {
                RoomType.Chamber => chamberColor,
                RoomType.Corridor => corridorColor,
                RoomType.Hub => hubRoomColor,
                RoomType.Specialty => specialtyRoomColor,
                _ => Color.white
            };
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
        
        #region Data Structures
        
        [System.Serializable]
        private struct WorldMapData
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
        
        [System.Serializable]
        private struct BiomeFieldMapInfo
        {
            public Polarity polarity;
            public float2 center;
            public float radius;
            public float strength;
            public string name;
        }
        
        #endregion
        
        // Public API
        public void RegenerateMap() => GenerateWorldMap();
        public void ToggleDetailedMap() => detailedMapDisplay?.gameObject.SetActive(!detailedMapDisplay.gameObject.activeInHierarchy);
        public void ToggleMinimap() => minimapImage?.gameObject.SetActive(!minimapImage.gameObject.activeInHierarchy);
        public Texture2D GetWorldMapTexture() => worldMapTexture;
        public Texture2D GetMinimapTexture() => minimapTexture;
        public WorldMapData GetWorldMapData() => worldMapData;
    }
}