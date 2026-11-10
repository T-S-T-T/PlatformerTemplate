using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Straight-line projectile. Dies on solid ground or on hitting an opposing Health.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class Projectile : MonoBehaviour
    {
        /// <summary>Number of live player projectiles (for the on-screen cap).</summary>
        public static int PlayerShots { get; private set; }

        public Team team = Team.Player;
        public int damage = 1;
        public float speed = 16f;
        public float lifetime = 2f;
        public LayerMask solidMask;

        Vector2 direction = Vector2.right;
        float age;
        bool counted;
        bool dead;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { PlayerShots = 0; }

        /// <summary>Spawns a projectile from a prefab, or builds a placeholder one if prefab is null.</summary>
        public static Projectile Spawn(GameObject prefab, Vector2 position, Vector2 dir, Team team, float speed, int damage)
        {
            GameObject go;
            if (prefab != null) go = Instantiate(prefab, position, Quaternion.identity);
            else go = CreateDefault(position, team);

            var p = go.GetComponent<Projectile>();
            p.team = team;
            p.speed = speed;
            p.damage = damage;
            p.direction = dir.normalized;
            if (team == Team.Player) { PlayerShots++; p.counted = true; }
            return p;
        }

        static GameObject CreateDefault(Vector2 position, Team team)
        {
            var go = new GameObject("Projectile");
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSprites.Square;
            sr.color = team == Team.Player ? new Color(1f, 0.9f, 0.3f) : new Color(1f, 0.35f, 0.3f);
            sr.sortingOrder = 20;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
            var p = go.AddComponent<Projectile>();
            p.solidMask = LayerMask.GetMask("Ground");
            return go;
        }

        void Awake()
        {
            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            if (solidMask.value == 0) solidMask = LayerMask.GetMask("Ground");
        }

        void Update()
        {
            if (GameManager.Instance.State != GameState.Playing) return;
            float dt = Time.deltaTime;
            transform.position += (Vector3)(direction * speed * dt);
            age += dt;
            if (age >= lifetime) Kill();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (dead) return;
            var target = other.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                if (target.Team == team) return;
                var info = DamageInfo.Make(damage, team, transform.position, 1f);
                if (target.TakeDamage(info))
                {
                    FxSpawner.Puff(transform.position, 4, GetComponent<SpriteRenderer>() != null ? GetComponent<SpriteRenderer>().color : Color.white, 3f);
                    Kill();
                }
                return;
            }

            if (((solidMask.value >> other.gameObject.layer) & 1) != 0)
            {
                FxSpawner.Puff(transform.position, 3, Color.white, 2f);
                Kill();
            }
        }

        void Kill()
        {
            dead = true;
            if (counted) { PlayerShots = Mathf.Max(0, PlayerShots - 1); counted = false; }
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (counted) { PlayerShots = Mathf.Max(0, PlayerShots - 1); counted = false; }
        }
    }
}
