using System.Collections.Generic;
using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Pooled sprite particles (dust, hit sparks, dash ghosts). No materials or particle systems needed.</summary>
    public static class FxSpawner
    {
        static readonly Stack<FxParticle> pool = new Stack<FxParticle>();
        static Transform holder;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            pool.Clear();
            holder = null;
        }

        static Transform Holder
        {
            get
            {
                if (holder == null)
                {
                    var go = new GameObject("[FX]");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    holder = go.transform;
                }
                return holder;
            }
        }

        static FxParticle Get()
        {
            while (pool.Count > 0)
            {
                FxParticle p = pool.Pop();
                if (p != null) return p;
            }
            var go = new GameObject("Fx");
            go.transform.SetParent(Holder, false);
            return go.AddComponent<FxParticle>();
        }

        internal static void Return(FxParticle p)
        {
            p.gameObject.SetActive(false);
            pool.Push(p);
        }

        /// <summary>Burst of small squares flying outward (or in a direction if given).</summary>
        public static void Puff(Vector2 position, int count, Color color, float speed = 3f, float life = 0.35f, float size = 0.18f)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 v = Random.insideUnitCircle.normalized * Random.Range(0.3f, 1f) * speed;
                Get().Init(PlaceholderSprites.Square, position, Vector3.one * size * Random.Range(0.6f, 1.2f), false, v, color, life * Random.Range(0.7f, 1.2f), true);
            }
        }

        /// <summary>Puff that travels along a direction (landing dust, wall-slide dust).</summary>
        public static void Directional(Vector2 position, Vector2 direction, int count, Color color, float speed = 3f, float life = 0.3f, float size = 0.16f)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 spread = Random.insideUnitCircle * 0.35f;
                Vector2 v = (direction.normalized + spread) * speed * Random.Range(0.5f, 1f);
                Get().Init(PlaceholderSprites.Square, position, Vector3.one * size * Random.Range(0.6f, 1.2f), false, v, color, life * Random.Range(0.7f, 1.2f), true);
            }
        }

        /// <summary>A fading copy of a sprite (dash afterimage).</summary>
        public static void Ghost(Sprite sprite, Vector3 position, Vector3 scale, bool flipX, Color color, float life = 0.2f)
        {
            Get().Init(sprite, position, scale, flipX, Vector2.zero, color, life, false);
        }
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public class FxParticle : MonoBehaviour
    {
        SpriteRenderer sr;
        Vector2 velocity;
        Vector3 baseScale;
        Color baseColor;
        float life, age;
        bool shrink;

        void Awake() { sr = GetComponent<SpriteRenderer>(); }

        public void Init(Sprite sprite, Vector3 position, Vector3 scale, bool flipX, Vector2 vel, Color color, float lifetime, bool shrinkOverTime)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.flipX = flipX;
            sr.color = color;
            sr.sortingOrder = 30;
            transform.position = position;
            transform.localScale = scale;
            velocity = vel;
            baseScale = scale;
            baseColor = color;
            life = Mathf.Max(0.01f, lifetime);
            age = 0f;
            shrink = shrinkOverTime;
            gameObject.SetActive(true);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            float t = age / life;
            if (t >= 1f) { FxSpawner.Return(this); return; }

            transform.position += (Vector3)(velocity * dt);
            velocity *= Mathf.Max(0f, 1f - 5f * dt);
            Color c = baseColor;
            c.a = baseColor.a * (1f - t);
            sr.color = c;
            if (shrink) transform.localScale = baseScale * (1f - t);
        }
    }
}
