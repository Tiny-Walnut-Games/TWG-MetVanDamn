# 🎮 Complete Platformer Tutorial: From Zero to Game
## *Build Your First Full MetVanDAMN Platformer*

> **"Ready to create something amazing? This tutorial takes you from empty project to complete platformer game. By the end, you'll have a working game you can share with friends!"**

---

## 🎯 **What You'll Build**
A complete 2D platformer with:
- ✅ Procedurally generated world
- ✅ Player character with full movement
- ✅ Enemies that chase and attack
- ✅ Collectible items and power-ups
- ✅ Combat system with health
- ✅ Save system and checkpoints
- ✅ Sound effects and music
- ✅ Menu system and game over screen

**Time**: 2-3 hours (can be done in sessions)
**Difficulty**: Intermediate
**Skills you'll learn**: Unity, C# scripting, game design, MetVanDAMN integration

---

## 🛠️ **Preparation (10 minutes)**

### **What You Need**
- Unity 6000.2+ installed
- MetVanDAMN project (from setup tutorial)
- Basic Unity knowledge (objects, components, scenes)

### **Project Setup**
1. Open your MetVanDAMN project in Unity
2. Create new scene: **File > New Scene > Basic (2D)**
3. Save as: `MyPlatformerGame.unity`
4. Set up 2D view: **Window > 2D > Set 2D view**

### **Import Assets** (Optional but Recommended)
For this tutorial, we'll use Unity's built-in assets. For a polished game, you might want:
- Sprite sheets for characters
- Tile sets for environments
- Sound effects and music
- UI sprites

---

## 🌍 **Phase 1: World Generation (20 minutes)**

### **Step 1: Add World Generator**
1. Right-click in Hierarchy: **Create Empty**
2. Name it: `WorldGenerator`
3. Add Component: **Smoke Test Scene Setup**
4. Configure settings:
   - **World Seed**: `12345` (for consistent results)
   - **World Size**: X=`60`, Y=`40` (good for platformer)
   - **Target Sector Count**: `4` (balanced districts)
   - **Enable Debug Visualization**: ✅ Checked

### **Step 2: Test World Generation**
1. Click **Play**
2. Press **M** to see the world map
3. You should see 4 districts with connections
4. Stop playing

### **Step 3: Adjust for Platforming**
Platformers work best with:
- Connected horizontal layouts
- Varied elevation changes
- Clear progression paths

Keep the default settings for now - we'll optimize later.

---

## 🎮 **Phase 2: Player Character (45 minutes)**

### **Step 1: Create Player Object**
1. Right-click: **Create Empty**
2. Name: `Player`
3. Add Component: **Rigidbody 2D**
   - **Gravity Scale**: `3`
   - **Freeze Rotation Z**: ✅ Checked
4. Add Component: **Box Collider 2D**
   - **Size**: X=`0.8`, Y=`1.8`
5. Add Component: **Sprite Renderer**
   - Choose a simple sprite or use built-in square

### **Step 2: Player Movement Script**
1. Add Component: **New Script** > `PlayerController`
2. Replace the code with:

```csharp
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float jumpForce = 16f;
    public float wallSlideSpeed = 2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Wall Check")]
    public Transform wallCheck;
    public float wallCheckDistance = 0.5f;
    public LayerMask wallLayer;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private int facingDirection = 1; // 1 = right, -1 = left

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Create ground check if needed
        if (groundCheck == null)
        {
            groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.parent = transform;
            groundCheck.localPosition = Vector3.down * 0.9f;
        }

        // Create wall check if needed
        if (wallCheck == null)
        {
            wallCheck = new GameObject("WallCheck").transform;
            wallCheck.parent = transform;
            wallCheck.localPosition = Vector3.right * 0.4f;
        }
    }

    void Update()
    {
        HandleInput();
        CheckSurroundings();
        HandleWallSliding();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleInput()
    {
        // Horizontal movement
        float moveInput = Input.GetAxisRaw("Horizontal");

        // Flip sprite based on direction
        if (moveInput != 0)
        {
            facingDirection = (int)Mathf.Sign(moveInput);
            transform.localScale = new Vector3(facingDirection, 1, 1);
        }

        // Jumping
        if (Input.GetButtonDown("Jump") && (isGrounded || isWallSliding))
        {
            Jump();
        }

        // Wall jump
        if (Input.GetButtonDown("Jump") && isWallSliding && !isGrounded)
        {
            WallJump();
        }
    }

    void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
    }

    void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    void WallJump()
    {
        rb.velocity = new Vector2(-facingDirection * moveSpeed * 0.7f, jumpForce * 0.8f);
    }

    void CheckSurroundings()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        isTouchingWall = Physics2D.Raycast(wallCheck.position, Vector2.right * facingDirection, wallCheckDistance, wallLayer);
    }

    void HandleWallSliding()
    {
        if (isTouchingWall && !isGrounded && rb.velocity.y < 0)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
        }
        else
        {
            isWallSliding = false;
        }
    }

    void OnDrawGizmos()
    {
        // Visualize ground check
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // Visualize wall check
        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.right * facingDirection * wallCheckDistance);
        }
    }
}
```

