using System.Text;
using Game.Shared;
using SkiaSharp;

/// <summary>
/// `BL-347` (0.269.0) — ITEM icons: `dotnet run --project tools/SkillIcons -- --items`.
///
/// One picture per KIND of item (<see cref="ItemCatalog.IconKey"/>), never per item — his *"can reuse same icon for weight
/// and armor part and weapon part .. just different colors"*. Each kind is a row of docs/data/item_icons.csv
/// (`KIND,ICON,COMMENT`, ICON a game-icons.net name as in skill_icons.csv), rendered GREY-AND-WHITE into
/// Resources/ItemIcons/&lt;kind&gt;.png: the client multiplies it by the item's rarity colour, so the frame comes out a dark
/// shade of that colour and the glyph the colour itself. Glyphs: game-icons.net, CC BY 3.0 (docs/CREDITS.md).
/// </summary>
internal static class ItemIcons
{
    /// <summary>Neutral so a multiply tints it cleanly: a mid-grey frame, a white glyph.</summary>
    private static readonly School Tintable = new("Item (tinted by rarity)", new SKColor(0x90, 0x90, 0x90), SKColors.White);

    public static int Run(string repo, Dictionary<string, string> icons)
    {
        string csvPath = Path.Combine(repo, "docs", "data", "item_icons.csv");
        var rows = new List<(string Kind, string Icon)>();
        var errors = new List<string>();
        var lines = File.ReadAllLines(csvPath);
        for (int i = 1; i < lines.Length; i++)
        {
            var c = Csv.Split(lines[i]);
            if (c.Count == 0 || string.IsNullOrWhiteSpace(c[0])) continue;
            string kind = c[0].Trim(), icon = c.Count > 1 ? c[1].Trim() : "";
            if (rows.Any(r => r.Kind == kind)) errors.Add($"line {i + 1}: {kind} has two rows");
            if (!ItemCatalog.IconKeys.Contains(kind)) errors.Add($"line {i + 1}: {kind} is not a kind ItemCatalog.IconKey returns");
            if (!icons.ContainsKey(icon)) errors.Add($"line {i + 1}: {kind} — no icon called '{icon}' on game-icons.net");
            rows.Add((kind, icon));
        }
        foreach (var k in ItemCatalog.IconKeys.Where(k => rows.All(r => r.Kind != k)))
            errors.Add($"{k} has no row (every kind ItemCatalog.IconKey returns needs one)");
        // Every item in the catalog must land on a listed kind — IconKeys and IconKey must agree.
        foreach (var def in ItemCatalog.AllItems)
            if (!ItemCatalog.IconKeys.Contains(ItemCatalog.IconKey(def)))
                errors.Add($"{def.Id}: IconKey '{ItemCatalog.IconKey(def)}' is missing from ItemCatalog.IconKeys");

        foreach (var e in errors.Distinct()) Console.WriteLine("🔴 " + e);
        if (errors.Count > 0) { Console.WriteLine($"{errors.Count} error(s) — nothing written."); return 1; }

        string dir = Path.Combine(repo, "Game.Client.Unity", "Assets", "Resources", "ItemIcons");
        Directory.CreateDirectory(dir);
        var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
        {
            byte[] png = Render.Icon(Svg.PathData(icons[r.Icon]), Tintable, false, 128);
            string path = Path.Combine(dir, r.Kind + ".png");
            if (!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(png)) File.WriteAllBytes(path, png);
            keep.Add(path);
        }
        foreach (var f in Directory.GetFiles(dir, "*.png").Where(f => !keep.Contains(f)))
        {
            File.Delete(f);
            if (File.Exists(f + ".meta")) File.Delete(f + ".meta");
        }
        Console.WriteLine($"{rows.Count} item icons rendered → {Path.GetRelativePath(repo, dir)}");
        return 0;
    }
}
