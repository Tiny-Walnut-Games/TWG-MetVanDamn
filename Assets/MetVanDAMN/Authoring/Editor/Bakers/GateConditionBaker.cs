#if UNITY_EDITOR
using TinyWalnutGames.MetVD.Core;
using Unity.Entities;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	public class GateConditionBaker : Baker<GateConditionAuthoring>
		{
		public override void Bake (GateConditionAuthoring authoring)
			{
			UnityEngine.GameObject targetGo = authoring.target != null ? authoring.target.gameObject : authoring.gameObject;
			Entity targetEntity = GetEntity(targetGo, TransformUsageFlags.Dynamic);
			DynamicBuffer<GateConditionBufferElement> buffer = AddBuffer<GateConditionBufferElement>(targetEntity);
			var gate = new GateCondition(
				authoring.requiredPolarity,
				authoring.requiredAbilities,
				authoring.softness,
				authoring.minimumSkillLevel,
				authoring.description);
			if (!ContainsGate(buffer, gate))
				{
				buffer.Add(gate);
				}
			}

		private static bool ContainsGate (DynamicBuffer<GateConditionBufferElement> buffer, GateCondition g)
			{
			for (int i = 0; i < buffer.Length; i++)
				{
				GateCondition existing = buffer [ i ].Value;
				if (existing.RequiredPolarity == g.RequiredPolarity && existing.RequiredAbilities == g.RequiredAbilities && existing.Softness == g.Softness && existing.MinimumSkillLevel == g.MinimumSkillLevel && existing.Description.Equals(g.Description))
					{
					return true;
					}
				}
			return false;
			}
		}
	}
#endif
