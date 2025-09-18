#nullable enable
using UnityEngine;
using System.Collections.Generic;

namespace TinyWalnutGames.MetVanDAMN.Authoring
    {
    /// <summary>
    /// Complete loot and treasure system managing drops, chests, and pickups.
    /// Integrates with inventory system for seamless item collection.
    /// </summary>
    public class DemoLootManager : MonoBehaviour
        {
        [Header("Loot Settings")]
        public LootTable[] lootTables = System.Array.Empty<LootTable>();
        public GameObject[] treasureChestPrefabs = System.Array.Empty<GameObject>();
        public GameObject? pickupPrefab;

        [Header("Drop Chances")]
        [Range(0f, 1f)] public float commonDropChance = 0.6f;
        [Range(0f, 1f)] public float uncommonDropChance = 0.3f;
        [Range(0f, 1f)] public float rareDropChance = 0.1f;
        [Range(0f, 1f)] public float epicDropChance = 0.05f;
        [Range(0f, 1f)] public float legendaryDropChance = 0.01f;

        [Header("Treasure Spawning")]
        public int maxTreasureChests = 5;
        public float chestSpawnRadius = 20f;
        public LayerMask groundLayer = 1;

        // Loot management
        private List<DemoTreasureChest> activeTreasureChests = new();
        private Dictionary<ItemRarity, float> rarityChances = null!; // set in InitializeLootSystem

        private void Awake()
            {
            InitializeLootSystem();
            SpawnInitialTreasureChests();
            }

        private void InitializeLootSystem()
            {
            // Initialize rarity chances
            rarityChances = new Dictionary<ItemRarity, float>
            {
                { ItemRarity.Common, commonDropChance },
                { ItemRarity.Uncommon, uncommonDropChance },
                { ItemRarity.Rare, rareDropChance },
                { ItemRarity.Epic, epicDropChance },
                { ItemRarity.Legendary, legendaryDropChance }
            };

            // Initialize default loot tables if none provided
            if (lootTables == null || lootTables.Length == 0)
                {
                CreateDefaultLootTables();
                }
            }

        private void CreateDefaultLootTables()
            {
            lootTables = new LootTable[]
            {
                CreateEnemyLootTable(),
                CreateBossLootTable(),
                CreateTreasureLootTable()
            };
            }

        private LootTable CreateEnemyLootTable()
            {
            return new LootTable
                {
                name = "Enemy Drops",
                items = new LootItem[]
                {
                    new() { item = CreateHealthPotion(), weight = 40 },
                    new() { item = CreateCoin(), weight = 30 },
                    new() { item = CreateIronSword(), weight = 15 },
                    new() { item = CreateLeatherArmor(), weight = 10 },
                    new() { item = CreateMagicRing(), weight = 5 }
                }
                };
            }

        private LootTable CreateBossLootTable()
            {
            return new LootTable
                {
                name = "Boss Drops",
                items = new LootItem[]
                {
                    new() { item = CreateMegaHealthPotion(), weight = 25 },
                    new() { item = CreateGoldCoin(), weight = 20 },
                    new() { item = CreateMasterSword(), weight = 20 },
                    new() { item = CreateDragonArmor(), weight = 15 },
                    new() { item = CreateLegendaryRing(), weight = 10 },
                    new() { item = CreateArtifactWeapon(), weight = 10 }
                }
                };
            }

        private LootTable CreateTreasureLootTable()
            {
            return new LootTable
                {
                name = "Treasure Chest",
                items = new LootItem[]
                {
                    new() { item = CreateGoldCoin(), weight = 35 },
                    new() { item = CreateHealthPotion(), weight = 25 },
                    new() { item = CreateSteelSword(), weight = 15 },
                    new() { item = CreateChainArmor(), weight = 12 },
                    new() { item = CreateEnchantedRing(), weight = 8 },
                    new() { item = CreateRareGem(), weight = 5 }
                }
                };
            }

        public void SpawnLoot(Vector3 position, LootTableType tableType = LootTableType.Enemy)
            {
            LootTable? table = GetLootTable(tableType);
            if (table == null) return;

            DemoItem? droppedItem = RollForLoot(table);
            if (droppedItem != null)
                {
                CreateLootPickup(droppedItem, position);
                }
            }

        public void SpawnBossLoot(Vector3 position)
            {
            LootTable? bossTable = GetLootTable(LootTableType.Boss);
            if (bossTable == null) return;

            // Bosses drop multiple items
            int itemCount = Random.Range(2, 5);
            for (int i = 0; i < itemCount; i++)
                {
                DemoItem? droppedItem = RollForLoot(bossTable);
                if (droppedItem != null)
                    {
                    Vector3 spawnPos = position + Random.insideUnitSphere * 2f;
                    spawnPos.y = position.y; // Keep at same height
                    CreateLootPickup(droppedItem, spawnPos);
                    }
                }
            }

        public void SpawnTreasureChest(Vector3 position)
            {
            if (treasureChestPrefabs.Length == 0) return;
            if (activeTreasureChests.Count >= maxTreasureChests) return;

            GameObject prefab = treasureChestPrefabs[Random.Range(0, treasureChestPrefabs.Length)];
            GameObject chestObj = Instantiate(prefab, position, Quaternion.identity);

            var chest = chestObj.GetComponent<DemoTreasureChest>();
            if (!chest)
                {
                chest = chestObj.AddComponent<DemoTreasureChest>();
                }

            // Generate chest contents
            LootTable? treasureTable = GetLootTable(LootTableType.Treasure);
            List<DemoItem> chestContents = new List<DemoItem>();

            int itemCount = Random.Range(1, 4);
            for (int i = 0; i < itemCount; i++)
                {
                if (treasureTable == null) break;
                DemoItem? item = RollForLoot(treasureTable);
                if (item != null)
                    {
                    chestContents.Add(item);
                    }
                }

            chest.Initialize(chestContents, this);
            activeTreasureChests.Add(chest);
            }

        private void SpawnInitialTreasureChests()
            {
            // Find player position for reference
            var player = FindFirstObjectByType<DemoPlayerMovement>();
            Vector3 playerPos = player ? player.transform.position : Vector3.zero;

            int chestsToSpawn = Random.Range(2, maxTreasureChests);

            for (int i = 0; i < chestsToSpawn; i++)
                {
                Vector3 spawnPos = GetRandomTreasurePosition(playerPos);
                SpawnTreasureChest(spawnPos);
                }
            }

        private Vector3 GetRandomTreasurePosition(Vector3 centerPoint)
            {
            Vector3 randomDirection = Random.insideUnitCircle.normalized;
            Vector3 candidatePos = centerPoint + (Vector3)randomDirection * Random.Range(5f, chestSpawnRadius);

            // Try to place on ground
            RaycastHit hit;
            if (Physics.Raycast(candidatePos + Vector3.up * 10f, Vector3.down, out hit, 20f, groundLayer))
                {
                candidatePos = hit.point;
                }

            return candidatePos;
            }

        private DemoItem? RollForLoot(LootTable table)
            {
            if (table.items.Length == 0) return null;

            // Calculate total weight
            float totalWeight = 0f;
            foreach (var lootItem in table.items)
                {
                totalWeight += lootItem.weight;
                }

            // Roll for item
            float roll = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var lootItem in table.items)
                {
                currentWeight += lootItem.weight;
                if (roll <= currentWeight)
                    {
                    return lootItem.item.Clone();
                    }
                }

            return null; // No item selected
            }

        private LootTable? GetLootTable(LootTableType tableType)
            {
            string tableName = tableType switch
                {
                    LootTableType.Enemy => "Enemy Drops",
                    LootTableType.Boss => "Boss Drops",
                    LootTableType.Treasure => "Treasure Chest",
                    _ => "Enemy Drops"
                    };

            foreach (var table in lootTables)
                {
                if (table.name == tableName)
                    {
                    return table;
                    }
                }

            return lootTables.Length > 0 ? lootTables[0] : null;
            }

        private void CreateLootPickup(DemoItem item, Vector3 position)
            {
            GameObject pickup;

            if (pickupPrefab)
                {
                pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
                }
            else
                {
                // Create default pickup visual
                pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pickup.transform.position = position;
                pickup.transform.localScale = Vector3.one * 0.5f;

                // Color by rarity
                var renderer = pickup.GetComponent<Renderer>();
                renderer.material.color = GetRarityColor(item.rarity);
                }

            var pickupComponent = pickup.GetComponent<DemoLootPickup>();
            if (!pickupComponent)
                {
                pickupComponent = pickup.AddComponent<DemoLootPickup>();
                }

            pickupComponent.Initialize(item, this);
            }

        private Color GetRarityColor(ItemRarity rarity)
            {
            return rarity switch
                {
                    ItemRarity.Common => Color.white,
                    ItemRarity.Uncommon => Color.green,
                    ItemRarity.Rare => Color.blue,
                    ItemRarity.Epic => new Color(0.6f, 0f, 0.8f), // Purple
                    ItemRarity.Legendary => Color.yellow,
                    _ => Color.gray
                    };
            }

        public void OnTreasureChestOpened(DemoTreasureChest chest)
            {
            activeTreasureChests.Remove(chest);
            }

        public void OnLootPickedUp(DemoLootPickup pickup)
            {
            // Handle any global loot pickup effects
            }

        #region Item Creation Methods

        private DemoItem CreateHealthPotion()
            {
            return new DemoItem
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
            }

        private DemoItem CreateMegaHealthPotion()
            {
            return new DemoItem
                {
                id = "mega_health_potion",
                name = "Mega Health Potion",
                description = "Restores 150 health points.",
                type = ItemType.Consumable,
                rarity = ItemRarity.Rare,
                stackSize = 3,
                value = 100,
                consumableEffect = ConsumableEffect.Heal,
                effectValue = 150
                };
            }

        private DemoItem CreateCoin()
            {
            return new DemoItem
                {
                id = "copper_coin",
                name = "Copper Coin",
                description = "Basic currency.",
                type = ItemType.Material,
                rarity = ItemRarity.Common,
                stackSize = 100,
                value = 1
                };
            }

        private DemoItem CreateGoldCoin()
            {
            return new DemoItem
                {
                id = "gold_coin",
                name = "Gold Coin",
                description = "Valuable currency.",
                type = ItemType.Material,
                rarity = ItemRarity.Uncommon,
                stackSize = 50,
                value = 10
                };
            }

        private DemoItem CreateIronSword()
            {
            return new DemoItem
                {
                id = "iron_sword",
                name = "Iron Sword",
                description = "A sturdy iron blade.",
                type = ItemType.Weapon,
                rarity = ItemRarity.Common,
                stackSize = 1,
                value = 75,
                weaponStats = new WeaponStats { damage = 30, range = 2f, attackSpeed = 1.2f }
                };
            }

        private DemoItem CreateSteelSword()
            {
            return new DemoItem
                {
                id = "steel_sword",
                name = "Steel Sword",
                description = "A sharp steel blade.",
                type = ItemType.Weapon,
                rarity = ItemRarity.Uncommon,
                stackSize = 1,
                value = 150,
                weaponStats = new WeaponStats { damage = 45, range = 2.2f, attackSpeed = 1.3f }
                };
            }

        private DemoItem CreateMasterSword()
            {
            return new DemoItem
                {
                id = "master_sword",
                name = "Master Sword",
                description = "A legendary blade of heroes.",
                type = ItemType.Weapon,
                rarity = ItemRarity.Epic,
                stackSize = 1,
                value = 500,
                weaponStats = new WeaponStats { damage = 75, range = 2.5f, attackSpeed = 1.5f }
                };
            }

        private DemoItem CreateArtifactWeapon()
            {
            return new DemoItem
                {
                id = "artifact_weapon",
                name = "Ancient Artifact",
                description = "A weapon from a lost civilization.",
                type = ItemType.Weapon,
                rarity = ItemRarity.Legendary,
                stackSize = 1,
                value = 1000,
                weaponStats = new WeaponStats { damage = 100, range = 3f, attackSpeed = 2f }
                };
            }

        private DemoItem CreateLeatherArmor()
            {
            return new DemoItem
                {
                id = "leather_armor",
                name = "Leather Armor",
                description = "Basic protection.",
                type = ItemType.Armor,
                rarity = ItemRarity.Common,
                stackSize = 1,
                value = 50,
                armorStats = new ArmorStats { defense = 10, healthBonus = 20 }
                };
            }

        private DemoItem CreateChainArmor()
            {
            return new DemoItem
                {
                id = "chain_armor",
                name = "Chain Mail",
                description = "Flexible metal protection.",
                type = ItemType.Armor,
                rarity = ItemRarity.Uncommon,
                stackSize = 1,
                value = 120,
                armorStats = new ArmorStats { defense = 20, healthBonus = 40 }
                };
            }

        private DemoItem CreateDragonArmor()
            {
            return new DemoItem
                {
                id = "dragon_armor",
                name = "Dragon Scale Armor",
                description = "Armor crafted from dragon scales.",
                type = ItemType.Armor,
                rarity = ItemRarity.Epic,
                stackSize = 1,
                value = 400,
                armorStats = new ArmorStats { defense = 50, healthBonus = 100 }
                };
            }

        private DemoItem CreateMagicRing()
            {
            return new DemoItem
                {
                id = "magic_ring",
                name = "Ring of Power",
                description = "A ring imbued with magic.",
                type = ItemType.Trinket,
                rarity = ItemRarity.Uncommon,
                stackSize = 1,
                value = 80,
                trinketStats = new TrinketStats { healthBonus = 15, manaBonus = 25, speedBonus = 0.1f }
                };
            }

        private DemoItem CreateEnchantedRing()
            {
            return new DemoItem
                {
                id = "enchanted_ring",
                name = "Enchanted Ring",
                description = "A powerfully enchanted ring.",
                type = ItemType.Trinket,
                rarity = ItemRarity.Rare,
                stackSize = 1,
                value = 200,
                trinketStats = new TrinketStats { healthBonus = 30, manaBonus = 50, speedBonus = 0.2f }
                };
            }

        private DemoItem CreateLegendaryRing()
            {
            return new DemoItem
                {
                id = "legendary_ring",
                name = "Legendary Ring of the Ancients",
                description = "A ring of immense power from the old world.",
                type = ItemType.Trinket,
                rarity = ItemRarity.Legendary,
                stackSize = 1,
                value = 750,
                trinketStats = new TrinketStats { healthBonus = 75, manaBonus = 100, speedBonus = 0.4f }
                };
            }

        private DemoItem CreateRareGem()
            {
            return new DemoItem
                {
                id = "rare_gem",
                name = "Rare Gem",
                description = "A precious gemstone.",
                type = ItemType.Material,
                rarity = ItemRarity.Rare,
                stackSize = 10,
                value = 50
                };
            }

        #endregion
        }

    [System.Serializable]
    public class LootTable
        {
        public string name = string.Empty;
        public LootItem[] items = System.Array.Empty<LootItem>();
        }

    [System.Serializable]
    public class LootItem
        {
        public DemoItem item = null!; // assigned via table construction
        public float weight = 1f;
        }

    public enum LootTableType
        {
        Enemy,
        Boss,
        Treasure
        }
    }
