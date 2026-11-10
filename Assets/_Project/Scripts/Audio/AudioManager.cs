using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>
    /// Plays sound events raised through GameEvents.Sfx, so gameplay code never touches audio directly.
    /// Optional "SoundBank" asset in a Resources folder overrides the generated placeholder blips.
    /// Volume categories (master/music/sfx) come from GameSettings; wire an AudioMixer here if you want groups.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const int VoiceCount = 8;

        AudioSource[] voices;
        AudioSource music;
        SoundBank bank;
        int next;

        void Awake()
        {
            bank = Resources.Load<SoundBank>("SoundBank");

            voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f;
            }

            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.spatialBlend = 0f;
            if (bank != null && bank.music != null)
            {
                music.clip = bank.music;
                music.volume = GameSettings.MusicVolume;
                music.Play();
            }

            GameEvents.Sfx += Play;
        }

        void OnDestroy()
        {
            GameEvents.Sfx -= Play;
        }

        void Update()
        {
            if (music != null) music.volume = GameSettings.MusicVolume;
        }

        public void Play(SfxId id)
        {
            AudioClip clip = null;
            float volume = 1f;
            if (bank != null) bank.TryGet(id, out clip, out volume);
            if (clip == null) clip = PlaceholderSfx.Get(id);

            AudioSource v = voices[next];
            next = (next + 1) % VoiceCount;
            v.pitch = Random.Range(0.97f, 1.03f);
            v.PlayOneShot(clip, volume * GameSettings.SfxVolume);
        }
    }
}
