using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Grants abilities permanently. The mode decides whether that is saved (metroidvania) or not.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class AbilityPickup : MonoBehaviour
    {
        public AbilityFlags grants = AbilityFlags.DoubleJump;
        public string id;
        public string displayName = "Ability";

        Vector3 start;

        void Start()
        {
            start = transform.position;
            if (GameManager.Instance.IsCollected(id)) gameObject.SetActive(false);
        }

        void Update()
        {
            transform.position = start + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.12f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.IsDead) return;

            player.Abilities.Unlock(grants);
            var gm = GameManager.Instance;
            gm.MarkCollected(id);
            if (gm.Mode != null) gm.Mode.OnAbilityUnlocked(grants);

            GameEvents.PlaySfx(SfxId.Pickup);
            FxSpawner.Puff(transform.position, 14, new Color(0.6f, 0.9f, 1f), 5f, 0.6f, 0.2f);
            GameEvents.Shake(0.15f, 0.15f);
            if (UIManager.Instance != null) UIManager.Instance.ShowToast("Unlocked: " + displayName);
            Destroy(gameObject);
        }
    }
}
