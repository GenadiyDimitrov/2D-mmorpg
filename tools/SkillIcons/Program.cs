using System.Diagnostics;
using System.Text;
using Game.Shared;
using SkiaSharp;

// =====================================================================================================
//  SKILL ICONS — `BL-331` (owner, 2026-10-01: *"ok lets do the route B"*).
//
//  docs/data/skill_icons.csv is HIS, same two-way contract as the class CSVs: one row per skill,
//  `SKILL_ID,ICON,SCHOOL,COMMENT` (an ACTION is `action:<id>`, e.g. `action:sit_stand`; a buff-bar row no skill
//  casts is `buff:<key>`, e.g. `buff:paved_streets` — see Rows.BarRows). ICON is a game-icons.net name (the last part of the site's URL,
//  e.g. game-icons.net/1x1/lorc/fireball.html → `fireball`); SCHOOL picks the colour from the palette
//  below. Change a row, re-run, and the PNG follows.
//
//  What a run does:
//    1. clones the game-icons.net set into tools/SkillIcons/.game-icons/ on first use (gitignored);
//    2. checks every row (a known skill, an icon that exists, a school that exists) and lists every
//       class-CSV skill with no row — those draw as initials in the game until they get one;
//    3. renders one 128x128 PNG per row into Game.Client.Unity/Assets/Resources/SkillIcons/<id>.png, or
//       Resources/ActionIcons/<id>.png for an action,
//       deleting PNGs whose row is gone;
//    4. writes docs/design/SkillIcons.html — every icon grouped by class file, for review.
//
//  🔑 Every icon is ONE white path on a black square (checked across all 4,180 when this was built),
//  so no SVG library is needed: SkiaSharp parses the path data and the frame is ours.
//
//  Glyphs: game-icons.net, CC BY 3.0 — the credit is in docs/CREDITS.md and must stay there.
// =====================================================================================================

var dir = new DirectoryInfo(AppContext.BaseDirectory);
while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Game.sln"))) dir = dir.Parent;
if (dir is null) { Console.Error.WriteLine("Could not find the repo root (Game.sln)."); return 1; }
string repo = dir.FullName;

string setDir = Path.Combine(repo, "tools", "SkillIcons", ".game-icons");
if (!Directory.Exists(setDir))
{
    Console.WriteLine("Cloning the game-icons.net set (first run) …");
    if (Run("git", $"clone --depth 1 https://github.com/game-icons/icons \"{setDir}\"") != 0) return 1;
}
else if (args.Contains("--update")) Run("git", $"-C \"{setDir}\" pull --depth 1");

// name → file. A few names exist under two authors; the first author alphabetically wins, and a row may
// name `author/name` to pick the other one.
var icons = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (var f in Directory.EnumerateFiles(setDir, "*.svg", SearchOption.AllDirectories)
                           .Where(f => !f.Contains(Path.DirectorySeparatorChar + "badges" + Path.DirectorySeparatorChar))
                           .OrderBy(f => f, StringComparer.Ordinal))
{
    string name = Path.GetFileNameWithoutExtension(f);
    string author = Path.GetFileName(Path.GetDirectoryName(f)!);
    icons.TryAdd(name, f);
    icons.TryAdd(author + "/" + name, f);
}

