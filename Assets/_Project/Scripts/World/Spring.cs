using System.Collections;
using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Launch pad. Refills double jump and dash like a pogo bounce.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Spring : MonoBehaviour
    {
        public float launchSpeed = 24f;
        public Transform visual;

        float cooldown;

        void Update()
        {
            if (cooldown > 0f) cooldown -= Time.deltaTime;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (cooldown > 0f) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.IsDead || player.Velocity.y > 0.5f) return;

            cooldown = 0.25f;
            player.Launch(launchSpeed);
            GameEvents.PlaySfx(SfxId.Spring);
            if (visual != null) StartCoroutine(Bounce());
        }

        IEnumerator Bounce()
        {
            Vector3 baseScale = visual.localScale;
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin(t / 0.2f * Mathf.PI);
                visual.localScale = new Vector3(baseScale.x * (1f - 0.2f * k), baseScale.y * (1f + 0.4f * k), baseScale.z);
                yield return null;
            }
            visual.localScale = baseScale;
        }
    }
}
