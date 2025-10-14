# GameObject Workflow

MetVanDAMN provides a complete GameObject-oriented workflow for fast visual iteration. This coexists with the ECS pipeline and does not replace it.

## Editor Menus

- Tiny Walnut Games → MetVanDAMN! → GameObject Workflow → GO Art Preview
  - Opens an editor window to preview biome palettes and spawn a deterministic grid of preview props.
- Tiny Walnut Games → MetVanDAMN! → Create Base DEMO Scene → (2D Platformer / Top-Down / 3D)
  - Creates a fully playable demo scene using GameObjects for camera/player + ECS for world generation.

## GO Art Preview

The GO Art Preview window offers:
- Deterministic seed control
- Biome color swatches (Sun, Moon, Heat, Cold, Earth, Wind, Life, Tech)
- One-click demo scene creation
- Spawn Sample Prop Grid (cubes/spheres/capsules) tinted by biome palette

The preview spawns under `__GOArtPreview_Root__`. Use Clear to remove it.

## Safe Defaults & Completeness

- All components are added explicitly; no null references
- No TODOs or placeholders in the tooling
- Menus are visible in Unity 6000.2.6f2 with URP/Lit shader defaults

## Extending

This is an Intended Expansion Zone:
- Replace primitive props with your project prefabs
- Extend palette generation to sample from your BiomeArtProfile assets
- Add thumbnails or sprite previews as needed
