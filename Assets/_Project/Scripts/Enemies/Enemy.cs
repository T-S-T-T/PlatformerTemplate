using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>
    /// Base for all enemies: health wiring, hurt/knockback, death, gravity movement, player awareness.
    /// Subclasses only implement Think(). Tuning lives in an <see cref="EnemyData"/> asset.
    /// </summary>
    [RequireComponent(typeof(KinematicBody2D), typeof(Health))]
    public abstract class Enemy : MonoBehaviour
    {
        public EnemyData data;

        protected KinematicBody2D body;
        protected Health health;
        protected SpriteRenderer sprite;
        protected HitFlash flash;
        protected Vector2 velocity;
        protected int facing = 1;
        protected float hurtTimer;
        protected float cooldown;
        protected Color baseColor = Color.white;

        public bool IsDead { get { return health != null && health.IsDead; } }

        protected PlayerController Player { get { return PlayerController.Instance; } }

        protected bool PlayerAlive
        {
            get { var p = PlayerController.Instance; return p != null && !p.IsDead; }
        }

        protected virtual void Awake()
        {
            body = GetComponent<KinematicBody2D>();
            health = GetComponent<Health>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            flash = GetComponent<HitFlash>();
            if (data == null) data = ScriptableObject.CreateInstance<EnemyData>();
            if (sprite != null) baseColor = sprite.color;

            health.team = Team.Enemy;
            health.Initialize(data.maxHealth);
            health.Damaged += OnDamaged;
            health.Died += OnDied;

            var dealers = GetComponentsInChildren<DamageDealer>();
            for (int i = 0; i < dealers.Length; i++)
            {
                dealers[i].team = Team.Enemy;
                dealers[i].damage = data.contactDamage;
                dealers[i].knockback = data.contactKnockback;
            }
        }

        protected virtual void OnDestroy()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        protected virtual void Update()
        {
            if (GameManager.Instance.State != GameState.Playing) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f || health.IsDead) return;

            if (cooldown > 0f) cooldown -= dt;
            if (hurtTimer > 0f)
            {
                hurtTimer -= dt;
                UpdateHurt(dt);
                return;
            }
            Think(dt);
            if (sprite != null) sprite.flipX = facing < 0;
        }

        protected abstract void Think(float dt);

        protected virtual void UpdateHurt(float dt)
        {
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, 20f * dt);
            ApplyGravity(dt);
            MoveBody(dt);
        }

        // ------------------------------------------------------------ movement helpers

        protected void ApplyGravity(float dt)
        {
            velocity.y = Mathf.Max(velocity.y - data.gravity * dt, -data.maxFallSpeed);
        }

        protected void MoveBody(float dt)
        {
            body.Move(velocity * dt);
            if (body.Below && velocity.y < 0f) velocity.y = 0f;
            if (body.Above && velocity.y > 0f) velocity.y = 0f;
            if ((body.Left && velocity.x < 0f) || (body.Right && velocity.x > 0f)) velocity.x = 0f;
        }

        // ------------------------------------------------------------ awareness helpers

        protected Vector2 ToPlayer
        {
            get { return PlayerAlive ? (Vector2)Player.transform.position - (Vector2)transform.position : Vector2.zero; }
        }

        protected int DirToPlayer()
        {
            return ToPlayer.x >= 0f ? 1 : -1;
        }

        protected bool PlayerInRange(float range)
        {
            return PlayerAlive && ToPlayer.magnitude <= range;
        }

        protected bool HasLineOfSight()
        {
            if (!PlayerAlive) return false;
            return Physics2D.Linecast(transform.position, Player.transform.position, body.solidMask.value).collider == null;
        }

        protected void Fire(Vector2 dir)
        {
            Projectile.Spawn(data.projectilePrefab, transform.position, dir, Team.Enemy, data.projectileSpeed, data.contactDamage);
            GameEvents.PlaySfx(SfxId.Shoot);
        }

        protected void Tint(Color c)
        {
            if (flash != null) flash.SetBaseColor(c);
            else if (sprite != null) sprite.color = c;
        }

        // ------------------------------------------------------------ health events

        protected virtual void OnDamaged(DamageInfo info)
        {
            if (flash != null) flash.Flash(0.1f);
            GameEvents.PlaySfx(SfxId.EnemyHit);
            FxSpawner.Puff(transform.position, 4, Color.white, 3f, 0.25f, 0.14f);
            if (health.IsDead) return;

            float dir = Mathf.Sign(transform.position.x - info.sourcePosition.x);
            if (dir == 0f) dir = 1f;
            velocity = new Vector2(dir * data.hurtKnockback.x * info.knockback, data.hurtKnockback.y);
            hurtTimer = data.hurtStun;
            Tint(baseColor);
        }

        protected virtual void OnDied(DamageInfo info)
        {
            if (GameEvents.EnemyKilled != null) GameEvents.EnemyKilled(this);
            GameEvents.PlaySfx(SfxId.EnemyDie);
            GameEvents.Shake(0.12f, 0.12f);
            FxSpawner.Puff(transform.position, 12, baseColor, 5f, 0.5f, 0.22f);
            Destroy(gameObject);
        }
    }
}
