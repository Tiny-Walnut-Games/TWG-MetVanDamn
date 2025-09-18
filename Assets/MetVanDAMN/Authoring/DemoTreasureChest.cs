using UnityEngine;
using System.Collections.Generic;

namespace TinyWalnutGames.MetVanDAMN.Authoring
    {
    /// <summary>
    /// Interactive treasure chest that can be opened to reveal loot.
    /// Implements the IDemoInteractable interface for player interaction.
    /// </summary>
    public class DemoTreasureChest : MonoBehaviour, IDemoInteractable
        {
        [Header("Chest Settings")]
        public bool isLocked = false;
        public string requiredKey = "";
        public bool isOpened = false;
        public float openAnimationTime = 1f;

        [Header("Visual Settings")]
        public GameObject closedModel; // Created in SetupVisuals if not assigned
        public GameObject openedModel; // Created in SetupVisuals if not assigned
        public ParticleSystem openEffect; // Placeholder created in Awake if absent
        public AudioClip openSound; // Optional – absence represented by empty clip sentinel

        [Header("Interaction")]
        public float interactionRange = 2f;
        public KeyCode interactKey = KeyCode.E;

        // Private state
        private List<DemoItem> chestContents = new();
        private DemoLootManager lootManager = null!; // Set during Initialize or replaced with sentinel
        private bool isAnimating = false;
        private AudioSource audioSource = null!;

        // UI Elements (never null after Awake)
        private GameObject interactionPrompt = null!;
        private UnityEngine.UI.Text interactionPromptText = null!;

        // Internal sentinels
        // Empty clip sentinel avoided (Unity best practice: just branch on openSound reference)
        private bool _baselineReady;
        private bool _initializedWithContents;

        private ParticleSystem _placeholderEffect = null!; // track placeholder so we can detect real assignment

        private void Awake()
            {
            // Deterministic baseline so no code path relies on null checks
            EnsureBaselineInfrastructure();
            }

        private void EnsureBaselineInfrastructure()
            {
            if (_baselineReady) return;

            // Audio Source always present
            audioSource = GetComponent<AudioSource>();
            if (!audioSource)
                {
                audioSource = gameObject.AddComponent<AudioSource>();
                }

            // Placeholder particle effect so callers can always invoke .Play() guarded by reference equality if needed
            if (!openEffect)
                {
                var effectGO = new GameObject("__ChestOpenEffect_Placeholder__");
                effectGO.transform.SetParent(transform);
                _placeholderEffect = effectGO.AddComponent<ParticleSystem>();
                openEffect = _placeholderEffect;
                var main = openEffect.main; // tweak so it never visibly emits
                main.loop = false;
                effectGO.SetActive(false);
                }
            else
                {
                _placeholderEffect = openEffect; // treat assigned as non-placeholder for equality logic
                }

            // Interaction prompt always exists (text updated later)
            if (!interactionPrompt)
                {
                interactionPrompt = new GameObject("InteractionPrompt");
                interactionPrompt.transform.SetParent(transform);
                interactionPrompt.transform.localPosition = Vector3.up * 2f;

                var canvas = interactionPrompt.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = Camera.main;

                interactionPromptText = interactionPrompt.AddComponent<UnityEngine.UI.Text>();
                interactionPromptText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                interactionPromptText.fontSize = 24;
                interactionPromptText.color = Color.white;
                interactionPromptText.alignment = TextAnchor.MiddleCenter;
                var rectTransform = interactionPromptText.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(200, 50);
                interactionPrompt.SetActive(false);
                }
            else if (!interactionPromptText)
                {
                interactionPromptText = interactionPrompt.GetComponent<UnityEngine.UI.Text>();
                if (!interactionPromptText)
                    {
                    interactionPromptText = interactionPrompt.AddComponent<UnityEngine.UI.Text>();
                    interactionPromptText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    interactionPromptText.fontSize = 24;
                    interactionPromptText.color = Color.white;
                    interactionPromptText.alignment = TextAnchor.MiddleCenter;
                    }
                }

            // Ensure trigger collider exists early so player proximity works even if external Initialize missed
            var triggerCollider = gameObject.GetComponent<SphereCollider>();
            if (!triggerCollider)
                {
                triggerCollider = gameObject.AddComponent<SphereCollider>();
                }
            triggerCollider.isTrigger = true;
            triggerCollider.radius = interactionRange;

            // Visuals may be constructed later; safe to call to guarantee models if absent
            SetupVisuals();

            _baselineReady = true;
            }

        public void Initialize(List<DemoItem> contents, DemoLootManager manager)
            {
            // set _initializedWithContents to false before initializing.
            _initializedWithContents = false;
            // External initialization of runtime data; baseline already ensured in Awake
            EnsureBaselineInfrastructure();
            chestContents = contents ?? chestContents; // never null after
            lootManager = manager;
            // Basic validation: consider initialized if we have at least one non-null item
            _initializedWithContents = chestContents.Count > 0 && chestContents.TrueForAll(i => i != null);
            // Update prompt text to reflect potential locked state
            interactionPromptText.text = GetInteractionText();
            }

        private void SetupVisuals()
            {
            // Ensure we have basic visual representation
            if (!closedModel && !openedModel)
                {
                // Create default chest visuals
                closedModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                closedModel.name = "ClosedChest";
                closedModel.transform.SetParent(transform);
                closedModel.transform.localPosition = Vector3.zero;
                closedModel.transform.localScale = new Vector3(1.5f, 1f, 1f);
                closedModel.GetComponent<Renderer>().material.color = new Color(0.6f, 0.3f, 0.1f); // Brown

                openedModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                openedModel.name = "OpenedChest";
                openedModel.transform.SetParent(transform);
                openedModel.transform.localPosition = Vector3.zero;
                openedModel.transform.localScale = new Vector3(1.5f, 0.8f, 1f);
                openedModel.GetComponent<Renderer>().material.color = new Color(0.8f, 0.4f, 0.2f); // Lighter brown

                // Remove colliders from visual objects
                DestroyImmediate(closedModel.GetComponent<Collider>());
                DestroyImmediate(openedModel.GetComponent<Collider>());
                }

            // Set initial state
            UpdateVisualState();
            }

        private void UpdateVisualState()
            {
            if (closedModel) closedModel.SetActive(!isOpened);
            if (openedModel) openedModel.SetActive(isOpened);
            }

        private void CreateInteractionTrigger() { /* legacy path retained for backward compatibility; collider ensured in EnsureBaselineInfrastructure */ }

        private void CreateInteractionPrompt() { /* legacy path; prompt created in EnsureBaselineInfrastructure */ }

        private string GetInteractionText()
            {
            if (isOpened)
                {
                return "";
                }
            else if (isLocked)
                {
                return $"[{interactKey}] Unlock (Need: {requiredKey})";
                }
            else
                {
                return $"[{interactKey}] Open Chest";
                }
            }

        private void OnTriggerEnter(Collider other)
            {
            if (other.GetComponent<DemoPlayerMovement>())
                {
                ShowInteractionPrompt();
                }
            }

        private void OnTriggerEnter2D(Collider2D other)
            {
            if (other.GetComponent<DemoPlayerMovement>())
                {
                ShowInteractionPrompt();
                }
            }

        private void OnTriggerExit(Collider other)
            {
            if (other.GetComponent<DemoPlayerMovement>())
                {
                HideInteractionPrompt();
                }
            }

        private void OnTriggerExit2D(Collider2D other)
            {
            if (other.GetComponent<DemoPlayerMovement>())
                {
                HideInteractionPrompt();
                }
            }

        private void ShowInteractionPrompt()
            {
            if (isOpened) return;
            interactionPromptText.text = GetInteractionText();
            interactionPrompt.SetActive(true);
            }

        private void HideInteractionPrompt() => interactionPrompt.SetActive(false);

        public void Interact(DemoPlayerMovement player)
            {
            if (isOpened || isAnimating) return;

            if (isLocked)
                {
                TryUnlock(player);
                }
            else
                {
                OpenChest(player);
                }
            }

        private void TryUnlock(DemoPlayerMovement player)
            {
            var inventory = player.GetComponent<DemoPlayerInventory>();
            if (inventory && inventory.HasItem(requiredKey))
                {
                // Consume key and unlock
                inventory.RemoveItem(requiredKey);
                isLocked = false;

                Debug.Log($"Chest unlocked with {requiredKey}!");

                // Open immediately after unlocking
                OpenChest(player);
                }
            else
                {
                Debug.Log($"This chest is locked. You need: {requiredKey}");
                }
            }

        private void OpenChest(DemoPlayerMovement player)
            {
            if (isOpened || isAnimating) return;

            StartCoroutine(OpenChestAnimation(player));
            }

        private System.Collections.IEnumerator OpenChestAnimation(DemoPlayerMovement player)
            {
            isAnimating = true;

            // Play opening sound
            if (openSound)
                {
                audioSource.PlayOneShot(openSound);
                }

            // Visual effect
            // Only play if not placeholder (active self check)
            if (openEffect && openEffect != _placeholderEffect)
                {
                openEffect.Play();
                }

            // Animate opening (simple scale animation)
            if (closedModel)
                {
                Vector3 originalScale = closedModel.transform.localScale;
                float elapsed = 0f;

                while (elapsed < openAnimationTime)
                    {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / openAnimationTime;

                    // Simple bob animation
                    closedModel.transform.localPosition = Vector3.up * Mathf.Sin(progress * Mathf.PI) * 0.2f;

                    yield return null;
                    }

                closedModel.transform.localPosition = Vector3.zero;
                }

            // Mark as opened and update visuals
            isOpened = true;
            UpdateVisualState();

            // Give loot to player
            GiveLootToPlayer(player);

            // Hide interaction prompt
            HideInteractionPrompt();

            // Notify loot manager
            if (lootManager != null)
                {
                lootManager.OnTreasureChestOpened(this);
                }

            isAnimating = false;
            }

        private void GiveLootToPlayer(DemoPlayerMovement player)
            {
            var inventory = player.GetComponent<DemoPlayerInventory>();
            if (!inventory)
                {
                // Drop items on ground if no inventory
                DropLootOnGround();
                return;
                }

            List<DemoItem> failedItems = new List<DemoItem>();

            foreach (var item in chestContents)
                {
                if (!inventory.AddItem(item))
                    {
                    failedItems.Add(item);
                    }
                else
                    {
                    Debug.Log($"Found: {item.name}!");
                    }
                }

            // Drop items that couldn't fit in inventory
            foreach (var item in failedItems)
                {
                // Create pickup for overflow items (lootManager may be sentinel if Initialize not called)
                if (lootManager != null)
                    {
                    Vector3 dropPos = transform.position + Random.insideUnitSphere * 1.5f;
                    dropPos.y = transform.position.y;
                    lootManager.SpawnLoot(dropPos);
                    }
                }

            // Clear contents
            chestContents.Clear();
            }

        private void DropLootOnGround()
            {
            foreach (var item in chestContents)
                {
                if (lootManager != null)
                    {
                    Vector3 dropPos = transform.position + Random.insideUnitSphere * 1.5f;
                    dropPos.y = transform.position.y;
                    lootManager.SpawnLoot(dropPos);
                    }
                }

            chestContents.Clear();
            }

        // Public API
        public bool IsOpened => isOpened;
        public bool IsLocked => isLocked;
        public List<DemoItem> GetContents() => new(chestContents);

        public void SetLocked(bool locked, string keyRequired = "")
            {
            isLocked = locked;
            requiredKey = keyRequired;

            // Update interaction prompt if visible
            if (interactionPrompt && interactionPrompt.activeInHierarchy)
                {
                var text = interactionPrompt.GetComponent<UnityEngine.UI.Text>();
                if (text) text.text = GetInteractionText();
                }
            }

        // Gizmos for debugging
        private void OnDrawGizmosSelected()
            {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
            }
        }

    /// <summary>
    /// Loot pickup component for items dropped in the world.
    /// Auto-collects when player walks over or can be manually picked up.
    /// </summary>
    public class DemoLootPickup : MonoBehaviour
        {
        [Header("Pickup Settings")]
        public bool autoCollect = true;
        public float collectRange = 1.5f;
        public float bobSpeed = 2f;
        public float bobHeight = 0.3f;
        public float rotateSpeed = 90f;

        [Header("Visual Effects")]
        public ParticleSystem collectEffect;
        public AudioClip collectSound;

        // Private state
        private DemoItem containedItem = null!; // set in Initialize
        private DemoLootManager lootManager = null!; // set in Initialize
        private Vector3 basePosition;
        private AudioSource audioSource;
        private bool isCollected = false;

        // UI Elements
        private GameObject itemNameDisplay = null!; // created in Initialize

        public void Initialize(DemoItem item, DemoLootManager manager)
            {
            containedItem = item;
            lootManager = manager;
            basePosition = transform.position;

            audioSource = GetComponent<AudioSource>();
            if (!audioSource)
                {
                audioSource = gameObject.AddComponent<AudioSource>();
                }

            SetupVisuals();
            CreateInteractionTrigger();
            CreateItemNameDisplay();
            }

        private void SetupVisuals()
            {
            // Set pickup color based on item rarity
            var renderer = GetComponent<Renderer>();
            if (renderer && containedItem != null)
                {
                renderer.material.color = GetRarityColor(containedItem.rarity);
                }
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

        private void CreateInteractionTrigger()
            {
            var triggerCollider = GetComponent<SphereCollider>();
            if (!triggerCollider)
                {
                triggerCollider = gameObject.AddComponent<SphereCollider>();
                }

            triggerCollider.isTrigger = true;
            triggerCollider.radius = collectRange;
            }

        private void CreateItemNameDisplay()
            {
            if (containedItem == null) return;

            itemNameDisplay = new GameObject("ItemNameDisplay");
            itemNameDisplay.transform.SetParent(transform);
            itemNameDisplay.transform.localPosition = Vector3.up * 1.5f;

            var canvas = itemNameDisplay.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            var text = itemNameDisplay.AddComponent<UnityEngine.UI.Text>();
            text.text = containedItem.name;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 16;
            text.color = GetRarityColor(containedItem.rarity);
            text.alignment = TextAnchor.MiddleCenter;

            var rectTransform = text.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(150, 30);

            // Initially hidden
            itemNameDisplay.SetActive(false);
            }

        private void Update()
            {
            if (isCollected) return;

            // Bob animation
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = basePosition + Vector3.up * bobOffset;

            // Rotation animation
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
            }

        private void OnTriggerEnter(Collider other)
            {
            HandlePlayerInteraction(other.gameObject);
            }

        private void OnTriggerEnter2D(Collider2D other)
            {
            HandlePlayerInteraction(other.gameObject);
            }

        private void HandlePlayerInteraction(GameObject other)
            {
            if (isCollected) return;

            var player = other.GetComponent<DemoPlayerMovement>();
            if (player)
                {
                if (autoCollect)
                    {
                    CollectItem(player);
                    }
                else
                    {
                    ShowItemName();
                    }
                }
            }

        private void OnTriggerExit(Collider other)
            {
            if (other.GetComponent<DemoPlayerMovement>())
                {
                HideItemName();
                }
            }

        private void OnTriggerExit2D(Collider2D other)
            {
            if (other.GetComponent<DemoPlayerMovement>())
                {
                HideItemName();
                }
            }

        private void ShowItemName()
            {
            if (itemNameDisplay)
                {
                itemNameDisplay.SetActive(true);
                }
            }

        private void HideItemName()
            {
            if (itemNameDisplay)
                {
                itemNameDisplay.SetActive(false);
                }
            }

        public void CollectItem(DemoPlayerMovement player)
            {
            if (isCollected || containedItem == null) return;

            var inventory = player.GetComponent<DemoPlayerInventory>();
            if (inventory && inventory.AddItem(containedItem))
                {
                // Successfully added to inventory
                isCollected = true;

                Debug.Log($"Picked up: {containedItem.name}");

                // Play collect effect
                if (collectEffect)
                    {
                    collectEffect.Play();
                    }

                if (collectSound && audioSource)
                    {
                    audioSource.PlayOneShot(collectSound);
                    }

                // Notify loot manager
                if (lootManager)
                    {
                    lootManager.OnLootPickedUp(this);
                    }

                // Destroy pickup after a short delay (for sound/effects)
                Destroy(gameObject, 0.5f);
                }
            else
                {
                // Inventory full
                Debug.Log("Inventory is full!");
                ShowItemName(); // Show name to indicate item is there
                }
            }

        // Public API
        public DemoItem GetItem() => containedItem;
        public bool IsCollected => isCollected;

        // Gizmos for debugging
        private void OnDrawGizmosSelected()
            {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, collectRange);
            }
        }
    }
