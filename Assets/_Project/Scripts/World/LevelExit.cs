using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Ends the level (level mode). Can stay locked until the boss is defeated.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        public bool requiresBossDefeat;
        public SpriteRenderer sprite;
        public Color lockedColor = new Color(0.4f, 0.4f, 0.45f);
        public Color openColor = new Color(0.4f, 0.9f, 1f);

        bool open;
        bool used;

        void Start()
        {
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            SetOpen(!requiresBossDefeat);
        }

        void OnEnable() { GameEvents.BossDefeated += OnBossDefeated; }
        void OnDisable() { GameEvents.BossDefeated -= OnBossDefeated; }

        void OnBossDefeated()
        {
            if (requiresBossDefeat) SetOpen(true);
        }

        void SetOpen(bool value)
        {
            open = value;
            if (sprite != null) sprite.color = open ? openColor : lockedColor;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!open || used) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.IsDead) return;

            used = true;
            GameEvents.PlaySfx(SfxId.Win);
            var mode = GameManager.Instance.Mode;
            if (mode != null) mode.OnLevelExit();
        }
    }
}
