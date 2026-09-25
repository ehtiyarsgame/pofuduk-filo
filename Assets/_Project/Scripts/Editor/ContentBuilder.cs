using System.Collections.Generic;
using PofudukFilo.Bullets;
using PofudukFilo.Enemies;
using PofudukFilo.Meta;
using PofudukFilo.Progression;
using PofudukFilo.Weapons;
using UnityEditor;
using UnityEngine;
using static PofudukFilo.EditorTools.SetupUtil;

namespace PofudukFilo.EditorTools
{
    /// <summary>
    /// Generates every gameplay asset from the GDD tables: sprites, materials, bullet types,
    /// passives, weapons + evolutions, enemies, bosses, runs and Workshop upgrades.
    /// Numbers come from design/gdd/*.md — tune the generated assets, or change them here and re-run.
    /// </summary>
    internal sealed class ContentBuilder
    {
        // Bullet type indices (order of BulletSystem.bulletTypes).
        public const int BFeather = 0, BGiantFeather = 1, BChick = 2, BStar = 3, BMeteor = 4,
            BEnemy = 5, BEnemyBig = 6, BBossSpecial = 7, BSpark = 8, BIce = 9, BYarn = 10;

        public readonly Dictionary<string, Sprite> Sprites = new();
        public Mesh Quad;
        public Material FlashMaterial;
        public Material LineMaterial;
        public Material ConfettiMaterial;
        /// <summary>Unlit sprite material for every SpriteRenderer (no 2D lights in the scene).</summary>
        public Material SpriteMaterial;
        public BulletTypeDefinition[] BulletTypes;
        public PickupVisual[] PickupVisuals;
        public readonly List<PassiveDefinition> Passives = new();
        public readonly List<WeaponDefinition> BaseWeapons = new();
        /// <summary>Each hero's own main gun (hero-guns.md); in the card pool only for that hero.</summary>
        public readonly List<WeaponDefinition> HeroGuns = new();
        public WeaponDefinition StartingWeapon;
        public readonly Dictionary<string, Enemy> Enemies = new();
        public readonly List<RunDefinition> Runs = new();
        public readonly List<MetaUpgradeDefinition> Workshop = new();
        public readonly List<CharacterDefinition> Characters = new();
        public readonly List<FusionRecipe> Fusions = new();
        public ConstellationDefinition Constellation;
        private readonly Dictionary<string, WeaponDefinition> _evolved = new();

        public void BuildAll()
        {
            BuildSprites();
            BuildMeshAndMaterials();
            BuildBulletTypes();
            BuildPickupVisuals();
            BuildWeapons(); // also creates the passives (evolution keys)
            BuildEnemies();
            BuildRuns();
            BuildWorkshop();
            ApplyLabCosts();
            BuildFusions();
            BuildCharacters();
            BuildConstellation();
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- Art

        private void BuildSprites()
        {
            void Add(string name, Painter p, float ppu, Vector4 border = default) =>
                Sprites[name] = p.SaveSprite(PathFor("Art", name + ".png"), ppu, border);
            void AddShip(string name, Painter pilot, Color wing) => Add(name, ArtRecipes.Ship(pilot, wing), 256);

            Add("bunny", ArtRecipes.Bunny(), 256);
            Add("chick", ArtRecipes.ChickEnemy(false), 256);
            Add("chick_elite", ArtRecipes.ChickEnemy(true), 256);
            Add("jelly_bear", ArtRecipes.JellyBear(false), 256);
            Add("jelly_king", ArtRecipes.JellyBear(true), 256);
            Add("cookie_robot", ArtRecipes.CookieRobot(false), 256);
            Add("cookie_mech", ArtRecipes.CookieRobot(true), 256);
            Add("ice_cream", ArtRecipes.IceCreamTower(), 256);
            Add("gum_balloon", ArtRecipes.GumBalloon(), 256);
            Add("donut_ufo", ArtRecipes.DonutUfo(), 256);
            Add("candy_bee", ArtRecipes.CandyBee(), 256);
            Add("marshmallow", ArtRecipes.Marshmallow(), 256);
            Add("queen_hen", ArtRecipes.QueenHen(), 256);
            Add("cat", ArtRecipes.Cat(), 128);
            Add("pilot_chick", ArtRecipes.ChickPilot(), 256);
            Add("pilot_cat", ArtRecipes.CatPilot(), 256);
            Add("pilot_hamster", ArtRecipes.Hamster(), 256);
            Add("pilot_fox", ArtRecipes.Fox(), 256);
            Add("pilot_mystery", ArtRecipes.MysteryBunny(), 256);
            Add("pilot_bunny", ArtRecipes.BunnyPilot(), 256);
            // In-run ships: the shared hull with each pilot in the cockpit.
            AddShip("ship_chick", ArtRecipes.ChickPilot(), ArtRecipes.Honey);
            AddShip("ship_cat", ArtRecipes.CatPilot(), ArtRecipes.Coral);
            AddShip("ship_hamster", ArtRecipes.Hamster(), ArtRecipes.Sky);
            AddShip("ship_fox", ArtRecipes.Fox(), ArtRecipes.Lilac);
            AddShip("ship_mystery", ArtRecipes.MysteryBunny(), ArtRecipes.HotPink);
            Add("pilot_penguin", ArtRecipes.PenguinPilot(), 256);
            Add("pilot_lamb", ArtRecipes.LambPilot(), 256);
            AddShip("ship_penguin", ArtRecipes.PenguinPilot(), ArtRecipes.Sky);
            AddShip("ship_lamb", ArtRecipes.LambPilot(), ArtRecipes.Pink);
            Add("fish", ArtRecipes.FishSprite(false), 128);
            Add("shark", ArtRecipes.FishSprite(true), 128);
            Add("yarn", ArtRecipes.YarnSprite(), 128);
            // Rewarded-ad placements (ad-rewards.md).
            Add("icon_ad", ArtRecipes.IconAd(), 128);
            Add("icon_gift", ArtRecipes.GiftBox(), 256);

            Add("b_feather", ArtRecipes.Feather(false), 64);
            Add("b_feather_giant", ArtRecipes.Feather(true), 64);
            Add("b_chick", ArtRecipes.PlayerOrb(ArtRecipes.Chick), 64);
            Add("b_star", ArtRecipes.MiniStar(ArtRecipes.Cream), 64);
            Add("b_meteor", ArtRecipes.PlayerOrb(ArtRecipes.Sky), 64); // player shots stay cool-coloured
            Add("b_enemy", ArtRecipes.EnemyBullet(ArtRecipes.Hex(0xFF2E4D)), 64);
            Add("b_enemy_big", ArtRecipes.EnemyBullet(ArtRecipes.Hex(0xFF2E9A)), 64);
            Add("b_boss_special", ArtRecipes.EnemyBullet(ArtRecipes.Hex(0xFF7A1A)), 64);

            Add("p_gem_blue", ArtRecipes.GemSprite(ArtRecipes.Sky), 64);
            Add("p_gem_green", ArtRecipes.GemSprite(ArtRecipes.Leaf), 64);
            Add("p_gem_pink", ArtRecipes.GemSprite(ArtRecipes.Gem), 64);
            Add("p_coin", ArtRecipes.Coin(), 64);
            Add("p_magnet", ArtRecipes.MagnetPickup(), 64);
            Add("p_heart", ArtRecipes.HeartPickup(), 64);
            Add("p_bomb", ArtRecipes.BombPickup(), 64);
            Add("p_coin_big", ArtRecipes.CoinStack(), 64);
            Add("p_gold_bar", ArtRecipes.GoldBar(), 64);
            Add("b_spark", ArtRecipes.SparkBolt(), 64);
            Add("b_ice", ArtRecipes.IceShard(), 64);
            Add("b_yarn", ArtRecipes.YarnSprite(), 128);

            Add("egg", ArtRecipes.Egg(), 64);
            Add("star", ArtRecipes.MiniStar(ArtRecipes.Honey), 64);
            Add("bubble", ArtRecipes.Bubble(), 64);
            Add("circle", ArtRecipes.SoftCircle(Color.white, 128), 128);
            Add("confetti", ArtRecipes.SoftCircle(Color.white, 16), 16);
            Add("ui_rounded", ArtRecipes.RoundedPanel(), 100, new Vector4(32, 32, 32, 32));

            Add("ui_button", ArtRecipes.ButtonFace(), 100, new Vector4(44, 44, 44, 44));
            Add("ui_edge_glow", ArtRecipes.EdgeGlow(), 100, new Vector4(60, 60, 60, 60));
            // Brand: studio intro emblem and the launcher icon (brand.md).
            Add("studio_emblem", ArtRecipes.StudioEmblem(), 256);
            Add("ui_bar_track", ArtRecipes.BarTrack(), 100, new Vector4(26, 26, 26, 26));
            Add("icon_research", ArtRecipes.ResearchIcon(), 256);
            Add("icon_stardust", ArtRecipes.StardustIcon(), 96);
            // Main menu dressing and icon set (design/ux/main-menu.md).
            Add("menu_pedestal", ArtRecipes.MenuPedestal(), 100);
            Add("menu_rays", ArtRecipes.LightRays(), 100);
            Add("ui_capsule", ArtRecipes.Capsule(), 100, new Vector4(36, 36, 36, 36));
            Add("ui_navbar", ArtRecipes.NavBar(), 100, new Vector4(40, 40, 40, 40));
            Add("menu_ribbon", ArtRecipes.LogoRibbon(), 100);
            Add("ui_shine", ArtRecipes.ShineBand(), 100);
            Add("ui_vignette", ArtRecipes.Vignette(), 100);
            Add("icon_gear", ArtRecipes.IconGear(), 128);
            Add("icon_play", ArtRecipes.IconPlay(), 128);
            Add("icon_pause", ArtRecipes.IconPause(), 128);
            Add("icon_trophy", ArtRecipes.IconTrophy(), 128);
            Add("icon_weapons", ArtRecipes.IconWeapons(), 256);
            Add("icon_paw", ArtRecipes.PawPrint(), 128);
            Add("ui_bar_fill", ArtRecipes.BarFill(), 100, new Vector4(21, 21, 21, 21));
            Add("app_icon", ArtRecipes.AppIcon(), 512);
            Add("bg_sky", ArtRecipes.SkyGradient(), 100);
            Add("bg_nebula", ArtRecipes.Nebula(), 100);
            Add("bg_stars_far", ArtRecipes.StarField(false), 100);
            Add("bg_stars_near", ArtRecipes.StarField(true), 100);
            Add("bg_planet_ring", ArtRecipes.RingPlanet(), 256);
            Add("bg_planet_donut", ArtRecipes.DonutPlanet(), 256);
            Add("bg_planet_moon", ArtRecipes.CandyMoon(), 256);
        }

        private void BuildMeshAndMaterials()
        {
            string meshPath = PathFor("Materials", "Quad.asset");
            Quad = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (Quad == null)
            {
                Quad = new Mesh { name = "Quad" };
                Quad.vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f) };
                Quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
                Quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
                Quad.RecalculateBounds();
                AssetDatabase.CreateAsset(Quad, meshPath);
                RequirePersisted(Quad, meshPath);
            }

