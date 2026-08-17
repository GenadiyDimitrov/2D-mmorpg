namespace Game.Shared;

/// <summary>
/// The NAME a discipline wears, per race, at the 3rd and the 4th class.
///
/// <para><b>Why this table exists.</b> A discipline used to be one string —
/// <c>Discipline.ToString()</c> — so a Human Knight, an Elf Templar and an Ork Beast all became a
/// "Bulwark". That sat badly against the owner's own ruling for the 2026-08-17 map (*"the varity
/// will come from race diference"*): the KIT could already differ per race (the trailing `RACE`
/// column in the 3rd/4th CSVs is exactly that), but the LABEL could not, so the variety was
/// invisible. The rogue line was the one exception — the archer merge split it by race in
/// 2026-07-29, which is why Nullblade/Phantom/Venomweaver and Sharpshooter/Trapper/Hunter are
/// separate <see cref="Discipline"/> values with names of their own.</para>
///
/// <para><b>These are NAMES, not ids.</b> Nothing persists a class name — characters store the
/// numeric class id — so retuning any string here is free and breaks no save. The ids
/// (101-136 for 3rd, 201-236 for 4th) are the things that must never move.</para>
///
/// <para><b>Naming rule (see CLAUDE.md).</b> Original generic fantasy only; never a class, town,
/// NPC, item or skill name trademarked by IG or any other game. The retired <c>Warlord</c> —
/// which was the war_aoe discipline's name until 2026-08-17 — went for exactly that reason, and
/// its three races are Banneret / Galeherald / Skullbreaker now.</para>
/// </summary>
public static class ClassNames
{
    /// <summary>The pair of names one (discipline, race) wears: at 40, and at 76.</summary>
    public readonly record struct Pair(string Third, string Fourth);

