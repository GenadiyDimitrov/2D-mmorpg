using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>`BL-180` — THE ADMIN BUFF MENU, sorted into the four drawers the owner named.
///
/// <para>His ask, 2026-09-06: *"can you make in functions under the buffsbuttons - add [buffs] -&gt; sub
/// menu to open with 4 more submenues -&gt; single, group, harmonies, marks"*. The Functions tab had six
/// hand-written buff buttons on it and every other buff in the game was reachable only by typing
/// `/buff &lt;name&gt;` on a phone keyboard.</para>
///
/// <para>🔑 <b>IT IS DERIVED, LIKE EVERY OTHER LIST IN THAT WINDOW.</b> The admin menu's gear, towns,
/// zones and classes are all read out of the catalogs, for the reason the hand-listed WPF menu proved:
/// a typed list goes stale and whole tiers silently vanish from it. So does this — add a harmony to
/// `buffer 3rd.csv`, a Mark to `buffer 4th.csv` or a blessing to the NPC shelf and its button appears
/// with no second edit here. That is the same rule <see cref="AdminBuffSet"/> already runs on.</para>
///
/// <para>🔑 <b>THE UNIVERSE IS THE TWO SHELVES THE GAME HAS</b>, unioned: what a max-level buffer CLASS
/// can cast (<see cref="AdminBuffSet"/>, plus the three <c>AdminBuffSkip</c> buffs a full buff
/// deliberately withholds) and what the Spirit Helper SELLS (<see cref="NewbieBuffSet"/>, which since
/// `BL-160`/`BL-161` carries the eight single harmonies and the three Marks). Those two shelves are
/// separate and neither may be built out of the other (his rule, 2026-09-03) — this READS both and
/// writes to neither, which is the only relationship between them that is allowed.</para>
///
/// <para>⚠ <b>ONE NAME, ONE BUTTON.</b> Several defs share a display name on purpose — "Might" is the
/// Warchanter's own multi-rung ladder AND the NPC's hour-long single — so the union is deduplicated by
/// display name, strongest first (class kit before NPC shelf). That is not a new rule: it is exactly
/// what `GameLoopService.MatchBuffsByName` does when you type the name, so the button and the typed
/// command land the same buff. The button sends the ID, so it can never be caught by the ambiguity
/// rule a name lookup has to live with.</para>
///
/// <para>📐 Print the four drawers without a phone:
/// <c>dotnet run --project tools/BalanceMatrix -- --buffmenu</c>.</para></summary>
public static partial class SkillCatalog
{
    /// <summary>The four drawers of the admin Buffs menu. The order is the order the buttons are
    /// offered in, which is his: *"single, group, harmonies, marks"*.</summary>
    public enum AdminBuffDrawer { Single, Group, Harmony, Mark }

    /// <summary>One button: which skill, and what to write on it.</summary>
    public readonly record struct AdminBuffEntry(string SkillId, string Name);

    private static Dictionary<AdminBuffDrawer, AdminBuffEntry[]>? _adminBuffMenu;

    /// <summary>The buttons for one drawer, alphabetical by name. Never null; a drawer with nothing in
    /// it returns empty (the menu says so rather than drawing a blank page).</summary>
    public static IReadOnlyList<AdminBuffEntry> AdminBuffMenu(AdminBuffDrawer drawer) =>
        (_adminBuffMenu ??= BuildAdminBuffMenu()).TryGetValue(drawer, out var rows)
            ? rows : Array.Empty<AdminBuffEntry>();

    /// <summary>Which drawer this buff belongs in — the whole classification, in one place, asked of
    /// the DATA every time.
    ///
    /// <para>🔑 The order of the tests is the design. A Mark is decided by its <b>buff key</b>
    /// (<c>MarkKey</c>, the shared family that is his *"Do not Stack with Other 'Mark' Skills"*), so
    /// the Harmony Mark files as a Mark and not as a harmony despite its name — which is what he asked
    /// for, since he listed it under Marks himself. A harmony is decided by its NAME, because that is
    /// the only thing the twelve of them share: the four CLASS harmonies carry `Magnitudes` and the
    /// eight NPC ones are one-child wrappers, so no structural test sees both. A group is decided by
    /// STRUCTURE (more than one child), the same test <c>BuildAdminBuffSet</c> uses to put groups
    /// first. Everything else is a single.</para></summary>
    public static AdminBuffDrawer DrawerOf(SkillDef def)
    {
        if (def.BuffKey == MarkKey) return AdminBuffDrawer.Mark;
        if (def.Name.StartsWith("Harmony", StringComparison.Ordinal)) return AdminBuffDrawer.Harmony;
        if (def.ChildBuffsAt(def.MaxLevel) is { Length: > 1 }) return AdminBuffDrawer.Group;
        return AdminBuffDrawer.Single;
    }

