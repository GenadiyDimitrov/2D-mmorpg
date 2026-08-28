namespace Game.Shared;

/// <summary>
/// A 4th class (level 76). It evolves ONE specific 3rd class — the character must already hold
/// that discipline — and it is <b>the same discipline under a new name</b>.
///
/// <para><b>Why there is no fourth enum.</b> The owner's 2026-08-17 map says the tiers are
/// `3rd` = the discipline from 40 and `4th` = the same discipline from 76 (see the CSV README:
/// sixteen 40+ files, eight disciplines, twice). So a 4th class does not branch and does not pick
/// a new identity — it is the awakening of the one you already walk. That is why this carries a
/// <see cref="Discipline"/> rather than an enum of its own, and why the id maps 1:1 onto its
/// parent's.</para>
///
/// <para><b>It carries no stats</b>, for the same reason the 3rd class does not (owner,
/// 2026-08-10: *"There is no identity. The identity is just the skills/passives kit"*). Do not add
/// a `Bonus` field here — see the ⚠ block at the foot of Classes.Third.cs.</para>
///
/// <para>⚠ <b>It carries no SKILLS yet either.</b> <see cref="ClassKey"/> has no tier component, so
/// a 4th-class kit cannot simply be registered against the discipline — that would leak it to
/// every level-40. The kit lands when the owner's `*.4th.csv` files are authored (`BL-02`), and
/// giving it one before then would violate the standing 40+ rule: *"Anything that's not inside the
/// csv should not exist except the class balance."* Until then the 4th class changes your NAME and
/// opens <see cref="Crafting.RequireFourthClassForL5"/>, and that is all it does.</para>
/// </summary>
public record FourthClassDef(int Id, string Name, Race Race, int ParentThirdClassId,
    Discipline Discipline);

/// <summary>
/// The 36 fourth classes, one per <see cref="ThirdClassCatalog"/> entry. Ids live at 201-236 so
/// they never collide with 2nd-class ids (1-18), the retired God ids (98/99) or the 3rd-class ids
/// (101-136) — every tier shares one id space because <c>ClassChangeRequirements.Requirement</c>
/// keys on a single "the class you BECOME" number.
/// </summary>
public static class FourthClassCatalog
{
    /// <summary>Character level required to take a 4th class (owner: *"L5,6 needs 76"*, and the
    /// CSV tiers put `4th` at 76+).</summary>
    public const int ChangeLevel = 76;

    /// <summary>The id offset from a 3rd class to its 4th: 101 -> 201, 136 -> 236.</summary>
    public const int IdOffset = 100;

    private static readonly Dictionary<int, FourthClassDef> All = Build();

    private static Dictionary<int, FourthClassDef> Build()
    {
        var d = new Dictionary<int, FourthClassDef>();
        foreach (var tc in ThirdClassCatalog.Playable)
        {
            int id = tc.Id + IdOffset;
            d[id] = new FourthClassDef(id, ClassNames.Fourth(tc.Discipline, tc.Race),
                tc.Race, tc.Id, tc.Discipline);
        }
        return d;
    }

    public static FourthClassDef? Get(int id) => All.GetValueOrDefault(id);

    public static IEnumerable<FourthClassDef> Playable => All.Values.OrderBy(c => c.Id);

    /// <summary>The 4th-tier sibling of <see cref="ThirdClassCatalog.Surviving"/> — maps a persisted
    /// 4th-class id whose discipline was RETIRED (`BL-97`: 212/224/236, the Tempest's ascensions) onto
    /// the one that still exists. Same positional rule, one tier up: a retired B slot sits one above
    /// its surviving A sibling. Anything else is returned unchanged.</summary>
    public static int Surviving(int fourthClassId) =>
        fourthClassId > 0 && !All.ContainsKey(fourthClassId) && All.ContainsKey(fourthClassId - 1)
            ? fourthClassId - 1
            : fourthClassId;

    /// <summary>The single 4th class a given 3rd class ascends into (null if the id is not a
    /// 3rd class). One-to-one — a 4th class is never a choice, only a threshold.</summary>
    public static FourthClassDef? ForParent(int thirdClassId) =>
        All.GetValueOrDefault(thirdClassId + IdOffset);
}
