using System;
using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Player abilities. Each can be enabled, disabled or unlocked at runtime.</summary>
    [Flags]
    public enum AbilityFlags
    {
        None = 0,
        DoubleJump = 1 << 0,
        WallSlide = 1 << 1,
        WallJump = 1 << 2,
        Dash = 1 << 3,
        Melee = 1 << 4,
        Ranged = 1 << 5,
        All = DoubleJump | WallSlide | WallJump | Dash | Melee | Ranged
    }

    public enum Team { Neutral, Player, Enemy }

    public enum GameState { Boot, Menu, Playing, Paused, GameOver, LevelComplete }

    public enum SfxId
    {
        Jump, DoubleJump, WallJump, Land, Dash, Melee, Shoot,
        PlayerHit, EnemyHit, EnemyDie, PlayerDie,
        Coin, Pickup, Checkpoint, Spring, Break, UiClick, Win, Telegraph
    }

    public struct DamageInfo
    {
        public int amount;
        public Team team;               // team of the attacker (Neutral hurts everyone)
        public Vector2 sourcePosition;  // used for knockback direction
        public float knockback;         // multiplier on the target's knockback
        public bool instantKill;

        public static DamageInfo Make(int amount, Team team, Vector2 source, float knockback = 1f)
        {
            return new DamageInfo { amount = amount, team = team, sourcePosition = source, knockback = knockback };
        }
    }

    public interface IDamageable
    {
        Team Team { get; }
        bool TakeDamage(DamageInfo info);
    }
}
