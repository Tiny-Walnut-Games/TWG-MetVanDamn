using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Core
    {
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SudoCodeSnippetExecutorSystem : ISystem
        {
        public void OnCreate(ref SystemState state)
            {
            state.RequireForUpdate<SudoCodeSnippet>();

            // Auto-register into Initialization group for manually created worlds used in tests (Editor only)
#if UNITY_EDITOR
            var initGroup = state.World.GetOrCreateSystemManaged<InitializationSystemGroup>();
            initGroup.AddSystemToUpdateList(state.SystemHandle);
#endif
            }

        public void OnUpdate(ref SystemState state)
            {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach ((RefRW<SudoCodeSnippet> snippetRW, Entity e) in SystemAPI.Query<RefRW<SudoCodeSnippet>>().WithEntityAccess())
                {
                ref SudoCodeSnippet snippet = ref snippetRW.ValueRW;
                if (snippet.RunOnce && snippet.HasExecuted) continue;

                Execute(ref state, ref ecb, ref snippet);

                if (snippet.RunOnce)
                    {
                    snippet.HasExecuted = true;
                    }
                }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            }

        private static void Execute(ref SystemState state, ref EntityCommandBuffer ecb, ref SudoCodeSnippet snippet)
            {
            string code = snippet.Code.ToString();
            if (string.IsNullOrWhiteSpace(code)) return;

            string[] lines = code.Split('\n');
            foreach (string raw in lines)
                {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                string[] tokens = line.Split(' ', '\t');
                if (tokens.Length == 0) continue;

                string cmd = tokens[0].ToLowerInvariant();
                switch (cmd)
                    {
                    case "log":
                            {
                            string msg = line.Length > 3 ? line[3..].Trim() : string.Empty;
                            if (!string.IsNullOrEmpty(msg)) Debug.Log($"[SudoCode] {msg}");
                            break;
                            }
                    case "spawn":
                            {
                            if (tokens.Length >= 2)
                                {
                                string key = tokens[1];
                                float3 pos = float3.zero;
                                if (tokens.Length >= 5 &&
                                    float.TryParse(tokens[2], out float x) &&
                                    float.TryParse(tokens[3], out float y) &&
                                    float.TryParse(tokens[4], out float z))
                                    {
                                    pos = new float3(x, y, z);
                                    }

                                Entity req = ecb.CreateEntity();
                                ecb.AddComponent(req, new SudoActionRequest
                                    {
                                    ActionKey = new FixedString64Bytes(key),
                                    ResolvedPosition = pos,
                                    ElevationMask = BiomeElevation.Surface,
                                    HasTypeConstraint = 0,
                                    TypeConstraint = default
                                    });
                                }
                            break;
                            }
                    default:
                        Debug.LogWarning($"[SudoCode] Unknown command: {cmd}");
                        break;
                    }
                }
            }
        }
    }
