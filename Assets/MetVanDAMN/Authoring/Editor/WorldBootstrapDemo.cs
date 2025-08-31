using TinyWalnutGames.MetVD.Shared;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring
	{
	/// <summary>
	/// Demonstration script showing WorldBootstrap configuration examples
	/// Add this to a GameObject to see different bootstrap configurations
	/// </summary>
	public class WorldBootstrapDemo : MonoBehaviour
		{
		[Header("Demo Configurations")]
		[SerializeField] private readonly DemoType demoType = DemoType.SmallWorld;

		[Header("Generated Configuration")]
		[SerializeField] private WorldBootstrapAuthoring targetBootstrap;

		public enum DemoType
			{
			SmallWorld,      // Quick testing: 3-5 districts, 2-4 sectors each
			MediumWorld,     // Balanced play: 6-10 districts, 3-6 sectors each  
			LargeWorld,      // Epic adventure: 10-15 districts, 4-8 sectors each
			DenseWorld,      // High detail: 5-8 districts, 6-12 sectors each
			Custom          // Use inspector values
			}

#if UNITY_EDITOR
		[ContextMenu("Apply Demo Configuration")]
		public void ApplyDemoConfiguration ()
			{
			if (this.targetBootstrap == null)
				{
				this.targetBootstrap = FindFirstObjectByType<WorldBootstrapAuthoring>();
				if (this.targetBootstrap == null)
					{
					Debug.LogError("No WorldBootstrapAuthoring found in scene. Add one first.");
					return;
					}
				}

			this.ApplyConfiguration();
			UnityEditor.EditorUtility.SetDirty(this.targetBootstrap);
			Debug.Log($"✅ Applied {this.demoType} configuration to WorldBootstrapAuthoring");
			}

		private void OnValidate ()
			{
			if (Application.isPlaying)
				{
				return;
				}

			if (this.targetBootstrap != null && this.demoType != DemoType.Custom)
				{
				this.ApplyConfiguration();
				}
			}
#endif

		private void ApplyConfiguration ()
			{
			if (this.targetBootstrap == null)
				{
				return;
				}

			switch (this.demoType)
				{
				case DemoType.SmallWorld:
					this.ConfigureSmallWorld();
					break;
				case DemoType.MediumWorld:
					this.ConfigureMediumWorld();
					break;
				case DemoType.LargeWorld:
					this.ConfigureLargeWorld();
					break;
				case DemoType.DenseWorld:
					this.ConfigureDenseWorld();
					break;
				case DemoType.Custom:
					// Don't modify - use inspector values
					break;
				default:
					break;
				}
			}

		private void ConfigureSmallWorld ()
			{
			// Quick testing configuration
			this.targetBootstrap.seed = 42;
			this.targetBootstrap.worldSize = new int2(40, 40);
			this.targetBootstrap.randomizationMode = RandomizationMode.Partial;

			this.targetBootstrap.biomeCount = new Vector2Int(2, 4);
			this.targetBootstrap.biomeWeight = 1.0f;

			this.targetBootstrap.districtCount = new Vector2Int(3, 5);
			this.targetBootstrap.districtMinDistance = 10f;
			this.targetBootstrap.districtWeight = 1.0f;

			this.targetBootstrap.sectorsPerDistrict = new Vector2Int(2, 4);
			this.targetBootstrap.sectorGridSize = new int2(4, 4);

			this.targetBootstrap.roomsPerSector = new Vector2Int(3, 8);
			this.targetBootstrap.targetLoopDensity = 0.3f;

			this.targetBootstrap.enableDebugVisualization = true;
			this.targetBootstrap.logGenerationSteps = true;
			}

		private void ConfigureMediumWorld ()
			{
			// Balanced gameplay configuration
			this.targetBootstrap.seed = 0; // Random
			this.targetBootstrap.worldSize = new int2(64, 64);
			this.targetBootstrap.randomizationMode = RandomizationMode.Partial;

			this.targetBootstrap.biomeCount = new Vector2Int(3, 6);
			this.targetBootstrap.biomeWeight = 1.2f;

			this.targetBootstrap.districtCount = new Vector2Int(6, 10);
			this.targetBootstrap.districtMinDistance = 15f;
			this.targetBootstrap.districtWeight = 1.0f;

			this.targetBootstrap.sectorsPerDistrict = new Vector2Int(3, 6);
			this.targetBootstrap.sectorGridSize = new int2(6, 6);

			this.targetBootstrap.roomsPerSector = new Vector2Int(4, 10);
			this.targetBootstrap.targetLoopDensity = 0.4f;

			this.targetBootstrap.enableDebugVisualization = true;
			this.targetBootstrap.logGenerationSteps = false;
			}

		private void ConfigureLargeWorld ()
			{
			// Epic adventure configuration
			this.targetBootstrap.seed = 0; // Random
			this.targetBootstrap.worldSize = new int2(100, 100);
			this.targetBootstrap.randomizationMode = RandomizationMode.Full;

			this.targetBootstrap.biomeCount = new Vector2Int(4, 8);
			this.targetBootstrap.biomeWeight = 1.5f;

			this.targetBootstrap.districtCount = new Vector2Int(10, 15);
			this.targetBootstrap.districtMinDistance = 20f;
			this.targetBootstrap.districtWeight = 0.8f;

			this.targetBootstrap.sectorsPerDistrict = new Vector2Int(4, 8);
			this.targetBootstrap.sectorGridSize = new int2(8, 8);

			this.targetBootstrap.roomsPerSector = new Vector2Int(5, 12);
			this.targetBootstrap.targetLoopDensity = 0.5f;

			this.targetBootstrap.enableDebugVisualization = false; // Too large for gizmos
			this.targetBootstrap.logGenerationSteps = true;
			}

		private void ConfigureDenseWorld ()
			{
			// High detail configuration
			this.targetBootstrap.seed = 12345;
			this.targetBootstrap.worldSize = new int2(80, 80);
			this.targetBootstrap.randomizationMode = RandomizationMode.Partial;

			this.targetBootstrap.biomeCount = new Vector2Int(3, 5);
			this.targetBootstrap.biomeWeight = 1.0f;

			this.targetBootstrap.districtCount = new Vector2Int(5, 8);
			this.targetBootstrap.districtMinDistance = 18f;
			this.targetBootstrap.districtWeight = 1.2f;

			this.targetBootstrap.sectorsPerDistrict = new Vector2Int(6, 12);
			this.targetBootstrap.sectorGridSize = new int2(10, 10);

			this.targetBootstrap.roomsPerSector = new Vector2Int(8, 15);
			this.targetBootstrap.targetLoopDensity = 0.6f;

			this.targetBootstrap.enableDebugVisualization = true;
			this.targetBootstrap.logGenerationSteps = true;
			}

#if UNITY_EDITOR
		[ContextMenu("Calculate Estimated Totals")]
		public void CalculateEstimatedTotals ()
			{
			if (this.targetBootstrap == null)
				{
				this.targetBootstrap = FindFirstObjectByType<WorldBootstrapAuthoring>();
				if (this.targetBootstrap == null)
					{
					Debug.LogError("No WorldBootstrapAuthoring found in scene.");
					return;
					}
				}

			// Calculate estimated totals (using averages)
			float avgBiomes = (this.targetBootstrap.biomeCount.x + this.targetBootstrap.biomeCount.y) * 0.5f;
			float avgDistricts = (this.targetBootstrap.districtCount.x + this.targetBootstrap.districtCount.y) * 0.5f;
			float avgSectorsPerDistrict = (this.targetBootstrap.sectorsPerDistrict.x + this.targetBootstrap.sectorsPerDistrict.y) * 0.5f;
			float avgRoomsPerSector = (this.targetBootstrap.roomsPerSector.x + this.targetBootstrap.roomsPerSector.y) * 0.5f;

			float totalSectors = avgDistricts * avgSectorsPerDistrict;
			float totalRooms = totalSectors * avgRoomsPerSector;

			Debug.Log($"📊 Estimated Totals for {this.demoType}:\n" +
					 $"🌿 Biomes: ~{avgBiomes:F1}\n" +
					 $"🏰 Districts: ~{avgDistricts:F1}\n" +
					 $"🏘️ Sectors: ~{totalSectors:F1}\n" +
					 $"🏠 Rooms: ~{totalRooms:F1}\n" +
					 $"🌍 World Size: {this.targetBootstrap.worldSize.x}x{this.targetBootstrap.worldSize.y}");
			}
#endif
		}
	}
