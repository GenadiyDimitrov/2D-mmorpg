namespace Game.Shared;

/// <summary>
/// The NAME a discipline wears, per race, at the 3rd and the 4th class.
///
/// <para><b>Why this table exists.</b> A discipline used to be one string —
/// <c>Discipline.ToString()</c> — so a Human Knight, an Elf Templar and an Demon Beast all became a
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
    //  TWENTY-FOUR ROWS = eight paths × three races. Eight, not twelve discipline VALUES, because the
    //  rogue line's six values are already race-specific (one race each) and so carry one row rather
    //  than three: dual = Nullblade/Phantom/Venomweaver, archer = Sharpshooter/Trapper/Hunter.
    //
    //  Tone, kept deliberate so a new row has somewhere to sit:
    //    Human — martial, ordered, heraldic (Human Knight → Iron Guard → Knight Commander)
    //    Elf   — light, wind, growth        (Elf Knight → Templar → Paladin)
    //    Demon — dread, blood, the abyss    (Demon Knight → Dread Knight → Abyssal Knight)
    //
    //  ⚠ The third race was the ORK until 2026-08-28 (`BL-101`) and its words were bone-and-endurance
    //    (Ironhide → Juggernaut, Bonemender, Bloodchanter). Demon let the MAGE lines finally speak —
    //    an demon shaman was always a shrug, a Dreadcaller is not.
    //
    //  🔑 2026-08-28, `BL-100` — THE WHOLE TABLE WAS REWRITTEN to his list, on one instruction:
    //  *"now just sound over complicated ... but I want it simpler"*. Three rules came out of it, and
    //  every one of them should be checked before a new name is written here:
    //
    //    1. A name says what the class DOES, in words a player already owns — `Iron Guard`,
    //       `Sword Master`, `Fire Adept` — never a coined compound whose meaning only we know
    //       (`Bladesworn`, `Galeherald`, `Bramblewarden`, `Gracebinder` all went for that reason).
    //
    //    2. 🔑🔑 EVERY AoE/SUPPORT 4th CLASS IS A "WAR" WORD. His rule, stated 2026-08-28:
    //       *"my general idea is anything aoe is War named"*. It holds across all six —
    //         war_aoe : War Master · War Storm · Warbringer
    //         buffer  : War Doctor · War Harmonist · Warlock   (*"it contains it as a letter :)"*)
    //       ⚠ **And the 3rd is that 4th's LESSER FORM, never a word that merely rhymes with it.**
    //       `Sword Dancer → War Dancer` was the one row that broke this and it is why it went.
    //
    //    3. Three races are three IDENTITIES of one path, so where a set exists it should be
    //       audible across all three: the nukers are Mana/Water/Fire → Arcane/Ice/Inferno (the
    //       element growing up), the war_aoe 3rds are martial POSITIONS (Vanguard / Skirmisher /
    //       Warborn), the buffers are Doctor / Harmonist / Dreadcaller.
    private static readonly Dictionary<(Discipline, Race), Pair> Table = new()
    {
        // --- TANK (2nd: Human/Elf/Demon Knight) ---------------------------------------------------
        //  🔑 THE ONLY tank discipline since 2026-08-28 (`BL-97`). His test for which of a pair dies:
        //  *"the 3 tanks must have their name and the other is the same for the 3 races ... So is the
        //  one that must go"* — a discipline wearing ONE name across three races was never three
        //  classes, and that is exactly what the retired Vanguard row looked like.
        [(Discipline.Bulwark, Race.Human)] = new("Iron Guard",  "Knight Commander"),
        [(Discipline.Bulwark, Race.Elf)]   = new("Templar",     "Paladin"),
        //  ⚠ `Juggernaut` went with the ork — his word, *"sounds orkish"* — and the demon took the
        //  abyss instead. `Hell Knight` was his first pick and he swapped it himself for `Dread
        //  Knight`, which is the safer of the two anyway (theirs is a bare Hell Knight).
        [(Discipline.Bulwark, Race.Demon)] = new("Dread Knight", "Abyssal Knight"),

        // --- WARRIOR, single-target burst (2nd: Human/Elf/Demon Warrior) --------------------------
        //  ⚠ The human's 4th is `Sword Master` and the elf's 3rd was going to be too — a hard startup
        //  failure, since DuplicateNames() has had no exemptions since `BL-97`. He saw the smell
        //  himself (*"it sounds like the elf warrior 3rd is stronger than human warrior 4th"*); the
        //  fix keeps his Sword-Saint endpoint and gives the elf `Swiftblade` below it.
        [(Discipline.Ravager, Race.Human)] = new("Champion",    "Sword Master"),
        [(Discipline.Ravager, Race.Elf)]   = new("Swiftblade",  "Sword Saint"),
        [(Discipline.Ravager, Race.Demon)] = new("Ravager",     "Berserker"),

        // --- WAR_AOE, the tanky AoE bruiser (2nd: Human/Elf/Demon Warrior) ------------------------
        //  `Warlord` stays retired — it is a class name in IG. `Vanguard` is FREE to use here: the
        //  discipline of that name died in `BL-97`, and a name is not an id (nothing persists one).
        //
        //  🔑 THE THREE 3rd NAMES ARE ONE SET: martial POSITION words. Vanguard / Skirmisher /
        //  Warborn — where you stand in the line, in words a player already owns. The elf was
        //  `Sword Dancer` until 2026-08-28 and it was the only 3rd on the whole roster chosen to
        //  RHYME with its 4th rather than to be that 4th's lesser form; it also sat one letter from a
        //  unit in another well-known fantasy game, which the naming rule covers as much as IG does.
        [(Discipline.Warlord, Race.Human)] = new("Vanguard",   "War Master"),
        [(Discipline.Warlord, Race.Elf)]   = new("Skirmisher", "War Storm"),
        [(Discipline.Warlord, Race.Demon)] = new("Warborn",    "Warbringer"),

        // --- DUAL, the melee rogue (already one discipline PER RACE — the archer merge) ---------
        [(Discipline.Nullblade,    Race.Human)] = new("Assassin",     "Nullblade"),
        [(Discipline.Phantom,      Race.Elf)]   = new("Phantom",      "Shadowblade"),
        [(Discipline.Venomweaver,  Race.Demon)] = new("Stalker",      "Venomblade"),

        // --- ARCHER, the ranged rogue (likewise per race already) -------------------------------
        //  ⚠ The demon pair is `Tracker → Hunter`, NOT the `Hunter → Tracker` his demon row happened to
        //  say: his own DEMON row wrote it the other way round, and Hunter is plainly the stronger
        //  endpoint. Flagged to him — one line to flip if he meant it.
        [(Discipline.Sharpshooter, Race.Human)] = new("Sharpshooter", "Deadeye"),
        [(Discipline.Trapper,      Race.Elf)]   = new("Sentinel",     "Trapper"),
        [(Discipline.Hunter,       Race.Demon)] = new("Soultracker",  "Soulhunter"),

        // --- NUKER (2nd: Human/Elf/Demon Apprentice) ----------------------------------------------
        //  🔑 THE ONLY nuker discipline since 2026-08-28 (`BL-97`). The three identities are ELEMENTS,
        //  one per race, and the LADDER IS THE ELEMENT GROWING UP (owner, 2026-08-28): the 3rd tier is
        //  the lesser form of what the 4th masters — *"apprentices -> water/fire/? adept -> ice or
        //  blizzard master / inferno master / arcane master"*. Water hardens into ice, fire swells
        //  into an inferno. `Adept → Master` rather than `Apprentice → Master`: an apprentice at 40
        //  reads junior, and `Demon Apprentice → Fire Apprentice` repeated the word.
        //  ⚠ The human is the odd one — `arcane` names a SCHOOL, not a magnitude, so there is no
        //  smaller word for it in the way water is smaller than ice. `Mana` is the raw stuff the
        //  arcane art is made of, and it is a word the player already owns off the blue bar.
        //  (`Blizzard Master` was his alternative for the elf; `Ice Master` avoids a very well-known
        //  company's name for no loss.)
        [(Discipline.Magus, Race.Human)] = new("Mana Adept",  "Arcane Master"),
        [(Discipline.Magus, Race.Elf)]   = new("Water Adept", "Ice Master"),
        [(Discipline.Magus, Race.Demon)] = new("Fire Adept",  "Inferno Master"),

        // --- HEALER (2nd: Human/Elf Priest, demon Shaman) -----------------------------------------
        //  `Light Bringer` became `Holy Priest` on 2026-08-28 — it read too close to the demon healer's
        //  `Lifebringer`, and Human Priest → Holy Priest → Holy Messenger is a ladder you can hear.
        [(Discipline.Lightbringer, Race.Human)] = new("Holy Priest",      "Holy Messenger"),
        [(Discipline.Lightbringer, Race.Elf)]   = new("Forest Whisperer", "Forest Elder"),
        [(Discipline.Lightbringer, Race.Demon)] = new("Dark Healer",      "Occultist"),

        // --- BUFFER (2nd: Human/Elf Priest, demon Shaman) -----------------------------------------
        //  ⚠ `Warlock` is NOT the IG class of that name and he is right about why: theirs is a
        //  SUMMONER, ours is a BUFFER. The test is word + same race + same ROLE, and this fails two
        //  of the three.
        [(Discipline.Warchanter, Race.Human)] = new("Doctor",      "War Doctor"),
        [(Discipline.Warchanter, Race.Elf)]   = new("Harmonist",   "War Harmonist"),
        [(Discipline.Warchanter, Race.Demon)] = new("Dreadcaller", "Warlock"),

        // --- NO ORPHANS LEFT --------------------------------------------------------------------
        //  🔑 The Tempest's and the Vanguard's rows are GONE (2026-08-28, `BL-97`). Both were rows that
        //  wore ONE name across all three races — which is precisely the tell he used to pick which of
        //  a pair should die — and both disciplines are retired. Nothing looks them up any more: they
        //  are minted into no third class, so `ClassNames.Third` is never asked about either.
        //
        //  ⚠ A retired discipline's NAME is free to be reused on a live one — `Vanguard` is a name, not
        //  an id, and nothing persists a name. Only the enum VALUES 0..13 must never move.
        //
        //  🔑 EIGHT paths × three races = the TWENTY-FOUR rows above, and every one is a real class.
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
    /// classes BY NAME, so a duplicate makes two different changes indistinguishable.
    ///
    /// <para>✅ NOTHING IS EXEMPT ANY MORE. The two exemptions this guard used to carry were the
    /// Vanguard and the Tempest, which deliberately repeated one name across their three races while
    /// they waited to be deleted. Both were retired on 2026-08-28 (`BL-97`), so every remaining row is
    /// a real class and every name in the table must be unique — which is what makes this guard worth
    /// having when the names are next reshuffled.</para></summary>
    public static IEnumerable<string> DuplicateNames()
    {
        var seen = new Dictionary<string, (Discipline D, Race R, string Tier)>();
        foreach (var ((d, race), pair) in Table)
        {
            foreach (var (name, tier) in new[] { (pair.Third, "3rd"), (pair.Fourth, "4th") })
            {
                if (seen.TryGetValue(name, out var prev))
                    yield return $"'{name}' is both {prev.R} {prev.D} {prev.Tier} and {race} {d} {tier}";
                else seen[name] = (d, race, tier);
            }
        }
    }
}
