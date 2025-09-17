# TLDL: MetVanDAMN — DistrictWfcSystem Test Harness Fix (ISystem Compliance)

**Author:** @Bellok — Tiny Walnut Games  
**Date:** 2025‑08‑21  
**Security Classification:** Internal Development Log  
**Disclaimer:** At this time, the TLDA is still under development. The current state of the Chronicle Keeper is broken as the TLDA must be customized for each project. While most of the setup is complete, the updated template repo reorganization has broken references and links in documentation and code. Until repaired, automatic documentation is not possible.

---

## 🎯 Purpose

Update the **WfcSystemTests** to correctly drive `DistrictWfcSystem` — an unmanaged `ISystem` — using Unity.Entities 1.0‑compliant patterns.  
Remove legacy managed‑system calls and ensure the test harness ticks the system via the proper simulation group.

---

## 🗓 Timeline & Context

- **When:** 2025‑08‑21 — after core ECS systems were compiling and running in Unity.  
- **Why:** Tests were failing to compile/run because they attempted to create and update `DistrictWfcSystem` as a managed system.  
- **How:** Replace direct `GetOrCreateSystemManaged<T>` and `.Update()` calls with SimulationSystemGroup‑driven updates.

---

## ✅ Changes Made

**File:**  
`Packages/com.tinywalnutgames.metvd.graph/Tests/Runtime/WfcSystemTests.cs`

**Before:**  
```csharp
// Invalid for unmanaged ISystem
var wfcSystem = world.GetOrCreateSystemManaged<DistrictWfcSystem>();
wfcSystem.Update();
```

**After:**  
```csharp
// Correct unmanaged system pattern
var simGroup = world.GetOrCreateSystemManaged<SimulationSystemGroup>();
// DistrictWfcSystem is automatically part of SimulationSystemGroup via [UpdateInGroup]
simGroup.Update(world.Unmanaged);
```

**Summary of Edits:**
- Removed `GetOrCreateSystemManaged<DistrictWfcSystem>()` and `CreateSystem<DistrictWfcSystem>()` calls.
- Removed direct `wfcSystem.Update()` calls.
- Added `SimulationSystemGroup` reference and updated via `simGroup.Update(world.Unmanaged)`.
- Adjusted test setup to ensure `DistrictWfcSystem` is registered in the group before ticking.

---

## 🧪 Validation

- **Build:** Succeeded after edits — no compile errors in test assembly.
- **Runtime:** Tests now tick `DistrictWfcSystem` through the SimulationSystemGroup without exceptions.
- **Behavior:** No functional changes to system logic; purely a harness compliance fix.

---

## 📌 Known Issues / Next Pass

```
- Tests still use stubbed WFC constraint validation; will need updates once those methods are implemented.
- No assertions yet for multi‑frame WFC progression; current tests only validate initial tick behavior.
```

---

## 🎯 Next Steps

1. Add assertions for WFC state transitions across multiple SimulationSystemGroup updates.
2. Expand test coverage to include candidate collapse, constraint propagation, and contradiction handling.
3. Integrate seed replay tests for DistrictWfcSystem into CI Chronicle Keeper once TLDA is repaired.

---

## 📜 Lessons Learned

- Unmanaged `ISystem` instances cannot be created/updated like managed systems; they must be driven by a system group.
- SimulationSystemGroup is the correct entry point for ticking systems in tests, ensuring update order and dependencies are respected.
- Aligning test harness patterns with runtime execution paths reduces divergence and future breakage.

---

**Milestone Goal:** Restore WfcSystemTests to green by aligning with Unity.Entities 1.0 unmanaged system patterns.  
**Success Criteria:** Tests compile and run without errors; `DistrictWfcSystem` ticks via SimulationSystemGroup in test context.
