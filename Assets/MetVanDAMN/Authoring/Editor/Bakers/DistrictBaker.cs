#if UNITY_EDITOR
using Unity.Entities;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    public class DistrictBaker : Baker<DistrictAuthoring>
    {
        public override void Bake(DistrictAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new NodeId(authoring.nodeId, authoring.level, authoring.parentId, authoring.gridCoordinates));
            AddComponent(entity, new WfcState(authoring.initialWfcState));
            AddComponent(entity, new SectorRefinementData(authoring.targetLoopDensity));
        }
    }
}
#endif
