# 🎮 Creating Your First MetVanDAMN World
## *From Empty Scene to Epic Adventure*

> **"Your first world is like your first painting - it might not be perfect, but it's yours, and that's what makes it amazing. Let's create something awesome together!"**

---

## 🎯 **What You'll Build**
A complete procedural world with:
- ✅ 3 connected districts
- ✅ Working player character
- ✅ Enemies that navigate
- ✅ Treasure to collect
- ✅ Debug visualization

**Time**: 20 minutes
**Difficulty**: Beginner
**Skills**: Unity basics, following steps

---

## 🛠️ **Step 1: Start Fresh (2 minutes)**

### **Create New Scene**
1. In Unity: **File > New Scene**
2. Save as: `MyFirstWorld.unity`
3. Location: `Assets/Scenes/` (create folder if needed)

**Why a new scene?** We want a clean slate to build on!

---

## 🌍 **Step 2: Add World Generation (3 minutes)**

### **The World Generator Component**
1. In Hierarchy, right-click **Create Empty**
2. Name it: `WorldGenerator`
3. With `WorldGenerator` selected, click **Add Component**
4. Search for: `Smoke Test Scene Setup`
5. Click **Add**

### **Configure Your World**
In the Inspector (right panel), set:

**Basic Settings**:
- **World Seed**: `42` (or any number - try different ones!)
- **World Size**: X=`50`, Y=`50` (good starting size)
- **Target Sector Count**: `3` (number of districts)

**Debug Settings**:
- **Enable Debug Visualization**: ✅ Checked
- **Log Generation Steps**: ✅ Checked

**Click Play!** You should see console messages about world generation.

---

## 🎮 **Step 3: Add Player Character (5 minutes)**

### **Create Player Object**
1. Right-click in Hierarchy: **Create Empty**
2. Name it: `Player`
3. Reset transform: Click gear icon > Reset

### **Add Player Components**
With `Player` selected:

1. **Add Component > Character Controller**
   - Height: `1.8`
   - Radius: `0.4`