            FlashMaterial = SaveMaterial(new Material(Shader.Find("PofudukFilo/SpriteWhiteFlash")), "SpriteFlash");

            Shader spriteDefault = Shader.Find("Sprites/Default");
            Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? spriteDefault;
            SpriteMaterial = SaveMaterial(new Material(unlit), "SpriteUnlit");
            LineMaterial = SaveMaterial(new Material(spriteDefault), "Line");
            var confetti = new Material(spriteDefault) { mainTexture = Sprites["confetti"].texture };
            ConfettiMaterial = SaveMaterial(confetti, "Confetti");
        }

        private Material Instanced(string spriteName, int queue)
        {
            var m = new Material(Shader.Find("PofudukFilo/InstancedSprite"))
            {
                mainTexture = Sprites[spriteName].texture,
                enableInstancing = true,
                renderQueue = queue
            };
            return SaveMaterial(m, "I_" + spriteName);
        }

        // ---------------------------------------------------------------- Bullets & pickups

        private void BuildBulletTypes()
        {
            BulletTypeDefinition Type(string name, string sprite, float scale, float radius, bool align, int queue)
            {
                var t = ScriptableObject.CreateInstance<BulletTypeDefinition>();
                t.mesh = Quad;
                t.material = Instanced(sprite, queue);
                t.visualScale = scale;
                t.hitRadius = radius;
                t.alignToVelocity = align;
                return SaveAsset(t, "Data/Bullets", name);
            }

            // Hit radii ~30 % under the visual (art-bible §3); enemy bullets render in a later queue.
            BulletTypes = new[]
            {
                Type("Feather", "b_feather", 0.55f, 0.12f, true, 3000),
                Type("GiantFeather", "b_feather_giant", 0.9f, 0.3f, true, 3000),
                Type("Chick", "b_chick", 0.35f, 0.14f, false, 3000),
                Type("Star", "b_star", 0.45f, 0.14f, true, 3000),
                Type("Meteor", "b_meteor", 0.5f, 0.2f, false, 3000),
                Type("EnemyRound", "b_enemy", 0.5f, 0.1f, false, 3010),
                Type("EnemyBig", "b_enemy_big", 0.75f, 0.17f, false, 3010),
                Type("BossSpecial", "b_boss_special", 0.8f, 0.19f, false, 3011),
                // Hero guns (hero-guns.md) — appended so the indices above stay put.
                Type("Spark", "b_spark", 0.5f, 0.13f, true, 3000),
                Type("Ice", "b_ice", 0.55f, 0.13f, true, 3000),
                Type("Yarn", "b_yarn", 0.42f, 0.17f, false, 3000)
            };
        }

        private void BuildPickupVisuals()
        {
            PickupVisual V(string sprite, float scale) => new() { mesh = Quad, material = Instanced(sprite, 3005), scale = scale };
            // Index = PickupKind.
            PickupVisuals = new[]
            {
                // Larger than before (device feedback: gems blended into the star field).
                V("p_gem_blue", 0.44f), V("p_gem_green", 0.52f), V("p_gem_pink", 0.62f),
                V("p_coin", 0.44f), V("p_magnet", 0.5f), V("p_heart", 0.5f), V("p_bomb", 0.5f),
                V("p_coin_big", 0.54f), V("p_gold_bar", 0.6f) // economy.md §3.2 coin tiers
            };
        }

        // ---------------------------------------------------------------- Passives & weapons

        private static readonly System.Collections.Generic.Dictionary<string, string> Descriptions = new()
        {
            ["feather_blaster"] = "Önüne düz tüy mermileri atar. Güvenilir ana silahın.",
            ["egg_mortar"] = "Düşman kalabalığına patlayan yumurtalar fırlatır.",
            ["star_boomerang"] = "Gidip geri dönen yıldız; iki yönde de vurur.",
            ["bubble_orbit"] = "Geminin etrafında dönen balonlar yakındaki düşmanları ezer.",
            ["spark_cat"] = "Düşmandan düşmana seken zincir şimşek atar.",
            ["fish_missile"] = "Kendi hedefini bulan balık füzeleri; hiç ıskalamaz.",
            // Hero main guns (hero-guns.md)
            ["chick_cannon"] = "Cıvık'ın ana silahı: ağır, patlayan civciv topları.",
            ["spark_pistol"] = "Mırnav'ın ana silahı: çok hızlı kıvılcım yağmuru.",
            ["bubble_rifle"] = "Balonbaş'ın ana silahı: düşmanları delen balon mermiler.",
            ["star_bow"] = "Yıldızpati'nin ana silahı: geniş yıldız yelpazesi.",
            ["ice_gun"] = "Pengu'nun ana silahı: hızlı, delici buz sarkıtları.",
            ["yarn_launcher"] = "Kuzu'nun ana silahı: iri, ağır yün yumakları.",
            ["mega_chick_cannon"] = "EVRİM: Dev civciv topları; her 4. atış 3 kat.",
            ["thunder_pistol"] = "EVRİM: Kıvılcım fırtınası; mermiler 2 düşman deler.",
            ["bubble_storm"] = "EVRİM: 6 delici balon, geniş yelpaze.",
            ["comet_bow"] = "EVRİM: 8 yıldızlık yelpaze; her 5. atış kuyruklu yıldız.",
            ["glacier_gun"] = "EVRİM: Buzul yağmuru; 4 düşman deler.",
            ["yarn_cyclone"] = "EVRİM: Yün kasırgası; dev yumaklar 2 düşman deler.",
            ["yarn_ball"] = "Ekran kenarlarından seken yün yumağı; değdiği her düşmanı ezer.",
            ["shark_swarm"] = "EVRİM: Dev köpekbalıkları; her vuruşta 2 yavru balık saldırır.",
            ["cosmic_yarn"] = "EVRİM: Dev yumak; her sekmede mini yumaklar saçar.",
            ["feather_storm"] = "EVRİM: 7 tüylük geniş yelpaze; tüyler 2 düşmanı deler, sık sık dev tüy atar.",
            ["supernova_omelette"] = "EVRİM: Dev yumurtalar ve ekranı kaplayan omlet dalgası.",
            ["galaxy_vortex"] = "EVRİM: Düşmanları içine çekip ezen galaksi girdabı.",
            ["gum_rings"] = "EVRİM: Mermileri emen, düşmanları biçen sakız halkaları.",
            ["storm_cat"] = "EVRİM: Gökten yıldırım yağdıran kedi tanrıça.",
            ["rainbow_storm"] = "FÜZYON: Tüy fırtınası ile fırtına kedisi tek silahta.",
            ["cosmic_breakfast"] = "FÜZYON: Omlet dalgası ile galaksi girdabı tek silahta.",
            ["candy_shield_galaxy"] = "FÜZYON: Sakız halkaları ile galaksi girdabı tek silahta.",
            ["crystal_glasses"] = "Kritik vuruş şansı: kritik vuruş 2 kat hasar verir.",
            ["hot_pan"] = "Patlama ve etki alanlarını büyütür.",
            ["moon_dust"] = "Silah etkileri daha uzun sürer.",
            ["stretchy_gum"] = "Yörünge ve alan silahlarını genişletir.",
            ["battery_collar"] = "Tüm silahlar daha sık ateş eder.",
            ["carrot_shield"] = "Maksimum canını artırır.",
            ["sharp_claws"] = "Mermiler bir düşmanı delip arkasındakine de çarpar.",
            ["double_barrel"] = "Tüm silahlar +1 mermi / top / sekme atar; ama her biri biraz daha az acıtır.",
            ["tiger_eye"] = "Kritik vuruşlar çok daha fazla hasar verir.",
            ["glass_cannon"] = "Büyük hasar artışı; bedeli maksimum canın.",
            ["last_stand"] = "Canın %40'ın altındayken çok daha sert vurursun.",
            ["sugar_heart"] = "Şeker barı daha hızlı dolar.",
            ["golden_paw"] = "Daha çok altın toplarsın: oyun dışı gelişime yatırım.",
            ["wise_owl"] = "Taşlardan daha çok tecrübe: daha sık kart seçersin.",
            ["turtle_shell"] = "Her darbe 2 daha az acıtır; ama silahlar biraz yavaşlar.",
            ["lucky_clover"] = "Nadir kart ve daha iyi ödül şansı.",
        };

