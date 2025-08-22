# MetVanDAMN Authoring Layer

Minimal authoring + baker layer enabling scene-driven setup of world generation without custom bootstrap scripts.

## Components

- WorldConfigurationAuthoring -> WorldConfiguration (seed, world size, target sectors)
- DistrictAuthoring -> NodeId, WfcState, SectorRefinementData (+ empty buffers)
- ConnectionAuthoring -> ConnectionBufferElement edges between districts
- BiomeFieldAuthoring -> BiomeFieldData field influences
- GateConditionAuthoring -> GateConditionBufferElement entries on districts

## Usage
1. Create an empty GameObject, add WorldConfigurationAuthoring.
2. Add several DistrictAuthoring objects (place in scene). Assign unique nodeIds.
3. Add ConnectionAuthoring objects specifying from/to DistrictAuthoring refs.
4. Add optional BiomeFieldAuthoring objects for biome gradient seeding.
5. Add GateConditionAuthoring to districts needing gates.
6. Enter Play Mode. Bakers convert authoring data to entities; WFC + refinement systems progress automatically.

## Notes
- Bakers avoid heavy logic; generation left to runtime systems.
- ConnectionAuthoring duplicates bidirectional links; one-way adds only forward edge.
- Gate descriptions truncated to 64 chars by FixedString64Bytes.

## TODO
- Gizmo drawing (district labels, connection arrows, biome field radius)
- Validation warnings for duplicate nodeIds
- Sample scene + prefabs
