using PofudukFilo.Core;
using PofudukFilo.Meta;
using PofudukFilo.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>
    /// Ar-Ge (design/ux/research.md): the one place gold becomes permanent power. Every row says in words
    /// what it does, what it gives now and after the purchase, and what it costs — device feedback
    /// 2026-09-24 found the old Atölye/Ocak/Laboratuvar split unreadable. Workshop "Hasar"/"Atış Hızı" are
    /// hidden here because the Forge's Ateş Gücü/Ateş Hızı do the same job (old levels still apply).
    /// </summary>
    public sealed partial class GameUI
    {
        private static readonly string[] HiddenWorkshopIds = { "damage", "fire_rate" };

        private void OpenResearch() => OpenMeta(_research);

        /// <summary>QA only (QaAutopilot screenshots): "research", "armory", "pilots", "settings" or "menu".</summary>
        public void QaShow(string screen)
        {
            HideMetaScreens();
            _menu.SetActive(true);
            switch (screen)
            {
                case "research": OpenResearch(); break;
                case "armory": OpenLab(); break;
                case "pilots": OpenHangar(); break;
                case "settings": OpenSettings(); break;
                case "missions": OpenMeta(_missions); break;
            }
        }

        private void RefreshResearch()
        {
            ClearChildren(_research.List);
            MetaProgressionService meta = run.Meta;

            Section(_research.List, "SALDIRI  ·  her oyunda geçerli");
            ForgeRow(ForgeTrack.Power, "Ateş Gücü", "Tüm silahların ve yardımcı pilotların hasarı artar. Sınırsız.",
                Formulas.ForgePowerPerLevel);
            ForgeRow(ForgeTrack.Speed, "Ateş Hızı", "Tüm silahlar ve yardımcı pilotlar daha sık ateş eder.",
                Formulas.ForgeSpeedPerLevel);

            Section(_research.List, "SAVUNMA");
            WorkshopRows(meta, "health", "armor", "revive");
            Section(_research.List, "YARDIMCI");
            WorkshopRows(meta, "gold", "experience", "luck", "reroll", "banish");

            Section(_research.List, "YILDIZ HARİTASI");
            Image row = Row(_research.List, "Constellation", 150f);
            Text info = _ui.Label(row.transform, "Boss yendikçe Yıldız Tozu kazanırsın; özel yetenekleri burada açarsın.",
                32, Palette.White, TextAnchor.MiddleLeft);
            info.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.Place(info, 0.04f, 0.08f, 0.64f, 0.92f);
            UIFactory.Place(_ui.Button(row.transform, "Aç", Palette.Lavender, () =>
            {
                _research.Root.SetActive(false);
                OpenMeta(_constellation);
            }, 44), 0.66f, 0.16f, 0.98f, 0.84f);
        }

        private void Section(Transform list, string title)
        {
            RectTransform node = _ui.Node("Section", list);
            node.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;
            Text t = _ui.Label(node, title, 38, Palette.Honey, TextAnchor.LowerLeft);
            UIFactory.Place(t, 0.02f, 0f, 1f, 1f);
        }

        private void ForgeRow(ForgeTrack track, string name, string description, float perLevel)
        {
            MetaProgressionService meta = run.Meta;
            int level = meta.GetForgeLevel(track);
            bool maxed = meta.IsForgeMaxed(track);
            string maxText = track == ForgeTrack.Speed ? $"/{Formulas.MaxForgeSpeedLevel}" : "";
            ResearchRow(name, $"Sv.{level}{maxText}", description,
                Pct(perLevel * level), maxed ? null : Pct(perLevel * (level + 1)),
                maxed ? -1 : meta.ForgeCost(track), meta.CanUpgradeForge(track),
                () => meta.TryUpgradeForge(track));
        }

        private void WorkshopRows(MetaProgressionService meta, params string[] ids)
        {
            foreach (string id in ids)
            foreach (MetaUpgradeDefinition u in run.Workshop)
            {
                if (u == null || u.id != id || System.Array.IndexOf(HiddenWorkshopIds, u.id) >= 0) continue;
                int level = meta.GetLevel(u);
                bool maxed = meta.IsMaxed(u);
                MetaUpgradeDefinition captured = u;
                ResearchRow(u.displayName, $"Sv.{level}/{u.maxLevel}", WorkshopDescription(u.stat),
                    WorkshopValue(u, level), maxed ? null : WorkshopValue(u, level + 1),
                    maxed ? -1 : u.CostForNext(level), meta.CanAfford(u),
                    () => meta.TryPurchase(captured));
            }
        }

        /// <summary>
        /// One upgrade row: name + level chip, a plain sentence of what it does, "Şu an X » Y", and the price.
        /// <paramref name="next"/> null = maxed.
        /// </summary>
        private void ResearchRow(string name, string levelChip, string description, string now, string next,
            int cost, bool canBuy, System.Func<bool> buy)
        {
            Image row = Row(_research.List, name, 230f);

            Text title = _ui.Label(row.transform, name, 46, Palette.Cream, TextAnchor.MiddleLeft);
            UIFactory.Place(title, 0.04f, 0.66f, 0.46f, 0.95f);
            Text chip = _ui.Label(row.transform, levelChip, 34, Palette.Mint, TextAnchor.MiddleLeft);
            UIFactory.Place(chip, 0.46f, 0.66f, 0.66f, 0.95f);

            Text desc = _ui.Label(row.transform, description, 30, Palette.White, TextAnchor.UpperLeft);
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.resizeTextForBestFit = true;
            desc.resizeTextMinSize = 22;
            desc.resizeTextMaxSize = 30;
            UIFactory.Place(desc, 0.04f, 0.28f, 0.65f, 0.66f);

            string value = next == null ? Loc.T($"Şu an {now}  ·  MAKS") : Loc.T($"Şu an {now}  »  {next}");
            Text val = _ui.Label(row.transform, value, 32, Palette.Honey, TextAnchor.MiddleLeft);
            UIFactory.Place(val, 0.04f, 0.04f, 0.65f, 0.28f);

            Button b;
            if (next == null)
            {
                b = _ui.Button(row.transform, "MAKS", Palette.Lavender, null, 44);
                b.interactable = false;
            }
            else
            {
                b = PriceButton(row.transform, "Geliştir", cost, 0, canBuy ? Palette.HotPink : Palette.Lavender, () =>
                {
                    if (buy()) RefreshWallet();
                }, 36);
                b.interactable = canBuy;
            }
            UIFactory.Place(b, 0.67f, 0.14f, 0.98f, 0.86f);
        }

        private static string Pct(float fraction) => $"+%{Mathf.RoundToInt(fraction * 1000f) / 10f:0.#}";

        private static string WorkshopValue(MetaUpgradeDefinition u, int level)
        {
            float v = u.effectPerLevel * level;
            return u.stat switch
            {
                StatType.Armor => $"-%{Mathf.RoundToInt(Formulas.ArmorReduction(v) * 100f)} hasar",
                StatType.Rerolls or StatType.Banishes or StatType.Revives => $"+{Mathf.RoundToInt(v)} hak",
                _ => Pct(v)
            };
        }

        private static string WorkshopDescription(StatType stat) => stat switch
        {
            StatType.MaxHp => "Oyuna daha fazla maksimum canla başlarsın.",
            StatType.Armor => "Zırh her darbenin bir yüzdesini emer (en çok %60).",
            StatType.Revives => "Ölünce olduğun yerde dirilme hakkı (her oyun).",
            StatType.GoldGain => "Her oyunda daha çok altın: gelişim daha hızlı.",
            StatType.Experience => "Taşlardan daha çok tecrübe: daha hızlı seviye atlarsın.",
            StatType.Luck => "Nadir ve efsanevi kartların çıkma şansı artar.",
            StatType.Rerolls => "Seviye atlayınca kartları yeniden çekme hakkı.",
            StatType.Banishes => "İstemediğin bir kartı o oyundan tamamen çıkarma hakkı.",
            StatType.Damage => "Tüm silahların hasarı artar.",
            StatType.CooldownReduction => "Silahlar daha sık ateş eder.",
            _ => ""
        };

        /// <summary>True when anything in Ar-Ge is affordable right now (menu tile badge).</summary>
        private bool ResearchAffordable()
        {
            MetaProgressionService meta = run.Meta;
            if (meta.CanUpgradeForge(ForgeTrack.Power) || meta.CanUpgradeForge(ForgeTrack.Speed)) return true;
            foreach (MetaUpgradeDefinition u in run.Workshop)
                if (u != null && System.Array.IndexOf(HiddenWorkshopIds, u.id) < 0 && !meta.IsMaxed(u) && meta.CanAfford(u)) return true;
            return false;
        }
    }
}
