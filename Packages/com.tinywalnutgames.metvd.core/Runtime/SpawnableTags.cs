using Unity.Entities;

namespace TinyWalnutGames.MetVD.Core
    {
    // Enemies
    public struct EnemyMeleeTag : IComponentData { }
    public struct EnemyRangedTag : IComponentData { }

    // Pickups
    public struct PickupHealthTag : IComponentData { }
    public struct PickupCoinTag : IComponentData { }
    public struct PickupGemTag : IComponentData { }
    public struct PickupMaterialTag : IComponentData { }
    public struct PickupEnergyTag : IComponentData { }
    public struct PickupMaxEnergyTag : IComponentData { }
    public struct PickupAbilityChargeTag : IComponentData { }
    public struct PickupHealthOrbTag : IComponentData { }
    public struct PickupMaxHealthTag : IComponentData { }
    public struct PickupDamageBoostTag : IComponentData { }
    public struct PickupDefenseBoostTag : IComponentData { }
    public struct PickupSpeedBoostTag : IComponentData { }
    public struct PickupJumpBoostTag : IComponentData { }
    public struct PickupInvincibilityTag : IComponentData { }
    public struct PickupAmmoGenericTag : IComponentData { }
    public struct PickupAmmoArrowTag : IComponentData { }
    public struct PickupAmmoShellTag : IComponentData { }
    public struct PickupAmmoEnergyTag : IComponentData { }
    public struct PickupAmmoSpecialTag : IComponentData { }
    public struct PickupQuestItemTag : IComponentData { }
    public struct PickupEventTokenTag : IComponentData { }
    public struct PickupKeyItemTag : IComponentData { }
    public struct PickupExtraLifeTag : IComponentData { }
    public struct PickupMapRevealTag : IComponentData { }
    public struct PickupSecretFinderTag : IComponentData { }
    public struct PickupRandomizerTag : IComponentData { }

    // Doors / Gates
    public struct DoorLockedTag : IComponentData { }
    public struct DoorAbilityTag : IComponentData { }
    public struct DoorTimedTag : IComponentData { }
    public struct ShortcutUnlockTag : IComponentData { }

    // Containers / Loot Sources
    public struct ChestTag : IComponentData { }
    public struct ChestLockedTag : IComponentData { }
    public struct CrateTag : IComponentData { }
    public struct CacheSecretTag : IComponentData { }
    public struct DropEnemyTag : IComponentData { }
    public struct DropBossRewardTag : IComponentData { }
    public struct DropQuestRewardTag : IComponentData { }

    // Equipment
    public struct PickupWeaponTag : IComponentData { }
    public struct PickupArmorTag : IComponentData { }
    public struct PickupToolTag : IComponentData { }

    // NPCs and Allies
    public struct NpcQuestTag : IComponentData { }
    public struct NpcMerchantTag : IComponentData { }
    public struct NpcLorekeeperTag : IComponentData { }
    public struct AllyTempTag : IComponentData { }
    public struct AllyEscortTag : IComponentData { }
    public struct AllySummonTag : IComponentData { }

    // Portals / Travel
    public struct PortalBiomeTag : IComponentData { }
    public struct FastTravelNodeTag : IComponentData { }
    public struct PortalOneTimeTag : IComponentData { }

    // Secrets
    public struct SecretRoomTag : IComponentData { }
    public struct BreakableWallTag : IComponentData { }
    public struct IllusionTileTag : IComponentData { }

    // World Events / Emitters
    public struct EventMeteorTag : IComponentData { }
    public struct EventAmbushTag : IComponentData { }
    public struct EventHazardBurstTag : IComponentData { }

    // Quest Triggers / Puzzle
    public struct TriggerItemTurninTag : IComponentData { }
    public struct TriggerEscortStartTag : IComponentData { }
    public struct TriggerPuzzleTag : IComponentData { }

    // Props
    public struct PropFloraTag : IComponentData { }
    public struct PropGeologyTag : IComponentData { }
    public struct PropRuinTag : IComponentData { }
    public struct PropIndustrialTag : IComponentData { }

    // Hazards
    public struct HazardSpikesTag : IComponentData { }
    public struct HazardLavaTag : IComponentData { }
    public struct HazardFallingDebrisTag : IComponentData { }
    public struct HazardElectricFieldTag : IComponentData { }

    // Interactives
    public struct SwitchTag : IComponentData { }
    public struct PressurePlateTag : IComponentData { }
    public struct MovableBlockTag : IComponentData { }
    public struct PuzzlePropTag : IComponentData { }

    // Debug / Development
    public struct MarkerWaypointTag : IComponentData { }
    public struct MarkerSpawnpointTag : IComponentData { }
    public struct TestAiDummyTag : IComponentData { }
    public struct TestPhysicsObjectTag : IComponentData { }
    public struct DebugFpsOverlayTag : IComponentData { }
    public struct DebugTriggerVisualizerTag : IComponentData { }
    public struct DebugGridGizmoTag : IComponentData { }

    // Setpieces / Dynamic spawns
    public struct SetpieceCrashedShipTag : IComponentData { }
    public struct SetpieceTreasureCaravanTag : IComponentData { }
    public struct SetpieceSeasonalBundleTag : IComponentData { }
    public struct SetpieceEasterEggTag : IComponentData { }
    public struct NpcQuestTempTag : IComponentData { }
    public struct HazardEmitterTimedTag : IComponentData { }
    public struct LootSpecialDropperTag : IComponentData { }
    }
