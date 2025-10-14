# Getting Started

This guide helps you open a scene and see a generated world in under 30 seconds.

1. Open `Assets/Scenes/MetVanDAMN_Baseline.unity`.
2. Add `WorldAuthoring` to an empty GameObject.
3. Configure:
   - worldSeed: 42 (deterministic)
   - worldSize: (50, 50)
   - targetSectorCount: 5
4. Play.

Alternative: Create a complete demo scene from the menu.
- Tiny Walnut Games → MetVanDAMN! → Create Base DEMO Scene → choose a demo type.
- For GameObject-oriented iteration: open GO Art Preview (GameObject Workflow) and spawn a sample prop grid.

Troubleshooting:
- No world entities: Ensure WorldAuthoring is active; check Console for generation logs.
- Nothing visible: Open Entities Hierarchy (Editor) and confirm District/Sector entities exist; enable Gizmos.
- Determinism: Use the same `worldSeed`. Different seeds generate different, coherent worlds.
