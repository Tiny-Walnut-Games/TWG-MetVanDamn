# MetVanDamn + TerraECS + AstroECS performance stack (Unity 6.2 + C# 10 IL2CPP)

---

## Hardware tiers and theoretical capacities

| Tier | Hardware profile | 2D active entities | 3D active entities | Space active entities |
|---|---|---:|---:|---:|
| Development machine | 8–16 CPU cores, 32–64 GB RAM | 10k–30k mixed; 100k–200k lightweight | 10k–20k mixed; 50k–100k lightweight | 10k–30k rigidbodies; 100k+ orbital seeds |
| Single server | 32–64 CPU cores, 256–512 GB RAM | 50k–150k mixed; 200k+ lightweight | 30k–50k mixed; 100k–200k lightweight | 10k–30k rigidbodies; 200k+ orbital seeds |
| Cabinet rack (8–12 servers) | ~500–700 CPU cores, 2–4 TB RAM | 0.5M–2.0M mixed; 5M+ lightweight (rack-wide) | 0.5M–1.0M mixed (rack-wide) | 0.5M–1.0M rigidbodies; millions of orbital seeds |

> Notes: “Mixed” includes physics + AI + nav; “lightweight” excludes frequent physics/AI ticks. Orbital seeds = deterministic bodies tracked without full physics until observed.

---

## Terrain throughput (TerraECS) and interactive depth

- **Depth ≤ 3:** **Interactive**.  
  - **Performance:** 20k–50k vertices in seconds.  
- **Depth 4:** **Borderline interactive**.  
  - **Performance:** ~15 seconds per slice.  
- **Depth 5+:** **Batch/precompute only**.  
  - **Performance:** minutes; treat as background jobs or cached assets.

---

## Use case snapshots

- **Development machine**
  - **Slice:** One district or city block with hundreds–thousands of agents.
  - **Terrain:** Depth‑3 live; depth‑4 queued.
  - **Fractal:** Debug gizmos at all levels; rapid iteration.

- **Single server**
  - **Slice:** Multiple districts or a small city; shard per portal/instance.
  - **Terrain:** Depth‑4 feasible; depth‑5 batch.
  - **Fractal:** Soft cap triggers channel hop at ~50k mixed entities.

- **Cabinet rack**
  - **Slice:** Civilization/planetary scale across shards; millions of entities rack‑wide.
  - **Terrain:** Depth‑4 across active shards; deeper precomputed.
  - **Fractal:** Elastic scaling via portals; clients render/LOD.

---

## Architecture talking points

- **Server role:**  
  - **Authority:** Seeds, rules, topology, aggregate states, validation.  
  - **Simulation:** 2D physics, AI nav, rule systems; headless.

- **Client role:**  
  - **Rendering:** Meshes/materials/sprites, lighting, FX.  
  - **Expansion:** Vegetation/props via deterministic seeds and biome rules.  
  - **Prediction:** Animation and movement smoothing.

- **Fractal/sharding doctrine:**  
  - **Soft cap per instance:** **~10k–50k** mixed entities; channel hop beyond.  
  - **Lazy collapse:** Simulate observed slices; seed the rest.
  - **Subscenes:** Partition by world → district → room for streaming.

---

## Direct answers

- **2D surrealism:** Hundreds of rooms (e.g., 500 × 100 entities = 50k) per instance are feasible.  
- **3D realism:** Tens of thousands of mixed entities per slice; terrain depth capped at 3–4 for live use.  
- **Rack scale:** Millions of entities simulated across shards with a budget‑to‑midrange cabinet.