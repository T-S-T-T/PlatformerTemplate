using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>A solid block that the player's attacks destroy. Put it on the Ground layer.</summary>
    public class BreakableBlock : MonoBehaviour, IDamageable
    {
        public Team Team { get { return Team.Neutral; } }

        public bool TakeDamage(DamageInfo info)
        {
            if (info.team != Team.Player) return false;
            GameEvents.PlaySfx(SfxId.Break);
            GameEvents.Shake(0.1f, 0.1f);
            var sr = GetComponentInChildren<SpriteRenderer>();
            FxSpawner.Puff(transform.position, 10, sr != null ? sr.color : Color.white, 5f, 0.5f, 0.22f);
            Destroy(gameObject);
            return true;
        }
    }
}
