using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Shared;

/// <summary>`BL-147` — THE CONSUMABLE-BUFF INVENTORY, GENERATED.
///
/// <para>His ask, 2026-09-03: *"can u show me what buffs we have as scrolls and what on potions (which
/// are bought which are crafted and which are same as npc buffer) and which we dont have that are
/// single buffs"*. Five questions, and the last one — the families with NO consumable at all — is the
/// one a hand-typed table can never keep true, because it is defined by an ABSENCE: the day someone
/// adds a Vampirism potion, a typed page still lists Vampirism as having none.</para>
///
/// <para>🔑 So it is READ OUT OF THE CATALOGS, never written down. Every column here is a query:
/// the item's own <c>UseSkillId</c> for what a bottle does, <c>ConsumableBuffForm</c> for potion vs
/// scroll (the DURATION is the only thing that ever told them apart), <c>ShopCatalog</c> /
/// <c>RecipeCatalog</c> / <c>BoxCatalog</c> / the mob drop tables for where it comes from, and
/// <c>NewbieBuffSet</c> for what the buffer NPC gives. Same rule as the mob CSV dump: regenerate it,
/// never edit it.</para>
///
/// <para>⚠ The "slot" column is the SERVER's cap predicate re-stated (<c>CountsAgainstBuffCap</c>),
/// which is why it can disagree with the row a buff draws in — and `BL-145` is exactly what happens
/// when nobody can see that it does.</para></summary>
internal static class ConsumableBuffPage
{
    public static void Run(string[] args)
    {
        string path = args.Length > 0 ? args[0] : "docs/data/BuffConsumables.md";
        var sb = new StringBuilder();

        // ---- Index every buff FAMILY the game has: a family is a BuffKey carried by singles. ----
        //      A "single" is a rung: Category Buff, a BuffKey, and no children of its own (a wrapper
        //      has children and borrows its child's key only for display).
        var rungs = SkillCatalog.AllSkills
            .Where(d => d.Category == SkillCategory.Buff
                        && !string.IsNullOrEmpty(d.BuffKey)
                        && (d.ChildBuffs is null || d.ChildBuffs.Length == 0))
            .GroupBy(d => d.BuffKey)
            .ToDictionary(g => g.Key, g => g.OrderBy(d => d.Rank).ToArray());

        // ---- Every WRAPPER: a skill that hands out exactly one rung. Potions, scrolls, the cleric's
        //      singles and the NPC buffer's blessings are all the same shape. ----
        var wrappers = SkillCatalog.AllSkills
            .Where(d => d.ChildBuffs is { Length: 1 } && SkillCatalog.Get(d.ChildBuffs[0]) is not null)
            .ToArray();

        // ---- The ITEMS that grant a buff. This is the whole consumable-buff shelf.
        //      ⚠ TWO SHAPES, and taking only the first is how a generated page lies: a Might Potion is a
        //      one-child WRAPPER (it hands out the family's rung), while a healing potion IS the buff —
        //      its own key, its own duration, no children. Filtering on `ChildBuffs.Length == 1` alone
        //      dropped every potion of the second shape out of section 1 and then listed its family in
        //      section 2 as "has no consumable", which is the exact opposite of the truth. ----
        var items = ItemCatalog.AllItems
            .Where(i => !string.IsNullOrEmpty(i.UseSkillId))
            .Select(i => (Item: i, Wrapper: SkillCatalog.Get(i.UseSkillId!)))
            .Where(p => p.Wrapper is { Category: SkillCategory.Buff })
            .Select(p => (p.Item, Wrapper: p.Wrapper!,
                          Child: p.Wrapper!.ChildBuffs is { Length: 1 } kids
                                 ? SkillCatalog.Get(kids[0]) ?? p.Wrapper!
                                 : p.Wrapper!))
            .Where(p => !string.IsNullOrEmpty(p.Child.BuffKey))
            .ToArray();

        // A LADDER RUNG is a pure CHILD — no duration, no MP, nothing casts it on its own. That is
        // exactly the set his question is about ("which we dont have that are single buffs"): the
        // things a potion, a scroll or a blessing hands out. A class self-buff carries its own clock
        // and is a different animal, so the two are reported in separate tables rather than mixed.
        // ⚠ A TOGGLE is NOT a rung even though it looks like one on these three fields: a stance has no
        // duration by definition (it runs until you switch it off), so testing the clock alone filed
        // Holy Soul as an unbuyable ladder family that nothing in the game grants.
        static bool IsLadderRung(SkillDef d) =>
            !d.Toggle && d.DurationTicks == 0 && d.MpCost == 0 && d.CastTicks == 0;

        // ---- What the NPC BUFFER gives, as a set of FAMILIES. A blessing may be a one-child wrapper
        //      or a multi-child group, so both shapes are unrolled to the families they cover. ----
        var npcFamilies = new HashSet<string>();
        foreach (var id in SkillCatalog.NewbieBuffSet)
        {
            if (SkillCatalog.Get(id) is not SkillDef def || def.ChildBuffs is null) continue;
            foreach (var kid in def.ChildBuffs)
                if (SkillCatalog.Get(kid) is { } c && !string.IsNullOrEmpty(c.BuffKey))
                    npcFamilies.Add(c.BuffKey);
        }

        sb.AppendLine("# Consumable buffs — what exists as a potion, what as a scroll, and what has neither");
        sb.AppendLine();
        sb.AppendLine("> 🤖 **GENERATED — do not edit by hand.** Every number and every yes/no on this page is a");
        sb.AppendLine("> query against `ItemCatalog` + `SkillCatalog` + `ShopCatalog` + `RecipeCatalog` + `BoxCatalog` +");
        sb.AppendLine("> the mob drop tables, so it cannot go stale the way a typed table would. Regenerate with:");
        sb.AppendLine(">");
        sb.AppendLine("> ```");
        sb.AppendLine("> dotnet run --project tools/BalanceMatrix -- --buff-consumables");
        sb.AppendLine("> ```");
        sb.AppendLine();
        sb.AppendLine("`BL-147`. **Potion vs scroll is a DURATION, not a different buff** — a Might Potion and a");
        sb.AppendLine("Scroll of Might hand out the *same* rung of the *same* family; the potion runs 20 minutes and");
        sb.AppendLine("the scroll an hour, which is why drinking a potion over an equal-rung scroll is refused");
        sb.AppendLine("rather than wasted. **Slot** = does it occupy one of the "
                      + GameConstants.MaxBuffSlots + " buff squares (the server's own");
        sb.AppendLine("`CountsAgainstBuffCap`), which is a different question from which BAR it draws in.");
        sb.AppendLine();

        // ============================ 1. FAMILIES WITH A CONSUMABLE ============================
        var byFamily = items.GroupBy(p => p.Child.BuffKey)
                            .ToDictionary(g => g.Key, g => g.ToArray());

        sb.AppendLine("## 1. Buff families you can BUY, CRAFT or LOOT");
        sb.AppendLine();
        sb.AppendLine("| Buff | Family | Potion | Scroll | Where it comes from | Same as the NPC buffer? | Slot |");
        sb.AppendLine("|---|---|---|---|---|---|---|");

        foreach (var fam in byFamily.Keys.OrderBy(FamilyName).ThenBy(k => k))
        {
            var group = byFamily[fam];
            string pots = RungList(group.Where(p => Form(p.Wrapper) == BuffForm.Potion));
            string scrs = RungList(group.Where(p => Form(p.Wrapper) == BuffForm.Scroll));
            string bursts = RungList(group.Where(p => Form(p.Wrapper) == BuffForm.None));
            // A BURST (Dash, a healing potion) is neither a potion-buff nor a scroll on the duration
            // test that separates the two — it is short by design. It still belongs in the potion
            // column, labelled, rather than being dropped or padded onto an em-dash.
            if (bursts.Length > 0) pots = (pots.Length == 0 ? "" : pots + " · ") + $"burst: {bursts}";
            if (pots.Length == 0) pots = "—";
            if (scrs.Length == 0) scrs = "—";

            var sources = group.SelectMany(p => Sources(p.Item.Id)).Distinct().OrderBy(s => s).ToArray();
            string where = sources.Length == 0 ? "**nothing grants it**" : string.Join(", ", sources);

            // The NPC buffer's answer needs the RUNG, not just the family: "yes, and stronger" is a
            // different piece of advice from "yes, identical" when deciding whether to spend a scroll.
            string npc = "no";
            if (npcFamilies.Contains(fam))
            {
                int npcRank = NpcRank(fam, npcFamilies);
                int best = group.Max(p => p.Child.Rank);
                npc = npcRank > best ? $"**yes — stronger** (rung {npcRank} vs {best})"
                    : npcRank == best ? "yes — identical rung"
                                      : $"yes — weaker (rung {npcRank} vs {best})";
            }

            sb.AppendLine($"| **{FamilyName(fam)}** | `{fam}` | {pots} | {scrs} | {where} | {npc} | "
                          + (group.Any(p => Slot(p.Wrapper, p.Child)) ? "**yes**" : "no") + " |");
        }
        sb.AppendLine();

        // ==================== 2. THE LIST HE ASKED FOR: NO CONSUMABLE AT ALL ====================
        // Split in two, because "single buff" means one specific thing here and lumping the class
        // self-buffs in with it would bury the answer in forty rows of things that were never meant to
        // come in a bottle.
        var uncovered = rungs.Keys.Where(f => !byFamily.ContainsKey(f))
                                  .OrderBy(FamilyName).ThenBy(f => f).ToArray();

        sb.AppendLine("## 2. SINGLE BUFFS with no consumable — the ladder families you cannot buy");
        sb.AppendLine();
        sb.AppendLine("The half of the ask that matters most, and the half a hand-written page always gets wrong:");
        sb.AppendLine("a family of *rungs* — the same shape as Might or Focus, something a potion COULD hand out —");
        sb.AppendLine("that has no potion and no scroll. Only a class or the buffer NPC can give you these, so a");
        sb.AppendLine("solo player without a buffer cannot have them at all.");
        sb.AppendLine();
        Table(uncovered.Where(f => rungs[f].All(IsLadderRung)));

        sb.AppendLine("## 3. Class and self buffs — not part of the single-buff ladder at all");
        sb.AppendLine();
        sb.AppendLine("Context, not a gap. Each of these carries its own duration and its own MP price: it IS a");
        sb.AppendLine("skill rather than a rung something else hands out, so \"why is there no potion of it\" does");
        sb.AppendLine("not apply the way it does to the table above.");
        sb.AppendLine();
        Table(uncovered.Where(f => !rungs[f].All(IsLadderRung)));

        // ============================ 4. EVERY ITEM, ONE ROW EACH ============================
        sb.AppendLine("## 4. Every consumable-buff item");
        sb.AppendLine();
        sb.AppendLine("| Item | Rarity | Kind | Gives | Rung | Runs for | Where it comes from | Bar | Slot |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|");

        foreach (var p in items.OrderBy(p => FamilyName(p.Child.BuffKey))
                               .ThenBy(p => p.Child.Rank).ThenBy(p => p.Item.Name))
        {
            var sources = Sources(p.Item.Id).ToArray();
            sb.AppendLine($"| {p.Item.Name} | {p.Item.Rarity} | {Form(p.Wrapper)} | {FamilyName(p.Child.BuffKey)}"
                          + $" — {Magnitude(p.Child)} | {p.Child.Rank} | {Minutes(p.Wrapper.DurationTicks)} | "
                          + (sources.Length == 0 ? "**nothing grants it**" : string.Join(", ", sources))
                          + $" | {p.Wrapper.BuffRow} | " + (Slot(p.Wrapper, p.Child) ? "yes" : "no") + " |");
        }
        sb.AppendLine();

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, sb.ToString());
        Console.WriteLine($"Wrote {path}");
        Console.WriteLine($"  {byFamily.Count} families with a consumable, "
                          + $"{rungs.Count - byFamily.Count} without, {items.Length} items.");

