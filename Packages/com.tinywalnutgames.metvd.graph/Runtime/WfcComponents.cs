using TinyWalnutGames.MetVD.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Graph
	{
	/// <summary>
	/// Socket definition for WFC tile constraints
	/// Defines how tiles can connect to each other
	/// </summary>
	public struct WfcSocket : IComponentData
		{
		/// <summary>
		/// Socket ID for matching compatible tiles
		/// </summary>
		public uint SocketId;

		/// <summary>
		/// Direction this socket faces (0=North, 1=East, 2=South, 3=West)
		/// </summary>
		public byte Direction;

		/// <summary>
		/// Required polarity for this socket connection
		/// </summary>
		public Polarity RequiredPolarity;

		/// <summary>
		/// Whether this socket allows connections
		/// </summary>
		public bool IsOpen;

		public WfcSocket (uint socketId, byte direction, Polarity requiredPolarity = Polarity.None, bool isOpen = true)
			{
			this.SocketId = socketId;
			this.Direction = (byte)(direction % 4);
			this.RequiredPolarity = requiredPolarity;
			this.IsOpen = isOpen;
			}

		/// <summary>
		/// Check if this socket is compatible with another socket
		/// </summary>
		public readonly bool IsCompatibleWith (WfcSocket other)
			{
			if (!this.IsOpen || !other.IsOpen)
				{
				return false;
				}

			// Sockets must have matching IDs and opposite directions
			bool directionMatch = (this.Direction + 2) % 4 == other.Direction;
			bool idMatch = this.SocketId == other.SocketId;

			// Check polarity compatibility (bitmask overlap, with Any/None treated as wildcards)
			bool polarityMatch =
				this.RequiredPolarity == Polarity.Any ||
				other.RequiredPolarity == Polarity.Any ||
				this.RequiredPolarity == Polarity.None ||
				other.RequiredPolarity == Polarity.None ||
				(this.RequiredPolarity & other.RequiredPolarity) != 0;

			return directionMatch && idMatch && polarityMatch;
			}
		}

	/// <summary>
	/// Buffer element for storing multiple sockets on a tile
	/// </summary>
	public struct WfcSocketBufferElement : IBufferElementData
		{
		public WfcSocket Value;

		public static implicit operator WfcSocket (WfcSocketBufferElement e) => e.Value;
		public static implicit operator WfcSocketBufferElement (WfcSocket e) => new() { Value = e };
		}

	/// <summary>
	/// WFC tile prototype definition for district generation
	/// </summary>
	public struct WfcTilePrototype : IComponentData
		{
		/// <summary>
		/// Unique identifier for this tile prototype
		/// </summary>
		public uint TileId;

		/// <summary>
		/// Weight for WFC selection probability
		/// </summary>
		public float Weight;

		/// <summary>
		/// Primary biome type this tile represents
		/// </summary>
		public BiomeType BiomeType;

		/// <summary>
		/// Primary polarity of this tile
		/// </summary>
		public Polarity PrimaryPolarity;

		/// <summary>
		/// Minimum number of connections this tile must have
		/// </summary>
		public byte MinConnections;

		/// <summary>
		/// Maximum number of connections this tile can have
		/// </summary>
		public byte MaxConnections;

		public WfcTilePrototype (uint tileId, float weight = 1.0f, BiomeType biomeType = BiomeType.Unknown,
							   Polarity primaryPolarity = Polarity.None, byte minConnections = 1, byte maxConnections = 4)
			{
			this.TileId = tileId;
			this.Weight = math.max(0.01f, weight);
			this.BiomeType = biomeType;
			this.PrimaryPolarity = primaryPolarity;
			// Explicit int math then cast to byte to avoid Unity.Mathematics overload ambiguity
			this.MinConnections = (byte)math.min(minConnections, 4);
			this.MaxConnections = (byte)math.min(maxConnections, 4);
			}
		}

	/// <summary>
	/// WFC state component for tracking collapse progress
	/// </summary>
	public struct WfcState : IComponentData
		{
		/// <summary>
		/// Current state of WFC generation
		/// </summary>
		public WfcGenerationState State;

		/// <summary>
		/// Current iteration count
		/// </summary>
		public int Iteration;

		/// <summary>
		/// Entropy (number of possible tiles) at this position
		/// </summary>
		public int Entropy;

		/// <summary>
		/// Whether this cell has been collapsed
		/// </summary>
		public bool IsCollapsed;

		/// <summary>
		/// Assigned tile ID after collapse
		/// </summary>
		public uint AssignedTileId;

		public WfcState (WfcGenerationState state = WfcGenerationState.Initialized)
			{
			this.State = state;
			this.Iteration = 0;
			this.Entropy = int.MaxValue;
			this.IsCollapsed = false;
			this.AssignedTileId = 0;
			}
		}

	/// <summary>
	/// WFC generation states
	/// </summary>
	public enum WfcGenerationState : byte
		{
		Uninitialized = 0,
		Initialized = 1,
		InProgress = 2,
		Completed = 3,
		Failed = 4,
		Contradiction = 5
		}

	/// <summary>
	/// Component to store possible tile candidates during WFC
	/// </summary>
	public struct WfcCandidateBufferElement : IBufferElementData
		{
		public uint TileId;
		public float Weight;

		public WfcCandidateBufferElement (uint tileId, float weight = 1.0f)
			{
			this.TileId = tileId;
			this.Weight = weight;
			}
		}
	}
