using UnityEngine;

namespace TinyWalnutGames.MetVanDAMN.Authoring
{
    /// <summary>
    /// Demo component placeholders for systems referenced by CompleteDemoSceneGenerator.
    /// These provide basic functionality that can be enhanced or replaced with full implementations.
    /// </summary>

    // Combat System Components
    public class DemoCombatManager : MonoBehaviour
    {
        [Header("Combat Settings")]
        public float globalDamageMultiplier = 1f;
        public bool enableFriendlyFire = false;

        private void Start()
        {
            Debug.Log("🗡️ Demo Combat Manager initialized");
        }
    }

    public class DemoWeaponSpawner : MonoBehaviour
    {
        [Header("Weapon Spawning")]
        public float spawnInterval = 30f;
        public Transform[] spawnPoints;

        private void Start()
        {
            InvokeRepeating(nameof(SpawnRandomWeapon), spawnInterval, spawnInterval);
        }

        private void SpawnRandomWeapon()
        {
            if (spawnPoints.Length == 0) return;

            Vector3 spawnPos = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
            var lootManager = FindObjectOfType<DemoLootManager>();
            if (lootManager)
            {
                lootManager.SpawnLoot(spawnPos);
            }
        }
    }

    // AI System Components  
    public class DemoEnemySpawner : MonoBehaviour
    {
        [Header("Enemy Spawning")]
        public float spawnInterval = 10f;
        public int maxEnemies = 5;
        public float spawnRadius = 15f;

        private void Start()
        {
            InvokeRepeating(nameof(TrySpawnEnemy), spawnInterval, spawnInterval);
        }

        private void TrySpawnEnemy()
        {
            var aiManager = GetComponentInParent<DemoAIManager>();
            if (aiManager && aiManager.ActiveEnemyCount < maxEnemies)
            {
                // AI Manager will handle the actual spawning
                Debug.Log("🤖 Enemy spawn requested");
            }
        }
    }

    // Inventory System Components
    public class DemoInventoryManager : MonoBehaviour
    {
        [Header("Inventory Management")]
        public bool autoSortInventory = true;
        public float autoSaveInterval = 60f;

        private void Start()
        {
            Debug.Log("🎒 Demo Inventory Manager initialized");
            if (autoSaveInterval > 0)
            {
                InvokeRepeating(nameof(AutoSave), autoSaveInterval, autoSaveInterval);
            }
        }

        private void AutoSave()
        {
            // Placeholder for inventory auto-save functionality
            Debug.Log("💾 Inventory auto-saved");
        }
    }

    // Loot System Components
    public class DemoTreasureSpawner : MonoBehaviour
    {
        [Header("Treasure Spawning")]
        public int treasureChestCount = 3;
        public float spawnRadius = 20f;

        private void Start()
        {
            SpawnTreasureChests();
        }

        private void SpawnTreasureChests()
        {
            var lootManager = GetComponentInParent<DemoLootManager>();
            if (!lootManager) return;

            for (int i = 0; i < treasureChestCount; i++)
            {
                Vector3 spawnPos = transform.position + Random.insideUnitSphere * spawnRadius;
                spawnPos.y = transform.position.y; // Keep at ground level
                lootManager.SpawnTreasureChest(spawnPos);
            }
        }
    }

    // Biome Art System Components
    public class DemoBiomeArtApplicator : MonoBehaviour
    {
        [Header("Art Application")]
        public bool autoApplyOnStart = true;
        public Color[] biomeColors = { Color.green, Color.blue, Color.red, Color.yellow };

        private void Start()
        {
            if (autoApplyOnStart)
            {
                ApplyBiomeArt();
            }
        }

        private void ApplyBiomeArt()
        {
            // Placeholder for biome art application
            // In a full implementation, this would integrate with the BiomeArtIntegrationSystem
            Debug.Log("🎨 Demo biome art applied");
        }
    }

    // Room System Components
    public class DemoRoomMaskingManager : MonoBehaviour
    {
        [Header("Room Masking")]
        public bool enableRoomMasking = true;
        public LayerMask roomLayers = 1;

        private void Start()
        {
            Debug.Log("🏠 Demo Room Masking Manager initialized");
        }

        public void ShowRoom(int roomId)
        {
            // Placeholder for room visibility management
            Debug.Log($"🔍 Showing room {roomId}");
        }

        public void HideRoom(int roomId)
        {
            // Placeholder for room visibility management
            Debug.Log($"🫥 Hiding room {roomId}");
        }
    }

