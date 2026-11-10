using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlatformerTemplate
{
    /// <summary>
    /// Persistent root. Created automatically before the first scene loads, so any scene can be played directly.
    /// Owns game state, the active game mode, run stats, checkpoints and scene loading.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public const string MenuScene = "MainMenu";

        static GameManager instance;

        public static GameManager Instance
        {
            get
            {
                if (instance == null) CreateSystems();
                return instance;
            }
        }

        public static bool Exists { get { return instance != null; } }

        public GameState State { get; private set; } = GameState.Playing;
        public GameMode Mode { get; private set; }
        public int Coins { get; private set; }
        public int Score { get; private set; }

        public string CheckpointScene { get; private set; }
        public Vector2 CheckpointPosition { get; private set; }
        public bool HasCheckpoint { get { return !string.IsNullOrEmpty(CheckpointScene); } }

        readonly HashSet<string> sessionCollected = new HashSet<string>();
        bool loading;
        Coroutine hitStopRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (instance == null) CreateSystems();
        }

        static void CreateSystems()
        {
            var root = new GameObject("[Platformer Systems]");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<GameManager>();
            GameSettings.Load();
            GameSettings.Apply();
            root.AddComponent<AudioManager>();
            root.AddComponent<UIManager>();
            if (Application.isEditor || Debug.isDebugBuild) root.AddComponent<DebugOverlay>();
        }

        // ---------------------------------------------------------------- state

        public void SetState(GameState newState)
        {
            if (State == newState) return;
            State = newState;
            Time.timeScale = newState == GameState.Paused ? 0f : 1f;
            if (GameEvents.StateChanged != null) GameEvents.StateChanged(newState);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
            else if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void RegisterMode(GameMode mode) { Mode = mode; }
        public void UnregisterMode(GameMode mode) { if (Mode == mode) Mode = null; }

        // ---------------------------------------------------------------- run stats

        public void ResetRun()
        {
            Coins = 0;
            Score = 0;
            ClearCheckpoint();
            sessionCollected.Clear();
            if (GameEvents.CoinsChanged != null) GameEvents.CoinsChanged(Coins);
            if (GameEvents.ScoreChanged != null) GameEvents.ScoreChanged(Score);
        }

        public void AddCoins(int n)
        {
            Coins += n;
            if (GameEvents.CoinsChanged != null) GameEvents.CoinsChanged(Coins);
        }

        public void AddScore(int n)
        {
            Score += n;
            if (GameEvents.ScoreChanged != null) GameEvents.ScoreChanged(Score);
        }

        public void SetScore(int value)
        {
            Score = value;
            if (GameEvents.ScoreChanged != null) GameEvents.ScoreChanged(Score);
        }

        // ---------------------------------------------------------------- collectibles

        public bool IsCollected(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (Mode != null && Mode.PersistProgress) return SaveSystem.Current.collected.Contains(id);
            return sessionCollected.Contains(id);
        }

        public void MarkCollected(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (Mode != null && Mode.PersistProgress)
            {
                if (!SaveSystem.Current.collected.Contains(id)) SaveSystem.Current.collected.Add(id);
            }
            else sessionCollected.Add(id);
        }

        // ---------------------------------------------------------------- checkpoints

        public void SetCheckpoint(Vector2 position)
        {
            CheckpointScene = SceneManager.GetActiveScene().name;
            CheckpointPosition = position;
            if (GameEvents.CheckpointReached != null) GameEvents.CheckpointReached(position);
            if (Mode != null) Mode.OnCheckpoint(position);
        }

        /// <summary>Restores a checkpoint (e.g. from a save file) without raising events or saving.</summary>
        public void RestoreCheckpoint(string scene, Vector2 position)
        {
            CheckpointScene = scene;
            CheckpointPosition = position;
        }

        public void ClearCheckpoint()
        {
            CheckpointScene = null;
        }

        // ---------------------------------------------------------------- scenes

        public void LoadScene(string sceneName)
        {
            if (loading) return;
            StartCoroutine(LoadRoutine(sceneName));
        }

        public void ReloadScene()
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }

        public void QuitToMenu()
        {
            ResetRun();
            LoadScene(MenuScene);
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            loading = true;
            var ui = UIManager.Instance;
            if (ui != null) yield return ui.FadeTo(1f, 0.25f);
            SetState(GameState.Playing);
            Time.timeScale = 1f;
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone) yield return null;
            yield return null;
            if (ui != null) yield return ui.FadeTo(0f, 0.25f);
            loading = false;
        }

        // ---------------------------------------------------------------- feel

        public void HitStop(float seconds)
        {
            if (!GameSettings.HitStop || seconds <= 0f || State != GameState.Playing) return;
            if (hitStopRoutine != null) StopCoroutine(hitStopRoutine);
            hitStopRoutine = StartCoroutine(HitStopRoutine(seconds));
        }

        IEnumerator HitStopRoutine(float seconds)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(seconds);
            if (State == GameState.Playing) Time.timeScale = 1f;
            hitStopRoutine = null;
        }

        void OnApplicationQuit()
        {
            Time.timeScale = 1f;
        }
    }
}
