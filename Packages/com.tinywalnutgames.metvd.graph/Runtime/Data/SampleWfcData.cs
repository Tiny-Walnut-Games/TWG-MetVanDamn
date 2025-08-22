using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph.Data
{
    /// <summary>
    /// Sample WFC data provider for testing and demo purposes
    /// Provides basic tile prototypes and socket definitions
    /// </summary>
    [BurstCompile]
    public static class SampleWfcData
    {
        /// <summary>
        /// Create basic tile prototypes for WFC testing
        /// </summary>
        public static NativeArray<WfcTilePrototype> CreateBasicTilePrototypes(Allocator allocator)
        {
            var prototypes = new NativeArray<WfcTilePrototype>(4, allocator);
            
            // Hub Tile - central connection point
            prototypes[0] = new WfcTilePrototype(
                tileId: 1,
                weight: 1.0f,
                biomeType: BiomeType.HubArea,
                primaryPolarity: Polarity.None,
                minConnections: 2,
                maxConnections: 4
            );
            
            // Corridor Tile - linear connection
            prototypes[1] = new WfcTilePrototype(
                tileId: 2,
                weight: 0.8f,
                biomeType: BiomeType.TransitionZone,
                primaryPolarity: Polarity.None,
                minConnections: 2,
                maxConnections: 2
            );
            
            // Chamber Tile - room with multiple exits
            prototypes[2] = new WfcTilePrototype(
                tileId: 3,
                weight: 0.6f,
                biomeType: BiomeType.SolarPlains,
                primaryPolarity: Polarity.Sun,
                minConnections: 1,
                maxConnections: 3
            );
            
            // Specialist Tile - unique functionality
            prototypes[3] = new WfcTilePrototype(
                tileId: 4,
                weight: 0.4f,
                biomeType: BiomeType.VolcanicCore,
                primaryPolarity: Polarity.Heat,
                minConnections: 1,
                maxConnections: 2
            );
            
            return prototypes;
        }
        
        /// <summary>
        /// Create basic socket definitions for each tile type
        /// </summary>
        public static NativeArray<WfcSocket> CreateHubTileSockets(Allocator allocator)
        {
            var sockets = new NativeArray<WfcSocket>(4, allocator);
            
            // Hub tile has 4 basic sockets, one in each direction
            sockets[0] = new WfcSocket(1, 0, Polarity.None, true);  // North
            sockets[1] = new WfcSocket(1, 1, Polarity.None, true);  // East  
            sockets[2] = new WfcSocket(1, 2, Polarity.None, true);  // South
            sockets[3] = new WfcSocket(1, 3, Polarity.None, true);  // West
            
            return sockets;
        }
        
        public static NativeArray<WfcSocket> CreateCorridorTileSockets(Allocator allocator)
        {
            var sockets = new NativeArray<WfcSocket>(2, allocator);
            
            // Corridor tile has 2 sockets on opposite sides
            sockets[0] = new WfcSocket(1, 0, Polarity.None, true);  // North
            sockets[1] = new WfcSocket(1, 2, Polarity.None, true);  // South
            
            return sockets;
        }
        
        public static NativeArray<WfcSocket> CreateChamberTileSockets(Allocator allocator)
        {
            var sockets = new NativeArray<WfcSocket>(3, allocator);
            
            // Chamber tile has 3 environmental sockets
            sockets[0] = new WfcSocket(2, 0, Polarity.Sun, true);   // North - polarity restricted
            sockets[1] = new WfcSocket(1, 1, Polarity.None, true);  // East - basic
            sockets[2] = new WfcSocket(1, 2, Polarity.None, true);  // South - basic
            
            return sockets;
        }
        
        public static NativeArray<WfcSocket> CreateSpecialistTileSockets(Allocator allocator)
        {
            var sockets = new NativeArray<WfcSocket>(2, allocator);
            
            // Specialist tile has restricted sockets
            sockets[0] = new WfcSocket(3, 1, Polarity.Heat, true);     // East - dual polarity required
            sockets[1] = new WfcSocket(2, 3, Polarity.Heat, true);     // West - heat required
            
            return sockets;
        }
        
        /// <summary>
        /// Helper method to get socket definitions for a specific tile ID
        /// </summary>
        public static NativeArray<WfcSocket> GetSocketsForTile(uint tileId, Allocator allocator)
        {
            return tileId switch
            {
                1 => CreateHubTileSockets(allocator),
                2 => CreateCorridorTileSockets(allocator),
                3 => CreateChamberTileSockets(allocator),
                4 => CreateSpecialistTileSockets(allocator),
                _ => new NativeArray<WfcSocket>(0, allocator) // Empty for unknown tiles
            };
        }
        
        /// <summary>
        /// Create a complete tile set with all prototypes and their sockets
        /// Returns number of tiles created for validation
        /// </summary>
        public static int InitializeSampleTileSet(EntityManager entityManager)
        {
            var prototypes = CreateBasicTilePrototypes(Allocator.Temp);
            int tilesCreated = 0;
            
            foreach (var prototype in prototypes)
            {
                var tileEntity = entityManager.CreateEntity();
                entityManager.AddComponentData(tileEntity, prototype);
                
                // Add socket buffer
                var socketBuffer = entityManager.AddBuffer<WfcSocketBufferElement>(tileEntity);
                var sockets = GetSocketsForTile(prototype.TileId, Allocator.Temp);
                
                foreach (var socket in sockets)
                {
                    socketBuffer.Add(socket);
                }
                
                sockets.Dispose();
                tilesCreated++;
            }
            
            prototypes.Dispose();
            return tilesCreated;
        }
    }
}
