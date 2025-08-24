#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVD.Shared;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    /// <summary>
    /// Editor utility for previewing procedural layout in edit mode
    /// </summary>
    public class ProceduralLayoutPreview : EditorWindow
    {
        [MenuItem("MetVanDAMN/World Layout Preview")]
        public static void ShowWindow()
        {
            GetWindow<ProceduralLayoutPreview>("Layout Preview");
        }

        private WorldConfigurationAuthoring _worldConfig;
        private DistrictAuthoring[] _districts;
        private int _previewDistrictCount = 5;
        private int2 _previewWorldSize = new(32, 32);
        private RandomizationMode _previewMode = RandomizationMode.Partial;
        private uint _previewSeed = 12345;

        private void OnGUI()
        {
            GUILayout.Label("Procedural Layout Preview", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            // Configuration section
            GUILayout.Label("Preview Configuration", EditorStyles.boldLabel);
            _previewDistrictCount = EditorGUILayout.IntField("District Count", _previewDistrictCount);
            _previewWorldSize.x = EditorGUILayout.IntField("World Width", _previewWorldSize.x);
            _previewWorldSize.y = EditorGUILayout.IntField("World Height", _previewWorldSize.y);
            _previewMode = (RandomizationMode)EditorGUILayout.EnumPopup("Randomization Mode", _previewMode);
            _previewSeed = (uint)EditorGUILayout.IntField("Seed", (int)_previewSeed);
            
            EditorGUILayout.Space();
            
            // Scene analysis section
            GUILayout.Label("Current Scene", EditorStyles.boldLabel);
            _worldConfig = FindFirstObjectByType<WorldConfigurationAuthoring>();
            _districts = FindObjectsByType<DistrictAuthoring>(FindObjectsSortMode.None);
            
            if (_worldConfig != null)
            {
                EditorGUILayout.LabelField("World Config Found", _worldConfig.name);
                EditorGUILayout.LabelField("Current Seed", _worldConfig.seed.ToString());
                EditorGUILayout.LabelField("Current World Size", _worldConfig.worldSize.ToString());
                EditorGUILayout.LabelField("Randomization Mode", _worldConfig.randomizationMode.ToString());
            }
            else
            {
                EditorGUILayout.HelpBox("No WorldConfigurationAuthoring found in scene", MessageType.Warning);
            }
            
            EditorGUILayout.LabelField("Districts in Scene", _districts.Length.ToString());
            
            EditorGUILayout.Space();
            
            // Actions section
            GUILayout.Label("Actions", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Preview Layout Algorithm"))
            {
                PreviewLayoutAlgorithm();
            }
            
            if (GUILayout.Button("Generate Preview Districts"))
            {
                GeneratePreviewDistricts();
            }
            
            if (_worldConfig != null && GUILayout.Button("Apply Preview Settings to Scene"))
            {
                ApplyPreviewSettingsToScene();
            }
            
            if (_districts.Length > 0 && GUILayout.Button("Show District Info"))
            {
                ShowDistrictInfo();
            }
            
            EditorGUILayout.Space();
            
            // Information section
            EditorGUILayout.HelpBox(
                "This tool allows you to preview the procedural layout algorithms without entering Play mode. " +
                "Use 'Preview Layout Algorithm' to see how districts would be placed with current settings.",
                MessageType.Info);
        }

        private void PreviewLayoutAlgorithm()
        {
            Debug.Log($"🔮 Previewing Layout Algorithm");
            Debug.Log($"   Districts: {_previewDistrictCount}");
            Debug.Log($"   World Size: {_previewWorldSize}");
            Debug.Log($"   Randomization: {_previewMode}");
            Debug.Log($"   Seed: {_previewSeed}");
            
            // Simulate the district placement algorithm
            var random = new Unity.Mathematics.Random(_previewSeed);
            var strategy = _previewDistrictCount > 16 ? 
                DistrictPlacementStrategy.JitteredGrid : 
                DistrictPlacementStrategy.PoissonDisc;
            
            Debug.Log($"   Strategy: {strategy}");
            
            // Generate positions using the same algorithm as DistrictLayoutSystem
            var positions = new int2[_previewDistrictCount];
            
            if (strategy == DistrictPlacementStrategy.PoissonDisc)
            {
                GeneratePreviewPoissonDisc(positions, _previewWorldSize, ref random);
            }
            else
            {
                GeneratePreviewJitteredGrid(positions, _previewWorldSize, ref random);
            }
            
            Debug.Log("   Calculated Positions:");
            for (int i = 0; i < positions.Length; i++)
            {
                Debug.Log($"     District {i + 1}: ({positions[i].x}, {positions[i].y})");
            }
            
            // Simulate rule randomization
            PreviewRuleRandomization(ref random);
        }

        private void GeneratePreviewPoissonDisc(int2[] positions, int2 worldSize, ref Unity.Mathematics.Random random)
        {
            float minDistance = math.min(worldSize.x, worldSize.y) * 0.2f;
            int maxAttempts = 30;

            for (int i = 0; i < positions.Length; i++)
            {
                bool validPosition = false;
                int attempts = 0;
                
                while (!validPosition && attempts < maxAttempts)
                {
                    int2 candidate = new int2(
                        random.NextInt(0, worldSize.x),
                        random.NextInt(0, worldSize.y)
                    );

                    validPosition = true;
                    for (int j = 0; j < i; j++)
                    {
                        float distance = math.length(new float2(candidate - positions[j]));
                        if (distance < minDistance)
                        {
                            validPosition = false;
                            break;
                        }
                    }

                    if (validPosition)
                    {
                        positions[i] = candidate;
                    }

                    attempts++;
                }

                if (!validPosition)
                {
                    positions[i] = new int2(
                        random.NextInt(0, worldSize.x),
                        random.NextInt(0, worldSize.y)
                    );
                }
            }
        }

        private void GeneratePreviewJitteredGrid(int2[] positions, int2 worldSize, ref Unity.Mathematics.Random random)
        {
            int gridDim = (int)math.ceil(math.sqrt(positions.Length));
            float2 cellSize = new float2(worldSize) / gridDim;
            float jitterAmount = math.min(cellSize.x, cellSize.y) * 0.3f;

            for (int i = 0; i < positions.Length; i++)
            {
                int gridX = i % gridDim;
                int gridY = i / gridDim;

                float2 cellCenter = new float2(gridX + 0.5f, gridY + 0.5f) * cellSize;
                float2 jitter = new float2(
                    random.NextFloat(-jitterAmount, jitterAmount),
                    random.NextFloat(-jitterAmount, jitterAmount)
                );

                float2 finalPosition = cellCenter + jitter;
                positions[i] = new int2(
                    math.clamp((int)finalPosition.x, 0, worldSize.x - 1),
                    math.clamp((int)finalPosition.y, 0, worldSize.y - 1)
                );
            }
        }

        private void PreviewRuleRandomization(ref Unity.Mathematics.Random random)
        {
            Debug.Log($"🎲 Rule Randomization Preview (Mode: {_previewMode})");
            
            switch (_previewMode)
            {
                case RandomizationMode.None:
                    Debug.Log("   Biome Polarities: Sun|Moon|Heat|Cold (curated)");
                    Debug.Log("   Upgrades: Jump|DoubleJump|Dash|WallJump (curated)");
                    break;
                
                case RandomizationMode.Partial:
                    var polarities = GenerateRandomPolarities(ref random, false);
                    Debug.Log($"   Biome Polarities: {polarities} (randomized)");
                    Debug.Log("   Upgrades: Jump|DoubleJump|Dash|WallJump (curated)");
                    break;
                
                case RandomizationMode.Full:
                    var fullPolarities = GenerateRandomPolarities(ref random, true);
                    var upgrades = GenerateRandomUpgrades(ref random);
                    Debug.Log($"   Biome Polarities: {fullPolarities} (randomized)");
                    Debug.Log($"   Upgrades: 0x{upgrades:X} (randomized, always includes Jump)");
                    break;
            }
        }

        private Polarity GenerateRandomPolarities(ref Unity.Mathematics.Random random, bool allowMore)
        {
            var availablePolarities = new[]
            {
                Polarity.Sun, Polarity.Moon, Polarity.Heat, Polarity.Cold,
                Polarity.Earth, Polarity.Wind, Polarity.Life, Polarity.Tech
            };

            var result = Polarity.None;
            int count = allowMore ? random.NextInt(2, 6) : random.NextInt(2, 4);
            
            for (int i = 0; i < count; i++)
            {
                int index = random.NextInt(0, availablePolarities.Length);
                result |= availablePolarities[index];
            }
            
            return result;
        }

        private uint GenerateRandomUpgrades(ref Unity.Mathematics.Random random)
        {
            uint upgrades = 1u; // Always include Jump (bit 0)
            
            for (int i = 1; i < 8; i++)
            {
                if (random.NextFloat() > 0.4f)
                {
                    upgrades |= 1u << i;
                }
            }
            
            return upgrades;
        }

        private void GeneratePreviewDistricts()
        {
            // This would create actual district authoring objects in the scene for preview
            Debug.Log("🏗️ Generate Preview Districts (would create scene objects)");
            // Implementation would create DistrictAuthoring GameObjects at calculated positions
        }

        private void ApplyPreviewSettingsToScene()
        {
            if (_worldConfig != null)
            {
                Undo.RecordObject(_worldConfig, "Apply Preview Settings");
                _worldConfig.seed = (int)_previewSeed;
                _worldConfig.worldSize = _previewWorldSize;
                _worldConfig.randomizationMode = _previewMode;
                EditorUtility.SetDirty(_worldConfig);
                Debug.Log("✅ Applied preview settings to WorldConfigurationAuthoring");
            }
        }

        private void ShowDistrictInfo()
        {
            Debug.Log($"📋 District Information ({_districts.Length} districts):");
            
            int unplacedCount = 0;
            int placedCount = 0;
            
            foreach (var district in _districts)
            {
                bool isUnplaced = district.gridCoordinates.x == 0 && district.gridCoordinates.y == 0;
                if (isUnplaced) unplacedCount++;
                else placedCount++;
                
                Debug.Log($"   District {district.nodeId}: " +
                         $"Pos({district.gridCoordinates.x}, {district.gridCoordinates.y}), " +
                         $"Level {district.level}, " +
                         $"Status: {(isUnplaced ? "UNPLACED" : "PLACED")}");
            }
            
            Debug.Log($"   Summary: {placedCount} placed, {unplacedCount} unplaced");
        }
    }
}
#endif
