using UnityEngine;
using System.Collections.Generic;

namespace TinyWalnutGames.MetVanDAMN.Authoring
{
    /// <summary>
    /// Complete player combat system with weapons, attacks, and skills.
    /// Supports melee, ranged, and AoE weapons with light/heavy/charged/combo attacks.
    /// </summary>
    public class DemoPlayerCombat : MonoBehaviour
    {
        [Header("Combat Settings")]
        public int maxHealth = 100;
        public float attackCooldown = 0.5f;
        public float heavyAttackMultiplier = 2f;
        public float chargeTime = 1f;
        public float comboWindow = 1f;
        
        [Header("Input")]
        public KeyCode lightAttackKey = KeyCode.Mouse0;
        public KeyCode heavyAttackKey = KeyCode.Mouse1;
        public KeyCode specialSkillKey = KeyCode.Q;
        public KeyCode weaponSwapKey = KeyCode.Tab;

        [Header("Audio")]
        public AudioClip attackSound;
        public AudioClip hitSound;
        public AudioClip weaponSwapSound;

        // Combat state
        private int currentHealth;
        private List<DemoWeapon> availableWeapons = new List<DemoWeapon>();
        private int currentWeaponIndex = 0;
        private float lastAttackTime;
        private int comboCount = 0;
        private float lastComboTime;
        private bool isCharging;
        private float chargeStartTime;
        private bool canUseSpecialSkill = true;
        private float specialSkillCooldown = 5f;
        private float lastSpecialSkillTime;

        // Components
        private DemoPlayerMovement playerMovement;
        private AudioSource audioSource;

        // Events
        public System.Action<int, int> OnHealthChanged; // current, max
        public System.Action<DemoWeapon> OnWeaponChanged;
        public System.Action<int> OnComboChanged;

        private void Awake()
        {
            currentHealth = maxHealth;
            playerMovement = GetComponent<DemoPlayerMovement>();
            audioSource = GetComponent<AudioSource>();
            
            if (!audioSource)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Initialize with default weapons
            InitializeDefaultWeapons();
        }

        private void Update()
        {
            HandleCombatInput();
            UpdateCombatState();
        }

        private void HandleCombatInput()
        {
            // Light attack
            if (Input.GetKeyDown(lightAttackKey))
            {
                if (isCharging)
                {
                    ReleaseChargedAttack();
                }
                else
                {
                    PerformLightAttack();
                }
            }

            // Heavy attack (can be charged)
            if (Input.GetKeyDown(heavyAttackKey))
            {
                StartChargingAttack();
            }
            
            if (Input.GetKeyUp(heavyAttackKey))
            {
                if (isCharging)
                {
                    ReleaseChargedAttack();
                }
                else
                {
                    PerformHeavyAttack();
                }
            }

            // Special skill
            if (Input.GetKeyDown(specialSkillKey))
            {
                PerformSpecialSkill();
            }

            // Weapon swap
            if (Input.GetKeyDown(weaponSwapKey))
            {
                SwapWeapon();
            }
        }

        private void UpdateCombatState()
        {
            // Update combo timer
            if (Time.time - lastComboTime > comboWindow)
            {
                ResetCombo();
            }

            // Update special skill cooldown
            if (!canUseSpecialSkill && Time.time - lastSpecialSkillTime > specialSkillCooldown)
            {
                canUseSpecialSkill = true;
            }

            // Update health regeneration
            UpdateHealthRegeneration();
        }

        private void InitializeDefaultWeapons()
        {
            // Create default weapons for demo
            var meleeWeapon = new DemoWeapon
            {
                name = "Demo Blade",
                type = WeaponType.Melee,
                damage = 25,
                range = 2f,
                attackSpeed = 1f,
                projectilePrefab = null
            };

            var rangedWeapon = new DemoWeapon
            {
                name = "Demo Bow",
                type = WeaponType.Ranged,
                damage = 20,
                range = 10f,
                attackSpeed = 0.8f,
                projectilePrefab = null // Will be set by weapon spawner
            };

            var aoeWeapon = new DemoWeapon
            {
                name = "Demo Staff",
                type = WeaponType.AoE,
                damage = 30,
                range = 5f,
                attackSpeed = 0.6f,
                projectilePrefab = null
            };

            availableWeapons.Add(meleeWeapon);
            availableWeapons.Add(rangedWeapon);
            availableWeapons.Add(aoeWeapon);

            OnWeaponChanged?.Invoke(GetCurrentWeapon());
        }

