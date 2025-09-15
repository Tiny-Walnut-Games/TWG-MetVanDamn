#if UNITY_EDITOR
using TinyWalnutGames.MetVD.Graph;
using Unity.Entities;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	/// <summary>
	/// Baker for WfcTilePrototypeAuthoring components
	/// Converts authoring data to runtime ECS components
	/// </summary>
	public class WfcTilePrototypeBaker : Baker<WfcTilePrototypeAuthoring>
		{
		public override void Bake(WfcTilePrototypeAuthoring authoring)
			{
			Entity entity = GetEntity(TransformUsageFlags.None);

			// Add the tile prototype component
			AddComponent(entity, new WfcTilePrototype(
				authoring.tileId,
				authoring.weight,
				authoring.biomeType,
				authoring.primaryPolarity,
				authoring.minConnections,
				authoring.maxConnections
			));

			// Add socket buffer if sockets are defined
			if (authoring.sockets != null && authoring.sockets.Length > 0)
				{
				DynamicBuffer<WfcSocketBufferElement> socketBuffer = AddBuffer<WfcSocketBufferElement>(entity);

				foreach (WfcSocketConfig socketConfig in authoring.sockets)
					{
					if (socketConfig.isOpen) // Only add open sockets
						{
						socketBuffer.Add(new WfcSocketBufferElement
							{
							Value = socketConfig.ToWfcSocket()
							});
						}
					}
				}

			// Add the authoring component as a component object for editor sync
			AddComponentObject(entity, authoring);
			}
		}
	}
#endif