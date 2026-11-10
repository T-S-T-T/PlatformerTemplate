using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Player-facing options, stored in PlayerPrefs (separate from game progress).</summary>
    public static class GameSettings
    {
        public static float MasterVolume = 0.8f;
        public static float MusicVolume = 0.6f;
        public static float SfxVolume = 1f;
        public static float ScreenShake = 1f;   // 0 disables shake
        public static bool HitStop = true;
        public static bool VSync = true;
        public static bool Fullscreen;

        public static void Load()
        {
            MasterVolume = PlayerPrefs.GetFloat("set_master", 0.8f);
            MusicVolume = PlayerPrefs.GetFloat("set_music", 0.6f);
            SfxVolume = PlayerPrefs.GetFloat("set_sfx", 1f);
            ScreenShake = PlayerPrefs.GetFloat("set_shake", 1f);
            HitStop = PlayerPrefs.GetInt("set_hitstop", 1) == 1;
            VSync = PlayerPrefs.GetInt("set_vsync", 1) == 1;
            Fullscreen = PlayerPrefs.GetInt("set_fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("set_master", MasterVolume);
            PlayerPrefs.SetFloat("set_music", MusicVolume);
            PlayerPrefs.SetFloat("set_sfx", SfxVolume);
            PlayerPrefs.SetFloat("set_shake", ScreenShake);
            PlayerPrefs.SetInt("set_hitstop", HitStop ? 1 : 0);
            PlayerPrefs.SetInt("set_vsync", VSync ? 1 : 0);
            PlayerPrefs.SetInt("set_fullscreen", Fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Applies audio and vsync. Fullscreen is only applied when the player changes it.</summary>
        public static void Apply()
        {
            AudioListener.volume = Mathf.Clamp01(MasterVolume);
            QualitySettings.vSyncCount = VSync ? 1 : 0;
        }

        public static void ApplyFullscreen()
        {
            Screen.fullScreen = Fullscreen;
        }
    }
}