        // ---- local helpers -------------------------------------------------------------------

        // One "family with no consumable" table. WHO can give it to you is DERIVED — every wrapper and
        // every group that names one of these rungs — so a blessing authored tomorrow appears here
        // without anyone remembering to add a row. An empty answer is a real finding: a ladder nothing
        // in the game hands out.
        void Table(IEnumerable<string> families)
        {
            sb.AppendLine("| Buff | Family | Rungs | Top rung | Who can give it to you | NPC buffer? |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (var fam in families)
            {
                var ladder = rungs[fam];
                var casters = wrappers
                    .Where(w => ladder.Any(r => r.Id == w.ChildBuffs![0]))
                    .Where(w => !items.Any(p => p.Wrapper.Id == w.Id))
                    .Select(w => w.Name).Distinct().OrderBy(n => n).ToArray();
                // A GROUP covers a family through a child it names, so it is a second route in and has
                // to be asked for separately or a family reads as unreachable when it is not.
                var groups = SkillCatalog.AllSkills
                    .Where(d => d.ChildBuffs is { Length: > 1 }
                                && d.ChildBuffs.Any(k => ladder.Any(r => r.Id == k)))
                    .Select(d => d.Name + " (group)").Distinct().OrderBy(n => n).ToArray();
                // The rung may BE the skill (a class self-buff), in which case it casts itself.
                var selves = ladder.Where(r => !IsLadderRung(r))
                                   .Select(r => r.Name).Distinct().OrderBy(n => n).ToArray();

                var all = casters.Concat(groups).Concat(selves).Distinct().ToArray();
                sb.AppendLine($"| **{FamilyName(fam)}** | `{fam}` | {ladder.Length} | {Magnitude(ladder[^1])} | "
                              + (all.Length == 0 ? "**nothing — dead family**" : string.Join(", ", all)) + " | "
                              + (npcFamilies.Contains(fam) ? "yes" : "no") + " |");
            }
            sb.AppendLine();
        }

        string FamilyName(string family) =>
            rungs.TryGetValue(family, out var l) && l.Length > 0 ? l[0].Name : family;

        string Magnitude(SkillDef rung) =>
            string.IsNullOrWhiteSpace(rung.Description) ? "—" : rung.Description.TrimEnd('.');

        string RungList(IEnumerable<(ItemDef Item, SkillDef Wrapper, SkillDef Child)> set)
        {
            var list = set.OrderBy(p => p.Child.Rank).ToArray();
            return list.Length == 0 ? "" : string.Join(" / ", list.Select(p => $"rung {p.Child.Rank}"));
        }

        // The buffer's rung for a family, read back out of what it actually hands out.
        int NpcRank(string family, HashSet<string> _)
        {
            int best = 0;
            foreach (var id in SkillCatalog.NewbieBuffSet)
            {
                if (SkillCatalog.Get(id) is not SkillDef def || def.ChildBuffs is null) continue;
                foreach (var kid in def.ChildBuffs)
                    if (SkillCatalog.Get(kid) is { } c && c.BuffKey == family && c.Rank > best)
                        best = c.Rank;
            }
            return best;
        }
    }

