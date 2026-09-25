using System;
using PofudukFilo.Core;
using PofudukFilo.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>
    /// Görevler (retention.md): the login-streak reward and three daily missions, each paying gold (and some
    /// Stardust). A trophy button on the menu opens it; it wears a "!" while something can be claimed.
    /// </summary>
    public sealed partial class GameUI
    {
        private MetaScreen _missions;

        private void BuildMissions(Transform menu, Transform safeRoot)
        {
            // Opened from the GÖREVLER tab in the bottom bar (its "!" badge lives on that tab).
            _missions = ListScreen(safeRoot, "Görevler", RefreshMissions);
        }

        private void RefreshMissionsBadge()
        {
            if (_missionsTile == null) return;
            long now = DateTime.UtcNow.Ticks;
            run.Meta.CheckIn(now);
            _missionsTile.Badge.SetActive(run.Meta.AnyMissionClaimable(now));
        }

        private void RefreshMissions()
        {
            ClearChildren(_missions.List);
            MetaProgressionService meta = run.Meta;
            long now = DateTime.UtcNow.Ticks;
            meta.CheckIn(now);

            // Login streak.
            int streak = meta.Streak;
            (int sGold, int sDust) = Missions.StreakReward(streak);
            Image streakRow = Row(_missions.List, "Streak", 210f);
            UIFactory.Place(_ui.Label(streakRow.transform, $"Giriş serisi: {streak}. gün", 42, Palette.Honey, TextAnchor.MiddleLeft),
                0.05f, 0.52f, 0.62f, 0.94f);
            Text streakInfo = _ui.Label(streakRow.transform, "Her gün gir, ödül büyüsün. 7. gün: büyük ödül!", 26, Palette.White, TextAnchor.UpperLeft);
            streakInfo.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.Place(streakInfo, 0.05f, 0.08f, 0.62f, 0.5f);
            bool canStreak = meta.CanClaimStreak(now);
            Button streakBtn = PriceButton(streakRow.transform, canStreak ? "Al" : "Alındı", sGold, sDust, canStreak ? Palette.Mint : Palette.Outline, () =>
            {
                if (run.Meta.ClaimStreak(DateTime.UtcNow.Ticks)) { RefreshMenu(); RefreshMissions(); }
            }, 34);
            streakBtn.interactable = canStreak;
            UIFactory.Place(streakBtn, 0.66f, 0.14f, 0.98f, 0.86f);

            // Today's missions.
            MissionDef[] missions = meta.TodaysMissions(now);
            for (int i = 0; i < missions.Length; i++)
            {
                MissionDef m = missions[i];
                int index = i;
                int progress = meta.MissionProgress(i);
                bool claimed = meta.MissionClaimed(i);
                bool ready = meta.CanClaimMission(i, now);

                Image row = Row(_missions.List, "Mission" + i, 210f);
                Text title = _ui.Label(row.transform, MissionText(m), 36, Palette.Cream, TextAnchor.MiddleLeft);
                title.horizontalOverflow = HorizontalWrapMode.Wrap;
                title.resizeTextForBestFit = true;
                title.resizeTextMinSize = 24;
                title.resizeTextMaxSize = 36;
                UIFactory.Place(title, 0.05f, 0.5f, 0.62f, 0.94f);

                HudBar bar = HudBar.Create(_ui, row.transform, barTrackSprite, barFillSprite, ready || claimed ? Palette.Mint : Palette.Sky, null, 24);
                UIFactory.Place(bar, 0.05f, 0.14f, 0.62f, 0.4f);
                bar.Snap((float)progress / Mathf.Max(1, m.Target));
                bar.Set((float)progress / Mathf.Max(1, m.Target), $"{progress}/{m.Target}");

                Button b = PriceButton(row.transform, claimed ? "Alındı" : ready ? "Al" : "", m.RewardGold, m.RewardDust,
                    ready ? Palette.Mint : Palette.Outline, () =>
                    {
                        if (run.Meta.ClaimMission(index, DateTime.UtcNow.Ticks)) { RefreshMenu(); RefreshMissions(); }
                    }, 34);
                b.interactable = ready;
                UIFactory.Place(b, 0.66f, 0.14f, 0.98f, 0.86f);
            }

            Text reset = _ui.Label(_missions.List, "Görevler her gün yenilenir.", 28, Palette.Lavender);
            reset.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;
        }

        private static string MissionText(in MissionDef m) => m.Kind switch
        {
            MissionKind.Kills => Loc.T($"{m.Target} düşman yok et"),
            MissionKind.Minutes => Loc.T($"Tek oyunda {m.Target} dakika dayan"),
            MissionKind.Gold => Loc.T($"Oyunlardan {m.Target} altın kazan"),
            MissionKind.Bosses => Loc.T($"{m.Target} boss yen"),
            MissionKind.Rushes => Loc.T($"{m.Target} kez Şeker Hücumu yap"),
            _ => Loc.T($"{m.Target} oyun oyna")
        };
    }
}
