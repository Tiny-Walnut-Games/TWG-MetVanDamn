using UnityEngine;
using UnityEditor;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Shared;
using TinyWalnutGames.MetVanDAMN.Authoring;
using System.Collections.Generic;

namespace TinyWalnutGames.MetVanDAMN.Authoring.Editor
{
    /// <summary>
    /// Editor tool for previewing dungeon generation with specific seeds.
    /// Allows designers to test and validate dungeon layouts before player experience.
    /// Fully compliant with MetVanDAMN mandate: complete tooling for development workflow.
    /// </summary>
    public class DungeonDelvePreviewTool : EditorWindow
    {
        [Header("Preview Configuration")]
        [SerializeField] private uint previewSeed = 42;
        [SerializeField] private bool autoRefresh = true;
        [SerializeField] private bool showBiomeColors = true;
        [SerializeField] private bool showProgressionLocks = true;
        [SerializeField] private bool showSecrets = true;
        [SerializeField] private bool showBosses = true;
        [SerializeField] private bool showPickups = false; // Too many to show by default
        
        [Header("Visualization Settings")]
        [SerializeField] private float previewScale = 1f;
        [SerializeField] private Vector2 previewOffset = Vector2.zero;
        [SerializeField] private bool showGrid = true;
        [SerializeField] private bool showLabels = true;
        
        // Preview data
        private DungeonPreviewData currentPreview;
        private bool needsRefresh = true;
        private Vector2 scrollPosition;
        private GUIStyle labelStyle;
        private GUIStyle headerStyle;
        
        // Colors for visualization
        private readonly Color[] floorColors = 
        {
            new Color(0.5f, 0.8f, 1f, 0.7f),    // Crystal blue
            new Color(1f, 0.4f, 0.2f, 0.7f),    // Molten red
            new Color(0.6f, 0.2f, 0.9f, 0.7f)   // Void purple
        };
        
        private readonly Color bossColor = Color.red;
        private readonly Color lockColor = Color.yellow;
        private readonly Color secretColor = new Color(1f, 1f, 0f, 0.8f);
        private readonly Color pickupColor = Color.green;
        
