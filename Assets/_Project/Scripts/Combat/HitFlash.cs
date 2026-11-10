using System.Collections;
using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>White flash on hit, plus optional blinking while invulnerable.</summary>
    public class HitFlash : MonoBehaviour
    {
        public SpriteRenderer target;
        public bool blinkWhileInvulnerable;
        public Color flashColor = Color.white;

        Health health;
        Color baseColor = Color.white;
        float flashTimer;

        void Awake()
        {
            if (target == null) target = GetComponentInChildren<SpriteRenderer>();
            health = GetComponent<Health>();
            if (target != null) baseColor = target.color;
        }

        /// <summary>Update the colour the sprite returns to after a flash.</summary>
        public void SetBaseColor(Color c)
        {
            baseColor = c;
            if (flashTimer <= 0f && target != null) target.color = c;
        }

        public void Flash(float duration = 0.1f)
        {
            flashTimer = duration;
            if (target != null) target.color = flashColor;
        }

        void Update()
        {
            if (target == null) return;

            if (flashTimer > 0f)
            {
                flashTimer -= Time.unscaledDeltaTime;
                if (flashTimer <= 0f) target.color = baseColor;
            }

            if (blinkWhileInvulnerable && health != null && !health.IsDead)
            {
                target.enabled = !health.IsInvulnerable || (Mathf.FloorToInt(Time.unscaledTime * 20f) % 2 == 0);
            }
            else if (!target.enabled && (health == null || !health.IsDead))
            {
                target.enabled = true;
            }
        }
    }
}
