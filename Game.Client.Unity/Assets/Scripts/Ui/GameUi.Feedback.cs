using System.Collections.Generic;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the things that tell you what is happening RIGHT NOW — the cast bar,
    /// floating damage numbers, level-coloured names and the zone readout.
    ///
    /// Without these the game is silent: you press a skill and nothing visibly happens until an HP bar
    /// twitches, and you cannot tell a level-3 rat from a level-70 one until it kills you.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        // cast bar
        private RectTransform _castPanel;
        private TextMeshProUGUI _castLabel;
        private Image _castFill;

        // zone readout
        private TextMeshProUGUI _zoneLabel;

        // floating damage
        private class FloatingNumber
        {
            public RectTransform Root;
            public TextMeshProUGUI Label;
            public Vector3 World;
            public float BornAt;
        }

        private readonly List<FloatingNumber> _floaters = new List<FloatingNumber>();
        private const float FloatSeconds = 1.1f;
        private const float FloatRisePixels = 70f;

        private void BuildFeedback()
        {
            // Cast bar: centred low, where a phone's thumbs are not covering it.
            _castPanel = UiKit.PanelBox(_worldRoot, "CastBar");
            UiKit.Place(_castPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(0f, 140f), new Vector2(420f, 44f));
            var inner = _castPanel.GetChild(0);

            _castFill = UiKit.ValueBar(inner, UiKit.Accent);
            UiKit.Stretch(UiKit.Rect(_castFill.transform.parent.gameObject), 8f, 22f, 8f, 8f);

            _castLabel = UiKit.Label(inner, "", 16f, UiKit.Text, TextAlignmentOptions.Center);
            UiKit.Place(UiKit.Rect(_castLabel.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -3f), new Vector2(400f, 20f));

            // THE WHOLE BAR IS THE CANCEL BUTTON.
            //
            // Android has no ESC key, and a small "Stop" button next to a 44px bar is a poor target
            // for a thumb during the two seconds it exists. Mobile MMOs overwhelmingly make the cast
            // bar itself the cancel target: it is large, it is centred where a thumb already rests,
            // and it only exists while there is something to cancel. The label stays so it is
            // obviously pressable rather than a mystery.
            var cancel = _castPanel.gameObject.AddComponent<Button>();
            cancel.targetGraphic = _castPanel.GetComponent<Image>();
            cancel.onClick.AddListener(() => Boot.CancelCast());

            var hint = UiKit.Label(inner, "tap to cancel", 13f, UiKit.TextDim, TextAlignmentOptions.Right);
            UiKit.Place(UiKit.Rect(hint.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-10f, -3f), new Vector2(140f, 18f));

            _castPanel.gameObject.SetActive(false);

            // Zone readout rides under the self panel — "where am I and what lives here".
            _zoneLabel = UiKit.Label(_worldRoot, "", 15f, UiKit.TextDim, TextAlignmentOptions.Left);
            UiKit.Place(UiKit.Rect(_zoneLabel.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(16f, -168f), new Vector2(360f, 22f));
        }

        /// <summary>
        /// Subscribed on the first frame that HAS a Boot, not while building.
        ///
        /// Awake runs the instant AddComponent is called — before GameBoot assigns itself to Boot — so
        /// `Boot.CombatHappened += …` during the build threw a NullReferenceException, which aborted
        /// Awake halfway and left the UI PARTLY CONSTRUCTED: panels that had already been created were
        /// on screen, everything after the throw did not exist, and the phase switching then had
        /// nothing coherent to show or hide. On the device that reads as "the game froze".
        /// </summary>
        private void HookFeedback()
        {
            Boot.CombatHappened -= SpawnDamageNumber;   // idempotent: no double numbers on a re-hook
            Boot.CombatHappened += SpawnDamageNumber;
        }

        private void UnhookFeedback()
        {
            if (Boot != null) Boot.CombatHappened -= SpawnDamageNumber;
        }

        private void RefreshFeedback()
        {
            RefreshCastBar();
            RefreshZoneLabel();
            RefreshFloaters();
        }

        private void RefreshCastBar()
        {
            bool casting = !string.IsNullOrEmpty(Boot.CastingSkill)
                           && Time.realtimeSinceStartup < Boot.CastEndsAt;
            _castPanel.gameObject.SetActive(casting);
            if (!casting) return;

            float total = Mathf.Max(0.01f, Boot.CastEndsAt - Boot.CastStartedAt);
            float done = Time.realtimeSinceStartup - Boot.CastStartedAt;
            UiKit.SetBar(_castFill, done, total);
            _castLabel.text = Boot.CastingSkill + "   " + Mathf.Max(0f, total - done).ToString("0.0") + "s";
        }

        /// <summary>Name the ground you are standing on: the town if you are in one, otherwise the
        /// level band of the spawn zone. Read from WorldMap, which is the same data the server spawns
        /// from, so it cannot drift.</summary>
        private void RefreshZoneLabel()
        {
            EntityDto self = null;
            if (Boot.Entities != null) Boot.Entities.TryGetState(Boot.SelfId, out self);
            if (self == null) { _zoneLabel.text = ""; return; }

            var town = WorldMap.SafeZoneAt(self.X, self.Y);
            if (town != null)
            {
                _zoneLabel.text = town.Name + "   (safe zone)";
                _zoneLabel.color = new Color(0.55f, 0.75f, 1f);
                return;
            }

            foreach (var zone in WorldMap.SpawnZones)
            {
                float dx = self.X - zone.X, dy = self.Y - zone.Y;
                if (dx * dx + dy * dy > zone.Radius * zone.Radius) continue;

                _zoneLabel.text = "Hunting ground   Lv " + zone.MinLevel + "-" + zone.MaxLevel;
                _zoneLabel.color = LevelColour(zone.MaxLevel, self.Level);
                return;
            }

            _zoneLabel.text = "Wilds";
            _zoneLabel.color = UiKit.TextDim;
        }

        /// <summary>
        /// How dangerous something is RELATIVE to you, L2-style: far above → red, above → orange,
        /// even → white, below → green, trivial → grey. Absolute level means little; the gap is what
        /// decides whether you can take it.
        /// </summary>
        internal static Color LevelColour(int targetLevel, int myLevel)
        {
            if (myLevel <= 0 || targetLevel <= 0) return UiKit.Text;
            int gap = targetLevel - myLevel;
            if (gap >= 9)  return new Color(1.00f, 0.30f, 0.30f);   // red — will kill you
            if (gap >= 5)  return new Color(1.00f, 0.55f, 0.25f);   // orange
            if (gap >= 2)  return new Color(1.00f, 0.85f, 0.35f);   // yellow
            if (gap >= -2) return new Color(0.92f, 0.94f, 0.96f);   // white — an even fight
            if (gap >= -8) return new Color(0.55f, 0.90f, 0.55f);   // green
            return new Color(0.60f, 0.63f, 0.68f);                  // grey — not worth the walk
        }

        // ----- floating damage --------------------------------------------------------------------

        private void SpawnDamageNumber(CombatEvent e)
        {
            if (Boot.Entities == null) return;

            // Anchor on the entity that was HIT — damage belongs where it landed. Misses still show,
            // because "nothing happened" and "I missed" look identical otherwise.
            var view = Boot.Entities.Find(e.TargetId);
            if (view == null) return;

            bool incoming = e.TargetId == Boot.SelfId;
            string text;
            Color colour;

            switch (e.Outcome)
            {
                case CombatOutcome.Miss:
                    text = "miss"; colour = UiKit.TextDim; break;
                case CombatOutcome.Crit:
                    text = e.Damage.ToString() + "!"; colour = new Color(1f, 0.85f, 0.30f); break;
                default:
                    if (e.Damage <= 0) return;
                    text = e.Damage.ToString();
                    colour = incoming ? new Color(1f, 0.45f, 0.45f) : UiKit.Text;
                    break;
            }

            var floater = FreeFloater();
            floater.Label.text = text;
            floater.Label.color = colour;
            floater.Label.fontSize = e.Outcome == CombatOutcome.Crit ? 26f : 20f;
            floater.World = view.transform.position + Vector3.up * NameplateHeight;
            floater.BornAt = Time.unscaledTime;
            floater.Root.gameObject.SetActive(true);
        }

        private FloatingNumber FreeFloater()
        {
            foreach (var floater in _floaters)
                if (!floater.Root.gameObject.activeSelf) return floater;

            var root = UiKit.Rect(UiKit.Box(_nameplateLayer, "Damage",
                                            new Color(0, 0, 0, 0), blocksInput: false).gameObject);
            root.sizeDelta = new Vector2(160f, 34f);
            var label = UiKit.Label(root, "", 20f, UiKit.Text, TextAlignmentOptions.Center);
            UiKit.Stretch(UiKit.Rect(label.gameObject), 0f, 0f, 0f, 0f);

            var created = new FloatingNumber { Root = root, Label = label };
            _floaters.Add(created);
            return created;
        }

        private void RefreshFloaters()
        {
            var cam = Camera.main;
            if (cam == null) return;

            foreach (var floater in _floaters)
            {
                if (!floater.Root.gameObject.activeSelf) continue;

                float age = (Time.unscaledTime - floater.BornAt) / FloatSeconds;
                if (age >= 1f) { floater.Root.gameObject.SetActive(false); continue; }

                var screen = cam.WorldToScreenPoint(floater.World);
                if (screen.z <= 0f) { floater.Root.gameObject.SetActive(false); continue; }

                // Rise and fade. The number stays pinned to the WORLD point it was born at rather than
                // following the entity — a mob that walks off should not drag its damage with it.
                floater.Root.position = screen + new Vector3(0f, age * FloatRisePixels, 0f);
                var colour = floater.Label.color;
                colour.a = 1f - age * age;
                floater.Label.color = colour;
            }
        }
    }
}
