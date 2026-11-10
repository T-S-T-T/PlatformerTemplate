using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace PlatformerTemplate.EditorTools
{
    /// <summary>
    /// Turns the ASCII layouts in <see cref="LevelLayouts"/> into scenes (tilemap + prefab instances) and wires up
    /// the game modes. Menu: Tools > Platformer Template > 2. Build Demo Content.
    /// </summary>
    public static class DemoScenes
    {
        class RoomDef
        {
            public string id;
            public float x0, y0, x1, y1;
            public RoomDef(string id, float x0, float y0, float x1, float y1)
            {
                this.id = id; this.x0 = x0; this.y0 = y0; this.x1 = x1; this.y1 = y1;
            }
        }

        class LevelBuild
        {
            public DemoLibrary lib;
            public string sceneName;
            public bool persistentIds;
            public bool hasBoss;
            public bool hasSpawn;
            public Vector3 spawn;
            public int width, height;
            public Transform platforms, pickups, enemies, hazards, props;
            public readonly List<LevelExit> exits = new List<LevelExit>();
        }

        // ------------------------------------------------------------ menu

        [MenuItem("Tools/Platformer Template/2. Build Demo Content")]
        public static void BuildMenu()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Platformer Template: stop Play Mode before building.");
                return;
            }

            bool existing = AssetDatabase.IsValidFolder(DemoAssets.PrefabsDir)
                            && AssetDatabase.FindAssets("t:Prefab", new[] { DemoAssets.PrefabsDir }).Length > 0;
            if (existing && !EditorUtility.DisplayDialog("Build Demo Content",
                    "This regenerates the demo prefabs, scenes and level layouts and overwrites any edits you made to them.\n\n" +
                    "Data assets (PlayerTuning, EnemyData, LevelDatabase) are kept.\n\nContinue?", "Rebuild", "Cancel"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EditorUtility.DisplayProgressBar("Platformer Template", "Creating assets and prefabs...", 0.1f);
                DemoLibrary lib = DemoAssets.Build();

                EditorUtility.DisplayProgressBar("Platformer Template", "Building scenes...", 0.5f);
                BuildAll(lib);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log("Platformer Template: demo content built. Open Assets/_Project/Scenes/MainMenu.unity and press Play.");
        }

        // ------------------------------------------------------------ all scenes

        static void BuildAll(DemoLibrary lib)
        {
            var paths = new List<string>();
            paths.Add(BuildMenuScene(lib));

            paths.Add(BuildLevelScene(lib, "Gym", LevelLayouts.Gym, null, false,
                go => { var m = go.AddComponent<GymMode>(); m.abilities = AbilityFlags.All; }));

            AbilityFlags l1 = AbilityFlags.Melee;
            AbilityFlags l2 = AbilityFlags.Melee | AbilityFlags.Ranged | AbilityFlags.DoubleJump | AbilityFlags.Dash;
            paths.Add(BuildLevelScene(lib, "Level_1", LevelLayouts.Level_1, null, false, go => AddLevelMode(go, lib, l1)));
            paths.Add(BuildLevelScene(lib, "Level_2", LevelLayouts.Level_2, null, false, go => AddLevelMode(go, lib, l2)));
            paths.Add(BuildLevelScene(lib, "Level_3", LevelLayouts.Level_3, null, true, go => AddLevelMode(go, lib, AbilityFlags.All)));

            var rooms = new[]
            {
                new RoomDef("R1_start",   0,   0,  30, 16),
                new RoomDef("R2_gap",    30,   0,  60, 16),
                new RoomDef("R3_climb",  60,   0,  90, 16),
                new RoomDef("R4_shaft",  90,   0, 120, 16),
                new RoomDef("R5_vault", 120,   0, 150, 16),
                new RoomDef("R6_upper",  90,  16, 120, 34),
            };
            paths.Add(BuildLevelScene(lib, "Metroidvania_Demo", LevelLayouts.Metroidvania, rooms, false,
                go => { var m = go.AddComponent<MetroidvaniaMode>(); m.newGameAbilities = AbilityFlags.Melee; }));

            paths.Add(BuildEndlessScene(lib));

            var list = new List<EditorBuildSettingsScene>();
            for (int i = 0; i < paths.Count; i++) list.Add(new EditorBuildSettingsScene(paths[i], true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static void AddLevelMode(GameObject go, DemoLibrary lib, AbilityFlags abilities)
        {
            var m = go.AddComponent<LevelMode>();
            m.levels = lib.levels;
            m.abilities = abilities;
        }

        // ------------------------------------------------------------ scene helpers

        static Scene NewScene(bool withCamera)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].GetComponent<Camera>() != null) continue;
                Object.DestroyImmediate(roots[i]);
            }

            if (withCamera)
            {
                Camera cam = Object.FindFirstObjectByType<Camera>();
                if (cam != null)
                {
                    cam.name = "Main Camera";
                    cam.orthographic = true;
                    cam.orthographicSize = 7f;
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.08f, 0.09f, 0.13f);
                    cam.transform.position = new Vector3(0f, 0f, -10f);
                }
            }
            return scene;
        }

        static string SaveScene(Scene scene, string name)
        {
            string path = DemoAssets.ScenesDir + "/" + name + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        static string BuildMenuScene(DemoLibrary lib)
        {
            Scene scene = NewScene(true);
            var go = new GameObject("Main Menu");
            var menu = go.AddComponent<MainMenuController>();
            menu.levels = lib.levels;
            menu.metroidvaniaScene = "Metroidvania_Demo";
            menu.endlessScene = "Endless_Demo";
            menu.gymScene = "Gym";
            return SaveScene(scene, "MainMenu");
        }

        static string BuildLevelScene(DemoLibrary lib, string sceneName, string[] rows, RoomDef[] rooms, bool boss,
                                      System.Action<GameObject> addMode)
        {
            Scene scene = NewScene(true);

            var b = NewBuild(lib, sceneName, true);
            b.hasBoss = boss;
            var world = new GameObject("World").transform;
            BuildLayout(b, rows, world);
            AddPit(world, b.width);

            var roomRoot = new GameObject("Rooms").transform;
            if (rooms == null)
            {
                AddRoom(roomRoot, sceneName, 0f, 0f, b.width, b.height);
            }
            else
            {
                for (int i = 0; i < rooms.Length; i++)
                    AddRoom(roomRoot, rooms[i].id, rooms[i].x0, rooms[i].y0, rooms[i].x1, rooms[i].y1);
            }

            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null) cam.gameObject.AddComponent<CameraFollow>();

            if (!b.hasSpawn) { b.spawn = new Vector3(3.5f, 3.2f, 0f); Debug.LogWarning(sceneName + ": layout has no 'P' spawn, using a default."); }
            var spawn = new GameObject("Player Spawn");
            spawn.AddComponent<PlayerSpawn>();
            spawn.transform.position = b.spawn;

            GameObject player = Place(lib.player, null, b.spawn);
            player.name = "Player";

            var modeGo = new GameObject("Game Mode");
            addMode(modeGo);

            return SaveScene(scene, sceneName);
        }

        static string BuildEndlessScene(DemoLibrary lib)
        {
            // Chunks are built inside a scratch scene, then saved as prefabs.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            DemoAssets.EnsureFolder(DemoAssets.PrefabsDir + "/Chunks");

            var chunkPrefabs = new List<EndlessChunk>();
            for (int i = 0; i < LevelLayouts.Chunks.Length; i++)
            {
                LevelLayouts.ChunkInfo info = LevelLayouts.Chunks[i];
                var root = new GameObject("Chunk_" + info.name);
                var chunk = root.AddComponent<EndlessChunk>();
                chunk.width = info.rows[0].Length;
                chunk.difficulty = info.difficulty;
                chunk.isStartChunk = info.isStart;

                var b = NewBuild(lib, "Chunk_" + info.name, false);
                BuildLayout(b, info.rows, root.transform);

                string path = DemoAssets.PrefabsDir + "/Chunks/Chunk_" + info.name + ".prefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                chunkPrefabs.Add(prefab.GetComponent<EndlessChunk>());
            }

            Scene scene = NewScene(true);
            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null) cam.gameObject.AddComponent<CameraFollow>();

            Vector3 spawnPos = new Vector3(3.5f, 3.2f, 0f);
            var spawn = new GameObject("Player Spawn");
            spawn.AddComponent<PlayerSpawn>();
            spawn.transform.position = spawnPos;

            GameObject player = Place(lib.player, null, spawnPos);
            player.name = "Player";

            var modeGo = new GameObject("Game Mode");
            var mode = modeGo.AddComponent<EndlessMode>();
            mode.abilities = AbilityFlags.All;
            mode.chunkPrefabs = chunkPrefabs.ToArray();

            return SaveScene(scene, "Endless_Demo");
        }

        static LevelBuild NewBuild(DemoLibrary lib, string sceneName, bool persistentIds)
        {
            return new LevelBuild { lib = lib, sceneName = sceneName, persistentIds = persistentIds };
        }

        // ------------------------------------------------------------ layout -> objects

        static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static GameObject Place(GameObject prefab, Transform parent, Vector3 position)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
            return go;
        }

        static void Touch(Object component)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorUtility.SetDirty(component);
        }

        static char At(string[] rows, int x, int y)
        {
            int r = rows.Length - 1 - y;
            if (r < 0 || r >= rows.Length || x < 0 || x >= rows[r].Length) return '.';
            return rows[r][x];
        }

        static string Id(LevelBuild b, string kind, int x, int y)
        {
            return b.persistentIds ? kind + "_" + b.sceneName + "_" + x + "_" + y : "";
        }

        static void BuildLayout(LevelBuild b, string[] rows, Transform parent)
        {
            DemoLibrary lib = b.lib;
            b.height = rows.Length;
            b.width = 0;
            for (int i = 0; i < rows.Length; i++) b.width = Mathf.Max(b.width, rows[i].Length);

            b.platforms = Group(parent, "Platforms");
            b.pickups = Group(parent, "Pickups");
            b.enemies = Group(parent, "Enemies");
            b.hazards = Group(parent, "Hazards");
            b.props = Group(parent, "Props");

            // Ground tilemap
            Tilemap ground = CreateTilemap(parent);
            for (int y = 0; y < b.height; y++)
            {
                for (int x = 0; x < b.width; x++)
                {
                    if (At(rows, x, y) != '#') continue;
                    TileBase tile = At(rows, x, y + 1) == '#' ? (TileBase)lib.ground : lib.groundTop;
                    ground.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
            ground.CompressBounds();

            // One-way platforms: one collider per horizontal run
            for (int y = 0; y < b.height; y++)
            {
                int x = 0;
                while (x < b.width)
                {
                    if (At(rows, x, y) != '=') { x++; continue; }
                    int start = x;
                    while (x < b.width && At(rows, x, y) == '=') x++;
                    AddOneWayRun(b, start, x - start, y);
                }
            }

            // Everything else
            for (int y = 0; y < b.height; y++)
            {
                for (int x = 0; x < b.width; x++)
                {
                    char ch = At(rows, x, y);
                    if (ch == '.' || ch == '#' || ch == '=') continue;
                    PlaceObject(b, ch, x, y);
                }
            }

            for (int i = 0; i < b.exits.Count; i++)
            {
                b.exits[i].requiresBossDefeat = b.hasBoss;
                Touch(b.exits[i]);
            }
        }

        static Tilemap CreateTilemap(Transform parent)
        {
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(parent, false);
            gridGo.AddComponent<Grid>();

            var go = new GameObject("Ground");
            go.transform.SetParent(gridGo.transform, false);
            go.layer = TemplateSetup.Layer("Ground");

            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = DemoAssets.OrderTiles;

            // The composite merges all tile colliders into one solid shape (no seams for the box casts to catch on).
            var composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            var body = go.GetComponent<Rigidbody2D>();
            if (body != null) body.bodyType = RigidbodyType2D.Static;

            var tileCollider = go.AddComponent<TilemapCollider2D>();
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            return tilemap;
        }

        static void AddOneWayRun(LevelBuild b, int startX, int length, int y)
        {
            var go = new GameObject("OneWay");
            go.transform.SetParent(b.platforms, false);
            go.layer = TemplateSetup.Layer("OneWay");
            go.transform.position = new Vector3(startX + length * 0.5f, y + 0.875f, 0f);

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(length, 0.25f);

            for (int i = 0; i < length; i++)
            {
                DemoAssets.AddSprite(go.transform, "Tile", b.lib.strip, new Color(0.85f, 0.75f, 0.45f),
                    new Vector2(i + 0.5f - length * 0.5f, -0.375f), Vector2.one, DemoAssets.OrderPlatform);
            }
        }

        static void AddPit(Transform parent, int width)
        {
            var go = new GameObject("Pit");
            go.transform.SetParent(parent, false);
            go.layer = TemplateSetup.Layer("Hazard");
            go.transform.position = new Vector3(width * 0.5f, -4f, 0f);

            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(width + 80f, 6f);

            var dealer = go.AddComponent<DamageDealer>();
            dealer.team = Team.Enemy;
            dealer.damage = 1;
            dealer.knockback = 0f;
            dealer.returnPlayerToSafeGround = true;
        }

        static void AddRoom(Transform parent, string id, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject("Room " + id);
            go.transform.SetParent(parent, false);
            go.layer = TemplateSetup.Layer("Trigger");
            go.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f);

            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(x1 - x0, y1 - y0);

            var room = go.AddComponent<Room>();
            room.roomId = id;
        }

        static void PlaceObject(LevelBuild b, char ch, int x, int y)
        {
            DemoLibrary lib = b.lib;
            float cx = x + 0.5f, cy = y + 0.5f;
            Vector3 center = new Vector3(cx, cy, 0f);

            switch (ch)
            {
                case 'P':
                    b.hasSpawn = true;
                    b.spawn = new Vector3(cx, y + 1.2f, 0f);
                    break;

                case 'C':
                    Place(lib.checkpoint, b.props, center);
                    break;

                case 'X':
                {
                    GameObject exit = Place(lib.exit, b.props, center);
                    b.exits.Add(exit.GetComponent<LevelExit>());
                    break;
                }

                case 'c':
                {
                    GameObject coin = Place(lib.coin, b.pickups, center);
                    var col = coin.GetComponent<Collectible>();
                    col.id = Id(b, "coin", x, y);
                    Touch(col);
                    break;
                }

                case '1': case '2': case '3': case '4': case '5':
                {
                    GameObject orb = Place(lib.pickup, b.pickups, center);
                    var pickup = orb.GetComponent<AbilityPickup>();
                    SetPickup(pickup, ch);
                    Touch(pickup);
                    break;
                }

                case 'p': Place(lib.patroller, b.enemies, new Vector3(cx, y + 0.42f, 0f)); break;
                case 'f': Place(lib.flyer, b.enemies, center); break;
                case 's': Place(lib.shooter, b.enemies, new Vector3(cx, y + 0.52f, 0f)); break;
                case 'h': Place(lib.charger, b.enemies, new Vector3(cx, y + 0.52f, 0f)); break;
                case 'B': Place(lib.boss, b.enemies, new Vector3(cx, y + 1.22f, 0f)); break;

                case 'M': Place(lib.movingPlatform, b.platforms, new Vector3(x + 1.5f, y - 0.25f, 0f)); break;
                case 'm': Place(lib.spring, b.props, center); break;
                case '^': Place(lib.spike, b.hazards, center); break;
                case 'b': Place(lib.breakable, b.props, center); break;

                case 'G':
                {
                    GameObject gate = Place(lib.gate, b.props, new Vector3(cx, y + 1.5f, 0f));
                    var g = gate.GetComponent<AbilityGate>();
                    g.required = AbilityFlags.Dash;
                    Touch(g);
                    break;
                }

                default:
                    Debug.LogWarning(b.sceneName + ": unknown layout character '" + ch + "' at " + x + "," + y);
                    break;
            }
        }

        static void SetPickup(AbilityPickup pickup, char ch)
        {
            switch (ch)
            {
                case '1': pickup.grants = AbilityFlags.DoubleJump; pickup.id = "ability_double_jump"; pickup.displayName = "Double Jump"; break;
                case '2': pickup.grants = AbilityFlags.WallSlide | AbilityFlags.WallJump; pickup.id = "ability_wall_jump"; pickup.displayName = "Wall Slide + Wall Jump"; break;
                case '3': pickup.grants = AbilityFlags.Dash; pickup.id = "ability_dash"; pickup.displayName = "Dash"; break;
                case '4': pickup.grants = AbilityFlags.Melee; pickup.id = "ability_melee"; pickup.displayName = "Melee Attack"; break;
                default: pickup.grants = AbilityFlags.Ranged; pickup.id = "ability_ranged"; pickup.displayName = "Ranged Attack"; break;
            }
        }
    }
}
