using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TinyWalnutGames.MetVanDAMN.Authoring
    {
    /// <summary>
    /// Complete inventory UI system with equipment slots and item management.
    /// Provides full visual interface for the inventory system.
    /// </summary>
    public class DemoInventoryUI : MonoBehaviour
        {
        [Header("UI Panels")]
        public GameObject inventoryPanel;
        public Transform inventoryGrid;
        public Transform equipmentPanel;

        [Header("Equipment Slots")]
        public DemoEquipmentSlot weaponSlot;
        public DemoEquipmentSlot offhandSlot;
        public DemoEquipmentSlot armorSlot;
        public DemoEquipmentSlot trinketSlot;

        [Header("Item Display")]
        public GameObject itemSlotPrefab;
        public GameObject tooltipPanel;
        public Text tooltipText;

        [Header("Player Info")]
        public Slider healthBar;
        public Text healthText;
        public Text coinsText;

        // Bound player inventory (can be assigned by spawner/owner)
        public DemoPlayerInventory playerInventory;
        private DemoPlayerCombat playerCombat;
        private List<DemoInventorySlot> inventorySlots = new();
        private bool isInventoryOpen = false;

        private void Awake()
            {
            // Find player components only if not externally bound
            if (!playerInventory)
                {
                var player = FindFirstObjectByType<DemoPlayerMovement>();
                if (player)
                    {
                    playerInventory = player.GetComponent<DemoPlayerInventory>();
                    playerCombat = player.GetComponent<DemoPlayerCombat>();
                    }
                }

            // Create UI if not present
            if (!inventoryPanel)
                {
                CreateInventoryUI();
                }

            // Subscribe to inventory events
            if (playerInventory)
                {
                playerInventory.OnInventoryChanged += RefreshInventoryDisplay;
                playerInventory.OnItemEquipped += OnItemEquipped;
                playerInventory.OnItemUnequipped += OnItemUnequipped;
                }

            if (playerCombat)
                {
                playerCombat.OnHealthChanged += UpdateHealthDisplay;
                }

            // Initially hide inventory
            SetInventoryVisible(false);
            }

        private void Start()
            {
            InitializeInventorySlots();
            RefreshInventoryDisplay();
            if (playerCombat)
                {
                UpdateHealthDisplay(playerCombat.CurrentHealth, playerCombat.MaxHealth);
                }
            else
                {
                UpdateHealthDisplay(100, 100);
                }
            }

        // Public one-shot initializer when created programmatically
        public void Initialize()
            {
            // Ensure UI exists and hooks are in place
            if (!inventoryPanel)
                {
                CreateInventoryUI();
                }

            // Rewire events to current playerInventory if not already set
            if (playerInventory)
                {
                playerInventory.OnInventoryChanged -= RefreshInventoryDisplay;
                playerInventory.OnItemEquipped -= OnItemEquipped;
                playerInventory.OnItemUnequipped -= OnItemUnequipped;

                playerInventory.OnInventoryChanged += RefreshInventoryDisplay;
                playerInventory.OnItemEquipped += OnItemEquipped;
                playerInventory.OnItemUnequipped += OnItemUnequipped;
                }

            InitializeInventorySlots();
            RefreshInventoryDisplay();
            }

        private void CreateInventoryUI()
            {
            // Create main canvas if not exists
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (!canvas)
                {
                GameObject canvasObj = new GameObject("UI Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                }

            // Create inventory panel
            inventoryPanel = new GameObject("InventoryPanel");
            inventoryPanel.transform.SetParent(canvas.transform, false);

            var panelImage = inventoryPanel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            var rectTransform = inventoryPanel.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.2f, 0.2f);
            rectTransform.anchorMax = new Vector2(0.8f, 0.8f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            CreateInventoryGrid();
            CreateEquipmentPanel();
            CreatePlayerInfoPanel();
            CreateTooltipPanel();
            }

        private void CreateInventoryGrid()
            {
            // Create inventory grid
            GameObject gridObj = new GameObject("InventoryGrid");
            gridObj.transform.SetParent(inventoryPanel.transform, false);

            var gridLayout = gridObj.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(60, 60);
            gridLayout.spacing = new Vector2(5, 5);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 5;

            var rectTransform = gridObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.4f, 0.1f);
            rectTransform.anchorMax = new Vector2(0.9f, 0.9f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            inventoryGrid = gridObj.transform;
            }

        private void CreateEquipmentPanel()
            {
            // Create equipment panel
            GameObject equipObj = new GameObject("EquipmentPanel");
            equipObj.transform.SetParent(inventoryPanel.transform, false);

            var rectTransform = equipObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.05f, 0.4f);
            rectTransform.anchorMax = new Vector2(0.35f, 0.9f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            equipmentPanel = equipObj.transform;

            // Create equipment slots
            CreateEquipmentSlot("Weapon", EquipmentSlot.Weapon, new Vector2(0.1f, 0.7f));
            CreateEquipmentSlot("Offhand", EquipmentSlot.Offhand, new Vector2(0.1f, 0.5f));
            CreateEquipmentSlot("Armor", EquipmentSlot.Armor, new Vector2(0.1f, 0.3f));
            CreateEquipmentSlot("Trinket", EquipmentSlot.Trinket, new Vector2(0.1f, 0.1f));
            }

        private void CreateEquipmentSlot(string slotName, EquipmentSlot slotType, Vector2 anchorPosition)
            {
            GameObject slotObj = new GameObject($"{slotName}Slot");
            slotObj.transform.SetParent(equipmentPanel, false);

            var rectTransform = slotObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorPosition;
            rectTransform.anchorMax = anchorPosition + new Vector2(0.8f, 0.15f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var slotImage = slotObj.AddComponent<Image>();
            slotImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            var equipSlot = slotObj.AddComponent<DemoEquipmentSlot>();
            equipSlot.Initialize(slotType, this);

            // Store reference
            switch (slotType)
                {
                case EquipmentSlot.Weapon: weaponSlot = equipSlot; break;
                case EquipmentSlot.Offhand: offhandSlot = equipSlot; break;
                case EquipmentSlot.Armor: armorSlot = equipSlot; break;
                case EquipmentSlot.Trinket: trinketSlot = equipSlot; break;
                }

            // Add label
            GameObject labelObj = new GameObject($"{slotName}Label");
            labelObj.transform.SetParent(slotObj.transform, false);

            var labelText = labelObj.AddComponent<Text>();
            labelText.text = slotName;
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 12;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;

            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            }

        private void CreatePlayerInfoPanel()
            {
            GameObject infoObj = new GameObject("PlayerInfoPanel");
            infoObj.transform.SetParent(inventoryPanel.transform, false);

            var rectTransform = infoObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.05f, 0.05f);
            rectTransform.anchorMax = new Vector2(0.35f, 0.35f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Health bar
            GameObject healthObj = new GameObject("HealthBar");
            healthObj.transform.SetParent(infoObj.transform, false);

            healthBar = healthObj.AddComponent<Slider>();
            healthBar.minValue = 0;
            healthBar.maxValue = 100;
            healthBar.value = 100;

            var healthRect = healthObj.GetComponent<RectTransform>();
            healthRect.anchorMin = new Vector2(0, 0.7f);
            healthRect.anchorMax = new Vector2(1, 0.9f);
            healthRect.offsetMin = Vector2.zero;
            healthRect.offsetMax = Vector2.zero;

            // Health text
            GameObject healthTextObj = new GameObject("HealthText");
            healthTextObj.transform.SetParent(infoObj.transform, false);

            healthText = healthTextObj.AddComponent<Text>();
            healthText.text = "Health: 100/100";
            healthText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            healthText.fontSize = 14;
            healthText.color = Color.white;

            var healthTextRect = healthTextObj.GetComponent<RectTransform>();
            healthTextRect.anchorMin = new Vector2(0, 0.5f);
            healthTextRect.anchorMax = new Vector2(1, 0.7f);
            healthTextRect.offsetMin = Vector2.zero;
            healthTextRect.offsetMax = Vector2.zero;

            // Coins text
            GameObject coinsObj = new GameObject("CoinsText");
            coinsObj.transform.SetParent(infoObj.transform, false);

            coinsText = coinsObj.AddComponent<Text>();
            coinsText.text = "Coins: 0";
            coinsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            coinsText.fontSize = 14;
            coinsText.color = Color.yellow;

            var coinsRect = coinsObj.GetComponent<RectTransform>();
            coinsRect.anchorMin = new Vector2(0, 0.3f);
            coinsRect.anchorMax = new Vector2(1, 0.5f);
            coinsRect.offsetMin = Vector2.zero;
            coinsRect.offsetMax = Vector2.zero;
            }

        private void CreateTooltipPanel()
            {
            tooltipPanel = new GameObject("TooltipPanel");
            tooltipPanel.transform.SetParent(inventoryPanel.transform, false);

            var tooltipImage = tooltipPanel.AddComponent<Image>();
            tooltipImage.color = new Color(0, 0, 0, 0.9f);

            var tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            tooltipRect.sizeDelta = new Vector2(200, 100);

            // Tooltip text
            GameObject textObj = new GameObject("TooltipText");
            textObj.transform.SetParent(tooltipPanel.transform, false);

            tooltipText = textObj.AddComponent<Text>();
            tooltipText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tooltipText.fontSize = 12;
            tooltipText.color = Color.white;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 5);
            textRect.offsetMax = new Vector2(-5, -5);

            tooltipPanel.SetActive(false);
            }

        private void InitializeInventorySlots()
            {
            if (!playerInventory) return;

            // Clear existing slots
            foreach (var slot in inventorySlots)
                {
                if (slot) Destroy(slot.gameObject);
                }
            inventorySlots.Clear();

            // Create inventory slots
            for (int i = 0; i < 20; i++) // Default inventory size
                {
                GameObject slotObj = new GameObject($"InventorySlot_{i}");
                slotObj.transform.SetParent(inventoryGrid, false);

                var slotImage = slotObj.AddComponent<Image>();
                slotImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

                var inventorySlot = slotObj.AddComponent<DemoInventorySlot>();
                inventorySlot.Initialize(i, this);

                inventorySlots.Add(inventorySlot);
                }
            }

        public void ToggleInventory()
            {
            SetInventoryVisible(!isInventoryOpen);
            }

        public void SetInventoryVisible(bool visible)
            {
            isInventoryOpen = visible;
            if (inventoryPanel)
                {
                inventoryPanel.SetActive(visible);
                }

            // Pause/unpause game when inventory is open
            Time.timeScale = visible ? 0f : 1f;
            }

        public bool IsVisible => isInventoryOpen;

        private void RefreshInventoryDisplay()
            {
            if (!playerInventory) return;

            var items = playerInventory.GetInventoryItems();

            // Clear all slots first
            foreach (var slot in inventorySlots)
                {
                slot.SetItem(null);
                }

            // Fill slots with items
            for (int i = 0; i < items.Count && i < inventorySlots.Count; i++)
                {
                inventorySlots[i].SetItem(items[i]);
                }

            // Update coins display
            UpdateCoinsDisplay();
            }

        private void UpdateCoinsDisplay()
            {
            if (!playerInventory || !coinsText) return;

            int coinCount = 0;
            var items = playerInventory.GetInventoryItems();
            foreach (var item in items)
                {
                if (item.id == "copper_coin" || item.id == "gold_coin")
                    {
                    coinCount += item.currentStack * (item.id == "gold_coin" ? 10 : 1);
                    }
                }

            coinsText.text = $"Coins: {coinCount}";
            }

        private void OnItemEquipped(DemoItem item)
            {
            // Update equipment slot display
            var slot = GetEquipmentSlotForItem(item);
            if (slot) { slot.SetItem(item); }
            }

        private void OnItemUnequipped(DemoItem item)
            {
            // Clear equipment slot display
            var slot = GetEquipmentSlotForItem(item);
            if (slot) { slot.SetItem(null); }
            }

        private DemoEquipmentSlot GetEquipmentSlotForItem(DemoItem item)
            {
            return item.type switch
                {
                    ItemType.Weapon => weaponSlot,
                    ItemType.Offhand => offhandSlot,
                    ItemType.Armor => armorSlot,
                    ItemType.Trinket => trinketSlot,
                    _ => null
                    };
            }

        private void UpdateHealthDisplay(int currentHealth, int maxHealth)
            {
            if (healthBar)
                {
                healthBar.maxValue = maxHealth;
                healthBar.value = currentHealth;
                }

            if (healthText)
                {
                healthText.text = $"Health: {currentHealth}/{maxHealth}";
                }
            }

        public void ShowTooltip(DemoItem item, Vector3 position)
            {
            if (!tooltipPanel || !tooltipText || item == null) return;

            string tooltip = $"<b>{item.name}</b>\n{item.description}\nValue: {item.value}";

            if (item.type == ItemType.Weapon && item.weaponStats != null)
                {
                tooltip += $"\nDamage: {item.weaponStats.damage}";
                tooltip += $"\nRange: {item.weaponStats.range:F1}";
                tooltip += $"\nSpeed: {item.weaponStats.attackSpeed:F1}";
                }
            else if (item.type == ItemType.Armor && item.armorStats != null)
                {
                tooltip += $"\nDefense: {item.armorStats.defense}";
                tooltip += $"\nHealth: +{item.armorStats.healthBonus}";
                }

            tooltipText.text = tooltip;
            tooltipPanel.transform.position = position;
            tooltipPanel.SetActive(true);
            }

        public void HideTooltip()
            {
            if (tooltipPanel)
                {
                tooltipPanel.SetActive(false);
                }
            }

        // Public API
        public DemoPlayerInventory PlayerInventory => playerInventory;
        public bool IsInventoryOpen => isInventoryOpen;
        }

    /// <summary>
    /// Individual inventory slot UI component
    /// </summary>
    public class DemoInventorySlot : MonoBehaviour
        {
        private int slotIndex;
        private DemoItem currentItem;
        private DemoInventoryUI inventoryUI;
        private Image slotImage;
        private Text stackText;

        public void Initialize(int index, DemoInventoryUI ui)
            {
            slotIndex = index;
            inventoryUI = ui;
            slotImage = GetComponent<Image>();

            // Create stack text
            GameObject stackObj = new GameObject("StackText");
            stackObj.transform.SetParent(transform, false);

            stackText = stackObj.AddComponent<Text>();
            stackText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            stackText.fontSize = 10;
            stackText.color = Color.white;
            stackText.alignment = TextAnchor.LowerRight;

            var stackRect = stackObj.GetComponent<RectTransform>();
            stackRect.anchorMin = Vector2.zero;
            stackRect.anchorMax = Vector2.one;
            stackRect.offsetMin = Vector2.zero;
            stackRect.offsetMax = Vector2.zero;

            // Add event handling
            var button = gameObject.AddComponent<Button>();
            button.onClick.AddListener(OnSlotClicked);
            }

        public void SetItem(DemoItem item)
            {
            currentItem = item;

            if (item != null)
                {
                // Set slot color based on rarity
                slotImage.color = GetRarityColor(item.rarity);

                // Show stack count if > 1
                if (item.currentStack > 1)
                    {
                    stackText.text = item.currentStack.ToString();
                    }
                else
                    {
                    stackText.text = "";
                    }
                }
            else
                {
                slotImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                stackText.text = "";
                }
            }

        private Color GetRarityColor(ItemRarity rarity)
            {
            Color baseColor = rarity switch
                {
                    ItemRarity.Common => Color.white,
                    ItemRarity.Uncommon => Color.green,
                    ItemRarity.Rare => Color.blue,
                    ItemRarity.Epic => new Color(0.6f, 0f, 0.8f),
                    ItemRarity.Legendary => Color.yellow,
                    _ => Color.gray
                    };

            // Make it more subtle for background
            return new Color(baseColor.r * 0.3f, baseColor.g * 0.3f, baseColor.b * 0.3f, 1f);
            }

        private void OnSlotClicked()
            {
            if (currentItem == null) return;

            if (currentItem.type == ItemType.Consumable)
                {
                // Use consumable
                if (inventoryUI.PlayerInventory) inventoryUI.PlayerInventory.UseItem(currentItem);
                }
            else if (currentItem.type == ItemType.Weapon || currentItem.type == ItemType.Armor ||
                     currentItem.type == ItemType.Offhand || currentItem.type == ItemType.Trinket)
                {
                // Equip item
                if (inventoryUI.PlayerInventory) inventoryUI.PlayerInventory.EquipItem(currentItem);
                }
            }

        // Mouse hover for tooltip
        private void OnMouseEnter()
            {
            if (currentItem != null)
                {
                inventoryUI.ShowTooltip(currentItem, transform.position);
                }
            }

        private void OnMouseExit()
            {
            inventoryUI.HideTooltip();
            }
        }

    /// <summary>
    /// Equipment slot UI component
    /// </summary>
    public class DemoEquipmentSlot : MonoBehaviour
        {
        private EquipmentSlot slotType;
        private DemoItem equippedItem;
        private DemoInventoryUI inventoryUI;
        private Image slotImage;

        public void Initialize(EquipmentSlot type, DemoInventoryUI ui)
            {
            slotType = type;
            inventoryUI = ui;
            slotImage = GetComponent<Image>();

            // Add event handling
            var button = gameObject.AddComponent<Button>();
            button.onClick.AddListener(OnSlotClicked);
            }

        public void SetItem(DemoItem item)
            {
            equippedItem = item;

            if (item != null)
                {
                slotImage.color = GetRarityColor(item.rarity);
                }
            else
                {
                slotImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                }
            }

        private Color GetRarityColor(ItemRarity rarity)
            {
            return rarity switch
                {
                    ItemRarity.Common => new Color(0.5f, 0.5f, 0.5f, 1f),
                    ItemRarity.Uncommon => new Color(0.2f, 0.6f, 0.2f, 1f),
                    ItemRarity.Rare => new Color(0.2f, 0.2f, 0.8f, 1f),
                    ItemRarity.Epic => new Color(0.6f, 0.2f, 0.8f, 1f),
                    ItemRarity.Legendary => new Color(0.8f, 0.8f, 0.2f, 1f),
                    _ => new Color(0.3f, 0.3f, 0.3f, 1f)
                    };
            }

        private void OnSlotClicked()
            {
            if (equippedItem != null)
                {
                // Unequip item
                if (inventoryUI.PlayerInventory) inventoryUI.PlayerInventory.UnequipItem(slotType);
                }
            }

        // Mouse hover for tooltip
        private void OnMouseEnter()
            {
            if (equippedItem != null)
                {
                inventoryUI.ShowTooltip(equippedItem, transform.position);
                }
            }

        private void OnMouseExit()
            {
            inventoryUI.HideTooltip();
            }
        }
    }
