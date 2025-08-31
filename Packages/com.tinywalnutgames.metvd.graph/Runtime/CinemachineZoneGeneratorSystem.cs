using System.Diagnostics; // Added for Stopwatch profiling
using TinyWalnutGames.MetVD.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Graph
	{
	/// <summary>
	/// Cinemachine zone generation system implementing Master Spec requirements:
	/// - Spawn (deferred) Virtual Camera metadata for each room entity
	/// - Biome + room type + generator adaptive camera preset
	/// - Confiner bounds calculation
	/// - Captures lightweight profiling (time spent) into ProceduralRoomGenerated.GenerationTime
	/// Converted to SystemBase so tests/harness can drive Update() directly.
	/// </summary>
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(RoomNavigationGeneratorSystem))]
	public partial class CinemachineZoneGeneratorSystem : SystemBase
		{
		private EntityQuery _roomsWithNavigationQuery;

		protected override void OnCreate ()
			{
			this._roomsWithNavigationQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<NodeId, RoomHierarchyData, RoomTemplate, ProceduralRoomGenerated>()
				.WithNone<CinemachineZoneData>()
				.Build(this.EntityManager);
			this.RequireForUpdate(this._roomsWithNavigationQuery);
			}

		protected override void OnUpdate ()
			{
			using NativeArray<Entity> roomEntities = this._roomsWithNavigationQuery.ToEntityArray(Allocator.Temp);
			using NativeArray<NodeId> nodeIds = this._roomsWithNavigationQuery.ToComponentDataArray<NodeId>(Allocator.Temp);
			using NativeArray<RoomHierarchyData> roomData = this._roomsWithNavigationQuery.ToComponentDataArray<RoomHierarchyData>(Allocator.Temp);
			using NativeArray<RoomTemplate> templates = this._roomsWithNavigationQuery.ToComponentDataArray<RoomTemplate>(Allocator.Temp);

			for (int i = 0; i < roomEntities.Length; i++)
				{
				Entity roomEntity = roomEntities [ i ];
				NodeId nodeId = nodeIds [ i ];
				RoomHierarchyData hierarchy = roomData [ i ];
				RoomTemplate template = templates [ i ];

				ProceduralRoomGenerated genStatus = this.EntityManager.GetComponentData<ProceduralRoomGenerated>(roomEntity);
				if (genStatus.CinemachineGenerated)
					{
					continue;
					}

				var sw = Stopwatch.StartNew();
				GenerateCinemachineZone(this.EntityManager, roomEntity, hierarchy, template, nodeId, ref genStatus);
				sw.Stop();
				genStatus.GenerationTime += (float)sw.Elapsed.TotalSeconds;

				genStatus.CinemachineGenerated = true;
				this.EntityManager.SetComponentData(roomEntity, genStatus);
				}
			}

		private static void GenerateCinemachineZone (EntityManager entityManager, Entity roomEntity,
												   RoomHierarchyData hierarchy, RoomTemplate template,
												   NodeId nodeId, ref ProceduralRoomGenerated genStatus)
			{
			RectInt bounds = hierarchy.Bounds;
			BiomeAffinity biomeAffinity = template.CapabilityTags.BiomeType;
			CinemachineCameraPreset cameraPreset = CreateBiomeSpecificCameraPreset(biomeAffinity, hierarchy.Type, template.GeneratorType);

			// Deterministic micro-variation derived from generation seed & node for subtle per-room uniqueness.
			// This provides a meaningful semantic use of genStatus beyond flag setting/timing.
			uint seedMix = genStatus.GenerationSeed ^ (nodeId._value * 0x9E3779B9u);
			var rand = new Unity.Mathematics.Random(seedMix == 0 ? 1u : seedMix);
			// Apply small stable variations (kept subtle to avoid test flakiness).
			cameraPreset.FieldOfView += rand.NextFloat(-1.5f, 1.5f); // +/-1.5 degrees wobble
			cameraPreset.Offset.x += rand.NextFloat(-0.5f, 0.5f);
			cameraPreset.Offset.y += rand.NextFloat(-0.25f, 0.25f);

			float3 cameraPosition = CalculateCameraPosition(bounds, cameraPreset);
			BoundingBox confinerBounds = CalculateConfinerBounds(bounds, cameraPreset);

			var zoneData = new CinemachineZoneData
				{
				RoomNodeId = nodeId._value,
				CameraPosition = cameraPosition,
				ConfinerBounds = confinerBounds,
				CameraPreset = cameraPreset,
				IsActive = false,
				BlendTime = CalculateBlendTime(hierarchy.Type, template.GeneratorType),
				Priority = CalculateCameraPriority(hierarchy.Type)
				};
			entityManager.AddComponentData(roomEntity, zoneData);
			CreateCameraZoneGameObjectData(entityManager, roomEntity, zoneData, nodeId);
			}

		private static CinemachineCameraPreset CreateBiomeSpecificCameraPreset (BiomeAffinity biome, RoomType roomType, RoomGeneratorType generatorType)
			{
			var preset = new CinemachineCameraPreset
				{
				FieldOfView = 60.0f,
				FollowDamping = new float3(1f, 1f, 1f),
				LookDamping = new float3(1f, 1f, 1f),
				LensShift = float2.zero,
				Offset = new float3(0, 2, -10)
				};

			switch (biome)
				{
				case BiomeAffinity.Sky:
					preset.FieldOfView = 75f;
					preset.Offset = new float3(0, 1, -15);
					preset.FollowDamping = new float3(0.5f, 0.3f, 0.5f);
					break;
				case BiomeAffinity.Underground:
					preset.FieldOfView = 45f;
					preset.Offset = new float3(0, 1, -8);
					preset.FollowDamping = new float3(1.5f, 1.5f, 1.5f);
					break;
				case BiomeAffinity.Mountain:
					preset.FieldOfView = 65f;
					preset.Offset = new float3(0, 3, -12);
					break;
				case BiomeAffinity.Ocean:
					preset.FieldOfView = 55f;
					preset.FollowDamping = new float3(0.8f, 0.4f, 0.8f);
					break;
				case BiomeAffinity.TechZone:
					preset.FieldOfView = 58f;
					preset.FollowDamping = new float3(1.2f, 1.2f, 1.2f);
					preset.LensShift = new float2(0, 0.1f);
					break;
				case BiomeAffinity.Any:
					break;
				case BiomeAffinity.Forest:
					break;
				case BiomeAffinity.Desert:
					break;
				case BiomeAffinity.Volcanic:
					break;
				default:
					break;
				}

			switch (roomType)
				{
				case RoomType.Boss:
					preset.FieldOfView += 10f;
					preset.FollowDamping *= 0.7f;
					break;
				case RoomType.Treasure:
					preset.FieldOfView -= 5f;
					preset.FollowDamping *= 1.3f;
					break;
				case RoomType.Hub:
					preset.FieldOfView += 5f;
					preset.FollowDamping *= 1.5f;
					break;
				case RoomType.Normal:
					break;
				case RoomType.Entrance:
					break;
				case RoomType.Exit:
					break;
				case RoomType.Shop:
					break;
				case RoomType.Save:
					break;
				default:
					break;
				}

			switch (generatorType)
				{
				case RoomGeneratorType.VerticalSegment:
					preset.Offset.y += 2f;
					preset.FieldOfView += 5f;
					break;
				case RoomGeneratorType.HorizontalCorridor:
					preset.FieldOfView += 8f;
					preset.Offset.z -= 2f;
					break;
				case RoomGeneratorType.SkyBiomePlatform:
					preset.FollowDamping *= 0.6f;
					preset.LookDamping *= 0.8f;
					break;
				case RoomGeneratorType.PatternDrivenModular:
					break;
				case RoomGeneratorType.ParametricChallenge:
					break;
				case RoomGeneratorType.WeightedTilePrefab:
					break;
				case RoomGeneratorType.BiomeWeightedTerrain:
					break;
				case RoomGeneratorType.LinearBranchingCorridor:
					break;
				case RoomGeneratorType.StackedSegment:
					break;
				case RoomGeneratorType.LayeredPlatformCloud:
					break;
				case RoomGeneratorType.BiomeWeightedHeightmap:
					break;
				default:
					break;
				}
			return preset;
			}

		private static float3 CalculateCameraPosition (RectInt bounds, CinemachineCameraPreset preset)
			{
			var roomCenter = new float3(bounds.x + bounds.width * 0.5f, bounds.y + bounds.height * 0.5f, 0);
			return roomCenter + preset.Offset;
			}

		private static BoundingBox CalculateConfinerBounds (RectInt bounds, CinemachineCameraPreset preset)
			{
			const float padding = 2f;
			var min = new float3(bounds.x - padding, bounds.y - padding, preset.Offset.z - 5f);
			var max = new float3(bounds.x + bounds.width + padding, bounds.y + bounds.height + padding, preset.Offset.z + 5f);
			return new BoundingBox { Min = min, Max = max };
			}

		private static float CalculateBlendTime (RoomType roomType, RoomGeneratorType generatorType)
			{
			return roomType switch
				{
					RoomType.Boss => 0.3f,
					RoomType.Hub => 1.0f,
					RoomType.Treasure => 0.5f,
					_ => generatorType switch
						{
							RoomGeneratorType.HorizontalCorridor => 0.7f,
							RoomGeneratorType.VerticalSegment => 0.4f,
							_ => 0.6f
							}
					};
			}

		private static int CalculateCameraPriority (RoomType roomType)
			{
			return roomType switch
				{
					RoomType.Boss => 15,
					RoomType.Treasure => 12,
					RoomType.Hub => 10,
					RoomType.Save => 8,
					RoomType.Shop => 8,
					_ => 5
					};
			}

		private static void CreateCameraZoneGameObjectData (EntityManager entityManager, Entity roomEntity,
														   CinemachineZoneData zoneData, NodeId nodeId)
			{
			// Provide enriched metadata so a later hybrid bridge can spawn & configure the actual Cinemachine Virtual Camera.
			float3 boundsCenter = zoneData.ConfinerBounds.Center;
			float3 boundsSize = zoneData.ConfinerBounds.Size;
			var gameObjectData = new CinemachineGameObjectReference
				{
				RoomEntity = roomEntity,
				CameraName = $"VCam_Room_{nodeId._value}",
				ShouldCreateGameObject = true,
				GameObjectInstanceId = 0,
				FieldOfView = zoneData.CameraPreset.FieldOfView,
				Priority = zoneData.Priority,
				BoundsCenter = new float2(boundsCenter.x, boundsCenter.y),
				BoundsSize = new float2(boundsSize.x, boundsSize.y)
				};
			entityManager.AddComponentData(roomEntity, gameObjectData);
			}
		}

	public struct CinemachineZoneData : IComponentData
		{
		public uint RoomNodeId;
		public float3 CameraPosition;
		public BoundingBox ConfinerBounds;
		public CinemachineCameraPreset CameraPreset;
		public bool IsActive;
		public float BlendTime;
		public int Priority;
		}

	public struct CinemachineCameraPreset : IComponentData
		{
		public float FieldOfView;
		public float3 FollowDamping;
		public float3 LookDamping;
		public float2 LensShift;
		public float3 Offset;
		}

	public struct CinemachineGameObjectReference : IComponentData
		{
		public Entity RoomEntity;
		public FixedString64Bytes CameraName;
		public bool ShouldCreateGameObject;
		public int GameObjectInstanceId;
		// Enriched metadata derived from zone data for later GameObject creation
		public float FieldOfView;
		public int Priority;
		public float2 BoundsCenter;
		public float2 BoundsSize;
		}

	public struct BoundingBox
		{
		public float3 Min;
		public float3 Max;
		public readonly float3 Center => (this.Min + this.Max) * 0.5f; // DO NOT REMOVE readonly FOR SANITY
		public readonly float3 Size => this.Max - this.Min; // DO NOT REMOVE readonly FOR SANITY
		}
	}
