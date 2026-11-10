using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>
    /// Three-phase demo boss. Alternates a charge and a projectile volley; each phase (by health) makes it faster
    /// and adds projectiles. Dying raises GameEvents.BossDefeated, which can open the level exit.
    /// </summary>
    public class BossEnemy : Enemy
    {
        enum Step { Wait, ChargeWindup, Charging, VolleyWindup, Recover }

        public int baseVolleyCount = 3;

        Step step = Step.Wait;
        float timer;
        bool nextIsCharge = true;

        int Phase
        {
            get
            {
                float f = (float)health.Current / Mathf.Max(1, health.maxHealth);
                return f > 0.66f ? 0 : (f > 0.33f ? 1 : 2);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            timer = 1.2f;   // short grace period before the first attack
        }

        protected override void Think(float dt)
        {
            ApplyGravity(dt);

            switch (step)
            {
                case Step.Wait:
                    velocity.x = 0f;
                    if (PlayerAlive) facing = DirToPlayer();
                    if (PlayerInRange(data.aggroRange))
                    {
                        timer -= dt;
                        if (timer <= 0f)
                        {
                            step = nextIsCharge ? Step.ChargeWindup : Step.VolleyWindup;
                            timer = data.windupTime;
                            Tint(Color.white);
                            GameEvents.PlaySfx(SfxId.Telegraph);
                        }
                    }
                    break;

                case Step.ChargeWindup:
                    velocity.x = 0f;
                    timer -= dt;
                    if (timer <= 0f)
                    {
                        step = Step.Charging;
                        timer = data.chargeDuration;
                        Tint(baseColor);
                    }
                    break;

                case Step.Charging:
                    velocity.x = facing * data.chargeSpeed * (1f + 0.25f * Phase);
                    timer -= dt;
                    if (timer <= 0f || (facing > 0 && body.Right) || (facing < 0 && body.Left))
                    {
                        GameEvents.Shake(0.3f, 0.2f);
                        EndStep();
                    }
                    break;

                case Step.VolleyWindup:
                    velocity.x = 0f;
                    timer -= dt;
                    if (timer <= 0f)
                    {
                        FireVolley();
                        Tint(baseColor);
                        EndStep();
                    }
                    break;

                case Step.Recover:
                    velocity.x = Mathf.MoveTowards(velocity.x, 0f, 40f * dt);
                    timer -= dt;
                    if (timer <= 0f)
                    {
                        step = Step.Wait;
                        timer = Mathf.Max(0.3f, data.attackCooldown - 0.4f * Phase);
                    }
                    break;
            }

            MoveBody(dt);
        }

        void EndStep()
        {
            step = Step.Recover;
            timer = Mathf.Max(0.3f, data.recoverTime - 0.2f * Phase);
            nextIsCharge = !nextIsCharge;
        }

        void FireVolley()
        {
            if (!PlayerAlive) return;
            int count = baseVolleyCount + 2 * Phase;
            Vector2 aim = ToPlayer.normalized;
            float spread = 14f;
            for (int i = 0; i < count; i++)
            {
                float angle = (i - (count - 1) * 0.5f) * spread;
                Vector2 dir = Quaternion.Euler(0f, 0f, angle) * aim;
                Fire(dir);
            }
        }

        protected override void OnDamaged(DamageInfo info)
        {
            base.OnDamaged(info);
            // The boss shrugs off knockback.
            velocity.x = 0f;
            hurtTimer = 0f;
        }

        protected override void OnDied(DamageInfo info)
        {
            if (GameEvents.BossDefeated != null) GameEvents.BossDefeated();
            if (GameManager.Instance.Mode != null) GameManager.Instance.Mode.OnBossDefeated();
            FxSpawner.Puff(transform.position, 30, baseColor, 9f, 0.9f, 0.3f);
            GameEvents.Shake(0.6f, 0.5f);
            base.OnDied(info);
        }
    }
}
