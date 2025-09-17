using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring
{
    /// <summary>
    /// Upgrade categories for procedural choice generation
    /// </summary>
    public enum UpgradeCategory
    {
        Movement,    // Jump, dash, wall jump, etc.
        Offense,     // Damage, fire rate, weapon types
        Defense,     // Health, armor, resistances
        Utility,     // Map, scan, interaction range
        Special      // Unique abilities, combo moves
    }

    /// <summary>
    /// Modifier types that can be applied to player stats
    /// </summary>
    public enum ModifierType
    {
        Additive,      // +10 health
        Multiplicative, // x1.5 damage
        NewAbility,    // Grants new ability
        Enhanced       // Enhances existing ability
    }

    /// <summary>
    /// Individual upgrade configuration for the procedural leveling system.
    /// Defines a single upgrade choice with requirements, effects, and metadata.
    /// </summary>
    [CreateAssetMenu(fileName = "New Upgrade", menuName = "MetVanDAMN/Upgrade Definition")]
    public class UpgradeDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] private string upgradeName = "New Upgrade";
        [SerializeField] [TextArea(2, 4)] private string description = "Description of the upgrade";
        [SerializeField] private Sprite icon;
        [SerializeField] private UpgradeCategory category = UpgradeCategory.Movement;

        [Header("Requirements")]
        [SerializeField] private int minimumLevel = 1;
        [SerializeField] private Ability requiredAbilities = Ability.None;
        [SerializeField] private Ability conflictingAbilities = Ability.None;
        [SerializeField] private string[] requiredUpgradeIds = new string[0];
        [SerializeField] private string[] conflictingUpgradeIds = new string[0];

        [Header("Effects")]
        [SerializeField] private Ability grantsAbilities = Ability.None;
        [SerializeField] private ModifierType modifierType = ModifierType.Additive;
        [SerializeField] private string targetStat = "";
        [SerializeField] private float value = 1f;
        [SerializeField] private string[] customEffectIds = new string[0];

        [Header("Rarity & Weighting")]
        [SerializeField] [Range(0f, 1f)] private float baseWeight = 1f;
        [SerializeField] [Range(0f, 2f)] private float biomeWeightMultiplier = 1f;
        [SerializeField] private bool isUnique = false;
        [SerializeField] private bool allowDuplicates = false;

        [Header("Preview Text")]
        [SerializeField] [TextArea(1, 3)] private string previewText = "";

        // Public properties
        public string UpgradeName => upgradeName;
        public string Description => description;
        public Sprite Icon => icon;
        public UpgradeCategory Category => category;
        public int MinimumLevel => minimumLevel;
        public Ability RequiredAbilities => requiredAbilities;
        public Ability ConflictingAbilities => conflictingAbilities;
        public string[] RequiredUpgradeIds => requiredUpgradeIds;
        public string[] ConflictingUpgradeIds => conflictingUpgradeIds;
        public Ability GrantsAbilities => grantsAbilities;
        public ModifierType ModifierType => modifierType;
        public string TargetStat => targetStat;
        public float Value => value;
        public string[] CustomEffectIds => customEffectIds;
        public float BaseWeight => baseWeight;
        public float BiomeWeightMultiplier => biomeWeightMultiplier;
        public bool IsUnique => isUnique;
        public bool AllowDuplicates => allowDuplicates;
        public string PreviewText => previewText;

        /// <summary>
        /// Unique identifier for this upgrade (uses asset name)
        /// </summary>
        public string Id => name;

        /// <summary>
        /// Check if this upgrade is available for the given player state
        /// </summary>
        public bool IsAvailableFor(int playerLevel, Ability currentAbilities, string[] currentUpgradeIds)
        {
            // Level requirement
            if (playerLevel < minimumLevel)
                return false;

            // Ability requirements
            if ((currentAbilities & requiredAbilities) != requiredAbilities)
                return false;

            // Conflicting abilities
            if ((currentAbilities & conflictingAbilities) != Ability.None)
                return false;

            // Required upgrade dependencies
            foreach (var requiredId in requiredUpgradeIds)
            {
                bool hasRequired = false;
                foreach (var currentId in currentUpgradeIds)
                {
                    if (currentId == requiredId)
                    {
                        hasRequired = true;
                        break;
                    }
                }
                if (!hasRequired)
                    return false;
            }

            // Conflicting upgrades
            foreach (var conflictingId in conflictingUpgradeIds)
            {
                foreach (var currentId in currentUpgradeIds)
                {
                    if (currentId == conflictingId)
                        return false;
                }
            }

            // Uniqueness check
            if (isUnique && !allowDuplicates)
            {
                foreach (var currentId in currentUpgradeIds)
                {
                    if (currentId == Id)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Calculate the weight of this upgrade for the given context
        /// </summary>
        public float CalculateWeight(Polarity biomeContext, uint worldSeed)
        {
            var random = new Unity.Mathematics.Random(worldSeed ^ (uint)Id.GetHashCode());
            
            float weight = baseWeight;
            
            // Biome influence
            weight *= biomeWeightMultiplier;
            
            // Add some randomness based on world seed
            weight *= random.NextFloat(0.8f, 1.2f);
            
            return weight;
        }

        /// <summary>
        /// Generate a formatted preview of this upgrade's effects
        /// </summary>
        public string GeneratePreview()
        {
            if (!string.IsNullOrEmpty(previewText))
                return previewText;

            var preview = "";
            
            if (grantsAbilities != Ability.None)
            {
                preview += $"Grants: {grantsAbilities}\n";
            }
            
            if (!string.IsNullOrEmpty(targetStat))
            {
                string modifier = modifierType switch
                {
                    ModifierType.Additive => $"+{value}",
                    ModifierType.Multiplicative => $"x{value:F1}",
                    ModifierType.NewAbility => "New ability",
                    ModifierType.Enhanced => "Enhanced",
                    _ => $"{value}"
                };
                preview += $"{targetStat}: {modifier}\n";
            }

            return preview.TrimEnd('\n');
        }

        private void OnValidate()
        {
            // Ensure name matches the upgrade name for consistency
            if (!string.IsNullOrEmpty(upgradeName) && upgradeName != name)
            {
                // Note: Can't rename asset from OnValidate, but we can suggest it
            }
        }
    }
}