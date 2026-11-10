using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PlatformerTemplate.EditorTools
{
    /// <summary>Everything the demo scenes are built from: placeholder sprites, tiles, data assets and prefabs.</summary>
    public class DemoLibrary
    {
        public Sprite character, square, circle, triangle, strip;
        public Tile ground, groundTop;

        public PlayerTuning tuning;
        public EnemyData patrolData, flyerData, shooterData, chargerData, bossData;
        public LevelDatabase levels;

        public GameObject player, playerProjectile, enemyProjectile;
        public GameObject patroller, flyer, shooter, charger, boss;
        public GameObject checkpoint, coin, pickup, spring, spike, breakable, gate, movingPlatform, exit;
    }

    public static class DemoAssets
    {
        public const string Root = "Assets/_Project";
        public const string ArtDir = Root + "/Art";
        public const string TilesDir = Root + "/Art/Tiles";
        public const string DataDir = Root + "/Data";
        public const string PrefabsDir = Root + "/Prefabs";
        public const string ScenesDir = Root + "/Scenes";

        // Sorting orders used by the demo content (the Default sorting layer is used on purpose).
        public const int OrderTiles = 0, OrderPlatform = 1, OrderHazard = 2, OrderPickup = 3, OrderEnemy = 5, OrderPlayer = 10;

        // ------------------------------------------------------------ entry point

        public static DemoLibrary Build()
        {
            TemplateSetup.EnsureLayers();

            EnsureFolder(ArtDir);
            EnsureFolder(TilesDir);
            EnsureFolder(DataDir);
            EnsureFolder(PrefabsDir);
            EnsureFolder(ScenesDir);

            var lib = new DemoLibrary();
            BuildSprites(lib);
            BuildTiles(lib);
            BuildData(lib);
            BuildPrefabs(lib);
            BuildLevelDatabase(lib);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return lib;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static string Absolute(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }

        // ------------------------------------------------------------ sprites (16 px per unit, point filtered)

        static void BuildSprites(DemoLibrary lib)
        {
            lib.square = MakeSprite("Square", (x, y) =>
            {
                bool edge = x == 0 || y == 0 || x == 15 || y == 15;
                return edge ? new Color32(205, 205, 205, 255) : new Color32(255, 255, 255, 255);
            });

            lib.character = MakeSprite("Character", (x, y) =>
            {
                bool edge = x == 0 || y == 0 || x == 15 || y == 15;
                bool eye = x >= 10 && x <= 12 && y >= 9 && y <= 12;
                if (eye) return new Color32(25, 25, 35, 255);
                return edge ? new Color32(205, 205, 205, 255) : new Color32(255, 255, 255, 255);
            });

            lib.circle = MakeSprite("Circle", (x, y) =>
            {
                float dx = x - 7.5f, dy = y - 7.5f;
                return dx * dx + dy * dy <= 7.6f * 7.6f ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            });

            lib.triangle = MakeSprite("Triangle", (x, y) =>
            {
                bool inside = Mathf.Abs(x - 7.5f) <= (16f - y) * 0.5f - 0.25f;
                return inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            });

            lib.strip = MakeSprite("Strip", (x, y) =>
            {
                return y >= 12 ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            });
        }

        static Sprite MakeSprite(string name, System.Func<int, int, Color32> pixel)
        {
            const int size = 16;
            string path = ArtDir + "/" + name + ".png";

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = pixel(x, y);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(Absolute(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16f;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ------------------------------------------------------------ tiles

        static void BuildTiles(DemoLibrary lib)
        {
            lib.ground = MakeTile("Ground", lib.square, new Color(0.30f, 0.33f, 0.42f));
            lib.groundTop = MakeTile("GroundTop", lib.square, new Color(0.45f, 0.66f, 0.42f));
        }

        static Tile MakeTile(string name, Sprite sprite, Color color)
        {
            string path = TilesDir + "/" + name + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = sprite;
            tile.color = color;
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        // ------------------------------------------------------------ data assets (created once, never overwritten)

        static T LoadOrCreate<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            if (init != null) init(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void BuildData(DemoLibrary lib)
        {
            lib.tuning = LoadOrCreate<PlayerTuning>(DataDir + "/PlayerTuning.asset", null);

            lib.patrolData = LoadOrCreate<EnemyData>(DataDir + "/Enemy_Patroller.asset", d =>
            {
                d.maxHealth = 2; d.scoreValue = 100; d.moveSpeed = 2f; d.chaseMultiplier = 1.6f; d.aggroRange = 7f;
            });
            lib.flyerData = LoadOrCreate<EnemyData>(DataDir + "/Enemy_Flyer.asset", d =>
            {
                d.maxHealth = 1; d.scoreValue = 150; d.moveSpeed = 2.5f; d.chaseMultiplier = 1.4f; d.aggroRange = 9f;
            });
            lib.shooterData = LoadOrCreate<EnemyData>(DataDir + "/Enemy_Shooter.asset", d =>
            {
                d.maxHealth = 2; d.scoreValue = 200; d.aggroRange = 10f; d.attackCooldown = 2.2f; d.windupTime = 0.6f; d.projectileSpeed = 7f;
            });
            lib.chargerData = LoadOrCreate<EnemyData>(DataDir + "/Enemy_Charger.asset", d =>
            {
                d.maxHealth = 3; d.scoreValue = 250; d.moveSpeed = 1.5f; d.aggroRange = 9f; d.windupTime = 0.6f;
                d.chargeSpeed = 11f; d.chargeDuration = 0.8f; d.recoverTime = 1f; d.attackCooldown = 1.5f;
            });
            lib.bossData = LoadOrCreate<EnemyData>(DataDir + "/Enemy_Boss.asset", d =>
            {
                d.maxHealth = 18; d.scoreValue = 2000; d.aggroRange = 14f; d.windupTime = 0.7f; d.chargeSpeed = 10f;
                d.chargeDuration = 1f; d.attackCooldown = 2.2f; d.recoverTime = 1.2f; d.projectileSpeed = 9f;
                d.hurtStun = 0.05f; d.hurtKnockback = new Vector2(0.5f, 0f);
            });
        }

        static void BuildLevelDatabase(DemoLibrary lib)
        {
            lib.levels = LoadOrCreate<LevelDatabase>(DataDir + "/LevelDatabase.asset", null);
            lib.levels.levels.Clear();
            lib.levels.levels.Add(new LevelEntry { sceneName = "Level_1", displayName = "1 - First Steps" });
            lib.levels.levels.Add(new LevelEntry { sceneName = "Level_2", displayName = "2 - Up and Over" });
            lib.levels.levels.Add(new LevelEntry { sceneName = "Level_3", displayName = "3 - Boss Gate" });
            EditorUtility.SetDirty(lib.levels);
        }

        // ------------------------------------------------------------ prefabs (always regenerated)

        static void BuildPrefabs(DemoLibrary lib)
        {
            lib.playerProjectile = MakeProjectile("PlayerProjectile", lib, new Color(1f, 0.92f, 0.35f), "PlayerHitbox", Team.Player);
            lib.enemyProjectile = MakeProjectile("EnemyProjectile", lib, new Color(1f, 0.35f, 0.3f), "EnemyHitbox", Team.Enemy);

            lib.shooterData.projectilePrefab = lib.enemyProjectile;
            lib.bossData.projectilePrefab = lib.enemyProjectile;
            EditorUtility.SetDirty(lib.shooterData);
            EditorUtility.SetDirty(lib.bossData);

            lib.player = MakePlayer(lib);

            lib.patroller = SavePrefab(MakeEnemy<PatrolEnemy>("Patroller", lib, lib.patrolData, new Vector2(0.9f, 0.8f), new Color(0.93f, 0.40f, 0.38f)), "Patroller");
            lib.flyer = SavePrefab(MakeEnemy<FlyerEnemy>("Flyer", lib, lib.flyerData, new Vector2(0.8f, 0.8f), new Color(0.72f, 0.48f, 0.95f)), "Flyer");
            lib.shooter = SavePrefab(MakeEnemy<ShooterEnemy>("Shooter", lib, lib.shooterData, new Vector2(0.9f, 1f), new Color(0.97f, 0.66f, 0.25f)), "Shooter");
            lib.charger = SavePrefab(MakeEnemy<ChargerEnemy>("Charger", lib, lib.chargerData, new Vector2(1.3f, 1f), new Color(0.88f, 0.47f, 0.67f)), "Charger");
            lib.boss = SavePrefab(MakeEnemy<BossEnemy>("Boss", lib, lib.bossData, new Vector2(2.4f, 2.4f), new Color(0.78f, 0.16f, 0.22f)), "Boss");

            lib.checkpoint = MakeCheckpoint(lib);
            lib.coin = MakeCoin(lib);
            lib.pickup = MakeAbilityPickup(lib);
            lib.spring = MakeSpring(lib);
            lib.spike = MakeSpike(lib);
            lib.breakable = MakeBreakable(lib);
            lib.gate = MakeGate(lib);
            lib.movingPlatform = MakeMovingPlatform(lib);
            lib.exit = MakeExit(lib);
        }

        static GameObject SavePrefab(GameObject go, string name)
        {
            string path = PrefabsDir + "/" + name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        public static SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, Color color,
                                               Vector2 localPos, Vector2 scale, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            go.layer = parent.gameObject.layer;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        static GameObject MakePlayer(DemoLibrary lib)
        {
            var go = new GameObject("Player");
            go.tag = "Player";
            go.layer = TemplateSetup.Layer("Player");

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            // PlayerController pulls in KinematicBody2D (+ BoxCollider2D), Health, PlayerAbilities and PlayerInputReader.
            var pc = go.AddComponent<PlayerController>();
            go.AddComponent<PlayerJuice>();

            var box = go.GetComponent<BoxCollider2D>();
            box.size = new Vector2(0.875f, 1.4375f);
            box.offset = Vector2.zero;

            var body = go.GetComponent<KinematicBody2D>();
            body.solidMask = LayerMask.GetMask("Ground");
            body.oneWayMask = LayerMask.GetMask("OneWay");

            SpriteRenderer visual = AddSprite(go.transform, "Visual", lib.character, new Color(0.35f, 0.8f, 1f),
                                              Vector2.zero, new Vector2(0.875f, 1.4375f), OrderPlayer);
            SpriteRenderer slash = AddSprite(go.transform, "Slash", lib.square, new Color(1f, 1f, 1f, 0.55f),
                                             Vector2.zero, Vector2.one, OrderPlayer + 5);
            slash.enabled = false;

            var flash = go.AddComponent<HitFlash>();
            flash.target = visual;
            flash.blinkWhileInvulnerable = true;

            pc.tuning = lib.tuning;
            pc.visual = visual.transform;
            pc.slash = slash;
            pc.projectilePrefab = lib.playerProjectile;
            pc.meleeMask = LayerMask.GetMask("Enemy", "Ground", "Hazard");

            return SavePrefab(go, "Player");
        }

        static GameObject MakeProjectile(string name, DemoLibrary lib, Color color, string layerName, Team team)
        {
            var go = new GameObject(name);
            go.layer = TemplateSetup.Layer(layerName);
            go.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = lib.circle;
            sr.color = color;
            sr.sortingOrder = 20;

            // Projectile pulls in Rigidbody2D and CircleCollider2D.
            var projectile = go.AddComponent<Projectile>();
            projectile.team = team;
            projectile.solidMask = LayerMask.GetMask("Ground");

            var rb = go.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            var col = go.GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            return SavePrefab(go, name);
        }

        static GameObject MakeEnemy<T>(string name, DemoLibrary lib, EnemyData data, Vector2 size, Color color) where T : Enemy
        {
            var go = new GameObject(name);
            go.layer = TemplateSetup.Layer("Enemy");

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            // Enemy pulls in KinematicBody2D (+ BoxCollider2D) and Health.
            var enemy = go.AddComponent<T>();
            enemy.data = data;

            var box = go.GetComponent<BoxCollider2D>();
            box.size = size;
            box.offset = Vector2.zero;

            var body = go.GetComponent<KinematicBody2D>();
            body.solidMask = LayerMask.GetMask("Ground");
            body.oneWayMask = LayerMask.GetMask("OneWay");

            var health = go.GetComponent<Health>();
            health.team = Team.Enemy;
            health.maxHealth = data.maxHealth;

            SpriteRenderer visual = AddSprite(go.transform, "Visual", lib.character, color, Vector2.zero, size, OrderEnemy);
            var flash = go.AddComponent<HitFlash>();
            flash.target = visual;

            var hitbox = new GameObject("Hitbox");
            hitbox.transform.SetParent(go.transform, false);
            hitbox.layer = TemplateSetup.Layer("EnemyHitbox");
            var hb = hitbox.AddComponent<BoxCollider2D>();
            hb.isTrigger = true;
            hb.size = size * 0.9f;
            var dealer = hitbox.AddComponent<DamageDealer>();
            dealer.team = Team.Enemy;
            dealer.damage = data.contactDamage;

            return go;
        }

        static GameObject MakeCheckpoint(DemoLibrary lib)
        {
            var go = new GameObject("Checkpoint");
            go.layer = TemplateSetup.Layer("Trigger");
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 2f);
            col.offset = new Vector2(0f, 0.5f);
            var cp = go.AddComponent<Checkpoint>();

            AddSprite(go.transform, "Base", lib.square, new Color(0.5f, 0.5f, 0.55f), new Vector2(0f, -0.44f), new Vector2(0.55f, 0.12f), OrderPickup);
            AddSprite(go.transform, "Pole", lib.square, new Color(0.7f, 0.7f, 0.75f), new Vector2(0f, 0.3f), new Vector2(0.1f, 1.6f), OrderPickup);
            cp.flag = AddSprite(go.transform, "Flag", lib.square, new Color(0.55f, 0.55f, 0.6f), new Vector2(0.3f, 0.95f), new Vector2(0.5f, 0.35f), OrderPickup + 1);

            return SavePrefab(go, "Checkpoint");
        }

        static GameObject MakeCoin(DemoLibrary lib)
        {
            var go = new GameObject("Coin");
            go.layer = TemplateSetup.Layer("Pickup");
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f;
            go.AddComponent<Collectible>();
            AddSprite(go.transform, "Visual", lib.circle, new Color(1f, 0.85f, 0.2f), Vector2.zero, new Vector2(0.5f, 0.5f), OrderPickup);
            return SavePrefab(go, "Coin");
        }

        static GameObject MakeAbilityPickup(DemoLibrary lib)
        {
            var go = new GameObject("AbilityPickup");
            go.layer = TemplateSetup.Layer("Pickup");
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
            go.AddComponent<AbilityPickup>();
            AddSprite(go.transform, "Orb", lib.circle, new Color(0.45f, 0.9f, 1f), Vector2.zero, new Vector2(0.85f, 0.85f), OrderPickup);
            AddSprite(go.transform, "Core", lib.square, Color.white, Vector2.zero, new Vector2(0.3f, 0.3f), OrderPickup + 1);
            return SavePrefab(go, "AbilityPickup");
        }

        static GameObject MakeSpring(DemoLibrary lib)
        {
            var go = new GameObject("Spring");
            go.layer = TemplateSetup.Layer("Trigger");
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.9f, 0.5f);
            col.offset = new Vector2(0f, -0.25f);
            var spring = go.AddComponent<Spring>();
            SpriteRenderer v = AddSprite(go.transform, "Visual", lib.square, new Color(0.4f, 0.9f, 0.5f), new Vector2(0f, -0.3f), new Vector2(0.8f, 0.4f), OrderHazard);
            spring.visual = v.transform;
            return SavePrefab(go, "Spring");
        }

        static GameObject MakeSpike(DemoLibrary lib)
        {
            var go = new GameObject("Spike");
            go.layer = TemplateSetup.Layer("Hazard");
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.4f);
            col.offset = new Vector2(0f, -0.3f);
            var dealer = go.AddComponent<DamageDealer>();
            dealer.team = Team.Enemy;
            dealer.damage = 1;
            dealer.knockback = 0.5f;
            dealer.returnPlayerToSafeGround = true;
            AddSprite(go.transform, "Visual", lib.triangle, new Color(0.85f, 0.87f, 0.95f), new Vector2(0f, -0.25f), new Vector2(1f, 0.5f), OrderHazard);
            return SavePrefab(go, "Spike");
        }

        static GameObject MakeBreakable(DemoLibrary lib)
        {
            var go = new GameObject("BreakableBlock");
            go.layer = TemplateSetup.Layer("Ground");
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            go.AddComponent<BreakableBlock>();
            AddSprite(go.transform, "Visual", lib.square, new Color(0.72f, 0.52f, 0.34f), Vector2.zero, Vector2.one, OrderPlatform);
            return SavePrefab(go, "BreakableBlock");
        }

        static GameObject MakeGate(DemoLibrary lib)
        {
            var go = new GameObject("AbilityGate");
            go.layer = TemplateSetup.Layer("Ground");
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 3f);
            var gate = go.AddComponent<AbilityGate>();
            gate.solid = col;
            gate.sprite = AddSprite(go.transform, "Visual", lib.square, new Color(0.9f, 0.4f, 0.3f), Vector2.zero, new Vector2(1f, 3f), OrderHazard);
            return SavePrefab(go, "AbilityGate");
        }

        static GameObject MakeMovingPlatform(DemoLibrary lib)
        {
            var go = new GameObject("MovingPlatform");
            go.layer = TemplateSetup.Layer("Ground");
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 0.5f);
            var mp = go.AddComponent<MovingPlatform>();
            mp.travel = new Vector2(5f, 0f);
            mp.speed = 2.5f;
            AddSprite(go.transform, "Visual", lib.square, new Color(0.85f, 0.7f, 0.3f), Vector2.zero, new Vector2(3f, 0.5f), OrderPlatform);
            return SavePrefab(go, "MovingPlatform");
        }

        static GameObject MakeExit(DemoLibrary lib)
        {
            var go = new GameObject("LevelExit");
            go.layer = TemplateSetup.Layer("Trigger");
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 2f);
            col.offset = new Vector2(0f, 0.5f);
            var exit = go.AddComponent<LevelExit>();
            exit.sprite = AddSprite(go.transform, "Visual", lib.square, new Color(0.4f, 0.9f, 1f), new Vector2(0f, 0.5f), new Vector2(0.9f, 1.9f), OrderPickup);
            return SavePrefab(go, "LevelExit");
        }
    }
}
