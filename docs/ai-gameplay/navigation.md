# 🤖 Navigation System
## *How Characters Find Their Way Around Your World*

> **"Good navigation makes your world feel alive. Bad navigation makes players feel lost and frustrated."**

[![Navigation](https://img.shields.io/badge/Navigation-AI-blue.svg)](navigation.md)
[![Unity 6000.2+](https://img.shields.io/badge/Unity-6000.2+-black.svg?style=flat&logo=unity)](https://unity3d.com/get-unity/download)

---

## 🎯 **What is Navigation?**

**Navigation** is how characters (players, enemies, NPCs) move around your procedural world. MetVanDAMN uses Unity's advanced **NavMesh** system combined with custom AI to handle:

- 🏃 **Pathfinding** - Finding the best route from A to B
- 🚪 **Door/Gate Logic** - Understanding when abilities are needed
- 👥 **Crowd Management** - Multiple characters moving without blocking each other
- 🧠 **Smart Decisions** - Choosing between multiple paths based on strategy

**Why it matters**: Without good navigation, enemies can't chase you, NPCs can't patrol, and players can get stuck!

---

## 🏗️ **How Navigation Works**

### **The Three Layers**

1. **World Structure** - Districts, rooms, and connections
2. **NavMesh Baking** - Unity calculates walkable surfaces
3. **AI Pathfinding** - Characters request and follow paths

### **Key Components**

```csharp
// Basic navigation setup for an enemy
public class EnemyController : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform target;

    void Update()
    {
        if (target != null)
        {
            agent.SetDestination(target.position);
        }
    }
}
```

---

## 🚀 **Quick Setup (5 Minutes)**

### **Step 1: Add Navigation to Your Scene**

```csharp
using UnityEngine;
using UnityEngine.AI;

// Attach this to any character that needs to navigate
public class BasicNavigator : MonoBehaviour
{
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        // Agent is now ready to navigate!
    }

    public void MoveTo(Vector3 destination)
    {
        agent.SetDestination(destination);
    }
}
```

### **Step 2: Bake Your World**

1. Go to `Window > AI > Navigation`
2. Select your world geometry
3. Click **Bake**
4. Characters can now navigate!

### **Step 3: Test It**

```csharp
// Test navigation with debug
void Update()
{
    if (Input.GetMouseButtonDown(0))
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GetComponent<BasicNavigator>().MoveTo(hit.point);
        }
    }
}
```

---

## 🎮 **Navigation for Different Character Types**

### **Player Navigation**

```csharp
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private NavMeshAgent agent;

    void Update()
    {
        // Click to move (point-and-click style)
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                agent.SetDestination(hit.point);
            }
        }

        // Or use WASD for direct control
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (horizontal != 0 || vertical != 0)
        {
            agent.ResetPath(); // Stop auto-navigation
            transform.Translate(new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime);
        }
    }
}
```

### **Patrolling Enemy**

```csharp
public class PatrollingEnemy : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waitTime = 2f;
    [SerializeField] private NavMeshAgent agent;

    private int currentWaypoint = 0;
    private float waitTimer = 0f;

    void Update()
    {
        if (agent.remainingDistance < 0.1f && !agent.pathPending)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                // Move to next waypoint
                currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
                agent.SetDestination(waypoints[currentWaypoint].position);
                waitTimer = 0f;
            }
        }
    }
}
```

### **Chasing Enemy**

```csharp
public class ChasingEnemy : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private NavMeshAgent agent;

    private bool isChasing = false;

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            // Start chasing
            isChasing = true;
            agent.SetDestination(player.position);
        }
        else if (isChasing)
        {
            // Lost player, stop chasing
            isChasing = false;
            agent.ResetPath();
        }
    }
}
```

---

## 🧠 **Advanced Navigation Features**

### **Ability-Aware Navigation**

```csharp
public class AbilityNavigator : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private PlayerAbilities abilities;

    public void NavigateToWithAbilities(Vector3 destination, AbilityType requiredAbility)
    {
        // Check if player has required ability
        if (abilities.HasAbility(requiredAbility))
        {
            agent.SetDestination(destination);
        }
        else
        {
            // Find alternative path or show "locked" message
            Debug.Log("Need " + requiredAbility + " to go there!");
        }
    }
}
```

### **Dynamic Obstacles**

```csharp
public class DynamicObstacle : MonoBehaviour
{
    [SerializeField] private NavMeshObstacle obstacle;

    public void EnableObstacle()
    {
        obstacle.enabled = true;
        // Forces navigation to recalculate around this obstacle
    }

    public void DisableObstacle()
    {
        obstacle.enabled = false;
    }
}
```

### **Off-Mesh Links (Advanced Jumps)**

```csharp
public class JumpLink : MonoBehaviour
{
    [SerializeField] private OffMeshLink link;
    [SerializeField] private float jumpSpeed = 5f;

    void Start()
    {
        link.activated = true;
    }

    // Called when agent starts traversing this link
    public void OnTraverseOffMeshLink(NavMeshAgent agent)
    {
        // Custom jump animation/logic here
        StartCoroutine(JumpAcross(agent));
    }

    private IEnumerator JumpAcross(NavMeshAgent agent)
    {
        agent.enabled = false; // Take control

        Vector3 startPos = transform.position;
        Vector3 endPos = link.endTransform.position;
        float journeyLength = Vector3.Distance(startPos, endPos);
        float startTime = Time.time;

        while (Vector3.Distance(transform.position, endPos) > 0.1f)
        {
            float distCovered = (Time.time - startTime) * jumpSpeed;
            float fractionOfJourney = distCovered / journeyLength;
            transform.position = Vector3.Lerp(startPos, endPos, fractionOfJourney);
            yield return null;
        }

        agent.enabled = true; // Give control back
        agent.CompleteOffMeshLink(); // Tell nav system we're done
    }
}
```

---

## 🔧 **Debugging Navigation**

### **Visual Debugging**

```csharp
public class NavigationDebugger : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private LineRenderer pathRenderer;

    void Update()
    {
        if (agent.hasPath)
        {
            // Draw the path
            pathRenderer.positionCount = agent.path.corners.Length;
            pathRenderer.SetPositions(agent.path.corners);
        }
    }
}
```

### **Common Issues & Fixes**

| Problem | Symptom | Solution |
|---------|---------|----------|
| **Agent Stuck** | Character doesn't move | Check if destination is on NavMesh |
| **Path Not Found** | `hasPath` is false | Bake NavMesh or check obstacles |
| **Jittery Movement** | Character shakes | Adjust agent stopping distance |
| **Wrong Speed** | Too fast/slow | Set `agent.speed` property |

---

## 🎯 **Best Practices**

### **Performance Tips**
- Bake NavMesh once, not every frame
- Use appropriate agent radius/height
- Cache path calculations when possible
- Consider using multiple NavMeshes for large worlds

### **Design Tips**
- Make navigation intuitive for players
- Use visual cues for navigation options
- Test with different character sizes
- Consider accessibility (colorblind, motor impaired)

### **Code Tips**
- Always check `agent.isOnNavMesh` before setting destinations
- Use `agent.Warp()` for instant teleportation
- Handle `OnNavMeshLinkTraversal` for custom movement
- Cache frequently used paths

---

## 🚀 **Next Steps**

**Ready to make smarter enemies?**
- **[Enemy AI Guide](enemies.md)** - Create intelligent enemy behaviors
- **[Player Systems](player.md)** - Advanced player movement and abilities
- **[Performance Optimization](../advanced/performance.md)** - Speed up navigation for big worlds

**Need help?**
- Check the [debug visualization](../art-visuals/debug-visualization.md) tools
- Look at the [working examples](../../Assets/Scenes/) in the project
- Ask questions on [GitHub Discussions](https://github.com/jmeyer1980/TWG-MetVanDamn/discussions)

---

*"Navigation is the invisible thread that connects every part of your game world. Make it strong!"*

**🍑 Happy Pathfinding! 🍑**
