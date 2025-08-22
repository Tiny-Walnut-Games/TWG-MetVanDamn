#if UNITY_EDITOR
using Unity.Entities;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    public class ConnectionBaker : Baker<ConnectionAuthoring>
    {
        public override void Bake(ConnectionAuthoring authoring)
        {
            if (authoring.from == null || authoring.to == null || authoring.from == authoring.to)
                return;

            uint fromIdVal = authoring.from.nodeId;
            uint toIdVal = authoring.to.nodeId;

            var fromEntity = GetEntity(authoring.from.gameObject, TransformUsageFlags.Dynamic);
            var toEntity = GetEntity(authoring.to.gameObject, TransformUsageFlags.Dynamic);

            var forward = new Connection(fromIdVal, toIdVal, authoring.type, authoring.requiredPolarity, authoring.traversalCost);
            var fromBuffer = AddBuffer<ConnectionBufferElement>(fromEntity); // returns existing if already present
            if (!ContainsConnection(fromBuffer, forward))
                fromBuffer.Add(forward);

            if (authoring.type == ConnectionType.Bidirectional)
            {
                var reverse = new Connection(toIdVal, fromIdVal, authoring.type, authoring.requiredPolarity, authoring.traversalCost);
                var toBuffer = AddBuffer<ConnectionBufferElement>(toEntity);
                if (!ContainsConnection(toBuffer, reverse))
                    toBuffer.Add(reverse);
            }
        }

        private static bool ContainsConnection(DynamicBuffer<ConnectionBufferElement> buffer, Connection c)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                var existing = buffer[i].Value;
                if (existing.FromNodeId == c.FromNodeId && existing.ToNodeId == c.ToNodeId && existing.Type == c.Type && existing.RequiredPolarity == c.RequiredPolarity && existing.TraversalCost == c.TraversalCost)
                    return true;
            }
            return false;
        }
    }
}
#endif
