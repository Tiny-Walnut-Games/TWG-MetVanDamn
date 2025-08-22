# 📜 The MetVanDAMN A‑to‑Z Setup Codex
*A progression‑gated guide for brave repository delvers — by @Bellok of Tiny Walnut Games*

---

## A — **Acquire the Relic**
**Gate:** None — starting zone.  
Clone the repo (template‑derived) into your workspace.  
`git clone ... && cd MetVanDAMN`  
→ *Lore:* “You stand at the gates of the abandoned engine‑temple. Your first step leaves the dust undisturbed no longer.”

---

## B — **Bootstrap the World**
**Gate:** Ability: `Unity Installation`  
- Install Unity 6+/Entities 1.0 with DOTS packages.  
- Burst compiler enabled.  
→ *Badge Unlocked:* “World‑Maker’s Hands”

---

## C — **Configure the Seed**
**Gate:** Item: `WorldSeed Scroll`  
- Create `WorldConfiguration` entity.  
- Set `WorldSeed` to a known value for reproducible runs.

---

## D — **Define the Domains**
**Gate:** Ability: `Component Scribing`  
- Ensure `MetVanDAMN.Core` assembly exists with ECS components:
  - `Biome`, `Connection`, `GateCondition`, `NodeId`.

---

## E — **Engage the Collapse**
**Gate:** Ability: `System Weaving`  
- Confirm `DistrictWfcSystem`, `SectorRefineSystem`, and `BiomeFieldSystem` live in `MetVanDAMN.Generation`.  
- All tagged for `SimulationSystemGroup` so they tick in order.

---

## F — **Forge the Fields**
**Gate:** Polarity Access: `Sun | Moon | Heat | Cold`  
- Spawn four `BiomeFieldEntities` with center positions & radius.  
- Strength ≈ 0.8 to start.

---

## G — **Gate the Path**
**Gate:** Ability: `Locksmith’s Logic`  
- Add `GateConditionBufferElement` to sectors.  
- First hard lock lands between rooms 6–10 along critical path.

---

## H — **Harness the Loops**
**Gate:** Metric: `LoopDensity >= 0.3`  
- Tune `SectorRefinementData` to produce meaningful backtrack routes.  
- Verify with reachability/loop density check.

---

## I — **Inspect the Integrity**
**Gate:** Tool: `Unit Test Invocation`  
- Run `PolaritySystemTests`, `ConnectionLogicTests`, `GateConditionTests`.  
- All green before proceeding.

---

## J — **Jot the Lore**
**Gate:** None (optional bonus route)  
- Add a TLDL scroll logging your setup so far.  
- Chronicle Keeper integration will auto‑link once TLDA is repaired.

---

## K — **Kindle the Chronicle**
**Gate:** Badge: `All Tests Pass`  
- Trigger CI run to produce badges/artifacts.  
- Download seed snapshots for `/docs/seed_snapshots`.

---

## L — **Link the Locks to Biomes**
**Gate:** Ability: `Biome‑Aware Gating` (future)  
- Feed polarity strength/difficulty into `GatePlacementSystem`.

---

## M — **Map the Macro**
**Gate:** Completion: `District WFC Pass`  
- Implement constraint checks for biome/polarity/socket in `ProcessWfcStep`.

---

## N — **Name the Nodes**
**Gate:** None — flavour unlock.  
- Label entities (`HubDistrict`, `Sector_X_Y`, etc.) for logs/visualization.

---

## O — **Observe the Overworld**
**Gate:** Renderer: `Debug Overlay` (optional)  
- Add gizmos or ASCII map output for in‑scene/top‑down overview.

---

## P — **Preserve the Paths**
**Gate:** Ability: `Seed Replay`  
- Commit passing seed snapshot & store alongside replay tests.

---

## Q — **Quell the Contradictions**
**Gate:** Metric: `WFC Contradiction Rate == 0`  
- Fix constraint rules until macro graphs generate without dead ends.

---

## R — **Refine the Routes**
**Gate:** Metric: `Reachability Pass`  
- Ensure every unlock opens 2–4 meaningful re‑entries.

---

## S — **Simulate the Saga**
**Gate:** Tool: `ProgressionSimulatorSystem`  
- Run multi‑trajectory reachability/backtrack scoring; enforce thresholds.

---

## T — **Test the Temple**
**Gate:** Run: `SmokeTestSceneSetup`  
- Hit Play in Unity; confirm hub, loops, locks, biomes all spawn & tick.

---

## U — **Unveil the Upgrades**
**Gate:** None — story beat.  
- Merge tested branch; badges update; scroll posted.

---

## V — **Validate the Vault**
**Gate:** Visual + Audit check.  
- Inspect CI artifacts/logs; verify polarity/biome audits clear.

---

## W — **Weave in the Writer**
**Gate:** Ability: `Lore Scribing`  
- Embed narrative hooks into docs so onboarding mirrors gameplay.

---

## X — **eXtend the Engine**
**Gate:** PR: `New Biome/Polarity`  
- Add new content types/rules; update tests and docs.

---

## Y — **Yield to the Yearning**
**Gate:** Personal — your creative impulse.  
- Prototype a microgame on the engine to keep dev loop fun.

---

## Z — **Zero the Cycle**
**Gate:** None — NG+ Unlock.  
- Archive current scrolls; increment version; restart from A with expanded feature set.

---

*🏆 When you’ve reached Z, you’ve essentially “beaten” the current MetVanDAMN build — and unlocked New Game Plus for your own worlds.*