// ---- the rows ------------------------------------------------------------------------------------------
string csvPath = Path.Combine(repo, "docs", "data", "skill_icons.csv");
var rows = new List<(string Id, string Icon, string School, int Line)>();
var errors = new List<string>();
{
    var lines = File.ReadAllLines(csvPath);
    for (int i = 1; i < lines.Length; i++)
    {
        var c = Csv.Split(lines[i]);
        if (c.Count == 0 || string.IsNullOrWhiteSpace(c[0])) continue;
        string id = c[0].Trim(), icon = c.Count > 1 ? c[1].Trim() : "", school = c.Count > 2 ? c[2].Trim() : "";
        if (rows.Any(r => r.Id == id)) errors.Add($"line {i + 1}: {id} has two rows");
        if (Rows.IsBuff(id))
        {
            if (!Rows.BarRows.ContainsKey(Rows.BuffKey(id)))
                errors.Add($"line {i + 1}: {id} is not a known buff-bar row (known: {string.Join(", ", Rows.BarRows.Keys)})");
        }
        else if (Rows.IsAction(id) ? ActionCatalog.Get(Rows.ActionId(id)) is null : SkillCatalog.Get(id) is null)
            errors.Add($"line {i + 1}: {id} is not a " + (Rows.IsAction(id) ? "known action" : "skill"));
        if (!icons.ContainsKey(icon)) errors.Add($"line {i + 1}: {id} — no icon called '{icon}' on game-icons.net");
        if (!Palette.Schools.ContainsKey(school))
            errors.Add($"line {i + 1}: {id} — unknown SCHOOL '{school}' (known: {string.Join(", ", Palette.Schools.Keys)})");
        rows.Add((id, icon, school, i + 1));
    }
}

// Which class file each skill comes from — for the coverage list and the review page's grouping.
string classDir = Path.Combine(repo, "docs", "data", "classes_skills_csv");
var fileOf = new Dictionary<string, string>();
var fileOrder = new List<string>();
foreach (var file in Directory.GetFiles(classDir, "*.csv").OrderBy(f => Order(Path.GetFileNameWithoutExtension(f))))
{
    string label = Path.GetFileNameWithoutExtension(file);
    fileOrder.Add(label);
    var lines = File.ReadAllLines(file);
    var head = Csv.Split(lines[0]).Select(h => h.Trim()).ToList();
    int idCol = head.IndexOf("SKILL_ID");
    if (idCol < 0) continue;
    foreach (var l in lines.Skip(1))
    {
        var c = Csv.Split(l);
        if (c.Count <= idCol) continue;
        string id = c[idCol].Trim();
        if (id.Length > 0 && id.All(ch => ch is >= 'a' and <= 'z' or >= '0' and <= '9' or '_')) fileOf.TryAdd(id, label);
    }
}
var missing = fileOf.Keys.Where(id => rows.All(r => r.Id != id))
    .Concat(ActionCatalog.All.Select(a => Rows.ActionPrefix + a.Id).Where(id => rows.All(r => r.Id != id)))
    .OrderBy(x => x).ToList();

foreach (var e in errors) Console.WriteLine("🔴 " + e);
if (errors.Count > 0) { Console.WriteLine($"{errors.Count} error(s) — nothing written."); return 1; }

// ---- render ----------------------------------------------------------------------------------------------
// Skills → Resources/SkillIcons/<skill id>.png; actions (`action:<id>` rows) → Resources/ActionIcons/<id>.png.
string resources = Path.Combine(repo, "Game.Client.Unity", "Assets", "Resources");
string skillDir = Path.Combine(resources, "SkillIcons"), actionDir = Path.Combine(resources, "ActionIcons");
Directory.CreateDirectory(skillDir);
Directory.CreateDirectory(actionDir);
var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var pngs = new Dictionary<string, byte[]>();
foreach (var r in rows)
{
    bool isAction = Rows.IsAction(r.Id), isBuff = Rows.IsBuff(r.Id);
    bool passive = !isAction && !isBuff && SkillCatalog.Get(r.Id)!.Category == SkillCategory.Passive;
    byte[] png = Render.Icon(Svg.PathData(icons[r.Icon]), Palette.Schools[r.School], passive, 128);
    // A `buff:` row lands beside the skills, under its bar key — the server sends that key as the buff's icon id.
    string path = isAction ? Path.Combine(actionDir, Rows.ActionId(r.Id) + ".png")
                : Path.Combine(skillDir, (isBuff ? Rows.BuffKey(r.Id) : r.Id) + ".png");
    // Only rewrite a file whose bytes changed, so git and Unity see real changes only.
    if (!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(png)) File.WriteAllBytes(path, png);
    keep.Add(path);
    pngs[r.Id] = Render.Thumb(png, 64);   // the review page embeds small WebP copies, not the 128px PNGs
}
int removed = 0;
foreach (var f in Directory.GetFiles(skillDir, "*.png").Concat(Directory.GetFiles(actionDir, "*.png")))
{
    if (keep.Contains(f)) continue;
    File.Delete(f);
    if (File.Exists(f + ".meta")) File.Delete(f + ".meta");
    removed++;
}

