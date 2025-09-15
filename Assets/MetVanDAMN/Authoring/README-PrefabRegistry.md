# SudoAction Prefab Registry (Out-of-the-box spawns)

## Keys (ECS-first starter set)

Recommended action keys to register in `EcsPrefabRegistryAuthoring`:

Core gameplay
- spawn_boss
- spawn_enemy_melee
- spawn_enemy_ranged
- spawn_npc_quest
- spawn_npc_merchant
- spawn_npc_lorekeeper

Loot / pickups
- spawn_chest, spawn_chest_locked, spawn_crate, spawn_cache_secret
- pickup_health, pickup_coin, pickup_gem, pickup_material
- pickup_weapon, pickup_armor, pickup_tool

Progression
- spawn_door_locked, spawn_door_ability, spawn_door_timed
- spawn_shortcut_unlock
- pickup_key_item, pickup_ability_unlock, pickup_quest_item

World / hazards / props
- spawn_portal_biome, spawn_fast_travel, spawn_portal_one_time
- spawn_secret_room, spawn_breakable_wall, spawn_illusion_tile
- spawn_hazard_spikes, spawn_hazard_lava, spawn_hazard_falling_debris, spawn_hazard_electric_field
- spawn_prop_flora, spawn_prop_geology, spawn_prop_ruin, spawn_prop_industrial
- setpiece_crashed_ship, setpiece_treasure_caravan, setpiece_seasonal_bundle, setpiece_easter_egg

Debug / dev
- spawn_marker_waypoint, spawn_marker_spawnpoint (use ECS prefabs for DOTS)

Consumables
- pickup_energy, pickup_max_energy, pickup_ability_charge
- pickup_damage_boost, pickup_defense_boost, pickup_speed_boost, pickup_jump_boost, pickup_invincibility
- pickup_ammo_generic, pickup_ammo_arrow, pickup_ammo_shell, pickup_ammo_energy, pickup_ammo_special
- pickup_extra_life, pickup_map_reveal, pickup_secret_finder, pickup_randomizer

Authoring tips
- Tag entity prefabs using provided authoring components:
  - BossAuthoring → BossTag
  - EnemyMeleeAuthoring → EnemyMeleeTag, EnemyRangedAuthoring → EnemyRangedTag
  - PickupHealthAuthoring → PickupHealthTag, PickupCoinAuthoring → PickupCoinTag
  - DoorLockedAuthoring → DoorLockedTag, DoorAbilityAuthoring → DoorAbilityTag
  - PickupWeaponAuthoring → PickupWeaponTag, PickupArmorAuthoring → PickupArmorTag, PickupToolAuthoring → PickupToolTag
  - SetpieceCrashedShipAuthoring → SetpieceCrashedShipTag, SetpieceTreasureCaravanAuthoring → SetpieceTreasureCaravanTag, SetpieceSeasonalBundleAuthoring → SetpieceSeasonalBundleTag, SetpieceEasterEggAuthoring → SetpieceEasterEggTag
- Add `EcsPrefabRegistryAuthoring` to a scene and register keys → prefab references.
- The ECS consumer spawns only when a key is registered. No hidden GO fallback is active by default.
- `spawn_loot`
- `spawn_marker`

Behavior:
- ECS-first: Use `EcsPrefabRegistryAuthoring` to bake an ECS registry (singleton + buffer of `EcsPrefabEntry`). The ECS consumer system will instantiate entity prefabs based on `SudoActionRequest.ActionKey`.
- If no ECS registry is present, nothing is spawned by ECS; you can enable the optional GameObject-based sample consumer by defining the `METVD_SAMPLES_GO_CONSUMER` compile symbol. In that sample mode, primitives are spawned as a visible fallback when no prefab is configured.
- You can still create a `PrefabRegistry` ScriptableObject for the sample path; production flow relies on the ECS registry for 100% DOTS consistency.
- A minimal `DemoBossController` MonoBehaviour exists for teams who prototype with classic prefabs; for ECS, tag your entity prefab with `BossTag` using `BossAuthoring`.

Editor helper
- Use menu `MetVanDAMN > Create Sample Scene With ECS Registry` to generate `Assets/Scenes/MetVanDAMN_Baseline.unity` with an `EcsPrefabRegistryAuthoring` GameObject pre-populated with representative keys. Assign your DOTS-ready prefabs in the Inspector, then enter Play.

Contract:
- Requests are consumed exactly once (request entity is destroyed after handling).
- Spawns use `SudoActionRequest.ResolvedPosition`.
- ECS path: requires an `EcsPrefabRegistry` with matching key; otherwise, no spawn occurs (by design). GO sample path: missing keys fall back to primitives.