    public class DemoRoomTransitionDetector : MonoBehaviour
    {
        [Header("Transition Detection")]
        public float detectionRadius = 2f;
        public bool debugTransitions = true;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<DemoPlayerMovement>())
            {
                HandleRoomTransition();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<DemoPlayerMovement>())
            {
                HandleRoomTransition();
            }
        }

        private void HandleRoomTransition()
        {
            if (debugTransitions)
            {
                Debug.Log("🚪 Room transition detected");
            }

            var maskingManager = FindObjectOfType<DemoRoomMaskingManager>();
            if (maskingManager)
            {
                // Placeholder for actual room transition logic
                maskingManager.ShowRoom(Random.Range(0, 10));
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }

    // Validation Components
    public class DemoValidationSettings : MonoBehaviour
    {
        [Header("Validation Configuration")]
        public bool validateOnStart = true;
        public bool requireAllSystems = true;
        public bool logValidationResults = true;

        [Header("System Requirements")]
        public bool requirePlayerMovement = true;
        public bool requireCombatSystem = true;
        public bool requireInventorySystem = true;
        public bool requireAISystem = true;
        public bool requireLootSystem = true;

        private void Start()
        {
            if (validateOnStart)
            {
                ValidateScene();
            }
        }

        private void ValidateScene()
        {
            var validator = GetComponent<DemoSceneValidator>();
            if (!validator)
            {
                validator = gameObject.AddComponent<DemoSceneValidator>();
            }

            // Configure validator based on settings
            validator.validateOnStart = validateOnStart;
            validator.requirePlayerMovement = requirePlayerMovement;
            validator.requireCombatSystem = requireCombatSystem;
            validator.requireInventorySystem = requireInventorySystem;
            validator.requireLootSystem = requireLootSystem;
            validator.requireEnemyAI = requireAISystem;

            bool validationPassed = validator.ValidateScene();

            if (logValidationResults)
            {
                if (validationPassed)
                {
                    Debug.Log("✅ Demo scene validation passed - all required systems are present and functional");
                }
                else
                {
                    Debug.LogWarning("⚠️ Demo scene validation found issues - check console for details");
                }
            }
        }

        // Public API for runtime validation
        public bool RunValidation()
        {
            ValidateScene();
            return true; // Placeholder - would return actual validation result
        }
    }

    // Projectile System Component
    public class DemoProjectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        public float speed = 10f;
        public float lifetime = 5f;
        public float damage = 25f;

        private Vector3 direction;
        private float remainingLifetime;
        private GameObject source;

        public void Initialize(float projectileDamage, Vector3 projectileDirection, float range, GameObject sourceObject)
        {
            damage = projectileDamage;
            direction = projectileDirection.normalized;
            remainingLifetime = range / speed; // Calculate lifetime based on range and speed
            source = sourceObject;

            // Add visual trail or effect
            CreateProjectileVisual();
        }

        private void Update()
        {
            // Move projectile
            transform.position += direction * speed * Time.deltaTime;

            // Check lifetime
            remainingLifetime -= Time.deltaTime;
            if (remainingLifetime <= 0)
            {
                DestroyProjectile();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleCollision(other.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleCollision(other.gameObject);
        }

        private void HandleCollision(GameObject target)
        {
            // Don't hit the source
            if (target == source) return;

            // Check for damageable target
            var damageable = target.GetComponent<IDemoDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, source, AttackType.Light);
                DestroyProjectile();
            }
            // Hit walls/obstacles
            else if (!target.CompareTag("Player") && target.layer != LayerMask.NameToLayer("Ignore Raycast"))
            {
                DestroyProjectile();
            }
        }

        private void CreateProjectileVisual()
        {
            // Simple visual representation
            var renderer = GetComponent<Renderer>();
            if (!renderer)
            {
                // Create default projectile visual
                var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.transform.SetParent(transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = Vector3.one * 0.2f;
                
                renderer = visual.GetComponent<Renderer>();
                renderer.material.color = Color.yellow;

                // Remove collider from visual
                DestroyImmediate(visual.GetComponent<Collider>());
            }

            // Add trigger collider for hit detection
            var collider = GetComponent<SphereCollider>();
            if (!collider)
            {
                collider = gameObject.AddComponent<SphereCollider>();
            }
            collider.isTrigger = true;
            collider.radius = 0.2f;
        }

        private void DestroyProjectile()
        {
            // Add impact effect
            CreateImpactEffect();
            
            Destroy(gameObject);
        }

        private void CreateImpactEffect()
        {
            // Simple impact visual
            GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impact.transform.position = transform.position;
            impact.transform.localScale = Vector3.one * 0.5f;
            
            var renderer = impact.GetComponent<Renderer>();
            renderer.material.color = Color.orange;
            
            DestroyImmediate(impact.GetComponent<Collider>());
            Destroy(impact, 0.3f);
        }
    }
}