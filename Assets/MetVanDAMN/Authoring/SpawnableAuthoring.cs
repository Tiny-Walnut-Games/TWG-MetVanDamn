#nullable enable
using Unity.Entities;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring
	{
	public sealed class EnemyMeleeAuthoring : MonoBehaviour
		{
		public class Baker : Baker<EnemyMeleeAuthoring>
			{
			public override void Bake(EnemyMeleeAuthoring authoring)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<EnemyMeleeTag>(e);
				}
			}
		}

	public sealed class EnemyRangedAuthoring : MonoBehaviour
		{
		public class Baker : Baker<EnemyRangedAuthoring>
			{
			public override void Bake(EnemyRangedAuthoring authoring)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<EnemyRangedTag>(e);
				}
			}
		}

	public sealed class PickupHealthAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupHealthAuthoring>
			{
			public override void Bake(PickupHealthAuthoring authoring)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupHealthTag>(e);
				}
			}
		}

	public sealed class PickupCoinAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupCoinAuthoring>
			{
			public override void Bake(PickupCoinAuthoring authoring)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupCoinTag>(e);
				}
			}
		}

	public sealed class DoorLockedAuthoring : MonoBehaviour
		{
		public class Baker : Baker<DoorLockedAuthoring>
			{
			public override void Bake(DoorLockedAuthoring authoring)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<DoorLockedTag>(e);
				}
			}
		}

	public sealed class DoorAbilityAuthoring : MonoBehaviour
		{
		public class Baker : Baker<DoorAbilityAuthoring>
			{
			public override void Bake(DoorAbilityAuthoring authoring)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<DoorAbilityTag>(e);
				}
			}
		}
	}

