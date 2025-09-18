#nullable enable
using UnityEngine;
using System.Collections.Generic;

namespace TinyWalnutGames.MetVanDAMN.Authoring
    {
    /// <summary>
    /// Complete player inventory system with equipment slots and item management.
    /// Supports weapon, offhand, armor, and trinket slots with full UI integration.
    /// </summary>
    public class DemoPlayerInventory : MonoBehaviour
        {
        [Header("Inventory Settings")]
        public int inventorySize = 20;

        [Header("Input")]
        public KeyCode inventoryKey = KeyCode.I;
        public KeyCode quickUseKey = KeyCode.F;

        // Equipment slots (initialized with null-forgiving; populated via Equip methods)
        private DemoItem? equippedWeapon; // Assigned when a weapon is equipped
        private DemoItem? equippedOffhand;
        private DemoItem? equippedArmor;
        private DemoItem? equippedTrinket;

        // Public accessors for equipped items (consumed by other systems)
        public DemoItem? EquippedWeapon { get { return equippedWeapon; } }
        public DemoItem? EquippedOffhand { get { return equippedOffhand; } }
        public DemoItem? EquippedArmor { get { return equippedArmor; } }
        public DemoItem? EquippedTrinket { get { return equippedTrinket; } }

        // Currency
        public int Coins { get; private set; } = 0;

        // Inventory storage
        private List<DemoItem> inventoryItems = new();
        private int maxInventorySize;

        // Components
        private DemoPlayerCombat playerCombat;
        private DemoPlayerMovement playerMovement;

        // Auto-loot and interaction ranges (configured via SetStats)
        private float interactionRange = 1.5f;
        private float scanRange = 10f;
        private float lootMagnetRange = 3f;

        // Events (nullable until one or more subscribers attach)
        public System.Action? OnInventoryChanged;
        public System.Action<DemoItem>? OnItemEquipped;
        public System.Action<DemoItem>? OnItemUnequipped;
        public System.Action<DemoItem>? OnItemUsed;

        private void Awake()
            {
            maxInventorySize = inventorySize;
            playerCombat = GetComponent<DemoPlayerCombat>();
            playerMovement = GetComponent<DemoPlayerMovement>();

            // Initialize with some starter items
            InitializeStarterItems();
            }

        private void Update()
            {
            HandleInventoryInput();
            }

        private void HandleInventoryInput()
            {
            if (Input.GetKeyDown(inventoryKey))
                {
                ToggleInventoryUI();
                }

            if (Input.GetKeyDown(quickUseKey))
                {
                UseQuickItem();
                }
            }

        private void InitializeStarterItems()
            {
            // Add some starter items for demo
            var starterSword = new DemoItem
                {
                id = "starter_sword",
                name = "Starter Sword",
                description = "A basic sword for beginning adventurers.",
                type = ItemType.Weapon,
                rarity = ItemRarity.Common,
                stackSize = 1,
                value = 50,
                weaponStats = new WeaponStats { damage = 25, range = 2f, attackSpeed = 1f }
                };

            var healthPotion = new DemoItem
                {
                id = "health_potion",
                name = "Health Potion",
                description = "Restores 50 health points.",
                type = ItemType.Consumable,
                rarity = ItemRarity.Common,
                stackSize = 5,
                value = 25,
                consumableEffect = ConsumableEffect.Heal,
                effectValue = 50
                };

            var leatherArmor = new DemoItem
                {
                id = "leather_armor",
                name = "Leather Armor",
                description = "Basic protection for new adventurers.",
                type = ItemType.Armor,
                rarity = ItemRarity.Common,
                stackSize = 1,
                value = 75,
                armorStats = new ArmorStats { defense = 10, healthBonus = 20 }
                };

            AddItem(starterSword);
            AddItem(healthPotion, 3);
            AddItem(leatherArmor);

            // Auto-equip starter items
            EquipItem(starterSword);
            EquipItem(leatherArmor);
            }

        // Currency API
        public void AddCoins(int amount)
            {
            if (amount <= 0) return;
            Coins += amount;
            OnInventoryChanged?.Invoke();
            }

        public bool SpendCoins(int amount)
            {
            if (amount <= 0) return true;
            if (Coins < amount) return false;
            Coins -= amount;
            OnInventoryChanged?.Invoke();
            return true;
            }

        public bool AddItem(DemoItem item, int quantity = 1)
            {
            if (item == null) return false;

            // Try to stack with existing items
            if (item.stackSize > 1)
                {
                foreach (var existingItem in inventoryItems)
                    {
                    if (existingItem.id == item.id && existingItem.currentStack < existingItem.stackSize)
                        {
                        int spaceInStack = existingItem.stackSize - existingItem.currentStack;
                        int amountToAdd = Mathf.Min(quantity, spaceInStack);

                        existingItem.currentStack += amountToAdd;
                        quantity -= amountToAdd;

                        if (quantity <= 0)
                            {
                            OnInventoryChanged?.Invoke();
                            return true;
                            }
                        }
                    }
                }

            // Add new stacks
            while (quantity > 0 && inventoryItems.Count < maxInventorySize)
                {
                var newItem = item.Clone();
                newItem.currentStack = Mathf.Min(quantity, newItem.stackSize);
                quantity -= newItem.currentStack;

                inventoryItems.Add(newItem);
                }

            OnInventoryChanged?.Invoke();
            return quantity <= 0; // Return true if all items were added
            }

        public bool RemoveItem(string itemId, int quantity = 1)
            {
            int remainingToRemove = quantity;

            for (int i = inventoryItems.Count - 1; i >= 0; i--)
                {
                if (inventoryItems[i].id == itemId)
                    {
                    int removeFromStack = Mathf.Min(remainingToRemove, inventoryItems[i].currentStack);
                    inventoryItems[i].currentStack -= removeFromStack;
                    remainingToRemove -= removeFromStack;

                    if (inventoryItems[i].currentStack <= 0)
                        {
                        inventoryItems.RemoveAt(i);
                        }

                    if (remainingToRemove <= 0)
                        {
                        break;
                        }
                    }
                }

            OnInventoryChanged?.Invoke();
            return remainingToRemove <= 0;
            }

        public bool HasItem(string itemId, int quantity = 1)
            {
            int totalCount = 0;
            foreach (var item in inventoryItems)
                {
                if (item.id == itemId)
                    {
                    totalCount += item.currentStack;
                    if (totalCount >= quantity)
                        {
                        return true;
                        }
                    }
                }
            return false;
            }

        public bool EquipItem(DemoItem item)
            {
            if (item == null) return false;

            // Check if item can be equipped
            switch (item.type)
                {
                case ItemType.Weapon:
                    return EquipWeapon(item);
                case ItemType.Offhand:
                    return EquipOffhand(item);
                case ItemType.Armor:
                    return EquipArmor(item);
                case ItemType.Trinket:
                    return EquipTrinket(item);
                default:
                    return false;
                }
            }

        public bool UnequipItem(EquipmentSlot slot)
            {
            DemoItem? itemToUnequip = null;

            switch (slot)
                {
                case EquipmentSlot.Weapon:
                    itemToUnequip = equippedWeapon;
                    equippedWeapon = null;
                    break;
                case EquipmentSlot.Offhand:
                    itemToUnequip = equippedOffhand;
                    equippedOffhand = null;
                    break;
                case EquipmentSlot.Armor:
                    itemToUnequip = equippedArmor;
                    equippedArmor = null;
                    break;
                case EquipmentSlot.Trinket:
                    itemToUnequip = equippedTrinket;
                    equippedTrinket = null;
                    break;
                }

            if (itemToUnequip != null)
                {
                // Try to add back to inventory
                if (AddItem(itemToUnequip))
                    {
                    OnItemUnequipped?.Invoke(itemToUnequip);
                    UpdatePlayerStats();
                    return true;
                    }
                else
                    {
                    // Re-equip if inventory is full
                    EquipItem(itemToUnequip);
                    return false;
                    }
                }

            return false;
            }

        public bool UseItem(DemoItem item)
            {
            if (item == null || item.type != ItemType.Consumable) return false;

            // Apply consumable effect
            switch (item.consumableEffect)
                {
                case ConsumableEffect.Heal:
                    if (playerCombat)
                        {
                        playerCombat.Heal(item.effectValue);
                        }
                    break;
                case ConsumableEffect.Mana:
                    // Restore mana/energy if system exists
                    break;
                case ConsumableEffect.Buff:
                    ApplyBuff(item);
                    break;
                }

            // Remove one from inventory
            RemoveItem(item.id, 1);
            OnItemUsed?.Invoke(item);

            return true;
            }

        private bool EquipWeapon(DemoItem weapon)
            {
            // Unequip current weapon if any
            if (equippedWeapon != null && !AddItem(equippedWeapon))
                {
                return false; // Can't unequip if inventory is full
                }

            equippedWeapon = weapon;
            RemoveItem(weapon.id, 1);

            // Update combat system with new weapon
            if (playerCombat && weapon.weaponStats != null)
                {
                var demoWeapon = new DemoWeapon
                    {
                    name = weapon.name,
                    type = GetWeaponType(weapon),
                    damage = weapon.weaponStats.damage,
                    range = weapon.weaponStats.range,
                    attackSpeed = weapon.weaponStats.attackSpeed
                    };
                playerCombat.AddWeapon(demoWeapon);
                }

            OnItemEquipped?.Invoke(weapon);
            UpdatePlayerStats();
            return true;
            }

        private bool EquipOffhand(DemoItem offhand)
            {
            if (equippedOffhand != null && !AddItem(equippedOffhand))
                {
                return false;
                }

            equippedOffhand = offhand;
            RemoveItem(offhand.id, 1);
            OnItemEquipped?.Invoke(offhand);
            UpdatePlayerStats();
            return true;
            }

        private bool EquipArmor(DemoItem armor)
            {
            if (equippedArmor != null && !AddItem(equippedArmor))
                {
                return false;
                }

            equippedArmor = armor;
            RemoveItem(armor.id, 1);
            OnItemEquipped?.Invoke(armor);
            UpdatePlayerStats();
            return true;
            }

        private bool EquipTrinket(DemoItem trinket)
            {
            if (equippedTrinket != null && !AddItem(equippedTrinket))
                {
                return false;
                }

            equippedTrinket = trinket;
            RemoveItem(trinket.id, 1);
            OnItemEquipped?.Invoke(trinket);
            UpdatePlayerStats();
            return true;
            }

        private void UpdatePlayerStats()
            {
            // Apply stat bonuses from equipped items
            int totalHealthBonus = 0;
            int totalDefenseBonus = 0;

            if (equippedArmor?.armorStats != null)
                {
                totalHealthBonus += equippedArmor.armorStats.healthBonus;
                totalDefenseBonus += equippedArmor.armorStats.defense;
                }

            if (equippedTrinket?.trinketStats != null)
                {
                totalHealthBonus += equippedTrinket.trinketStats.healthBonus;
                // Apply other trinket bonuses
                }

            // Update player combat with new stats
            if (playerCombat)
                {
                // Apply stat bonuses from equipped items to combat system
                ApplyStatBonusesToCombat();
                }
            }

        private void ApplyBuff(DemoItem item)
            {
            // Apply temporary buff effects from consumable items
            if (item.type == ItemType.Consumable)
                {
                Debug.Log($"🧪 Applying buff from {item.name}");

                // Apply different buffs based on item properties
                if (item.name.ToLower().Contains("health"))
                    {
                    ApplyHealthBuff(item);
                    }
                else if (item.name.ToLower().Contains("damage"))
                    {
                    ApplyDamageBuff(item);
                    }
                else if (item.name.ToLower().Contains("speed"))
                    {
                    ApplySpeedBuff(item);
                    }
                else if (item.name.ToLower().Contains("defense"))
                    {
                    ApplyDefenseBuff(item);
                    }
                else
                    {
                    // Generic buff
                    ApplyGenericBuff(item);
                    }
                }
            }

        private void ApplyHealthBuff(DemoItem item)
            {
            // Restore health based on item potency (armorStats optional)
            float healAmount = item.armorStats?.healthBonus ?? 0f;
            if (healAmount <= 0) healAmount = 25f; // Default heal amount

            if (playerCombat)
                {
                playerCombat.Heal(healAmount);
                Debug.Log($"💚 Healed for {healAmount} HP");
                }
            }

        private void ApplyDamageBuff(DemoItem item)
            {
            // Temporary damage increase
            float damageBonus = item.weaponStats?.damage ?? 0f;
            if (damageBonus <= 0) damageBonus = 10f; // Default damage bonus

            StartCoroutine(ApplyTemporaryDamageBuff(damageBonus, 30f)); // 30 second duration
            }

        private void ApplySpeedBuff(DemoItem item)
            {
            // Temporary speed increase
            var playerMovement = GetComponent<DemoPlayerMovement>();
            if (playerMovement)
                {
                StartCoroutine(ApplyTemporarySpeedBuff(playerMovement, 1.5f, 20f)); // 50% speed boost for 20 seconds
                }
            }

        private void ApplyDefenseBuff(DemoItem item)
            {
            // Temporary defense increase
            float defenseBonus = item.armorStats?.defense ?? 0f;
            if (defenseBonus <= 0) defenseBonus = 5f; // Default defense bonus

            StartCoroutine(ApplyTemporaryDefenseBuff(defenseBonus, 60f)); // 1 minute duration
            }

        private void ApplyGenericBuff(DemoItem item)
            {
            // Apply a small bonus to all stats
            Debug.Log($"✨ Applied generic buff from {item.name}");

            StartCoroutine(ApplyTemporaryDamageBuff(5f, 20f));

            var playerMovement = GetComponent<DemoPlayerMovement>();
            if (playerMovement)
                {
                StartCoroutine(ApplyTemporarySpeedBuff(playerMovement, 1.2f, 20f));
                }
            }

        private System.Collections.IEnumerator ApplyTemporaryDamageBuff(float damageBonus, float duration)
            {
            if (playerCombat)
                {
                playerCombat.AddDamageBonus(damageBonus);
                Debug.Log($"⚔️ Damage increased by {damageBonus} for {duration} seconds");

                yield return new WaitForSeconds(duration);

                playerCombat.RemoveDamageBonus(damageBonus);
                Debug.Log($"⚔️ Damage buff expired");
                }
            }

        private System.Collections.IEnumerator ApplyTemporarySpeedBuff(DemoPlayerMovement movement, float speedMultiplier, float duration)
            {
            movement.ApplySpeedMultiplier(speedMultiplier);
            Debug.Log($"💨 Speed increased by {(speedMultiplier - 1) * 100}% for {duration} seconds");

            yield return new WaitForSeconds(duration);

            movement.RemoveSpeedMultiplier(speedMultiplier);
            Debug.Log($"💨 Speed buff expired");
            }

        private System.Collections.IEnumerator ApplyTemporaryDefenseBuff(float defenseBonus, float duration)
            {
            if (playerCombat)
                {
                playerCombat.AddDefenseBonus(defenseBonus);
                Debug.Log($"🛡️ Defense increased by {defenseBonus} for {duration} seconds");

                yield return new WaitForSeconds(duration);

                playerCombat.RemoveDefenseBonus(defenseBonus);
                Debug.Log($"🛡️ Defense buff expired");
                }
            }

        private void ApplyStatBonusesToCombat()
            {
            // Calculate total stat bonuses from all equipped items
            float totalHealthBonus = 0f;
            float totalDefenseBonus = 0f;
            float totalDamageBonus = 0f;

            if (equippedArmor?.armorStats != null)
                {
                totalHealthBonus += equippedArmor.armorStats.healthBonus;
                totalDefenseBonus += equippedArmor.armorStats.defense;
                }

            if (equippedWeapon?.weaponStats != null)
                {
                totalDamageBonus += equippedWeapon.weaponStats.damage * 0.1f; // 10% of weapon damage as bonus
                }

            if (equippedTrinket != null)
                {
                // Trinkets provide small bonuses to all stats (stats objects optional)
                if (equippedTrinket.armorStats != null)
                    {
                    totalHealthBonus += equippedTrinket.armorStats.healthBonus;
                    totalDefenseBonus += equippedTrinket.armorStats.defense;
                    }
                if (equippedTrinket.weaponStats != null)
                    {
                    totalDamageBonus += equippedTrinket.weaponStats.damage * 0.05f; // 5% damage bonus
                    }
                }

            // Apply bonuses to combat system
            if (playerCombat)
                {
                playerCombat.SetEquipmentBonuses(totalHealthBonus, totalDefenseBonus, totalDamageBonus);
                }
            }

        private WeaponType GetWeaponType(DemoItem weapon)
            {
            // Determine weapon type based on weapon stats or item properties
            if (weapon.weaponStats != null && weapon.weaponStats.range > 5f)
                {
                return WeaponType.Ranged;
                }
            else if (weapon.name.ToLower().Contains("staff"))
                {
                return WeaponType.AoE;
                }
            else
                {
                return WeaponType.Melee;
                }
            }

        private void UseQuickItem()
            {
            // Use first consumable item in inventory
            foreach (var item in inventoryItems)
                {
                if (item.type == ItemType.Consumable)
                    {
                    UseItem(item);
                    break;
                    }
                }
            }

        private void ToggleInventoryUI()
            {
            // Full implementation for inventory UI toggle
            var inventoryUI = FindFirstObjectByType<DemoInventoryUI>();
            if (inventoryUI)
                {
                inventoryUI.ToggleInventory();
                Debug.Log($"🎒 Inventory UI toggled: {(inventoryUI.IsVisible ? "Open" : "Closed")}");
                }
            else
                {
                // Create inventory UI if it doesn't exist
                CreateInventoryUI();
                }
            }

        private void CreateInventoryUI()
            {
            Debug.Log("🎒 Creating inventory UI...");

            // Find or create canvas
            var canvas = FindFirstObjectByType<Canvas>();
            if (!canvas)
                {
                var canvasObj = new GameObject("InventoryCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                // Add EventSystem if not present
                if (!FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>())
                    {
                    var eventSystemObj = new GameObject("EventSystem");
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    }
                }

            // Create inventory UI GameObject
            var inventoryUIObj = new GameObject("DemoInventoryUI");
            inventoryUIObj.transform.SetParent(canvas.transform, false);

            var inventoryUI = inventoryUIObj.AddComponent<DemoInventoryUI>();
            // Bind and initialize UI
            inventoryUI.playerInventory = this;
            inventoryUI.Initialize();
            inventoryUI.ToggleInventory();

            Debug.Log("🎒 Inventory UI created and opened");
            }

        // Public API
        public List<DemoItem> GetInventoryItems() => new(inventoryItems);
        public DemoItem? GetEquippedItem(EquipmentSlot slot)
            {
            switch (slot)
                {
                case EquipmentSlot.Weapon: return equippedWeapon;
                case EquipmentSlot.Offhand: return equippedOffhand;
                case EquipmentSlot.Armor: return equippedArmor;
                case EquipmentSlot.Trinket: return equippedTrinket;
                default: return null;
                }
            }

        public int GetInventorySpace() => maxInventorySize - inventoryItems.Count;
        public bool IsInventoryFull() => inventoryItems.Count >= maxInventorySize;

        // Upgrade system support
        private bool autoLootEnabled = false;

        /// <summary>
        /// Set inventory stats from upgrade system
        /// </summary>
        public void SetStats(int newInventorySlots, float newInteractionRange, float newScanRange, float newLootMagnetRange)
            {
            maxInventorySize = newInventorySlots;
            interactionRange = newInteractionRange;
            scanRange = newScanRange;
            lootMagnetRange = newLootMagnetRange;
            }

        /// <summary>
        /// Enable/disable automatic loot pickup
        /// </summary>
        public void EnableAutoLoot(bool enabled)
            {
            autoLootEnabled = enabled;

            if (enabled)
                {
                // Start auto-loot checking coroutine
                StartCoroutine(AutoLootCoroutine());
                }
            }

        /// <summary>
        /// Auto-loot coroutine - automatically pickup nearby items
        /// </summary>
        private System.Collections.IEnumerator AutoLootCoroutine()
            {
            while (autoLootEnabled)
                {
                // Find nearby loot items
                var nearbyItems = Physics.OverlapSphere(transform.position, lootMagnetRange);

                foreach (var item in nearbyItems)
                    {
                    var lootComponent = item.GetComponent<DemoLootDrop>();
                    if (lootComponent && !IsInventoryFull())
                        {
                        var lootItem = lootComponent.GetItem();
                        if (lootItem != null)
                            {
                            AddItem(lootItem);
                            Destroy(item.gameObject);
                            }
                        }
                    }

                yield return new WaitForSeconds(0.5f); // Check every half second
                }
            }
        }

    [System.Serializable]
    public class DemoItem
        {
        public string id = string.Empty;
        public string name = string.Empty;
        public string description = string.Empty;
        public ItemType type;
        public ItemRarity rarity;
        public int stackSize = 1;
        public int currentStack = 1;
        public int value;

        // Equipment stats
        public WeaponStats? weaponStats;
        public ArmorStats? armorStats;
        public TrinketStats? trinketStats;

        // Consumable properties
        public ConsumableEffect consumableEffect;
        public int effectValue;

        public DemoItem Clone()
            {
            return new DemoItem
                {
                id = this.id,
                name = this.name,
                description = this.description,
                type = this.type,
                rarity = this.rarity,
                stackSize = this.stackSize,
                currentStack = 1, // New stack starts at 1
                value = this.value,
                weaponStats = this.weaponStats?.Clone(),
                armorStats = this.armorStats?.Clone(),
                trinketStats = this.trinketStats?.Clone(),
                consumableEffect = this.consumableEffect,
                effectValue = this.effectValue
                };
            }
        }

    [System.Serializable]
    public class WeaponStats
        {
        public int damage;
        public float range;
        public float attackSpeed;

        public WeaponStats Clone()
            {
            return new WeaponStats
                {
                damage = this.damage,
                range = this.range,
                attackSpeed = this.attackSpeed
                };
            }
        }

    [System.Serializable]
    public class ArmorStats
        {
        public int defense;
        public int healthBonus;

        public ArmorStats Clone()
            {
            return new ArmorStats
                {
                defense = this.defense,
                healthBonus = this.healthBonus
                };
            }
        }

    [System.Serializable]
    public class TrinketStats
        {
        public int healthBonus;
        public int manaBonus;
        public int damageBonus;
        public int defenseBonus;
        public float speedBonus;

        public TrinketStats Clone()
            {
            return new TrinketStats
                {
                healthBonus = this.healthBonus,
                manaBonus = this.manaBonus,
                speedBonus = this.speedBonus
                };
            }
        }

    public enum ItemType
        {
        Weapon,
        Offhand,
        Armor,
        Trinket,
        Consumable,
        Material,
        Quest
        }

    public enum ItemRarity
        {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
        }

    public enum EquipmentSlot
        {
        Weapon,
        Offhand,
        Armor,
        Trinket
        }

    public enum ConsumableEffect
        {
        Heal,
        Mana,
        Buff,
        DamageBuff,
        DefenseBuff,
        SpeedBuff,
        Debuff
        }
    }
