using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring
{
    /// <summary>
    /// Core system for generating procedural level-up choices.
    /// Integrates with world seed, biome context, and player state for curated upgrade options.
    /// </summary>
    public class LevelUpChoiceSystem : MonoBehaviour
    {
        [Header("Choice Generation")]
        [SerializeField] [Range(2, 6)] private int choicesPerLevelUp = 3;
        [SerializeField] private int minDistinctCategories = 2;
        [SerializeField] private bool preventDuplicateChoices = true;
        [SerializeField] private int maxRetries = 10;

        [Header("Upgrade Collections")]
        [SerializeField] private UpgradeCollection[] upgradeCollections = new UpgradeCollection[0];

        [Header("Weighting Configuration")]
        [SerializeField] private CategoryWeightEntry[] categoryWeights = new CategoryWeightEntry[0];
        [SerializeField] [Range(0f, 1f)] private float seedInfluence = 0.3f;
        [SerializeField] [Range(0f, 1f)] private float biomeInfluence = 0.4f;
        [SerializeField] [Range(0f, 1f)] private float playerStateInfluence = 0.3f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = true;

        // Events
        public System.Action<UpgradeDefinition[]> OnChoicesGenerated;
        public System.Action<UpgradeDefinition> OnUpgradeChosen;
        public System.Action<string> OnGenerationError;

        // Cached components
        private PlayerLevelProgression playerProgression;
        private Dictionary<UpgradeCategory, UpgradeCollection> collectionMap;

        [System.Serializable]
        public class CategoryWeightEntry
        {
            public UpgradeCategory category;
            [Range(0f, 3f)] public float baseWeight = 1f;
            [Range(0f, 2f)] public float seedMultiplier = 1f;
            [Range(0f, 2f)] public float biomeMultiplier = 1f;
        }

        private void Awake()
        {
            playerProgression = GetComponent<PlayerLevelProgression>();
            if (playerProgression == null)
            {
                Debug.LogError("LevelUpChoiceSystem requires PlayerLevelProgression component");
                return;
            }

            BuildCollectionMap();
            
            // Subscribe to level up events
            playerProgression.OnLevelUp += HandleLevelUp;
        }

        private void OnDestroy()
        {
            if (playerProgression != null)
            {
                playerProgression.OnLevelUp -= HandleLevelUp;
            }
        }

        private void BuildCollectionMap()
        {
            collectionMap = new Dictionary<UpgradeCategory, UpgradeCollection>();
            
            foreach (var collection in upgradeCollections)
            {
                if (collection != null)
                {
                    collectionMap[collection.Category] = collection;
                }
            }

            if (enableDebugLogging)
            {
                Debug.Log($"📚 Built upgrade collection map with {collectionMap.Count} categories");
                foreach (var kvp in collectionMap)
                {
                    var stats = kvp.Value.GetStats(1, Ability.None, new string[0]);
                    Debug.Log($"  • {kvp.Key}: {stats.total} total, {stats.unique} unique");
                }
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"🎉 Level up detected: {newLevel}");
            }

            GenerateUpgradeChoices();
        }

        /// <summary>
        /// Generate procedural upgrade choices based on current player state and world context
        /// </summary>
        public void GenerateUpgradeChoices()
        {
            if (playerProgression == null)
            {
                OnGenerationError?.Invoke("Player progression component not found");
                return;
            }

            // Get current player state
            int currentLevel = playerProgression.CurrentLevel;
            Ability currentAbilities = playerProgression.CurrentAbilities;
            string[] currentUpgrades = playerProgression.CurrentUpgradeIds;

            // Get world context
            uint worldSeed = GetWorldSeed();
            Polarity biomeContext = GetCurrentBiomeContext();

            // Generate choices
            var choices = GenerateChoices(currentLevel, currentAbilities, currentUpgrades, worldSeed, biomeContext);

            if (choices.Length == 0)
            {
                OnGenerationError?.Invoke("No valid upgrade choices available");
                return;
            }

            if (enableDebugLogging)
            {
                Debug.Log($"🎯 Generated {choices.Length} upgrade choices:");
                foreach (var choice in choices)
                {
                    Debug.Log($"  • {choice.UpgradeName} ({choice.Category}): {choice.Description}");
                }
            }

            OnChoicesGenerated?.Invoke(choices);
        }

        private UpgradeDefinition[] GenerateChoices(int playerLevel, Ability currentAbilities, string[] currentUpgrades, 
                                                   uint worldSeed, Polarity biomeContext)
        {
            var choices = new List<UpgradeDefinition>();
            var usedCategories = new HashSet<UpgradeCategory>();
            var usedUpgradeIds = new HashSet<string>();
            
            // Create seed-based random generator
            var random = new Unity.Mathematics.Random(worldSeed ^ (uint)playerLevel);

            int attempts = 0;
            while (choices.Count < choicesPerLevelUp && attempts < maxRetries)
            {
                attempts++;

                // Select category first (ensures minimum distinct categories)
                var targetCategory = SelectCategory(usedCategories, currentAbilities, biomeContext, random.NextUInt());

                if (!collectionMap.TryGetValue(targetCategory, out var collection))
                {
                    if (enableDebugLogging && attempts == 1)
                    {
                        Debug.LogWarning($"No collection found for category: {targetCategory}");
                    }
                    continue;
                }

                // Get available upgrades from this category
                var availableUpgrades = collection.GetAvailableUpgrades(playerLevel, currentAbilities, currentUpgrades);
                
                // Remove already selected upgrades if preventing duplicates
                if (preventDuplicateChoices)
                {
                    availableUpgrades = availableUpgrades.Where(u => !usedUpgradeIds.Contains(u.Id)).ToList();
                }

                if (availableUpgrades.Count == 0)
                    continue;

                // Select random upgrade from available options
                var selectedUpgrade = collection.SelectRandomUpgrade(playerLevel, currentAbilities, currentUpgrades, 
                                                                   biomeContext, random.NextUInt());

                if (selectedUpgrade != null && (!preventDuplicateChoices || !usedUpgradeIds.Contains(selectedUpgrade.Id)))
                {
                    choices.Add(selectedUpgrade);
                    usedCategories.Add(targetCategory);
                    usedUpgradeIds.Add(selectedUpgrade.Id);
                }
            }

            return choices.ToArray();
        }

        private UpgradeCategory SelectCategory(HashSet<UpgradeCategory> usedCategories, Ability currentAbilities, 
                                            Polarity biomeContext, uint seed)
        {
            var random = new Unity.Mathematics.Random(seed);
            var availableCategories = new List<UpgradeCategory>();
            var weights = new List<float>();

            foreach (var category in System.Enum.GetValues(typeof(UpgradeCategory)).Cast<UpgradeCategory>())
            {
                // Ensure minimum distinct categories
                if (usedCategories.Count < minDistinctCategories && usedCategories.Contains(category))
                    continue;

                if (collectionMap.ContainsKey(category))
                {
                    availableCategories.Add(category);
                    weights.Add(CalculateCategoryWeight(category, currentAbilities, biomeContext, seed));
                }
            }

            if (availableCategories.Count == 0)
            {
                // Fallback: allow any category
                foreach (var category in collectionMap.Keys)
                {
                    availableCategories.Add(category);
                    weights.Add(CalculateCategoryWeight(category, currentAbilities, biomeContext, seed));
                }
            }

            if (availableCategories.Count == 0)
                return UpgradeCategory.Movement; // Ultimate fallback

            // Weighted selection
            return SelectWeightedCategory(availableCategories, weights, random);
        }

        private float CalculateCategoryWeight(UpgradeCategory category, Ability currentAbilities, Polarity biomeContext, uint seed)
        {
            float weight = 1f;

            // Base category weight
            var categoryWeightEntry = categoryWeights.FirstOrDefault(cw => cw.category == category);
            if (categoryWeightEntry != null)
            {
                weight *= categoryWeightEntry.baseWeight;
                
                // Seed influence
                var seedRandom = new Unity.Mathematics.Random(seed ^ (uint)category);
                weight *= Unity.Mathematics.math.lerp(1f, categoryWeightEntry.seedMultiplier, seedInfluence) * 
                          seedRandom.NextFloat(0.8f, 1.2f);
                
                // Biome influence  
                if (collectionMap.TryGetValue(category, out var collection))
                {
                    float biomeWeight = collection.CalculateCategoryWeight(biomeContext);
                    weight *= Unity.Mathematics.math.lerp(1f, biomeWeight * categoryWeightEntry.biomeMultiplier, biomeInfluence);
                }
            }

            // Player state influence (boost categories for missing abilities)
            weight *= CalculatePlayerStateInfluence(category, currentAbilities);

            return Unity.Mathematics.math.max(0.001f, weight); // Ensure never zero
        }

        private float CalculatePlayerStateInfluence(UpgradeCategory category, Ability currentAbilities)
        {
            // Boost categories where player is lacking abilities
            float influence = 1f;

            switch (category)
            {
                case UpgradeCategory.Movement:
                    if ((currentAbilities & Ability.AllMovement) == Ability.None)
                        influence = 2f;
                    else if ((currentAbilities & Ability.AllMovement) == Ability.Jump)
                        influence = 1.5f;
                    break;

                case UpgradeCategory.Utility:
                    if ((currentAbilities & (Ability.Scan | Ability.MapUnlock)) == Ability.None)
                        influence = 1.8f;
                    break;

                case UpgradeCategory.Defense:
                    if ((currentAbilities & Ability.AllEnvironmental) == Ability.None)
                        influence = 1.6f;
                    break;

                case UpgradeCategory.Special:
                    // Special abilities get higher weight at higher levels
                    if (playerProgression != null && playerProgression.CurrentLevel > 5)
                        influence = 1.4f;
                    break;
            }

            return Unity.Mathematics.math.lerp(1f, influence, playerStateInfluence);
        }

        private UpgradeCategory SelectWeightedCategory(List<UpgradeCategory> categories, List<float> weights, Unity.Mathematics.Random random)
        {
            float totalWeight = weights.Sum();
            if (totalWeight <= 0f)
                return categories[random.NextInt(categories.Count)];

            float randomValue = random.NextFloat(0f, totalWeight);
            float currentWeight = 0f;

            for (int i = 0; i < categories.Count; i++)
            {
                currentWeight += weights[i];
                if (randomValue <= currentWeight)
                {
                    return categories[i];
                }
            }

            return categories[categories.Count - 1];
        }

        private uint GetWorldSeed()
        {
            // Try to get world seed from ECS world configuration
            if (Unity.Entities.World.DefaultGameObjectInjectionWorld != null)
            {
                var entityManager = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;
                var query = entityManager.CreateEntityQuery(typeof(WorldConfiguration));
                
                if (!query.IsEmptyIgnoreFilter)
                {
                    var config = query.GetSingleton<WorldConfiguration>();
                    return (uint)config.Seed;
                }
            }

            // Fallback to a combination of current time and instance
            return (uint)(System.DateTime.Now.Ticks ^ GetInstanceID());
        }

        private Polarity GetCurrentBiomeContext()
        {
            var playerTransform = playerProgression.transform;
            Vector3 playerPos = playerTransform.position;
            
            // Multi-layer biome detection system with overlap support
            Polarity detectedPolarity = Polarity.None;
            
            // Layer 1: Elevation-based biomes
            if (playerPos.y > 50f)
            {
                detectedPolarity |= Polarity.Wind | Polarity.Cold; // Mountain/Sky biome
            }
            else if (playerPos.y < -20f)
            {
                detectedPolarity |= Polarity.Earth; // Underground biome
                // Check for heat signatures in deep areas
                if (playerPos.y < -50f)
                    detectedPolarity |= Polarity.Heat; // Deep underground/volcanic
            }
            
            // Layer 2: Horizontal zone detection with distance-based intensity
            float xDistance = Mathf.Abs(playerPos.x);
            float zDistance = Mathf.Abs(playerPos.z);
            
            // Eastern/Western solar influence
            if (playerPos.x > 75f)
            {
                detectedPolarity |= Polarity.Sun; // Eastern sun-blessed regions
                if (xDistance > 150f) detectedPolarity |= Polarity.Heat; // Desert transition
            }
            else if (playerPos.x < -75f)
            {
                detectedPolarity |= Polarity.Moon; // Western moon-touched regions
                if (xDistance > 150f) detectedPolarity |= Polarity.Cold; // Tundra transition
            }
            
            // Layer 3: Regional climate detection based on combined coordinates
            Vector2 regional = new Vector2(playerPos.x / 100f, playerPos.z / 100f);
            float climateSeed = Mathf.PerlinNoise(regional.x + 0.5f, regional.y + 0.5f);
            
            // Perlin-based climate variation
            if (climateSeed > 0.7f)
                detectedPolarity |= Polarity.Heat; // Hot climate pockets
            else if (climateSeed < 0.3f)
                detectedPolarity |= Polarity.Cold; // Cold climate pockets
            
            // Layer 4: Proximity to water/ocean features
            if (zDistance > 100f)
            {
                detectedPolarity |= Polarity.Water; // Ocean proximity
                // Ocean depth simulation
                if (zDistance > 200f && playerPos.y < 0f)
                    detectedPolarity |= Polarity.Moon; // Deep ocean mystery
            }
            
            // Layer 5: Special region detection using world seed influence
            if (worldSeed != 0)
            {
                uint positionHash = (uint)(playerPos.x * 73 + playerPos.y * 179 + playerPos.z * 283) ^ worldSeed;
                float specialChance = (positionHash % 1000) / 1000f;
                
                if (specialChance > 0.95f) // 5% chance for special biome modifiers
                {
                    // Add rare biome combinations
                    if ((positionHash % 4) == 0) detectedPolarity |= Polarity.Sun | Polarity.Heat;
                    else if ((positionHash % 4) == 1) detectedPolarity |= Polarity.Moon | Polarity.Cold;
                    else if ((positionHash % 4) == 2) detectedPolarity |= Polarity.Earth | Polarity.Water;
                    else detectedPolarity |= Polarity.Wind | Polarity.Water;
                }
            }
            
            // Fallback: Ensure we always have some biome context
            if (detectedPolarity == Polarity.None)
            {
                // Default to balanced biome based on rough position
                if (Mathf.Abs(playerPos.x) > Mathf.Abs(playerPos.z))
                    detectedPolarity = (playerPos.x > 0) ? Polarity.Sun : Polarity.Moon;
                else
                    detectedPolarity = (playerPos.z > 0) ? Polarity.Wind : Polarity.Earth;
            }
            
            return detectedPolarity;
        }

        /// <summary>
        /// Manually trigger choice generation (for testing)
        /// </summary>
        [ContextMenu("Generate Choices")]
        public void GenerateChoicesManual()
        {
            GenerateUpgradeChoices();
        }

        /// <summary>
        /// Apply a chosen upgrade to the player
        /// </summary>
        public void ChooseUpgrade(UpgradeDefinition upgrade)
        {
            if (upgrade == null)
            {
                OnGenerationError?.Invoke("Cannot choose null upgrade");
                return;
            }

            if (playerProgression == null)
            {
                OnGenerationError?.Invoke("Player progression component not found");
                return;
            }

            // Apply the upgrade
            playerProgression.ApplyUpgrade(upgrade);

            if (enableDebugLogging)
            {
                Debug.Log($"✅ Upgrade chosen: {upgrade.UpgradeName}");
            }

            OnUpgradeChosen?.Invoke(upgrade);
        }

        private void OnValidate()
        {
            // Ensure we have at least one collection per category
            var categoriesWithCollections = upgradeCollections
                .Where(c => c != null)
                .Select(c => c.Category)
                .Distinct()
                .ToArray();

            var allCategories = System.Enum.GetValues(typeof(UpgradeCategory)).Cast<UpgradeCategory>().ToArray();
            var missingCategories = allCategories.Except(categoriesWithCollections).ToArray();

            if (missingCategories.Length > 0)
            {
                Debug.LogWarning($"Missing upgrade collections for categories: {string.Join(", ", missingCategories)}");
            }
        }
    }
}