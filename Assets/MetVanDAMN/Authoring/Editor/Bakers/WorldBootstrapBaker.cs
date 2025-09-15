#if UNITY_EDITOR
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Shared;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	public class WorldBootstrapBaker : Baker<WorldBootstrapAuthoring>
		{
		public override void Bake(WorldBootstrapAuthoring authoring)
			{
			Entity entity = GetEntity(TransformUsageFlags.None);
			// Map authoring ranges to *Range fields
			var biomeSettings = new BiomeGenerationSettings
				{
				BiomeCountRange = new int2(authoring.biomeCount.x, authoring.biomeCount.y),
				BiomeWeight = authoring.biomeWeight
				};
			var districtSettings = new DistrictGenerationSettings
				{
				DistrictCountRange = new int2(authoring.districtCount.x, authoring.districtCount.y),
				DistrictMinDistance = authoring.districtMinDistance,
				DistrictWeight = authoring.districtWeight
				};
			var sectorSettings = new SectorGenerationSettings
				{
				SectorsPerDistrictRange = new int2(authoring.sectorsPerDistrict.x, authoring.sectorsPerDistrict.y),
				SectorGridSize = authoring.sectorGridSize
				};
			var roomSettings = new RoomGenerationSettings
				{
				RoomsPerSectorRange = new int2(authoring.roomsPerSector.x, authoring.roomsPerSector.y),
				TargetLoopDensity = authoring.targetLoopDensity
				};
			var bootstrapConfig = new WorldBootstrapConfiguration(
				authoring.seed,
				authoring.worldSize,
				authoring.randomizationMode,
				biomeSettings,
				districtSettings,
				sectorSettings,
				roomSettings,
				authoring.enableDebugVisualization,
				authoring.logGenerationSteps
			);
			AddComponent(entity, bootstrapConfig);
			AddComponent(entity, new WorldConfiguration
				{
				Seed = authoring.seed,
				WorldSize = authoring.worldSize,
				TargetSectors = authoring.sectorsPerDistrict.y * authoring.districtCount.y,
				RandomizationMode = authoring.randomizationMode
				});
			// Add WorldSeed for determinism (required by DistrictWfcSystem)
			AddComponent(entity, new WorldSeed { Value = (uint)authoring.seed });
			}
		}
	}
#endif
