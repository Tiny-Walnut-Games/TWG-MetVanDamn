using UnityEngine;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Authoring;
using Random = UnityEngine.Random;
using Unity.Collections;

namespace TinyWalnutGames.MetVanDAMN.Authoring
    {
#nullable enable
    /// <summary>
    /// Complete AI system managing enemy behavior and boss mechanics.
    /// Supports patrol/chase, ranged kite, melee brute, and support caster AI types.
    /// </summary>
    public class DemoAIManager : MonoBehaviour
        {
        [Header("AI Settings")]
        public GameObject[] enemyPrefabs;
        public GameObject[] bossPrefabs;
        public Transform? player;
        public float spawnRadius = 20f;
        public int maxEnemies = 10;
        public float spawnCooldown = 5f;

        // AI Management
        private List<DemoEnemyAI> activeEnemies = new();
        private List<DemoBossAI> activeBosses = new();
        private float lastSpawnTime;

        private void Start()
            {
            // Find player if not assigned
            if (player == null)
                {
                DemoPlayerMovement playerMovement = FindAnyObjectByType<DemoPlayerMovement>();
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
            DemoEnemyAI aiComponent = enemy.GetComponent<DemoEnemyAI>();

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
            DemoBossAI bossComponent = boss.GetComponent<DemoBossAI>();

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
            if (player == null) return Vector3.zero;
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
        public List<DemoEnemyAI> GetActiveEnemies() => new(activeEnemies);
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
        private Transform? target;
        private DemoAIManager? aiManager;
        private Vector3 patrolCenter;
        private Vector3 patrolTarget;
        private float lastAttackTime;
        private float patrolWaitTimer;
        private AIState currentState = AIState.Patrol;

        // Navigation system integration
        private Entity navigationEntity;
        private EntityManager entityManager;
        private World? defaultWorld;
        private AINavigationState navState;
        private DynamicBuffer<PathNodeBufferElement> pathBuffer;
        private AgentCapabilities agentCapabilities;
        private uint currentTargetNodeId;
        private bool navigationInitialized;

        // Enhanced movement tracking
        private Vector3 lastPosition;
        private float stuckTimer;
        private const float StuckThreshold = 0.1f;
        private const float StuckTimeout = 2f;

        public bool IsDead => currentHealth <= 0;

        public void Initialize(Transform playerTarget, DemoAIManager manager)
            {
            target = playerTarget;
            aiManager = manager;
            currentHealth = maxHealth;
            patrolCenter = transform.position;
            lastPosition = transform.position;

            // Initialize ECS navigation
            InitializeNavigationSystem();

            // Setup agent capabilities based on AI type
            SetupAgentCapabilities();

            // Find initial navigation nodes
            FindNearestNavigationNode();

            // Legacy physics setup (fallback for non-navigation movement)
            SetupPhysicsComponents();
            }

        /// <summary>
        /// Initialize ECS navigation system integration
        /// </summary>
        private void InitializeNavigationSystem()
            {
            defaultWorld = World.DefaultGameObjectInjectionWorld;
            if (defaultWorld != null && defaultWorld.IsCreated)
                {
                entityManager = defaultWorld.EntityManager;

                // Create navigation entity for this AI
                navigationEntity = entityManager.CreateEntity();
                entityManager.SetName(navigationEntity, $"AI_Nav_{gameObject.name}");

                // Add navigation components
                navState = new AINavigationState(0, 0);
                entityManager.AddComponentData(navigationEntity, navState);
                entityManager.AddComponentData(navigationEntity, agentCapabilities);
                entityManager.AddBuffer<PathNodeBufferElement>(navigationEntity);

                pathBuffer = entityManager.GetBuffer<PathNodeBufferElement>(navigationEntity);
                navigationInitialized = true;

                Debug.Log($"🤖 AI Navigation initialized for {gameObject.name}");
                }
            else
                {
                Debug.LogWarning($"⚠️ ECS World not available for {gameObject.name} - falling back to basic movement");
                navigationInitialized = false;
                }
            }

        /// <summary>
        /// Setup agent capabilities based on AI type and configuration
        /// </summary>
        private void SetupAgentCapabilities()
            {
            // Configure capabilities based on AI type
            Polarity availablePolarity = Polarity.Any; // Most enemies can traverse any polarity
            Ability availableAbilities = Ability.Everything;
            float skillLevel = 0.5f;

            switch (aiType)
                {
                case AIType.PatrolChase:
                    availableAbilities |= Ability.Jump | Ability.Dash;
                    skillLevel = 0.3f;
                    break;

                case AIType.RangedKite:
                    availableAbilities |= Ability.Jump | Ability.GlideSpeed;
                    availablePolarity = Polarity.Sun | Polarity.Moon; // Prefer open areas
                    skillLevel = 0.6f;
                    break;

                case AIType.MeleeBrute:
                    availableAbilities |= Ability.ChargedJump | Ability.WallJump;
                    skillLevel = 0.4f;
                    break;

                case AIType.SupportCaster:
                    availableAbilities |= Ability.ArcJump | Ability.TeleportArc;
                    availablePolarity = Polarity.Heat | Polarity.Cold; // Magical biomes
                    skillLevel = 0.8f;
                    break;
                }

            agentCapabilities = new AgentCapabilities(
                availablePolarity,
                availableAbilities,
                skillLevel,
                aiType.ToString(),
                true
            );
            }

        /// <summary>
        /// Find the nearest navigation node to this AI's position
        /// </summary>
        private void FindNearestNavigationNode()
            {
            if (!navigationInitialized) return;

            uint nearestNodeId = 0;
            float nearestDistance = float.MaxValue;
            float3 aiPosition = transform.position;

            // Query for all navigation nodes
            using (EntityQuery nodeQuery = entityManager.CreateEntityQuery(typeof(NavNode), typeof(NodeId)))
                {
                NativeArray<Entity> entities = nodeQuery.ToEntityArray(Allocator.Temp);

                foreach (Entity entity in entities)
                    {
                    NodeId nodeId = entityManager.GetComponentData<NodeId>(entity);
                    NavNode navNode = entityManager.GetComponentData<NavNode>(entity);

                    if (!navNode.IsActive) continue;

                    float distance = math.distance(aiPosition, navNode.WorldPosition);
                    if (distance < nearestDistance)
                        {
                        nearestDistance = distance;
                        nearestNodeId = nodeId._value;
                        }
                    }

                entities.Dispose();
                }

            if (nearestNodeId != 0)
                {
                navState.CurrentNodeId = nearestNodeId;
                if (navigationInitialized && entityManager.Exists(navigationEntity))
                    {
                    entityManager.SetComponentData(navigationEntity, navState);
                    }
                Debug.Log($"🎯 AI {gameObject.name} assigned to navigation node {nearestNodeId}");
                }
            }

        /// <summary>
        /// Legacy physics setup for fallback movement
        /// </summary>
        private void SetupPhysicsComponents()
            {
            Rigidbody2D rb2D = GetComponent<Rigidbody2D>();
            Rigidbody rb3D = GetComponent<Rigidbody>();

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
            if (target == null) return;
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
            // Use navigation-aware patrol target selection
            if (patrolTarget == Vector3.zero || Vector3.Distance(transform.position, patrolTarget) < 1f)
                {
                patrolWaitTimer += Time.deltaTime;
                if (patrolWaitTimer >= patrolWaitTime)
                    {
                    patrolTarget = GetRandomPatrolPoint();
                    patrolWaitTimer = 0f;
                    Debug.Log($"🚶 AI {gameObject.name} chose new patrol target: {patrolTarget}");
                    }
                }
            else
                {
                // Move toward patrol target using enhanced navigation
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
                if (target != null)
                    MoveTowards(target.position, moveSpeed);
                }
            }

        private void RangedKiteBehavior(float distance)
            {
            if (distance < kiteDistance)
                {
                // Move away from player
                if (target == null) return;
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
                if (target != null)
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
                if (target == null) return;
                Vector3 awayDirection = (transform.position - target.position).normalized;
                Vector3 castPosition = transform.position + awayDirection;
                MoveTowards(castPosition, moveSpeed * 0.8f);
                }
            else if (distance > idealDistance + 2f)
                {
                // Move closer
                if (target != null)
                    MoveTowards(target.position, moveSpeed * 0.8f);
                }

            // Cast support spells (buff other enemies, debuff player, etc.)
            TryCastSupportSpell();
            }

        /// <summary>
        /// Enhanced movement using navigation system with fallback to direct movement
        /// </summary>
        private void MoveTowards(Vector3 targetPosition, float speed)
            {
            if (navigationInitialized && UseNavigationPathfinding(targetPosition))
                {
                // Use ECS navigation system for pathfinding
                NavigateWithPathfinding(targetPosition, speed);
                }
            else
                {
                // Fallback to direct movement
                DirectMovement(targetPosition, speed);
                }

            // Track stuck detection
            if (Vector3.Distance(transform.position, lastPosition) < StuckThreshold)
                {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > StuckTimeout)
                    {
                    HandleStuckSituation(targetPosition);
                    }
                }
            else
                {
                stuckTimer = 0f;
                lastPosition = transform.position;
                }
            }

        /// <summary>
        /// Determine if navigation pathfinding should be used
        /// </summary>
        private bool UseNavigationPathfinding(Vector3 targetPosition)
            {
            // Use navigation for longer distances or when obstacles likely
            float distance = Vector3.Distance(transform.position, targetPosition);
            return distance > 5f || (currentState == AIState.Patrol);
            }

        /// <summary>
        /// Navigate using ECS pathfinding system
        /// </summary>
        private void NavigateWithPathfinding(Vector3 targetPosition, float speed)
            {
            // Find target navigation node
            uint targetNodeId = FindNearestNavigationNodeToPosition(targetPosition);

            if (targetNodeId != 0 && targetNodeId != currentTargetNodeId)
                {
                currentTargetNodeId = targetNodeId;
                RequestPathfinding(targetNodeId);
                }

            // Follow current path if available
            FollowNavigationPath(speed);
            }

        /// <summary>
        /// Find nearest navigation node to given position
        /// </summary>
        private uint FindNearestNavigationNodeToPosition(Vector3 position)
            {
            if (!navigationInitialized) return 0;

            uint nearestNodeId = 0;
            float nearestDistance = float.MaxValue;
            float3 targetPosition = position;

            using (EntityQuery nodeQuery = entityManager.CreateEntityQuery(typeof(NavNode), typeof(NodeId)))
                {
                NativeArray<Entity> entities = nodeQuery.ToEntityArray(Allocator.Temp);

                foreach (Entity entity in entities)
                    {
                    NodeId nodeId = entityManager.GetComponentData<NodeId>(entity);
                    NavNode navNode = entityManager.GetComponentData<NavNode>(entity);

                    if (!navNode.IsActive || !navNode.IsCompatibleWith(agentCapabilities)) continue;

                    float distance = math.distance(targetPosition, navNode.WorldPosition);
                    if (distance < nearestDistance)
                        {
                        nearestDistance = distance;
                        nearestNodeId = nodeId._value;
                        }
                    }

                entities.Dispose();
                }

            return nearestNodeId;
            }

        /// <summary>
        /// Request pathfinding to target node
        /// </summary>
        private void RequestPathfinding(uint targetNodeId)
            {
            if (!navigationInitialized || !entityManager.Exists(navigationEntity)) return;

            navState.TargetNodeId = targetNodeId;
            navState.Status = PathfindingStatus.Idle; // Trigger new pathfinding
            entityManager.SetComponentData(navigationEntity, navState);
            }

        /// <summary>
        /// Follow the calculated navigation path
        /// </summary>
        private void FollowNavigationPath(float speed)
            {
            if (!navigationInitialized || !entityManager.Exists(navigationEntity)) return;

            // Update navigation state
            navState = entityManager.GetComponentData<AINavigationState>(navigationEntity);

            if (navState.Status == PathfindingStatus.PathFound && navState.PathLength > 0)
                {
                pathBuffer = entityManager.GetBuffer<PathNodeBufferElement>(navigationEntity);

                if (navState.CurrentPathStep < pathBuffer.Length)
                    {
                    // Get next waypoint
                    uint nextNodeId = pathBuffer[navState.CurrentPathStep].NodeId;
                    Vector3 nextWaypoint = GetNodeWorldPosition(nextNodeId);

                    if (nextWaypoint != Vector3.zero)
                        {
                        // Move towards next waypoint
                        float distanceToWaypoint = Vector3.Distance(transform.position, nextWaypoint);

                        if (distanceToWaypoint < 2f) // Reached waypoint
                            {
                            navState.CurrentPathStep++;
                            entityManager.SetComponentData(navigationEntity, navState);
                            }
                        else
                            {
                            DirectMovement(nextWaypoint, speed);
                            }
                        }
                    }
                }
            else if (navState.Status == PathfindingStatus.NoPathFound || navState.Status == PathfindingStatus.TargetUnreachable)
                {
                // Fallback to direct movement
                if (target != null)
                    DirectMovement(target.position, speed * 0.5f);
                }
            }

        /// <summary>
        /// Get world position of navigation node
        /// </summary>
        private Vector3 GetNodeWorldPosition(uint nodeId)
            {
            using (EntityQuery nodeQuery = entityManager.CreateEntityQuery(typeof(NavNode), typeof(NodeId)))
                {
                NativeArray<Entity> entities = nodeQuery.ToEntityArray(Allocator.Temp);

                foreach (Entity entity in entities)
                    {
                    NodeId id = entityManager.GetComponentData<NodeId>(entity);
                    if (id._value == nodeId)
                        {
                        NavNode navNode = entityManager.GetComponentData<NavNode>(entity);
                        entities.Dispose();
                        return navNode.WorldPosition;
                        }
                    }

                entities.Dispose();
                }

            return Vector3.zero;
            }

        /// <summary>
        /// Direct movement implementation (legacy fallback)
        /// </summary>
        private void DirectMovement(Vector3 targetPosition, float speed)
            {
            Vector3 direction = (targetPosition - transform.position).normalized;

            Rigidbody2D rb2D = GetComponent<Rigidbody2D>();
            Rigidbody rb3D = GetComponent<Rigidbody>();

            if (rb2D)
                {
                rb2D.linearVelocity = direction * speed;
                }
            else if (rb3D)
                {
                rb3D.linearVelocity = direction * speed;
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

        /// <summary>
        /// Handle situation where AI is stuck
        /// </summary>
        private void HandleStuckSituation(Vector3 targetPosition)
            {
            Debug.Log($"🚫 AI {gameObject.name} is stuck, attempting recovery...");

            // Try random movement to unstick
            Vector3 randomDirection = Random.insideUnitSphere;
            randomDirection.y = 0; // Keep on ground level
            Vector3 unstickPosition = transform.position + randomDirection.normalized * 2f;

            DirectMovement(unstickPosition, moveSpeed);

            // Reset navigation if stuck too long
            if (stuckTimer > StuckTimeout * 2)
                {
                FindNearestNavigationNode();
                stuckTimer = 0f;
                }
            }

        /// <summary>
        /// Enhanced patrol behavior using navigation waypoints
        /// </summary>
        private Vector3 GetRandomPatrolPoint()
            {
            if (navigationInitialized)
                {
                // Use navigation nodes for patrol points
                uint randomNodeId = GetRandomNearbyNavigationNode();
                if (randomNodeId != 0)
                    {
                    Vector3 nodePosition = GetNodeWorldPosition(randomNodeId);
                    if (nodePosition != Vector3.zero)
                        {
                        return nodePosition;
                        }
                    }
                }

            // Fallback to random point around patrol center
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            return patrolCenter + (Vector3)randomDirection * Random.Range(1f, patrolRadius);
            }

        /// <summary>
        /// Get a random nearby navigation node for patrol
        /// </summary>
        private uint GetRandomNearbyNavigationNode()
            {
            var nearbyNodes = new List<uint>();
            float3 aiPosition = transform.position;

            using (EntityQuery nodeQuery = entityManager.CreateEntityQuery(typeof(NavNode), typeof(NodeId)))
                {
                NativeArray<Entity> entities = nodeQuery.ToEntityArray(Allocator.Temp);

                foreach (Entity entity in entities)
                    {
                    NodeId nodeId = entityManager.GetComponentData<NodeId>(entity);
                    NavNode navNode = entityManager.GetComponentData<NavNode>(entity);

                    if (!navNode.IsActive || !navNode.IsCompatibleWith(agentCapabilities)) continue;

                    float distance = math.distance(aiPosition, navNode.WorldPosition);
                    if (distance <= patrolRadius && distance >= 2f) // Not too close, not too far
                        {
                        nearbyNodes.Add(nodeId._value);
                        }
                    }

                entities.Dispose();
                }

            if (nearbyNodes.Count > 0)
                {
                return nearbyNodes[Random.Range(0, nearbyNodes.Count)];
                }

            return 0;
            }

        /// <summary>
        /// Cleanup navigation entity when AI is destroyed
        /// </summary>
        private void OnDestroy()
            {
            if (navigationInitialized && defaultWorld != null && defaultWorld.IsCreated && entityManager.Exists(navigationEntity))
                {
                entityManager.DestroyEntity(navigationEntity);
                }
            }

        private void TryAttack(float attackDamage = -1f)
            {
            if (Time.time - lastAttackTime < 1f / fireRate) return;

            if (attackDamage < 0) attackDamage = damage;

            // Deal damage to player if in range
            if (target == null) return;
            DemoPlayerCombat playerCombat = target.GetComponent<DemoPlayerCombat>();
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
                if (target == null) return;
                Vector3 direction = (target.position - transform.position).normalized;
                GameObject projectile = Instantiate(projectilePrefab, transform.position + direction * 0.5f, Quaternion.identity);
                // Basic physics-based projectile fallback if no script is present
                var rb = projectile.GetComponent<Rigidbody>();
                if (!rb) { rb = projectile.AddComponent<Rigidbody>(); rb.useGravity = false; }
                rb.linearVelocity = direction * Mathf.Max(8f, detectionRange * 0.5f);
                Destroy(projectile, 5f);
                }

            lastAttackTime = Time.time;
            }

        private void TryCastSupportSpell()
            {
            if (Time.time - lastAttackTime < 2f) return; // Slower casting

            // Find other enemies to support
            if (aiManager == null) return;
            List<DemoEnemyAI> nearbyEnemies = aiManager.GetActiveEnemies();
            foreach (DemoEnemyAI enemy in nearbyEnemies)
                {
                if (enemy != this && Vector3.Distance(transform.position, enemy.transform.position) <= detectionRange)
                    {
                    // Cast buff spell on ally
                    enemy.ApplyBuff(EnemyBuffType.Damage, 1.2f, 5f);
                    break;
                    }
                }

            lastAttackTime = Time.time;
            }

        // Duplicate GetRandomPatrolPoint removed; using the navigation-aware version above.

        private void CreateAttackEffect()
            {
            // Simple visual effect
            GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            effect.transform.position = transform.position;
            effect.transform.localScale = Vector3.one * 0.5f;

            Renderer renderer = effect.GetComponent<Renderer>();
            renderer.material.color = Color.red;

            DestroyImmediate(effect.GetComponent<Collider>());
            Destroy(effect, 0.2f);
            }

        public void ApplyBuff(EnemyBuffType buffType, float multiplier, float duration)
            {
            // Apply temporary buff
            switch (buffType)
                {
                case EnemyBuffType.Damage:
                    StartCoroutine(ApplyDamageBuff(multiplier, duration));
                    break;
                case EnemyBuffType.Speed:
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
            Renderer renderer = GetComponent<Renderer>();
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
            if (aiManager != null)
                aiManager.RegisterEnemyDeath(this);

            Destroy(gameObject);
            }

        private void DropLoot()
            {
            // Simple loot drop system
            if (Random.value < 0.3f) // 30% chance to drop something
                {
                DemoLootManager lootManager = FindFirstObjectByType<DemoLootManager>();
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

    public enum EnemyBuffType
        {
        Damage,
        Speed,
        Defense
        }
    }
