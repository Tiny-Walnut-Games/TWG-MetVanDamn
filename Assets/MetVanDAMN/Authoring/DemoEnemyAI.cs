using UnityEngine;
using System.Collections.Generic;

namespace TinyWalnutGames.MetVanDAMN.Authoring
{
    /// <summary>
    /// Complete AI system managing enemy behavior and boss mechanics.
    /// Supports patrol/chase, ranged kite, melee brute, and support caster AI types.
    /// </summary>
    public class DemoAIManager : MonoBehaviour
    {
        [Header("AI Settings")]
        public GameObject[] enemyPrefabs;
        public GameObject[] bossPrefabs;
        public Transform player;
        public float spawnRadius = 20f;
        public int maxEnemies = 10;
        public float spawnCooldown = 5f;

        // AI Management
        private List<DemoEnemyAI> activeEnemies = new List<DemoEnemyAI>();
        private List<DemoBossAI> activeBosses = new List<DemoBossAI>();
        private float lastSpawnTime;

        private void Start()
        {
            // Find player if not assigned
            if (player == null)
            {
                var playerMovement = FindObjectOfType<DemoPlayerMovement>();
                if (playerMovement)
                {
                    player = playerMovement.transform;
                }
            }

            // Spawn initial enemies
            SpawnInitialEnemies();
        }

        private void Update()
        {
            ManageEnemySpawning();
            CleanupDeadEnemies();
        }

        private void SpawnInitialEnemies()
        {
            int initialCount = Mathf.Min(3, maxEnemies);
            for (int i = 0; i < initialCount; i++)
            {
                SpawnRandomEnemy();
            }
        }

        private void ManageEnemySpawning()
        {
            if (Time.time - lastSpawnTime > spawnCooldown && activeEnemies.Count < maxEnemies)
            {
                SpawnRandomEnemy();
                lastSpawnTime = Time.time;
            }
        }

        private void SpawnRandomEnemy()
        {
            if (enemyPrefabs.Length == 0 || !player) return;

            // Choose random enemy type
            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            
            // Find spawn position around player
            Vector3 spawnPos = GetRandomSpawnPosition();
            
            GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            var aiComponent = enemy.GetComponent<DemoEnemyAI>();
            
            if (!aiComponent)
            {
                aiComponent = enemy.AddComponent<DemoEnemyAI>();
            }
            
            aiComponent.Initialize(player, this);
            activeEnemies.Add(aiComponent);
        }

        public void SpawnBoss(int bossIndex = -1)
        {
            if (bossPrefabs.Length == 0 || !player) return;

            // Choose boss (random if not specified)
            if (bossIndex < 0 || bossIndex >= bossPrefabs.Length)
            {
                bossIndex = Random.Range(0, bossPrefabs.Length);
            }

            GameObject prefab = bossPrefabs[bossIndex];
            Vector3 spawnPos = player.position + Vector3.up * 2f; // Spawn above player
            
            GameObject boss = Instantiate(prefab, spawnPos, Quaternion.identity);
            var bossComponent = boss.GetComponent<DemoBossAI>();
            
            if (!bossComponent)
            {
                bossComponent = boss.AddComponent<DemoBossAI>();
            }
            
            bossComponent.Initialize(player, this);
            activeBosses.Add(bossComponent);
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 randomDirection = Random.insideUnitCircle.normalized;
            Vector3 spawnPos = player.position + (Vector3)randomDirection * spawnRadius;
            
            // Ensure spawn position is valid (not inside walls, etc.)
            // This is a simple implementation - could be enhanced with navmesh sampling
            return spawnPos;
        }

        private void CleanupDeadEnemies()
        {
            activeEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead);
            activeBosses.RemoveAll(boss => boss == null || boss.IsDead);
        }

        public void RegisterEnemyDeath(DemoEnemyAI enemy)
        {
            activeEnemies.Remove(enemy);
        }

        public void RegisterBossDeath(DemoBossAI boss)
        {
            activeBosses.Remove(boss);
        }