    // ===== THE TABLE ============================================================================
    //  Eight disciplines × three races. The rogue line's six values are already race-specific
    //  (one race each), so they carry one row rather than three.
    //
    //  Tone, kept deliberate so a new row has somewhere to sit:
    //    Human — martial, ordered, heraldic (Knight → Bulwark → Ironcrown)
    //    Elf   — light, wind, growth        (Templar → Aegis → Dawnshield)
    //    Ork   — bone, blood, endurance     (Beast → Ironhide → Stonemaw)
    private static readonly Dictionary<(Discipline, Race), Pair> Table = new()
    {
        // --- TANK (2nd: Knight / Templar / Beast) -----------------------------------------------
        [(Discipline.Bulwark, Race.Human)] = new("Bulwark",      "Ironcrown"),
        [(Discipline.Bulwark, Race.Elf)]   = new("Aegis",        "Dawnshield"),
        [(Discipline.Bulwark, Race.Ork)]   = new("Ironhide",     "Stonemaw"),

        // --- WARRIOR, single-target burst (2nd: Champion / Sentinel / Warrior) ------------------
        //  Ravager moved to the ORK — it always read as the ork's word — and the human took a
        //  martial one, which is why the human's 3rd name changed and the ork's did not.
        [(Discipline.Ravager, Race.Human)] = new("Bladesworn",   "Bladelord"),
        [(Discipline.Ravager, Race.Elf)]   = new("Thornblade",   "Windreaver"),
        [(Discipline.Ravager, Race.Ork)]   = new("Ravager",      "Bloodrager"),

        // --- WAR_AOE, the tanky AoE bruiser (2nd: Champion / Sentinel / Warrior) ----------------
        //  `Warlord` is retired here: it is a class name in IG, so it fell to the naming rule.
        [(Discipline.Warlord, Race.Human)] = new("Banneret",     "Warmarshal"),
        [(Discipline.Warlord, Race.Elf)]   = new("Galeherald",   "Stormcrown"),
        [(Discipline.Warlord, Race.Ork)]   = new("Skullbreaker", "Bonecrusher"),

        // --- DUAL, the melee rogue (already one discipline PER RACE — the archer merge) ---------
        [(Discipline.Nullblade,    Race.Human)] = new("Nullblade",    "Hexbane"),
        [(Discipline.Phantom,      Race.Elf)]   = new("Phantom",      "Nightveil"),
        [(Discipline.Venomweaver,  Race.Ork)]   = new("Venomweaver",  "Plaguefang"),

        // --- ARCHER, the ranged rogue (likewise per race already) -------------------------------
        [(Discipline.Sharpshooter, Race.Human)] = new("Sharpshooter", "Deadeye"),
        [(Discipline.Trapper,      Race.Elf)]   = new("Trapper",      "Bramblewarden"),
        [(Discipline.Hunter,       Race.Ork)]   = new("Hunter",       "Bloodhunter"),

        // --- NUKER (2nd: Sorcerer / Inquisitor / Witch) -----------------------------------------
        [(Discipline.Magus, Race.Human)] = new("Magus",       "Runelord"),
        [(Discipline.Magus, Race.Elf)]   = new("Starweaver",  "Celestine"),
        [(Discipline.Magus, Race.Ork)]   = new("Cinderwitch", "Pyrelord"),

        // --- HEALER (2nd: Cleric / Priest / Shaman) ---------------------------------------------
        [(Discipline.Lightbringer, Race.Human)] = new("Lightbringer", "Lifewarden"),
        [(Discipline.Lightbringer, Race.Elf)]   = new("Dawnsworn",    "Everdawn"),
        [(Discipline.Lightbringer, Race.Ork)]   = new("Bonemender",   "Spiritbinder"),

        // --- BUFFER (2nd: Cleric / Priest / Shaman) ---------------------------------------------
        [(Discipline.Warchanter, Race.Human)] = new("Warchanter",   "Oathkeeper"),
        [(Discipline.Warchanter, Race.Elf)]   = new("Harmonist",    "Gracebinder"),
        [(Discipline.Warchanter, Race.Ork)]   = new("Bloodchanter", "Totemlord"),

        // --- THE TWO ORPHANS -------------------------------------------------------------------
        //  Vanguard (the off-tank) and Tempest (the AoE nuker) are dropped/merged by the owner's
        //  2026-08-17 map, but the enum is NOT collapsed — the values persist on characters and
        //  `Disciplines.Of` still offers them, so a live character can be holding one. They get ONE
        //  name for all three races on purpose: inventing six race names for two classes on their
        //  way out would be work spent on something that is being deleted. When the merge actually
        //  happens these two rows go with the enum values.
        [(Discipline.Vanguard, Race.Human)] = new("Vanguard", "Doomward"),
        [(Discipline.Vanguard, Race.Elf)]   = new("Vanguard", "Doomward"),
        [(Discipline.Vanguard, Race.Ork)]   = new("Vanguard", "Doomward"),
        [(Discipline.Tempest,  Race.Human)] = new("Tempest",  "Skybreaker"),
        [(Discipline.Tempest,  Race.Elf)]   = new("Tempest",  "Skybreaker"),
        [(Discipline.Tempest,  Race.Ork)]   = new("Tempest",  "Skybreaker"),
    };

    /// <summary>The 3rd-class name — what a level-40 of this race and discipline is CALLED.
    /// Falls back to the enum name so a discipline added without a row still reads as something
    /// rather than blank.</summary>
    public static string Third(Discipline d, Race race) =>
        Table.TryGetValue((d, race), out var p) ? p.Third : d.ToString();

    /// <summary>The 4th-class name — what the same character is called from 76.</summary>
    public static string Fourth(Discipline d, Race race) =>
        Table.TryGetValue((d, race), out var p) ? p.Fourth : d.ToString();

    /// <summary>Startup guard: every name must be unique across the whole table, both tiers
    /// together. Two classes sharing a label is not a cosmetic bug — the class-change NPC lists
    /// classes BY NAME, so a duplicate makes two different changes indistinguishable. The two
    /// orphans are exempt: they deliberately repeat one name across their three races.</summary>
    public static IEnumerable<string> DuplicateNames()
    {
        var seen = new Dictionary<string, (Discipline D, Race R, string Tier)>();
        foreach (var ((d, race), pair) in Table)
        {
            if (d is Discipline.Vanguard or Discipline.Tempest) continue;
            foreach (var (name, tier) in new[] { (pair.Third, "3rd"), (pair.Fourth, "4th") })
            {
                if (seen.TryGetValue(name, out var prev))
                    yield return $"'{name}' is both {prev.R} {prev.D} {prev.Tier} and {race} {d} {tier}";
                else seen[name] = (d, race, tier);
            }
        }
    }
}
