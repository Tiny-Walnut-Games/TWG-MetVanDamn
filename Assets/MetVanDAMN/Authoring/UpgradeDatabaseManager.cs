using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TinyWalnutGames.MetVanDAMN.Authoring
{
    /// <summary>
    /// Centralized database manager for all upgrade definitions and collections.
    /// Provides lookup services for the progression system.
    /// </summary>
    public class UpgradeDatabaseManager : MonoBehaviour
    {
        [Header("Database")]
        [SerializeField] private UpgradeCollection[] allCollections = new UpgradeCollection[0];
        [SerializeField] private bool autoFindCollections = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = true;

        // Runtime database
        private Dictionary<string, UpgradeDefinition> upgradeDatabase = new Dictionary<string, UpgradeDefinition>();
        private Dictionary<UpgradeCategory, List<UpgradeDefinition>> categoryDatabase = new Dictionary<UpgradeCategory, List<UpgradeDefinition>>();

        // Singleton pattern for easy access
        private static UpgradeDatabaseManager instance;
        public static UpgradeDatabaseManager Instance => instance;

        private void Awake()
        {
            // Singleton setup
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            BuildDatabase();
        }

        private void BuildDatabase()
        {
            upgradeDatabase.Clear();
            categoryDatabase.Clear();

            // Auto-find collections if enabled
            if (autoFindCollections)
            {
                var foundCollections = Resources.FindObjectsOfTypeAll<UpgradeCollection>().ToList();
                if (foundCollections.Count > 0)
                {
                    allCollections = foundCollections.ToArray();
                    if (enableDebugLogging)
                    {
                        Debug.Log($"🔍 Auto-found {foundCollections.Count} upgrade collections");
                    }
                }
            }

            // Initialize category lists
            foreach (UpgradeCategory category in System.Enum.GetValues(typeof(UpgradeCategory)))
            {
                categoryDatabase[category] = new List<UpgradeDefinition>();
            }

            // Process all collections
            int totalUpgrades = 0;
            foreach (var collection in allCollections)
            {
                if (collection == null) continue;

                foreach (var upgrade in collection.Upgrades)
                {
                    if (upgrade == null) continue;

                    // Add to main database
                    if (!upgradeDatabase.ContainsKey(upgrade.Id))
                    {
                        upgradeDatabase[upgrade.Id] = upgrade;
                        categoryDatabase[upgrade.Category].Add(upgrade);
                        totalUpgrades++;
                    }
                    else if (enableDebugLogging)
                    {
                        Debug.LogWarning($"Duplicate upgrade ID found: {upgrade.Id}");
                    }
                }
            }

            if (enableDebugLogging)
            {
                Debug.Log($"📚 Upgrade database built: {totalUpgrades} total upgrades across {allCollections.Length} collections");
                foreach (var kvp in categoryDatabase)
                {
                    Debug.Log($"  • {kvp.Key}: {kvp.Value.Count} upgrades");
                }
            }
        }

        /// <summary>
        /// Get an upgrade by its ID
        /// </summary>
        public UpgradeDefinition GetUpgradeById(string id)
        {
            upgradeDatabase.TryGetValue(id, out var upgrade);
            return upgrade;
        }

        /// <summary>
        /// Get all upgrades in a category
        /// </summary>
        public List<UpgradeDefinition> GetUpgradesByCategory(UpgradeCategory category)
        {
            categoryDatabase.TryGetValue(category, out var upgrades);
            return upgrades ?? new List<UpgradeDefinition>();
        }

        /// <summary>
        /// Get all upgrade IDs
        /// </summary>
        public string[] GetAllUpgradeIds()
        {
            return upgradeDatabase.Keys.ToArray();
        }

        /// <summary>
        /// Get upgrade statistics
        /// </summary>
        public (int total, int byCategory, int unique) GetStats(UpgradeCategory? filterCategory = null)
        {
            if (filterCategory.HasValue)
            {
                var categoryUpgrades = GetUpgradesByCategory(filterCategory.Value);
                int unique = categoryUpgrades.Count(u => u.IsUnique);
                return (categoryUpgrades.Count, categoryUpgrades.Count, unique);
            }
            else
            {
                int total = upgradeDatabase.Count;
                int unique = upgradeDatabase.Values.Count(u => u.IsUnique);
                return (total, total, unique);
            }
        }

        /// <summary>
        /// Search upgrades by name or description
        /// </summary>
        public List<UpgradeDefinition> SearchUpgrades(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return upgradeDatabase.Values.ToList();

            var results = new List<UpgradeDefinition>();
            string lowerSearchTerm = searchTerm.ToLower();

            foreach (var upgrade in upgradeDatabase.Values)
            {
                if (upgrade.UpgradeName.ToLower().Contains(lowerSearchTerm) ||
                    upgrade.Description.ToLower().Contains(lowerSearchTerm) ||
                    upgrade.Category.ToString().ToLower().Contains(lowerSearchTerm))
                {
                    results.Add(upgrade);
                }
            }

            return results;
        }

        /// <summary>
        /// Get upgrades that grant specific abilities
        /// </summary>
        public List<UpgradeDefinition> GetUpgradesByGrantedAbility(TinyWalnutGames.MetVD.Core.Ability ability)
        {
            return upgradeDatabase.Values
                .Where(u => (u.GrantsAbilities & ability) != TinyWalnutGames.MetVD.Core.Ability.None)
                .ToList();
        }

        /// <summary>
        /// Get upgrades that require specific abilities
        /// </summary>
        public List<UpgradeDefinition> GetUpgradesByRequiredAbility(TinyWalnutGames.MetVD.Core.Ability ability)
        {
            return upgradeDatabase.Values
                .Where(u => (u.RequiredAbilities & ability) != TinyWalnutGames.MetVD.Core.Ability.None)
                .ToList();
        }

        /// <summary>
        /// Validate the database for common issues
        /// </summary>
        [ContextMenu("Validate Database")]
        public void ValidateDatabase()
        {
            var issues = new List<string>();

            // Check for missing collections in categories
            foreach (UpgradeCategory category in System.Enum.GetValues(typeof(UpgradeCategory)))
            {
                if (!categoryDatabase.ContainsKey(category) || categoryDatabase[category].Count == 0)
                {
                    issues.Add($"No upgrades found for category: {category}");
                }
            }

            // Check for upgrades with missing data
            foreach (var upgrade in upgradeDatabase.Values)
            {
                if (string.IsNullOrEmpty(upgrade.UpgradeName))
                    issues.Add($"Upgrade {upgrade.Id} has no name");

                if (string.IsNullOrEmpty(upgrade.Description))
                    issues.Add($"Upgrade {upgrade.Id} has no description");

                if (upgrade.Icon == null)
                    issues.Add($"Upgrade {upgrade.Id} has no icon");

                if (upgrade.BaseWeight <= 0)
                    issues.Add($"Upgrade {upgrade.Id} has invalid base weight: {upgrade.BaseWeight}");
            }

            // Check for circular dependencies
            foreach (var upgrade in upgradeDatabase.Values)
            {
                if (HasCircularDependency(upgrade.Id, new HashSet<string>()))
                {
                    issues.Add($"Circular dependency detected for upgrade: {upgrade.Id}");
                }
            }

            // Report results
            if (issues.Count == 0)
            {
                Debug.Log("✅ Database validation passed - no issues found");
            }
            else
            {
                Debug.LogWarning($"⚠️ Database validation found {issues.Count} issues:");
                foreach (var issue in issues)
                {
                    Debug.LogWarning($"  • {issue}");
                }
            }
        }

        private bool HasCircularDependency(string upgradeId, HashSet<string> visited)
        {
            if (visited.Contains(upgradeId))
                return true;

            var upgrade = GetUpgradeById(upgradeId);
            if (upgrade == null)
                return false;

            visited.Add(upgradeId);

            foreach (var requiredId in upgrade.RequiredUpgradeIds)
            {
                if (HasCircularDependency(requiredId, new HashSet<string>(visited)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Rebuild database (useful during development)
        /// </summary>
        [ContextMenu("Rebuild Database")]
        public void RebuildDatabase()
        {
            BuildDatabase();
        }

        /// <summary>
        /// Get collection by category
        /// </summary>
        public UpgradeCollection GetCollectionByCategory(UpgradeCategory category)
        {
            return allCollections.FirstOrDefault(c => c != null && c.Category == category);
        }

        /// <summary>
        /// Log database statistics
        /// </summary>
        [ContextMenu("Log Database Stats")]
        public void LogDatabaseStats()
        {
            Debug.Log("=== UPGRADE DATABASE STATISTICS ===");
            Debug.Log($"Total Collections: {allCollections.Length}");
            Debug.Log($"Total Upgrades: {upgradeDatabase.Count}");
            
            foreach (UpgradeCategory category in System.Enum.GetValues(typeof(UpgradeCategory)))
            {
                var count = categoryDatabase.ContainsKey(category) ? categoryDatabase[category].Count : 0;
                var unique = categoryDatabase.ContainsKey(category) ? categoryDatabase[category].Count(u => u.IsUnique) : 0;
                Debug.Log($"  {category}: {count} total, {unique} unique");
            }
        }

        private void OnValidate()
        {
            // Validate collections array
            if (allCollections != null)
            {
                for (int i = 0; i < allCollections.Length; i++)
                {
                    if (allCollections[i] == null)
                    {
                        Debug.LogWarning($"Null collection found at index {i}");
                    }
                }
            }
        }
    }
}