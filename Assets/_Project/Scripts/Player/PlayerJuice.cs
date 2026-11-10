using System.Collections;
using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>
    /// Game-feel layer for the player: squash and stretch, dust, dash trail, death burst.
    /// Purely cosmetic; listens to GameEvents. Each effect can be switched off.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerJuice : MonoBehaviour
    {
        public bool squashAndStretch = true;
        public bool dust = true;
        public bool dashTrail = true;
        public bool deathBurst = true;
        public Color dustColor = new Color(1f, 1f, 1f, 0.55f);
        public Color trailColor = new Color(0.4f, 0.9f, 1f, 0.6f);

        PlayerController pc;
        Vector3 baseScale = Vector3.one;
        Vector3 baseLocalPos = Vector3.zero;
        SpriteRenderer sr;
        Coroutine squashRoutine;
        float trailTimer;
        float slideDustTimer;

        void Awake()
        {
            pc = GetComponent<PlayerController>();
        }

        void Start()
        {
            if (pc.visual != null)
            {
                baseScale = pc.visual.localScale;
                baseLocalPos = pc.visual.localPosition;
                sr = pc.visual.GetComponent<SpriteRenderer>();
            }
        }

        void OnEnable()
        {
            GameEvents.PlayerJumped += OnJump;
            GameEvents.PlayerDoubleJumped += OnDoubleJump;
            GameEvents.PlayerWallJumped += OnWallJump;
            GameEvents.PlayerLanded += OnLand;
            GameEvents.PlayerDashed += OnDash;
            GameEvents.PlayerDied += OnDied;
            GameEvents.PlayerRespawned += OnRespawned;
        }

        void OnDisable()
        {
            GameEvents.PlayerJumped -= OnJump;
            GameEvents.PlayerDoubleJumped -= OnDoubleJump;
            GameEvents.PlayerWallJumped -= OnWallJump;
            GameEvents.PlayerLanded -= OnLand;
            GameEvents.PlayerDashed -= OnDash;
            GameEvents.PlayerDied -= OnDied;
            GameEvents.PlayerRespawned -= OnRespawned;
        }

        float FeetY { get { return transform.position.y - 0.7f; } }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (dashTrail && pc.State == PlayerState.Dash && sr != null)
            {
                trailTimer -= dt;
                if (trailTimer <= 0f)
                {
                    trailTimer = 0.03f;
                    FxSpawner.Ghost(sr.sprite, pc.visual.position, pc.visual.lossyScale, sr.flipX, trailColor, 0.22f);
                }
            }

            if (dust && pc.State == PlayerState.WallSlide)
            {
                slideDustTimer -= dt;
                if (slideDustTimer <= 0f)
                {
                    slideDustTimer = 0.08f;
                    Vector2 p = new Vector2(transform.position.x + pc.Facing * 0.4f, transform.position.y);
                    FxSpawner.Directional(p, Vector2.up, 1, dustColor, 1.2f, 0.3f, 0.12f);
                }
            }
        }

        void OnJump(Vector2 pos)
        {
            Squash(new Vector2(0.78f, 1.25f), 0.18f);
            if (dust) FxSpawner.Directional(new Vector2(pos.x, FeetY), Vector2.up, 4, dustColor, 1.5f);
        }

        void OnDoubleJump(Vector2 pos)
        {
            Squash(new Vector2(0.8f, 1.22f), 0.16f);
            if (dust) FxSpawner.Puff(new Vector2(pos.x, FeetY), 6, dustColor, 2.2f);
        }

        void OnWallJump(Vector2 pos, int away)
        {
            Squash(new Vector2(0.8f, 1.2f), 0.16f);
            if (dust) FxSpawner.Directional(new Vector2(pos.x - away * 0.4f, pos.y), new Vector2(away, 0.3f), 4, dustColor, 2f);
        }

        void OnLand(Vector2 pos, float impact)
        {
            float k = Mathf.Clamp01(impact / 18f);
            if (k > 0.1f) Squash(new Vector2(Mathf.Lerp(1.08f, 1.35f, k), Mathf.Lerp(0.92f, 0.65f, k)), 0.2f);
            if (dust && k > 0.15f)
            {
                var feet = new Vector2(pos.x, FeetY);
                FxSpawner.Directional(feet, Vector2.right, 3, dustColor, 2.5f);
                FxSpawner.Directional(feet, Vector2.left, 3, dustColor, 2.5f);
            }
        }

        void OnDash(Vector2 pos, Vector2 dir)
        {
            Squash(Mathf.Abs(dir.x) > 0.1f ? new Vector2(1.35f, 0.75f) : new Vector2(0.75f, 1.35f), 0.2f);
            if (dust) FxSpawner.Directional(pos, -dir, 5, dustColor, 3f);
        }

        void OnDied()
        {
            if (deathBurst) FxSpawner.Puff(transform.position, 14, new Color(1f, 0.45f, 0.4f, 1f), 6f, 0.6f, 0.25f);
            if (sr != null) sr.enabled = false;
        }

        void OnRespawned()
        {
            if (sr != null) sr.enabled = true;
            ApplyScale(Vector2.one);
        }

        void Squash(Vector2 factor, float duration)
        {
            if (!squashAndStretch || pc.visual == null) return;
            if (squashRoutine != null) StopCoroutine(squashRoutine);
            squashRoutine = StartCoroutine(SquashRoutine(factor, duration));
        }

        IEnumerator SquashRoutine(Vector2 factor, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                ApplyScale(Vector2.Lerp(factor, Vector2.one, k));
                yield return null;
            }
            ApplyScale(Vector2.one);
            squashRoutine = null;
        }

        // Scales the sprite while keeping its feet planted.
        void ApplyScale(Vector2 f)
        {
            if (pc.visual == null) return;
            pc.visual.localScale = new Vector3(baseScale.x * f.x, baseScale.y * f.y, baseScale.z);
            pc.visual.localPosition = baseLocalPos + new Vector3(0f, baseScale.y * (f.y - 1f) * 0.5f, 0f);
        }
    }
}
