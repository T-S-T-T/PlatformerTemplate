using System;
using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Hit points + invulnerability window. Shared by the player and every enemy.</summary>
    public class Health : MonoBehaviour, IDamageable
    {
        public Team team = Team.Neutral;
        public int maxHealth = 3;
        [Tooltip("Seconds of invulnerability after taking damage.")]
        public float invulnerableTime = 0f;

        public int Current { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsInvulnerable { get { return invulnTimer > 0f; } }
        public Team Team { get { return team; } }

        public event Action<int, int> Changed;           // current, max
        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;

        float invulnTimer;
        bool initialized;

        void Awake()
        {
            if (!initialized) Initialize(maxHealth);
        }

        void Update()
        {
            if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;
        }

        public void Initialize(int max)
        {
            initialized = true;
            maxHealth = max;
            Current = max;
            IsDead = false;
            invulnTimer = 0f;
            if (Changed != null) Changed(Current, maxHealth);
        }

        public void ResetFull()
        {
            Initialize(maxHealth);
        }

        public void SetInvulnerable(float seconds)
        {
            invulnTimer = Mathf.Max(invulnTimer, seconds);
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            if (Changed != null) Changed(Current, maxHealth);
        }

        public bool TakeDamage(DamageInfo info)
        {
            if (IsDead) return false;
            if (IsInvulnerable && !info.instantKill) return false;
            if (info.team != Team.Neutral && info.team == team) return false;

            Current = info.instantKill ? 0 : Mathf.Max(0, Current - info.amount);
            invulnTimer = invulnerableTime;
            bool dead = Current <= 0;
            if (dead) IsDead = true;

            if (Changed != null) Changed(Current, maxHealth);
            if (Damaged != null) Damaged(info);
            if (dead && Died != null) Died(info);
            return true;
        }
    }
}