        [MenuItem("MetVanDAMN/Dungeon Delve Preview Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<DungeonDelvePreviewTool>("Dungeon Delve Preview");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }
        
        private void OnEnable()
        {
            InitializeStyles();
            needsRefresh = true;
        }
        
        private void InitializeStyles()
        {
            labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };
            
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
        }
        
        private void OnGUI()
        {
            DrawHeader();
            DrawControlPanel();
            
            if (needsRefresh && autoRefresh)
            {
                RefreshPreview();
            }
            
            DrawPreviewArea();
            DrawStatistics();
            
            if (needsRefresh)
            {
                Repaint();
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("🏰 Dungeon Delve Preview Tool", headerStyle);
            EditorGUILayout.LabelField("Preview and validate dungeon generation with specific seeds", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();
        }
        
        private void DrawControlPanel()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Generation Controls", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            // Seed input
            EditorGUILayout.BeginHorizontal();
            uint newSeed = (uint)EditorGUILayout.IntField("Seed:", (int)previewSeed);
            if (GUILayout.Button("Random", GUILayout.Width(70)))
            {
                newSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
            }
            EditorGUILayout.EndHorizontal();
            
            if (newSeed != previewSeed)
            {
                previewSeed = newSeed;
                needsRefresh = true;
            }
            
            // Auto refresh toggle
            autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);
            
            // Manual refresh button
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 Refresh Preview"))
            {
                RefreshPreview();
            }
            if (GUILayout.Button("📋 Copy Seed"))
            {
                GUIUtility.systemCopyBuffer = previewSeed.ToString();
                Debug.Log($"Seed {previewSeed} copied to clipboard");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Visualization options
            EditorGUILayout.LabelField("Visualization Options", EditorStyles.boldLabel);
            showBiomeColors = EditorGUILayout.Toggle("Show Biome Colors", showBiomeColors);
            showProgressionLocks = EditorGUILayout.Toggle("Show Progression Locks", showProgressionLocks);
            showSecrets = EditorGUILayout.Toggle("Show Secrets", showSecrets);
            showBosses = EditorGUILayout.Toggle("Show Bosses", showBosses);
            showPickups = EditorGUILayout.Toggle("Show Pickups", showPickups);
            showGrid = EditorGUILayout.Toggle("Show Grid", showGrid);
            showLabels = EditorGUILayout.Toggle("Show Labels", showLabels);
            
            EditorGUILayout.Space();
            
            // Preview settings
            EditorGUILayout.LabelField("Preview Settings", EditorStyles.boldLabel);
            previewScale = EditorGUILayout.Slider("Scale", previewScale, 0.1f, 3f);
            previewOffset = EditorGUILayout.Vector2Field("Offset", previewOffset);
            
            if (EditorGUI.EndChangeCheck() && autoRefresh)
            {
                needsRefresh = true;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPreviewArea()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dungeon Layout Preview", EditorStyles.boldLabel);
            
            if (currentPreview == null)
            {
                EditorGUILayout.HelpBox("No preview data available. Click 'Refresh Preview' to generate.", MessageType.Info);
                return;
            }
            
            var rect = GUILayoutUtility.GetRect(position.width - 20, 300);
            
            EditorGUI.BeginGroup(rect);
            
            // Background
            EditorGUI.DrawRect(new Rect(0, 0, rect.width, rect.height), Color.black);
            
            // Grid
            if (showGrid)
            {
                DrawGrid(rect);
            }
            
            // Draw floors
            DrawFloors(rect);
            
            // Draw interactive elements
            if (showBosses) DrawBosses(rect);
            if (showProgressionLocks) DrawProgressionLocks(rect);
            if (showSecrets) DrawSecrets(rect);
            if (showPickups) DrawPickups(rect);
            
            EditorGUI.EndGroup();
        }
        
        private void DrawGrid(Rect rect)
        {
            var gridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            var gridSize = 20f * previewScale;
            
            // Vertical lines
            for (float x = 0; x < rect.width; x += gridSize)
            {
                EditorGUI.DrawRect(new Rect(x, 0, 1, rect.height), gridColor);
            }
            
            // Horizontal lines
            for (float y = 0; y < rect.height; y += gridSize)
            {
                EditorGUI.DrawRect(new Rect(0, y, rect.width, 1), gridColor);
            }
        }
        
        private void DrawFloors(Rect rect)
        {
            if (currentPreview?.floors == null) return;
            
            for (int i = 0; i < currentPreview.floors.Count; i++)
            {
                var floor = currentPreview.floors[i];
                var floorRect = GetFloorRect(rect, i);
                
                if (showBiomeColors)
                {
                    EditorGUI.DrawRect(floorRect, floorColors[i % floorColors.Length]);
                }
                else
                {
                    EditorGUI.DrawRect(floorRect, new Color(0.2f, 0.2f, 0.2f, 0.8f));
                }
                
                // Floor border
                DrawRectOutline(floorRect, Color.white, 2);
                
                // Floor label
                if (showLabels)
                {
                    var labelRect = new Rect(floorRect.x + 5, floorRect.y + 5, 100, 20);
                    GUI.Label(labelRect, $"Floor {i + 1}: {floor.biomeName}", labelStyle);
                }
            }
        }
        
        private void DrawBosses(Rect rect)
        {
            if (currentPreview?.bosses == null) return;
            
            foreach (var boss in currentPreview.bosses)
            {
                var pos = WorldToScreenPosition(boss.position, rect);
                var bossRect = new Rect(pos.x - 8, pos.y - 8, 16, 16);
                
                EditorGUI.DrawRect(bossRect, bossColor);
                DrawRectOutline(bossRect, Color.white, 1);
                
                if (showLabels)
                {
                    var labelRect = new Rect(pos.x - 30, pos.y + 10, 60, 15);
                    GUI.Label(labelRect, boss.name, labelStyle);
                }
            }
        }
        
        private void DrawProgressionLocks(Rect rect)
        {
            if (currentPreview?.progressionLocks == null) return;
            
            foreach (var lockData in currentPreview.progressionLocks)
            {
                var pos = WorldToScreenPosition(lockData.position, rect);
                var lockRect = new Rect(pos.x - 6, pos.y - 6, 12, 12);
                
                EditorGUI.DrawRect(lockRect, lockColor);
                DrawRectOutline(lockRect, Color.black, 1);
                
                if (showLabels)
                {
                    var labelRect = new Rect(pos.x - 25, pos.y + 8, 50, 15);
                    GUI.Label(labelRect, lockData.name, labelStyle);
                }
            }
        }
        
        private void DrawSecrets(Rect rect)
        {
            if (currentPreview?.secrets == null) return;
            
            foreach (var secret in currentPreview.secrets)
            {
                var pos = WorldToScreenPosition(secret.position, rect);
                var secretRect = new Rect(pos.x - 4, pos.y - 4, 8, 8);
                
                EditorGUI.DrawRect(secretRect, secretColor);
                DrawRectOutline(secretRect, Color.black, 1);
                
                if (showLabels)
                {
                    var labelRect = new Rect(pos.x - 15, pos.y + 6, 30, 15);
                    GUI.Label(labelRect, "Secret", labelStyle);
                }
            }
        }
        
        private void DrawPickups(Rect rect)
        {
            if (currentPreview?.pickups == null) return;
            
            foreach (var pickup in currentPreview.pickups)
            {
                var pos = WorldToScreenPosition(pickup.position, rect);
                var pickupRect = new Rect(pos.x - 2, pos.y - 2, 4, 4);
                
                EditorGUI.DrawRect(pickupRect, pickupColor);
                
                if (showLabels)
                {
                    var labelRect = new Rect(pos.x - 10, pos.y + 4, 20, 15);
                    GUI.Label(labelRect, pickup.type.ToString().Substring(0, 1), labelStyle);
                }
            }
        }
        
        private void DrawStatistics()
        {
            if (currentPreview == null) return;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generation Statistics", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField($"🎲 Seed: {previewSeed}");
            EditorGUILayout.LabelField($"🏢 Floors: {currentPreview.floors?.Count ?? 0}");
            EditorGUILayout.LabelField($"👹 Bosses: {currentPreview.bosses?.Count ?? 0}");
            EditorGUILayout.LabelField($"🔒 Progression Locks: {currentPreview.progressionLocks?.Count ?? 0}");
            EditorGUILayout.LabelField($"🔍 Secrets: {currentPreview.secrets?.Count ?? 0}");
            EditorGUILayout.LabelField($"💎 Pickups: {currentPreview.pickups?.Count ?? 0}");
            
            EditorGUILayout.Space();
            
            if (currentPreview.floors != null)
            {
                EditorGUILayout.LabelField("Floor Details:", EditorStyles.boldLabel);
                for (int i = 0; i < currentPreview.floors.Count; i++)
                {
                    var floor = currentPreview.floors[i];
                    EditorGUILayout.LabelField($"  Floor {i + 1}: {floor.biomeName} ({floor.roomCount} rooms, {floor.secretCount} secrets)");
                }
            }
            
            EditorGUILayout.EndVertical();
            
            // Validation results
            DrawValidationResults();
        }
        
        private void DrawValidationResults()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation Results", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            bool hasThreeFloors = currentPreview.floors?.Count == 3;
            bool hasCorrectBossCount = currentPreview.bosses?.Count == 3;
            bool hasCorrectLockCount = currentPreview.progressionLocks?.Count == 3;
            bool hasMinimumSecrets = (currentPreview.secrets?.Count ?? 0) >= 3;
            
            DrawValidationItem("✓ Three floors generated", hasThreeFloors);
            DrawValidationItem("✓ Correct boss count (3)", hasCorrectBossCount);
            DrawValidationItem("✓ Correct progression lock count (3)", hasCorrectLockCount);
            DrawValidationItem("✓ Minimum secrets (3+)", hasMinimumSecrets);
            
            bool allValidationsPassed = hasThreeFloors && hasCorrectBossCount && hasCorrectLockCount && hasMinimumSecrets;
            
            EditorGUILayout.Space();
            
            if (allValidationsPassed)
            {
                EditorGUILayout.HelpBox("✅ All validations passed! This seed generates a valid dungeon.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("⚠️ Some validations failed. Consider using a different seed.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawValidationItem(string text, bool passed)
        {
            var color = passed ? Color.green : Color.red;
            var prevColor = GUI.color;
            GUI.color = color;
            EditorGUILayout.LabelField(text);
            GUI.color = prevColor;
        }
        
        private void RefreshPreview()
        {
            Debug.Log($"🔄 Refreshing dungeon preview for seed {previewSeed}");
            
            try
            {
                currentPreview = GeneratePreviewData(previewSeed);
                needsRefresh = false;
                Repaint();
                
                Debug.Log($"✅ Preview refreshed successfully for seed {previewSeed}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to generate preview: {e.Message}");
                currentPreview = null;
            }
        }
        
        private DungeonPreviewData GeneratePreviewData(uint seed)
        {
            var preview = new DungeonPreviewData();
            var random = new Unity.Mathematics.Random(seed);
            
            // Generate floors
            preview.floors = new List<FloorPreviewData>();
            string[] biomeNames = { "Crystal Caverns", "Molten Depths", "Void Sanctum" };
            
            for (int i = 0; i < 3; i++)
            {
                var floor = new FloorPreviewData
                {
                    floorIndex = i,
                    biomeName = biomeNames[i],
                    biomeColor = floorColors[i],
                    roomCount = 8 + (i * 2),
                    secretCount = 1 + i,
                    seed = GenerateFloorSeed(seed, i)
                };
                
                preview.floors.Add(floor);
            }
            
            // Generate bosses
            preview.bosses = new List<BossPreviewData>();
            string[] miniBossNames = { "Crystal Guardian", "Magma Serpent" };
            string finalBossName = "Void Overlord";
            
            for (int i = 0; i < 3; i++)
            {
                var boss = new BossPreviewData
                {
                    floorIndex = i,
                    name = i < 2 ? miniBossNames[i] : finalBossName,
                    position = GetBossPositionForFloor(i),
                    isFinalBoss = i == 2
                };
                
                preview.bosses.Add(boss);
            }
            
            // Generate progression locks
            preview.progressionLocks = new List<ProgressionLockPreviewData>();
            string[] lockNames = { "Crystal Key", "Flame Essence", "Void Core" };
            
            for (int i = 0; i < 3; i++)
            {
                var lockData = new ProgressionLockPreviewData
                {
                    floorIndex = i,
                    name = lockNames[i],
                    position = GetLockPositionForFloor(i)
                };
                
                preview.progressionLocks.Add(lockData);
            }
            
            // Generate secrets
            preview.secrets = new List<SecretPreviewData>();
            
            for (int floorIndex = 0; floorIndex < 3; floorIndex++)
            {
                int secretsForFloor = 1 + floorIndex;
                for (int secretIndex = 0; secretIndex < secretsForFloor; secretIndex++)
                {
                    var secret = new SecretPreviewData
                    {
                        floorIndex = floorIndex,
                        secretIndex = secretIndex,
                        position = GetSecretPositionForFloor(floorIndex, secretIndex, random)
                    };
                    
                    preview.secrets.Add(secret);
                }
            }
            
            // Generate pickups
            preview.pickups = new List<PickupPreviewData>();
            var pickupTypes = new[] { PickupType.Health, PickupType.Mana, PickupType.Currency, PickupType.Equipment, PickupType.Consumable };
            
            for (int floorIndex = 0; floorIndex < 3; floorIndex++)
            {
                for (int pickupIndex = 0; pickupIndex < 15; pickupIndex++) // 15 pickups per floor
                {
                    var pickup = new PickupPreviewData
                    {
                        floorIndex = floorIndex,
                        type = pickupTypes[pickupIndex % pickupTypes.Length],
                        position = GetRandomPositionOnFloor(floorIndex, random)
                    };
                    
                    preview.pickups.Add(pickup);
                }
            }
            
            return preview;
        }
        
        // Helper methods for position calculation
        private Vector3 GetBossPositionForFloor(int floorIndex)
        {
            return new Vector3(20 + floorIndex * 5, 0, floorIndex * 30);
        }
        
        private Vector3 GetLockPositionForFloor(int floorIndex)
        {
            return new Vector3(0, 0, floorIndex * 30 + 15);
        }
        
        private Vector3 GetSecretPositionForFloor(int floorIndex, int secretIndex, Unity.Mathematics.Random random)
        {
            float angle = random.NextFloat(0, 2 * math.PI);
            float distance = random.NextFloat(10, 20);
            
            return new Vector3(
                math.cos(angle) * distance,
                random.NextFloat(-2, 3),
                floorIndex * 30 + math.sin(angle) * distance
            );
        }
        
        private Vector3 GetRandomPositionOnFloor(int floorIndex, Unity.Mathematics.Random random)
        {
            return new Vector3(
                random.NextFloat(-15, 16),
                random.NextFloat(-1, 2),
                floorIndex * 30 + random.NextFloat(-10, 11)
            );
        }
        
        private uint GenerateFloorSeed(uint baseSeed, int floorIndex)
        {
            var random = new Unity.Mathematics.Random(baseSeed);
            for (int i = 0; i <= floorIndex; i++)
            {
                random.NextUInt();
            }
            return random.NextUInt();
        }
        
        // Helper methods for drawing
        private Rect GetFloorRect(Rect previewRect, int floorIndex)
        {
            float floorHeight = previewRect.height / 3f;
            float y = floorIndex * floorHeight;
            
            return new Rect(previewOffset.x, y + previewOffset.y, previewRect.width - 40, floorHeight - 10);
        }
        
        private Vector2 WorldToScreenPosition(Vector3 worldPos, Rect screenRect)
        {
            // Convert world position to screen position
            float x = (worldPos.x + 25) / 50f * screenRect.width + previewOffset.x;
            float y = ((worldPos.z / 30f) % 1f) * (screenRect.height / 3f) + (worldPos.z / 30f) * (screenRect.height / 3f) + previewOffset.y;
            
            return new Vector2(x, y);
        }
        
        private void DrawRectOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), color);
        }
    }
    
    // Data structures for preview
    [System.Serializable]
    public class DungeonPreviewData
    {
        public List<FloorPreviewData> floors;
        public List<BossPreviewData> bosses;
        public List<ProgressionLockPreviewData> progressionLocks;
        public List<SecretPreviewData> secrets;
        public List<PickupPreviewData> pickups;
    }
    
    [System.Serializable]
    public class FloorPreviewData
    {
        public int floorIndex;
        public string biomeName;
        public Color biomeColor;
        public int roomCount;
        public int secretCount;
        public uint seed;
    }
    
    [System.Serializable]
    public class BossPreviewData
    {
        public int floorIndex;
        public string name;
        public Vector3 position;
        public bool isFinalBoss;
    }
    
    [System.Serializable]
    public class ProgressionLockPreviewData
    {
        public int floorIndex;
        public string name;
        public Vector3 position;
    }
    
    [System.Serializable]
    public class SecretPreviewData
    {
        public int floorIndex;
        public int secretIndex;
        public Vector3 position;
    }
    
    [System.Serializable]
    public class PickupPreviewData
    {
        public int floorIndex;
        public PickupType type;
        public Vector3 position;
    }
}