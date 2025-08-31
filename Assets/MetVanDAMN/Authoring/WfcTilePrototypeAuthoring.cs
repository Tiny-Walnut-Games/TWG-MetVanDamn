using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring
	{
	/// <summary>
	/// Authoring component for WFC tile prototypes
	/// Allows designers to define tile rules and socket configurations in the inspector
	/// </summary>
	public class WfcTilePrototypeAuthoring : MonoBehaviour
		{
		[Header("Tile Identity")]
		[Tooltip("Unique identifier for this tile prototype")]
		public uint tileId = 1;

		[Header("Generation Rules")]
		[Range(0.01f, 5.0f)]
		[Tooltip("Weight for WFC selection probability (higher = more likely)")]
		public float weight = 1.0f;

		[Tooltip("Primary biome type this tile represents")]
		public BiomeType biomeType = BiomeType.Unknown;

		[Tooltip("Primary polarity of this tile")]
		public Polarity primaryPolarity = Polarity.None;

		[Header("Connection Constraints")]
		[Range(0, 4)]
		[Tooltip("Minimum number of connections this tile must have")]
		public byte minConnections = 1;

		[Range(1, 4)]
		[Tooltip("Maximum number of connections this tile can have")]
		public byte maxConnections = 4;

		[Header("Socket Configuration")]
		[Tooltip("Socket definitions for this tile (up to 4 sockets for N/E/S/W)")]
		public WfcSocketConfig [ ] sockets = new WfcSocketConfig [ 0 ];

		private void OnValidate ()
			{
			// Ensure tile ID is not zero
			if (this.tileId == 0)
				{
				this.tileId = 1;
				}

			// Ensure weight is positive
			if (this.weight <= 0)
				{
				this.weight = 0.01f;
				}

			// Ensure connection constraints are valid
			this.minConnections = (byte)Mathf.Clamp(this.minConnections, 0, 4);
			this.maxConnections = (byte)Mathf.Clamp(this.maxConnections, this.minConnections, 4);

			// Validate socket array length
			if (this.sockets != null && this.sockets.Length > 4)
				{
				System.Array.Resize(ref this.sockets, 4);
				Debug.LogWarning($"WfcTilePrototypeAuthoring: Socket array truncated to 4 elements on {this.name}", this);
				}
			}

		private void Reset ()
			{
			// Provide sensible defaults when component is first added
			this.tileId = (uint)(this.GetInstanceID() & 0x7FFFFFFF); // Unique but positive ID
			this.weight = 1.0f;
			this.biomeType = BiomeType.Unknown;
			this.primaryPolarity = Polarity.None;
			this.minConnections = 1;
			this.maxConnections = 4;

			// Default socket configuration: basic open sockets in all directions
			this.sockets = new WfcSocketConfig [ ]
			{
				new() { socketId = 1, direction = 0, requiredPolarity = Polarity.None, isOpen = true },
				new() { socketId = 1, direction = 1, requiredPolarity = Polarity.None, isOpen = true },
				new() { socketId = 1, direction = 2, requiredPolarity = Polarity.None, isOpen = true },
				new() { socketId = 1, direction = 3, requiredPolarity = Polarity.None, isOpen = true }
			};
			}
		}

	/// <summary>
	/// Serializable socket configuration for the inspector
	/// </summary>
	[System.Serializable]
	public struct WfcSocketConfig
		{
		[Tooltip("Socket ID for matching compatible tiles")]
		public uint socketId;

		[Range(0, 3)]
		[Tooltip("Direction this socket faces (0=North, 1=East, 2=South, 3=West)")]
		public byte direction;

		[Tooltip("Required polarity for this socket connection")]
		public Polarity requiredPolarity;

		[Tooltip("Whether this socket allows connections")]
		public bool isOpen;

		/// <summary>
		/// Convert to runtime WfcSocket
		/// </summary>
		public readonly WfcSocket ToWfcSocket ()
			{
			return new WfcSocket(this.socketId, this.direction, this.requiredPolarity, this.isOpen);
			}
		}
	}