using System.Collections.Generic;
using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>
    /// Endless runner mode. Stitches prefab chunks together ahead of the player, gets harder with distance,
    /// ends the run on death and keeps a best score.
    /// </summary>
    public class EndlessMode : GameMode
    {
        public const string BestScoreKey = "endless_best";

        [Tooltip("All chunk prefabs. Exactly one (or more) should be marked isStartChunk.")]
        public EndlessChunk[] chunkPrefabs;
        public AbilityFlags abilities = AbilityFlags.All;
        public float spawnAhead = 50f;
        public float despawnBehind = 40f;
        public float killY = -10f;
        [Tooltip("Distance (units) per +1 of maximum chunk difficulty.")]
        public float unitsPerDifficulty = 120f;

        public override AbilityFlags StartingAbilities { get { return abilities; } }

        readonly List<EndlessChunk> active = new List<EndlessChunk>();
        EndlessChunk last;
        float cursorX;
        float startX;
        int bonus;
        bool over;

        public override void BeginMode()
        {
            base.BeginMode();
            if (player == null) return;

            over = false;
            bonus = 0;
            cursorX = 0f;
            startX = player.transform.position.x;
            GameManager.Instance.SetScore(0);

            if (chunkPrefabs == null || chunkPrefabs.Length == 0)
            {
                Debug.LogError("EndlessMode: no chunk prefabs assigned.", this);
                return;
            }

            EndlessChunk start = null;
            for (int i = 0; i < chunkPrefabs.Length; i++)
                if (chunkPrefabs[i] != null && chunkPrefabs[i].isStartChunk) { start = chunkPrefabs[i]; break; }
            if (start == null) start = chunkPrefabs[0];
            SpawnChunk(start);
        }

        void Update()
        {
            if (over || player == null || chunkPrefabs == null || chunkPrefabs.Length == 0) return;
            if (GameManager.Instance.State != GameState.Playing) return;

            float px = player.transform.position.x;
            int guard = 0;
            while (cursorX < px + spawnAhead && guard++ < 8) SpawnNext();
            Cleanup(px);

            int distance = Mathf.Max(0, Mathf.FloorToInt(px - startX));
            GameManager.Instance.SetScore(distance + bonus + GameManager.Instance.Coins * 10);

            if (!player.IsDead && player.transform.position.y < killY)
            {
                player.Health.TakeDamage(new DamageInfo
                {
                    amount = 99, team = Team.Neutral, sourcePosition = player.transform.position, instantKill = true
                });
            }
        }

        void SpawnNext()
        {
            int maxDifficulty = 1 + Mathf.FloorToInt(Mathf.Max(0f, cursorX - startX) / Mathf.Max(1f, unitsPerDifficulty));

            var candidates = new List<EndlessChunk>();
            for (int i = 0; i < chunkPrefabs.Length; i++)
            {
                var c = chunkPrefabs[i];
                if (c == null || c.isStartChunk || c.difficulty > maxDifficulty) continue;
                if (c == last && chunkPrefabs.Length > 2) continue;
                candidates.Add(c);
            }
            if (candidates.Count == 0) candidates.Add(chunkPrefabs[Random.Range(0, chunkPrefabs.Length)]);
            SpawnChunk(candidates[Random.Range(0, candidates.Count)]);
        }

        void SpawnChunk(EndlessChunk prefab)
        {
            EndlessChunk chunk = Instantiate(prefab, new Vector3(cursorX, 0f, 0f), Quaternion.identity, transform);
            active.Add(chunk);
            last = prefab;
            cursorX += prefab.width;
            Physics2D.SyncTransforms();
        }

        void Cleanup(float playerX)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i] == null) { active.RemoveAt(i); continue; }
                if (active[i].transform.position.x + active[i].width < playerX - despawnBehind)
                {
                    Destroy(active[i].gameObject);
                    active.RemoveAt(i);
                }
            }
        }

        protected override void OnEnemyKilled(Enemy enemy)
        {
            if (enemy != null && enemy.data != null) bonus += enemy.data.scoreValue;
        }

        public override void OnPlayerDied()
        {
            if (over) return;
            over = true;

            int score = GameManager.Instance.Score;
            int best = PlayerPrefs.GetInt(BestScoreKey, 0);
            bool newBest = score > best;
            if (newBest)
            {
                best = score;
                PlayerPrefs.SetInt(BestScoreKey, best);
                PlayerPrefs.Save();
            }

            string text = "Score: " + score + (newBest ? "   NEW BEST!" : "") + "\nBest: " + best;
            UIManager.Instance.ShowGameOver(text);
        }
    }
}
