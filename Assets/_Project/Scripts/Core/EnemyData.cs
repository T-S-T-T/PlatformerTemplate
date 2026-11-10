using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Tuning for one enemy type. A new variant is a new asset, not new code.</summary>
    [CreateAssetMenu(menuName = "Platformer Template/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Health")]
        public int maxHealth = 2;
        public int scoreValue = 100;
        public float hurtStun = 0.25f;
        public Vector2 hurtKnockback = new Vector2(5f, 4f);

        [Header("Contact damage")]
        public int contactDamage = 1;
        public float contactKnockback = 1f;

        [Header("Movement")]
        public float moveSpeed = 2f;
        public float chaseMultiplier = 1.6f;
        public float gravity = 60f;
        public float maxFallSpeed = 20f;

        [Header("Awareness")]
        public float aggroRange = 8f;

        [Header("Attacks")]
        public float attackCooldown = 2f;
        public float windupTime = 0.5f;
        public float recoverTime = 0.8f;
        public float projectileSpeed = 8f;
        public GameObject projectilePrefab;
        public float chargeSpeed = 12f;
        public float chargeDuration = 0.7f;
    }
}
