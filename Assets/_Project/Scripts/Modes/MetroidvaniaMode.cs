using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>
    /// One connected map, ability-gated progress and backtracking. Checkpoints are save points; abilities and
    /// collected items persist; death reloads the scene and returns to the last save point.
    /// </summary>
    public class MetroidvaniaMode : GameMode
    {
        [Tooltip("Abilities every new game starts with.")]
        public AbilityFlags newGameAbilities = AbilityFlags.Melee;

        public override bool PersistProgress { get { return true; } }
        public override bool LoadsSave { get { return true; } }
        public override int SaveSlot { get { return 0; } }

        public override AbilityFlags StartingAbilities
        {
            get { return (AbilityFlags)SaveSystem.Current.abilities | newGameAbilities; }
        }

        protected override void OnProgressLoaded()
        {
            var gm = GameManager.Instance;
            var save = SaveSystem.Current;

            if (!gm.HasCheckpoint && !string.IsNullOrEmpty(save.checkpointScene))
                gm.RestoreCheckpoint(save.checkpointScene, new Vector2(save.checkpointX, save.checkpointY));

            if (gm.Coins < save.coins) gm.AddCoins(save.coins - gm.Coins);
        }

        public override void OnCheckpoint(Vector2 position)
        {
            var save = SaveSystem.Current;
            var gm = GameManager.Instance;
            save.checkpointScene = gm.CheckpointScene;
            save.checkpointX = position.x;
            save.checkpointY = position.y;
            save.coins = gm.Coins;
            if (PlayerController.Instance != null)
                save.abilities = (int)PlayerController.Instance.Abilities.Current;
            SaveSystem.Save();
            if (UIManager.Instance != null) UIManager.Instance.ShowToast("Game saved");
        }

        public override void OnAbilityUnlocked(AbilityFlags flags)
        {
            SaveSystem.Current.abilities |= (int)flags;
            SaveSystem.Save();
        }

        public override void OnPlayerDied()
        {
            SaveSystem.Current.deaths++;
            SaveSystem.Save();
            GameManager.Instance.ReloadScene();
        }
    }
}
