#if !UNITY_TRANSFORMS_LOCALTRANSFORM
using Unity.Mathematics;
using Unity.Entities;

namespace TinyWalnutGames.MetVD.Core.Compat
{
    /// <summary>
    /// Compatibility shim for Unity.Transforms.LocalTransform when the Transforms package / struct is unavailable.
    /// Provides minimal fields used by gameplay code. Do NOT use in new production code.
    /// </summary>
    public struct LocalTransformCompat : IComponentData // was internal, made public for cross-assembly aliasing
    {
        public float3 Position;
        public quaternion Rotation;
        public float Scale;

        public static LocalTransformCompat FromPosition(float3 p) => new LocalTransformCompat { Position = p, Rotation = quaternion.identity, Scale = 1f };
    }

    public static class LocalTransformCompatExtensions // visibility widened to public for external usage if needed
    {
        public static float3 GetPosition(ref this LocalTransformCompat t) => t.Position;
        public static void SetPosition(ref this LocalTransformCompat t, float3 p) => t.Position = p;
    }
}
#endif
