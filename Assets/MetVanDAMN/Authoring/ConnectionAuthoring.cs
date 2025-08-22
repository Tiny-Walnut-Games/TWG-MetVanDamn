using UnityEngine;
using Unity.Entities;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Authoring
{
    public class ConnectionAuthoring : MonoBehaviour
    {
        [Header("Endpoints (DistrictAuthoring components)")]
        public DistrictAuthoring from;
        public DistrictAuthoring to;

        [Header("Connection Properties")]
        public ConnectionType type = ConnectionType.Bidirectional;
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
