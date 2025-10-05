#nullable enable
using TinyWalnutGames.MetVD.Core;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring
	{
	public class ConnectionAuthoring : MonoBehaviour
		{
		[Header("Connection Identity")] public uint connectionId;

		[Header("Endpoints (DistrictAuthoring components)")]
		public DistrictAuthoring from;

		public DistrictAuthoring to;

		[Header("Node References")] public uint sourceNode;

		public uint targetNode;

		[Header("Connection Properties")] public ConnectionType type = ConnectionType.Bidirectional;

		public Polarity requiredPolarity = Polarity.None;
		[Min(0.1f)] public float traversalCost = 1f;

		private void OnValidate()
			{
			if (from == to && from != null)
				{
				Debug.LogWarning("ConnectionAuthoring: 'from' and 'to' reference the same district.", this);
				}
			}
		}
	}
