using System;
using System.Collections.Generic;
using PofudukFilo.Core;
using PofudukFilo.Meta;
using PofudukFilo.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>
    /// Meta hub screens: Hangar (characters), Weapon Lab (card pool), Constellation board and
    /// Settings (meta-economy.md §3.3 B–D, art-bible §4 accessibility toggles).
    /// </summary>
    public sealed partial class GameUI
    {
        private sealed class MetaScreen
        {
            public GameObject Root;
            public Transform List;
            public WalletView Wallet;
            public Action Refresh;
        }

        private readonly List<MetaScreen> _metaScreens = new();
        private MetaScreen _hangar;
        private MetaScreen _lab;
        private MetaScreen _constellation;
        private MetaScreen _research;
        private GameObject _settings;
        private Text _pilotText;

        // Constellation state
        private readonly List<Transform> _branchColumns = new(3);
        private Text _nodeDetail;
        private Button _buyNodeButton;
        private Button _respecButton;
        private ConstellationNode _selectedNode;

        private void BuildMetaScreens(Transform root)
        {
            _hangar = ListScreen(root, "Pilotlar", RefreshHangar);
            _lab = ListScreen(root, "Silahlar", RefreshLab);
            _research = ListScreen(root, "Ar-Ge", RefreshResearch);
            _constellation = BuildConstellationScreen(root);
        }

        private void HideMetaScreens()
        {
            foreach (MetaScreen m in _metaScreens) m.Root.SetActive(false);
            if (_settings != null) _settings.SetActive(false);
        }

        private void RefreshOpenMetaScreen()
        {
            foreach (MetaScreen m in _metaScreens)
            {
                if (!m.Root.activeSelf) continue;
                SetWallet(m.Wallet);
                m.Refresh();
            }
        }


        private void OpenHangar() => OpenMeta(_hangar);
        private void OpenLab() => OpenMeta(_lab);
        private void OpenConstellation() => OpenMeta(_constellation);

        private void OpenMeta(MetaScreen screen)
        {
            HideMetaScreens(); // tabs switch screens directly, no stack of screens to back out of
            _menu.SetActive(false);
            screen.Root.SetActive(true);
            screen.Root.transform.SetAsLastSibling();
            if (_nav != null)
            {
                _nav.transform.SetAsLastSibling(); // the tab bar stays on top of every meta screen
                SelectTab(screen == _research ? 0 : screen == _lab ? 1 : screen == _hangar ? 3 : screen == _missions ? 4 : -1);
            }
            SetWallet(screen.Wallet);
            screen.Refresh();
        }

        private MetaScreen ListScreen(Transform root, string title, Action refresh)
        {
            var screen = new MetaScreen { Refresh = refresh };
            screen.Root = _ui.Node(title, root).gameObject;
            // Night-sky panel with the menu's vignettes and a logo-style title (same family as the main menu).
            Image bg = _ui.Panel(screen.Root.transform, Palette.Hex(0x2A1D45), "Background");
            bg.type = Image.Type.Simple;
            Vignette(screen.Root.transform, 0.82f, 1f, false);
            Vignette(screen.Root.transform, 0f, 0.2f, true);
            LogoLine(screen.Root.transform, title, 88, Palette.Hex(0xFFF6C8), Palette.Honey, 0.9f, 0.97f).GetComponent<UIWave>().amplitude = 2f;
            Image walletCap = Capsule(screen.Root.transform, 0.2f, 0.855f, 0.8f, 0.895f);
            screen.Wallet = MakeWallet(walletCap.transform, 0.05f, 0.1f, 0.95f, 0.9f, 40);
            // "[TV] +" beside the wallet: an ad for gold and Stardust right where the player is short of it.
            Button more = AdButton(screen.Root.transform, "", "+", Palette.Mint, ClaimGift, 34);
            UIFactory.Place(more, 0.82f, 0.855f, 0.97f, 0.895f);
            _walletAdButtons.Add(more);

            // Down to the tab bar, which replaces the old Back button (main-menu.md §7).
            screen.List = ScrollList(screen.Root.transform, 0.04f, 0.125f, 0.96f, 0.85f);
            screen.Root.SetActive(false);
            _metaScreens.Add(screen);
            return screen;
        }

        /// <summary>A vertically scrolling list (drag to scroll); returns the content node rows go into.</summary>
        private Transform ScrollList(Transform parent, float x0, float y0, float x1, float y1)
        {
            RectTransform viewport = _ui.Node("Viewport", parent);
            UIFactory.Place(viewport, x0, y0, x1, y1);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f); // catches drags between rows

            RectTransform content = _ui.Node("List", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.padding = new RectOffset(0, 0, 4, 24);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
            return content;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                child.SetActive(false); // leave layouts now; Destroy is deferred
                Destroy(child);
            }
        }

        private Image Row(Transform list, string name, float height)
        {
            Image row = _ui.Panel(list, Palette.Hex(0x3E2C63), name);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        // ---------------------------------------------------------------- Hangar

        private void RefreshPilot()
        {
            if (_heroShip == null) return;
            string id = run.Meta.SelectedCharacterId;
            CharacterDefinition pilot = run.Characters.Count > 0 ? run.Characters[0] : null;
            foreach (CharacterDefinition c in run.Characters)
                if (c.id == id && run.Meta.IsUnlocked(c)) pilot = c;
            if (pilot != null) RefreshLobbyPilot(pilot);
        }

        private void RefreshHangar()
        {
            ClearChildren(_hangar.List);
            MetaProgressionService meta = run.Meta;

            foreach (CharacterDefinition c in run.Characters)
            {
                Image row = Row(_hangar.List, c.id, 250f);

                if (c.sprite != null)
                {
                    RectTransform iconNode = _ui.Node("Icon", row.transform);
                    UIFactory.Place(iconNode, 0.01f, 0.06f, 0.2f, 0.94f);
                    var icon = iconNode.gameObject.AddComponent<Image>();
                    icon.sprite = c.sprite;
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    if (!meta.IsUnlocked(c)) icon.color = new Color(0.2f, 0.15f, 0.3f, 1f); // silhouette
                }

                int pilotLevel = meta.GetPilotLevel(c.id);
                // Name, perk and level as separate, individually translated lines (no composite rich text).
                Text nameText = _ui.Label(row.transform, c.displayName, 44, Palette.Cream, TextAnchor.MiddleLeft);
                UIFactory.Place(nameText, 0.22f, 0.68f, 0.64f, 0.96f);
                Text perk = _ui.Label(row.transform, c.perkText, 28, Palette.White, TextAnchor.UpperLeft);
                perk.horizontalOverflow = HorizontalWrapMode.Wrap;
                perk.resizeTextForBestFit = true;
                perk.resizeTextMinSize = 20;
                perk.resizeTextMaxSize = 28;
                UIFactory.Place(perk, 0.22f, 0.44f, 0.64f, 0.68f);
                // The hero's own guns (hero-guns.md): main gun + signature weapon.
                WeaponDefinition gun = c.mainGun, second = c.startingWeapon;
                string arms = gun == null ? "" : second == null || second == gun ? Loc.T(gun.displayName)
                    : $"{Loc.T(gun.displayName)} + {Loc.T(second.displayName)}";
                Text armsText = _ui.Label(row.transform, arms, 26, Palette.Honey, TextAnchor.MiddleLeft);
                armsText.horizontalOverflow = HorizontalWrapMode.Wrap;
                armsText.resizeTextForBestFit = true;
                armsText.resizeTextMinSize = 18;
                armsText.resizeTextMaxSize = 26;
                UIFactory.Place(armsText, 0.22f, 0.3f, 0.64f, 0.44f);
                if (meta.IsUnlocked(c))
                {
                    int bonus = Mathf.RoundToInt(Formulas.PilotBonusPerLevel * (pilotLevel - 1) * 100f);
                    Text lvl = _ui.Label(row.transform, $"Pilot Sv. {pilotLevel}/{Formulas.MaxPilotLevel}  (+%{bonus} hasar ve can)", 26, Palette.Mint, TextAnchor.MiddleLeft);
                    UIFactory.Place(lvl, 0.22f, 0.04f, 0.64f, 0.3f);
                }

                CharacterDefinition captured = c;
                Button action;
                if (meta.IsUnlocked(c))
                {
                    bool selected = meta.SelectedCharacterId == c.id;
                    action = _ui.Button(row.transform, selected ? "Seçili" : "Seç", selected ? Palette.Mint : Palette.HotPink, () =>
                    {
                        if (meta.SelectCharacter(captured)) RefreshHangar();
                    }, 42);
                    action.interactable = !selected;

                    // Level-up sits under the select button.
                    bool maxed = pilotLevel >= Formulas.MaxPilotLevel;
                    Button levelUp = PriceButton(row.transform, maxed ? "MAKS" : "Sv. Atla", maxed ? 0 : Formulas.PilotLevelCost(pilotLevel), 0,
                        Palette.Honey, () =>
                        {
                            if (meta.TryLevelPilot(captured)) RefreshOpenMetaScreen();
                        }, 30);
                    levelUp.interactable = !maxed && meta.CanLevelPilot(c);
                    UIFactory.Place(levelUp, 0.66f, 0.06f, 0.98f, 0.46f);
                    UIFactory.Place(action, 0.66f, 0.54f, 0.98f, 0.94f);
                    continue;
                }
                else if (c.requiresChapterCleared >= 0)
                {
                    // Reach the stage for free, or buy early access now.
                    int early = MetaProgressionService.EarlyAccessCost(c) + c.goldCost;
                    action = PriceButton(row.transform, "Hemen aç", early, c.stardustCost, Palette.Honey, () =>
                    {
                        if (meta.TryUnlockEarly(captured)) RefreshOpenMetaScreen();
                    }, 34);
                    action.interactable = meta.CanUnlockEarly(c);
                    Text gate = _ui.Label(row.transform, $"veya Bölüm {c.requiresChapterCleared + 1} geçilince", 26, Palette.Lavender);
                    UIFactory.Place(gate, 0.64f, 0.0f, 1f, 0.14f);
                }
                else
                {
                    action = PriceButton(row.transform, "Aç", c.goldCost, c.stardustCost, Palette.Honey, () =>
                    {
                        if (meta.TryUnlock(captured)) RefreshOpenMetaScreen();
                    }, 36);
                    action.interactable = meta.CanUnlock(c);
                    // Try it for one run after an ad (ad-rewards.md §3.2); buying it is gold and Stardust only.
                    AddTrialButton(row.transform, () => TryPilot(captured));
                }
                UIFactory.Place(action, 0.66f, 0.14f, 0.98f, 0.86f);
            }
        }

        // ---------------------------------------------------------------- Weapon Lab

        private void RefreshLab()
        {
            ClearChildren(_lab.List);
            MetaProgressionService meta = run.Meta;

            HeroGunRows(meta);
            Section(_lab.List, "LABORATUVAR · kart havuzuna silah ve pasif ekle");

            var listed = new HashSet<WeaponDefinition>();
            foreach (WeaponDefinition w in run.LabWeapons)
            {
                if (w == null || !listed.Add(w)) continue; // the starter can appear twice in the lab list
                WeaponDefinition captured = w;
                bool unlocked = meta.IsUnlocked(w);
                int mastery = meta.GetMastery(w.id);
                bool maxed = mastery >= Formulas.MaxWeaponMastery;

                Image row = Row(_lab.List, w.id, 250f);
                AddIcon(row.transform, w.icon, unlocked);
                Text name = _ui.Label(row.transform, w.displayName, 42, Palette.Cream, TextAnchor.MiddleLeft);
                UIFactory.Place(name, 0.22f, 0.7f, 0.65f, 0.96f);
                Text desc = _ui.Label(row.transform, w.description ?? "", 28, Palette.White, TextAnchor.UpperLeft);
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                desc.resizeTextForBestFit = true;
                desc.resizeTextMinSize = 20;
                desc.resizeTextMaxSize = 28;
                UIFactory.Place(desc, 0.22f, 0.3f, 0.65f, 0.7f);
                string state = unlocked
                    ? Loc.T($"Ustalık {mastery}/{Formulas.MaxWeaponMastery}: +%{Mathf.RoundToInt(Formulas.MasteryDamagePerLevel * mastery * 100f)} hasar")
                    : Loc.T("Kilitli: açınca oyunda kartı çıkmaya başlar");
                if (unlocked)
                {
                    Text stateText = _ui.Label(row.transform, state, 28, unlocked ? Palette.Mint : Palette.Honey, TextAnchor.MiddleLeft);
                    UIFactory.Place(stateText, 0.22f, 0.04f, 0.65f, 0.3f);
                }

                Button b;
                if (!unlocked)
                {
                    b = PriceButton(row.transform, "Aç", w.labCost, 0, Palette.Honey, () =>
                    {
                        if (meta.TryUnlock(captured)) RefreshOpenMetaScreen();
                    }, 38);
                    b.interactable = meta.Gold >= w.labCost;
                    AddTrialButton(row.transform, () => TryWeapon(captured));
                }
                else if (maxed)
                {
                    b = _ui.Button(row.transform, "MAKS", Palette.Mint, null, 44);
                    b.interactable = false;
                }
                else
                {
                    b = PriceButton(row.transform, "Geliştir", Formulas.MasteryCost(mastery), 0, Palette.HotPink, () =>
                    {
                        if (meta.TryUpgradeMastery(captured)) RefreshOpenMetaScreen();
                    }, 36);
                    b.interactable = meta.CanUpgradeMastery(w);
                }
                UIFactory.Place(b, 0.66f, 0.14f, 0.98f, 0.86f);
            }

            foreach (PassiveDefinition p in run.LabPassives)
            {
                if (p.labCost <= 0) continue;
                PassiveDefinition captured = p;
                bool unlocked = meta.IsUnlocked(p);
                Image row = Row(_lab.List, p.id, 210f);
                AddIcon(row.transform, p.icon, unlocked);
                UIFactory.Place(_ui.Label(row.transform, $"{p.displayName}  (pasif)", 40, Palette.Cream, TextAnchor.MiddleLeft), 0.22f, 0.62f, 0.65f, 0.95f);
                Text pdesc = _ui.Label(row.transform, p.description ?? "", 28, Palette.White, TextAnchor.UpperLeft);
                pdesc.horizontalOverflow = HorizontalWrapMode.Wrap;
                UIFactory.Place(pdesc, 0.22f, 0.08f, 0.65f, 0.6f);
                Button b = unlocked
                    ? _ui.Button(row.transform, "Havuzda", Palette.Mint, null, 44)
                    : PriceButton(row.transform, "Aç", p.labCost, 0, Palette.Honey, () =>
                    {
                        if (meta.TryUnlock(captured)) RefreshOpenMetaScreen();
                    }, 38);
                b.interactable = !unlocked && meta.Gold >= p.labCost;
                UIFactory.Place(b, 0.66f, 0.14f, 0.98f, 0.86f);
            }
        }

        /// <summary>
        /// Hero gun mods (hero-guns.md §4): every unlocked pilot's own gun with its three permanent tracks, at the top of
        /// the Weapons screen ("her silahın kendine özgü geliştirmesi olsun").
        /// </summary>
        private void HeroGunRows(MetaProgressionService meta)
        {
            Section(_lab.List, "KAHRAMAN SİLAHLARI · her silahın kendi gelişimi");
            var shown = new HashSet<string>();
            foreach (CharacterDefinition c in run.Characters)
            {
                if (c == null || !meta.IsUnlocked(c)) continue;
                WeaponDefinition gun = run.MainGunOf(c);
                if (gun == null || !GunMods.HasMods(gun.id) || !shown.Add(gun.id)) continue;

                Image head = Row(_lab.List, gun.id, 150f);
                head.color = Palette.Hex(0x4A3478);
                AddIcon(head.transform, gun.icon != null ? gun.icon : c.sprite, true);
                Text title = _ui.Label(head.transform, Loc.T($"{Loc.T(gun.displayName)} · {Loc.T(c.displayName)}"), 42, Palette.Honey, TextAnchor.MiddleLeft);
                UIFactory.Place(title, 0.22f, 0.52f, 0.98f, 0.96f);
                Text desc = _ui.Label(head.transform, gun.description ?? "", 26, Palette.White, TextAnchor.UpperLeft);
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                desc.resizeTextForBestFit = true;
                desc.resizeTextMinSize = 18;
                desc.resizeTextMaxSize = 26;
                UIFactory.Place(desc, 0.22f, 0.06f, 0.98f, 0.52f);

                foreach (GunMod m in GunMods.For(gun.id))
                {
                    string gunId = gun.id, key = m.Key;
                    int level = meta.GetGunMod(gunId, key);
                    bool maxed = level >= GunMods.MaxLevel;
                    Image row = Row(_lab.List, key, 140f);
                    Text name = _ui.Label(row.transform, Loc.T($"{Loc.T(m.Name)}  Sv.{level}/{GunMods.MaxLevel}"), 36, Palette.Cream, TextAnchor.MiddleLeft);
                    UIFactory.Place(name, 0.05f, 0.5f, 0.64f, 0.95f);
                    Text what = _ui.Label(row.transform, m.Description, 28, Palette.Mint, TextAnchor.MiddleLeft);
                    UIFactory.Place(what, 0.05f, 0.06f, 0.64f, 0.5f);
                    Button b;
                    if (maxed)
                    {
                        b = _ui.Button(row.transform, "MAKS", Palette.Mint, null, 40);
                        b.interactable = false;
                    }
                    else
                    {
                        b = PriceButton(row.transform, "Geliştir", GunMods.Cost(level), 0, Palette.HotPink, () =>
                        {
                            if (run.Meta.TryUpgradeGunMod(gunId, key)) RefreshOpenMetaScreen();
                        }, 34);
                        b.interactable = meta.CanUpgradeGunMod(gunId, key);
                    }
                    UIFactory.Place(b, 0.66f, 0.12f, 0.98f, 0.88f);
                }
            }
        }

        private void AddIcon(Transform row, Sprite sprite, bool unlocked)
        {
            if (sprite == null) return;
            RectTransform node = _ui.Node("Icon", row);
            UIFactory.Place(node, 0.02f, 0.1f, 0.2f, 0.9f);
            var img = node.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            if (!unlocked) img.color = new Color(0.35f, 0.3f, 0.45f, 1f);
        }

        // ---------------------------------------------------------------- Constellation

        private MetaScreen BuildConstellationScreen(Transform root)
        {
            var screen = new MetaScreen { Refresh = RefreshConstellation };
            screen.Root = _ui.Node("Constellation", root).gameObject;
            _ui.Panel(screen.Root.transform, Palette.Hex(0x2B2140), "Background"); // night sky

            UIFactory.Place(_ui.Label(screen.Root.transform, "Takımyıldız", 96, Palette.Cream), 0.05f, 0.9f, 0.95f, 0.97f);
            screen.Wallet = MakeWallet(screen.Root.transform, 0.05f, 0.86f, 0.95f, 0.9f, 46);

            _nodeDetail = _ui.Label(screen.Root.transform, "Bir yıldıza dokun.", 40, Palette.White);
            UIFactory.Place(_nodeDetail, 0.06f, 0.76f, 0.94f, 0.85f);

            for (int b = 0; b < 3; b++)
            {
                RectTransform column = _ui.Node($"Branch{b}", screen.Root.transform);
                UIFactory.Place(column, 0.03f + b * 0.32f, 0.215f, 0.33f + b * 0.32f, 0.74f);
                var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 10f;
                layout.padding = new RectOffset(6, 6, 0, 0);
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = true;
                _branchColumns.Add(column);
            }

            // Above the tab bar (no Back button: the tabs take you anywhere).
            _buyNodeButton = _ui.Button(screen.Root.transform, "Aç", Palette.Honey, BuySelectedNode, 52);
            UIFactory.Place(_buyNodeButton, 0.05f, 0.13f, 0.55f, 0.2f);
            _respecButton = _ui.Button(screen.Root.transform, "Sıfırla", Palette.Lavender, Respec, 44);
            UIFactory.Place(_respecButton, 0.6f, 0.13f, 0.95f, 0.2f);

            screen.Root.SetActive(false);
            _metaScreens.Add(screen);
            return screen;
        }

        private void RefreshConstellation()
        {
            ConstellationDefinition board = run.Constellation;
            foreach (Transform column in _branchColumns) ClearChildren(column);
            if (board == null) return;

            MetaProgressionService meta = run.Meta;
            foreach (ConstellationNode node in board.nodes)
            {
                Transform column = _branchColumns[Mathf.Clamp(node.branch, 0, _branchColumns.Count - 1)];
                bool owned = meta.HasNode(node);
                bool available = !owned && ConstellationRules.PrerequisitesMet(node.requires, OwnedIds(board, meta));
                Color color = owned ? Palette.Mint : available ? Palette.Honey : Palette.Outline;

                ConstellationNode captured = node;
                Button star = PriceButton(column, node.displayName, 0, node.stardustCost, color, () => SelectNode(captured), 28);
                if (node == _selectedNode) Feel.Juice.PopIn(star.transform);
            }

            _buyNodeButton.interactable = _selectedNode != null && meta.CanBuy(_selectedNode);
            _respecButton.interactable = meta.CanRespec(DateTime.UtcNow.Ticks);
        }

        private static HashSet<string> OwnedIds(ConstellationDefinition board, MetaProgressionService meta)
        {
            var owned = new HashSet<string>();
            foreach (ConstellationNode n in board.nodes)
                if (meta.HasNode(n)) owned.Add(n.id);
            return owned;
        }

        private void SelectNode(ConstellationNode node)
        {
            _selectedNode = node;
            string state = run.Meta.HasNode(node) ? "  (açık)" : "";
            _nodeDetail.text = Loc.T($"{node.displayName}{state}\n{node.description}");
            _buyNodeButton.interactable = run.Meta.CanBuy(node);
        }

        private void BuySelectedNode()
        {
            if (_selectedNode != null && run.Meta.TryBuy(_selectedNode)) SelectNode(_selectedNode);
            RefreshOpenMetaScreen();
        }

        private void Respec()
        {
            if (run.Meta.Respec(run.Constellation, DateTime.UtcNow.Ticks)) _nodeDetail.text = Loc.T("Tüm yıldız tozu iade edildi.");
            RefreshOpenMetaScreen();
        }

        // ---------------------------------------------------------------- Settings

        private void BuildSettings(Transform root)
        {
            _settings = _ui.Node("Settings", root).gameObject;
            _ui.Dimmer(_settings.transform);
            Image card = _ui.Panel(_settings.transform, Palette.Hex(0x3E2C63), "Card");
            UIFactory.Place(card, 0.08f, 0.2f, 0.92f, 0.82f);

            UIFactory.Place(_ui.Label(card.transform, "Ayarlar", 80, Palette.Cream), 0.05f, 0.88f, 0.95f, 0.98f);
            Toggle(card.transform, 0.745f, "Ses efektleri", () => GameSettings.Sfx, v => GameSettings.Sfx = v);
            Toggle(card.transform, 0.625f, "Müzik", () => GameSettings.Music, v => GameSettings.Music = v);
            Toggle(card.transform, 0.505f, "Titreşim", () => GameSettings.Vibration, v => GameSettings.Vibration = v);
            Toggle(card.transform, 0.385f, "Ekran sarsıntısı", () => GameSettings.ScreenShake, v => GameSettings.ScreenShake = v);
            Toggle(card.transform, 0.265f, "Hasar sayıları", () => GameSettings.DamageNumbers, v => GameSettings.DamageNumbers = v);

            // Language: two explicit choices, the active one lit (device feedback: a single toggle flipped
            // the language on the first tap without saying what it would do). Screens are built once from
            // code, so a change reloads the scene.
            UIFactory.Place(_ui.Label(card.transform, "Dil / Language", 44, Palette.White, TextAnchor.MiddleLeft), 0.06f, 0.145f, 0.5f, 0.255f);
            LanguageButton(card.transform, "Türkçe", Language.Turkish, 0.5f, 0.71f);
            LanguageButton(card.transform, "English", Language.English, 0.73f, 0.94f);

            UIFactory.Place(_ui.Button(card.transform, "Kapat", Palette.HotPink, () => _settings.SetActive(false), 52), 0.3f, 0.02f, 0.7f, 0.13f);

            // Version and studio under the card, for bug reports and store support.
            Text about = _ui.Label(_settings.transform, $"{Loc.GameTitle} v{Application.version} · Ehtiyars Game", 30,
                new Color(1f, 1f, 1f, 0.55f));
            UIFactory.Place(about, 0.08f, 0.155f, 0.92f, 0.19f);
            about.raycastTarget = false;
            _settings.SetActive(false);
        }

        private void LanguageButton(Transform parent, string label, Language language, float x0, float x1)
        {
            bool active = Loc.Current == language;
            Button b = _ui.Button(parent, "", active ? Palette.Mint : Palette.Outline, () =>
            {
                if (Loc.Current == language) return;
                Loc.Current = language;
                TimeScaleController.SetPaused(false);
                TimeScaleController.SetFingerLifted(false);
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }, 36);
            // Language names are shown as themselves, never translated.
            Text t = b.GetComponentInChildren<Text>();
            t.text = label;
            t.color = active ? Palette.White : new Color(1f, 1f, 1f, 0.6f);
            UIFactory.Place(b, x0, 0.15f, x1, 0.25f);
        }

        private void Toggle(Transform parent, float y, string label, Func<bool> get, Action<bool> set)
        {
            UIFactory.Place(_ui.Label(parent, label, 48, Palette.White, TextAnchor.MiddleLeft), 0.06f, y, 0.6f, y + 0.13f);
            Button button = null;
            button = _ui.Button(parent, get() ? "Açık" : "Kapalı", get() ? Palette.Mint : Palette.Outline, () =>
            {
                set(!get());
                UIFactory.SetText(button, get() ? "Açık" : "Kapalı");
                button.transform.Find("Face").GetComponent<Image>().color = get() ? Palette.Mint : Palette.Outline;
            }, 44);
            UIFactory.Place(button, 0.64f, y + 0.01f, 0.94f, y + 0.12f);
        }

        private void OpenSettings()
        {
            _settings.SetActive(true);
            _settings.transform.SetAsLastSibling();
        }
    }
}
