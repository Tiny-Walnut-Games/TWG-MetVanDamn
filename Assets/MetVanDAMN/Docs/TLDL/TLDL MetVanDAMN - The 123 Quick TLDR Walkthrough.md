## **0️⃣ Before You Begin — Clear the Stage**
- Install **Unity 6+ / Entities 1.0+** with DOTS packages ready.
- Clone the repo (template‑derived) into your local workspace.
- Ensure `.NET SDK` and **Burst compiler** are installed.
- Verify you can open the project without compile errors.

---

## **1️⃣ One World Seed**
- Create a **WorldConfiguration** entity with:
  - `WorldSeed` set to a fixed test value.
  - `WorldBounds` sized for your target grid.

---

## **2️⃣ Two Core Assemblies**
- `MetVanDAMN.Core` — ECS components (Biome, Connection, GateCondition, etc.).
- `MetVanDAMN.Generation` — WFC + sector refinement systems.

---

## **3️⃣ Three Crucial Systems**
- `DistrictWfcSystem`
- `SectorRefineSystem`
- `BiomeFieldSystem`  
Place them in `SimulationSystemGroup` so they tick automatically.

---

## **4️⃣ Four Polarity Fields**
- Sun, Moon, Heat, Cold — seeded in `BiomeFieldEntities`.
- Give each a center position and radius.

---

## **5️⃣ Five Test Seeds**
- Save at least five reproducible seeds to `/docs/seed_snapshots`.
- Use them for regression tests and CI comparisons.

---

## **6️⃣ Six Gate Types**
- Hard, Soft, Dual‑polarity, One‑way, Drop, Vent (expand as needed).

---

## **7️⃣ Seven Loop Density Targets**
- Tune `SectorRefinementData.TargetLoopDensity` for pacing.
- Start around 0.3, adjust to hit 7+ satisfying backtracks per full run.

---

## **8️⃣ Eight Validation Checks**
- Reachability, Loop count, Biome/polarity audit, Gate pacing, Difficulty curve, Orphan check, Contradiction check, Seed replay match.

---

## **9️⃣ Nine CI Steps**
1. Checkout  
2. Cache restore  
3. Build  
4. Unit tests  
5. Integration seed replays  
6. Badge generation  
7. Chronicle Keeper scroll generation (manual until TLDA fixed)  
8. Artifact upload  
9. README badge update

---

## **🔟 Ten Lore Hooks**
- Add narrative snippets to TLDLs for each major system.
- Chronicle Keeper picks them up once TLDA integration is restored.

---