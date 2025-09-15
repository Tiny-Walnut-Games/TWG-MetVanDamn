using TinyWalnutGames.MetVD.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Graph
    {
    /// <summary>
    /// Selects world aspect (wide / tall / square) deterministically from seed and bias
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct WorldAspectRandomizerSystem : ISystem
        {
        private EntityQuery _configQ;
        private EntityQuery _biasQ;
        private bool _initialized;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
            {
            _configQ = new EntityQueryBuilder(Allocator.Temp).WithAll<WorldConfiguration>().Build(ref state);
            _biasQ = new EntityQueryBuilder(Allocator.Temp).WithAll<WorldAspectBias>().Build(ref state);
            }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
            {
            if (_configQ.IsEmptyIgnoreFilter) return;
            var config = _configQ.GetSingleton<WorldConfiguration>();
            if (config.Flow != GenerationFlow.ShapeFirstOrganic)
                {
                return; // legacy flow - do nothing
                }

            if (_initialized)
                return;

            var bias = _biasQ.IsEmptyIgnoreFilter ? new WorldAspectBias { WideWeight = 55, TallWeight = 35, SquareWeight = 10 } : _biasQ.GetSingleton<WorldAspectBias>();
            var rng = new Random((uint)math.max(1, config.Seed));
            int total = bias.WideWeight + bias.TallWeight + bias.SquareWeight;
            int pick = rng.NextInt(0, math.max(1, total));

            // Maintain approximate area while altering aspect
            int area = math.max(64 * 64, config.WorldSize.x * config.WorldSize.y);
            float aspect;
            if (pick < bias.WideWeight) aspect = rng.NextFloat(1.6f, 2.4f);
            else if (pick < bias.WideWeight + bias.TallWeight) aspect = 1f / rng.NextFloat(1.6f, 2.4f);
            else aspect = rng.NextFloat(0.9f, 1.1f);

            float w = math.sqrt(area * aspect);
            float h = area / w;
            config.WorldSize = new int2(math.clamp((int)w, 32, 256), math.clamp((int)h, 32, 256));
            _configQ.GetSingletonRW<WorldConfiguration>().ValueRW = config;

            _initialized = true;
            }
        }
    }
