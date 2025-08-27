using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;

namespace TinyWalnutGames.MetVD.Authoring
{
    public class DistrictAuthoring : MonoBehaviour
    {
        [Header("Identification")] 
        public uint nodeId = 1;
        [Tooltip("Hierarchy level (0=district)")] public byte level = 0;
        public uint parentId = 0;
        public int2 gridCoordinates;

        [Header("District Configuration")]
        public BiomeType biomeType = BiomeType.SolarPlains;
        public DistrictType districtType = DistrictType.Standard;
        public float2 size = new(100f, 100f);
        public int targetSectorCount = 4;

        [Header("Generation Settings")] 
        [Range(0.1f,1f)] public float targetLoopDensity = 0.3f;
        [Tooltip("Initial WFC state")] public WfcGenerationState initialWfcState = WfcGenerationState.Initialized;

        private void OnValidate()
        {
            if (nodeId == 0) nodeId = 1; // avoid zero ID edge case
        }
    }
}
