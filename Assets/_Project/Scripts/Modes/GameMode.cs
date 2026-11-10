using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlatformerTemplate
{
    /// <summary>
    /// Base for the mode layer. A mode decides abilities, spawn, death handling, progression and saving.
    /// The player, combat and world code never check which mode is running; they only call into this class.
    /// </summary>
    public abstract class GameMode : MonoBehaviour
    {
        /// <summary>Collected items and unlocks persist in the save file (metroidvania).</summary>
        public virtual bool PersistProgress { get { return false; } }

        /// <summary>Load SaveSystem.Current from SaveSlot when the mode begins.</summary>
        public virtual bool LoadsSave { get { return false; } }

        public virtual int SaveSlot { get { return 0; } }

        public virtual AbilityFlags StartingAbilities { get { return AbilityFlags.All; } }

        protected PlayerController player;

        // ------------------------------------------------------------ lifecycle

        protected virtual void Awake()
        {
            GameManager.Instance.RegisterMode(this);
        }

        protected virtual void OnEnable()
        {
            GameEvents.EnemyKilled += OnEnemyKilled;
        }

        protected virtual void OnDisable()
        {
            GameEvents.EnemyKilled -= OnEnemyKilled;
        }

        protected virtual void OnDestroy()
        {
            if (GameManager.Exists) GameManager.Instance.UnregisterMode(this);
        }

        protected virtual void Start()
        {
            BeginMode();
        }

        public virtual void BeginMode()
        {
            player = PlayerController.Instance;
            if (player == null)
            {
                Debug.LogError(GetType().Name + ": no PlayerController in the scene.", this);
                return;
            }

            if (LoadsSave) SaveSystem.Load(SaveSlot);
            OnProgressLoaded();

            player.Abilities.Set(StartingAbilities);
            player.Respawn(ResolveSpawn());
            GameManager.Instance.SetState(GameState.Playing);
            SnapCamera();
        }

        // ------------------------------------------------------------ hooks for subclasses

        protected virtual void OnProgressLoaded() { }

        public virtual void OnCheckpoint(Vector2 position) { }

        public virtual void OnAbilityUnlocked(AbilityFlags flags) { }

        public virtual void OnLevelExit() { }

        public virtual void OnBossDefeated() { }

        protected virtual void OnEnemyKilled(Enemy enemy)
        {
            if (enemy != null && enemy.data != null) GameManager.Instance.AddScore(enemy.data.scoreValue);
        }

        /// <summary>Called by the player a moment after dying. Default: fade out, respawn at checkpoint, fade in.</summary>
        public virtual void OnPlayerDied()
        {
            StartCoroutine(RespawnRoutine());
        }

        // ------------------------------------------------------------ helpers

        protected Vector2 ResolveSpawn()
        {
            var gm = GameManager.Instance;
            if (gm.HasCheckpoint && gm.CheckpointScene == SceneManager.GetActiveScene().name)
                return gm.CheckpointPosition;

            var spawn = FindFirstObjectByType<PlayerSpawn>();
            if (spawn != null) return spawn.transform.position;
            return player != null ? (Vector2)player.transform.position : Vector2.zero;
        }

        protected void SnapCamera()
        {
            var cam = FindFirstObjectByType<CameraFollow>();
            if (cam != null) cam.SnapToTarget();
        }

        protected IEnumerator RespawnRoutine()
        {
            var ui = UIManager.Instance;
            if (ui != null) yield return ui.FadeTo(1f, 0.2f);
            player.Respawn(ResolveSpawn());
            SnapCamera();
            if (ui != null) yield return ui.FadeTo(0f, 0.2f);
        }
    }
}
