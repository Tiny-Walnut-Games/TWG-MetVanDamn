using TinyWalnutGames.MetVD.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Graph
    {
    /// <summary>
    /// Builds a coarse world shape mask via a simple seeded cellular automata pass
    /// (placeholder for WFC; deterministic per seed). Emits WorldShapeReadyTag when filled.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(WorldAspectRandomizerSystem))]
    public partial struct WorldShapeWfcSystem : ISystem
        {
        private EntityQuery _configQ;
        private EntityQuery _shapeQ;
        private bool _done;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
            {
            _configQ = new EntityQueryBuilder(Allocator.Temp).WithAll<WorldConfiguration>().Build(ref state);
            _shapeQ = new EntityQueryBuilder(Allocator.Temp).WithAll<WorldShapeConfig>().Build(ref state);
            // Do not hard-require; allow compatibility bridge to populate config later in the frame.
            }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
            {
            if (_configQ.IsEmptyIgnoreFilter) return;
            var config = _configQ.GetSingleton<WorldConfiguration>();
            if (config.Flow != GenerationFlow.ShapeFirstOrganic || _done)
                return;

            // Ensure a shape config entity exists
            Entity shapeEntity;
            if (_shapeQ.IsEmptyIgnoreFilter)
                {
                shapeEntity = state.EntityManager.CreateEntity();
                int2 grid = new int2(math.clamp(config.WorldSize.x / 2, 16, 128), math.clamp(config.WorldSize.y / 2, 16, 128));
                state.EntityManager.AddComponentData(shapeEntity, new WorldShapeConfig { GridSize = grid, FillTarget = 0.65f });
                state.EntityManager.AddBuffer<ShapeCell>(shapeEntity);
                }
            else
                {
                shapeEntity = _shapeQ.GetSingletonEntity();
                }

            var shape = state.EntityManager.GetComponentData<WorldShapeConfig>(shapeEntity);
            var buffer = state.EntityManager.GetBuffer<ShapeCell>(shapeEntity);
            buffer.Clear();

            var rng = new Random((uint)math.max(1, config.Seed * 1103515245 + 12345));
            int count = shape.GridSize.x * shape.GridSize.y;
            var temp = new NativeArray<byte>(count, Allocator.Temp);
            try
                {
                // Seed random field
                for (int i = 0; i < count; i++) temp[i] = (byte)(rng.NextFloat() < shape.FillTarget ? 1 : 0);
                // Smooth a few iterations to produce an organic blob
                int2 s = shape.GridSize;
                for (int iter = 0; iter < 3; iter++)
                    {
                    for (int y = 0; y < s.y; y++)
                        for (int x = 0; x < s.x; x++)
                            {
                            int idx = y * s.x + x;
                            int neighbors = 0;
                            for (int oy = -1; oy <= 1; oy++)
                                for (int ox = -1; ox <= 1; ox++)
                                    {
                                    if (ox == 0 && oy == 0) continue;
                                    int nx = x + ox, ny = y + oy;
                                    if ((uint)nx < (uint)s.x && (uint)ny < (uint)s.y)
                                        neighbors += temp[ny * s.x + nx];
                                    }
                            byte val = temp[idx];
                            if (neighbors >= 5) val = 1; else if (neighbors <= 3) val = 0;
                            temp[idx] = val;
                            }
                    }
                // Emit buffer
                for (int y = 0; y < shape.GridSize.y; y++)
                    for (int x = 0; x < shape.GridSize.x; x++)
                        buffer.Add(new ShapeCell(new int2(x, y), temp[y * shape.GridSize.x + x]));
                }
            finally
                {
                temp.Dispose();
                }

            state.EntityManager.AddComponent<WorldShapeReadyTag>(shapeEntity);
            _done = true;
            }
        }
    }
