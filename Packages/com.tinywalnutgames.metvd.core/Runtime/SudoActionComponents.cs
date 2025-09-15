using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Core
    {
    /// <summary>
    /// Design-time or runtime-provided hint that a special action should be dispatched
    /// near a location with optional constraints (e.g., elevation, biome type).
    /// </summary>
    public struct SudoActionHint : IComponentData
        {
        public FixedString64Bytes ActionKey; // Identifier other systems filter on
        public bool OneOff;                  // Dispatch once per session
        public float3 Center;                // World-space center
        public float Radius;                 // Area radius for randomized placement
        public BiomeElevation ElevationMask; // Optional elevation filter (Any means no restriction)
        public BiomeType TypeConstraint;     // Optional specific biome type constraint
        public byte HasTypeConstraint;       // 0 = no constraint, 1 = has constraint
        public uint Seed;                    // Optional per-hint seed to vary randomness deterministically
        }

    /// <summary>
    /// Runtime request emitted by the dispatcher for consumers to handle.
    /// Usage:
    /// - Systems should query for SudoActionRequest and filter by ActionKey.
    /// - After handling, destroy the request entity (or tag it) to prevent re-processing.
    /// - For one-off hints, the dispatcher tags the originating hint with SudoActionDispatched.
    /// </summary>
    public struct SudoActionRequest : IComponentData
        {
        public FixedString64Bytes ActionKey;
        public float3 ResolvedPosition;
        public BiomeElevation ElevationMask;
        public BiomeType TypeConstraint;
        public byte HasTypeConstraint;
        public uint Seed;            // Effective seed used for this request
        public Entity SourceHint;    // The originating hint entity
        }

    /// <summary>
    /// Tag added to a hint once a request has been dispatched to avoid re-emission.
    /// </summary>
    public struct SudoActionDispatched : IComponentData { }
    }
