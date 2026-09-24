using PofudukFilo.Bullets;
using PofudukFilo.Core;
using PofudukFilo.Enemies;
using PofudukFilo.Feel;
using PofudukFilo.Player;
using PofudukFilo.Progression;
using PofudukFilo.UI;
using PofudukFilo.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static PofudukFilo.EditorTools.SetupUtil;

namespace PofudukFilo.EditorTools
{
    /// <summary>
    /// One click from an empty Unity 6 (2D URP) project to a playable build:
    /// Pofuduk Filo ▸ Oynanabilir Sahneyi Kur. Safe to re-run — assets are overwritten in place.
    /// </summary>
    public static class PofudukFiloSetup
    {
        public const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        public const string UiFontPath = "Assets/_Project/Fonts/Fredoka.ttf";

        [MenuItem("Pofuduk Filo/Oynanabilir Sahneyi Kur", priority = 0)]
        public static void BuildEverything()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildEverythingNonInteractive();
        }

        /// <summary>Same as the menu item without prompts — used by CI (CiBuild.BuildAndroid).</summary>
        public static void BuildEverythingNonInteractive()
        {
            try
            {
                // The scene must exist BEFORE the content: NewScene unloads every asset no scene
                // references, which orphans the freshly created ScriptableObjects and prefabs the
                // builder still holds, and they would then be wired into the scene as null.
                UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                EditorUtility.DisplayProgressBar("Pofuduk Filo", "İçerik üretiliyor…", 0.2f);
                var content = new ContentBuilder();
                content.BuildAll();

                EditorUtility.DisplayProgressBar("Pofuduk Filo", "Sahne kuruluyor…", 0.8f);
                BuildScene(content, scene);
                ConfigureProject();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log("[Pofuduk Filo] Kurulum tamam. Play'e basın (Game görünümünü 9:19.5 portreye alın).");
        }

        /// <summary>Launcher icon, studio name, and no engine splash — our own studio intro plays instead (brand.md).</summary>
        private static void ConfigureBrand(Texture2D icon)
        {
            PlayerSettings.companyName = "Ehtiyars Game";
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            if (icon == null) return;
#if UNITY_6000_0_OR_NEWER
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
#else
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
#endif
        }

        private static void ConfigureProject()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.productName = "Fluffy Fleet"; // global name; Turkish UI says "Pofuduk Filo"

            var scenes = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene s in scenes)
                if (s.path == ScenePath) return;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
            {
                new(ScenePath, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static void BuildScene(ContentBuilder c, UnityEngine.SceneManagement.Scene scene)
        {

            // Camera: 10 world units wide on a 9:19.5 portrait screen.
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 10.8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ArtRecipes.Hex(0x3E335C);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transparencySortMode = TransparencySortMode.Orthographic;
            camGo.AddComponent<AudioListener>(); // without it the synthesized audio is silent

            var bg = new GameObject("Background").AddComponent<BackgroundScroller>();
            bg.transform.position = new Vector3(0f, 0f, 5f);
            Set(bg, "material", c.SpriteMaterial);
            Set(bg, "sky", c.Sprites["bg_sky"]);
            Set(bg, "nebula", c.Sprites["bg_nebula"]);
            Set(bg, "farStars", c.Sprites["bg_stars_far"]);
            Set(bg, "nearStars", c.Sprites["bg_stars_near"]);
            SetArray(bg, "props", new Object[] { c.Sprites["bg_planet_ring"], c.Sprites["bg_planet_donut"], c.Sprites["bg_planet_moon"] });

            // ---- Systems
            var systems = new GameObject("Systems");

            var bullets = systems.AddComponent<BulletSystem>();
            SetArray(bullets, "bulletTypes", c.BulletTypes);

            var enemies = systems.AddComponent<EnemyManager>();
            SetArray(enemies, "prewarmPrefabs", new Object[]
            {
                c.Enemies["Chick"], c.Enemies["CookieRobot"], c.Enemies["JellyBear"], c.Enemies["GumBalloon"]
            });
            Set(enemies, "despawnBelowY", -12.5f);

            var waves = systems.AddComponent<WaveDirector>();
            Set(waves, "run", c.Runs[0]);
            Set(waves, "startOnStart", false); // RunController starts runs from the menu

            var vfx = systems.AddComponent<VfxSystem>();
            Set(vfx, "circleSprite", c.Sprites["circle"]);
            Set(vfx, "lineMaterial", c.LineMaterial);
            Set(vfx, "spriteMaterial", c.SpriteMaterial);
            Set(vfx, "confetti", BuildConfetti(systems.transform, c.ConfettiMaterial));
            Set(vfx, "sparkSprite", c.Sprites["star"]);
            systems.AddComponent<CombatFx>();

            var juice = systems.AddComponent<Juice>();
            Set(juice, "cameraRig", cam.transform);

            // ---- Player
            var playerGo = new GameObject("Player");
            playerGo.transform.position = new Vector3(0f, -7f, 0f);
            // The ship art hangs off a child so the heart emblem sits exactly on the hitbox (root pivot).
            var visual = new GameObject("Visual");
            visual.transform.SetParent(playerGo.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.24f, 0f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = c.Sprites["bunny"];
            sr.sharedMaterial = c.SpriteMaterial;
            sr.sortingOrder = 10;
            playerGo.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
            var shipVisual = playerGo.AddComponent<ShipVisual>();
            Set(shipVisual, "visual", visual.transform);
            Set(shipVisual, "shipRenderer", sr);
            Set(shipVisual, "flameSprite", c.Sprites["circle"]);
            Set(shipVisual, "material", c.SpriteMaterial);

            var heart = new GameObject("Hitbox Heart");
            heart.transform.SetParent(playerGo.transform, false);
            heart.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            var heartSr = heart.AddComponent<SpriteRenderer>();
            heartSr.sprite = c.Sprites["p_heart"];
            heartSr.sharedMaterial = c.SpriteMaterial;
            heartSr.sortingOrder = 11;

            playerGo.AddComponent<PlayerController>();
            var health = playerGo.AddComponent<PlayerHealth>();
            var inventory = playerGo.AddComponent<WeaponInventory>();
            Set(inventory, "startingWeapon", c.StartingWeapon);
            var xp = playerGo.AddComponent<XpSystem>();
            var draft = playerGo.AddComponent<UpgradeDraft>();
            Set(draft, "inventory", inventory);
            SetArray(draft, "weaponPool", c.BaseWeapons.ToArray());
            SetArray(draft, "passivePool", c.Passives.ToArray());

            // ---- Pickups (needs player-side refs)
            var pickups = systems.AddComponent<PickupSystem>();
            Set(pickups, "xpSystem", xp);
            Set(pickups, "inventory", inventory);
            Set(pickups, "waveDirector", waves);
            SetPickupVisuals(pickups, c.PickupVisuals);

            // ---- Flow & UI
            var flow = new GameObject("Game");
            var run = flow.AddComponent<RunController>();
            Set(run, "waveDirector", waves);
            Set(run, "xpSystem", xp);
            Set(run, "draft", draft);
            Set(run, "inventory", inventory);
            Set(run, "pickups", pickups);
            Set(run, "player", health);
            Set(run, "juice", juice);
            Set(run, "playerStartPosition", new Vector2(0f, -7f));
            SetArray(run, "chapters", c.Runs.ToArray());
            SetArray(run, "workshop", c.Workshop.ToArray());
            SetArray(run, "characters", c.Characters.ToArray());
            SetArray(run, "fusions", c.Fusions.ToArray());
            // Lab lists every base weapon: the starter too (mastery), paid ones also unlock there.
            var labWeapons = new System.Collections.Generic.List<Object> { c.StartingWeapon };
            labWeapons.AddRange(c.BaseWeapons);
            SetArray(run, "labWeapons", labWeapons.ToArray());
            SetArray(run, "labPassives", c.Passives.ToArray());
            Set(run, "constellation", c.Constellation);
            Set(run, "playerSprite", sr);

            var audio = systems.AddComponent<PofudukFilo.Audio.AudioManager>();
            Set(audio, "run", run);
            Set(audio, "inventory", inventory);
            Set(audio, "pickups", pickups);
            Set(audio, "waveDirector", waves);

            var ui = flow.AddComponent<GameUI>();
            Set(ui, "run", run);
            Set(ui, "xpSystem", xp);
            Set(ui, "inventory", inventory);
            Set(ui, "player", health);
            Set(ui, "waveDirector", waves);
            Set(ui, "pickups", pickups);
            Set(ui, "roundedSprite", c.Sprites["ui_rounded"]);
            Set(ui, "edgeGlowSprite", c.Sprites["ui_edge_glow"]);
            Set(ui, "buttonSprite", c.Sprites["ui_button"]);
            Set(ui, "barTrackSprite", c.Sprites["ui_bar_track"]);
            Set(ui, "barFillSprite", c.Sprites["ui_bar_fill"]);
            Set(ui, "heartIcon", c.Sprites["p_heart"]);
            Set(ui, "coinIcon", c.Sprites["p_coin"]);
            Set(ui, "xpIcon", c.Sprites["p_gem_blue"]);
            Set(ui, "sugarIcon", c.Sprites["p_gem_pink"]);
            Set(ui, "researchIcon", c.Sprites["icon_research"]);
            Set(ui, "stardustIcon", c.Sprites["icon_stardust"]);
            foreach ((string field, string key) in new[]
                     {
                         ("weaponsIcon", "icon_weapons"), ("gearIcon", "icon_gear"), ("playIcon", "icon_play"),
                         ("pauseIcon", "icon_pause"), ("trophyIcon", "icon_trophy"), ("pedestalSprite", "menu_pedestal"),
                         ("raysSprite", "menu_rays"), ("capsuleSprite", "ui_capsule"), ("navBarSprite", "ui_navbar"),
                         ("ribbonSprite", "menu_ribbon"), ("shineSprite", "ui_shine"), ("vignetteSprite", "ui_vignette")
                     })
                Set(ui, field, c.Sprites[key]);
            // Rounded display font, fetched by CI (tools/ci, OFL licence); built-in font otherwise.
            Font uiFont = AssetDatabase.LoadAssetAtPath<Font>(UiFontPath);
            if (uiFont != null)
            {
                Set(ui, "font", uiFont);
                Set(systems.GetComponent<CombatFx>(), "font", uiFont);
            }
            else Debug.LogWarning($"[Setup] {UiFontPath} not found; using the built-in font.");

            // Studio intro ("Ehtiyars Game") before the menu, once per launch (brand.md).
            var intro = flow.AddComponent<StudioIntro>();
            Set(intro, "emblem", c.Sprites["studio_emblem"]);
            if (uiFont != null) Set(intro, "font", uiFont);
            ConfigureBrand(c.Sprites["app_icon"].texture);

            // Signature loop: Şeker Hücumu combo/fever and the rescued-wingmen fleet.
            flow.AddComponent<SugarRush>();
            var fleet = flow.AddComponent<Fleet>();
            Set(fleet, "inventory", inventory);
            SetArray(fleet, "pilotSprites", new Object[]
            {
                c.Sprites["pilot_chick"], c.Sprites["pilot_cat"], c.Sprites["pilot_hamster"], c.Sprites["pilot_fox"]
            });
            Set(fleet, "bubbleSprite", c.Sprites["bubble"]);
            Set(fleet, "material", c.SpriteMaterial);
            Set(fleet, "bulletTypeIndex", ContentBuilder.BStar);

            EnsureFolder("Assets/_Project/Scenes");
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new System.InvalidOperationException($"Could not save {ScenePath}.");
        }

        private static void SetPickupVisuals(PickupSystem pickups, PickupVisual[] visuals)
        {
            var so = new SerializedObject(pickups);
            SerializedProperty list = so.FindProperty("visuals");
            list.arraySize = visuals.Length;
            for (int i = 0; i < visuals.Length; i++)
            {
                SerializedProperty v = list.GetArrayElementAtIndex(i);
                v.FindPropertyRelative("mesh").objectReferenceValue = visuals[i].mesh;
                v.FindPropertyRelative("material").objectReferenceValue = visuals[i].material;
                v.FindPropertyRelative("scale").floatValue = visuals[i].scale;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>One shared confetti system; VfxSystem.Confetti emits into it (hundreds of pops, one draw).</summary>
        private static ParticleSystem BuildConfetti(Transform parent, Material material)
        {
            var go = new GameObject("Confetti");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 0f, -0.8f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.gravityModifier = 0.8f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1500; // VFX budget high tier (architecture.md §7)

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;

            ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 40;
            return ps;
        }
    }

    /// <summary>
    /// `commands.smoke` in project.yaml: batch-mode check that the scene opens and every
    /// required reference is wired. Exit code 0 = OK. CiBuild runs the same check before
    /// building, so an APK with null references is never produced.
    /// </summary>
    public static class SmokeCheck
    {
        public static void Run()
        {
            int problems = CountUnassignedReferences();
            Debug.Log(problems == 0 ? "[Smoke] OK" : $"[Smoke] {problems} unassigned reference(s).");
            EditorApplication.Exit(problems == 0 ? 0 : 1);
        }

        /// <summary>
        /// Reopens the saved scene from disk (what the build will ship) and counts empty object
        /// references on the game's components, array elements included.
        /// </summary>
        public static int CountUnassignedReferences()
        {
            if (!System.IO.File.Exists(PofudukFiloSetup.ScenePath))
            {
                Debug.LogError($"[Smoke] {PofudukFiloSetup.ScenePath} missing — run Pofuduk Filo ▸ Oynanabilir Sahneyi Kur.");
                return 1;
            }

            int problems = 0;
            EditorSceneManager.OpenScene(PofudukFiloSetup.ScenePath);
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb == null)
                {
                    Debug.LogError("[Smoke] A component's script is missing.");
                    problems++;
                    continue;
                }
                string ns = mb.GetType().Namespace;
                if (ns == null || !ns.StartsWith("PofudukFilo")) continue;
                var so = new SerializedObject(mb);
                SerializedProperty p = so.GetIterator();
                while (p.NextVisible(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference || p.objectReferenceValue != null) continue;
                    if (p.name == "m_Script") continue;
                    // Optional art hooks may stay empty.
                    if (p.name is "font" or "icon" or "weaponMount") continue;
                    Debug.LogError($"[Smoke] {mb.GetType().Name}.{p.propertyPath} is not assigned.");
                    problems++;
                }
            }
            return problems;
        }
    }
}
