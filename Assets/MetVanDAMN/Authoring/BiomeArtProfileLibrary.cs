#nullable enable
using System;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

#nullable enable

namespace TinyWalnutGames.MetVD.Authoring
	{
	/// <summary>
	/// Central library of available BiomeArtProfiles used at runtime for auto-assignment.
	/// Designers populate this once; systems will pick a profile per biome entity.
	/// </summary>
	[CreateAssetMenu(fileName = "BiomeArtProfileLibrary", menuName = "MetVanDAMN/Biome Art Profile Library")]
	public class BiomeArtProfileLibrary : ScriptableObject
		{
		[Tooltip("Profiles available for auto-assignment; selection is deterministic by biome type/seed.")]
		public BiomeArtProfile[] profiles = Array.Empty<BiomeArtProfile>(); // global fallback pool

		[Tooltip("Optional per-type profile buckets; preferred over the global pool when present.")]
		public BiomeTypeBucket[] perTypeBuckets = Array.Empty<BiomeTypeBucket>();

		[Tooltip(
			"Optional per-type + elevation buckets with multi-select elevation mask; preferred over type-only buckets.")]
		public BiomeTypeElevationBucket[] perTypeElevationBuckets = Array.Empty<BiomeTypeElevationBucket>();

		public BiomeArtProfile[] GetProfiles(BiomeType type, BiomeElevation elevation)
			{
			// 1) Prefer Type+Elevation mask buckets
			if (perTypeElevationBuckets != null)
				{
				for (int i = 0; i < perTypeElevationBuckets.Length; i++)
					{
					BiomeTypeElevationBucket b = perTypeElevationBuckets[i];
					if (b == null || b.profiles == null || b.profiles.Length == 0) continue;
					if (b.type != type) continue;
					if ((b.elevations & elevation) == 0 && elevation != BiomeElevation.None) continue;
					return b.profiles;
					}
				}

			// 2) Fallback to Type-only buckets
			if (perTypeBuckets != null)
				{
				for (int i = 0; i < perTypeBuckets.Length; i++)
					{
					BiomeTypeBucket b = perTypeBuckets[i];
					if (b == null || b.profiles == null || b.profiles.Length == 0) continue;
					if (b.type != type) continue;
					return b.profiles;
					}
				}

			// 3) Global fallback
			return profiles;
			}

		[Serializable]
		public class BiomeTypeBucket
			{
			public BiomeType type;

			[Tooltip("Profiles available for this biome type.")]
			public BiomeArtProfile[] profiles = Array.Empty<BiomeArtProfile>();
			}

		[Serializable]
		public class BiomeTypeElevationBucket
			{
			public BiomeType type;

			[Tooltip("Allowed elevation layers for this bucket (multi-select mask).")]
			public BiomeElevation elevations = BiomeElevation.Any;

			[Tooltip("Profiles available for this biome type at the specified elevations.")]
			public BiomeArtProfile[] profiles = Array.Empty<BiomeArtProfile>();
			}
		}
	}
