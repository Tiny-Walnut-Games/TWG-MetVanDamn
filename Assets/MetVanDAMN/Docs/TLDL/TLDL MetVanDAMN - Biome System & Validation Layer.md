# TLDL: MetVanDAMN — Biome System & Validation Layer

**Author:** @Bellok — Tiny Walnut Games  
**Date:** 2025‑08‑21  
**Security Classification:** Internal Development Log  
**Last AI Session:** 2025‑08‑21 09:41 EDT  

---

## 🎯 Purpose

Implement **Biome**, **BiomeField**, and **BiomeValidation** systems to drive polarity‑aware biome assignment, gradient strength calculation, and difficulty scaling for procedural Metroidvania world generation. Ensure runtime coherence and provide validation hooks for debugging and CI audits.

---

## 🗓 Timeline & Context

- **When:** 2025‑08‑21 — post‑core ECS integration, pre‑full gate placement.
- **Why:** Worldgen needs biome + polarity semantics in place before gate orchestration; mismatched polarities at seams can break gating logic.
- **How:** Created ECS components, Burst‑compiled systems/jobs, and validation passes for biome coherence.

---

## ✅ Completed Components & Systems

```
Biome.cs
 - Polarity enum (bitmask, 8 poles, dual‑pole combos, Any)
 - BiomeType enum (aligned by polarity group)
 - Biome struct w/ type, primary/secondary polarity, polarity strength, difficulty modifier, helper methods

BiomeFieldData.cs
 - Lightweight IComponentData for seeding global biome field parameters

BiomeFieldSystem.cs
 - Burst‑compiled ISystem
 - Lookups: Biome, NodeId, Connection buffers
 - Deterministic per‑frame random seed
 - Schedules BiomeFieldJob in parallel

BiomeFieldJob (IJobEntity)
 - Assigns biome type if unknown (polarity+position heuristic)
 - Calculates polarity strength (base type → position → level depth → random variation)
 - Assigns secondary polarity for mixed/transition biomes
 - Updates difficulty modifier (strength + dual polarity + inherent biome difficulty)

BiomeValidationSystem.cs
 - Burst‑compiled ISystem in Presentation group
 - Runs BiomeValidationJob every ~5 seconds

BiomeValidationJob (IJobEntity)
 - Checks polarity coherence (primary before secondary, 0..1 range for strength)
 - Validates biome type ↔ polarity match
 - Validates difficulty modifier bounds
```

---

## 🛠 Key Behaviors

**Polarity Assignment:**  
- Hub/neutral biomes default to `HubArea` or `TransitionZone`.  
- Sun/Moon: Y‑axis split (sky vs. underworld/ocean).  
- Heat/Cold: X/Y thresholds for volcanic vs. frozen assignments.  
- Other poles mapped by enum modulus into valid biome IDs.

**Strength Calculation:**  
- Starts from biome base strength table.  
- Position‑based attenuation: central nodes weaker, edges stronger.  
- Level‑based scaling: deeper levels = stronger fields.  
- ±20 % random variation per node.

**Difficulty Scaling:**  
- Strength adds up to +0.5 difficulty.  
- Dual polarity adds +0.3.  
- Biome‑specific multipliers (e.g., `VoidChambers` × 1.5).

**Validation Hooks:**  
- Warn on polarity mismatch for fixed‑alignment biomes (e.g., `SolarPlains` w/o Sun).  
- Warn on secondary polarity present w/ `PrimaryPolarity.None`.  
- Flag extreme strength or difficulty values.

---

## 📊 Validation Run Summary

**Environment:** Unity Entities 1.0+ project after fresh repo clone.  
**Result:**  
- No compile errors.  
- Biome systems recognized and updatable in Unity.  
- Smoke test scene runs biome assignment pass without null refs.  
- Validation system triggers expected warnings when intentionally seeded with mismatches.

---

## 📌 Known Issues / Next Pass

```
- Current biome assignment doesn’t query neighboring nodes for seam blending.
- Validation logs are warnings only — not yet wired to CI badge/report.
- No link yet between biome polarity fields and gate placement constraints.
```

---

## 🎯 Next Steps

1. Integrate neighbor‑aware biome blending for smoother transitions.
2. Feed biome polarity data into `GatePlacementSystem` for coherent lock distribution.
3. Extend validation to assert no impossible gates are generated at biome seams.
4. Wire validation warnings into CI scrolls + badge output.

---

## 📜 Lessons Learned

- Deterministic seeding per‑frame is critical for reproducibility in debug runs.
- Encoding polarity as flags makes dual‑gate checks trivial in both biome and connection logic.
- Validation cadence (~5 s) balances runtime cost with catching drift.

---

**Milestone Goal:** Biome layer actively populates and validates polarity‑aligned terrain, ready for use by gate/sector refinement.  
**Success Criteria:** No Unity compile errors, stable runtime biome assignment, validation hooks catching seeded inconsistencies.
