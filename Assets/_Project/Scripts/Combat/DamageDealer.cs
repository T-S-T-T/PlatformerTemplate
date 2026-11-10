using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>
    /// Trigger that damages anything with a Health on the other team while overlapping.
    /// Use team = Enemy for enemy bodies, Neutral for spikes/saws (hurts everyone).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DamageDealer : MonoBehaviour
    {
        public Team team = Team.Enemy;
        public int damage = 1;
        public float knockback = 1f;
        [Tooltip("Kills the target outright (pits, crushers).")]
        public bool instantKill;
        [Tooltip("After hurting the player, return them to the last safe ground (spikes).")]
        public bool returnPlayerToSafeGround;

        void Reset()
        {
            var c = GetComponent<Collider2D>();
            if (c != null) c.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other) { TryHit(other); }
        void OnTriggerStay2D(Collider2D other) { TryHit(other); }

        void TryHit(Collider2D other)
        {
            var target = other.GetComponentInParent<IDamageable>();
            if (target == null) return;
            if (team != Team.Neutral && target.Team == team) return;

            var info = new DamageInfo
            {
                amount = damage,
                team = team,
                sourcePosition = transform.position,
                knockback = knockback,
                instantKill = instantKill
            };

            if (!target.TakeDamage(info)) return;

            if (returnPlayerToSafeGround)
            {
                var player = other.GetComponentInParent<PlayerController>();
                if (player != null && !player.IsDead) player.ReturnToSafePosition();
            }
        }
    }
}
