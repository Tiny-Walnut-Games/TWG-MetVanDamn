using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Graph
	{
	public enum DistrictPlacementStrategy : byte { PoissonDisc = 0, JitteredGrid = 1 }

	[BurstCompile]
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	public partial struct DistrictLayoutSystem : ISystem
		{
		private EntityQuery _unplacedQuery;
		private EntityQuery _worldConfigQuery;
		private EntityQuery _layoutDoneQuery;
		private bool _loggedFallback;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
			{
			_unplacedQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<NodeId, WfcState>()
				.Build(ref state);
			_worldConfigQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<WorldConfiguration>()
				.Build(ref state);
			_layoutDoneQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<DistrictLayoutDoneTag>()
				.Build(ref state);
			// Only require unplaced districts; world config now optional (fallback if missing)
			state.RequireForUpdate(_unplacedQuery);
			}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
			{
			if (!_layoutDoneQuery.IsEmptyIgnoreFilter)
				{
				return;
				}

			// Fallback configuration if authoring/baker did not supply one (e.g., tests / legacy bootstrap)
			WorldConfiguration worldConfig;
			if (_worldConfigQuery.IsEmptyIgnoreFilter)
				{
				worldConfig = new WorldConfiguration { Seed = (int)(state.WorldUnmanaged.Time.ElapsedTime * 1000 + 1), WorldSize = new int2(64, 64), TargetSectors = 0, RandomizationMode = RandomizationMode.None };
				if (!_loggedFallback && SystemAPI.Time.ElapsedTime > 0)
					{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
					// UnityEngine.Debug.LogWarning("DistrictLayoutSystem: WorldConfiguration missing. Using fallback defaults (64x64, all sectors)."); // REMOVED: Debug.LogWarning not allowed in Burst jobs
					// Fallback configuration: 64x64, all sectors (check _loggedFallback flag to see if fallback was used)
#endif
					_loggedFallback = true;
					}
				}
			else
				{
				worldConfig = _worldConfigQuery.GetSingleton<WorldConfiguration>();
				}

			NativeArray<Entity> unplacedEntities = _unplacedQuery.ToEntityArray(Allocator.Temp);
			NativeArray<NodeId> nodeIds = _unplacedQuery.ToComponentDataArray<NodeId>(Allocator.Temp);
			try
				{
				// Collect all level 0 districts still at (0,0)
				int unplacedCount = 0;
				for (int i = 0; i < nodeIds.Length; i++)
					{
					NodeId n = nodeIds [ i ];
					if (n.Level == 0 && n.Coordinates.x == 0 && n.Coordinates.y == 0)
						{
						unplacedCount++;
						}
					}
				if (unplacedCount == 0)
					{
					Entity layoutDoneEntity = state.EntityManager.CreateEntity();
					state.EntityManager.AddComponentData(layoutDoneEntity, new DistrictLayoutDoneTag(0, 0));
					return;
					}

				// ALWAYS place all remaining unplaced districts so we do not leave any at origin.
				var random = new Random((uint)(worldConfig.Seed == 0 ? 1 : worldConfig.Seed));
				DistrictPlacementStrategy strategy = unplacedCount > 16 ? DistrictPlacementStrategy.JitteredGrid : DistrictPlacementStrategy.PoissonDisc;
				var positions = new NativeArray<int2>(unplacedCount, Allocator.Temp);
				try
					{
					GenerateDistrictPositions(positions, worldConfig.WorldSize, strategy, ref random);

					// Derive sectors per district: distribute TargetSectors across placed districts (>=1)
					int sectorsPerDistrict = 1;
					if (worldConfig.TargetSectors > 0)
						{
						sectorsPerDistrict = math.max(1, worldConfig.TargetSectors / math.max(1, unplacedCount));
						}

					sectorsPerDistrict = math.clamp(sectorsPerDistrict, 1, 25); // safety cap

					int positionIndex = 0;
					int placedCount = 0;
					for (int i = 0; i < nodeIds.Length && positionIndex < unplacedCount; i++)
						{
						NodeId nodeId = nodeIds [ i ];
						if (nodeId.Level == 0 && nodeId.Coordinates.x == 0 && nodeId.Coordinates.y == 0)
							{
							nodeId.Coordinates = positions [ positionIndex++ ];
							state.EntityManager.SetComponentData(unplacedEntities [ i ], nodeId);
							// Attach sector hierarchy data if not already present
							if (!state.EntityManager.HasComponent<SectorHierarchyData>(unplacedEntities [ i ]))
								{
								var sectorData = new SectorHierarchyData(new int2(6, 6), sectorsPerDistrict, random.NextUInt());
								state.EntityManager.AddComponentData(unplacedEntities [ i ], sectorData);
								}
							placedCount++;
							}
						}
					// Now mark done AFTER all have coordinates.
					Entity doneEntity = state.EntityManager.CreateEntity();
					state.EntityManager.AddComponentData(doneEntity, new DistrictLayoutDoneTag(placedCount, 0));
					}
				finally
					{
					if (positions.IsCreated)
						{
						positions.Dispose();
						}
					}
				}
			finally
				{
				if (unplacedEntities.IsCreated)
					{
					unplacedEntities.Dispose();
					}

				if (nodeIds.IsCreated)
					{
					nodeIds.Dispose();
					}
				}
			}

		private static void GenerateDistrictPositions(NativeArray<int2> positions, int2 worldSize, DistrictPlacementStrategy strategy, ref Random random)
			{
			switch (strategy)
				{
				case DistrictPlacementStrategy.PoissonDisc: GeneratePoissonDiscPositions(positions, worldSize, ref random); break;
				case DistrictPlacementStrategy.JitteredGrid: GenerateJitteredGridPositions(positions, worldSize, ref random); break;
				default:
					break;
				}
			}
		private static void GeneratePoissonDiscPositions(NativeArray<int2> positions, int2 worldSize, ref Random random)
			{
			float minDistance = math.min(worldSize.x, worldSize.y) * 0.2f;
			int maxAttempts = 30;
			for (int i = 0; i < positions.Length; i++)
				{
				bool validPosition = false; int attempts = 0;
				while (!validPosition && attempts < maxAttempts)
					{
					int2 candidate = new(random.NextInt(0, worldSize.x), random.NextInt(0, worldSize.y));
					validPosition = true;
					for (int j = 0; j < i; j++)
						{
						float distance = math.length(new float2(candidate - positions [ j ]));
						if (distance < minDistance) { validPosition = false; break; }
						}
					if (validPosition)
						{
						positions [ i ] = candidate;
						}

					attempts++;
					}
				if (!validPosition)
					{
					positions [ i ] = new(random.NextInt(0, worldSize.x), random.NextInt(0, worldSize.y));
					}
				}
			}
		private static void GenerateJitteredGridPositions(NativeArray<int2> positions, int2 worldSize, ref Random random)
			{
			int gridDim = (int)math.ceil(math.sqrt(positions.Length));
			float2 cellSize = new float2(worldSize) / gridDim;
			float jitterAmount = math.min(cellSize.x, cellSize.y) * 0.3f;
			var shuffledIndices = new NativeArray<int>(positions.Length, Allocator.Temp);
			for (int i = 0; i < shuffledIndices.Length; i++)
				{
				shuffledIndices [ i ] = i;
				}

			for (int i = shuffledIndices.Length - 1; i > 0; i--)
				{ int j = random.NextInt(0, i + 1); (shuffledIndices [ i ], shuffledIndices [ j ]) = (shuffledIndices [ j ], shuffledIndices [ i ]); }
			for (int i = 0; i < positions.Length; i++)
				{
				int gridIndex = shuffledIndices [ i ];
				int gridX = gridIndex % gridDim; int gridY = gridIndex / gridDim;
				float2 cellCenter = new float2(gridX + 0.5f, gridY + 0.5f) * cellSize;
				float2 jitter = new(random.NextFloat(-jitterAmount, jitterAmount), random.NextFloat(-jitterAmount, jitterAmount));
				float2 finalPosition = cellCenter + jitter;
				positions [ i ] = new(math.clamp((int)finalPosition.x, 0, worldSize.x - 1), math.clamp((int)finalPosition.y, 0, worldSize.y - 1));
				}
			shuffledIndices.Dispose();
			}
		}
	}
