using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring
{
    /// <summary>
    /// Manages player level progression, XP tracking, and applied upgrades.
    /// Integrates with the level-up choice system for seamless progression.
    /// </summary>
    public class PlayerLevelProgression : MonoBehaviour
    {
        [Header("Level Progression")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int currentXP = 0;
        [SerializeField] private int baseXPRequirement = 100;
        [SerializeField] private float xpGrowthRate = 1.2f;
        [SerializeField] private int maxLevel = 50;

        [Header("Abilities")]
        [SerializeField] private Ability startingAbilities = Ability.Jump;
        [SerializeField] private Ability currentAbilities = Ability.Jump;

        [Header("Applied Upgrades")]
        [SerializeField] private List<string> appliedUpgradeIds = new List<string>();
        [SerializeField] private List<AppliedUpgrade> appliedUpgrades = new List<AppliedUpgrade>();

        [Header("Save/Load")]
        [SerializeField] private bool autoSave = true;
        [SerializeField] private string saveKey = "MetVanDAMN_PlayerProgression";

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = true;

        // Events
        public System.Action<int> OnLevelUp;
        public System.Action<int, int> OnXPGained; // gained XP, total XP
        public System.Action<UpgradeDefinition> OnUpgradeApplied;
        public System.Action<int, int, int> OnStatsChanged; // level, XP, XP required for next level

        // Cached components
        private DemoPlayerMovement playerMovement;
        private DemoPlayerCombat playerCombat;
        private DemoPlayerInventory playerInventory;
        private UpgradeEffectApplicator effectApplicator;

        [System.Serializable]
        public class AppliedUpgrade
        {
            public string upgradeId;
            public string upgradeName;
            public UpgradeCategory category;
            public int levelAcquired;
            public System.DateTime timeAcquired;

            public AppliedUpgrade(string id, string name, UpgradeCategory cat, int level)
            {
                upgradeId = id;
                upgradeName = name;
                category = cat;
                levelAcquired = level;
                timeAcquired = System.DateTime.Now;
            }
        }

        // Public properties
        public int CurrentLevel => currentLevel;
        public int CurrentXP => currentXP;
        public Ability CurrentAbilities => currentAbilities;
        public string[] CurrentUpgradeIds => appliedUpgradeIds.ToArray();
        public AppliedUpgrade[] AppliedUpgrades => appliedUpgrades.ToArray();
        public int XPRequiredForNextLevel => CalculateXPRequiredForLevel(currentLevel + 1);
        public float ProgressToNextLevel => (float)currentXP / XPRequiredForNextLevel;

        private void Awake()
        {
            // Cache components
            playerMovement = GetComponent<DemoPlayerMovement>();
            playerCombat = GetComponent<DemoPlayerCombat>();
            playerInventory = GetComponent<DemoPlayerInventory>();
            effectApplicator = GetComponent<UpgradeEffectApplicator>();

            if (effectApplicator == null)
            {
                effectApplicator = gameObject.AddComponent<UpgradeEffectApplicator>();
            }

            LoadProgression();
        }

        private void Start()
        {
            // Initialize starting state
            if (currentLevel == 1 && currentXP == 0 && appliedUpgrades.Count == 0)
            {
                InitializeStartingState();
            }

            // Apply all upgrades to ensure player state is correct
            ReapplyAllUpgrades();

            if (enableDebugLogging)
            {
                Debug.Log($"🎮 Player progression initialized: Level {currentLevel}, XP {currentXP}/{XPRequiredForNextLevel}, Abilities: {currentAbilities}");
            }
        }

        private void InitializeStartingState()
        {
            currentAbilities = startingAbilities;
            
            if (enableDebugLogging)
            {
                Debug.Log($"🌱 Player starting state: Level {currentLevel}, Abilities: {currentAbilities}");
            }
        }

        /// <summary>
        /// Grant XP to the player and check for level up
        /// </summary>
        public void GainXP(int amount)
        {
            if (amount <= 0 || currentLevel >= maxLevel)
                return;

            int oldXP = currentXP;
            currentXP += amount;

            if (enableDebugLogging)
            {
                Debug.Log($"⭐ Gained {amount} XP (Total: {currentXP})");
            }

            OnXPGained?.Invoke(amount, currentXP);

            // Check for level up
            CheckForLevelUp();

            OnStatsChanged?.Invoke(currentLevel, currentXP, XPRequiredForNextLevel);

            if (autoSave)
            {
                SaveProgression();
            }
        }

        private void CheckForLevelUp()
        {
            while (currentLevel < maxLevel && currentXP >= CalculateXPRequiredForLevel(currentLevel + 1))
            {
                LevelUp();
            }
        }

        private void LevelUp()
        {
            int oldLevel = currentLevel;
            currentLevel++;

            if (enableDebugLogging)
            {
                Debug.Log($"🎉 LEVEL UP! Now level {currentLevel}");
            }

            OnLevelUp?.Invoke(currentLevel);

            if (autoSave)
            {
                SaveProgression();
            }
        }

        /// <summary>
        /// Calculate XP required to reach a specific level
        /// </summary>
        public int CalculateXPRequiredForLevel(int level)
        {
            if (level <= 1)
                return 0;

            int totalXP = 0;
            for (int i = 1; i < level; i++)
            {
                totalXP += Mathf.RoundToInt(baseXPRequirement * Mathf.Pow(xpGrowthRate, i - 1));
            }
            return totalXP;
        }

        /// <summary>
        /// Apply an upgrade to the player
        /// </summary>
        public void ApplyUpgrade(UpgradeDefinition upgrade)
        {
            if (upgrade == null)
            {
                Debug.LogError("Cannot apply null upgrade");
                return;
            }

            // Check if already applied (for unique upgrades)
            if (upgrade.IsUnique && !upgrade.AllowDuplicates && appliedUpgradeIds.Contains(upgrade.Id))
            {
                Debug.LogWarning($"Upgrade {upgrade.UpgradeName} is unique and already applied");
                return;
            }

            // Add to applied upgrades
            appliedUpgradeIds.Add(upgrade.Id);
            appliedUpgrades.Add(new AppliedUpgrade(upgrade.Id, upgrade.UpgradeName, upgrade.Category, currentLevel));

            // Grant abilities
            currentAbilities |= upgrade.GrantsAbilities;

            // Apply stat effects through the effect applicator
            if (effectApplicator != null)
            {
                effectApplicator.ApplyUpgrade(upgrade);
            }

            if (enableDebugLogging)
            {
                Debug.Log($"✅ Applied upgrade: {upgrade.UpgradeName}");
                Debug.Log($"   Abilities gained: {upgrade.GrantsAbilities}");
                Debug.Log($"   Current abilities: {currentAbilities}");
            }

            OnUpgradeApplied?.Invoke(upgrade);

            if (autoSave)
            {
                SaveProgression();
            }
        }

        /// <summary>
        /// Remove an upgrade (for debugging/testing)
        /// </summary>
        public void RemoveUpgrade(string upgradeId)
        {
            for (int i = appliedUpgradeIds.Count - 1; i >= 0; i--)
            {
                if (appliedUpgradeIds[i] == upgradeId)
                {
                    appliedUpgradeIds.RemoveAt(i);
                    appliedUpgrades.RemoveAt(i);
                    break;
                }
            }

            // Reapply all upgrades to recalculate abilities and stats
            ReapplyAllUpgrades();

            if (autoSave)
            {
                SaveProgression();
            }
        }

        /// <summary>
        /// Reapply all upgrades (used after loading or removing upgrades)
        /// </summary>
        private void ReapplyAllUpgrades()
        {
            // Reset to starting state
            currentAbilities = startingAbilities;

            if (effectApplicator != null)
            {
                effectApplicator.ResetToDefaults();
            }

            // Reapply all upgrades
            var upgradeDatabase = FindObjectOfType<UpgradeDatabaseManager>();
            if (upgradeDatabase != null)
            {
                foreach (var upgradeId in appliedUpgradeIds)
                {
                    var upgrade = upgradeDatabase.GetUpgradeById(upgradeId);
                    if (upgrade != null)
                    {
                        currentAbilities |= upgrade.GrantsAbilities;
                        
                        if (effectApplicator != null)
                        {
                            effectApplicator.ApplyUpgrade(upgrade);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get upgrades by category
        /// </summary>
        public AppliedUpgrade[] GetUpgradesByCategory(UpgradeCategory category)
        {
            return appliedUpgrades.Where(u => u.category == category).ToArray();
        }

        /// <summary>
        /// Get upgrade count by category
        /// </summary>
        public int GetUpgradeCountByCategory(UpgradeCategory category)
        {
            return appliedUpgrades.Count(u => u.category == category);
        }

        /// <summary>
        /// Check if player has a specific upgrade
        /// </summary>
        public bool HasUpgrade(string upgradeId)
        {
            return appliedUpgradeIds.Contains(upgradeId);
        }

        /// <summary>
        /// Get progression summary for UI display
        /// </summary>
        public string GetProgressionSummary()
        {
            int xpForNext = XPRequiredForNextLevel;
            float progress = ProgressToNextLevel * 100f;
            
            return $"Level {currentLevel} • {currentXP}/{xpForNext} XP ({progress:F1}%)\n" +
                   $"Upgrades: {appliedUpgrades.Count} total\n" +
                   $"Abilities: {System.Convert.ToString((long)currentAbilities, 2).Count(c => c == '1')} active";
        }

        #region Save/Load System

        [System.Serializable]
        private class ProgressionSaveData
        {
            public int level;
            public int xp;
            public uint abilities; // Store as uint for serialization
            public string[] upgradeIds;
            public string timestamp;
        }

        private void SaveProgression()
        {
            var saveData = new ProgressionSaveData
            {
                level = currentLevel,
                xp = currentXP,
                abilities = (uint)currentAbilities,
                upgradeIds = appliedUpgradeIds.ToArray(),
                timestamp = System.DateTime.Now.ToBinary().ToString()
            };

            string json = JsonUtility.ToJson(saveData, true);
            PlayerPrefs.SetString(saveKey, json);
            PlayerPrefs.Save();

            if (enableDebugLogging)
            {
                Debug.Log("💾 Player progression saved");
            }
        }

        private void LoadProgression()
        {
            if (PlayerPrefs.HasKey(saveKey))
            {
                try
                {
                    string json = PlayerPrefs.GetString(saveKey);
                    var saveData = JsonUtility.FromJson<ProgressionSaveData>(json);

                    currentLevel = saveData.level;
                    currentXP = saveData.xp;
                    currentAbilities = (Ability)saveData.abilities;
                    appliedUpgradeIds = saveData.upgradeIds.ToList();

                    // Rebuild applied upgrades list (we don't save full details, just IDs)
                    appliedUpgrades.Clear();
                    foreach (var id in appliedUpgradeIds)
                    {
                        appliedUpgrades.Add(new AppliedUpgrade(id, id, UpgradeCategory.Movement, currentLevel));
                    }

                    if (enableDebugLogging)
                    {
                        Debug.Log($"💾 Player progression loaded: Level {currentLevel}, XP {currentXP}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load progression: {e.Message}");
                    // Keep default values on load failure
                }
            }
        }

        [ContextMenu("Reset Progression")]
        private void ResetProgression()
        {
            currentLevel = 1;
            currentXP = 0;
            currentAbilities = startingAbilities;
            appliedUpgradeIds.Clear();
            appliedUpgrades.Clear();

            if (effectApplicator != null)
            {
                effectApplicator.ResetToDefaults();
            }

            PlayerPrefs.DeleteKey(saveKey);

            if (enableDebugLogging)
            {
                Debug.Log("🔄 Player progression reset to defaults");
            }
        }

        [ContextMenu("Gain 50 XP")]
        private void DebugGainXP()
        {
            GainXP(50);
        }

        [ContextMenu("Level Up")]
        private void DebugLevelUp()
        {
            GainXP(XPRequiredForNextLevel - currentXP);
        }

        #endregion
    }
}