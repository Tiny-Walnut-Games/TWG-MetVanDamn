using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Graph
    {
    /// <summary>
    /// Places district NodeId.Coordinates by sampling positions from the world shape mask
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(WorldShapeWfcSystem))]
    public partial struct DistrictFitToShapeSystem : ISystem
        {
        private EntityQuery _shapeQ;
        private EntityQuery _unplacedQ;
        private EntityQuery _configQ;
        private EntityQuery _doneQ;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
            {
            _shapeQ = new EntityQueryBuilder(Allocator.Temp).WithAll<WorldShapeConfig, ShapeCell, WorldShapeReadyTag>().Build(ref state);
            _unplacedQ = new EntityQueryBuilder(Allocator.Temp).WithAll<NodeId, WfcState>().Build(ref state);
            _configQ = new EntityQueryBuilder(Allocator.Temp).WithAll<WorldConfiguration>().Build(ref state);
            _doneQ = new EntityQueryBuilder(Allocator.Temp).WithAll<DistrictLayoutDoneTag>().Build(ref state);
            }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
            {
            if (_doneQ.IsEmptyIgnoreFilter == false) return;
            if (_configQ.IsEmptyIgnoreFilter) return; // no world config yet
            var config = _configQ.GetSingleton<WorldConfiguration>();
            if (config.Flow != GenerationFlow.ShapeFirstOrganic) return;
            if (_shapeQ.IsEmptyIgnoreFilter) return;

            var shapeEntity = _shapeQ.GetSingletonEntity();
            var shape = state.EntityManager.GetComponentData<WorldShapeConfig>(shapeEntity);
            var cells = state.EntityManager.GetBuffer<ShapeCell>(shapeEntity);

            // Gather filled cells
            var inside = new NativeList<int2>(Allocator.Temp);
            for (int i = 0; i < cells.Length; i++) if (cells[i].Filled != 0) inside.Add(cells[i].Position);
            if (inside.Length == 0) return;

            var rng = new Random((uint)math.max(1, config.Seed * 747796405 + 2891336453));

            NativeArray<Entity> unplacedEntities = _unplacedQ.ToEntityArray(Allocator.Temp);
            NativeArray<NodeId> nodeIds = _unplacedQ.ToComponentDataArray<NodeId>(Allocator.Temp);
            try
                {
                int placed = 0;
                int2 worldSize = config.WorldSize;
                for (int i = 0; i < nodeIds.Length; i++)
                    {
                    var id = nodeIds[i];
                    if (id.Level != 0 || (id.Coordinates.x | id.Coordinates.y) != 0) continue;
                    // pick a random filled cell, map from shape grid to world coords
                    int2 cell = inside[rng.NextInt(0, inside.Length)];
                    float2 uv = ((float2)cell + 0.5f) / (float2)shape.GridSize;
                    int2 pos = new int2((int)math.round(uv.x * (worldSize.x - 1)), (int)math.round(uv.y * (worldSize.y - 1)));
                    id.Coordinates = pos;
                    state.EntityManager.SetComponentData(unplacedEntities[i], id);
                    placed++;
                    }

                var done = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(done, new DistrictLayoutDoneTag(placed, 0));
                }
            finally
                {
                if (unplacedEntities.IsCreated) unplacedEntities.Dispose();
                if (nodeIds.IsCreated) nodeIds.Dispose();
                inside.Dispose();
                }
            }
        }
    }
