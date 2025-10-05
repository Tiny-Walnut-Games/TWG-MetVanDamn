# 👹 Enemy AI Systems
## *Creating Smart, Challenging Enemies That Feel Alive*

> **"Enemies aren't just obstacles - they're the heartbeat of your game's challenge and personality."**

[![Enemy AI](https://img.shields.io/badge/Enemy-AI-red.svg)](enemies.md)
[![Unity 6000.2+](https://img.shields.io/badge/Unity-6000.2+-black.svg?style=flat&logo=unity)](https://unity3d.com/get-unity/download)

---

## 🎯 **What Makes Good Enemy AI?**

**Enemy AI** is the brain that makes your enemies feel intelligent and challenging. Good AI:

- 🧠 **Adapts** to player behavior and abilities
- 🎯 **Provides feedback** through attacks and patterns
- 📈 **Scales difficulty** as players progress
- 🎭 **Has personality** through unique behaviors
- ⚖️ **Balances challenge** with fairness

**Bad AI feels random and unfair. Good AI feels clever and beatable!**

---

## 🏗️ **AI System Architecture**

### **Core Components**

1. **Behavior Tree** - Decision making system
2. **State Machine** - Current action state
3. **Sensors** - Detecting player/environment
4. **Actions** - Movement, attacks, animations

### **Basic Enemy Structure**

```csharp
public class EnemyAI : MonoBehaviour
{
    [Header("Core Components")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private Health health;

    [Header("AI Settings")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;

    // AI States
    private enum AIState { Patrolling, Chasing, Attacking, Stunned }
    private AIState currentState = AIState.Patrolling;

    // References
    private Transform player;
    private Vector3 startPosition;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        startPosition = transform.position;
        agent.speed = patrolSpeed;
    }

    void Update()
    {
        switch (currentState)
        {
            case AIState.Patrolling:
                Patrol();
                break;
            case AIState.Chasing:
                Chase();
                break;
            case AIState.Attacking:
                Attack();
                break;
            case AIState.Stunned:
                // Do nothing, wait for stun to wear off
                break;
        }
    }
}
```

---

## 🚀 **Quick Enemy Setup (10 Minutes)**

### **Step 1: Basic Patrolling Enemy**

```csharp
public class PatrollingEnemy : MonoBehaviour
{
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waitTime = 2f;
    [SerializeField] private NavMeshAgent agent;

    private int currentPoint = 0;
    private float waitTimer = 0f;

    void Start()
    {
        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[0].position);
        }
    }

    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                // Move to next point
                currentPoint = (currentPoint + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPoint].position);
                waitTimer = 0f;
            }
        }
    }
}
```

### **Step 2: Add Player Detection**

```csharp
public class DetectingEnemy : PatrollingEnemy
{
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float loseRange = 12f; // Larger than detection
    [SerializeField] private LayerMask playerLayer;

    private Transform player;
    private bool playerDetected = false;

    void Start()
    {
        base.Start();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        base.Update();

        // Check for player
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (!playerDetected && distanceToPlayer <= detectionRange)
        {
            // Player spotted!
            playerDetected = true;
            OnPlayerDetected();
        }
        else if (playerDetected && distanceToPlayer > loseRange)
        {
            // Player lost
            playerDetected = false;
            OnPlayerLost();
        }

        if (playerDetected)
        {
            ChasePlayer();
        }
    }

    protected virtual void OnPlayerDetected()
    {
        Debug.Log("Player detected!");
        // Change animation, play sound, etc.
    }

    protected virtual void OnPlayerLost()
    {
        Debug.Log("Player lost...");
        // Return to patrol
    }

    private void ChasePlayer()
    {
        GetComponent<NavMeshAgent>().SetDestination(player.position);
    }
}
```

### **Step 3: Add Combat**

```csharp
public class CombatEnemy : DetectingEnemy
{
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int attackDamage = 1;

    private float lastAttackTime = 0f;

    void Update()
    {
        base.Update();

        if (playerDetected)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
            }
        }
    }

    private void Attack()
    {
        lastAttackTime = Time.time;

        // Deal damage to player
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }

        // Play attack animation
        GetComponent<Animator>().SetTrigger("Attack");

        Debug.Log("Enemy attacks for " + attackDamage + " damage!");
    }
}
```

---

## 🎮 **Advanced Enemy Behaviors**

### **Ranged Attacker**

```csharp
public class RangedEnemy : DetectingEnemy
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float fireRate = 1f;

    private float lastFireTime = 0f;

    void Update()
    {
        base.Update();

        if (playerDetected && Time.time >= lastFireTime + (1f / fireRate))
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= detectionRange && distanceToPlayer > 3f) // Keep distance
            {
                FireProjectile();
            }
        }
    }

    private void FireProjectile()
    {
        lastFireTime = Time.time;

        // Create projectile
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        // Add velocity toward player
        Vector3 direction = (player.position - firePoint.position).normalized;
        projectile.GetComponent<Rigidbody2D>().velocity = direction * projectileSpeed;

        // Play fire animation/sound
        GetComponent<Animator>().SetTrigger("Fire");
    }
}
```

### **Flying Enemy**

```csharp
public class FlyingEnemy : DetectingEnemy
{
    [SerializeField] private float flyHeight = 3f;
    [SerializeField] private float hoverAmplitude = 0.5f;
    [SerializeField] private float hoverSpeed = 2f;

    private Vector3 basePosition;
    private float hoverTimer = 0f;

    void Start()
    {
        base.Start();
        basePosition = transform.position;
        basePosition.y = flyHeight;
    }

    void Update()
    {
        base.Update();

        // Hover up and down
        hoverTimer += Time.time * hoverSpeed;
        float hoverOffset = Mathf.Sin(hoverTimer) * hoverAmplitude;

        Vector3 targetPosition = basePosition;
        if (playerDetected)
        {
            // Move toward player horizontally
            targetPosition.x = player.position.x;
            targetPosition.z = player.position.z;
        }

        targetPosition.y = flyHeight + hoverOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 2f);
    }
}
```

### **Boss Enemy with Phases**

```csharp
public class BossEnemy : MonoBehaviour
{
    [System.Serializable]
    public class BossPhase
    {
        public string name;
        public float healthThreshold; // 0-1, when to enter this phase
        public float moveSpeed;
        public float attackDamage;
        public Color phaseColor;
    }

    [SerializeField] private BossPhase[] phases;
    [SerializeField] private Health health;
    [SerializeField] private SpriteRenderer renderer;

    private int currentPhaseIndex = 0;

    void Start()
    {
        health.OnHealthChanged += CheckPhaseTransition;
        EnterPhase(0);
    }

    private void CheckPhaseTransition(float currentHealth, float maxHealth)
    {
        float healthPercent = currentHealth / maxHealth;

        for (int i = phases.Length - 1; i > currentPhaseIndex; i--)
        {
            if (healthPercent <= phases[i].healthThreshold)
            {
                EnterPhase(i);
                break;
            }
        }
    }

    private void EnterPhase(int phaseIndex)
    {
        currentPhaseIndex = phaseIndex;
        BossPhase phase = phases[phaseIndex];

        // Update stats
        GetComponent<NavMeshAgent>().speed = phase.moveSpeed;
        // Update damage, etc.

        // Visual feedback
        renderer.color = phase.phaseColor;

        // Play phase transition animation/sound
        Debug.Log("Boss entering phase: " + phase.name);

        // Trigger special abilities or behaviors
        OnPhaseEnter(phase);
    }

    protected virtual void OnPhaseEnter(BossPhase phase)
    {
        // Override in subclasses for specific phase behaviors
    }
}
```

---

## 🧠 **Smart AI Patterns**

### **Predictive Movement**

```csharp
public class PredictiveEnemy : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private float predictionTime = 0.5f; // How far ahead to predict

    void Update()
    {
        if (player != null)
        {
            // Predict where player will be
            Vector3 playerVelocity = player.GetComponent<Rigidbody2D>().velocity;
            Vector3 predictedPosition = player.position + (Vector3)playerVelocity * predictionTime;

            // Move to intercept
            agent.SetDestination(predictedPosition);
        }
    }
}
```

### **Coordinated Group AI**

```csharp
public class GroupAI : MonoBehaviour
{
    [SerializeField] private EnemyAI[] groupMembers;
    [SerializeField] private float formationRadius = 3f;

    public void FormUpAround(Vector3 center)
    {
        for (int i = 0; i < groupMembers.Length; i++)
        {
            // Arrange in circle formation
            float angle = (i / (float)groupMembers.Length) * 360f * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * formationRadius,
                0,
                Mathf.Sin(angle) * formationRadius
            );

            groupMembers[i].GetComponent<NavMeshAgent>().SetDestination(center + offset);
        }
    }

    public void AttackAsGroup(Transform target)
    {
        foreach (var enemy in groupMembers)
        {
            enemy.GetComponent<ChasingEnemy>().ChaseTarget(target);
        }
    }
}
```

### **Learning Enemy (Adaptive Difficulty)**

```csharp
public class AdaptiveEnemy : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private float adaptationRate = 0.1f;

    private float playerSkillLevel = 1f; // 0 = beginner, 2 = expert

    void Start()
    {
        // Analyze player performance
        StartCoroutine(AnalyzePlayerSkill());
    }

    private IEnumerator AnalyzePlayerSkill()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f); // Check every 10 seconds

            // Simple skill assessment based on player actions
            float recentDeaths = player.GetRecentDeathCount();
            float recentKills = player.GetRecentKillCount();
            float accuracy = player.GetShotAccuracy();

            // Adjust skill level (0-2 range)
            playerSkillLevel = Mathf.Clamp(
                playerSkillLevel + (recentKills - recentDeaths) * adaptationRate,
                0f, 2f
            );

            // Adapt enemy difficulty
            AdaptToPlayerSkill(playerSkillLevel);
        }
    }

    private void AdaptToPlayerSkill(float skill)
    {
        var agent = GetComponent<NavMeshAgent>();
        var combat = GetComponent<CombatEnemy>();

        // Harder for better players
        agent.speed = 2f + skill * 2f; // 2-6 speed
        combat.attackDamage = 1 + (int)(skill * 2f); // 1-5 damage
        combat.attackCooldown = 1f - skill * 0.3f; // 1.0-0.4 seconds

        Debug.Log("Adapted to player skill level: " + skill);
    }
}
```

---

## 🔧 **Debugging & Balancing**

### **AI Debug Visualizer**

```csharp
public class AIDebugVisualizer : MonoBehaviour
{
    [SerializeField] private EnemyAI enemy;
    [SerializeField] private bool showDetectionRange = true;
    [SerializeField] private bool showAttackRange = true;
    [SerializeField] private bool showPath = true;

    void OnDrawGizmos()
    {
        if (enemy == null) return;

        // Detection range
        if (showDetectionRange)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, enemy.detectionRange);
        }

        // Attack range
        if (showAttackRange)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, enemy.attackRange);
        }

        // Current path
        if (showPath && enemy.agent.hasPath)
        {
            Gizmos.color = Color.blue;
            Vector3[] corners = enemy.agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }
}
```

### **Balance Testing Tools**

```csharp
public class EnemyBalanceTester : MonoBehaviour
{
    [SerializeField] private EnemyAI[] testEnemies;
    [SerializeField] private Transform playerStart;
    [SerializeField] private float testDuration = 60f;

    private Dictionary<string, int> killCounts = new Dictionary<string, int>();
    private float testStartTime;

    [ContextMenu("Run Balance Test")]
    public void RunBalanceTest()
    {
        // Reset
        killCounts.Clear();
        testStartTime = Time.time;

        // Position player
        playerStart.position = Vector3.zero;

        // Start test
        StartCoroutine(RunTest());
    }

    private IEnumerator RunTest()
    {
        // Spawn enemies in waves
        for (int wave = 0; wave < 5; wave++)
        {
            SpawnWave(wave);
            yield return new WaitForSeconds(12f); // Wait between waves
        }

        yield return new WaitForSeconds(testDuration - (Time.time - testStartTime));

        // Report results
        ReportResults();
    }

    private void SpawnWave(int waveNumber)
    {
        int enemyCount = 3 + waveNumber; // Increasing difficulty
        for (int i = 0; i < enemyCount; i++)
        {
            // Spawn enemy at random position
            Vector3 spawnPos = Random.insideUnitCircle * 20f;
            Instantiate(testEnemies[Random.Range(0, testEnemies.Length)], spawnPos, Quaternion.identity);
        }
    }

    public void RecordKill(string enemyType)
    {
        if (!killCounts.ContainsKey(enemyType))
            killCounts[enemyType] = 0;
        killCounts[enemyType]++;
    }

    private void ReportResults()
    {
        Debug.Log("=== BALANCE TEST RESULTS ===");
        foreach (var kvp in killCounts)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value} kills");
        }
        Debug.Log($"Test completed in {Time.time - testStartTime:F1} seconds");
    }
}
```

---

## 🎯 **Best Practices**

### **Design Principles**
- **Clear Feedback** - Players should understand enemy behaviors
- **Fair Challenge** - Enemies should be beatable with skill
- **Variety** - Mix different enemy types for interesting combat
- **Performance** - Keep AI simple enough to run smoothly

### **Technical Tips**
- Use object pooling for projectiles
- Cache component references in Start()
- Use coroutines for complex behaviors
- Profile AI performance regularly

### **Player Experience**
- Test enemy difficulty with real players
- Provide audio/visual cues for enemy actions
- Balance enemy health/damage with player progression
- Consider accessibility (epilepsy, colorblindness)

---

## 🚀 **Next Steps**

**Ready to create more enemy types?**
- **[Navigation Guide](navigation.md)** - Advanced pathfinding techniques
- **[Player Systems](player.md)** - Create responsive player controls
- **[Custom Behaviors Tutorial](../../tutorials/enemy-behavior.md)** - Build unique enemy personalities

**Need inspiration?**
- Study the [demo enemies](../../Assets/Scenes/) in the project
- Check [enemy behavior tutorials](../../tutorials/enemy-behavior.md)
- Join the [community discussions](https://github.com/jmeyer1980/TWG-MetVanDamn/discussions)

---

*"Great enemies don't just fight players - they teach them how to play your game better."*

**🍑 Happy Enemy Crafting! 🍑**