### **Step 3: Setup Layers and Physics**
1. Create layers: **Edit > Project Settings > Tags and Layers**
   - Add layer: `Ground`
   - Add layer: `Wall`

2. Assign layers to world (we'll do this when we add platforms)

### **Step 4: Add Camera Follow**
1. Create: **Main Camera** (if not exists)
2. Add script: **CameraFollow2D**

```csharp
using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 2, -10);
    public float lookAheadDistance = 2f;

    void Start()
    {
        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Calculate look-ahead position
        float lookAhead = target.GetComponent<Rigidbody2D>().velocity.x * 0.1f;
        Vector3 targetPosition = target.position + offset + Vector3.right * lookAhead;

        // Smooth camera movement
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}
```

### **Step 5: Test Player Movement**
1. Click Play
2. Use **A/D** or **Arrow Keys** to move
3. **Space** to jump
4. Test wall jumping by jumping toward walls
5. Stop playing

---

## 🏗️ **Phase 3: Platforms and Terrain (30 minutes)**

### **Step 1: Create Platform Prefab**
1. Right-click: **Create Empty** > Name: `Platform`
2. Add: **Box Collider 2D** (set as trigger: ❌)
3. Add: **Sprite Renderer** (use square sprite)
4. Scale: X=`4`, Y=`1` (long platform)
5. Set layer to: `Ground`
6. Drag to Project window to create prefab

### **Step 2: Add Ground**
1. Duplicate platform prefab multiple times
2. Arrange them to create ground:
   ```
   [Platform][Platform][Platform][Platform]
   ```
3. Position at Y=`-4` (below player start)

### **Step 3: Add Walls and Obstacles**
1. Create vertical platforms for walls
2. Scale: X=`1`, Y=`6`
3. Set layer to: `Wall`
4. Place walls to create rooms and barriers

### **Step 4: Add Moving Platforms** (Advanced)
Create a script for moving platforms:

```csharp
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float speed = 2f;

    private Vector3 targetPosition;

    void Start()
    {
        targetPosition = pointB.position;
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            targetPosition = (targetPosition == pointA.position) ? pointB.position : pointA.position;
        }
    }

    void OnDrawGizmos()
    {
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointA.position, pointB.position);
        }
    }
}
```

---

## 🤖 **Phase 4: Enemies (30 minutes)**

### **Step 1: Create Basic Enemy**
1. Right-click: **Create Empty** > Name: `BasicEnemy`
2. Add: **Rigidbody 2D** (gravity: `3`, freeze rotation Z: ✅)
3. Add: **Box Collider 2D** (Size: X=`0.8`, Y=`1.8`)
4. Add: **Sprite Renderer** (different color than player)

### **Step 2: Enemy AI Script**
Add script: `BasicEnemyAI`

```csharp
using UnityEngine;

public class BasicEnemyAI : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float detectionRange = 8f;
    public float attackRange = 1.5f;
    public int damage = 1;
    public float attackCooldown = 1f;

    private Transform player;
    private Rigidbody2D rb;
    private bool canAttack = true;
    private float lastAttackTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        FindPlayer();
    }

    void FindPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            // Move toward player
            Vector3 direction = (player.position - transform.position).normalized;
            rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);

            // Flip sprite to face player
            transform.localScale = new Vector3(Mathf.Sign(direction.x), 1, 1);

            // Attack if in range
            if (distanceToPlayer <= attackRange && canAttack && Time.time > lastAttackTime + attackCooldown)
            {
                Attack();
            }
        }
        else
        {
            // Idle movement or patrol
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    void Attack()
    {
        // Deal damage to player (we'll add health system next)
        Debug.Log("Enemy attacks player!");
        lastAttackTime = Time.time;

        // Add attack animation/cooldown logic here
    }

    void OnDrawGizmosSelected()
    {
        // Visualize detection and attack ranges
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
```

### **Step 3: Place Enemies**
1. Create multiple enemy instances
2. Place them in different areas of the world
3. Test that they detect and chase the player

---

## ❤️ **Phase 5: Health & Combat (30 minutes)**

### **Step 1: Player Health System**
Add to PlayerController:

```csharp
[Header("Health")]
public int maxHealth = 3;
public int currentHealth;

void Start()
{
    // ... existing code ...
    currentHealth = maxHealth;
}

public void TakeDamage(int damage)
{
    currentHealth -= damage;
    Debug.Log($"Player health: {currentHealth}");

    if (currentHealth <= 0)
    {
        Die();
    }
}

void Die()
{
    Debug.Log("Player died!");
    // Add death logic here (respawn, game over, etc.)
}
```

### **Step 2: Enemy Attack Integration**
Modify BasicEnemyAI Attack method:

```csharp
void Attack()
{
    PlayerController playerController = player.GetComponent<PlayerController>();
    if (playerController != null)
    {
        playerController.TakeDamage(damage);
    }

    lastAttackTime = Time.time;
    // Add attack animation/cooldown
}
```

### **Step 3: Add Player Attack**
Add to PlayerController:

```csharp
[Header("Combat")]
public float attackRange = 1.5f;
public int attackDamage = 1;
public float attackCooldown = 0.5f;

private float lastAttackTime;

void HandleInput()
{
    // ... existing code ...

    // Attack
    if (Input.GetButtonDown("Fire1") && Time.time > lastAttackTime + attackCooldown)
    {
        Attack();
    }
}

void Attack()
{
    // Check for enemies in attack range
    Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange);

    foreach (Collider2D enemy in hitEnemies)
    {
        BasicEnemyAI enemyAI = enemy.GetComponent<BasicEnemyAI>();
        if (enemyAI != null)
        {
            // Damage enemy (we'll add enemy health next)
            Debug.Log("Player attacks enemy!");
        }
    }

    lastAttackTime = Time.time;
}
```

---

## 💰 **Phase 6: Collectibles & Power-ups (20 minutes)**

### **Step 1: Create Coin Collectible**
1. Create: **Coin** object
2. Add: **Circle Collider 2D** (Is Trigger: ✅)
3. Add: **Sprite Renderer** (yellow circle)
4. Add script: `CoinCollectible`

```csharp
using UnityEngine;

public class CoinCollectible : MonoBehaviour
{
    public int coinValue = 1;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Add to player score (we'll add scoring system)
            Debug.Log($"Collected coin worth {coinValue}!");
            Destroy(gameObject);
        }
    }
}
```

### **Step 2: Add Scoring System**
Create: `GameManager` script

```csharp
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int score = 0;
    public int coins = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        score += amount * 10; // 10 points per coin
        Debug.Log($"Coins: {coins}, Score: {score}");
    }
}
```

Modify CoinCollectible:

```csharp
void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Player"))
    {
        GameManager.Instance.AddCoins(coinValue);
        Destroy(gameObject);
    }
}
```

### **Step 3: Add Health Pickup**
Create: `HealthPickup` script

```csharp
using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    public int healAmount = 1;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && player.currentHealth < player.maxHealth)
            {
                player.currentHealth += healAmount;
                Debug.Log($"Healed! Health: {player.currentHealth}");
                Destroy(gameObject);
            }
        }
    }
}
```

---

## 🎯 **Phase 7: Polish & Testing (30 minutes)**

### **Step 1: Add UI**
1. Create: **Canvas** (UI > Canvas)
2. Add: **Text - TextMeshPro** for score/health
3. Position in corners
4. Create script to update UI

### **Step 2: Add Sound Effects**
1. Add: **Audio Source** components
2. Import sound files
3. Play sounds on events (jump, collect, damage)

### **Step 3: Add Respawn System**
1. Create checkpoints
2. Save player position
3. Respawn on death

### **Step 4: Final Testing**
1. Test all mechanics
2. Check for bugs
3. Balance difficulty
4. Get feedback from friends

---

## 🎉 **Congratulations! You Built a Game!**

**What you created:**
- ✅ Procedural world generation
- ✅ Full player movement system
- ✅ Enemy AI with combat
- ✅ Health and scoring systems
- ✅ Collectibles and power-ups
- ✅ Complete game loop

### **Sharing Your Game**
1. **Build**: File > Build Settings > Add scene > Build
2. **Test Build**: Run the executable
3. **Share**: Send to friends or upload to itch.io

### **Next Steps**
- **Add More Enemies**: Flying enemies, ranged attacks
- **Create Levels**: Design specific challenges
- **Add Bosses**: Epic end-of-level fights
- **Polish Art**: Custom sprites and animations
- **Add Story**: Cutscenes and narrative

### **Advanced Tutorials**
- **[Custom Biome Tutorial](custom-biome.md)** - Create unique environmental themes
- **[Enemy Behavior Tutorial](enemy-behavior.md)** - Design complex AI patterns
- **[Multiplayer Setup](../advanced/multiplayer.md)** - Add co-op gameplay

---

*"You just went from knowing nothing about MetVanDAMN to building a complete game! That's incredible. The journey of a thousand games begins with a single Play button."*

**What will you create next?**

**🍑 ✨ Amazing Work! You're a Game Developer Now! ✨ 🍑**
