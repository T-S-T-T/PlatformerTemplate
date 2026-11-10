using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlatformerTemplate
{
    public enum PlayerState { Grounded, Airborne, WallSlide, Dash, Hurt, Dead }

    /// <summary>
    /// Tight, frame-rate independent platformer controller on top of KinematicBody2D.
    /// Features: variable jump, coyote time, jump buffering, corner correction, apex hang, one-way drop-through,
    /// moving platforms, double jump, wall slide/jump, 8-way dash, melee (with pogo) and ranged attacks.
    /// Every number comes from <see cref="PlayerTuning"/>.
    /// </summary>
    [RequireComponent(typeof(KinematicBody2D), typeof(Health), typeof(PlayerAbilities))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Data")]
        public PlayerTuning tuning;

        [Header("References (found by name if empty)")]
        public Transform visual;
        public SpriteRenderer slash;
        public GameObject projectilePrefab;

        [Header("Melee hits these layers")]
        public LayerMask meleeMask;

        // Public read-only state (used by camera, juice, UI, debug overlay)
        public PlayerState State { get; private set; } = PlayerState.Airborne;
        public Vector2 Velocity { get { return velocity; } }
        public int Facing { get { return facing; } }
        public bool Grounded { get { return grounded; } }
        public bool IsDead { get { return State == PlayerState.Dead; } }
        public Vector2 LookInput { get; private set; }
        public Vector2 LastSafePosition { get; private set; }
        public float CoyoteTimer { get { return coyoteTimer; } }
        public float BufferTimer { get { return jumpBufferTimer; } }
        public float RangedEnergy { get { return rangedEnergy; } }
        public int DashesLeft { get { return dashesLeft; } }
        public int ExtraJumpsLeft { get { return extraJumpsLeft; } }

        public KinematicBody2D Body { get; private set; }
        public Health Health { get; private set; }
        public PlayerAbilities Abilities { get; private set; }
        public PlayerInputReader InputReader { get; private set; }

        const float MaxDt = 0.05f;

        Vector2 velocity;
        int facing = 1;
        bool grounded;
        Collider2D groundCol;
        MovingPlatform platform;
        SpriteRenderer visualSprite;

        float coyoteTimer, jumpBufferTimer, wallCoyoteTimer, wallLockTimer;
        float dashTimer, dashCooldownTimer, apexTimer, hurtTimer;
        float meleeCooldownTimer, meleeActiveTimer, dropThroughTimer;
        float rangedEnergy;
        int lastWallDir, wallDir, extraJumpsLeft, dashesLeft;
        Vector2 dashDir, meleeDir;
        readonly HashSet<Collider2D> meleeHit = new HashSet<Collider2D>();

        // ------------------------------------------------------------ lifecycle

        void Awake()
        {
            Instance = this;
            Body = GetComponent<KinematicBody2D>();
            Health = GetComponent<Health>();
            Abilities = GetComponent<PlayerAbilities>();
            InputReader = GetComponent<PlayerInputReader>();

            if (tuning == null)
            {
                Debug.LogWarning("PlayerController: no PlayerTuning assigned, using defaults.", this);
                tuning = ScriptableObject.CreateInstance<PlayerTuning>();
            }
            if (visual == null) visual = transform.Find("Visual");
            if (visual != null) visualSprite = visual.GetComponent<SpriteRenderer>();
            if (slash == null)
            {
                Transform s = transform.Find("Slash");
                if (s != null) slash = s.GetComponent<SpriteRenderer>();
            }
            if (slash != null) slash.enabled = false;

            Health.team = Team.Player;
            Health.invulnerableTime = tuning.invulnerableTime;
            Health.Initialize(tuning.maxHealth);
            Health.Changed += OnHealthChanged;
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
        }

        void Start()
        {
            LastSafePosition = transform.position;
            rangedEnergy = tuning.rangedMaxEnergy;
            OnHealthChanged(Health.Current, Health.maxHealth);
        }

        void OnDestroy()
        {
            if (Health != null)
            {
                Health.Changed -= OnHealthChanged;
                Health.Damaged -= OnDamaged;
                Health.Died -= OnDied;
            }
            if (Instance == this) Instance = null;
        }

        void OnHealthChanged(int current, int max)
        {
            if (GameEvents.PlayerHealthChanged != null) GameEvents.PlayerHealthChanged(current, max);
        }

        bool Has(AbilityFlags f) { return Abilities.Has(f); }

        // ------------------------------------------------------------ main loop

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, MaxDt);
            if (dt <= 0f) return;
            if (GameManager.Instance.State != GameState.Playing) return;
            if (State == PlayerState.Dead) return;

            Vector2 move = InputReader.Move;
            LookInput = move;
            bool jumpHeld = InputReader.JumpHeld;
            if (InputReader.JumpPressed) jumpBufferTimer = tuning.jumpBuffer;

            TickTimers(dt);

            // Ride a moving platform (platforms update first via execution order).
            if (platform != null && grounded) Body.Move(platform.FrameDelta, dropThroughTimer > 0f);

            bool wasGrounded = grounded;
            float prevVy = velocity.y;
            ProbeSurroundings();

            if (!wasGrounded && grounded)
            {
                if (GameEvents.PlayerLanded != null) GameEvents.PlayerLanded(transform.position, -prevVy);
                GameEvents.PlaySfx(SfxId.Land);
            }

            switch (State)
            {
                case PlayerState.Dash: UpdateDash(dt); break;
                case PlayerState.Hurt: UpdateHurt(dt); break;
                default: UpdateNormal(dt, move, jumpHeld); break;
            }

            UpdateMelee(dt);
            UpdateVisual();
        }

        void TickTimers(float dt)
        {
            coyoteTimer -= dt;
            jumpBufferTimer -= dt;
            wallCoyoteTimer -= dt;
            wallLockTimer -= dt;
            dashCooldownTimer -= dt;
            apexTimer -= dt;
            meleeCooldownTimer -= dt;
            dropThroughTimer -= dt;
            rangedEnergy = Mathf.Min(tuning.rangedMaxEnergy, rangedEnergy + dt / Mathf.Max(0.05f, tuning.rangedRechargeTime));
        }

        void ProbeSurroundings()
        {
            grounded = false;
            groundCol = null;
            platform = null;

            if (velocity.y <= 0.01f)
                grounded = Body.Probe(Vector2.down, 0.05f, dropThroughTimer <= 0f, out groundCol);

            if (grounded)
            {
                coyoteTimer = tuning.coyoteTime;
                extraJumpsLeft = tuning.extraJumps;
                dashesLeft = tuning.airDashes;
                wallCoyoteTimer = 0f;
                platform = groundCol != null ? groundCol.GetComponentInParent<MovingPlatform>() : null;
                if (platform == null && State != PlayerState.Hurt && State != PlayerState.Dash)
                    LastSafePosition = transform.position;
            }

            wallDir = 0;
            if (!grounded && (Has(AbilityFlags.WallSlide) || Has(AbilityFlags.WallJump)))
            {
                Collider2D wall;
                if (Body.Probe(Vector2.right, tuning.wallCheckDistance, false, out wall)) wallDir = 1;
                else if (Body.Probe(Vector2.left, tuning.wallCheckDistance, false, out wall)) wallDir = -1;
            }
            if (wallDir != 0)
            {
                lastWallDir = wallDir;
                wallCoyoteTimer = tuning.wallCoyoteTime;
                dashesLeft = tuning.airDashes;
            }
        }

        // ------------------------------------------------------------ normal movement

        void UpdateNormal(float dt, Vector2 move, bool jumpHeld)
        {
            // Dash
            if (InputReader.DashPressed && Has(AbilityFlags.Dash) && dashCooldownTimer <= 0f && dashesLeft > 0)
            {
                StartDash(move);
                UpdateDash(dt);
                return;
            }

            // Attacks
            if (InputReader.MeleePressed && Has(AbilityFlags.Melee) && meleeCooldownTimer <= 0f) StartMelee(move);
            if (InputReader.RangedPressed && Has(AbilityFlags.Ranged)) TryShoot();

            // Horizontal
            bool locked = wallLockTimer > 0f;
            if (!locked)
            {
                float target = Mathf.Abs(move.x) > 0.3f ? Mathf.Sign(move.x) * tuning.maxRunSpeed : 0f;
                float accelTime = grounded ? tuning.groundAccelTime : tuning.groundAccelTime / Mathf.Max(0.01f, tuning.airControl);
                float decelTime = grounded ? tuning.groundDecelTime : tuning.groundDecelTime / Mathf.Max(0.01f, tuning.airControl);
                float accelRate = tuning.maxRunSpeed / Mathf.Max(0.001f, accelTime);
                float decelRate = tuning.maxRunSpeed / Mathf.Max(0.001f, decelTime);

                bool speedingUp = target != 0f
                    && Mathf.Abs(target) > Mathf.Abs(velocity.x)
                    && (velocity.x == 0f || Mathf.Sign(velocity.x) == Mathf.Sign(target));
                velocity.x = Mathf.MoveTowards(velocity.x, target, (speedingUp ? accelRate : decelRate) * dt);

                if (Mathf.Abs(move.x) > 0.3f) facing = move.x > 0f ? 1 : -1;
            }

            // Drop through one-way platforms: down + jump
            if (grounded && groundCol != null && jumpBufferTimer > 0f && move.y < -0.6f
                && ((Body.oneWayMask.value >> groundCol.gameObject.layer) & 1) != 0)
            {
                dropThroughTimer = tuning.dropThroughTime;
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                grounded = false;
                velocity.y = -2f;
            }

            // Jumps
            if (jumpBufferTimer > 0f)
            {
                if (coyoteTimer > 0f)
                {
                    velocity.y = tuning.JumpVelocity;
                    coyoteTimer = 0f;
                    jumpBufferTimer = 0f;
                    apexTimer = 0f;
                    if (GameEvents.PlayerJumped != null) GameEvents.PlayerJumped(transform.position);
                    GameEvents.PlaySfx(SfxId.Jump);
                }
                else if (wallCoyoteTimer > 0f && Has(AbilityFlags.WallJump))
                {
                    int away = -lastWallDir;
                    velocity = new Vector2(away * tuning.wallJumpPush, tuning.WallJumpVelocity);
                    facing = away;
                    wallLockTimer = tuning.wallJumpLock;
                    wallCoyoteTimer = 0f;
                    jumpBufferTimer = 0f;
                    apexTimer = 0f;
                    extraJumpsLeft = tuning.extraJumps;
                    if (GameEvents.PlayerWallJumped != null) GameEvents.PlayerWallJumped(transform.position, away);
                    GameEvents.PlaySfx(SfxId.WallJump);
                }
                else if (extraJumpsLeft > 0 && Has(AbilityFlags.DoubleJump))
                {
                    velocity.y = tuning.DoubleJumpVelocity;
                    extraJumpsLeft--;
                    jumpBufferTimer = 0f;
                    apexTimer = 0f;
                    if (GameEvents.PlayerDoubleJumped != null) GameEvents.PlayerDoubleJumped(transform.position);
                    GameEvents.PlaySfx(SfxId.DoubleJump);
                }
            }

            // Wall slide
            bool sliding = !grounded && wallDir != 0 && velocity.y <= 0f && Has(AbilityFlags.WallSlide)
                && Mathf.Abs(move.x) > 0.3f && Mathf.Sign(move.x) == wallDir;

            // Gravity
            float g = tuning.Gravity;
            if (velocity.y > 0f)
            {
                if (!jumpHeld) g *= tuning.CutGravityMultiplier;
            }
            else g *= tuning.fallGravityMultiplier;
            if (apexTimer > 0f) g *= tuning.apexGravityMultiplier;

            float oldVy = velocity.y;
            float newVy = oldVy - g * dt;
            newVy = sliding ? Mathf.Max(newVy, -tuning.wallSlideSpeed) : Mathf.Max(newVy, -tuning.maxFallSpeed);
            if (oldVy > 0f && newVy <= 0f && jumpHeld && !grounded) apexTimer = tuning.apexHangTime;
            float dy = (oldVy + newVy) * 0.5f * dt;
            velocity.y = newVy;

            // Move
            if (dy > 0f) Body.TryCornerCorrection(dy, tuning.cornerCorrection);
            Body.Move(new Vector2(velocity.x * dt, dy), dropThroughTimer > 0f);
            ResolveContacts();

            State = grounded ? PlayerState.Grounded : (sliding ? PlayerState.WallSlide : PlayerState.Airborne);
        }

        void ResolveContacts()
        {
            if (Body.Above && velocity.y > 0f) velocity.y = 0f;
            if (Body.Below && velocity.y < 0f) velocity.y = 0f;
            if ((Body.Left && velocity.x < 0f) || (Body.Right && velocity.x > 0f)) velocity.x = 0f;
        }

        // ------------------------------------------------------------ dash

        void StartDash(Vector2 move)
        {
            Vector2 dir = move.sqrMagnitude > 0.25f ? SnapTo8(move) : new Vector2(facing, 0f);
            dashDir = dir;
            dashTimer = tuning.dashDuration;
            dashCooldownTimer = tuning.dashDuration + tuning.dashCooldown;
            dashesLeft--;
            velocity = dir * (tuning.dashDistance / Mathf.Max(0.01f, tuning.dashDuration));
            State = PlayerState.Dash;
            apexTimer = 0f;
            wallLockTimer = 0f;
            if (Mathf.Abs(dir.x) > 0.1f) facing = dir.x > 0f ? 1 : -1;
            if (tuning.dashInvulnerable) Health.SetInvulnerable(tuning.dashDuration + 0.05f);
            if (GameEvents.PlayerDashed != null) GameEvents.PlayerDashed(transform.position, dir);
            GameEvents.PlaySfx(SfxId.Dash);
        }

        static Vector2 SnapTo8(Vector2 v)
        {
            float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            angle = Mathf.Round(angle / 45f) * 45f;
            float r = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Round(Mathf.Cos(r) * 1000f) / 1000f, Mathf.Round(Mathf.Sin(r) * 1000f) / 1000f).normalized;
        }

        void UpdateDash(float dt)
        {
            dashTimer -= dt;
            Body.Move(velocity * dt, dropThroughTimer > 0f);

            bool blocked = (Body.Left && dashDir.x < 0f) || (Body.Right && dashDir.x > 0f)
                        || (Body.Above && dashDir.y > 0f) || (Body.Below && dashDir.y < 0f);
            if (dashTimer <= 0f || blocked) EndDash();
        }

        void EndDash()
        {
            float vy = dashDir.y > 0f ? dashDir.y * tuning.dashEndUpSpeed : (dashDir.y < 0f ? dashDir.y * 6f : 0f);
            velocity = new Vector2(dashDir.x * tuning.maxRunSpeed, vy);
            State = PlayerState.Airborne;
        }

        // ------------------------------------------------------------ hurt / death

        void OnDamaged(DamageInfo info)
        {
            if (State == PlayerState.Dead) return;
            if (GameEvents.PlayerDamaged != null) GameEvents.PlayerDamaged(transform.position);
            GameEvents.PlaySfx(SfxId.PlayerHit);
            GameEvents.Shake(0.3f, 0.2f);
            GameManager.Instance.HitStop(tuning.hitStop);
            if (Health.IsDead) return;

            float dir = Mathf.Sign(transform.position.x - info.sourcePosition.x);
            if (dir == 0f) dir = -facing;
            velocity = new Vector2(dir * tuning.knockbackX * Mathf.Max(0.5f, info.knockback), tuning.knockbackY);
            hurtTimer = tuning.hurtStun;
            State = PlayerState.Hurt;
            meleeActiveTimer = 0f;
            if (slash != null) slash.enabled = false;
        }

        void UpdateHurt(float dt)
        {
            hurtTimer -= dt;
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, 25f * dt);
            velocity.y = Mathf.Max(velocity.y - tuning.Gravity * tuning.fallGravityMultiplier * dt, -tuning.maxFallSpeed);
            Body.Move(velocity * dt, dropThroughTimer > 0f);
            ResolveContacts();
            if (hurtTimer <= 0f) State = PlayerState.Airborne;
        }

        void OnDied(DamageInfo info)
        {
            State = PlayerState.Dead;
            velocity = Vector2.zero;
            if (slash != null) slash.enabled = false;
            if (GameEvents.PlayerDied != null) GameEvents.PlayerDied();
            GameEvents.PlaySfx(SfxId.PlayerDie);
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            yield return new WaitForSeconds(tuning.deathDelay);
            var mode = GameManager.Instance.Mode;
            if (mode != null) mode.OnPlayerDied();
            else GameManager.Instance.ReloadScene();
        }

        /// <summary>Teleports the player to a position and restores full health.</summary>
        public void Respawn(Vector2 position)
        {
            StopAllCoroutines();
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            velocity = Vector2.zero;
            State = PlayerState.Airborne;
            coyoteTimer = jumpBufferTimer = wallCoyoteTimer = wallLockTimer = 0f;
            dashTimer = dashCooldownTimer = apexTimer = hurtTimer = meleeActiveTimer = 0f;
            rangedEnergy = tuning.rangedMaxEnergy;
            Health.ResetFull();
            Health.SetInvulnerable(1f);
            LastSafePosition = position;
            if (slash != null) slash.enabled = false;
            if (GameEvents.PlayerRespawned != null) GameEvents.PlayerRespawned();
        }

        /// <summary>Used by spikes: put the player back on the last safe ground.</summary>
        public void ReturnToSafePosition()
        {
            transform.position = new Vector3(LastSafePosition.x, LastSafePosition.y + 0.05f, transform.position.z);
            velocity = Vector2.zero;
            if (State != PlayerState.Dead) State = PlayerState.Airborne;
            hurtTimer = 0f;
            Health.SetInvulnerable(0.6f);
        }

        // ------------------------------------------------------------ external impulses

        /// <summary>Launches the player upward (springs, pogo). Refills air jumps and dashes.</summary>
        public void Launch(float verticalSpeed)
        {
            velocity.y = verticalSpeed;
            extraJumpsLeft = tuning.extraJumps;
            dashesLeft = tuning.airDashes;
            coyoteTimer = 0f;
            apexTimer = 0f;
            grounded = false;
            if (State != PlayerState.Dead && State != PlayerState.Dash) State = PlayerState.Airborne;
        }

        // ------------------------------------------------------------ attacks

        void StartMelee(Vector2 move)
        {
            meleeCooldownTimer = tuning.meleeCooldown;
            meleeActiveTimer = tuning.meleeActiveTime;
            meleeHit.Clear();

            if (move.y > 0.6f) meleeDir = Vector2.up;
            else if (move.y < -0.6f && !grounded) meleeDir = Vector2.down;
            else meleeDir = new Vector2(facing, 0f);

            GameEvents.PlaySfx(SfxId.Melee);
        }

        void UpdateMelee(float dt)
        {
            if (meleeActiveTimer <= 0f)
            {
                if (slash != null && slash.enabled) slash.enabled = false;
                return;
            }
            meleeActiveTimer -= dt;

            Vector2 half = new Vector2(0.45f, 0.72f);
            bool vertical = Mathf.Abs(meleeDir.y) > 0.5f;
            Vector2 size = vertical
                ? new Vector2(tuning.meleeThickness, tuning.meleeRange)
                : new Vector2(tuning.meleeRange, tuning.meleeThickness);
            float reach = vertical ? half.y + tuning.meleeRange * 0.5f : half.x + tuning.meleeRange * 0.5f;
            Vector2 center = (Vector2)transform.position + meleeDir * reach;

            if (slash != null)
            {
                slash.enabled = true;
                slash.transform.position = new Vector3(center.x, center.y, transform.position.z);
                slash.transform.localScale = new Vector3(size.x, size.y, 1f);
            }

            bool hitSomething = false;
            Collider2D[] found = Physics2D.OverlapBoxAll(center, size, 0f, meleeMask.value);
            for (int i = 0; i < found.Length; i++)
            {
                Collider2D c = found[i];
                if (c.transform.IsChildOf(transform) || !meleeHit.Add(c)) continue;

                var target = c.GetComponentInParent<IDamageable>();
                if (target != null && target.Team != Team.Player)
                {
                    var info = DamageInfo.Make(tuning.meleeDamage, Team.Player, transform.position, tuning.meleeKnockback);
                    if (target.TakeDamage(info)) hitSomething = true;
                }
                else if (target == null && c.GetComponent<DamageDealer>() != null)
                {
                    hitSomething = true; // bouncing off spikes
                }
            }

            if (hitSomething)
            {
                GameManager.Instance.HitStop(0.03f);
                GameEvents.Shake(0.12f, 0.1f);
                if (meleeDir.y < -0.5f)
                {
                    Launch(tuning.pogoBounce);
                    meleeActiveTimer = 0f;
                }
            }
        }

        void TryShoot()
        {
            if (rangedEnergy < 1f || Projectile.PlayerShots >= tuning.maxProjectiles) return;
            rangedEnergy -= 1f;
            Vector2 dir = new Vector2(facing, 0f);
            Vector2 pos = (Vector2)transform.position + new Vector2(facing * 0.6f, 0.1f);
            Projectile.Spawn(projectilePrefab, pos, dir, Team.Player, tuning.projectileSpeed, tuning.rangedDamage);
            GameEvents.PlaySfx(SfxId.Shoot);
        }

        // ------------------------------------------------------------ visuals

        void UpdateVisual()
        {
            if (visualSprite != null) visualSprite.flipX = facing < 0;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + (Vector3)(new Vector2(facing, 0f) * 1.2f), new Vector3(1.4f, 1.3f, 0f));
        }
    }
}
