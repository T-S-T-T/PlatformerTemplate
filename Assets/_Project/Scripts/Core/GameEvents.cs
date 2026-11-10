using System;
using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>
    /// Central event hub. UI, audio, camera and VFX listen here instead of holding direct
    /// references to gameplay objects, so any system can be removed or replaced safely.
    /// </summary>
    public static class GameEvents
    {
        // Player
        public static Action<int, int> PlayerHealthChanged;     // current, max
        public static Action<Vector2> PlayerDamaged;
        public static Action PlayerDied;
        public static Action PlayerRespawned;
        public static Action<AbilityFlags> AbilitiesChanged;
        public static Action<Vector2> PlayerJumped;
        public static Action<Vector2> PlayerDoubleJumped;
        public static Action<Vector2, int> PlayerWallJumped;    // position, direction pushed
        public static Action<Vector2, float> PlayerLanded;     // position, impact speed
        public static Action<Vector2, Vector2> PlayerDashed;   // position, direction

        // World
        public static Action<Enemy> EnemyKilled;
        public static Action<int> CoinsChanged;
        public static Action<int> ScoreChanged;
        public static Action<Room> RoomEntered;
        public static Action<Vector2> CheckpointReached;
        public static Action BossDefeated;

        // Game flow
        public static Action<GameState> StateChanged;

        // Presentation
        public static Action<SfxId> Sfx;
        public static Action<float, float> ShakeRequested;     // intensity (units), duration (s)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAll()
        {
            PlayerHealthChanged = null; PlayerDamaged = null; PlayerDied = null; PlayerRespawned = null;
            AbilitiesChanged = null; PlayerJumped = null; PlayerDoubleJumped = null; PlayerWallJumped = null;
            PlayerLanded = null; PlayerDashed = null; EnemyKilled = null; CoinsChanged = null;
            ScoreChanged = null; RoomEntered = null; CheckpointReached = null; BossDefeated = null;
            StateChanged = null; Sfx = null; ShakeRequested = null;
        }

        public static void PlaySfx(SfxId id) { if (Sfx != null) Sfx(id); }
        public static void Shake(float intensity, float duration) { if (ShakeRequested != null) ShakeRequested(intensity, duration); }
    }
}