// ---- the review page --------------------------------------------------------------------------------------
string page = Path.Combine(repo, "docs", "design", "SkillIcons.html");
File.WriteAllText(page, Gallery.Html(rows.Select(r => (r.Id, r.Icon, r.School)).ToList(), fileOf, fileOrder, pngs, missing),
                  new UTF8Encoding(false));

Console.WriteLine($"{rows.Count} icons rendered → {Path.GetRelativePath(repo, resources)}" + (removed > 0 ? $" ({removed} stale removed)" : ""));
Console.WriteLine($"Review page → {Path.GetRelativePath(repo, page)}");
if (missing.Count > 0)
    Console.WriteLine($"⚪ {missing.Count} skill(s)/action(s) with no icon row (they show initials): {string.Join(", ", missing)}");
return 0;

static int Order(string file)
{
    // 1st → 2nd → 3rd → 4th, then the rest; a stable, readable order for the review page.
    if (file.Contains("1st")) return 0;
    if (file.Contains("2nd")) return 1;
    if (file.Contains("3rd")) return 2;
    if (file.Contains("4th")) return 3;
    return 4;
}

static int Run(string exe, string arguments)
{
    using var p = Process.Start(new ProcessStartInfo(exe, arguments) { UseShellExecute = false })!;
    p.WaitForExit();
    return p.ExitCode;
}

/// <summary>A school's two colours: the frame (BASE) and the glyph's lower tint (LIGHT).</summary>
internal sealed record School(string Label, SKColor Base, SKColor Light);