    /// <summary>Is this a HARMONY or a MARK — the top shelf, the two families that occupy a buff slot
    /// however briefly they run (owner, 2026-09-11: *"all buffs that are not 20min and not harmonies
    /// or marks ... not enter the limit"*).
    ///
    /// <para>🔑 THE SAME TWO TESTS <see cref="DrawerOf"/> USES, IN THE SAME ORDER, and deliberately
    /// sharing them: the classification he named the drawers by is the classification the cap is
    /// priced on, and two copies of "what is a harmony" would drift the first time one grew a
    /// thirteenth member. Mark by buff KEY (his *"Do not Stack with Other 'Mark' Skills"* family),
    /// harmony by NAME — the only thing the twelve share, since four carry magnitudes and eight are
    /// one-child wrappers.</para>
    ///
    /// <para>⚠ Asked of the LANDING def. The eight NPC harmonies land through a wrapper, so what
    /// arrives here is their child — which does not matter, because those run an hour and clear the
    /// duration line on their own. It is the four CLASS harmonies and the Marks, at five minutes, that
    /// need this test at all.</para></summary>
    public static bool IsHarmonyOrMark(SkillDef def) =>
        def.BuffKey == MarkKey || def.Name.StartsWith("Harmony", StringComparison.Ordinal);

    private static HashSet<string>? _buffLimitIds;

    /// <summary>`BL-198` — <b>THE BUFF-LIMIT COLLECTION</b>: the ids of every buff that occupies one of
    /// the <see cref="GameConstants.MaxBuffSlots"/> squares. Membership is the whole test.
    ///
    /// <para>🔑 <b>HIS RULING, 2026-09-11, AND IT OVERTURNED THE DURATION TEST `BL-195` SHIPPED THE
    /// DAY BEFORE:</b> *"it should not work only on timer ... the limit should have an id collection
    /// ... and if that skill is inside that collection it goes to the buff bar and counts ... i gave
    /// the duration as filter not as solution"*.</para>
    ///
    /// <para>🔴 <b>AND HE IS RIGHT, WITH A COUNTEREXAMPLE THE DURATION TEST COULD NOT SURVIVE:</b>
    /// *"if one buff a 10 min buff and it doubles it probanbly break en enter the count .. but it
    /// shouldns"*. `BL-190`'s <c>DoubleDurationRate</c> doubles a landed duration on a roll — so a
    /// 10-minute buff that happened to roll a double would cross twenty minutes and start costing a
    /// slot, and the same buff on the same character would cost a slot or not depending on a die. A
    /// property of the SKILL cannot be decided by a per-cast roll. **Never make a rule read a number
    /// something else in the game is allowed to multiply.**</para>
    ///
    /// <para>🔑 <b>THE COLLECTION IS DERIVED, NOT TYPED OUT</b>, for the reason every list in this file
    /// is: a typed list goes stale and whole tiers silently vanish from it. His own enumeration —
    /// *"single buffs, grouped buffs, harmonies, marks, archers 20 min buffs, any other self 20 min
    /// buff we have (cant remember them all)"* — is exactly two sources:</para>
    /// <list type="number">
    ///   <item><b>THE TWO SHELVES</b>, unioned — every single, group, harmony and Mark the game has.
    ///         That is the same universe the admin Buffs menu's four drawers are built from, so his
    ///         first four categories ARE those four drawers and need no second definition.</item>
    ///   <item><b>EVERY OTHER 20-MINUTE BUFF</b>, by its AUTHORED <c>DurationTicks</c> — which is what
    ///         picks up the archer's Bow Expertise / Blessing / Spirit and anything else authored that
    ///         long later, with no edit here. ⚠ The authored field, never the landed one: that is what
    ///         makes it immune to the doubling above.</item>
    /// </list>
    ///
    /// <para>⚠ <b>ROW <c>Buff</c> ONLY IN RULE 2.</b> The three RUNES run an HOUR and would otherwise
    /// be swept in, and they must not be: a ~1/s reconciliation loop re-derives them from the held
    /// items, so evicting one frees a slot for a fraction of a second and then puts it straight back.
    /// They draw in <c>BuffRow.Consumable</c>, and so do potions and scrolls — whose CHILDREN are
    /// already in via rule 1, because a potion of Might and a cleric's Might are literally the same
    /// buff from different bottles and always have been.</para>
    ///
    /// <para>⚠ <b>THE CHILD IDS ARE IN TOO.</b> A single blessing lands through a one-child wrapper and
    /// the buff that ends up on the bar carries the CHILD's id, so a set of wrapper ids alone would
    /// match nothing at the only moment it is asked.</para>
    ///
    /// <para>📐 Print it: <c>dotnet run --project tools/BalanceMatrix -- --bufflimit</c>. That listing
    /// is the answer to his *"U can ask me for some that i didnt meantion"* — it is faster to read the
    /// derived list than to remember the buffs.</para></summary>
    public static IReadOnlyCollection<string> BuffLimitIds => _buffLimitIds ??= BuildBuffLimitIds();