        private void PerformLightAttack()
        {
            if (!CanAttack()) return;

            var weapon = GetCurrentWeapon();
            if (weapon == null) return;

            float damage = weapon.damage;
            
            // Combo bonus
            if (comboCount > 0)
            {
                damage *= (1f + comboCount * 0.2f); // 20% bonus per combo
            }

            ExecuteAttack(weapon, damage, AttackType.Light);
            
            // Update combo
            if (Time.time - lastComboTime < comboWindow)
            {
                comboCount++;
            }
            else
            {
                comboCount = 1;
            }
            
            lastComboTime = Time.time;
            lastAttackTime = Time.time;
            
            OnComboChanged?.Invoke(comboCount);
        }

        private void PerformHeavyAttack()
        {
            if (!CanAttack()) return;

            var weapon = GetCurrentWeapon();
            if (weapon == null) return;

            float damage = weapon.damage * heavyAttackMultiplier;
            ExecuteAttack(weapon, damage, AttackType.Heavy);
            
            ResetCombo();
            lastAttackTime = Time.time;
        }

        private void StartChargingAttack()
        {
            if (!CanAttack()) return;

            isCharging = true;
            chargeStartTime = Time.time;
        }

        private void ReleaseChargedAttack()
        {
            if (!isCharging) return;

            isCharging = false;
            float chargeLevel = Mathf.Clamp01((Time.time - chargeStartTime) / chargeTime);
            
            var weapon = GetCurrentWeapon();
            if (weapon == null) return;

            float damage = weapon.damage * heavyAttackMultiplier * (1f + chargeLevel);
            ExecuteAttack(weapon, damage, AttackType.Charged);
            
            ResetCombo();
            lastAttackTime = Time.time;
        }

        private void PerformSpecialSkill()
        {
            if (!canUseSpecialSkill) return;

            var weapon = GetCurrentWeapon();
            if (weapon == null) return;

            // Special skill varies by weapon type
            switch (weapon.type)
            {
                case WeaponType.Melee:
                    ExecuteSpinAttack(weapon);
                    break;
                case WeaponType.Ranged:
                    ExecuteMultiShot(weapon);
                    break;
                case WeaponType.AoE:
                    ExecuteExplosiveBlast(weapon);
                    break;
            }

            canUseSpecialSkill = false;
            lastSpecialSkillTime = Time.time;
        }

        private void ExecuteAttack(DemoWeapon weapon, float damage, AttackType attackType)
        {
            switch (weapon.type)
            {
                case WeaponType.Melee:
                    ExecuteMeleeAttack(weapon, damage, attackType);
                    break;
                case WeaponType.Ranged:
                    ExecuteRangedAttack(weapon, damage, attackType);
                    break;
                case WeaponType.AoE:
                    ExecuteAoEAttack(weapon, damage, attackType);
                    break;
            }

            PlayAttackSound();
        }

        private void ExecuteMeleeAttack(DemoWeapon weapon, float damage, AttackType attackType)
        {
            // Create attack hitbox in front of player
            Vector3 attackPosition = transform.position + transform.right * transform.localScale.x * 1.5f;
            float attackRadius = weapon.range;

            // Find all enemies in range
            Collider2D[] hits2D = Physics2D.OverlapCircleAll(attackPosition, attackRadius);
            Collider[] hits3D = Physics.OverlapSphere(attackPosition, attackRadius);

            // Damage enemies
            foreach (var hit in hits2D)
            {
                DamageTarget(hit.gameObject, damage, attackType);
            }
            
            foreach (var hit in hits3D)
            {
                DamageTarget(hit.gameObject, damage, attackType);
            }

            // Visual effect (placeholder)
            CreateAttackEffect(attackPosition, attackRadius, Color.red);
        }

