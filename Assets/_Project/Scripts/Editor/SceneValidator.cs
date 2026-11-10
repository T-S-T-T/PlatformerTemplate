using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PlatformerTemplate.EditorTools
{
    /// <summary>
    /// Checks the open scene for the setup mistakes that are easy to make when building rooms by hand
    /// (wrong layers, missing spawn, duplicate ids, no mode component). Menu: Tools > Platformer Template > Validate Open Scene.
    /// </summary>
    public static class SceneValidator
    {
        [MenuItem("Tools/Platformer Template/Validate Open Scene")]
        public static void Validate()
        {
            int problems = 0;
            int ground = LayerMask.NameToLayer("Ground");

            for (int i = 0; i < TemplateSetup.Layers.Length; i++)
            {
                if (LayerMask.NameToLayer(TemplateSetup.Layers[i]) < 0)
                    problems += Report("Layer '" + TemplateSetup.Layers[i] + "' does not exist. Run Tools > Platformer Template > 1. Setup Project.");
            }

            var modes = Object.FindObjectsByType<GameMode>(FindObjectsSortMode.None);
            if (modes.Length == 0) problems += Report("No GameMode component in the scene (add LevelMode, MetroidvaniaMode, EndlessMode or GymMode).");
            if (modes.Length > 1) problems += Report("More than one GameMode in the scene (" + modes.Length + "). Keep exactly one.");

            if (Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length != 1)
                problems += Report("The scene needs exactly one Player instance.");

            if (Object.FindFirstObjectByType<PlayerSpawn>() == null)
                problems += Report("No PlayerSpawn in the scene. The player will start where it was placed.");

            var cam = Object.FindFirstObjectByType<CameraFollow>();
            if (cam == null) problems += Report("No CameraFollow on the camera.");

            bool isEndless = modes.Length > 0 && modes[0] is EndlessMode;
            if (!isEndless && Object.FindObjectsByType<Room>(FindObjectsSortMode.None).Length == 0)
                problems += Report("No Room triggers. The camera will follow the player without bounds.");

            if (modes.Length > 0 && modes[0] is LevelMode && Object.FindFirstObjectByType<LevelExit>() == null)
                problems += Report("Level mode scene has no LevelExit.");

            // Tilemaps that should block the player
            var tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            for (int i = 0; i < tilemaps.Length; i++)
            {
                var t = tilemaps[i];
                var tc = t.GetComponent<TilemapCollider2D>();
                if (tc != null && t.gameObject.layer != ground)
                    problems += Report("Tilemap '" + t.name + "' has a collider but is not on the Ground layer.", t);
                if (tc != null && t.GetComponent<CompositeCollider2D>() == null)
                    problems += Report("Tilemap '" + t.name + "' has no CompositeCollider2D. Seams between tiles can snag the player.", t);
            }

            // Solid things the player can bump into must be on Ground / OneWay
            var blocks = Object.FindObjectsByType<BreakableBlock>(FindObjectsSortMode.None);
            for (int i = 0; i < blocks.Length; i++)
                if (blocks[i].gameObject.layer != ground)
                    problems += Report("BreakableBlock '" + blocks[i].name + "' must be on the Ground layer.", blocks[i]);

            var platforms = Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None);
            for (int i = 0; i < platforms.Length; i++)
            {
                int layer = platforms[i].gameObject.layer;
                if (layer != ground && layer != LayerMask.NameToLayer("OneWay"))
                    problems += Report("MovingPlatform '" + platforms[i].name + "' must be on the Ground or OneWay layer.", platforms[i]);
            }

            // Enemies need data and sensible masks
            var enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i].data == null)
                    problems += Report("Enemy '" + enemies[i].name + "' has no EnemyData assigned.", enemies[i]);
                var body = enemies[i].GetComponent<KinematicBody2D>();
                if (body != null && body.solidMask.value == 0)
                    problems += Report("Enemy '" + enemies[i].name + "' has an empty solidMask.", enemies[i]);
            }

            // Duplicate persistent ids (collectibles and pickups)
            var seen = new HashSet<string>();
            var coins = Object.FindObjectsByType<Collectible>(FindObjectsSortMode.None);
            for (int i = 0; i < coins.Length; i++)
                problems += CheckId(seen, coins[i].id, coins[i]);
            var pickups = Object.FindObjectsByType<AbilityPickup>(FindObjectsSortMode.None);
            for (int i = 0; i < pickups.Length; i++)
            {
                if (string.IsNullOrEmpty(pickups[i].id))
                    problems += Report("AbilityPickup '" + pickups[i].name + "' has no id, so it will respawn after every load.", pickups[i]);
                else
                    problems += CheckId(seen, pickups[i].id, pickups[i]);
            }

            // Room ids must be unique in metroidvania mode
            var rooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
            var roomIds = new HashSet<string>();
            for (int i = 0; i < rooms.Length; i++)
            {
                if (string.IsNullOrEmpty(rooms[i].roomId)) continue;
                if (!roomIds.Add(rooms[i].roomId))
                    problems += Report("Duplicate roomId '" + rooms[i].roomId + "'.", rooms[i]);
            }

            if (problems == 0) Debug.Log("Platformer Template: scene looks good.");
            else Debug.LogWarning("Platformer Template: " + problems + " problem(s) found in the open scene.");
        }

        static int CheckId(HashSet<string> seen, string id, Object context)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            if (seen.Add(id)) return 0;
            return Report("Duplicate id '" + id + "'. Ids must be unique per scene.", context);
        }

        static int Report(string message, Object context = null)
        {
            Debug.LogWarning("Validate: " + message, context);
            return 1;
        }
    }
}
