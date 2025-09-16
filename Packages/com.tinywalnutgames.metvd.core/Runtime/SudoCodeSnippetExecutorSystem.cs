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
        private EntityQuery _snippetsQuery;

        public void OnCreate(ref SystemState state)
            {
            _snippetsQuery = state.GetEntityQuery(ComponentType.ReadWrite<SudoCodeSnippet>());
            state.RequireForUpdate<SudoCodeSnippet>();

			// Auto-register into Initialization group for manually created worlds used in tests (Editor only)
#if UNITY_EDITOR
			InitializationSystemGroup initGroup = state.World.GetOrCreateSystemManaged<InitializationSystemGroup>();
            initGroup.AddSystemToUpdateList(state.SystemHandle);
#endif
            }

        public void OnUpdate(ref SystemState state)
            {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

			// Use more compatible query approach for Unity Entities 1.3.14
			NativeArray<Entity> snippetEntities = _snippetsQuery.ToEntityArray(Allocator.Temp);
			NativeArray<SudoCodeSnippet> snippetComponents = _snippetsQuery.ToComponentDataArray<SudoCodeSnippet>(Allocator.Temp);

            for (int i = 0; i < snippetEntities.Length; i++)
                {
				Entity entity = snippetEntities[i];
				SudoCodeSnippet snippet = snippetComponents[i];
                
                if (snippet.RunOnce && snippet.HasExecuted) continue;

                Execute(ref state, ref ecb, ref snippet);

                if (snippet.RunOnce)
                    {
                    snippet.HasExecuted = true;
                    state.EntityManager.SetComponentData(entity, snippet);
                    }
                }

            snippetEntities.Dispose();
            snippetComponents.Dispose();
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
                                    HasTypeConstraint = 0,
                                    TypeConstraint = default,
                                    ElevationMask = default,
                                    Seed = 0,
                                    SourceHint = Entity.Null
                                    });
                                }
                            break;
                            }
                    default:
                        // Ignore unknown commands
                        break;
                    }
                }
            }
        }
    }