        private void ExecuteRangedAttack(DemoWeapon weapon, float damage, AttackType attackType)
        {
            if (weapon.projectilePrefab == null)
            {
                // Fallback: instant raycast attack
                Vector3 attackDirection = transform.right * transform.localScale.x;
                RaycastHit2D hit2D = Physics2D.Raycast(transform.position, attackDirection, weapon.range);
                RaycastHit hit3D;
                
                if (hit2D.collider)
                {
                    DamageTarget(hit2D.collider.gameObject, damage, attackType);
                }
                else if (Physics.Raycast(transform.position, attackDirection, out hit3D, weapon.range))
                {
                    DamageTarget(hit3D.collider.gameObject, damage, attackType);
                }
            }
            else
            {
                // Spawn projectile
                Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
                Vector3 direction = transform.right * transform.localScale.x;
                
                GameObject projectile = Instantiate(weapon.projectilePrefab, spawnPos, Quaternion.identity);
                var projectileScript = projectile.GetComponent<DemoProjectile>();
                if (projectileScript)
                {
                    projectileScript.Initialize(damage, direction, weapon.range, this);
                }
            }
        }

        private void ExecuteAoEAttack(DemoWeapon weapon, float damage, AttackType attackType)
        {
            // Create area of effect around player
            Vector3 aoeCenter = transform.position;
            float aoeRadius = weapon.range;

            // Find all enemies in area
            Collider2D[] hits2D = Physics2D.OverlapCircleAll(aoeCenter, aoeRadius);
            Collider[] hits3D = Physics.OverlapSphere(aoeCenter, aoeRadius);

            foreach (var hit in hits2D)
            {
                DamageTarget(hit.gameObject, damage, attackType);
            }
            
            foreach (var hit in hits3D)
            {
                DamageTarget(hit.gameObject, damage, attackType);
            }

            // Visual effect
            CreateAttackEffect(aoeCenter, aoeRadius, Color.blue);
        }

        private void ExecuteSpinAttack(DemoWeapon weapon)
        {
            // 360-degree melee attack
            float damage = weapon.damage * 1.5f;
            Collider2D[] hits2D = Physics2D.OverlapCircleAll(transform.position, weapon.range * 1.5f);
            Collider[] hits3D = Physics.OverlapSphere(transform.position, weapon.range * 1.5f);

            foreach (var hit in hits2D)
            {
                DamageTarget(hit.gameObject, damage, AttackType.Special);
            }
            
            foreach (var hit in hits3D)
            {
                DamageTarget(hit.gameObject, damage, AttackType.Special);
            }

            CreateAttackEffect(transform.position, weapon.range * 1.5f, Color.yellow);
        }

        private void ExecuteMultiShot(DemoWeapon weapon)
        {
            // Fire multiple projectiles in a spread
            int projectileCount = 3;
            float spreadAngle = 30f;
            
            for (int i = 0; i < projectileCount; i++)
            {
                float angle = (i - 1) * (spreadAngle / (projectileCount - 1));
                Vector3 direction = Quaternion.Euler(0, 0, angle) * (transform.right * transform.localScale.x);
                
                if (weapon.projectilePrefab)
                {
                    Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
                    GameObject projectile = Instantiate(weapon.projectilePrefab, spawnPos, Quaternion.identity);
                    var projectileScript = projectile.GetComponent<DemoProjectile>();
                    if (projectileScript)
                    {
                        projectileScript.Initialize(weapon.damage * 0.8f, direction, weapon.range, this);
                    }
                }
            }
        }

        private void ExecuteExplosiveBlast(DemoWeapon weapon)
        {
            // Large AoE explosion with knockback
            float damage = weapon.damage * 2f;
            float blastRadius = weapon.range * 2f;
            
            Collider2D[] hits2D = Physics2D.OverlapCircleAll(transform.position, blastRadius);
            Collider[] hits3D = Physics.OverlapSphere(transform.position, blastRadius);

            foreach (var hit in hits2D)
            {
                DamageTarget(hit.gameObject, damage, AttackType.Special);
                ApplyKnockback(hit.gameObject, transform.position, 10f);
            }
            
            foreach (var hit in hits3D)
            {
                DamageTarget(hit.gameObject, damage, AttackType.Special);
                ApplyKnockback(hit.gameObject, transform.position, 10f);
            }

            CreateAttackEffect(transform.position, blastRadius, Color.orange);
        }