    /// <summary>Does this landing def occupy a buff slot? See <see cref="BuffLimitIds"/>.</summary>
    public static bool OccupiesBuffSlot(SkillDef def) => BuffLimitIds.Contains(def.Id);

    private static HashSet<string> BuildBuffLimitIds()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);

        void Include(SkillDef def)
        {
            // The same test the drawers use: a TIMED BUFF and nothing else. It is what drops the
            // attack skills, the heals, the totems and every passive as the kit grows.
            if (def.Category != SkillCategory.Buff || def.DurationTicks <= 0) return;
            if (!set.Add(def.Id)) return;
            // …and whatever actually LANDS. A one-child wrapper stamps the CHILD's id on the buff, and
            // a wrapper may pick a different child per level (Sprint's two rungs), so every level's.
            for (int lv = 1; lv <= Math.Max(1, def.MaxLevel); lv++)
                if (def.ChildBuffsAt(lv) is { Length: 1 } kid)
                    set.Add(kid[0]);
        }

        // 1 — the two shelves: singles, groups, harmonies and Marks, i.e. his first four categories.
        foreach (string id in AdminBuffSet.Concat(AdminBuffSkip).Concat(NewbieBuffSet))
            if (Get(id) is SkillDef shelf) Include(shelf);

        // 2 — every other TWENTY-MINUTE buff, off its AUTHORED duration. ⚠ `BuffRow.Buff` only: see
        //     the note about the runes on BuffLimitIds.
        foreach (var def in AllSkills)
            if (def.BuffRow == BuffRow.Buff
                && def.DurationTicks >= GameConstants.BuffLimitMinDurationTicks)
                Include(def);

        return set;
    }

    private static Dictionary<AdminBuffDrawer, AdminBuffEntry[]> BuildAdminBuffMenu()
    {
        // ⚠ ORDER MATTERS HERE AND NOWHERE ELSE: the dedupe below keeps the FIRST def of a given
        // display name, so the class kit has to come before the NPC shelf or "Might" would resolve to
        // the hour-long single instead of the Warchanter's top rung.
        var universe = AdminBuffSet
            .Concat(AdminBuffSkip)      // Shrouding Hymn, Bow Expertise, War Bulwark — withheld, not gone
            .Concat(NewbieBuffSet);     // the Spirit Helper's 30, incl. the 8 single harmonies + 3 Marks

        var seenId = new HashSet<string>(StringComparer.Ordinal);
        var seenName = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var byDrawer = new Dictionary<AdminBuffDrawer, List<AdminBuffEntry>>();

        foreach (string id in universe)
        {
            if (!seenId.Add(id)) continue;
            if (Get(id) is not SkillDef def) continue;
            // The same test AdminBuffSet uses: a TIMED BUFF and nothing else. It is what drops the
            // attack skills, the heals, the totems and every passive as the kit grows.
            if (def.Category != SkillCategory.Buff || def.DurationTicks <= 0) continue;
            if (!seenName.Add(def.Name)) continue;

            var drawer = DrawerOf(def);
            if (!byDrawer.TryGetValue(drawer, out var list))
                byDrawer[drawer] = list = new List<AdminBuffEntry>();
            list.Add(new AdminBuffEntry(id, def.Name));
        }

        return byDrawer.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.OrderBy(e => e.Name, StringComparer.Ordinal).ToArray());
    }
}
