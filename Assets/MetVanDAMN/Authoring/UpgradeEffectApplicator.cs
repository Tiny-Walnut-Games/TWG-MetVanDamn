using UnityEngine;
using System.Collections.Generic;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring
{
    /// <summary>
    /// Applies upgrade effects to player components and stats.
    /// Handles stat modifications, ability grants, and custom effects.
    /// </summary>
    public class UpgradeEffectApplicator : MonoBehaviour
    {
        [Header("Base Stats")]
        [SerializeField] private PlayerStats baseStats = new PlayerStats();
        [SerializeField] private PlayerStats currentStats = new PlayerStats();

        [Header("Modifiers")]
        [SerializeField] private List<StatModifier> activeModifiers = new List<StatModifier>();

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = true;

        // Cached components
        private DemoPlayerMovement playerMovement;
        private DemoPlayerCombat playerCombat;
        private DemoPlayerInventory playerInventory;

        [System.Serializable]
        public class PlayerStats
        {
            [Header("Movement")]
            public float walkSpeed = 5f;
            public float runSpeed = 8f;
            public float jumpForce = 12f;
            public float dashForce = 15f;
            public float dashCooldown = 1f;

            [Header("Combat")]
            public int maxHealth = 100;
            public float attackDamage = 25f;
            public float attackSpeed = 1f;
            public float criticalChance = 0.1f;
            public float damageReduction = 0f;

            [Header("Utility")]
            public float interactionRange = 2f;
            public float scanRange = 10f;
            public int inventorySlots = 10;
            public float lootMagnetRange = 5f;

            public PlayerStats Clone()
            {
                return new PlayerStats
                {
                    walkSpeed = this.walkSpeed,
                    runSpeed = this.runSpeed,
                    jumpForce = this.jumpForce,
                    dashForce = this.dashForce,
                    dashCooldown = this.dashCooldown,
                    maxHealth = this.maxHealth,
                    attackDamage = this.attackDamage,
                    attackSpeed = this.attackSpeed,
                    criticalChance = this.criticalChance,
                    damageReduction = this.damageReduction,
                    interactionRange = this.interactionRange,
                    scanRange = this.scanRange,
                    inventorySlots = this.inventorySlots,
                    lootMagnetRange = this.lootMagnetRange
                };
            }
        }

        [System.Serializable]
        public class StatModifier
        {
            public string upgradeId;
            public string statName;
            public ModifierType modifierType;
            public float value;
            public bool isActive = true;

            public StatModifier(string id, string stat, ModifierType type, float val)
            {
                upgradeId = id;
                statName = stat;
                modifierType = type;
                value = val;
            }
        }

        private void Awake()
        {
            // Cache components
            playerMovement = GetComponent<DemoPlayerMovement>();
            playerCombat = GetComponent<DemoPlayerCombat>();
            playerInventory = GetComponent<DemoPlayerInventory>();

            // Initialize current stats from base stats
            currentStats = baseStats.Clone();
        }

        private void Start()
        {
            ApplyStatsToComponents();
        }

        /// <summary>
        /// Apply an upgrade's effects to the player
        /// </summary>
        public void ApplyUpgrade(UpgradeDefinition upgrade)
        {
            if (upgrade == null)
                return;

            // Apply stat modifications
            if (!string.IsNullOrEmpty(upgrade.TargetStat))
            {
                var modifier = new StatModifier(upgrade.Id, upgrade.TargetStat, upgrade.ModifierType, upgrade.Value);
                activeModifiers.Add(modifier);

                if (enableDebugLogging)
                {
                    Debug.Log($"📈 Applied stat modifier: {upgrade.TargetStat} {upgrade.ModifierType} {upgrade.Value}");
                }
            }

            // Handle custom effects
            foreach (var effectId in upgrade.CustomEffectIds)
            {
                ApplyCustomEffect(effectId, upgrade);
            }

            // Recalculate and apply stats
            RecalculateStats();
            ApplyStatsToComponents();
        }

        /// <summary>
        /// Reset stats to base values (removes all modifiers)
        /// </summary>
        public void ResetToDefaults()
        {
            activeModifiers.Clear();
            currentStats = baseStats.Clone();
            ApplyStatsToComponents();

            if (enableDebugLogging)
            {
                Debug.Log("🔄 Reset player stats to defaults");
            }
        }

        /// <summary>
        /// Recalculate current stats based on base stats and active modifiers
        /// </summary>
        private void RecalculateStats()
        {
            // Start with base stats
            currentStats = baseStats.Clone();

            // Apply additive modifiers first
            foreach (var modifier in activeModifiers)
            {
                if (!modifier.isActive || modifier.modifierType != ModifierType.Additive)
                    continue;

                ApplyStatModifier(modifier);
            }

            // Apply multiplicative modifiers second
            foreach (var modifier in activeModifiers)
            {
                if (!modifier.isActive || modifier.modifierType != ModifierType.Multiplicative)
                    continue;

                ApplyStatModifier(modifier);
            }

            if (enableDebugLogging)
            {
                Debug.Log($"📊 Stats recalculated: Health {currentStats.maxHealth}, Speed {currentStats.runSpeed}, Damage {currentStats.attackDamage}");
            }
        }

        /// <summary>
        /// Apply a single stat modifier to current stats
        /// </summary>
        private void ApplyStatModifier(StatModifier modifier)
        {
            switch (modifier.statName.ToLower())
            {
                // Movement stats
                case "walkspeed":
                    currentStats.walkSpeed = ApplyModifierValue(currentStats.walkSpeed, modifier);
                    break;
                case "runspeed":
                    currentStats.runSpeed = ApplyModifierValue(currentStats.runSpeed, modifier);
                    break;
                case "jumpforce":
                    currentStats.jumpForce = ApplyModifierValue(currentStats.jumpForce, modifier);
                    break;
                case "dashforce":
                    currentStats.dashForce = ApplyModifierValue(currentStats.dashForce, modifier);
                    break;
                case "dashcooldown":
                    currentStats.dashCooldown = ApplyModifierValue(currentStats.dashCooldown, modifier);
                    break;

                // Combat stats
                case "maxhealth":
                case "health":
                    currentStats.maxHealth = Mathf.RoundToInt(ApplyModifierValue(currentStats.maxHealth, modifier));
                    break;
                case "attackdamage":
                case "damage":
                    currentStats.attackDamage = ApplyModifierValue(currentStats.attackDamage, modifier);
                    break;
                case "attackspeed":
                    currentStats.attackSpeed = ApplyModifierValue(currentStats.attackSpeed, modifier);
                    break;
                case "criticalchance":
                case "crit":
                    currentStats.criticalChance = Mathf.Clamp01(ApplyModifierValue(currentStats.criticalChance, modifier));
                    break;
                case "damagereduction":
                case "armor":
                    currentStats.damageReduction = Mathf.Clamp01(ApplyModifierValue(currentStats.damageReduction, modifier));
                    break;

                // Utility stats
                case "interactionrange":
                    currentStats.interactionRange = ApplyModifierValue(currentStats.interactionRange, modifier);
                    break;
                case "scanrange":
                    currentStats.scanRange = ApplyModifierValue(currentStats.scanRange, modifier);
                    break;
                case "inventoryslots":
                    currentStats.inventorySlots = Mathf.RoundToInt(ApplyModifierValue(currentStats.inventorySlots, modifier));
                    break;
                case "lootmagnetrange":
                    currentStats.lootMagnetRange = ApplyModifierValue(currentStats.lootMagnetRange, modifier);
                    break;

                default:
                    if (enableDebugLogging)
                    {
                        Debug.LogWarning($"Unknown stat: {modifier.statName}");
                    }
                    break;
            }
        }

        /// <summary>
        /// Apply a modifier value based on its type
        /// </summary>
        private float ApplyModifierValue(float currentValue, StatModifier modifier)
        {
            switch (modifier.modifierType)
            {
                case ModifierType.Additive:
                    return currentValue + modifier.value;
                case ModifierType.Multiplicative:
                    return currentValue * modifier.value;
                case ModifierType.NewAbility:
                case ModifierType.Enhanced:
                    // These don't modify numeric stats directly
                    return currentValue;
                default:
                    return currentValue;
            }
        }

        /// <summary>
        /// Apply current stats to player components
        /// </summary>
        private void ApplyStatsToComponents()
        {
            // Apply movement stats
            if (playerMovement != null)
            {
                playerMovement.SetStats(currentStats.walkSpeed, currentStats.runSpeed, 
                                      currentStats.jumpForce, currentStats.dashForce, currentStats.dashCooldown);
            }

            // Apply combat stats
            if (playerCombat != null)
            {
                playerCombat.SetStats(currentStats.maxHealth, currentStats.attackDamage, 
                                    currentStats.attackSpeed, currentStats.criticalChance, currentStats.damageReduction);
            }

            // Apply inventory stats
            if (playerInventory != null)
            {
                playerInventory.SetStats(currentStats.inventorySlots, currentStats.interactionRange, 
                                       currentStats.scanRange, currentStats.lootMagnetRange);
            }
        }

        /// <summary>
        /// Apply custom upgrade effects that don't fit the standard stat modification pattern
        /// </summary>
        private void ApplyCustomEffect(string effectId, UpgradeDefinition upgrade)
        {
            switch (effectId.ToLower())
            {
                case "doublejump":
                    // Enable double jump capability
                    if (playerMovement != null)
                    {
                        playerMovement.EnableDoubleJump(true);
                    }
                    break;

                case "walljump":
                    // Enable wall jump capability
                    if (playerMovement != null)
                    {
                        playerMovement.EnableWallJump(true);
                    }
                    break;

                case "aimdash":
                    // Enable aimed dash
                    if (playerMovement != null)
                    {
                        playerMovement.EnableAimedDash(true);
                    }
                    break;

                case "chargeattack":
                    // Enable charge attack
                    if (playerCombat != null)
                    {
                        playerCombat.EnableChargeAttack(true);
                    }
                    break;

                case "comboattack":
                    // Enable combo attack system
                    if (playerCombat != null)
                    {
                        playerCombat.EnableComboAttacks(true);
                    }
                    break;

                case "autoloot":
                    // Enable automatic loot pickup
                    if (playerInventory != null)
                    {
                        playerInventory.EnableAutoLoot(true);
                    }
                    break;

                case "mapreveal":
                    // Enable map reveal functionality by increasing scan range significantly
                    if (playerInventory != null)
                    {
                        // Reveal map by dramatically increasing scan range
                        float currentScanRange = effectApplicator.GetCurrentStat("scanrange");
                        float newScanRange = currentScanRange + upgrade.Value * 10f; // Multiply effect for map reveal
                        
                        // Apply the enhanced scan range
                        var modifier = new StatModifier(upgrade.Id, "scanrange", ModifierType.Additive, upgrade.Value * 10f);
                        activeModifiers.Add(modifier);
                        
                        // Also enable auto-discovery of nearby interactive elements
                        StartCoroutine(MapRevealCoroutine(newScanRange));
                    }
                    break;

                case "healthregeneration":
                    // Enable health regeneration
                    if (playerCombat != null)
                    {
                        playerCombat.EnableHealthRegeneration(upgrade.Value);
                    }
                    break;

                default:
                    if (enableDebugLogging)
                    {
                        Debug.LogWarning($"Unknown custom effect: {effectId}");
                    }
                    break;
            }

            if (enableDebugLogging)
            {
                Debug.Log($"🔮 Applied custom effect: {effectId}");
            }
        }

        /// <summary>
        /// Remove a specific upgrade's effects
        /// </summary>
        public void RemoveUpgrade(string upgradeId)
        {
            // Remove all modifiers from this upgrade
            for (int i = activeModifiers.Count - 1; i >= 0; i--)
            {
                if (activeModifiers[i].upgradeId == upgradeId)
                {
                    activeModifiers.RemoveAt(i);
                }
            }

            // Recalculate stats
            RecalculateStats();
            ApplyStatsToComponents();

            if (enableDebugLogging)
            {
                Debug.Log($"🗑️ Removed upgrade effects: {upgradeId}");
            }
        }

        /// <summary>
        /// Get current stat value by name
        /// </summary>
        public float GetCurrentStat(string statName)
        {
            switch (statName.ToLower())
            {
                case "walkspeed": return currentStats.walkSpeed;
                case "runspeed": return currentStats.runSpeed;
                case "jumpforce": return currentStats.jumpForce;
                case "dashforce": return currentStats.dashForce;
                case "dashcooldown": return currentStats.dashCooldown;
                case "maxhealth": return currentStats.maxHealth;
                case "attackdamage": return currentStats.attackDamage;
                case "attackspeed": return currentStats.attackSpeed;
                case "criticalchance": return currentStats.criticalChance;
                case "damagereduction": return currentStats.damageReduction;
                case "interactionrange": return currentStats.interactionRange;
                case "scanrange": return currentStats.scanRange;
                case "inventoryslots": return currentStats.inventorySlots;
                case "lootmagnetrange": return currentStats.lootMagnetRange;
                default: return 0f;
            }
        }

        /// <summary>
        /// Get stat comparison (current vs base)
        /// </summary>
        public string GetStatComparison(string statName)
        {
            float currentValue = GetCurrentStat(statName);
            float baseValue = GetBaseStat(statName);
            
            if (Mathf.Approximately(currentValue, baseValue))
            {
                return $"{currentValue:F1}";
            }
            else
            {
                float difference = currentValue - baseValue;
                string sign = difference > 0 ? "+" : "";
                return $"{currentValue:F1} ({sign}{difference:F1})";
            }
        }

        private float GetBaseStat(string statName)
        {
            switch (statName.ToLower())
            {
                case "walkspeed": return baseStats.walkSpeed;
                case "runspeed": return baseStats.runSpeed;
                case "jumpforce": return baseStats.jumpForce;
                case "dashforce": return baseStats.dashForce;
                case "dashcooldown": return baseStats.dashCooldown;
                case "maxhealth": return baseStats.maxHealth;
                case "attackdamage": return baseStats.attackDamage;
                case "attackspeed": return baseStats.attackSpeed;
                case "criticalchance": return baseStats.criticalChance;
                case "damagereduction": return baseStats.damageReduction;
                case "interactionrange": return baseStats.interactionRange;
                case "scanrange": return baseStats.scanRange;
                case "inventoryslots": return baseStats.inventorySlots;
                case "lootmagnetrange": return baseStats.lootMagnetRange;
                default: return 0f;
            }
        }

        [ContextMenu("Log Current Stats")]
        private void LogCurrentStats()
        {
            Debug.Log($"=== CURRENT PLAYER STATS ===");
            Debug.Log($"Movement: Walk {currentStats.walkSpeed}, Run {currentStats.runSpeed}, Jump {currentStats.jumpForce}");
            Debug.Log($"Combat: Health {currentStats.maxHealth}, Damage {currentStats.attackDamage}, Speed {currentStats.attackSpeed}");
            Debug.Log($"Utility: Interaction {currentStats.interactionRange}, Scan {currentStats.scanRange}, Inventory {currentStats.inventorySlots}");
            Debug.Log($"Active Modifiers: {activeModifiers.Count}");
        }

        /// <summary>
        /// Coroutine for map reveal functionality - continuously scans for and reveals nearby interactive elements
        /// </summary>
        private System.Collections.IEnumerator MapRevealCoroutine(float revealRange)
        {
            while (true)
            {
                // Scan for nearby interactive objects and mark them as revealed
                var nearbyObjects = Physics.OverlapSphere(transform.position, revealRange);
                
                foreach (var obj in nearbyObjects)
                {
                    // Mark treasures, enemies, and interactive elements as revealed
                    var treasure = obj.GetComponent<DemoTreasureChest>();
                    if (treasure != null)
                    {
                        // Add visual indicator or glow effect for revealed treasures
                        AddRevealEffect(obj.gameObject, Color.yellow);
                    }
                    
                    var enemy = obj.GetComponent<DemoEnemyAI>();
                    if (enemy != null)
                    {
                        // Add visual indicator for revealed enemies
                        AddRevealEffect(obj.gameObject, Color.red);
                    }
                    
                    var interactable = obj.GetComponent<IDemoInteractable>();
                    if (interactable != null)
                    {
                        // Add visual indicator for revealed interactive objects
                        AddRevealEffect(obj.gameObject, Color.cyan);
                    }
                }
                
                yield return new WaitForSeconds(1f); // Scan every second
            }
        }

        /// <summary>
        /// Adds a visual reveal effect to discovered objects
        /// </summary>
        private void AddRevealEffect(GameObject target, Color effectColor)
        {
            // Check if already has reveal effect
            if (target.GetComponent<MapRevealEffect>() != null)
                return;
                
            // Add reveal effect component
            var revealEffect = target.AddComponent<MapRevealEffect>();
            revealEffect.Initialize(effectColor);
        }

        /// <summary>
        /// Component for visual reveal effects on discovered map objects
        /// </summary>
        private class MapRevealEffect : MonoBehaviour
        {
            private Light revealLight;
            private float pulseTimer = 0f;
            private Color baseColor;

            public void Initialize(Color color)
            {
                baseColor = color;
                
                // Create a subtle light effect
                var lightObj = new GameObject("RevealLight");
                lightObj.transform.SetParent(transform);
                lightObj.transform.localPosition = Vector3.up * 0.5f;
                
                revealLight = lightObj.AddComponent<Light>();
                revealLight.type = LightType.Point;
                revealLight.color = baseColor;
                revealLight.intensity = 0.5f;
                revealLight.range = 3f;
                revealLight.shadows = LightShadows.None;
            }

            private void Update()
            {
                if (revealLight != null)
                {
                    // Gentle pulsing effect
                    pulseTimer += Time.deltaTime;
                    float pulse = 0.5f + 0.3f * Mathf.Sin(pulseTimer * 2f);
                    revealLight.intensity = pulse;
                }
            }
        }
    }
}