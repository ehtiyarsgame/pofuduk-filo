using System.Collections.Generic;
using PofudukFilo.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>
    /// The build at a glance (design/ux/hud.md §1): a slim row of icons under the top panel — weapons, then boosts —
    /// each with its level, in gold as MAX once maxed or EVO once evolved. Device feedback 2026-09-25: "aldığımız oyun içi
    /// geliştirmeleri bir yerde gösterelim simge olarak".
    /// </summary>
    public sealed partial class GameUI
    {
        private sealed class LoadoutSlot
        {
            public GameObject Root;
            public Image Frame;
            public Image Icon;
            public Text Level;
        }

        private const int LoadoutSlots = WeaponInventory.MaxWeapons + WeaponInventory.MaxPassives;
        private readonly List<LoadoutSlot> _loadout = new(LoadoutSlots);
        private float _nextLoadoutRefresh;

        private void BuildLoadoutStrip(Transform hud)
        {
            const float size = 0.06f, gap = 0.008f, groupGap = 0.02f, y0 = 0.852f, y1 = 0.886f;
            float x = 0.02f;
            for (int i = 0; i < LoadoutSlots; i++)
            {
                if (i == WeaponInventory.MaxWeapons) x += groupGap; // weapons | boosts
                var slot = new LoadoutSlot();
                RectTransform node = _ui.Node("Loadout" + i, hud);
                UIFactory.Place(node, x, y0, x + size, y1);
                slot.Root = node.gameObject;
                slot.Frame = node.gameObject.AddComponent<Image>();
                slot.Frame.sprite = roundedSprite;
                slot.Frame.type = Image.Type.Sliced;
                slot.Frame.raycastTarget = false;
                slot.Icon = _ui.Node("Icon", node).gameObject.AddComponent<Image>();
                slot.Icon.preserveAspect = true;
                slot.Icon.raycastTarget = false;
                UIFactory.Place(slot.Icon, 0.06f, 0.06f, 0.94f, 0.94f);
                slot.Level = _ui.Label(node, "", 22, Color.white, TextAnchor.LowerRight);
                slot.Level.horizontalOverflow = HorizontalWrapMode.Overflow;
                UIFactory.Place(slot.Level, 0f, -0.18f, 1.08f, 0.5f);
                slot.Root.SetActive(false);
                _loadout.Add(slot);
                x += size + gap;
            }
        }

        private void UpdateLoadoutStrip()
        {
            if (_loadout.Count == 0 || _hud == null || !_hud.activeInHierarchy) return;
            if (Time.unscaledTime < _nextLoadoutRefresh) return;
            _nextLoadoutRefresh = Time.unscaledTime + 0.4f;

            IReadOnlyList<WeaponBehaviour> weapons = inventory.Weapons;
            for (int i = 0; i < WeaponInventory.MaxWeapons; i++)
            {
                LoadoutSlot slot = _loadout[i];
                bool has = i < weapons.Count && weapons[i] != null && weapons[i].Definition != null;
                slot.Root.SetActive(has);
                if (!has) continue;
                WeaponDefinition d = weapons[i].Definition;
                bool legendary = d.rarity == Rarity.Legendary; // evolutions and fusions
                ShowSlot(slot, d.icon, legendary || weapons[i].IsMaxLevel, weapons[i].Level,
                    i == 0 ? Palette.HotPink : Palette.Sky, legendary);
            }

            int p = 0;
            foreach (KeyValuePair<PassiveDefinition, int> pair in inventory.Passives)
            {
                if (p >= WeaponInventory.MaxPassives) break;
                LoadoutSlot slot = _loadout[WeaponInventory.MaxWeapons + p++];
                slot.Root.SetActive(true);
                ShowSlot(slot, pair.Key.icon, pair.Value >= pair.Key.maxLevel, pair.Value, Palette.Mint, false);
            }
            for (; p < WeaponInventory.MaxPassives; p++) _loadout[WeaponInventory.MaxWeapons + p].Root.SetActive(false);
        }

        private static void ShowSlot(LoadoutSlot slot, Sprite icon, bool maxed, int level, Color frame, bool evolved)
        {
            slot.Icon.sprite = icon;
            slot.Icon.enabled = icon != null;
            slot.Frame.color = new Color(frame.r * 0.45f, frame.g * 0.45f, frame.b * 0.55f, 0.8f);
            slot.Level.text = evolved ? "EVO" : maxed ? "MAX" : level.ToString(); // ASCII: the UI font has no star glyph
            slot.Level.color = evolved || maxed ? Palette.Honey : Color.white;
        }
    }
}
