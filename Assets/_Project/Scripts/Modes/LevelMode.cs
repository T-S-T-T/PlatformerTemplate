using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlatformerTemplate
{
    /// <summary>
    /// Level-by-level progression. One scene per level, abilities set per level, checkpoint respawn
    /// (the scene reloads so enemies reset), and a level-complete screen with a link to the next level.
    /// </summary>
    public class LevelMode : GameMode
    {
        public LevelDatabase levels;
        public AbilityFlags abilities = AbilityFlags.Melee;

        public override bool LoadsSave { get { return true; } }
        public override int SaveSlot { get { return 1; } }
        public override AbilityFlags StartingAbilities { get { return abilities; } }

        public override void OnPlayerDied()
        {
            SaveSystem.Current.deaths++;
            GameManager.Instance.ReloadScene();
        }

        public override void OnLevelExit()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (!SaveSystem.Current.clearedLevels.Contains(scene)) SaveSystem.Current.clearedLevels.Add(scene);
            SaveSystem.Save();

            GameManager gm = GameManager.Instance;
            gm.ClearCheckpoint();

            LevelEntry next = levels != null ? levels.Next(scene) : null;
            System.Action onNext = null;
            if (next != null)
            {
                string nextScene = next.sceneName;
                onNext = () => gm.LoadScene(nextScene);
            }

            string summary = "Coins: " + gm.Coins + "    Score: " + gm.Score;
            UIManager.Instance.ShowLevelComplete(summary, onNext);
        }
    }
}
