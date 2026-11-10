using UnityEngine;
using UnityEngine.UI;

namespace PlatformerTemplate
{
    /// <summary>Builds the main menu in code: mode select, level select, continue/new game, settings, quit.</summary>
    public class MainMenuController : MonoBehaviour
    {
        public LevelDatabase levels;
        public string metroidvaniaScene = "Metroidvania_Demo";
        public string endlessScene = "Endless_Demo";
        public string gymScene = "Gym";

        GameObject container, mainPanel, levelPanel, metroPanel;

        void Start()
        {
            var gm = GameManager.Instance;
            gm.SetState(GameState.Menu);
            gm.ResetRun();

            container = UIFactory.Create("Main Menu", UIManager.Instance.Root);
            UIFactory.Stretch(container.GetComponent<RectTransform>());

            BuildMain();
            Show(mainPanel);
        }

        void OnDestroy()
        {
            if (container != null) Destroy(container);
        }

        void Show(GameObject panel)
        {
            if (mainPanel != null) mainPanel.SetActive(panel == mainPanel);
            if (levelPanel != null) levelPanel.SetActive(panel == levelPanel);
            if (metroPanel != null) metroPanel.SetActive(panel == metroPanel);
            UIManager.SelectFirst(panel);
        }

        void BuildMain()
        {
            var col = UIFactory.MakeMenu(container.transform, "Main Panel", "PLATFORMER TEMPLATE", 460f, out mainPanel);
            UIFactory.MakeButton(col, "Level Mode", () => { BuildLevelSelect(); Show(levelPanel); });
            UIFactory.MakeButton(col, "Metroidvania Mode", () => { BuildMetroidvania(); Show(metroPanel); });
            UIFactory.MakeButton(col, "Endless Mode", () => Launch(endlessScene));
            UIFactory.MakeButton(col, "Gym (test room)", () => Launch(gymScene));
            UIFactory.MakeButton(col, "Settings", () =>
            {
                mainPanel.SetActive(false);
                UIManager.Instance.OpenSettings(() => Show(mainPanel));
            });
            UIFactory.MakeButton(col, "Quit", Quit);
        }

        void BuildLevelSelect()
        {
            if (levelPanel != null) Destroy(levelPanel);

            SaveSystem.Load(1);
            var col = UIFactory.MakeMenu(container.transform, "Level Panel", "SELECT LEVEL", 460f, out levelPanel);
            if (levels != null)
            {
                for (int i = 0; i < levels.levels.Count; i++)
                {
                    LevelEntry entry = levels.levels[i];
                    bool unlocked = levels.IsUnlocked(i, SaveSystem.Current);
                    bool cleared = SaveSystem.Current.clearedLevels.Contains(entry.sceneName);
                    string label = entry.displayName + (cleared ? "  (cleared)" : "") + (unlocked ? "" : "  (locked)");
                    string scene = entry.sceneName;
                    Button b = UIFactory.MakeButton(col, label, () => Launch(scene));
                    b.interactable = unlocked;
                }
            }
            else
            {
                UIFactory.MakeLabel(col, "No LevelDatabase assigned.", 20, TextAnchor.MiddleCenter, UIFactory.TextColor);
            }
            UIFactory.MakeButton(col, "Back", () => Show(mainPanel));
            levelPanel.SetActive(false);
        }

        void BuildMetroidvania()
        {
            if (metroPanel != null) Destroy(metroPanel);

            var col = UIFactory.MakeMenu(container.transform, "Metroidvania Panel", "METROIDVANIA", 460f, out metroPanel);
            if (SaveSystem.Exists(0))
                UIFactory.MakeButton(col, "Continue", () => Launch(metroidvaniaScene));
            UIFactory.MakeButton(col, "New Game", () =>
            {
                SaveSystem.Delete(0);
                Launch(metroidvaniaScene);
            });
            UIFactory.MakeButton(col, "Back", () => Show(mainPanel));
            metroPanel.SetActive(false);
        }

        void Launch(string scene)
        {
            GameManager.Instance.ResetRun();
            GameManager.Instance.LoadScene(scene);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