        private PassiveDefinition Passive(string id, string name, StatType stat, float value, Rarity rarity,
            int maxLevel = 5, StatType drawbackStat = StatType.Damage, float drawback = 0f)
        {
            var p = ScriptableObject.CreateInstance<PassiveDefinition>();
            p.id = id;
            p.displayName = name;
            p.description = Descriptions.TryGetValue(id, out string pd) ? pd : "";
            p.stat = stat;
            p.valuePerLevel = value;
            p.maxLevel = maxLevel;
            p.drawbackStat = drawbackStat;
            p.drawbackPerLevel = drawback;
            p.rarity = rarity;
            p.icon = IconFor(id);
            p = SaveAsset(p, "Data/Passives", id);
            Passives.Add(p);
            return p;
        }

        private Sprite IconFor(string id)
        {
            string key = "icon_" + id;
            if (!Sprites.TryGetValue(key, out Sprite sprite))
                Sprites[key] = sprite = ArtRecipes.Icon(id).SaveSprite(PathFor("Art/Icons", key + ".png"), 128);
            return sprite;
        }

        private static WeaponLevelStats L(float dmg, float cd, int count, float spread, float speed, int pierce,
            float area, float life, string text, int specialEvery = 0, float specialMult = 1f, BulletEffect fx = BulletEffect.None) =>
            new()
            {
                damage = dmg, cooldown = cd, projectileCount = count, spreadDegrees = spread,
                projectileSpeed = speed, pierce = pierce, area = area, lifetime = life,
                specialEveryN = specialEvery, specialDamageMultiplier = specialMult, upgradeText = text, effects = fx
            };

        private const BulletEffect Boom = BulletEffect.Explode, Split = BulletEffect.Split,
            Chain = BulletEffect.Chain, Slow = BulletEffect.Slow;

        private WeaponDefinition Weapon(string id, string name, Rarity rarity, int bulletType, WeaponBehaviour prefab,
            WeaponLevelStats[] levels, PassiveDefinition key = null, WeaponDefinition evolvesInto = null)
        {
            var w = ScriptableObject.CreateInstance<WeaponDefinition>();
            w.id = id;
            w.displayName = name;
            w.description = Descriptions.TryGetValue(id, out string wd) ? wd : "";
            w.rarity = rarity;
            w.bulletTypeIndex = bulletType;
            w.behaviourPrefab = prefab;
            w.levels = levels;
            w.evolutionPassive = key;
            w.evolvesInto = evolvesInto;
            w.icon = IconFor(id);
            return SaveAsset(w, "Data/Weapons", id);
        }

        private T WeaponPrefab<T>(string name, System.Action<T> configure) where T : WeaponBehaviour
        {
            var go = new GameObject(name);
            T behaviour = go.AddComponent<T>();
            configure?.Invoke(behaviour);
            return SavePrefab<T>(go, "W_" + name);
        }

