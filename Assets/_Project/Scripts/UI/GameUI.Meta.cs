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
            public Text Wallet;
            public Action Refresh;
        }

        private readonly List<MetaScreen> _metaScreens = new();
        private MetaScreen _hangar;
        private MetaScreen _lab;
        private MetaScreen _constellation;
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
            _hangar = ListScreen(root, "Hangar", RefreshHangar);
            _lab = ListScreen(root, "Silah Laboratuvarı", RefreshLab);
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
                m.Wallet.text = Loc.T(WalletLine());
                m.Refresh();
            }
        }

        private string WalletLine() => $"{run.Meta.Gold} altın   ·   {run.Meta.Stardust} Yıldız Tozu";

        private void OpenHangar() => OpenMeta(_hangar);
        private void OpenLab() => OpenMeta(_lab);
        private void OpenConstellation() => OpenMeta(_constellation);

        private void OpenMeta(MetaScreen screen)
        {
            _menu.SetActive(false);
            screen.Root.SetActive(true);
            screen.Root.transform.SetAsLastSibling();
            screen.Wallet.text = Loc.T(WalletLine());
            screen.Refresh();
        }

        private void CloseMeta(MetaScreen screen)
        {
            screen.Root.SetActive(false);
            _menu.SetActive(true);
            RefreshMenu();
        }

        private MetaScreen ListScreen(Transform root, string title, Action refresh)
        {
            var screen = new MetaScreen { Refresh = refresh };
            screen.Root = _ui.Node(title, root).gameObject;
            _ui.Panel(screen.Root.transform, Palette.Lavender, "Background");

            UIFactory.Place(_ui.Label(screen.Root.transform, title, 96, Palette.Cream), 0.05f, 0.9f, 0.95f, 0.97f);
            screen.Wallet = _ui.Label(screen.Root.transform, "", 46, Palette.Honey);
            UIFactory.Place(screen.Wallet, 0.05f, 0.86f, 0.95f, 0.9f);

            screen.List = _ui.Node("List", screen.Root.transform);
            UIFactory.Place(screen.List, 0.04f, 0.13f, 0.96f, 0.85f);
            var layout = screen.List.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            UIFactory.Place(_ui.Button(screen.Root.transform, "Geri", Palette.HotPink, () => CloseMeta(screen), 64), 0.25f, 0.03f, 0.75f, 0.1f);
            screen.Root.SetActive(false);
            _metaScreens.Add(screen);
            return screen;
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
            Image row = _ui.Panel(list, Palette.Outline, name);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        // ---------------------------------------------------------------- Hangar

        private void RefreshPilot()
        {
            if (_pilotText == null) return;
            string id = run.Meta.SelectedCharacterId;
            CharacterDefinition pilot = run.Characters.Count > 0 ? run.Characters[0] : null;
            foreach (CharacterDefinition c in run.Characters)
                if (c.id == id && run.Meta.IsUnlocked(c)) pilot = c;
            _pilotText.text = Loc.T(pilot != null ? $"Pilot: {pilot.displayName}" : "");
            if (_heroShip != null && pilot != null)
            {
                _heroShip.sprite = pilot.shipSprite != null ? pilot.shipSprite : pilot.sprite;
                Feel.Juice.PopIn(_heroShip.transform);
            }
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
                string levelLine = meta.IsUnlocked(c) ? $"\n<size=32>Pilot Sv. {pilotLevel}/{Formulas.MaxPilotLevel}  (+%{Mathf.RoundToInt(Formulas.PilotBonusPerLevel * (pilotLevel - 1) * 100f)} hasar ve can)</size>" : "";
                Text text = _ui.Label(row.transform, $"{c.displayName}\n<size=34>{c.perkText}</size>{levelLine}", 46, Palette.White, TextAnchor.MiddleLeft);
                text.supportRichText = true;
                UIFactory.Place(text, 0.22f, 0.04f, 0.64f, 0.96f);

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
                    Button levelUp = _ui.Button(row.transform, maxed ? "MAKS" : $"Sv. Atla\n{Formulas.PilotLevelCost(pilotLevel)} altın",
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
                    action = _ui.Button(row.transform, $"Bölüm {c.requiresChapterCleared + 1}'i bitir", Palette.Lavender, null, 34);
                    action.interactable = false;
                }
                else
                {
                    action = _ui.Button(row.transform, $"{c.goldCost} altın\n{c.stardustCost} toz", Palette.Honey, () =>
                    {
                        if (meta.TryUnlock(captured)) RefreshOpenMetaScreen();
                    }, 36);
                    action.interactable = meta.CanUnlock(c);
                }
                UIFactory.Place(action, 0.66f, 0.14f, 0.98f, 0.86f);
            }
        }

        // ---------------------------------------------------------------- Weapon Lab

        private void RefreshLab()
        {
            ClearChildren(_lab.List);
            MetaProgressionService meta = run.Meta;

            foreach (WeaponDefinition w in run.LabWeapons)
            {
                if (w == null) continue;
                WeaponDefinition captured = w;
                bool unlocked = meta.IsUnlocked(w);
                int mastery = meta.GetMastery(w.id);
                bool maxed = mastery >= Formulas.MaxWeaponMastery;

                Image row = Row(_lab.List, w.id, 210f);
                AddIcon(row.transform, w.icon, unlocked);
                string info = unlocked
                    ? $"{w.displayName}\n<size=34>Ustalık {mastery}/{Formulas.MaxWeaponMastery}</size>\n<size=34>+%{Mathf.RoundToInt(Formulas.MasteryDamagePerLevel * mastery * 100f)} hasar</size>"
                    : $"{w.displayName}\n<size=34>Kilitli — açınca kart havuzuna girer</size>";
                Text text = _ui.Label(row.transform, info, 46, Palette.White, TextAnchor.MiddleLeft);
                text.supportRichText = true;
                UIFactory.Place(text, 0.22f, 0.04f, 0.64f, 0.96f);

                Button b;
                if (!unlocked)
                {
                    b = _ui.Button(row.transform, $"Aç\n{w.labCost} altın", Palette.Honey, () =>
                    {
                        if (meta.TryUnlock(captured)) RefreshOpenMetaScreen();
                    }, 38);
                    b.interactable = meta.Gold >= w.labCost;
                }
                else if (maxed)
                {
                    b = _ui.Button(row.transform, "MAKS", Palette.Mint, null, 44);
                    b.interactable = false;
                }
                else
                {
                    b = _ui.Button(row.transform, $"Geliştir\n{Formulas.MasteryCost(mastery)} altın", Palette.HotPink, () =>
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
                Image row = Row(_lab.List, p.id, 170f);
                AddIcon(row.transform, p.icon, unlocked);
                UIFactory.Place(_ui.Label(row.transform, $"{p.displayName}  (pasif)", 44, Palette.White, TextAnchor.MiddleLeft), 0.22f, 0.1f, 0.64f, 0.9f);
                Button b = unlocked
                    ? _ui.Button(row.transform, "Havuzda", Palette.Mint, null, 44)
                    : _ui.Button(row.transform, $"Aç\n{p.labCost} altın", Palette.Honey, () =>
                    {
                        if (meta.TryUnlock(captured)) RefreshOpenMetaScreen();
                    }, 38);
                b.interactable = !unlocked && meta.Gold >= p.labCost;
                UIFactory.Place(b, 0.66f, 0.14f, 0.98f, 0.86f);
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
            screen.Wallet = _ui.Label(screen.Root.transform, "", 46, Palette.Honey);
            UIFactory.Place(screen.Wallet, 0.05f, 0.86f, 0.95f, 0.9f);

            _nodeDetail = _ui.Label(screen.Root.transform, "Bir yıldıza dokun.", 40, Palette.White);
            UIFactory.Place(_nodeDetail, 0.06f, 0.76f, 0.94f, 0.85f);

            for (int b = 0; b < 3; b++)
            {
                RectTransform column = _ui.Node($"Branch{b}", screen.Root.transform);
                UIFactory.Place(column, 0.03f + b * 0.32f, 0.12f, 0.33f + b * 0.32f, 0.74f);
                var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 10f;
                layout.padding = new RectOffset(6, 6, 0, 0);
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = true;
                _branchColumns.Add(column);
            }

            UIFactory.Place(_ui.Button(screen.Root.transform, "Geri", Palette.HotPink, () => CloseMeta(screen), 52), 0.03f, 0.03f, 0.32f, 0.1f);
            _buyNodeButton = _ui.Button(screen.Root.transform, "Aç", Palette.Honey, BuySelectedNode, 52);
            UIFactory.Place(_buyNodeButton, 0.35f, 0.03f, 0.65f, 0.1f);
            _respecButton = _ui.Button(screen.Root.transform, "Sıfırla", Palette.Lavender, Respec, 44);
            UIFactory.Place(_respecButton, 0.68f, 0.03f, 0.97f, 0.1f);

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
                Button star = _ui.Button(column, $"{node.displayName}\n{node.stardustCost} toz", color, () => SelectNode(captured), 28);
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
            Image card = _ui.Panel(_settings.transform, Palette.Lavender, "Card");
            UIFactory.Place(card, 0.08f, 0.2f, 0.92f, 0.82f);

            UIFactory.Place(_ui.Label(card.transform, "Ayarlar", 80, Palette.Cream), 0.05f, 0.88f, 0.95f, 0.98f);
            Toggle(card.transform, 0.745f, "Ses efektleri", () => GameSettings.Sfx, v => GameSettings.Sfx = v);
            Toggle(card.transform, 0.625f, "Müzik", () => GameSettings.Music, v => GameSettings.Music = v);
            Toggle(card.transform, 0.505f, "Titreşim", () => GameSettings.Vibration, v => GameSettings.Vibration = v);
            Toggle(card.transform, 0.385f, "Ekran sarsıntısı", () => GameSettings.ScreenShake, v => GameSettings.ScreenShake = v);
            Toggle(card.transform, 0.265f, "Hasar sayıları", () => GameSettings.DamageNumbers, v => GameSettings.DamageNumbers = v);

            // Language: every screen is built once from code, so switching reloads the scene.
            UIFactory.Place(_ui.Label(card.transform, "Dil", 48, Palette.White, TextAnchor.MiddleLeft), 0.06f, 0.145f, 0.6f, 0.255f);
            UIFactory.Place(_ui.Button(card.transform, Loc.Current == Language.Turkish ? "Türkçe" : "English", Palette.Sky, () =>
            {
                Loc.Current = Loc.Current == Language.Turkish ? Language.English : Language.Turkish;
                TimeScaleController.SetPaused(false);
                TimeScaleController.SetFingerLifted(false);
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }, 44), 0.62f, 0.15f, 0.94f, 0.25f);

            UIFactory.Place(_ui.Button(card.transform, "Kapat", Palette.HotPink, () => _settings.SetActive(false), 52), 0.3f, 0.02f, 0.7f, 0.13f);
            _settings.SetActive(false);
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
