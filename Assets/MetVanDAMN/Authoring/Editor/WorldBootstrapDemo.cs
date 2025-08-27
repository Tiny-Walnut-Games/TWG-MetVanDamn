using UnityEngine;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVD.Shared;

namespace TinyWalnutGames.MetVD.Authoring
{
    /// <summary>
    /// Demonstration script showing WorldBootstrap configuration examples
    /// Add this to a GameObject to see different bootstrap configurations
    /// </summary>
    public class WorldBootstrapDemo : MonoBehaviour
    {
        [Header("Demo Configurations")]
        private readonly DemoType demoType = DemoType.SmallWorld;
        
        [Header("Generated Configuration")]
        [SerializeField] private WorldBootstrapAuthoring targetBootstrap;

        public enum DemoType
        {
            SmallWorld,      // Quick testing: 3-5 districts, 2-4 sectors each
            MediumWorld,     // Balanced play: 6-10 districts, 3-6 sectors each  
            LargeWorld,      // Epic adventure: 10-15 districts, 4-8 sectors each
            DenseWorld,      // High detail: 5-8 districts, 6-12 sectors each
            Custom          // Use inspector values
        }

#if UNITY_EDITOR
        [ContextMenu("Apply Demo Configuration")]
        public void ApplyDemoConfiguration()
        {
            if (targetBootstrap == null)
            {
                targetBootstrap = FindFirstObjectByType<WorldBootstrapAuthoring>();
                if (targetBootstrap == null)
                {
                    Debug.LogError("No WorldBootstrapAuthoring found in scene. Add one first.");
                    return;
                }
            }

            ApplyConfiguration();
            UnityEditor.EditorUtility.SetDirty(targetBootstrap);
            Debug.Log($"✅ Applied {demoType} configuration to WorldBootstrapAuthoring");
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            
            if (targetBootstrap != null && demoType != DemoType.Custom)
            {
                ApplyConfiguration();
            }
        }
#endif

        private void ApplyConfiguration()
        {
            if (targetBootstrap == null) return;

            switch (demoType)
            {
                case DemoType.SmallWorld:
                    ConfigureSmallWorld();
                    break;
                case DemoType.MediumWorld:
                    ConfigureMediumWorld();
                    break;
                case DemoType.LargeWorld:
                    ConfigureLargeWorld();
                    break;
                case DemoType.DenseWorld:
                    ConfigureDenseWorld();
                    break;
                case DemoType.Custom:
                    // Don't modify - use inspector values
                    break;
            }
        }

        private void ConfigureSmallWorld()
        {
            // Quick testing configuration
            targetBootstrap.seed = 42;
            targetBootstrap.worldSize = new int2(40, 40);
            targetBootstrap.randomizationMode = RandomizationMode.Partial;
            
            targetBootstrap.biomeCount = new Vector2Int(2, 4);
            targetBootstrap.biomeWeight = 1.0f;
            
            targetBootstrap.districtCount = new Vector2Int(3, 5);
            targetBootstrap.districtMinDistance = 10f;
            targetBootstrap.districtWeight = 1.0f;
            
            targetBootstrap.sectorsPerDistrict = new Vector2Int(2, 4);
            targetBootstrap.sectorGridSize = new int2(4, 4);
            
            targetBootstrap.roomsPerSector = new Vector2Int(3, 8);
            targetBootstrap.targetLoopDensity = 0.3f;
            
            targetBootstrap.enableDebugVisualization = true;
            targetBootstrap.logGenerationSteps = true;
        }

        private void ConfigureMediumWorld()
        {
            // Balanced gameplay configuration
            targetBootstrap.seed = 0; // Random
            targetBootstrap.worldSize = new int2(64, 64);
            targetBootstrap.randomizationMode = RandomizationMode.Partial;
            
            targetBootstrap.biomeCount = new Vector2Int(3, 6);
            targetBootstrap.biomeWeight = 1.2f;
            
            targetBootstrap.districtCount = new Vector2Int(6, 10);
            targetBootstrap.districtMinDistance = 15f;
            targetBootstrap.districtWeight = 1.0f;
            
            targetBootstrap.sectorsPerDistrict = new Vector2Int(3, 6);
            targetBootstrap.sectorGridSize = new int2(6, 6);
            
            targetBootstrap.roomsPerSector = new Vector2Int(4, 10);
            targetBootstrap.targetLoopDensity = 0.4f;
            
            targetBootstrap.enableDebugVisualization = true;
            targetBootstrap.logGenerationSteps = false;
        }

