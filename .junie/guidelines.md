# MetVanDAMN! Project Guidelines for AI Agents

**Version 2.0 - Professional Production Release - 2025-10-05**

---

## 🎯 Project Overview

**MetVanDAMN!** is a **production-ready, professional procedural 2D Metroidvania framework** built with Unity ECS/DOTS architecture. The project provides a complete, modular foundation for 100% procedural world generation that developers can use immediately and extend without breaking existing functionality.

### Core Business Model

- **Production-Ready Framework** - Every feature is complete and functional out of the box
- **Professional Target Audience** - For experienced Unity developers building commercial games
- **Extensible Architecture** - Complete modules that can be extended without modification
- **Zero Placeholders** - No incomplete code, no TODO comments, no null possibilities

---

## 🏗️ Architecture & Core Principles

### ECS-First Design

- **All gameplay logic uses Unity ECS/DOTS systems** - No MonoBehaviour logic for core features
- **Authoring components are configuration points** - Not content containers
- **Procedural generation at runtime** - Minimal manual scene setup
- **Burst-compiled jobs for performance** - Critical spatial analysis and world generation

### World Generation Pipeline

1. **WorldAuthoring** → Configuration entry point
2. **ECS Systems** → Generate districts, sectors, rooms, biomes
3. **BiomeArtProfile** → Visual art and prop placement
4. **Validation** → Manual testing ensures coherence and playability

---

## 🧪 Philosophy of Completeness (Story Test Heritage)

### The Completeness Principle

**Every feature in MetVanDAMN is complete and production-ready.** This philosophy is inherited from the deprecated "Story Test" validation approach, which emphasized narrative-driven, end-to-end verification.

#### Core Beliefs

1. **Default Values Everywhere** - Null is not an option. Every variable, every parameter, every system has a safe, sensible default value.
2. **No Incomplete Code** - Placeholder implementations, TODO comments, and "coming soon" features undermine developer confidence.
3. **Professional Confidence** - Developers using MetVanDAMN must trust that every system works as documented, immediately.
4. **Complete ≠ Closed** - A complete module can still be extended. Completeness means "ready to use now," not "cannot be improved."

#### Why This Matters

- **Loss of Confidence**: Incomplete code signals unreliability. Professional developers need certainty.
- **Null Propagation**: One nullable field cascades into defensive null-checks throughout the codebase.
- **Production Readiness**: Commercial games cannot ship with placeholders and TODOs.

### Current Validation Status

- **Story Test Framework**: Deprecated and removed due to legacy code issues
- **TLDA/TLDL Tooling**: Removed from core framework (historical artifacts remain in `docs/TLDL-Archive/`)
- **GitHub CI/CD**: CID workflows still run but do not enforce completeness
- **Developer Responsibility**: Each contributor must ensure completeness manually until validation is restored

### Testing Requirements

- **All systems must be fully functional** - No stub implementations allowed
- **Unit tests for critical algorithms** - Cover edge cases and error conditions
- **Integration tests for ECS authoring** - Verify end-to-end data flow
- **Manual validation before commit** - Test in Unity Editor with real scenarios

---

## 📂 Project Structure

```
Assets/
├── MetVanDAMN/
│   ├── Authoring/          # Scene setup, ECS authoring components
│   ├── QuadRig/            # Character system (quad-rig humanoid)
│   └── Data/               # ScriptableObject definitions
├── TWG/
│   └── Strengthening/      # Deprecated Story Test framework (removed)
├── Scenes/
│   ├── MetVanDAMN_Baseline.unity              # Minimal setup scene
│   └── MetVanDAMN_Complete2DPlatformer.unity  # Full demo scene
├── Tools/Editor/           # Custom Unity editor tools
└── Plugins/                # (Deprecated TLDA tooling removed)

Packages/
├── com.tinywalnutgames.metvd.core/     # Core ECS systems
├── com.tinywalnutgames.metvd.biome/    # Biome generation
├── com.tinywalnutgames.metvd.graph/    # Navigation graphs
├── com.tinywalnutgames.metvd.quadrig/  # Character systems
└── com.tinywalnutgames.metvd.utility/  # Shared utilities

docs/
├── TLDL-Archive/           # Historical documentation (read-only)
├── oracle_visions/         # Deprecated AI artifacts (read-only)
└── README.md               # Current production documentation
```

---

## 💎 Sacred Code Classification Protocol

