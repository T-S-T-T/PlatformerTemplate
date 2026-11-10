using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Walks back and forth, turns at walls and ledges, and speeds up toward the player when close.</summary>
    public class PatrolEnemy : Enemy
    {
        protected override void Think(float dt)
        {
            float speed = data.moveSpeed;
            bool aggro = PlayerAlive && Mathf.Abs(ToPlayer.y) < 1.6f && Mathf.Abs(ToPlayer.x) < data.aggroRange;

            if (aggro)
            {
                facing = DirToPlayer();
                speed *= data.chaseMultiplier;
            }

            bool blocked = (facing > 0 && body.Right) || (facing < 0 && body.Left)
                        || (body.Below && !body.GroundAhead(facing));

            if (blocked)
            {
                if (aggro) speed = 0f;     // stop at ledges instead of jittering
                else facing = -facing;
            }

            velocity.x = facing * speed;
            ApplyGravity(dt);
            MoveBody(dt);
        }
    }
}