        private void BuildWeapons()
        {
            PassiveDefinition crystal = Passive("crystal_glasses", "Kristal Gözlük", StatType.CritChance, 0.06f, Rarity.Rare);
            PassiveDefinition pan = Passive("hot_pan", "Sıcak Tava", StatType.Area, 0.10f, Rarity.Common);
            PassiveDefinition moonDust = Passive("moon_dust", "Ay Tozu", StatType.Duration, 0.12f, Rarity.Common);
            PassiveDefinition gum = Passive("stretchy_gum", "Esnek Sakız", StatType.Area, 0.10f, Rarity.Common);
            PassiveDefinition battery = Passive("battery_collar", "Pil Tasması", StatType.CooldownReduction, 0.07f, Rarity.Rare);
            PassiveDefinition carrot = Passive("carrot_shield", "Havuç Kalkan", StatType.MaxHp, 0.15f, Rarity.Common);
            // The magnet card was removed on device feedback (2026-09-25): pickups fly to the ship by themselves.
            // Build cards (passives.md §3.2) — each pushes a different strategy, some at a price.
            PassiveDefinition sharpClaws = Passive("sharp_claws", "Delici Pençe", StatType.Pierce, 1f, Rarity.Rare, maxLevel: 3);
            Passive("double_barrel", "Çift Namlu", StatType.ExtraProjectiles, 1f, Rarity.Epic, maxLevel: 2,
                drawbackStat: StatType.Damage, drawback: -0.08f);
            Passive("tiger_eye", "Kaplan Gözü", StatType.CritDamage, 0.35f, Rarity.Rare);
            Passive("glass_cannon", "Cam Top", StatType.Damage, 0.2f, Rarity.Epic, maxLevel: 3,
                drawbackStat: StatType.MaxHp, drawback: -0.12f);
            Passive("last_stand", "Son Direniş", StatType.LowHpDamage, 0.25f, Rarity.Rare, maxLevel: 4);
            Passive("sugar_heart", "Şeker Kalbi", StatType.RushGain, 0.15f, Rarity.Common);
            Passive("golden_paw", "Altın Pati", StatType.GoldGain, 0.15f, Rarity.Common);
            Passive("wise_owl", "Bilge Baykuş", StatType.Experience, 0.12f, Rarity.Common);
            Passive("turtle_shell", "Kaplumbağa Kabuğu", StatType.Armor, 2f, Rarity.Rare, maxLevel: 4,
                drawbackStat: StatType.CooldownReduction, drawback: -0.04f);
            Passive("lucky_clover", "Şans Yoncası", StatType.Luck, 0.10f, Rarity.Epic);

            // 1) Feather Blaster → Feather Storm. The Rainbow Prism Beam evolution was removed on device feedback
            // (2026-09-24): a screen-long piercing laser erased every fight ("çekişme yok, oyun zevki kalmıyor").
            var storm = WeaponPrefab<FeatherBlaster>("FeatherStorm", b => Set(b, "giantBulletTypeIndex", BGiantFeather));
            WeaponDefinition prismDef = Weapon("feather_storm", "Tüy Fırtınası", Rarity.Legendary, BFeather, storm,
                // ≈1.7× the DPS of a max-level blaster — a real step up, not a screen wipe.
                new[] { L(16f, 0.22f, 7, 50f, 16f, 2, 0, 1.6f, "7 tüy; tüyler bölünür ve patlar; her 4. atış dev tüy", 4, 3f, Split | Boom) });
            var feather = WeaponPrefab<FeatherBlaster>("FeatherBlaster", b => Set(b, "giantBulletTypeIndex", BGiantFeather));
            // Every level adds a new trait (hero-guns.md §3.2), not just more feathers.
            StartingWeapon = Weapon("feather_blaster", "Tüy Blaster", Rarity.Common, BFeather, feather, new[]
            {
                L(12f, 0.25f, 2, 0, 14f, 0, 0, 1.5f, "2 paralel tüy, hızlı atış"),
                L(12f, 0.24f, 2, 0, 14f, 0, 0, 1.5f, "YENİ: tüyler çarpınca ikiye bölünür", fx: Split),
                L(12f, 0.24f, 3, 16f, 14f, 0, 0, 1.5f, "3 tüylük yelpaze", fx: Split),
                L(14f, 0.24f, 3, 16f, 14f, 1, 0, 1.5f, "YENİ: tüyler 1 düşmanı deler", fx: Split),
                L(15f, 0.24f, 3, 16f, 14f, 1, 0, 1.5f, "YENİ: her 5. atış patlayan dev tüy (3×)", 5, 3f, Split)
            }, crystal, prismDef);

            // 2) Egg Mortar → Supernova Omelette
            var eggEvo = WeaponPrefab<EggMortar>("SupernovaOmelette", b =>
            {
                Set(b, "eggSprite", Sprites["egg"]);
                Set(b, "isEvolved", true);
                Set(b, "chickBulletType", BChick);
            });
            WeaponDefinition eggEvoDef = Weapon("supernova_omelette", "Süpernova Omleti", Rarity.Legendary, BChick, eggEvo,
                new[] { L(45f, 1.2f, 4, 0, 0, 0, 2.4f, 0, "Her 5 atışta ekranı kaplayan omlet dalgası", 5, 6f) });
            var egg = WeaponPrefab<EggMortar>("EggMortar", b =>
            {
                Set(b, "eggSprite", Sprites["egg"]);
                Set(b, "chickBulletType", BChick);
            });
            BaseWeapons.Add(Weapon("egg_mortar", "Yumurta Havanı", Rarity.Common, BChick, egg, new[]
            {
                L(30f, 2.0f, 1, 0, 0, 0, 1.5f, 0, "Patlayan yumurta"),
                L(30f, 2.0f, 2, 0, 0, 0, 1.5f, 0, "2 yumurta"),
                L(30f, 2.0f, 2, 0, 0, 0, 2.1f, 0, "Patlama alanı +%40"),
                L(30f, 2.0f, 2, 0, 0, 0, 2.1f, 0, "Patlamadan 2 mini civciv çıkar"),
                L(30f, 1.4f, 4, 0, 0, 0, 2.1f, 0, "4 yumurta, daha sık")
            }, pan, eggEvoDef));

            // 3) Star Boomerang → Galaxy Vortex
            var starEvo = WeaponPrefab<StarBoomerang>("GalaxyVortex", b =>
            {
                Set(b, "starSprite", Sprites["star"]);
                Set(b, "isEvolved", true);
                Set(b, "fireworkBulletType", BStar);
            });
            WeaponDefinition starEvoDef = Weapon("galaxy_vortex", "Galaksi Girdabı", Rarity.Legendary, BStar, starEvo,
                new[] { L(12f, 5f, 1, 0, 0, 0, 2.5f, 4f, "Düşmanları çeken galaksi sarmalı") });
            var star = WeaponPrefab<StarBoomerang>("StarBoomerang", b => Set(b, "starSprite", Sprites["star"]));
            BaseWeapons.Add(Weapon("star_boomerang", "Yıldız Bumerang", Rarity.Common, BStar, star, new[]
            {
                L(15f, 1.8f, 1, 0, 10f, 0, 0, 1.6f, "Gidip dönen yıldız"),
                L(15f, 1.8f, 2, 40f, 10f, 0, 0, 1.6f, "2 yıldız"),
                L(15f, 1.8f, 2, 40f, 10f, 0, 0, 2.1f, "Menzil +%30, dönüşte 1.5× hasar"),
                L(15f, 1.8f, 3, 50f, 10f, 0, 0, 2.1f, "3 yıldız"),
                L(15f, 1.8f, 3, 50f, 10f, 0, 0, 2.1f, "Dönen yıldızlar gemi etrafında tur atar")
            }, moonDust, starEvoDef));

            // 4) Bubble Orbit → Gum Rings
            var bubbleEvo = WeaponPrefab<BubbleOrbit>("GumRings", b =>
            {
                Set(b, "bubbleSprite", Sprites["bubble"]);
                Set(b, "isEvolved", true);
                Set(b, "meteorBulletType", BMeteor);
            });
            WeaponDefinition bubbleEvoDef = Weapon("gum_rings", "Sakız Gezegen Halkaları", Rarity.Legendary, BMeteor, bubbleEvo,
                new[] { L(14f, 0.25f, 4, 0, 0, 0, 1.4f, 0, "Emen, yapıştıran ve biçen 3 halka") });
            var bubble = WeaponPrefab<BubbleOrbit>("BubbleOrbit", b => Set(b, "bubbleSprite", Sprites["bubble"]));
            BaseWeapons.Add(Weapon("bubble_orbit", "Sakız Balonu", Rarity.Rare, BMeteor, bubble, new[]
            {
                L(10f, 0.3f, 2, 0, 0, 0, 1.2f, 0, "Etrafında dönen 2 balon"),
                L(10f, 0.3f, 3, 0, 0, 0, 1.2f, 0, "3 balon"),
                L(10f, 0.3f, 3, 0, 0, 0, 1.2f, 0, "Balonlar düşman mermisi yutar"),
                L(10f, 0.3f, 4, 0, 0, 0, 1.5f, 0, "4 balon, daha geniş yörünge"),
                L(10f, 0.3f, 4, 0, 0, 0, 1.5f, 0, "Mermi yutan balon patlar")
            }, gum, bubbleEvoDef));

            // 5) Spark Cat → Storm Cat Goddess
            var catEvo = WeaponPrefab<SparkCat>("StormCat", b =>
            {
                Set(b, "catSprite", Sprites["cat"]);
                Set(b, "isEvolved", true);
            });
            WeaponDefinition catEvoDef = Weapon("storm_cat", "Fırtına Kedisi Tanrıçası", Rarity.Legendary, BStar, catEvo,
                new[] { L(30f, 0.5f, 3, 0, 0, 0, 4f, 0, "Yıldırım yağmuru ve elektrikli papatyalar", 16, 1.2f) });
            var cat = WeaponPrefab<SparkCat>("SparkCat", b => Set(b, "catSprite", Sprites["cat"]));
            BaseWeapons.Add(Weapon("spark_cat", "Kıvılcım Kedi", Rarity.Rare, BStar, cat, new[]
            {
                L(18f, 1.2f, 2, 0, 0, 0, 3.5f, 0, "Zincirleme şimşek (2 sekme)"),
                L(18f, 1.2f, 4, 0, 0, 0, 3.5f, 0, "4 sekme"),
                L(18f, 1.2f, 4, 0, 0, 0, 3.5f, 0, "Vurulan düşman sersemler"),
                L(18f, 1.2f, 4, 0, 0, 0, 3.5f, 0, "İkinci kedi"),
                L(18f, 1.2f, 4, 0, 0, 0, 3.5f, 0, "Sekmelerde hasar azalmaz")
            }, battery, catEvoDef));

            // 6) Fish Missile → Shark Swarm (ad-rewards.md: the first weapons offered for ads)
            var sharkPrefab = WeaponPrefab<FishMissile>("SharkSwarm", b =>
            {
                Set(b, "fishSprite", Sprites["shark"]);
                Set(b, "isEvolved", true);
            });
            WeaponDefinition sharkDef = Weapon("shark_swarm", "Köpekbalığı Sürüsü", Rarity.Legendary, BStar, sharkPrefab,
                new[] { L(24f, 0.8f, 4, 0, 10f, 1, 1.2f, 3.5f, "4 köpekbalığı; her vuruşta 2 yavru balık") });
            var fishPrefab = WeaponPrefab<FishMissile>("FishMissile", b => Set(b, "fishSprite", Sprites["fish"]));
            BaseWeapons.Add(Weapon("fish_missile", "Balık Füzesi", Rarity.Rare, BStar, fishPrefab, new[]
            {
                L(14f, 1.1f, 2, 0, 9f, 0, 0f, 3f, "Hedef takip eden 2 balık"),
                L(14f, 1.1f, 3, 0, 9f, 0, 0f, 3f, "3 balık"),
                L(14f, 1.1f, 3, 0, 9f, 0, 0.8f, 3f, "Çarpınca küçük patlama"),
                L(14f, 0.95f, 4, 0, 10f, 0, 0.8f, 3f, "4 balık, daha sık"),
                L(16f, 0.95f, 5, 0, 10f, 1, 1.0f, 3f, "5 balık; her biri 2 düşmana çarpar")
            }, sharpClaws, sharkDef));

            // 7) Yarn Ball → Cosmic Yarn (Ball Blast's bouncing ball)
            var cosmicPrefab = WeaponPrefab<YarnBall>("CosmicYarn", b =>
            {
                Set(b, "yarnSprite", Sprites["yarn"]);
                Set(b, "isEvolved", true);
            });
            WeaponDefinition cosmicDef = Weapon("cosmic_yarn", "Kozmik Yumak", Rarity.Legendary, BStar, cosmicPrefab,
                new[] { L(22f, 1f, 1, 0, 9f, 0, 1.1f, 12f, "Dev yumak; her sekmede mini yumaklar") });
            var yarnPrefab = WeaponPrefab<YarnBall>("YarnBall", b => Set(b, "yarnSprite", Sprites["yarn"]));
            BaseWeapons.Add(Weapon("yarn_ball", "Yün Yumağı", Rarity.Rare, BStar, yarnPrefab, new[]
            {
                L(10f, 1f, 1, 0, 8f, 0, 0.45f, 8f, "Kenarlardan seken 1 yumak"),
                L(10f, 1f, 2, 0, 8f, 0, 0.45f, 8f, "2 yumak"),
                L(10f, 1f, 2, 0, 8f, 0, 0.6f, 8f, "Yumaklar büyür"),
                L(10f, 1f, 3, 0, 8.5f, 0, 0.6f, 8f, "3 yumak"),
                L(12f, 1f, 3, 0, 8.5f, 0, 0.6f, 8f, "Her sekme hasarı %10 artırır (en çok 5)")
            }, carrot, cosmicDef));

            BuildHeroGuns(crystal);
            BaseWeapons.Insert(0, StartingWeapon);
            _evolved["shark_swarm"] = sharkDef;
            _evolved["cosmic_yarn"] = cosmicDef;
            _evolved["feather_storm"] = prismDef;
            _evolved["supernova_omelette"] = eggEvoDef;
            _evolved["galaxy_vortex"] = starEvoDef;
            _evolved["gum_rings"] = bubbleEvoDef;
            _evolved["storm_cat"] = catEvoDef;
        }

