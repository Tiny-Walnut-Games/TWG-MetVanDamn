using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Core
{
    /// <summary>
    /// Connection traversal types for Metroidvania navigation
    /// </summary>
    public enum ConnectionType : byte
    {
        Bidirectional = 0,  // Can traverse in both directions
        OneWay = 1,         // Can only traverse in one direction
        Drop = 2,           // One-way drop (physics-based)
        Vent = 3,           // Air vent passage
        CrumbleFloor = 4,   // Floor that breaks after passing
        Teleporter = 5,     // Instant transport connection
        ConditionalGate = 6 // Requires specific conditions/abilities
    }

    /// <summary>
    /// Defines link between nodes with traversal rules and polarity requirements
    /// Core component for the Metroidvania interconnected world structure
    /// </summary>
    public struct Connection : IComponentData
    {
        /// <summary>
        /// Source node of this connection
        /// </summary>
        public uint FromNodeId;
        
        /// <summary>
        /// Destination node of this connection
        /// </summary>
        public uint ToNodeId;
        
        /// <summary>
        /// Type of connection determining traversal rules
        /// </summary>
        public ConnectionType Type;
        
        /// <summary>
        /// Required polarity to traverse this connection
        /// </summary>
        public Polarity RequiredPolarity;
        
        /// <summary>
        /// Traversal cost for pathfinding algorithms
        /// </summary>
        public float TraversalCost;
        
        /// <summary>
        /// Whether this connection is currently active/passable
        /// </summary>
        public bool IsActive;
        
        /// <summary>
        /// Whether this connection has been discovered by the player
        /// </summary>
        public bool IsDiscovered;

        public Connection(uint fromNodeId, uint toNodeId, ConnectionType type = ConnectionType.Bidirectional,
                         Polarity requiredPolarity = Polarity.None, float traversalCost = 1.0f)
        {
            FromNodeId = fromNodeId;
            ToNodeId = toNodeId;
            Type = type;
            RequiredPolarity = requiredPolarity;
            TraversalCost = math.max(0.1f, traversalCost);
            IsActive = true;
            IsDiscovered = false;
        }

        /// <summary>
        /// Check if this connection can be traversed from the given node
        /// </summary>
        public readonly bool CanTraverseFrom(uint nodeId, Polarity availablePolarity)
        {
            if (!IsActive)
            {
                return false;
            }

            // Check if starting from the correct node
            bool validDirection = (Type == ConnectionType.Bidirectional) 
                ? (nodeId == FromNodeId || nodeId == ToNodeId)
                : (nodeId == FromNodeId);
                
            if (!validDirection)
            {
                return false;
            }

            // Check polarity requirements
            if (RequiredPolarity == Polarity.None || RequiredPolarity == Polarity.Any)
            {
                return true;
            }

            return (availablePolarity & RequiredPolarity) != 0;
        }

        /// <summary>
        /// Get the destination node when traversing from the given source
        /// </summary>
        public readonly uint GetDestination(uint sourceNodeId)
        {
            if (Type == ConnectionType.Bidirectional)
            {
                return sourceNodeId == FromNodeId ? ToNodeId : FromNodeId;
            }
            
            return sourceNodeId == FromNodeId ? ToNodeId : 0; // 0 indicates invalid traversal
        }

        public override readonly string ToString()
        {
            string direction = Type == ConnectionType.Bidirectional ? "<->" : "->";
            return $"Connection({FromNodeId} {direction} {ToNodeId}, {Type}, {RequiredPolarity})";
        }
    }

    /// <summary>
    /// Buffer element for storing multiple connections from a single node
    /// Enables efficient graph traversal and pathfinding
    /// </summary>
    public struct ConnectionBufferElement : IBufferElementData
    {
        public Connection Value;
        
        public static implicit operator Connection(ConnectionBufferElement e) => e.Value;
        public static implicit operator ConnectionBufferElement(Connection e) => new(){ Value = e };
    }
}