All code in this project follows a **classification taxonomy** to protect core algorithms while encouraging safe extension.

### ??? PROTECTED CORE (Sacred & Untouchable)

**Comment Pattern**: `// ??? CORE ALGORITHM - Modification requires architectural review`

- Core ECS systems and world generation foundations
- Mathematical algorithms (prime detection, Fibonacci, golden ratio)
- Burst-compiled performance-critical jobs
- Blittable ECS component definitions
- Coordinate transform logic

**DO NOT modify without architectural review.**

### ?? INTENDED EXPANSION ZONES (Safe to Customize)

**Comment Pattern**: `// ?Intended use!? [Purpose] - Expand as needed for your project`

- Constraint validation logic (game-specific rules)
- Prop placement strategies (art-specific algorithms)
- BiomeArtProfile configurations
- Navigation quick fixes (project-specific pathfinding)
- Configuration-driven behaviors

**DO modify freely for your project needs.**

### ?? ENHANCEMENT CANDIDATES (Deprecated - Remove This Classification)

**Former Comment Pattern**: `// ?? ENHANCEMENT READY - Simplified for compilation success, coordinate-awareness awaited`

**This classification is deprecated.** Any code marked as "ENHANCEMENT READY" should be:
- Completed to production quality and reclassified as PROTECTED CORE or EXPANSION ZONE
- Or removed if it cannot be brought to production standards

**No incomplete code is allowed in production releases.**

### ?? COORDINATE-AWARENESS ZONES (Spatial Intelligence)

**Comment Pattern**: `// ?? COORDINATE-AWARE - Uses nodeId.Coordinates for spatial intelligence`

- Spatial analysis systems (distance calculations, pattern recognition)
- Biome coherence logic (neighbor analysis, connectivity)
- Material generation (coordinate-influenced visuals)
- World position calculations (mathematical patterns)

**DO build upon existing spatial intelligence patterns.**

---

## 🚫 Code Quality Standards (Non-Negotiable)

### Forbidden Practices

- ❌ **No unsealed symbols** - All classes/methods must be `sealed` or designed for inheritance
- ❌ **No unused code** - Every line exists with purpose or is archived in version control
- ❌ **No commented-out code** - Version control is the archive
- ❌ **No TODO comments** - All code must be complete before commit
- ❌ **No placeholder implementations** - Every method must provide real, functional logic
- ❌ **No incomplete features** - Features are either complete or not included
- ❌ **No nullable without defaults** - Every field must have a safe fallback value
- ❌ **No magic numbers** - Use named constants or enums
- ❌ **No global state** - Avoid static variables and singletons
- ❌ **No side effects in getters** - Properties reveal state, never alter it
- ❌ **No silent failures** - All errors must be surfaced or logged
- ❌ **No circular dependencies** - Dependencies flow outward only

### Production-Ready Completeness Requirements

#### Complete, Not Closed

Every system must be:
- **Functional Immediately** - Works without additional configuration or "coming soon" features
- **Safely Extensible** - Provides extension points (interfaces, abstract classes, ScriptableObjects) for customization
- **Well-Documented** - Clear purpose, usage examples, and extension patterns explained
- **Default-Safe** - All parameters have sensible defaults; null is handled gracefully or prevented entirely

#### Example: The Right Way

```csharp
/// <summary>
/// ?? INTENDED EXPANSION ZONE - Customize placement strategy via ScriptableObject
/// Handles prop placement within biome regions with deterministic randomization.
/// </summary>
public sealed class PropPlacementSystem : SystemBase
{
    // Default strategy provided, can be overridden via configuration
    private PropPlacementStrategy _strategy = new RandomPlacementStrategy();

    protected override void OnUpdate()
    {
        // Complete implementation with safe defaults
        Entities.ForEach((ref BiomeData biome, ref PropPlacementRequest request) =>
        {
            // Always returns valid placement, even if strategy is null
            var placement = _strategy?.Calculate(biome) ?? biome.CenterPosition;
            PlaceProp(placement, request.PropType);
        }).Run();
    }

    /// <summary>
    /// Places a prop at the specified position with fallback to safe default.
    /// </summary>
    private void PlaceProp(float3 position, PropType type)
    {
        // Guaranteed to succeed - no null checks needed by caller
        var entity = EntityManager.CreateEntity(typeof(PropComponent));
        EntityManager.SetComponentData(entity, new PropComponent
        {
            Position = position,
            Type = type,
            IsPlaced = true
        });
    }
}
```

