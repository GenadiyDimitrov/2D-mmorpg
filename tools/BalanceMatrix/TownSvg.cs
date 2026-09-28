using System.Globalization;
using System.Text;
using Game.Shared;

/// <summary>
/// `BL-319` — THE TOWNS AS BUILT, drawn from the live data: <see cref="TownLayout"/> (roads, plaza, paths,
/// footprints, gates), <see cref="WorldMap.Npcs"/> (who stands where) and the guard posts in
/// <see cref="WorldMap.SpawnZones"/>. The sketch in <c>docs/design/TownPlans.html</c> is what he approved; this
/// is what the code actually places, so the two can be compared on one screen.
///
/// <para><c>dotnet run --project tools/BalanceMatrix -- --town-svg out.html</c> writes one SVG per town.</para>
/// </summary>
static class TownSvg
{
    public static void Run(string[] args)
    {
        string outPath = args.Length > 1 ? args[1] : "towns.html";
        var ci = CultureInfo.InvariantCulture;
        string F(float v) => v.ToString("0", ci);
        var sb = new StringBuilder();
        sb.Append("<!doctype html><meta charset=utf-8><title>Towns as built</title><style>body{background:#1b2326;color:#e1e7e5;font:14px sans-serif;margin:16px}"
                + "svg{width:100%;max-width:900px;height:auto;display:block;margin-bottom:24px;background:#212b2e}</style>");

        foreach (var plan in TownLayout.Plans)
        {
            var town = Towns.ById(plan.TownId)!;
            float r = town.Radius + 400;
            sb.Append($"<h2>{town.Name} — {plan.Shape}, r {F(town.Radius)}</h2>");
            sb.Append($"<svg viewBox='{F(town.X - r)} {F(town.Y - r)} {F(2 * r)} {F(2 * r)}'>");

            // Wall octagon.
            var pts = Enumerable.Range(0, 8).Select(i =>
            {
                double a = Math.PI / 8 + i * Math.PI / 4;
                return $"{F(town.X + town.Radius * (float)Math.Cos(a))},{F(town.Y + town.Radius * (float)Math.Sin(a))}";
            });
            sb.Append($"<polygon points='{string.Join(" ", pts)}' fill='#1e2a24' stroke='#6b7b80' stroke-width='40'/>");

            foreach (var road in WorldMap.Roads)
                sb.Append($"<polyline points='{string.Join(" ", road.Points.Select(p => $"{F(p.X)},{F(p.Y)}"))}' fill='none' stroke='#4a4032' stroke-width='300'/>");
            foreach (var s in plan.Roads)
                sb.Append($"<line x1='{F(s.A.X)}' y1='{F(s.A.Y)}' x2='{F(s.B.X)}' y2='{F(s.B.Y)}' stroke='{(s.Path ? "#6a5a45" : "#8a7659")}' stroke-width='{F(s.Width)}'/>");
            sb.Append($"<circle cx='{F(plan.Centre.X)}' cy='{F(plan.Centre.Y)}' r='{F(plan.PlazaRadius)}' fill='#9a8666'/>");

            foreach (var lot in plan.Lots)
            {
                string col = lot.Kind switch
                {
                    LotKind.Shop => "#546b80", LotKind.Church => "#7a5270", LotKind.Craft => "#855e38",
                    LotKind.Yard => "#4f402b", LotKind.Guild => "#45735e", LotKind.Shrine => "#948c5c", _ => "#4d5459",
                };
                sb.Append($"<rect x='{F(lot.Centre.X - lot.W / 2)}' y='{F(lot.Centre.Y - lot.H / 2)}' width='{F(lot.W)}' height='{F(lot.H)}'"
                        + $" transform='rotate({F(lot.AngleDeg)} {F(lot.Centre.X)} {F(lot.Centre.Y)})' fill='{col}' stroke='#101416' stroke-width='18'/>");
                if (lot.Label.Length > 0)
                    sb.Append($"<text x='{F(lot.Centre.X)}' y='{F(lot.Centre.Y)}' font-size='90' fill='#fff' text-anchor='middle'>{lot.Label.Replace("&", "&amp;")}</text>");
            }

            foreach (var z in WorldMap.SpawnZones)
                if (Math.Abs(z.X - town.X) < r && Math.Abs(z.Y - town.Y) < r && z.MobTypes.Any(m => m.StartsWith("guard_town")))
                    sb.Append($"<circle cx='{F(z.X)}' cy='{F(z.Y)}' r='50' fill='#e0735a'/>");

            foreach (var n in WorldMap.Npcs)
                if (Math.Abs(n.X - town.X) < r && Math.Abs(n.Y - town.Y) < r)
                    sb.Append($"<circle cx='{F(n.X)}' cy='{F(n.Y)}' r='45' fill='#f0d060'/>"
                            + $"<text x='{F(n.X + 60)}' y='{F(n.Y + 25)}' font-size='70' fill='#f0d060'>{n.Name}</text>");
            sb.Append("</svg>");
        }

        File.WriteAllText(outPath, sb.ToString());
        Console.WriteLine($"Wrote {TownLayout.Plans.Count} town plans to {Path.GetFullPath(outPath)}");
    }
}
