#nullable enable
using TinyWalnutGames.MetVD.Shared;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring
	{
	[DisallowMultipleComponent]
	public class WorldConfigurationAuthoring : MonoBehaviour
		{
		[Tooltip("Seed for deterministic world generation")]
		public int seed = 12345;

		[Tooltip("World bounds size (X,Z)")] public int2 worldSize = new(64, 64);

		[Tooltip("Optional target number of sectors (advisory)")]
		public int targetSectors = 16;

		[Tooltip("Randomization mode for adaptive rule generation")]
		public RandomizationMode randomizationMode = RandomizationMode.Partial;

		public Vector2Int biomeCount;
		public float biomeWeight;
		public Vector2Int districtCount;
		public float districtMinDistance;
		public int districtPlacementAttempts;
		public float districtWeight;
		public Vector2Int sectorsPerDistrict;
		public int2 sectorGridSize;
		public Vector2Int roomsPerSector;
		public float targetLoopDensity;
		public bool enableDebugVisualization;
		public bool logGenerationSteps;
		}
	}
