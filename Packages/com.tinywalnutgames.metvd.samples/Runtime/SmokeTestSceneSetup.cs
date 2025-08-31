using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Samples
	{
	/// <summary>
	/// Smoke test scene setup for MetVanDAMN engine
	/// Provides immediate "hit Play -> see map" experience for validation
	/// </summary>
	public class SmokeTestSceneSetup : MonoBehaviour
		{
		[Header("World Generation Parameters")]
		[SerializeField] private readonly uint worldSeed = 42;
		[SerializeField] private int2 worldSize = new(50, 50);
		[SerializeField] private readonly int targetSectorCount = 5;
		[SerializeField] private readonly float biomeTransitionRadius = 10.0f;

		[Header("Debug Visualization")]
		[SerializeField] private readonly bool enableDebugVisualization = true;
		[SerializeField] private readonly bool logGenerationSteps = true;

		private EntityManager entityManager;
		private World defaultWorld;

		private void Start ()
			{
			this.SetupSmokeTestWorld();
			}

		private void Update ()
			{
			// Periodically re-draw bounds if debug visualization enabled (consumes field each frame)
			if (this.enableDebugVisualization && Time.frameCount % 120 == 0)
				{
				this.DebugDrawBounds();
				}
			}

#if UNITY_EDITOR
		private void OnValidate ()
			{
			// Respond immediately in editor when toggled so value is meaningfully used
			if (Application.isPlaying == false && this.enableDebugVisualization)
				{
				this.DebugDrawBounds();
				}
			}
#endif

		private void SetupSmokeTestWorld ()
			{
			this.defaultWorld = World.DefaultGameObjectInjectionWorld;
			this.entityManager = this.defaultWorld.EntityManager;

			if (this.logGenerationSteps)
				{
				Debug.Log("🚀 MetVanDAMN Smoke Test: Starting world generation...");
				}

			this.CreateWorldConfiguration();
			this.CreateDistrictEntities();
			this.CreateBiomeFieldEntities();

			if (this.enableDebugVisualization)
				{
				this.DebugDrawBounds();
				}

			if (this.logGenerationSteps)
				{
				Debug.Log($"✅ MetVanDAMN Smoke Test: World setup complete with seed {this.worldSeed}");
				Debug.Log($"   World size: {this.worldSize.x}x{this.worldSize.y}");
				Debug.Log($"   Target sectors: {this.targetSectorCount}");
				Debug.Log("   Systems will begin generation on next frame.");
				}
			}

		private void CreateWorldConfiguration ()
			{
			Entity configEntity = this.entityManager.CreateEntity();
			this.entityManager.SetName(configEntity, "WorldConfiguration");

			this.entityManager.AddComponentData(configEntity, new WorldSeed { Value = this.worldSeed });
			this.entityManager.AddComponentData(configEntity, new WorldBounds
				{
				Min = new int2(-this.worldSize.x / 2, -this.worldSize.y / 2),
				Max = new int2(this.worldSize.x / 2, this.worldSize.y / 2)
				});

			// Integrate targetSectorCount with generation pipeline
			this.entityManager.AddComponentData(configEntity, new WorldGenerationConfig
				{
				TargetSectorCount = this.targetSectorCount,
				MaxDistrictCount = this.targetSectorCount * 4, // Allow room for subdivision
				BiomeTransitionRadius = this.biomeTransitionRadius
				});
			}

		private void CreateDistrictEntities ()
			{
			// Use targetSectorCount to determine how many districts to create
			int actualDistrictCount = math.min(this.targetSectorCount, 24); // Reasonable upper limit
			int gridSize = (int)math.ceil(math.sqrt(actualDistrictCount));

			Entity hubEntity = this.entityManager.CreateEntity();
			this.entityManager.SetName(hubEntity, "HubDistrict");

			this.entityManager.AddComponentData(hubEntity, new NodeId
				{
				Coordinates = int2.zero,
				Level = 0,
				_value = 0,
				ParentId = 0
				});

			this.entityManager.AddComponentData(hubEntity, new WfcState());
			this.entityManager.AddBuffer<WfcCandidateBufferElement>(hubEntity);
			this.entityManager.AddBuffer<ConnectionBufferElement>(hubEntity);

			int districtId = 1;
			int districtsCreated = 0;
			int halfGrid = gridSize / 2;

			for (int x = -halfGrid; x <= halfGrid && districtsCreated < actualDistrictCount; x++)
				{
				for (int y = -halfGrid; y <= halfGrid && districtsCreated < actualDistrictCount; y++)
					{
					if (x == 0 && y == 0)
						{
						continue; // Skip hub position
						}

					Entity districtEntity = this.entityManager.CreateEntity();
					this.entityManager.SetName(districtEntity, $"District_{x}_{y}");

					this.entityManager.AddComponentData(districtEntity, new NodeId
						{
						Coordinates = new int2(x * 10, y * 10),
						Level = (byte)(math.abs(x) + math.abs(y)),
						_value = (uint)districtId++,
						ParentId = 0
						});

					this.entityManager.AddComponentData(districtEntity, new WfcState());
					this.entityManager.AddBuffer<WfcCandidateBufferElement>(districtEntity);
					this.entityManager.AddBuffer<ConnectionBufferElement>(districtEntity);
					this.entityManager.AddComponentData(districtEntity, new SectorRefinementData(0.3f));
					this.entityManager.AddBuffer<GateConditionBufferElement>(districtEntity);

					districtsCreated++;
					}
				}

			if (this.logGenerationSteps)
				{
				Debug.Log($"Created {districtsCreated} districts based on targetSectorCount ({this.targetSectorCount})");
				}
			}

		private void CreateBiomeFieldEntities ()
			{
			this.CreatePolarityField(Polarity.Sun, new float2(15, 15), "SunField");
			this.CreatePolarityField(Polarity.Moon, new float2(-15, -15), "MoonField");
			this.CreatePolarityField(Polarity.Heat, new float2(15, -15), "HeatField");
			this.CreatePolarityField(Polarity.Cold, new float2(-15, 15), "ColdField");
			}

		private void CreatePolarityField (Polarity polarity, float2 center, string name)
			{
			Entity fieldEntity = this.entityManager.CreateEntity();
			this.entityManager.SetName(fieldEntity, name);

			this.entityManager.AddComponentData(fieldEntity, new PolarityFieldData
				{
				Polarity = polarity,
				Center = center,
				Radius = this.biomeTransitionRadius,
				Strength = 0.8f
				});
			}

		private void DebugDrawBounds ()
			{
			var color = new Color(0.2f, 0.9f, 0.4f, 0.6f);
			var half = new float3(this.worldSize.x * 0.5f, 0, this.worldSize.y * 0.5f);
			Debug.DrawLine(new Vector3(-half.x, 0, -half.z), new Vector3(half.x, 0, -half.z), color, 1f);
			Debug.DrawLine(new Vector3(half.x, 0, -half.z), new Vector3(half.x, 0, half.z), color, 1f);
			Debug.DrawLine(new Vector3(half.x, 0, half.z), new Vector3(-half.x, 0, half.z), color, 1f);
			Debug.DrawLine(new Vector3(-half.x, 0, half.z), new Vector3(-half.x, 0, -half.z), color, 1f);
			}

		private void OnDestroy ()
			{
			if (this.logGenerationSteps)
				{
				Debug.Log("🔚 MetVanDAMN Smoke Test: Scene cleanup complete");
				}
			}
		}

	public struct WorldSeed : IComponentData
		{
		public uint Value;
		}

	public struct WorldBounds : IComponentData
		{
		public int2 Min;
		public int2 Max;
		}

	public struct PolarityFieldData : IComponentData
		{
		public Polarity Polarity;
		public float2 Center;
		public float Radius;
		public float Strength;
		}

	/// <summary>
	/// World generation configuration that integrates targetSectorCount with the generation pipeline
	/// </summary>
	public struct WorldGenerationConfig : IComponentData
		{
		public int TargetSectorCount;
		public int MaxDistrictCount;
		public float BiomeTransitionRadius;
		public uint WorldSeed;
		}
	}
