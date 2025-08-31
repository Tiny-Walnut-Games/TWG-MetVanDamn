using Unity.Entities;

namespace TinyWalnutGames.MetVD.Core
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