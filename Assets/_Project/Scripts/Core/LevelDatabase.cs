using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlatformerTemplate
{
    [Serializable]
    public class LevelEntry
    {
        public string sceneName;
        public string displayName;
    }

    /// <summary>Ordered list of levels for level mode. Level N unlocks when level N-1 is cleared.</summary>
    [CreateAssetMenu(menuName = "Platformer Template/Level Database", fileName = "LevelDatabase")]
    public class LevelDatabase : ScriptableObject
    {
        public List<LevelEntry> levels = new List<LevelEntry>();

        public int IndexOf(string sceneName)
        {
            for (int i = 0; i < levels.Count; i++)
                if (levels[i].sceneName == sceneName) return i;
            return -1;
        }

        public LevelEntry Next(string sceneName)
        {
            int i = IndexOf(sceneName);
            return (i >= 0 && i + 1 < levels.Count) ? levels[i + 1] : null;
        }

        public bool IsUnlocked(int index, SaveData save)
        {
            if (index <= 0) return true;
            return save.clearedLevels.Contains(levels[index - 1].sceneName);
        }
    }
}
