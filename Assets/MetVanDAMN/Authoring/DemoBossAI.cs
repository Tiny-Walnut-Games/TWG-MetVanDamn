#nullable enable
#nullable enable
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TinyWalnutGames.MetVanDAMN.Authoring
	{
	/// <summary>
	/// Advanced boss AI with phase changes, telegraphed attacks, and arena mechanics.
	/// Implements complex boss behavior patterns for engaging encounters.
	/// </summary>
	public class DemoBossAI : MonoBehaviour, IDemoDamageable
		{
		private static readonly BossPhase SentinelPhase = new()
			{
			name = "(No Phase Configured)",
			healthThreshold = 1f,
			moveSpeedMultiplier = 1f,
			damageMultiplier = 1f,
			specialAttacks = System.Array.Empty<SpecialAttack>(),
			minionsToSummon = 0
			};

		[Header("Boss Stats")] public int maxHealth = 200;

		public float moveSpeed = 2f;
		public int baseDamage = 30;
		public float detectionRange = 15f;
		public string bossName = "Demo Boss";

		[Header("Phase Settings")] public BossPhase[] phases;

		public float phaseTransitionTime = 2f;

		[Header("Attack Settings")] public float basicAttackCooldown = 2f;

		public float specialAttackCooldown = 8f;
		public float telegraphTime = 1f;

		[Header("Arena Mechanics")] public GameObject[] summonPrefabs;

		public Transform[] summonPoints;
		public float arenaRadius = 10f;

		[Header("Visual Effects")] public GameObject telegraphEffect;

		public GameObject phaseTransitionEffect;
		public Color[] phaseColors;
		private DemoAIManager aiManager = null!; // Set during Initialize
		private Vector3 arenaCenter;
		private Renderer bossRenderer = null!; // Cached renderer (placeholder if absent)

		// Private state
		private int currentHealth;
		private int currentPhaseIndex = 0;
		private bool isInPhaseTransition;
		private bool isTelegraphing;
		private float lastBasicAttackTime;
		private float lastSpecialAttackTime;
		public System.Action<string> OnBossAttack;
		public System.Action OnBossDefeated;

		// Events
		public System.Action<int> OnPhaseChanged;

		// Components
		private Rigidbody2D rb2D = null!; // Deterministically assigned in Initialize or Awake fallback
		private Rigidbody rb3D = null!; // Deterministically assigned in Initialize or Awake fallback
		private List<GameObject> summonedMinions = new();
		private Transform target = null!; // Set during Initialize

		public bool IsDead => currentHealth <= 0;

		public BossPhase CurrentPhase => currentPhaseIndex < phases.Length && phases.Length > 0
			? phases[currentPhaseIndex]
			: SentinelPhase;

		// Public API
		public float HealthPercentage => (float)currentHealth / maxHealth;
		public bool IsInPhaseTransition => isInPhaseTransition;
		public bool IsTelegraphing => isTelegraphing;

		private void Awake()
			{
			// Provide deterministic placeholder renderer so color flashes don't null-ref even if Initialize not yet called.
			bossRenderer = GetComponent<Renderer>() ?? gameObject.AddComponent<MeshRenderer>();
			// If someone forgot to call Initialize before first frame, ensure minimal viability.
			if (phases == null || phases.Length == 0)
				{
				InitializeDefaultPhases();
				}

			currentHealth = Mathf.Max(1, maxHealth); // ensure >0
			}

		// Gizmos for debugging
		private void OnDrawGizmosSelected()
			{
			// Arena bounds
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(arenaCenter, arenaRadius);

			// Detection range
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(transform.position, detectionRange);

			// Summon points
			if (summonPoints != null)
				{
				Gizmos.color = Color.magenta;
				foreach (Transform point in summonPoints)
					{
					if (point) Gizmos.DrawSphere(point.position, 0.5f);
					}
				}
			}

		public void TakeDamage(float damageAmount, GameObject source, AttackType attackType)
			{
			if (isInPhaseTransition) return; // Invulnerable during phase transition

			currentHealth -= Mathf.RoundToInt(damageAmount);

			// Visual feedback
			StartCoroutine(FlashRed());

			if (currentHealth <= 0)
				{
				Die();
				}
			}

		public int GetCurrentHealth() => currentHealth;

		public void Initialize(Transform playerTarget, DemoAIManager manager)
			{
			target = playerTarget;
			aiManager = manager;
			currentHealth = maxHealth;
			arenaCenter = transform.position;

			rb2D = GetComponent<Rigidbody2D>();
			rb3D = GetComponent<Rigidbody>();
			// If a renderer exists, cache it; otherwise ensure one so later color flashes are safe
			if (!bossRenderer)
				{
				Renderer existing = GetComponent<Renderer>();
				if (existing)
					bossRenderer = existing;
				else
					{
					// MeshRenderer may be overkill but provides a material container; placeholder material created implicitly
					bossRenderer = gameObject.AddComponent<MeshRenderer>();
					}
				}

			// Add rigidbody if none exists
			if (!rb2D && !rb3D)
				{
				if (transform.position.z == 0)
					{
					rb2D = gameObject.AddComponent<Rigidbody2D>();
					rb2D.gravityScale = 0;
					}
				else
					{
					rb3D = gameObject.AddComponent<Rigidbody>();
					rb3D.useGravity = false;
					}
				}

			// Initialize with default phases if none set
			if (phases == null || phases.Length == 0)
				{
				InitializeDefaultPhases();
				}

			StartCoroutine(BossAILoop());
			}

		private void InitializeDefaultPhases()
			{
			phases = new BossPhase[]
				{
				new()
					{
					name = "Aggressive Phase",
					healthThreshold = 0.75f,
					moveSpeedMultiplier = 1f,
					damageMultiplier = 1f,
					specialAttacks = new SpecialAttack[] { SpecialAttack.BasicRush },
					minionsToSummon = 0
					},
				new()
					{
					name = "Summoner Phase",
					healthThreshold = 0.5f,
					moveSpeedMultiplier = 0.8f,
					damageMultiplier = 1.2f,
					specialAttacks = new SpecialAttack[] { SpecialAttack.SummonMinions, SpecialAttack.TelegraphedSlam },
					minionsToSummon = 2
					},
				new()
					{
					name = "Desperate Phase",
					healthThreshold = 0.25f,
					moveSpeedMultiplier = 1.3f,
					damageMultiplier = 1.5f,
					specialAttacks = new SpecialAttack[] { SpecialAttack.TelegraphedSlam, SpecialAttack.ArenaHazard },
					minionsToSummon = 1
					}
				};
			}

		private IEnumerator BossAILoop()
			{
			while (!IsDead && target)
				{
				if (!isInPhaseTransition && !isTelegraphing)
					{
					CheckPhaseTransition();
					ExecuteBossAI();
					}

				yield return new WaitForSeconds(0.1f); // 10Hz update rate
				}
			}

		private void CheckPhaseTransition()
			{
			float healthPercent = (float)currentHealth / maxHealth;

			for (int i = currentPhaseIndex + 1; i < phases.Length; i++)
				{
				if (healthPercent <= phases[i].healthThreshold)
					{
					StartCoroutine(TransitionToPhase(i));
					break;
					}
				}
			}

		private IEnumerator TransitionToPhase(int newPhaseIndex)
			{
			isInPhaseTransition = true;

			// Visual and audio cues for phase transition
			if (phaseTransitionEffect)
				{
				Instantiate(phaseTransitionEffect, transform.position, Quaternion.identity);
				}

			// Change boss color for new phase
			if (bossRenderer && phaseColors.Length > newPhaseIndex)
				{
				bossRenderer.material.color = phaseColors[newPhaseIndex];
				}

			// Brief invulnerability during transition
			yield return new WaitForSeconds(phaseTransitionTime);

			currentPhaseIndex = newPhaseIndex;
			OnPhaseChanged?.Invoke(currentPhaseIndex);

			Debug.Log($"Boss entered {CurrentPhase?.name ?? "Unknown Phase"}!");

			// Summon minions if required for new phase
			if (CurrentPhase != null && CurrentPhase.minionsToSummon > 0)
				{
				SummonMinions(CurrentPhase.minionsToSummon);
				}

			isInPhaseTransition = false;
			}

		private void ExecuteBossAI()
			{
			if (CurrentPhase == null) return;

			if (!target)
				{
				return;
				}

			float distanceToPlayer = Vector3.Distance(transform.position, target.position);

			// Basic movement AI
			if (distanceToPlayer > 3f)
				{
				MoveTowardsPlayer();
				}
			else if (distanceToPlayer < 2f)
				{
				MoveAwayFromPlayer();
				}

			// Basic attack pattern
			if (Time.time - lastBasicAttackTime > basicAttackCooldown)
				{
				PerformBasicAttack();
				}

			// Special attack pattern
			if (Time.time - lastSpecialAttackTime > specialAttackCooldown)
				{
				PerformSpecialAttack();
				}

			// Keep boss within arena bounds
			EnforceArenaBounds();
			}

		private void MoveTowardsPlayer()
			{
			if (!target) return;
			Vector3 direction = (target.position - transform.position).normalized;
			float currentSpeed = moveSpeed * (CurrentPhase?.moveSpeedMultiplier ?? 1f);

			if (rb2D)
				{
				rb2D.linearVelocity = direction * currentSpeed;
				}
			else if (rb3D)
				{
				rb3D.linearVelocity = direction * currentSpeed;
				}
			else
				{
				transform.position += direction * currentSpeed * Time.deltaTime;
				}
			}

		private void MoveAwayFromPlayer()
			{
			if (!target) return;
			Vector3 direction = (transform.position - target.position).normalized;
			float currentSpeed = moveSpeed * (CurrentPhase?.moveSpeedMultiplier ?? 1f) * 0.5f;

			if (rb2D)
				{
				rb2D.linearVelocity = direction * currentSpeed;
				}
			else if (rb3D)
				{
				rb3D.linearVelocity = direction * currentSpeed;
				}
			else
				{
				transform.position += direction * currentSpeed * Time.deltaTime;
				}
			}

		private void EnforceArenaBounds()
			{
			float distanceFromCenter = Vector3.Distance(transform.position, arenaCenter);
			if (distanceFromCenter > arenaRadius)
				{
				Vector3 directionToCenter = (arenaCenter - transform.position).normalized;
				transform.position = arenaCenter + directionToCenter * (arenaRadius - 1f);
				}
			}

		private void PerformBasicAttack()
			{
			float damage = baseDamage * CurrentPhase.damageMultiplier;

			// Simple melee attack
			float attackRange = 3f;
			if (target && Vector3.Distance(transform.position, target.position) <= attackRange)
				{
				DemoPlayerCombat playerCombat = target.GetComponent<DemoPlayerCombat>();
				if (playerCombat)
					{
					playerCombat.TakeDamage(damage);
					OnBossAttack?.Invoke("Basic Attack");
					}

				CreateAttackEffect(transform.position, attackRange, Color.red);
				}

			lastBasicAttackTime = Time.time;
			}

		private void PerformSpecialAttack()
			{
			if (CurrentPhase.specialAttacks == null || CurrentPhase.specialAttacks.Length == 0)
				{
				return;
				}

			// Choose random special attack from current phase
			SpecialAttack attack = CurrentPhase.specialAttacks[Random.Range(0, CurrentPhase.specialAttacks.Length)];

			StartCoroutine(ExecuteSpecialAttack(attack));
			lastSpecialAttackTime = Time.time;
			}

		private IEnumerator ExecuteSpecialAttack(SpecialAttack attack)
			{
			switch (attack)
				{
					case SpecialAttack.BasicRush:
						yield return ExecuteBasicRush();
						break;
					case SpecialAttack.TelegraphedSlam:
						yield return ExecuteTelegraphedSlam();
						break;
					case SpecialAttack.SummonMinions:
						yield return ExecuteSummonMinions();
						break;
					case SpecialAttack.ArenaHazard:
						yield return ExecuteArenaHazard();
						break;
				}
			}

		private IEnumerator ExecuteBasicRush()
			{
			OnBossAttack?.Invoke("Rush Attack");

			// Rush towards player at high speed
			if (!target) yield break;
			Vector3 rushDirection = (target.position - transform.position).normalized;
			float rushSpeed = moveSpeed * 3f;
			float rushDuration = 1f;

			float elapsed = 0f;
			while (elapsed < rushDuration)
				{
				if (rb2D)
					{
					rb2D.linearVelocity = rushDirection * rushSpeed;
					}
				else if (rb3D)
					{
					rb3D.linearVelocity = rushDirection * rushSpeed;
					}

				elapsed += Time.deltaTime;
				yield return null;
				}

			// Stop rush
			if (rb2D) rb2D.linearVelocity = Vector2.zero;
			if (rb3D) rb3D.linearVelocity = Vector3.zero;
			}

		private IEnumerator ExecuteTelegraphedSlam()
			{
			OnBossAttack?.Invoke("Slam Attack");

			// Telegraph the attack
			if (!target) yield break;
			Vector3 slamPosition = target.position;
			GameObject? telegraph = null; // local nullable - disposed if created

			if (telegraphEffect)
				{
				telegraph = Instantiate(telegraphEffect, slamPosition, Quaternion.identity);
				}

			isTelegraphing = true;
			yield return new WaitForSeconds(telegraphTime);
			isTelegraphing = false;

			// Destroy telegraph effect
			if (telegraph)
				{
				Destroy(telegraph);
				}

			// Execute slam attack
			float slamRadius = 4f;
			float damage = baseDamage * 2f * CurrentPhase.damageMultiplier;

			if (target && Vector3.Distance(target.position, slamPosition) <= slamRadius)
				{
				DemoPlayerCombat playerCombat = target.GetComponent<DemoPlayerCombat>();
				if (playerCombat)
					{
					playerCombat.TakeDamage(damage);
					}
				}

			CreateAttackEffect(slamPosition, slamRadius, Color.orange);
			}

		private IEnumerator ExecuteSummonMinions()
			{
			OnBossAttack?.Invoke("Summon Minions");

			if (summonPrefabs.Length == 0) yield break;

			int minionsToSummon = CurrentPhase?.minionsToSummon ?? 1;

			for (int i = 0; i < minionsToSummon; i++)
				{
				Vector3 summonPos = GetRandomSummonPosition();
				GameObject prefab = summonPrefabs[Random.Range(0, summonPrefabs.Length)];

				GameObject minion = Instantiate(prefab, summonPos, Quaternion.identity);
				DemoEnemyAI ai = minion.GetComponent<DemoEnemyAI>();
				if (!ai)
					{
					ai = minion.AddComponent<DemoEnemyAI>();
					}

				ai.Initialize(target, aiManager);
				summonedMinions.Add(minion);

				CreateAttackEffect(summonPos, 2f, Color.purple);
				yield return new WaitForSeconds(0.5f);
				}
			}

		private IEnumerator ExecuteArenaHazard()
			{
			OnBossAttack?.Invoke("Arena Hazard");

			// Create hazardous areas around the arena
			int hazardCount = 3;
			List<Vector3> hazardPositions = new List<Vector3>();

			// Place hazards
			for (int i = 0; i < hazardCount; i++)
				{
				Vector3 hazardPos = arenaCenter + Random.insideUnitSphere * arenaRadius;
				hazardPos.y = arenaCenter.y; // Keep hazards at ground level
				hazardPositions.Add(hazardPos);

				// Telegraph hazard
				if (telegraphEffect)
					{
					GameObject telegraph = Instantiate(telegraphEffect, hazardPos, Quaternion.identity);
					Destroy(telegraph, telegraphTime);
					}
				}

			yield return new WaitForSeconds(telegraphTime);

			// Activate hazards
			foreach (Vector3 pos in hazardPositions)
				{
				float hazardRadius = 3f;
				float damage = baseDamage * 1.5f * CurrentPhase.damageMultiplier;

				if (target && Vector3.Distance(target.position, pos) <= hazardRadius)
					{
					DemoPlayerCombat playerCombat = target.GetComponent<DemoPlayerCombat>();
					if (playerCombat)
						{
						playerCombat.TakeDamage(damage);
						}
					}

				CreateAttackEffect(pos, hazardRadius, Color.magenta);
				}
			}

		private Vector3 GetRandomSummonPosition()
			{
			if (summonPoints.Length > 0)
				{
				return summonPoints[Random.Range(0, summonPoints.Length)].position;
				}
			else
				{
				Vector3 randomOffset = Random.insideUnitSphere * arenaRadius * 0.8f;
				randomOffset.y = 0; // Keep on ground level
				return arenaCenter + randomOffset;
				}
			}

		private void SummonMinions(int count)
			{
			StartCoroutine(ExecuteSummonMinions());
			}

		private void CreateAttackEffect(Vector3 position, float radius, Color color)
			{
			GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			effect.transform.position = position;
			effect.transform.localScale = Vector3.one * radius * 2f;

			Renderer renderer = effect.GetComponent<Renderer>();
			renderer.material.color = new Color(color.r, color.g, color.b, 0.5f);

			DestroyImmediate(effect.GetComponent<Collider>());
			Destroy(effect, 0.5f);
			}

		private IEnumerator FlashRed()
			{
			if (bossRenderer)
				{
				Color originalColor = bossRenderer.material.color;
				bossRenderer.material.color = Color.red;
				yield return new WaitForSeconds(0.2f);
				bossRenderer.material.color = originalColor;
				}
			}

		private void Die()
			{
			OnBossDefeated?.Invoke();

			// Clear all summoned minions
			foreach (GameObject minion in summonedMinions)
				{
				if (minion) Destroy(minion);
				}

			// Drop special boss loot
			DemoLootManager lootManager = FindFirstObjectByType<DemoLootManager>();
			if (lootManager)
				{
				lootManager.SpawnBossLoot(transform.position);
				}

			// Notify AI manager
			if (aiManager)
				{
				aiManager.RegisterBossDeath(this);
				}

			Debug.Log($"{bossName} has been defeated!");
			Destroy(gameObject);
			}

		public string GetCurrentPhaseName() => CurrentPhase?.name ?? "Unknown Phase";
		}

	[System.Serializable]
	public class BossPhase
		{
		public string name = string.Empty;
		public float healthThreshold; // 0.0 to 1.0
		public float moveSpeedMultiplier = 1f;
		public float damageMultiplier = 1f;
		public SpecialAttack[] specialAttacks = System.Array.Empty<SpecialAttack>();
		public int minionsToSummon = 0;
		}

	public enum SpecialAttack
		{
		BasicRush,
		TelegraphedSlam,
		SummonMinions,
		ArenaHazard
		}
	}
