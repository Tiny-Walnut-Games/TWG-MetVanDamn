#if UNITY_EDITOR
using TinyWalnutGames.MetVD.Core;
using Unity.Entities;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	/// <summary>
	/// Baker now only records endpoint entity references; it no longer mutates the district entities directly.
	/// Runtime system will materialize ConnectionBufferElement buffers once after baking.
	/// </summary>
	public class ConnectionBaker : Baker<ConnectionAuthoring>
		{
		public override void Bake (ConnectionAuthoring authoring)
			{
			if (authoring.from == null || authoring.to == null || authoring.from == authoring.to)
				{
				return;
				}

			Entity connectionEntity = this.GetEntity(TransformUsageFlags.None);
			Entity fromEntity = this.GetEntity(authoring.from.gameObject, TransformUsageFlags.None);
			Entity toEntity = this.GetEntity(authoring.to.gameObject, TransformUsageFlags.None);

			this.AddComponent(connectionEntity, new ConnectionEdge
				{
				From = fromEntity,
				To = toEntity,
				Type = authoring.type,
				RequiredPolarity = authoring.requiredPolarity,
				TraversalCost = authoring.traversalCost
				});
			}
		}
	}
#endif
