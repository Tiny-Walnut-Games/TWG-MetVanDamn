using TinyWalnutGames.MetVD.Shared;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring
	{
	/// <summary>
	/// Bootstrap Authoring Object that generates full world hierarchies procedurally.
	/// This is the single scene object that replaces manual DistrictAuthoring placement.
	/// Supports configurable ranges for biomes, districts, sectors, and rooms.
	/// </summary>
	[DisallowMultipleComponent]
	public class WorldBootstrapAuthoring : MonoBehaviour
		{
		[Header("World Configuration")]
		[Tooltip("Seed for deterministic world generation (0 = random)")]
		public int seed = 0;

		[Tooltip("World bounds size (X,Z)")]
		public int2 worldSize = new(64, 64);

		[Tooltip("Randomization mode for adaptive rule generation")]
		public RandomizationMode randomizationMode = RandomizationMode.Partial;

		[Header("Biome Generation")]
		[Tooltip("Number of different biomes to generate")]
		public Vector2Int biomeCount = new(3, 6);

		[Tooltip("Weight for biome placement (higher = more influence)")]
		[Range(0.1f, 2.0f)]
		public float biomeWeight = 1.0f;

		[Header("District Generation")]
		[Tooltip("Number of districts to generate")]
		public Vector2Int districtCount = new(4, 12);

		[Tooltip("Minimum distance between districts")]
		[Range(5f, 50f)]
		public float districtMinDistance = 15f;

		[Tooltip("Number of attempts to place each district")]
		public int districtPlacementAttempts = 10;

		[Tooltip("Weight for district placement")]
		[Range(0.1f, 2.0f)]
		public float districtWeight = 1.0f;

		[Header("Sector Generation")]
		[Tooltip("Number of sectors per district")]
		public Vector2Int sectorsPerDistrict = new(2, 8);

		[Tooltip("Local grid size for sector subdivision")]
		public int2 sectorGridSize = new(6, 6);

		[Header("Room Generation")]
		[Tooltip("Number of rooms per sector")]
		public Vector2Int roomsPerSector = new(3, 12);

		[Tooltip("Target loop density for room connections")]
		[Range(0.1f, 1.0f)]
		public float targetLoopDensity = 0.3f;

		[Header("Advanced Options")]
		[Tooltip("Enable debug visualization during generation")]
		public bool enableDebugVisualization = true;

		[Tooltip("Log generation steps to console")]
		public bool logGenerationSteps = true;

		private void OnValidate ()
			{
			// Ensure valid ranges
			this.biomeCount.x = Mathf.Max(1, this.biomeCount.x);
			this.biomeCount.y = Mathf.Max(this.biomeCount.x, this.biomeCount.y);

			this.districtCount.x = Mathf.Max(1, this.districtCount.x);
			this.districtCount.y = Mathf.Max(this.districtCount.x, this.districtCount.y);

			this.sectorsPerDistrict.x = Mathf.Max(1, this.sectorsPerDistrict.x);
			this.sectorsPerDistrict.y = Mathf.Max(this.sectorsPerDistrict.x, this.sectorsPerDistrict.y);

			this.roomsPerSector.x = Mathf.Max(1, this.roomsPerSector.x);
			this.roomsPerSector.y = Mathf.Max(this.roomsPerSector.x, this.roomsPerSector.y);

			this.sectorGridSize = math.max(new int2(2, 2), this.sectorGridSize);
			}

#if UNITY_EDITOR
		private void OnDrawGizmos ()
			{
			if (this.enableDebugVisualization)
				{
				this.DrawWorldBounds();
				}
			}

		private void DrawWorldBounds ()
			{
			Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.3f);
			Vector3 center = this.transform.position;
			var size = new Vector3(this.worldSize.x, 0.1f, this.worldSize.y);
			Gizmos.DrawWireCube(center, size);

			Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.1f);
			Gizmos.DrawCube(center, size);
			}
#endif
		}
	}