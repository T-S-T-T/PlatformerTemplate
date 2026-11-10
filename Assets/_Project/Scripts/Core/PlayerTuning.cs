using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>
    /// Every movement, ability and combat number for the player. Edit this asset in Play Mode to tune feel.
    /// Units: 1 unit = 1 tile (16 px). Jump height and apex time define gravity, so you tune in tiles.
    /// </summary>
    [CreateAssetMenu(menuName = "Platformer Template/Player Tuning", fileName = "PlayerTuning")]
    public class PlayerTuning : ScriptableObject
    {
        [Header("Run")]
        public float maxRunSpeed = 8f;
        [Tooltip("Seconds to reach top speed on the ground.")]
        public float groundAccelTime = 0.08f;
        [Tooltip("Seconds to stop from top speed on the ground.")]
        public float groundDecelTime = 0.05f;
        [Range(0.1f, 1f), Tooltip("Air acceleration as a fraction of ground acceleration.")]
        public float airControl = 0.8f;

        [Header("Jump")]
        public float jumpHeight = 4f;
        [Tooltip("Height reached when the button is tapped. Sets the jump-cut gravity multiplier.")]
        public float tapJumpHeight = 1.5f;
        public float timeToApex = 0.35f;
        public float fallGravityMultiplier = 1.6f;
        public float maxFallSpeed = 20f;
        public float coyoteTime = 0.10f;
        public float jumpBuffer = 0.10f;
        [Tooltip("Max horizontal nudge (units) to slide around a ceiling corner.")]
        public float cornerCorrection = 0.3f;
        public float apexHangTime = 0.05f;
        [Range(0.05f, 1f)] public float apexGravityMultiplier = 0.4f;
        public float dropThroughTime = 0.2f;

        [Header("Double jump")]
        public int extraJumps = 1;
        public float doubleJumpHeight = 3.2f;

        [Header("Wall slide / wall jump")]
        public float wallSlideSpeed = 3f;
        public float wallCheckDistance = 0.06f;
        public float wallCoyoteTime = 0.10f;
        public float wallJumpPush = 7f;
        public float wallJumpHeight = 3.6f;
        [Tooltip("Seconds that horizontal input is ignored after a wall jump.")]
        public float wallJumpLock = 0.15f;

        [Header("Dash")]
        public float dashDistance = 3f;
        public float dashDuration = 0.15f;
        public float dashCooldown = 0.15f;
        public int airDashes = 1;
        public bool dashInvulnerable = false;
        public float dashEndUpSpeed = 6f;

        [Header("Health")]
        public int maxHealth = 3;
        public float invulnerableTime = 1.0f;
        public float hurtStun = 0.2f;
        public float knockbackX = 8f;
        public float knockbackY = 7f;
        public float hitStop = 0.05f;
        public float deathDelay = 0.8f;

        [Header("Melee")]
        public int meleeDamage = 1;
        public float meleeCooldown = 0.25f;
        public float meleeActiveTime = 0.10f;
        public float meleeRange = 1.4f;
        public float meleeThickness = 1.3f;
        public float meleeKnockback = 1f;
        public float pogoBounce = 14f;

        [Header("Ranged (recharging energy)")]
        public int rangedDamage = 1;
        public float projectileSpeed = 16f;
        public int maxProjectiles = 3;
        public int rangedMaxEnergy = 3;
        [Tooltip("Seconds to recharge one shot.")]
        public float rangedRechargeTime = 1.2f;

        // Derived values
        public float Gravity { get { return 2f * jumpHeight / (timeToApex * timeToApex); } }
        public float JumpVelocity { get { return Gravity * timeToApex; } }
        public float DoubleJumpVelocity { get { return Mathf.Sqrt(2f * Gravity * doubleJumpHeight); } }
        public float WallJumpVelocity { get { return Mathf.Sqrt(2f * Gravity * wallJumpHeight); } }
        public float CutGravityMultiplier { get { return Mathf.Max(1f, jumpHeight / Mathf.Max(0.1f, tapJumpHeight)); } }
    }
}
