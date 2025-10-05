#nullable enable
using TinyWalnutGames.MetVD.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.MetVanDAMN.Authoring
	{
	/// <summary>
	/// Authoring component for a sudo action hint. At runtime, the baker-provided
	/// SudoActionHint will be converted by SudoActionDispatcherSystem into a
	/// SudoActionRequest exactly once when OneOff is true.
	/// Consumers should read SudoActionRequest by ActionKey and then destroy the
	/// request entity after handling to avoid re-processing.
	/// </summary>
	public class SudoActionHintAuthoring : MonoBehaviour
		{
		[Header("Sudo Action")] public string actionKey = "SpecialEvent";

		public bool oneOff = true;
		public float radius = 10f;
		public uint seed = 0;

		[Header("Constraints (optional)")] public BiomeElevation elevationMask = BiomeElevation.Any;

		public bool constrainBiomeType = false;
		public BiomeType biomeType = BiomeType.Unknown;

		/// <summary>
		/// Bakes the authoring data into a SudoActionHint component.
		/// </summary>
		class Baker : Baker<SudoActionHintAuthoring>
			{
			public override void Bake(SudoActionHintAuthoring authoring)
				{
				Entity entity = GetEntity(TransformUsageFlags.None);
				var key = new Unity.Collections.FixedString64Bytes(authoring.actionKey ?? "");
				Vector3 center = authoring.transform.position;
				AddComponent(entity, new SudoActionHint
					{
					ActionKey = key,
					OneOff = authoring.oneOff,
					Center = (float3)center,
					Radius = Mathf.Max(0f, authoring.radius),
					ElevationMask = authoring.elevationMask,
					TypeConstraint = authoring.biomeType,
					HasTypeConstraint = (byte)(authoring.constrainBiomeType && authoring.biomeType != BiomeType.Unknown
						? 1
						: 0),
					Seed = authoring.seed,
					});
				}
			}
		}
	}
