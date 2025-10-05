# 🦸 Player Systems
## *Creating Responsive, Fun Player Characters*

> **"The player character is the player's avatar in your world. Make them feel powerful, responsive, and fun to control!"**

[![Player Systems](https://img.shields.io/badge/Player-Systems-green.svg)](player.md)
[![Unity 6000.2+](https://img.shields.io/badge/Unity-6000.2+-black.svg?style=flat&logo=unity)](https://unity3d.com/get-unity/download)

---

## 🎯 **What Makes a Great Player Character?**

**Player systems** handle everything about how the player moves, fights, and interacts with your world. Great player characters:

- 🎮 **Responsive** - Controls feel tight and immediate
- 🎯 **Powerful** - Abilities feel satisfying to use
- 📈 **Progressive** - Gets stronger as the game advances
- 🎭 **Expressive** - Animations and effects show personality
- ⚖️ **Balanced** - Challenging but not frustrating

**Remember**: Players will spend hours controlling this character. Make it enjoyable!

---

## 🏗️ **Player System Architecture**

### **Core Components**

1. **Movement System** - Walking, running, jumping
2. **Combat System** - Attacking, defending, abilities
3. **Health & Progression** - Taking damage, leveling up
4. **Inventory & Abilities** - Collecting items, unlocking powers
5. **Animation & Effects** - Visual feedback for actions

### **Basic Player Structure**

```csharp
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float groundCheckDistance = 0.1f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackCooldown = 0.5f;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // State
    private bool isGrounded = true;
    private bool canAttack = true;
    private float lastAttackTime = 0f;
    private Vector2 movementInput;

    void Update()
    {
        // Get input
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");

        // Handle actions
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        if (Input.GetButtonDown("Fire1") && canAttack)
        {
            Attack();
        }

        // Update visuals
        UpdateAnimation();
        UpdateFacingDirection();
    }

    void FixedUpdate()
    {
        // Apply movement
        Move();
        CheckGrounded();
    }
}
```

---

## 🚀 **Quick Player Setup (15 Minutes)**

### **Step 1: Basic Movement**

```csharp
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    private bool isGrounded = false;
    private float groundCheckRadius = 0.2f;

    void Update()
    {
        // Horizontal movement
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

        // Jumping
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        // Flip sprite based on direction
        if (moveInput > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput < 0)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    void FixedUpdate()
    {
        // Ground check
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
```

### **Step 2: Add Combat**

```csharp
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackCooldown = 0.3f;

    [SerializeField] private Animator animator;

    private float lastAttackTime = 0f;

    void Update()
    {
        if (Input.GetButtonDown("Fire1") && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }
    }

    private void Attack()
    {
        lastAttackTime = Time.time;

        // Play attack animation
        animator.SetTrigger("Attack");

        // Detect enemies in range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        // Damage them
        foreach (Collider2D enemy in hitEnemies)
        {
            enemy.GetComponent<EnemyHealth>().TakeDamage(attackDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}
```

### **Step 3: Add Health & Respawn**

```csharp
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float invincibilityTime = 1f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private int currentHealth;
    private bool isInvincible = false;
    private float invincibilityTimer = 0f;

    public event Action<int, int> OnHealthChanged; // current, max
    public event Action OnPlayerDeath;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Update()
    {
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0)
            {
                isInvincible = false;
                spriteRenderer.color = Color.white; // Normal color
            }
            else
            {
                // Flash effect
                spriteRenderer.color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(Time.time * 10, 1));
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Become invincible briefly
            isInvincible = true;
            invincibilityTimer = invincibilityTime;
        }

        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        Debug.Log("Player died!");
        OnPlayerDeath?.Invoke();

        // Could respawn, show game over screen, etc.
        // For now, just disable
        gameObject.SetActive(false);
    }
}
```

---

## 🎮 **Advanced Player Features**

### **Ability System**

```csharp
[System.Serializable]
public enum AbilityType
{
    DoubleJump,
    Dash,
    WallJump,
    Shoot,
    Shield
}

public class PlayerAbilities : MonoBehaviour
{
    [System.Serializable]
    public class AbilityData
    {
        public AbilityType type;
        public bool unlocked = false;
        public KeyCode keybind = KeyCode.None;
    }

    [SerializeField] private AbilityData[] abilities;

    public bool HasAbility(AbilityType ability)
    {
        var data = abilities.FirstOrDefault(a => a.type == ability);
        return data != null && data.unlocked;
    }

    public void UnlockAbility(AbilityType ability)
    {
        var data = abilities.FirstOrDefault(a => a.type == ability);
        if (data != null)
        {
            data.unlocked = true;
            Debug.Log($"Unlocked ability: {ability}");
        }
    }

    void Update()
    {
        // Handle ability inputs
        foreach (var ability in abilities)
        {
            if (ability.unlocked && Input.GetKeyDown(ability.keybind))
            {
                UseAbility(ability.type);
            }
        }
    }

    private void UseAbility(AbilityType ability)
    {
        switch (ability)
        {
            case AbilityType.DoubleJump:
                // Implement double jump
                break;
            case AbilityType.Dash:
                // Implement dash
                break;
            // etc.
        }
    }
}
```

### **Double Jump Ability**

```csharp
public class DoubleJumpAbility : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private float doubleJumpForce = 8f;

    private bool canDoubleJump = false;
    private bool hasDoubleJumped = false;

    void Start()
    {
        // Listen for ground touch to reset double jump
        movement.OnGrounded += ResetDoubleJump;
    }

    public void TryDoubleJump()
    {
        if (canDoubleJump && !hasDoubleJumped)
        {
            movement.GetComponent<Rigidbody2D>().AddForce(Vector2.up * doubleJumpForce, ForceMode2D.Impulse);
            hasDoubleJumped = true;
        }
    }

    private void ResetDoubleJump()
    {
        hasDoubleJumped = false;
    }

    public void Unlock()
    {
        canDoubleJump = true;
    }
}
```

### **Dash Ability**

```csharp
public class DashAbility : MonoBehaviour
{
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    private Rigidbody2D rb;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float cooldownTimer = 0f;
    private Vector2 dashDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                EndDash();
            }
        }
        else if (Input.GetKeyDown(KeyCode.LeftShift) && cooldownTimer <= 0)
        {
            StartDash();
        }
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        cooldownTimer = dashCooldown;

        // Get dash direction from input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (horizontal == 0 && vertical == 0)
        {
            // Dash in facing direction
            horizontal = transform.localScale.x > 0 ? 1 : -1;
        }

        dashDirection = new Vector2(horizontal, vertical).normalized;

        // Apply dash velocity
        rb.velocity = dashDirection * dashSpeed;

        Debug.Log("Dash started!");
    }

    private void EndDash()
    {
        isDashing = false;
        // Reset to normal movement
        rb.velocity = Vector2.zero;
        Debug.Log("Dash ended!");
    }
}
```

### **Inventory System**

```csharp
[System.Serializable]
public class InventoryItem
{
    public string name;
    public Sprite icon;
    public int quantity = 1;
    public bool isKeyItem = false; // Can't drop key items
}

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int maxSlots = 20;
    [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();

    public event Action OnInventoryChanged;

    public bool AddItem(InventoryItem newItem)
    {
        // Check if we can stack this item
        var existingItem = items.Find(item => item.name == newItem.name);
        if (existingItem != null && !existingItem.isKeyItem)
        {
            existingItem.quantity += newItem.quantity;
            OnInventoryChanged?.Invoke();
            return true;
        }

        // Check if we have space
        if (items.Count >= maxSlots)
        {
            Debug.Log("Inventory full!");
            return false;
        }

        // Add new item
        items.Add(newItem);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(string itemName, int quantity = 1)
    {
        var item = items.Find(i => i.name == itemName);
        if (item != null && !item.isKeyItem)
        {
            item.quantity -= quantity;
            if (item.quantity <= 0)
            {
                items.Remove(item);
            }
            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool HasItem(string itemName, int quantity = 1)
    {
        var item = items.Find(i => i.name == itemName);
        return item != null && item.quantity >= quantity;
    }

    public List<InventoryItem> GetItems()
    {
        return new List<InventoryItem>(items);
    }
}
```

---

## 🎨 **Animation & Visual Effects**

### **Animation Controller**

```csharp
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerCombat combat;

    private void Update()
    {
        // Movement animations
        bool isMoving = Mathf.Abs(movement.GetHorizontalInput()) > 0.1f;
        animator.SetBool("IsMoving", isMoving);
        animator.SetBool("IsGrounded", movement.IsGrounded());
        animator.SetFloat("VerticalVelocity", movement.GetVerticalVelocity());

        // Combat animations
        if (combat.IsAttacking())
        {
            animator.SetTrigger("Attack");
        }

        // Facing direction
        if (movement.GetHorizontalInput() > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (movement.GetHorizontalInput() < 0)
        {
            spriteRenderer.flipX = true;
        }
    }
}
```

### **Particle Effects**

```csharp
public class PlayerEffects : MonoBehaviour
{
    [SerializeField] private ParticleSystem jumpParticles;
    [SerializeField] private ParticleSystem dashParticles;
    [SerializeField] private ParticleSystem damageParticles;

    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerHealth health;

    void Start()
    {
        // Subscribe to events
        movement.OnJump += PlayJumpEffect;
        movement.OnDash += PlayDashEffect;
        health.OnTakeDamage += PlayDamageEffect;
    }

    private void PlayJumpEffect()
    {
        jumpParticles.Play();
    }

    private void PlayDashEffect()
    {
        dashParticles.Play();
    }

    private void PlayDamageEffect()
    {
        damageParticles.Play();
    }
}
```

---

## 🔧 **Debugging & Balancing**

### **Player Debug Display**

```csharp
public class PlayerDebugDisplay : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private TMPro.TextMeshProUGUI debugText;

    void Update()
    {
        if (debugText != null)
        {
            debugText.text = $"Position: {transform.position:F2}\n" +
                           $"Velocity: {player.GetVelocity():F2}\n" +
                           $"Grounded: {player.IsGrounded()}\n" +
                           $"Health: {player.GetCurrentHealth()}/{player.GetMaxHealth()}\n" +
                           $"Abilities: {string.Join(", ", player.GetUnlockedAbilities())}";
        }
    }
}
```

### **Performance Monitoring**

```csharp
public class PlayerPerformanceMonitor : MonoBehaviour
{
    private float updateTime = 0f;
    private int frameCount = 0;
    private float fps = 0f;

    void Update()
    {
        frameCount++;
        updateTime += Time.deltaTime;

        if (updateTime >= 1f)
        {
            fps = frameCount / updateTime;
            frameCount = 0;
            updateTime = 0f;

            // Log if FPS drops too low
            if (fps < 30f)
            {
                Debug.LogWarning($"Low FPS detected: {fps:F1}. Player position: {transform.position}");
            }
        }
    }
}
```

---

## 🎯 **Best Practices**

### **Controls**
- **Consistent** - Same inputs do the same things
- **Responsive** - Minimal input lag
- **Accessible** - Work with different input methods
- **Intuitive** - Easy to learn, hard to master

### **Feedback**
- **Visual** - Clear animations and effects
- **Audio** - Sound effects for actions
- **Haptic** - Controller vibration when available
- **UI** - Health bars, ability cooldowns

### **Balance**
- **Progressive** - Abilities unlock naturally
- **Challenging** - Requires skill to master
- **Forgiving** - Fair difficulty curve
- **Rewarding** - Satisfying to use abilities

### **Performance**
- **Efficient** - Minimal calculations per frame
- **Optimized** - Use object pooling for effects
- **Scalable** - Works on different hardware
- **Tested** - Regular performance monitoring

---

## 🚀 **Next Steps**

**Ready to enhance your player?**
- **[Ability System Tutorial](../../tutorials/custom-abilities.md)** - Create unique player powers
- **[Animation Guide](../art-visuals/animation.md)** - Polish movement and combat animations
- **[UI Systems](../ui/player-ui.md)** - Add health bars and ability displays

**Need examples?**
- Check the [demo player](../../Assets/Scenes/MetVanDAMN_Demo.unity) in the project
- Study the [player controller scripts](../../Assets/Scripts/Player/) included
- Join [community discussions](https://github.com/jmeyer1980/TWG-MetVanDamn/discussions) for tips

---

*"A great player character makes players feel powerful, skilled, and immersed in your world."*

**🍑 Happy Player Crafting! 🍑**