        /// <summary>
        /// hero-guns.md: every hero shoots from the ship with a gun of their own (device feedback 2026-09-25: "her
        /// kahramanın kendine has silahı olmalı"). Straight shooters on the FeatherBlaster behaviour, each with its
        /// own bullet and pattern, all evolving with Kristal Gözlük like the Feather Blaster.
        /// </summary>
        private void BuildHeroGuns(PassiveDefinition key)
        {
            void Gun(string id, string name, int bullet, WeaponLevelStats[] levels, string evoId, string evoName, WeaponLevelStats evo)
            {
                var evoPrefab = WeaponPrefab<FeatherBlaster>(evoId, b => Set(b, "giantBulletTypeIndex", -1));
                WeaponDefinition evoDef = Weapon(evoId, evoName, Rarity.Legendary, bullet, evoPrefab, new[] { evo });
                evoDef.heroOnly = true;
                EditorUtility.SetDirty(evoDef);
                var prefab = WeaponPrefab<FeatherBlaster>(id, b => Set(b, "giantBulletTypeIndex", -1));
                WeaponDefinition def = Weapon(id, name, Rarity.Common, bullet, prefab, levels, key, evoDef);
                def.heroOnly = true;
                EditorUtility.SetDirty(def);
                HeroGuns.Add(def);
            }

            // Each level unlocks a new trait (explode / split / chain / slow) so every gun grows differently.
            Gun("chick_cannon", "Civciv Topu", BChick, new[]
            {
                L(26f, 0.30f, 1, 0, 11f, 0, 0, 1.8f, "Ağır civciv topu"),
                L(26f, 0.30f, 1, 0, 11f, 0, 0, 1.8f, "YENİ: toplar çarpınca patlar", fx: Boom),
                L(26f, 0.28f, 2, 0, 11f, 0, 0, 1.8f, "2 patlayan top", fx: Boom),
                L(28f, 0.28f, 2, 0, 11f, 0, 0, 1.8f, "YENİ: patlamadan 2 yavru civciv fırlar", fx: Boom | Split),
                L(30f, 0.26f, 3, 20f, 11f, 0, 0, 1.8f, "3 top; her 4. atış 3 kat", 4, 3f, Boom | Split)
            }, "mega_chick_cannon", "Dev Civciv Topu", L(40f, 0.24f, 4, 24f, 12f, 1, 0, 1.8f, "4 dev top: patlar, yavrular, deler", 4, 3f, Boom | Split));

            Gun("spark_pistol", "Kıvılcım Tabancası", BSpark, new[]
            {
                L(8f, 0.13f, 1, 0, 18f, 0, 0, 1.2f, "Çok hızlı kıvılcım"),
                L(8f, 0.13f, 1, 0, 18f, 0, 0, 1.2f, "YENİ: kıvılcım yandaki düşmana sıçrar", fx: Chain),
                L(8f, 0.12f, 2, 0, 18f, 0, 0, 1.2f, "2 kıvılcım", fx: Chain),
                L(9f, 0.12f, 2, 0, 18f, 0, 0, 1.2f, "YENİ: elektrik çarpan düşman yavaşlar", fx: Chain | Slow),
                L(10f, 0.11f, 3, 12f, 19f, 1, 0, 1.2f, "YENİ: kıvılcımlar 1 düşmanı deler", fx: Chain | Slow)
            }, "thunder_pistol", "Yıldırım Tabancası", L(12f, 0.10f, 4, 16f, 20f, 2, 0, 1.2f, "Kıvılcım fırtınası: sıçrar, yavaşlatır, 2 deler", fx: Chain | Slow));

            Gun("bubble_rifle", "Balon Tüfeği", BMeteor, new[]
            {
                L(16f, 0.30f, 2, 0, 11f, 1, 0, 1.8f, "2 delici balon"),
                L(16f, 0.30f, 2, 0, 11f, 1, 0, 1.8f, "YENİ: sakız yapışır, düşman yavaşlar", fx: Slow),
                L(16f, 0.28f, 3, 20f, 11f, 1, 0, 1.8f, "3 balon, yelpaze", fx: Slow),
                L(18f, 0.28f, 3, 20f, 11f, 1, 0, 1.8f, "YENİ: balonlar çarpınca patlar", fx: Slow | Boom),
                L(20f, 0.26f, 4, 24f, 12f, 2, 0, 1.8f, "4 balon, 2 düşman deler", fx: Slow | Boom)
            }, "bubble_storm", "Balon Fırtınası", L(22f, 0.24f, 6, 36f, 12f, 3, 0, 1.8f, "6 balon: yapışır, patlar, 3 deler", fx: Slow | Boom));

            Gun("star_bow", "Yıldız Yayı", BStar, new[]
            {
                L(11f, 0.26f, 3, 24f, 14f, 0, 0, 1.4f, "3 yıldızlık yelpaze"),
                L(11f, 0.26f, 3, 24f, 14f, 0, 0, 1.4f, "YENİ: yıldızlar yandaki düşmana sıçrar", fx: Chain),
                L(11f, 0.24f, 4, 30f, 14f, 0, 0, 1.4f, "4 yıldız", fx: Chain),
                L(12f, 0.24f, 4, 30f, 14f, 0, 0, 1.4f, "YENİ: yıldızlar ikiye bölünür", fx: Chain | Split),
                L(13f, 0.22f, 5, 40f, 15f, 0, 0, 1.4f, "5 yıldız; her 5. atış 3 kat", 5, 3f, Chain | Split)
            }, "comet_bow", "Kuyruklu Yıldız Yayı", L(15f, 0.2f, 7, 52f, 16f, 1, 0, 1.4f, "7 yıldız: sıçrar, bölünür, deler", 5, 3f, Chain | Split));

            Gun("ice_gun", "Buz Tabancası", BIce, new[]
            {
                L(14f, 0.24f, 2, 0, 17f, 1, 0, 1.3f, "2 delici buz sarkıtı"),
                L(14f, 0.24f, 2, 0, 17f, 1, 0, 1.3f, "YENİ: buz dondurur, düşman yavaşlar", fx: Slow),
                L(14f, 0.22f, 3, 0, 17f, 2, 0, 1.3f, "3 sarkıt, 2 düşman deler", fx: Slow),
                L(15f, 0.22f, 3, 10f, 18f, 2, 0, 1.3f, "YENİ: buz çarpınca parçalara ayrılır", fx: Slow | Split),
                L(17f, 0.20f, 4, 14f, 18f, 3, 0, 1.3f, "4 sarkıt, 3 düşman deler", fx: Slow | Split)
            }, "glacier_gun", "Buzul Topu", L(19f, 0.18f, 5, 18f, 19f, 4, 0, 1.3f, "Buzul yağmuru: dondurur, parçalanır, 4 deler", fx: Slow | Split));

            Gun("yarn_launcher", "Yün Atar", BYarn, new[]
            {
                L(22f, 0.34f, 1, 0, 10f, 0, 0, 2f, "İri yün yumağı"),
                L(22f, 0.34f, 1, 0, 10f, 0, 0, 2f, "YENİ: yün dolanır, düşman yavaşlar", fx: Slow),
                L(22f, 0.32f, 2, 0, 10f, 0, 0, 2f, "YENİ: yumak çözülüp ikiye bölünür", fx: Slow | Split),
                L(24f, 0.30f, 2, 0, 10f, 0, 0, 2f, "YENİ: yumaklar patlar", fx: Slow | Split | Boom),
                L(26f, 0.28f, 3, 18f, 11f, 1, 0, 2f, "3 yumak; her 4. atış 3 kat", 4, 3f, Slow | Split | Boom)
            }, "yarn_cyclone", "Yün Kasırgası", L(30f, 0.26f, 4, 24f, 11f, 2, 0, 2f, "Yün kasırgası: dolanır, bölünür, patlar", 4, 3f, Slow | Split | Boom));
        }

        // ---------------------------------------------------------------- Lab, fusions, Hangar, Constellation

