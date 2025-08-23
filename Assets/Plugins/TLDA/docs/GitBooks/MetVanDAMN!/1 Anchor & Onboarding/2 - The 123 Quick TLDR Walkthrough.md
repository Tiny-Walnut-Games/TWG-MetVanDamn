## **0️⃣ Before You Begin — Clear the Stage**
- Install **Unity 6+ / Entities 1.0+** with DOTS packages ready.
- Clone the repo (template‑derived) into your local workspace.
- Ensure `.NET SDK` and **Burst compiler** are installed.
- Verify you can open the project without compile errors.

---

## ⚡ One‑Click Baseline Playable Scene (Fast Onboarding)
Use the built‑in bootstrap to generate a ready‑to‑run scene + sub‑scenes that exercise world generation, WFC, biome fields and future UI/NPC layers.

Menu: `MetVanDAMN/Create Baseline Scene`  (Shortcut: Ctrl/Cmd + Shift + M)
Outputs:
- Root scene: `Assets/Scenes/MetVanDAMN_Baseline.unity`
- Sub‑scenes (additive, auto‑created if missing):
  - `WorldGen_Terrain`
  - `WorldGen_Dungeon`
  - `NPC_Interactions`
  - `UI_HUD`
- `Bootstrap` GameObject (adds `SmokeTestSceneSetup` if available) with seed + world size defaults
- Light + Camera added if absent

Two operating modes:
1. Direct Mode (define `METVD_FULL_DOTS` + have Samples & SubScene assemblies referenced) – compile‑time safety.
2. Fallback Mode (no symbol) – reflection gracefully skips absent packages (ideal for lean CI / partial installs).

Switch to Direct Mode:
1. Add asmdef references to `TinyWalnutGames.MetVD.Samples` and `Unity.Scenes`.
2. Add scripting define symbol: `METVD_FULL_DOTS` (in asmdef or Player Settings).
3. Recompile – bootstrap now uses strong types.

Why run this first?
- Instant “Press Play” validation of generation pipeline.
- Standardized layout for PRs & screenshots.
- Establishes reproducible seeds quickly for regression snapshots.

If you skip it: follow the 1️⃣–4️⃣ seeds/assemblies/systems/polarity steps below manually.

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
