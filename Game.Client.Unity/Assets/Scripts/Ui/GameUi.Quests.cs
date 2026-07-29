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
    /// Read-only on purpose. Accepting, advancing and completing a quest all happen through an NPC
    /// (QuestAction carries the NPC's entity id), so a log that offered those buttons would be
    /// offering something the server refuses without a conversation. That conversation is the NPC
    /// dialog panel — the next batch.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _questPanel, _questContent;
        private int _questStamp = -1;

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
