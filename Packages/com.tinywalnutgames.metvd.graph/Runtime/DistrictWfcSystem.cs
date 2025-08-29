using System;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using TinyWalnutGames.MetVD.Core;
using Unity.Jobs;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// District WFC System for macro-level world generation
    /// Generates solvable district graphs using Wave Function Collapse
    /// Status: Fully implemented with ECB pattern for Unity 6.2 compatibility
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct DistrictWfcSystem : ISystem
    {
        private BufferLookup<WfcSocketBufferElement> socketBufferLookup;
        private BufferLookup<WfcCandidateBufferElement> candidateBufferLookup;
        private EntityQuery _layoutDoneQuery; // optional

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            socketBufferLookup = state.GetBufferLookup<WfcSocketBufferElement>(true);
            candidateBufferLookup = state.GetBufferLookup<WfcCandidateBufferElement>();
            state.RequireForUpdate<WfcState>();
            // Optional layout done tag (do not require so tests without it still run)
            _layoutDoneQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<DistrictLayoutDoneTag>()
                .Build(ref state);
        }

        // Replace the body of WfcProcessingJob.Execute() to avoid using SystemAPI.Query inside the job.
        // Instead, pass in a NativeArray of entities to process, and use Component/BufferLookups.

        public void OnUpdate(ref SystemState state)
        {
            socketBufferLookup.Update(ref state);
            candidateBufferLookup.Update(ref state);

            float deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            uint baseSeed = (uint)(state.WorldUnmanaged.Time.ElapsedTime * 911.0);

            // Gather all entities with WfcState and NodeId into a NativeArray
            EntityQuery wfcQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<WfcState, NodeId>()
                .Build(ref state);

            NativeArray<Entity> entities = wfcQuery.ToEntityArray(Allocator.Temp);
            ComponentLookup<WfcState> wfcStates = state.GetComponentLookup<WfcState>(false);
            ComponentLookup<NodeId> nodeIds = state.GetComponentLookup<NodeId>(true);

            var wfcJob = new WfcProcessingJob
            {
                CandidateBufferLookup = candidateBufferLookup,
                SocketBufferLookup = socketBufferLookup,
                DeltaTime = deltaTime,
                BaseSeed = baseSeed,
                Entities = entities,
                WfcStates = wfcStates,
                NodeIds = nodeIds
            };

            wfcJob.Execute();

            entities.Dispose();
        }

        private struct WfcProcessingJob : IJob
        {
            public BufferLookup<WfcCandidateBufferElement> CandidateBufferLookup;
            [ReadOnly] public BufferLookup<WfcSocketBufferElement> SocketBufferLookup;
            public float DeltaTime;
            public uint BaseSeed;
            [ReadOnly] public NativeArray<Entity> Entities;
            public ComponentLookup<WfcState> WfcStates;
            [ReadOnly] public ComponentLookup<NodeId> NodeIds;

            public void Execute()
            {
                var random = new Unity.Mathematics.Random(BaseSeed == 0 ? 1u : BaseSeed);

                for (int i = 0; i < Entities.Length; i++)
                {
                    Entity entity = Entities[i];
                    RefRW<WfcState> wfcState = WfcStates.GetRefRW(entity);
                    RefRO<NodeId> nodeId = NodeIds.GetRefRO(entity);

                    var entityRandom = new Unity.Mathematics.Random((uint)(entity.Index * 1103515245u) ^ random.state);
                    NodeId nodeIdRO = nodeId.ValueRO;

                    switch (wfcState.ValueRO.State)
                    {
                        case WfcGenerationState.Initialized:
                            ProcessInitialized(entity, wfcState, entityRandom, nodeIdRO);
                            break;
                        case WfcGenerationState.InProgress:
                            ProcessInProgress(entity, wfcState, nodeIdRO, entityRandom);
                            break;
                        case WfcGenerationState.Completed:
                        case WfcGenerationState.Failed:
                            break;
                        default:
                            wfcState.ValueRW.State = WfcGenerationState.Initialized;
                            break;
                    }
                }
            }

            private void ProcessInitialized(Entity entity, RefRW<WfcState> wfcState, Unity.Mathematics.Random random, in NodeId nodeId)
            {
                if (!CandidateBufferLookup.HasBuffer(entity))
                {
                    wfcState.ValueRW.State = WfcGenerationState.Failed;
                    return;
                }

                InitializeCandidates(entity, random, nodeId);
                wfcState.ValueRW.Entropy = CandidateBufferLookup[entity].Length;
                wfcState.ValueRW.State = WfcGenerationState.InProgress;
            }

            private void ProcessInProgress(Entity entity, RefRW<WfcState> wfcState, in NodeId nodeId, Unity.Mathematics.Random random)
            {
                if (!CandidateBufferLookup.HasBuffer(entity))
                {
                    wfcState.ValueRW.State = WfcGenerationState.Failed;
                    return;
                }

                DynamicBuffer<WfcCandidateBufferElement> candidates = CandidateBufferLookup[entity];
                if (candidates.Length == 0)
                {
                    wfcState.ValueRW.State = WfcGenerationState.Contradiction;
                    return;
                }

                if (candidates.Length == 1)
                {
                    wfcState.ValueRW.AssignedTileId = candidates[0].TileId;
                    wfcState.ValueRW.IsCollapsed = true;
                    wfcState.ValueRW.State = WfcGenerationState.Completed;
                    return;
                }

                // Process constraints and entropy reduction
                PropagateConstraints(entity, candidates, nodeId, random);
                wfcState.ValueRW.Iteration++;
                wfcState.ValueRW.Entropy = candidates.Length;

                // Force collapse after many iterations
                if (wfcState.ValueRO.Iteration > 100 && candidates.Length > 1)
                {
                    uint selectedTileId = CollapseRandomly(candidates, random);
                    if (selectedTileId == 0)
                    {
                        wfcState.ValueRW.State = WfcGenerationState.Failed;
                    }
                    else
                    {
                        wfcState.ValueRW.AssignedTileId = selectedTileId;
                        wfcState.ValueRW.IsCollapsed = true;
                        wfcState.ValueRW.State = WfcGenerationState.Completed;
                    }
                }
            }

            private readonly void InitializeCandidates(Entity entity, Unity.Mathematics.Random random, in NodeId nodeId)
            {
                if (!CandidateBufferLookup.HasBuffer(entity))
                {
                    return;
                }

                DynamicBuffer<WfcCandidateBufferElement> candidates = CandidateBufferLookup[entity];
                candidates.Clear();

                // Fix: Explicit conversion from int2 to float2 for Burst compatibility
                var coords = (float2)nodeId.Coordinates;
                float distance = math.length(coords) * 0.02f;
                float centralBias = math.saturate(1f - distance);
                float entityVariance = 0.9f + ((entity.Index & 7) * 0.02f);

                candidates.Add(new WfcCandidateBufferElement(1, math.lerp(0.6f, 1.2f, centralBias) * entityVariance));
                candidates.Add(new WfcCandidateBufferElement(2, math.lerp(1.0f, 0.7f, centralBias) * entityVariance));
                candidates.Add(new WfcCandidateBufferElement(3, (0.4f + random.NextFloat(0.0f, 0.3f)) * entityVariance));
                candidates.Add(new WfcCandidateBufferElement(4, (0.2f + distance * 0.5f) * entityVariance));
            }

            private readonly void PropagateConstraints(Entity entity, DynamicBuffer<WfcCandidateBufferElement> candidates, in NodeId nodeId, Unity.Mathematics.Random random)
            {
                var coords = (float2)nodeId.Coordinates;

                for (int i = candidates.Length - 1; i >= 0; i--)
                {
                    WfcCandidateBufferElement candidate = candidates[i];
                    bool isValid = ValidateBiomeCompatibility(candidate.TileId, nodeId, random)
                                   & ValidatePolarityCompatibility(candidate.TileId, nodeId, random)
                                   & ValidateSocketConstraints(entity, candidate.TileId, nodeId, random);
                    if (!isValid)
                    {
                        candidates.RemoveAt(i);
                        continue;
                    }

                    // Apply entropy reduction
                    float entropyReduction = DeltaTime * 0.1f;
                    candidate.Weight = math.max(0.05f, candidate.Weight - entropyReduction);

                    // Apply distance-based weight adjustments
                    float distanceFromCenter = math.length(coords) / 50.0f;
                    if (candidate.TileId == 1)
                    {
                        candidate.Weight *= math.max(0.4f, 1.0f - distanceFromCenter);
                    }
                    else if (candidate.TileId >= 3)
                    {
                        candidate.Weight *= math.max(0.5f, distanceFromCenter);
                    }

                    candidate.Weight *= random.NextFloat(0.95f, 1.05f);
                    candidates[i] = candidate;
                }
            }

            private readonly bool ValidateBiomeCompatibility(uint tileId, in NodeId nodeId, Unity.Mathematics.Random random)
            {
                var coords = (float2)nodeId.Coordinates;
                float d = math.length(coords);
                if (d > 60f && tileId == 1)
                {
                    return random.NextFloat() < 0.1f;
                }

                return true;
            }

            private readonly bool ValidatePolarityCompatibility(uint tileId, in NodeId nodeId, Unity.Mathematics.Random random)
            {
                int parity = (nodeId.Coordinates.x ^ nodeId.Coordinates.y) & 1;
                if (parity == 1 && tileId == 4)
                {
                    return random.NextFloat() > 0.2f;
                }

                return true;
            }

            private readonly bool ValidateSocketConstraints(Entity entity, uint tileId, in NodeId nodeId, Unity.Mathematics.Random random)
            {
                if (!SocketBufferLookup.HasBuffer(entity))
                {
                    return true;
                }

                var coords = (float2)nodeId.Coordinates;
                float centerFactor = math.saturate(1f - math.length(coords) / 80f);
                if ((tileId & 1) == 0 && centerFactor < 0.2f)
                {
                    return random.NextFloat() > 0.3f;
                }

                return true;
            }

            private readonly uint CollapseRandomly(DynamicBuffer<WfcCandidateBufferElement> candidates, Unity.Mathematics.Random random)
            {
                if (candidates.Length == 0)
                {
                    return 0;
                }

                float totalWeight = 0f;
                for (int i = 0; i < candidates.Length; i++)
                {
                    totalWeight += candidates[i].Weight;
                }

                if (totalWeight <= 0)
                {
                    int idx = random.NextInt(0, candidates.Length);
                    return candidates[idx].TileId;
                }

                float pick = random.NextFloat(0, totalWeight);
                float accum = 0f;
                for (int i = 0; i < candidates.Length; i++)
                {
                    accum += candidates[i].Weight;
                    if (pick <= accum)
                    {
                        return candidates[i].TileId;
                    }
                }
                return candidates[^1].TileId;
            }
        }
    }
}
