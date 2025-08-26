using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Samples
{
    /// <summary>
    /// Cross-Scale Integration Example demonstrating universal abstractions
    /// Shows how the same components work from dungeon rooms to star systems
    /// </summary>
    public static class CrossScaleIntegrationExample
    {
        /// <summary>
        /// Example: Character navigating a dungeon room (Level 4)
        /// </summary>
        public static void CreateDungeonRoom(EntityManager entityManager)
        {
            var roomEntity = entityManager.CreateEntity();
            
            // Room identification
            entityManager.AddComponentData(roomEntity, new NodeId(
                value: 40001,
                level: 4,        // Room level
                parentId: 3001,  // Parent sector
                coordinates: new int2(10, 5)
            ));
            
            // Room biome
            entityManager.AddComponentData(roomEntity, new TinyWalnutGames.MetVD.Core.Biome(
                type: BiomeType.VolcanicCore,
                primaryPolarity: Polarity.Heat,
                polarityStrength: 0.8f,
                difficultyModifier: 1.2f
            ));
            
            // Gate condition for room exit (requires heat resistance)
            entityManager.AddComponentData(roomEntity, new GateCondition(
                requiredPolarity: Polarity.Heat,
                requiredAbilities: Ability.Jump | Ability.HeatResistance
            ));
        }
        
        /// <summary>
        /// Example: Solar system navigation (Level 3) - same abstractions!
        /// </summary>
        public static void CreateSolarSystem(EntityManager entityManager)
        {
            var systemEntity = entityManager.CreateEntity();
            
            // System identification (same NodeId structure)
            entityManager.AddComponentData(systemEntity, new NodeId(
                value: 30001,
                level: 3,        // Solar system level
                parentId: 2001,  // Parent star cluster
                coordinates: new int2(42, 18)
            ));
            
            // System biome (plasma storms - same polarity system!)
            entityManager.AddComponentData(systemEntity, new TinyWalnutGames.MetVD.Core.Biome(
                type: BiomeType.PlasmaFields,  // AstroECS equivalent of VolcanicCore
                primaryPolarity: Polarity.Heat,
                polarityStrength: 0.9f,
                difficultyModifier: 2.1f
            ));
            
            // Gate condition for wormhole access (requires heat shielding)
            entityManager.AddComponentData(systemEntity, new GateCondition(
                requiredPolarity: Polarity.Heat,
                requiredAbilities: Ability.WarpDrive | Ability.HeatShielding
            ));
        }
        
        /// <summary>
        /// Example: Universal pathfinding works at any scale
        /// </summary>
        public static bool CanTraverse(NodeId from, NodeId to, Connection connection, 
                                     Polarity availablePolarity, Ability availableAbilities)
        {
            // Same logic works for character movement OR spacecraft navigation
            bool hasPolarity = (availablePolarity & connection.RequiredPolarity) != 0;
            
            // Gate conditions would be checked the same way regardless of scale
            // This is the power of universal abstractions!
            
            return connection.IsActive && hasPolarity;
        }
        
        /// <summary>
        /// Example: Scale-aware physics calculations
        /// </summary>
        public static float CalculateTraversalTime(Connection connection, byte nodeLevel)
        {
            // Base traversal cost from connection
            float baseCost = connection.TraversalCost;
            
            // Scale factors for different levels
            float scaleFactor = nodeLevel switch
            {
                4 => 1.0f,      // Room level: seconds
                3 => 60.0f,     // System level: minutes  
                2 => 3600.0f,   // Cluster level: hours
                1 => 86400.0f,  // Quadrant level: days
                0 => 31536000.0f, // Galaxy level: years
                _ => 1.0f
            };
            
            return baseCost * scaleFactor;
        }
    }
}