        // Public API
        public int ActiveEnemyCount => activeEnemies.Count;
        public int ActiveBossCount => activeBosses.Count;
        public List<DemoEnemyAI> GetActiveEnemies() => new List<DemoEnemyAI>(activeEnemies);
    }

    /// <summary>
    /// Enemy AI component with multiple behavior types
    /// </summary>
    public class DemoEnemyAI : MonoBehaviour, IDemoDamageable
    {
        [Header("Enemy Stats")]
        public int maxHealth = 50;
        public float moveSpeed = 3f;
        public int damage = 15;
        public float attackRange = 2f;
        public float detectionRange = 8f;
        public AIType aiType = AIType.PatrolChase;

        [Header("Patrol Settings")]
        public float patrolRadius = 5f;
        public float patrolWaitTime = 2f;

        [Header("Ranged Settings")]
        public GameObject projectilePrefab;
        public float fireRate = 1f;
        public float kiteDistance = 6f;

        // Private state
        private int currentHealth;
        private Transform target;
        private DemoAIManager aiManager;
        private Vector3 patrolCenter;
        private Vector3 patrolTarget;
        private float lastAttackTime;
        private float patrolWaitTimer;
        private AIState currentState = AIState.Patrol;
        
        // Components
        private Rigidbody2D rb2D;
        private Rigidbody rb3D;

        public bool IsDead => currentHealth <= 0;

        public void Initialize(Transform playerTarget, DemoAIManager manager)
        {
            target = playerTarget;
            aiManager = manager;
            currentHealth = maxHealth;
            patrolCenter = transform.position;
            patrolTarget = GetRandomPatrolPoint();
            
            rb2D = GetComponent<Rigidbody2D>();
            rb3D = GetComponent<Rigidbody>();

            // Add rigidbody if none exists
            if (!rb2D && !rb3D)
            {
                if (transform.position.z == 0) // Assume 2D if Z is 0
                {
                    rb2D = gameObject.AddComponent<Rigidbody2D>();
                    rb2D.gravityScale = 0; // Most enemies float/fly
                }
                else
                {
                    rb3D = gameObject.AddComponent<Rigidbody>();
                    rb3D.useGravity = false;
                }
            }
        }

        private void Update()
        {
            if (IsDead || !target) return;

            UpdateAI();
        }

        private void UpdateAI()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, target.position);
            
            // State transitions
            switch (currentState)
            {
                case AIState.Patrol:
                    if (distanceToPlayer <= detectionRange)
                    {
                        currentState = AIState.Combat;
                    }
                    else
                    {
                        PatrolBehavior();
                    }
                    break;
                
                case AIState.Combat:
                    if (distanceToPlayer > detectionRange * 1.5f)
                    {
                        currentState = AIState.Patrol;
                        patrolTarget = GetRandomPatrolPoint();
                    }
                    else
                    {
                        CombatBehavior(distanceToPlayer);
                    }
                    break;
            }
        }

        private void PatrolBehavior()
        {
            float distanceToPatrolTarget = Vector3.Distance(transform.position, patrolTarget);
            
            if (distanceToPatrolTarget < 1f)
            {
                // Reached patrol point, wait then choose new one
                patrolWaitTimer += Time.deltaTime;
                if (patrolWaitTimer >= patrolWaitTime)
                {
                    patrolTarget = GetRandomPatrolPoint();
                    patrolWaitTimer = 0f;
                }
            }
            else
            {
                // Move toward patrol target
                MoveTowards(patrolTarget, moveSpeed * 0.5f);
            }
        }

        private void CombatBehavior(float distanceToPlayer)
        {
            switch (aiType)
            {
                case AIType.PatrolChase:
                    PatrolChaseBehavior(distanceToPlayer);
                    break;
                case AIType.RangedKite:
                    RangedKiteBehavior(distanceToPlayer);
                    break;
                case AIType.MeleeBrute:
                    MeleeBruteBehavior(distanceToPlayer);
                    break;
                case AIType.SupportCaster:
                    SupportCasterBehavior(distanceToPlayer);
                    break;
            }
        }

        private void PatrolChaseBehavior(float distance)
        {
            if (distance <= attackRange)
            {
                // Attack player
                TryAttack();
            }
            else
            {
                // Chase player
                MoveTowards(target.position, moveSpeed);
            }
        }

        private void RangedKiteBehavior(float distance)
        {
            if (distance < kiteDistance)
            {
                // Move away from player
                Vector3 awayDirection = (transform.position - target.position).normalized;
                Vector3 kitePosition = transform.position + awayDirection * 2f;
                MoveTowards(kitePosition, moveSpeed);
            }
            else if (distance <= detectionRange)
            {
                // Stop and shoot
                TryRangedAttack();
            }
        }

        private void MeleeBruteBehavior(float distance)
        {
            if (distance <= attackRange)
            {
                // Heavy attack
                TryAttack(damage * 1.5f);
            }
            else
            {
                // Charge at player
                MoveTowards(target.position, moveSpeed * 1.2f);
            }
        }

        private void SupportCasterBehavior(float distance)
        {
            // Stay at medium range and cast support spells
            float idealDistance = detectionRange * 0.7f;
            
            if (distance < idealDistance)
            {
                // Move away
                Vector3 awayDirection = (transform.position - target.position).normalized;
                Vector3 castPosition = transform.position + awayDirection;
                MoveTowards(castPosition, moveSpeed * 0.8f);
            }
            else if (distance > idealDistance + 2f)
            {
                // Move closer
                MoveTowards(target.position, moveSpeed * 0.8f);
            }
            
            // Cast support spells (buff other enemies, debuff player, etc.)
            TryCastSupportSpell();
        }

        private void MoveTowards(Vector3 targetPosition, float speed)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            
            if (rb2D)
            {
                rb2D.velocity = direction * speed;
            }
            else if (rb3D)
            {
                rb3D.velocity = direction * speed;
            }
            else
            {
                transform.position += direction * speed * Time.deltaTime;
            }

            // Face movement direction
            if (direction.x != 0)
            {
                transform.localScale = new Vector3(Mathf.Sign(direction.x), 1, 1);
            }
        }

        private void TryAttack(float attackDamage = -1f)
        {
            if (Time.time - lastAttackTime < 1f / fireRate) return;

            if (attackDamage < 0) attackDamage = damage;

            // Deal damage to player if in range
            var playerCombat = target.GetComponent<DemoPlayerCombat>();
            if (playerCombat)
            {
                playerCombat.TakeDamage(attackDamage);
            }

            lastAttackTime = Time.time;

            // Visual effect
            CreateAttackEffect();
        }

        private void TryRangedAttack()
        {
            if (Time.time - lastAttackTime < 1f / fireRate) return;

            if (projectilePrefab)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                GameObject projectile = Instantiate(projectilePrefab, transform.position + direction * 0.5f, Quaternion.identity);
                
                var projectileScript = projectile.GetComponent<DemoProjectile>();
                if (projectileScript)
                {
                    projectileScript.Initialize(damage, direction, detectionRange, this.gameObject);
                }
            }

            lastAttackTime = Time.time;
        }

        private void TryCastSupportSpell()
        {
            if (Time.time - lastAttackTime < 2f) return; // Slower casting

            // Find other enemies to support
            var nearbyEnemies = aiManager.GetActiveEnemies();
            foreach (var enemy in nearbyEnemies)
            {
                if (enemy != this && Vector3.Distance(transform.position, enemy.transform.position) <= detectionRange)
                {
                    // Cast buff spell on ally
                    enemy.ApplyBuff(BuffType.Damage, 1.2f, 5f);
                    break;
                }
            }

            lastAttackTime = Time.time;
        }

        private Vector3 GetRandomPatrolPoint()
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            return patrolCenter + (Vector3)randomDirection * Random.Range(1f, patrolRadius);
        }

        private void CreateAttackEffect()
        {
            // Simple visual effect
            GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            effect.transform.position = transform.position;
            effect.transform.localScale = Vector3.one * 0.5f;
            
            var renderer = effect.GetComponent<Renderer>();
            renderer.material.color = Color.red;
            
            DestroyImmediate(effect.GetComponent<Collider>());
            Destroy(effect, 0.2f);
        }

        public void ApplyBuff(BuffType buffType, float multiplier, float duration)
        {
            // Apply temporary buff
            switch (buffType)
            {
                case BuffType.Damage:
                    StartCoroutine(ApplyDamageBuff(multiplier, duration));
                    break;
                case BuffType.Speed:
                    StartCoroutine(ApplySpeedBuff(multiplier, duration));
                    break;
            }
        }

        private System.Collections.IEnumerator ApplyDamageBuff(float multiplier, float duration)
        {
            float originalDamage = damage;
            damage = Mathf.RoundToInt(damage * multiplier);
            
            yield return new WaitForSeconds(duration);
            
            damage = Mathf.RoundToInt(originalDamage);
        }

        private System.Collections.IEnumerator ApplySpeedBuff(float multiplier, float duration)
        {
            float originalSpeed = moveSpeed;
            moveSpeed *= multiplier;
            
            yield return new WaitForSeconds(duration);
            
            moveSpeed = originalSpeed;
        }

        public void TakeDamage(float damageAmount, GameObject source, AttackType attackType)
        {
            currentHealth -= Mathf.RoundToInt(damageAmount);
            
            // Visual feedback
            StartCoroutine(FlashRed());
            
            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                // Enter combat mode when damaged
                currentState = AIState.Combat;
            }
        }

        private System.Collections.IEnumerator FlashRed()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer)
            {
                Color originalColor = renderer.material.color;
                renderer.material.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                renderer.material.color = originalColor;
            }
        }

        private void Die()
        {
            // Drop loot
            DropLoot();
            
            // Notify AI manager
            if (aiManager)
            {
                aiManager.RegisterEnemyDeath(this);
            }
            
            Destroy(gameObject);
        }

        private void DropLoot()
        {
            // Simple loot drop system
            if (Random.value < 0.3f) // 30% chance to drop something
            {
                var lootManager = FindObjectOfType<DemoLootManager>();
                if (lootManager)
                {
                    lootManager.SpawnLoot(transform.position);
                }
            }
        }

        // Gizmos for debugging
        private void OnDrawGizmosSelected()
        {
            // Detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Patrol area
            if (Application.isPlaying)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(patrolCenter, patrolRadius);
                
                // Current patrol target
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(patrolTarget, 0.3f);
            }
        }
    }

    public enum AIType
    {
        PatrolChase,
        RangedKite,
        MeleeBrute,
        SupportCaster
    }

    public enum AIState
    {
        Patrol,
        Combat,
        Fleeing
    }

    public enum BuffType
    {
        Damage,
        Speed,
        Defense
    }
}