using UnityEngine;
using Unity.Entities;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Authoring
{
    public class GateConditionAuthoring : MonoBehaviour
    {
        [Header("Node References")]
        public uint sourceNode;
        public uint targetNode;
        
        [Header("Target District (optional - self if null)")] 
        public DistrictAuthoring target;
        public Polarity requiredPolarity = Polarity.None;
        public Ability requiredAbilities = Ability.None;
        public GateSoftness softness = GateSoftness.Hard;
        [Range(0f,1f)] public float minimumSkillLevel = 0f;
        [Tooltip("Description (<=64 chars)")] public string description = "Gate";
    }
}