#### Example: The Wrong Way (DO NOT DO THIS)

```csharp
// ❌ INCOMPLETE: TODO comment, null possibility, placeholder
public sealed class PropPlacementSystem : SystemBase
{
    private PropPlacementStrategy _strategy; // TODO: Implement strategy selection

    protected override void OnUpdate()
    {
        // Placeholder - real implementation coming soon
        if (_strategy == null) return; // Silent failure

        // TODO: Add actual placement logic
        throw new NotImplementedException("Prop placement not yet implemented");
    }
}
```

---

## 🔧 Development Workflow

### Scene Setup (Quick Start)

**Goal**: "Hit Play → See Generated World" in 30 seconds

1. Open `Assets/Scenes/MetVanDAMN_Baseline.unity`
2. Add `WorldAuthoring` component to a GameObject
3. Configure parameters:
   - `worldSeed`: 42 (reproducible)
   - `worldSize`: (50, 50)
   - `targetSectorCount`: 5
4. Hit Play → See console logs and generated world

### Common Setup Issues

| Issue | Solution |
|-------|----------|
| No world entities created | Verify WorldAuthoring is active, check console logs |
| Districts not visible | Open Window > Entities > Entity Debugger, look for "HubDistrict" |
| Systems not processing | ECS systems may take 1-2 frames to begin |
| Debug visualization missing | Enable Scene view Gizmos, look for green wireframe bounds |

### Before Submitting Code

1. **Ensure Complete Implementation** - No TODOs, no placeholders, no incomplete features
2. **Verify Default Values** - All fields have safe defaults, null is prevented or handled
3. **Test in Unity Editor** - Manual validation with real scenarios (multiple seeds, edge cases)
4. **Verify ECS Integration** - Use Entity Debugger to confirm component structure
5. **Check Sacred Code Classifications** - Ensure proper comment patterns for core vs. extension code
6. **Run Available Tests** - Execute any existing unit/integration tests
7. **Document Extension Points** - If adding extensibility, provide XML documentation and usage examples

---

## 📚 Documentation Standards

### Production Documentation Requirements

All features must include:

1. **XML Documentation Comments** - For all public APIs, types, and members
2. **README Files** - In package directories explaining system purpose and usage
3. **Code Examples** - Demonstrating common usage patterns and extension scenarios
4. **Extension Guides** - How to customize or extend the system safely

### Example XML Documentation

```csharp
/// <summary>
/// ?? INTENDED EXPANSION ZONE - Customize biome transitions via ScriptableObject profiles.
/// Manages smooth visual transitions between biome regions during world generation.
/// </summary>
/// <remarks>
/// This system processes BiomeTransitionRequest components and applies gradient blending
/// between adjacent biomes. Default transition uses linear interpolation but can be
/// overridden via BiomeTransitionProfile ScriptableObjects.
///
/// Extension Example:
/// <code>
/// var customProfile = ScriptableObject.CreateInstance&lt;BiomeTransitionProfile&gt;();
/// customProfile.TransitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
/// biomeSystem.SetTransitionProfile(customProfile);
/// </code>
/// </remarks>
public sealed class BiomeTransitionSystem : SystemBase
{
    // Implementation...
}
```

### Legacy Documentation

