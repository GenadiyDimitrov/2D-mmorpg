using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Game.Shared;

/// <summary>`BL-330` step 1 — THE SKILL TREE PAGE, generated (owner, 2026-10-01: *"skill tree per race. U select a race
/// then select fighter or mage and then from there onward"*).
///
/// Writes `docs/design/SkillTree.html`: one self-contained page, its data embedded as JSON. The data is
/// <see cref="SkillTreeData"/> in Game.Shared — the SAME builder the client's Skill Tree window reads (step 2), so the
/// page and the game cannot disagree. Re-run after any class-table, face or icon change. This file only shapes that data
/// into the page's short JSON keys, strips the TMP tags the game renders, and embeds the icons.
///
/// Expanded, a skill shows ONE ROW PER LEARN LEVEL with that rung's own text (his §120a: *"each row to show its own
/// descirpion -> to compare powers"*). The STAT SWAPS and the SIGILS are their own tab beside the races (his §120a
/// follow-up).</summary>
internal static class SkillTree
{
    private static readonly Regex Tags = new("<[^>]+>", RegexOptions.Compiled);

    public static int Run(string repo)
    {
        var races = SkillTreeData.Races.Select(race => new
        {
            name = race.ToString(),
            bases = SkillTreeData.For(race).Select(b => new
            {
                name = b.BaseClass.ToString(),
                s1 = Page(b.First),
                life = Page(b.Life),
                seconds = b.Seconds.Select(s => new
                {
                    name = s.Name,
                    blurb = s.Blurb,
                    s2 = Page(s.Second),
                    thirds = s.Thirds.Select(t => new
                    {
                        third = t.ThirdName,
                        fourth = t.FourthName,
                        s3 = Page(t.Third),
                        s4 = Page(t.Fourth),
                    }).ToArray(),
                }).ToArray(),
            }).ToArray(),
        }).ToArray();
        var shared = Page(SkillTreeData.Shared4th());
        var swaps = Page(SkillTreeData.Swaps());
        var sigils = SkillTreeData.Sigils().Select(g => new { name = g.Name, s = Page(g.Skills) }).ToArray();

        var icons = Icons(repo);
        var data = JsonSerializer.Serialize(new { version = GameConstants.GameVersion, races, shared, swaps, sigils, icons },
            new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        string outPath = Path.Combine(repo, "docs", "design", "SkillTree.html");
        File.WriteAllText(outPath, Template.Replace("/*DATA*/null", data), new UTF8Encoding(false));
        Console.WriteLine($"Wrote {outPath} ({new FileInfo(outPath).Length / 1024} KB, game {GameConstants.GameVersion}).");
        return 0;
    }

    /// <summary>The page's shape for a skill: short keys (id, n = name, c = kind, lv = learn levels, r = rungs, t = tag),
    /// text without TMP tags. Every skill passed here is also queued for an icon.</summary>
    private sealed record Row(int l, int k, string? n, string d);
    private sealed record Skill(string id, string n, string c, int[] lv, Row[] r, string? t);

    private static Skill[] Page(IEnumerable<SkillTreeSkill> skills) => skills.Select(s =>
    {
        Shown.Add(s.Id);
        return new Skill(s.Id, s.Name, s.Category.ToString(), s.Rungs.Select(r => r.Level).Distinct().ToArray(),
                         s.Rungs.Select(r => new Row(r.Level, r.Rung, r.Name, Tags.Replace(r.Text, "").Trim())).ToArray(),
                         s.Tag);
    }).ToArray();

    /// <summary>Every skill the page lists, so <see cref="Icons"/> embeds only those.</summary>
    private static readonly HashSet<string> Shown = new();

    /// <summary>`BL-331`'s icons, once per skill: the client's own 128px PNGs (rendered by tools/SkillIcons from
    /// `skill_icons.csv`) shrunk to 48px WebP — the full PNGs would make the page ~7 MB. A skill with no PNG is left
    /// out and the page draws an empty square, as the game keeps its letters.</summary>
    private static Dictionary<string, string> Icons(string repo)
    {
        string dir = Path.Combine(repo, "Game.Client.Unity", "Assets", "Resources", "SkillIcons");
        var icons = new Dictionary<string, string>();
        foreach (var id in Shown.Order(StringComparer.Ordinal))
        {
            string png = Path.Combine(dir, id + ".png");
            if (!File.Exists(png)) continue;
            using var src = SkiaSharp.SKBitmap.Decode(png);
            using var small = src.Resize(new SkiaSharp.SKImageInfo(48, 48),
                                         new SkiaSharp.SKSamplingOptions(SkiaSharp.SKCubicResampler.Mitchell));
            using var img = SkiaSharp.SKImage.FromBitmap(small);
            using var webp = img.Encode(SkiaSharp.SKEncodedImageFormat.Webp, 88);
            icons[id] = Convert.ToBase64String(webp.ToArray());
        }
        Console.WriteLine($"Icons: {icons.Count} of {Shown.Count} skills ({Shown.Count - icons.Count} keep the blank square).");
        return icons;
    }

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
.choice.extra { border-style: dashed; }
.blurb { color: var(--dim); font-size: 0.9rem; margin: 0 0 20px; max-width: 65ch; }
section.tier { margin-top: 28px; }
.tier h2 { font: 500 1.35rem/1.2 var(--display); margin: 0 0 2px; display: flex; flex-wrap: wrap; gap: 4px 12px; align-items: baseline; }
.tier h2 .band { font: 400 0.8rem var(--mono); color: var(--accent); }
.tier .note { color: var(--dim); font-size: 0.85rem; margin: 0 0 10px; max-width: 65ch; }
.list { border-top: 1px solid var(--line); }
details { border-bottom: 1px solid var(--line); }
summary { display: grid; grid-template-columns: 3.2rem 32px minmax(0, 1fr) auto; gap: 12px; align-items: center;
  padding: 8px 4px; cursor: pointer; list-style: none; }
summary::-webkit-details-marker { display: none; }
summary:focus-visible { outline: 2px solid var(--accent); outline-offset: -2px; }
.first { font: 400 0.85rem var(--mono); color: var(--accent); font-variant-numeric: tabular-nums; }
.name { font-weight: 600; min-width: 0; }
.icon { width: 32px; height: 32px; border-radius: 4px; display: block; background: var(--line); }
.kind { font: 600 0.7rem var(--body); letter-spacing: 0.06em; text-transform: uppercase; }
.k-Physical { color: var(--phys); } .k-Magic { color: var(--magic); } .k-Buff { color: var(--buff); }
.k-Debuff { color: var(--debuff); } .k-Heal { color: var(--heal); } .k-Passive { color: var(--passive); }
.more { padding: 0 4px 12px calc(3.2rem + 32px + 28px); display: grid; gap: 6px; }
.more p { margin: 0; max-width: 65ch; }
.tag { font: 600 0.75rem var(--body); letter-spacing: 0.06em; text-transform: uppercase; color: var(--dim); }
.rung { display: grid; grid-template-columns: 5.5rem minmax(0, 1fr); gap: 12px; align-items: baseline; }
.rung .at { font: 400 0.8rem var(--mono); color: var(--accent); font-variant-numeric: tabular-nums; }
.rung .rn { font-weight: 600; }
.empty { color: var(--dim); font-style: italic; padding: 8px 4px; }
footer { margin-top: 36px; color: var(--dim); font-size: 0.8rem; }
@media (max-width: 480px) { .step-label { width: 100%; } .more { padding-left: 4px; }
  .rung { grid-template-columns: 4.5rem minmax(0, 1fr); } }
</style>
<div class="wrap">
  <h1>Skill Tree</h1>
  <p class="sub">Pick a race, then Fighter or Mage, then the paths it opens. Each section lists only what that class
    adds; tap a skill to see every level it is learned at, each with its own text. The stat swaps and sigils have
    their own tab after the races.</p>
  <div class="steps" id="steps"></div>
  <p class="blurb" id="blurb"></p>
  <div id="tiers"></div>
  <footer id="foot"></footer>
</div>
<script>
const DATA = /*DATA*/null;
const EXTRAS = "Swaps & Sigils";
const state = { race: 0, base: 0, second: 0, third: 0 };
try { Object.assign(state, JSON.parse(localStorage.getItem("skilltree") || "{}")); } catch (e) {}
const el = (tag, cls, text) => { const e = document.createElement(tag); if (cls) e.className = cls; if (text != null) e.textContent = text; return e; };

function stepRow(label, items, key) {
  const row = el("div", "step");
  row.append(el("span", "step-label", label));
  items.forEach((name, i) => {
    const b = el("button", name === EXTRAS ? "choice extra" : "choice", name);
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
    const ic = el(DATA.icons[sk.id] ? "img" : "span", "icon");
    if (DATA.icons[sk.id]) { ic.src = "data:image/webp;base64," + DATA.icons[sk.id]; ic.alt = ""; }
    sum.append(el("span", "first", "Lv " + sk.lv[0]), ic, el("span", "name", sk.n), el("span", "kind k-" + sk.c, sk.c));
    const more = el("div", "more");
    if (sk.t) more.append(el("span", "tag", sk.t));
    // One row per rung with its own text. Rungs sharing a learn level (the swaps' five at 40) read by rank.
    const byRank = new Set(sk.r.map(x => x.l)).size < sk.r.length;
    sk.r.forEach(x => {
      const row = el("div", "rung");
      row.append(el("span", "at", byRank ? "Rank " + x.k : "Level " + x.l));
      const p = el("p");
      if (x.n) p.append(el("span", "rn", x.n + "  "));
      p.append(document.createTextNode(x.d || "-"));
      row.append(p);
      more.append(row);
    });
    d.append(sum, more);
    list.append(d);
  });
  s.append(list);
  return s;
}

function raceRow() { return stepRow("Race", DATA.races.map(r => r.name).concat(EXTRAS), "race"); }

function renderExtras() {
  document.getElementById("steps").replaceChildren(raceRow());
  document.getElementById("blurb").textContent =
    "In no path: both are bought on their own shelf, by every race.";
  document.getElementById("tiers").replaceChildren(
    tier("Stat swaps", "40+ · 3rd class", "Each rank moves one point from one stat to another: at most +5 on any " +
         "stat and 9 ranks in all, paid in gold, the price rising with the ranks you already own.", DATA.swaps),
    ...DATA.sigils.map(g => tier(g.name + " sigils", "76+ · ascended",
         "You take one Attack, one Defence and one Support sigil, from any flavour.", g.s)));
}

function render() {
  try { localStorage.setItem("skilltree", JSON.stringify(state)); } catch (e) {}
  footer();
  if (state.race === DATA.races.length) { renderExtras(); return; }
  const race = DATA.races[state.race] || DATA.races[0];
  const base = race.bases[state.base] || race.bases[0];
  const second = base.seconds[state.second] || base.seconds[0];
  const third = second.thirds[state.third] || second.thirds[0];

  const steps = document.getElementById("steps");
  steps.replaceChildren(
    raceRow(),
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
    tier("Every 4th class", "shared", "Learned by every class once ascended. The sigils are on the " + EXTRAS + " tab.",
         DATA.shared));
}

function footer() {
  document.getElementById("foot").textContent =
    "Generated from the game's own class tables and skill names, version " + DATA.version + ".";
}
render();
</script>
""";
}