        private void DamageTarget(GameObject target, float damage, AttackType attackType)
        {
            // Check if target is an enemy
            var enemy = target.GetComponent<IDemoDamageable>();
            if (enemy != null && target != gameObject)
            {
                enemy.TakeDamage(damage, this.gameObject, attackType);
                PlayHitSound();
            }
        }

        private void ApplyKnockback(GameObject target, Vector3 source, float force)
        {
            Vector3 direction = (target.transform.position - source).normalized;
            
            var rb2D = target.GetComponent<Rigidbody2D>();
            var rb3D = target.GetComponent<Rigidbody>();
            
            if (rb2D)
            {
                rb2D.AddForce(direction * force, ForceMode2D.Impulse);
            }
            else if (rb3D)
            {
                rb3D.AddForce(direction * force, ForceMode.Impulse);
            }
        }

        private void CreateAttackEffect(Vector3 position, float radius, Color color)
        {
            // Simple visual effect (can be enhanced with particles/VFX)
            GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            effect.transform.position = position;
            effect.transform.localScale = Vector3.one * radius * 2f;
            
            var renderer = effect.GetComponent<Renderer>();
            renderer.material.color = new Color(color.r, color.g, color.b, 0.3f);
            
            // Remove collider
            DestroyImmediate(effect.GetComponent<Collider>());
            
            // Destroy effect after short time
            Destroy(effect, 0.3f);
        }

        private void SwapWeapon()
        {
            if (availableWeapons.Count <= 1) return;

            currentWeaponIndex = (currentWeaponIndex + 1) % availableWeapons.Count;
            OnWeaponChanged?.Invoke(GetCurrentWeapon());
            
            if (weaponSwapSound)
            {
                audioSource.PlayOneShot(weaponSwapSound);
            }
        }

        private void ResetCombo()
        {
            comboCount = 0;
            OnComboChanged?.Invoke(comboCount);
        }

        private bool CanAttack()
        {
            return Time.time - lastAttackTime > attackCooldown / GetCurrentWeapon()?.attackSpeed ?? 1f;
        }

        private void PlayAttackSound()
        {
            if (attackSound)
            {
                audioSource.PlayOneShot(attackSound);
            }
        }

        private void PlayHitSound()
        {
            if (hitSound)
            {
                audioSource.PlayOneShot(hitSound);
            }
        }

        // Public API
        public DemoWeapon GetCurrentWeapon()
        {
            if (availableWeapons.Count == 0) return null;
            return availableWeapons[currentWeaponIndex];
        }

        public void AddWeapon(DemoWeapon weapon)
        {
            availableWeapons.Add(weapon);
        }

