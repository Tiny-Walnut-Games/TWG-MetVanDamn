using Unity.Collections;
using Unity.Entities;

namespace TinyWalnutGames.MetVD.Core
	{
	/// <summary>
	/// Enemy rarity types that affect naming and affix display behavior
	/// </summary>
	public enum RarityType : byte
		{
		Common = 0,
		Uncommon = 1,
		Rare = 2,
		Unique = 3,
		MiniBoss = 4,
		Boss = 5,
		FinalBoss = 6
		}

	/// <summary>
	/// Categories for organizing affixes and traits
	/// </summary>
	public enum TraitCategory : byte
		{
		Combat = 0,
		Movement = 1,
		Behavior = 2,
		Boss = 3,
		Unique = 4
		}

	/// <summary>
	/// Display mode configuration for affix and name display
	/// </summary>
	public enum AffixDisplayMode : byte
		{
		NamesOnly = 0,
		IconsOnly = 1,
		NamesAndIcons = 2
		}

	/// <summary>
	/// Component representing a single enemy affix with all required data
	/// </summary>
	public struct EnemyAffix : IComponentData
		{
		/// <summary>
		/// Unique identifier for this affix type
		/// </summary>
		public FixedString64Bytes Id;

		/// <summary>
		/// Human-readable display name for full names (rares/bosses)
		/// </summary>
		public FixedString64Bytes DisplayName;

		/// <summary>
		/// UI icon asset reference for display
		/// </summary>
		public FixedString64Bytes IconRef;

		/// <summary>
		/// Syllables for procedural boss name generation
		/// </summary>
		public FixedList512Bytes<FixedString32Bytes> BossSyllables;

		/// <summary>
		/// Category for rarity rules and weighting
		/// </summary>
		public TraitCategory Category;

		/// <summary>
		/// Relative chance of rolling this affix
		/// </summary>
		public byte Weight;

		/// <summary>
		/// Used for combo weighting/reroll logic
		/// </summary>
		public byte ToxicityScore;

		/// <summary>
		/// Tooltip/lore text description
		/// </summary>
		public FixedString128Bytes Description;

		public EnemyAffix(FixedString64Bytes id, FixedString64Bytes displayName, FixedString64Bytes iconRef,
						 TraitCategory category, byte weight = 5, byte toxicityScore = 1,
						 FixedString128Bytes description = default)
			{
			Id = id;
			DisplayName = displayName;
			IconRef = iconRef;
			BossSyllables = new FixedList512Bytes<FixedString32Bytes>();
			Category = category;
			Weight = weight;
			ToxicityScore = toxicityScore;
			Description = description;
			}

		/// <summary>
		/// Add a boss syllable to this affix
		/// </summary>
		public void AddBossSyllable(FixedString32Bytes syllable)
			{
			if (BossSyllables.Length < BossSyllables.Capacity)
				{
				BossSyllables.Add(syllable);
				}
			}

		/// <summary>
		/// Get a random boss syllable from this affix
		/// </summary>
		public readonly FixedString32Bytes GetRandomBossSyllable(uint seed)
			{
			if (BossSyllables.Length == 0)
				{
				return new FixedString32Bytes();
				}

			var random = Unity.Mathematics.Random.CreateFromIndex(seed);
			int index = random.NextInt(0, BossSyllables.Length);
			return BossSyllables[index];
			}
		}

	/// <summary>
	/// Component representing an enemy's complete profile including rarity and affixes
	/// </summary>
	public struct EnemyProfile : IComponentData
		{
		/// <summary>
		/// Rarity level determining naming and display behavior
		/// </summary>
		public RarityType Rarity;

		/// <summary>
		/// Base enemy type name (e.g., "Crawler", "Archer")
		/// </summary>
		public FixedString64Bytes BaseType;

		/// <summary>
		/// Seed for consistent random generation
		/// </summary>
		public uint GenerationSeed;

		public EnemyProfile(RarityType rarity, FixedString64Bytes baseType, uint generationSeed)
			{
			Rarity = rarity;
			BaseType = baseType;
			GenerationSeed = generationSeed;
			}
		}

	/// <summary>
	/// Component storing the final generated name and display configuration
	/// </summary>
	public struct EnemyNaming : IComponentData
		{
		/// <summary>
		/// Final generated display name
		/// </summary>
		public FixedString128Bytes DisplayName;

		/// <summary>
		/// Whether to show the full name (based on rarity)
		/// </summary>
		public bool ShowFullName;

		/// <summary>
		/// Whether icons should be displayed
		/// </summary>
		public bool ShowIcons;

		/// <summary>
		/// Global display mode override
		/// </summary>
		public AffixDisplayMode DisplayMode;

		public EnemyNaming(FixedString128Bytes displayName, bool showFullName, bool showIcons,
						  AffixDisplayMode displayMode = AffixDisplayMode.NamesAndIcons)
			{
			DisplayName = displayName;
			ShowFullName = showFullName;
			ShowIcons = showIcons;
			DisplayMode = displayMode;
			}
		}

	/// <summary>
	/// Buffer element for storing multiple affixes on an enemy
	/// </summary>
	public struct EnemyAffixBufferElement : IBufferElementData
		{
		public EnemyAffix Value;

		public static implicit operator EnemyAffix(EnemyAffixBufferElement e) => e.Value;
		public static implicit operator EnemyAffixBufferElement(EnemyAffix e) => new() { Value = e };
		}

	/// <summary>
	/// Global configuration component for enemy naming system
	/// </summary>
	public struct EnemyNamingConfig : IComponentData
		{
		/// <summary>
		/// Global display mode setting
		/// </summary>
		public AffixDisplayMode GlobalDisplayMode;

		/// <summary>
		/// Maximum number of affixes to display simultaneously
		/// </summary>
		public byte MaxDisplayedAffixes;

		/// <summary>
		/// Whether boss names should use procedural syllables
		/// </summary>
		public bool UseProceduralBossNames;

		/// <summary>
		/// Seed for consistent naming across sessions
		/// </summary>
		public uint NamingSeed;

		public EnemyNamingConfig(AffixDisplayMode globalDisplayMode = AffixDisplayMode.NamesAndIcons,
							   byte maxDisplayedAffixes = 4, bool useProceduralBossNames = true,
							   uint namingSeed = 12345)
			{
			GlobalDisplayMode = globalDisplayMode;
			MaxDisplayedAffixes = maxDisplayedAffixes;
			UseProceduralBossNames = useProceduralBossNames;
			NamingSeed = namingSeed;
			}
		}

	/// <summary>
	/// Tag component to identify entities that need name generation
	/// </summary>
	public struct NeedsNameGeneration : IComponentData { }

	/// <summary>
	/// Tag component to identify affix database entities
	/// </summary>
	public struct AffixDatabaseTag : IComponentData { }


	}
