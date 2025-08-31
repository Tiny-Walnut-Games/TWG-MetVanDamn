using TinyWalnutGames.MetVD.Core;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring
	{
	public class BiomeFieldAuthoring : MonoBehaviour
		{
		[Header("Biome Configuration")]
		public BiomeType biomeType = BiomeType.SolarPlains;
		public BiomeType primaryBiome = BiomeType.SolarPlains;
		public BiomeType secondaryBiome = BiomeType.Unknown;

		[Header("Field Properties")]
		[Range(0f, 1f)] public float strength = 1f;
		[Range(0f, 1f)] public float gradient = 0.5f;
		public float fieldRadius = 50f;
		public Polarity polarity = Polarity.Any;

		[Header("Node Configuration")]
		public uint nodeId = 0;

		[Header("Art Integration")]
		public BiomeArtProfile artProfile;
		}
	}
