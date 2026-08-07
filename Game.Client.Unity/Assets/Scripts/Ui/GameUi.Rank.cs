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

        /// <summary>The pseudo-category for the TITLES tab. Not a leaderboard: it shows what you may
        /// wear rather than who is winning, and it lives in this window because the titles come from
        /// these boards and this is where you were looking when you read that you had won one.</summary>
        private const string TitlesTab = "titles";

        /// <summary>Last Titles push drawn, so the tab redraws when the server re-reads the boards.</summary>
        private int _seenTitlesRevision = -1;

        private void BuildRankWindow()
        {
            _rankPanel = UiKit.PanelBox(_worldRoot, "Rank");
            UiKit.Place(_rankPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(700f, 640f));
            var inner = _rankPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_rankPanel, "Leaderboards", () => CloseWindow(_rankPanel));

            // Tab row: the boards, then Titles. Sized to FIT the row — at the old 104 wide the sixth
            // board (charisma, added later) already hung off the right edge of the window.
            float tw = 92f, gap = 6f, x0 = 10f;
            for (int i = 0; i <= Leaderboards.Categories.Length; i++)
            {
                bool titles = i == Leaderboards.Categories.Length;
                string cat = titles ? TitlesTab : Leaderboards.Categories[i];
                var btn = UiKit.TextButton(inner, titles ? "Titles" : Leaderboards.Label(cat),
                                           () => SelectRankTab(cat), 13f);
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

            if (category == TitlesTab) { PopulateTitles(); return; }

            RankNote("Loading…");
            Boot.RequestLeaderboard(category, PopulateRank);
        }

        /// <summary>
        /// The titles you may WEAR. One row per board you currently top, plus "No title".
        ///
        /// It only ever offers what the server said you hold: a title is held while you are rank 1, not
        /// earned and kept, so this list shrinks when someone out-ranks you and the server refuses
        /// anything stale anyway.
        /// </summary>
        private void PopulateTitles()
        {
            if (_rankList == null) return;
            _seenTitlesRevision = Boot.TitlesRevision;

            for (int i = _rankList.childCount - 1; i >= 0; i--)
                Destroy(_rankList.GetChild(i).gameObject);

            var held = Boot.HeldTitles ?? new string[0];
            string worn = Boot.WornTitle ?? "";

            TitleRow("No title", worn.Length == 0, () => Boot.SetTitle(""));

            // Each row in the title's OWN colour, so the picker previews what will actually sit over
            // your head. Staff titles come through here too — Source() is what tells them apart, since
            // "top of ..." would be a lie for one held by role.
            foreach (var cat in held)
                TitleRow("<color=#" + TitleCatalog.ColorHex(cat) + ">«" + TitleCatalog.Text(cat)
                         + "»</color>   — " + TitleCatalog.Source(cat),
                         cat == worn, () => Boot.SetTitle(cat));

            // THE ONE YOU WROTE. Only shown to a character granted the right — for everyone else the
            // row would be an advertisement for something they cannot do anything about.
            if (Boot.MayWriteTitle && !string.IsNullOrEmpty(Boot.CustomTitle))
            {
                string hex = string.IsNullOrEmpty(Boot.CustomTitleColor)
                           ? TitleCatalog.DefaultHex : Boot.CustomTitleColor;
                TitleRow("<color=#" + hex + ">«" + Boot.CustomTitle + "»</color>   — your own",
                         worn == TitleCatalog.Custom, () => Boot.SetTitle(TitleCatalog.Custom));
            }

            if (held.Length == 0 && string.IsNullOrEmpty(Boot.CustomTitle))
            {
                var note = UiKit.Label(_rankList,
                    "You hold no titles yet. Reach #1 on any board and its title becomes yours to wear "
                    + "— for as long as you hold the top spot.", 15f, UiKit.TextDim);
                note.gameObject.AddComponent<LayoutElement>().minHeight = 60f;
            }

            if (Boot.MayWriteTitle)
            {
                var note = UiKit.Label(_rankList,
                    "You may name yourself:  /title <text>   (" + TitleCatalog.MaxCustomLength
                    + " characters)\n/titlecolor <colour>  —  " + TitleCatalog.PaletteNames()
                    + "\nRank titles are earned, not written.", 14f, UiKit.TextDim);
                note.gameObject.AddComponent<LayoutElement>().minHeight = 76f;
            }
        }

        /// <summary>One pickable title: the name, and a button that reads WORN when it is the one on
        /// your head. The state is on the button rather than a separate tick so there is exactly one
        /// thing to look at per row.</summary>
        private void TitleRow(string text, bool worn, System.Action onWear)
        {
            var row = UiKit.Box(_rankList, "Row", UiKit.PanelLight);
            row.gameObject.AddComponent<LayoutElement>().minHeight = 46f;

            var label = UiKit.Label(row.transform, text, 16f,
                                    worn ? UiKit.Good : UiKit.Text, TextAlignmentOptions.Left);
            UiKit.Place(UiKit.Rect(label.gameObject), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(14f, 0f), new Vector2(460f, 40f));

            var button = UiKit.TextButton(row.transform, worn ? "Worn" : "Wear", onWear, 15f);
            UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                        new Vector2(-12f, 0f), new Vector2(96f, 38f));
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
                // The #1's title, in its board's colour. The text is sent; the colour comes from the
                // category this list IS, so no extra field has to ride every row to say it.
                string title = string.IsNullOrEmpty(e.Title)
                             ? ""
                             : "   <color=#" + TitleCatalog.ColorHex(_rankCategory) + ">«"
                               + e.Title + "»</color>";
                string text = "#" + e.Rank + "   " + e.Name + "   Lv " + e.Level
                            + "   " + FormatRankValue(_rankCategory, e.Value) + title;
                var label = UiKit.Label(_rankList, text, 16f,
                                        e.Rank == 1 ? UiKit.Good : UiKit.Text, TextAlignmentOptions.Left);
                label.gameObject.AddComponent<LayoutElement>().minHeight = 34f;
            }
        }

        /// <summary>Redraw the Titles tab when the server pushes a new set — you may have just won or
        /// lost one while the window was open, and a picker showing a title you no longer hold would
        /// only produce a refusal.</summary>
        private void RefreshTitlesTab()
        {
            if (_rankPanel == null || !_rankPanel.gameObject.activeSelf) return;
            if (_rankCategory != TitlesTab || _seenTitlesRevision == Boot.TitlesRevision) return;
            PopulateTitles();
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
