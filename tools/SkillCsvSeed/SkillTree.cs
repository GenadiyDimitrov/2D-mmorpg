using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Game.Shared;

/// <summary>`BL-330` step 1 — THE SKILL TREE PAGE, generated (owner, 2026-10-01: *"skill tree per race. U select a race
/// then select fighter or mage and then from there onward"*).
///
/// Writes `docs/design/SkillTree.html`: one self-contained page, its data embedded as JSON, built from the COMPILED
/// tables (`ClassSkills`, `ClassCatalog`, `Disciplines`, `ClassNames`) and the faces (`SkillFaces`), so it says what the
/// game teaches and can never drift from it — re-run after any class-table or face change.
///
/// Each step of a path lists only what THAT step adds: the 1st class's own list; what a fighter or mage keeps for life
/// (race layer + grade, from `Cumulative` minus the base list); the 2nd class's list; the 3rd class's (its discipline
/// list + stat swaps, again by difference); the 4th's own list. The all-classes 4th kit (`shared 4th.csv` + sigils) is
/// listed once. A skill appears once per step with every character level it can be learned at; its name and text are
/// the face that race/lineage sees, at the TOP rung that step teaches.</summary>
internal static class SkillTree
{
    private static readonly Race[] Races = { Race.Human, Race.Elf, Race.Demon };
    private static readonly Regex Tags = new("<[^>]+>", RegexOptions.Compiled);

    private sealed record Skill(string id, string n, string c, int[] lv, string d);

    public static int Run(string repo)
    {
        var races = new List<object>();
        object[]? shared = null;

        foreach (var race in Races)
        {
            var bases = new List<object>();
            foreach (var bc in new[] { BaseClass.Fighter, BaseClass.Mage })
            {
                var lin1 = SkillFaces.Lineage(race, bc, null, null, false);
                var own1 = ClassSkills.ForClass(race, bc, null, null);
                var life = Minus(ClassSkills.Cumulative(race, bc, null, null), own1);

                var seconds = new List<object>();
                foreach (var sc in ClassCatalog.OptionsFor(race, bc))
                {
                    var arch = sc.Archetype;
                    var lin2 = SkillFaces.Lineage(race, bc, arch, null, false);
                    var thirds = new List<object>();
                    var (a, b) = Disciplines.Of(race, arch);
                    foreach (var d in b is Discipline bd ? new[] { a, bd } : new[] { a })
                    {
                        var lin3 = SkillFaces.Lineage(race, bc, arch, d, false);
                        var lin4 = SkillFaces.Lineage(race, bc, arch, d, true);
                        var own3 = Minus(ClassSkills.Cumulative(race, bc, arch, d), ClassSkills.Cumulative(race, bc, arch, null));
                        var own4 = ClassSkills.ForClass(race, bc, arch, d, fourth: true);
                        if (shared is null)
                        {
                            var all4 = Minus(Minus(ClassSkills.Cumulative(race, bc, arch, d, true),
                                                   ClassSkills.Cumulative(race, bc, arch, d)), own4);
                            shared = Group(all4, null, null);
                        }
                        thirds.Add(new
                        {
                            third = ClassNames.Third(d, race),
                            fourth = ClassNames.Fourth(d, race),
                            s3 = Group(own3, race, lin3),
                            s4 = Group(own4, race, lin4),
                        });
                    }
                    seconds.Add(new
                    {
                        name = sc.Name,
                        blurb = ClassCatalog.ArchetypeBlurb(arch),
                        s2 = Group(ClassSkills.ForClass(race, bc, arch, null), race, lin2),
                        thirds,
                    });
                }
                bases.Add(new
                {
                    name = bc.ToString(),
                    s1 = Group(own1, race, lin1),
                    life = Group(life, race, lin1),
                    seconds,
                });
            }
            races.Add(new { name = race.ToString(), bases });
        }

        var data = JsonSerializer.Serialize(new { version = GameConstants.GameVersion, races, shared });
        string outPath = Path.Combine(repo, "docs", "design", "SkillTree.html");
        File.WriteAllText(outPath, Template.Replace("/*DATA*/null", data), new UTF8Encoding(false));
        Console.WriteLine($"Wrote {outPath} ({new FileInfo(outPath).Length / 1024} KB, game {GameConstants.GameVersion}).");
        return 0;
    }

    /// <summary>The rows of <paramref name="all"/> that <paramref name="minus"/> does not have (a multiset difference
    /// keyed on skill, learn level and rung).</summary>
    private static List<ClassSkill> Minus(IEnumerable<ClassSkill> all, IEnumerable<ClassSkill> minus)
    {
        var left = minus.GroupBy(Key).ToDictionary(g => g.Key, g => g.Count());
        var result = new List<ClassSkill>();
        foreach (var cs in all)
        {
            var k = Key(cs);
            if (left.TryGetValue(k, out int n) && n > 0) { left[k] = n - 1; continue; }
            result.Add(cs);
        }
        return result;
    }

