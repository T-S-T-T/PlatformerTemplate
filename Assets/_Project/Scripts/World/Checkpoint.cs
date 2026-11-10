using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlatformerTemplate
{
    /// <summary>Respawn point. In metroidvania mode it doubles as a save point.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        public SpriteRenderer flag;
        public Color inactiveColor = new Color(0.55f, 0.55f, 0.6f);
        public Color activeColor = new Color(0.35f, 1f, 0.5f);
        public bool healOnActivate = true;

        static Checkpoint current;

        public Vector2 RespawnPosition { get { return (Vector2)transform.position + Vector2.up * 0.3f; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { current = null; }

        void Start()
        {
            if (flag == null) flag = GetComponentInChildren<SpriteRenderer>();
            SetVisual(false);

            var gm = GameManager.Instance;
            if (gm.HasCheckpoint && gm.CheckpointScene == SceneManager.GetActiveScene().name
                && Vector2.Distance(gm.CheckpointPosition, RespawnPosition) < 0.1f)
            {
                current = this;
                SetVisual(true);
            }
        }

        void OnDestroy()
        {
            if (current == this) current = null;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.IsDead || current == this) return;

            if (current != null) current.SetVisual(false);
            current = this;
            SetVisual(true);

            if (healOnActivate) player.Health.Heal(player.Health.maxHealth);
            GameManager.Instance.SetCheckpoint(RespawnPosition);
            GameEvents.PlaySfx(SfxId.Checkpoint);
            FxSpawner.Puff(transform.position, 10, activeColor, 4f, 0.5f);
        }

        void SetVisual(bool active)
        {
            if (flag != null) flag.color = active ? activeColor : inactiveColor;
        }
    }
}