        /// <summary>
        /// meta-economy.md §3.3 C: 3 weapons + 4 passives free, the rest bought in the Lab — or unlocked by
        /// watching rewarded ads (ad-rewards.md §3.1).
        /// </summary>
        private void ApplyLabCosts()
        {
            var costs = new Dictionary<string, int>
            {
                ["star_boomerang"] = 1200, ["bubble_orbit"] = 2000, ["fish_missile"] = 3500, ["yarn_ball"] = 5000,
                ["moon_dust"] = 300, ["stretchy_gum"] = 300, ["lucky_clover"] = 500,
                ["double_barrel"] = 900, ["glass_cannon"] = 600, ["last_stand"] = 450
            };
            // Pilots and weapons are bought, never unlocked by ads (device feedback 2026-09-25: "karakter açmak
            // 3-5 video ile olmaz"). Ads pay gold and Stardust instead (ad-rewards.md §3.3).
            var ads = new Dictionary<string, int>();
            foreach (WeaponDefinition w in BaseWeapons)
            {
                w.labCost = costs.TryGetValue(w.id, out int c) ? c : 0;
                w.adsToUnlock = ads.TryGetValue(w.id, out int a) ? a : 0;
                EditorUtility.SetDirty(w);
            }
            foreach (PassiveDefinition p in Passives)
            {
                p.labCost = costs.TryGetValue(p.id, out int c) ? c : 0;
                EditorUtility.SetDirty(p);
            }
        }

        /// <summary>weapon-system.md §3.4 fusions: both evolutions run together in one slot.</summary>
        private void BuildFusions()
        {
            void Fusion(string id, string name, string a, string b)
            {
                WeaponDefinition partA = _evolved[a], partB = _evolved[b];
                FusionWeapon prefab = WeaponPrefab<FusionWeapon>("Fusion_" + id, f => SetArray(f, "parts", new Object[] { partA, partB }));
                WeaponDefinition result = Weapon(id, name, Rarity.Legendary, BStar, prefab,
                    new[] { L(0f, 999f, 0, 0, 0, 0, 0, 0, $"{partA.displayName} + {partB.displayName}") });

                var recipe = ScriptableObject.CreateInstance<FusionRecipe>();
                recipe.a = partA;
                recipe.b = partB;
                recipe.result = result;
                recipe.damageBonus = 0.25f;
                Fusions.Add(SaveAsset(recipe, "Data/Fusions", id));
            }

            Fusion("rainbow_storm", "Gökkuşağı Fırtınası", "feather_storm", "storm_cat");
            Fusion("cosmic_breakfast", "Kozmik Kahvaltı", "supernova_omelette", "galaxy_vortex");
            Fusion("candy_shield_galaxy", "Şeker Kalkanı Galaksisi", "gum_rings", "galaxy_vortex");
        }

        /// <summary>meta-economy.md §3.3 B.</summary>
        private void BuildCharacters()
        {
            WeaponDefinition W(string id)
            {
                foreach (WeaponDefinition w in BaseWeapons) if (w.id == id) return w;
                return StartingWeapon;
            }

            void C(string id, string name, string perk, string sprite, string weapon, int gold, int dust,
                CharacterPerk special = CharacterPerk.None, int chapterGate = -1, string gun = null, params StatModifier[] mods)
            {
                var c = ScriptableObject.CreateInstance<CharacterDefinition>();
                c.id = id;
                c.displayName = name;
                c.perkText = perk;
                c.sprite = Sprites[sprite == "bunny" ? "pilot_bunny" : sprite];
                c.shipSprite = Sprites[sprite switch
                {
                    "bunny" => "bunny",
                    "pilot_chick" => "ship_chick",
                    "pilot_cat" => "ship_cat",
                    "pilot_hamster" => "ship_hamster",
                    "pilot_fox" => "ship_fox",
                    "pilot_penguin" => "ship_penguin",
                    "pilot_lamb" => "ship_lamb",
                    _ => "ship_mystery"
                }];
                c.startingWeapon = W(weapon);
                c.mainGun = gun == null ? StartingWeapon : HeroGuns.Find(g => g.id == gun);
                c.goldCost = gold;
                c.stardustCost = dust;
                c.adsToUnlock = 0; // pilots are bought, never unlocked by ads (ad-rewards.md, 2026-09-25)
                c.perk = special;
                c.requiresChapterCleared = chapterGate;
                c.modifiers = mods;
                Characters.Add(SaveAsset(c, "Data/Characters", id));
            }

            C("pitir", "Pıtır", "Tavşan. Her 10 seviyede +1 kart seçeneği.", "bunny", "feather_blaster", 0, 0,
                CharacterPerk.CardEvery10Levels);
            C("civik", "Cıvık", "Civciv. Patlamalar %20 büyük, can -%10.", "pilot_chick", "egg_mortar", 1500, 0, gun: "chick_cannon", // first pilot: reachable in ~4 runs, no Stardust gate
                mods: new[] { new StatModifier(StatType.Area, 0.2f), new StatModifier(StatType.MaxHp, -0.1f) });
            C("mirnav", "Mırnav", "Kedi. Sersemletme süresi 2 kat.", "pilot_cat", "spark_cat", 6000, 25, gun: "spark_pistol",
                mods: new StatModifier(StatType.StunDuration, 1f));
            C("balonbas", "Balonbaş", "Hamster. Yuttuğu her mermi 1 can.", "pilot_hamster", "bubble_orbit", 10000, 40, gun: "bubble_rifle",
                mods: new StatModifier(StatType.AbsorbHeal, 1f));
            C("yildizpati", "Yıldızpati", "Tilki. Her evrim +%15 hasar.", "pilot_fox", "star_boomerang", 16000, 60, gun: "star_bow",
                mods: new StatModifier(StatType.EvolutionDamage, 0.15f));
            C("pengu", "Pengu", "Penguen. Mermiler %25 hızlı, +%5 kritik.", "pilot_penguin", "fish_missile", 13000, 50, gun: "ice_gun",
                mods: new[] { new StatModifier(StatType.ProjectileSpeed, 0.25f), new StatModifier(StatType.CritChance, 0.05f) });
            C("kuzu", "Kuzu", "Kuzu. Can +%30, alan +%10.", "pilot_lamb", "yarn_ball", 22000, 80, gun: "yarn_launcher",
                mods: new[] { new StatModifier(StatType.MaxHp, 0.3f), new StatModifier(StatType.Area, 0.1f) });
            C("gizli", "Gökkuşağı Pıtır", "Gizli. Her koşu rastgele bir pasifle başlar.", "pilot_mystery", "feather_blaster", 0, 0,
                CharacterPerk.RandomPassive, chapterGate: 2);
        }

        /// <summary>
        /// meta-economy.md §3.3 D: 30 nodes in three 10-node branches; costs 2→10 per branch
        /// (50 each, 150 total ≈ 30 boss wins). Each node requires the previous one in its branch.
        /// </summary>
        private void BuildConstellation()
        {
            int[] costs = { 2, 2, 3, 3, 4, 5, 6, 7, 8, 10 };
            var nodes = new List<ConstellationNode>();

            void Branch(int branch, string prefix, (string name, string desc, StatType stat, float value)[] steps)
            {
                for (int i = 0; i < steps.Length; i++)
                {
                    nodes.Add(new ConstellationNode
                    {
                        id = $"{prefix}_{i + 1}",
                        displayName = steps[i].name,
                        description = steps[i].desc,
                        branch = branch,
                        stardustCost = costs[i],
                        requires = i == 0 ? System.Array.Empty<string>() : new[] { $"{prefix}_{i}" },
                        modifiers = new[] { new StatModifier(steps[i].stat, steps[i].value) }
                    });
                }
            }

            Branch(0, "atk", new[]
            {
                ("Kıvılcım", "+%3 hasar", StatType.Damage, 0.03f),
                ("Keskin Göz", "+%2 kritik şansı", StatType.CritChance, 0.02f),
                ("Alev", "+%3 hasar", StatType.Damage, 0.03f),
                ("Çevik Pati", "+%2 atış hızı", StatType.CooldownReduction, 0.02f),
                ("Geniş Kanat", "+%5 alan", StatType.Area, 0.05f),
                ("Yıldız Ateşi", "+%4 hasar", StatType.Damage, 0.04f),
                ("Rüzgâr", "+%8 mermi hızı", StatType.ProjectileSpeed, 0.08f),
                ("Uzun Kuyruk", "+%8 süre", StatType.Duration, 0.08f),
                ("Süpernova", "+%5 hasar", StatType.Damage, 0.05f),
                ("Evrim Yıldızı", "Evrim sandığı +1 pasif seviyesi verir", StatType.EvolutionChestLevels, 1f)
            });
            Branch(1, "def", new[]
            {
                ("Pamuk", "+%5 maks. can", StatType.MaxHp, 0.05f),
                ("Kabuk", "Alınan hasar -1", StatType.Armor, 1f),
                ("Tatlı Diş", "+%10 şeker dolumu", StatType.RushGain, 0.10f),
                ("Yastık", "+%5 maks. can", StatType.MaxHp, 0.05f),
                ("Kıl Payı", "Kıl payı XP'si 2 kat", StatType.GrazeXp, 1f),
                ("Zırh", "Alınan hasar -1", StatType.Armor, 1f),
                ("Kalp", "+%8 maks. can", StatType.MaxHp, 0.08f),
                ("Anka", "+1 diriliş", StatType.Revives, 1f),
                ("Şeker Ustası", "+%15 şeker dolumu", StatType.RushGain, 0.15f),
                ("Kale", "Alınan hasar -2", StatType.Armor, 2f)
            });
            Branch(2, "luck", new[]
            {
                ("Kese", "+%5 altın", StatType.GoldGain, 0.05f),
                ("Yonca", "+%3 şans", StatType.Luck, 0.03f),
                ("Bilge", "+%3 XP", StatType.Experience, 0.03f),
                ("Taç Avcısı", "Elitlerden +%25 altın", StatType.EliteGold, 0.25f),
                ("Hazine", "+%5 altın", StatType.GoldGain, 0.05f),
                ("İkinci Şans", "+1 yeniden çek", StatType.Rerolls, 1f),
                ("Âlim", "+%4 XP", StatType.Experience, 0.04f),
                ("Seçici", "+1 yasakla", StatType.Banishes, 1f),
                ("Define", "+%10 altın", StatType.GoldGain, 0.10f),
                ("Dördüncü Kart", "Seviye atlamada 4 kart", StatType.DraftChoices, 1f)
            });

            var board = ScriptableObject.CreateInstance<ConstellationDefinition>();
            board.nodes = nodes.ToArray();
            Constellation = SaveAsset(board, "Data", "Constellation");
        }

