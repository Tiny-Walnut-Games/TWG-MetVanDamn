# MetVanDAMN Quick Start Guide - Procedural Generation

**For Developers**: Get up and running with MetVanDAMN's procedural generation systems  
**Difficulty**: Intermediate (Unity + ECS knowledge helpful)  
**Time**: 30 minutes to understand, lifetime to master  

---

> 🧙‍♂️ *"Every great journey begins with understanding the map. Here's yours."*

---

## 🚀 5-Minute Overview

### **What Is MetVanDAMN?**
A **coordinate-intelligent procedural generation system** that creates Metroidvania worlds where:
- **Every room knows where it is** and adapts accordingly
- **Distance from origin = increased complexity**
- **Biomes influence terrain characteristics**  
- **Performance is king** (Burst-compiled ECS throughout)

### **Core Innovation**
Traditional procedural generation: "Generate random room"  
MetVanDAMN: "Generate room that makes sense for coordinates (5, 12) in VolcanicCore biome with Wind polarity, ensuring it's appropriately challenging for a room this far from origin"

---

## 🏗️ Architecture At-A-Glance

```
📂 Generation Pipeline
├── 🎯 RoomGenerationPipelineSystem (The Orchestrator)
│   ├── Step 1: Biome Selection
│   ├── Step 2: Layout Decision  
│   ├── Step 3: Generator Choice
│   ├── Step 4: Content Pass
│   ├── Step 5: Biome Overrides
│   └── Step 6: Navigation Gen
│
└── 🛠️ TerrainAndSkyGenerators (The Workers)
    ├── StackedSegmentGenerator      // Vertical towers/shafts
    ├── LinearBranchingCorridorGenerator // Horizontal flow
    ├── BiomeWeightedHeightmapGenerator  // Natural terrain
    └── LayeredPlatformCloudGenerator    // Sky areas
```

---

## ⚡ Quick Setup

### **Prerequisites**
- Unity 2023.3+ with DOTS packages
- Basic ECS knowledge helpful but not required
- C# 9.0+ understanding

### **1. Explore the Core File**
**File**: `Packages/com.tinywalnutgames.metvd.graph/Runtime/TerrainAndSkyGenerators.cs`  
**What to look for**:
- **4 main generators** (each ~200-300 lines)
- **Coordinate complexity calculations** (distance-based scaling)
- **Biome integration** throughout
- **Zero IDE warnings** (every symbol meaningful)

### **2. Understand Coordinate Intelligence**
**Key Concept**: Rooms adapt based on their world position

```csharp
var coords = nodeId.Coordinates;        // Where am I in the world?
var distance = math.length(coords);     // How far from origin?
var complexity = distance / 20f;        // Farther = more complex
```

**Real Impact**: A room at (0,0) gets 1-2 platforms. A room at (20,20) gets 4-6 platforms + challenges.

### **3. See Beat-Based Generation**
**File**: Look for `DetermineBeatType` function  
**Innovation**: Corridors use musical "beats" for pacing

```csharp
var corridorLength = totalBeats > 8 ? "Long" : "Medium" : "Short";
// Long corridors: gentle intro → intense middle → easier ending
// Short corridors: front-loaded intensity
```

---

## 🎯 Key Concepts Deep Dive

### **1. Coordinate-Aware Generation**

**Traditional Approach**:
```csharp
// Generate random room regardless of position
var platformCount = random.NextInt(1, 4);
```

**MetVanDAMN Approach**:
```csharp
// Generate room appropriate for world position
var coordinateComplexity = CalculateCoordinateBasedComplexity(nodeId.ValueRO);
var platformCount = (int)(basePlatformCount * coordinateComplexity);
```

**Why This Matters**: Natural difficulty progression without manual tuning!

### **2. Progressive Rhythm System**

**Corridor Generation**: Uses "beats" like music composition
- **Challenge beats**: Obstacles and active sections
- **Rest beats**: Safe areas with health pickups
- **Secret beats**: Hidden areas requiring exploration

