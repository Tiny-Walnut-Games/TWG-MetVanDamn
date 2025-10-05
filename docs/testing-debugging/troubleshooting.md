# 🔧 Troubleshooting Guide
## *Fixing Common MetVanDAMN Problems*

> **"Every bug is a feature waiting to be discovered. Some just need more debugging than others."**

[![Troubleshooting](https://img.shields.io/badge/Troubleshooting-Guide-red.svg)](troubleshooting.md)
[![Common Issues](https://img.shields.io/badge/Common-Issues-yellow.svg)](troubleshooting.md)

---

## 🎯 **How to Use This Guide**

**Having problems with MetVanDAMN?** This guide covers the most common issues and their solutions. For each problem:

- 🚨 **Symptoms** - What you'll see
- 🔍 **Causes** - Why it happens
- ✅ **Solutions** - How to fix it
- 🧪 **Testing** - How to verify the fix

**Start with the symptom that matches your problem!**

---

## 🚨 **World Generation Issues**

### **"No world generates"**

**Symptoms:**
- Empty scene after running generation
- Console shows no generation logs
- Entity count stays at 0

**Causes:**
- Missing world configuration
- Systems not running
- ECS world not initialized

**Solutions:**

1. **Check World Configuration**
```csharp
// Add this to debug
var em = World.DefaultGameObjectInjectionWorld.EntityManager;
bool hasConfig = false;
foreach (var entity in em.GetAllEntities())
{
    if (em.HasComponent<WorldConfiguration>(entity))
    {
        hasConfig = true;
        break;
    }
}
Debug.Log($"World config exists: {hasConfig}");
```

2. **Verify System Execution**
```csharp
// Add logging to generation system
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class DebugGenerationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Debug.Log("Generation system running!");
        // Your generation code
    }
}
```

3. **Check ECS World**
```csharp
// Verify world exists
void Start()
{
    var world = World.DefaultGameObjectInjectionWorld;
    if (world == null)
    {
        Debug.LogError("No ECS world found!");
    }
    else
    {
        Debug.Log($"ECS world active: {world.Name}");
    }
}
```

**Testing:** Run generation and check console for "Generation system running!" message.

---

### **"World generates but looks wrong"**

**Symptoms:**
- Rooms appear in wrong positions
- Missing connections between rooms
- Biome effects not applying

**Causes:**
- Incorrect coordinate calculations
- Missing connection data
- Biome system timing issues

**Solutions:**

1. **Debug Room Positions**
```csharp
// Add to room creation
Entity roomEntity = em.CreateEntity();
em.AddComponentData(roomEntity, new LocalTransform
{
    Position = roomPosition,
    Rotation = quaternion.identity,
    Scale = 1f
});

Debug.Log($"Created room at position: {roomPosition}");
```

2. **Verify Connections**
```csharp
// Check connection components
foreach (var (connection, entity) in SystemAPI.Query<RefRO<RoomConnection>>().WithEntityAccess())
{
    Debug.Log($"Connection: {connection.ValueRO.FromRoom} -> {connection.ValueRO.ToRoom}");
}
```

3. **Biome System Timing**
```csharp
// Ensure biome system runs after room generation
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(RoomGenerationSystem))] // Run after room creation
public partial class BiomeApplicationSystem : SystemBase
{
    // Apply biomes here
}
```

**Testing:** Use [debug visualization](debug-tools.md) to see room positions and connections.

---

### **"Performance drops during generation"**

**Symptoms:**
- Frame rate drops to unplayable levels
- Generation takes too long
- Game becomes unresponsive

**Causes:**
- Too many entities created at once
- Synchronous operations blocking main thread
- Missing performance optimizations

**Solutions:**

1. **Chunk Generation**
```csharp
// Generate in chunks instead of all at once
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ChunkedGenerationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        const int maxPerFrame = 10;
        int processed = 0;

        foreach (var chunk in SystemAPI.Query<RefRW<GenerationChunk>>())
        {
            if (processed >= maxPerFrame) break;

            // Generate one chunk
            GenerateChunk(chunk.ValueRW);
            processed++;
        }
    }
}
```

2. **Async Operations**
```csharp
// Use jobs for heavy computation
[BurstCompile]
public partial struct GenerationJob : IJob
{
    public NativeArray<float3> Positions;
    public int Seed;

    [BurstCompile]
    public void Execute()
    {
        // Heavy generation logic here
        var random = new Unity.Mathematics.Random((uint)Seed);
        for (int i = 0; i < Positions.Length; i++)
        {
            Positions[i] = GenerateRoomPosition(random);
        }
    }
}
```

3. **Entity Pooling**
```csharp
// Reuse entities instead of creating/destroying
public struct EntityPool
{
    public NativeList<Entity> AvailableEntities;
    public Entity Prefab;

    public Entity GetEntity(EntityManager em)
    {
        if (AvailableEntities.Length > 0)
        {
            Entity entity = AvailableEntities[AvailableEntities.Length - 1];
            AvailableEntities.RemoveAt(AvailableEntities.Length - 1);
            return entity;
        }
        return em.Instantiate(Prefab);
    }
}
```

**Testing:** Use Unity Profiler to identify performance bottlenecks.

---

## 🎮 **Player & Enemy Issues**

### **"Player can't move"**

**Symptoms:**
- Player character doesn't respond to input
- No movement despite pressing keys
- Animation plays but position doesn't change

**Causes:**
- Missing input handling
- Rigidbody constraints
- NavMesh issues

**Solutions:**

1. **Check Input**
```csharp
void Update()
{
    float horizontal = Input.GetAxis("Horizontal");
    float vertical = Input.GetAxis("Vertical");

    Debug.Log($"Input: {horizontal}, {vertical}");

    if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
    {
        // Input is working
        MovePlayer(horizontal, vertical);
    }
}
```

2. **Verify Movement Code**
```csharp
private void MovePlayer(float h, float v)
{
    Vector3 movement = new Vector3(h, 0, v) * speed * Time.deltaTime;
    transform.Translate(movement, Space.World);

    Debug.Log($"Moving by: {movement}");
}
```

3. **Check Collisions**
```csharp
// Temporarily disable colliders to test
void Start()
{
    var colliders = GetComponentsInChildren<Collider>();
    foreach (var col in colliders)
    {
        col.enabled = false; // Test without collisions
    }
}
```

**Testing:** Add debug logs to see if input is received and movement is applied.

---

### **"Enemies don't chase player"**

**Symptoms:**
- Enemies stand still
- No pathfinding behavior
- Player can walk through enemies

**Causes:**
- Missing AI components
- NavMesh not baked
- Player reference not set

**Solutions:**

1. **Verify AI Components**
```csharp
void Start()
{
    var agent = GetComponent<NavMeshAgent>();
    if (agent == null)
    {
        Debug.LogError("No NavMeshAgent found!");
    }
    else
    {
        Debug.Log("NavMeshAgent present");
    }
}
```

2. **Check Player Reference**
```csharp
[SerializeField] private Transform player;

void Start()
{
    if (player == null)
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogError("Player not found!");
        }
    }
}
```

3. **Test Pathfinding**
```csharp
void Update()
{
    if (player != null)
    {
        var agent = GetComponent<NavMeshAgent>();
        bool pathFound = agent.SetDestination(player.position);

        if (!pathFound)
        {
            Debug.LogWarning("No path to player found!");
        }
    }
}
```

**Testing:** Use [debug visualization](debug-tools.md) to see enemy paths.

---

## 🎨 **Art & Visual Issues**

### **"Biome effects not showing"**

**Symptoms:**
- All rooms look the same
- No visual differences between biomes
- Art integration not working

**Causes:**
- Biome data not applied
- Missing art assets
- System execution order

**Solutions:**

1. **Check Biome Assignment**
```csharp
// Debug biome data
foreach (var (biome, entity) in SystemAPI.Query<RefRO<BiomeData>>().WithEntityAccess())
{
    Debug.Log($"Entity {entity}: Biome {biome.ValueRO.Type}");
}
```

2. **Verify Art System**
```csharp
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class DebugBiomeArtSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var art in SystemAPI.Query<RefRO<BiomeArtData>>())
        {
            if (!art.ValueRO.Applied)
            {
                Debug.LogWarning("Biome art not applied!");
            }
        }
    }
}
```

3. **Asset References**
```csharp
// Check if art assets exist
void Start()
{
    var biomeProfile = GetComponent<BiomeArtProfile>();
    if (biomeProfile == null)
    {
        Debug.LogError("No BiomeArtProfile found!");
    }
    else if (biomeProfile.Tiles.Length == 0)
    {
        Debug.LogError("No tiles in biome profile!");
    }
}
```

**Testing:** Use scene view to check if art components are attached correctly.

---

### **"Textures/Sprites not loading"**

**Symptoms:**
- Missing textures on objects
- Pink/magenta materials
- Console errors about missing assets

**Causes:**
- Incorrect asset paths
- Missing imports
- Platform-specific issues

**Solutions:**

1. **Check Asset Paths**
```csharp
// Verify asset loading
void Start()
{
    Sprite sprite = Resources.Load<Sprite>("Sprites/PlayerSprite");
    if (sprite == null)
    {
        Debug.LogError("Failed to load sprite!");
        // Try different paths
        sprite = Resources.Load<Sprite>("Assets/Sprites/PlayerSprite");
    }
}
```

2. **Platform-Specific Assets**
```csharp
void Start()
{
    string path = "Sprites/";
    #if UNITY_IOS
        path += "iOS/PlayerSprite";
    #elif UNITY_ANDROID
        path += "Android/PlayerSprite";
    #else
        path += "Default/PlayerSprite";
    #endif

    var sprite = Resources.Load<Sprite>(path);
}
```

3. **Asset Bundle Issues**
```csharp
// Check bundle loading
void Start()
{
    var bundle = AssetBundle.LoadFromFile("Assets/StreamingAssets/artbundle");
    if (bundle == null)
    {
        Debug.LogError("Failed to load asset bundle!");
    }
}
```

**Testing:** Check Resources folder and ensure assets are set to readable.

---

## 🔧 **Build & Platform Issues**

### **"Game works in editor but not in build"**

**Symptoms:**
- Crashes on startup
- Missing features in build
- Different behavior between editor and build

**Causes:**
- Platform-specific code issues
- Missing scripting defines
- Build settings problems

**Solutions:**

1. **Check Build Settings**
```csharp
// Add platform checks
void Start()
{
    #if UNITY_EDITOR
        Debug.Log("Running in editor");
    #elif UNITY_STANDALONE
        Debug.Log("Running on standalone");
    #elif UNITY_WEBGL
        Debug.Log("Running on WebGL");
    #endif
}
```

2. **Scripting Defines**
```csharp
// Check for required defines
void Start()
{
    #if METVANDAMN_DEBUG
        Debug.Log("Debug mode enabled");
    #else
        Debug.Log("Release mode");
    #endif
}
```

3. **Exception Handling**
```csharp
try
{
    // Your code
    InitializeMetVanDAMN();
}
catch (Exception e)
{
    Debug.LogError($"Initialization failed: {e.Message}");
    // Fallback behavior
}
```

**Testing:** Make test builds frequently and compare with editor behavior.

---

### **"Mobile performance issues"**

**Symptoms:**
- Low frame rate on mobile
- High battery usage
- Overheating

**Causes:**
- Too many draw calls
- Unoptimized shaders
- Excessive entity count

**Solutions:**

1. **Reduce Draw Calls**
```csharp
// Use static batching
void Start()
{
    var renderers = GetComponentsInChildren<MeshRenderer>();
    foreach (var renderer in renderers)
    {
        renderer.staticShadowCaster = true;
    }
}
```

2. **Simplify Shaders**
```csharp
// Use mobile-friendly shaders
void Start()
{
    var renderers = GetComponentsInChildren<Renderer>();
    foreach (var renderer in renderers)
    {
        // Switch to mobile shader
        renderer.material.shader = Shader.Find("Mobile/Diffuse");
    }
}
```

3. **LOD System**
```csharp
// Implement level of detail
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class MobileLODSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float maxDistance = 50f; // Reduce for mobile

        foreach (var (transform, lod) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<LODLevel>>())
        {
            float distance = math.distance(transform.ValueRO.Position, Camera.main.transform.position);
            lod.ValueRW.CurrentLevel = distance > maxDistance ? 2 : 0; // Simple LOD
        }
    }
}
```

**Testing:** Use Unity's mobile profiling tools and test on actual devices.

---

## 🧪 **Testing & Validation Issues**

### **"Validation tools not working"**

**Symptoms:**
- No validation output
- Errors not detected
- Tools don't run

**Causes:**
- Missing validation components
- Incorrect setup
- Timing issues

**Solutions:**

1. **Check Validation Setup**
```csharp
// Verify validation runner
void Start()
{
    var runner = GetComponent<ValidationRunner>();
    if (runner == null)
    {
        Debug.LogError("ValidationRunner component missing!");
    }
}
```

2. **Manual Validation**
```csharp
// Run validation manually
[ContextMenu("Manual Validate")]
void ManualValidate()
{
    var em = World.DefaultGameObjectInjectionWorld.EntityManager;
    var result = WorldValidation.ValidateWorld(em);

    Debug.Log($"Validation: {result.Errors.Count} errors, {result.Warnings.Count} warnings");
}
```

3. **Timing Issues**
```csharp
// Ensure validation runs after generation
IEnumerator Start()
{
    // Wait for world generation
    yield return new WaitForSeconds(2f);

    // Now run validation
    RunValidation();
}
```

**Testing:** Check console output and ensure validation systems are active.

---

## 🚀 **Getting Help**

### **When to Ask for Help**

**Try these first:**
1. Check this troubleshooting guide
2. Use [debug tools](debug-tools.md) to investigate
3. Run [validation tools](validation.md)
4. Search existing [GitHub Issues](https://github.com/jmeyer1980/TWG-MetVanDAMN/issues)

**Then ask for help:**
- Include Unity version and MetVanDAMN version
- Describe exact symptoms and steps to reproduce
- Share console output and screenshots
- Include minimal reproduction project if possible

### **Community Resources**
- **[GitHub Issues](https://github.com/jmeyer1980/TWG-MetVanDAMN/issues)** - Bug reports and fixes
- **[GitHub Discussions](https://github.com/jmeyer1980/TWG-MetVanDAMN/discussions)** - Questions and help
- **[Unity Forums](https://forum.unity.com/forums/ecs.222/)** - ECS-specific help
- **Discord/Slack communities** - Real-time help

---

## 🎯 **Prevention Tips**

### **Development Best Practices**
- **Test frequently** - Catch issues early
- **Use version control** - Track what changed
- **Document changes** - Know what you modified
- **Backup working versions** - Easy rollback

### **Code Quality**
- **Add error handling** - Graceful failure
- **Use debug logs** - Understand execution flow
- **Validate inputs** - Check data before using
- **Test edge cases** - Unusual scenarios

### **Performance Monitoring**
- **Profile regularly** - Know your performance baseline
- **Monitor entity counts** - Watch for memory leaks
- **Test on target hardware** - Don't assume performance
- **Use validation tools** - Automated problem detection

---

*"Most bugs are just misunderstandings between you and your code. Take the time to listen to what it's trying to tell you."*

**🍑 Happy Troubleshooting! 🍑**