namespace TinyWalnutGames.MetVanDAMN.Authoring
	{
	public sealed class PickupGemAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupGemAuthoring>
			{
			public override void Bake(PickupGemAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupGemTag>(e);
				}
			}
		}

	public sealed class PickupMaterialAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupMaterialAuthoring>
			{
			public override void Bake(PickupMaterialAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupMaterialTag>(e);
				}
			}
		}

	public sealed class PickupEnergyAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupEnergyAuthoring>
			{
			public override void Bake(PickupEnergyAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupEnergyTag>(e);
				}
			}
		}

	public sealed class PickupMaxEnergyAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupMaxEnergyAuthoring>
			{
			public override void Bake(PickupMaxEnergyAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupMaxEnergyTag>(e);
				}
			}
		}

	public sealed class PickupAbilityChargeAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupAbilityChargeAuthoring>
			{
			public override void Bake(PickupAbilityChargeAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupAbilityChargeTag>(e);
				}
			}
		}

	public sealed class PickupHealthOrbAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupHealthOrbAuthoring>
			{
			public override void Bake(PickupHealthOrbAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupHealthOrbTag>(e);
				}
			}
		}

	public sealed class PickupMaxHealthAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupMaxHealthAuthoring>
			{
			public override void Bake(PickupMaxHealthAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupMaxHealthTag>(e);
				}
			}
		}

	public sealed class PickupDamageBoostAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupDamageBoostAuthoring>
			{
			public override void Bake(PickupDamageBoostAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupDamageBoostTag>(e);
				}
			}
		}

	public sealed class PickupDefenseBoostAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupDefenseBoostAuthoring>
			{
			public override void Bake(PickupDefenseBoostAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupDefenseBoostTag>(e);
				}
			}
		}

	public sealed class PickupSpeedBoostAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupSpeedBoostAuthoring>
			{
			public override void Bake(PickupSpeedBoostAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupSpeedBoostTag>(e);
				}
			}
		}

	public sealed class PickupJumpBoostAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupJumpBoostAuthoring>
			{
			public override void Bake(PickupJumpBoostAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupJumpBoostTag>(e);
				}
			}
		}

	public sealed class PickupInvincibilityAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupInvincibilityAuthoring>
			{
			public override void Bake(PickupInvincibilityAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupInvincibilityTag>(e);
				}
			}
		}

	public sealed class PickupAmmoGenericAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupAmmoGenericAuthoring>
			{
			public override void Bake(PickupAmmoGenericAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupAmmoGenericTag>(e);
				}
			}
		}

	public sealed class PickupAmmoArrowAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupAmmoArrowAuthoring>
			{
			public override void Bake(PickupAmmoArrowAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupAmmoArrowTag>(e);
				}
			}
		}

	public sealed class PickupAmmoShellAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupAmmoShellAuthoring>
			{
			public override void Bake(PickupAmmoShellAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupAmmoShellTag>(e);
				}
			}
		}

	public sealed class PickupAmmoEnergyAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupAmmoEnergyAuthoring>
			{
			public override void Bake(PickupAmmoEnergyAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupAmmoEnergyTag>(e);
				}
			}
		}

	public sealed class PickupAmmoSpecialAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupAmmoSpecialAuthoring>
			{
			public override void Bake(PickupAmmoSpecialAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupAmmoSpecialTag>(e);
				}
			}
		}

	public sealed class PickupQuestItemAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupQuestItemAuthoring>
			{
			public override void Bake(PickupQuestItemAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupQuestItemTag>(e);
				}
			}
		}

	public sealed class PickupEventTokenAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupEventTokenAuthoring>
			{
			public override void Bake(PickupEventTokenAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupEventTokenTag>(e);
				}
			}
		}

	public sealed class PickupKeyItemAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupKeyItemAuthoring>
			{
			public override void Bake(PickupKeyItemAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupKeyItemTag>(e);
				}
			}
		}

	public sealed class PickupExtraLifeAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupExtraLifeAuthoring>
			{
			public override void Bake(PickupExtraLifeAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupExtraLifeTag>(e);
				}
			}
		}

	public sealed class PickupMapRevealAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupMapRevealAuthoring>
			{
			public override void Bake(PickupMapRevealAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupMapRevealTag>(e);
				}
			}
		}

	public sealed class PickupSecretFinderAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupSecretFinderAuthoring>
			{
			public override void Bake(PickupSecretFinderAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupSecretFinderTag>(e);
				}
			}
		}

	public sealed class PickupRandomizerAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupRandomizerAuthoring>
			{
			public override void Bake(PickupRandomizerAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupRandomizerTag>(e);
				}
			}
		}

	public sealed class DoorTimedAuthoring : MonoBehaviour
		{
		public class Baker : Baker<DoorTimedAuthoring>
			{
			public override void Bake(DoorTimedAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<DoorTimedTag>(e);
				}
			}
		}

	public sealed class ShortcutUnlockAuthoring : MonoBehaviour
		{
		public class Baker : Baker<ShortcutUnlockAuthoring>
			{
			public override void Bake(ShortcutUnlockAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<ShortcutUnlockTag>(e);
				}
			}
		}

	public sealed class ChestAuthoring : MonoBehaviour
		{
		public class Baker : Baker<ChestAuthoring>
			{
			public override void Bake(ChestAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<ChestTag>(e);
				}
			}
		}

	public sealed class ChestLockedAuthoring : MonoBehaviour
		{
		public class Baker : Baker<ChestLockedAuthoring>
			{
			public override void Bake(ChestLockedAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<ChestLockedTag>(e);
				}
			}
		}

	public sealed class CrateAuthoring : MonoBehaviour
		{
		public class Baker : Baker<CrateAuthoring>
			{
			public override void Bake(CrateAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<CrateTag>(e);
				}
			}
		}

	public sealed class CacheSecretAuthoring : MonoBehaviour
		{
		public class Baker : Baker<CacheSecretAuthoring>
			{
			public override void Bake(CacheSecretAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<CacheSecretTag>(e);
				}
			}
		}

	public sealed class DropEnemyAuthoring : MonoBehaviour
		{
		public class Baker : Baker<DropEnemyAuthoring>
			{
			public override void Bake(DropEnemyAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<DropEnemyTag>(e);
				}
			}
		}

	public sealed class DropBossRewardAuthoring : MonoBehaviour
		{
		public class Baker : Baker<DropBossRewardAuthoring>
			{
			public override void Bake(DropBossRewardAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<DropBossRewardTag>(e);
				}
			}
		}

	public sealed class DropQuestRewardAuthoring : MonoBehaviour
		{
		public class Baker : Baker<DropQuestRewardAuthoring>
			{
			public override void Bake(DropQuestRewardAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<DropQuestRewardTag>(e);
				}
			}
		}

	public sealed class NpcQuestAuthoring : MonoBehaviour
		{
		public class Baker : Baker<NpcQuestAuthoring>
			{
			public override void Bake(NpcQuestAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<NpcQuestTag>(e);
				}
			}
		}

	public sealed class NpcMerchantAuthoring : MonoBehaviour
		{
		public class Baker : Baker<NpcMerchantAuthoring>
			{
			public override void Bake(NpcMerchantAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<NpcMerchantTag>(e);
				}
			}
		}

	public sealed class NpcLorekeeperAuthoring : MonoBehaviour
		{
		public class Baker : Baker<NpcLorekeeperAuthoring>
			{
			public override void Bake(NpcLorekeeperAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<NpcLorekeeperTag>(e);
				}
			}
		}

	public sealed class AllyTempAuthoring : MonoBehaviour
		{
		public class Baker : Baker<AllyTempAuthoring>
			{
			public override void Bake(AllyTempAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<AllyTempTag>(e);
				}
			}
		}

	public sealed class AllyEscortAuthoring : MonoBehaviour
		{
		public class Baker : Baker<AllyEscortAuthoring>
			{
			public override void Bake(AllyEscortAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<AllyEscortTag>(e);
				}
			}
		}

	public sealed class AllySummonAuthoring : MonoBehaviour
		{
		public class Baker : Baker<AllySummonAuthoring>
			{
			public override void Bake(AllySummonAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<AllySummonTag>(e);
				}
			}
		}

	public sealed class PortalBiomeAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PortalBiomeAuthoring>
			{
			public override void Bake(PortalBiomeAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PortalBiomeTag>(e);
				}
			}
		}

	public sealed class FastTravelNodeAuthoring : MonoBehaviour
		{
		public class Baker : Baker<FastTravelNodeAuthoring>
			{
			public override void Bake(FastTravelNodeAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<FastTravelNodeTag>(e);
				}
			}
		}

	public sealed class PortalOneTimeAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PortalOneTimeAuthoring>
			{
			public override void Bake(PortalOneTimeAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PortalOneTimeTag>(e);
				}
			}
		}

	public sealed class SecretRoomAuthoring : MonoBehaviour
		{
		public class Baker : Baker<SecretRoomAuthoring>
			{
			public override void Bake(SecretRoomAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<SecretRoomTag>(e);
				}
			}
		}

	public sealed class BreakableWallAuthoring : MonoBehaviour
		{
		public class Baker : Baker<BreakableWallAuthoring>
			{
			public override void Bake(BreakableWallAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<BreakableWallTag>(e);
				}
			}
		}

	public sealed class IllusionTileAuthoring : MonoBehaviour
		{
		public class Baker : Baker<IllusionTileAuthoring>
			{
			public override void Bake(IllusionTileAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<IllusionTileTag>(e);
				}
			}
		}

	public sealed class EventMeteorAuthoring : MonoBehaviour
		{
		public class Baker : Baker<EventMeteorAuthoring>
			{
			public override void Bake(EventMeteorAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<EventMeteorTag>(e);
				}
			}
		}

	public sealed class EventAmbushAuthoring : MonoBehaviour
		{
		public class Baker : Baker<EventAmbushAuthoring>
			{
			public override void Bake(EventAmbushAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<EventAmbushTag>(e);
				}
			}
		}

	public sealed class EventHazardBurstAuthoring : MonoBehaviour
		{
		public class Baker : Baker<EventHazardBurstAuthoring>
			{
			public override void Bake(EventHazardBurstAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<EventHazardBurstTag>(e);
				}
			}
		}

	public sealed class TriggerItemTurninAuthoring : MonoBehaviour
		{
		public class Baker : Baker<TriggerItemTurninAuthoring>
			{
			public override void Bake(TriggerItemTurninAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<TriggerItemTurninTag>(e);
				}
			}
		}

	public sealed class TriggerEscortStartAuthoring : MonoBehaviour
		{
		public class Baker : Baker<TriggerEscortStartAuthoring>
			{
			public override void Bake(TriggerEscortStartAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<TriggerEscortStartTag>(e);
				}
			}
		}

	public sealed class TriggerPuzzleAuthoring : MonoBehaviour
		{
		public class Baker : Baker<TriggerPuzzleAuthoring>
			{
			public override void Bake(TriggerPuzzleAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<TriggerPuzzleTag>(e);
				}
			}
		}

	public sealed class PropFloraAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PropFloraAuthoring>
			{
			public override void Bake(PropFloraAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PropFloraTag>(e);
				}
			}
		}

	public sealed class PropGeologyAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PropGeologyAuthoring>
			{
			public override void Bake(PropGeologyAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PropGeologyTag>(e);
				}
			}
		}

	public sealed class PropRuinAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PropRuinAuthoring>
			{
			public override void Bake(PropRuinAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PropRuinTag>(e);
				}
			}
		}

	public sealed class PropIndustrialAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PropIndustrialAuthoring>
			{
			public override void Bake(PropIndustrialAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PropIndustrialTag>(e);
				}
			}
		}

	public sealed class HazardSpikesAuthoring : MonoBehaviour
		{
		public class Baker : Baker<HazardSpikesAuthoring>
			{
			public override void Bake(HazardSpikesAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<HazardSpikesTag>(e);
				}
			}
		}

	public sealed class HazardLavaAuthoring : MonoBehaviour
		{
		public class Baker : Baker<HazardLavaAuthoring>
			{
			public override void Bake(HazardLavaAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<HazardLavaTag>(e);
				}
			}
		}

	public sealed class HazardFallingDebrisAuthoring : MonoBehaviour
		{
		public class Baker : Baker<HazardFallingDebrisAuthoring>
			{
			public override void Bake(HazardFallingDebrisAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<HazardFallingDebrisTag>(e);
				}
			}
		}

	public sealed class HazardElectricFieldAuthoring : MonoBehaviour
		{
		public class Baker : Baker<HazardElectricFieldAuthoring>
			{
			public override void Bake(HazardElectricFieldAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<HazardElectricFieldTag>(e);
				}
			}
		}

	public sealed class SwitchAuthoring : MonoBehaviour
		{
		public class Baker : Baker<SwitchAuthoring>
			{
			public override void Bake(SwitchAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<SwitchTag>(e);
				}
			}
		}

	public sealed class PressurePlateAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PressurePlateAuthoring>
			{
			public override void Bake(PressurePlateAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PressurePlateTag>(e);
				}
			}
		}

	public sealed class MovableBlockAuthoring : MonoBehaviour
		{
		public class Baker : Baker<MovableBlockAuthoring>
			{
			public override void Bake(MovableBlockAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<MovableBlockTag>(e);
				}
			}
		}

	public sealed class PuzzlePropAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PuzzlePropAuthoring>
			{
			public override void Bake(PuzzlePropAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PuzzlePropTag>(e);
				}
			}
		}

	public sealed class MarkerWaypointAuthoring : MonoBehaviour
		{
		public class Baker : Baker<MarkerWaypointAuthoring>
			{
			public override void Bake(MarkerWaypointAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<MarkerWaypointTag>(e);
				}
			}
		}

	public sealed class MarkerSpawnpointAuthoring : MonoBehaviour
		{
		public class Baker : Baker<MarkerSpawnpointAuthoring>
			{
			public override void Bake(MarkerSpawnpointAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<MarkerSpawnpointTag>(e);
				}
			}
		}

	public sealed class TestAiDummyAuthoring : MonoBehaviour
		{
		public class Baker : Baker<TestAiDummyAuthoring>
			{
			public override void Bake(TestAiDummyAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<TestAiDummyTag>(e);
				}
			}
		}

	public sealed class TestPhysicsObjectAuthoring : MonoBehaviour
		{
		public class Baker : Baker<TestPhysicsObjectAuthoring>
			{
			public override void Bake(TestPhysicsObjectAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<TestPhysicsObjectTag>(e);
				}
			}
		}

	public sealed class DebugFpsOverlayAuthoring : MonoBehaviour
		{
		public class Baker : Baker<DebugFpsOverlayAuthoring>
			{
			public override void Bake(DebugFpsOverlayAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<DebugFpsOverlayTag>(e);
				}
			}
		}

	public sealed class DebugTriggerVisualizerAuthoring : MonoBehaviour
		{
		public class Baker : Baker<DebugTriggerVisualizerAuthoring>
			{
			public override void Bake(DebugTriggerVisualizerAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<DebugTriggerVisualizerTag>(e);
				}
			}
		}

	public sealed class DebugGridGizmoAuthoring : MonoBehaviour
		{
		public class Baker : Baker<DebugGridGizmoAuthoring>
			{
			public override void Bake(DebugGridGizmoAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<DebugGridGizmoTag>(e);
				}
			}
		}

	// Equipment
	public sealed class PickupWeaponAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupWeaponAuthoring>
			{
			public override void Bake(PickupWeaponAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupWeaponTag>(e);
				}
			}
		}

	public sealed class PickupArmorAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupArmorAuthoring>
			{
			public override void Bake(PickupArmorAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupArmorTag>(e);
				}
			}
		}

	public sealed class PickupToolAuthoring : MonoBehaviour
		{
		public class Baker : Baker<PickupToolAuthoring>
			{
			public override void Bake(PickupToolAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<PickupToolTag>(e);
				}
			}
		}

	// Setpieces / Dynamic Spawns
	public sealed class SetpieceCrashedShipAuthoring : MonoBehaviour
		{
		public class Baker : Baker<SetpieceCrashedShipAuthoring>
			{
			public override void Bake(SetpieceCrashedShipAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<SetpieceCrashedShipTag>(e);
				}
			}
		}

	public sealed class SetpieceTreasureCaravanAuthoring : MonoBehaviour
		{
		public class Baker : Baker<SetpieceTreasureCaravanAuthoring>
			{
			public override void Bake(SetpieceTreasureCaravanAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<SetpieceTreasureCaravanTag>(e);
				}
			}
		}

	public sealed class SetpieceSeasonalBundleAuthoring : MonoBehaviour
		{
		public class Baker : Baker<SetpieceSeasonalBundleAuthoring>
			{
			public override void Bake(SetpieceSeasonalBundleAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<SetpieceSeasonalBundleTag>(e);
				}
			}
		}

	public sealed class SetpieceEasterEggAuthoring : MonoBehaviour
		{
		public class Baker : Baker<SetpieceEasterEggAuthoring>
			{
			public override void Bake(SetpieceEasterEggAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<SetpieceEasterEggTag>(e);
				}
			}
		}

	public sealed class NpcQuestTempAuthoring : MonoBehaviour
		{
		public class Baker : Baker<NpcQuestTempAuthoring>
			{
			public override void Bake(NpcQuestTempAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<NpcQuestTempTag>(e);
				}
			}
		}

	public sealed class HazardEmitterTimedAuthoring : MonoBehaviour
		{
		public class Baker : Baker<HazardEmitterTimedAuthoring>
			{
			public override void Bake(HazardEmitterTimedAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<HazardEmitterTimedTag>(e);
				}
			}
		}

	public sealed class LootSpecialDropperAuthoring : MonoBehaviour
		{
		public class Baker : Baker<LootSpecialDropperAuthoring>
			{
			public override void Bake(LootSpecialDropperAuthoring a)
				{
				Entity e = GetEntity(TransformUsageFlags.None);
				AddComponent<LootSpecialDropperTag>(e);
				}
			}
		}
	}
