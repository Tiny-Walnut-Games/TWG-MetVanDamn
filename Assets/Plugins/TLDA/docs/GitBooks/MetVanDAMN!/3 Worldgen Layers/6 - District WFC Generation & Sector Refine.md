# TLDL: MetVanDAMN — District WFC Generation & Sector Refinement

**Author:** @Bellok — Tiny Walnut Games  
**Date:** 2025‑08‑21  
**Security Classification:** Internal Development Log  
**Last AI Session:** 2025‑08‑21 09:44 EDT  

---

## 🎯 Purpose

Design and implement the **macro‑level world generation** for MetVanDAMN using **Wave Function Collapse (WFC)**, then **refine sectors** to add loops, hard locks, and path validation. This stage establishes the high‑level topology before biome blending and gate orchestration.

---

## 🗓 Timeline & Context

- **When:** 2025‑08‑21 — following biome/polarity layer integration.  
- **Why:** The WFC pass creates the “district graph” — the scaffold all later layers depend on. Sector refinement ensures the generated world has the genre‑critical flow: loops for backtracking and gates for progression.  
- **How:** Burst‑compiled ECS systems/jobs for WFC candidate initialization, iterative collapse, constraint propagation, and deterministic refinement passes.

---

## ✅ Completed Components & Systems

```
DistrictWfcSystem.cs
 - ISystem, Burst‑compiled, runs in Simulation group.
 - Lookups/buffers: WfcState, WfcTilePrototype, WfcSocketBufferElement, WfcCandidateBufferElement.
 - Deterministic per‑frame random seeding.
 - Schedules DistrictWfcJob in parallel.

DistrictWfcJob
 - Handles Initialized → InProgress → Completed/Failed state machine.
 - InitializeCandidates(): populates candidate tiles (stubbed with sample IDs + weights).
 - ProcessWfcStep(): collapses singletons, detects contradictions, propagates constraints (stubs for ValidateBiomeCompatibility/ValidatePolarityCompatibility/ValidateSocketConstraints).
 - PropagateConstraints(): filters candidates, adjusts weights based on entropy, position bias (hub vs. specialized).
 - CollapseRandomly(): weighted selection fallback after >100 iterations.

SectorRefineSystem.cs
 - Runs after DistrictWfcSystem.
 - Lookups/buffers: SectorRefinementData, WfcState, NodeId, ConnectionBufferElement, GateConditionBufferElement.
 - Schedules SectorRefinementJob in parallel.

SectorRefinementJob
 - Phase‑based progression: Planning → LoopCreation → LockPlacement → PathValidation → Completed/Failed.
 - Planning: waits for WFC completion, sets CriticalPathLength and TargetLoopDensity.
 - LoopCreation: adds deterministic loops with polarity constraints.
 - LockPlacement: inserts first hard lock at 6–10 rooms; adds more locks based on path length.
 - PathValidation: basic solvability checks (loops + locks present).
```

---

## 🛠 Key Behaviors

**Candidate Initialization:**
- Seeds with hub, corridor, chamber, specialist tiles (IDs 1–4).
- Sets initial entropy to candidate count.

**Constraint Propagation (stubbed for now):**
- Will eventually check:
  - Biome compatibility (tile ↔ assigned biome type).
  - Polarity compatibility (tile ↔ node polarity fields).
  - Socket constraints (tile socket ↔ neighbor socket).

**Weight Biasing:**
- Hubs: bias toward center.
- Specialized tiles: bias toward edges.

**Sector Refinement Loops:**
- Deterministic polarity per loop index (None → Sun → Heat → SunMoon).
- One‑way connections with traversal cost scaling.

**Hard Lock Placement:**
- Uses GateCondition with fixed polarity/ability per lock index.
- Descriptions for debug/UI.

---

## 📊 Smoke Test Validation

**Environment:** Unity Entities 1.0+ project, SmokeTestSceneSetup.  
**Result:**
- WFC job schedules without errors; candidate buffers populated.
- Stub validation passes; constraint methods return `true` until implemented.
- Sector refinement cycles through Planning → LoopCreation → LockPlacement → PathValidation.
- Logs show loop and lock counts aligning with TargetLoopDensity and CriticalPathLength.

---

## 📌 Known Issues / Next Pass

```
- Constraint validation methods in DistrictWfcJob are stubs; need biome/polarity/socket checks wired in.
- Loop creation currently unaware of biome polarity; will need cross‑layer data feed.
- PathValidation is minimal; no full reachability/backtracking scoring yet.
- Gate placement not yet influenced by biome difficulty or polarity field data.
```

---

## 🎯 Next Steps

1. Implement biome/polarity/socket constraint checks in DistrictWfcJob.
2. Add neighbor‑aware candidate pruning for faster WFC convergence.
3. Integrate BiomeFieldSystem data into loop polarity/gate placement.
4. Replace PathValidation with ProgressionSimulatorSystem reachability & loop density scoring.
5. Wire lock/loop data into CI badge + Chronicle Keeper scrolls.

---

## 📜 Lessons Learned

- Phased sector refinement makes it easy to pause/inspect world state between steps.
- Deterministic seeding per entity ensures reproducible layouts for debugging.
- Weight biasing lets you sculpt macro flow without hard‑coding placements.

---

**Milestone Goal:** Macro district graph generated, sectors refined with basic loops and locks, ready for biome/gate integration.  
**Success Criteria:** No runtime/compile errors; WFC candidates collapse; sector refinement places loops and hard locks in a reproducible, inspectable way.
