using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Idle, then telegraphs, charges in a straight line, and is left briefly exposed.</summary>
    public class ChargerEnemy : Enemy
    {
        enum Phase { Idle, Windup, Charging, Recover }

        Phase phase = Phase.Idle;
        float timer;

        protected override void Think(float dt)
        {
            ApplyGravity(dt);

            switch (phase)
            {
                case Phase.Idle:
                    velocity.x = 0f;
                    if (PlayerAlive) facing = DirToPlayer();
                    if (cooldown <= 0f && PlayerAlive
                        && Mathf.Abs(ToPlayer.x) < data.aggroRange && Mathf.Abs(ToPlayer.y) < 1.8f)
                    {
                        phase = Phase.Windup;
                        timer = data.windupTime;
                        Tint(Color.white);
                        GameEvents.PlaySfx(SfxId.Telegraph);
                    }
                    break;

                case Phase.Windup:
                    velocity.x = 0f;
                    timer -= dt;
                    if (timer <= 0f)
                    {
                        phase = Phase.Charging;
                        timer = data.chargeDuration;
                        Tint(baseColor);
                    }
                    break;

                case Phase.Charging:
                    velocity.x = facing * data.chargeSpeed;
                    timer -= dt;
                    if (timer <= 0f || (facing > 0 && body.Right) || (facing < 0 && body.Left)
                        || (body.Below && !body.GroundAhead(facing, 1.2f)))
                    {
                        phase = Phase.Recover;
                        timer = data.recoverTime;
                        velocity.x = 0f;
                    }
                    break;

                case Phase.Recover:
                    velocity.x = Mathf.MoveTowards(velocity.x, 0f, 30f * dt);
                    timer -= dt;
                    if (timer <= 0f)
                    {
                        phase = Phase.Idle;
                        cooldown = data.attackCooldown;
                    }
                    break;
            }

            MoveBody(dt);
        }

        protected override void OnDamaged(DamageInfo info)
        {
            base.OnDamaged(info);
            if (phase == Phase.Windup) { phase = Phase.Idle; Tint(baseColor); cooldown = 0.5f; }
        }
    }
}
