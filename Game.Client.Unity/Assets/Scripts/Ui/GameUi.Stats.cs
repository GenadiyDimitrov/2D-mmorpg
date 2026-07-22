using System.Text;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the character sheet — the WPF client's Stats panel.
    ///
    /// Every number here already arrives in the StatsUpdate push; the Unity client was simply
    /// throwing it away. That is the shape of most of the remaining parity work: the server is
    /// complete, the transport is complete, and the client just had nowhere to show it.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _statsPanel;
        private TextMeshProUGUI _statsBody;
        private int _statsStamp = -1;

        private void BuildStatsWindow()
        {
            _statsPanel = UiKit.PanelBox(_worldRoot, "Stats");
            UiKit.Place(_statsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(620f, 460f));
            var inner = _statsPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_statsPanel, "Character", () => CloseWindow(_statsPanel));

            ScrollRect scroll;
            var content = UiKit.ScrollArea(inner, out scroll, 2f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 10f, 16f, 16f);

            _statsBody = UiKit.Label(content, "", 16f, UiKit.Text, TextAlignmentOptions.TopLeft);
            var fitter = _statsBody.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _statsPanel.gameObject.SetActive(false);
        }

        private void RefreshStatsWindow()
        {
            if (!_statsPanel.gameObject.activeSelf) return;

            var s = Boot.Stats;
            if (s == null) { _statsBody.text = "Waiting for stats …"; return; }

            // Rebuild only when a number actually moved. Regen ticks every 3s and HP/MP change
            // constantly, so a naive per-frame rebuild would re-lay out a long text block forever.
            // Karma and the kill counts are in the stamp too — they arrive on their own push, so a
            // sheet keyed only on StatsUpdate would keep showing yesterday's karma.
            int stamp = s.GetHashCode() ^ (Boot.Progress != null ? Boot.Progress.Level * 7919 : 0)
                      ^ (Boot.Karma * 31 + Boot.PkCount * 7 + Boot.PvpCount + (Boot.PvpEnabled ? 1 : 0));
            if (stamp == _statsStamp) return;
            _statsStamp = stamp;

            var t = new StringBuilder();

            var active = Boot.ActiveClass;
            if (active != null)
            {
                string cls = ClassCatalog.Get(active.SecondClass)?.Name;
                string disc = ThirdClassCatalog.Get(active.ThirdClass)?.Name;
                t.AppendLine(Head("Class"));
                t.AppendLine(Row2("Race", active.Race.ToString(), "Level", active.Level.ToString()));
                t.AppendLine(Row2("Class", cls ?? active.BaseClass.ToString(), "Discipline", disc ?? "—"));
                t.AppendLine();
            }

            t.AppendLine(Head("Primary"));
            t.AppendLine(Row2("CON", s.Con.ToString(), "ATK", s.Atk.ToString()));
            t.AppendLine(Row2("WIT", s.Wit.ToString(), "DEX", s.Dex.ToString()));
            t.AppendLine(Row2("SPT", s.Spt.ToString(), "SP", s.SkillPoints.ToString()));
            t.AppendLine();

            t.AppendLine(Head("Vitals"));
            t.AppendLine(Row2("Max HP", s.MaxHp.ToString(), "Max MP", s.MaxMp.ToString()));
            t.AppendLine(Row2("HP regen", s.HpRegen.ToString("0.#") + "/s",
                              "MP regen", s.MpRegen.ToString("0.#") + "/s"));
            t.AppendLine();

            t.AppendLine(Head("Offence"));
            t.AppendLine(Row2("P.Atk", s.AttackPower.ToString(), "M.Atk", s.MagicAttack.ToString()));
            t.AppendLine(Row2("Accuracy", s.Accuracy.ToString(), "Crit", Pct(s.CritChance)));
            t.AppendLine(Row2("Magic crit", Pct(s.MagicCritChance), "Crit dmg", "+" + Pct(s.CritDamage)));
            t.AppendLine(Row2("Atk speed", "x" + s.AttackSpeedMult.ToString("0.00"),
                              "Cast speed", "x" + s.CastSpeedMult.ToString("0.00")));
            t.AppendLine();

            t.AppendLine(Head("Defence"));
            t.AppendLine(Row2("P.Def", s.Defence.ToString(), "M.Def", s.MagicDefence.ToString()));
            t.AppendLine(Row2("Evasion", s.Evasion.ToString(), "Speed", s.MoveSpeed.ToString("0")));
            if (s.HasShield)
            {
                t.AppendLine(Row2("Block", Pct(s.BlockChance), "Block red.", Pct(s.BlockReduction)));
                t.AppendLine(Row2("Shield def", s.ShieldDefense.ToString(), "", ""));
            }
            t.AppendLine();

            t.AppendLine(Head("Gear"));
            t.AppendLine(Row2("Armour", string.IsNullOrEmpty(s.ArmorMastery) ? "—" : s.ArmorMastery,
                              "Set", string.IsNullOrEmpty(s.ActiveSet) ? "—" : s.ActiveSet));
            t.AppendLine(Row2("Gold", Boot.Gold.ToString("N0"), "State", s.MoveState.ToString()));

            // PvP / reputation, at the bottom because it is read rarely and matters enormously when it
            // is. Karma was not shown ANYWHERE before — and karma is what turns guards hostile, makes
            // you drop gear on death and takes the safety out of towns, so a player carrying it had no
            // way to find out except by dying.
            t.AppendLine();
            t.AppendLine(Head("PvP"));
            t.AppendLine(Row2("PvP kills", Boot.PvpCount.ToString(), "PK kills", Boot.PkCount.ToString()));
            t.AppendLine(Row2("Karma", Boot.Karma > 0 ? "<color=#FF6060>" + Boot.Karma.ToString("N0") + "</color>"
                                                      : "0",
                              "Flag", Boot.PvpEnabled ? "ON" : "off"));

            // Healers care about these and nothing else shows them; skip when they are neutral so a
            // fighter's sheet is not padded with 1.00s that mean "not applicable".
            if (s.HealPowerFlat != 0 || s.HealPowerMod != 1f)
            {
                t.AppendLine();
                t.AppendLine(Head("Healing"));
                t.AppendLine(Row2("Heal power", "+" + s.HealPowerFlat, "Heal mod", "x" + s.HealPowerMod.ToString("0.00")));
            }

            _statsBody.text = t.ToString().TrimEnd();
        }

        private static string Head(string title) => "<b>" + title + "</b>";

        /// <summary>Two label/value pairs per line — a phone is wide in landscape and a single column
        /// would need twice the scrolling.</summary>
        private static string Row2(string a, string av, string b, string bv)
        {
            string left = (a + ":").PadRight(12) + av;
            if (string.IsNullOrEmpty(b)) return left;
            return left.PadRight(30) + (b + ":").PadRight(12) + bv;
        }

        private static string Pct(float value) => (value * 100f).ToString("0.#") + "%";
    }
}
