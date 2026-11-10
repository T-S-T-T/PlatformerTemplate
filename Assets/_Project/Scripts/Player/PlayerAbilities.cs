using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Which abilities the player currently has. Modes set this; pickups add to it.</summary>
    public class PlayerAbilities : MonoBehaviour
    {
        public AbilityFlags startingAbilities = AbilityFlags.All;

        public AbilityFlags Current { get; private set; }

        void Awake()
        {
            Current = startingAbilities;
        }

        public bool Has(AbilityFlags flag)
        {
            return flag != AbilityFlags.None && (Current & flag) == flag;
        }

        public void Set(AbilityFlags flags)
        {
            Current = flags;
            if (GameEvents.AbilitiesChanged != null) GameEvents.AbilitiesChanged(Current);
        }

        public void Unlock(AbilityFlags flags)
        {
            Set(Current | flags);
        }
    }
}
