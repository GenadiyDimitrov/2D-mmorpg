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
        /// <summary>How many quests may be pinned at once. The tracker earns its place by being
        /// readable at a glance while you fight; a dozen pinned quests is just the log again, in the
        /// way of the game (owner asked for a 3-5 limit).</summary>
        private const int MaxTrackedQuests = 5;

        private readonly List<string> _trackedQuests = new List<string>();
        private RectTransform _trackerPanel;
        private TextMeshProUGUI _trackerText;
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
        /// player who does not want it.</summary>
        private void BuildQuestTracker()
        {
            _trackerPanel = UiKit.PanelBox(_worldRoot, "QuestTracker");
            UiKit.Place(_trackerPanel, new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-12f, -220f), new Vector2(300f, 180f));
            var inner = _trackerPanel.GetChild(0);

            // Movable, like the other floating panels — it will inevitably sit over something.
            _trackerPanel.gameObject.AddComponent<DragMove>();

            _trackerText = UiKit.Label(inner, "", 14f, UiKit.Text, TextAlignmentOptions.TopLeft);
            UiKit.Stretch(UiKit.Rect(_trackerText.gameObject), 10f, 8f, 10f, 8f);
            var fitter = _trackerText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _trackerPanel.gameObject.SetActive(false);
        }

        /// <summary>Pin/unpin a quest. Pinning past the cap drops the OLDEST pin rather than refusing:
        /// the player asked for this one, and silently doing nothing reads as a broken button.</summary>
        private void ToggleTrackedQuest(string questId)
        {
            if (_trackedQuests.Remove(questId)) { _questStamp = -1; return; }
            _trackedQuests.Add(questId);
            while (_trackedQuests.Count > MaxTrackedQuests) _trackedQuests.RemoveAt(0);
            _questStamp = -1;   // force the log to redraw its Track/Untrack labels
        }

        /// <summary>Redraw the tracker. Called from the UI tick; rebuilds only when the text changes,
        /// since quest progress moves on kills, not frames.</summary>
        private void RefreshQuestTracker()
        {
            if (_trackerPanel == null) return;

            var log = Boot.Quests;
            var text = new StringBuilder();
            if (log != null)
            {
                foreach (var q in log.Active)
                {
                    if (!_trackedQuests.Contains(q.Id)) continue;
                    text.Append("<b>").Append(q.Name).Append("</b>");
                    if (q.CanComplete) text.Append("   <color=#7CE07C>READY</color>");
                    text.AppendLine();
                    if (!string.IsNullOrWhiteSpace(q.CurrentStepText))
                        text.Append("  ").AppendLine(q.CurrentStepText);
                    if (q.CounterNeeded > 0)
                        text.Append("  ").Append(q.Counter).Append(" / ").Append(q.CounterNeeded).AppendLine();
                }
            }

            // Drop pins for quests that are no longer active (handed in or abandoned) so the list does
            // not silently fill with dead entries.
            if (log != null && _trackedQuests.Count > 0)
                _trackedQuests.RemoveAll(id => !log.Active.Any(a => a.Id == id));

            string body = text.ToString().TrimEnd();
            if (body == _trackerStamp) return;
            _trackerStamp = body;

            bool show = body.Length > 0;
            if (_trackerPanel.gameObject.activeSelf != show) _trackerPanel.gameObject.SetActive(show);
            if (show) _trackerText.text = body;
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

            int stamp = _questTab * 7919 + entries.Length * 31 + _trackedQuests.Count;
            foreach (var e in entries)
                stamp = stamp * 31 + (int)e.State * 7 + e.StepIndex * 3 + StepCounter(e);
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

            if (entry.State == QuestAvailability.Active)
            {
                var step = CurrentStep(entry);
                if (step != null)
                {
                    if (!string.IsNullOrWhiteSpace(step.Text)) text.AppendLine(step.Text);
                    if (step.Needed > 1) text.AppendLine("Progress: " + step.Counter + " / " + step.Needed);
                    if (!string.IsNullOrWhiteSpace(step.Location)) text.AppendLine("Where: " + step.Location);
                }
                // A gathering contract has no step worth reading — what you carry IS the progress.
                foreach (var g in entry.Gathers ?? new QuestGatherDto[0])
                    text.AppendLine("Gathered: " + g.ItemName + "  " + g.Held + "   (" + g.MobName + ")");
            }
            else if (!string.IsNullOrEmpty(entry.GiverName))
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
            // MaxTrackedQuests: the point is a glance, and a wall of pinned text is not one.
            bool tracked = _trackedQuests.Contains(qid);
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
