# TLDL: MetVanDAMN — Procedural Metroidvania Engine

## Purpose

This document serves as the **source of truth** for the MetVanDAMN engine project — a procedural, polarity‑aware Metroidvania world generator — using **ECSDOTS** as the data backbone, **WFC** for topology and biome shaping, and **gated progression logic** as the genre’s DNA.  
It will be maintained as a living dev log with The Living Dev Agent (LDA) as our historian, ensuring every major decision, milestone, and unlock is preserved.

---

## How to Use This Document

### 1. **Anchor Context for AI Agents**
* Contains the overall project goal, world‑generation grammar, and gating rules so resets or new collaborators can quickly orient.
* Documents current implementation status for each engine subsystem (topology, biome logic, progression orchestration).

### 2. **Track Development Progress**
* Completed features, current blockers, and upcoming tasks are listed under `Current Progress`.
* Engine runs, seed tests, and CI validation output are summarized and linked.

### 3. **Facilitate Debugging and Problem‑Solving**
* Notes on WFC rule conflicts, polarity seam mismatches, and unreachable node detection.
* References to specific ECSDOTS systems and biome socket rules in question.

### 4. **Enhance Collaboration and Knowledge Sharing**
* Serves as a shared lore + tech archive for contributors.
* Preserves *genre recognizability* for external reviewers.

---

## Template Structure

### Project Overview

**Goal**:  
```
Build the procedural Metroidvania engine people recognize on sight — “MetVanDAMN” — complete with procedural map generation, biomes, polarity locks, one‑way routes, and intentional backtracking rewards.
```

**Status**:  
```
Core ECSDOTS components stubbed.  
Polarity grammar draft in progress.  
Macro WFC tileset under design.
```

---

### Current Progress

#### Completed Components
```
- ECSDOTS base entity/component layout for NodeId, Biome, Connection, GateCondition.
- Bitmask polarity enum supporting dual‑polarity gates.
- Initial stub systems for DistrictWfcSystem, SectorRefineSystem.
```

#### Current Issues
```
1. **Polarity Socket Rule Conflicts**
   - Location: BiomeFieldSystem
   - Suspected Cause: Socket definitions allow invalid dual‑polarity adjacency.
   - Status: In progress — adjusting adjacency constraints.
```

#### Next Steps
```
1. Finalize polarity grammar v1 with 6–8 poles and valid adjacency rules.
2. Author 12–20 macro district WFC tiles with socket metadata.
3. Integrate reachability + loop density validation into CI scroll system.
```

---

## 🏷 Naming Convention Ruleset

**Domain conventions follow project ECS norms**  
```
PascalCase → Systems, Components
camelCase  → Locals, parameters
Suffix “System” for ECS systems
Suffix “Component” or “Data” for data holders
```

**ECS Examples:**
```
System      → DistrictWfcSystem.cs
Component   → GateConditionComponent.cs
Buffer Elem → ConnectionBufferElement.cs
Utility     → PolarityMathHelper.cs
```

---

### Architecture

#### Systems
| System                   | Purpose                                            | Status       |
|--------------------------|----------------------------------------------------|--------------|
| DistrictWfcSystem        | Generates macro‑level district graph via WFC       | In progress  |
| SectorRefineSystem       | Adds loops, seeds first hard lock                   | Planned      |
| BiomeFieldSystem         | Assigns biome polarity fields                       | Stubbed      |
| GatePlacementSystem      | Places hard/soft gates per pacing rules             | Planned      |
| ProgressionSimulatorSystem| Simulates unlock order/reachability                | Planned      |
| RewardWeaverSystem       | Populates backtrack nodes with loot/lore            | Planned      |

#### Components
| Component       | Purpose                                           | Status       |
|-----------------|---------------------------------------------------|--------------|
| NodeId          | Uniquely identifies graph node at any scale       | Complete     |
| Biome           | Assigns biome type + polarity field               | Complete     |
| Connection      | Defines link, one‑way state, required polarities  | Complete     |
| GateCondition   | Polarity mask + ability/softness tuple            | Complete     |

---

### File Inventory

#### Implementation
```
/MetVanDAMN.Core
    NodeId.cs
    Biome.cs
    Connection.cs
    GateCondition.cs
/MetVanDAMN.Generation
    DistrictWfcSystem.cs
    SectorRefineSystem.cs
    BiomeFieldSystem.cs
```

#### Reference
```
- ECSDOTS base library
- Sample WFC socket definition files from test harness
```

---

### Development History

* **2025‑08‑20 [JMC]**: Drafted Initial TLDL, stubbed core ECS components.
* **2025‑08‑21 [JMC]**: Wrote polarity grammar outline.

#### Milestone Goals
* **Phase 1**: Generate solvable macro world graphs with polarity coherence.
* **Phase 2**: Add sector/room refinement and gating logic.
* **Phase 3**: Integrate biome fields, gate pacing, and backtrack rewards.

#### Milestones Reached
* **2025‑08‑20**: Engine skeleton committed to repo.
* **2025‑08‑21**: Macro WFC design rules in draft.

---

### Key Learnings
```
- Polarity masks simplify dual‑gate logic compared to discrete enums.
- Early validation prevents WFC collapse dead‑ends.
```

---

### Debug Information

#### Current Issues Details
```
Problem: Adjacent biomes generating invalid polarity junctions.
Location: BiomeFieldSystem
Suspected Cause: Socket metadata incomplete for dual polarity support.
Status: In progress
```

#### Debug Focus Areas
```
- Verify socket metadata for each macro tile.
- Test dual polarity adjacency in WFC test harness.
```

---

## Notes for Conversation Reset
```
Current Status: Macro generation core drafted, polarity grammar WIP.
Last Working: Core ECS compile + initial district WFC test run.
Debug Focus: Biome socket rule validation.
Test Reference: WFC seed replay with socket rule logging enabled.
Comparison Method: Reachability + polarity audit diff before/after grammar changes.
```