    private static BuffForm Form(SkillDef wrapper) => SkillCatalog.ConsumableBuffForm(wrapper);

    private static string Minutes(int ticks) =>
        ticks <= 0 ? "—"
      : ticks >= 600 ? $"{ticks / 600f:0.#} min"
                     : $"{ticks / 10f:0.#}s";

    /// <summary>The SERVER's own cap predicate, restated: `CountsAgainstBuffCap`. The landing row is the
    /// WRAPPER's (a wrapper's child lands in the wrapper's row) and the flag is the CHILD's.</summary>
    private static bool Slot(SkillDef wrapper, SkillDef child) =>
        child.CountsTowardBuffLimit
        && wrapper.BuffRow is BuffRow.Buff or BuffRow.Consumable;

    /// <summary>Every route by which this item can reach a player's bag. An empty answer is a real
    /// finding, not a gap in the query — it means the item exists and nothing grants it.</summary>
    private static IEnumerable<string> Sources(string itemId)
    {
        foreach (var shop in ShopCatalog.AllShops)
            if (shop.ItemIds.Contains(itemId))
                yield return $"vendor ({shop.Title})";

        if (RecipeCatalog.All.Any(r => r.OutputId == itemId))
            yield return "craft";

        foreach (var box in BoxCatalog.AllBoxes)
            if (box.Entries.Any(e => e.ItemId == itemId))
                yield return $"box ({ItemCatalog.Get(box.Id)?.Name ?? box.Id})";

        if (MobCatalog.Templates.Any(m => m.Drops is { } d && d.Any(e => e.ItemId == itemId)))
            yield return "drop";
    }
}
