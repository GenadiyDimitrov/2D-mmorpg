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
    private static AdminBuffDrawer DrawerOf(SkillDef def)
    {
        if (def.BuffKey == MarkKey) return AdminBuffDrawer.Mark;
        if (def.Name.StartsWith("Harmony", StringComparison.Ordinal)) return AdminBuffDrawer.Harmony;
        if (def.ChildBuffsAt(def.MaxLevel) is { Length: > 1 }) return AdminBuffDrawer.Group;
        return AdminBuffDrawer.Single;
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
