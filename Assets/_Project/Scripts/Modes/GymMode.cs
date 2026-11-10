using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>
    /// Test-room mode: every ability on, respawn in place on death, nothing saved.
    /// Use it as the regression course for movement tuning.
    /// </summary>
    public class GymMode : GameMode
    {
        public AbilityFlags abilities = AbilityFlags.All;

        public override AbilityFlags StartingAbilities { get { return abilities; } }
    }
}
