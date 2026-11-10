using UnityEditor;
using UnityEngine;

namespace PlatformerTemplate.EditorTools
{
    /// <summary>
    /// One-time project setup: physics layers and sorting layers the template relies on.
    /// Menu: Tools > Platformer Template > 1. Setup Project.
    /// </summary>
    public static class TemplateSetup
    {
        public static readonly string[] Layers =
        {
            "Player", "Enemy", "Ground", "OneWay", "Hazard", "PlayerHitbox", "EnemyHitbox", "Pickup", "Trigger"
        };

        public static readonly string[] SortingLayers =
        {
            "Background", "FarDecor", "Ground", "Characters", "ForegroundDecor", "VFX", "UI"
        };

        [MenuItem("Tools/Platformer Template/1. Setup Project (layers)")]
        public static void SetupMenu()
        {
            bool ok = EnsureLayers();
            Debug.Log(ok
                ? "Platformer Template: layers are set up. Next: Tools > Platformer Template > 2. Build Demo Content."
                : "Platformer Template: layer setup hit a problem, see the messages above.");
        }

        /// <summary>Adds any missing layers (slot 8 and up) and sorting layers. Safe to run repeatedly.</summary>
        public static bool EnsureLayers()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("Platformer Template: could not open ProjectSettings/TagManager.asset.");
                return false;
            }

            var tagManager = new SerializedObject(assets[0]);
            bool allFound = true;

            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                Debug.LogError("Platformer Template: TagManager has no 'layers' property.");
                return false;
            }

            for (int n = 0; n < Layers.Length; n++)
            {
                string wanted = Layers[n];
                if (LayerSlot(layers, wanted) >= 0) continue;

                bool placed = false;
                for (int i = 8; i < layers.arraySize && i < 32; i++)
                {
                    SerializedProperty element = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(element.stringValue))
                    {
                        element.stringValue = wanted;
                        placed = true;
                        break;
                    }
                }
                if (!placed)
                {
                    Debug.LogError("Platformer Template: no free layer slot for '" + wanted + "'. Free one up in Project Settings > Tags and Layers.");
                    allFound = false;
                }
            }

            // Sorting layers are a convenience (the demo content only uses sorting orders), so failures here are not fatal.
            try
            {
                SerializedProperty sorting = tagManager.FindProperty("m_SortingLayers");
                if (sorting != null && sorting.isArray)
                {
                    for (int n = 0; n < SortingLayers.Length; n++)
                    {
                        string wanted = SortingLayers[n];
                        if (SortingSlot(sorting, wanted) >= 0) continue;

                        sorting.InsertArrayElementAtIndex(sorting.arraySize);
                        SerializedProperty element = sorting.GetArrayElementAtIndex(sorting.arraySize - 1);
                        element.FindPropertyRelative("name").stringValue = wanted;
                        element.FindPropertyRelative("uniqueID").intValue = 700000 + n * 7919;
                        element.FindPropertyRelative("locked").boolValue = false;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Platformer Template: could not add sorting layers (" + e.Message + "). Add them by hand if you want them.");
            }

            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            return allFound;
        }

        static int LayerSlot(SerializedProperty layers, string name)
        {
            for (int i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == name) return i;
            return -1;
        }

        static int SortingSlot(SerializedProperty sorting, string name)
        {
            for (int i = 0; i < sorting.arraySize; i++)
            {
                SerializedProperty n = sorting.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                if (n != null && n.stringValue == name) return i;
            }
            return -1;
        }

        /// <summary>Layer index by name; logs once and falls back to Default when the layer is missing.</summary>
        public static int Layer(string name)
        {
            int index = LayerMask.NameToLayer(name);
            if (index < 0)
            {
                Debug.LogWarning("Platformer Template: layer '" + name + "' is missing. Run Tools > Platformer Template > 1. Setup Project.");
                return 0;
            }
            return index;
        }
    }
}
