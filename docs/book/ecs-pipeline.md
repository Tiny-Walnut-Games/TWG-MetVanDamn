# ECS Pipeline

MetVanDAMN is ECS-first. Authoring data drives runtime systems that generate districts, sectors, rooms, and biomes.

## Core Flow

WorldAuthoring (config)
    ↓
ECS World Generation Systems
    ↓
District / Sector / Room Entities
    ↓
BiomeArtProfile (visual layer)
    ↓
Tilemap + Prop Placement

## Determinism

- Use `worldSeed` to reproduce the same world
- Critical algorithms favor Burst-friendly data and allocations

## Validation

- Use Dungeon Delve Preview Tool to validate progression rules and counts in the Editor
- Use Entity Debugger to inspect generated entity data

## Extensibility

- Follow the Sacred Code Classification Protocol
  - PROTECTED CORE: generation algorithms, blittable components
  - INTENDED EXPANSION: content rules, validation, art strategies

## Testing

- Unit tests cover graph and refinement behaviors (see TinyWalnutGames.MetVD.Graph.Tests)
- Integration tests validate Authoring and baking flows
