using System.Collections.Generic;
using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Generates simple retro blips so the template has audio before you add real sound files.</summary>
    public static class PlaceholderSfx
    {
        enum Wave { Square, Sine, Noise }

        static readonly Dictionary<SfxId, AudioClip> cache = new Dictionary<SfxId, AudioClip>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); }

        public static AudioClip Get(SfxId id)
        {
            AudioClip clip;
            if (cache.TryGetValue(id, out clip) && clip != null) return clip;
            clip = Build(id);
            cache[id] = clip;
            return clip;
        }

        static AudioClip Build(SfxId id)
        {
            switch (id)
            {
                case SfxId.Jump:       return Make(id, 260f, 520f, 0.12f, Wave.Square, 0.18f);
                case SfxId.DoubleJump: return Make(id, 380f, 760f, 0.12f, Wave.Square, 0.18f);
                case SfxId.WallJump:   return Make(id, 300f, 600f, 0.10f, Wave.Square, 0.18f);
                case SfxId.Land:       return Make(id, 140f, 70f, 0.07f, Wave.Sine, 0.3f);
                case SfxId.Dash:       return Make(id, 900f, 200f, 0.14f, Wave.Noise, 0.2f);
                case SfxId.Melee:      return Make(id, 600f, 300f, 0.09f, Wave.Noise, 0.2f);
                case SfxId.Shoot:      return Make(id, 800f, 300f, 0.10f, Wave.Square, 0.15f);
                case SfxId.PlayerHit:  return Make(id, 220f, 60f, 0.25f, Wave.Square, 0.3f);
                case SfxId.EnemyHit:   return Make(id, 330f, 150f, 0.08f, Wave.Square, 0.2f);
                case SfxId.EnemyDie:   return Make(id, 400f, 80f, 0.22f, Wave.Noise, 0.25f);
                case SfxId.PlayerDie:  return Make(id, 300f, 40f, 0.6f, Wave.Square, 0.3f);
                case SfxId.Coin:       return Make(id, 880f, 1320f, 0.10f, Wave.Square, 0.15f);
                case SfxId.Pickup:     return Make(id, 440f, 1100f, 0.35f, Wave.Sine, 0.3f);
                case SfxId.Checkpoint: return Make(id, 520f, 780f, 0.25f, Wave.Sine, 0.3f);
                case SfxId.Spring:     return Make(id, 200f, 700f, 0.15f, Wave.Sine, 0.3f);
                case SfxId.Break:      return Make(id, 250f, 60f, 0.18f, Wave.Noise, 0.3f);
                case SfxId.UiClick:    return Make(id, 700f, 700f, 0.04f, Wave.Square, 0.12f);
                case SfxId.Win:        return Make(id, 523f, 1046f, 0.5f, Wave.Sine, 0.3f);
                case SfxId.Telegraph:  return Make(id, 500f, 500f, 0.12f, Wave.Square, 0.12f);
                default:               return Make(id, 440f, 440f, 0.08f, Wave.Sine, 0.2f);
            }
        }

        static AudioClip Make(SfxId id, float f0, float f1, float duration, Wave wave, float volume)
        {
            const int sampleRate = 44100;
            int n = Mathf.Max(1, (int)(sampleRate * duration));
            var data = new float[n];
            float phase = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += f / sampleRate;

                float s;
                if (wave == Wave.Square) s = Mathf.Repeat(phase, 1f) < 0.5f ? 1f : -1f;
                else if (wave == Wave.Sine) s = Mathf.Sin(phase * 2f * Mathf.PI);
                else s = Random.value * 2f - 1f;

                float env = (1f - t) * (1f - t);
                data[i] = s * env * volume;
            }

            var clip = AudioClip.Create("sfx_" + id, n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
