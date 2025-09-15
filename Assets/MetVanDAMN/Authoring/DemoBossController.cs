using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring
    {
    // Intentionally simple boss for demo/testing. Real AI is out of scope for the generator.
    public sealed class DemoBossController : MonoBehaviour
        {
        [Header("Stats")]
        public int maxHealth = 100;
        public int contactDamage = 10;

        [Header("Presentation")]
        public Color bossColor = new(0.8f, 0.2f, 0.2f, 1f);
        public float gizmoRadius = 1.25f;

        private int _hp;

        private void Awake()
            {
            _hp = Mathf.Max(1, maxHealth);
            name = string.IsNullOrEmpty(name) ? "DemoBoss" : name;
            }

        public void TakeDamage(int amount)
            {
            if (amount <= 0) return;
            _hp -= amount;
            if (_hp <= 0)
                {
                Die();
                }
            }

        private void Die()
            {
            // Simple death: destroy self; in real game, trigger loot/event/etc.
            Destroy(gameObject);
            }

        private void OnDrawGizmos()
            {
            Gizmos.color = bossColor;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
            }
        }
    }
