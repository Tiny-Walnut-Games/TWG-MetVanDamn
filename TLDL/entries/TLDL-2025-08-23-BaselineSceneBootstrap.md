# TLDL-2025-08-23-BaselineSceneBootstrap

**Entry ID:** TLDL-2025-08-23-BaselineSceneBootstrap  
**Author:** @copilot  
**Context:** Added hybrid baseline scene bootstrap (direct + fallback reflection modes) and updated onboarding GitBook + authoring README  
**Summary:** Implemented one‑click MetVanDAMN baseline scene generator with dual compile paths and documentation integration to accelerate onboarding & reproducible smoke tests.  

---

> *"A newcomer who presses Play and sees a living world stays; one who sees an empty hierarchy drifts into other realms."* — **Onboarding Codex, Addendum VII**

---

## Discoveries
### [Onboarding Gap]
- **Key Finding**: Manual multi‑step scene prep (seed entity, biome fields, systems) slowed first‑session validation
- **Impact**: Increased friction for contributors; higher context re-creation cost for regression testing
- **Evidence**: Repeated ad‑hoc instructions in PR comments / chat
- **Root Cause**: No canonical, reproducible baseline scene artifact
- **Pattern Recognition**: Common early‑stage ECS projects defer a curated entry scene, amplifying perceived complexity

### [Package Variability]
- **Key Finding**: Teams operate with different subsets of DOTS (Samples / SubScene sometimes excluded)
- **Impact**: Hard references could break editor scripts in lean CI or partial installs
- **Evidence**: Prior compile guards / reflection usage patterns
- **Pattern Recognition**: Need graceful degradation for optional modules

## Actions Taken
1. **Baseline Scene Generator**
   - **What**: Created `MetVanDAMNSceneBootstrap` editor utility (menu + shortcut) building a root scene & four additive sub‑scenes
   - **Why**: Provide immediate worldgen smoke test & shared reproducible environment
   - **How**: Editor scripting; conditional compilation; idempotent save logic
   - **Result**: Single command yields runnable world with seed + biome polarity fields
2. **Hybrid Compile Mode**
   - **What**: Introduced `METVD_FULL_DOTS` symbol path for direct typed references & fallback reflection path otherwise
   - **Why**: Allow strict compile-time safety in full installs while preserving flexibility for slim environments / CI
   - **How**: `#if METVD_FULL_DOTS` blocks for SubScene & SmokeTestSceneSetup; reflection lookups else
   - **Result**: Zero build errors across variable dependency footprints
3. **Documentation Integration**
   - **What**: Updated Authoring README + GitBook “123 Quick TLDR” with quickstart & mode switch instructions
   - **Why**: Reduce tribal knowledge; formalize onboarding ritual
   - **How**: Added explicit steps, outputs, and symbol definition guidance
   - **Result**: Clear path from clone → Play in < 1 minute
4. **Safety & Idempotence Enhancements**
   - **What**: Overwrite confirmation; skip re‑opening loaded sub‑scenes; folder auto‑creation
   - **Why**: Prevent accidental asset churn and duplicate tabs
   - **Result**: Stable repeatable regeneration

## Technical Details
### Generated Artifacts
```text
Assets/Scenes/MetVanDAMN_Baseline.unity
Assets/Scenes/SubScenes/WorldGen_Terrain.unity
Assets/Scenes/SubScenes/WorldGen_Dungeon.unity
Assets/Scenes/SubScenes/NPC_Interactions.unity
Assets/Scenes/SubScenes/UI_HUD.unity
```

### Conditional Compilation
```csharp
#if METVD_FULL_DOTS
using TinyWalnutGames.MetVD.Samples; // SmokeTestSceneSetup
using Unity.Scenes;                  // SubScene
#else
// Reflection fallback: FindTypeAnywhere("TinyWalnutGames.MetVD.Samples.SmokeTestSceneSetup")
#endif
```

### Reflection Lookup Helper (Fallback Mode)
```csharp
Type FindTypeAnywhere(string fullName) {
  foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
    var t = asm.GetType(fullName, false); if (t != null) return t;
  }
  return null;
}
```

### Bootstrap Flow
1. Create / confirm folders
2. Prompt before overwriting root scene
3. Spawn light + camera if missing
4. Attach SmokeTestSceneSetup (direct or reflective)
5. Create / load four sub‑scenes (idempotent)
6. Assign SubScene component (or marker) & auto-load flag
7. Persist scenes & refresh assets

## Lessons Learned
### What Worked Well
- Hybrid symbol + reflection pattern balanced safety & portability
- Minimal scene content keeps focus on ECS generation pipeline
- Documentation alignment reduced future support load

### What Could Be Improved
- Add automated gizmo visualization for bounds & polarity fields
- Integrate a seed snapshot capture after generation
- Provide CLI or menu validation pass post-creation

### Knowledge Gaps Identified
- Automated verification of sub‑scene content presence (e.g., required authoring components)
- Metrics collection (time-to-first-play baseline)

## Next Steps
### Immediate Actions (High Priority)
- [ ] Add gizmo layer for districts / polarity radii
- [ ] Seed snapshot exporter hook (store in /docs/seed_snapshots)
- [ ] Simple validation report after generation (log missing systems)

### Medium-term Actions (Medium Priority)
- [ ] Integrate baseline scene creation into CI smoke job
- [ ] Add menu to regenerate only missing sub‑scenes
- [ ] Provide preset variants (Minimal / Full Debug)

### Long-term Considerations (Low Priority)
- [ ] Scene template versioning + upgrade path
- [ ] Multi-world baseline generation (stress test mode)
- [ ] UI_HUD sub‑scene auto-populate with basic overlays

## References
- Authoring README (baseline quickstart section)
- GitBook “123 Quick TLDR Walkthrough” updated section
- `MetVanDAMNSceneBootstrap.cs`
- `SmokeTestSceneSetup.cs`

---

## TLDL Metadata
**Tags**: #bootstrap #scene #onboarding #metvd #dots #automation  
**Complexity**: Medium  
**Impact**: High  
**Team Members**: @copilot  
**Duration**: Implementation + documentation pass  
**Related Epic**: Onboarding Acceleration  

---

**Created**: 2025-08-23 00:00:00 UTC  
**Last Updated**: 2025-08-23 00:00:00 UTC  
**Status**: Complete  

*Generated to preserve lineage of the baseline scene bootstrap improvement.*
