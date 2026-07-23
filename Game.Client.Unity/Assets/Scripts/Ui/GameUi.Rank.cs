using System.Collections.Generic;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the RANK window — server leaderboards for Level, Wealth, PvP kills, Player
    /// kills and Time played. Five tabs across the top; each fetches its board on demand
    /// (Boot.RequestLeaderboard → the hub reads it straight from the DB). The #1 of each board wears an
    /// honorary title (Leaderboards.TopTitle), shown beside the name — the seed of a rewards layer.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _rankPanel, _rankList;
        private readonly List<(string Cat, Button Btn)> _rankTabs = new();
        private string _rankCategory = "level";

        private void BuildRankWindow()
        {
            _rankPanel = UiKit.PanelBox(_worldRoot, "Rank");
            UiKit.Place(_rankPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(560f, 640f));
            var inner = _rankPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_rankPanel, "Leaderboards", () => CloseWindow(_rankPanel));

            // Tab row.
            float tw = 104f, gap = 6f, x0 = 10f;
            for (int i = 0; i < Leaderboards.Categories.Length; i++)
            {
                string cat = Leaderboards.Categories[i];
                var btn = UiKit.TextButton(inner, Leaderboards.Label(cat), () => SelectRankTab(cat), 13f);
                UiKit.Place(UiKit.Rect(btn.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(x0 + i * (tw + gap), -chrome - 4f), new Vector2(tw, 36f));
                _rankTabs.Add((cat, btn));
            }

            _rankList = UiKit.ScrollArea(inner, out var scroll, 3f);
            UiKit.Stretch((RectTransform)scroll.transform, 10f, chrome + 46f, 10f, 10f);

            _rankPanel.gameObject.SetActive(false);
        }

        /// <summary>Open the leaderboard window and (re)load the current tab.</summary>
        private void OpenRank()
        {
            OpenWindow(_rankPanel);
            SelectRankTab(_rankCategory);
        }

        private void SelectRankTab(string category)
        {
            _rankCategory = category;
            foreach (var (cat, btn) in _rankTabs)   // persistent active-tint via the ColorBlock
            {
                var cb = btn.colors;
                cb.normalColor = cat == category ? UiKit.TabActive : UiKit.PanelLight;
                btn.colors = cb;
            }

            RankNote("Loading…");
            Boot.RequestLeaderboard(category, PopulateRank);
        }

        private void PopulateRank(LeaderboardDto dto)
        {
            if (dto == null || _rankList == null) return;
            if (dto.Category != _rankCategory) return;   // a later tab won the race — ignore this one

            for (int i = _rankList.childCount - 1; i >= 0; i--)
                Destroy(_rankList.GetChild(i).gameObject);

            if (dto.Entries == null || dto.Entries.Count == 0)
            {
                RankNote("No one has ranked here yet.");
                return;
            }

            foreach (var e in dto.Entries)
            {
                string title = string.IsNullOrEmpty(e.Title) ? "" : "   «" + e.Title + "»";
                string text = "#" + e.Rank + "   " + e.Name + "   Lv " + e.Level
                            + "   " + FormatRankValue(_rankCategory, e.Value) + title;
                var label = UiKit.Label(_rankList, text, 16f,
                                        e.Rank == 1 ? UiKit.Good : UiKit.Text, TextAlignmentOptions.Left);
                label.gameObject.AddComponent<LayoutElement>().minHeight = 34f;
            }
        }

        private void RankNote(string text)
        {
            for (int i = _rankList.childCount - 1; i >= 0; i--)
                Destroy(_rankList.GetChild(i).gameObject);
            UiKit.Label(_rankList, text, 16f, UiKit.TextDim).gameObject.AddComponent<LayoutElement>().minHeight = 34f;
        }

        private static string FormatRankValue(string category, long v) => category switch
        {
            "gold"   => v.ToString("N0") + " " + GameConstants.CurrencyName,
            "pvp"    => v + (v == 1 ? " kill" : " kills"),
            "pk"     => v + (v == 1 ? " kill" : " kills"),
            "online" => (v / 3600) + "h " + (v % 3600 / 60) + "m",
            _        => "",   // level is already shown as "Lv N"
        };
    }
}
