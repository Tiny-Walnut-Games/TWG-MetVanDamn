#if UNITY_EDITOR
using Unity.Entities;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    public class GateConditionBaker : Baker<GateConditionAuthoring>
    {
        public override void Bake(GateConditionAuthoring authoring)
        {
            var targetGo = authoring.target != null ? authoring.target.gameObject : authoring.gameObject;
            var targetEntity = GetEntity(targetGo, TransformUsageFlags.Dynamic);
            var buffer = AddBuffer<GateConditionBufferElement>(targetEntity);
            var gate = new GateCondition(
                authoring.requiredPolarity,
                authoring.requiredAbilities,
                authoring.softness,
                authoring.minimumSkillLevel,
                authoring.description);
            if (!ContainsGate(buffer, gate))
                buffer.Add(gate);
        }

        private static bool ContainsGate(DynamicBuffer<GateConditionBufferElement> buffer, GateCondition g)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                var existing = buffer[i].Value;
                if (existing.RequiredPolarity == g.RequiredPolarity && existing.RequiredAbilities == g.RequiredAbilities && existing.Softness == g.Softness && existing.MinimumSkillLevel == g.MinimumSkillLevel && existing.Description.Equals(g.Description))
                    return true;
            }
            return false;
        }
    }
}
#endif
