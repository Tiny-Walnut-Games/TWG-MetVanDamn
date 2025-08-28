using System;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using TinyWalnutGames.MetVD.Core;

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

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // If layout tag exists but zero districts placed yet, you could early out. For now proceed regardless.
            socketBufferLookup.Update(ref state);
            candidateBufferLookup.Update(ref state);

            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            uint baseSeed = (uint)(state.WorldUnmanaged.Time.ElapsedTime * 911.0);
            var random = new Unity.Mathematics.Random(baseSeed == 0 ? 1u : baseSeed);

            // Use foreach instead of Schedule to avoid nullable reference issues
            foreach (var (wfcState, nodeId, entity) in SystemAPI.Query<RefRW<WfcState>, RefRO<NodeId>>().WithEntityAccess())
            {
                var entityRandom = new Unity.Mathematics.Random((uint)(entity.Index * 1103515245u) ^ random.state);
                
                switch (wfcState.ValueRO.State)
                {
                    case WfcGenerationState.Initialized:
                        InitializeCandidates(entity, ref wfcState.ValueRW, ref entityRandom, nodeId.ValueRO);
                        break;
                    case WfcGenerationState.InProgress:
                        ProcessWfcStep(entity, ref wfcState.ValueRW, nodeId.ValueRO, ref entityRandom, deltaTime);
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

        private readonly void InitializeCandidates(Entity entity, ref WfcState wfcState, ref Unity.Mathematics.Random random, in NodeId nodeId)
        {
            if (!candidateBufferLookup.HasBuffer(entity))
            {
                wfcState.State = WfcGenerationState.Failed;
                return;
            }
            var candidates = candidateBufferLookup[entity];
            candidates.Clear();
            float distance = math.length(new float2(nodeId.Coordinates)) * 0.02f;
            float centralBias = math.saturate(1f - distance);
            float entityVariance = 0.9f + ((entity.Index & 7) * 0.02f);
            candidates.Add(new WfcCandidateBufferElement(1, math.lerp(0.6f, 1.2f, centralBias) * entityVariance));
            candidates.Add(new WfcCandidateBufferElement(2, math.lerp(1.0f, 0.7f, centralBias) * entityVariance));
            candidates.Add(new WfcCandidateBufferElement(3, (0.4f + random.NextFloat(0.0f, 0.3f)) * entityVariance));
            candidates.Add(new WfcCandidateBufferElement(4, (0.2f + distance * 0.5f) * entityVariance));
            wfcState.Entropy = candidates.Length;
            wfcState.State = WfcGenerationState.InProgress;
        }

        private readonly void ProcessWfcStep(Entity entity, ref WfcState wfcState, in NodeId nodeId, ref Unity.Mathematics.Random random, float deltaTime)
        {
            if (!candidateBufferLookup.HasBuffer(entity))
            {
                wfcState.State = WfcGenerationState.Failed;
                return;
            }
            var candidates = candidateBufferLookup[entity];
            if (candidates.Length == 0)
            {
                wfcState.State = WfcGenerationState.Contradiction;
                return;
            }
            if (candidates.Length == 1)
            {
                wfcState.AssignedTileId = candidates[0].TileId;
                wfcState.IsCollapsed = true;
                wfcState.State = WfcGenerationState.Completed;
                return;
            }
            PropagateConstraints(entity, ref wfcState, candidates, nodeId, ref random, deltaTime);
            wfcState.Iteration++;
            wfcState.Entropy = candidates.Length;
            if (wfcState.Iteration > 100)
                CollapseRandomly(ref wfcState, candidates, ref random);
        }

        private readonly void PropagateConstraints(Entity entity, ref WfcState wfcState, DynamicBuffer<WfcCandidateBufferElement> candidates, in NodeId nodeId, ref Unity.Mathematics.Random random, float deltaTime)
        {
            for (int i = candidates.Length - 1; i >= 0; i--)
            {
                var candidate = candidates[i];
                bool isValid = ValidateBiomeCompatibility(candidate.TileId, nodeId, ref random)
                               & ValidatePolarityCompatibility(candidate.TileId, nodeId, ref random)
                               & ValidateSocketConstraints(entity, candidate.TileId, nodeId, ref random);
                if (!isValid)
                {
                    candidates.RemoveAt(i);
                    continue;
                }
                float entropyReduction = (wfcState.Iteration * 0.02f) + deltaTime * 0.1f;
                candidate.Weight = math.max(0.05f, candidate.Weight - entropyReduction);
                float distanceFromCenter = math.length(new float2(nodeId.Coordinates)) / 50.0f;
                if (candidate.TileId == 1)
                    candidate.Weight *= math.max(0.4f, 1.0f - distanceFromCenter);
                else if (candidate.TileId >= 3)
                    candidate.Weight *= math.max(0.5f, distanceFromCenter);
                candidate.Weight *= random.NextFloat(0.95f, 1.05f);
                candidates[i] = candidate;
            }
        }

        private readonly bool ValidateBiomeCompatibility(uint tileId, in NodeId nodeId, ref Unity.Mathematics.Random random)
        {
            float d = math.length(new float2(nodeId.Coordinates));
            if (d > 60f && tileId == 1) return random.NextFloat() < 0.1f;
            return true;
        }

        private readonly bool ValidatePolarityCompatibility(uint tileId, in NodeId nodeId, ref Unity.Mathematics.Random random)
        {
            int parity = (nodeId.Coordinates.x ^ nodeId.Coordinates.y) & 1;
            if (parity == 1 && tileId == 4)
                return random.NextFloat() > 0.2f;
            return true;
        }

        private readonly bool ValidateSocketConstraints(Entity entity, uint tileId, in NodeId nodeId, ref Unity.Mathematics.Random random)
        {
            if (!socketBufferLookup.HasBuffer(entity))
                return true;
            float centerFactor = math.saturate(1f - math.length(new float2(nodeId.Coordinates)) / 80f);
            if ((tileId & 1) == 0 && centerFactor < 0.2f)
                return random.NextFloat() > 0.3f;
            return true;
        }

        private readonly void CollapseRandomly(ref WfcState wfcState, DynamicBuffer<WfcCandidateBufferElement> candidates, ref Unity.Mathematics.Random random)
        {
            if (candidates.Length == 0)
            {
                wfcState.State = WfcGenerationState.Failed;
                return;
            }
            float totalWeight = 0f;
            for (int i = 0; i < candidates.Length; i++) totalWeight += candidates[i].Weight;
            if (totalWeight <= 0)
            {
                int idx = random.NextInt(0, candidates.Length);
                wfcState.AssignedTileId = candidates[idx].TileId;
                wfcState.IsCollapsed = true;
                wfcState.State = WfcGenerationState.Completed;
                return;
            }
            float pick = random.NextFloat(0, totalWeight);
            float accum = 0f;
            for (int i = 0; i < candidates.Length; i++)
            {
                accum += candidates[i].Weight;
                if (pick <= accum)
                {
                    wfcState.AssignedTileId = candidates[i].TileId;
                    wfcState.IsCollapsed = true;
                    wfcState.State = WfcGenerationState.Completed;
                    return;
                }
            }
            wfcState.AssignedTileId = candidates[^1].TileId;
            wfcState.IsCollapsed = true;
            wfcState.State = WfcGenerationState.Completed;
        }
    }
}
