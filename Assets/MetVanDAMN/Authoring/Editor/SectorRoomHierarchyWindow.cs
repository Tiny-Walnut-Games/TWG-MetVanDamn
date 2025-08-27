using UnityEngine;
using UnityEditor;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Authoring;
using System.Collections.Generic;
using System.Linq;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    /// <summary>
    /// Sector and room hierarchy drill-down visualization tool
    /// Addresses TODO: "Sector / room hierarchy drill-down visualization"
    /// </summary>
    public class SectorRoomHierarchyWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private string searchFilter = "";
        private string nodeIdSearchFilter = "";
        private string connectionTypeFilter = "";
        private string biomeFilter = "All";
        private bool showOnlyConnected = false;
        private bool groupByDistrict = true;
        private bool showDetailedInfo = false;
        private bool enableClickToSelect = true;
        private bool enableMultiSelect = true;
        
        private List<DistrictHierarchy> districtHierarchies = new List<DistrictHierarchy>();
        private Dictionary<uint, List<uint>> connectionMap = new Dictionary<uint, List<uint>>();
        private HashSet<string> availableBiomeTypes = new HashSet<string>();
        private HashSet<uint> selectedNodeIds = new HashSet<uint>();
        private HashSet<GameObject> highlightedObjects = new HashSet<GameObject>();

        [System.Serializable]
        private class DistrictHierarchy
        {
            public DistrictAuthoring district;
            public BiomeFieldAuthoring associatedBiome;
            public string biomeType = "Unknown";
            public List<SectorInfo> sectors = new List<SectorInfo>();
            public List<ConnectionInfo> connections = new List<ConnectionInfo>();
            public int totalRoomCount;
            public float averageConnectivity;
            public bool isExpanded = true;
        }

        [System.Serializable]
        private class SectorInfo
        {
            public int sectorId;
            public Vector3 estimatedPosition;
            public int roomCount;
            public List<RoomInfo> rooms = new List<RoomInfo>();
            public bool isExpanded = false;
        }

        [System.Serializable]
        private class RoomInfo
        {
            public int roomId;
            public Vector3 position;
            public string roomType;
            public List<string> connections = new List<string>();
        }

        [System.Serializable]
        private class ConnectionInfo
        {
            public uint targetNodeId;
            public ConnectionAuthoring connection;
            public bool hasGateCondition;
            public string connectionType;
        }

        [MenuItem("Tools/MetVanDAMN/World Debugger/Sector Room Hierarchy")]
        public static void ShowWindow()
        {
            var window = GetWindow<SectorRoomHierarchyWindow>("Sector Hierarchy");
            window.minSize = new Vector2(400, 300);
            window.RefreshHierarchyData();
        }

        private void OnEnable()
        {
            RefreshHierarchyData();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);
            
            // Header controls
            DrawHeaderControls();
            
            EditorGUILayout.Space(10);
            
            // Hierarchy content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            if (districtHierarchies.Count == 0)
            {
                DrawEmptyState();
            }
            else
            {
                DrawHierarchy();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(5);
            
            // Footer stats
            DrawFooterStats();
        }

        private void DrawHeaderControls()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                RefreshHierarchyData();
            }
            
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.Width(120));
            
            GUILayout.Space(10);
            
            // NodeId search field
            EditorGUILayout.LabelField("NodeId:", GUILayout.Width(50));
            nodeIdSearchFilter = EditorGUILayout.TextField(nodeIdSearchFilter, GUILayout.Width(80));
            
            GUILayout.Space(10);
            
            // Connection type filter
            EditorGUILayout.LabelField("Connection:", GUILayout.Width(70));
            connectionTypeFilter = EditorGUILayout.TextField(connectionTypeFilter, GUILayout.Width(100));
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            // Biome filtering dropdown
            EditorGUILayout.LabelField("Biome:", GUILayout.Width(45));
            string[] biomeOptions = new[] { "All" }.Concat(availableBiomeTypes.OrderBy(t => t)).ToArray();
            int currentBiomeIndex = System.Array.IndexOf(biomeOptions, biomeFilter);
            if (currentBiomeIndex < 0) currentBiomeIndex = 0;
            
            int newBiomeIndex = EditorGUILayout.Popup(currentBiomeIndex, biomeOptions, GUILayout.Width(100));
            if (newBiomeIndex != currentBiomeIndex && newBiomeIndex >= 0 && newBiomeIndex < biomeOptions.Length)
            {
                biomeFilter = biomeOptions[newBiomeIndex];
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Clear Filters", GUILayout.Width(80)))
            {
                searchFilter = "";
                nodeIdSearchFilter = "";
                connectionTypeFilter = "";
                biomeFilter = "All";
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            showOnlyConnected = EditorGUILayout.Toggle("Connected Only", showOnlyConnected, GUILayout.Width(120));
            groupByDistrict = EditorGUILayout.Toggle("Group by District", groupByDistrict, GUILayout.Width(130));
            showDetailedInfo = EditorGUILayout.Toggle("Show Details", showDetailedInfo, GUILayout.Width(100));
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            enableClickToSelect = EditorGUILayout.Toggle("Click to Select", enableClickToSelect, GUILayout.Width(120));
            enableMultiSelect = EditorGUILayout.Toggle("Multi-Select", enableMultiSelect, GUILayout.Width(100));
            
            GUILayout.Space(10);
            
            if (selectedNodeIds.Count > 0)
            {
                EditorGUILayout.LabelField($"Selected: {selectedNodeIds.Count}", GUILayout.Width(80));
                
                if (GUILayout.Button("Clear Selection", GUILayout.Width(100)))
                {
                    ClearSelection();
                }
            }
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Expand All", GUILayout.Width(80)))
            {
                foreach (var hierarchy in districtHierarchies)
                {
                    hierarchy.isExpanded = true;
                    foreach (var sector in hierarchy.sectors)
                    {
                        sector.isExpanded = true;
                    }
                }
            }
            
            if (GUILayout.Button("Collapse All", GUILayout.Width(80)))
            {
                foreach (var hierarchy in districtHierarchies)
                {
                    hierarchy.isExpanded = false;
                    foreach (var sector in hierarchy.sectors)
                    {
                        sector.isExpanded = false;
                    }
                }
            }
            
            if (GUILayout.Button("Select All Visible", GUILayout.Width(100)))
            {
                SelectAllVisibleObjects();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEmptyState()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.FlexibleSpace();
            
            GUIStyle centeredStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Italic
            };
            
            GUILayout.Label("No districts found in scene", centeredStyle);
            GUILayout.Space(5);
            
            GUIStyle smallCenteredStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };
            
            GUILayout.Label("Add DistrictAuthoring components to see hierarchy", smallCenteredStyle);
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawHierarchy()
        {
            var filteredHierarchies = GetFilteredHierarchies();
            
            foreach (var hierarchy in filteredHierarchies)
            {
                DrawDistrictHierarchy(hierarchy);
            }
        }

        private void DrawDistrictHierarchy(DistrictHierarchy hierarchy)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // District header
            EditorGUILayout.BeginHorizontal();
            
            hierarchy.isExpanded = EditorGUILayout.Foldout(hierarchy.isExpanded, "", true);
            
            if (GUILayout.Button("🏛️", GUILayout.Width(25)))
            {
                Selection.activeObject = hierarchy.district;
                EditorGUIUtility.PingObject(hierarchy.district);
            }
            
            EditorGUILayout.LabelField($"District {hierarchy.district.nodeId.Value}: {hierarchy.district.name}", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.LabelField($"{hierarchy.sectors.Count} sectors", EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField($"{hierarchy.totalRoomCount} rooms", EditorStyles.miniLabel, GUILayout.Width(80));
            
            EditorGUILayout.EndHorizontal();
            
            if (showDetailedInfo)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Target Sectors: {hierarchy.district.targetSectorCount}", EditorStyles.miniLabel, GUILayout.Width(120));
                EditorGUILayout.LabelField($"Connections: {hierarchy.connections.Count}", EditorStyles.miniLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField($"Connectivity: {hierarchy.averageConnectivity:F2}", EditorStyles.miniLabel, GUILayout.Width(120));
                EditorGUILayout.EndHorizontal();
            }
            
            if (hierarchy.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                // Draw connections
                if (hierarchy.connections.Count > 0)
                {
                    EditorGUILayout.LabelField("Connections:", EditorStyles.boldLabel);
                    foreach (var connection in hierarchy.connections)
                    {
                        DrawConnectionInfo(connection);
                    }
                    EditorGUILayout.Space(5);
                }
                
                // Draw sectors
                EditorGUILayout.LabelField("Sectors:", EditorStyles.boldLabel);
                foreach (var sector in hierarchy.sectors)
                {
                    DrawSectorInfo(sector);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawConnectionInfo(ConnectionInfo connection)
        {
            EditorGUILayout.BeginHorizontal();
            
            string gateIcon = connection.hasGateCondition ? "🚪" : "↔️";
            EditorGUILayout.LabelField(gateIcon, GUILayout.Width(20));
            
            EditorGUILayout.LabelField($"→ District {connection.targetNodeId}", GUILayout.Width(120));
            
            if (connection.connection != null)
            {
                EditorGUILayout.LabelField(connection.connectionType, EditorStyles.miniLabel, GUILayout.Width(80));
                
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    Selection.activeObject = connection.connection;
                    EditorGUIUtility.PingObject(connection.connection);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSectorInfo(SectorInfo sector)
        {
            EditorGUILayout.BeginHorizontal();
            
            sector.isExpanded = EditorGUILayout.Foldout(sector.isExpanded, "", true);
            
            EditorGUILayout.LabelField($"🏘️ Sector {sector.sectorId}", GUILayout.Width(100));
            EditorGUILayout.LabelField($"{sector.roomCount} rooms", EditorStyles.miniLabel, GUILayout.Width(80));
            
            if (showDetailedInfo)
            {
                EditorGUILayout.LabelField($"Pos: {sector.estimatedPosition:F1}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (sector.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                foreach (var room in sector.rooms)
                {
                    DrawRoomInfo(room);
                }
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawRoomInfo(RoomInfo room)
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField($"🏠 Room {room.roomId}", GUILayout.Width(80));
            EditorGUILayout.LabelField(room.roomType, EditorStyles.miniLabel, GUILayout.Width(100));
            
            if (showDetailedInfo)
            {
                EditorGUILayout.LabelField($"Pos: {room.position:F1}", EditorStyles.miniLabel, GUILayout.Width(120));
                EditorGUILayout.LabelField($"Links: {room.connections.Count}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFooterStats()
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            
            int totalDistricts = districtHierarchies.Count;
            int totalSectors = districtHierarchies.Sum(h => h.sectors.Count);
            int totalRooms = districtHierarchies.Sum(h => h.totalRoomCount);
            int totalConnections = districtHierarchies.Sum(h => h.connections.Count);
            
            EditorGUILayout.LabelField($"Districts: {totalDistricts}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Sectors: {totalSectors}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Rooms: {totalRooms}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Connections: {totalConnections}", EditorStyles.miniLabel);
            
            float avgSectorsPerDistrict = totalDistricts > 0 ? (float)totalSectors / totalDistricts : 0f;
            float avgRoomsPerSector = totalSectors > 0 ? (float)totalRooms / totalSectors : 0f;
            
            EditorGUILayout.LabelField($"Avg S/D: {avgSectorsPerDistrict:F1}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Avg R/S: {avgRoomsPerSector:F1}", EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshHierarchyData()
        {
            districtHierarchies.Clear();
            connectionMap.Clear();
            availableBiomeTypes.Clear();
            
            var districts = Object.FindObjectsByType<DistrictAuthoring>(FindObjectsSortMode.None);
            var connections = Object.FindObjectsByType<ConnectionAuthoring>(FindObjectsSortMode.None);
            var gates = Object.FindObjectsByType<GateConditionAuthoring>(FindObjectsSortMode.None);
            var biomes = Object.FindObjectsByType<BiomeFieldAuthoring>(FindObjectsSortMode.None);
            
            // Build biome type collection
            foreach (var biome in biomes)
            {
                if (biome != null && biome.artProfile != null && !string.IsNullOrEmpty(biome.artProfile.biomeName))
                    availableBiomeTypes.Add(biome.artProfile.biomeName);
                else
                    availableBiomeTypes.Add("Unknown");
            }
            
            // Build connection map
            foreach (var connection in connections)
            {
                if (connection == null) continue;
                uint sourceId = connection.sourceNode; // assuming now uint
                uint targetId = connection.targetNode;
                
                if (!connectionMap.ContainsKey(sourceId))
                    connectionMap[sourceId] = new List<uint>();
                
                connectionMap[sourceId].Add(targetId);
            }
            
            // Build district hierarchies with biome associations
            foreach (var district in districts)
            {
                if (district == null) continue;
                var hierarchy = BuildDistrictHierarchy(district, connections, gates, biomes);
                districtHierarchies.Add(hierarchy);
            }
            
            // Sort by node ID
            districtHierarchies.Sort((a,b)=> a.district.nodeId.CompareTo(b.district.nodeId));
        }

        private DistrictHierarchy BuildDistrictHierarchy(DistrictAuthoring district, ConnectionAuthoring[] connections, GateConditionAuthoring[] gates, BiomeFieldAuthoring[] biomes)
        {
            var hierarchy = new DistrictHierarchy { district = district };
            
            uint nodeId = district.nodeId;
            
            // Find associated biome
            var associatedBiome = biomes.FirstOrDefault(b=> b!=null && b.nodeId == nodeId);
            if (associatedBiome != null)
            {
                hierarchy.associatedBiome = associatedBiome;
                hierarchy.biomeType = associatedBiome.artProfile?.biomeName ?? "Unknown";
            }
            else
            {
                // Try to find by proximity if no exact match
                var closestBiome = biomes
                    .Where(b => b != null)
                    .OrderBy(b => Vector3.Distance(district.transform.position, b.transform.position))
                    .FirstOrDefault();
                
                if (closestBiome!=null && Vector3.Distance(district.transform.position, closestBiome.transform.position) < 10f)
                {
                    hierarchy.associatedBiome = closestBiome;
                    hierarchy.biomeType = $"{closestBiome.artProfile?.biomeName ?? "Unknown"} (nearby)";
                }
                else hierarchy.biomeType = "No Biome";
            }
            
            // Build connections
            var districtConnections = connections.Where(c=> c!=null && c.sourceNode == nodeId);
            foreach (var connection in districtConnections)
            {
                var info = new ConnectionInfo
                {
                    targetNodeId = connection.targetNode,
                    connection = connection,
                    hasGateCondition = gates.Any(g=> g!=null && g.sourceNode == connection.sourceNode && g.targetNode == connection.targetNode),
                    connectionType = DetermineConnectionType(connection)
                };
                
                hierarchy.connections.Add(info);
            }
            
            // Build sectors (simulated based on target sector count)
            int targetSectorCount = district.targetSectorCount;
            for (int i=0;i<targetSectorCount;i++)
                hierarchy.sectors.Add(GenerateSectorInfo(i, district.transform.position, targetSectorCount));
            
            hierarchy.totalRoomCount = hierarchy.sectors.Sum(s=> s.roomCount);
            hierarchy.averageConnectivity = CalculateConnectivity(nodeId);
            
            return hierarchy;
        }

        private SectorInfo GenerateSectorInfo(int sectorId, Vector3 districtPosition, int totalSectors)
        {
            var sector = new SectorInfo
            {
                sectorId = sectorId,
                estimatedPosition = CalculateSectorPosition(districtPosition, sectorId, totalSectors),
                roomCount = UnityEngine.Random.Range(2, 8) // Simulated room count
            };
            
            // Generate rooms for this sector
            for (int i = 0; i < sector.roomCount; i++)
            {
                var room = new RoomInfo
                {
                    roomId = i,
                    position = sector.estimatedPosition + UnityEngine.Random.insideUnitSphere * 5f,
                    roomType = GenerateRoomType(),
                    connections = GenerateRoomConnections(i, sector.roomCount)
                };
                
                sector.rooms.Add(room);
            }
            
            return sector;
        }

        private Vector3 CalculateSectorPosition(Vector3 districtPosition, int sectorId, int totalSectors)
        {
            if (totalSectors == 1)
                return districtPosition;
            
            float angle = (float)sectorId / totalSectors * 2f * Mathf.PI;
            float radius = 8f; // Approximate sector spacing
            
            return districtPosition + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );
        }

        private string GenerateRoomType()
        {
            string[] roomTypes = { "Standard", "Challenge", "Treasure", "Boss", "Hub", "Secret" };
            return roomTypes[UnityEngine.Random.Range(0, roomTypes.Length)];
        }

        private List<string> GenerateRoomConnections(int roomId, int totalRooms)
        {
            var connections = new List<string>();
            
            // Most rooms connect to at least one other room
            int connectionCount = UnityEngine.Random.Range(1, Mathf.Min(4, totalRooms));

            for (int i = 0; i < connectionCount; i++)
            {
                int targetRoom = UnityEngine.Random.Range(0, totalRooms);
                if (targetRoom != roomId)
                {
                    connections.Add($"Room {targetRoom}");
                }
            }
            
            return connections.Distinct().ToList();
        }

        private string DetermineConnectionType(ConnectionAuthoring connection)
        {
            // Analyze connection to determine type
            return "Standard"; // Simplified for now
        }

        private float CalculateConnectivity(uint nodeId)
        {
            if (!connectionMap.ContainsKey(nodeId))
                return 0f;
            
            // Advanced connectivity analysis using graph theory metrics
            var connections = connectionMap[nodeId];
            float baseConnectivity = connections.Count;
            
            // Calculate connectivity quality based on connection types and weights
            float qualityScore = 0f;
            float redundancyBonus = 0f;
            
            foreach (var connection in connections)
            {
                // Weight connections based on target node importance
                float targetConnectivity = connectionMap.ContainsKey(connection) ? 
                    connectionMap[connection].Count : 0f;
                
                // More connected targets are more valuable
                qualityScore += 1f + (targetConnectivity * 0.1f);
                
                // Bidirectional connections get bonus (redundancy)
                if (connectionMap.ContainsKey(connection) && 
                    connectionMap[connection].Contains(nodeId))
                {
                    redundancyBonus += 0.5f;
                }
            }
            
            // Normalize and combine metrics
            float normalizedQuality = qualityScore / Mathf.Max(1f, connections.Count);
            float normalizedRedundancy = redundancyBonus / Mathf.Max(1f, connections.Count);
            
            return baseConnectivity * (0.6f + normalizedQuality * 0.3f + normalizedRedundancy * 0.1f);
        }

        /// <summary>
        /// Selects all visible objects in the hierarchy based on current filters
        /// </summary>
        private void SelectAllVisibleObjects()
        {
            var objectsToSelect = new List<UnityEngine.Object>();
            
            foreach (var hierarchy in GetFilteredHierarchies())
            {
                if (hierarchy.district != null)
                {
                    objectsToSelect.Add(hierarchy.district.gameObject);
                }
                
                if (hierarchy.associatedBiome != null)
                {
                    objectsToSelect.Add(hierarchy.associatedBiome.gameObject);
                }
                
                foreach (var connection in hierarchy.connections)
                {
                    if (connection.connection != null)
                    {
                        objectsToSelect.Add(connection.connection.gameObject);
                    }
                }
            }
            
            if (objectsToSelect.Count > 0)
            {
                Selection.objects = objectsToSelect.ToArray();
                EditorGUIUtility.PingObject(objectsToSelect[0]);
            }
        }

        /// <summary>
        /// Gets filtered hierarchies based on current search and biome filters
        /// </summary>
        private IEnumerable<DistrictHierarchy> GetFilteredHierarchies()
        {
            return districtHierarchies.Where(h=>
            {
                // Apply general search filter
                bool matchesSearch = string.IsNullOrEmpty(searchFilter) ||
                    h.district.name.ToLower().Contains(searchFilter.ToLower()) ||
                    h.district.nodeId.ToString().Contains(searchFilter);
                
                // Apply NodeId search filter
                bool matchesNodeId = string.IsNullOrEmpty(nodeIdSearchFilter) ||
                    h.district.nodeId.ToString().Contains(nodeIdSearchFilter);
                
                // Apply connection type filter
                bool matchesConnectionType = string.IsNullOrEmpty(connectionTypeFilter) ||
                    h.connections.Any(c => c.connectionType.ToLower().Contains(connectionTypeFilter.ToLower()));
                
                // Apply biome filter
                bool matchesBiome = biomeFilter == "All" || h.biomeType.Contains(biomeFilter);
                
                // Apply connected filter
                bool matchesConnected = !showOnlyConnected || h.connections.Count > 0;
                
                return matchesSearch && matchesNodeId && matchesConnectionType && matchesBiome && matchesConnected;
            });
        }

        /// <summary>
        /// Clears all selections and highlights
        /// </summary>
        private void ClearSelection()
        {
            selectedNodeIds.Clear();
            highlightedObjects.Clear();
            Selection.objects = new UnityEngine.Object[0];
        }

        /// <summary>
        /// Enhanced click-to-select functionality with multi-select support
        /// </summary>
        private void HandleObjectSelection(UnityEngine.Object targetObject, Event currentEvent)
        {
            if (enableClickToSelect && targetObject != null && currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                // Extract NodeId if available
                uint nodeId = 0;
                if (targetObject is Component comp)
                {
                    var districtAuth = comp.GetComponent<DistrictAuthoring>();
                    if (districtAuth != null) nodeId = districtAuth.nodeId.Value;
                    
                    var biomeAuth = comp.GetComponent<BiomeFieldAuthoring>();
                    if (biomeAuth != null) nodeId = biomeAuth.nodeId.Value;
                }
                else if (targetObject is GameObject go)
                {
                    var districtAuth = go.GetComponent<DistrictAuthoring>();
                    if (districtAuth != null) nodeId = districtAuth.nodeId.Value;
                    
                    var biomeAuth = go.GetComponent<BiomeFieldAuthoring>();
                    if (biomeAuth != null) nodeId = biomeAuth.nodeId.Value;
                }
                
                if (enableMultiSelect && (currentEvent.control || currentEvent.command))
                {
                    // Multi-select mode
                    if (nodeId != 0)
                    {
                        if (selectedNodeIds.Contains(nodeId))
                        {
                            selectedNodeIds.Remove(nodeId);
                            highlightedObjects.Remove(targetObject as GameObject);
                        }
                        else
                        {
                            selectedNodeIds.Add(nodeId);
                            if (targetObject is GameObject gameObj)
                                highlightedObjects.Add(gameObj);
                        }
                    }
                    
                    // Add to Unity selection
                    var currentSelection = Selection.objects.ToList();
                    if (!currentSelection.Contains(targetObject))
                    {
                        currentSelection.Add(targetObject);
                        Selection.objects = currentSelection.ToArray();
                    }
                }
                else
                {
                    // Single select mode - clear previous selections
                    selectedNodeIds.Clear();
                    highlightedObjects.Clear();
                    
                    if (nodeId != 0)
                    {
                        selectedNodeIds.Add(nodeId);
                        if (targetObject is GameObject gameObj)
                            highlightedObjects.Add(gameObj);
                    }
                    
                    // Replace Unity selection
                    Selection.activeObject = targetObject;
                    EditorGUIUtility.PingObject(targetObject);
                }
                
                // Focus scene view on object
                SceneView.lastActiveSceneView?.FrameSelected();
                
                currentEvent.Use();
                Repaint(); // Update window to show selection changes
            }
        }
    }
}
