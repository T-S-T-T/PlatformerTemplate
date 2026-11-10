using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>A barrier that opens once the player owns the required ability (e.g. a dash door).</summary>
    public class AbilityGate : MonoBehaviour
    {
        public AbilityFlags required = AbilityFlags.Dash;
        public Collider2D solid;
        public SpriteRenderer sprite;
        public Color lockedColor = new Color(0.9f, 0.4f, 0.3f);

        bool open;

        void Start()
        {
            if (solid == null) solid = GetComponent<Collider2D>();
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null) sprite.color = lockedColor;
            Refresh();
        }

        void OnEnable() { GameEvents.AbilitiesChanged += OnAbilitiesChanged; }
        void OnDisable() { GameEvents.AbilitiesChanged -= OnAbilitiesChanged; }

        void OnAbilitiesChanged(AbilityFlags flags) { Refresh(); }

        void Refresh()
        {
            var player = PlayerController.Instance;
            bool has = player != null && player.Abilities.Has(required);
            if (has == open) return;
            open = has;
            if (solid != null) solid.enabled = !open;
            if (sprite != null) sprite.enabled = !open;
            if (open)
            {
                GameEvents.PlaySfx(SfxId.Break);
                FxSpawner.Puff(transform.position, 8, lockedColor, 4f, 0.5f);
            }
        }
    }
}
