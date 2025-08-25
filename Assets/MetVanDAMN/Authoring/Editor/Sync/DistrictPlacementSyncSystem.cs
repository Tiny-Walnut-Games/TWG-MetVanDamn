#if UNITY_EDITOR
using Unity.Entities;
using UnityEditor;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    /// <summary>
    /// Editor-only system that mirrors ECS NodeId.Coordinates for level-0 districts
    /// back onto their baked DistrictAuthoring MonoBehaviours for gizmos.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
    public partial class DistrictPlacementSyncSystem : SystemBase
    {
        private EntityQuery _districtQuery;
        protected override void OnCreate()
        {
            _districtQuery = GetEntityQuery(ComponentType.ReadOnly<NodeId>(), ComponentType.ReadOnly<DistrictAuthoring>());
            RequireForUpdate(_districtQuery);
        }

        protected override void OnUpdate()
        {
            var nodeIdHandle = GetComponentTypeHandle<NodeId>(true); // not used directly but keeps pattern
            var nodeIds = _districtQuery.ToComponentDataArray<NodeId>(Unity.Collections.Allocator.Temp);
            var entities = _districtQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var em = EntityManager;
            try
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var node = nodeIds[i];
                    if (node.Level != 0) continue;
                    if (!em.HasComponent<DistrictAuthoring>(entities[i])) continue; // safety
                    var authoring = em.GetComponentObject<DistrictAuthoring>(entities[i]);
                    if (authoring.gridCoordinates.x != node.Coordinates.x || authoring.gridCoordinates.y != node.Coordinates.y)
                    {
                        authoring.gridCoordinates = node.Coordinates;
                        EditorUtility.SetDirty(authoring);
                    }
                }
            }
            finally
            {
                if (nodeIds.IsCreated) nodeIds.Dispose();
                if (entities.IsCreated) entities.Dispose();
            }
        }
    }
}
#endif
