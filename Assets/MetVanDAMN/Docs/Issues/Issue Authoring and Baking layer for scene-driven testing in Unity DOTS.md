### Issue: Authoring and Baking layer for scene-driven testing in Unity DOTS

A scene you can hit Play on should not depend on ad‑hoc bootstrappers. This issue formalizes the Authoring + Baker layer so designers can place objects, bake them to ECS, and run the full worldgen loop—no custom code required. Currently, validation relies on a MonoBehaviour harness (SmokeTestSceneSetup) and playmode tests; no reusable authoring/baking exists yet.

---

### Overview and goals

- **Problem**
  - The project lacks Authoring MonoBehaviours and Bakers for core data. Scene setup depends on a one-off bootstrapper instead of reusable authoring components.

- **Goal**
  - Create a complete Authoring + Baker layer for core worldgen concepts so a scene can be authored visually and baked into ECS without custom runtime scaffolding.

- **Outcomes**
  - **Authorable scene** with districts, connections, biome fields, and gates.
  - **Deterministic bake** that feeds existing systems (WFC, refinement, biome).
  - **Designer-friendly** inspectors with validation and helpful defaults.

- **Non-goals**
  - **No new generation logic** or rendering features.
  - **No save/load** or player progression persistence.

> Sources: 

---

### Scope and deliverables

- **Authoring/Baker pairs (minimum viable set)**
  - **World configuration**
    - WorldConfigurationAuthoring → WorldConfigurationBaker
    - ECS: WorldSeed, WorldBounds
  - **Districts (macro nodes)**
    - DistrictAuthoring → DistrictBaker
    - ECS: NodeId, WfcState, WfcCandidateBufferElement, SectorRefinementData
  - **Connections (edges)**
    - ConnectionAuthoring → ConnectionBaker
    - ECS: ConnectionBufferElement (From/To, type, required polarity, cost)
  - **Biome fields**
    - BiomeFieldAuthoring → BiomeFieldBaker
    - ECS: BiomeFieldData (primary/secondary biome, strength, gradient)
  - **Gate conditions**
    - GateConditionAuthoring → GateConditionBaker
    - ECS: GateConditionBufferElement (polarity, abilities, softness)
  - **WFC tile prototypes (if prototyped in-scene)**
    - WfcTilePrototypeAuthoring → WfcTilePrototypeBaker
    - ECS: WfcTilePrototype, WfcSocketBufferElement, weights

- **Optional quality-of-life (recommended)**
  - **Baking gizmos** for districts, connections, and field radii.
  - **Authoring validators** (OnValidate) to catch obvious mistakes (e.g., empty polarity, invalid bounds).
  - **Sample prefabs**: District, ConnectionAnchor, BiomeField.

- **Editor menus**
  - **Create > MetVanDAMN**: Quick authoring prefab drops (District, Connection Anchor, Biome Field).
  - **Tools > MetVanDAMN**: “Populate Sample Scene” action that instantiates authoring components instead of runtime-only entities.

---

### File layout and naming

- **Namespaces**
  - **Runtime ECS**: TinyWalnutGames.MetVD.Core / .Biome / .Generation
  - **Authoring**: TinyWalnutGames.MetVD.Authoring
  - **Editor (Bakers/Drawers)**: TinyWalnutGames.MetVD.Authoring.Editor