internal static class Palette
{
    /// <summary>The colour of a skill = what KIND of thing it is, so a whole kit reads at a glance. Racial
    /// variants of one skill share a glyph and differ by school (elf nature, demon blood, human arcane/steel);
    /// a harmony wears its single's glyph in `sound`, a whisp its effect's glyph in `whisp`.</summary>
    public static readonly Dictionary<string, School> Schools = new()
    {
        ["steel"]   = new("Physical / weapon",       new SKColor(0x6f, 0x7f, 0x92), new SKColor(0xe3, 0xeb, 0xf5)),
        ["earth"]   = new("Defence / tank",          new SKColor(0x8a, 0x6a, 0x3a), new SKColor(0xf3, 0xe2, 0xc4)),
        ["wind"]    = new("Speed / evasion",         new SKColor(0x2f, 0xa0, 0x8a), new SKColor(0xe0, 0xff, 0xf6)),
        ["blood"]   = new("Blood / crit / demon",    new SKColor(0x9c, 0x1f, 0x2f), new SKColor(0xff, 0xc8, 0xc8)),
        ["fire"]    = new("Fire",                    new SKColor(0xc4, 0x47, 0x1f), new SKColor(0xff, 0xd9, 0xa0)),
        ["frost"]   = new("Frost",                   new SKColor(0x3a, 0x8f, 0xc4), new SKColor(0xd8, 0xf3, 0xff)),
        ["storm"]   = new("Lightning / shock",       new SKColor(0x2f, 0x7f, 0xa8), new SKColor(0xff, 0xf5, 0x9a)),
        ["arcane"]  = new("Arcane / magic",          new SKColor(0x7a, 0x4c, 0xc4), new SKColor(0xe9, 0xd9, 0xff)),
        ["mind"]    = new("Mana / spirit",           new SKColor(0x2f, 0x5f, 0xb8), new SKColor(0xcf, 0xe2, 0xff)),
        ["holy"]    = new("Holy / healing",          new SKColor(0xc4, 0x9a, 0x22), new SKColor(0xff, 0xf3, 0xc4)),
        ["life"]    = new("Life / regeneration",     new SKColor(0x3f, 0x9c, 0x5a), new SKColor(0xdc, 0xff, 0xd9)),
        ["nature"]  = new("Nature / elf",            new SKColor(0x4f, 0x8a, 0x2a), new SKColor(0xe6, 0xff, 0xc4)),
        ["poison"]  = new("Poison",                  new SKColor(0x6a, 0x9c, 0x1f), new SKColor(0xea, 0xff, 0xb0)),
        ["shadow"]  = new("Shadow / curse / stealth", new SKColor(0x4a, 0x3a, 0x66), new SKColor(0xd6, 0xc8, 0xf0)),
        ["sound"]   = new("Song / harmony",          new SKColor(0xb8, 0x40, 0x8a), new SKColor(0xff, 0xd6, 0xf0)),
        ["whisp"]   = new("Whisp",                   new SKColor(0x3f, 0xaa, 0xb8), new SKColor(0xe0, 0xfb, 0xff)),
        ["sigil"]   = new("Sigil",                   new SKColor(0xa8, 0x74, 0x2a), new SKColor(0xff, 0xe3, 0xb0)),
        ["neutral"] = new("Neutral",                 new SKColor(0x6a, 0x6a, 0x6a), new SKColor(0xee, 0xee, 0xee)),
        ["social"]  = new("Social / party / chat",   new SKColor(0x4a, 0x6f, 0xa5), new SKColor(0xdd, 0xe8, 0xf7)),
        // An ITEM's buff (a potion, a scroll) wears its item's RARITY — the hues of GameUi.RarityColour, darkened
        // for the frame, so the bottle on the bar reads the same colour as its name in the bag.
        ["common"]    = new("Item: Common",    new SKColor(0x7a, 0x7a, 0x7a), new SKColor(0xf2, 0xf2, 0xf2)),
        ["uncommon"]  = new("Item: Uncommon",  new SKColor(0x3a, 0x7c, 0xb8), new SKColor(0xd9, 0xef, 0xff)),
        ["rare"]      = new("Item: Rare",      new SKColor(0xb0, 0x8a, 0x10), new SKColor(0xff, 0xf0, 0xb0)),
        ["epic"]      = new("Item: Epic",      new SKColor(0x7a, 0x44, 0xc0), new SKColor(0xec, 0xdc, 0xff)),
        ["legendary"] = new("Item: Legendary", new SKColor(0xc0, 0x5a, 0x10), new SKColor(0xff, 0xdc, 0xb8)),
        ["mythic"]    = new("Item: Mythic",    new SKColor(0xb0, 0x26, 0x30), new SKColor(0xff, 0xc8, 0xcc)),
    };
}

/// <summary>A row's SKILL_ID is a skill id, or `action:<id>` for one of the built-in actions (ActionCatalog) —
/// the same spelling the skill bar stores, so the Actions tab and the bar read one row.</summary>
internal static class Rows
{
    public const string ActionPrefix = "action:";
    public static bool IsAction(string id) => id.StartsWith(ActionPrefix, StringComparison.Ordinal);
    public static string ActionId(string id) => id.Substring(ActionPrefix.Length);

    /// <summary>`buff:<key>` — a buff-bar row that no skill casts (the server builds it from a state: standing on the
    /// paving, wearing over-grade gear). Its key is what <c>BuffDto.IconSkillId</c> carries, so it must never collide
    /// with a skill id. Name → shown on the review page.</summary>
    public const string BuffPrefix = "buff:";
    public static bool IsBuff(string id) => id.StartsWith(BuffPrefix, StringComparison.Ordinal);
    public static string BuffKey(string id) => id.Substring(BuffPrefix.Length);
    public static readonly Dictionary<string, string> BarRows = new()
    {
        ["paved_streets"]        = "Paved Streets",
        ["grade_penalty_armor"]  = "Over-Grade Armor",
        ["grade_penalty_weapon"] = "Over-Grade Weapon",
        ["mentor_aura"]          = "Mentor Aura",          // `BL-339`
        ["mentor_guidance"]      = "Mentor's Guidance",
    };

