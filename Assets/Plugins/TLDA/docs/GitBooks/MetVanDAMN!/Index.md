# MetVanDAMN Master Scroll Index (Linked Edition)

> Curated build order of the engine’s living design & validation scrolls. Follow in sequence for the full New‑Game‑Plus onboarding arc: **setup → core → generation layers → validation → automation**.

---
## Quick Jump
- [1. Anchor & Onboarding](#1--anchor--onboarding)
- [2. Core ECS Integration](#2--core-ecs-integration)
- [3. Worldgen Layers](#3--worldgen-layers)
- [4. Testing & Validation](#4--testing--validation)
- [5. CI & Keeper Wiring](#5--ci--keeper-wiring)
- [Full Linear Navigation](#linear-nextprevious-navigation)
- [Document Map Table](#document-map-table)

---
## 1 — Anchor & Onboarding
1. **[The A to Z MetVanDAMN Onboarding Codex](./1%20Anchor%20%26%20Onboarding/1%20-%20The%20A%20to%20Z%20MetVanDAMN%20Onboarding%20Codex.md)**  
   Progression‑gated ritual guide (A→Z) establishing lore, setup steps, and NG+ philosophy.
2. **[The 123 Quick TLDR Walkthrough](./1%20Anchor%20%26%20Onboarding/2%20-%20The%20123%20Quick%20TLDR%20Walkthrough.md)**  
   Minimal “just run it” path: 0–10 checklist for impatient validators.
3. **[Procedural Metroidvania Engine](./1%20Anchor%20%26%20Onboarding/3%20-%20Procedural%20Metroidvania%20Engine.md)**  
   Source‑of‑truth macro design: subsystems, naming conventions, progression grammar.

---
## 2 — Core ECS Integration
4. **[Core & Biome Systems Integration](./2%20Core%20ECS%20Integration/4%20-%20Core%20%26%20Biome%20Systems%20Integration.md)**  
   First clean compile milestone: core components (Biome / Connection / GateCondition / NodeId) + stub systems.

---
## 3 — Worldgen Layers
5. **[Biome System & Validation Layer](./3%20Worldgen%20Layers/5%20-%20Biome%20System%20%26%20Validation%20Layer.md)**  
   Polarity‑aware biome assignment, strength/difficulty scaling, validation cadence.
6. **[District WFC Generation & Sector Refinement](./3%20Worldgen%20Layers/6%20-%20District%20WFC%20Generation%20%26%20Sector%20Refine.md)**  
   Macro topology (WFC) + loop / lock refinement phases (planning → loops → locks → validation).
7. **[Progression Gates & GateCondition Orchestration](./3%20Worldgen%20Layers/7%20-%20Progression%20Gates%20%26%20GateCondition%20Orchestration.md)**  
   Polarity + ability gating model, pacing rules, soft vs hard lock semantics.

---
## 4 — Testing & Validation
8. **[Unit Testing & Validation Artifacts](./4%20Testing%20%26%20Validation/8%20-%20Unit%20Testing%20%26%20Validation%20Artifacts.md)**  
   Test suite architecture (core / generation / integration seed replay) + coverage goals.
9. **[Smoke Test Scene Setup & Immediate Validation](./4%20Testing%20%26%20Validation/9%20-%20Smoke%20Test%20Scene%20Setup%20%26%20Immediate%20Validation.md)**  
   One‑click scene harness seeding world, WFC, refinement, biome fields.
10. **[WfcSystemTests Logic & Assertions](./4%20Testing%20%26%20Validation/10%20-%20WfcSystemTests%20Logic%20%26%20Assertions.md)**  
    Intent + coverage of WFC tests (initialization, collapse, contradiction).
11. **[DistrictWfcSystem Test Harness Fix (ISystem Compliance)](./4%20Testing%20%26%20Validation/11%20-%20DistrictWfcSystem%20Test%20Harness%20Fix%20(ISystem%20Compliance).md)**  
    Migration of tests to proper unmanaged ISystem ticking via SimulationSystemGroup.

---
## 5 — CI & Keeper Wiring
12. **[Integration & CI Chronicle Keeper Wiring](./5%20CI%20%26%20Keeper%20Wiring/12%20-%20Integration%20%26%20CI%20Chronicle%20Keeper%20Wiring.md)**  
    Automated narration: tests → badges → scroll synthesis with multiline‑safe event parsing.

---
## Linear Next/Previous Navigation
| # | Title | Next |
|---|-------|------|
|1|A to Z Onboarding Codex|2|
|2|123 Quick TLDR|3|
|3|Procedural Metroidvania Engine|4|
|4|Core & Biome Systems Integration|5|
|5|Biome System & Validation Layer|6|
|6|District WFC Generation & Sector Refinement|7|
|7|Progression Gates & GateCondition Orchestration|8|
|8|Unit Testing & Validation Artifacts|9|
|9|Smoke Test Scene Setup & Immediate Validation|10|
|10|WfcSystemTests Logic & Assertions|11|
|11|DistrictWfcSystem Test Harness Fix|12|
|12|Integration & CI Chronicle Keeper Wiring|—|

> For any scroll, treat its numeric position as an ordered chapter. Future additions should append while preserving existing numbering (avoid renumbering to keep external references stable).

---
## Document Map Table
| # | Scroll | Domain Focus | Key Outcome |
|---|--------|--------------|-------------|
|1|A to Z Codex|Onboarding Lore|Sequential NG+ style setup|
|2|123 TLDR|Rapid Start|Minimal reproducible seed run|
|3|Engine Source Scroll|Architecture|Subsystem taxonomy & naming|
|4|Core & Biome Integration|Baseline Compile|Clean ECS core + stubs|
|5|Biome System & Validation|Biome Layer|Polarity strength + audits|
|6|District WFC & Refinement|Macro Gen|Topology + loops + locks|
|7|Progression Gates|Gating|Lock pacing & pass logic|
|8|Unit Test Layer|Quality|Deterministic validation suite|
|9|Smoke Test Scene|Runtime Harness|Immediate editor feedback|
|10|WFC Tests Logic|Test Design|Assertion intent & scope|
|11|WFC Harness Fix|Test Infra|ISystem compliance update|
|12|CI Chronicle Wiring|Automation|Narrated runs + badges|

---
## Usage Guidance
- Start at (1) if completely new; jump to (6–8) if validating generation pipeline health.
- Always consult (8) + (12) before large refactors—ensures test & CI narrative impact understood.
- When adding a new layer (e.g., Progression Simulator), allocate next number (13) and link it here under the appropriate section.

---
## Extension Slots (Reserved)
| Planned # | Placeholder | Rationale |
|-----------|-------------|-----------|
|13|Progression Simulator & Reachability Scoring|Will replace basic PathValidation metrics|
|14|Reward Weaver & Backtrack Economics|Populate re-entry loops with value|
|15|Seed Diff Visualizer & ASCII Maps|Aid CI artifact comprehension|

---
## Keeper Extraction Hint
`KeeperNote:` style annotations inside source + this index allow future tooling to auto‑assemble a release compendium (regex anchor: `^(KeeperNote:)`).

---
_Last updated: ${CURRENT_DATE}_
