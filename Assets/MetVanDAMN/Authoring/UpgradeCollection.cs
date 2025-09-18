using UnityEngine;
using System.Collections.Generic;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring
    {
    /// <summary>
    /// Collection of upgrades grouped by category for procedural choice generation.
    /// Used by the LevelUpChoiceSystem to roll category-specific upgrade options.
    /// </summary>
    [CreateAssetMenu(fileName = "New Upgrade Collection", menuName = "MetVanDAMN/Upgrade Collection")]
    public class UpgradeCollection : ScriptableObject
        {
        [Header("Collection Info")]
        [SerializeField] private UpgradeCategory category = UpgradeCategory.Movement;
        [SerializeField] private string collectionName = "Movement Upgrades";
        [SerializeField][TextArea(2, 3)] private string description = "Collection of movement-related upgrades";

        [Header("Upgrades")]
        [SerializeField] private UpgradeDefinition[] upgrades = new UpgradeDefinition[0];

        [Header("Category Weighting")]
        [SerializeField][Range(0f, 2f)] private float baseWeight = 1f;
        [SerializeField] private BiomeWeightEntry[] biomeWeights = new BiomeWeightEntry[0];

        // Public properties
        public UpgradeCategory Category => category;
        public string CollectionName => collectionName;
        public string Description => description;
        public UpgradeDefinition[] Upgrades => upgrades;
        public float BaseWeight => baseWeight;

        // =====================================================================
        // Nullability Annihilation Addendum (Collection Level)
        // ---------------------------------------------------------------------
        // Upgrade selection historically returned null when no upgrade matched
        // criteria. Downstream callers then had to branch on null, fanning out
        // fragile optional logic. We replace that with a deterministic
        // SENTINEL upgrade instance that communicates "no-op" while preserving
        // a non-null reference invariant.
        //
        // Contract:
        //  * SelectRandomUpgrade NEVER returns null.
        //  * Sentinel instance can be detected via IsSentinel(upgrade).
        //  * Sentinel has inert/default values; applying it must be a no-op.
        // =====================================================================
        private static UpgradeDefinition _sentinelUpgrade = null!; // lazily created; non-null after first access

        private static UpgradeDefinition Sentinel
            {
            get
                {
                if (_sentinelUpgrade == null)
                    {
                    _sentinelUpgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
                    _sentinelUpgrade.name = "__SentinelUpgrade__"; // editor clarity
                    // Attempt shallow initialization via reflection (best effort; safe if layout changes)
                    TrySetStringField(_sentinelUpgrade, "id", "SENTINEL_UPGRADE_NONE");
                    TrySetStringField(_sentinelUpgrade, "upgradeId", "SENTINEL_UPGRADE_NONE");
                    }
                return _sentinelUpgrade;
                }
            }

        public static bool IsSentinel(UpgradeDefinition upgrade)
            => upgrade == Sentinel || (upgrade != null && upgrade.name == "__SentinelUpgrade__");

        private static void TrySetStringField(UpgradeDefinition target, string fieldName, string value)
            {
            var f = typeof(UpgradeDefinition).GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(string))
                {
                f.SetValue(target, value);
                }
            }

        /// <summary>
        /// Biome-specific weight multiplier entry
        /// </summary>
        [System.Serializable]
        public class BiomeWeightEntry
            {
            public Polarity biomeType;
            [Range(0f, 3f)] public float weightMultiplier = 1f;
            }

        /// <summary>
        /// Get all available upgrades for the given player state
        /// </summary>
        public List<UpgradeDefinition> GetAvailableUpgrades(int playerLevel, Ability currentAbilities, string[] currentUpgradeIds)
            {
            var available = new List<UpgradeDefinition>();

            foreach (var upgrade in upgrades)
                {
                if (upgrade != null && upgrade.IsAvailableFor(playerLevel, currentAbilities, currentUpgradeIds))
                    {
                    available.Add(upgrade);
                    }
                }

            return available;
            }

        /// <summary>
        /// Calculate the total weight of this category for the given context
        /// </summary>
        public float CalculateCategoryWeight(Polarity biomeContext)
            {
            float weight = baseWeight;

            // Apply biome-specific multipliers
            foreach (var biomeWeight in biomeWeights)
                {
                if ((biomeContext & biomeWeight.biomeType) != Polarity.None)
                    {
                    weight *= biomeWeight.weightMultiplier;
                    }
                }

            return weight;
            }

        /// <summary>
        /// Randomly select an upgrade from available options using weighted selection
        /// </summary>
        public UpgradeDefinition SelectRandomUpgrade(int playerLevel, Ability currentAbilities, string[] currentUpgradeIds,
                                                    Polarity biomeContext, uint seed)
            {
            var available = GetAvailableUpgrades(playerLevel, currentAbilities, currentUpgradeIds);
            if (available.Count == 0)
                {
                return Sentinel; // deterministic non-null no-op
                }

            var random = new Unity.Mathematics.Random(seed);

            // Calculate weights for each available upgrade
            var weights = new float[available.Count];
            var totalWeight = 0f;

            for (int i = 0; i < available.Count; i++)
                {
                weights[i] = available[i].CalculateWeight(biomeContext, seed);
                totalWeight += weights[i];
                }

            if (totalWeight <= 0f)
                {
                return available[random.NextInt(available.Count)]; // Fallback to uniform selection
                }

            // Weighted random selection
            var randomValue = random.NextFloat(0f, totalWeight);
            var currentWeight = 0f;

            for (int i = 0; i < available.Count; i++)
                {
                currentWeight += weights[i];
                if (randomValue <= currentWeight)
                    {
                    return available[i];
                    }
                }

            // Fallback (should not reach here)
            return available[^1];
            }

        /// <summary>
        /// Get upgrade count and basic stats for this collection
        /// </summary>
        public (int total, int unique, int available) GetStats(int playerLevel, Ability currentAbilities, string[] currentUpgradeIds)
            {
            int total = upgrades.Length;
            int unique = 0;
            int available = 0;

            foreach (var upgrade in upgrades)
                {
                if (upgrade == null) continue;

                if (upgrade.IsUnique)
                    unique++;

                if (upgrade.IsAvailableFor(playerLevel, currentAbilities, currentUpgradeIds))
                    available++;
                }

            return (total, unique, available);
            }

        private void OnValidate()
            {
            // Ensure all upgrades match the collection category
            for (int i = 0; i < upgrades.Length; i++)
                {
                if (upgrades[i] != null && upgrades[i].Category != category)
                    {
                    Debug.LogWarning($"Upgrade '{upgrades[i].name}' in collection '{name}' has mismatched category. " +
                                   $"Expected: {category}, Found: {upgrades[i].Category}");
                    }
                }

            // Auto-set collection name if empty
            if (string.IsNullOrEmpty(collectionName))
                {
                collectionName = $"{category} Upgrades";
                }
            }
        }
    }
