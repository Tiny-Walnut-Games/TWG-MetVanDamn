# TLDL: MetVanDAMN — Progression Gates & GateCondition Orchestration

**Author:** @Bellok — Tiny Walnut Games  
**Date:** 2025‑08‑21  
**Security Classification:** Internal Development Log  
**Last AI Session:** 2025‑08‑21 09:59 EDT

---

## 🎯 Purpose

Establish a robust progression‑gating system that feels unmistakably Metroidvania: polarity‑aware locks, ability requirements, and skill‑based soft bypasses. Integrate gates into sector refinement so the world’s loops and critical path are paced by readable, satisfying unlocks.

---

## 🗓 Timeline & Context

- **When:** 2025‑08‑21 — after District WFC and initial Sector refinement scaffolding.  
- **Why:** Gated progression is the genre’s core loop; locks must pace exploration, create re‑entry value, and honor biome/polarity logic.  
- **How:** Core GateCondition data model + Connection traversal types, with hard‑lock placement wired into SectorRefineSystem.

---

## ✅ Completed Components & Systems

```
GateCondition.cs
 - Ability enum (movement, environmental, tools, polarity access, meta) as bitflags
 - GateSoftness enum (Hard→Trivial) for skill-based bypass semantics
 - GateCondition (Polarity RequiredPolarity, Ability RequiredAbilities, GateSoftness Softness,
   float MinimumSkillLevel, bool IsActive, bool IsUnlocked, Description)
 - Methods:
   • CanPass(availablePolarity, availableAbilities, playerSkillLevel)
     - Hard checks: polarity Any/None or bitmask overlap; abilities set inclusion
     - Soft bypass when Softness != Hard and playerSkill ≥ (1 - Softness/5)
   • GetMissingRequirements(...) returns missing polarity/abilities masks
 - GateConditionBufferElement for multi-gate entities
```
This defines the canonical “lock tuple” and pass logic used across the graph.

```
Connection.cs
 - ConnectionType (Bidirectional, OneWay, Drop, Vent, CrumbleFloor, Teleporter, ConditionalGate)
 - Connection struct: From/To, Type, RequiredPolarity, TraversalCost, IsActive/IsDiscovered
 - CanTraverseFrom(...) respects one-way rules + polarity mask
 - Buffer element for per-node fanout
```
Traversal types allow one‑way drops, vents, and conditional routes that pair naturally with gates.

```
SectorRefineSystem.cs (placement & pacing)
 - Phase machine: Planning → LoopCreation → LockPlacement → PathValidation
 - LockPlacement:
   • First hard lock placed between 6–10 rooms on the critical path
   • Additional hard locks scaled by path length (capped), with deterministic
     selections from curated ability/polarity sets
   • Each gate added to GateCondition buffer with debug Description
 - ValidatePaths ensures minimal loop/lock presence before completion
```
This wires progression locks into the macro topology with readable early gating and scalable challenge.

```
SmokeTestSceneSetup.cs
 - District entities pre-seeded with GateCondition buffers alongside WFC candidates
   so locks can be placed in the demo world immediately
```
Out‑of‑the‑box scene confirms gates participate in the first playable generation loop.

---

## 🛠 Key Behaviors (Playbook)

- **Hard vs Soft Gates:**  
  - Hard requires polarity match (Any/None or bitmask overlap) AND abilities set inclusion; otherwise blocked.  
  - Soft allows bypass if Softness != Hard AND playerSkill ≥ (1 − Softness/5). Example: Moderate (3) → threshold 0.4.

- **Polarity Semantics:**  
  - Single‑pole and dual‑pole masks supported; Any matches all, None ignores polarity.  
  - Traversal checks reuse the same bitmask overlap logic to keep doors and edges consistent.

- **Pacing Rules (Sector Refinement):**  
  - First hard lock after 6–10 rooms to “teach the language” early.  
  - Additional locks scale with critical path length; cap to avoid over‑gating.  
  - Deterministic selection pools ensure reproducible seeds while still feeling authored.

- **One‑Way Patterns:**  
  - Drops, vents, crumble floors, and teleports authored via ConnectionType; backtracking relies on alternate loops or later unlocks.

---

## 🧪 Smoke Test & Validation

- **Setup:** SmokeTest scene auto‑creates districts, WFC state, connections, and GateCondition buffers; systems begin on next frame.  
- **Result:** Gates are placed during LockPlacement, traversal logic compiles and runs; PathValidation advances to Completed/Failed based on basic loop/lock thresholds.

---

## 📌 Known Issues / Next Pass

```
- Gate placement doesn’t yet consume biome polarity/difficulty data (no “biome-aware gating”).
- PathValidation is minimal; no full reachability or backtrack value scoring yet.
- Gate unlock persistence (IsUnlocked) not wired to save/progression state.
- No unit tests validating CanPass edge cases (dual-polarity + soft bypass thresholds).
```

---

## 🎯 Next Steps (Step‑by‑Step)

1. Feed BiomeField polarity strength/difficulty into gate placement to align locks with environmental affordances.  
2. Replace PathValidation with ProgressionSimulator: simulate loadouts, compute reachable sets per unlock, and enforce 2–4 meaningful re‑entries per unlock.  
3. Add unit tests for GateCondition.CanPass covering: Any/None, dual‑polarity, partial ability sets, each Softness tier, and boundary skill values.  
4. Author “gateback” routes using ConnectionType.Drop/CrumbleFloor + later return unlocks for signature re‑entry moments.  
5. Expose gate descriptions in debug overlay; emit TLDL snippets per lock in CI Chronicle Keeper.

---

## 🧭 Reproduction (Gifted‑Children Tutorial)

- **Add a new hard gate to a node:**  
  1) Get/Add GateConditionBuffer on the entity.  
  2) Append GateCondition(requiredPolarity: Polarity.Heat, requiredAbilities: Ability.Grapple | Ability.HeatResistance, softness: GateSoftness.Hard, description: "Magma Grapple Gate").

- **Author a one‑way drop with a future return:**  
  1) Add Connection(from A → B, Type: Drop, RequiredPolarity: None).  
  2) Later, place a GateCondition on an alternate route back (e.g., MoonAccess + DoubleJump).

- **Check if a player can pass a gate:**  
  1) Query CanPass(availablePolarity, availableAbilities, playerSkill).  
  2) If false, call GetMissingRequirements(...) to display what’s needed.

---

## 📜 Lessons Learned

- Encoding gates as a polarity mask + ability set keeps rules composable and testable; soft bypass gives designers a “grit dial” without special‑cases.  
- Early, guaranteed hard‑lock placement creates immediate genre recognition; scaling additional locks by path length avoids choke‑point fatigue.  
- Aligning traversal types (one‑way, vents, drops) with gate logic makes backtracking feel authored rather than accidental.

---

**Milestone Goal:** Progression gates integrated and pacing the macro flow; hard locks land early, additional locks scale sanely, and traversal rules read consistently.  
**Success Criteria:** Gates compile and run in smoke test; locks appear per pacing rules; traversal decisions reflect polarity/ability masks; ready for biome‑aware placement and reachability scoring.
