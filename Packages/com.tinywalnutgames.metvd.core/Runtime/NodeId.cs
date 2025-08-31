using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Core
	{
	/// <summary>
	/// Uniquely identifies graph node at any scale (district, sector, room)
	/// This is the fundamental identification system for the MetVanDAMN world graph
	/// </summary>
	public struct NodeId : IComponentData
		{
		/// <summary>
		/// Unique identifier for this node
		/// </summary>
		public uint _value;

		/// <summary>
		/// Hierarchical level (0=district, 1=sector, 2=room)
		/// </summary>
		public byte Level;

		/// <summary>
		/// Parent node ID for hierarchical relationships
		/// </summary>
		public uint ParentId;

		/// <summary>
		/// Spatial coordinates for the node in the graph
		/// </summary>
		public int2 Coordinates;

		// Backward-compatible alias used by older tests (nodeId.value)
		public uint Value { readonly get => this._value; set => this._value = value; }

		public NodeId (uint value, byte level = 0, uint parentId = 0, int2 coordinates = default)
			{
			this._value = value;
			this.Level = level;
			this.ParentId = parentId;
			this.Coordinates = coordinates;
			}

		public override readonly string ToString ()
			{
			return $"NodeId({this._value}, L{this.Level}, Parent:{this.ParentId}, Pos:{this.Coordinates})";
			}

		public static implicit operator uint (NodeId nodeId) => nodeId._value;
		public static implicit operator NodeId (uint value) => new(value);
		}
	}
