using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Core
{
    /// <summary>
    /// Uniquely identifies graph node at any scale (district, sector, room)
    /// Fundamental identification for the MetVanDAMN world graph.
    /// </summary>
    public struct NodeId : IComponentData
    {
        /// <summary>Unique identifier for this node (preferred member).</summary>
        public uint Value;

        /// <summary>Hierarchical level (0=district, 1=sector, 2=room).</summary>
        public byte Level;

        /// <summary>Parent node ID for hierarchical relationships.</summary>
        public uint ParentId;

        /// <summary>Spatial coordinates for the node in the graph.</summary>
        public int2 Coordinates;

        /// <summary>
        /// Backward-compatible alias used by older code/tests that referenced nodeId.value (lowercase).
        /// NOTE: Do not remove without updating all usages. Kept clean (no Obsolete attribute) to avoid warning noise in tests.
        /// </summary>
        public uint value
        {
            readonly get => Value;
            set => Value = value;
        }

        /// <summary>
        /// Legacy transitional alias (kept for any intermediate refactors that used ValueParam).
        /// </summary>
        public uint ValueParam
        {
            readonly get => Value;
            set => Value = value;
        }

        public NodeId(uint value, byte level = 0, uint parentId = 0, int2 coordinates = default)
        {
            Value = value;
            Level = level;
            ParentId = parentId;
            Coordinates = coordinates;
        }

        public override readonly string ToString()
        {
            return $"NodeId({Value}, L{Level}, Parent:{ParentId}, Pos:{Coordinates})";
        }

        public static implicit operator uint(NodeId nodeId) => nodeId.Value;
        public static implicit operator NodeId(uint value) => new(value);
    }
}