        // ---------------------------------------------------------------- Enemies

        private Enemy EnemyPrefab<T>(string name, string sprite, float size, System.Action<T> configure) where T : Enemy
        {
            var go = new GameObject(name);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprites[sprite];
            renderer.sharedMaterial = SpriteMaterial;
            go.transform.localScale = new Vector3(size, size, 1f);

            T enemy = go.AddComponent<T>();
            Set(enemy, "spriteRenderer", renderer);
            Set(enemy, "flashMaterial", FlashMaterial);
            Set(enemy, "fxColor", FxColors.TryGetValue(sprite, out Color fx) ? fx : ArtRecipes.Pink);
            if (typeof(T) == typeof(BossEnemy)) Set(enemy, "wobble", 0.025f);
            configure(enemy);
            Enemy saved = SavePrefab<T>(go, "E_" + name);
            Enemies[name] = saved;
            return saved;
        }

        private static readonly Dictionary<string, Color> FxColors = new()
        {
            ["chick"] = ArtRecipes.Chick, ["chick_elite"] = ArtRecipes.Honey, ["jelly_bear"] = ArtRecipes.Lilac,
            ["jelly_king"] = ArtRecipes.Lilac, ["cookie_robot"] = ArtRecipes.Cookie, ["cookie_mech"] = ArtRecipes.Cookie,
            ["ice_cream"] = ArtRecipes.Mint, ["gum_balloon"] = ArtRecipes.Hex(0xFF7AC2), ["queen_hen"] = ArtRecipes.Coral,
            ["donut_ufo"] = ArtRecipes.Hex(0xFF8FC2), ["candy_bee"] = ArtRecipes.Honey, ["marshmallow"] = ArtRecipes.Hex(0xFFF3F7)
        };

        private static void Stats(Enemy e, float hp, float radius, int xp, int gold, float fall, float fire,
            int bullets, float spread, float speed, float damage, bool aim, int type = BEnemy)
        {
            Set(e, "baseHp", hp);
            Set(e, "hitRadius", radius);
            Set(e, "xpValue", xp);
            Set(e, "goldValue", gold);
            Set(e, "velocity", new Vector2(0f, -fall));
            Set(e, "fireInterval", fire);
            Set(e, "bulletsPerVolley", bullets);
            Set(e, "volleySpreadDegrees", spread);
            Set(e, "bulletSpeed", speed);
            Set(e, "bulletDamage", damage);
            Set(e, "aimAtPlayer", aim);
            Set(e, "bulletTypeIndex", type);
        }

        private void BuildEnemies()
        {
            EnemyPrefab<Enemy>("Chick", "chick", 0.9f, e => Stats(e, 10, 0.38f, 1, 1, 1.4f, 5f, 1, 0, 2.8f, 10, true));
            EnemyPrefab<Enemy>("JellyBear", "jelly_bear", 1.2f, e => Stats(e, 45, 0.55f, 3, 2, 0.8f, 5f, 3, 30, 2.8f, 12, true, BEnemyBig));
            EnemyPrefab<Enemy>("CookieRobot", "cookie_robot", 1f, e => Stats(e, 25, 0.45f, 3, 1, 1f, 3.4f, 1, 0, 4f, 12, true));
            EnemyPrefab<Enemy>("IceCreamTower", "ice_cream", 1.3f, e => Stats(e, 70, 0.6f, 4, 3, 0.6f, 3.5f, 8, 315, 2.4f, 10, false));
            EnemyPrefab<Enemy>("GumBalloon", "gum_balloon", 1f, e => Stats(e, 20, 0.45f, 3, 1, 1.2f, 6f, 6, 300, 2.5f, 10, false));
            // Added 2026-09-25 (device feedback: "tek düşman tipi var"): each moves and shoots differently.
            EnemyPrefab<Enemy>("CandyBee", "candy_bee", 0.8f, e =>
            {
                // Fast and swervy, one quick aimed shot: hard to track, easy to kill.
                Stats(e, 8, 0.34f, 1, 1, 2.4f, 4f, 1, 0, 4.2f, 8, true);
                Set(e, "swayAmplitude", 1.8f);
                Set(e, "swayFrequency", 2.6f);
            });
            EnemyPrefab<Enemy>("DonutUfo", "donut_ufo", 1.15f, e =>
            {
                // Slides wide across the screen and drops a 3-shot fan straight down.
                Stats(e, 30, 0.55f, 3, 2, 0.7f, 3f, 3, 40, 3.2f, 10, false);
                Set(e, "swayAmplitude", 2.6f);
                Set(e, "swayFrequency", 0.9f);
            });
            EnemyPrefab<Enemy>("Marshmallow", "marshmallow", 1.35f, e =>
            {
                // Slow tank: soaks damage and sprays a slow ring you must weave through.
                Stats(e, 110, 0.65f, 6, 4, 0.45f, 4.5f, 10, 360, 2f, 10, false, BEnemyBig);
                Set(e, "swayAmplitude", 0.3f);
            });
            EnemyPrefab<Enemy>("EliteChick", "chick_elite", 1.3f, e =>
            {
                Stats(e, 180, 0.6f, 25, 5, 0.7f, 1.8f, 3, 25, 3.6f, 12, true);
                Set(e, "isElite", true);
            });

            BossPattern P(BossPatternKind kind, int bullets, float speed, float spread, float spin, float dmg, bool special = false) =>
                new() { kind = kind, bullets = bullets, speed = speed, spreadDegrees = spread, spinPerVolley = spin,
                        damage = dmg, bulletTypeIndex = BEnemy, special = special };

            EnemyPrefab<BossEnemy>("JellyKing", "jelly_king", 3f, b =>
            {
                Stats(b, 1200, 1.3f, 60, 20, 0f, 1.3f, 0, 0, 0, 0, false);
                Set(b, "formationHoldSeconds", 9999f);
                Set(b, "specialBulletTypeIndex", BBossSpecial);
                SetPhases(b, new[]
                {
                    Phase(1f, 1.3f, P(BossPatternKind.Ring, 14, 2.8f, 0, 12, 10), P(BossPatternKind.AimedFan, 5, 3.5f, 40, 0, 12)),
                    Phase(0.5f, 1.0f, P(BossPatternKind.Spiral, 0, 3f, 3, 17, 10), P(BossPatternKind.Ring, 18, 3f, 0, 10, 14, true))
                });
            });
            EnemyPrefab<BossEnemy>("CookieMech", "cookie_mech", 3f, b =>
            {
                Stats(b, 2000, 1.3f, 90, 30, 0f, 1.1f, 0, 0, 0, 0, false);
                Set(b, "formationHoldSeconds", 9999f);
                Set(b, "specialBulletTypeIndex", BBossSpecial);
                SetPhases(b, new[]
                {
                    Phase(1f, 1.1f, P(BossPatternKind.AimedFan, 7, 4f, 50, 0, 12), P(BossPatternKind.Spiral, 0, 3f, 4, 15, 10)),
                    Phase(0.6f, 0.9f, P(BossPatternKind.Rain, 12, 3.2f, 0, 0, 12), P(BossPatternKind.AimedFan, 9, 4.2f, 70, 0, 12)),
                    Phase(0.25f, 0.8f, P(BossPatternKind.Ring, 24, 3.2f, 0, 7, 14, true), P(BossPatternKind.Spiral, 0, 3.4f, 5, 13, 12))
                });
            });
            EnemyPrefab<BossEnemy>("QueenHen", "queen_hen", 3.6f, b =>
            {
                Stats(b, 4000, 1.6f, 200, 60, 0f, 1.0f, 0, 0, 0, 0, false);
                Set(b, "formationHoldSeconds", 9999f);
                Set(b, "specialBulletTypeIndex", BBossSpecial);
                SetPhases(b, new[]
                {
                    Phase(1f, 1.0f, P(BossPatternKind.Ring, 16, 3f, 0, 11, 12), P(BossPatternKind.AimedFan, 5, 4f, 30, 0, 12)),
                    Phase(0.66f, 0.85f, P(BossPatternKind.Spiral, 0, 3.2f, 4, 14, 12), P(BossPatternKind.Rain, 14, 3.4f, 0, 0, 12),
                        P(BossPatternKind.AimedFan, 12, 3.2f, 120, 0, 15, true)),
                    Phase(0.33f, 0.7f, P(BossPatternKind.Ring, 28, 3.4f, 0, 6, 15, true), P(BossPatternKind.Spiral, 0, 3.6f, 6, 12, 12),
                        P(BossPatternKind.Rain, 16, 3.8f, 0, 0, 14))
                });
            });
        }

