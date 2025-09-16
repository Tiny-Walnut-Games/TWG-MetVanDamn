using Unity.Entities;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring
    {
    public sealed class BossAuthoring : MonoBehaviour
        {
        public class Baker : Baker<BossAuthoring>
            {
            public override void Bake(BossAuthoring authoring)
                {
				Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent<BossTag>(entity);
                }
            }
        }
    }