2. **Add Component > Rigidbody**
   - Mass: `1`
   - Drag: `0`
   - Angular Drag: `0.05`
   - Uncheck "Use Gravity" (we'll handle jumping manually)

3. **Add Component > Capsule Collider**
   - Center Y: `0.9`
   - Radius: `0.4`
   - Height: `1.8`

### **Add Player Movement Script**
1. **Add Component > New Script**
2. Name: `SimplePlayerController`
3. Language: **C#**
4. Click **Create and Add**

### **Write the Movement Code**
Double-click the script to open it. Replace everything with:

```csharp
using UnityEngine;

public class SimplePlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;
    public float gravity = -30f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (groundCheck == null)
        {
            // Create ground check if not set
            groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.parent = transform;
            groundCheck.localPosition = Vector3.down * 0.9f;
        }
    }

    void Update()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to stay grounded
        }

        // Get input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Move
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
```

### **Setup Ground Check**
1. Back in Unity, select the `Player`
2. Find the `SimplePlayerController` component
3. Drag the `GroundCheck` object (child) into the **Ground Check** field
4. Set **Ground Mask** to **Default** (or create a "Ground" layer)

---

## 🎨 **Step 4: Add Visuals (3 minutes)**

### **Make Player Visible**
1. Right-click `Player` > **3D Object > Capsule**
2. This creates a visible capsule shape
3. Scale it to: X=`0.8`, Y=`1.8`, Z=`0.8`

### **Add Camera**
1. Right-click `Player` > **Create Empty**
2. Name it: `CameraRig`
3. Right-click `CameraRig` > **Camera**
4. Position camera at: X=`0`, Y=`2`, Z=`-5`
5. In Camera component:
   - Clear Flags: **Solid Color**
   - Background: Light blue (R=0.5, G=0.7, B=1.0)

### **Make Camera Follow Player**
1. Select `CameraRig`
2. **Add Component > New Script**
3. Name: `CameraFollow`
4. Replace code with:

```csharp
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 2, -5);

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
            transform.LookAt(target.position + Vector3.up);
        }
    }
}
```

5. Drag `Player` into the **Target** field

---

## 🤖 **Step 5: Add Simple Enemy (4 minutes)**

### **Create Enemy**
1. Right-click in Hierarchy: **Create Empty**
2. Name: `SimpleEnemy`
3. Position somewhere visible (try X=`10`, Y=`1`, Z=`0`)

### **Add Enemy Components**
1. **Add Component > Capsule Collider**
   - Height: `1.8`, Radius: `0.4`
2. **Add Component > Rigidbody**
   - Uncheck "Use Gravity"

### **Make Enemy Visible**
1. Right-click `SimpleEnemy` > **3D Object > Capsule**
2. Scale to match collider

### **Add Simple AI Script**
1. **Add Component > New Script**
2. Name: `SimpleEnemyAI`
3. Replace code with:

```csharp
using UnityEngine;

public class SimpleEnemyAI : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 3f;
    public float detectionRange = 15f;

    void Start()
    {
        // Find player automatically
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            player = GameObject.Find("Player")?.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= detectionRange)
        {
            // Move toward player
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;

            // Look at player
            transform.LookAt(player.position);
        }
    }
}
```

### **Tag the Player**
1. Select `Player`
2. In Inspector, click tag dropdown
3. Choose **Player** (or create new tag if needed)

---

## 💰 **Step 6: Add Treasure (3 minutes)**

### **Create Treasure**
1. Right-click: **3D Object > Cube**
2. Name: `Treasure`
3. Position: X=`5`, Y=`1`, Z=`5`
4. Scale: X=`0.5`, Y=`0.5`, Z=`0.5`
5. Color: Gold/Yellow

### **Add Collection Script**
1. **Add Component > New Script**
2. Name: `TreasureCollect`
3. Replace code with:

```csharp
using UnityEngine;

public class TreasureCollect : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("🎉 Treasure collected!");
            Destroy(gameObject);
        }
    }
}
```

### **Make it Collectible**
1. **Add Component > Box Collider**
2. Check **Is Trigger**
3. Adjust size to match cube

---

## 🎯 **Step 7: Test Your World! (2 minutes)**

### **Run the Scene**
1. Click **Play**
2. Use **WASD** to move
3. **Space** to jump
4. Walk near the enemy - it should follow you!
5. Touch the treasure - it should disappear with a message!

### **Debug Visualization**
- Press **M** to see the world map
- Look for the green debug bounds showing your generated world
- Console should show generation messages

---

## 🎉 **Congratulations! You Built a World!**

**What you created:**
- ✅ Procedural world with 3 districts
- ✅ Player that moves and jumps
- ✅ Enemy that follows the player
- ✅ Collectible treasure
- ✅ Working camera system

### **What to Try Next**

**Experiment Ideas**:
- Change the **world seed** to see different layouts
- Add more **enemies** in different positions
- Create **multiple treasures** to collect
- Adjust **player speed** and **jump height**

**Advanced Challenges**:
- Add **health systems** to player and enemies
- Create **different enemy types** (ranged, flying, etc.)
- Add **sound effects** for collecting treasure
- Create **checkpoints** or **save systems**

### **Common Issues & Fixes**

**"Player falls through ground"**
- Add a ground plane: GameObject > 3D Object > Plane
- Or enable gravity on player and add ground

**"Enemy doesn't move"**
- Check that player has "Player" tag
- Verify enemy script is attached and enabled

**"Can't see world generation"**
- Check console for error messages
- Verify SmokeTestSceneSetup component is on WorldGenerator

**"Camera doesn't follow"**
- Ensure CameraFollow script has player assigned to target field

---

## 📚 **What's Happening Behind the Scenes**

**World Generation**: MetVanDAMN creates districts, rooms, and connections automatically

**ECS Systems**: Behind the scenes, Unity's Entity Component System manages:
- Navigation calculations
- Biome field effects
- AI pathfinding
- Performance optimization

**Your Code**: The scripts you wrote handle:
- Player input and movement
- Enemy AI behavior
- Treasure collection
- Camera following

**Together**: Procedural generation + your gameplay = complete game worlds!

---

*"You just took your first steps as a world builder. Every game developer started exactly like this - with a simple scene that grew into something amazing. What's your next creation going to be?"*

**Ready for more?** Check out **[Configuration Guide](../building-worlds/configuration.md)** to customize your worlds!

**🍑 ✨ Amazing Work! ✨ 🍑**
