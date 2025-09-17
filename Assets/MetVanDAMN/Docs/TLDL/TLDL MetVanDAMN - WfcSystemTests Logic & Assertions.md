# TLDL: MetVanDAMN — WfcSystemTests Logic & Assertions

**Author:** @Bellok — Tiny Walnut Games  
**Date:** 2025‑08‑21  
**Security Classification:** Internal Development Log  
**Disclaimer:** At this time, the TLDA is still under development. The current state of the Chronicle Keeper is broken as the TLDA must be customized for each project. While most of the setup is complete, the updated template repo reorganization has broken references and links in documentation and code. Until repaired, automatic documentation is not possible.

---

## 🎯 Purpose

Document the **purpose, structure, and intent** of the `WfcSystemTests` suite for `DistrictWfcSystem` now that the harness update allows unmanaged `ISystem` ticking via `SimulationSystemGroup`.  
Clarify what each test is verifying, so future contributors can extend coverage without breaking deterministic generation.

---

## 🗓 Timeline & Context

- **When:** 2025‑08‑21 — immediately following harness compliance fix for unmanaged systems.  
- **Why:** Tests verify critical **Wave Function Collapse** behaviors at the macro (district) level, ensuring initial seeding, state transitions, and collapse logic behave as expected with the current stubs.  
- **How:** Use Unity.Entities test world, register `DistrictWfcSystem` in SimulationSystemGroup, drive updates, and assert expected component/buffer state.

---

## ✅ Current Test Coverage

> Note: Naming is representative; align with actual method names in file.

### **Setup**
- Create `World` with required component types: `WfcState`, `NodeId`, `WfcCandidateBufferElement`, `WfcTilePrototype`, `WfcSocketBufferElement`.
- Ensure `DistrictWfcSystem` is present in `SimulationSystemGroup`.

### **Test 1 — Initialization Seeds Candidates**
```
• Arrange: Entity with WfcState.State = Initialized, empty Candidate buffer
• Act: Tick SimulationSystemGroup once
• Assert:
  - Candidate buffer count > 0
  - WfcState.State == InProgress
  - WfcState.Entropy == CandidateCount
```

### **Test 2 — Collapse On Single Candidate**
```
• Arrange: Entity with 1 candidate (TileId = 3, weight = 1.0f)
• Act: Tick SimulationSystemGroup once
• Assert:
  - WfcState.IsCollapsed == true
  - WfcState.State == Completed
  - AssignedTileId == 3
```

### **Test 3 — Contradiction On No Candidates**
```
• Arrange: Entity with empty Candidate buffer, State = InProgress
• Act: Tick SimulationSystemGroup once
• Assert:
  - WfcState.State == Contradiction
```

### **Test 4 — InProgress Retains Multiple Candidates**
```
• Arrange: Entity with >1 candidate, no contradictions
• Act: Tick SimulationSystemGroup once
• Assert:
  - WfcState.State == InProgress
  - Entropy reduced OR unchanged (no collapse until constraints force it)
```

---

## 🧪 Validation Status

- **Build:** Passes post‑harness fix — compiles cleanly under Unity.Entities 1.0 test assemblies.
- **Runtime:** Tests execute without exceptions; tick ordering matches runtime group sequence.
- **Assertions:** All pass with current stubbed constraint methods returning `true`.

---

## 📌 Known Issues / Next Pass

```
- Constraint validation methods in DistrictWfcJob are stubs; need tests for biome/polarity/socket filtering once implemented.
- No weight‑bias testing: cannot yet verify hub‑vs‑special tile selection.
- No multi‑frame progression tests: current suite only validates single‑tick outcomes.
- Entropy/Iteration effects untested beyond initial increment.
```

---

## 🎯 Next Steps

1. Implement constraint validation; add tests where candidates are removed based on biome/polarity/socket rules.
2. Add multi‑tick tests to observe:
   - Candidate list shrinking over iterations.
   - Weighted collapse after `Iteration > 100`.
3. Seed test with `SampleWfcData` prototypes + sockets to verify correct hub bias and polarity matching.
4. Create deterministic “full collapse” integration test — ensures that from Initialized → Completed, no contradictions occur for a given seed.
5. Hook results into CI Chronicle Keeper once TLDA is operational.

---

## 🧭 Reproduction (Gifted‑Children Tutorial)

To run these tests locally:

1. Open **Unity Test Runner** (Edit Mode).  
2. Ensure `TinyWalnutGames.MetVD.Graph.Tests` assembly definition is present and references runtime assemblies.  
3. Select `WfcSystemTests` from the test list.  
4. Run all — verify 100% pass rate.  
5. To step through, set breakpoints in `DistrictWfcJob.Execute` and tick SimulationSystemGroup manually.

---

## 📜 Lessons Learned

- Aligning tests to tick systems via SimulationSystemGroup matches runtime behavior and avoids divergence bugs.
- Single‑frame “initialization to in‑progress” and “single candidate to collapse” are fast sanity checks for core WFC state transitions.
- Stubbed constraint methods simplify early testing, but real value emerges once rules prune candidates — plan future coverage accordingly.

---

**Milestone Goal:** Ensure `DistrictWfcSystem` has a working test harness with clear, intentional coverage that’s easy to extend when constraint logic arrives.  
**Success Criteria:** All current WfcSystemTests compile and pass; contributors can read this scroll to understand exactly what’s tested and why.
