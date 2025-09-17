# Sudo Actions: Authoring → One-off Runtime Requests

Sudo Actions let designers place lightweight, deterministic "do this here" hints in a scene that become a single runtime request consumable by any system.

## Quick Start

1) Authoring: Add `SudoActionHintAuthoring` to a GameObject and fill fields:

- `ActionKey`: required non-empty key (e.g., `spawn_boss`).
- `OneOff`: if true, fires once then tags hint as dispatched.
- `Center` and `Radius`: when `Radius == 0`, the request uses `Center` directly.
- `ElevationMask`: optional elevation limits.
- `TypeConstraint`: optional biome type; set `HasTypeConstraint = true` to enable.

2) Dispatcher: `SudoActionDispatcherSystem` converts hints → `SudoActionRequest` at init. Deterministically seeds using world seed, key, elevation mask, and optional type constraint.

3) Consumer: Create a system that listens for `SudoActionRequest`, filters by `ActionKey`, acts, and destroys the request entity.

Minimal example (already included): `SudoActionRequestConsumerSystem`.

```csharp
foreach (var (request, e) in SystemAPI.Query<RefRO<SudoActionRequest>>().WithEntityAccess())
{
    var req = request.ValueRO;
    if (req.ActionKey.Equals(new FixedString64Bytes("spawn_boss")))
    {
        // spawn boss at req.Center within req.Radius ...
        ecb.DestroyEntity(e);
    }
}
```

## Determinism Notes

- The dispatcher uses FNV-1a hashing over `ActionKey` to avoid platform `GetHashCode` variance.
- Seed folding includes: world seed, `ActionKey` hash, `ElevationMask`, and optional `TypeConstraint` when present.
- Zero radius → use `Center` as-is; empty `ActionKey` is skipped (and marked dispatched if `OneOff`).

## Tips

- Use multiple hints with different `ActionKey`s to drive separate systems.
- Keep `ActionKey` short and stable to preserve determinism across versions.
- One-off hints are tagged with `SudoActionDispatched` after emission to prevent re-emission.

## SudoCode Snippets (Editor authoring → ECS at runtime)

- Add `SudoCodeSnippetAuthoring` to any GameObject.
- Enter commands in the `Code` field (TextArea). Supported commands:
- `log <message>` → writes to console at init time
- `spawn <key> [x y z]` → emits a `SudoActionRequest` with optional position
- Set `RunOnce` if the snippet should execute a single time per scene load.
- Example:

```
log Preparing debug markers
spawn spawn_marker_waypoint 0 0 0
spawn spawn_boss 12 0 0
```

This flows through the same ECS pipeline: snippet → `SudoActionRequest` → registry + consumer → entity prefab spawn.
