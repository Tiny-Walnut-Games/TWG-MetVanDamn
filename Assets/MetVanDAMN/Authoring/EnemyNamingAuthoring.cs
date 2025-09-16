using Unity.Entities;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring
	{
	/// <summary>
	/// Authoring component for setting up enemy naming and affix display
	/// Provides Unity Inspector interface for configuring enemy profiles
	/// </summary>
	public class EnemyNamingAuthoring : MonoBehaviour
		{
		[Header("Enemy Profile")]
		[SerializeField] private RarityType rarity = RarityType.Common;
		[SerializeField] private string baseType = "Enemy";
		[SerializeField] private uint generationSeed = 12345;

		[Header("Affix Assignment")]
		[SerializeField] private bool useRandomAffixes = true;
		[SerializeField] private string[] specificAffixIds = new string[0];

		[Header("Display Configuration")]
		[SerializeField] private bool overrideGlobalDisplayMode = false;
		[SerializeField] private AffixDisplayMode displayModeOverride = AffixDisplayMode.NamesAndIcons;

		[Header("Auto-Generation")]
		[SerializeField] private bool generateNameOnBake = true;

		/// <summary>
		/// Baker for converting authoring component to ECS data
		/// </summary>
		public class Baker : Baker<EnemyNamingAuthoring>
			{
			public override void Bake(EnemyNamingAuthoring authoring)
				{
				var entity = GetEntity(TransformUsageFlags.Dynamic);

				// Add enemy profile
				AddComponent(entity, new EnemyProfile(
					authoring.rarity,
					authoring.baseType,
					authoring.generationSeed == 0 ? (uint)authoring.GetInstanceID() : authoring.generationSeed
				));

				// Mark for name generation if requested
				if (authoring.generateNameOnBake)
					{
					AddComponent<NeedsNameGeneration>(entity);
					}

				// Add display configuration override if specified
				if (authoring.overrideGlobalDisplayMode)
					{
					AddComponent(entity, new EnemyNaming(
						authoring.baseType, // Temporary, will be overwritten by naming system
						ShouldShowFullName(authoring.rarity),
						ShouldShowIcons(authoring.displayModeOverride),
						authoring.displayModeOverride
					));
					}

				// Handle affix assignment
				if (authoring.useRandomAffixes)
					{
					// Random affixes will be assigned by EnemyAffixDatabase.AssignRandomAffixes
					// This is typically done in a bootstrap system or initialization
					AddComponent<NeedsRandomAffixAssignment>(entity);
					}
				else if (authoring.specificAffixIds != null && authoring.specificAffixIds.Length > 0)
					{
					// Specific affixes will be resolved during baking
					AddComponent<NeedsSpecificAffixAssignment>(entity);
					
					// Store the affix IDs for later resolution
					var affixIdBuffer = AddBuffer<PendingAffixIdElement>(entity);
					foreach (var affixId in authoring.specificAffixIds)
						{
						if (!string.IsNullOrEmpty(affixId))
							{
							affixIdBuffer.Add(new PendingAffixIdElement { AffixId = affixId });
							}
						}
					}
				}

			/// <summary>
			/// Determine if an enemy should show its full name based on rarity
			/// </summary>
			private static bool ShouldShowFullName(RarityType rarity)
				{
				return rarity switch
					{
						RarityType.Common => false,
						RarityType.Uncommon => false,
						RarityType.Rare => true,
						RarityType.Unique => true,
						RarityType.MiniBoss => true,
						RarityType.Boss => true,
						RarityType.FinalBoss => true,
						_ => false
					};
				}

			/// <summary>
			/// Determine if an enemy should show affix icons
			/// </summary>
			private static bool ShouldShowIcons(AffixDisplayMode mode)
				{
				return mode != AffixDisplayMode.NamesOnly;
				}
			}

		/// <summary>
		/// Preview the generated name in the inspector (Editor only)
		/// </summary>
		[ContextMenu("Preview Generated Name")]
		private void PreviewGeneratedName()
			{
			#if UNITY_EDITOR
			Debug.Log($"Enemy Profile Preview:\n" +
					 $"Rarity: {rarity}\n" +
					 $"Base Type: {baseType}\n" +
					 $"Seed: {generationSeed}\n" +
					 $"Expected Behavior: {GetExpectedBehavior()}");
			#endif
			}

		/// <summary>
		/// Get expected behavior description for this configuration
		/// </summary>
		private string GetExpectedBehavior()
			{
			string behavior = "";

			// Name display behavior
			switch (rarity)
				{
				case RarityType.Common:
				case RarityType.Uncommon:
					behavior += $"Will show '{baseType}' + affix icons";
					break;
				case RarityType.Rare:
				case RarityType.Unique:
					behavior += $"Will show full name like 'Adjective {baseType} of Trait' + icons";
					break;
				case RarityType.MiniBoss:
				case RarityType.Boss:
				case RarityType.FinalBoss:
					behavior += "Will show procedural name from affix syllables + icons";
					break;
				}

			// Affix count
			int expectedAffixCount = GetExpectedAffixCount(rarity);
			behavior += $"\nExpected {expectedAffixCount} affix(es)";

			// Display mode
			if (overrideGlobalDisplayMode)
				{
				behavior += $"\nDisplay mode override: {displayModeOverride}";
				}

			return behavior;
			}

		/// <summary>
		/// Get expected number of affixes for a rarity
		/// </summary>
		private static int GetExpectedAffixCount(RarityType rarity)
			{
			return rarity switch
				{
					RarityType.Common => 1,
					RarityType.Uncommon => 2,
					RarityType.Rare => 2,
					RarityType.Unique => 3,
					RarityType.MiniBoss => 2,
					RarityType.Boss => 3,
					RarityType.FinalBoss => 4,
					_ => 1
				};
			}

		#if UNITY_EDITOR
		/// <summary>
		/// Validate configuration in the inspector
		/// </summary>
		private void OnValidate()
			{
			// Ensure base type is not empty
			if (string.IsNullOrEmpty(baseType))
				{
				baseType = "Enemy";
				}

			// Validate affix IDs if using specific assignment
			if (!useRandomAffixes && specificAffixIds != null)
				{
				for (int i = 0; i < specificAffixIds.Length; i++)
					{
					if (string.IsNullOrEmpty(specificAffixIds[i]))
						{
						specificAffixIds[i] = "";
						}
					}
				}
			}
		#endif
		}

	/// <summary>
	/// Tag component indicating entity needs random affix assignment
	/// </summary>
	public struct NeedsRandomAffixAssignment : IComponentData { }

	/// <summary>
	/// Tag component indicating entity needs specific affix assignment
	/// </summary>
	public struct NeedsSpecificAffixAssignment : IComponentData { }

	/// <summary>
	/// Buffer element for storing pending affix IDs during baking
	/// </summary>
	public struct PendingAffixIdElement : IBufferElementData
		{
		public FixedString64Bytes AffixId;

		public PendingAffixIdElement(string affixId)
			{
			AffixId = new FixedString64Bytes(affixId);
			}
		}

	/// <summary>
	/// System that processes entities needing affix assignment after database initialization
	/// </summary>
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	[UpdateAfter(typeof(EnemyNamingSystem))]
	public partial struct EnemyAffixAssignmentSystem : ISystem
		{
		public void OnCreate(ref SystemState state)
			{
			state.RequireForUpdate<AffixDatabaseTag>();
			}

		public void OnUpdate(ref SystemState state)
			{
			// Process random affix assignments
			foreach (var (profile, entity) in SystemAPI.Query<RefRO<EnemyProfile>>()
				.WithAll<NeedsRandomAffixAssignment>()
				.WithEntityAccess())
				{
				EnemyAffixDatabase.AssignRandomAffixes(state.EntityManager, entity, profile.ValueRO.Rarity, profile.ValueRO.GenerationSeed);
				state.EntityManager.RemoveComponent<NeedsRandomAffixAssignment>(entity);

				// Mark for name generation if not already marked
				if (!state.EntityManager.HasComponent<NeedsNameGeneration>(entity))
					{
					state.EntityManager.AddComponent<NeedsNameGeneration>(entity);
					}
				}

			// Process specific affix assignments
			foreach (var (profile, pendingAffixBuffer, entity) in SystemAPI.Query<RefRO<EnemyProfile>, DynamicBuffer<PendingAffixIdElement>>()
				.WithAll<NeedsSpecificAffixAssignment>()
				.WithEntityAccess())
				{
				AssignSpecificAffixes(ref state, entity, pendingAffixBuffer);
				state.EntityManager.RemoveComponent<NeedsSpecificAffixAssignment>(entity);
				state.EntityManager.RemoveComponent<PendingAffixIdElement>(entity);

				// Mark for name generation if not already marked
				if (!state.EntityManager.HasComponent<NeedsNameGeneration>(entity))
					{
					state.EntityManager.AddComponent<NeedsNameGeneration>(entity);
					}
				}
			}

		/// <summary>
		/// Assign specific affixes based on IDs
		/// </summary>
		private static void AssignSpecificAffixes(ref SystemState state, Entity entity, DynamicBuffer<PendingAffixIdElement> pendingIds)
			{
			if (pendingIds.Length == 0)
				{
				return;
				}

			// Get all available affixes
			var affixQuery = state.EntityManager.CreateEntityQuery(typeof(EnemyAffix), typeof(AffixDatabaseTag));
			var affixEntities = affixQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

			if (affixEntities.Length == 0)
				{
				affixEntities.Dispose();
				return;
				}

			// Create affix buffer on enemy
			var affixBuffer = state.EntityManager.AddBuffer<EnemyAffixBufferElement>(entity);

			// Find and assign requested affixes
			foreach (var pendingId in pendingIds)
				{
				foreach (var affixEntity in affixEntities)
					{
					var affix = state.EntityManager.GetComponentData<EnemyAffix>(affixEntity);
					if (affix.Id.Equals(pendingId.AffixId))
						{
						affixBuffer.Add(affix);
						break;
						}
					}
				}

			affixEntities.Dispose();
			}
		}
	}