The project previously used **TLDL (Today's Living Dev Log)** and **TLDA (Living Dev Agent)** tooling, which have been deprecated and removed. Historical documentation remains in:

- `docs/TLDL-Archive/` - Archived development logs (read-only, for historical context)
- `docs/oracle_visions/` - Legacy AI collaboration artifacts (deprecated)

**Do not create new TLDL entries or TLDA artifacts.** Use standard XML documentation, README files, and professional commit messages instead.

---

## 🎨 Communication Style & Tone

### Professional Communication Style

As a production framework for commercial use:

- **Clear, Concise Documentation** - Focus on functionality, usage, and extension patterns
- **Professional Tone** - Maintain technical clarity without excessive humor or narrative
- **Solution-Oriented** - Emphasize what works and how to use it effectively
- **Respect Developer Time** - Provide quick reference guides and practical examples

### Historical Context: The Manifesto

The project's early development embraced **"Save the Butts" culture** with humor, lore, and RPG terminology. This philosophy lives on in the commitment to:

- **Developer Confidence** - Complete, reliable systems prevent frustration
- **Defensive Design** - Safe defaults and null-prevention protect against errors
- **Extensibility** - Respect that every developer has unique needs
- **Quality Over Speed** - Ship complete features, not placeholders

See `.github/MANIFESTO.md` for historical context (archived philosophy, not current guidance).

---

## 🔗 Integration Points & Data Flow

### Core Pipeline

```
WorldAuthoring (Config)
    ↓
ECS World Generation Systems
    ↓
District/Sector/Room Entities
    ↓
BiomeArtProfile (Visual Layer)
    ↓
Tilemap + Prop Placement
    ↓
Manual Validation (Story Test framework deprecated)
```

### Key Systems

- **WorldAuthoring → ECS → BiomeArtProfile**: Config → Generation → Visuals
- **Navigation Graph System**: Pathfinding and reachability
- **Biome Transition System**: Smooth environmental changes
- **Gate Condition System**: Progression gating and unlocks

---

## 🛠️ Building & Testing

### Build Commands

```bash
# Run all tests (Edit Mode + Play Mode)
.\run_tests.ps1

# Manual testing in Unity Editor (primary validation method)
# 1. Open MetVanDAMN_Baseline.unity
# 2. Hit Play with various world seeds
# 3. Verify generation completes without errors
```

### CI/CD Status

- **Current**: GitHub workflows run CID (Chronicle/Integration/Deployment checks)
- **Limitation**: No automated completeness validation (Story Test framework deprecated)
- **Developer Responsibility**: Manual verification required before commit

### Testing Requirements

- **Critical algorithms must have unit tests** - Cover edge cases and error conditions
- **Authoring components must have integration tests** - Verify ECS baking pipeline
- **Manual validation required** - Test in Unity Editor with multiple scenarios
- **No test gaps** - If you cannot test it, do not commit it

---

## 🧙 AI Agent Personality & Workflow Intelligence

### Contextual Behavior

- **Recognize `.agent-profile.yaml` flags** - Adjust tone, dry-run mode, pipeline preferences
- **Prioritize conversational commits** - Respond to comment pings proactively
- **Avoid ECS-only assumptions** - Support render pipeline neutrality
- **Professional Focus** - Emphasize production readiness and completeness

### Documentation Handling

- **Focus on API Documentation** - XML comments for all public types and members
- **Avoid Legacy Patterns** - Do not create TLDL entries or TLDA artifacts
- **Preserve Historical Context** - Recognize deprecated terms ("TLDA", "Living Dev Agent", "Story Tests") as historical, not active guidance

### Workflow Modes

| Mode | Behavior |
|------|----------|
| Exploration | Suggest investigative approaches, ask clarifying questions |
| Implementation | Focus on complete, tested solutions with safe defaults |
| Documentation | Emphasize clear XML comments and usage examples |
| Code Review | Verify completeness, null-safety, and extension point documentation |

---

## 🚀 Key Files & Quick Reference

### Documentation

- `.github/copilot-instructions.md` - Canonical AI instructions (may reference deprecated patterns)
- `.github/Sacred Code Classification Protocol.md` - Code taxonomy for core vs. extensible code
- `.github/MANIFESTO.md` - Historical project philosophy (archived)

### Critical Systems

- `Assets/MetVanDAMN/Authoring/MetVanDAMNMapGenerator.cs` - World generation entry point
- `Packages/com.tinywalnutgames.metvd.core/` - Core ECS systems
- `Packages/com.tinywalnutgames.metvd.biome/` - Biome generation and art integration

### Validation

- **Manual Testing Required** - Story Test framework deprecated, no automated completeness checks
- Use Unity Test Runner for existing unit/integration tests
- Verify functionality in Unity Editor before committing

---

## 🎯 Quick Decision Guide for AI Agents

### When approaching a task:

1. **Is this modifying PROTECTED CORE code?**
   - ✅ Yes → Request architectural review, provide analysis first
   - ❌ No → Proceed with caution

2. **Is this implementation complete?**
   - ✅ Yes → Proceed with commit
   - ❌ No → Do not commit until complete (no TODOs, no placeholders)

3. **Is this introducing new ECS systems?**
   - ✅ Yes → Ensure complete implementation with default values, document extension points
   - ❌ No → Follow standard patterns

4. **Does this need XML documentation?**
   - ✅ Public API, complex system, extension point → Add comprehensive XML comments
   - ❌ Private implementation detail → Standard inline comments sufficient

5. **Does this change affect world generation?**
   - ✅ Yes → Test multiple seeds in Unity Editor, verify deterministic behavior
   - ❌ No → Standard validation applies

6. **Are all default values safe?**
   - ✅ Yes → Proceed
   - ❌ No → Add safe defaults or null-prevention logic before committing

7. **Is this creating incomplete code?**
   - ✅ Yes → STOP. Complete the feature or do not include it
   - ❌ No → Proceed

---

## 🏆 Success Metrics

### Developer Confidence

- ✅ Immediate trust in every system - "it just works"
- ✅ No fear of null exceptions or missing features
- ✅ Clear understanding of what is core vs. what is extensible
- ✅ Preserved core system stability

### Code Quality

- ✅ Zero incomplete features in production releases
- ✅ Clear architectural boundaries via Sacred Code Classification
- ✅ Consistent default-safety patterns throughout codebase
- ✅ Comprehensive XML documentation for all public APIs

### Project Health

- ✅ Professional developers can build commercial games immediately
- ✅ Successful extension without breaking core functionality
- ✅ Maintained performance in Burst-compiled systems
- ✅ Enhanced visual feedback through coordinate-aware art
- ✅ Improved world generation stability and predictability

---

## 📄 License & Attribution

**Created by**: Tiny Walnut Games Development Team
**License**: Commercial Production Framework (see LICENSE file for details)
**Philosophy**: Professional completeness, safe extensibility, developer confidence

---

## 🌟 Professional Commitment to Excellence

By working on MetVanDAMN, we commit to:

1. **Deliver Complete Features** - No placeholders, no TODOs, no "coming soon"
2. **Provide Safe Defaults** - Null is prevented or handled gracefully everywhere
3. **Preserve Algorithmic Beauty** - Protect core mathematical systems from degradation
4. **Enable Fearless Extension** - Complete modules with clear, documented extension points
5. **Document Clearly** - Professional XML documentation and usage examples
6. **Test Thoroughly** - Manual validation before every commit
7. **Respect Developer Time** - Reliable, production-ready systems that work immediately
8. **Maintain Extensibility** - Complete does not mean closed; extension is always possible

**This is a professional framework for commercial game development. Every line of code must honor that responsibility.**

---

## 📊 Completeness Checklist

Before committing any code, verify:

- [ ] **No TODO comments** - All implementation is complete
- [ ] **No placeholder code** - No `throw new NotImplementedException()`
- [ ] **No commented-out code** - Version control is the archive
- [ ] **Safe defaults everywhere** - All fields have sensible fallback values
- [ ] **Null prevention** - Nullable types are justified and handled defensively
- [ ] **Extension points documented** - If extensible, show how via XML comments and examples
- [ ] **Tested in Unity Editor** - Manual validation with real scenarios (multiple seeds, edge cases)
- [ ] **XML documentation** - Public APIs have comprehensive comments with usage examples
- [ ] **Sacred Code Classification** - Core algorithms properly marked with ??? or ?? patterns
- [ ] **ECS best practices** - Follows DOTS performance guidelines (Burst, Jobs, Collections)
- [ ] **Deterministic behavior** - World generation is reproducible with same seed

---

## 🎓 Understanding the Completeness Philosophy

### What "Complete" Means

- ✅ Every method has real, functional implementation
- ✅ Every field has a safe default value
- ✅ Every system works immediately without additional setup
- ✅ Every extension point is documented with examples

### What "Complete" Does NOT Mean

- ❌ Cannot be extended or customized
- ❌ Cannot be improved or optimized
- ❌ Must include every possible feature
- ❌ Cannot evolve over time

### The Modular Completeness Model

Think of MetVanDAMN as a complete LEGO set:
- Every piece (module/system) is complete and functional on its own
- Pieces connect together seamlessly (integration points)
- You can add new pieces without breaking existing ones (extensibility)
- You don't need to wait for "more pieces coming soon" to build something (production-ready)

### Drawing the Line

**Include in Release:**
- Features that are fully implemented and tested
- Systems with safe defaults and null-prevention
- Extension points that are documented and functional

**Exclude from Release:**
- Features that are partially implemented
- Systems with TODO comments or placeholders
- "Coming soon" functionality that doesn't work yet

**If a feature isn't complete, defer it to the next release. Do not ship incomplete code.**

---

*If you do not see a generated world when hitting Play, check for missing WorldAuthoring components or invalid BiomeArtProfile configuration. All setup must be procedural and complete out-of-the-box.*
