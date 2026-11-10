using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Hovers at its home position, then sweeps toward the player when they come into range.</summary>
    public class FlyerEnemy : Enemy
    {
        Vector2 home;
        float phase;

        protected override void Awake()
        {
            base.Awake();
            home = transform.position;
            phase = Random.value * 6.28f;
        }

        protected override void Think(float dt)
        {
            bool aggro = PlayerInRange(data.aggroRange);
            Vector2 target;
            float speed;

            if (aggro)
            {
                target = (Vector2)Player.transform.position;
                speed = data.moveSpeed * data.chaseMultiplier;
                facing = DirToPlayer();
            }
            else
            {
                target = home + new Vector2(0f, Mathf.Sin(Time.time * 2f + phase) * 0.35f);
                speed = data.moveSpeed;
            }

            Vector2 desired = (target - (Vector2)transform.position);
            desired = desired.magnitude > 0.1f ? desired.normalized * speed : Vector2.zero;
            velocity = Vector2.MoveTowards(velocity, desired, 18f * dt);

            body.Move(velocity * dt);
            if ((body.Left && velocity.x < 0f) || (body.Right && velocity.x > 0f)) velocity.x = 0f;
            if ((body.Above && velocity.y > 0f) || (body.Below && velocity.y < 0f)) velocity.y = 0f;
        }

        protected override void UpdateHurt(float dt)
        {
            velocity = Vector2.MoveTowards(velocity, Vector2.zero, 25f * dt);
            body.Move(velocity * dt);
        }
    }
}
