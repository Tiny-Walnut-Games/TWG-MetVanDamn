# MetVanDAMN Authoring Layer

Minimal authoring + baker layer enabling scene-driven setup of world generation without custom bootstrap scripts.

## Components

- WorldConfigurationAuthoring -> WorldConfiguration (seed, world size, target sectors)
- DistrictAuthoring -> NodeId, WfcState, SectorRefinementData (+ empty buffers)
- ConnectionAuthoring -> ConnectionBufferElement edges between districts
- BiomeFieldAuthoring -> BiomeFieldData field influences
- GateConditionAuthoring -> GateConditionBufferElement entries on districts

## Quickstart: Baseline Playable Scene

A one‑click baseline scene generator is available to accelerate onboarding and smoke testing.

Menu Path: `MetVanDAMN/Create Baseline Scene` (Shortcut: Ctrl/Cmd + Shift + M)

What it does:
- Creates/overwrites `Assets/Scenes/MetVanDAMN_Baseline.unity`
- Generates (or reuses) additive sub‑scenes under `Assets/Scenes/SubScenes/`:
  - `WorldGen_Terrain`
  - `WorldGen_Dungeon`
  - `NPC_Interactions`
  - `UI_HUD`
- Adds a `Bootstrap` GameObject and (if present) a `SmokeTestSceneSetup` component pre‑configured with seed + world size
- Adds basic light + camera if missing
- Attaches SubScene references when the DOTS SubScene component is available; otherwise drops a harmless marker so the hierarchy still reads clearly

Why this matters:
- Zero configuration “Press Play” experience exercising core WFC + Biome seeding
- Consistent folder + naming conventions for PR / CI reproducibility
- Foundation for future authoring validations (e.g. gizmo + duplication checks)

### Reflection Fallback vs Direct Mode

The bootstrap uses reflection to locate:
- `TinyWalnutGames.MetVD.Samples.SmokeTestSceneSetup`
- `Unity.Scenes.SubScene`

Advantages of reflection mode:
- Safe when sample or DOTS packages are excluded (lean installs, CI light mode)
- No hard asmdef reference churn

If your project ALWAYS ships with full DOTS + Samples, you can switch to a direct reference variant:
1. Add assembly definition references for `TinyWalnutGames.MetVD.Samples` and the SubScene provider (Entities / Scenes).
2. Define a scripting symbol e.g. `METVD_FULL_DOTS`.
3. Wrap the reflection code: 
   ```csharp
   #if METVD_FULL_DOTS
   // direct types here
   #else
   // reflection fallback
   #endif
   ```
4. Remove the reflection helper once stable.

Reflection overhead here is negligible (< 1 ms per invocation) so keep it unless you explicitly want compile‑time breakage on missing dependencies.

### Regeneration Safety
- Existing baseline scene prompts before overwrite.
- Sub‑scenes reused if already on disk (idempotent).
- Already loaded sub‑scenes are not re‑opened (prevents duplicate tabs).

## Usage
1. Create an empty GameObject, add WorldConfigurationAuthoring.
2. Add several DistrictAuthoring objects (place in scene). Assign unique nodeIds.
3. Add ConnectionAuthoring objects specifying from/to DistrictAuthoring refs.
4. Add optional BiomeFieldAuthoring objects for biome gradient seeding.
5. Add GateConditionAuthoring to districts needing gates.
6. (Optional) Run baseline generator for a pre‑wired smoke scene.
7. Enter Play Mode. Bakers convert authoring data to entities; WFC + refinement systems progress automatically.

## Notes
- Bakers avoid heavy logic; generation left to runtime systems.
- ConnectionAuthoring duplicates bidirectional links; one-way adds only forward edge.
- Gate descriptions truncated to 64 chars by FixedString64Bytes.

## TODO
- Gizmo drawing (district labels, connection arrows, biome field radius)
- Validation warnings for duplicate nodeIds
- Sample scene + prefabs (baseline generator partially covers this)
- Authoring validation report (missing connections, unreferenced districts)