**Smart Pacing**: Long corridors pace differently than short ones
```csharp
// Long corridors get gentle intro and ending
if (progressionFactor < 0.2f || progressionFactor > 0.8f)
{
    // Favor rest beats at start/end
}
```

### **3. Sky Complexity Algorithm**

**For aerial/sky biomes**: Y coordinate = altitude = complexity
```csharp
var altitudeComplexity = math.clamp(altitude / 10f + 1f, 0.8f, 2.5f);
// Higher = more challenging sky navigation
```

**Result**: Sky gardens feel appropriately elevated and challenging!

### **4. Biome Integration**

**Every generator respects biome characteristics**:
```csharp
var noiseScale = biome.Type switch
{
    BiomeType.VolcanicCore => 0.2f,     // Jagged terrain
    BiomeType.FrozenWastes => 0.05f,    // Smooth ice
    BiomeType.SolarPlains => 0.1f,      // Rolling hills
    _ => 0.1f
};
```

---

## 🔧 Common Customization Patterns

### **Add New Biome Response**

**1. Find the biome switch statements** (search for `biome.Type switch`)

**2. Add your case**:
```csharp
BiomeType.MyCustomBiome => 0.15f, // Your custom value
```

**3. Do this in ALL relevant functions**:
- `GetBiomeNoiseScale`
- `GetBiomeHeightVariation`  
- `GetBiomeSpecificFeature`

### **Modify Coordinate Complexity**

**Current formula**:
```csharp
var distanceComplexity = math.clamp(distance / 20f, 0.7f, 1.8f);
```

**Make it more/less aggressive**:
```csharp
var distanceComplexity = math.clamp(distance / 10f, 0.5f, 2.5f); // More aggressive
var distanceComplexity = math.clamp(distance / 30f, 0.8f, 1.2f); // More gentle
```

### **Add New Generator Type**

**1. Create the system** (follow existing patterns):
```csharp
[BurstCompile]
public partial struct MyCustomGenerator : ISystem
{
    // Follow StackedSegmentGenerator pattern
}
```

**2. Add enum value**:
```csharp
public enum RoomGeneratorType : byte 
{
    // Existing values...
    MyCustomType = 4,
}
```

**3. Integrate with pipeline** in `ProcessRoomGeneratorChoice`

---

## 🧪 Testing Your Changes

### **Quick Validation**
1. **No compilation errors**: Zero tolerance policy
2. **No IDE warnings**: Every symbol should be meaningful
3. **Run existing tests**: Ensure you didn't break anything
4. **Visual inspection**: Generate some rooms and see if they look right

### **Performance Check**
```csharp
// Time your generation
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
GenerateRoom();
stopwatch.Stop();
Debug.Log($"Generation took: {stopwatch.ElapsedMilliseconds}ms");
```

**Target**: <5ms per room (with Burst compilation)

### **Determinism Validation**
```csharp
// Same seed should = same result
var result1 = GenerateWithSeed(12345);
var result2 = GenerateWithSeed(12345);
Assert.AreEqual(result1, result2); // Should be identical
```

---

## 🎓 Learning Path

### **Level 1: Understanding** (You Are Here)
- Read this guide ✓
- Explore `TerrainAndSkyGenerators.cs`
- Run some tests
- Make a small biome customization

### **Level 2: Modification**
- Modify coordinate complexity formulas
- Add custom biome responses
- Adjust beat generation patterns
- Create custom feature types

### **Level 3: Extension**
- Build new generator system
- Implement custom coordinate algorithms
- Integrate with player progression system
- Add visual editor tools

### **Level 4: Innovation**
- Research new procedural techniques
- Contribute to open source project
- Share learnings with community
- Push the boundaries of what's possible

---

## 📚 Essential Reading

