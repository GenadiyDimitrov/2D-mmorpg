namespace Game.Shared;

/// <summary>Which SHAPE of consumable a buff came in. The two are not interchangeable: a scroll lasts
/// an hour and is bought (Blessing Box only), a potion lasts twenty minutes and is found — which is
/// why the auto-buff tab arms them separately and prefers a scroll at equal rarity.</summary>
public enum BuffForm { None = 0, Potion = 1, Scroll = 2 }

/// <summary>One consumable ITEM that hands out one rung of one buff family.</summary>
/// <param name="Family">The <see cref="SkillDef.BuffKey"/> of the rung it applies — the thing that
/// makes two sources compete instead of stack.</param>
/// <param name="Rank">The rung's position on its family's ladder. NOT the rarity: for a scroll-only
/// family the one Rare scroll sits on rung 6 of 6.</param>
public record BuffConsumable(string ItemId, string ItemName, string Family, string FamilyName,
                             int Rank, ItemRarity Rarity, BuffForm Form);

/// <summary>
/// Every buff potion and buff scroll in the catalog, indexed by the FAMILY it belongs to.
///
/// Nothing here is authored — it is read back out of <see cref="ItemCatalog"/> and
/// <see cref="SkillCatalog"/>: an item's <c>UseSkillId</c> is a wrapper skill whose one child is the
/// family rung, and the child carries the family key, the rank and the display name. So adding a
/// potion in Items.cs is enough for it to appear in the auto-buff tab, and there is no second list to
/// keep in step with the first.
///
/// <para>The BURSTS drop out for free: Dash's wrapper lasts 150 ticks, which is neither the potion nor
/// the scroll duration, so <see cref="SkillCatalog.ConsumableBuffForm"/> calls it <c>None</c> and it
/// never reaches a row. That is the right answer — the autopilot drinking a 15-second sprint on a
/// 1-minute reuse would empty the stack for nothing (the same reason the old keep-everything-up loop
/// had to exclude it by hand).</para>
/// </summary>
public static class BuffConsumables
{
    private static readonly Lazy<BuffConsumable[]> Lazy = new(Build);

    /// <summary>Every buff consumable, family order then strongest first.</summary>
    public static IReadOnlyList<BuffConsumable> All => Lazy.Value;

    private static BuffConsumable[] Build()
    {
        var list = new List<BuffConsumable>();
        foreach (var item in ItemCatalog.AllItems)
        {
            if (string.IsNullOrEmpty(item.UseSkillId)) continue;
            if (SkillCatalog.Get(item.UseSkillId) is not SkillDef wrapper) continue;

            var form = SkillCatalog.ConsumableBuffForm(wrapper);
            if (form == BuffForm.None) continue;
            if (wrapper.ChildBuffs is not { Length: > 0 } children) continue;
            if (SkillCatalog.Get(children[0]) is not SkillDef rung || string.IsNullOrEmpty(rung.BuffKey)) continue;

            list.Add(new BuffConsumable(item.Id, item.Name, rung.BuffKey, rung.Name,
                                        rung.Rank, item.Rarity, form));
        }

        // Family order is the order the families first appear in the catalog, which is the order they
        // were authored (the speed four, then Might/Bulwark/Force/Ward/Aim, then the scroll-only
        // eight). Stable and meaningful; alphabetical would scatter the pairs that belong together.
        var order = new Dictionary<string, int>();
        foreach (var c in list)
            if (!order.ContainsKey(c.Family)) order[c.Family] = order.Count;

        return list
            .OrderBy(c => order[c.Family])
            .ThenByDescending(c => c.Rarity)
            .ThenByDescending(c => c.Form == BuffForm.Scroll)
            .ThenByDescending(c => c.Rank)
            .ToArray();
    }

    /// <summary>Every family that has at least one consumable, in catalog order, with the name to put
    /// on its row ("Might", "Bulwark", …). Families that exist only as a class buff's child — Vampirism,
    /// Resolve — are absent, and must be: there is nothing to arm.</summary>
    public static IReadOnlyList<(string Family, string Name)> Families
    {
        get
        {
            var seen = new List<(string, string)>();
            var keys = new HashSet<string>();
            foreach (var c in All)
                if (keys.Add(c.Family)) seen.Add((c.Family, c.FamilyName));
            return seen;
        }
    }

    /// <summary>The consumables of one family, strongest first.</summary>
    public static IEnumerable<BuffConsumable> OfFamily(string family) =>
        All.Where(c => c.Family == family);

    /// <summary>Does this family sell a potion / a scroll at all? The row greys the toggle it has no
    /// item for, rather than offering a switch that can never do anything (nine families have both;
    /// the other eight are scroll-only).</summary>
    public static bool HasForm(string family, BuffForm form) =>
        All.Any(c => c.Family == family && c.Form == form);

    /// <summary>The rarities a family actually sells, weakest first — the rungs its "max rarity" cap
    /// can stop at. Derived, so a family with one Rare scroll offers exactly one choice.
    ///
    /// <para>Returns a concrete <c>List</c> rather than <c>IReadOnlyList</c> on purpose: the caller
    /// wants <c>IndexOf</c>/<c>Contains</c>, and on the interface those bind to the SPAN extensions
    /// instead, which is a compile error rather than a fallback.</para></summary>
    public static List<ItemRarity> RaritiesOf(string family) =>
        All.Where(c => c.Family == family).Select(c => c.Rarity).Distinct().OrderBy(r => r).ToList();

    /// <summary>
    /// THE PICK ORDER for one family, given what the player armed (owner, playtest-21):
    /// <i>"priority is rarity first, then scroll &gt; potion — uncommon scroll → uncommon potion →
    /// common scroll → common potion."</i>
    ///
    /// <para>Rank is only the tiebreak, never the lead. It agrees with rarity everywhere in today's
    /// ladder (a family's Rare scroll is also its top rung), but the owner named RARITY, and rarity is
    /// what the row's cap is spelled in — so a cap of "uncommon" must never be undercut by a rung that
    /// happens to sort higher.</para>
    /// </summary>
    public static IEnumerable<BuffConsumable> PickOrder(string family, bool potions, bool scrolls,
                                                       ItemRarity maxRarity) =>
        All.Where(c => c.Family == family
                    && c.Rarity <= maxRarity
                    && (c.Form == BuffForm.Potion ? potions : scrolls))
           .OrderByDescending(c => c.Rarity)
           .ThenByDescending(c => c.Form == BuffForm.Scroll)
           .ThenByDescending(c => c.Rank);
}
