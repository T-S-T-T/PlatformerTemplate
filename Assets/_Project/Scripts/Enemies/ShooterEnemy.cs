using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Stationary. Telegraphs, then fires at the player when in range and in line of sight.</summary>
    public class ShooterEnemy : Enemy
    {
        float windup;
        bool winding;

        protected override void Think(float dt)
        {
            ApplyGravity(dt);
            velocity.x = 0f;
            MoveBody(dt);

            if (PlayerAlive) facing = DirToPlayer();

            if (winding)
            {
                windup -= dt;
                if (windup <= 0f)
                {
                    winding = false;
                    Tint(baseColor);
                    if (PlayerAlive)
                    {
                        Vector2 dir = ToPlayer.normalized;
                        Fire(dir);
                    }
                    cooldown = data.attackCooldown;
                }
                return;
            }

            if (cooldown <= 0f && PlayerInRange(data.aggroRange) && HasLineOfSight())
            {
                winding = true;
                windup = data.windupTime;
                Tint(Color.white);
                GameEvents.PlaySfx(SfxId.Telegraph);
            }
        }

        protected override void OnDamaged(DamageInfo info)
        {
            base.OnDamaged(info);
            if (winding) { winding = false; Tint(baseColor); }
        }
    }
}