        private void ConfigureLargeWorld()
        {
            // Epic adventure configuration
            targetBootstrap.seed = 0; // Random
            targetBootstrap.worldSize = new int2(100, 100);
            targetBootstrap.randomizationMode = RandomizationMode.Full;
            
            targetBootstrap.biomeCount = new Vector2Int(4, 8);
            targetBootstrap.biomeWeight = 1.5f;
            
            targetBootstrap.districtCount = new Vector2Int(10, 15);
            targetBootstrap.districtMinDistance = 20f;
            targetBootstrap.districtWeight = 0.8f;
            
            targetBootstrap.sectorsPerDistrict = new Vector2Int(4, 8);
            targetBootstrap.sectorGridSize = new int2(8, 8);
            
            targetBootstrap.roomsPerSector = new Vector2Int(5, 12);
            targetBootstrap.targetLoopDensity = 0.5f;
            
            targetBootstrap.enableDebugVisualization = false; // Too large for gizmos
            targetBootstrap.logGenerationSteps = true;
        }

        private void ConfigureDenseWorld()
        {
            // High detail configuration
            targetBootstrap.seed = 12345;
            targetBootstrap.worldSize = new int2(80, 80);
            targetBootstrap.randomizationMode = RandomizationMode.Partial;
            
            targetBootstrap.biomeCount = new Vector2Int(3, 5);
            targetBootstrap.biomeWeight = 1.0f;
            
            targetBootstrap.districtCount = new Vector2Int(5, 8);
            targetBootstrap.districtMinDistance = 18f;
            targetBootstrap.districtWeight = 1.2f;
            
            targetBootstrap.sectorsPerDistrict = new Vector2Int(6, 12);
            targetBootstrap.sectorGridSize = new int2(10, 10);
            
            targetBootstrap.roomsPerSector = new Vector2Int(8, 15);
            targetBootstrap.targetLoopDensity = 0.6f;
            
            targetBootstrap.enableDebugVisualization = true;
            targetBootstrap.logGenerationSteps = true;
        }

#if UNITY_EDITOR
        [ContextMenu("Calculate Estimated Totals")]
        public void CalculateEstimatedTotals()
        {
            if (targetBootstrap == null)
            {
                targetBootstrap = FindFirstObjectByType<WorldBootstrapAuthoring>();
                if (targetBootstrap == null)
                {
                    Debug.LogError("No WorldBootstrapAuthoring found in scene.");
                    return;
                }
            }

            // Calculate estimated totals (using averages)
            float avgBiomes = (targetBootstrap.biomeCount.x + targetBootstrap.biomeCount.y) * 0.5f;
            float avgDistricts = (targetBootstrap.districtCount.x + targetBootstrap.districtCount.y) * 0.5f;
            float avgSectorsPerDistrict = (targetBootstrap.sectorsPerDistrict.x + targetBootstrap.sectorsPerDistrict.y) * 0.5f;
            float avgRoomsPerSector = (targetBootstrap.roomsPerSector.x + targetBootstrap.roomsPerSector.y) * 0.5f;

            float totalSectors = avgDistricts * avgSectorsPerDistrict;
            float totalRooms = totalSectors * avgRoomsPerSector;

            Debug.Log($"📊 Estimated Totals for {demoType}:\n" +
                     $"🌿 Biomes: ~{avgBiomes:F1}\n" +
                     $"🏰 Districts: ~{avgDistricts:F1}\n" +
                     $"🏘️ Sectors: ~{totalSectors:F1}\n" +
                     $"🏠 Rooms: ~{totalRooms:F1}\n" +
                     $"🌍 World Size: {targetBootstrap.worldSize.x}x{targetBootstrap.worldSize.y}");
        }
#endif
    }
}
