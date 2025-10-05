#nullable enable
// Define METVD_SAMPLES_GO_CONSUMER to enable this sample GameObject-based consumer.
#if METVD_SAMPLES_GO_CONSUMER
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using TinyWalnutGames.MetVD.Core;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring
    {
    /// Minimal sample consumer system for SudoActionRequest.
    /// - Filters by ActionKey when `FilterKey` is non-empty.
    /// - Demonstrates reading radius/center and elevation/type constraints.
    /// - Destroys request entities after handling to avoid reprocessing.
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct SudoActionRequestConsumerSystem : ISystem
        {
        private PrefabRegistry _registry;

        public void OnCreate(ref SystemState state)
            {
            state.RequireForUpdate<SudoActionRequest>();
            // Try to find a PrefabRegistry in the scene or via Resources (optional)
            _registry = Object.FindFirstObjectByType<PrefabRegistry>();
            if (_registry == null)
                {
                // As a convenience, we attempt to load a default registry if present under Resources
                _registry = Resources.Load<PrefabRegistry>("PrefabRegistry");
                }
            }

        public void OnUpdate(ref SystemState state)
            {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (request, entity) in SystemAPI.Query<RefRO<SudoActionRequest>>().WithEntityAccess())
                {
                var req = request.ValueRO;
                HandleRequest(req, _registry);
                ecb.DestroyEntity(entity);
                }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            }

        private static void HandleRequest(in SudoActionRequest req, PrefabRegistry registry)
            {
            // Minimal production-ready actions without new content dependencies
            if (req.ActionKey.Equals(new FixedString64Bytes("spawn_boss")))
                {
                if (!TrySpawnFromRegistry(registry, req, out _))
                    SpawnPrimitiveMarker(req, PrimitiveType.Capsule, Color.red, scale: new Vector3(1f, 2f, 1f));
                return;
                }
            if (req.ActionKey.Equals(new FixedString64Bytes("spawn_loot")))
                {
                if (!TrySpawnFromRegistry(registry, req, out _))
                    SpawnPrimitiveMarker(req, PrimitiveType.Sphere, Color.yellow, scale: Vector3.one * 0.75f);
                return;
                }
            if (req.ActionKey.Equals(new FixedString64Bytes("spawn_marker")))
                {
                if (!TrySpawnFromRegistry(registry, req, out _))
                    SpawnPrimitiveMarker(req, PrimitiveType.Cube, Color.cyan, scale: Vector3.one * 0.5f);
                return;
                }

            // Default: log unhandled keys for visibility
            Debug.Log($"SudoActionRequest unhandled: '{req.ActionKey.ToString()}' at {req.ResolvedPosition} (elev={req.ElevationMask}, type?{req.HasTypeConstraint}:{req.TypeConstraint})");
            }

        private static bool TrySpawnFromRegistry(PrefabRegistry registry, in SudoActionRequest req, out GameObject spawned)
            {
            spawned = null;
            if (registry == null) return false;
            var key = req.ActionKey.ToString();
            if (!registry.TryGet(key, out var prefab) || prefab == null) return false;
            var pos = req.ResolvedPosition;
            spawned = Object.Instantiate(prefab, new Vector3(pos.x, pos.y, pos.z), Quaternion.identity);
            spawned.name = $"SudoAction_{key}";
            // Ensure marker tag for optional cleanup
            if (!spawned.TryGetComponent<SudoSpawnMarkerTag>(out _))
                spawned.AddComponent<SudoSpawnMarkerTag>();
            return true;
            }

        private static void SpawnPrimitiveMarker(in SudoActionRequest req, PrimitiveType type, Color color, Vector3? scale
 = null)
            {
            var go = GameObject.CreatePrimitive(type);
            go.name = $"SudoAction_{req.ActionKey.ToString()}";
            var pos = req.ResolvedPosition;
            go.transform.position = new Vector3(pos.x, pos.y == 0 ? 0.5f : pos.y, pos.z);
            if (scale.HasValue) go.transform.localScale = scale.Value;

            if (go.TryGetComponent<Renderer>(out var renderer))
                {
                var baseMat = renderer.sharedMaterial;
                var instMat = baseMat != null ? Object.Instantiate(baseMat) : new Material(Shader.Find("Standard"));
                instMat.color = color;
                renderer.sharedMaterial = instMat;
                }

            go.AddComponent<SudoSpawnMarkerTag>();
            }
        }

    /// Marker MonoBehaviour to find cleanup targets if needed
    public sealed class SudoSpawnMarkerTag : MonoBehaviour { }
    }
#endif