        public void TakeDamage(float damage)
        {
            currentHealth = Mathf.Max(0, currentHealth - Mathf.RoundToInt(damage));
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(int amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            // Handle player death (restart level, show game over, etc.)
            Debug.Log("Player died! Restarting in 3 seconds...");
            Invoke(nameof(Respawn), 3f);
        }

        private void Respawn()
        {
            currentHealth = maxHealth;
            transform.position = Vector3.zero;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        // Properties
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsCharging => isCharging;
        public int ComboCount => comboCount;
        public bool CanUseSpecialSkill => canUseSpecialSkill;
        
        // Equipment bonus system for inventory integration
        private float equipmentDamageBonus = 0f;
        private float equipmentDefenseBonus = 0f;
        private float equipmentHealthBonus = 0f;
        private float temporaryDamageBonus = 0f;
        private float temporaryDefenseBonus = 0f;
        
        // Upgrade system support
        private bool chargeAttackEnabled = false;
        private bool comboAttacksEnabled = false;
        private bool healthRegenerationEnabled = false;
        private float healthRegenRate = 0f;
        private float lastRegenTime = 0f;
        
        public void SetEquipmentBonuses(float healthBonus, float defenseBonus, float damageBonus)
        {
            equipmentHealthBonus = healthBonus;
            equipmentDefenseBonus = defenseBonus;
            equipmentDamageBonus = damageBonus;
            
            // Update max health if health bonus changed
            int newMaxHealth = Mathf.RoundToInt(maxHealth + equipmentHealthBonus);
            if (newMaxHealth != maxHealth + equipmentHealthBonus)
            {
                maxHealth = newMaxHealth;
                currentHealth = Mathf.Min(currentHealth, maxHealth);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
            
            Debug.Log($"🎯 Equipment bonuses applied: +{healthBonus} HP, +{defenseBonus} DEF, +{damageBonus} DMG");
        }
        
        public void AddDamageBonus(float bonus)
        {
            temporaryDamageBonus += bonus;
            Debug.Log($"⚔️ Temporary damage bonus added: +{bonus} (Total: +{temporaryDamageBonus})");
        }
        
        public void RemoveDamageBonus(float bonus)
        {
            temporaryDamageBonus = Mathf.Max(0f, temporaryDamageBonus - bonus);
            Debug.Log($"⚔️ Temporary damage bonus removed: -{bonus} (Remaining: +{temporaryDamageBonus})");
        }
        
        public void AddDefenseBonus(float bonus)
        {
            temporaryDefenseBonus += bonus;
            Debug.Log($"🛡️ Temporary defense bonus added: +{bonus} (Total: +{temporaryDefenseBonus})");
        }
        
        public void RemoveDefenseBonus(float bonus)
        {
            temporaryDefenseBonus = Mathf.Max(0f, temporaryDefenseBonus - bonus);
            Debug.Log($"🛡️ Temporary defense bonus removed: -{bonus} (Remaining: +{temporaryDefenseBonus})");
        }
        
        public float GetTotalDamageBonus()
        {
            return equipmentDamageBonus + temporaryDamageBonus;
        }
        
        public float GetTotalDefenseBonus()
        {
            return equipmentDefenseBonus + temporaryDefenseBonus;
        }
        
        // Override Heal method to accept float for inventory compatibility
        public void Heal(float amount)
        {
            Heal(Mathf.RoundToInt(amount));
        }

        /// <summary>
        /// Set combat stats from upgrade system
        /// </summary>
        public void SetStats(int newMaxHealth, float newAttackDamage, float newAttackSpeed, float newCriticalChance, float newDamageReduction)
        {
            int oldMaxHealth = maxHealth;
            maxHealth = newMaxHealth;
            
            // Scale current health proportionally if max health increased
            if (newMaxHealth > oldMaxHealth)
            {
                currentHealth = Mathf.RoundToInt((float)currentHealth / oldMaxHealth * newMaxHealth);
            }
            else
            {
                currentHealth = Mathf.Min(currentHealth, maxHealth);
            }

            // Update attack stats
            attackCooldown = 1f / newAttackSpeed; // Convert attack speed to cooldown
            heavyAttackMultiplier = newAttackDamage / 25f; // Scale based on damage increase
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Enable/disable charge attack capability
        /// </summary>
        public void EnableChargeAttack(bool enabled)
        {
            chargeAttackEnabled = enabled;
        }

        /// <summary>
        /// Enable/disable combo attack system
        /// </summary>
        public void EnableComboAttacks(bool enabled)
        {
            comboAttacksEnabled = enabled;
            if (!enabled)
            {
                ResetCombo();
            }
        }

        /// <summary>
        /// Enable health regeneration
        /// </summary>
        public void EnableHealthRegeneration(float regenPerSecond)
        {
            healthRegenerationEnabled = regenPerSecond > 0f;
            healthRegenRate = regenPerSecond;
            lastRegenTime = Time.time;
        }

        /// <summary>
        /// Update health regeneration (call from Update)
        /// </summary>
        private void UpdateHealthRegeneration()
        {
            if (healthRegenerationEnabled && currentHealth < maxHealth && Time.time - lastRegenTime >= 1f)
            {
                int regenAmount = Mathf.RoundToInt(healthRegenRate);
                if (regenAmount > 0)
                {
                    Heal(regenAmount);
                    lastRegenTime = Time.time;
                }
            }
        }
    }

    [System.Serializable]
    public class DemoWeapon
    {
        public string name;
        public WeaponType type;
        public int damage;
        public float range;
        public float attackSpeed;
        public GameObject projectilePrefab;
    }

    public enum WeaponType
    {
        Melee,
        Ranged,
        AoE
    }

    public enum AttackType
    {
        Light,
        Heavy,
        Charged,
        Special
    }

    public interface IDemoDamageable
    {
        void TakeDamage(float damage, GameObject source, AttackType attackType);
    }
}