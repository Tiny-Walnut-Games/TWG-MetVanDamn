using UnityEngine;

namespace TinyWalnutGames.MetVanDAMN.Authoring
{
    /// <summary>
    /// Dungeon progression lock component - biome-themed locks that gate progress through floors.
    /// Each lock is tied to a specific floor and requires specific conditions to unlock.
    /// </summary>
    public class DungeonProgressionLock : MonoBehaviour
    {
        [Header("Lock Configuration")]
        [SerializeField] private int floorIndex;
        [SerializeField] private string lockName;
        [SerializeField] private bool isUnlocked = false;
        [SerializeField] private float interactionRange = 2f;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject unlockedEffect;
        [SerializeField] private AudioClip unlockSound;
        
        private DungeonDelveMode dungeonMode;
        private Transform playerTransform;
        private Renderer lockRenderer;
        private Material lockedMaterial;
        private Material unlockedMaterial;
        private bool playerInRange = false;
        
        public bool IsUnlocked => isUnlocked;
        public string LockName => lockName;
        public int FloorIndex => floorIndex;
        
        public void Initialize(int floor, string name, DungeonDelveMode mode)
        {
            floorIndex = floor;
            lockName = name;
            dungeonMode = mode;
            
            // Find player
            var playerMovement = FindObjectOfType<DemoPlayerMovement>();
            if (playerMovement)
            {
                playerTransform = playerMovement.transform;
            }
            
            // Setup materials
            lockRenderer = GetComponent<Renderer>();
            if (lockRenderer)
            {
                lockedMaterial = lockRenderer.material;
                unlockedMaterial = new Material(lockedMaterial);
                unlockedMaterial.color = Color.green;
            }
            
            Debug.Log($"🔒 Progression lock '{lockName}' initialized for floor {floor + 1}");
        }
        
        private void Update()
        {
            if (isUnlocked || playerTransform == null) return;
            
            // Check if player is in range
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            bool wasInRange = playerInRange;
            playerInRange = distance <= interactionRange;
            
            // Show interaction prompt when player enters range
            if (playerInRange && !wasInRange)
            {
                ShowInteractionPrompt();
            }
            else if (!playerInRange && wasInRange)
            {
                HideInteractionPrompt();
            }
            
            // Handle interaction input
            if (playerInRange && Input.GetKeyDown(KeyCode.E))
            {
                TryUnlock();
            }
        }
        
        private void ShowInteractionPrompt()
        {
            Debug.Log($"🔑 Press E to interact with {lockName}");
            // In a full implementation, this would show UI prompt
        }
        
        private void HideInteractionPrompt()
        {
            // Hide UI prompt
        }
        
        private void TryUnlock()
        {
            if (isUnlocked) return;
            
            // Check if player has the required conditions to unlock
            // For demo purposes, we'll unlock based on floor progression
            bool canUnlock = CheckUnlockConditions();
            
            if (canUnlock)
            {
                UnlockLock();
            }
            else
            {
                Debug.Log($"🚫 Cannot unlock {lockName} - requirements not met");
                ShowUnlockRequirements();
            }
        }
        
        private bool CheckUnlockConditions()
        {
            // For demo purposes, allow unlocking if the previous floor boss is defeated
            if (floorIndex == 0) return true; // First lock can always be unlocked
            
            if (dungeonMode && floorIndex > 0)
            {
                // Check if previous floor boss is defeated
                // This is a simplified check - in a full implementation, 
                // you might require specific items or achievements
                return dungeonMode.CurrentFloor >= floorIndex;
            }
            
            return true; // Allow unlocking for demo purposes
        }
        
        private void ShowUnlockRequirements()
        {
            switch (floorIndex)
            {
                case 0:
                    Debug.Log("💎 Requires: Prove your worth in the Crystal Caverns");
                    break;
                case 1:
                    Debug.Log("🔥 Requires: Flame essence from the Molten Depths");
                    break;
                case 2:
                    Debug.Log("🌌 Requires: Void energy from the dark realms");
                    break;
            }
        }
        
        private void UnlockLock()
        {
            isUnlocked = true;
            
            // Visual feedback
            if (lockRenderer && unlockedMaterial)
            {
                lockRenderer.material = unlockedMaterial;
            }
            
            // Audio feedback
            if (unlockSound)
            {
                AudioSource.PlayClipAtPoint(unlockSound, transform.position);
            }
            
            // Particle effect
            if (unlockedEffect)
            {
                Instantiate(unlockedEffect, transform.position, transform.rotation);
            }
            
            // Notify dungeon mode
            if (dungeonMode)
            {
                dungeonMode.OnProgressionLockUnlocked(floorIndex);
            }
            
            Debug.Log($"🔓 {lockName} has been unlocked!");
            
            // Hide interaction prompt
            HideInteractionPrompt();
        }
        