        private static BossPhase Phase(float threshold, float interval, params BossPattern[] patterns) =>
            new() { hpThreshold = threshold, fireInterval = interval, patterns = patterns };

        private static void SetPhases(BossEnemy boss, BossPhase[] phases)
        {
            var so = new SerializedObject(boss);
            SerializedProperty list = so.FindProperty("phases");
            list.arraySize = phases.Length;
            for (int i = 0; i < phases.Length; i++)
            {
                SerializedProperty ph = list.GetArrayElementAtIndex(i);
                ph.FindPropertyRelative("hpThreshold").floatValue = phases[i].hpThreshold;
                ph.FindPropertyRelative("fireInterval").floatValue = phases[i].fireInterval;
                SerializedProperty pats = ph.FindPropertyRelative("patterns");
                pats.arraySize = phases[i].patterns.Length;
                for (int k = 0; k < phases[i].patterns.Length; k++)
                {
                    BossPattern src = phases[i].patterns[k];
                    SerializedProperty dst = pats.GetArrayElementAtIndex(k);
                    dst.FindPropertyRelative("kind").enumValueIndex = (int)src.kind;
                    dst.FindPropertyRelative("bullets").intValue = src.bullets;
                    dst.FindPropertyRelative("speed").floatValue = src.speed;
                    dst.FindPropertyRelative("spreadDegrees").floatValue = src.spreadDegrees;
                    dst.FindPropertyRelative("spinPerVolley").floatValue = src.spinPerVolley;
                    dst.FindPropertyRelative("damage").floatValue = src.damage;
                    dst.FindPropertyRelative("bulletTypeIndex").intValue = src.bulletTypeIndex;
                    dst.FindPropertyRelative("special").boolValue = src.special;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- Runs (game-concept.md §3.2)

        private void BuildRuns()
        {
            string[] names = { "Şekerkamışı", "Jöle Nebulası", "Kurabiye Kuşağı" };
            for (int c = 0; c < names.Length; c++)
            {
                var run = ScriptableObject.CreateInstance<RunDefinition>();
                run.chapterIndex = c;
                run.breatherSeconds = 9f;
                run.maxLiveFormations = 2;
                run.phases = BuildTimeline(names[c]);
                Runs.Add(SaveAsset(run, "Data/Runs", $"Run_{c + 1}"));
            }
        }

        private RunPhase[] BuildTimeline(string chapterName)
        {
            SwarmEntry S(string enemy, float cost, float weight) => new() { prefab = Enemies[enemy], threatCost = cost, weight = weight };
            FormationEntry F(string enemy, FormationShape shape, int rows, int cols, float weight, float spacing = 0.95f) =>
                new() { prefab = Enemies[enemy], shape = shape, rows = rows, columns = cols, spacing = spacing, weight = weight };

            // Formations are not all chicks any more: bees, robots, donuts and bears arrive in their own shapes.
            FormationEntry[] early = { F("Chick", FormationShape.Grid, 3, 5, 1), F("CandyBee", FormationShape.V, 1, 7, 1) };
            FormationEntry[] mixed =
            {
                F("Chick", FormationShape.Grid, 3, 5, 1), F("CandyBee", FormationShape.V, 1, 7, 1),
                F("CookieRobot", FormationShape.Arc, 1, 6, 1, 1.05f), F("DonutUfo", FormationShape.Grid, 1, 3, 0.7f, 1.6f),
                F("JellyBear", FormationShape.Grid, 2, 3, 0.6f, 1.25f)
            };

            return new[]
            {
                new RunPhase { label = $"{chapterName} — Dalga 1", startMinute = 0f,
                    swarm = new[] { S("Chick", 1, 10), S("CandyBee", 1.5f, 5), S("GumBalloon", 4, 2) },
                    formations = early, formationInterval = 20f },
                new RunPhase { label = "Dalga 2", startMinute = 1.2f,
                    swarm = new[] { S("Chick", 1, 8), S("CandyBee", 1.5f, 4), S("CookieRobot", 3, 4), S("DonutUfo", 4, 3), S("JellyBear", 5, 2) },
                    formations = mixed, formationInterval = 18f },
                new RunPhase { label = "Mini-Boss: Jöle Kral!", kind = PhaseKind.MiniBoss, startMinute = 3f,
                    swarm = new[] { S("Chick", 1, 10) }, budgetScale = 0.3f, formationInterval = 0f, bossPrefab = Enemies["JellyKing"] },
                new RunPhase { label = "Dalga 3", startMinute = 3.2f,
                    swarm = new[] { S("Chick", 1, 8), S("CandyBee", 1.5f, 4), S("CookieRobot", 3, 4), S("DonutUfo", 4, 3),
                        S("JellyBear", 5, 3), S("GumBalloon", 4, 2), S("Marshmallow", 8, 1.5f), S("EliteChick", 25, 0.4f) },
                    formations = mixed, formationInterval = 16f },
                new RunPhase { label = "Mini-Boss: Kurabiye Robot Ana!", kind = PhaseKind.MiniBoss, startMinute = 6f,
                    swarm = new[] { S("Chick", 1, 10) }, budgetScale = 0.3f, formationInterval = 0f, bossPrefab = Enemies["CookieMech"] },
                new RunPhase { label = "Dalga 5: Kaos", startMinute = 6.2f, budgetScale = 1.2f,
                    swarm = new[] { S("Chick", 1, 8), S("CandyBee", 1.5f, 4), S("CookieRobot", 3, 4), S("DonutUfo", 4, 3),
                        S("JellyBear", 5, 3), S("GumBalloon", 4, 2), S("Marshmallow", 8, 2), S("IceCreamTower", 6, 2),
                        S("EliteChick", 25, 0.6f) },
                    formations = mixed, formationInterval = 12f },
                new RunPhase { label = "Final: Kraliçe Tavuk!", kind = PhaseKind.FinalBoss, startMinute = 8f,
                    swarm = new[] { S("Chick", 1, 6) }, budgetScale = 0.2f, formationInterval = 0f, bossPrefab = Enemies["QueenHen"] }
            };
        }

        // ---------------------------------------------------------------- Workshop (meta-economy.md §3.3 A)

        private void BuildWorkshop()
        {
            void U(string id, string name, StatType stat, float effect, int max, int baseCost, float growth)
            {
                var u = ScriptableObject.CreateInstance<MetaUpgradeDefinition>();
                u.id = id;
                u.displayName = name;
                u.stat = stat;
                u.effectPerLevel = effect;
                u.maxLevel = max;
                u.baseCost = baseCost;
                u.costGrowth = growth;
                Workshop.Add(SaveAsset(u, "Data/Workshop", id));
            }

            U("health", "Can", StatType.MaxHp, 0.08f, 10, 80, 1.32f);
            U("damage", "Hasar", StatType.Damage, 0.05f, 10, 100, 1.35f);
            U("fire_rate", "Atış Hızı", StatType.CooldownReduction, 0.03f, 10, 120, 1.36f);
            U("gold", "Altın Kazancı", StatType.GoldGain, 0.10f, 10, 80, 1.35f); // replaced Mıknatıs (2026-09-25)
            U("armor", "Zırh", StatType.Armor, 1f, 5, 150, 1.40f);
            U("luck", "Şans", StatType.Luck, 0.05f, 5, 200, 1.40f);
            U("experience", "Tecrübe", StatType.Experience, 0.04f, 5, 250, 1.45f);
            U("reroll", "Yeniden Çek", StatType.Rerolls, 1f, 3, 300, 1.8f);
            U("banish", "Yasakla", StatType.Banishes, 1f, 3, 400, 1.8f);
            U("revive", "Diriliş", StatType.Revives, 1f, 2, 1500, 3.0f);
        }
    }
}
