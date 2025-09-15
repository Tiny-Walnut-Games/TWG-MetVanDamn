using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Shared
	{
	/// <summary>
	/// World generation flow selector
	/// </summary>
	public enum GenerationFlow : byte
		{
		/// <summary>
		/// Legacy behavior: place districts directly in world bounds, then proceed (boxy worlds likely)
		/// </summary>
		GridFirstLegacy = 0,
		/// <summary>
		/// Intended behavior: generate an organic world silhouette first, then fit districts into that shape
		/// </summary>
		ShapeFirstOrganic = 1
		}

	/// <summary>
	/// Randomization modes for world generation
	/// </summary>
	public enum RandomizationMode : byte
		{
		None = 0,     // Use curated biome polarities & upgrade rules
		Partial = 1,  // Randomize biome polarities, keep upgrade list fixed
		Full = 2      // Randomize biome polarities, upgrade availability, and traversal rules
		}

	/// <summary>
	/// World configuration component data for procedural generation settings
	/// </summary>
	public struct WorldConfiguration : IComponentData
		{
		public int Seed;
		public int2 WorldSize;
		public int TargetSectors;
		public RandomizationMode RandomizationMode;
		/// <summary>
		/// Selects the world generation flow. Defaults to GridFirstLegacy if not set.
		/// </summary>
		public GenerationFlow Flow;
		}

	/// <summary>
	/// Simple component to carry an explicit world seed (tests and authoring use this)
	/// </summary>
	public struct WorldSeed : IComponentData
		{
		public uint Value;
		}

	/// <summary>
	/// World bounds component used by scene setup and tests
	/// </summary>
	public struct WorldBounds : IComponentData
		{
		public int2 Min;
		public int2 Max;
		}

	/// <summary>
	/// Lightweight generation configuration used by authoring and tests
	/// </summary>
	public struct WorldGenerationConfig : IComponentData
		{
		public uint WorldSeed;
		public int TargetSectorCount;
		public int MaxDistrictCount;
		public float BiomeTransitionRadius;
		}

	/// <summary>
	/// Biases for world aspect when generating shape-first organic silhouettes
	/// </summary>
	public struct WorldAspectBias : IComponentData
		{
		public byte WideWeight;   // tendency for width > height
		public byte TallWeight;   // tendency for height > width
		public byte SquareWeight; // tendency for near-square
		}
	}
