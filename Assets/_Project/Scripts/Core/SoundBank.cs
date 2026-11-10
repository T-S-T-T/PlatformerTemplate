using System;
using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>
    /// Optional: maps sound events to real clips. Put one asset named "SoundBank" in any Resources folder.
    /// Any event without a clip falls back to a generated placeholder blip.
    /// </summary>
    [CreateAssetMenu(menuName = "Platformer Template/Sound Bank", fileName = "SoundBank")]
    public class SoundBank : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public SfxId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        public Entry[] entries = new Entry[0];
        public AudioClip music;

        public bool TryGet(SfxId id, out AudioClip clip, out float volume)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].id == id && entries[i].clip != null)
                {
                    clip = entries[i].clip;
                    volume = entries[i].volume;
                    return true;
                }
            }
            clip = null;
            volume = 1f;
            return false;
        }
    }
}
