#if UNITY_EDITOR
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using Unity.Entities;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	public class DistrictBaker : Baker<DistrictAuthoring>
		{
		public override void Bake (DistrictAuthoring authoring)
			{
			Entity entity = GetEntity(TransformUsageFlags.Dynamic);
			AddComponent(entity, new NodeId(authoring.nodeId, authoring.level, authoring.parentId, authoring.gridCoordinates));
			AddComponent(entity, new WfcState(authoring.initialWfcState));
			AddComponent(entity, new SectorRefinementData(authoring.targetLoopDensity));
			// Add the authoring MonoBehaviour as a component object so a sync system can push ECS changes back for gizmos/editor.
			AddComponentObject(entity, authoring);
			}
		}
	}
#endif