    /// <summary>The review page's section for a row that no class file lists.</summary>
    public static string Group(string id) =>
        IsAction(id) ? "actions"
        : IsBuff(id) ? "buff bar"
        : id.StartsWith("npc_", StringComparison.Ordinal) ? "spirit helper (NPC buffs)"
        : id.StartsWith("pot_", StringComparison.Ordinal) ? "potions"
        : id.StartsWith("scr_", StringComparison.Ordinal) ? "scrolls"
        : id.StartsWith("rune_", StringComparison.Ordinal) ? "runes"
        : "(no class file)";
    public static readonly string[] Groups =
        { "actions", "spirit helper (NPC buffs)", "potions", "scrolls", "runes", "buff bar", "(no class file)" };
}

internal static class Svg
{
    /// <summary>Every game-icons.net file is `&lt;path d="M0 0h512v512H0z"/&gt;` (the black square) followed by
    /// ONE `&lt;path fill="#fff" d="…"/&gt;` — the glyph. Return the glyph's path data.</summary>
    public static string PathData(string file)
    {
        string text = File.ReadAllText(file);
        int at = text.IndexOf("fill=\"#fff\"", StringComparison.Ordinal);
        if (at < 0) throw new InvalidDataException(file + ": no white path");
        int d = text.IndexOf(" d=\"", at, StringComparison.Ordinal) + 4;
        return text.Substring(d, text.IndexOf('"', d) - d);
    }
}

internal static class Render
{
    /// <summary>One icon: a rounded square in the school's colour (darker toward the bottom, a soft glow behind
    /// the glyph), the glyph white fading to the school's light tint, a drop shadow, and a two-tone rim. A PASSIVE
    /// is the same picture dimmed — darker frame, no white in the glyph — so on/off-bar skills read differently.</summary>
    public static byte[] Icon(string pathData, School s, bool passive, int size)
    {
        float S = size;
        var info = new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var c = surface.Canvas;
        c.Clear(SKColors.Transparent);

        float dim = passive ? 0.62f : 1f;
        var top = Mul(s.Base, 0.95f * dim);
        var bottom = Mul(s.Base, 0.32f * dim);
        var rect = new SKRect(1.5f, 1.5f, S - 1.5f, S - 1.5f);
        float radius = S * 0.13f;
        var rrect = new SKRoundRect(rect, radius, radius);

        using (var bg = new SKPaint { IsAntialias = true })
        {
            bg.Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, S),
                new[] { top, bottom }, SKShaderTileMode.Clamp);
            c.DrawRoundRect(rrect, bg);
        }

        c.Save();
        c.ClipRoundRect(rrect, SKClipOperation.Intersect, true);
        using (var glow = new SKPaint { IsAntialias = true })
        {
            glow.Shader = SKShader.CreateRadialGradient(new SKPoint(S / 2, S * 0.45f), S * 0.48f,
                new[] { s.Light.WithAlpha((byte)(passive ? 45 : 85)), s.Light.WithAlpha(0) }, SKShaderTileMode.Clamp);
            c.DrawRect(rect, glow);
        }
        c.Restore();

        using var path = SKPath.ParseSvgPathData(pathData);
        float inner = S * 0.72f, off = (S - inner) / 2f;
        path.Transform(SKMatrix.CreateScaleTranslation(inner / 512f, inner / 512f, off, off));

        using (var shadow = new SKPaint { IsAntialias = true, Color = new SKColor(0, 0, 0, 170) })
        {
            shadow.ImageFilter = SKImageFilter.CreateBlur(S * 0.018f, S * 0.018f);
            c.Save();
            c.Translate(S * 0.012f, S * 0.022f);
            c.DrawPath(path, shadow);
            c.Restore();
        }

        using (var fg = new SKPaint { IsAntialias = true })
        {
            var hi = passive ? Mix(s.Light, s.Base, 0.15f) : SKColors.White;
            var lo = passive ? Mix(s.Light, s.Base, 0.45f) : s.Light;
            fg.Shader = SKShader.CreateLinearGradient(new SKPoint(0, off), new SKPoint(0, off + inner),
                new[] { hi, lo }, SKShaderTileMode.Clamp);
            c.DrawPath(path, fg);
        }

        using (var rim = new SKPaint { IsAntialias = true, IsStroke = true, StrokeWidth = S * 0.025f })
        {
            rim.Color = Mix(s.Light, s.Base, 0.35f).WithAlpha((byte)(passive ? 110 : 190));
            var inset = new SKRoundRect(SKRect.Inflate(rect, -S * 0.02f, -S * 0.02f), radius * 0.85f, radius * 0.85f);
            c.DrawRoundRect(inset, rim);
            rim.Color = new SKColor(0, 0, 0, 200);
            rim.StrokeWidth = S * 0.014f;
            c.DrawRoundRect(rrect, rim);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>A small WebP copy for the review page — the full PNGs would make it ~7 MB.</summary>
    public static byte[] Thumb(byte[] png, int size)
    {
        using var src = SKBitmap.Decode(png);
        using var small = src.Resize(new SKImageInfo(size, size), new SKSamplingOptions(SKCubicResampler.Mitchell));
        using var img = SKImage.FromBitmap(small);
        using var data = img.Encode(SKEncodedImageFormat.Webp, 88);
        return data.ToArray();
    }

    private static SKColor Mul(SKColor c, float k) =>
        new((byte)Math.Clamp(c.Red * k, 0, 255), (byte)Math.Clamp(c.Green * k, 0, 255), (byte)Math.Clamp(c.Blue * k, 0, 255));

    private static SKColor Mix(SKColor a, SKColor b, float t) =>
        new((byte)(a.Red + (b.Red - a.Red) * t), (byte)(a.Green + (b.Green - a.Green) * t), (byte)(a.Blue + (b.Blue - a.Blue) * t));
}

