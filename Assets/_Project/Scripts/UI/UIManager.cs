using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PlatformerTemplate
{
    /// <summary>
    /// Persistent UI built entirely in code: HUD, pause, settings, game over, level complete, toasts and the
    /// scene fade. Panels follow GameManager.State. Menu scenes add their own panels under <see cref="Root"/>.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        public RectTransform Root { get; private set; }

        Canvas canvas;
        Image fade;
        float fadeAlpha;

        GameObject hud, pausePanel, gameOverPanel, levelCompletePanel, settingsPanel;
        RectTransform heartRow;
        readonly List<Image> hearts = new List<Image>();
        Text infoText, abilityText, toastText, gameOverInfo, levelInfo;
        Button nextButton;
        Action nextAction;
        Action settingsOnClose;
        float toastTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            Instance = this;
            BuildCanvases();
            BuildHud();
            BuildPause();
            BuildGameOver();
            BuildLevelComplete();
            BuildSettings();
            EnsureEventSystem();

            GameEvents.StateChanged += OnStateChanged;
            GameEvents.PlayerHealthChanged += OnHealthChanged;
            GameEvents.CoinsChanged += OnStatsChanged;
            GameEvents.ScoreChanged += OnStatsChanged;
            GameEvents.AbilitiesChanged += OnAbilitiesChanged;

            OnStateChanged(GameManager.Instance.State);
            RefreshInfo();
        }

        void OnDestroy()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.PlayerHealthChanged -= OnHealthChanged;
            GameEvents.CoinsChanged -= OnStatsChanged;
            GameEvents.ScoreChanged -= OnStatsChanged;
            GameEvents.AbilitiesChanged -= OnAbilitiesChanged;
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------ building

        void BuildCanvases()
        {
            var go = new GameObject("UI Canvas", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            Root = go.GetComponent<RectTransform>();

            // Separate canvas so the fade always sits above every panel.
            var fadeGo = new GameObject("Fade Canvas", typeof(RectTransform));
            fadeGo.transform.SetParent(transform, false);
            var fadeCanvas = fadeGo.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 1000;
            fadeGo.AddComponent<GraphicRaycaster>();
            fade = UIFactory.MakeImage(fadeGo.transform, "Fade", new Color(0f, 0f, 0f, 0f));
            UIFactory.Stretch(fade.rectTransform);
        }

        void BuildHud()
        {
            hud = UIFactory.Create("HUD", Root);
            UIFactory.Stretch(hud.GetComponent<RectTransform>());

            // Hearts (top-left)
            var heartGo = UIFactory.Create("Hearts", hud.transform);
            heartRow = heartGo.GetComponent<RectTransform>();
            heartRow.anchorMin = heartRow.anchorMax = heartRow.pivot = new Vector2(0f, 1f);
            heartRow.anchoredPosition = new Vector2(24f, -24f);
            heartRow.sizeDelta = new Vector2(400f, 34f);
            var hl = heartGo.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 6f;
            hl.childAlignment = TextAnchor.UpperLeft;
            hl.childControlWidth = false;
            hl.childControlHeight = false;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = false;

            // Coins / score (top-right)
            infoText = UIFactory.MakeLabel(hud.transform, "", 24, TextAnchor.UpperRight, UIFactory.TextColor);
            var ir = infoText.rectTransform;
            ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(1f, 1f);
            ir.anchoredPosition = new Vector2(-24f, -20f);
            ir.sizeDelta = new Vector2(360f, 70f);

            // Abilities (bottom-left)
            abilityText = UIFactory.MakeLabel(hud.transform, "", 16, TextAnchor.LowerLeft, new Color(0.7f, 0.75f, 0.85f));
            var ar = abilityText.rectTransform;
            ar.anchorMin = ar.anchorMax = ar.pivot = new Vector2(0f, 0f);
            ar.anchoredPosition = new Vector2(24f, 18f);
            ar.sizeDelta = new Vector2(700f, 30f);

            // Toast (top-centre)
            toastText = UIFactory.MakeLabel(hud.transform, "", 28, TextAnchor.MiddleCenter, UIFactory.AccentColor);
            var tr = toastText.rectTransform;
            tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0.5f, 1f);
            tr.anchoredPosition = new Vector2(0f, -90f);
            tr.sizeDelta = new Vector2(700f, 44f);
        }

        void BuildPause()
        {
            var col = UIFactory.MakeMenu(Root, "Pause Panel", "PAUSED", 380f, out pausePanel);
            UIFactory.MakeButton(col, "Resume", () => GameManager.Instance.SetState(GameState.Playing));
            UIFactory.MakeButton(col, "Settings", () =>
            {
                pausePanel.SetActive(false);
                OpenSettings(() => { pausePanel.SetActive(true); SelectFirst(pausePanel); });
            });
            UIFactory.MakeButton(col, "Restart", () =>
            {
                GameManager.Instance.SetState(GameState.Playing);
                GameManager.Instance.ReloadScene();
            });
            UIFactory.MakeButton(col, "Main Menu", () => GameManager.Instance.QuitToMenu());
            pausePanel.SetActive(false);
        }

        void BuildGameOver()
        {
            var col = UIFactory.MakeMenu(Root, "Game Over Panel", "GAME OVER", 420f, out gameOverPanel);
            gameOverInfo = UIFactory.MakeLabel(col, "", 26, TextAnchor.MiddleCenter, UIFactory.TextColor);
            gameOverInfo.gameObject.AddComponent<LayoutElement>().minHeight = 80f;
            UIFactory.MakeButton(col, "Retry", () =>
            {
                GameManager.Instance.ResetRun();
                GameManager.Instance.SetState(GameState.Playing);
                GameManager.Instance.ReloadScene();
            });
            UIFactory.MakeButton(col, "Main Menu", () => GameManager.Instance.QuitToMenu());
            gameOverPanel.SetActive(false);
        }

        void BuildLevelComplete()
        {
            var col = UIFactory.MakeMenu(Root, "Level Complete Panel", "LEVEL COMPLETE", 440f, out levelCompletePanel);
            levelInfo = UIFactory.MakeLabel(col, "", 24, TextAnchor.MiddleCenter, UIFactory.TextColor);
            levelInfo.gameObject.AddComponent<LayoutElement>().minHeight = 60f;
            nextButton = UIFactory.MakeButton(col, "Next Level", () =>
            {
                if (nextAction != null) nextAction();
            });
            UIFactory.MakeButton(col, "Replay Level", () =>
            {
                GameManager.Instance.SetState(GameState.Playing);
                GameManager.Instance.ReloadScene();
            });
            UIFactory.MakeButton(col, "Main Menu", () => GameManager.Instance.QuitToMenu());
            levelCompletePanel.SetActive(false);
        }

        void BuildSettings()
        {
            var col = UIFactory.MakeMenu(Root, "Settings Panel", "SETTINGS", 480f, out settingsPanel);
            UIFactory.MakeStepper(col, "Master volume", () => Mathf.RoundToInt(GameSettings.MasterVolume * 100f) + "%",
                () => { GameSettings.MasterVolume = Mathf.Clamp01(GameSettings.MasterVolume - 0.1f); GameSettings.Apply(); },
                () => { GameSettings.MasterVolume = Mathf.Clamp01(GameSettings.MasterVolume + 0.1f); GameSettings.Apply(); });
            UIFactory.MakeStepper(col, "Music volume", () => Mathf.RoundToInt(GameSettings.MusicVolume * 100f) + "%",
                () => GameSettings.MusicVolume = Mathf.Clamp01(GameSettings.MusicVolume - 0.1f),
                () => GameSettings.MusicVolume = Mathf.Clamp01(GameSettings.MusicVolume + 0.1f));
            UIFactory.MakeStepper(col, "SFX volume", () => Mathf.RoundToInt(GameSettings.SfxVolume * 100f) + "%",
                () => GameSettings.SfxVolume = Mathf.Clamp01(GameSettings.SfxVolume - 0.1f),
                () => GameSettings.SfxVolume = Mathf.Clamp01(GameSettings.SfxVolume + 0.1f));
            UIFactory.MakeStepper(col, "Screen shake", () => Mathf.RoundToInt(GameSettings.ScreenShake * 100f) + "%",
                () => GameSettings.ScreenShake = Mathf.Clamp(GameSettings.ScreenShake - 0.25f, 0f, 1f),
                () => GameSettings.ScreenShake = Mathf.Clamp(GameSettings.ScreenShake + 0.25f, 0f, 1f));
            UIFactory.MakeToggle(col, "Hit-stop", () => GameSettings.HitStop, v => GameSettings.HitStop = v);
            UIFactory.MakeToggle(col, "VSync", () => GameSettings.VSync, v => { GameSettings.VSync = v; GameSettings.Apply(); });
            UIFactory.MakeToggle(col, "Fullscreen", () => GameSettings.Fullscreen, v => { GameSettings.Fullscreen = v; GameSettings.ApplyFullscreen(); });
            UIFactory.MakeButton(col, "Back", CloseSettings);
            settingsPanel.SetActive(false);
        }

        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.transform.SetParent(transform, false);
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        // ------------------------------------------------------------ public API

        public IEnumerator FadeTo(float target, float duration)
        {
            float start = fadeAlpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                fadeAlpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
                ApplyFade();
                yield return null;
            }
            fadeAlpha = target;
            ApplyFade();
        }

        void ApplyFade()
        {
            Color c = fade.color;
            c.a = fadeAlpha;
            fade.color = c;
            fade.raycastTarget = fadeAlpha > 0.01f;
        }

        public void ShowToast(string message)
        {
            toastText.text = message;
            toastTimer = 2.2f;
        }

        public void ShowGameOver(string info)
        {
            gameOverInfo.text = info;
            GameManager.Instance.SetState(GameState.GameOver);
        }

        public void ShowLevelComplete(string summary, Action onNext)
        {
            levelInfo.text = summary;
            nextAction = onNext;
            nextButton.gameObject.SetActive(onNext != null);
            GameManager.Instance.SetState(GameState.LevelComplete);
        }

        public void OpenSettings(Action onClose)
        {
            settingsOnClose = onClose;
            settingsPanel.SetActive(true);
            SelectFirst(settingsPanel);
        }

        void CloseSettings()
        {
            settingsPanel.SetActive(false);
            GameSettings.Save();
            Action cb = settingsOnClose;
            settingsOnClose = null;
            if (cb != null) cb();
        }

        public static void SelectFirst(GameObject panel)
        {
            if (panel == null || EventSystem.current == null) return;
            var b = panel.GetComponentInChildren<Button>();
            if (b != null) EventSystem.current.SetSelectedGameObject(b.gameObject);
        }

        // ------------------------------------------------------------ events

        void OnStateChanged(GameState state)
        {
            bool settingsOpen = settingsPanel != null && settingsPanel.activeSelf;
            pausePanel.SetActive(state == GameState.Paused && !settingsOpen);
            if (state != GameState.Paused && settingsOpen && settingsOnClose != null)
            {
                settingsPanel.SetActive(false);
                settingsOnClose = null;
            }
            gameOverPanel.SetActive(state == GameState.GameOver);
            levelCompletePanel.SetActive(state == GameState.LevelComplete);

            if (state == GameState.Paused) SelectFirst(pausePanel);
            else if (state == GameState.GameOver) SelectFirst(gameOverPanel);
            else if (state == GameState.LevelComplete) SelectFirst(levelCompletePanel);
        }

        void OnHealthChanged(int current, int max)
        {
            while (hearts.Count < max)
            {
                var img = UIFactory.MakeImage(heartRow, "Heart", Color.red);
                var le = img.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 30f;
                le.preferredHeight = 30f;
                hearts.Add(img);
            }
            while (hearts.Count > max)
            {
                Destroy(hearts[hearts.Count - 1].gameObject);
                hearts.RemoveAt(hearts.Count - 1);
            }
            for (int i = 0; i < hearts.Count; i++)
                hearts[i].color = i < current ? new Color(0.95f, 0.25f, 0.3f) : new Color(0.25f, 0.18f, 0.2f);
        }

        void OnStatsChanged(int value) { RefreshInfo(); }

        void RefreshInfo()
        {
            var gm = GameManager.Instance;
            infoText.text = "Coins  " + gm.Coins + "\nScore  " + gm.Score;
        }

        void OnAbilitiesChanged(AbilityFlags flags) { RefreshAbilityText(); }

        void RefreshAbilityText()
        {
            var p = PlayerController.Instance;
            if (p == null) { abilityText.text = ""; return; }
            AbilityFlags f = p.Abilities.Current;
            var parts = new List<string>();
            if ((f & AbilityFlags.Melee) != 0) parts.Add("Attack [J]");
            if ((f & AbilityFlags.Ranged) != 0) parts.Add("Shot [K]");
            if ((f & AbilityFlags.Dash) != 0) parts.Add("Dash [Shift]");
            if ((f & AbilityFlags.DoubleJump) != 0) parts.Add("Double jump");
            if ((f & (AbilityFlags.WallSlide | AbilityFlags.WallJump)) != 0) parts.Add("Wall jump");
            abilityText.text = string.Join("   ", parts.ToArray());
        }

        // ------------------------------------------------------------ per-frame

        void Update()
        {
            var gm = GameManager.Instance;
            bool inGame = PlayerController.Instance != null && gm.State != GameState.Menu;
            if (hud.activeSelf != inGame) hud.SetActive(inGame);
            if (inGame) RefreshAbilityText();

            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                Color c = toastText.color;
                c.a = Mathf.Clamp01(toastTimer / 0.5f);
                toastText.color = c;
                if (toastTimer <= 0f) toastText.text = "";
            }

            bool pausePressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                             || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
            if (!pausePressed) return;

            if (settingsPanel.activeSelf) CloseSettings();
            else if (gm.State == GameState.Paused) gm.SetState(GameState.Playing);
            else if (gm.State == GameState.Playing && PlayerController.Instance != null) gm.SetState(GameState.Paused);
        }
    }
}
