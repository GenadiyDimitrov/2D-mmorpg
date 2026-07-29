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
    /// GameUi, continued: the quest log.
    ///
    /// Accepting, advancing and completing a quest all happen through an NPC (QuestAction carries the
    /// NPC's entity id), so the log does not offer those. It DOES offer the two things that are the
    /// player's alone: ABANDON (give it up) and TRACK (pin it on screen).
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _questPanel, _questContent;
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
                        Vector2.zero, new Vector2(640f, 440f));
            var inner = _questPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_questPanel, "Quests", () => CloseWindow(_questPanel));

            ScrollRect scroll;
            _questContent = UiKit.ScrollArea(inner, out scroll, 4f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 10f, 16f, 16f);

            _questPanel.gameObject.SetActive(false);

            BuildQuestTracker();
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

        private void RefreshQuestWindow()
        {
            if (!_questPanel.gameObject.activeSelf) return;

            var log = Boot.Quests;
            int stamp = log == null ? 0
                      : log.Active.Length * 31 + log.Completed.Length;
            foreach (var q in log?.Active ?? new QuestSummary[0])
                stamp = stamp * 31 + q.StepIndex * 7 + q.Counter;
            if (stamp == _questStamp) return;
            _questStamp = stamp;

            for (int i = _questContent.childCount - 1; i >= 0; i--)
                Destroy(_questContent.GetChild(i).gameObject);

            if (log == null || (log.Active.Length == 0 && log.Completed.Length == 0))
            {
                var empty = UiKit.Label(_questContent, "No quests yet — talk to an NPC.", 16f, UiKit.TextDim);
                empty.gameObject.AddComponent<LayoutElement>().minHeight = 34f;
                return;
            }

            foreach (var quest in log.Active)
            {
                var row = UiKit.Box(_questContent, "Quest", UiKit.PanelLight);
                var element = row.gameObject.AddComponent<LayoutElement>();
                element.minHeight = 84f;

                var text = new StringBuilder();
                text.Append("<b>").Append(quest.Name).Append("</b>");
                if (quest.StepCount > 1) text.Append("   step ").Append(quest.StepIndex + 1)
                                             .Append(" / ").Append(quest.StepCount);
                if (quest.CanComplete) text.Append("   <b>READY TO HAND IN</b>");
                text.AppendLine();

                if (!string.IsNullOrWhiteSpace(quest.CurrentStepText))
                    text.AppendLine(quest.CurrentStepText);

                // A counter with no target reads as noise; one with a target is the whole progress bar.
                if (quest.CounterNeeded > 0)
                    text.AppendLine("Progress: " + quest.Counter + " / " + quest.CounterNeeded);

                if (!string.IsNullOrWhiteSpace(quest.Location))
                    text.AppendLine("Where: " + quest.Location);

                var label = UiKit.Label(row.transform, text.ToString().TrimEnd(), 15f,
                                        quest.CanComplete ? UiKit.Good : UiKit.Text,
                                        TextAlignmentOptions.TopLeft);
                UiKit.Stretch(UiKit.Rect(label.gameObject), 12f, 8f, 120f, 8f);   // room for the button

                var fitter = label.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // ABANDON, with a confirmation — the progress is gone, and if you have since climbed
                // past the quest's level ceiling you cannot take it again (owner, playtest-13).
                string qid = quest.Id, qname = quest.Name;

                // TRACK — pin this quest to the on-screen tracker so you can read the objective while
                // fighting, instead of opening the log every time (owner, playtest-13). Capped at
                // MaxTrackedQuests: the point is a glance, and a wall of pinned text is not one.
                bool tracked = _trackedQuests.Contains(qid);
                var track = UiKit.TextButton(row.transform, tracked ? "Untrack" : "Track",
                    () => ToggleTrackedQuest(qid), 14f);
                UiKit.Place(UiKit.Rect(track.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                            new Vector2(-10f, -48f), new Vector2(100f, 34f));
                if (tracked) track.targetGraphic.color = UiKit.TabActive;

                var drop = UiKit.TextButton(row.transform, "Abandon",
                    () => Ask("Abandon \"" + qname + "\"?\nAll progress on it is lost, and if you are "
                              + "outside its level range you will not be able to take it again.",
                              "Abandon", () => Boot.QuestAction("abandon", qid)), 14f);
                UiKit.Place(UiKit.Rect(drop.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                            new Vector2(-10f, -10f), new Vector2(100f, 34f));
                drop.targetGraphic.color = new Color(0.42f, 0.20f, 0.20f, 0.95f);   // destructive
            }

            if (log.Completed.Length > 0)
            {
                var done = UiKit.Label(_questContent,
                    "Completed (" + log.Completed.Length + "):  " + string.Join(", ", log.Completed),
                    14f, UiKit.TextDim, TextAlignmentOptions.TopLeft);
                var fitter = done.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                done.gameObject.AddComponent<LayoutElement>().minHeight = 30f;
            }
        }
    }
}
