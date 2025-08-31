using TinyWalnutGames.MetVD.Core;
using Unity.Entities;

namespace TinyWalnutGames.MetVD.Biome
	{
	/// <summary>
	/// Global biome field configuration for the world. Used by biome systems to seed
	/// polarity and biome gradients. This is a lightweight data component created by
	/// the SmokeTest scene setup.
	/// </summary>
	public struct BiomeFieldData : IComponentData
		{
		public BiomeType PrimaryBiome;
		public BiomeType SecondaryBiome;
		public float Strength; // Overall field strength/intensity
		public float Gradient; // How quickly field changes over distance (0..1)
		}

	// buffer element to hold biome influence data per entity
	public struct BiomeInfluence : IBufferElementData
		{
		public float Influence;
		public float Distance;
		public BiomeType Biome;
		}

	// BiomeValidationRecord holds validation data for biome interactions
	public struct BiomeValidationRecord : IBufferElementData
		{
		public int NodeId;              // numeric ID or hash for node
		public int BufferIndex;         // index or entity order reference
		public float Distance;          // optional distance metric
		public BiomeType BiomeType;     // biome classification
		public Polarity PrimaryPolarity;// captured polarity
		public float DifficultyModifier;// difficulty snapshot
		public bool IsValid;            // validation flag
		}
	}
