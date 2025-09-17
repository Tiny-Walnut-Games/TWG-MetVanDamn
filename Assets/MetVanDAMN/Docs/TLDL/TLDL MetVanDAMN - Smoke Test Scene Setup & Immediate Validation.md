# TLDL: MetVanDAMN — Smoke Test Scene Setup & Immediate Validation

**Author:** @Bellok — Tiny Walnut Games  
**Date:** 2025‑08‑21  
**Security Classification:** Internal Development Log  
**Last AI Session:** 2025‑08‑21 10:05 EDT  

---

## 🎯 Purpose

Create an **immediate‑feedback harness** for MetVanDAMN’s procedural engine so you can *hit Play in Unity* and see a generated map with:
- District WFC topology
- Sector refinement with loops and hard locks
- Biome/polarity fields

…all without manual setup. This scene doubles as a **validation sandbox** for ECS systems, ensuring the entire generation loop works in‑editor after a fresh clone.

---

## 🗓 Timeline & Context

- **When:** 2025‑08‑21 — after Core, Biome, WFC, Gate, and Refinement layers were compiling cleanly.  
- **Why:** Needed a turnkey, reproducible environment to verify the whole engine stack works in Unity Entities 1.0+ without additional bootstrap code.  
- **How:** Authored `SmokeTestSceneSetup` MonoBehaviour to spawn minimal world config + entities, seeding them with all necessary components and buffers for systems to run on the next frame.

---

## ✅ Completed Setup Steps

```
Scene: SmokeTestSceneSetup.cs
 - Public params: worldSeed, worldSize, targetSectorCount, biomeTransitionRadius
 - Debug toggles for visualization & logging

SetupSmokeTestWorld():
 1) CreateWorldConfiguration():
    - Entity "WorldConfiguration"
    - WorldSeed + WorldBounds components

 2) CreateDistrictEntities():
    - Hub district at (0,0), Level 0, Value 0
    - WfcState, WfcCandidateBufferElement, ConnectionBufferElement
    - Grid of surrounding districts (-2..2, skip 0,0), spaced at (x*10, y*10)
    - Assign NodeId with Level = |x| + |y|
    - Add SectorRefinementData (0.3 loop density)
    - Add GateConditionBufferElement

 3) CreateBiomeFieldEntities():
    - Four PolarityFieldData zones (Sun, Moon, Heat, Cold)
    - Centered around ±15 coords, radius = biomeTransitionRadius
    - Strength = 0.8f

Log:
 - ✔ "🚀 MetVanDAMN Smoke Test: Starting world generation..."
 - ✔ "✅ MetVanDAMN Smoke Test: World setup complete with seed X"
 - ✔ Prints world size, target sectors, and notice that systems run next frame
```

---

## 🛠 Key Behaviors

- **Deterministic World Seed:** WorldSeed component makes WFC + random selections reproducible.  
- **Minimal Entity Set:** Only essential ECS components created to trigger:
  - DistrictWfcSystem → SectorRefineSystem → BiomeFieldSystem
- **Grid Layout:** Ensures early WFC has meaningful adjacency (hub + outer districts).  
- **Biome Polarity Fields:** Placed before biome assignment so `BiomeFieldSystem` can use them immediately.

---

## 🧪 Validation Results

**Environment:** Unity Editor, Entities 1.0+  
**Result:**  
- Systems auto‑ran after play; WFC candidates populated; Sector refinement advanced; Biome fields assigned.  
- No compile or runtime errors after fresh repo clone.  
- Logs confirm sequential execution: configuration → district spawn → biome field spawn → systems tick on next frame.

---

## 📌 Known Issues / Next Pass

```
- No direct visualization: relies on debug logs unless paired with a renderer.
- BiomeFieldEntities use static positions; no procedural placement logic yet.
- GateConditions in smoke test are empty until SectorRefineSystem runs LockPlacement.
- No assertions in setup: silent fail if Entities 1.0 API changes break component signatures.
```

---

## 🎯 Next Steps

1. Add lightweight debug renderer to visualize districts, connections, and polarity fields in scene view.  
2. Auto‑spawn a `SamplePlayerEntity` with baseline abilities for in‑scene traversal tests.  
3. Populate GateConditionBuffer in setup for known PR seeds to demo gate checks immediately.  
4. Hook SmokeTest scene into CI playmode tests to catch regression in ECS workflows.

---

## 📜 Lessons Learned

- Keeping setup atomic and MonoBehaviour‑driven makes first‑run validation frictionless.  
- Explicit naming of entities (`HubDistrict`, `District_X_Y`, polarity field names) eases log reading and debugging.  
- Even minimal ECS worlds should seed all required buffers/components to avoid conditional null‑paths in systems.

---

**Milestone Goal:** Provide a one‑click scene to prove all generation systems survive Unity compilation and execute correctly after repo clone.  
**Success Criteria:** No compile/runtime errors; WFC, refinement, and biome systems tick automatically; developer sees logs and/or visualization confirming full loop operation.
