using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>A coin. The id keeps it collected across death/reload (and across saves in metroidvania mode).</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Collectible : MonoBehaviour
    {
        public string id;
        public int value = 1;
        public int scoreValue = 10;
        public float bobHeight = 0.08f;

        Vector3 start;

        void Start()
        {
            start = transform.position;
            if (GameManager.Instance.IsCollected(id)) gameObject.SetActive(false);
        }

        void Update()
        {
            transform.position = start + Vector3.up * (Mathf.Sin(Time.time * 4f + start.x) * bobHeight);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.IsDead) return;

            var gm = GameManager.Instance;
            gm.AddCoins(value);
            gm.AddScore(scoreValue);
            gm.MarkCollected(id);
            GameEvents.PlaySfx(SfxId.Coin);
            FxSpawner.Puff(transform.position, 5, new Color(1f, 0.9f, 0.3f), 3f, 0.3f, 0.12f);
            Destroy(gameObject);
        }
    }
}