- **Folders**
  - **Assets/MetVanDAMN/Authoring/**
    - WorldConfigurationAuthoring.cs
    - DistrictAuthoring.cs
    - ConnectionAuthoring.cs
    - BiomeFieldAuthoring.cs
    - GateConditionAuthoring.cs
    - WfcTilePrototypeAuthoring.cs (optional)
  - **Assets/MetVanDAMN/Authoring/Editor/**
    - WorldConfigurationBaker.cs
    - DistrictBaker.cs
    - ConnectionBaker.cs
    - BiomeFieldBaker.cs
    - GateConditionBaker.cs
    - WfcTilePrototypeBaker.cs (optional)
    - Custom inspectors, gizmo drawers
  - **Assets/MetVanDAMN/Prefabs/**
    - District.prefab, ConnectionAnchor.prefab, BiomeField.prefab (optional)

- **Assembly definitions**
  - **MetVanDAMN.Authoring.asmdef**
    - References: Core/Generation runtime assemblies
  - **MetVanDAMN.Authoring.Editor.asmdef**
    - Editor only; references Unity.Entities.Editor, Authoring runtime asmdef

---

### Acceptance criteria and test plan

- **Authoring UX**
  - **World config**
    - Fields: Seed, WorldBounds (size/extent), Optional: TargetSectorCount.
    - Bake creates a singleton configuration entity.
  - **Districts**
    - Fields: NodeId (grid coords, level), Tags (hub/edge), Initial WfcState (Initialized).
    - Bake assigns NodeId, WfcState, SectorRefinementData (loop density default 0.3).
  - **Connections**
    - Fields: From (Transform/Reference), To (Transform/Reference), Type, RequiredPolarity, TraversalCost.
    - Bake populates ConnectionBufferElement on both endpoints as appropriate (respecting one-way).
  - **Biome fields**
    - Fields: PrimaryBiome, SecondaryBiome, Strength [0..1], Gradient [0..1], Radius, Center.
    - Bake emits BiomeFieldData; gizmo draws radius and polarity color.
  - **Gates**
    - Fields: RequiredPolarity, RequiredAbilities, Softness, MinSkill, Description.
    - Bake appends GateConditionBufferElement on target district.

- **Bake behavior**
  - **Determinism**: Given the same authored scene, bake produces identical ECS data.
  - **Conversion**: Entering Play converts all authoring to entities; the worldgen loop (WFC → refinement → biome) runs without extra code.
  - **Validation**: OnValidate warnings for empty/invalid fields (e.g., connection missing endpoints).

- **Tests (Edit Mode / Play Mode)**
  - **Bake smoke test**
    - Create a scene with 1 hub + 4 neighbors, connections around, 4 biome fields, 1 gate.
    - Enter Play: verify presence of configuration, districts, connections, fields, and gate buffers.
    - Assert systems advance at least one phase (WFC InProgress or Completed).
  - **Prefab authoring test**
    - Instantiate District prefab + ConnectionAnchor prefab pairs; bake must wire buffers correctly.
  - **Determinism test**
    - Load same scene twice with same seed; assert identical serialized ECS snapshot (hash).

> Sources: 

---

### Implementation notes and risks

- **Implementation notes**
  - **Authoring MonoBehaviours**
    - Keep fields serialized; offer sensible defaults; add tooltips.
    - Provide helper methods for grid/ID calculation in the inspector.
  - **Bakers**
    - Use IComponentData/IBufferElementData adds; avoid managed refs in runtime data.
    - Respect UpdateInGroup ordering already established by systems.
  - **Gizmos**
    - District: label NodeId and level.
    - Connection: arrow with type color; one-way marked by triangle head.
    - Biome Field: wireframe circle/sphere radius, polarity color.

- **Risks**
  - **API drift**: Entities 1.x conversion pipeline changes may affect Bakers—pin Unity/DOTS versions in README.
  - **Mis-baked connections**: Endpoint resolution errors; mitigate with inspector validation and scene GUID refs.
  - **Over-baking**: Duplicate data if the scene also uses runtime bootstrap—make bootstrap optional and document mutual exclusivity.

- **Migration**
  - **Deprecate** SmokeTestSceneSetup as the default entry path; keep it as “Sample: Programmatic Bootstrap” for CI tests.
  - **Add** a new Sample Scene that relies solely on Authoring components.

> Sources: 

---

### Task checklist

1. **Authoring foundations**
   - **Create** Authoring asmdefs (runtime, editor).
   - **Scaffold** WorldConfigurationAuthoring + Baker.
2. **Districts**
   - **Implement** DistrictAuthoring + Baker (NodeId, WfcState, SectorRefinementData).
   - **Add** gizmo labels (coords/level).
3. **Connections**
   - **Implement** ConnectionAuthoring + Baker (bidirectional/one-way, polarity, cost).
   - **Validate** endpoints; draw arrows.
4. **Biome fields**
   - **Implement** BiomeFieldAuthoring + Baker (strength, gradient, radius).
   - **Draw** field radius gizmo; color by polarity/biome.
5. **Gates**
   - **Implement** GateConditionAuthoring + Baker (polarity, abilities, softness).
   - **Append** buffer to target districts.
6. **WFC prototypes (optional)**
   - **Implement** WfcTilePrototypeAuthoring + Baker (IDs, sockets, weights).
7. **Samples**
   - **Create** prefabs for District, ConnectionAnchor, BiomeField.
   - **Author** “MetVanDAMN Authoring Sample” scene.
8. **Tests**
   - **Bake smoke test** (scene bakes and systems tick).
   - **Determinism test** (hash or snapshot compare).
9. **Docs**
   - **Add** mini READMEs to Authoring folders.
   - **Update** onboarding to prefer AuthoringSample over SmokeTest bootstrap.

---