        /// <summary>
        /// Force unlock this lock (for testing or special conditions)
        /// </summary>
        public void ForceUnlock()
        {
            UnlockLock();
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw interaction range
            Gizmos.color = isUnlocked ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
    
    /// <summary>
    /// Dungeon secret component - hidden discoverable elements that provide rewards.
    /// Each secret is biome-themed and provides meaningful rewards for the current run.
    /// </summary>
    public class DungeonSecret : MonoBehaviour
    {
        [Header("Secret Configuration")]
        [SerializeField] private int floorIndex;
        [SerializeField] private int secretIndex;
        [SerializeField] private bool isDiscovered = false;
        [SerializeField] private float discoveryRange = 1.5f;
        
        [Header("Rewards")]
        [SerializeField] private int currencyReward = 50;
        [SerializeField] private bool grantsHealthBonus = true;
        [SerializeField] private bool grantsManaBonus = false;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject discoveryEffect;
        [SerializeField] private AudioClip discoverySound;
        
        private DungeonDelveMode dungeonMode;
        private Transform playerTransform;
        private Renderer secretRenderer;
        private Material hiddenMaterial;
        private Material discoveredMaterial;
        private float originalAlpha;
        
        public bool IsDiscovered => isDiscovered;
        public int FloorIndex => floorIndex;
        public int SecretIndex => secretIndex;
        
        public void Initialize(int floor, int index, DungeonDelveMode mode)
        {
            floorIndex = floor;
            secretIndex = index;
            dungeonMode = mode;
            
            // Find player
            var playerMovement = FindObjectOfType<DemoPlayerMovement>();
            if (playerMovement)
            {
                playerTransform = playerMovement.transform;
            }
            
            // Setup materials for hidden state
            secretRenderer = GetComponent<Renderer>();
            if (secretRenderer)
            {
                hiddenMaterial = secretRenderer.material;
                originalAlpha = hiddenMaterial.color.a;
                
                // Make secrets semi-transparent initially
                var hiddenColor = hiddenMaterial.color;
                hiddenColor.a = 0.3f;
                hiddenMaterial.color = hiddenColor;
                
                discoveredMaterial = new Material(hiddenMaterial);
                var discoveredColor = discoveredMaterial.color;
                discoveredColor.a = 1f;
                discoveredColor = Color.yellow; // Bright color when discovered
                discoveredMaterial.color = discoveredColor;
            }
            
            Debug.Log($"🔍 Secret {index} initialized on floor {floor + 1}");
        }
        
        private void Update()
        {
            if (isDiscovered || playerTransform == null) return;
            
            // Check if player is close enough to discover the secret
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distance <= discoveryRange)
            {
                DiscoverSecret();
            }
        }
        
        private void DiscoverSecret()
        {
            if (isDiscovered) return;
            
            isDiscovered = true;
            
            // Visual feedback
            if (secretRenderer && discoveredMaterial)
            {
                secretRenderer.material = discoveredMaterial;
            }
            
            // Audio feedback
            if (discoverySound)
            {
                AudioSource.PlayClipAtPoint(discoverySound, transform.position);
            }
            
            // Particle effect
            if (discoveryEffect)
            {
                Instantiate(discoveryEffect, transform.position, transform.rotation);
            }
            
            // Apply rewards
            ApplySecretRewards();
            
            // Notify dungeon mode
            if (dungeonMode)
            {
                dungeonMode.OnSecretDiscovered(floorIndex, secretIndex);
            }
            
            Debug.Log($"🌟 Secret discovered on floor {floorIndex + 1}! Rewards granted.");
        }
        
        private void ApplySecretRewards()
        {
            var playerInventory = FindObjectOfType<DemoPlayerInventory>();
            var playerCombat = FindObjectOfType<DemoPlayerCombat>();
            
            // Currency reward
            if (currencyReward > 0)
            {
                Debug.Log($"💰 Gained {currencyReward} currency from secret!");
                // In full implementation, would add to player inventory
            }
            
            // Health bonus
            if (grantsHealthBonus)
            {
                Debug.Log("❤️ Gained permanent health bonus from secret!");
                // In full implementation, would increase max health
            }
            
            // Mana bonus
            if (grantsManaBonus)
            {
                Debug.Log("💙 Gained permanent mana bonus from secret!");
                // In full implementation, would increase max mana
            }
            
            // Biome-specific rewards based on floor
            ApplyBiomeSpecificReward();
        }
        
        private void ApplyBiomeSpecificReward()
        {
            switch (floorIndex)
            {
                case 0: // Crystal Caverns
                    Debug.Log("💎 Crystal power enhances your defenses!");
                    break;
                case 1: // Molten Depths
                    Debug.Log("🔥 Flame essence increases your attack power!");
                    break;
                case 2: // Void Sanctum
                    Debug.Log("🌌 Void energy grants mystical abilities!");
                    break;
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw discovery range
            Gizmos.color = isDiscovered ? Color.yellow : new Color(1f, 1f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, discoveryRange);
        }
    }
    
    /// <summary>
    /// Dungeon pickup component - functional pickup items integrated with inventory systems.
    /// All pickups are biome-themed and provide meaningful benefits for the current run.
    /// </summary>
    public class DungeonPickup : MonoBehaviour
    {
        [Header("Pickup Configuration")]
        [SerializeField] private PickupType pickupType;
        [SerializeField] private int value = 1;
        [SerializeField] private bool isCollected = false;
        [SerializeField] private float collectionRange = 1f;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject collectionEffect;
        [SerializeField] private AudioClip collectionSound;
        [SerializeField] private float bobHeight = 0.5f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float rotationSpeed = 90f;
        
        private DungeonDelveMode dungeonMode;
        private Transform playerTransform;
        private Vector3 originalPosition;
        private float bobOffset;
        
        public PickupType Type => pickupType;
        public bool IsCollected => isCollected;
        public int Value => value;
        
        public void Initialize(PickupType type, DungeonDelveMode mode)
        {
            pickupType = type;
            dungeonMode = mode;
            
            // Find player
            var playerMovement = FindObjectOfType<DemoPlayerMovement>();
            if (playerMovement)
            {
                playerTransform = playerMovement.transform;
            }
            
            // Setup animation
            originalPosition = transform.position;
            bobOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            
            // Set value based on type
            SetPickupValue();
            
            Debug.Log($"💎 {pickupType} pickup initialized with value {value}");
        }
        
        private void SetPickupValue()
        {
            switch (pickupType)
            {
                case PickupType.Health:
                    value = 25; // Health points to restore
                    break;
                case PickupType.Mana:
                    value = 20; // Mana points to restore
                    break;
                case PickupType.Currency:
                    value = UnityEngine.Random.Range(10, 30); // Random currency amount
                    break;
                case PickupType.Equipment:
                    value = 1; // Equipment piece
                    break;
                case PickupType.Consumable:
                    value = 1; // Consumable item
                    break;
            }
        }
        
        private void Update()
        {
            if (isCollected) return;
            
            // Animate the pickup (bobbing and rotation)
            AnimatePickup();
            
            // Check for collection
            if (playerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                if (distance <= collectionRange)
                {
                    CollectPickup();
                }
            }
        }
        
        private void AnimatePickup()
        {
            // Bobbing motion
            float bobY = Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight;
            transform.position = originalPosition + Vector3.up * bobY;
            
            // Rotation
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
        
        private void CollectPickup()
        {
            if (isCollected) return;
            
            isCollected = true;
            
            // Visual feedback
            if (collectionEffect)
            {
                Instantiate(collectionEffect, transform.position, transform.rotation);
            }
            
            // Audio feedback
            if (collectionSound)
            {
                AudioSource.PlayClipAtPoint(collectionSound, transform.position);
            }
            
            // Apply pickup effects
            ApplyPickupEffect();
            
            // Notify dungeon mode
            if (dungeonMode)
            {
                dungeonMode.OnPickupCollected(pickupType);
            }
            
            Debug.Log($"✨ Collected {pickupType} pickup (value: {value})");
            
            // Destroy the pickup object
            Destroy(gameObject);
        }
        
        private void ApplyPickupEffect()
        {
            var playerInventory = FindObjectOfType<DemoPlayerInventory>();
            var playerCombat = FindObjectOfType<DemoPlayerCombat>();
            
            switch (pickupType)
            {
                case PickupType.Health:
                    Debug.Log($"❤️ Restored {value} health points!");
                    // In full implementation: playerCombat.RestoreHealth(value);
                    break;
                    
                case PickupType.Mana:
                    Debug.Log($"💙 Restored {value} mana points!");
                    // In full implementation: playerCombat.RestoreMana(value);
                    break;
                    
                case PickupType.Currency:
                    Debug.Log($"💰 Gained {value} currency!");
                    // In full implementation: playerInventory.AddCurrency(value);
                    break;
                    
                case PickupType.Equipment:
                    Debug.Log("⚔️ Found new equipment!");
                    // In full implementation: playerInventory.AddEquipment(GenerateRandomEquipment());
                    break;
                    
                case PickupType.Consumable:
                    Debug.Log("🧪 Found consumable item!");
                    // In full implementation: playerInventory.AddConsumable(GenerateRandomConsumable());
                    break;
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw collection range
            Gizmos.color = isCollected ? Color.gray : Color.green;
            Gizmos.DrawWireSphere(transform.position, collectionRange);
        }
    }
}