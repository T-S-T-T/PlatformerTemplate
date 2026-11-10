using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Versioned save data. Add fields freely; bump <see cref="CurrentVersion"/> and migrate in Load.</summary>
    [Serializable]
    public class SaveData
    {
        public int version = SaveSystem.CurrentVersion;
        public int abilities;                       // AbilityFlags as int
        public List<string> collected = new List<string>();
        public List<string> clearedLevels = new List<string>();
        public List<string> visitedRooms = new List<string>();
        public string checkpointScene = "";
        public float checkpointX;
        public float checkpointY;
        public int coins;
        public int deaths;
        public string lastSaved = "";
    }

    /// <summary>JSON save slots in Application.persistentDataPath.</summary>
    public static class SaveSystem
    {
        public const int CurrentVersion = 1;

        public static SaveData Current { get; private set; } = new SaveData();
        public static int Slot { get; private set; }

        static string PathFor(int slot)
        {
            return Path.Combine(Application.persistentDataPath, "save_" + slot + ".json");
        }

        public static bool Exists(int slot)
        {
            return File.Exists(PathFor(slot));
        }

        /// <summary>Loads a slot into Current. Returns false (and starts a fresh save) if none exists.</summary>
        public static bool Load(int slot)
        {
            Slot = slot;
            try
            {
                string path = PathFor(slot);
                if (File.Exists(path))
                {
                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                    if (data != null)
                    {
                        Migrate(data);
                        Current = data;
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("SaveSystem: failed to load slot " + slot + ": " + e.Message);
            }
            Current = new SaveData();
            return false;
        }

        public static void Save()
        {
            try
            {
                Current.version = CurrentVersion;
                Current.lastSaved = DateTime.Now.ToString("s");
                string path = PathFor(Slot);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(Current, true));
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("SaveSystem: failed to save: " + e.Message);
            }
        }

        public static void Delete(int slot)
        {
            try { if (File.Exists(PathFor(slot))) File.Delete(PathFor(slot)); }
            catch (Exception e) { Debug.LogWarning("SaveSystem: failed to delete: " + e.Message); }
            if (slot == Slot) Current = new SaveData();
        }

        static void Migrate(SaveData data)
        {
            // Future versions: upgrade old data here, then set data.version = CurrentVersion.
            data.version = CurrentVersion;
        }
    }
}