### **Start With These Files**
1. **`TerrainAndSkyGenerators.cs`** - The core generation systems
2. **`RoomGenerationPipelineSystem.cs`** - The orchestrator
3. **Test files** - See expected behaviors and patterns

### **Key Functions to Understand**
- `CalculateCoordinateBasedComplexity` - How distance affects generation
- `DetermineBeatType` - How corridor pacing works
- `CalculateSkyComplexity` - How altitude affects sky generation
- `GetBiome*` functions - How biomes influence parameters

### **Documentation**
- **[Technical Specification](MetVanDAMN-Technical-Specification.md)** - Complete system details
- **[Marathon Week Chronicle](TLDL-2025-01-02-MetVanDAMNMarathonWeek-ProceduralGenerationMasterpiece.md)** - Development story
- **[Project Overview](MetVanDAMN-Project-Overview.md)** - High-level architecture

---

## 🔥 Pro Tips

### **Understanding the Code**
- **Every variable has purpose**: If it's there, it affects generation
- **Comments tell stories**: Look for why decisions were made
- **Patterns repeat**: Learn one generator, understand them all
- **Tests reveal intent**: Look at tests to see expected behaviors

### **Making Changes**
- **Start small**: Tweak numbers before adding features
- **Test immediately**: Don't change multiple things at once
- **Document decisions**: Future you will thank present you
- **Preserve patterns**: Follow existing architectural styles

### **Debugging Generation**
- **Use deterministic seeds**: Same seed = reproducible bugs
- **Add debug logs**: Understand what values are being generated
- **Visual validation**: Generate rooms and look at them
- **Test edge cases**: Very high/low coordinates, extreme biomes

### **Performance Optimization**
- **Profile before optimizing**: Measure what's actually slow
- **Burst compilation**: Keep all jobs Burst-compatible
- **Minimize allocations**: Use ECS patterns, avoid managed memory
- **Batch operations**: Process multiple entities together

---

## 🚀 What's Next?

### **Immediate Actions**
1. **Explore the codebase** - Get familiar with the patterns
2. **Run the tests** - See the system in action
3. **Make a small change** - Tweak a biome parameter
4. **See the results** - Generate some rooms and observe differences

### **Building Understanding**
1. **Read the TLDL chronicles** - Understand the development journey
2. **Study the technical spec** - Deep dive into system architecture
3. **Experiment with parameters** - Learn how changes affect output
4. **Join the community** - Share learnings and get help

### **Contributing Back**
1. **Document your discoveries** - Add to the knowledge base
2. **Share improvements** - Contribute optimizations and features
3. **Help others learn** - Answer questions and provide guidance
4. **Push boundaries** - Research new procedural generation techniques

---

## 🤝 Community & Support

### **Getting Help**
- **GitHub Issues**: Technical questions and bug reports
- **Discussions**: Design questions and feature ideas
- **TLDL Chronicles**: Learn from documented development journey
- **Code Comments**: Inline explanations of complex algorithms

### **Contributing**
- **Documentation**: Help improve guides and explanations
- **Testing**: Add test coverage for edge cases
- **Features**: Implement new generators or biome types
- **Performance**: Optimize algorithms and memory usage

### **Philosophy**
This project embodies the principle that **sophisticated code + comprehensive documentation = sustainable value**. Every improvement, every optimization, every new feature should be documented for future developers.

---

**Ready to start?** 🧙‍♂️

Open `TerrainAndSkyGenerators.cs`, find the `CalculateCoordinateBasedComplexity` function, and see how distance becomes difficulty. That's where the magic begins.

**Remember**: You're not just modifying code - you're extending a coordinate-intelligent world generation system that could revolutionize indie Metroidvania development. Every change you make contributes to that legacy.

*"The best way to understand a complex system is to change something small and see what happens. Start with curiosity, proceed with caution, and document everything."* ✨

---

**Guide Status**: 🍑 **Buttsafe Certified for Developer Success**  
**Maintained By**: The MetVanDAMN community  
**Last Updated**: 2025-01-02