    private static (string, int, int) Key(ClassSkill cs) => (cs.SkillId, cs.LearnLevel, cs.SkillLevel);

    private static object[] Group(IEnumerable<ClassSkill> rows, Race? race, IReadOnlyList<string>? lineage) =>
        rows.GroupBy(r => r.SkillId)
            .Select(g =>
            {
                var def = SkillCatalog.Get(g.Key);
                int top = g.Max(r => r.SkillLevel);
                var face = SkillFaces.For(g.Key, race, lineage);
                string desc = Tags.Replace(SkillFaces.DescriptionOf(face, g.Key, top), "").Trim();
                return new Skill(g.Key, SkillFaces.NameOf(face, g.Key, top),
                                 (def?.Category ?? SkillCategory.Physical).ToString(),
                                 g.Select(r => r.LearnLevel).Distinct().OrderBy(x => x).ToArray(), desc);
            })
            .OrderBy(s => s.lv[0]).ThenBy(s => s.n, StringComparer.OrdinalIgnoreCase)
            .Cast<object>().ToArray();

    // The page. `/*DATA*/null` is replaced with the JSON above.
    private const string Template = """
<title>L2Clone Skill Tree</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Alegreya+SC:wght@500;700&family=Source+Sans+3:wght@400;600&family=JetBrains+Mono:wght@400&display=swap">
<style>
/* Layout: a stepper (race -> Fighter/Mage -> 2nd -> 3rd) over one column of tier sections, each a skill list. */
:root {
  --bg: #f3f4f1; --surface: #ffffff; --ink: #1d2422; --dim: #5d6863; --line: #d6dbd6;
  --accent: #8a5a12; --accent-soft: #f1e5cf;
  --phys: #9b3b2e; --magic: #2e5d9b; --buff: #2f7a4a; --debuff: #7a2f73; --heal: #2a7f7c; --passive: #6b6b5e;
  --display: "Alegreya SC", Georgia, serif; --body: "Source Sans 3", system-ui, sans-serif;
  --mono: "JetBrains Mono", ui-monospace, monospace;
}
@media (prefers-color-scheme: dark) { :root:not([data-theme="light"]) {
  --bg: #141917; --surface: #1c2220; --ink: #e4e8e3; --dim: #9aa6a0; --line: #2f3834;
  --accent: #e0b25f; --accent-soft: #3a2f1c;
  --phys: #e08474; --magic: #84aee8; --buff: #7dcc97; --debuff: #d48ace; --heal: #74cfca; --passive: #b5b5a5;
  color-scheme: dark } }
:root[data-theme="dark"] {
  --bg: #141917; --surface: #1c2220; --ink: #e4e8e3; --dim: #9aa6a0; --line: #2f3834;
  --accent: #e0b25f; --accent-soft: #3a2f1c;
  --phys: #e08474; --magic: #84aee8; --buff: #7dcc97; --debuff: #d48ace; --heal: #74cfca; --passive: #b5b5a5;
  color-scheme: dark }
body { background: var(--bg); color: var(--ink); font: 16px/1.5 var(--body); }
.wrap { max-width: 960px; margin: 0 auto; padding-inline: 16px; padding-block: 24px 48px; }
h1 { font: 700 2rem/1.1 var(--display); margin: 0; text-wrap: balance; }
.sub { color: var(--dim); margin: 6px 0 20px; max-width: 65ch; }
.steps { display: grid; gap: 12px; margin-bottom: 24px; }
.step { display: flex; flex-wrap: wrap; align-items: center; gap: 8px; }
.step-label { font: 600 0.75rem var(--body); letter-spacing: 0.08em; text-transform: uppercase; color: var(--dim); width: 5.5rem; }
.choice { font: 600 0.95rem var(--body); color: var(--ink); background: var(--surface); border: 1px solid var(--line);
  border-radius: 6px; padding: 6px 12px; cursor: pointer; }
.choice:hover { border-color: var(--accent); }
.choice[aria-pressed="true"] { background: var(--accent-soft); border-color: var(--accent); color: var(--ink); }
.choice:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
.blurb { color: var(--dim); font-size: 0.9rem; margin: 0 0 20px; max-width: 65ch; }
section.tier { margin-top: 28px; }
.tier h2 { font: 500 1.35rem/1.2 var(--display); margin: 0 0 2px; display: flex; flex-wrap: wrap; gap: 4px 12px; align-items: baseline; }
.tier h2 .band { font: 400 0.8rem var(--mono); color: var(--accent); }
.tier .note { color: var(--dim); font-size: 0.85rem; margin: 0 0 10px; }
.list { border-top: 1px solid var(--line); }
details { border-bottom: 1px solid var(--line); }
summary { display: grid; grid-template-columns: 3.2rem minmax(0, 1fr) auto; gap: 12px; align-items: baseline;
  padding: 8px 4px; cursor: pointer; list-style: none; }
summary::-webkit-details-marker { display: none; }
summary:focus-visible { outline: 2px solid var(--accent); outline-offset: -2px; }
.first { font: 400 0.85rem var(--mono); color: var(--accent); font-variant-numeric: tabular-nums; }
.name { font-weight: 600; min-width: 0; }
.kind { font: 600 0.7rem var(--body); letter-spacing: 0.06em; text-transform: uppercase; }
.k-Physical { color: var(--phys); } .k-Magic { color: var(--magic); } .k-Buff { color: var(--buff); }
.k-Debuff { color: var(--debuff); } .k-Heal { color: var(--heal); } .k-Passive { color: var(--passive); }
.more { padding: 0 4px 12px calc(3.2rem + 16px); display: grid; gap: 6px; }
.more p { margin: 0; max-width: 65ch; }
.lv { font: 400 0.8rem var(--mono); color: var(--dim); font-variant-numeric: tabular-nums; overflow-wrap: anywhere; }
.empty { color: var(--dim); font-style: italic; padding: 8px 4px; }
footer { margin-top: 36px; color: var(--dim); font-size: 0.8rem; }
@media (max-width: 480px) { .step-label { width: 100%; } .more { padding-left: 4px; } }
</style>
<div class="wrap">
  <h1>Skill Tree</h1>
  <p class="sub">Pick a race, then Fighter or Mage, then the paths it opens. Each section lists only what that class
    adds; tap a skill for its text and every level it can be learned at.</p>
  <div class="steps" id="steps"></div>
  <p class="blurb" id="blurb"></p>
  <div id="tiers"></div>
  <footer id="foot"></footer>
</div>
<script>
const DATA = /*DATA*/null;
const state = { race: 0, base: 0, second: 0, third: 0 };
try { Object.assign(state, JSON.parse(localStorage.getItem("skilltree") || "{}")); } catch (e) {}
const el = (tag, cls, text) => { const e = document.createElement(tag); if (cls) e.className = cls; if (text != null) e.textContent = text; return e; };

function stepRow(label, items, key) {
  const row = el("div", "step");
  row.append(el("span", "step-label", label));
  items.forEach((name, i) => {
    const b = el("button", "choice", name);
    b.type = "button"; b.id = key + "-" + i;
    b.setAttribute("aria-pressed", state[key] === i ? "true" : "false");
    b.onclick = () => {
      state[key] = i;
      const order = ["race", "base", "second", "third"];
      order.slice(order.indexOf(key) + 1).forEach(k => state[k] = 0);
      render();
    };
    row.append(b);
  });
  return row;
}

function tier(title, band, note, skills) {
  const s = el("section", "tier");
  const h = el("h2"); h.append(el("span", null, title)); if (band) h.append(el("span", "band", band));
  s.append(h);
  if (note) s.append(el("p", "note", note));
  const list = el("div", "list");
  if (!skills.length) list.append(el("div", "empty", "Nothing authored here yet."));
  skills.forEach(sk => {
    const d = el("details");
    const sum = el("summary");
    sum.append(el("span", "first", "Lv " + sk.lv[0]), el("span", "name", sk.n), el("span", "kind k-" + sk.c, sk.c));
    const more = el("div", "more");
    if (sk.d) more.append(el("p", null, sk.d));
    more.append(el("span", "lv", (sk.lv.length > 1 ? "Learned at levels " : "Learned at level ") + sk.lv.join(" · ")));
    d.append(sum, more);
    list.append(d);
  });
  s.append(list);
  return s;
}

function render() {
  const race = DATA.races[state.race] || DATA.races[0];
  const base = race.bases[state.base] || race.bases[0];
  const second = base.seconds[state.second] || base.seconds[0];
  const third = second.thirds[state.third] || second.thirds[0];
  try { localStorage.setItem("skilltree", JSON.stringify(state)); } catch (e) {}

  const steps = document.getElementById("steps");
  steps.replaceChildren(
    stepRow("Race", DATA.races.map(r => r.name), "race"),
    stepRow("Start as", race.bases.map(b => b.name), "base"),
    stepRow("2nd class", base.seconds.map(s => s.name), "second"),
    stepRow("3rd class", second.thirds.map(t => t.third + " → " + t.fourth), "third"));
  document.getElementById("blurb").textContent = second.blurb;

  const tiers = document.getElementById("tiers");
  tiers.replaceChildren(
    tier(race.name + " " + base.name, "1st class · 1-19", null, base.s1),
    tier("Kept for life", "every " + race.name + " " + base.name.toLowerCase(),
         "The race skills and grade passives stay through every class change.", base.life),
    tier(second.name, "2nd class · 20-39", null, second.s2),
    tier(third.third, "3rd class · 40-75", null, third.s3),
    tier(third.fourth, "4th class · 76+", "After the Rite of Ascension.", third.s4),
    tier("Every 4th class", "shared", "Learned by all classes once ascended, plus the sigils.", DATA.shared));
  document.getElementById("foot").textContent =
    "Generated from the game's own class tables and skill names, version " + DATA.version + ".";
}
render();
</script>
""";
}
