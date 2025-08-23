using Unity.Entities;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Authoring
{
    /// <summary>
    /// Lightweight baked edge referencing two district entities.
    /// Used post-bake to build per-node ConnectionBufferElement lists.
    /// </summary>
    public struct ConnectionEdge : IComponentData
    {
        public Entity From;
        public Entity To;
        public ConnectionType Type;
        public Polarity RequiredPolarity;
        public float TraversalCost;
    }
}
