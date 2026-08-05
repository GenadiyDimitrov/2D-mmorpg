using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the quest log — three tabs and a per-quest detail window (0.43.0).
    ///
    /// Owner, playtest-13: *"the quest windows (menu-&gt;quests) should show active/unavailable/
    /// compleated ... each row in each tab must have [details] button to show information about the
    /// quest/description - who gave it what u had to do each step etc"*.
    ///
    /// The middle tab is called AVAILABLE rather than "unavailable": it lists every quest you have not
    /// taken, the ones you can take right now first and the rest with the reason they are shut
    /// (*"lvl to high, lvl to low"*, an unfinished prerequisite). A tab that could only ever tell you
    /// what you CANNOT do would leave "what can I do now" answered nowhere.
    ///
    /// The window still does not accept or complete anything by itself — those belong to the NPC, and
    /// the detail window grows an Accept/Decline pair only while you are standing in that
    /// conversation. What it does own are the two things that are the player's alone: ABANDON and
    /// TRACK (pin it on screen).
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _questPanel, _questContent;
        private Button[] _questTabButtons;
        private int _questTab;                  // 0 active, 1 available, 2 completed
        private int _questStamp = -1;

        // ----- quest TRACKER -------------------------------------------------------------------
        // The pins are SERVER state (playtest-18 Q1). This client used to keep them in a
        // List<string> that nothing ever wrote anywhere, so they died with the app and belonged to the
        // install rather than to the character. It now reads QuestEntry.Tracked and asks the server to
        // toggle — the same rule as the skill bar: the client never authors state it did not receive.
        // The cap lives in GameConstants.MaxTrackedQuests, where both halves can see it.
        private RectTransform _trackerPanel, _trackerList;
        private string _trackerStamp = "";

        private void BuildQuestWindow()
        {
            _questPanel = UiKit.PanelBox(_worldRoot, "Quests");
            UiKit.Place(_questPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(680f, 480f));
            var inner = _questPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_questPanel, "Quests", () => CloseWindow(_questPanel));

            _questTabButtons = new Button[3];
            string[] names = { "Active", "Available", "Completed" };
            for (int i = 0; i < names.Length; i++)
            {
                int tab = i;
                var button = UiKit.TextButton(inner, names[i], () =>
                {
                    _questTab = tab;
                    _questStamp = -1;      // force a rebuild
                }, 17f);
                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(18f + i * 154f, -chrome - 6f), new Vector2(148f, 38f));
                _questTabButtons[i] = button;
            }

            ScrollRect scroll;
            _questContent = UiKit.ScrollArea(inner, out scroll, 4f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 50f, 16f, 16f);

            _questPanel.gameObject.SetActive(false);

            BuildQuestTracker();
            BuildQuestDetail();
        }

        /// <summary>The always-on-screen tracker: a small draggable panel listing the pinned quests and
        /// their current objective. Hidden entirely when nothing is pinned, so it costs nothing to the
        /// player who does not want it.
        ///
        /// One tappable ROW per pinned quest since playtest-18 Q4 (*"clicking a tracker row opens that
        /// quest's DETAIL page"*) — it used to be a single block of text, which had nothing to click.
        /// A row is the shortest path from "what am I doing" to the whole quest, and it skips the log
        /// window entirely.</summary>
        // The tracker sizes itself to its pins (see RefreshQuestTracker). Width is fixed, so the text
        // width a row will get is knowable up front: panel − the list's 8px insets − the label's 6px.
        private const float TrackerWidth = 300f;
        private const float TrackerTextWidth = TrackerWidth - 16f - 12f;
        private const float TrackerMaxHeight = 420f;

        private void BuildQuestTracker()
        {
            _trackerPanel = UiKit.PanelBox(_worldRoot, "QuestTracker");
            // Pivot is the TOP-right corner, so growing the height extends it downward — which is
            // what lets the panel follow its content without the pins walking up the screen.
            UiKit.Place(_trackerPanel, new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-12f, -220f), new Vector2(TrackerWidth, 180f));
            var inner = _trackerPanel.GetChild(0);

            // Movable, like the other floating panels — it will inevitably sit over something.
            _trackerPanel.gameObject.AddComponent<DragMove>();

            var listGo = new GameObject("Pins", typeof(RectTransform));
            listGo.transform.SetParent(inner, false);
            _trackerList = (RectTransform)listGo.transform;
            UiKit.Stretch(_trackerList, 8f, 6f, 8f, 6f);

            var layout = listGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 3f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            _trackerPanel.gameObject.SetActive(false);
        }

        /// <summary>Pin/unpin a quest. The server decides — it holds the pins and the cap, and pushes a
        /// fresh log back, which is what redraws both this window's Track/Untrack labels and the
        /// tracker itself.</summary>
        private void ToggleTrackedQuest(string questId)
        {
            Boot.QuestAction("track", questId);
        }

        /// <summary>Redraw the tracker. Called from the UI tick; rebuilds only when the text changes,
        /// since quest progress moves on kills, not frames.
        ///
        /// OBJECTIVES ONLY (playtest-18 Q3: *"the tracker row shows only the objectives (items /
        /// kills), not the full description"*). It reads the structured steps rather than the
        /// pre-formatted step sentence the dialog uses, so a gathering contract shows what you carry
        /// and a kill step shows its count on the same line — and nothing shows the story.</summary>
        private void RefreshQuestTracker()
        {
            if (_trackerPanel == null) return;

            var log = Boot.Quests;
            var entries = log == null || log.Entries == null ? new QuestEntry[0] : log.Entries;

            var pinIds = new List<string>();
            var pinRows = new List<string>();
            foreach (var q in entries)
            {
                if (!q.Tracked || q.State != QuestAvailability.Active) continue;

                var text = new StringBuilder();
                text.Append("<b>").Append(q.Name).Append("</b>");
                if (q.CanComplete) text.Append("   <color=#7CE07C>READY</color>");

                // A gathering contract's objective IS what you are carrying; it has no step worth
                // reading ("come back when you're done").
                var gathers = q.Gathers ?? new QuestGatherDto[0];
                if (gathers.Length > 0)
                {
                    foreach (var g in gathers)
                        text.AppendLine().Append("  ").Append(g.ItemName).Append("  ").Append(g.Held);
                }
                else
                {
                    var step = CurrentStep(q);
                    if (step != null)
                    {
                        if (!string.IsNullOrWhiteSpace(step.Text))
                            text.AppendLine().Append("  ").Append(step.Text);
                        if (step.Needed > 1)
                            text.Append("   ").Append(step.Counter).Append(" / ").Append(step.Needed);
                    }
                }
                pinIds.Add(q.Id);
                pinRows.Add(text.ToString());
            }

            // Rebuild only when a pinned quest, its order or its objective text changes — progress
            // moves on kills, not frames, and these rows are GameObjects now.
            string stamp = string.Join("|", pinIds.ToArray()) + "\n"
                         + string.Join("\n", pinRows.ToArray());
            if (stamp == _trackerStamp) return;
            _trackerStamp = stamp;

            for (int i = _trackerList.childCount - 1; i >= 0; i--)
                Destroy(_trackerList.GetChild(i).gameObject);

            float stacked = 0f;
            for (int p = 0; p < pinIds.Count; p++)
            {
                string qid = pinIds[p], body = pinRows[p];

                // The row IS the button (Q4). Its own faint fill both says "tappable" and separates one
                // pinned quest from the next, which a single text block never did.
                var row = UiKit.Box(_trackerList, "Pin", new Color(0.17f, 0.20f, 0.24f, 0.55f));
                var button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = row;
                string open = qid;
                button.onClick.AddListener(() => ShowQuestDetail(open));

                var label = UiKit.Label(row.transform, body, 14f, UiKit.Text, TextAlignmentOptions.TopLeft);
                UiKit.Stretch(UiKit.Rect(label.gameObject), 6f, 3f, 6f, 3f);

                // ⚠ MEASURE the text, don't count '\n'. Word-wrap is on, and a real objective —
                // "Hunt in the Bracken fields, then return to Huntmaster Cera" — is ONE newline and
                // TWO drawn lines, so a line count left every wrapped row short and the label spilled
                // onto the pin below it. GetPreferredValues asks TMP at the width the row will
                // actually have, before any layout pass has run.
                float h = label.GetPreferredValues(body, TrackerTextWidth, 0f).y + 6f;
                row.gameObject.AddComponent<LayoutElement>().minHeight = h;
                stacked += h + (p > 0 ? 3f : 0f);   // + the layout group's spacing
            }

            // And GROW THE PANEL to fit them. It was a fixed 180px, which Q2 then made worse by
            // auto-pinning on accept: five pins of two lines each need well over that, and with no
            // mask and no scroll the extra ones simply drew outside the panel onto the world. Capped
            // so a full tracker still can't swallow the screen.
            _trackerPanel.sizeDelta = new Vector2(TrackerWidth,
                Mathf.Clamp(stacked + 12f, 60f, TrackerMaxHeight));

            bool show = pinIds.Count > 0;
            if (_trackerPanel.gameObject.activeSelf != show) _trackerPanel.gameObject.SetActive(show);
        }

        /// <summary>The entries for one tab. Active is its own tab; AVAILABLE holds both the takeable
        /// and the locked (the server has already hidden what this character can never take).</summary>
        private static bool InTab(QuestEntry entry, int tab)
        {
            switch (tab)
            {
                case 0: return entry.State == QuestAvailability.Active;
                case 1: return entry.State == QuestAvailability.Available
                            || entry.State == QuestAvailability.Locked;
                default: return entry.State == QuestAvailability.Completed;
            }
        }

        private void RefreshQuestWindow()
        {
            if (!_questPanel.gameObject.activeSelf) return;

            for (int i = 0; i < _questTabButtons.Length; i++)
                _questTabButtons[i].targetGraphic.color =
                    i == _questTab ? UiKit.TabActive : UiKit.PanelLight;

            var log = Boot.Quests;
            var entries = log == null || log.Entries == null
                        ? new QuestEntry[0] : log.Entries;

            int stamp = _questTab * 7919 + entries.Length * 31;
            foreach (var e in entries)
                stamp = stamp * 31 + (int)e.State * 7 + e.StepIndex * 3 + StepCounter(e)
                      + (e.Tracked ? 1013 : 0);
            if (stamp == _questStamp) return;
            _questStamp = stamp;

            for (int i = _questContent.childCount - 1; i >= 0; i--)
                Destroy(_questContent.GetChild(i).gameObject);

            int shown = 0;
            foreach (var entry in entries)
            {
                if (!InTab(entry, _questTab)) continue;
                shown++;
                BuildQuestRow(entry);
            }

            if (shown == 0)
            {
                string empty = _questTab == 0 ? "Nothing in progress — talk to an NPC with a mark over their head."
                             : _questTab == 1 ? "Nothing left to take at your level."
                             : "You have not finished a quest yet.";
                var label = UiKit.Label(_questContent, empty, 16f, UiKit.TextDim);
                label.gameObject.AddComponent<LayoutElement>().minHeight = 34f;
            }
        }

        /// <summary>The live counter of the step the player is on — part of the redraw stamp, so a kill
        /// that moves "3 / 10" to "4 / 10" repaints and nothing else does.</summary>
        private static int StepCounter(QuestEntry entry)
        {
            if (entry.Steps == null) return 0;
            foreach (var s in entry.Steps) if (s.Current) return s.Counter;
            return 0;
        }

        private void BuildQuestRow(QuestEntry entry)
        {
            var row = UiKit.Box(_questContent, "Quest", UiKit.PanelLight);
            var element = row.gameObject.AddComponent<LayoutElement>();
            // Tall enough for the buttons stacked down the right edge — three on an active row
            // (Details / Track / Abandon), one everywhere else.
            element.minHeight = entry.State == QuestAvailability.Active ? 130f : 68f;

            var text = new StringBuilder();
            text.Append("<b>").Append(entry.Name).Append("</b>");
            if (entry.Steps != null && entry.Steps.Length > 1 && entry.State == QuestAvailability.Active)
                text.Append("   step ").Append(entry.StepIndex + 1).Append(" / ").Append(entry.Steps.Length);
            text.Append("   ").Append(LevelBand(entry));
            text.AppendLine();

            if (!string.IsNullOrEmpty(entry.Status))
                text.AppendLine(entry.Status);

            // SHORT, on every tab (playtest-18 Q5, the same rule as C6): name, level band, the one-line
            // status, the progress NUMBER and who to see. The step text, where to find it and the story
            // live in Details and nowhere else — an Active row used to print all of it and was four
            // times the height of an Available one.
            if (entry.State == QuestAvailability.Active)
            {
                var step = CurrentStep(entry);
                if (step != null && step.Needed > 1)
                    text.AppendLine("Progress: " + step.Counter + " / " + step.Needed);
                // A gathering contract has no step worth reading — what you carry IS the progress.
                foreach (var g in entry.Gathers ?? new QuestGatherDto[0])
                    text.AppendLine("Gathered: " + g.ItemName + "  " + g.Held);
            }

            if (!string.IsNullOrEmpty(entry.GiverName))
            {
                text.AppendLine("From: " + entry.GiverName
                                + (string.IsNullOrEmpty(entry.GiverLocation) ? "" : " — " + entry.GiverLocation));
            }

            Color colour = entry.CanComplete ? UiKit.Good
                         : entry.State == QuestAvailability.Locked ? UiKit.TextDim
                         : entry.State == QuestAvailability.Completed ? UiKit.TextDim
                         : UiKit.Text;

            var label = UiKit.Label(row.transform, text.ToString().TrimEnd(), 15f, colour,
                                    TextAlignmentOptions.TopLeft);
            UiKit.Stretch(UiKit.Rect(label.gameObject), 12f, 8f, 120f, 8f);   // room for the buttons

            var fitter = label.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            string qid = entry.Id, qname = entry.Name;

            // DETAILS — on every row of every tab (owner). The whole quest: who gave it, what each step
            // was, what it pays.
            var details = UiKit.TextButton(row.transform, "Details", () => ShowQuestDetail(qid), 14f);
            UiKit.Place(UiKit.Rect(details.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-10f, -10f), new Vector2(100f, 34f));

            if (entry.State != QuestAvailability.Active) return;

            // TRACK — pin this quest to the on-screen tracker so you can read the objective while
            // fighting, instead of opening the log every time (owner, playtest-13). Capped at
            // GameConstants.MaxTrackedQuests: the point is a glance, and a wall of pinned text is not
            // one. The flag comes from the server, which owns the pins.
            bool tracked = entry.Tracked;
            var track = UiKit.TextButton(row.transform, tracked ? "Untrack" : "Track",
                () => ToggleTrackedQuest(qid), 14f);
            UiKit.Place(UiKit.Rect(track.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-10f, -48f), new Vector2(100f, 34f));
            if (tracked) track.targetGraphic.color = UiKit.TabActive;

            // ABANDON, with a confirmation — the progress is gone, and if you have since climbed
            // past the quest's level ceiling you cannot take it again (owner, playtest-13).
            var drop = UiKit.TextButton(row.transform, "Abandon",
                () => Ask("Abandon \"" + qname + "\"?\nAll progress on it is lost, and if you are "
                          + "outside its level range you will not be able to take it again.",
                          "Abandon", () => Boot.QuestAction("abandon", qid)), 14f);
            UiKit.Place(UiKit.Rect(drop.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-10f, -86f), new Vector2(100f, 34f));
            drop.targetGraphic.color = new Color(0.42f, 0.20f, 0.20f, 0.95f);   // destructive
        }

        private static QuestStepDto CurrentStep(QuestEntry entry)
        {
            if (entry.Steps == null) return null;
            foreach (var s in entry.Steps) if (s.Current) return s;
            return entry.Steps.Length > 0 ? entry.Steps[entry.Steps.Length - 1] : null;
        }

        private static string LevelBand(QuestEntry entry)
        {
            if (entry.MaxLevel > 0) return "<color=#AEB6C2>Lv " + entry.MinLevel + "-" + entry.MaxLevel + "</color>";
            return "<color=#AEB6C2>Lv " + entry.MinLevel + "+</color>";
        }

        // ----- the DETAIL window ------------------------------------------------------------------

        private RectTransform _questDetailPanel;
        private TextMeshProUGUI _questDetailTitle, _questDetailBody;
        private Button _questAcceptButton, _questDeclineButton;
        private string _questDetailId = "";
        private object _questDetailLog;         // the log this page was drawn from

        private void BuildQuestDetail()
        {
            _questDetailPanel = UiKit.PanelBox(_worldRoot, "QuestDetail");
            UiKit.Place(_questDetailPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -10f), new Vector2(620f, 460f));
            var inner = _questDetailPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_questDetailPanel, "Quest",
                                              () => CloseWindow(_questDetailPanel));

            _questDetailTitle = UiKit.Label(inner, "", 20f, UiKit.Accent, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(_questDetailTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, -chrome - 8f), new Vector2(560f, 26f));

            ScrollRect scroll;
            var content = UiKit.ScrollArea(inner, out scroll, 2f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 42f, 16f, 70f);

            _questDetailBody = UiKit.Label(content, "", 16f, UiKit.Text, TextAlignmentOptions.TopLeft);
            var fitter = _questDetailBody.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // DECLINE is just "close" — a quest you did not take needs nothing said to the server. It is
            // spelled Decline while an NPC is offering, because that is the answer the conversation asked
            // for; it says Close everywhere else.
            _questDeclineButton = UiKit.TextButton(inner, "Close",
                                                   () => CloseWindow(_questDetailPanel), 16f);
            UiKit.Place(UiKit.Rect(_questDeclineButton.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(18f, 14f), new Vector2(260f, 46f));

            _questAcceptButton = UiKit.TextButton(inner, "Accept", () =>
            {
                string id = _questDetailId;
                CloseWindow(_questDetailPanel);
                Boot.QuestAction("accept", id);
            }, 16f);
            UiKit.Place(UiKit.Rect(_questAcceptButton.gameObject), new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-18f, 14f), new Vector2(260f, 46f));

            _questDetailPanel.gameObject.SetActive(false);
        }

        /// <summary>Keep an OPEN detail page live: the server pushes a whole new log on every kill that
        /// moves a counter, so redrawing when the log object changes is both exact and free. Without it
        /// a page opened mid-hunt would sit frozen at the count it had when you tapped Details.</summary>
        private void RefreshQuestDetail()
        {
            if (_questDetailPanel == null || !_questDetailPanel.gameObject.activeSelf) return;
            if (ReferenceEquals(Boot.Quests, _questDetailLog)) return;
            ShowQuestDetail(_questDetailId);
        }

        /// <summary>Everything about one quest, from the log or from the conversation. Accept appears
        /// only while the NPC in front of you is actually offering it — the detail window is the place
        /// the decision is made, so it is the place the offer belongs (roadmap: *"per-quest detail
        /// window with accept/decline instead of one wall of text"*).</summary>
        private void ShowQuestDetail(string questId)
        {
            var entry = FindQuestEntry(questId);
            var offered = OfferedNow(questId);
            if (entry == null && offered == null) return;

            _questDetailId = questId;
            _questDetailLog = Boot.Quests;
            _questDetailTitle.text = entry != null ? entry.Name : offered.Name;

            var text = new StringBuilder();
            string description = entry != null ? entry.Description : offered.Description;
            if (!string.IsNullOrWhiteSpace(description)) text.AppendLine(description).AppendLine();

            if (entry != null)
            {
                if (!string.IsNullOrEmpty(entry.Status)) text.AppendLine("<b>" + entry.Status + "</b>");
                text.AppendLine("Level:   " + (entry.MaxLevel > 0
                    ? entry.MinLevel + " - " + entry.MaxLevel : entry.MinLevel + " and up"));
                if (!string.IsNullOrEmpty(entry.GiverName))
                    text.AppendLine("Given by:   " + entry.GiverName
                        + (string.IsNullOrEmpty(entry.GiverLocation) ? "" : " — " + entry.GiverLocation));
                if (entry.Daily) text.AppendLine("Daily — once per server day.");
                else if (entry.Repeatable) text.AppendLine("Repeatable — the giver hands it back.");

                text.AppendLine();
                text.AppendLine("<b>Steps</b>");
                for (int i = 0; i < (entry.Steps == null ? 0 : entry.Steps.Length); i++)
                {
                    var step = entry.Steps[i];
                    // [x] done, -> the one you are on, · still ahead. A quest you have not taken shows
                    // all of them as plain dots: it is a plan, not progress. ASCII-safe marks on
                    // purpose — the TMP font ships Latin-1, and a tick would render as a box.
                    string mark = step.Done ? "<color=#7CE07C>[x]</color>"
                                : step.Current ? "<color=#5BA6FF>→</color>" : "<color=#AEB6C2>·</color>";
                    string line = mark + " " + step.Text;
                    if (step.Needed > 1 && (step.Current || step.Done))
                        line += "   " + step.Counter + " / " + step.Needed;
                    else if (step.Needed > 1)
                        line += "   (" + step.Needed + ")";
                    text.AppendLine(line);
                    if (!string.IsNullOrWhiteSpace(step.Location))
                        text.AppendLine("     <color=#AEB6C2>" + step.Location + "</color>");
                }

                if (entry.Gathers != null && entry.Gathers.Length > 0)
                {
                    text.AppendLine();
                    text.AppendLine("<b>Gather</b>");
                    foreach (var g in entry.Gathers)
                    {
                        // The token's worth is a share of what the creature itself pays, so it stays
                        // level-appropriate — say that, rather than printing a number that is not one.
                        string chance = g.DropChance >= 1f ? "every kill"
                                      : (g.DropChance * 100f).ToString("0.#") + "% of kills";
                        text.AppendLine("· " + g.ItemName + " — " + g.MobName + ", " + chance);
                        text.AppendLine("     <color=#AEB6C2>you carry " + g.Held + " · each pays "
                                        + (g.RewardModifier * 100f).ToString("0") + "% of that creature's own exp and gold</color>");
                    }
                }

                if (!string.IsNullOrEmpty(entry.RewardText))
                {
                    text.AppendLine();
                    text.AppendLine("<b>Reward</b>");
                    text.AppendLine(entry.RewardText);
                }
            }

            _questDetailBody.text = text.ToString().TrimEnd();

            bool canAccept = offered != null;
            _questAcceptButton.gameObject.SetActive(canAccept);
            UiKit.SetButtonText(_questDeclineButton, canAccept ? "Decline" : "Close");
            UiKit.Place(UiKit.Rect(_questDeclineButton.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(18f, 14f), new Vector2(canAccept ? 260f : 560f, 46f));

            OpenWindow(_questDetailPanel);
        }

        private QuestEntry FindQuestEntry(string questId)
        {
            var log = Boot.Quests;
            if (log == null || log.Entries == null) return null;
            foreach (var e in log.Entries) if (e.Id == questId) return e;
            return null;
        }

        /// <summary>The quest as the NPC you are TALKING TO is offering it, or null — which is also the
        /// test for whether Accept may be shown at all.</summary>
        private QuestSummary OfferedNow(string questId)
        {
            var d = Boot.Dialog;
            if (d == null || d.Offered == null || !_dialogPanel.gameObject.activeSelf) return null;
            foreach (var q in d.Offered) if (q.Id == questId) return q;
            return null;
        }
    }
}
