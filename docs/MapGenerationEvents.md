# MetVanDAMN Map Generation & Exploration Events

This document describes the runtime events emitted by `MetVanDAMNMapGenerator` to support UI overlays, analytics, tutorials, accessibility tooling, and automated story integrity validation.

## Event Overview

| Event | Signature | Timing | Purpose |
|-------|-----------|--------|---------|
| `WorldMapGenerated` | `Action<Texture2D>` | After detailed world map (if enabled) finishes rendering | Allows export pipelines, UI panels, thumbnails, or analytics to process the full map texture. |
| `MinimapUpdated` | `Action<Texture2D>` | After the minimap UI (re)generates or is refreshed due to player movement | Lets HUD systems or streaming capture reuse the same texture without polling. |
| `ExplorationProgressed` | `Action<float>` | When district exploration crosses a milestone bucket (10/25/50/75/90/100%) | Progress feedback, achievement unlock triggers, adaptive difficulty pacing. |
| `RoomExplorationProgressed` | `Action<float>` | When room exploration crosses a milestone bucket | Finer‑grained progression metrics, gating tutorial phases. |
| `MinimapPlayerMoved` | `Action<Vector2, Vector2>` (worldPos, minimapPos) | When player movement exceeds the minimap emission threshold (~2 minimap units) | Cursor/ping tracking, breadcrumb trails, minimap-centered camera logic. |

All events are `static` and pre-initialized to empty delegates to eliminate null checks and satisfy strict non-nullability rules.

## Bucket Logic
Both district and room exploration events fire only when percentage surpasses the next milestone in the ordered set:
```
[10, 25, 50, 75, 90, 100]
```
This prevents log and event spam while keeping meaningful narrative beats.

## Usage Example
```csharp
using TinyWalnutGames.MetVanDAMN.Authoring;
using UnityEngine;

public class MapEventListener : MonoBehaviour
{
    void OnEnable()
    {
        MetVanDAMNMapGenerator.WorldMapGenerated += OnWorldMap;
        MetVanDAMNMapGenerator.MinimapUpdated += OnMinimap;
        MetVanDAMNMapGenerator.ExplorationProgressed += OnDistrictProgress;
        MetVanDAMNMapGenerator.RoomExplorationProgressed += OnRoomProgress;
        MetVanDAMNMapGenerator.MinimapPlayerMoved += OnPlayerMoved;
    }

    void OnDisable()
    {
        MetVanDAMNMapGenerator.WorldMapGenerated -= OnWorldMap;
        MetVanDAMNMapGenerator.MinimapUpdated -= OnMinimap;
        MetVanDAMNMapGenerator.ExplorationProgressed -= OnDistrictProgress;
        MetVanDAMNMapGenerator.RoomExplorationProgressed -= OnRoomProgress;
        MetVanDAMNMapGenerator.MinimapPlayerMoved -= OnPlayerMoved;
    }

    private void OnWorldMap(Texture2D tex) => Debug.Log($"Full map available: {tex.width}x{tex.height}");
    private void OnMinimap(Texture2D tex) => Debug.Log("Minimap refreshed");
    private void OnDistrictProgress(float pct) => Debug.Log($"District exploration hit {pct:0.0}%");
    private void OnRoomProgress(float pct) => Debug.Log($"Room exploration hit {pct:0.0}%");
    private void OnPlayerMoved(Vector2 worldPos, Vector2 minimapPos) => Debug.Log($"Player minimap pos: {minimapPos}");
}
```

## Design Principles
- **Nullability Annihilation**: Events are always safe to invoke (never null).
- **Narrative Cohesion**: Each event corresponds to a beat in the player's traversal or map visibility story.
- **Allocation Discipline**: No per-event allocations beyond unavoidable closures in listener code.
- **Extensibility**: Future events (e.g., biome overlay applied, district state changed) can follow the same static pre-initialized pattern.

## Future Candidates

| Candidate | Rationale |
|----------|-----------|
| `BiomeOverlayApplied` | External biome art / shader layering systems can react post-pass. |
| `DistrictStateChanged` | Lock/unlock, danger state, or threat level broadcasts. |
| `MapExported` | Signal after PNG export completes for asset pipeline listeners. |

---
Generated as part of the foundation strengthening initiative (Map Generator Event Expansion).