internal static class Csv
{
    /// <summary>One CSV line → cells; quotes and doubled quotes honoured (his comments carry commas).</summary>
    public static List<string> Split(string line)
    {
        var cells = new List<string>();
        var cur = new StringBuilder();
        bool q = false;
        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (q)
            {
                if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                else if (ch == '"') q = false;
                else cur.Append(ch);
            }
            else if (ch == '"') q = true;
            else if (ch == ',') { cells.Add(cur.ToString()); cur.Clear(); }
            else cur.Append(ch);
        }
        cells.Add(cur.ToString());
        return cells;
    }
}

internal static class Gallery
{
    public static string Html(List<(string Id, string Icon, string School)> rows, Dictionary<string, string> fileOf,
                              List<string> fileOrder, Dictionary<string, byte[]> pngs, List<string> missing)
    {
        static string E(string s) => System.Net.WebUtility.HtmlEncode(s);
        var sb = new StringBuilder();
        sb.Append("""
<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Skill Icons</title>
<style>
:root{--bg:#f6f5f2;--panel:#fff;--text:#1d1d1f;--dim:#6b6b70;--line:#e3e1dc;--chip:#efede8}
@media (prefers-color-scheme:dark){:root:not([data-theme="light"]){--bg:#15161a;--panel:#1e2026;--text:#ecebe8;--dim:#9a9aa2;--line:#2c2f37;--chip:#262932}}
:root[data-theme="dark"]{--bg:#15161a;--panel:#1e2026;--text:#ecebe8;--dim:#9a9aa2;--line:#2c2f37;--chip:#262932}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:14px/1.45 system-ui,-apple-system,Segoe UI,sans-serif}
main{max-width:1200px;margin:0 auto;padding:24px 16px 64px}
h1{font-size:22px;margin:0 0 4px}h2{font-size:15px;margin:28px 0 10px;text-transform:capitalize;color:var(--dim);font-weight:600;letter-spacing:.02em}
p.lede{color:var(--dim);margin:0 0 16px;max-width:760px}
.bar{display:flex;gap:10px;flex-wrap:wrap;align-items:center;margin:12px 0 4px}
input{font:inherit;padding:7px 10px;border:1px solid var(--line);border-radius:8px;background:var(--panel);color:var(--text);min-width:240px}
.legend{display:flex;flex-wrap:wrap;gap:6px;margin:10px 0 0}.legend span{background:var(--chip);border-radius:999px;padding:3px 10px 3px 6px;font-size:12px;display:inline-flex;align-items:center;gap:6px}
.legend i{width:12px;height:12px;border-radius:3px;display:inline-block}
.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(150px,1fr));gap:10px}
.card{background:var(--panel);border:1px solid var(--line);border-radius:10px;padding:10px;display:flex;gap:10px;align-items:center;min-width:0}
.card img{width:56px;height:56px;flex:none}
.card div{min-width:0}.n{font-weight:600;font-size:13px;overflow-wrap:anywhere}.m{color:var(--dim);font-size:11px;overflow-wrap:anywhere}
.miss{color:var(--dim)}footer{margin-top:40px;color:var(--dim);font-size:12px}
a{color:inherit}
</style></head><body><main>
<h1>Skill Icons</h1>
<p class="lede">Every player skill's icon, as the game draws it. A picture you want changed: edit that skill's row in
<code>docs/data/skill_icons.csv</code> — <b>ICON</b> is any name from game-icons.net (the end of the icon's address), <b>SCHOOL</b> one of the
colours below — or just say which one. Passives are drawn dimmer than skills you press.</p>
<div class="bar"><input id="q" type="search" placeholder="Filter by name, id, icon or school…"></div>
<div class="legend">
""");
        foreach (var (key, s) in Palette.Schools)
            sb.Append($"<span><i style=\"background:#{s.Base.Red:x2}{s.Base.Green:x2}{s.Base.Blue:x2}\"></i>{E(key)} — {E(s.Label)}</span>");
        sb.Append("</div>\n");

        foreach (var file in fileOrder.Concat(Rows.Groups))
        {
            var mine = rows.Where(r => (fileOf.TryGetValue(r.Id, out var f) ? f : Rows.Group(r.Id)) == file).ToList();
            if (mine.Count == 0) continue;
            sb.Append($"<section><h2>{E(file)} · {mine.Count}</h2><div class=\"grid\">\n");
            foreach (var r in mine)
            {
                string name = Rows.IsAction(r.Id) ? ActionCatalog.Get(Rows.ActionId(r.Id))!.Name
                    : Rows.IsBuff(r.Id) ? Rows.BarRows[Rows.BuffKey(r.Id)]
                    : SkillFaces.NameOf(SkillFaces.Get(r.Id), r.Id, 1);
                string b64 = Convert.ToBase64String(pngs[r.Id]);
                string hay = (name + " " + r.Id + " " + r.Icon + " " + r.School).ToLowerInvariant();
                sb.Append($"<div class=\"card\" data-k=\"{E(hay)}\"><img alt=\"\" src=\"data:image/webp;base64,{b64}\">"
                        + $"<div><div class=\"n\">{E(name)}</div><div class=\"m\">{E(r.Id)}</div>"
                        + $"<div class=\"m\">{E(r.Icon)} · {E(r.School)}</div></div></div>\n");
            }
            sb.Append("</div></section>\n");
        }

        if (missing.Count > 0)
            sb.Append($"<h2>No icon yet · {missing.Count}</h2><p class=\"miss\">{E(string.Join(", ", missing))}</p>\n");

        sb.Append("""
<footer>Icons by Lorc, Delapouite and the other contributors of <a href="https://game-icons.net">game-icons.net</a>,
licensed <a href="https://creativecommons.org/licenses/by/3.0/">CC BY 3.0</a>. Generated by <code>tools/SkillIcons</code> — do not edit.</footer>
</main>
<script>
const q=document.getElementById('q');q.addEventListener('input',()=>{const v=q.value.trim().toLowerCase();
document.querySelectorAll('.card').forEach(c=>c.style.display=!v||c.dataset.k.includes(v)?'':'none');
document.querySelectorAll('section').forEach(s=>s.style.display=[...s.querySelectorAll('.card')].some(c=>c.style.display!=='none')?'':'none');});
</script>
</body></html>
""");
        return sb.ToString();
    }
}
