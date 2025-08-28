#if !UNITY_TRANSFORMS_LOCALTRANSFORM
using Unity.Mathematics;
using Unity.Entities;

namespace TinyWalnutGames.MetVD.Core.Compat
{
    /// <summary>
    /// Compatibility shim for Unity.Transforms.LocalTransform when the Transforms package / struct is unavailable.
    /// Provides minimal fields used by gameplay code. Do NOT use in new production code.
    /// </summary>
    internal struct LocalTransformCompat : IComponentData
    {
        public float3 Position;
        public quaternion Rotation;
        public float Scale;

        public static LocalTransformCompat FromPosition(float3 p) => new() { Position = p, Rotation = quaternion.identity, Scale = 1f };
    }

    internal static class LocalTransformCompatExtensions
    {
        public static float3 GetPosition(ref this LocalTransformCompat t) => t.Position;
        public static void SetPosition(ref this LocalTransformCompat t, float3 p) => t.Position = p;
    }
}
#endif
