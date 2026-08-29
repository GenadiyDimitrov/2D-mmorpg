using Game.Server.Simulation;
using Game.Shared;

// Balance matrix generator.
//
// Builds REAL Entities (same RecomputeDerived the server runs) wearing the best gear tier
// available at each level, then reports what the combat formulas actually produce against
// the mob curve of that level. The point is that every number below is MEASURED, not
// hand-derived — the magic retune is calibrated against two owner anchors:
//
//   1. A geared mage 2-3 shots a normal same-level mob.
//   2. A mage does 300-400 damage per nuke to a high-level tank.
//
// Run:  dotnet run --project tools/BalanceMatrix
//
// ⚠ WHAT THIS CANNOT TELL YOU YET (owner, 2026-07-29). The 3rd-class DISCIPLINE kits are
// PLACEHOLDERS — they re-use existing skills with a display name — and there is no 4th class at all.
// So every row from 40 up is measuring a character fighting with its SECOND-class kit, which is not
// what an endgame character will actually have. The gear, stat and mob curves below are real; the
// LEVEL 61/76/85 damage columns are a floor, not a forecast. Treat them as "this is the worst the
// endgame can be" and re-run the moment the discipline CSVs land.

// `--dump-mob-csv <path>` regenerates docs/data/mobs/mob_base_stats.csv FROM the code, so the
// documented dump can never drift from MobBaseStats. It keeps every authored column (id, name,
// level, type, speeds) and rewrites only the six stat columns. Run it after any curve edit.
if (args.Length > 0 && args[0] == "--dump-mob-csv")
{
    string path = args.Length > 1 ? args[1] : "docs/data/mobs/mob_base_stats.csv";
    var lines = File.ReadAllLines(path);

    // ⚠ THE ROSTER IS REBUILT FROM THE CATALOG, NOT JUST THE NUMBERS (fixed 2026-08-28).
    //
    // This used to walk the EXISTING FILE and refresh columns 4-9 in place, which meant the row LIST
    // was hand-maintained while only the stats were generated. So the file quietly disagreed with the
    // game in both directions at once: `rift_portling`, deleted from MobCatalog on his ruling, kept
    // its row; the four BL-79 guards, added the day before, never got one. A reference that is
    // "regenerated from the code" (CLAUDE.md) but silently keeps whatever roster it already had is
    // the same failure as 0.93.1's hand-copied interrupt table — it can never contradict you, because
    // the half that would have disagreed is the half nobody regenerates.
    //
    // The ID column is a synthetic external-style id with no source in code, so it is PRESERVED by
    // name for every row that already had one and assigned from the top for anything new. That keeps
    // existing ids stable (they are cited in docs/balance/MobCurveVsIG.md) without pretending the
    // code owns them.
    var oldIdByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    int maxId = 20000;
    for (int i = 1; i < lines.Length; i++)
    {
        if (string.IsNullOrWhiteSpace(lines[i])) continue;
        var c = lines[i].Split(',');
        if (c.Length < 2) continue;
        oldIdByName[c[1]] = c[0];
        if (int.TryParse(c[0], out int id) && id > maxId) maxId = id;
    }

    // HandPlaced creatures are excluded on purpose: the demo five and the BL-79 guards are
    // PLAYER-BUILT, so MobBaseStats says nothing true about them — printing curve numbers beside a
    // creature that never reads the curve would be worse than leaving it out. Dummies likewise.
    var roster = MobCatalog.Templates
        .Where(m => !m.Dummy && !m.HandPlaced)
        .OrderBy(m => m.Level).ThenBy(m => m.Name, StringComparer.Ordinal)
        .ToList();

    var outp = new List<string> { lines[0] };
    int added = 0, dropped = oldIdByName.Count;
    foreach (var m in roster)
    {
        int lvl = m.Level;
        string id;
        if (oldIdByName.TryGetValue(m.Name, out var known)) { id = known; dropped--; }
        else { id = (++maxId).ToString(); added++; }

        outp.Add(string.Join(',', new[]
        {
            id, m.Name, lvl.ToString(), MobCategoryLabel(m.Category),
            MobBaseStats.Hp(lvl).ToString(), MobBaseStats.Mp(lvl).ToString(),
            MobBaseStats.PDef(lvl).ToString(), MobBaseStats.MDef(lvl).ToString(),
            MobBaseStats.PAtk(lvl).ToString(), MobBaseStats.MAtk(lvl).ToString(),
            ((int)m.RunSpeed).ToString(), "333",
        }));
    }

    File.WriteAllLines(path, outp);
    Console.WriteLine($"rewrote {outp.Count - 1} rows in {path} from MobCatalog + MobBaseStats "
                    + $"({added} added, {dropped} removed)");
    return;

    static string MobCategoryLabel(MobCategory c) => c switch
    {
        MobCategory.MagicCreature => "Magic Creature",
        _ => c.ToString(),
    };
}

// `--fizzle [spellLevel] [from] [to]` prints the MAGIC FAIL curve straight out of
// StatCalculator.MagicFailChance — the shipped function, not a copy. It answers "what does a SPELL
// of level N lose against defenders of level X..Y", which is a question about LEVELS ONLY.
// 🔑 THE ATTACKER LEVEL IS THE RUNG'S LEARN LEVEL (owner 2026-08-24). GameLoopService passes
// `RungLevel(caster, def, lvl)`, so a level-80 nuker casting his @80 bolt fizzles as an 80 and the
// @40 rung in the same bar fizzles as a 40 — the caster's own level is only the fallback for a spell
// no class list owns (mob spells, scrolls, the practice dummy).
if (args.Length > 0 && args[0] == "--fizzle")
{
    int cl   = args.Length > 1 ? int.Parse(args[1]) : 74;
    int from = args.Length > 2 ? int.Parse(args[2]) : 60;
    int to   = args.Length > 3 ? int.Parse(args[3]) : 90;
    Console.WriteLine();
    Console.WriteLine($"=== MAGIC FIZZLE: a spell LEARNED AT {cl} vs defenders {from}-{to} ===");
    Console.WriteLine("  (the caster may be any level at all — only the rung's learn level is read)");
    Console.WriteLine("  A fizzle is NOT a miss: the spell still lands for damage/3 (GameLoopService),");
    Console.WriteLine("  and it still rolls the interrupt. Ceiling {0:P0}, so nothing is ever immune.",
                      StatCaps.MagicFailMax);
    Console.WriteLine();
    Console.WriteLine($"  {"def lvl",7} {"delta",6} | {"normal",8} {"tank x2",8} {"+4 mEvasion",12} {"bow x25",8}");
    for (int dl = from; dl <= to; dl++)
    {
        float plain = StatCalculator.MagicFailChance(cl, dl);
        float tank  = StatCalculator.MagicFailChance(cl, dl, defenderMod: 2f);
        float evade = StatCalculator.MagicFailChance(cl, dl, defenderFlatPoints: 4f);
        float bow   = StatCalculator.MagicFailChance(cl, dl,
                          weaponMod: StatCaps.UntrainedWeaponMagicFailMod);
        Console.WriteLine($"  {dl,7} {dl - cl,+6} | {plain,8:P0} {tank,8:P0} {evade,12:P0} {bow,8:P0}");
    }
    return;
}

// `--mana-ray [power] [level]` sizes a DamageToMp spell (the healer's Mana Ray) against real
// same-level players: the magic pipeline's own number, read against the TARGET'S MP POOL rather
// than its HP. A mana drain is only balanced relative to the pool it drains, and that pool is
// 1/8th the size of an HP bar — which is why a nuke-sized power reads as enormous here.
if (args.Length > 0 && args[0] == "--mana-ray")
{
    float power = args.Length > 1 ? float.Parse(args[1]) : 160f;
    int L = args.Length > 2 ? int.Parse(args[2]) : 74;
    // Optional HYPOTHETICAL magic resistance, as a fraction: `--mana-ray 165 74 0.20 0.30`.
    // -1 = use whatever the real Entity computes (fighters 0.00, casters 0.10 off Robe Mastery).
    float fRes = args.Length > 3 ? float.Parse(args[3]) : -1f;
    float cRes = args.Length > 4 ? float.Parse(args[4]) : -1f;

    var healer = BuildPlayer(Race.Human, BaseClass.Mage, L, healer: true);
    int hAtk = (int)healer.EffectiveMagicAttack;

    Console.WriteLine();
    Console.WriteLine($"=== MANA RAY: power {power} at level {L} (Human Cleric, wand + best gear, Spell Rune on) ===");
    Console.WriteLine($"  caster internal M.Atk {hAtk} (shown {(int)healer.EffectiveMagicAttackShown}), " +
                      $"magic crit {healer.MagicCritChance:P1} x{StatCaps.MagicCritDamageBase}");
    Console.WriteLine();
    Console.WriteLine($"  {"target",-14} {"M.Def",6} {"mRes",5} {"MaxMP",7} | {"hit",6} {"crit",6} {"fizzle",6} | " +
                      $"{"%pool",6} {"casts",6}");

    foreach (var (who, t) in Targets(L, fRes, cRes))
    {
        int hit = StatCalculator.MagicDamageFM(hAtk, 0, power,
            (int)t.EffectiveMagicDefence, t.MagicDefCoef);
        int crit = (int)(hit * StatCaps.MagicCritDamageBase);
        int fizzle = Math.Max(1, hit / 3);
        float pct = hit / (float)Math.Max(1, t.MaxMp);
        Console.WriteLine($"  {who,-14} {(int)t.EffectiveMagicDefence,6} {t.MagicDefCoef,5:F2} {t.MaxMp,7} | " +
                          $"{hit,6} {crit,6} {fizzle,6} | {pct,6:P0} {t.MaxMp / (float)Math.Max(1, hit),6:F1}");
    }

    // THE EXCHANGE RATE is the real balance metric, not the raw number: a drain is worth casting
    // only if it takes more MP off the target than it costs the caster, and it only MATTERS for as
    // long as the target cannot regenerate it back.
    Console.WriteLine();
    Console.WriteLine($"  caster pool {healer.MaxMp} MP, regen {StatCalculator.MpRegenPerSecond((int)healer.EffectiveSpt, L):F1}/s");
    Console.WriteLine($"  {"target",-14} {"regen/s",8} {"sec to undo one hit",20}");
    foreach (var (who, t) in Targets(L, fRes, cRes).Where(x => x.Item1 is "tank" or "nuker"))
    {
        int hit = StatCalculator.MagicDamageFM(hAtk, 0, power, (int)t.EffectiveMagicDefence, t.MagicDefCoef);
        float rg = StatCalculator.MpRegenPerSecond((int)t.EffectiveSpt, L);
        Console.WriteLine($"  {who,-14} {rg,8:F1} {Math.Min(hit, t.MaxMp) / rg,20:F0}");
    }

    // Same spell against the MOB curve, where his "half effect on monsters" (PveDamageMult 0.5)
    // applies. A mob's MP pool is MobBaseStats.Mp, not the player curve.
    Console.WriteLine();
    Console.WriteLine($"  vs MOBS (x0.5 PveDamageMult): {"lvl",4} {"M.Def",6} {"MP",6} {"hit",6} {"%pool",6}");
    foreach (int ml in new[] { L - 5, L, L + 5 })
    {
        int hit = (int)(StatCalculator.MagicDamageFM(hAtk, 0, power, MobBaseStats.MDef(ml), 1f) * 0.5f);
        Console.WriteLine($"  {"",28} {ml,4} {MobBaseStats.MDef(ml),6} {MobBaseStats.Mp(ml),6} {hit,6} " +
                          $"{hit / (float)Math.Max(1, MobBaseStats.Mp(ml)),6:P0}");
    }

    // What power lands a chosen fraction of each target's pool, so the number can be CHOSEN
    // rather than guessed: power scales the damage linearly, so this is an exact inversion.
    Console.WriteLine();
    Console.WriteLine("  POWER NEEDED for a given share of the target's MP pool (per cast):");
    Console.WriteLine($"  {"target",-14} {"5%",7} {"10%",7} {"15%",7} {"20%",7} {"25%",7}");
    foreach (var (who, t) in Targets(L, fRes, cRes))
    {
        int at1 = StatCalculator.MagicDamageFM(hAtk, 0, 100f, (int)t.EffectiveMagicDefence, t.MagicDefCoef);
        string Row(float share) => $"{share * t.MaxMp / (at1 / 100f),7:F0}";
        Console.WriteLine($"  {who,-14} {Row(0.05f)} {Row(0.10f)} {Row(0.15f)} {Row(0.20f)} {Row(0.25f)}");
    }
    // ---- THE THREE CANDIDATE MODELS (owner, 2026-08-20) ----
    // A = today's engine: the magic pipeline's own number, taken off MP (K·power·√mAtk / mDef·mRes).
    // B = FLAT: the authored power IS the MP drained, divided by the target's magic resistance
    //     (resist protects, the same direction as every other defence in the game).
    // C = "MANA PUNISHMENT": flat, but MULTIPLIED by magic resistance — the more the target armours
    //     himself against magic damage, the more his mana is exposed. Deliberately inverted.
    // D = POOL SHARE: the drain is a percentage of the target's OWN max MP (power/10, so his authored
    //     ladder 120..153 reads as 12.0%..15.3%). Pool-independent BY CONSTRUCTION — the only model of
    //     the four in which one authored number means the same thing to a 696-MP tank and a 3158-MP
    //     healer, because it is the 4.5x pool gap, not the defence, that makes A and B lopsided.
    // 🔴 E AND E' ARE MEASUREMENT ONLY — NOT SHIPPED. The owner saw this table on 2026-08-21 and ruled
    //    *"leave it as is"*: the engine keeps model D. These two columns stay so the comparison does
    //    not have to be re-derived if the question comes back.
    // E = THE IG FORMULA, verbatim (owner, 2026-08-21): √mAtk · power · (targetMaxMp / 97) / targetMDef.
    //     Read it carefully and it is MODEL D WITH THE MAGIC PIPELINE BOLTED BACK ON: the (maxMp/97)
    //     term is the same pool-proportionality that made D fair, and √mAtk/mDef is our own magic
    //     ratio. So IG did not choose between "a share" and "a damage number" — it multiplied them.
    //     ⚠ Its 97 is a constant on IG'S M.Atk scale, so column E's SIZE is meaningless to us; only
    //     its SHAPE (how the drain moves with gear and with the target's M.Def) transfers.
    // E' = that shape renormalised onto our numbers: model D times (√mAtk / mDef) measured against a
    //     reference pair, so a baseline healer hitting a baseline same-level target drains exactly D
    //     and everything above/below that is gear and defence. This is the shippable form of E.
    float mDefRef = Targets(L, fRes, cRes).Average(x => x.Item2.EffectiveMagicDefence);
    float igRef   = MathF.Sqrt(hAtk) / mDefRef;    // the baseline healer against a baseline target
    Console.WriteLine();
    Console.WriteLine($"  ---- MODELS at power {power} ---- (drain per cast / % of pool / casts to zero)");
    Console.WriteLine($"  {"target",-10} {"pool",5} {"mRes",5} | {"A pipeline",-19} {"B flat/mRes",-19} " +
                      $"{"C flat x mRes",-19} {"D pool share",-19} {"E IG raw",-19} {"E' IG renormalised",-19}");
    foreach (var (who, t) in Targets(L, fRes, cRes))
    {
        int a = StatCalculator.MagicDamageFM(hAtk, 0, power, (int)t.EffectiveMagicDefence, t.MagicDefCoef);
        int b = (int)(power / t.MagicDefCoef);
        int c = (int)(power * t.MagicDefCoef);
        int d = StatCalculator.ManaDrain(t.MaxMp, power);   // the SHIPPED formula, not a copy of it
        int e = (int)(MathF.Sqrt(hAtk) * power * (t.MaxMp / 97f) / Math.Max(1f, t.EffectiveMagicDefence));
        int e2 = (int)(d * (MathF.Sqrt(hAtk) / Math.Max(1f, t.EffectiveMagicDefence)) / igRef);
        string Cell(int x) => $"{x,4} {x / (float)t.MaxMp,5:P0} {t.MaxMp / (float)Math.Max(1, x),4:F1}x";
        Console.WriteLine($"  {who,-10} {t.MaxMp,5} {t.MagicDefCoef,5:F2} | {Cell(a),-19} {Cell(b),-19} " +
                          $"{Cell(c),-19} {Cell(d),-19} {Cell(e),-19} {Cell(e2),-19}");
    }

    // ---- WHAT THE IG SHAPE ACTUALLY BUYS: how far the drain swings with M.Atk and with M.Def.
    // The whole question is whether (√mAtk / mDef) has enough spread here to be worth the loss of
    // "one number means one thing". Measure both ends instead of assuming.
    Console.WriteLine();
    Console.WriteLine("  ---- E' SENSITIVITY (multiplier on the D share) ----");
    Console.Write($"  caster M.Atk: ");
    foreach (float mult in new[] { 0.5f, 0.75f, 1f, 1.5f, 2f, 3f })
        Console.Write($"x{mult:F2} gear -> x{MathF.Sqrt(hAtk * mult) / MathF.Sqrt(hAtk),5:F2}   ");
    Console.WriteLine();
    Console.Write($"  target M.Def: ");
    foreach (var (who, t) in Targets(L, fRes, cRes))
        Console.Write($"{who} {t.EffectiveMagicDefence,4:F0} -> x{mDefRef / Math.Max(1f, t.EffectiveMagicDefence),5:F2}   ");
    Console.WriteLine();

    // ---- CAN THE HEALER AFFORD IT? ----
    // The metric that actually decides this skill: a full drain is only a "strategy move" if it costs
    // the caster a real share of his OWN bar. mpCost 90 is his authored level-70 rung; x3 is his ask.
    Console.WriteLine();
    Console.WriteLine("  ---- COST OF A FULL DRAIN (share of the healer's OWN bar to zero the target) ----");
    foreach (var (model, drain) in new (string, Func<Entity, int>)[]
    {
        ("A pipeline", t => StatCalculator.MagicDamageFM(hAtk, 0, power, (int)t.EffectiveMagicDefence, t.MagicDefCoef)),
        ("B flat    ", t => (int)(power / t.MagicDefCoef)),
        ("D share   ", t => StatCalculator.ManaDrain(t.MaxMp, power)),
    })
    foreach (int cost in new[] { 90, 270 })
    {
        Console.Write($"  {model} mpCost {cost,3} (x{cost / 90f:F0}):  ");
        foreach (var (who, t) in Targets(L, fRes, cRes))
            Console.Write($"{who} {t.MaxMp / (float)Math.Max(1, drain(t)) * cost / healer.MaxMp,5:P0}   ");
        Console.WriteLine();
    }
    Console.WriteLine($"  (100% = the healer empties his entire {healer.MaxMp}-MP bar to fully drain that target.)");
    Console.WriteLine();
    return;
}

if (args.Length > 0 && args[0] == "--warchanter")
{
    int L = args.Length > 1 ? int.Parse(args[1]) : 90;
    // Optional demon ATK override sweep: `--warchanter 90 31 38 41 44 47`
    var sweep = args.Skip(2).Select(int.Parse).ToArray();
    if (sweep.Length == 0) sweep = new[] { 31, 38, 41, 44, 47 };

    Console.WriteLine();
    Console.WriteLine($"=== THE THREE WARCHANTERS at level {L} — the race split measured, not asserted ===");
    Console.WriteLine("  Each race in the weapon and armour ITS OWN masteries train (his kit split):");
    Console.WriteLine("    Human  heavy + 1H mace + SHIELD   Demon  heavy + 2H maul   Elf  light + bow");
    Console.WriteLine("  Best-for-tier gear, every Warchanter skill learnable by this level, War/Spell Rune ON,");
    Console.WriteLine("  NO buffs cast (this is the naked character sheet he compared).");
    Console.WriteLine();
    Console.WriteLine($"  {"race",-7} {"CON",4} {"ATK",4} {"WIT",4} {"AGI",4} {"SPT",4} | " +
                      $"{"P.Atk",7} {"M.Atk",7} {"P.Def",7} {"M.Def",7} {"acc",5} {"eva",5} | {"HP",7} {"MP",7}");
    foreach (var (race, label) in new[] { (Race.Human, "human"), (Race.Demon, "demon"), (Race.Elf, "elf") })
    {
        var e = BuildWarchanter(race, L);
        var s = StatCalculator.GetBaseStats(race, BaseClass.Mage);
        Console.WriteLine($"  {label,-7} {s.Con,4} {s.Atk,4} {s.Wit,4} {s.Agi,4} {s.Spt,4} | " +
                          $"{(int)e.EffectiveAttack,7} {(int)e.EffectiveMagicAttackShown,7} " +
                          $"{(int)e.EffectiveDefence,7} {(int)e.EffectiveMagicDefence,7} " +
                          $"{e.Accuracy,5} {(int)e.EffectiveEvasion,5} | {e.MaxHp,7} {e.MaxMp,7}");
    }

    Console.WriteLine();
    Console.WriteLine($"=== WHAT AN ORK ATK RAISE ACTUALLY BUYS (level {L}) ===");
    Console.WriteLine("  The demon's ATK is the ONLY thing changed; his gear, kit and every other stat stand still.");
    Console.WriteLine("  'vs human' is the demon's P.Atk over the human's — who also carries a SHIELD the demon has not.");
    Console.WriteLine();
    var human = BuildWarchanter(Race.Human, L);
    var elf = BuildWarchanter(Race.Elf, L);
    int hP = (int)human.EffectiveAttack, hD = (int)human.EffectiveDefence;
    int eP = (int)elf.EffectiveAttack;
    Console.WriteLine($"  reference: human P.Atk {hP} / P.Def {hD}   elf P.Atk {eP} / P.Def {(int)elf.EffectiveDefence}");
    Console.WriteLine();
    Console.WriteLine($"  {"demon ATK",8} {"P.Atk",7} {"M.Atk",7} | {"vs human",9} {"vs elf",8}");
    foreach (int a in sweep)
    {
        var e = BuildWarchanter(Race.Demon, L, atkOverride: a);
        int p = (int)e.EffectiveAttack;
        Console.WriteLine($"  {a,8} {p,7} {(int)e.EffectiveMagicAttackShown,7} | " +
                          $"{(p - hP) * 100f / hP,8:+0.0;-0.0}% {(p - eP) * 100f / eP,7:+0.0;-0.0}%");
    }

    // ⚠ ATK IS ONE STAT PER RACE+BASECLASS. Raising it for the buffer raises it for every demon MAGE
    // — the Shaman and the Witch too. The nuker is the one that could be broken by it, so measure
    // him rather than reasoning about him: same staff, same robe, only the base ATK moves.
    Console.WriteLine();
    Console.WriteLine($"=== THE SIDE EFFECT: the ORK NUKER (staff + robe), who shares the same ATK ===");
    var humanNuke = BuildWarchanter(Race.Human, L, disc: Discipline.Magus);
    int hM = (int)humanNuke.EffectiveMagicAttackShown;
    Console.WriteLine($"  reference: human Magus M.Atk {hM}, WIT {StatCalculator.GetBaseStats(Race.Human, BaseClass.Mage).Wit} " +
                      $"(cast x{humanNuke.EffectiveCastSpeedMultiplier:F2}, magic crit {humanNuke.MagicCritChance:P1})");
    Console.WriteLine();
    Console.WriteLine($"  {"demon ATK",8} {"M.Atk",7} | {"vs human",9} | {"cast",6} {"m.crit",7}");
    foreach (int a in sweep)
    {
        var n = BuildWarchanter(Race.Demon, L, atkOverride: a, disc: Discipline.Magus);
        int m = (int)n.EffectiveMagicAttackShown;
        Console.WriteLine($"  {a,8} {m,7} | {(m - hM) * 100f / hM,8:+0.0;-0.0}% | " +
                          $"x{n.EffectiveCastSpeedMultiplier,5:F2} {n.MagicCritChance,7:P1}");
    }
    return;
}
// `--buffs` — the buff census (see BuffCensus at the bottom of this file).
if (args.Length > 0 && args[0] == "--buffs") { BuffCensus.Run(); return; }

// `--hpcurve` — the PLAYER HP CURVE against IG's own per-class tables and the three anchors the
// owner set on 2026-08-27 (tank@40 CON43 = 2380, buffer@40 CON31 = 1180, knight@80 CON43 = 9840).
// Reads StatCalculator directly, so it measures the SHIPPED curve, not a re-derivation.
if (args.Length > 0 && args[0] == "--hpcurve")
{
    Console.WriteLine("=== PLAYER HP CURVE vs IG (base = pre-CON level term; naked = x ConHpModifier) ===");
    Console.WriteLine("  Reference sheets: fighters = human CON 43 | nuker/healer = human CON 27 | buffer = demon CON 31");
    Console.WriteLine();

    // IG's own per-class base tables, as supplied. null = not supplied for that level.
    var igTank = new Dictionary<int, double> { [1] = 43.8, [10] = 107, [20] = 253.7, [40] = 1184.1, [50] = 1801, [60] = 2607, [70] = 3611.9, [80] = 4895.5 };
    var igBuff = new Dictionary<int, double> { [1] = 49.2, [10] = 108.5, [20] = 240, [40] = 907.7, [50] = 1438.5, [60] = 2115.4, [70] = 2938.5, [80] = 3938.5 };

    void Track(string name, Race race, BaseClass cls, Archetype? arch, Discipline? disc, int con,
               Dictionary<int, double>? ig)
    {
        Console.WriteLine($"  --- {name}  ({race} {cls}, CON {con}) ---");
        Console.WriteLine("   lvl      base     naked |    IG base     err");
        foreach (var L in new[] { 1, 10, 20, 40, 50, 60, 70, 80, 85 })
        {
            float b = StatCalculator.HpBase(race, cls, L, arch, disc);
            int hp = StatCalculator.MaxHp(con, L, race, cls, arch, disc);
            string igCol = "         -       -";
            if (ig != null && ig.TryGetValue(L, out var v))
                igCol = $"{v,10:0} {(b / v - 1),7:+0.0%;-0.0%;0.0%}";
            Console.WriteLine($"   {L,3} {b,9:0} {hp,9} | {igCol}");
        }
        Console.WriteLine();
    }

    Track("TANK", Race.Human, BaseClass.Fighter, Archetype.Tank, Discipline.Bulwark, 43, igTank);
    Track("WARRIOR", Race.Human, BaseClass.Fighter, Archetype.Warrior, Discipline.Ravager, 43, null);
    Track("ROGUE", Race.Human, BaseClass.Fighter, Archetype.Rogue, Discipline.Nullblade, 43, null);
    Track("BUFFER (Warchanter)", Race.Demon, BaseClass.Mage, Archetype.Healer, Discipline.Warchanter, 31, igBuff);
    Track("HEALER (Lightbringer)", Race.Human, BaseClass.Mage, Archetype.Healer, Discipline.Lightbringer, 27, null);
    Track("NUKER", Race.Human, BaseClass.Mage, Archetype.Nuker, Discipline.Magus, 27, null);

    Console.WriteLine("  --- THE OWNER'S THREE ANCHORS ---");
    int a1 = StatCalculator.MaxHp(43, 40, Race.Human, BaseClass.Fighter, Archetype.Tank, Discipline.Bulwark);
    int a2 = StatCalculator.MaxHp(31, 40, Race.Demon, BaseClass.Mage, Archetype.Healer, Discipline.Warchanter);
    int a3 = StatCalculator.MaxHp(43, 80, Race.Human, BaseClass.Fighter, Archetype.Tank, Discipline.Bulwark);
    Console.WriteLine($"   tank   @40 CON43 = {a1,6}   target 2380   {(a1 / 2380.0 - 1),7:+0.0%;-0.0%;0.0%}");
    Console.WriteLine($"   buffer @40 CON31 = {a2,6}   target 1180   {(a2 / 1180.0 - 1),7:+0.0%;-0.0%;0.0%}");
    Console.WriteLine($"   knight @80 CON43 = {a3,6}   target 9840   {(a3 / 9840.0 - 1),7:+0.0%;-0.0%;0.0%}");
    Console.WriteLine();

    Console.WriteLine("  --- THE CLASS-CHANGE STEP: what taking a discipline at 40 is worth ---");
    foreach (var L in new[] { 39, 40, 50, 80 })
    {
        int pre = StatCalculator.MaxHp(31, L, Race.Demon, BaseClass.Mage, Archetype.Healer, null);
        int wc = StatCalculator.MaxHp(31, L, Race.Demon, BaseClass.Mage, Archetype.Healer, Discipline.Warchanter);
        int lb = StatCalculator.MaxHp(31, L, Race.Demon, BaseClass.Mage, Archetype.Healer, Discipline.Lightbringer);
        Console.WriteLine($"   L{L,-3} no discipline {pre,6} | Warchanter {wc,6} ({(wc / (double)pre - 1),6:+0.0%;-0.0%;0.0%}) | Lightbringer {lb,6}");
    }
    Console.WriteLine();

    Console.WriteLine("  --- ORDERING AT 80 (base HP; the owner's rule: nuker=healer < buffer < rogue < warrior < tank) ---");
    Console.WriteLine($"   nuker {StatCalculator.HpBase(Race.Human, BaseClass.Mage, 80, Archetype.Nuker, Discipline.Magus),6:0}"
        + $" | healer {StatCalculator.HpBase(Race.Human, BaseClass.Mage, 80, Archetype.Healer, Discipline.Lightbringer),6:0}"
        + $" | buffer {StatCalculator.HpBase(Race.Human, BaseClass.Mage, 80, Archetype.Healer, Discipline.Warchanter),6:0}"
        + $" | rogue {StatCalculator.HpBase(Race.Human, BaseClass.Fighter, 80, Archetype.Rogue, Discipline.Nullblade),6:0}"
        + $" | warrior {StatCalculator.HpBase(Race.Human, BaseClass.Fighter, 80, Archetype.Warrior, Discipline.Ravager),6:0}"
        + $" | tank {StatCalculator.HpBase(Race.Human, BaseClass.Fighter, 80, Archetype.Tank, Discipline.Bulwark),6:0}");
    Console.WriteLine();
    return;
}

// `--mpregen` — the MP ECONOMY: spell-spam drain against natural and buffed regen, under BOTH the
// model that ships today and the owner's proposed one (2026-08-26). Nothing here changes the engine.
// `--hpregen` - the HP ECONOMY: regen against the potion throughput that really replaces HP,
// and against a level-appropriate mob's damage. `BL-92` part two. Nothing here changes the engine.
if (args.Length > 0 && args[0] == "--hpregen")
{
    HpEconomy(args.Skip(1).Select(int.Parse).ToArray());
    return;
}

if (args.Length > 0 && args[0] == "--mpregen")
{
    MpEconomy(args.Skip(1).Select(int.Parse).ToArray());
    return;
}

// `--mpdrain` - the MP POTION question (2026-08-27): the NAMED spells' drain against regen, in
// flat MP/s, for all three races across 20-80. Nothing here changes the engine.
if (args.Length > 0 && args[0] == "--mpdrain")
{
    MpDrain(args.Skip(1).Select(int.Parse).ToArray());
    return;
}

// `--mpnpc` - the FULLY NPC-BUFFED mage at 74, all three roles (2026-08-27).
if (args.Length > 0 && args[0] == "--mpnpc")
{
    MpNpc();
    return;
}

// `--stacks` - what every item in the catalog stacks to, and what a farm trip costs in ROWS (0.93.0).
if (args.Length > 0 && args[0] == "--stacks")
{
    Stacks();
    return;
}

// `--mpcase` - HIS level-43 demon healer, measured as he actually plays it (2026-08-27).
if (args.Length > 0 && args[0] == "--mpcase")
{
    MpCase();
    return;
}

// `--guards` — BL-79: is a guard actually calibrated to the player he is supposed to stop?
//
// His target, 2026-08-27: "they should be strong enough so a player attacking them (pvp-on must be
// on) and when the guard retaliate a player has its hands full .. to match a 80 lvl player S grade
// equip (no nenchanted)". That is a MEASURABLE claim, so it gets measured rather than asserted —
// the 0.93.1 lesson, where a hand-copied literal let a tool agree with itself forever.
//
// Both sides are built through the REAL paths: the guard by MobBuild/ApplyMobBuild exactly as
// BuildMob does at spawn, the player by BuildPlayer + the same S-grade Epic pieces. Nothing here
// restates a number that lives in MobCatalog.
if (args.Length > 0 && args[0] == "--guards")
{
    Console.WriteLine("=== BL-79 GUARDS vs THE REFERENCE PLAYER ===");
    Console.WriteLine("  His target: \"to match a 80 lvl player S grade equip (no nenchanted)\".");
    Console.WriteLine("  Reference  = level 80 human, S grade (t80) Epic, +0 — tank and warrior.");
    Console.WriteLine("  ⚠ Block is NOT modelled by PhysDps, so a SHIELDED side lives longer than shown.");
    Console.WriteLine();

    static Entity GuardEntity(string mobId)
    {
        var t = MobCatalog.Get(mobId);
        var e = new Entity { Name = t.Name, Kind = EntityKind.Mob, Level = t.Level, MobTypeId = mobId };
        if (t.Build is MobBuild b) e.ApplyMobBuild(b);

        // ⚠ THE HELD RUNE MUST BE APPLIED HERE OR THE COMPARISON IS A LIE. A creature's rune buff is
        // put up by GameLoopService at SPAWN (it has no clock and no login, so the buff is applied once
        // and never expires) — ApplyMobBuild only puts the rune in its inventory. BuildPlayer gives the
        // reference player exactly this buff, so measuring a runed player against an un-runed guard
        // hands the player a silent +100% P.Atk and reads as "the guard is half as strong as it is".
        if (t.Build is MobBuild rb && rb.Held.Length > 0
            && ItemCatalog.Get(rb.Held) is { RuneBuffSkillId: { Length: > 0 } runeBuffId }
            && SkillCatalog.Get(runeBuffId) is SkillDef runeSkill)
            e.Buffs.Add(new Game.Server.Simulation.BuffInstance
            {
                Effect = runeSkill.Effect, Magnitudes = runeSkill.Magnitudes,
                TicksRemaining = int.MaxValue, Name = runeSkill.Name, Key = runeSkill.BuffKey,
            });

        e.RecomputeDerived();
        e.Hp = e.MaxHp;
        return e;
    }

    static Entity RefPlayer(bool warrior)
    {
        var e = BuildPlayer(Race.Human, BaseClass.Fighter, 80, warrior: warrior);

        // ⚠ STRIP WHAT BuildPlayer ALREADY EQUIPPED, OR THE REFERENCE WEARS TWO FULL SETS.
        // BuildPlayer dresses its character in best-for-tier gear (the MYTHIC base-tier pieces,
        // `sword1h_t80` etc.), and every piece added afterwards is ALSO flagged Equipped — nothing in
        // this tool enforces one item per slot. Adding the S+0 Epic set on top therefore produced a
        // player wearing 22 equipped items whose stats were roughly double a real one's, and it read
        // exactly like "a guard in identical gear is 5x weaker than a player". It was not: the player
        // was double-geared. The guards were closer to his target than the first measurement claimed.
        // The rune BUFF that BuildPlayer applies is deliberately kept — it is not inventory, and the
        // guards hold one too.
        e.Inventory.Clear();

        foreach (var (id, ench) in new[]
                 {
                     ($"{(warrior ? "sword2h" : "sword1h")}_t80_epic", 0), ("heavy_t80_epic", 0),
                     ("helm_t80_epic", 0), ("gloves_t80_epic", 0), ("boots_t80_epic", 0),
                     ("necklace_t80_epic", 0), ("ring_t80_epic", 0), ("ring_t80_epic", 0),
                     ("earring_t80_epic", 0), ("earring_t80_epic", 0),
                 })
            EquipEnchanted(e, id, ench);
        if (!warrior) EquipEnchanted(e, "shield_t80", 0);
        e.RecomputeDerived();
        e.Hp = e.MaxHp;
        return e;
    }

    var contenders = new (string Label, Entity E)[]
    {
        ("PLAYER tank   (S+0, shield)", RefPlayer(warrior: false)),
        ("PLAYER warrior (S+0, 2H)",    RefPlayer(warrior: true)),
        ("guard_town_tank",             GuardEntity("guard_town_tank")),
        ("guard_town_archer",           GuardEntity("guard_town_archer")),
        ("guard_field_tank",            GuardEntity("guard_field_tank")),
        ("guard_field_archer",          GuardEntity("guard_field_archer")),
    };

    Console.WriteLine($"{"",-30} {"Lv",3} {"HP",8} {"P.Atk",7} {"P.Def",7} {"M.Def",7} {"Eva",5} {"Acc",5}");
    foreach (var (label, e) in contenders)
        Console.WriteLine($"{label,-30} {e.Level,3} {e.MaxHp,8} {(int)e.EffectiveAttack,7} "
                        + $"{(int)e.EffectiveDefence,7} {(int)e.EffectiveMagicDefence,7} "
                        + $"{(int)e.EffectiveEvasion,5} {(int)e.Accuracy,5}");
    Console.WriteLine();

    Console.WriteLine("--- TIME TO KILL, both directions (seconds; '-' = cannot hurt it) ---");
    Console.WriteLine($"{"matchup",-46} {"player kills guard",19} {"guard kills player",19}");
    foreach (var (pLabel, p) in contenders.Take(2))
        foreach (var (gLabel, g) in contenders.Skip(2))
        {
            float pDps = PhysDps(p, g), gDps = PhysDps(g, p);
            string pT = pDps > 0.01f ? (g.MaxHp / pDps).ToString("0") + "s" : "-";
            string gT = gDps > 0.01f ? (p.MaxHp / gDps).ToString("0") + "s" : "-";
            Console.WriteLine($"{pLabel + "  vs  " + gLabel,-46} {pT,19} {gT,19}");
        }

    Console.WriteLine();
    Console.WriteLine("  READ IT LIKE THIS: 'hands full' is the TOWN pair — the player should win, but");
    Console.WriteLine("  not comfortably, and the two columns should be within sight of each other.");
    Console.WriteLine("  The FIELD pair (90, +16, War Rune) is meant to be a wall, not a duel: if the");
    Console.WriteLine("  right column is far shorter than the left, that is the design, not a bug.");
    Console.WriteLine();

    Console.WriteLine("--- WHERE THE GUARDS STAND ---");
    foreach (var z in WorldPlan.GuardZones)
        Console.WriteLine($"  ({z.X,6:0},{z.Y,6:0}) r{z.Radius,-4:0} Lv{z.MinLevel,-3} "
                        + $"respawn {z.RespawnSeconds,4:0}s · {string.Join(" + ", z.MobTypes)}");
    Console.WriteLine($"  {WorldPlan.GuardZones.Length} posts, "
                    + $"{WorldPlan.GuardZones.Sum(z => z.TotalCount)} guards in the world.");
    return;
}

// `--goldflow` - BL-23: what an hour of farming EARNS against what it BURNS in potions and rune
// upkeep, level band by level band (2026-08-27, his own framing of the coin-curve question).
if (args.Length > 0 && args[0] == "--goldflow")
{
    GoldFlow();
    return;
}

// The four same-level PvP targets a drain is judged against. One list, so every table below
// measures the same characters.
//
// ⚠ fighterRes/casterRes OVERRIDE the measured MagicResist with a HYPOTHETICAL one (owner,
// 2026-08-20: *"add a tanks mresist as 20% .. mages mresist as 30%"* — explicitly NOT authored
// values). Today's real spread is 0% on fighters and 10% on casters, which is why the drain
// models barely separate; these let the question "what if mRes actually had a spread" be
// measured instead of guessed. Set after RecomputeDerived, which zeroes and rebuilds the field.
static (string, Entity)[] Targets(int L, float fighterRes = -1f, float casterRes = -1f)
{
    var list = new (string, Entity)[]
    {
        ("tank",     BuildPlayer(Race.Human, BaseClass.Fighter, L)),
        ("champion", BuildPlayer(Race.Human, BaseClass.Fighter, L, warrior: true)),
        ("nuker",    BuildPlayer(Race.Human, BaseClass.Mage, L)),
        ("healer",   BuildPlayer(Race.Human, BaseClass.Mage, L, healer: true)),
    };
    foreach (var (_, e) in list)
    {
        float over = e.BaseClass == BaseClass.Fighter ? fighterRes : casterRes;
        if (over >= 0f) e.MagicResist = over;
    }
    return list;
}

int[] levels = { 20, 40, 52, 61, 76, 85 };

Console.WriteLine();
Console.WriteLine("!! 3rd/4th-class kits are placeholders — levels 61+ measure a 2nd-class kit.");
Console.WriteLine("!! Endgame damage below is a FLOOR, not a forecast. See the header comment.");
Console.WriteLine("=== MOB CURVE ===");
Console.WriteLine($"{"Lvl",4} {"HP",8} {"P.Def",7} {"M.Def",7} {"P.Atk",7} {"M.Atk",7}");
foreach (int L in Enumerable.Range(1, 85).Where(l => l == 1 || l % 10 == 0 || l == 85))
    Console.WriteLine($"{L,4} {MobBaseStats.Hp(L),8} {MobBaseStats.PDef(L),7} {MobBaseStats.MDef(L),7} " +
                      $"{MobBaseStats.PAtk(L),7} {MobBaseStats.MAtk(L),7}");

Console.WriteLine();
Console.WriteLine("=== HIT / MISS vs a SAME-LEVEL mob (accuracy = AGI + level, 1 point = 1%) ===");
Console.WriteLine("  'naked' = base stats only, no gear and no passives. 'geared' = best gear for tier + kit.");
Console.WriteLine("  A same-AGI, same-level pair must sit at the 5% base BOTH ways — that is the whole point of");
Console.WriteLine("  the level term. Before it, a player's accuracy was flat for life while the mob's grew +1/level:");
Console.WriteLine("  they crossed at 20 and a naked level-90 fighter missed 75% while the mob never missed at all.");
Console.WriteLine($"{"Lvl",4} {"mob A/E",8} | {"naked",6} {"nk miss",8} {"mob miss",9} | " +
                  $"{"gear A",7} {"gear E",7} {"miss",6} {"mob miss",9}");

foreach (int L in Enumerable.Range(1, 90).Where(l => l == 1 || l % 10 == 0))
{
    var mob = BuildMobEntity(L);
    var naked = BuildNaked(Race.Human, BaseClass.Fighter, L);
    var geared = BuildPlayer(Race.Human, BaseClass.Fighter, L, warrior: true);

    Console.WriteLine($"{L,4} {mob.Accuracy + "/" + (int)mob.EffectiveEvasion,8} | " +
                      $"{naked.Accuracy,6} {Pct(Miss(naked, mob)),8} {Pct(Miss(mob, naked)),9} | " +
                      $"{geared.Accuracy,7} {(int)geared.EffectiveEvasion,7} " +
                      $"{Pct(Miss(geared, mob)),6} {Pct(Miss(mob, geared)),9}");
}

// -----------------------------------------------------------------------------------------------
// E1-E3: the playtest-20 "unlimited farm" board. Three characters ran an uncapped offline farm and
// only the ROGUE never died: the mob simply cannot hit him. E1 measures that, E2 measures the
// champion's complaint (defence "on par with the robe", evasion below the mob's accuracy) and E3
// measures the nuker's MP economy (why he sits out of mana). All three are the same question asked
// of three sheets, so they share one roster.
// -----------------------------------------------------------------------------------------------

/// <summary>The six sheets the offline farm actually puts in a field, at one level. Built once and
/// reused by E1/E2 so a row can never mean a different character in two tables.</summary>
static (string Name, Entity E)[] FarmRoster(int level)
{
    int t = GearTier(level);
    var rogueBow = BuildRogue(level);
    rogueBow.Inventory.RemoveAll(i => ItemCatalog.Get(i.DefId)?.Slot == EquipSlot.Weapon);
    Equip(rogueBow, $"bow_t{t}");
    rogueBow.RecomputeDerived();

    // The champion is measured TWO-HANDED. BuildPlayer dresses every fighter in 1H + shield, which
    // for a warrior measures a character who never pays — and never collects — his Two-Hand Weapon
    // Mastery: the +30/50% P.Atk, the crit-damage flat, and the DefencePct −0.10 / Evasion −3 the
    // owner is asking about are ALL gated on WeaponType.TwoHanded. A 1H champion is not a champion.
    var champ = BuildPlayer(Race.Human, BaseClass.Fighter, level, warrior: true);
    champ.Inventory.RemoveAll(i => ItemCatalog.Get(i.DefId) is { } d
        && (d.Slot == EquipSlot.Weapon || d.Slot == EquipSlot.Shield));
    Equip(champ, $"sword2h_t{t}");
    champ.RecomputeDerived();

    return new (string, Entity)[]
    {
        ("mob (same level)",   BuildMobEntity(level)),
        ("tank  1H+shield",    BuildPlayer(Race.Human, BaseClass.Fighter, level)),
        ("champion 2H sword",  champ),
        ("rogue duals+light",  BuildRogue(level)),
        ("rogue bow+light",    rogueBow),
        ("nuker robe",         BuildPlayer(Race.Human, BaseClass.Mage, level)),
    };
}

/// <summary>The same roster with the NEWBIE BUFFER's one-hour set on every player (the state the
/// owner actually farms in). The mob row is left bare — mobs get no buffer.</summary>
static (string Name, Entity E)[] FarmRosterBuffed(int level)
{
    var r = FarmRoster(level);
    foreach (var (_, e) in r) if (e.Kind == EntityKind.Player) ApplyNpcBuffs(e);
    return r;
}

Console.WriteLine();
Console.WriteLine("=== E1: ACCURACY vs EVASION — the whole board (why only the rogue never dies) ===");
{
    Console.WriteLine("  The resolver is one line: miss = 5% + (defender EVASION − attacker ACCURACY) x 1%, clamped");
    Console.WriteLine("  to [5%, 95%], and only THEN the class floors. So the spread is the entire mechanic, and the");
    Console.WriteLine("  rogue's EvadeFloor (10/20/30%) never binds while the spread alone already beats it.");
    Console.Write($"  {"spread",-10}");
    for (int d = -10; d <= 60; d += 5) Console.Write($"{d,6}");
    Console.WriteLine();
    Console.Write($"  {"-> miss",-10}");
    for (int d = -10; d <= 60; d += 5)
        Console.Write($"{Pct(Math.Clamp(StatCaps.AvoidBase + d * StatCaps.AvoidStatSlope,
            StatCaps.AvoidBase, StatCaps.AvoidSoftCeil)),6}");
    Console.WriteLine();
    Console.WriteLine();

    Console.WriteLine("  --- A. the sheets: ACC / EVA, bare and with the newbie buffer's 1h set ---");
    Console.WriteLine($"  {"Lvl",3} {"who",-20} {"acc",5} {"eva",5} | {"acc+B",6} {"eva+B",6} | {"floor",6}");
    foreach (int L in new[] { 20, 28, 36, 44, 52 })
    {
        var bare = FarmRoster(L);
        var buff = FarmRosterBuffed(L);
        for (int i = 0; i < bare.Length; i++)
            Console.WriteLine($"  {L,3} {bare[i].Name,-20} {bare[i].E.Accuracy,5} {(int)bare[i].E.EffectiveEvasion,5} | " +
                $"{buff[i].E.Accuracy,6} {(int)buff[i].E.EffectiveEvasion,6} | {Pct(bare[i].E.EvadeFloor),6}");
        Console.WriteLine();
    }

    Console.WriteLine("  --- B. what a SAME-LEVEL MOB does to each sheet (the offline-farm question) ---");
    Console.WriteLine("  'mob miss' is how often the mob's swing is dodged. 'player miss' is the other direction.");
    Console.WriteLine($"  {"Lvl",3} {"who",-20} {"spread",7} {"mob miss",9} | {"spread'",8} {"plr miss",9}");
    foreach (int L in new[] { 20, 28, 36, 44, 52 })
    {
        var r = FarmRosterBuffed(L);
        var mob = r[0].E;
        foreach (var (name, e) in r.Skip(1))
            Console.WriteLine($"  {L,3} {name,-20} {(int)e.EffectiveEvasion - mob.Accuracy,7} {Pct(Miss(mob, e)),9} | " +
                $"{(int)mob.EffectiveEvasion - e.Accuracy,8} {Pct(Miss(e, mob)),9}");
        Console.WriteLine();
    }

    Console.WriteLine("  --- C. the full cross matrix at level 36 (BUFFED), attacker row -> defender column ---");
    {
        var r = FarmRosterBuffed(36);
        Console.Write($"  {"attacker \\ defender",-20}");
        foreach (var (n, _) in r) Console.Write($"{n.Split(' ')[0],9}");
        Console.WriteLine();
        foreach (var (an, a) in r)
        {
            Console.Write($"  {an,-20}");
            foreach (var (_, d) in r) Console.Write($"{Pct(Miss(a, d)),9}");
            Console.WriteLine();
        }
    }
    Console.WriteLine();

    Console.WriteLine("  --- D. THE TUNING KNOB: take N evasion points off the melee rogue, what does the mob hit? ---");
    Console.WriteLine("  The rogue's evasion above the base AGI+level comes from ONE place worth cutting: the light");
    Console.WriteLine("  armor mastery (Skills.Masteries.cs RogueArmor lightEva = 7/11/13/13/13 at 20/24/28/32/36),");
    Console.WriteLine("  plus the base fighter light mastery (+3) and the Agility buff (+4, buffer/potion/scroll).");
    Console.WriteLine($"  {"Lvl",3} {"eva",5} {"base",5} {"extra",6} |" +
        $" {"cut 0",7} {"cut 3",7} {"cut 6",7} {"cut 9",7} {"cut 12",7} {"cut 15",7}");
    foreach (int L in new[] { 20, 28, 36, 44, 52 })
    {
        var r = FarmRosterBuffed(L);
        var mob = r[0].E; var rogue = r[3].E;
        int eva = (int)rogue.EffectiveEvasion;
        int bas = StatCalculator.Evasion((int)rogue.EffectiveAgi, L);
        Console.Write($"  {L,3} {eva,5} {bas,5} {eva - bas,6} |");
        foreach (int cut in new[] { 0, 3, 6, 9, 12, 15 })
            Console.Write($" {Pct(StatCalculator.ResolveAvoidChance(mob.Accuracy, eva - cut,
                rogue.EvadeFloor, 0f, L, L)),6}");
        Console.WriteLine();
    }
    Console.WriteLine("  (a cut of N is worth exactly N% until the 5% floor or the EvadeFloor catches it — so the");
    Console.WriteLine("   floor is what the last few points are worth, and cutting past it buys nothing.)");
    Console.WriteLine();

    Console.WriteLine("  --- E. THE ROLLED ATTRIBUTE — fixed 2026-08-07, this is the before/after ---");
    Console.WriteLine("  A DUAL weapon used to roll AttributeType.EvasionPercent (RampWide, cap 30), applied as");
    Console.WriteLine("  `Evasion += Evasion * pct/100` — a MULTIPLIER on the whole stat, base AGI+level included.");
    Console.WriteLine("  It alone tripled the rogue's evasion budget and grew with level forever. It is now");
    Console.WriteLine("  AttributeType.Evasion: FLAT, RampFlat5, cap 5 — the owner's \"5 roll is a flat 5% increase\".");
    Console.WriteLine("  ✅ The BOW's mirror is now fixed the same way (2026-08-07b): AccuracyPercent RampWide cap 30");
    Console.WriteLine("  -> AttributeType.Accuracy FLAT cap 5. His ruling: \"the AccuracyPercent is a mirror of the");
    Console.WriteLine("  evasion so +5 roll — the archers will have acc buffs/passives\". The bow row is below.");
    Console.WriteLine($"  {"Lvl",3} {"mob acc",8} | {"no roll",8} {"miss",6} |" +
        $" {"NEW max +5",11} {"spread",7} {"miss",6} | {"OLD max 30%",12} {"spread",7} {"miss",6}");
    foreach (int L in new[] { 20, 28, 36, 44, 52 })
    {
        var mob = BuildMobEntity(L);
        var bare = BuildRogue(L); ApplyNpcBuffs(bare);

        var now = BuildRogue(L);
        foreach (var it in now.Inventory.Where(i => ItemCatalog.Get(i.DefId)?.Slot == EquipSlot.Weapon))
            it.Attributes.Add(new ItemAttribute(AttributeType.Evasion, 5));
        ApplyNpcBuffs(now);

        var old = BuildRogue(L);
        foreach (var it in old.Inventory.Where(i => ItemCatalog.Get(i.DefId)?.Slot == EquipSlot.Weapon))
            it.Attributes.Add(new ItemAttribute(AttributeType.EvasionPercent, 30));
        ApplyNpcBuffs(old);

        int eN = (int)now.EffectiveEvasion, eO = (int)old.EffectiveEvasion;
        Console.WriteLine($"  {L,3} {mob.Accuracy,8} | {(int)bare.EffectiveEvasion,8} {Pct(Miss(mob, bare)),6} |" +
            $" {eN,11} {eN - mob.Accuracy,7} {Pct(Miss(mob, now)),6} |" +
            $" {eO,12} {eO - mob.Accuracy,7} {Pct(Miss(mob, old)),6}");
    }
    Console.WriteLine("  The OLD column is the character he measured in game (level ~35, mob acc 65, dagger eva 95).");
    Console.WriteLine("  The NEW roll costs a flat 5 points = a flat 5% at every level, and the rogue's total dodge");
    Console.WriteLine("  vs a same-level mob lands where he said he wants it.");
    Console.WriteLine();

    Console.WriteLine("  --- E1b: THE BOW'S MIRROR — AccuracyPercent 30 -> Accuracy flat 5 ---");
    Console.WriteLine("  Same defect inverted: the old roll multiplied an accuracy that already contains AGI + level,");
    Console.WriteLine("  so it grew forever. What a roll is WORTH, though, is not symmetric with evasion, because");
    Console.WriteLine("  miss = clamp(5% + (eva - acc), defenderFloor, 95%): accuracy can only claw back the part of");
    Console.WriteLine("  the gap that is ABOVE both the universal 5% and the defender's own evade FLOOR.");
    Console.WriteLine($"  {"eva-acc gap",12} {"miss @0",8} {"miss +5",8} {"bought",7} |" +
        $" {"vs a 10% evade floor:",22} {"miss @0",8} {"miss +5",8} {"bought",7}");
    foreach (int gap in new[] { 0, 3, 5, 10, 15, 20, 30 })
    {
        // Level-matched, so the gap term is the ONLY thing moving. 40 is an arbitrary anchor.
        float M(int acc, float floor) => StatCalculator.ResolveAvoidChance(acc, 100 + gap, floor, 0f, 40, 40);
        float n0 = M(100, 0f), n5 = M(105, 0f), f0 = M(100, 0.10f), f5 = M(105, 0.10f);
        Console.WriteLine($"  {gap,12} {Pct(n0),8} {Pct(n5),8} {Pct(n0 - n5),7} |" +
            $" {"",22} {Pct(f0),8} {Pct(f5),8} {Pct(f0 - f5),7}");
    }
    Console.WriteLine("  🔴 READ THIS: +5 accuracy buys the full 5% only once the defender out-evades you by 10+.");
    Console.WriteLine("  Against a ROGUE it buys NOTHING at any gap under 10, because his 10% evade floor is a hard");
    Console.WriteLine("  lower bound on miss that no amount of accuracy can go under. His \"the archers will have acc");
    Console.WriteLine("  buffs/passives\" therefore needs an answer: accuracy is currently a stat that does nothing");
    Console.WriteLine("  against the one target class it is meant to counter. (Evasion has no such problem — it is");
    Console.WriteLine("  additive against the 5% base from the first point.) Not a bug in this change; a design gap.");
    Console.WriteLine();
    Console.WriteLine("  And the live rogue-vs-mob numbers, which is where a bow actually shoots:");
    Console.WriteLine($"  {"Lvl",3} {"mob eva",8} | {"no roll",8} {"hit",6} |" +
        $" {"NEW acc +5",11} {"hit",6} | {"OLD acc +30%",13} {"hit",6}");
    foreach (int L in new[] { 20, 28, 36, 44, 52 })
    {
        var mob = BuildMobEntity(L);

        var bare = BuildRogue(L); ApplyNpcBuffs(bare);
        var now = BuildRogue(L);
        foreach (var it in now.Inventory.Where(i => ItemCatalog.Get(i.DefId)?.Slot == EquipSlot.Weapon))
            it.Attributes.Add(new ItemAttribute(AttributeType.Accuracy, 5));
        ApplyNpcBuffs(now);
        var old = BuildRogue(L);
        foreach (var it in old.Inventory.Where(i => ItemCatalog.Get(i.DefId)?.Slot == EquipSlot.Weapon))
            it.Attributes.Add(new ItemAttribute(AttributeType.AccuracyPercent, 30));
        ApplyNpcBuffs(old);

        // The rogue ATTACKING: his accuracy vs the mob's evasion (same call the tick loop makes).
        float HitPct(Entity a) => 1f - StatCalculator.ResolveAvoidChance(
            a.Accuracy, (int)mob.EffectiveEvasion, mob.EvadeFloor, 0f, a.Level, mob.Level);
        Console.WriteLine($"  {L,3} {(int)mob.EffectiveEvasion,8} | {bare.Accuracy,8} {Pct(HitPct(bare)),6} |" +
            $" {now.Accuracy,11} {Pct(HitPct(now)),6} |" +
            $" {old.Accuracy,13} {Pct(HitPct(old)),6}");
    }
    Console.WriteLine("  Already pinned at the 95% cap with no roll at all — so against MOBS the old +30% was buying");
    Console.WriteLine("  literally nothing, and the flat +5 that replaces it loses nothing. The whole roll is a PvP");
    Console.WriteLine("  stat, which is exactly why capping it at 5 costs the archer no farming speed.");
}
Console.WriteLine();

Console.WriteLine("=== E2: DEFENCE & SURVIVAL — the champion's complaint, measured ===");
{
    Console.WriteLine("  'P.Def on par with the robe' and 'dies when the buffs run out'. Both are one table: what a");
    Console.WriteLine("  same-level mob does per second to each sheet, and how many seconds the sheet lasts.");
    Console.WriteLine($"  {"Lvl",3} {"who",-20} {"P.Def",6} {"MaxHP",6} {"mob dps",8} {"survives",9} |" +
        $" {"P.Def+B",8} {"HP+B",6} {"dps+B",7} {"survives+B",11}");
    foreach (int L in new[] { 20, 28, 36, 44, 52 })
    {
        var bare = FarmRoster(L);
        var buff = FarmRosterBuffed(L);
        var mobBare = bare[0].E;
        var mobBuff = buff[0].E;
        for (int i = 1; i < bare.Length; i++)
        {
            var b = bare[i].E; var f = buff[i].E;
            float dpsBare = Dps(mobBare, b), dpsBuff = Dps(mobBuff, f);
            Console.WriteLine($"  {L,3} {bare[i].Name,-20} {(int)b.EffectiveDefence,6} {b.MaxHp,6} {dpsBare,8:F0} " +
                $"{b.MaxHp / Math.Max(0.01f, dpsBare),8:F0}s | {(int)f.EffectiveDefence,8} {f.MaxHp,6} " +
                $"{dpsBuff,7:F0} {f.MaxHp / Math.Max(0.01f, dpsBuff),10:F0}s");
        }
        Console.WriteLine();
    }
    Console.WriteLine("  The champion's 2H Weapon Mastery (Skills.WeaponMasteries.cs) carries DefencePct −0.10 and");
    Console.WriteLine("  Evasion −3 on EVERY rung — but it is gated to WeaponType.TwoHanded, so the roster above");
    Console.WriteLine("  (1H + shield, what BuildPlayer dresses him in) does NOT pay it. The 2H row:");
    Console.WriteLine($"  {"Lvl",3} {"weapon",-20} {"P.Def",6} {"eva",5} {"mob miss",9} {"mob dps",8} {"survives",9}");
    foreach (int L in new[] { 20, 28, 36, 44, 52 })
    {
        int t = GearTier(L);
        var mob = BuildMobEntity(L);
        foreach (var (label, weapon) in new[] { ("1H sword + shield", (string?)null), ("2H sword (mastery on)", $"sword2h_t{t}") })
        {
            var c = BuildPlayer(Race.Human, BaseClass.Fighter, L, warrior: true);
            if (weapon is not null)
            {
                c.Inventory.RemoveAll(i => ItemCatalog.Get(i.DefId) is { } d
                    && (d.Slot == EquipSlot.Weapon || d.Slot == EquipSlot.Shield));
                Equip(c, weapon);
            }
            ApplyNpcBuffs(c);
            float dps = Dps(mob, c);
            Console.WriteLine($"  {L,3} {label,-20} {(int)c.EffectiveDefence,6} {(int)c.EffectiveEvasion,5} " +
                $"{Pct(Miss(mob, c)),9} {dps,8:F0} {c.MaxHp / Math.Max(0.01f, dps),8:F0}s");
        }
        Console.WriteLine();
    }
}
Console.WriteLine();

Console.WriteLine("=== E3: THE NUKER'S MP ECONOMY — why he sits out of mana ===");
{
    var rs = SkillCatalog.Get(SkillCatalog.RestoreSpirit)!;
    Console.WriteLine("  ⚠ REBUILT 2026-08-07b. Restore Spirit had ONE level for life (20 MP for 65 HP, learned at");
    Console.WriteLine("  25) while the bolt ladder grew 30 -> 116, so it slowed the drain instead of sustaining a");
    Console.WriteLine("  rotation. It now has TEN levels: level 1 @25 is the AUTHORED CSV and is untouched, and");
    Console.WriteLine("  levels 2-10 arrive @40 then every 5 to 80, ending at 120 MP for 200 HP. The ROBE mastery");
    Console.WriteLine("  keeps its CSV rungs (@20/25/30/35) and gains rungs 5-8 @40/50/60/70 — both halves of his");
    Console.WriteLine("  \"+200 MP for -200 HP\" are 40+ content, the only band with no CSV.");
    Console.WriteLine("  ⚠ mpWhenRestored is a PERCENT since 2026-08-19: 19/23/26/30 then 38/45/53/60, i.e. the old");
    Console.WriteLine("  flat x0.75, so the top rung is x1.60 (120 -> 192 where the flat +80 gave 200). The '+mast'");
    Console.WriteLine("  column is that MULTIPLIER. It buys the mana-over-time pipe: a totem pulse scales too. Cast "
        + $"{rs.CastTicks / 10f:F1}s, reuse {rs.CooldownTicks / 10f:F1}s.");
    Console.WriteLine($"  {"Lvl",3} {"sk",3} {"MaxMP",6} {"MaxHP",6} {"mpReg/s",8} {"nuke MP",8} {"nukes",6} |" +
        $" {"base",5} {"xmast",6} {"restore",8} {"HP cost",8} {"MP/HP",7} {"%bar",6} {"%HP",5} {"MP/s",6} {"nukes/cast",11}");
    foreach (int L in new[] { 25, 30, 36, 44, 52, 60, 70, 80 })
    {
        var m = BuildPlayer(Race.Human, BaseClass.Mage, L);
        ApplyNpcBuffs(m);
        var (nuke, nl) = TopSkill(m, SkillEffect.MagicDamage);
        int nukeMp = nuke is null ? 0 : nuke.MpCostAt(nl);
        // The LEARNED level of Restore Spirit is what he actually casts — reading level 1 for a
        // level-80 mage is exactly the measuring error the old table made.
        int rl = Math.Max(1, m.SkillLevelOf(rs.Id));
        int baseMp = rs.PowerAt(rl), hpCost = rs.HpCostAt(rl);
        // The robe mastery is a MULTIPLIER now (×1.19 … ×1.60), not a flat +N — same shape the
        // engine applies in RestoreMpOne, so this stays a measurement and not a second formula.
        int restored = (int)Math.Round(baseMp * m.RestoreMpMod);
        float cycle = (rs.CastTicks + rs.CooldownTicks) / 10f;
        float mpPct = m.Buffs.Where(b => b.Has(SkillEffect.BuffMpRegen))
                            .Sum(b => b.Percent(SkillEffect.BuffMpRegen));
        float mpReg = (StatCalculator.MpRegenPerSecond(m.EffectiveSpt, m.Level) + m.MpRegenBonus)
                      * m.MpRegenMult * (1f + mpPct);
        Console.WriteLine($"  {L,3} {rl,3} {m.MaxMp,6} {m.MaxHp,6} {mpReg,8:F1} {nukeMp,8} " +
            $"{(nukeMp > 0 ? m.MaxMp / (float)nukeMp : 0),6:F1} | {baseMp,5} {m.RestoreMpMod,6:F2} {restored,8} {hpCost,8} " +
            $"{(hpCost > 0 ? restored / (float)hpCost : 0),7:F2} " +
            $"{(m.MaxMp > 0 ? restored * 100f / m.MaxMp : 0),5:F0}% {(m.MaxHp > 0 ? hpCost * 100f / m.MaxHp : 0),4:F0}% " +
            $"{restored / cycle,6:F1} {(nukeMp > 0 ? restored / (float)nukeMp : 0),11:F2}");
    }
    Console.WriteLine("  'nukes' = a full mana bar in top-nuke casts. 'nukes/cast' = how many nukes ONE Restore");
    Console.WriteLine("  Spirit pays for. Below ~1.0 the skill cannot sustain a rotation at any HP price.");
    Console.WriteLine("  'MP/HP' is the DELIVERED trade and is authored to fall 1.18 -> 1.00 across 25 -> 80.");
    Console.WriteLine("  '%bar'/'%HP' are the real cost of a cast: what fraction of each pool one press moves.");
    Console.WriteLine();

    Console.WriteLine("  --- E3b: the MASTERY STACK (2026-08-07 restructure) — every weight, both mage classes ---");
    Console.WriteLine("  Armor masteries STACK now, composing percentages multiplicatively. Spellcaster Mastery owns");
    Console.WriteLine("  the wrong-weight penalty (light/heavy/none = cast x0.5, atkSpd x0.5); the class mastery is");
    Console.WriteLine("  pure bonus. The CLERIC's light row is authored to CANCEL that penalty — cast x1.90 and");
    Console.WriteLine("  atkSpd x2.00 — so the numbers to check are the COMPOSED ones: light cast should read ~x0.95");
    Console.WriteLine("  and light attack speed ~x1.00 for the cleric, and x0.50 for the nuker.");
    Console.WriteLine($"  {"who",-22} {"weight",7} {"cast x",7} {"atkSpd x",9} {"mpReg x",8} {"P.Def",6} {"MaxMP",6} {"restore x",9}");
    // 18 = Human Sorcerer (Nuker), 17 = Human Cleric (Healer). BuildPlayer hardwires the Sorcerer for
    // every mage, so the cleric has to be re-classed and re-taught from HIS table — otherwise both
    // rows measure the nuker and the cancellation this table exists to check is never exercised.
    foreach (var (who, arch) in new[] { ("nuker (Sorcerer)", 18), ("cleric (Cleric)", 17) })
    {
        foreach (var (label, body) in new[] { ("robe", "robe_t40"), ("light", "light_t40"), ("heavy", "heavy_t40"), ("none", (string?)null) })
        {
            var e = BuildPlayer(Race.Human, BaseClass.Mage, 40);
            // Drop the Sorcerer kit before teaching the target class's, so a leftover Mage Armor
            // Mastery cannot stack on top of the cleric's own.
            foreach (var cs in ClassSkills.Cumulative(Race.Human, BaseClass.Mage, Archetype.Nuker, null))
                e.LearnedSkills.Remove(cs.SkillId);
            e.SecondClass = arch;
            foreach (var cs in ClassSkills.Cumulative(Race.Human, BaseClass.Mage, e.Archetype, e.Discipline))
                if (cs.LearnLevel <= 40) e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
            foreach (var id in e.LearnedSkills.Keys.ToList())
                if (SkillCatalog.Get(id)?.Replaces is { } rep)
                    foreach (var r in rep) e.LearnedSkills.Remove(r);
            e.Inventory.RemoveAll(i => ItemCatalog.Get(i.DefId)?.Slot == EquipSlot.Armor);
            if (body is not null) Equip(e, body);
            e.RecomputeDerived();
            Console.WriteLine($"  {who,-22} {label,7} {1f / e.CastSpeedMultiplier,7:F2} " +
                $"{1f / e.AttackSpeedMultiplier,9:F2} {e.MpRegenMult,8:F2} {(int)e.EffectiveDefence,6} " +
                $"{e.MaxMp,6} {e.RestoreMpMod,8:F2}");
        }
        Console.WriteLine();
    }
    Console.WriteLine("  (cast/atkSpd are shown as SPEED multipliers — 1/the internal time multiplier — so >1 is");
    Console.WriteLine("   faster. A robe row must show x1.00 cast: the robe is the caster's weight, not a bonus.)");
}
Console.WriteLine();

Console.WriteLine("=== E4: THE FARM LOOP — cost per KILL, which is what an offline farm actually spends ===");
{
    Console.WriteLine("  'survives Ns' (E2) is the wrong clock for a farm: nobody stands still being hit. What decides");
    Console.WriteLine("  whether an unattended character lives is how much HP and MP ONE kill costs, against what");
    Console.WriteLine("  regenerates in the same seconds. A sheet whose net HP per kill is positive never dies.");
    Console.WriteLine($"  {"Lvl",3} {"who",-20} {"TTK",6} {"HP/kill",8} {"regen",7} {"net HP",7} {"kills",6} |" +
        $" {"MP/kill",8} {"MPregen",8} {"net MP",7} {"kills",6}");
    foreach (int L in new[] { 20, 28, 36, 44, 52 })
    {
        var r = FarmRosterBuffed(L);
        var mob = r[0].E;
        foreach (var (name, e) in r.Skip(1))
        {
            float phys = PhysDps(e, mob), magic = MagicDps(e, mob);
            bool caster = magic > phys;
            float dps = Math.Max(0.01f, Math.Max(phys, magic));
            float ttk = mob.MaxHp / dps;

            var (skill, sl) = TopSkill(e, caster ? SkillEffect.MagicDamage : SkillEffect.PhysicalDamage);
            float mpPerKill = 0f;
            if (skill is not null)
            {
                float cycle = Math.Max(0.1f, SkillCycleSeconds(e, skill));
                mpPerKill = ttk / cycle * skill.MpCostAt(sl);
            }

            float hpLost = Dps(mob, e) * ttk;
            float hpRegen = (StatCalculator.HpRegenPerSecond(e.EffectiveCon, e.Level) + e.HpRegenBonus) * e.HpRegenMult * ttk;
            float mpPct = e.Buffs.Where(b => b.Has(SkillEffect.BuffMpRegen)).Sum(b => b.Percent(SkillEffect.BuffMpRegen));
            float mpRegen = (StatCalculator.MpRegenPerSecond(e.EffectiveSpt, e.Level) + e.MpRegenBonus)
                            * e.MpRegenMult * (1f + mpPct) * ttk;
            float netHp = hpRegen - hpLost, netMp = mpRegen - mpPerKill;

            Console.WriteLine($"  {L,3} {name,-20} {ttk,5:F1}s {hpLost,8:F0} {hpRegen,7:F0} {netHp,7:F0} " +
                $"{(netHp >= 0 ? "  never" : (e.MaxHp / -netHp).ToString("F0")),6} | " +
                $"{mpPerKill,8:F0} {mpRegen,8:F0} {netMp,7:F0} " +
                $"{(netMp >= 0 ? "  never" : (e.MaxMp / -netMp).ToString("F0")),6}");
        }
        Console.WriteLine();
    }
    Console.WriteLine("  'kills' = kills until the bar is empty ('never' = it refills faster than it drains). The HP");
    Console.WriteLine("  column is the death question and the MP column is the STANDING-STILL question — a sheet that");
    Console.WriteLine("  runs dry stops killing, takes hits for free, and only then starts dying.");
}
Console.WriteLine();

Console.WriteLine();
Console.WriteLine("=== MAGE (Human Mage / Nuker, best gear for tier) ===");
Console.WriteLine($"{"Lvl",4} {"Gear",5} {"M.Atk",7} {"MaxHP",7} {"M.Def",7} | {"nuke",5} {"dmg",7} {"mobHP",7} {"casts",6} | {"vs TANK",8}");

foreach (int L in levels)
{
    var mage = BuildPlayer(Race.Human, BaseClass.Mage, L);
    var tank = BuildPlayer(Race.Human, BaseClass.Fighter, L);

    int mAtk = (int)mage.EffectiveMagicAttack;
    int power = TopNukePower(mage);

    int mobMDef = MobBaseStats.MDef(L);
    int mobHp = MobBaseStats.Hp(L);
    int dmgMob = StatCalculator.MagicDamage(mAtk, power, mobMDef, L);
    float casts = dmgMob > 0 ? mobHp / (float)dmgMob : 0;

    int dmgTank = StatCalculator.MagicDamage(mAtk, power, (int)tank.EffectiveMagicDefence, L);

    Console.WriteLine($"{L,4} {GearTier(L),5} {mAtk,7} {mage.MaxHp,7} {(int)mage.EffectiveMagicDefence,7} | " +
                      $"{power,5} {dmgMob,7} {mobHp,7} {casts,6:F1} | {dmgTank,8}");
}

Console.WriteLine();
Console.WriteLine("=== MAGIC CRIT (base 40 x witMod x buffs x passives + flat) ===");
Console.WriteLine("  Rate is WIT-only — no weapon term. Crit DAMAGE is its own channel too:");
Console.WriteLine($"  x{StatCaps.MagicCritDamageBase} base x multipliers x (1 - debuffs), cap x{StatCaps.MagicCritDamageCap}. Ferocity");
Console.WriteLine($"  and the crit-damage attribute stay PHYSICAL. Rate cap {StatCaps.MagicCritRate:P0}.");
Console.WriteLine();
Console.WriteLine($"  {"WIT",4} {"witMod",7} {"base",6} {"x1.2 Res",9} {"x2 Insight",11} {"x4 (4th)",9}  who");
foreach ((int wit, string who) in new[]
{
    (5,  "every MOB (flat WIT 5 at all levels)"),
    (10, "demon fighter"),
    (15, "human fighter"),
    (19, "demon mage, bare"),
    (20, "HUMAN MAGE, bare  <- the x1.00 anchor"),
    (23, "elf mage, bare"),
    (26, "demon mage + set +2 + swap +5"),
    (27, "human mage + set +2 + swap +5"),
    (30, "ELF MAGE + set +2 + swap +5  <- tests the cap"),
})
{
    float witMod = StatCalculator.CritWitMod(wit);
    float b = StatCalculator.MagicCritBase(wit);
    // The chain as RecomputeDerived folds it: base x every multiplier, then the single clamp.
    float res = Math.Min(b * 1.2f, StatCaps.MagicCritRate);
    float ins = Math.Min(b * 2.0f, StatCaps.MagicCritRate);
    // x4 = Insight x2 × the 4th-class buffer's crit-rate x2. NOT clamped in this column ON PURPOSE:
    // the whole point of the 2026-08-19 rescale is that the computed value now sits ABOVE the cap,
    // so raising StatCaps.MagicCritRate later actually pays a mage. The clamp is still real in game.
    float four = b * 4.0f;
    Console.WriteLine($"  {wit,4} {witMod,7:F2} {b,6:P1} {res,9:P1} {ins,11:P1} {four,9:P1}  {who}");
}
Console.WriteLine("  (the x4 column is UNCLAMPED headroom — in game it is capped at " +
                  $"{StatCaps.MagicCritRate:P0}. His targets: elf 8 / 16 / 32.)");
Console.WriteLine();
Console.WriteLine("  MEASURED off real Entities (level 74, best gear) — the chain, not the formula:");
Console.WriteLine($"  {"race",6} {"WIT",4} {"unbuffed",9} {"+Insight x2",12}   (no swap/attribute: BuildPlayer has neither)");
foreach (Race r in new[] { Race.Human, Race.Elf, Race.Demon })
{
    var bare = BuildPlayer(r, BaseClass.Mage, 74);
    var buffed = BuildPlayer(r, BaseClass.Mage, 74);
    ApplyOneBuff(buffed, SkillCatalog.NpcInsight);
    Console.WriteLine($"  {r,6} {(int)bare.EffectiveWit,4} {bare.MagicCritChance,9:P2} {buffed.MagicCritChance,12:P2}");
}
Console.WriteLine();
Console.WriteLine("=== MAGIC LANDING (owner's formula, playtest-20 `57d`) ===");
Console.WriteLine("  fail% = round( 1.3^(defLvl - atkLvl) x defenderMod x weaponMod ),  clamped to [0, 95].");
Console.WriteLine("  Parity with every mod at 1 is round(1) = 1, so SAME LEVEL = 1% fail. There is no");
Console.WriteLine("  caster-side accuracy stat: the levers are the level gap, the tank's x2 passive, and");
Console.WriteLine($"  the untrained weapon (bow/dual/bare = x{StatCaps.UntrainedWeaponMagicFailMod:0}).");
Console.WriteLine();
Console.WriteLine($"  {"dLvl",5} {"wand",8} {"vs tank",9} {"BOW",8} {"bow+tank",9}   (SUCCESS %, caster level 60)");
foreach (int d in new[] { -10, -5, -2, 0, 1, 2, 3, 4, 5, 6, 8, 10, 12, 14, 16, 18, 20 })
{
    float bow = StatCaps.UntrainedWeaponMagicFailMod;
    float w  = 1f - StatCalculator.MagicFailChance(60, 60 + d, 1f, 1f);
    float t  = 1f - StatCalculator.MagicFailChance(60, 60 + d, 2f, 1f);
    float b  = 1f - StatCalculator.MagicFailChance(60, 60 + d, 1f, bow);
    float bt = 1f - StatCalculator.MagicFailChance(60, 60 + d, 2f, bow);
    string note = d == 0 ? "  <- parity" : d == 3 ? "  <- bow+tank hits the 5% floor" : "";
    Console.WriteLine($"  {d,+5} {w,8:P0} {t,9:P0} {b,8:P0} {bt,9:P0}{note}");
}
Console.WriteLine();
Console.WriteLine("  ! The bow penalty is MULTIPLICATIVE, so it fades when punching DOWN: at dLvl -10 a bow");
Console.WriteLine("    caster is back to ~98% success. That is inherent to the formula, not a bug — but it");
Console.WriteLine("    does mean a bow caster can farm well below his level almost unpunished.");
Console.WriteLine();
Console.WriteLine("  MAGIC RESISTANCE is a DAMAGE reduction, not a fizzle (mRes rides inside M.Def):");
Console.WriteLine($"  {"mRes",6} {"coef",6} {"dmg taken",10}   (the mob ladder's 1.25 is exactly mRes +25%)");
foreach (float r in new[] { 0f, 0.05f, 0.10f, 0.15f, 0.25f, 0.5f, 1.0f })
    Console.WriteLine($"  {r,6:P0} {1f + r,6:F2} {1f / (1f + r),10:F3}");
Console.WriteLine();

// THE SPELL LADDERS. Owner ruling 2026-08-24: the fizzle's attacker level is the RUNG'S learn level,
// not the caster's, so a spell ladder now decays with age exactly like the CC ladders below it. That
// makes "where does the TOP rung stop working" a real balance number for every fizzling spell — and
// "does this class even have a rung near the level cap" a question the tool has to answer, because a
// ladder that stops at 40 is a class that stops nuking at 58.
//
// ⚠ IT IS KEYED BY (spell, DISCIPLINE), not by spell. Merging the disciplines is what hides the only
// casualties worth finding: Vampiric Bolt has 13 rungs to @80 on the NUKER ladder and exactly ONE, at
// 14, on the Warchanter's — so a spell that looks healthy in a merged table is dead at 32 for the class
// that actually carries it. Disciplines sharing an identical top rung are collapsed onto one row.
Console.WriteLine("  THE SPELL LADDERS — where each FIZZLING spell's BEST rung stops working, PER CLASS:");
{
    // (skillId, topRung) -> the disciplines that stop there, plus the rung count on that ladder.
    var spells = new Dictionary<(string Id, int Top), (string Name, int Rungs, SortedSet<string> Discs)>();
    foreach (Discipline d in Enum.GetValues<Discipline>())
    {
        var baseCls = Disciplines.Parent(d) is Archetype.Healer or Archetype.Nuker
            ? BaseClass.Mage : BaseClass.Fighter;
        var perSkill = new Dictionary<string, (string Name, SortedSet<int> Learn)>();
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
            // ⚠ The BASE-class list is walked too. `Cumulative` is the CURRENT tier only, so without
            // this every skill bought before level 20 — Magic Bolt, the base mage's Vampiric Bolt @14 —
            // is missing from the table, which is precisely the set the rung rule bites hardest.
            // GameLoopService.RungLevel asks the same two lists in the same order.
            foreach (var cs in ClassSkills.Cumulative(race, baseCls, Disciplines.Parent(d), d)
                         .Concat(ClassSkills.ForClass(race, baseCls, null, null)))
            {
                if (SkillCatalog.Get(cs.SkillId) is not SkillDef sd) continue;
                // Exactly the two arms of ExecuteSkill that roll MagicFailChance: magic damage, and
                // an UNCONTESTED debuff (a contested one rolls DebuffLandChance instead — that is the
                // CC table below). SureHit spells never fizzle at all, so they are not on a ladder.
                bool fizzles = (sd.Effect & SkillEffect.MagicDamage) != 0
                            || ((sd.Effect & SkillEffect.AnyDebuff & ~SkillEffect.ContestCc) != 0
                                && (sd.Effect & SkillEffect.ContestCc) == 0);
                if (!fizzles || sd.SureHit) continue;
                if (!perSkill.TryGetValue(cs.SkillId, out var e))
                    e = perSkill[cs.SkillId] = (sd.Name, new SortedSet<int>());
                e.Learn.Add(cs.LearnLevel);
            }
        foreach (var (id, e) in perSkill)
        {
            var key = (id, e.Learn.Max);
            if (!spells.TryGetValue(key, out var row))
                row = spells[key] = (e.Name, e.Learn.Count, new SortedSet<string>());
            row.Discs.Add(d.ToString());
        }
    }
    Console.WriteLine($"  {"spell",18} {"rungs",6} {"top @",6} {"5% by",6} {"50% by",7} {"95% by",7}  classes");
    foreach (var (key, row) in spells.OrderBy(r => r.Key.Top).ThenBy(r => r.Key.Id))
    {
        int top = key.Top;
        int At(float pct)
        {
            for (int t = top; t <= 200; t++)
                if (StatCalculator.MagicFailChance(top, t) >= pct) return t;
            return 200;
        }
        string Lvl(int t) => t > 90 ? "-" : t.ToString();
        string flag = At(StatCaps.MagicFailMax) <= 90 ? " !" : "";
        Console.WriteLine($"  {row.Name,18} {row.Rungs,6} {top,6} {Lvl(At(0.05f)),6} {Lvl(At(0.50f)),7} "
                        + $"{Lvl(At(StatCaps.MagicFailMax)),7}{flag}  {string.Join(",", row.Discs)}");
    }
    if (spells.Count == 0) Console.WriteLine("  (none authored)");
    Console.WriteLine();
    Console.WriteLine("  ! Target levels, and PARITY WITH THE RUNG is 1% fail. A '!' marks a ladder that");
    Console.WriteLine("    reaches the 95% ceiling inside the level range — that class stops casting it.");
    Console.WriteLine("  ! A fizzle is not a miss: it still lands damage/3, so a spell pinned at the ceiling");
    Console.WriteLine("    is doing ~37% of its damage, not 0%. The fix for a short ladder is a CSV ladder.");
}
Console.WriteLine();
Console.WriteLine("=== CONTESTED DEBUFFS: stun/root/fear/slow + the DoTs (owner ruling 2026-08-19) ===");
Console.WriteLine("  land% = 0.5 + 0.5*(atk - def*L) / (atk + def*L),  clamped to "
                + $"[{StatCaps.CcLandMin:P0}, {StatCaps.CcLandMax:P0}]");
Console.WriteLine($"  where L = {StatCaps.CcLevelBase:F4}^(targetLvl - casterLvl) scales the DEFENDER's stat, so parity");
Console.WriteLine($"  is exactly x1 (pure stat vs stat) and equal stats hit the floor/ceiling at +-{StatCaps.CcLevelFloorGap} levels.");
Console.WriteLine("  Attacker stat = EffectiveAtk (base + stat swaps + armour), or EffectiveAgi for bleed/venom.");
Console.WriteLine("  Defender stat = EffectiveCon (physical school) or EffectiveSpt (magical school).");
Console.WriteLine("  ! dLvl is the target's level MINUS THE RUNG'S LEARN LEVEL — not the caster's level. A");
Console.WriteLine("    rung learned at 40, cast by a level-80 character at a level-80 mob, is dLvl +40.");
Console.WriteLine("    A skill no class list owns (mob spell, scroll) falls back to the caster's level.");
Console.WriteLine();
Console.WriteLine($"  {"dLvl",5} {"equal",7} {"melee",7} {"archer",7} {"mage",7} {"tank",7} {"elite",7} {"BOSS",7}   "
                + "(a level-60 attacker, ATK 40)");
foreach (int d in new[] { -18, -13, -10, -5, -2, 0, 2, 5, 8, 10, 13, 16, 18 })
{
    // The physical school (CON), which is what a stun or a bleed contests.
    float Land(int def) => StatCalculator.DebuffLandChance(40, def, 60, 60 + d);
    int melee = StatCalculator.MobCcCon(MobRole.Melee);
    int elite = (int)MathF.Round(melee * StatCaps.CcRankMult(MobRank.Elite));
    int boss  = (int)MathF.Round(melee * StatCaps.CcRankMult(MobRank.Boss));
    string note = d == 0 ? "  <- parity" : d == StatCaps.CcLevelFloorGap ? "  <- floor" : "";
    Console.WriteLine($"  {d,+5} {Land(40),7:P1} {Land(melee),7:P1} {Land(StatCalculator.MobCcCon(MobRole.Archer)),7:P1} "
                    + $"{Land(StatCalculator.MobCcCon(MobRole.Mage)),7:P1} {Land(50),7:P1} {Land(elite),7:P1} "
                    + $"{Land(boss),7:P1}{note}");
}
Console.WriteLine();
Console.WriteLine("  The two schools at the SAME level, so the role lean is readable (attacker ATK 40):");
Console.WriteLine($"  {"role",8} {"CON",5} {"SPT",5} {"stun/bleed",11} {"root/hold",10}");
foreach (var (name, con, spt) in new[]
{
    ("melee",  StatCalculator.MobCcCon(MobRole.Melee),  StatCalculator.MobCcSpt(MobRole.Melee)),
    ("archer", StatCalculator.MobCcCon(MobRole.Archer), StatCalculator.MobCcSpt(MobRole.Archer)),
    ("mage",   StatCalculator.MobCcCon(MobRole.Mage),   StatCalculator.MobCcSpt(MobRole.Mage)),
    ("tank",   50, 40),
})
    Console.WriteLine($"  {name,8} {con,5} {spt,5} {StatCalculator.DebuffLandChance(40, con, 60, 60),11:P1} "
                    + $"{StatCalculator.DebuffLandChance(40, spt, 60, 60),10:P1}");
Console.WriteLine();
Console.WriteLine("  The MOB's own attack side, same level (it was 8 + 2*lvl = 168 at 80 — a permanent stun):");
Console.WriteLine($"  {"attacker",18} {"ATK",5} {"vs fighter",11} {"vs mage",9}   (CON for a stun, SPT for a slow)");
foreach (var (name, role, rank, physical) in new[]
{
    ("melee stun",      MobRole.Melee, MobRank.Normal, true),
    ("mage stun",       MobRole.Mage,  MobRank.Normal, true),
    ("mage slow",       MobRole.Mage,  MobRank.Normal, false),
    ("ELITE melee stun",MobRole.Melee, MobRank.Elite,  true),
    ("BOSS slam (stun)",MobRole.Melee, MobRank.Boss,   true),
    ("BOSS thorn (slow)",MobRole.Melee,MobRank.Boss,   false),
})
{
    int atk = (int)MathF.Round(StatCalculator.MobCcAtk(role) * StatCaps.CcRankMult(rank));
    var ftr = StatCalculator.GetBaseStats(Race.Human, BaseClass.Fighter);
    var mag = StatCalculator.GetBaseStats(Race.Human, BaseClass.Mage);
    int fDef = physical ? ftr.Con : ftr.Spt, mDef = physical ? mag.Con : mag.Spt;
    Console.WriteLine($"  {name,18} {atk,5} {StatCalculator.DebuffLandChance(atk, fDef, 60, 60),11:P1} "
                    + $"{StatCalculator.DebuffLandChance(atk, mDef, 60, 60),9:P1}");
}
Console.WriteLine();
// EVERY authored contested-CC rung, and the target level at which its TOP rung stops working. This is
// generated from the class tables, so it re-measures itself as the CSV ladders land — the whole point
// of the rung rule is that a skill with one rung expires and a skill with ten does not.
Console.WriteLine();
Console.WriteLine("  THE RUNG LADDERS — where each CC skill's BEST rung hits the 10% floor:");
{
    var rungs = new Dictionary<string, (string Name, SortedSet<int> Learn)>();
    foreach (Discipline d in Enum.GetValues<Discipline>())
    {
        var baseCls = Disciplines.Parent(d) is Archetype.Healer or Archetype.Nuker
            ? BaseClass.Mage : BaseClass.Fighter;
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
            foreach (var cs in ClassSkills.Cumulative(race, baseCls, Disciplines.Parent(d), d))
            {
                if (SkillCatalog.Get(cs.SkillId) is not SkillDef sd) continue;
                if ((sd.Effect & SkillEffect.ContestCc) == 0) continue;
                if (!rungs.TryGetValue(cs.SkillId, out var e))
                    e = rungs[cs.SkillId] = (sd.Name, new SortedSet<int>());
                e.Learn.Add(cs.LearnLevel);
            }
    }
    Console.WriteLine($"  {"skill",22} {"rungs",6} {"learn levels",16} {"top rung floors at",18}");
    foreach (var (id, e) in rungs.OrderBy(r => r.Value.Learn.Min))
    {
        string rungLevels = string.Join("/", e.Learn);
        int floorAt = e.Learn.Max + StatCaps.CcLevelFloorGap;
        string verdict = floorAt >= 90 ? "never (caps out)" : $"target lvl {floorAt}+";
        Console.WriteLine($"  {e.Name,22} {e.Learn.Count,6} {rungLevels,16} {verdict,18}");
    }
    if (rungs.Count == 0) Console.WriteLine("  (none authored)");

    // CC skills that exist as SkillDefs but no class can LEARN — the 40+ purge left the Lightbringer
    // and Warchanter kits commented out pending his CSVs, so their holds are defs with no learn line.
    // Worth printing: an unlearnable skill is not a balance problem, and confusing it for one wastes a
    // pass. It disappears from this line by itself the moment a learn line is authored.
    var ccOrphans = SkillCatalog.AllSkills
        .Where(s => (s.Effect & SkillEffect.ContestCc) != 0 && !rungs.ContainsKey(s.Id)
                 && !s.Id.StartsWith("mob_") && !s.Id.StartsWith("boss_"))
        .Select(s => s.Name).OrderBy(n => n).ToList();
    if (ccOrphans.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine($"  ! In the catalog but NOT learnable by any class ({ccOrphans.Count}): "
                        + string.Join(", ", ccOrphans));
        Console.WriteLine("    These cost nothing and mean nothing until a CSV gives them a learn line.");
    }
}
Console.WriteLine();
Console.WriteLine("  ! The BOSS column above is what a DoT or a stat debuff rolls against it. Stun/root/fear/");
Console.WriteLine("    slow never land on a boss at all, at any rate — it takes the x2 AND the immunity.");
Console.WriteLine($"  ! Rank multiplies ALL THREE stats (elite x{StatCaps.CcRankMult(MobRank.Elite):0.##}, "
                + $"boss x{StatCaps.CcRankMult(MobRank.Boss):0.##}), so a rank is harder to");
Console.WriteLine("    control AND lands its own control harder — one number, both directions.");
Console.WriteLine();

// =====================================================================================================
//  DEBUFF SUCCESS — what a skill's authored `(success chance xN)` is actually worth (BL-90).
//
//  His ruling, 2026-08-24: *"DebuffLandMod should be floating one value - default 1 … armor/weapon
//  break + gravity + Arcane/Fros/Pyro blasts(nuker 3rd) should be 75% at parity (x1.5) and the other
//  should be 25% at parity (x0.5)"*. Those two numbers only close off a 50% base — the CONTESTED
//  curve — which is what forced the routing fix (see GameLoopService.IsContestedDebuff). This table's
//  job is to prove that his arithmetic actually holds in the built game, per skill.
// =====================================================================================================
Console.WriteLine("=== DEBUFF SUCCESS: the per-skill multiplier (BL-90) ===");
Console.WriteLine("  A debuff lands on ONE of two rolls:");
Console.WriteLine("    CONTESTED — a ContestCc flag OR a declared DebuffSchool -> DebuffLandChance, 50% at parity");
Console.WriteLine("    FIZZLE    — neither                                     -> 1 - MagicFailChance, ~99%");
Console.WriteLine("  DebuffLandMod multiplies the probability it STICKS on whichever roll ran. It never");
Console.WriteLine("  touches damage and never touches interrupt. Default 1 = untagged.");
Console.WriteLine();
{
    float contested  = StatCalculator.DebuffLandChance(40, 40, 60, 60);
    float fizzleLand = 1f - StatCalculator.MagicFailChance(60, 60);
    Console.WriteLine($"  HIS SCALE, on the contested curve ({contested:P0} at parity):");
    Console.WriteLine($"  {"x0.3",8} {"x0.5",8} {"x0.7",8} {"x1.0",8} {"x1.5",8}");
    Console.WriteLine($"  {contested * 0.3f,8:P0} {contested * 0.5f,8:P0} {contested * 0.7f,8:P0} "
                    + $"{contested * 1.0f,8:P0} {Math.Min(contested * 1.5f, StatCaps.CcLandMax),8:P0}");
    Console.WriteLine();

    // Generated from the catalog, so it re-measures itself as kits land and cannot drift.
    var tagged = SkillCatalog.AllSkills
        .Where(s => s.DebuffLandMod != 1f && !s.Id.StartsWith("mob_") && !s.Id.StartsWith("boss_"))
        .OrderByDescending(s => s.DebuffLandMod).ThenBy(s => s.Name).ToList();
    Console.WriteLine($"  TAGGED IN CODE ({tagged.Count}) — everything else runs at x1:");
    Console.WriteLine($"  {"skill",22} {"mod",6} {"path",10} {"at parity",10}");
    foreach (var s in tagged)
    {
        bool isContested = (s.Effect & SkillEffect.ContestCc) != 0 || s.DebuffSchool != DebuffSchool.None;
        float at = isContested
            ? Math.Clamp(contested * s.DebuffLandMod, 0f, StatCaps.CcLandMax)
            : Math.Clamp(fizzleLand * s.DebuffLandMod, 0f, 1f);
        string warn = isContested ? "" : "   <- FIZZLE path: his parity arithmetic does NOT hold here";
        Console.WriteLine($"  {s.Name,22} x{s.DebuffLandMod,-5:0.##} {(isContested ? "contested" : "FIZZLE"),-10} "
                        + $"{at,10:P1}{warn}");
    }
    if (tagged.Count == 0) Console.WriteLine("  (none)");
    Console.WriteLine();

    // The four skills the routing fix moved. Named explicitly because this is the behaviour change
    // most likely to surprise him in play: they used to land ~99% of the time and now do not.
    Console.WriteLine("  MOVED FROM FIZZLE TO CONTESTED BY THE ROUTING FIX (they declare a DebuffSchool):");
    foreach (var s in SkillCatalog.AllSkills
                 .Where(s => s.DebuffSchool != DebuffSchool.None
                          && (s.Effect & SkillEffect.ContestCc) == 0
                          && !s.Id.StartsWith("mob_") && !s.Id.StartsWith("boss_"))
                 .OrderBy(s => s.Name))
        Console.WriteLine($"  {s.Name,22} x{s.DebuffLandMod,-5:0.##} "
                        + $"was ~{fizzleLand,4:P0} -> now {Math.Clamp(contested * s.DebuffLandMod, 0f, StatCaps.CcLandMax),6:P0} at parity");
    Console.WriteLine();
    Console.WriteLine("  ! Parity is a BEST CASE. Both curves read the RUNG'S learn level, so an old rung decays");
    Console.WriteLine("    from here — see THE SPELL LADDERS and THE RUNG LADDERS above.");
    Console.WriteLine("  ! The multiplier is applied AFTER the [10%, 90%] clamp and only the CEILING is re-applied:");
    Console.WriteLine("    a x0.5 skill is allowed under the floor (it is unreliable by design), a x1.5 one is not");
    Console.WriteLine("    allowed past 90% (nothing is ever a certainty).");
    Console.WriteLine("  ! Snare Trap (dmg+root) and the Warchanter's stun-rider are deliberately left at x1 —");
    Console.WriteLine("    retro-taxing a built class is your call, one line each.");
}
Console.WriteLine();


// =====================================================================================================
//  INTERRUPT — IG'S OWN FORMULA (owner, 2026-08-26). This section REPLACED the DPS-contest tables of
//  0.83.0 wholesale; the model they measured no longer exists.
//
//      BaseChance  = (DmgTaken / MaxHP) x random(100..120)
//      FinalChance = BaseChance x MEN-mod x (1 - Buffs)
//
//  Two departures from IG, both his: no robe-set 50% resist ("mages become [un]interruptable - and i
//  dont want that"), and a FLATTENED MEN curve — IG's 20=x1 / 50=x0.23 becomes his 20=x1 / 50=x0.67.
// =====================================================================================================
Console.WriteLine("=== INTERRUPT: IG's formula (owner 2026-08-26) ===");
Console.WriteLine("  p(one hit) = dmgTaken/MaxHP x rand(1.00-1.20) x SPT-mod x (1 - resist buffs) x skill.InterruptMult");
Console.WriteLine($"  SPT curve: {StatCalculator.InterruptSpiritFloor} SPT = x1.00, "
                + $"50 SPT = x{StatCalculator.InterruptSpiritAt50:0.00} (IG's own is x0.23 — flattened on your call).");
Console.WriteLine("  Tables below use the MEAN roll (x1.10); the engine rolls it fresh per hit.");
Console.WriteLine();
{
    const float roll = StatCalculator.InterruptRollMean;

    // 1. THE SPT CURVE, on the bases we actually ship.
    Console.WriteLine("  THE SPT CURVE — our own race/class bases, and what each is worth:");
    Console.WriteLine($"  {"who",22} {"SPT",4} {"x mod",7} {"resist",7} | {"IG's curve",11}");
    foreach (var (who, race, cls) in new[]
    {
        ("human fighter", Race.Human, BaseClass.Fighter),
        ("elf fighter",   Race.Elf,   BaseClass.Fighter),
        ("demon fighter",   Race.Demon,   BaseClass.Fighter),
        ("elf mage",      Race.Elf,   BaseClass.Mage),
        ("human mage",    Race.Human, BaseClass.Mage),
        ("demon mage",      Race.Demon,   BaseClass.Mage),
    })
    {
        int spt = StatCalculator.GetBaseStats(race, cls).Spt;
        float mod = StatCalculator.SpiritInterruptMod(spt);
        float ig  = MathF.Pow(0.23f, (spt - 20) / 30f);   // IG's steep curve, for comparison only
        Console.WriteLine($"  {who,22} {spt,4} {mod,7:0.000} {1f - mod,7:P0} | {ig,11:0.000}");
    }
    Console.WriteLine("  (your check: a level-39 human mage is x0.395 under IG's curve -> a 50%-HP hit is 9%,");
    Console.WriteLine("   \"a bit low\". Ours prices the same mage at the x mod column.)");
    Console.WriteLine();

    // 2. HIS WORKED EXAMPLE, and the grid around it.
    Console.WriteLine("  YOUR EXAMPLE — 1000 damage on a 2000 HP pool, no SPT mod, Resolve 54%:");
    Console.WriteLine($"    base 50 (at roll 1.00) -> final "
                    + $"{StatCalculator.InterruptChance(1000, 2000, 1f, 0.54f, 1.0f):P0}   (your 23%)");
    Console.WriteLine();
    Console.WriteLine("  THE GRID — chance ONE hit breaks a cast, by how big the hit is:");
    Console.WriteLine($"  {"hit as % of MaxHP",18} | {"no SPT, no buff",16} {"human mage 39",14} "
                    + $"{"+Resolve 54%",13} {"demon mage 45 +54%",17}");
    foreach (float share in new[] { 0.02f, 0.05f, 0.10f, 0.20f, 0.35f, 0.50f })
    {
        float hm = StatCalculator.SpiritInterruptMod(39), om = StatCalculator.SpiritInterruptMod(45);
        Console.WriteLine($"  {share,18:P0} | "
            + $"{StatCalculator.InterruptChance(share, 1f, 1f, 0f, roll),16:P1} "
            + $"{StatCalculator.InterruptChance(share, 1f, hm, 0f, roll),14:P1} "
            + $"{StatCalculator.InterruptChance(share, 1f, hm, 0.54f, roll),13:P1} "
            + $"{StatCalculator.InterruptChance(share, 1f, om, 0.54f, roll),17:P1}");
    }
    Console.WriteLine();

    // 3. MEASURED, REAL BUILDS — a fighter hitting a mage mid-cast, at four levels.
    Console.WriteLine("  MEASURED — a same-level human fighter vs a human mage's cast, both best-in-tier:");
    Console.WriteLine($"  {"lvl",4} {"mage HP",8} {"ftr basic",10} {"ftr skill",10} {"SPT",4} {"xmod",6} "
                    + $"| {"p/basic",8} {"p/skill",8} | {"+Res basic",11} {"+Res skill",11}");
    foreach (int L in new[] { 20, 40, 60, 80 })
    {
        var mage = BuildPlayer(Race.Human, BaseClass.Mage, L);
        var ftr  = BuildPlayer(Race.Human, BaseClass.Fighter, L);
        int basic = StatCalculator.PhysicalDamage((int)ftr.EffectiveBasicAttack, 0, (int)mage.EffectiveDefence, L);
        int skill = StatCalculator.PhysicalDamage((int)ftr.EffectiveAttack, TopPhysSkillPower(ftr),
                                                  (int)mage.EffectiveDefence, L);
        float mod = mage.InterruptSpiritMod;
        Console.WriteLine($"  {L,4} {mage.MaxHp,8} {basic,10} {skill,10} {mage.EffectiveSpt,4} {mod,6:0.00} | "
            + $"{StatCalculator.InterruptChance(basic, mage.MaxHp, mod, 0f, roll),8:P1} "
            + $"{StatCalculator.InterruptChance(skill, mage.MaxHp, mod, 0f, roll),8:P1} | "
            + $"{StatCalculator.InterruptChance(basic, mage.MaxHp, mod, 0.54f, roll),11:P1} "
            + $"{StatCalculator.InterruptChance(skill, mage.MaxHp, mod, 0.54f, roll),11:P1}");
    }
    Console.WriteLine();
    Console.WriteLine("  ! RESOLVE NO LONGER DECAYS. It is a straight x(1-0.54) at every level — the whole point");
    Console.WriteLine("    of moving it from flat points to a percent. Compare the last two column pairs.");
    Console.WriteLine("  ! Disrupt (InterruptPower 99999, now read as percentage POINTS) still always breaks a cast.");
}
Console.WriteLine();
Console.WriteLine();


// =====================================================================================================
//  THE ELF NUKER'S LOW-DAMAGE SKILLS — his question of 2026-08-24 and the ruling that closed it.
//  *"we can make the two lower dmg elf nuker skills have x3 interrupt chance ... probably should make
//  them x10 or something"* → ruled **x2** on 2026-08-26: *"They are fast cast and x2 interrupt chance
//  is good enough"*.
//
//  🔑 EVERY NUMBER BELOW IS READ OFF THE REAL `SkillDef` — power, cast, reuse AND the multiplier. The
//  kit shipped in 0.87.0, so the literal four-row table this block used to carry (his CSV's top rungs,
//  hand-copied while `nuker 3rd` was unbuilt) is gone: retune a rung and this table moves with it
//  instead of quietly going stale. That staleness is not hypothetical — `BL-91` still read "not in the
//  code" on 2026-08-27, two days after it was.
// =====================================================================================================
Console.WriteLine("=== INTERRUPT: the elf nuker's spells vs a same-level mage (BL-91) ===");
{
    const int L = 74;
    const float roll = StatCalculator.InterruptRollMean;
    var caster = BuildPlayer(Race.Elf, BaseClass.Mage, L);
    var victim = BuildPlayer(Race.Human, BaseClass.Mage, L);
    float vMod = victim.InterruptSpiritMod;

    Console.WriteLine($"  caster: elf mage {L}, M.Atk {(int)caster.EffectiveMagicAttack}. "
                    + $"target: human mage {L}, Max HP {victim.MaxHp}, SPT {victim.EffectiveSpt} (x{vMod:0.00}).");
    Console.WriteLine($"  {"spell",18} {"power",6} {"cast",5} {"reuse",6} {"dmg",7} {"% HP",6} "
                    + $"| {"x1",7} {"BUILT",8} {"x5",7} {"x10",7} | {"built +Resolve",15}");
    foreach (string id in new[]
    {
        SkillCatalog.FrostSpikes, SkillCatalog.FrostPierce,
        SkillCatalog.ElementalBlast, SkillCatalog.Thunderstorm,
    })
    {
        if (SkillCatalog.Get(id) is not SkillDef def) { Console.WriteLine($"  {id,18}  🔴 NOT IN CATALOG"); continue; }

        // The TOP rung, because the question was asked about the level-74 kit. Everything here is the
        // SkillDef's own answer at that rung — no CSV number is repeated in this file any more.
        int top      = def.MaxLevel;
        int power    = def.PowerAt(top);
        float castS  = def.CastTicksAt(top) / 10f;
        float reuseS = def.CooldownTicksAt(top) / 10f;
        float mult   = def.InterruptMult;

        float dmg = StatCalculator.MagicDamageFM((int)caster.EffectiveMagicAttack, 0, power,
                                                 (int)victim.EffectiveMagicDefence, victim.MagicDefCoef);
        float share = dmg / victim.MaxHp;
        Console.WriteLine($"  {def.Name,18} {power,6} {castS,5:0.0} {reuseS,6:0} {dmg,7:0} {share,6:P1} | "
            + $"{StatCalculator.InterruptChance(dmg, victim.MaxHp, vMod, 0f, roll, 1f),7:P1} "
            + $"{StatCalculator.InterruptChance(dmg, victim.MaxHp, vMod, 0f, roll, mult),8:P1} "
            + $"{StatCalculator.InterruptChance(dmg, victim.MaxHp, vMod, 0f, roll, 5f),7:P1} "
            + $"{StatCalculator.InterruptChance(dmg, victim.MaxHp, vMod, 0f, roll, 10f),7:P1} | "
            + $"{StatCalculator.InterruptChance(dmg, victim.MaxHp, vMod, 0.54f, roll, mult),15:P1}");
    }
    Console.WriteLine();
    Console.WriteLine("  ! ✅ BUILT 0.87.0: Frost Spikes and Frost Pierce carry SkillDef.InterruptMult = 2, and the");
    Console.WriteLine("    BUILT column READS IT — it is whatever the catalog says, not what this file remembers.");
    Console.WriteLine("    x5/x10 are kept only to show what was rejected. Both rows also say \"(interrupt chance x2)\"");
    Console.WriteLine("    in `nuker 3rd.csv`, and Descr.cs checks them against the SkillDef on every --check.");
    Console.WriteLine("  ! 🔑 A NUKE ON A MAGE IS *NOT* A SMALL HIT. A mage's HP pool is the smallest in the game,");
    Console.WriteLine("    so even the cheap spells take a real slice of it — which is why x2 lands where x10 was");
    Console.WriteLine("    guessed: x10 on either Frost skill is a guaranteed cancel, i.e. Disrupt, not a nuke.");
    Console.WriteLine("  ! These are PER HIT, and both Frost skills fire every ~2.5s, so a long cast eats several:");
    Console.WriteLine("    the chance a cast is broken AT ALL is 1-(1-p)^n.");
}
Console.WriteLine();
Console.WriteLine();
Console.WriteLine("=== TANK / FIGHTER (Human Fighter, best gear for tier) ===");
Console.WriteLine("  'basic' = autoattack; 'skill' = best physical skill. Compare SKILL against the mage's");
Console.WriteLine("  nuke — the basic column is not the fighter's damage, it is its filler.");
Console.WriteLine($"{"Lvl",4} {"P.Atk",7} {"MaxHP",7} {"P.Def",7} {"M.Def",7} | {"basic",7} {"skill",7} {"mobHP",7} {"hits",6}");

foreach (int L in levels)
{
    var f = BuildPlayer(Race.Human, BaseClass.Fighter, L);
    int pAtk = (int)f.EffectiveAttack;
    int mobPDef = MobBaseStats.PDef(L);
    int mobHp = MobBaseStats.Hp(L);
    int hit = StatCalculator.PhysicalDamage(pAtk, 0, mobPDef, L);
    int skillHit = StatCalculator.PhysicalDamage(pAtk, TopPhysSkillPower(f), mobPDef, L);
    float hits = skillHit > 0 ? mobHp / (float)skillHit : 0;

    Console.WriteLine($"{L,4} {pAtk,7} {f.MaxHp,7} {(int)f.EffectiveDefence,7} {(int)f.EffectiveMagicDefence,7} | " +
                      $"{hit,7} {skillHit,7} {mobHp,7} {hits,6:F1}");
}
Console.WriteLine();
// =====================================================================================================
//  DPS COMPARISON — a CHAMPION's reference skill vs a NUKER's best, at the same level.
//
//  The owner's reference (2026-07-29): "Heavenly Crush" — power 7600, 7s reuse, 1.8s cast, can double.
//  It does not exist in the catalogue yet; it is modelled here so warrior and nuker can be compared on
//  the one measure that means anything, DAMAGE PER SECOND, before it gets authored.
//
//  Note the shape of our formula: power is ADDITIVE with P.Atk and defence DIVIDES the sum, so a big
//  IG-style power number is tempered rather than explosive — 7600 is not the outlier it looks like next
//  to a nuke's power 116.
// =====================================================================================================
{
    const int refLevel = 74;
    const int refPower = 7600;
    const int refCastTicks = 18;   // 1.8s
    const int refReuseTicks = 70;  // 7s

    int mobPDef = MobBaseStats.PDef(refLevel);
    int mobMDef = MobBaseStats.MDef(refLevel);
    int mobHp = MobBaseStats.Hp(refLevel);

    Console.WriteLine($"=== DPS @ {refLevel}: CHAMPION vs NUKER — both on the OWNER-SPECIFIED reference skills ===");
    Console.WriteLine($"  mob: {mobHp} HP, {mobPDef} P.Def, {mobMDef} M.Def");

    // ---- Champion: Heavenly Crush on cooldown, autoattacks filling the gaps ----
    var champ = BuildPlayer(Race.Human, BaseClass.Fighter, refLevel, warrior: true);
    ApplyNpcBuffs(champ);
    int cAtk = (int)champ.EffectiveAttack;
    // Crit folds in the FLAT crit damage (it joins P.Atk inside the ratio on a crit); [Double] is the
    // ATK curve and is a flat x2 that never touches crit damage. docs/design/CritBlowAndDouble.md.
    float critF = CritFactor(champ.CritChance, StatCalculator.PhysicalCritMult(champ.CritDamageBonus)
        * StatCalculator.CritFlatFactor(champ.EffectiveAttack, champ.CritDamageFlat, refPower));
    float dblF  = CritFactor(StatCalculator.PhysicalDoubleChance(champ.AtkStat), 2f);

    int crushHit = StatCalculator.PhysicalDamage(cAtk, refPower, mobPDef, refLevel);
    // A PHYSICAL skill's cast time is shortened by ATTACK speed, exactly as a spell's is by cast speed
    // (SkillReuseTicks picks the multiplier by SkillCategory). Leaving Crush at a flat 1.8s while the
    // nuke's 4s shrank to 2.6s quietly taxed the Champion for being buffed — its attack speed nearly
    // doubled and none of that reached its skill.
    int crushCastTicks = Math.Max(2, (int)(refCastTicks * champ.EffectiveAttackSpeedMultiplier));
    float crushCycle = (crushCastTicks + refReuseTicks) * GameConstants.TickSeconds;
    float crushDps = crushHit * critF * dblF / crushCycle;

    int autoHit = StatCalculator.PhysicalDamage(cAtk, 0, mobPDef, refLevel);
    float autoEvery = AutoAttackSeconds(champ);
    // Autoattacks only fill the time the cast is NOT occupying.
    float autoShare = (crushCycle - crushCastTicks * GameConstants.TickSeconds) / crushCycle;
    float autoDps = autoHit * critF / autoEvery * autoShare;

    // Print the AVERAGE damage — crit and double already folded in — so the arithmetic on each line
    // reconciles. Printing the raw hit next to a crit-adjusted dps read as a mistake (2119 / 8.8s is
    // 241, not 309); the gap was the crit multiplier, invisibly applied to only one of the two.
    Console.WriteLine($"  CHAMPION  P.Atk {cAtk}  crit x{critF:F2}  double x{dblF:F2}   (avg dmg = hit x crit x double)");
    Console.WriteLine($"    Heavenly Crush  {crushHit * critF * dblF,6:F0} avg / {crushCycle,4:F1}s  = {crushDps,7:F1} dps");
    Console.WriteLine($"    autoattack      {autoHit * critF,6:F0} avg / {autoEvery,4:F2}s  = {autoDps,7:F1} dps"
                      + $"   (only {autoShare:P0} of the cycle is free to swing)");
    Console.WriteLine($"    TOTAL                                  = {crushDps + autoDps,7:F1} dps"
                      + $"   ({mobHp / Math.Max(1f, crushDps + autoDps):F1}s to kill)");

    // ---- Nuker: the REFERENCE top nuke, same basis as the Champion's ----
    //
    // Also the owner's number, not the catalogue's: power 108, 4s cast, 1s reuse. Taking it from the
    // catalogue would measure the PLACEHOLDER 3rd-class kit and quietly pass an estimate off as data —
    // and it did exactly that, picking a fast low-power spell that flatters the class. Both sides of
    // this comparison are now the same kind of number: what the owner intends the class to have at 74.
    // Neither class's next skill improves DPS — the nuker's level-76 nuke is SLOWER, and the
    // Champion's is utility with less power — so level 74 is the fair place to compare.
    const int nukePower = 108;
    const int nukeCastTicks = 40;   // 4s base, shortened by cast speed below
    const int nukeReuseTicks = 10;  // 1s

    var nuker = BuildPlayer(Race.Human, BaseClass.Mage, refLevel);
    ApplyNpcBuffs(nuker);
    int mAtk = (int)nuker.EffectiveMagicAttack;
    float mCritF = CritFactor(nuker.MagicCritChance, nuker.EffectiveMagicCritDamage);

    int nukeHit = StatCalculator.MagicDamage(mAtk, nukePower, mobMDef, refLevel);
    // Cast time scales with CAST speed exactly as SkillReuseTicks does; reuse does not.
    float nukeCycle = (Math.Max(2, (int)(nukeCastTicks * nuker.EffectiveCastSpeedMultiplier)) + nukeReuseTicks)
                      * GameConstants.TickSeconds;
    float nukeDps = nukeHit * mCritF / nukeCycle;

    Console.WriteLine($"  NUKER     M.Atk {mAtk}  magic crit x{mCritF:F2}");
    Console.WriteLine($"    top nuke (power {nukePower})  {nukeHit * mCritF,6:F0} avg / {nukeCycle,4:F1}s  = {nukeDps,7:F1} dps"
                      + $"   ({mobHp / Math.Max(1f, nukeDps):F1}s to kill)");
    Console.WriteLine($"    -> CHAMPION/NUKER = {(crushDps + autoDps) / Math.Max(1f, nukeDps):F2}x");
    Console.WriteLine();

    // ---- REUSE REDUCTION sweep ----
    //
    // ⚠ THERE IS NO REUSE BUFF IN THE GAME. CooldownPct exists as a field and is used by exactly one
    // thing — the healer's Caster Mastery passive — so nothing a player can be BUFFED with reduces
    // reuse today. This sweep models what one would be worth before it gets authored.
    //
    // It is not symmetric, and that is the point: reuse shortens the COOLDOWN only, never the cast.
    // The Champion's Crush is 1.8s cast on a 7s cooldown, so almost all of its cycle is reducible.
    // The nuke is a ~2.9s cast on a 1s cooldown — barely any of it is. A reuse buff is therefore a
    // WARRIOR buff wearing a neutral name, and handing "the same" buff to both classes would quietly
    // hand the fight to the melee.
    Console.WriteLine("  REUSE-REDUCTION sweep (no such buff exists yet — this is what one would do):");
    Console.WriteLine($"    {"reuse-",7} {"champion",9} {"nuker",9} {"ratio",7}");
    foreach (float cdr in new[] { 0f, 0.10f, 0.20f, 0.30f })
    {
        float cCycle = (crushCastTicks + Math.Max(1, (int)(refReuseTicks * (1f - cdr)))) * GameConstants.TickSeconds;
        float cShare = (cCycle - crushCastTicks * GameConstants.TickSeconds) / cCycle;
        float cDps = crushHit * critF * dblF / cCycle + autoHit * critF / autoEvery * cShare;

        float nCast = Math.Max(2, (int)(nukeCastTicks * nuker.EffectiveCastSpeedMultiplier));
        float nCycle = (nCast + Math.Max(1, (int)(nukeReuseTicks * (1f - cdr)))) * GameConstants.TickSeconds;
        float nDps = nukeHit * mCritF / nCycle;

        Console.WriteLine($"    {cdr,6:P0} {cDps,9:F1} {nDps,9:F1} {cDps / Math.Max(1f, nDps),7:F2}x");
    }
    Console.WriteLine();

    // The PLACEHOLDER catalogue kit, listed separately and ranked by DPS so it is never mistaken for
    // the reference above. This is where the estimated kit's own problems show up.
    Console.WriteLine("  (placeholder catalogue kit at this level, ranked by DPS — NOT the reference:)");
    var nukes = new List<(string Name, int Power, float Dps)>();
    foreach (var (id, lvl) in nuker.LearnedSkills)
    {
        var def = SkillCatalog.Get(id);
        if (def is null || (def.Effect & SkillEffect.MagicDamage) == 0) continue;
        if (!string.IsNullOrEmpty(def.ConsumableId)) continue;
        int power = def.PowerAt(lvl);
        int hit = StatCalculator.MagicDamage(mAtk, power, mobMDef, refLevel);
        float cycle = SkillCycleSeconds(nuker, def);
        nukes.Add(($"{def.Name} L{lvl}", power, hit * mCritF / cycle));
    }
    foreach (var n in nukes.OrderByDescending(n => n.Dps))
        Console.WriteLine($"      {n.Name,-22} power {n.Power,4}  = {n.Dps,7:F1} dps");
    Console.WriteLine();
}

Console.WriteLine("=== UNARMED / NAKED (no weapon, no armor) — should be FEEBLE ===");
Console.WriteLine($"{"Lvl",4} {"class",8} {"P.Atk",7} | {"basic",7} {"mobHP",7} {"hits",6}");
foreach (int L in new[] { 1, 4, 8, 20 })
{
    foreach (var (cls, label) in new[] { (BaseClass.Fighter, "Fighter"), (BaseClass.Mage, "Mage") })
    {
        var e = new Entity { Name = "naked", Kind = EntityKind.Player };
        e.Race = Race.Human; e.BaseClass = cls; e.Level = L;
        var s = StatCalculator.GetBaseStats(Race.Human, cls);
        e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Agi = s.Agi; e.Spt = s.Spt;   // 🔴 SPT was MISSING here until 2026-08-26 — every player row ran at SPT 0
        e.RecomputeDerived();   // NO gear equipped
        int pAtk = (int)e.EffectiveAttack;
        int mobPDef = MobBaseStats.PDef(L);
        int mobHp = MobBaseStats.Hp(L);
        int hit = StatCalculator.PhysicalDamage(pAtk, 0, mobPDef, L);
        Console.WriteLine($"{L,4} {label,8} {pAtk,7} | {hit,7} {mobHp,7} {(hit > 0 ? mobHp / (float)hit : 0),6:F1}");
    }
}
Console.WriteLine("  (before this change a naked L1 fighter had 42 P.Atk and ONE-SHOT level-4-8 mobs)");
Console.WriteLine();

// -----------------------------------------------------------------------------------------------
// LOW LEVEL (1-10): a REAL new player — TRAINING gear, NO runes — vs same-level mob HP. This is the
// band the device playtest flagged ("lvl-1 one-shots a lvl 4-8 mob"). BuildPlayer floors to level-20
// gear + runes, so it can't show this; BuildStarter equips the training kit and no rune buff.
// "1-shot?" = the mob dies in a single hit/nuke (mobHp <= dmg).
// -----------------------------------------------------------------------------------------------
Console.WriteLine("=== LOW LEVEL 1-10 — REAL new player (training gear, NO shots) ===");
Console.WriteLine($"{"Lvl",4} | {"MAGE M.Atk",10} {"nuke",5} {"dmg",6} {"mobHP",6} {"1shot?",6} | {"FTR P.Atk",9} {"basic",6} {"1shot?",6}");
foreach (int L in new[] { 1, 2, 3, 4, 5, 6, 8, 10 })
{
    var mage = BuildStarter(BaseClass.Mage, L);
    var ftr  = BuildStarter(BaseClass.Fighter, L);
    int mAtk = (int)mage.EffectiveMagicAttack;
    int power = TopNukePower(mage);
    int mobMDef = MobBaseStats.MDef(L), mobPDef = MobBaseStats.PDef(L), mobHp = MobBaseStats.Hp(L);
    int nuke = StatCalculator.MagicDamage(mAtk, power, mobMDef, L);
    int basic = StatCalculator.PhysicalDamage((int)ftr.EffectiveAttack, 0, mobPDef, L);
    Console.WriteLine($"{L,4} | {mAtk,10} {power,5} {nuke,6} {mobHp,6} {(nuke >= mobHp ? "YES" : "no"),6} | " +
                      $"{(int)ftr.EffectiveAttack,9} {basic,6} {(basic >= mobHp ? "YES" : "no"),6}");
}
Console.WriteLine("  (a lvl-1 that one-shots a same-or-higher-level mob = the balance bug to fix)");
Console.WriteLine();

// -----------------------------------------------------------------------------------------------
// M.ATK DISPLAY RAMP (owner 2026-07-25): the shown M.Atk = scale·√internal. "now" uses the flat
// scale 20; "new" ramps scale = min(level, 20) so low levels read close to IG (a lvl-1 wand mage
// showed ~72 where IG shows ~8). DAMAGE is untouched (it uses the internal value, printed too).
// Training gear, no shots — a real new character 1-30.
// -----------------------------------------------------------------------------------------------
Console.WriteLine("=== M.ATK DISPLAY: flat-20 (now) vs min(internal, 20·√internal) (new) — best gear ===");
Console.WriteLine("  new = show raw internal until it passes 20·√internal (crossover at internal=400), then shrink.");
Console.WriteLine($"{"Lvl",4} | {"FTR int",8} {"now",6} {"new",6} | {"MAGE int",9} {"now",6} {"new",6}");
static float ShownNow(Entity e) => 20 * MathF.Sqrt(e.EffectiveMagicAttack);
static float ShownNew(Entity e) => MathF.Min(e.EffectiveMagicAttack, 20 * MathF.Sqrt(e.EffectiveMagicAttack));
foreach (int L in new[] { 1, 5, 10, 20, 30, 40, 52, 61, 76, 85 })
{
    // Below 20 use training gear (no tier gear exists), at/above 20 use best-for-tier — the real play state.
    var ftr  = L >= 20 ? BuildPlayer(Race.Human, BaseClass.Fighter, L) : BuildStarter(BaseClass.Fighter, L);
    var mage = L >= 20 ? BuildPlayer(Race.Human, BaseClass.Mage, L)    : BuildStarter(BaseClass.Mage, L);
    Console.WriteLine(
        $"{L,4} | {ftr.EffectiveMagicAttack,8:F0} {ShownNow(ftr),6:F0} {ShownNew(ftr),6:F0} | " +
        $"{mage.EffectiveMagicAttack,9:F0} {ShownNow(mage),6:F0} {ShownNew(mage),6:F0}");
}
Console.WriteLine();

Console.WriteLine("=== PROGRESSION (x1 rates; a NORMAL x1-toughness mob, solo, zero level gap) ===");
Console.WriteLine($"{"Lvl",4} {"exp/kill",10} {"sp/kill",9} {"expToNext",14} {"mobs/level",11} {"cumulative",12}");
long cumulative = 0;
for (int L = 1; L <= ExpCurve.MaxLevel; L++)
{
    long exp = StatCalculator.MobExpReward(L);
    long sp = StatCalculator.MobSpReward(L);
    long next = ExpCurve.ExpToNext(L);
    long mobs = next <= 0 ? 0 : (long)Math.Ceiling(next / (double)exp);
    cumulative += mobs;
    if (levels.Contains(L) || L is 1 or 2 or 5 or 10 or 79 or 80 or 85)
        Console.WriteLine($"{L,4} {exp,10:N0} {sp,9:N0} {next,14:N0} {mobs,11:N0} {cumulative,12:N0}");
}
Console.WriteLine($"  TOTAL mobs 1->{ExpCurve.MaxLevel}: {cumulative:N0}"
    + $"  (~{cumulative * 10 / 3600.0:N0} h at 10s/kill)");
Console.WriteLine("  (a mob that buys bulk with an HP-multiplier passive pays that multiple in EXP and SP)");
Console.WriteLine();

// =====================================================================================================
//  ECONOMY — the playtest-14 faucet. "Level 25 with 3kk gold purely from selling trash" (owner).
//
//  Every number here comes from the REAL drop tables (MobCatalog.StandardDrops, resolved with the same
//  group math GameLoopService.RollDrop runs) and the REAL vendor prices (ItemCatalog.SellPrice). It
//  exists because the 3kk figure has been argued about from hand-derived multipliers twice, and the two
//  levers (drop chance and sell price) MULTIPLY — which is exactly the shape of arithmetic people get
//  wrong. Do not re-derive; change a number and re-run this.
// =====================================================================================================
Console.WriteLine("=== ECONOMY: expected TRASH GOLD per kill (real drop tables x real sell prices) ===");

// The marginal chance of each entry, replicating RollDrop: independents roll alone; a group rolls once
// at the summed member chance, then picks one member weighted — so a member's marginal chance is
// min(1, groupSum * rate) * (its share of the group). Level gap is 0 here (killing your own level).
static IEnumerable<(DropEntry Entry, float Chance)> Marginals(IEnumerable<DropEntry> table, int mobLevel)
{
    var applicable = table.Where(e => e.AppliesAtLevel(mobLevel)).ToList();
    // ⚠ NOT clamped to 1 any more: above 100% the roll pays COPIES (MobCatalog.DropCopies), so the
    // expected yield per kill really is the raw number and clamping here would under-report every
    // high-rate server by exactly the amount the clamp used to throw away.
    foreach (var e in applicable.Where(e => e.GroupId == 0))
        yield return (e, MobCatalog.EffectiveChance(e));
    foreach (var g in applicable.Where(e => e.GroupId != 0).GroupBy(e => e.GroupId))
    {
        // Weights are the PER-ITEM-TUNED chances (MobCatalog.ItemWeight), matching RollDrop exactly —
        // this tool's whole job is to be the same arithmetic, so it reads the same two helpers.
        float sum = g.Sum(MobCatalog.ItemWeight);
        // (Lambda, not a method group: EffectiveChance now takes the PLAYER's drop multiplier as an
        //  optional second argument — this tool has no player, so it measures the bare server rates.)
        float trigger = g.Sum(e => MobCatalog.EffectiveChance(e));
        foreach (var e in g)
            yield return (e, sum <= 0 ? 0 : trigger * (MobCatalog.ItemWeight(e) / sum));
    }
}

// What one kill is worth AT A VENDOR, split by what produced it. Quantity uses the entry's mean.
static (double Gear, double Mats, double Consumables, double Gold, double Items) KillValue(
    MobType mob, int mobLevel)
{
    double gear = 0, mats = 0, cons = 0, items = 0;
    foreach (var (e, chance) in Marginals(mob.Drops ?? Array.Empty<DropEntry>(), mobLevel))
    {
        if (ItemCatalog.Get(e.ItemId) is not ItemDef def) continue;
        double qty = (e.MinQty + e.MaxQty) / 2.0 * RateConfig.World.DropAmount;
        double value = chance * qty * ItemCatalog.SellPrice(def);
        items += chance;
        if (MobCatalog.IsGearGroup(e.GroupId)) gear += value;
        else if (e.GroupId == MobCatalog.GroupMats || def.Id.StartsWith("mat_")) mats += value;
        else cons += value;
    }
    return (gear, mats, cons, StatCalculator.MobGoldReward(mobLevel) * RateConfig.World.Gold, items);
}

// The roster mob(s) closest to a given level — averaged, so one odd template can't skew a row.
static MobType[] MobsNear(int level) =>
    MobCatalog.Templates.Where(m => !m.Dummy && m.Level > 0)
        .GroupBy(m => Math.Abs(m.Level - level)).OrderBy(g => g.Key).First().ToArray();

Console.WriteLine($"{"Lvl",4} {"mob",22} {"items/kill",10} | {"gear",10} {"mats",8} {"cons",8} {"coin",7} {"TOTAL",10}");
foreach (int L in new[] { 5, 10, 15, 20, 25, 30, 40, 52, 61, 76 })
{
    var near = MobsNear(L);
    var v = near.Select(m => KillValue(m, L)).ToArray();
    Console.WriteLine($"{L,4} {near[0].Name,22} {v.Average(x => x.Items),10:F2} | " +
        $"{v.Average(x => x.Gear),10:N0} {v.Average(x => x.Mats),8:N0} {v.Average(x => x.Consumables),8:N0} " +
        $"{v.Average(x => x.Gold),7:N0} {v.Average(x => x.Gear + x.Mats + x.Consumables + x.Gold),10:N0}");
}
Console.WriteLine("  'cons' = potions/scrolls (the Always + Scrolls groups). 'coin' = the gold drop itself.");
// Price anchors, so the per-kill column above can be checked by hand against docs/design/EconomyRework.md
// without re-deriving the whole ladder. The E Common gauntlet is the level-25 playtest's actual trash.
foreach (var id in new[] { "gloves_t20_common", "heavy_t20_common", "sword2h_t20_common",
                           "ring_t20_common", "heavy_t76_common", "potion_minor" })
    if (ItemCatalog.Get(id) is ItemDef anchor)
        Console.Write($"  {anchor.Name} [{id.Split('_')[^1]}] buy {ItemCatalog.BuyPrice(anchor):N0} / sell {ItemCatalog.SellPrice(anchor):N0}\n");
Console.WriteLine();

// The owner's actual acceptance test: how much gold has a character SOLD by the time it hits level 25,
// assuming it vendors everything and kills only its own level. Run at the LIVE ExpRate (what he plays
// on) and at x1, because a 10x exp rate means 10x FEWER kills for the same level — and therefore 10x
// less trash gold. Getting that backwards is how a "16x cut" turns into "68x".
Console.WriteLine("=== ECONOMY: cumulative TRASH GOLD by level (kills-to-level x gold-per-kill) ===");
Console.WriteLine($"  live ExpRate = x{RateConfig.World.Exp:0.##}, DropChanceRate = x{RateConfig.World.DropChance:0.##}"
    + $" (gear groups x{RateConfig.DropGroupRate("armor"):0.##}; the global now reaches EVERY group —"
    + " above 100% a drop pays COPIES rather than clamping)");
Console.WriteLine($"{"Lvl",4} {"kills(live)",12} {"sold(live)",14} | {"kills(x1)",11} {"sold(x1)",14}");
double soldLive = 0, soldX1 = 0;
double killsLive = 0, killsX1 = 0;
for (int L = 1; L <= 85; L++)
{
    var near = MobsNear(L);
    double perKill = near.Select(m => KillValue(m, L)).Average(x => x.Gear + x.Mats + x.Consumables + x.Gold);
    long exp = StatCalculator.MobExpReward(L);
    long next = ExpCurve.ExpToNext(L);
    if (next <= 0 || exp <= 0) continue;
    double kX1 = next / (double)exp;
    double kLive = kX1 / Math.Max(0.01f, RateConfig.World.Exp);
    killsX1 += kX1; killsLive += kLive;
    soldX1 += kX1 * perKill; soldLive += kLive * perKill;
    if (L is 10 or 20 or 25 or 40 or 61 or 85)
        Console.WriteLine($"{L + 1,4} {killsLive,12:N0} {soldLive,14:N0} | {killsX1,11:N0} {soldX1,14:N0}");
}
Console.WriteLine("  ^ the level column is the level REACHED. The owner's target: ~400k by level 25.");
Console.WriteLine("  (he reported 3,000,000 on the 0.33.1 build — divide to get the achieved cut)");
Console.WriteLine();

// The GEAR-GROUP multiplier is the knob that moves trash gold without touching a single authored number,
// and it is live-tunable in game with `/droprate gear <x>`. Sweep it so the owner can pick a value from a
// measured column instead of guessing — the whole reason per-group multipliers exist.
Console.WriteLine("=== ECONOMY: /droprate gear <x> — what it does to trash gold by level 25 ===");
Console.WriteLine($"{"gear x",8} {"effective",10} {"gold @25",14} {"vs 400k target",16}");
float[] saved = new[] { "armor", "accessory", "weapon", "jewel" }
    .Select(g => RateConfig.DropGroupRate(g)).ToArray();
foreach (float mul in new[] { 1f, 0.5f, 0.34f, 0.25f, 0.1f })
{
    foreach (var g in new[] { "armor", "accessory", "weapon", "jewel" })
        RateConfig.DropGroupRates[g] = mul;
    double sold = 0;
    for (int L = 1; L <= 25; L++)
    {
        long exp = StatCalculator.MobExpReward(L);
        long next = ExpCurve.ExpToNext(L);
        if (next <= 0 || exp <= 0) continue;
        double kills = next / (double)exp / Math.Max(0.01f, RateConfig.World.Exp);
        sold += kills * MobsNear(L).Select(m => KillValue(m, L))
            .Average(x => x.Gear + x.Mats + x.Consumables + x.Gold);
    }
    Console.WriteLine($"{mul,8:0.##} {mul * RateConfig.World.DropChance,10:0.##}x {sold,14:N0} "
        + $"{sold / 400_000.0,16:0.00}x");
}
for (int i = 0; i < saved.Length; i++)
    RateConfig.DropGroupRates[new[] { "armor", "accessory", "weapon", "jewel" }[i]] = saved[i];
Console.WriteLine("  'effective' = the global DropChanceRate x this multiplier — what a gear group really rolls at.");
Console.WriteLine();

// =====================================================================================================
//  §R: PREMIUM REWARD RUNES (`BL-01`) — what a rung is actually WORTH.
//
//  He asked for a ladder (+5%, then tenths to +100%) without pricing it, and a percentage on an item
//  card tells nobody what they are buying. So price it in the two currencies a player feels: HOURS off
//  the climb, and gold per hour of farm. Both are read from the same curves the server uses, and the
//  runes are applied the way the server applies them — one multiplier per channel, best rune wins.
//
//  The Sinister row is the one to read twice: it is the grinder's rune, so its exp column is not a loss
//  to be minimised, it is the POINT (level 34 forever, farming a level-34 field). What it must not do is
//  cost gold, and that is what the table proves.
// =====================================================================================================
Console.WriteLine("#####################################################################################");
Console.WriteLine("###  R: PREMIUM REWARD RUNES — what each rung buys                                 ###");
Console.WriteLine("#####################################################################################");
Console.WriteLine();

// Kills and gold to climb 1 -> 60 at the LIVE rates, as the baseline every rune is measured against.
(double Kills, double Gold) Climb(int toLevel, float expMult, float goldMult, float dropMult)
{
    double kills = 0, gold = 0;
    for (int L = 1; L < toLevel; L++)
    {
        long exp = StatCalculator.MobExpReward(L);
        long next = ExpCurve.ExpToNext(L);
        if (next <= 0 || exp <= 0) continue;
        double k = next / (double)exp / Math.Max(0.01f, RateConfig.World.Exp * expMult);
        kills += k;
        // A kill pays coin (the Gold rune) and a table (the Drop rune). Only the gear/mats/consumable
        // half scales with drop chance — the coin is not a drop roll.
        var kv = MobsNear(L).Select(m => KillValue(m, L)).ToList();
        double coin = kv.Average(x => x.Gold);
        double table = kv.Average(x => x.Gear + x.Mats + x.Consumables);
        gold += k * (coin * goldMult + table * dropMult);
    }
    return (kills, gold);
}

var baseline = Climb(60, 1f, 1f, 1f);
Console.WriteLine("=== R1: the climb to 60 under one rune at a time (live rates) ===");
Console.WriteLine($"{"rune",34} {"kills 1->60",12} {"vs base",9} {"gold sold",14} {"vs base",9}");
Console.WriteLine($"{"(none)",34} {baseline.Kills,12:N0} {"1.00x",9} {baseline.Gold,14:N0} {"1.00x",9}");

void RuneRow(string label, float expMult, float goldMult, float dropMult)
{
    var r = Climb(60, expMult, goldMult, dropMult);
    Console.WriteLine($"{label,34} {r.Kills,12:N0} {r.Kills / baseline.Kills,8:0.00}x {r.Gold,14:N0} "
        + $"{r.Gold / baseline.Gold,8:0.00}x");
}

foreach (int rung in new[] { 0, 1, 2, 5, 10 })   // +5%, +10%, +20%, +50%, +100%
{
    int pct = RewardRunes.Percent(rung);
    float m = 1f + RewardRunes.Ladder[rung];
    RuneRow($"Rune of Experience ({pct}%)", m, 1f, 1f);
}
foreach (int rung in new[] { 2, 10 })
{
    int pct = RewardRunes.Percent(rung);
    float m = 1f + RewardRunes.Ladder[rung];
    RuneRow($"Rune of Gold ({pct}%)", 1f, m, 1f);
    RuneRow($"Rune of Drop ({pct}%)", 1f, 1f, m);
}
Console.WriteLine("  ^ 'gold sold' is coin + everything vendored. An EXP rune lowers it: fewer kills for");
Console.WriteLine("    the same level means less trash — the same inversion the economy section warns about.");
Console.WriteLine();

// The grinder's rune, measured where it is actually used: parked at one level, farming that field.
Console.WriteLine("=== R2: Rune of Sinister — the grinder's rune, 1000 kills at a parked level ===");
Console.WriteLine($"{"level",6} {"exp gained",14} {"levels gained",14} {"gold + loot",14}");
foreach (int L in new[] { 34, 50, 70 })
{
    var v = MobsNear(L).Select(m => KillValue(m, L)).Average(x => x.Gold + x.Gear + x.Mats + x.Consumables);
    // Sinister: the rune set's Exp and Sp are 0, everything else untouched.
    Console.WriteLine($"{L,6} {0,14:N0} {0,14:N0} {1000 * v,14:N0}");
}
Console.WriteLine("  ^ zero exp by design (*\"so a grinder can grind and no lvl up\"*) and the full loot of");
Console.WriteLine("    1000 kills. The Rune of Sinners is this row with the last column zeroed too.");
Console.WriteLine();

// =====================================================================================================
//  ECONOMY — the playtest-18 THREE-CHARACTER experiment (owner, 2026-08-05). He ran three characters
//  through the same ~14-15 h of idle farm: a mage that sold NOTHING finished level 34 with 350k, a tank
//  that sold only EQUIPMENT finished 36 with 3.3kk, a rogue that sold EVERYTHING finished 34 with 4.6kk.
//  Those three numbers pin the live faucet from the actual game, so this section calibrates on the one
//  with no player choice in it — the COIN — and then prices the same kills. If the model reproduces his
//  3.3 and 4.6, the sweep under it can be trusted to pick the cut instead of guessing a multiplier.
// =====================================================================================================
Console.WriteLine("=== ECONOMY: the playtest-18 three-character experiment (level ~34, ~14-15 h idle) ===");
const int PlaytestLevel = 33;          // a level-34 character killing its own level
const double CoinObserved = 350_000;   // his mage: gold drops only, sold nothing — the calibration point

static (double Gear, double Trash, double Coin) PerKill(int level)
{
    var v = MobsNear(level).Select(m => KillValue(m, level)).ToArray();
    return (v.Average(x => x.Gear), v.Average(x => x.Mats + x.Consumables), v.Average(x => x.Gold));
}

var pk = PerKill(PlaytestLevel);
double farmKills = CoinObserved / Math.Max(1, pk.Coin);
Console.WriteLine($"  per kill: gear {pk.Gear,9:N0}  trash {pk.Trash,7:N0}  coin {pk.Coin,6:N0}"
    + $"   (gear is {pk.Gear / Math.Max(1, pk.Coin):N0}x the coin)");
Console.WriteLine($"  his 350k of coin => {farmKills:N0} kills ({farmKills / 14.5:N0}/h over 14.5 h)");
Console.WriteLine($"  those same kills:    sells gear only {farmKills * (pk.Gear + pk.Coin),12:N0}   (he measured 3,300,000)");
Console.WriteLine($"                     sells EVERYTHING {farmKills * (pk.Gear + pk.Trash + pk.Coin),11:N0}   (he measured 4,600,000)");
Console.WriteLine();

// The two levers, swept TOGETHER, against his stated target: ~1kk total over that same farm. Sell price
// is linear in the divisor and drop chance is linear in the group multiplier, so both are applied
// analytically to the measured per-kill value — no mutation of the live catalog needed.
Console.WriteLine("=== ECONOMY: picking the cut — gear DROP rate x gear SELL divisor, level ~34 ===");
Console.WriteLine($"  owner's target: ~1,000,000 over this farm (350k coin + ~650k of sales)");
Console.WriteLine($"{"gear x",7} {"sell /",7} | {"gear gold",11} {"trash",8} {"coin",8} {"TOTAL",11} {"vs 1kk",7} {"gear:coin",10}");
// The baselines are whatever the catalog is LIVE at — pk.Gear was measured through them, so the sweep
// has to divide them back out. Hardcoding 1/3 and 25 here would silently misreport the moment either
// knob is retuned, which is exactly the arithmetic this section exists to stop people getting wrong.
float liveGearMul = RateConfig.DropGroupRate("armor");
int liveDivisor = GameConstants.GearSellDivisor;
Console.WriteLine($"  live now: gear groups x{liveGearMul:0.###}, GearSellDivisor {liveDivisor}");
// ⚠ Every multiplier below was tripled on 2026-08-05, when `DropChanceRate` went 3 → 1 and the x3 was
// folded into the groups that were taking it. The DELIVERED rates are identical — only the units moved
// — but a row labelled 0.025 would now mean a third of what it meant when these were first written.
foreach (var (gearMul, divisor) in new (float, int)[]
         {
             (1f,      25),   // the pre-playtest-18 setting, for reference (was 1/3 under global x3)
             (1f,      250),  // sell price x0.1, drops untouched
             (0.15f,   25),   // drops x0.15, price untouched
             (0.3f,    50),   // both, split evenly
             (0.075f,  10),   // SHIPPED (playtest-18): 13x rarer, worth 2.5x more
             (0.075f,  25),
             (0.15f,   10),
             (0.0375f, 10),
         })
{
    double gearGold = farmKills * pk.Gear * (gearMul / liveGearMul) * (liveDivisor / (double)divisor);
    double trash = farmKills * pk.Trash;
    double coin = farmKills * pk.Coin;
    double total = gearGold + trash + coin;
    Console.WriteLine($"{gearMul,7:0.###} {divisor,7} | {gearGold,11:N0} {trash,8:N0} {coin,8:N0} "
        + $"{total,11:N0} {total / 1_000_000.0,7:0.00}x {gearGold / coin,10:0.0}");
}
Console.WriteLine("  'gear:coin' = how many times the sold gear outweighs the mob's own gold drop.");
Console.WriteLine();

// The STRUCTURAL problem behind the number: gear sell value follows the tier ladder (roughly geometric)
// while the mob's own gold drop is linear in level. Any FLAT cut fixes exactly one level band and is
// wrong again twenty levels later — so print the drift, which is what decides whether one multiplier
// is even the right shape of fix.
Console.WriteLine("=== ECONOMY: gear-sale vs coin drift across the ladder (why a flat cut expires) ===");
Console.WriteLine($"{"Lvl",4} {"gear/kill",11} {"coin/kill",10} {"ratio",8}");
foreach (int L in new[] { 10, 20, 25, 33, 40, 52, 61, 76, 85 })
{
    var p = PerKill(L);
    Console.WriteLine($"{L,4} {p.Gear,11:N0} {p.Coin,10:N0} {p.Gear / Math.Max(1, p.Coin),8:0.0}x");
}
Console.WriteLine();

// =====================================================================================================
//  SCROLL FLOOD — how OFTEN each scroll family lands, and what it is worth (owner, playtest-18 V2b:
//  "the attribute scrolls and enchant scrolls also need to lower the chances + move them in the lvls").
//
//  Frequency, not gold, is the measure here — enchant and attribute scrolls have no Value at all, so
//  they sell for 0 and cannot flood the ECONOMY however many drop. What they flood is the bag, and what
//  they cheapen is enchanting. The BuffPotion column is the opposite case: it is pure gold, and it is
//  the whole of the remaining consumable faucet.
// =====================================================================================================
Console.WriteLine("=== SCROLLS: per-kill chance by family, and gold (enchant/attribute sell for 0) ===");
Console.WriteLine($"{"Lvl",4} {"enchant",9} {"attribute",10} {"buff pot",13} | {"buff gold/kill",15}");
foreach (int L in new[] { 5, 15, 20, 33, 40, 45, 52, 61, 76, 80, 85 })
{
    var near = MobsNear(L);
    double ench = 0, attr = 0, buff = 0, buffGold = 0;
    foreach (var m in near)
        foreach (var (e, chance) in Marginals(m.Drops ?? Array.Empty<DropEntry>(), L))
        {
            if (ItemCatalog.Get(e.ItemId) is not ItemDef def) continue;
            double c = chance / near.Length;
            switch (def.Subtype)
            {
                case ItemSubtype.EnchantScroll: ench += c; break;
                case ItemSubtype.AttributeScroll: attr += c; break;
                case ItemSubtype.BuffPotion:
                    buff += c; buffGold += c * ItemCatalog.SellPrice(def); break;
            }
        }
    Console.WriteLine($"{L,4} {ench,9:P1} {attr,10:P1} {buff,13:P1} | {buffGold,15:N0}");
}
Console.WriteLine("  'buff pot' = the non-enchant half of the Scrolls group. Buff SCROLLS are not in it at");
Console.WriteLine("  any level any more (playtest-17 E3) — they come out of the Blessing Box or nowhere.");
Console.WriteLine();

// D1: the enchant ladder is now TWO axes (type x grade) and lives in two places — the catalog and the
// rank drop table. Print both and assert they agree, because a scroll authored in one and missing from
// the other is exactly the silent hole this file exists to catch.
Console.WriteLine("=== D1: the 18 enchant scrolls (type x grade) ===");
Console.WriteLine($"{"grade",6} {"opens",6} | {"Normal (breaks)",-34} {"Greater (-1)",-34} {"Safe (keeps)",-34}");
int scrollsFound = 0;
foreach (var (grade, rarity, _, level, _) in ItemCatalog.EnchantScrollBands)
{
    var cells = new List<string>();
    foreach (var (kind, _, _) in ItemCatalog.EnchantScrollTypes)
    {
        string id = ItemCatalog.EnchantScrollKey(kind, grade);
        var def = ItemCatalog.Get(id);
        if (def is null) { cells.Add($"!! MISSING {id}"); continue; }
        scrollsFound++;
        // The two axes must actually be ON the def — the server validates from these fields.
        string flag = def.ScrollKind == kind && def.ScrollGrade == grade && def.Rarity == rarity
            ? "" : " !!AXES";
        cells.Add($"{ItemCatalog.SellPrice(def),8:N0}g {def.Rarity,-9}{flag}");
    }
    Console.WriteLine($"{EnchantRules.GradeName(grade),6} {level,6} | {cells[0],-34} {cells[1],-34} {cells[2],-34}");
}
Console.WriteLine($"  {scrollsFound} of 18 present. Sell price shown; rarity is how the GRADE is signalled.");
Console.WriteLine();

Console.WriteLine("=== D1: the elite / boss scroll layer (delivered chance per kill) ===");
Console.WriteLine($"{"Lvl",4} {"band",5} | {"ELITE",-40} | {"BOSS",-52}");
foreach (int L in new[] { 15, 25, 45, 55, 65, 78, 82 })
{
    string Row(MobRank rank) => string.Join("  ", MobCatalog.EnchantScrollDrops(L, rank)
        .Select(d => $"{ItemCatalog.Get(d.ItemId)?.Name ?? "!!" + d.ItemId} {MobCatalog.EffectiveChance(d):P1}")
        .Select(s => s.Replace("Scroll of Enchant ", "")));
    var band = EnchantRules.GradeOf(L);
    Console.WriteLine($"{L,4} {EnchantRules.GradeName(band),5} | {Row(MobRank.Elite),-40} | {Row(MobRank.Boss),-52}");
}
// A rank entry naming an item that does not exist never reaches the normal integrity check below,
// because these entries are built at KILL time and are in no template.
var badRank = new[] { MobRank.Elite, MobRank.Boss }
    .SelectMany(r => new[] { 15, 25, 45, 55, 65, 78, 82, 90 }
        .SelectMany(L => MobCatalog.EnchantScrollDrops(L, r)))
    .Where(d => ItemCatalog.Get(d.ItemId) is null).ToArray();
Console.WriteLine($"  rank-layer ids that resolve: {(badRank.Length == 0 ? "all" : $"!! {badRank.Length} MISSING")}");
Console.WriteLine("  F band (below 20) yields nothing at any rank — there is no F scroll by design.");
Console.WriteLine();

// E3's whole claim is "no buff scroll drops, from anything, ever". That is a property of ~200 drop
// tables, so assert it rather than trusting a reading: the Blessing Box's own contents ARE the list of
// the 17, so the box and the guard can never drift apart.
var scrollIds = (BoxCatalog.Get(ItemCatalog.BoxBuffScrolls)?.Entries ?? Array.Empty<BoxEntry>())
    .Select(e => e.ItemId).ToHashSet();
var leaked = MobCatalog.Templates
    .SelectMany(m => (m.Drops ?? Array.Empty<DropEntry>()).Select(d => (Mob: m.Id, d.ItemId)))
    .Where(x => scrollIds.Contains(x.ItemId))
    .ToArray();
Console.WriteLine($"=== E3: buff scrolls in drop tables — {leaked.Length} (must be 0 of {scrollIds.Count}) ===");
foreach (var b in leaked.Take(10)) Console.WriteLine($"    !! {b.Mob} -> {b.ItemId}");
Console.WriteLine();

// Integrity: a drop entry naming an item that does not exist is a silent hole in the loot table — the
// roll succeeds and the player gets nothing. Cheap to check, and it has caught renames before.
Console.WriteLine("=== ECONOMY: drop-table integrity ===");
var badDrops = MobCatalog.Templates
    .SelectMany(m => (m.Drops ?? Array.Empty<DropEntry>()).Select(d => (Mob: m.Id, d.ItemId)))
    .Where(x => ItemCatalog.Get(x.ItemId) is null)
    .ToArray();
int entryCount = MobCatalog.Templates.Sum(m => m.Drops?.Length ?? 0);
Console.WriteLine($"  {entryCount:N0} drop entries across {MobCatalog.Templates.Count()} templates; "
    + $"{badDrops.Length} unresolved id(s).");
foreach (var b in badDrops.Take(20)) Console.WriteLine($"    !! {b.Mob} -> {b.ItemId}");

// ELITE and BOSS gear is built at kill time, not baked into a template, so it never passes through the
// check above — and it is the path that reaches the Legendary and Mythic ids nothing else touches.
foreach (var rank in new[] { MobRank.Elite, MobRank.Boss })
{
    var rows = new List<string>();
    foreach (int L in new[] { 1, 10, 19, 20, 40, 52, 61, 76, 85 })
    {
        var table = MobCatalog.GearDrops(L, rank).ToArray();
        var missing = table.Where(d => ItemCatalog.Get(d.ItemId) is null).Select(d => d.ItemId).ToArray();
        double best = table.Length == 0 ? 0
            : Marginals(table, L).Where(x => ItemCatalog.Get(x.Entry.ItemId) is ItemDef)
                .Sum(x => x.Chance);
        rows.Add($"L{L}:{best:P0}{(missing.Length > 0 ? $" !!{missing.Length} MISSING ({missing[0]})" : "")}"
               + (table.Length == 0 ? " !!EMPTY" : ""));
    }
    Console.WriteLine($"  {rank,-5} gear pieces per kill:  {string.Join("  ", rows)}");
}

// Every group must be a sane probability: a group that sums past 1.0 is silently clamped, which throws
// away the weights inside it (this is what DropChanceRate = 3 was doing to the 100% groups).
var hotGroups = MobCatalog.Templates
    .SelectMany(m => (m.Drops ?? Array.Empty<DropEntry>())
        .Where(d => d.GroupId != 0)
        .GroupBy(d => d.GroupId)
        .Select(g => (Mob: m.Id, Group: g.Key, Sum: g.Sum(e => MobCatalog.EffectiveChance(e)))))
    .Where(x => x.Sum > 1.0001f)
    .ToArray();
Console.WriteLine($"  {hotGroups.Length} group(s) clamped at 100% (weights inside would be preserved anyway,"
    + " but the group's own rate is capped).");
foreach (var h in hotGroups.Take(10)) Console.WriteLine($"    !! {h.Mob} group {h.Group} = {h.Sum:P0}");
Console.WriteLine();

Console.WriteLine("=== LEVEL-GAP PENALTY (symmetric; applies to EXP and DROPS, personal per member) ===");
Console.Write("  gap ");
for (int g = 0; g <= 14; g++) Console.Write($"{g,6}");
Console.WriteLine();
Console.Write("  mult");
for (int g = 0; g <= 14; g++) Console.Write($"{ExpCurve.LevelGapMultiplier(g),6:F2}");
Console.WriteLine();
Console.WriteLine();

Console.WriteLine("=== PARTY BONUS (multiplies the pot; the pot is then split EQUALLY) ===");
Console.Write("  members  ");
for (int n = 1; n <= 9; n++) Console.Write($"{n,7}");
Console.WriteLine();
Console.Write("  bonus    ");
for (int n = 1; n <= 9; n++) Console.Write($"{ExpCurve.PartyBonus(n),7:F2}");
Console.WriteLine();
Console.Write("  per head ");
for (int n = 1; n <= 9; n++) Console.Write($"{ExpCurve.PartyBonus(n) / n,7:P0}");
Console.WriteLine("   <- share of a solo kill; the party must out-kill this to win");
Console.WriteLine();

// Every item that claims a SetId must resolve to a real set, or the client's set panel silently
// renders nothing (BuildSetSection returns null) — which is exactly how "set info missing from the
// item window" can look like a UI bug when it is really a DATA gap.
Console.WriteLine("=== ITEM ↔ ARMOR-SET WIRING ===");
var orphans = ItemCatalog.AllItems
    .Where(d => !string.IsNullOrEmpty(d.SetId))
    .Where(d => !ArmorSetCatalog.All.Any(s =>
        s.Id == d.SetId || (string.IsNullOrEmpty(s.AccessorySetId) ? s.Id : s.AccessorySetId) == d.SetId))
    .ToList();
int withSet = ItemCatalog.AllItems.Count(d => !string.IsNullOrEmpty(d.SetId));
Console.WriteLine($"  {withSet} items carry a SetId; {ArmorSetCatalog.All.Count()} sets defined; " +
                  $"{orphans.Count} orphaned.");
foreach (var o in orphans.Take(15))
    Console.WriteLine($"    ORPHAN  {o.Id,-22} -> SetId '{o.SetId}' matches no set");
Console.WriteLine();

// You may not walk the same ARCHETYPE twice, nor the same DISCIPLINE twice, across the classes ONE
// character owns. Matched on the archetype/discipline, NOT the class id — a human Sorcerer and an elf
// Inquisitor are different ids but the same NUKER path, and holding both is exactly what is forbidden.
Console.WriteLine("=== CLASS UNIQUENESS ACROSS SUBCLASSES ===");
{
    var c = new Entity { Name = "dual", Kind = EntityKind.Player, Race = Race.Human };
    c.Subclasses.Clear();
    // Class #0: a Human Mage who became a Sorcerer (Nuker) and then a Magus — the ONE nuker discipline
    // since `BL-97` (2026-08-28). It used to say "and then a Tempest", and the branch it named is gone.
    var nuker = new Subclass { Slot = 0, BaseClass = BaseClass.Mage, SecondClass = 18 };
    nuker.ThirdClass = ThirdClassCatalog.ForParent(18).First().Id;
    c.Subclasses.Add(nuker);
    // Class #1: a second Human Mage, currently classless — what may he become?
    c.Subclasses.Add(new Subclass { Slot = 1, BaseClass = BaseClass.Mage });
    c.SwitchSubclass(1);

    string owned = $"{ClassCatalog.Get(18)?.Name} ({ClassCatalog.Get(18)?.Archetype})" +
                   $" / {ThirdClassCatalog.Get(nuker.ThirdClass)?.Name} ({ThirdClassCatalog.Get(nuker.ThirdClass)?.Discipline})";
    Console.WriteLine($"  Class #0 is: {owned}");
    Console.WriteLine("  Class #1 may become ANY 2nd class (archetypes are NOT restricted):");
    foreach (var def in ClassCatalog.OptionsFor(Race.Human, BaseClass.Mage))
        Console.WriteLine($"    OK      {def.Name,-14} ({def.Archetype})");

    // ⚠ Walked over ALL THREE nuker 2nd classes, not just the human's, and that is the point the demo
    // exists to make: the bar is on the DISCIPLINE, so owning a human Magus also bars the elf's
    // Starweaver and the demon's Cinderwitch — different ids, same path. Before `BL-97` this loop ran
    // over one parent and showed a Tempest still OK beside a barred Magus; there is no such escape now,
    // which makes the cross-race half of the rule the only half left to demonstrate.
    Console.WriteLine("  …but every nuker DISCIPLINE is barred, in all three races (the bar is the path,");
    Console.WriteLine("     not the class id) — and the healer line beside it is still open:");
    foreach (var parent in new[] { 18, 12, 6 })                       // Sorcerer / Inquisitor / Witch
        foreach (var tc in ThirdClassCatalog.ForParent(parent))
            Console.WriteLine(c.CanTakeThirdClass(tc.Id)
                ? $"    OK      {tc.Race,-5} {tc.Name,-14} ({tc.Discipline})"
                : $"    BARRED  {tc.Race,-5} {tc.Name,-14} ({tc.Discipline}) — that discipline is already taken");
    foreach (var tc in ThirdClassCatalog.ForParent(17))               // the human healer, for contrast
        Console.WriteLine(c.CanTakeThirdClass(tc.Id)
            ? $"    OK      {tc.Race,-5} {tc.Name,-14} ({tc.Discipline})"
            : $"    BARRED  {tc.Race,-5} {tc.Name,-14} ({tc.Discipline}) — that discipline is already taken");
}
Console.WriteLine();

Console.WriteLine("=== STAT SWAPS: +5 PER STAT, 9 RUNGS, 35kk ===");
Console.WriteLine("  (the DIRECTION rule is gone — owner, playtest-20 #4. Two numeric limits only.)");
Console.WriteLine();

// Walk a shopping list rung by rung the way the server does, printing the running bill.
static void BuySwaps(string who, params (string Id, int Level)[] wants)
{
    var owned = new Dictionary<string, int>();
    long bill = 0;
    Console.WriteLine($"  {who}");
    foreach (var (id, level) in wants)
    {
        int have = owned.TryGetValue(id, out int h) ? h : 0;
        string? clash = SkillCatalog.StatSwapConflict(id, level, owned);
        if (clash is not null)
        {
            Console.WriteLine($"    REFUSED  {NameOf(id),-22} -> Lv{level}   {clash}");
            continue;
        }
        int rungs = SkillCatalog.StatSwapRungsOwned(owned);
        long price = SkillCatalog.StatSwapPriceRange(rungs, rungs + (level - have));
        bill += price;
        owned[id] = level;
        Console.WriteLine($"    bought   {NameOf(id),-22} -> Lv{level}   {price / 1_000_000.0,5:0.#}kk"
                        + $"   (rungs {SkillCatalog.StatSwapRungsOwned(owned)}/{SkillCatalog.StatSwapMaxTotal},"
                        + $" bill {bill / 1_000_000.0:0.#}kk)");
    }
    Console.WriteLine();
}

// HIS worked examples, verbatim from the 2026-08-10 answer.
BuySwaps("his example 1: +5 AGI -5 CON, then +4 ATK -4 CON  (= +5/+4/-9, must cost 35kk)",
    (SkillCatalog.SwapAgiCon, 5), (SkillCatalog.SwapAtkCon, 4));

BuySwaps("his example 2: +5 ATK -5 SPT, +2 WIT -2 SPT, +2 CON -2 AGI",
    (SkillCatalog.SwapAtkMen, 5), (SkillCatalog.SwapWitMen, 2), (SkillCatalog.SwapConAgi, 2));

BuySwaps("\"cannot have +9 AGI -9 CON\" — the per-stat ceiling must refuse it",
    (SkillCatalog.SwapAgiCon, 9));

BuySwaps("the budget must refuse a 10th rung",
    (SkillCatalog.SwapAgiCon, 5), (SkillCatalog.SwapAtkCon, 5));

BuySwaps("cancelling yourself is LEGAL now (nets +1 AGI -1 CON for 15kk)",
    (SkillCatalog.SwapAgiCon, 5), (SkillCatalog.SwapConAgi, 4));

Console.WriteLine("  Who may buy what:");
foreach (var (label, bc, disc) in new (string, BaseClass, Discipline?)[]
         { ("FIGHTER", BaseClass.Fighter, null), ("MAGE", BaseClass.Mage, null),
           ("BUFFER ", BaseClass.Fighter, Discipline.Warchanter) })
    Console.WriteLine($"    {label}  {string.Join(", ", SkillCatalog.StatSwapsFor(bc, disc).Select(NameOf))}");

Console.WriteLine();
Console.WriteLine("  (debug \"learn all skills\" still grants NO swaps — a swap is a build choice.)");
Console.WriteLine();

// =====================================================================================================
//  MOB-AS-PLAYER FEASIBILITY (playtest-18 G3, owner 2026-08-05). MEASURE ONLY — nothing is built and
//  no server code moves on the back of this section. His question, verbatim:
//
//    "before we do and start to build i would like to check if a player vs mob-player where the player
//     is a normal character with balance matrix gear and a mob-player is just an entity that works
//     exactly as a normal player can be done — where the mob-pl have items of lower grade and enchanted
//     + passives for hp etc. Will we be able to manage something like everything-is-a-player logic
//     (just different equipment and skill kits)?"
//
//  So: build the mob as a REAL Kind=Player Entity through the REAL RecomputeDerived, dress it in
//  lower-grade / lower-quality / enchanted gear, and measure how far it lands from the authored
//  MobBaseStats curve the whole game is currently tuned against. Every ratio printed below is
//  "what a type PASSIVE would have to supply to reconcile the two" — x1.00 means gear alone did it.
//
//  What this deliberately does NOT do: invent a mob archetype table, add an HP passive skill, or touch
//  MobCatalog. The point of a feasibility check is to learn the SHAPE of the problem before authoring.
// =====================================================================================================
// =====================================================================================================
//  BL-13 — BOSS PACE. *"boss had 260? He should have 520? Check."* plus his two targets: a FIELD boss
//  should take a 3-DD party about SIX MINUTES, and *"a world [boss] should take an hour for ~10 parties
//  (~50 DDs)"*.
//
//  Everything here is measured through the real entities and the real rank multipliers that
//  GameLoopService.BuildMob applies (MobHpScale / MobPAtkScale / MobAccFlat), so the HP printed is the
//  HP a boss actually spawns with — which is the "check" half of his question.
//
//  ⚠ The party DPS is a CEILING: three champions swinging with no downtime, no deaths, no adds, no
//  boss phases and no time spent walking back in. A real fight is slower, so a boss tuned to exactly
//  360s here will run longer in play. That is the right direction to be wrong in.
// =====================================================================================================
// =====================================================================================================
//  BL-14 — WHAT THE WEAPON A MOB HOLDS IS WORTH. *"Archer is slower but does more dmg, the fast
//  attacking have more crit rate and more atck speed but less dmg."* Attack speed and crit rate came
//  off the weapon already (2026-08-10); per-hit POWER did not, so a slow weapon was a pure nerf.
//  MobWeaponPowerFactor is the missing trade. Read the DPS column: it should be FLAT across the
//  melee rows — that is what "trade, not nerf" means — while hit size and crit rate diverge.
// =====================================================================================================
Console.WriteLine("=== BL-14: a mob's weapon — the trade, measured (level 40, vs a same-level geared champion) ===");
Console.WriteLine($"{"weapon",16} {"atk base",9} {"pwr x",7} {"P.Atk",8} {"crit",7} {"dps",8}");
{
    const int L = 40;
    var victim = BuildPlayer(Race.Human, BaseClass.Fighter, L, warrior: true);
    foreach (var w in new[] { WeaponType.Dual, WeaponType.Sword, WeaponType.Blunt,
                              WeaponType.TwoHandedSword, WeaponType.Bow, WeaponType.None })
    {
        var s = StatCalculator.MobStats(L);
        var m = new Entity { Name = "mob", Kind = EntityKind.Mob, Level = L, InnateWeaponType = w };
        m.Con = s.Con; m.AtkStat = s.Atk; m.Wit = s.Wit; m.Agi = s.Agi; m.Spt = s.Spt;
        m.RecomputeDerived();
        Console.WriteLine($"{w,16} {StatCalculator.WeaponAttackBaseSpeed(w),9} "
            + $"{"x" + StatCalculator.MobWeaponPowerFactor(w).ToString("0.00"),7} "
            + $"{(int)m.EffectiveAttack,8} {(m.CritChance * 100f).ToString("0.0") + "%",7} {Dps(m, victim),8:F1}");
    }
}
Console.WriteLine("  BOW is x1.00 on purpose: MobRole.Archer already pays the same trade explicitly (P.Atk x2,");
Console.WriteLine("     450 range, less P.Def), and charging his one sentence twice would make archers ~3x per arrow.");
Console.WriteLine();

Console.WriteLine("=== BL-13: BOSS PACE — 10 to 30 minutes, for a REAL party ===");
Console.WriteLine("  His band is 600-1800s and *\"the target rises\"*. The party is his own: a tank, a healer");
Console.WriteLine("  and three DDs (2 champions + 1 nuker), all in best-for-tier gear with runes up.");
Console.WriteLine("  Rank multipliers come from MobRankScale — the SAME code BuildMob spawns with.");
Console.WriteLine("  ⚠ THE LEVELS ARE THE ONES THAT EXIST: the lowest boss in the game is the Hollow Crypt's");
Console.WriteLine("     Grave Lich at 44, then the world boss at 60, Dread Knight 65 and Disciple of the Dawn 90.");
Console.WriteLine("     20 is kept as a SHAPE check for the curve, not because anything spawns there.");
Console.WriteLine($"{"Lvl",4} {"rank",6} {"HP x",7} {"boss HP",10} {"P.Def",7} {"party dps",10} {"TTK",8} {"band",14}");
foreach (int L in new[] { 20, 44, 60, 65, 76, 85, 90 })
{
    var party = BuildBossParty(L);
    foreach (var rank in new[] { MobRank.Elite, MobRank.Boss })
    {
        var boss = SpawnRanked(L, rank);
        float dps = PartyDps(party, boss);
        float ttk = boss.MaxHp / Math.Max(0.01f, dps);
        string band = rank != MobRank.Boss ? "-"
            : ttk < 600f ? "TOO FAST" : ttk > 1800f ? "TOO SLOW" : $"ok ({ttk / 60f:F0} min)";
        Console.WriteLine($"{L,4} {rank,6} {"x" + MobRankScale.Hp(rank, L).ToString("0"),7} {boss.MaxHp,10} "
            + $"{(int)boss.EffectiveDefence,7} {dps,10:F0} {ttk,7:F0}s {band,14}");
    }
}
Console.WriteLine("  ⚠ Party DPS is a CEILING — no downtime, no deaths, no adds, no phases, no running back in.");
Console.WriteLine("     A real fight is slower, so a boss measured at the FLOOR of the band plays inside it.");
Console.WriteLine();

// ---------------------------------------------------------------------------------------------
// BL-13, the other two clauses. *"stronger defences, more atk (not one shooting but a tank can feel
// it)"* and *"A healer, tank and dds in a party are a must"*. Neither is a time; both are a BAND, and
// a band needs two ends measured:
//
//   • ONE-SHOT — the boss's biggest single blow as a share of the victim's pool. Over ~90% of a robe
//     is a delete; under ~15% of a tank is a boss that cannot be felt. Both ends are failures.
//   • MANDATORY HEALER — the boss must out-damage what a tank sustains ALONE (his own regen), or the
//     healer is decoration. And the healer must be able to hold him up, or the party cannot win at
//     all. So the boss's damage on the tank has to sit BETWEEN tank regen and healer throughput.
// ---------------------------------------------------------------------------------------------
Console.WriteLine("=== BL-13: IS A PARTY MANDATORY? — the boss's damage against a tank, a robe and a healer ===");
Console.WriteLine("  The BASIC attack is the unavoidable channel and is what \"one shooting\" means. The SLAM is a");
Console.WriteLine("  3s telegraphed AoE in 250 — a robe caught in it made a positioning mistake, not a balance one.");
Console.WriteLine("  \"a tank can feel it\" is measured as TIME, not as a share of one blow: how long the tank");
Console.WriteLine("  lives with nobody healing him. Over a minute and the boss is scenery.");
Console.WriteLine($"{"Lvl",4} {"basic→tank",11} {"%tank",6} {"basic→robe",11} {"%robe",6} {"slam→robe",10} {"%robe",6} "
    + $"{"dps→tank",9} {"unhealed",9} {"heal/s",7} {"verdict",24}");
foreach (int L in new[] { 20, 44, 60, 65, 76, 85, 90 })
{
    var tank   = BuildPlayer(Race.Human, BaseClass.Fighter, L);                 // Knight: shield + heavy
    var robe   = BuildPlayer(Race.Human, BaseClass.Mage, L);                    // Sorcerer: the squishiest
    var healer = BuildPlayer(Race.Human, BaseClass.Mage, L, healer: true,
                             discipline: Discipline.Lightbringer);
    var boss   = SpawnRanked(L, MobRank.Boss);

    int hitTank = BasicHit(boss, tank);
    int hitRobe = BasicHit(boss, robe);
    int slamRobe = BiggestHit(boss, robe);
    // SUSTAINED damage on the tank, with the SHIELD in it. Neither Dps() nor the rest of this tool
    // models block — everywhere else it is measuring a player's OUTPUT, where the defender is a mob
    // and mobs carry no shield. Here the defender is a Knight, and leaving his shield out overstates
    // what the healer has to cover by the whole block channel.
    float onTank = Dps(boss, tank) * BlockFactor(boss, tank);
    // Standing and fighting = the Run stance (x1.0). The flats sit OUTSIDE the multipliers since BL-92.
    float tankRegen = StatCalculator.HpRegenPerSecond(tank.EffectiveCon, L) * tank.HpRegenMult
                      + tank.HpRegenBonus;
    float hps = HealerHps(healer);

    // How long the tank lives with NOBODY healing him — his own regen is all he has.
    float net = onTank - tankRegen;
    float unhealed = net <= 0f ? float.PositiveInfinity : tank.MaxHp / net;
    string verdict =
        hitRobe >= robe.MaxHp ? "ONE-SHOTS A ROBE"
        : hitTank * 100f / tank.MaxHp > 50f ? "two-shots the TANK"
        : unhealed > 60f ? "a tank cannot feel it"
        : hps < onTank ? "healer alone cannot hold"
        : "healer needed + enough";
    Console.WriteLine($"{L,4} {hitTank,11} {hitTank * 100f / tank.MaxHp,5:F0}% {hitRobe,11} "
        + $"{hitRobe * 100f / robe.MaxHp,5:F0}% {slamRobe,10} {slamRobe * 100f / robe.MaxHp,5:F0}% "
        + $"{onTank,9:F0} {(float.IsInfinity(unhealed) ? "never" : unhealed.ToString("F0") + "s"),9} "
        + $"{hps,7:F0} {verdict,24}");
}
Console.WriteLine("  'healer hps' is the top heal on its own cycle — NOT MP-limited, so it is also a ceiling.");
Console.WriteLine("  A boss that a tank out-regens is a boss with no party requirement at all, whatever its HP.");
Console.WriteLine();

// ---------------------------------------------------------------------------------------------
// BL-49: what a rank now PAYS, and whether that matches his ruling.
//
// His rule, 2026-08-14: "bosses should give exp based on how long it takes to kill a normal mob vs
// boss (x1.2~2) - killing a boss gives you twice (or 1.5) the exp for the same time of normal
// fighting". So the column that has to come out right is the LAST one: exp-per-second of boss
// fighting, divided by exp-per-second of trash fighting, at the same level with the same character.
// If that lands on the rank's efficiency constant, the rule is implemented; the rest is bookkeeping.
Console.WriteLine("=== BL-49: WHAT A RANK PAYS — is an hour on bosses worth his 1.2-2 hours on trash? ===");
Console.WriteLine("  exp = MobExpReward(level) x killTimeRatio x rankEfficiency.  killTimeRatio = HP x P.Def,");
Console.WriteLine("  measured off the spawned mob; your DPS cancels, so this is character-independent.");
Console.WriteLine($"{"Lvl",4} {"rank",6} {"trash exp",11} {"rank exp",13} {"exp x",8} {"TTK x",8} {"resp x",7} {"exp/sec x",10} {"was 0.67",11} {"% of level",11} {"/ 9-man",9}");
// ⚠ BOSS ROWS ONLY MEAN SOMETHING AT LEVELS A BOSS EXISTS AT — 44 (Grave Lich), 60 (the world boss),
// 65 (Dread Knight) and 90 (Disciple of the Dawn). 20 is kept as a shape check for the HP curve.
foreach (int L in new[] { 20, 44, 60, 65, 76, 85, 90 })
{
    long trashExp = StatCalculator.MobExpReward(L);
    foreach (var rank in new[] { MobRank.Elite, MobRank.Boss })
    {
        // BL-13 — the rank's own scales, including the DEFENCE term it gained. That term feeds
        // straight into the exp below through defRatio, which is the right answer and is also his
        // `85j` park resolving itself: a boss that takes longer to kill pays more, with no separate
        // ruling and no authored number anywhere.
        string rankName = rank.ToString();
        var mob = SpawnRanked(L, rank);
        float hpMul = MobRankScale.Hp(rank, L);

        // The same two factors the server applies — kept as literals here on purpose: this tool must
        // be able to DISAGREE with the server, or it cannot catch the server drifting.
        float hpRatio  = mob.MaxHp / (float)Math.Max(1, MobBaseStats.Hp(L));
        float defRatio = mob.EffectiveDefence / Math.Max(1f, MobBaseStats.PDef(L));
        float timeRatio = Math.Clamp(hpRatio * Math.Max(0.25f, defRatio), 0.25f, 400f);
        // 🔴 PLAYTEST 23: a boss is 2.0 (the 1.5 was justified by a 5-way party split he has struck out
        // — "the time it takes a 1 dd to kill the boss not 5"), and a THIRD factor is in: what you spend
        // WAITING for the thing to respawn. The world's own authored cadences are the inputs — 22s trash,
        // 60-90s elite, 1800s field boss, 21h world boss.
        float eff = rank == MobRank.Boss ? 2.0f : 1.2f;
        float respawnSeconds = rank == MobRank.Boss ? 1800f : 60f;
        float respawn = Math.Clamp(
            MathF.Pow(respawnSeconds / GameConstants.BaselineRespawnSeconds, GameConstants.RespawnScarcityExponent),
            1f, 12f);

        long rankExp = Math.Max(1L, (long)(trashExp * timeRatio * eff * respawn));
        // What 0.67.0 paid: the same time ratio at the OLD efficiency, with no respawn term at all.
        long oldExp = Math.Max(1L, (long)(trashExp * timeRatio * (rank == MobRank.Boss ? 1.5f : 1.2f)));

        // The number that decides whether the efficiency constant is SANE rather than merely
        // self-consistent: one kill as a fraction of the level it happens at, solo and split 9 ways.
        long next = ExpCurve.ExpToNext(L);
        double pctSolo = rankExp * 100.0 / Math.Max(1L, next);

        Console.WriteLine($"{L,4} {rankName,6} {trashExp,11:N0} {rankExp,13:N0} "
            + $"{"x" + (rankExp / (double)trashExp).ToString("0.0"),8} "
            + $"{"x" + timeRatio.ToString("0.0"),8} "
            + $"{"x" + respawn.ToString("0.00"),7} "
            + $"{"x" + (rankExp / (double)trashExp / timeRatio / respawn).ToString("0.00"),10} "
            + $"{oldExp,11:N0} "
            + $"{pctSolo.ToString("0.0") + "%",11} {(pctSolo / 9.0).ToString("0.0") + "%",9}");
    }
}
Console.WriteLine("  'exp/sec x' IS his ruling and must read 1.20 / 2.00 on every row — anything else means the");
Console.WriteLine("     time ratio and the payout have come apart. 'resp x' is the NEW playtest-23 term: what you");
Console.WriteLine("     are paid for the time spent WAITING for the thing to come back — 22s trash is x1.00 by");
Console.WriteLine("     construction, so ordinary levelling is untouched. 'was 0.67' is the same kill under the");
Console.WriteLine("     shipped 0.67.0 rule: his complaint was a level-90 boss paying 6kk when he wanted 20kk+.");
Console.WriteLine("  '% of level' is ONE kill against ExpToNext at that level; '/ 9-man' is the same kill split");
Console.WriteLine("     across a full party, which is how a boss is actually fought. A boss takes ~100x a trash");
Console.WriteLine("     mob's time, so a large share of a level is CORRECT here — the check is that it is not a");
Console.WriteLine("     whole level per kill, which would make everything else in the game pointless.");
Console.WriteLine();

Console.WriteLine("  ⚠ THE WORLD BOSS IS NOT IN THIS TABLE — there is no such rank. MobRank is Normal/Elite/Boss,");
Console.WriteLine("     and the only thing separating his 21-hour spawn from a 30-minute one is the respawn timer.");
Console.WriteLine("     ~50 DDs for 3600s is ~16.7x the party and 10x the time = ~167x a field boss's HP, which is a");
Console.WriteLine("     new rank, not a bigger number. Not invented here.");
Console.WriteLine();

Console.WriteLine("#####################################################################################");
Console.WriteLine("###  G3 MOB-AS-PLAYER FEASIBILITY — measurement only, nothing built                ###");
Console.WriteLine("#####################################################################################");
Console.WriteLine();

int[] g3Levels = { 20, 40, 60, 80 };
var g3Archs = new[] { Archetype.Tank, Archetype.Warrior, Archetype.Rogue, Archetype.Nuker };

// -----------------------------------------------------------------------------------------------
// 1. WHERE THE PLAYER PIPELINE LANDS. One representative loadout (one grade DOWN, Common quality,
//    +0) on every archetype, against the mob curve of the same level. This is the "just put gear on
//    it" answer with no passives at all.
// -----------------------------------------------------------------------------------------------
Console.WriteLine("=== G3.1: a Kind=Player mob in ONE-GRADE-DOWN Common +0 gear vs the authored mob curve ===");
Console.WriteLine("  ratios are mob-player / MobBaseStats — x1.00 = gear alone reproduces today's mob.");
Console.WriteLine("  'HP x' is the multiplier an HP-type PASSIVE would have to supply (gear grants no HP).");
Console.WriteLine($"{"Lvl",4} {"archetype",9} {"gear",14} | {"HP",7} {"HP x",7} {"P.Def",7} {"x",6} " +
                  $"{"M.Def",7} {"x",6} {"P.Atk",7} {"x",6} {"M.Atk",8} {"x",6}");
foreach (int L in g3Levels)
{
    foreach (var arch in g3Archs)
    {
        var mp = BuildMobPlayer(L, arch, tierDrop: 1, ItemRarity.Common, enchant: 0, kit: false);
        Console.WriteLine($"{L,4} {arch,9} {G3GearLabel(L, 1, ItemRarity.Common, 0),14} | " +
            $"{mp.MaxHp,7} {Ratio(mp.MaxHp, MobBaseStats.Hp(L)),7} " +
            $"{(int)mp.EffectiveDefence,7} {Ratio(mp.EffectiveDefence, MobBaseStats.PDef(L)),6} " +
            $"{(int)mp.EffectiveMagicDefence,7} {Ratio(mp.EffectiveMagicDefence, MobBaseStats.MDef(L)),6} " +
            $"{(int)mp.EffectiveAttack,7} {Ratio(mp.EffectiveAttack, MobBaseStats.PAtk(L)),6} " +
            $"{(int)mp.EffectiveMagicAttack,8} {Ratio(mp.EffectiveMagicAttack, MobBaseStats.MAtk(L)),6}");
    }
}
Console.WriteLine();

// -----------------------------------------------------------------------------------------------
// 2. THE GEAR SEARCH. If this migration is a GEAR-AUTHORING job (his hope), then for each level and
//    archetype there exists some (grade, quality, enchant) whose defences and attack all land near
//    x1.00 at once. Sweep the real ladder and report the best — and how far off it still is.
//
//    HP is reported separately and NOT scored, because no tier armor carries HpBonus: a mob-player's
//    HP is fixed by archetype + level + CON, so it is exactly the thing that has to become a passive.
// -----------------------------------------------------------------------------------------------
Console.WriteLine("=== G3.2: can GEAR ALONE land on the mob curve? (sweep grade x quality x enchant) ===");
Console.WriteLine("  scored on P.Def / M.Def / attack together — 'worst off' is the biggest single miss.");
Console.WriteLine($"{"Lvl",4} {"archetype",9} {"best loadout",22} | {"P.Def x",8} {"M.Def x",8} {"atk x",7} " +
                  $"{"worst off",10} | {"HP x needed",12}");
var g3Qualities = new[] { ItemRarity.Common, ItemRarity.Uncommon, ItemRarity.Rare,
                          ItemRarity.Epic, ItemRarity.Legendary, ItemRarity.Mythic };
foreach (int L in g3Levels)
{
    foreach (var arch in g3Archs)
    {
        bool caster = arch is Archetype.Nuker or Archetype.Healer;
        (string Label, double PDef, double MDef, double Atk, double Score, int Hp) best =
            ("-", 0, 0, 0, double.MaxValue, 0);

        foreach (int drop in new[] { 0, 1, 2, 3 })
            foreach (var q in g3Qualities)
            {
                // The S tier carries only Epic/Legendary/Mythic (ItemCatalog.IsTopHalfOnly), so a
                // low-quality S loadout resolves to NOTHING and would measure a half-naked entity —
                // which scores well by accident. Skip any combination the catalogue cannot dress.
                if (!G3LoadoutExists(G3Tier(L, drop), q)) continue;

                foreach (int ench in new[] { 0, 3, 6, 10, 16 })
                {
                    var e = BuildMobPlayer(L, arch, drop, q, ench, kit: false);
                    double pd = e.EffectiveDefence / (double)MobBaseStats.PDef(L);
                    double md = e.EffectiveMagicDefence / (double)MobBaseStats.MDef(L);
                    double at = caster
                        ? e.EffectiveMagicAttack / (double)MobBaseStats.MAtk(L)
                        : e.EffectiveAttack / (double)MobBaseStats.PAtk(L);
                    // Score = the WORST of the three, in log space, so x2 and x0.5 are equally wrong.
                    double score = Math.Max(Math.Abs(Math.Log(Math.Max(1e-6, pd))),
                                   Math.Max(Math.Abs(Math.Log(Math.Max(1e-6, md))),
                                            Math.Abs(Math.Log(Math.Max(1e-6, at)))));
                    if (score < best.Score)
                        best = (G3GearLabel(L, drop, q, ench), pd, md, at, score, e.MaxHp);
                }
            }

        Console.WriteLine($"{L,4} {arch,9} {best.Label,22} | {"x" + best.PDef.ToString("0.00"),8} " +
            $"{"x" + best.MDef.ToString("0.00"),8} {"x" + best.Atk.ToString("0.00"),7} " +
            $"{(Math.Exp(best.Score) - 1).ToString("P0"),10} | " +
            $"{Ratio(MobBaseStats.Hp(L), best.Hp),12}");
    }
}
Console.WriteLine("  'HP x needed' = MobBaseStats.Hp / the mob-player's own HP — the HP passive's job.");
Console.WriteLine();

// -----------------------------------------------------------------------------------------------
// 3. THE ZONE-LEVEL PROBLEM, measured. "Zone assigns the level" means ONE template spawns at 20 and
//    at 60. Freeze a single authored loadout and walk it up the levels: if the ratios collapse, a
//    per-template loadout cannot exist and the design needs a level -> grade function.
// -----------------------------------------------------------------------------------------------
Console.WriteLine("=== G3.3: ONE frozen loadout across the zone bands (why a per-template kit can't work) ===");
Console.WriteLine("  the same E-grade Common +0 warrior kit, spawned at every level, vs that level's mob curve.");
Console.WriteLine($"{"spawn",6} {"HP",7} {"HP x",7} {"P.Def",7} {"x",7} {"P.Atk",7} {"x",7}");
foreach (int L in new[] { 20, 30, 40, 52, 61, 76, 85 })
{
    // tierDrop is computed from level 20 deliberately: the loadout is FIXED at E grade (t20) whatever
    // the spawn level is — that is what "one template, many zones" means today.
    var mp = BuildMobPlayerFixedTier(L, Archetype.Warrior, tier: 20, ItemRarity.Common, enchant: 0);
    Console.WriteLine($"{L,6} {mp.MaxHp,7} {Ratio(mp.MaxHp, MobBaseStats.Hp(L)),7} " +
        $"{(int)mp.EffectiveDefence,7} {Ratio(mp.EffectiveDefence, MobBaseStats.PDef(L)),7} " +
        $"{(int)mp.EffectiveAttack,7} {Ratio(mp.EffectiveAttack, MobBaseStats.PAtk(L)),7}");
}
Console.WriteLine("  (HP tracks because HP comes from LEVEL, not from the frozen gear — only defence and");
Console.WriteLine("   attack rot, and they are exactly the two the gear was supposed to supply.)");
Console.WriteLine();

// -----------------------------------------------------------------------------------------------
// 4. THE FIGHT. The only measure that decides whether the swap is playable: time to kill, both
//    directions, a REAL geared player (rune buff on, best-for-tier, the state he plays in) against
//    today's mob and against the mob-player. Crit is folded in as an expected multiplier and the
//    miss chance comes from the real accuracy/evasion resolver.
//
//    ⚠ BLOCK IS NOT MODELLED (it needs a roll), so a SHIELDED mob-player survives longer than the
//    numbers below say — and a Tank mob-player carries a shield. Read its column as a ceiling.
// -----------------------------------------------------------------------------------------------
Console.WriteLine("=== G3.4: TIME TO KILL, both directions — geared player vs today's mob vs a mob-player ===");
Console.WriteLine("  'player' = BuildPlayer Champion, best gear for tier, War Rune on (the real play state).");
Console.WriteLine($"{"Lvl",4} {"opponent",22} {"opp HP",8} {"plr dps",8} {"plr TTK",8} | " +
                  $"{"opp dps",8} {"plr TTK'd",10} {"miss->opp",10} {"miss->plr",10}");

// ⚠ The VERDICT block at the end of G3 used to restate these figures as hardcoded prose, and by
// 2026-08-15 all three of its numbers had drifted from the table printed directly above them —
// a reader comparing the two would have believed whichever they read second. Everything the verdict
// claims is accumulated here instead, so the summary can only ever say what the run measured.
float g3TtkMobLo = float.MaxValue, g3TtkMobHi = 0f, g3TtkMpLo = float.MaxValue, g3TtkMpHi = 0f;
float g3TopMobDps = 0f, g3TopMpDpsLo = float.MaxValue, g3TopMpDpsHi = 0f;
int g3TopLevel = g3Levels[^1];

foreach (int L in g3Levels)
{
    var player = BuildPlayer(Race.Human, BaseClass.Fighter, L, warrior: true);

    var opponents = new List<(string Label, Entity E)> { ("today's mob (Kind=Mob)", BuildMobEntity(L)) };
    foreach (var arch in g3Archs)
        opponents.Add(($"mob-player {arch}", BuildMobPlayer(L, arch, tierDrop: 1, ItemRarity.Common, 0, kit: true)));

    bool isTodaysMob = true;
    foreach (var (label, opp) in opponents)
    {
        float pDps = Dps(player, opp);
        float oDps = Dps(opp, player);
        float ttk = opp.MaxHp / Math.Max(0.01f, pDps);
        Console.WriteLine($"{L,4} {label,22} {opp.MaxHp,8} {pDps,8:F0} " +
            $"{ttk,7:F1}s | {oDps,8:F0} " +
            $"{player.MaxHp / Math.Max(0.01f, oDps),9:F1}s " +
            $"{Pct(Miss(player, opp)),10} {Pct(Miss(opp, player)),10}");

        if (isTodaysMob)
        {
            g3TtkMobLo = Math.Min(g3TtkMobLo, ttk); g3TtkMobHi = Math.Max(g3TtkMobHi, ttk);
            if (L == g3TopLevel) g3TopMobDps = oDps;
        }
        else
        {
            g3TtkMpLo = Math.Min(g3TtkMpLo, ttk); g3TtkMpHi = Math.Max(g3TtkMpHi, ttk);
            if (L == g3TopLevel)
            {
                g3TopMpDpsLo = Math.Min(g3TopMpDpsLo, oDps);
                g3TopMpDpsHi = Math.Max(g3TopMpDpsHi, oDps);
            }
        }
        isTodaysMob = false;
    }
    Console.WriteLine();
}

// -----------------------------------------------------------------------------------------------
// 5. THE DIVERGENCES that flipping Kind would cause, each one MEASURED rather than asserted. These
//    are the things "just make it a player" changes by side effect — the reason this is an audit and
//    not a one-line flag flip.
// -----------------------------------------------------------------------------------------------
Console.WriteLine("=== G3.5: what flipping Kind actually changes (measured side effects) ===");
float g3SwingRatio = 1f;   // read by the verdict block below — see the note at G3.4.
{
    const int L = 60;
    var oldMob = BuildMobEntity(L);
    var newMob = BuildMobPlayer(L, Archetype.Warrior, tierDrop: 1, ItemRarity.Common, 0, kit: false);

    // (a) SWING RATE. The attack interval is keyed off Kind in ResolveAttack, so a mob that becomes a
    //     player swings on the PLAYER clock — a silent DPS uplift nobody authored.
    float oldSwing = Math.Max(2, (int)(GameConstants.MobAttackIntervalTicks * oldMob.EffectiveAttackSpeedMultiplier))
                     * GameConstants.TickSeconds;
    float newSwing = Math.Max(2, (int)(GameConstants.PlayerAttackIntervalTicks * newMob.EffectiveAttackSpeedMultiplier))
                     * GameConstants.TickSeconds;
    g3SwingRatio = oldSwing / newSwing;
    Console.WriteLine($"  swing interval   Kind=Mob {oldSwing:F2}s -> Kind=Player {newSwing:F2}s "
        + $"(x{g3SwingRatio:0.00} swings/sec"
        + (Math.Abs(g3SwingRatio - 1f) < 0.005f ? ", no drift — the two clocks agree)" : ", unauthored)"));

    // (b) THE NEUTRAL-OPPONENT BENCHMARK. Mob AGI is flat 30 on purpose (owner 2026-08-02) so a
    //     same-level pair sits at the 5% floor both ways, and MobAgiReference IS the human-fighter
    //     base — so a fighter-shaped mob-player inherits the same number by construction. The mage
    //     archetypes are the ones to watch: their class base AGI is not the reference.
    var refPlayer = BuildPlayer(Race.Human, BaseClass.Fighter, L, warrior: true);
    Console.WriteLine($"  AGI benchmark    MobStats AGI is flat {StatCalculator.MobAgiReference} "
        + $"(Kind=Mob: acc {oldMob.Accuracy} eva {(int)oldMob.EffectiveEvasion}); a geared player misses it "
        + $"{Pct(Miss(refPlayer, oldMob))}");
    foreach (var arch in g3Archs)
    {
        var a = BuildMobPlayer(L, arch, 1, ItemRarity.Common, 0, kit: false);
        Console.WriteLine($"                     {arch,-8} AGI {a.Agi,3} acc {a.Accuracy,4} eva {(int)a.EffectiveEvasion,4}"
            + $"  player misses it {Pct(Miss(refPlayer, a)),4}, it misses the player {Pct(Miss(a, refPlayer)),4}"
            + $"{(a.Agi == StatCalculator.MobAgiReference ? "" : "   << OFF THE BENCHMARK")}");
    }

    // (c) GRADE PENALTY. Player-only, and it only bites gear ABOVE your grade — so lower-grade mob
    //     gear is inert here. Prove it rather than assume it.
    int worstGap = 0;
    foreach (var it in newMob.Inventory)
        if (it.Equipped && ItemCatalog.Get(it.DefId) is ItemDef d)
            worstGap = Math.Max(worstGap, GradePenalty.Gap(d, L));
    Console.WriteLine($"  grade penalty    worst gap across the mob-player's kit = {worstGap} "
        + $"(x{GradePenalty.FactorForGap(worstGap):0.00}) — lower-grade gear is inert, as designed");

    // (d) THE HP SOURCE. This is the twin of the 0.42.3 mob-regen bug: the player HP curve is
    //     exponential in CON and gated by an archetype modifier mobs do not have.
    Console.WriteLine($"  HP source        Kind=Mob reads MobBaseStats.Hp({L}) = {MobBaseStats.Hp(L)} DIRECT;");
    Console.WriteLine($"                   mob-player runs StatCalculator.MaxHp(CON {newMob.Con}, "
        + $"g@40+ {StatCalculator.HpGrowth(BaseClass.Fighter, Archetype.Warrior, null).T3:0.00}) = {newMob.MaxHp}");
    foreach (var arch in g3Archs)
        Console.WriteLine($"                     {arch,-8} = "
            + $"{BuildMobPlayer(L, arch, 1, ItemRarity.Common, 0, kit: false).MaxHp,6}"
            + $"  (needs an HP passive of {Ratio(MobBaseStats.Hp(L), BuildMobPlayer(L, arch, 1, ItemRarity.Common, 0, kit: false).MaxHp)})");

    // (e) THE SKILL KIT. "Different skill kits" is not free — the class tables carry MASTERIES, and a
    //     mob-player that learns them gets stat floors no mob was ever meant to have.
    var bare = BuildMobPlayer(L, Archetype.Warrior, 1, ItemRarity.Common, 0, kit: false);
    var kitted = BuildMobPlayer(L, Archetype.Warrior, 1, ItemRarity.Common, 0, kit: true);
    Console.WriteLine($"  learned kit      Warrior L{L}: {kitted.LearnedSkills.Count} skills learned changes "
        + $"P.Atk {(int)bare.EffectiveAttack} -> {(int)kitted.EffectiveAttack} "
        + $"({Ratio(kitted.EffectiveAttack, bare.EffectiveAttack)}), "
        + $"P.Def {(int)bare.EffectiveDefence} -> {(int)kitted.EffectiveDefence} "
        + $"({Ratio(kitted.EffectiveDefence, bare.EffectiveDefence)})");
    Console.WriteLine("                   -> a mob kit must be an AUTHORED list, never ClassSkills.Cumulative.");

    // (f) WHERE THE ATTACK GAP COMES FROM. The first thing anyone asks about G3.1/G3.6 is why a fully
    //     geared mob-player only reaches a fifth of the mob's authored P.Atk. Half of it is the RUNE:
    //     a player's expected play state includes the War Rune (+100% P.Atk), and a mob holds no runes.
    //     Measure the split so the weapon-type passive is authored against the right number.
    var runed = BuildMobPlayer(L, Archetype.Warrior, 1, ItemRarity.Common, 0, kit: false);
    if (SkillCatalog.Get(SkillCatalog.WarRuneBuff) is SkillDef rune)
    {
        runed.Buffs.Add(new Game.Server.Simulation.BuffInstance
        {
            Effect = rune.Effect, Magnitudes = rune.Magnitudes,
            TicksRemaining = int.MaxValue, Name = rune.Name, Key = rune.BuffKey,
        });
        runed.RecomputeDerived();
    }
    Console.WriteLine($"  attack gap       Warrior L{L} P.Atk {(int)newMob.EffectiveAttack} bare, "
        + $"{(int)runed.EffectiveAttack} with a War Rune, vs MobBaseStats.PAtk({L}) = {MobBaseStats.PAtk(L)}");
    Console.WriteLine($"                   -> the rune alone closes {Ratio(runed.EffectiveAttack, newMob.EffectiveAttack)} "
        + $"of the {Ratio(MobBaseStats.PAtk(L), newMob.EffectiveAttack)} needed; the rest is the "
        + "weapon-type passive's job.");

    // (g) EXP / DROPS. Both are level-driven, not stat-driven — so the migration does NOT re-roll them
    //     by itself. Worth stating, because it is the one place this is cheaper than it looks.
    Console.WriteLine($"  exp / gold       StatCalculator.MobExpReward({L}) = {StatCalculator.MobExpReward(L):N0}"
        + $" — level-driven, UNAFFECTED by which pipeline made the stats");
}
Console.WriteLine();

// -----------------------------------------------------------------------------------------------
// 6. THE PASSIVE TABLE THE DESIGN WOULD HAVE TO AUTHOR. G3.2 shows no gear combination closes all
//    three gaps at once, so the type passives are not a garnish — they carry the reconciliation.
//    The question that decides the whole migration is therefore: is the multiplier each passive must
//    supply CONSTANT across levels?
//
//      constant  -> one flat passive per type. His five families are enough, and this is authoring.
//      drifting  -> the passive itself needs a per-level table, i.e. the mob curve re-authored in a
//                   second place. That is new machinery, and it is where "everything is a player"
//                   stops being free.
//
//    Held on ONE loadout (one grade down, Common, +0) so the drift measured is the PIPELINE's, not a
//    gear choice's. 'spread' = max/min of the multiplier across the four levels: 1.0 = perfectly flat.
// -----------------------------------------------------------------------------------------------
Console.WriteLine("=== G3.6: the multiplier each TYPE PASSIVE must supply, and whether it holds across levels ===");
Console.WriteLine($"{"archetype",9} {"stat",7} | {"L20",7} {"L40",7} {"L60",7} {"L80",7} | {"spread",7}  verdict");
foreach (var arch in g3Archs)
{
    bool caster = arch is Archetype.Nuker or Archetype.Healer;
    var stats = new (string Name, Func<Entity, int, double> Need)[]
    {
        ("HP",    (e, L) => MobBaseStats.Hp(L)   / (double)e.MaxHp),
        ("P.Def", (e, L) => MobBaseStats.PDef(L) / (double)e.EffectiveDefence),
        ("M.Def", (e, L) => MobBaseStats.MDef(L) / (double)e.EffectiveMagicDefence),
        (caster ? "M.Atk" : "P.Atk",
                  (e, L) => (caster ? MobBaseStats.MAtk(L) : MobBaseStats.PAtk(L))
                            / (double)(caster ? e.EffectiveMagicAttack : e.EffectiveAttack)),
    };

    foreach (var (name, need) in stats)
    {
        var vals = g3Levels
            .Select(L => need(BuildMobPlayer(L, arch, 1, ItemRarity.Common, 0, kit: false), L))
            .ToArray();
        double spread = vals.Max() / Math.Max(1e-6, vals.Min());
        string verdict = spread < 1.25 ? "FLAT — one authored number does it"
                       : spread < 2.0  ? "drifts — needs a per-band table"
                                       : "DRIFTS HARD — a second mob curve in disguise";
        Console.WriteLine($"{arch,9} {name,7} | " + string.Join(" ", vals.Select(v => $"x{v,-6:0.00}"))
            + $"| {spread,6:0.00}x  {verdict}");
    }
    Console.WriteLine();
}

// -----------------------------------------------------------------------------------------------
// 7. HIS COUNTER-ARGUMENT, playtest 24 (2026-08-16). He rejected G3.2's "no gear closes it" and named
//    the reason it was wrong: *"a human fighter with S grade Mace enchanted to +60 ... and B grade
//    leather only have the same pDef and twice less p atk ... if we make the elite passive x2 p atk
//    and hp boost we can make him the same values"*.
//
//    Two things G3.2 could not do, and he is right about both:
//      (a) its enchant axis stopped at +16 — the realistic PLAYER ceiling, but a mob's enchant is an
//          authored number, not something it has to farm scrolls for. His is +60.
//      (b) it moved every slot together, so "over-enchanted weapon + under-grade armor" — the exact
//          shape that fixes the mirror — was never constructed.
//    G3.2 is left untouched so its old reading stays attributable. This is the same question asked
//    the way he asked it, and the last column is HIS test: does what remains fit inside a x2 passive?
// -----------------------------------------------------------------------------------------------
Console.WriteLine("=== G3.7: HIS loadout — weapon and armor swept SEPARATELY, enchant to +60 ===");
Console.WriteLine("  best = the smallest worst-miss over armor(grade x quality x ench) x weapon(grade x quality x ench).");
Console.WriteLine("  'passive needed' = what a mob passive must still supply. His hypothesis: all of it inside x2.");
Console.WriteLine($"{"Lvl",4} {"archetype",9} {"armor",16} {"weapon",16} | {"P.Def x",8} {"M.Def x",8} " +
                  $"{"atk x",7} | {"passive needed (pd/md/atk/hp)",30} {"fits x2?",9}");
var g37Qualities = new[] { ItemRarity.Common, ItemRarity.Uncommon, ItemRarity.Rare,
                           ItemRarity.Epic, ItemRarity.Legendary, ItemRarity.Mythic };
int[] g37ArmorEnch  = { 0, 8, 16 };
int[] g37WeaponEnch = { 0, 16, 30, 45, 60 };
int g37Fits = 0, g37Rows = 0;
double g37WorstMiss = 0, g37WorstAtkPassive = 0;
foreach (int L in g3Levels)
{
    foreach (var arch in g3Archs)
    {
        bool caster = arch is Archetype.Nuker or Archetype.Healer;
        (string A, string W, double PDef, double MDef, double Atk, double Score, int Hp) best =
            ("-", "-", 0, 0, 0, double.MaxValue, 0);

        foreach (int aDrop in new[] { 0, 1, 2, 3 })
            foreach (var aq in g37Qualities)
            {
                if (!G3LoadoutExists(G3Tier(L, aDrop), aq)) continue;
                foreach (int aEnch in g37ArmorEnch)
                    foreach (int wDrop in new[] { 0, 1, 2, 3 })
                        foreach (var wq in g37Qualities)
                        {
                            if (!G3WeaponExists(arch, G3Tier(L, wDrop), wq)) continue;
                            foreach (int wEnch in g37WeaponEnch)
                            {
                                var e = BuildMobPlayerSplit(L, arch, G3Tier(L, aDrop), aq, aEnch,
                                                                     G3Tier(L, wDrop), wq, wEnch);
                                double pd = e.EffectiveDefence      / (double)MobBaseStats.PDef(L);
                                double md = e.EffectiveMagicDefence / (double)MobBaseStats.MDef(L);
                                double at = caster
                                    ? e.EffectiveMagicAttack / (double)MobBaseStats.MAtk(L)
                                    : e.EffectiveAttack      / (double)MobBaseStats.PAtk(L);
                                double score = Math.Max(Math.Abs(Math.Log(Math.Max(1e-6, pd))),
                                               Math.Max(Math.Abs(Math.Log(Math.Max(1e-6, md))),
                                                        Math.Abs(Math.Log(Math.Max(1e-6, at)))));
                                if (score < best.Score)
                                    best = ($"t{G3Tier(L, aDrop)} {aq}{(aEnch > 0 ? " +" + aEnch : "")}",
                                            $"t{G3Tier(L, wDrop)} {wq}{(wEnch > 0 ? " +" + wEnch : "")}",
                                            pd, md, at, score, e.MaxHp);
                            }
                        }
            }

        // What a passive must still supply on top of the best loadout. >1 = the passive adds.
        double needPd = 1.0 / Math.Max(1e-6, best.PDef);
        double needMd = 1.0 / Math.Max(1e-6, best.MDef);
        double needAt = 1.0 / Math.Max(1e-6, best.Atk);
        double needHp = MobBaseStats.Hp(L) / (double)Math.Max(1, best.Hp);
        // His x2: a passive that only ever DOUBLES. A need below 1.0 means the gear over-delivers and
        // no passive can pull it down, so that is a miss too — hence the two-sided window.
        bool fits = new[] { needPd, needMd, needAt, needHp }.All(n => n >= 0.5 && n <= 2.0);
        if (fits) g37Fits++;
        g37Rows++;
        g37WorstMiss = Math.Max(g37WorstMiss, Math.Exp(best.Score) - 1);
        g37WorstAtkPassive = Math.Max(g37WorstAtkPassive, needAt);

        Console.WriteLine($"{L,4} {arch,9} {best.A,16} {best.W,16} | " +
            $"{"x" + best.PDef.ToString("0.00"),8} {"x" + best.MDef.ToString("0.00"),8} " +
            $"{"x" + best.Atk.ToString("0.00"),7} | " +
            $"{$"x{needPd:0.00} / x{needMd:0.00} / x{needAt:0.00} / x{needHp:0.00}",30} " +
            $"{(fits ? "YES" : "no"),9}");
    }
}
Console.WriteLine($"  {g37Fits}/{g37Rows} rows land inside his x2 passive on all four stats at once.");
Console.WriteLine($"  worst single miss after the best split loadout: {g37WorstMiss:P0} "
                + $"(G3.2, coupled slots and +16 max, could not get under 185-221% at L80).");
Console.WriteLine($"  the biggest ATTACK passive still needed anywhere: x{g37WorstAtkPassive:0.00}.");
Console.WriteLine();

// 8. THE DEMO ITSELF (BL-47 step 2, 2026-08-16). Everything above is a SEARCH — it asks what a
//    loadout could do. This asks what the five creatures actually authored in MobCatalog DO, spawned
//    the way the server spawns them, against the ordinary creature of the same level standing next to
//    them in the Proving Grounds.
//
//    🔑 It builds them through Entity.ApplyMobBuild — the SAME method GameLoopService.BuildMob calls —
//    rather than reproducing the construction here. A tool that rebuilds a creature by hand ends up
//    measuring one the server does not spawn, which is how a fitted number goes stale without anyone
//    touching it.
Console.WriteLine("=== G3.8: THE DEMO — the five authored creatures vs their curve twins ===");
Console.WriteLine("     (x1.00 = the player-built creature lands exactly on today's mob curve)");
Console.WriteLine($"{"creature",-26} {"lvl",3} {"HP",10} {"P.Atk",8} {"P.Def",8} {"M.Def",8} {"M.Atk",8}");
foreach (var (demoId, twinId) in new[]
         {
             ("demo_goblin_raider",       "demo_curve_40"),
             ("demo_goblin_raider_elder", "demo_curve_45"),
             ("demo_lich",                "demo_curve_60"),
             ("demo_seraph",              "demo_curve_80"),
             ("demo_seraph_rune",         "demo_curve_80"),
         })
{
    var d = SpawnTemplate(demoId);
    var t = SpawnTemplate(twinId);
    var type = MobCatalog.Get(demoId);
    Console.WriteLine($"{type.Name,-26} {type.Level,3} {d.MaxHp,10:N0} {(int)d.EffectiveAttack,8} "
        + $"{(int)d.EffectiveDefence,8} {(int)d.EffectiveMagicDefence,8} {(int)d.EffectiveMagicAttack,8}");
    Console.WriteLine($"{"  vs " + MobCatalog.Get(twinId).Name,-26} {"",3} "
        + $"{Ratio(d.MaxHp, t.MaxHp),10} {Ratio(d.EffectiveAttack, t.EffectiveAttack),8} "
        + $"{Ratio(d.EffectiveDefence, t.EffectiveDefence),8} {Ratio(d.EffectiveMagicDefence, t.EffectiveMagicDefence),8} "
        + $"{Ratio(d.EffectiveMagicAttack, t.EffectiveMagicAttack),8}");
}
Console.WriteLine();
Console.WriteLine("  #1 vs #2 (Raider 40 vs 45) is the +-5 BAND question: the same authored loadout, five");
Console.WriteLine("     levels apart. If both rows sit near x1.00, one loadout covers a band and his");
Console.WriteLine("     \"prefixed 100+ mobs with +-5 lvl ranges\" needs no level->grade function at all.");
Console.WriteLine("  #4 vs #5 (Seraph vs Runebearer) is the RUNE question: identical creatures except one");
Console.WriteLine("     carries a x1.55 authored attack passive and the other a held War Rune (+100% P.Atk)");
Console.WriteLine("     and no passive. The P.Atk gap between those two rows is the whole answer.");
Console.WriteLine();

Console.WriteLine("=== G3: VERDICT INPUTS (read the tables, not this line) ===");
Console.WriteLine(g37Fits == g37Rows
    ? "  * GEAR PLUS A x2 PASSIVE CLOSES IT (G3.7) — his loadout shape (over-enchanted weapon over"
    + "\n    under-grade armor) leaves every stat inside a x2 passive at every level tested. G3.2's"
    + "\n    'no gear combination works' was true of ITS sweep (coupled slots, +16 ceiling), not of gear."
    : g37Fits > 0
    ? $"  * PARTLY (G3.7): {g37Fits}/{g37Rows} archetype-levels land inside his x2 passive once the weapon"
    + "\n    and armor are dressed separately and the weapon can go past +16. G3.2's flat 'no gear"
    + $"\n    combination works' was overstated — the biggest attack passive still needed is x{g37WorstAtkPassive:0.00}."
    : "  * NO gear combination closes all three gaps at once — and G3.7 says that survives BOTH of his"
    + "\n    levers: splitting the weapon from the armor and enchanting to +60. The player pipeline is the"
    + "\n    MIRROR of the mob curve — armor over-delivers P.Def/M.Def while the weapon under-delivers"
    + "\n    attack, at every level and every grade. Gear cannot flip that sign.");
Console.WriteLine("  * so the TYPE PASSIVES carry the reconciliation, not the gear — and G3.6 says the");
Console.WriteLine("    multipliers they must supply DRIFT with level, so each passive needs a per-band table.");
Console.WriteLine("    That is his 'levelled passive with a name per level' — the design already assumes it.");
Console.WriteLine("  * HP is the cleanest case: archetype + level alone lands within x0.96-x1.16 for Tank and");
Console.WriteLine("    Warrior. Rogue (x1.4) and especially Nuker (x3.3) need real HP passives.");
Console.WriteLine("  * a FROZEN per-template loadout rots across zone bands (G3.3) — a level->grade function is");
Console.WriteLine("    mandatory, not optional, if 'the zone assigns the level' survives.");
Console.WriteLine($"  * the FIGHTS are already playable (G3.4): mob-player TTKs land {g3TtkMpLo:F1}-{g3TtkMpHi:F1}s "
                + $"against today's {g3TtkMobLo:F1}-{g3TtkMobHi:F1}s.");
Console.WriteLine($"    Their damage OUT is the weak side — at L{g3TopLevel} a mob-player deals "
                + $"{g3TopMpDpsLo:F0}-{g3TopMpDpsHi:F0} dps where today's mob deals {g3TopMobDps:F0},");
Console.WriteLine("    which is the same attack gap seen in G3.1/G3.2 showing up in the fight.");
Console.WriteLine(Math.Abs(g3SwingRatio - 1f) < 0.005f
    ? "  * the SWING CLOCK no longer moves when Kind flips (G3.5a, x1.00) — it did once, and that is now"
    + "\n    a closed side effect rather than a cost of the migration."
    : $"  * flipping Kind moves the SWING CLOCK by side effect (G3.5a, x{g3SwingRatio:0.00}).");
Console.WriteLine("    The AGI benchmark survives for fighter archetypes by construction — MobAgiReference");
Console.WriteLine("    IS the human-fighter base. See docs/design/MobsAsPlayers.md for what this all means.");
Console.WriteLine();

// =====================================================================================================
//  C1 — CRIT DAMAGE (flat), BLOWS and [Double]                    docs/design/CritBlowAndDouble.md
//
//  Three changes measured against what they replaced, because all three moved at once (2026-08-05):
//    * crit damage "+80" in the CSVs is FLAT ATTACK added inside the ratio on a crit, not "x2.8";
//    * a landed BLOW is now computed WITH the crit-damage values (it used to return base damage,
//      so a dagger's whole crit-damage ladder did nothing at all);
//    * [Double] chance is a pure ATK curve capped 25%, not max(AGI,ATK)/1000 capped 30%.
//  OLD columns re-create the previous arithmetic here so the magnitude of the swing is visible.
// =====================================================================================================
Console.WriteLine("=== C1: [Double] chance — ATK curve (new) vs max(AGI,ATK)/1000 cap 30% (old) ===");
{
    int[] atks = { 30, 35, 40, 45, 50, 55, 60, 70 };
    Console.Write("  ATK stat ");   foreach (int a in atks) Console.Write($"{a,8}");
    Console.WriteLine();
    Console.Write("  new      ");
    foreach (int a in atks) Console.Write($"{StatCalculator.PhysicalDoubleChance(a) * 100,7:F1}%");
    Console.WriteLine();
    Console.Write("  old      ");
    foreach (int a in atks) Console.Write($"{Math.Clamp(a * 0.001f, 0f, 0.30f) * 100,7:F1}%");
    Console.WriteLine("     (old ALSO read AGI, so a rogue sat far higher than this row)");
    Console.WriteLine("  his anchors: 30 -> 2.5%, 40 -> 10%, 60+ -> 25% (he wrote 50 -> 15%; the formula gives 17.5%)");
}
Console.WriteLine();

Console.WriteLine("=== C1: ROGUE — the five crit-damage rungs (duals + light, best gear, vs a same-level mob) ===");
{
    Console.WriteLine("  lvl | P.Atk  crit%  ATK | critFlat |  basic hit  avg OLD  avg NEW |   blow hit  avg OLD  avg NEW");
    foreach (int lvl in new[] { 20, 24, 28, 32, 36 })
    {
        var r = BuildRogue(lvl);
        var mob = BuildMobEntity(lvl);
        int pDef = Math.Max(1, (int)mob.EffectiveDefence);
        float csvFlat = r.CritDamageFlat;                       // = the CSV's "+N" for this rung
        float oldMult = 2f + csvFlat / 100f;                    // what the code used to do with it

        int basicHit = StatCalculator.PhysicalDamage((int)r.EffectiveBasicAttack, 0, pDef, lvl);
        float basicNew = CritFactor(r.CritChance, 2f *
            StatCalculator.CritFlatFactor(r.EffectiveBasicAttack, csvFlat));
        float basicOld = CritFactor(r.CritChance, oldMult);

        var (blow, blvl) = TopSkill(r, SkillEffect.PhysicalDamage);
        float blowHit = 0f, blowOld = 0f, blowNew = 0f;
        if (blow is not null)
        {
            int power = blow.PowerAt(blvl);
            blowHit = StatCalculator.PhysicalDamage((int)r.EffectiveAttack, power, pDef, lvl);
            blowNew = SkillHitFactor(r, blow, power, 2f);
            // OLD: a landed blow returned base damage untouched, then doubled off max(AGI,ATK).
            float oldDbl = Math.Clamp(Math.Max(r.EffectiveAgi, r.AtkStat) * 0.001f, 0f, 0.30f);
            blowOld = blow.BlowOnCrit
                ? r.CritChance * (1f + oldDbl) + (1f - r.CritChance) * blow.BlowFailFraction
                : CritFactor(r.CritChance, oldMult);
        }
        Console.WriteLine($"  {lvl,3} | {(int)r.EffectiveAttack,5} {r.CritChance * 100,5:F1}% {r.AtkStat,4} |"
            + $" {csvFlat,8:F0} | {basicHit,10} {basicHit * basicOld,8:F0} {basicHit * basicNew,8:F0} |"
            + $" {blowHit,10:F0} {blowHit * blowOld,8:F0} {blowHit * blowNew,8:F0}"
            + (blow is null ? "  (no skill)" : $"  ({NameOf(blow.Id)} {blvl})"));
    }
    Console.WriteLine("  avg = expected damage per hit with crit / blow / [Double] folded in.");
}
Console.WriteLine();

Console.WriteLine("=== C1: WARRIOR 2H — same rungs (crit dmg +35/+48/+64/+84/+106), no blow ===");
{
    Console.WriteLine("  lvl | P.Atk  crit% | critFlat | basic hit  avg OLD  avg NEW | skill hit  avg OLD  avg NEW");
    foreach (int lvl in new[] { 20, 24, 28, 32, 36 })
    {
        // BuildPlayer dresses a fighter in a 1H sword + shield — which grants the 2H mastery
        // NOTHING (its profile is gated on WeaponType.TwoHanded). Swap to the greatsword, or this
        // table measures a warrior who isn't wearing the thing being measured.
        var w = BuildPlayer(Race.Human, BaseClass.Fighter, lvl, warrior: true);
        w.Inventory.RemoveAll(i => i.DefId.StartsWith("sword1h") || i.DefId.StartsWith("shield"));
        Equip(w, $"sword2h_t{GearTier(lvl)}");
        w.RecomputeDerived();
        var mob = BuildMobEntity(lvl);
        int pDef = Math.Max(1, (int)mob.EffectiveDefence);
        float csvFlat = w.CritDamageFlat;
        float oldMult = 2f + csvFlat / 100f;

        int basicHit = StatCalculator.PhysicalDamage((int)w.EffectiveBasicAttack, 0, pDef, lvl);
        float basicNew = CritFactor(w.CritChance, 2f *
            StatCalculator.CritFlatFactor(w.EffectiveBasicAttack, csvFlat));
        float basicOld = CritFactor(w.CritChance, oldMult);

        var (sk, sl) = TopSkill(w, SkillEffect.PhysicalDamage);
        float skHit = 0f, skOld = 0f, skNew = 0f;
        if (sk is not null)
        {
            int power = sk.PowerAt(sl);
            skHit = StatCalculator.PhysicalDamage((int)w.EffectiveAttack, power, pDef, lvl);
            skNew = SkillHitFactor(w, sk, power, 2f);
            skOld = sk.CanDouble
                ? CritFactor(Math.Clamp(Math.Max(w.EffectiveAgi, w.AtkStat) * 0.001f, 0f, 0.30f), 2f)
                : CritFactor(w.CritChance, oldMult);
        }
        Console.WriteLine($"  {lvl,3} | {(int)w.EffectiveAttack,5} {w.CritChance * 100,5:F1}% | {csvFlat,8:F0} |"
            + $" {basicHit,9} {basicHit * basicOld,8:F0} {basicHit * basicNew,8:F0} |"
            + $" {skHit,9:F0} {skHit * skOld,8:F0} {skHit * skNew,8:F0}"
            + (sk is null ? "" : $"  ({NameOf(sk.Id)} {sl})"));
    }
    Console.WriteLine("  a [Double] skill never took crit damage in EITHER model — it is a flat x2 by design.");
}
Console.WriteLine();

Console.WriteLine("=== C1: sustained DPS after the change — ROGUE (duals) vs WARRIOR (2H), same-level mob ===");
{
    Console.WriteLine("  the rogue's whole ladder is now these crit-damage rungs, so this is the row to watch.");
    Console.WriteLine("  lvl | rogue dps | warrior dps | rogue/warrior | mob HP | rogue TTK | warrior TTK");
    foreach (int lvl in new[] { 20, 24, 28, 32, 36 })
    {
        var r = BuildRogue(lvl);
        var w = BuildPlayer(Race.Human, BaseClass.Fighter, lvl, warrior: true);
        w.Inventory.RemoveAll(i => i.DefId.StartsWith("sword1h") || i.DefId.StartsWith("shield"));
        Equip(w, $"sword2h_t{GearTier(lvl)}");
        w.RecomputeDerived();
        var mob = BuildMobEntity(lvl);
        float rd = PhysDps(r, mob), wd = PhysDps(w, mob);
        int hp = MobBaseStats.Hp(lvl);
        Console.WriteLine($"  {lvl,3} | {rd,9:F1} | {wd,11:F1} | {rd / Math.Max(1f, wd),13:F2}x |"
            + $" {hp,6} | {hp / Math.Max(1f, rd),8:F1}s | {hp / Math.Max(1f, wd),10:F1}s");
    }
}
Console.WriteLine();

Console.WriteLine("=== C2: CRIT RATE — his IG model, decomposed (docs/design/CritBlowAndDouble.md §5) ===");
{
    Console.WriteLine("  crit = (110 x weaponFactor x agiMod  x  passives x buffs  +  flat) x debuffs x enemyLightArmor");
    Console.WriteLine("  numbers on HIS 0-1000 scale (1000 = 100%), cap 500. mult = every passive AND buff folded.");
    Console.WriteLine("  build                     lvl | AGI agiMod | weapon    base | mult  | flat | FINAL      %");
    void CritRow(string label, int lvl, Entity e)
    {
        int agi = (int)e.EffectiveAgi;
        Console.WriteLine($"  {label,-25} {lvl,3} | {agi,3} x{StatCalculator.CritAgiMod(agi),4:F2} |"
            + $" {e.WeaponType.Base(),-8} {StatCalculator.PhysicalCritBase(agi, e.WeaponType) * 1000f,4:F0} |"
            + $" x{e.CritRateMult,4:F2} | {e.CritRateFlat * 1000f,4:F0} | {e.CritChance * 1000f,5:F0} {e.CritChance * 100f,6:F1}%");
    }
    // Re-arm a build with a different weapon: the crit base IS the weapon, so a rogue's bow and his
    // duals are different rows, and the warrior's blunt is the case the flat term has to carry.
    Entity Rearm(Entity e, string weaponId)
    {
        e.Inventory.RemoveAll(i => ItemCatalog.Get(i.DefId)?.Slot == EquipSlot.Weapon);
        Equip(e, weaponId);
        e.RecomputeDerived();
        return e;
    }
    // Add the top crit-rate BUFFS: Focus rung 6 (x1.30 — his own x1.3) and Harmony of the Warrior
    // (x1.75, where his ladder assumed x2). The NPC-buffer "Focus" is only a wrapper whose ChildBuffs
    // point at the ladder rung, so the rung is what carries the magnitudes — buff the rung, not it.
    Entity Buffed(Entity e)
    {
        foreach (var id in new[] { SkillCatalog.Rung(SkillCatalog.FamCritRate, 6), SkillCatalog.NpcHarmonyWarrior })
            if (SkillCatalog.Get(id) is { Magnitudes: not null } def)
                e.Buffs.Add(new Game.Server.Simulation.BuffInstance
                {
                    Effect = def.Effect, Magnitudes = def.Magnitudes,
                    TicksRemaining = int.MaxValue, Name = def.Name, Key = def.BuffKey,
                });
        e.RecomputeDerived();
        return e;
    }
    Entity Warrior(int lvl) => BuildPlayer(Race.Human, BaseClass.Fighter, lvl, warrior: true);
    foreach (int lvl in new[] { 20, 28, 36, 44 })
    {
        int t = GearTier(lvl);
        CritRow("rogue, duals", lvl, BuildRogue(lvl));
        CritRow("rogue, bow", lvl, Rearm(BuildRogue(lvl), $"bow_t{t}"));
        CritRow("warrior, 2H sword", lvl, Rearm(Warrior(lvl), $"sword2h_t{t}"));
        CritRow("warrior, 2H blunt", lvl, Rearm(Warrior(lvl), $"blunt2h_t{t}"));
        Console.WriteLine("   ...the same four, fully BUFFED (Focus x1.30 + Harmony x1.75):");
        CritRow("rogue, duals +buffs", lvl, Buffed(BuildRogue(lvl)));
        CritRow("rogue, bow +buffs", lvl, Buffed(Rearm(BuildRogue(lvl), $"bow_t{t}")));
        CritRow("warrior, sword +buffs", lvl, Buffed(Rearm(Warrior(lvl), $"sword2h_t{t}")));
        CritRow("warrior, blunt +buffs", lvl, Buffed(Rearm(Warrior(lvl), $"blunt2h_t{t}")));
        Console.WriteLine();
    }
    Console.WriteLine("  cap is StatCaps.PhysicalCritRate = 500 (50%). Two things to read here:");
    Console.WriteLine("   - flat is 0 on every row, and NOTHING in the game fills it any more. The weapon's");
    Console.WriteLine("     crit-rate ATTRIBUTE moved into 'mult' on 2026-08-07 (checklist 0d): it was landing");
    Console.WriteLine("     here as value/100, i.e. a maxed roll was +30 PERCENTAGE POINTS (+300 on this scale,");
    Console.WriteLine("     vs IG's +109 at S) and, being flat, it flattened the 3:2:1 weapon identity below.");
    Console.WriteLine("     These rows roll no attributes, so they show the BASE model only — to see the roll,");
    Console.WriteLine("     multiply: sword x1.90 at its new 90 ceiling, dual/bow x1.30 at 30.");
    Console.WriteLine("     His model's flat 'heavy set +127' — the term that is supposed to carry the BLUNT");
    Console.WriteLine("     warrior, who cannot multiply his way anywhere — still does not exist.");
    Console.WriteLine("   - AGI is 30 on every row because AGI is per RACE+BASE CLASS: only an ELF fighter (35)");
    Console.WriteLine("     moves it, and no armor set in these tiers carries a Agi line. See the elf row below.");
    // The one build that actually exercises agiMod today.
    var elf = BuildRogue(36);
    elf.Agi = StatCalculator.GetBaseStats(Race.Elf, BaseClass.Fighter).Agi;
    elf.RecomputeDerived();
    CritRow("ELF rogue, duals", 36, elf);
    CritRow("ELF rogue +buffs", 36, Buffed(elf));
}
Console.WriteLine();

Console.WriteLine("=== C3: M8 audit — every physical-damage skill and what it is ALLOWED to roll ===");
{
    Console.WriteLine("  \"If a skill is not described as Can Crit or Can Double it doesn't do it.\" Both flags are");
    Console.WriteLine("  OPT-IN and exclusive; a blow's crit gate is BlowOnCrit. Anything showing '-  -  -' lands FLAT");
    Console.WriteLine("  (it can still miss and still be blocked) — check that against the skill's own description.");
    Console.WriteLine("  skill                     | blow | crit | dbl | critMod | description says...");
    foreach (var def in SkillCatalog.AllSkills
                 .Where(d => d.Effect.HasFlag(SkillEffect.PhysicalDamage))
                 .OrderBy(d => d.Name))
    {
        string desc = def.Description ?? "";
        bool saysCrit = desc.Contains("critical", StringComparison.OrdinalIgnoreCase);
        bool saysDouble = desc.Contains("DOUBLE", StringComparison.Ordinal)
                       || desc.Contains("[Double]", StringComparison.Ordinal);
        string claim = (saysCrit ? "crit " : "") + (saysDouble ? "double" : "");
        Console.WriteLine($"  {def.Name,-25} |  {(def.BlowOnCrit ? "Y" : "-"),-3} |  {(def.CanCrit ? "Y" : "-"),-3} |"
            + $"  {(def.CanDouble ? "Y" : "-"),-2} | {(def.CritRateMod == 1f ? "-" : "x" + def.CritRateMod.ToString("0.0")),-7} |"
            + $" {(claim.Length == 0 ? "(neither)" : claim)}");
    }
    Console.WriteLine("  a row whose flags and description disagree is an AUTHORING bug, not a formula bug.");
}
Console.WriteLine();

// =====================================================================================================
//  E: ENCHANT — the +0 vs +16 jump, per playstyle (him, 2026-08-11)
// =====================================================================================================
// He asked for this by name when he gave the flat enchant table: "that needs testing — the dps of a
// non-enchanted warrior/dagger/tank/mage/bow vs fully enchanted, to see the dmg jump". It is the
// measurement that decides whether the bow's grade-scaled row (10..20 per enchant, against a flat 6/8
// for everything else) is the archer identity he wants or simply the best weapon in the game:
// "as archer they rely on basic attack and acc so a more P.Atk jump is better, while the others should
// rely more on crit/skills".
//
// Every subject wears the FULL loadout of its tier at Mythic quality, first at +0 and then with every
// piece at +16 — which is the honest comparison, because his table pays per ITEM and a player enchants
// a set, not a weapon.
Console.WriteLine("=== E: ENCHANT — what a full +16 set is worth, per playstyle (his flat table) ===");
{
    Console.WriteLine("  Full loadout at Mythic quality: weapon, body, 3 accessories, 5 jewels (+shield for the");
    Console.WriteLine("  tank), at +0 then all at +16. 'atk' is P.Atk, or M.Atk for the mage. 'dps' is the real");
    Console.WriteLine("  resolver vs a same-level mob. 'ttk' = seconds a +0 warrior of the same level needs to");
    Console.WriteLine("  kill the subject — the defensive half of the table (HP and defence move together).");

    (string Label, Archetype Arch, string? Weapon, bool Caster)[] roster =
    {
        ("tank 1H+shield",  Archetype.Tank,    null,    false),
        ("warrior 2H",      Archetype.Warrior, null,    false),
        ("dagger duals",    Archetype.Rogue,   "duals", false),
        ("archer bow",      Archetype.Rogue,   "bow",   false),
        ("mage staff",      Archetype.Nuker,   null,    true),
    };

    static string Jump(double a, double b, string fmt = "0") =>
        $"{a.ToString(fmt),6} ->{b.ToString(fmt),7} ({(a <= 0 ? "  -  " : (b / a - 1).ToString("+0%;-0%"))})";

    foreach (int lvl in new[] { 52, 80 })
    {
        int tier = GearTier(lvl);
        var mob = BuildMobEntity(lvl);
        // The reference killer never changes between the two columns, so any movement in ttk is the
        // subject's own defence, not a stronger attacker.
        var killer = BuildMobPlayerFixedTier(lvl, Archetype.Warrior, tier, ItemRarity.Mythic, 0, kit: true);

        Console.WriteLine();
        Console.WriteLine($"  --- level {lvl}, t{tier} = {EnchantRules.GradeName(EnchantRules.GradeOf(tier))} grade "
            + $"(armour +{EnchantRules.ArmorDefPerEnchant}/ench P.Def, "
            + $"+{EnchantRules.HpDelta(ItemCatalog.Get($"heavy_t{tier}")!, 1)}/ench HP) ---");
        Console.WriteLine($"  {"",-15} | {"atk",-22} | {"dps vs mob",-22} | {"Max HP",-22} | {"ttk (s)",-22}");
        foreach (var (label, arch, weapon, caster) in roster)
        {
            var a = BuildMobPlayerFixedTier(lvl, arch, tier, ItemRarity.Mythic, 0, kit: true, weaponOverride: weapon);
            var b = BuildMobPlayerFixedTier(lvl, arch, tier, ItemRarity.Mythic,
                                            EnchantRules.MaxEnchant, kit: true, weaponOverride: weapon);
            double atk0 = caster ? a.EffectiveMagicAttack : a.EffectiveAttack;
            double atk1 = caster ? b.EffectiveMagicAttack : b.EffectiveAttack;
            double ttk0 = a.MaxHp / Math.Max(0.01f, Dps(killer, a));
            double ttk1 = b.MaxHp / Math.Max(0.01f, Dps(killer, b));
            Console.WriteLine($"  {label,-15} | {Jump(atk0, atk1)} | {Jump(Dps(a, mob), Dps(b, mob))} "
                + $"| {Jump(a.MaxHp, b.MaxHp)} | {Jump(ttk0, ttk1, "0.0")}");
        }
    }
    Console.WriteLine();
    Console.WriteLine("  Read it as: does the archer's dps column pull away from the other four? His bow row is");
    Console.WriteLine("  worth 2.5x a greatsword's per enchant at S (+320 vs +128), on top of the bow already");
    Console.WriteLine("  carrying the highest base P.Atk — that is deliberate, but this is where it shows up.");
    Console.WriteLine("  ttk moves for everyone because the HP row is FLAT and class-blind: the same +1920 at S");
    Console.WriteLine("  (five pieces for a tank, who also wears a shield) doubles a caster's pool and adds a");
    Console.WriteLine("  third to a tank's. That is his ruling, not a bug: 'an enchant is just an offset of the");
    Console.WriteLine("  norm — same for all'.");
}
Console.WriteLine();

// =====================================================================================================
//  §M: CRAFTING MATERIALS — the faucet, measured per GRADE GROUP.
//
//  Owner, 2026-08-13, and it BLOCKS `BL-05`'s recipe costs: *"make me a balance matrix file with all the
//  dropped mats for a lvl .. a kills/h + the drops of mats and rarity .. in each grade group .. so we can
//  decide the mats consumption per item ... now looking at it 1000-2000 legend mats for a single 75%
//  chance to fail mytiic S is a bit harsh ... ofc depending on the mats drop"*.
//
//  GRADE is the right axis because the crafting rungs ARE the grades now (L1=E … L6=S, F uncraftable).
//  Every number comes from the real drop tables through Marginals/EffectiveChance and from the real refine
//  recipes in RecipeCatalog — the mat ladder is 5+1+1 today, and if Recipes.cs changes this table moves
//  with it. Nothing here is hand-multiplied; that is the whole point of pricing it in the tool.
// =====================================================================================================
Console.WriteLine("#####################################################################################");
Console.WriteLine("###  M: CRAFTING MATERIALS — kills/h and mat yield, per GRADE GROUP                ###");
Console.WriteLine("#####################################################################################");
Console.WriteLine();

// The grade ladder is GradePenalty's, never re-listed here — a band is [floor, next floor).
int topMobLevel = MobCatalog.Templates.Where(m => !m.Dummy && m.Level > 0).Max(m => m.Level);
var gradeBands = GradePenalty.GradeLevels.Select((floor, i) => (
        Name: GradePenalty.GradeNames[i],
        Floor: floor,
        Top: i + 1 < GradePenalty.GradeLevels.Length
            ? GradePenalty.GradeLevels[i + 1] - 1
            : Math.Min(ExpCurve.MaxLevel, topMobLevel)))
    .ToArray();

// Seconds to kill one same-level mob, averaged over the five sheets the offline farm actually fields —
// E4's clock and E4's roster, so one class can never decide a band's rate on its own.
static float BandTtk(int level)
{
    var r = FarmRosterBuffed(level);
    var mob = r[0].E;
    return r.Skip(1).Average(x => mob.MaxHp / Math.Max(0.01f, Dps(x.E, mob)));
}

// kills/h is NOT 3600/TTK. TTK is one to three seconds at every level, and his measured farm is ~84/h, so
// the loop is almost entirely walking, respawn and retarget. Calibrate that overhead ONCE against the only
// empirical anchor there is — the playtest-18 mage, whose 350k of pure coin pins the kill count — and then
// let TTK move it per band. Same measurement the economy section above calibrates on.
double coinAtAnchor = PerKill(PlaytestLevel).Coin;
double anchorKills = CoinObserved / Math.Max(1, coinAtAnchor);
double anchorKph = anchorKills / 14.5;
double loopOverhead = Math.Max(0, 3600.0 / Math.Max(1, anchorKph) - BandTtk(PlaytestLevel));
double KillsPerHour(int level) => 3600.0 / (loopOverhead + BandTtk(level));

// Mats per kill BY RARITY, straight off the real tables through Marginals — which runs the same four
// knobs the kill roll runs (per-item x per-group x global x rune). Averaged over the templates nearest
// the level, like every other economy row in this file.
static double[] MatsPerKill(int level)
{
    var byRarity = new double[Crafting.MaterialRarities.Length];
    var near = MobsNear(level);
    foreach (var mob in near)
        foreach (var (e, chance) in Marginals(mob.Drops ?? Array.Empty<DropEntry>(), level))
            if (ItemCatalog.Get(e.ItemId) is { Slot: EquipSlot.Material } def)
                byRarity[(int)def.Rarity] +=
                    chance * ((e.MinQty + e.MaxQty) / 2.0) * RateConfig.World.DropAmount / near.Length;
    return byRarity;
}

string RarityHeader() => string.Concat(Crafting.MaterialRarities.Select(r => $"{r,10}"));

Console.WriteLine("=== M1: the farm, per grade band — MATS PER KILL (all five types together) ===");
Console.WriteLine($"{"gr",3} {"levels",8} {"mob",20} {"TTK",6} {"kills/h",8} | {RarityHeader()}");
foreach (var (name, floor, top) in gradeBands)
{
    var mk = MatsPerKill(top);
    Console.WriteLine($"{name,3} {floor + "-" + top,8} {MobsNear(top)[0].Name,20} {BandTtk(top),5:F1}s "
        + $"{KillsPerHour(top),8:F0} | {string.Concat(mk.Select(v => $"{v,10:0.####}"))}");
}
Console.WriteLine($"  kills/h = 3600 / (TTK + {loopOverhead:F0}s loop overhead), the overhead calibrated on his own");
Console.WriteLine($"  14.5 h farm at level {PlaytestLevel + 1} ({anchorKph:F0} kills/h). Combat is NOT the farm's clock — walking is.");
Console.WriteLine("  ⚠ Each row is the band's TOP level, i.e. its BEST case. The mat rarity gates sit at 30 / 60 / 76");
Console.WriteLine("    (uncommon / rare / epic), so the bottom of E, of B and of A yield less than the row shows.");
Console.WriteLine("  ⚠ 'all five types together' — the mats group splits three ways by mob CATEGORY (two flavored");
Console.WriteLine("    types + Gem), so any ONE type is about a third of the Common column, and a refine's two CROSS");
Console.WriteLine("    inputs come from a different creature family or from trade.");
Console.WriteLine();

Console.WriteLine("=== M2: the same thing PER HOUR ===");
Console.WriteLine($"{"gr",3} {"levels",8} {"kills/h",8} | {RarityHeader()}");
foreach (var (name, floor, top) in gradeBands)
{
    double kph = KillsPerHour(top);
    Console.WriteLine($"{name,3} {floor + "-" + top,8} {kph,8:F0} | "
        + string.Concat(MatsPerKill(top).Select(v => $"{v * kph,10:0.##}")));
}
Console.WriteLine("  🔴 Legendary and Mythic are ZERO in every band: no mob drops them. The only source is REFINING,");
Console.WriteLine("     which is what M3 prices — and it is why the top two rungs behave nothing like the bottom four.");
Console.WriteLine();

// What ONE material COSTS in kills at a level: the cheaper of farming it directly and REFINING it out of
// the rung below. The refine cost is READ OFF the real recipe (5 of itself + 2 cross today), never a
// hardcoded 7 — and the recursion is what makes a Mythic mat price itself in Common mats automatically.
static double KillsPerMat(ItemRarity rarity, double[] perKill)
{
    double direct = perKill[(int)rarity] > 0 ? 1.0 / perKill[(int)rarity] : double.PositiveInfinity;
    var recipe = RecipeCatalog.All.FirstOrDefault(r => r.Id.StartsWith("refine_")
        && ItemCatalog.Get(r.OutputId) is { Slot: EquipSlot.Material } d && d.Rarity == rarity);
    double refine = double.PositiveInfinity;
    if (recipe is not null)
    {
        double sum = 0;
        foreach (var input in recipe.Inputs)
            if (ItemCatalog.Get(input.ItemId) is { Slot: EquipSlot.Material } m)
                sum += input.Qty * KillsPerMat(m.Rarity, perKill);
        refine = sum / Math.Max(1, recipe.OutputQty);
    }
    return Math.Min(direct, refine);
}

static string Span(double hours) =>
    double.IsInfinity(hours) ? "never"
    : hours >= 8760 ? $"{hours:N0} h ({hours / 8760:0.0} y)"
    : hours >= 10 ? $"{hours:N0} h"
    : $"{hours:0.0} h";

Console.WriteLine("=== M3: what ONE material of each rarity costs, at each band (drop vs refine — cheaper wins) ===");
Console.WriteLine($"{"gr",3} {"levels",8} | {RarityHeader()}   (kills per 1 mat)");
foreach (var (name, floor, top) in gradeBands)
{
    var mk = MatsPerKill(top);
    Console.WriteLine($"{name,3} {floor + "-" + top,8} | " + string.Concat(
        Crafting.MaterialRarities.Select(r =>
        {
            double k = KillsPerMat(r, mk);
            return double.IsInfinity(k) ? $"{"never",10}" : $"{k,10:N0}";
        })));
}
Console.WriteLine("  Refining is NEVER the cheap path for anything that drops: the drop ladder thins by ~4-6x a rung");
Console.WriteLine("  while a refine costs 7 in for 1 out. Above EPIC nothing drops at all, so Legendary is a forced 7x");
Console.WriteLine("  and Mythic a forced 49x on the rarest thing in the table.");
Console.WriteLine();

// His authored ranges (2026-08-13), which are the thing this section exists to PRICE. Deliberately NOT
// shipped anywhere — no gear recipe carries them yet, because they were given as *"depending on drop
// rates/amount"*. This table is the measurement that resolves each range.
var craftRungs = new (string Grade, ItemRarity Bulk, int BulkLo, int BulkHi,
                      ItemRarity Accent, int AccLo, int AccHi, float Mythic, float Fail)[]
{
    ("E", ItemRarity.Common,     500, 1000, ItemRarity.Uncommon,  10, 10, 0.50f, 0.10f),
    ("D", ItemRarity.Uncommon,   100,  500, ItemRarity.Rare,       2,  5, 0.45f, 0.15f),
    ("C", ItemRarity.Rare,       100,  200, ItemRarity.Epic,       1,  2, 0.40f, 0.20f),
    ("B", ItemRarity.Epic,       100,  200, ItemRarity.Legendary,  1,  2, 0.30f, 0.30f),
    ("A", ItemRarity.Legendary,  100,  200, ItemRarity.Mythic,     1,  2, 0.20f, 0.50f),
    ("S", ItemRarity.Legendary, 1000, 2000, ItemRarity.Mythic,    10, 20, 0.05f, 0.75f),
};

// A rung is priced at ITS OWN grade band (where that gear is worn), and again at the TOP of the world —
// because mat yield only improves with level, the endgame column is the FLOOR of what the rung can cost.
(double Lo, double Hi) RungHours(
    (string Grade, ItemRarity Bulk, int BulkLo, int BulkHi,
     ItemRarity Accent, int AccLo, int AccHi, float Mythic, float Fail) rung, int level)
{
    var mk = MatsPerKill(level);
    double kph = KillsPerHour(level);
    double bulk = KillsPerMat(rung.Bulk, mk), acc = KillsPerMat(rung.Accent, mk);
    return ((rung.BulkLo * bulk + rung.AccLo * acc) / kph,
            (rung.BulkHi * bulk + rung.AccHi * acc) / kph);
}

int endgame = gradeBands[^1].Top;
Console.WriteLine("=== M4: HIS authored mat ranges, priced in FARM HOURS per craft ATTEMPT ===");
Console.WriteLine($"{"rung",5} {"recipe (his ranges)",44} {"hours @ own band",30} {"hours @ " + endgame,24}");
foreach (var rung in craftRungs)
{
    var band = gradeBands.First(b => b.Name == rung.Grade);
    var own = RungHours(rung, band.Top);
    var end = RungHours(rung, endgame);
    string recipe = $"{rung.BulkLo}-{rung.BulkHi} {rung.Bulk} + {rung.AccLo}-{rung.AccHi} {rung.Accent}";
    Console.WriteLine($"{rung.Grade + " (" + band.Floor + "+)",5} {recipe,44} "
        + $"{Span(own.Lo) + " - " + Span(own.Hi),30} {Span(end.Lo) + " - " + Span(end.Hi),24}");
}
Console.WriteLine("  'own band' = farming the grade you are crafting for. '@ " + endgame + "' = the best MAT YIELD in the game.");
Console.WriteLine("  Where the endgame column reads HIGHER (the E and D rungs), it is not a worse faucet — those rungs");
Console.WriteLine("  need nothing that gates late, and the low bands simply kill faster (shorter TTK, same 40s walk).");
Console.WriteLine();

Console.WriteLine("=== M5: the same rungs AFTER the fail table — hours per SUCCESS and per MYTHIC piece ===");
Console.WriteLine($"{"rung",5} {"fail",6} {"attempts",9} {"->Mythic",9} | {"per success (own band)",34} {"per MYTHIC (own band)",30}");
foreach (var rung in craftRungs)
{
    var band = gradeBands.First(b => b.Name == rung.Grade);
    var own = RungHours(rung, band.Top);
    double attempts = 1.0 / Math.Max(0.01f, 1f - rung.Fail);
    double toMythic = 1.0 / Math.Max(0.001f, rung.Mythic);
    Console.WriteLine($"{rung.Grade,5} {rung.Fail * 100,5:F0}% {attempts,8:0.0}x {toMythic,8:0.0}x | "
        + $"{Span(own.Lo * attempts) + " - " + Span(own.Hi * attempts),34} "
        + $"{Span(own.Lo * toMythic) + " - " + Span(own.Hi * toMythic),30}");
}
Console.WriteLine("  'attempts' = 1/(1-fail) — a fail EATS the mats, so the sticker price is not the price. '->Mythic'");
Console.WriteLine("  is 1/P(mythic): a craft that succeeds still lands on Legendary most of the time.");
Console.WriteLine();

// The solve he actually needs to rule on: the top two rungs are unreachable because their mats have no
// faucet at all. So ask the question backwards — if an A or S craft is to cost a stated number of hours,
// what would a Legendary / Mythic mat have to drop at? That is a number he can accept or move.
Console.WriteLine("=== M6: THE SOLVE — what a Legendary/Mythic mat would have to drop at to hit a target ===");
Console.WriteLine($"  (at level {endgame}, {KillsPerHour(endgame):F0} kills/h, holding his authored quantities)");
Console.WriteLine($"{"rung",5} {"target/attempt",15} {"kills available",16} {"Legendary/kill",16} {"Mythic/kill",14}");
foreach (var rung in craftRungs.Where(r => r.Grade is "A" or "S"))
    foreach (double targetHours in new[] { 20.0, 50.0, 100.0 })
    {
        double kills = targetHours * KillsPerHour(endgame);
        // Split the budget the way the recipe's own value splits it: the bulk mat carries the pile, the
        // accent the handful. Solve each side for the per-kill rate that spends exactly its share.
        double bulkShare = 0.75, accShare = 0.25;
        double legPerKill = rung.BulkHi / (kills * bulkShare);
        double mythPerKill = rung.AccHi / (kills * accShare);
        Console.WriteLine($"{rung.Grade,5} {targetHours,14:F0}h {kills,16:N0} {legPerKill,16:0.####} {mythPerKill,14:0.####}");
    }
Console.WriteLine("  Read the last two columns as drops per kill; the farm budget is split 75/25 bulk/accent, which is an");
Console.WriteLine("  assumption of mine, not his. For scale, the CURRENT top of the ladder is the Epic mat at "
    + $"{MatsPerKill(endgame)[(int)ItemRarity.Epic]:0.####}/kill,");
Console.WriteLine($"  and Common — the most common thing in the game — runs at {MatsPerKill(endgame)[(int)ItemRarity.Common]:0.##}/kill. An S rung asking a");
Console.WriteLine("  Legendary mat to drop MORE OFTEN THAN ONCE PER KILL is the arithmetic saying the faucet is not the");
Console.WriteLine("  lever here. The quantities are — which is M7.");
Console.WriteLine();

// The counter-proposal, and the reason it can be one: his six ranges all share the SAME SHAPE — 100 bulk
// to 1 accent, in every rung, top of range and bottom. So the ladder has exactly one free number per rung,
// and pricing it against a target is a solve rather than a redesign. The target curve below (doubling per
// rung, from a 5 h E) is MINE and is the one thing here he should overrule if he wants a different feel;
// everything else falls out of the drop tables.
Console.WriteLine("=== M7: THE COUNTER-PROPOSAL — his 100:1 shape, re-solved for a target cost per SUCCESS ===");
Console.WriteLine($"{"rung",5} {"target/success",15} {"his range",22} {"solved",9} {"accent",7}  {"verdict",22}");
double target = 5;
foreach (var rung in craftRungs)
{
    var band = gradeBands.First(b => b.Name == rung.Grade);
    var mk = MatsPerKill(band.Top);
    double kph = KillsPerHour(band.Top);
    double attempts = 1.0 / Math.Max(0.01f, 1f - rung.Fail);
    // One "unit" of his shape is 100 bulk + 1 accent. Solve how many units the target budget buys.
    double unitKills = 100 * KillsPerMat(rung.Bulk, mk) + KillsPerMat(rung.Accent, mk);
    double bulk = 100 * (target / attempts * kph / unitKills);
    string verdict =
        bulk >= rung.BulkLo && bulk <= rung.BulkHi ? "INSIDE his range"
        : bulk < 100 ? $"{rung.BulkHi / Math.Max(0.01, bulk):N0}x smaller — shape breaks"
        : $"{rung.BulkHi / Math.Max(0.01, bulk):0.#}x smaller";
    Console.WriteLine($"{rung.Grade,5} {target,14:F0}h {rung.BulkLo + "-" + rung.BulkHi + " " + rung.Bulk,22} "
        + $"{bulk,9:N0} {Math.Max(1, Math.Round(bulk / 100)),7:N0}  {verdict,22}");
    target *= 2;
}
Console.WriteLine("  🔑 E, D and C land INSIDE his own authored ranges — the bottom half of his ladder is already right,");
Console.WriteLine("     and it needs nothing from me except picking the number inside the range he already wrote.");
Console.WriteLine("  🔴 B, A and S all solve BELOW 100 bulk — at which point his own 100:1 shape stops being expressible,");
Console.WriteLine("     because there is no longer a pile for the accent mat to accent. That is the finding: the top three");
Console.WriteLine("     rungs are not mis-numbered, they are un-authorable at the current faucet, and the break starts at");
Console.WriteLine("     B — one rung EARLIER than the S he flagged. Either Legendary and Mythic mats get a real source (a");
Console.WriteLine("     boss/elite drop, the way the top enchant scrolls did in D1), or those rungs must be authored in a");
Console.WriteLine("     different currency than 'a pile of the rung below'.");
Console.WriteLine("  ⚠ S solves to the same pile as A despite twice the target: its 75% fail rate eats the entire");
Console.WriteLine("     doubling on its own. The fail table and the mat cost are one knob, not two.");
Console.WriteLine("  The target column (5 h doubling to 160 h per finished item) is MY proposal and the one number in");
Console.WriteLine("  this table to argue with; the rest is the drop table doing arithmetic. M8 replaces it with HIS.");
Console.WriteLine();

// =====================================================================================================
//  M8-M11: HIS RULING of 2026-08-13, and the three things it turned out to rest on that M1-M7 never
//  measured — the per-TYPE mat rate, the consumable faucet, and whether elites/bosses can be a faucet.
// =====================================================================================================

// 🔑 A "day" is 12 FARM HOURS — his auto+offline allowance — NOT 24. Owner, 2026-08-13: *"2-3h of farming
// for E grade per weapon craft, 3-5h per D grade, 5-10 C, 12-1d B, 1-3d A, 7-14d S ... 1d of farming to
// mean the full 12h(auto+offline) -- so a 1-3d A grade to be a wall clock of 12-36h of non stop farming --
// that seems fair (atleast for now) -- to caft a single weapon"*.
//
// These are per FINISHED weapon, i.e. AFTER the fail table — which is why M8 divides by attempts where M4
// multiplied by them. ⚠ His "12-1d" for B collapses to a POINT under his own definition of a day; it is
// read here as 1-2 days. Both ends solve into the same pile, so the ambiguity changes no conclusion, but
// it is the one number in his message to re-confirm before anything is authored.
const double FarmDay = 12.0;
var hisCurve = new (string Grade, double Lo, double Hi)[]
{
    ("E", 2, 3), ("D", 3, 5), ("C", 5, 10),
    ("B", FarmDay, 2 * FarmDay), ("A", FarmDay, 3 * FarmDay), ("S", 7 * FarmDay, 14 * FarmDay),
};

// Span() is built for the multi-year numbers of M4/M5 and floors everything under 10 h to one decimal.
// The mat columns below run from 20 seconds to 6 hours, so they need their own formatter.
static string Hrs(double h) =>
    double.IsInfinity(h) ? "never"
    : h >= 100 ? $"{h:N0}h"
    : h >= 1 ? $"{h:0.0}h"
    : $"{h * 60:0.#}m";

Console.WriteLine("=== M8: HIS target curve — the same 100:1 solve, run against HIS hours ===");
Console.WriteLine($"{"rung",5} {"per finished",13} {"per attempt",13} | {"1 bulk mat",11} {"1 accent",10} | "
    + $"{"solved bulk",13} {"accent",7}  {"vs his own range",28}");
foreach (var (grade, lo, hi) in hisCurve)
{
    var rung = craftRungs.First(r => r.Grade == grade);
    var band = gradeBands.First(b => b.Name == grade);
    var mk = MatsPerKill(band.Top);
    double kph = KillsPerHour(band.Top);
    double attempts = 1.0 / Math.Max(0.01f, 1f - rung.Fail);
    // Hours to obtain ONE mat of each side of the recipe. This is the column that carries the finding:
    // once a single bulk mat costs hours, no target curve can buy a PILE of them.
    double bulkH = KillsPerMat(rung.Bulk, mk) / kph, accH = KillsPerMat(rung.Accent, mk) / kph;
    double unitH = 100 * bulkH + accH;                       // one "unit" of his 100:1 shape
    double bLo = 100 * (lo / attempts) / unitH, bHi = 100 * (hi / attempts) / unitH;
    string verdict =
        bHi < 100 ? "🔴 under 100 — shape breaks"
        : bLo >= rung.BulkLo && bHi <= rung.BulkHi ? "inside his range"
        : bHi < rung.BulkLo ? "BELOW his own range"
        : "straddles his range";
    Console.WriteLine($"{grade,5} {$"{lo:0.#}-{hi:0.#}h",13} {$"{lo / attempts:0.#}-{hi / attempts:0.#}h",13} | "
        + $"{Hrs(bulkH),11} {Hrs(accH),10} | {$"{bLo:N0}-{bHi:N0}",13} "
        + $"{Math.Round(bHi / 100),7:N0}  {verdict,28}");
}
Console.WriteLine("  'per attempt' = his finished-item target x (1-fail): a fail eats the mats, so the budget for ONE");
Console.WriteLine("  attempt is SMALLER than the price of the item. 'solved bulk' is how many bulk mats that budget buys");
Console.WriteLine("  while keeping his 100 bulk : 1 accent shape.");
Console.WriteLine("  🔑 His curve is ~2.5x CHEAPER than M7's at E/D/C, and cutting the target SHRINKS the pile — so the");
Console.WriteLine("     break in his own 100:1 shape moved DOWN a rung, from B to C. E and D now solve BELOW the ranges");
Console.WriteLine("     he authored himself.");
Console.WriteLine("  🔴 Read the '1 bulk mat' column: no target curve can fix the top. When one Legendary mat costs hours");
Console.WriteLine("     by itself, a 36 h budget buys a HANDFUL, not a pile. The top rungs are not mis-priced — they are");
Console.WriteLine("     quantised too coarsely to price. Either those mats get a faucet (M11), or the top three rungs are");
Console.WriteLine("     authored as few-and-precious and the top FAIL rates come down to match.");
Console.WriteLine();

// His armor/jewel rule (2026-08-13): every non-weapon slot is a FRACTION of the weapon, authored so that a
// full set of either costs exactly one weapon. *"gloves/boots to cost weapon_hours(WH) divided by 10 for 1
// item; helmet WH/3.33; body WH/2 ... rings WH/10, ear WH/5, neck WH/2.5"*.
// ✅ The slot counts are the REAL ones: ArmorSlot = {Head, Body, Gloves, Boots} and JewelType = {Ring,
//    Earring, Necklace} worn 2/2/1. Both sums land on 1.000.
// 🔴 EquipSlot.Shield is in NEITHER sum. It is a real slot and his message did not price it.
var pieces = new (string Name, double Divisor, int Worn, string Set)[]
{
    ("weapon", 1.00, 1, "weapon"),
    ("body", 2.0, 1, "armor"), ("helmet", 3.33, 1, "armor"), ("gloves", 10.0, 1, "armor"), ("boots", 10.0, 1, "armor"),
    ("necklace", 2.5, 1, "jewel"), ("earring", 5.0, 2, "jewel"), ("ring", 10.0, 2, "jewel"),
};

Console.WriteLine("=== M9: his slot FRACTIONS — farm hours per piece, and what a full character costs ===");
Console.WriteLine($"{"rung",5} {"weapon",12} {"body",11} {"helmet",11} {"gloves",10} {"boots",10} "
    + $"{"neck",10} {"ear x2",10} {"ring x2",10} | {"FULL CHAR",12}");
foreach (var (grade, lo, hi) in hisCurve)
{
    double mid = (lo + hi) / 2.0;
    string Cell(string name) => Hrs(mid / pieces.First(p => p.Name == name).Divisor);
    double full = pieces.Sum(p => p.Worn * mid / p.Divisor);
    Console.WriteLine($"{grade,5} {Cell("weapon"),12} {Cell("body"),11} {Cell("helmet"),11} {Cell("gloves"),10} "
        + $"{Cell("boots"),10} {Cell("necklace"),10} {Cell("earring"),10} {Cell("ring"),10} | {Hrs(full),12}");
}
foreach (string set in new[] { "armor", "jewel" })
    Console.WriteLine($"  the {set} set sums to {pieces.Where(p => p.Set == set).Sum(p => p.Worn / p.Divisor):0.000} weapons"
        + " — his fractions are exact.");
Console.WriteLine("  Each cell is at the MIDPOINT of his range for that rung. A full character = weapon + armor set +");
Console.WriteLine("  jewel set = 3 weapons, so the S column is the real endgame number: three S weapons of farming.");
Console.WriteLine("  🔴 The SHIELD has no fraction. It is its own EquipSlot, outside both sums, and needs one.");
Console.WriteLine();

// The 3x question. M1's caveat says "any one type is about a third of the Common column" — that is true for
// COMMON and false for everything above it, and the difference decides whether every hour in M8 triples.
// StandardDrops splits the guaranteed mats group three ways (mats.A, mats.B, Gem) but authors the higher
// rarities as INDEPENDENT rolls on A and B only: Uncommon A .08 + B .05, Rare A .03, Epic A .005. So Rare
// and Epic are SINGLE-TYPE per mob — and WHICH type they are is decided by the mob's CATEGORY.
static double[,] MatsByType(int lo, int hi)
{
    var res = new double[Crafting.MaterialTypes.Length, Crafting.MaterialRarities.Length];
    var band = MobCatalog.Templates.Where(m => !m.Dummy && m.Level >= lo && m.Level <= hi).ToArray();
    if (band.Length == 0) return res;
    foreach (var mob in band)
        foreach (var (e, chance) in Marginals(mob.Drops ?? Array.Empty<DropEntry>(), mob.Level))
            if (ItemCatalog.Get(e.ItemId) is { Slot: EquipSlot.Material } def)
                for (int t = 0; t < Crafting.MaterialTypes.Length; t++)
                    if (e.ItemId == Crafting.MaterialId(Crafting.MaterialTypes[t], def.Rarity))
                        res[t, (int)def.Rarity] +=
                            chance * ((e.MinQty + e.MaxQty) / 2.0) * RateConfig.World.DropAmount / band.Length;
    return res;
}

Console.WriteLine("=== M10: the per-TYPE penalty — a recipe naming ONE material type, not 'any' ===");
Console.WriteLine("  (per kill, averaged over every template IN the band, so the category spread is real)");
foreach (var (name, floor, top) in gradeBands.Where(b => b.Name is "E" or "C" or "A" or "S"))
{
    var byType = MatsByType(floor, top);
    Console.WriteLine($"  --- {name} band ({floor}-{top}) --- {RarityHeader()}");
    for (int t = 0; t < Crafting.MaterialTypes.Length; t++)
    {
        var row = Crafting.MaterialRarities.Select((_, r) => byType[t, r]).ToArray();
        if (row.All(v => v <= 0)) continue;
        Console.WriteLine($"  {Crafting.MaterialTypes[t],-10}      {string.Concat(row.Select(v => $"{v,10:0.####}"))}");
    }
    Console.WriteLine($"  {"ALL TYPES",-10}      " + string.Concat(
        Crafting.MaterialRarities.Select((_, r) =>
            $"{Enumerable.Range(0, Crafting.MaterialTypes.Length).Sum(t => byType[t, r]),10:0.####}")));
    Console.WriteLine($"  {"x PENALTY",-10}      " + string.Concat(
        Crafting.MaterialRarities.Select((_, r) =>
        {
            double all = Enumerable.Range(0, Crafting.MaterialTypes.Length).Sum(t => byType[t, r]);
            double best = Enumerable.Range(0, Crafting.MaterialTypes.Length).Max(t => byType[t, r]);
            return best <= 0 ? $"{"-",10}" : $"{all / best,9:0.0}x";
        })));
}
Console.WriteLine("  'x PENALTY' = how much LONGER a farm takes when the recipe names one specific type, versus the");
Console.WriteLine("  'all five together' totals every row of M1-M8 is built on. It is measured against the BEST type in");
Console.WriteLine("  the band — the friendliest possible reading, i.e. a floor on the penalty, not a worst case.");
Console.WriteLine("  🔑 If a recipe says '300 Ingot' rather than '300 material', multiply that rung's hours by this.");
Console.WriteLine();

// The consumable half of his message, and the one part of it that rests on a factual claim: *"if for 1h of
// farming i can get 1 drop of enchant, it should cost me the same to make another one"* and *"1h of farming
// should buy u 1h of buffs"*. Both are checkable, and the first one decides whether crafted consumables are
// priced off a real number or an assumed one.
static (double Scrolls, double Enchants, double Potions, double BuffSeconds) ConsumablesPerKill(int level)
{
    double scrolls = 0, ench = 0, pots = 0, buffSec = 0;
    var near = MobsNear(level);
    foreach (var mob in near)
        foreach (var (e, chance) in Marginals(mob.Drops ?? Array.Empty<DropEntry>(), level))
        {
            if (ItemCatalog.Get(e.ItemId) is not ItemDef def) continue;
            double n = chance * ((e.MinQty + e.MaxQty) / 2.0) * RateConfig.World.DropAmount / near.Length;
            if (def.Slot == EquipSlot.Scroll) { scrolls += n; if (def.ScrollGrade != EnchantGrade.None) ench += n; }
            else if (def.Slot == EquipSlot.Consumable)
            {
                pots += n;
                // A potion does not implement an effect, it NAMES a skill (see ItemDef.UseSkillId), so the
                // buff length is the skill's DurationTicks at 10 ticks/sec — never a number authored here.
                if (SkillCatalog.Get(def.UseSkillId) is { DurationTicks: > 0 } sk) buffSec += n * sk.DurationTicks / 10.0;
            }
        }
    return (scrolls, ench, pots, buffSec);
}

Console.WriteLine("=== M11: the CONSUMABLE faucet, and whether elites/bosses can be one ===");
Console.WriteLine($"{"gr",3} {"levels",8} {"kills/h",8} | {"scrolls/h",10} {"enchants/h",11} {"1 enchant every",16} "
    + $"{"potions/h",10} {"buff-min/h",11}");
foreach (var (name, floor, top) in gradeBands)
{
    var c = ConsumablesPerKill(top);
    double kph = KillsPerHour(top);
    double enchPerHour = c.Enchants * kph;
    Console.WriteLine($"{name,3} {floor + "-" + top,8} {kph,8:F0} | {c.Scrolls * kph,10:0.###} {enchPerHour,11:0.###} "
        + $"{(enchPerHour > 0 ? Hrs(1 / enchPerHour) : "never"),16} {c.Potions * kph,10:0.###} {c.BuffSeconds * kph / 60,11:0.#}");
}
Console.WriteLine("  🔑 'buff-min/h' against his rule *\"1h of farming should buy u 1h of buffs\"*: 60 would be parity, and");
Console.WriteLine("     the game already runs 3-4x OVER it at every band. Potion uptime is not scarce and pricing a");
Console.WriteLine("     crafted potion against 'an hour buys an hour' would make it far CHEAPER than it already is.");
Console.WriteLine("  🔑 '1 enchant every' against his premise *\"if for 1h of farming i can get 1 drop of enchant\"*: it");
Console.WriteLine("     is nowhere near 1 h, it is 3.6-18.6 h, and it gets WORSE as you climb — the opposite shape to");
Console.WriteLine("     the one the premise assumes. A crafted-scroll price built on 'one an hour' would be ~10x cheap.");
Console.WriteLine("  🔴 The S band reads ZERO enchants/h and that is BY DESIGN, not a bug: the normal-mob enchant faucet");
Console.WriteLine("     closes at 80 (D1), leaving elites and bosses as the only source. So at the exact level where the");
Console.WriteLine("     crafting ladder needs its top rung, the drop it would be priced against does not exist at all —");
Console.WriteLine("     which makes CRAFTING the intended A/S scroll supply rather than a convenience.");
Console.WriteLine();

// Can elites/bosses carry the top mats? That is a SPAWN question, not a drop-table one: a camp that holds
// two mobs on a 180 s timer has a hard ceiling no drop rate can raise. Read the real zones.
Console.WriteLine("  --- elite / boss AVAILABILITY (the ceiling a faucet there would run into) ---");
Console.WriteLine($"  {"rank",6} {"camps",7} {"held",6} {"respawn",10} {"kills/h per camp",18} {"vs a normal farm",18}");
double normalKph = KillsPerHour(gradeBands[^1].Top);
foreach (var rank in new[] { MobRank.Elite, MobRank.Boss })
{
    var zones = WorldMap.SpawnZones.Where(z => z.Rank == rank).ToArray();
    if (zones.Length == 0) continue;
    double held = zones.Average(z => z.TotalCount), resp = zones.Average(z => z.RespawnSeconds);
    double perCamp = held * 3600.0 / Math.Max(1, resp);
    Console.WriteLine($"  {rank,6} {zones.Length,7} {held,6:0.#} {resp,9:N0}s {perCamp,18:0.##} "
        + $"{perCamp / normalKph,17:P1}");
}
foreach (var rank in new[] { MobRank.Elite, MobRank.Boss })
    Console.WriteLine($"  {rank,6} enchant scrolls: {MobCatalog.EnchantScrollDrops(gradeBands[^1].Top, rank).Sum(e => MobCatalog.EffectiveChance(e)),6:0.###} per kill "
        + $"at level {gradeBands[^1].Top}");
Console.WriteLine("  This is D1's precedent for the top MATS: when the normal-mob scroll faucet closed at B, the top of");
Console.WriteLine("  that ladder moved to elites and bosses rather than being deleted. The question M8 leaves open is");
Console.WriteLine("  whether Legendary/Mythic MATS can do the same, and the answer is in the rate column above.");
Console.WriteLine("  🔑 ELITES CAN CARRY A FAUCET. An elite camp is RESPAWN-limited rather than walk-limited, and that");
Console.WriteLine("     turns out to be an ADVANTAGE, not a ceiling — a camp that holds several on a ~2 min timer beats");
Console.WriteLine("     the walk-limited normal farm outright. A bulk mat could genuinely live there.");
Console.WriteLine("  🔴 BOSSES CANNOT. A ~10 h respawn on a single spawn is ~0.1 kills/h — a thousandth of a normal");
Console.WriteLine("     farm. A boss can gate a ONE-OFF (a single Mythic accent per item), never a quantity.");
Console.WriteLine("  ⚠ Both rate columns are CEILINGS: they assume the camp is killed dry the instant it repopulates,");
Console.WriteLine("     with no travel and no TTK. An elite's own TTK is longer than a normal mob's, so treat the elite");
Console.WriteLine("     row as an upper bound on what a faucet there could deliver, not as a farm rate.");
Console.WriteLine();

// =====================================================================================================
//  M12: THE AUTHORED RECIPES, PRICED. This is the one that closes `BL-05` — everything above measures
//  what a faucet YIELDS; this measures what the recipes actually shipped in Recipes.cs COST, in the same
//  farm hours his target curve is written in, so the two can be compared without arithmetic in between.
//
//  🔑 It reads RecipeCatalog, not a table of its own. Change GearBulk / GearAccent / SlotFraction /
//  EliteMatDrops and re-run — if this section still lands on his curve, the change is fine, and if it
//  does not, the numbers are wrong and not the tool.
//
//  ⚠ It also finally supplies the measurement §7f listed as MISSING: an elite's own TTK, which turns
//  M11's respawn CEILING into a real farm rate.
// =====================================================================================================
Console.WriteLine("#####################################################################################");
Console.WriteLine("###  M12: the AUTHORED gear recipes, priced against his target curve               ###");
Console.WriteLine("#####################################################################################");
Console.WriteLine();

// An ELITE is 4x HP and 1.5x ATK (GameLoopService.SpawnOne), so its TTK is ~4x a normal mob's. At a camp
// you are not walking between scattered spawns — you are standing in it waiting — so the 40 s loop
// overhead of a normal farm collapses to a retarget. The rate is then whichever binds first: your own
// killing speed, or the camp's respawn ceiling from M11.
const double EliteHpMul = 4.0, EliteCampOverhead = 10.0;
var eliteZones = WorldMap.SpawnZones.Where(z => z.Rank == MobRank.Elite).ToArray();
double eliteCeiling = eliteZones.Length == 0 ? 0
    : eliteZones.Average(z => z.TotalCount) * 3600.0 / Math.Max(1, eliteZones.Average(z => z.RespawnSeconds));
double EliteKillsPerHour(int level) =>
    Math.Min(eliteCeiling, 3600.0 / (EliteHpMul * BandTtk(level) + EliteCampOverhead));

Console.WriteLine("=== M12a: the elite farm rate — M11's ceiling, with an elite's own TTK applied ===");
Console.WriteLine($"{"gr",3} {"levels",8} {"normal TTK",11} {"elite TTK",10} {"elite kills/h",14} "
    + $"{"ceiling",9} {"vs normal farm",15}");
foreach (var (name, floor, top) in gradeBands.Where(b => b.Name is "B" or "A" or "S"))
    Console.WriteLine($"{name,3} {floor + "-" + top,8} {BandTtk(top),10:F1}s {EliteHpMul * BandTtk(top),9:F1}s "
        + $"{EliteKillsPerHour(top),14:F0} {eliteCeiling,9:F0} {EliteKillsPerHour(top) / KillsPerHour(top),14:P0}");
Console.WriteLine("  This is the number §7f said was missing. An elite camp trades four times the HP for no walking,");
Console.WriteLine("  which is what makes it a candidate home for the top mats — read 'vs normal farm' to see whether");
Console.WriteLine("  that trade is still winning. It was 115% at S before the 0.73.0 mob-curve refit and is not now:");
Console.WriteLine("  doubling creature defence doubles the elite's TTK too, and the walking it saves did not change.");
Console.WriteLine();

// Mats per hour BY TYPE at a band, optionally including the ELITE layer (which is applied at kill time by
// rank, not baked into the template, so it has to be layered here exactly as the kill roll layers it).
// ⚠ Averaged over EVERY template in the band, not just the nearest few — same population M10 uses, and
// for the same reason: mat TYPES are flavored by mob CATEGORY, so a handful of neighbouring templates can
// show a type as literally undroppable when the band as a whole pays it. A player farms the band.
static double[,] MatsPerHourByType(int lo, int hi, bool elite, double kph, bool salvage = true)
{
    var res = new double[Crafting.MaterialTypes.Length, Crafting.MaterialRarities.Length];
    var band = MobCatalog.Templates.Where(m => !m.Dummy && m.Level >= lo && m.Level <= hi).ToArray();
    if (band.Length == 0) return res;
    foreach (var mob in band)
    {
        var rows = (mob.Drops ?? Array.Empty<DropEntry>()).ToList();
        if (elite)
        {
            rows.AddRange(MobCatalog.EliteMatDrops(mob.Level, MobRank.Elite, mob.Category));
            // The GEAR table is rank-swapped at kill time exactly like the mats above: an elite drops
            // the elite gear column, not the template's normal one. It has to be layered the same way
            // here, or salvage at the top would be priced off a table the player never sees.
            rows.RemoveAll(e => MobCatalog.IsGearGroup(e.GroupId));
            rows.AddRange(MobCatalog.GearDrops(mob.Level, MobRank.Elite));
        }
        foreach (var (e, chance) in Marginals(rows, mob.Level))
        {
            var def = ItemCatalog.Get(e.ItemId);
            if (def is { Slot: EquipSlot.Material })
            {
                for (int t = 0; t < Crafting.MaterialTypes.Length; t++)
                    if (e.ItemId == Crafting.MaterialId(Crafting.MaterialTypes[t], def.Rarity))
                        res[t, (int)def.Rarity] +=
                            chance * ((e.MinQty + e.MaxQty) / 2.0) * RateConfig.World.DropAmount * kph / band.Length;
                continue;
            }

            // ---- `BL-22`: DISASSEMBLY. Every piece of GEAR this table drops is also a pile of mats,
            // because the player can break it down instead of selling it. That makes the gear column a
            // second mat faucet, and it is the whole reason the feature moves the farm-hours number at
            // all. Modelled here rather than argued about, because his budget for it is a MEASUREMENT
            // (*"10~20% decrease in time should be ok"*, from 347h) and hand-derived balance has been
            // wrong in this file before.
            //
            // ⚠ Assumes the player salvages EVERYTHING, which is the generous end: it is the bound the
            // budget has to survive, and anyone farming for mats will in fact salvage everything.
            if (salvage && Crafting.Disassemble(def) is Crafting.Salvage s)
            {
                int t = Array.IndexOf(Crafting.MaterialTypes, s.Type);
                if (t >= 0)
                    res[t, (int)s.Rarity] +=
                        chance * ((e.MinQty + e.MaxQty) / 2.0) * s.Qty * RateConfig.World.DropAmount
                        * kph / band.Length;
            }
        }
    }
    return res;
}

// Hours to gather a whole recipe. A farmer collects every type AT ONCE, so the clock is the SLOWEST
// single ingredient, not the sum — that is the difference between "trade for the rest" and "farm it all",
// and it is the honest number for a solo crafter. Refining is offered as an alternative source for
// anything the band does not drop, and the cheaper of the two wins per ingredient.
// Two numbers, and the difference between them is the whole cross-profession-trade design.
//
//  • MAIN  = the hours for the recipe's LARGEST single ingredient — your own profession's material, the
//            60% of a weapon that is Ingot. This is what the costs are AUTHORED against, because the
//            composition split exists precisely so the other 40% comes from trade (*"finished items need
//            several types → cross-profession trade"*, Crafting.cs). Pricing a smith's sword as though he
//            must personally farm its Wood prices the design out of existence.
//  • SOLO  = the slowest ingredient of all, i.e. a crafter who trades with nobody. Reported, never
//            authored against. At E and D it is 3-4x MAIN and it binds on **Wood**, a 20% component,
//            because mat flavor follows mob CATEGORY and few E-band creatures are Animals or Plants.
//            That gap is the trade incentive, measured.
static double RecipeHours(Recipe r, double[,] perHour) => RecipeHoursDetail(r, perHour).Main;

static (double Main, double Solo, string Binds) RecipeHoursDetail(Recipe r, double[,] perHour)
{
    double worst = 0, biggestBulk = 0, accents = 0;
    int biggestQty = -1;
    string binds = "";
    var bulkRarity = ItemCatalog.Get(r.Inputs[0].ItemId)?.Rarity ?? ItemRarity.Common;
    foreach (var inp in r.Inputs)
    {
        if (ItemCatalog.Get(inp.ItemId) is not { Slot: EquipSlot.Material } def) continue;
        int type = Array.IndexOf(Crafting.MaterialTypes,
            Crafting.MaterialTypes.First(t => Crafting.MaterialId(t, def.Rarity) == inp.ItemId));
        double rate = perHour[type, (int)def.Rarity];
        double hours = rate > 0 ? inp.Qty / rate : double.PositiveInfinity;
        // Refine alternative: 5 of the rung below + 2 cross, all farmed at THIS band's rates.
        //
        // ⚠ FIXED 2026-08-14 while measuring `BL-22`. This used to be tried ONLY when the direct rate
        // was exactly zero, which contradicts the header above it ("the cheaper of the two wins per
        // ingredient") and is not merely pedantic — it made the model NON-MONOTONIC. Adding a trickle
        // of a material the band previously never dropped replaced a cheap refine path with an
        // expensive direct one, so a strictly larger faucet came out as a LONGER farm. Disassembly is
        // exactly such a trickle, and the first M13 run printed D at +286% and C at +52% because of
        // it — supply going up cannot make a recipe dearer, so the tool was wrong, not the feature.
        // Both paths are now costed and the MINIMUM wins, which is what a player would actually do.
        if (def.Rarity > ItemRarity.Common)
        {
            var refine = RecipeCatalog.Get($"refine_{Crafting.MaterialTypes[type]}_{def.Rarity}".ToLowerInvariant());
            if (refine is not null)
            {
                double one = 0;
                foreach (var ri in refine.Inputs)
                    if (ItemCatalog.Get(ri.ItemId) is { Slot: EquipSlot.Material } rd)
                    {
                        int rt = Array.IndexOf(Crafting.MaterialTypes,
                            Crafting.MaterialTypes.First(t => Crafting.MaterialId(t, rd.Rarity) == ri.ItemId));
                        double rr = perHour[rt, (int)rd.Rarity];
                        one += rr > 0 ? ri.Qty / rr : double.PositiveInfinity;
                    }
                hours = Math.Min(hours, one * inp.Qty);
            }
        }
        if (hours > worst) { worst = hours; binds = $"{inp.Qty} {def.Rarity} {Crafting.MaterialTypes[type]}"; }
        // MAIN = the biggest BULK line (your own material) + every ACCENT line. The accent is only a
        // handful, but it is the rung above and there is no trading around it — nobody has a spare
        // Mythic. The smaller CROSS-material bulk lines are what MAIN assumes you trade for.
        if (def.Rarity > bulkRarity) accents = Math.Max(accents, hours);
        else if (inp.Qty > biggestQty) { biggestQty = inp.Qty; biggestBulk = hours; }
    }
    // MAX, not sum, on both models: every ingredient accumulates at the SAME time while you farm, so the
    // clock is the slowest one, not the total. (Summing them was the first version and it made MAIN
    // exceed SOLO, which is impossible — MAIN is a subset of the same maximum.)
    return (Math.Max(biggestBulk, accents), worst, binds);
}

Console.WriteLine("=== M12b: every authored WEAPON recipe, per rung — attempt, finished, vs his target ===");
Console.WriteLine($"{"rung",5} {"bulk",22} {"accent",18} | {"per attempt",12} {"attempts",9} "
    + $"{"per finished",13} {"his target",13}  verdict");
foreach (var (grade, lo, hi) in hisCurve)
{
    int rung = Array.FindIndex(hisCurve, c => c.Grade == grade) + 1;
    int itemLevel = Crafting.GearItemLevels[rung - 1];
    bool elite = rung >= 3;                      // C and up: the accent mats only exist at an elite camp
    var band = gradeBands.First(b => b.Name == grade);
    double kph = elite ? EliteKillsPerHour(band.Top) : KillsPerHour(band.Top);
    var perHour = MatsPerHourByType(band.Floor, band.Top, elite, kph);

    // The representative WEAPON of the rung: his curve is authored per weapon and every other slot is a
    // fraction of it, so pricing one weapon prices the whole rung.
    var recipe = RecipeCatalog.All
        .Where(r => r.CraftLevel == rung
                    && ItemCatalog.Get(r.OutputId) is { Slot: EquipSlot.Weapon } d && d.ItemLevel == itemLevel)
        .OrderBy(r => r.Id).FirstOrDefault();
    if (recipe is null) { Console.WriteLine($"{grade,5}  (no weapon recipe at this rung)"); continue; }

    var odds = Crafting.GearCraftOdds(rung);
    double attempts = 1.0 / Math.Max(0.01f, 1f - odds.Fail);
    var (perAttempt, soloAttempt, binds) = RecipeHoursDetail(recipe, perHour);
    double finished = perAttempt * attempts;
    string bulkTxt = string.Join(" + ", recipe.Inputs.Take(recipe.Inputs.Length - 1)
        .Select(i => $"{i.Qty}"));
    var last = recipe.Inputs[^1];
    string verdict = finished < lo * 0.75 ? "🔴 TOO CHEAP"
        : finished > hi * 1.25 ? "🔴 TOO DEAR"
        : finished < lo || finished > hi ? "near (inside 25%)" : "✅ inside his range";
    Console.WriteLine($"{grade,5} {bulkTxt + " " + ItemCatalog.Get(recipe.Inputs[0].ItemId)?.Rarity,22} "
        + $"{last.Qty + " " + ItemCatalog.Get(last.ItemId)?.Rarity,18} | {Hrs(perAttempt),12} {attempts,8:0.0}x "
        + $"{Hrs(finished),13} {$"{lo:0.#}-{hi:0.#}h",13}  {verdict,-18} solo {Hrs(soloAttempt * attempts),8} (binds on {binds})");
}
Console.WriteLine("  'per attempt' is the SLOWEST ingredient, not the sum — you farm every material type at once, so");
Console.WriteLine("  the clock is whichever one the band pays least of. That makes it the SOLO number; a crafter who");
Console.WriteLine("  trades for his cross-materials beats it, which is the trade the composition split exists to force.");
Console.WriteLine("  B, A and S are priced at an ELITE camp, because Epic/Legendary/Mythic mats exist nowhere else.");
Console.WriteLine();

Console.WriteLine("=== M12c: the full character, by slot — his fractions applied to the AUTHORED recipes ===");
Console.WriteLine($"{"rung",5} {"weapon",10} {"body",9} {"helmet",9} {"shield",9} {"gloves",9} "
    + $"{"boots",9} {"neck",9} {"ear",9} {"ring",9} | {"FULL",10} {"+shield",9}");
foreach (var (grade, lo, hi) in hisCurve)
{
    int rung = Array.FindIndex(hisCurve, c => c.Grade == grade) + 1;
    int itemLevel = Crafting.GearItemLevels[rung - 1];
    bool elite = rung >= 3;
    var band = gradeBands.First(b => b.Name == grade);
    double kph = elite ? EliteKillsPerHour(band.Top) : KillsPerHour(band.Top);
    var perHour = MatsPerHourByType(band.Floor, band.Top, elite, kph);
    double attempts = 1.0 / Math.Max(0.01f, 1f - Crafting.GearCraftOdds(rung).Fail);

    // One representative recipe per SLOT SHAPE at this grade, priced from what actually shipped.
    double Slot(Func<ItemDef, bool> pick)
    {
        var r = RecipeCatalog.All
            .Where(x => x.CraftLevel == rung
                        && ItemCatalog.Get(x.OutputId) is { } d && d.ItemLevel == itemLevel && pick(d))
            .OrderBy(x => x.Id).FirstOrDefault();
        return r is null ? 0 : RecipeHours(r, perHour) * attempts;
    }
    double weapon = Slot(d => d.Slot == EquipSlot.Weapon);
    double body   = Slot(d => d.Slot == EquipSlot.Armor && d.ArmorSlot == ArmorSlot.Body);
    double head   = Slot(d => d.Slot == EquipSlot.Armor && d.ArmorSlot == ArmorSlot.Head);
    double shield = Slot(d => d.Slot == EquipSlot.Shield);
    double gloves = Slot(d => d.Slot == EquipSlot.Armor && d.ArmorSlot == ArmorSlot.Gloves);
    double boots  = Slot(d => d.Slot == EquipSlot.Armor && d.ArmorSlot == ArmorSlot.Boots);
    double neck   = Slot(d => d.Slot == EquipSlot.Jewel && d.JewelType == JewelType.Necklace);
    double ear    = Slot(d => d.Slot == EquipSlot.Jewel && d.JewelType == JewelType.Earring);
    double ring   = Slot(d => d.Slot == EquipSlot.Jewel && d.JewelType == JewelType.Ring);
    double full = weapon + body + head + gloves + boots + neck + 2 * ear + 2 * ring;
    Console.WriteLine($"{grade,5} {Hrs(weapon),10} {Hrs(body),9} {Hrs(head),9} {Hrs(shield),9} {Hrs(gloves),9} "
        + $"{Hrs(boots),9} {Hrs(neck),9} {Hrs(ear),9} {Hrs(ring),9} | {Hrs(full),10} {Hrs(full + shield),9}");
}
Console.WriteLine("  🔑 The SHIELD column is his 2026-08-13 ruling — *\"It's armor so make it as a helmet price\"* — so it");
Console.WriteLine("     should read the same as the helmet column beside it. It is OUTSIDE both of his sums, which is why");
Console.WriteLine("     'FULL' and '+shield' are printed apart: a shield user's kit is 1.30 weapons of armor, not 1.00.");
Console.WriteLine("  ⚠ 'FULL' is the number to sanity-check, not the per-item ones — it is what a finished character of");
Console.WriteLine("     that grade costs in farm hours, and at S it is the real endgame figure.");
Console.WriteLine();

// =====================================================================================================
//  M13: DISASSEMBLY (`BL-22`) — the same full-character sum, measured WITH and WITHOUT salvage.
//
//  This section exists because his approval of `BL-22` came with a number attached and no other:
//    *"now as 347h for fully geared if we add the disassembly this should not go to 20h ..
//      10~20% decrease in time should be ok"*
//  and *"u give up gold to get mats"*. So the feature is not done when it works — it is done when the
//  S row of M12c has moved by 10-20% and no further. The one knob is Crafting.SalvageQtyByRung.
// =====================================================================================================
Console.WriteLine("########################################################################################");
Console.WriteLine("###  M13: BL-22 disassembly — the farm budget, with salvage and without               ###");
Console.WriteLine("########################################################################################");
Console.WriteLine();
Console.WriteLine($"  his budget: a fully S-geared character must fall 10-20% from 347h, i.e. to ~278-312h");
Console.WriteLine($"  the knob:   Crafting.SalvageQtyByRung = [{string.Join(", ", Crafting.SalvageQtyByRung)}]  (F,E,D,C,B,A,S)");
Console.WriteLine();
Console.WriteLine($"{"rung",5} {"FULL before",13} {"FULL after",12} {"change",9}  verdict");
foreach (var (grade, _, _) in hisCurve)
{
    int rung = Array.FindIndex(hisCurve, c => c.Grade == grade) + 1;
    int itemLevel = Crafting.GearItemLevels[rung - 1];
    bool elite = rung >= 3;
    var band = gradeBands.First(b => b.Name == grade);
    double kph = elite ? EliteKillsPerHour(band.Top) : KillsPerHour(band.Top);
    double attempts = 1.0 / Math.Max(0.01f, 1f - Crafting.GearCraftOdds(rung).Fail);

    double FullAt(bool withSalvage)
    {
        var perHour = MatsPerHourByType(band.Floor, band.Top, elite, kph, withSalvage);
        double Slot(Func<ItemDef, bool> pick)
        {
            var r = RecipeCatalog.All
                .Where(x => x.CraftLevel == rung
                            && ItemCatalog.Get(x.OutputId) is { } d && d.ItemLevel == itemLevel && pick(d))
                .OrderBy(x => x.Id).FirstOrDefault();
            return r is null ? 0 : RecipeHours(r, perHour) * attempts;
        }
        return Slot(d => d.Slot == EquipSlot.Weapon)
             + Slot(d => d.Slot == EquipSlot.Armor && d.ArmorSlot == ArmorSlot.Body)
             + Slot(d => d.Slot == EquipSlot.Armor && d.ArmorSlot == ArmorSlot.Head)
             + Slot(d => d.Slot == EquipSlot.Armor && d.ArmorSlot == ArmorSlot.Gloves)
             + Slot(d => d.Slot == EquipSlot.Armor && d.ArmorSlot == ArmorSlot.Boots)
             + Slot(d => d.Slot == EquipSlot.Jewel && d.JewelType == JewelType.Necklace)
             + 2 * Slot(d => d.Slot == EquipSlot.Jewel && d.JewelType == JewelType.Earring)
             + 2 * Slot(d => d.Slot == EquipSlot.Jewel && d.JewelType == JewelType.Ring);
    }

    // The diagnostic that matters when a row refuses to move: what is the DEAREST rarity salvage can
    // pay here, and what does the recipe actually bind on? A 0% change is never a bug in the tuning —
    // it means salvage's rarity ceiling sits below the bottleneck, which no quantity can fix.
    var noSalv = MatsPerHourByType(band.Floor, band.Top, elite, kph, false);
    var withSalv = MatsPerHourByType(band.Floor, band.Top, elite, kph, true);
    ItemRarity? bestSalvage = null;
    for (int t = 0; t < Crafting.MaterialTypes.Length; t++)
        for (int q = 0; q < Crafting.MaterialRarities.Length; q++)
            if (withSalv[t, q] - noSalv[t, q] > 0.0001)
                bestSalvage = Crafting.MaterialRarities[q];

    var wpn = RecipeCatalog.All
        .Where(x => x.CraftLevel == rung
                    && ItemCatalog.Get(x.OutputId) is { Slot: EquipSlot.Weapon } d && d.ItemLevel == itemLevel)
        .OrderBy(x => x.Id).FirstOrDefault();
    string binds = wpn is null ? "?" : RecipeHoursDetail(wpn, withSalv).Binds;

    double before = FullAt(false), after = FullAt(true);
    double cut = before <= 0 ? 0 : (before - after) / before;
    // Only the S row carries his budget; the rest are printed so a rung cannot quietly collapse
    // while S looks healthy.
    string verdict = grade != "S" ? ""
        : cut < 0.05 ? "🔴 barely moves — salvage is not worth doing"
        : cut < 0.10 ? "near (under his 10%)"
        : cut <= 0.20 ? "✅ inside his 10-20%"
        : cut <= 0.30 ? "🔴 too generous"
        : "🔴 COLLAPSE — this is the '20h' he warned about";
    Console.WriteLine($"{grade,5} {Hrs(before),13} {Hrs(after),12} {-cut,9:P0}  {verdict}");
    Console.WriteLine($"        binds on {binds,-24} salvage here tops out at "
        + $"{bestSalvage?.ToString() ?? "nothing"}");
}
Console.WriteLine("  ⚠ This assumes the player salvages EVERY piece of gear that drops, which is the GENEROUS bound —");
Console.WriteLine("     the budget has to hold at the extreme, and a mat farmer really does break down everything.");
Console.WriteLine("  🔑 'You give up gold to get mats': the same items are the gear-sale income measured in the ECONOMY");
Console.WriteLine("     section far above, so every hour saved here is gold not earned. The two are alternatives.");
Console.WriteLine();
Console.WriteLine("  🔴 FINDING — HIS 10-20% CANNOT REACH S, AND NO AMOUNT OF TUNING CHANGES THAT.");
Console.WriteLine("     Read the two columns above together. His mapping is *\"rarity for mats rarity\"*, so salvage can");
Console.WriteLine("     only ever pay the rarity of the gear that DROPS. Gear rarity is capped by rank, not by band:");
Console.WriteLine("     a normal mob stops at Epic (0.0001) and an ELITE stops at Epic too (MobCatalog.EliteGearRates:");
Console.WriteLine("     Uncommon .10 / Rare .02 / Epic .002). Only a BOSS drops Legendary or Mythic gear — and a boss is");
Console.WriteLine("     0.09 kills/h (M11), which is a keepsake, not a faucet.");
Console.WriteLine("     Meanwhile the A and S recipes bind on LEGENDARY, which salvage therefore never produces.");
Console.WriteLine("     Measured, not argued: at SalvageQtyByRung = 20 across the board, E/D/C collapse to -24/-39/-72%");
Console.WriteLine("     while A and S still move 0.00%. The quantity knob is not the binding constraint; the rarity");
Console.WriteLine("     mapping is. So the honest options are all HIS to pick:");
Console.WriteLine("       1. accept it — disassembly is a mid-game feature (D/C get his 10-20%), S keeps its 347h;");
Console.WriteLine("       2. let elites drop LEGENDARY gear, which opens a gear faucet that competes with crafting;");
Console.WriteLine("       3. let a high GRADE bump the salvaged rarity up a rung, which contradicts \"rarity for rarity\".");
Console.WriteLine("     Nothing is invented here: option 1 is what ships.");
Console.WriteLine();

static string NameOf(string id) => SkillCatalog.Get(id)?.Name ?? id;

/// <summary>A Human ASSASSIN (rogue) of this level in the best duals + LIGHT armor for its tier —
/// the class the crit-damage rungs actually belong to. BuildPlayer only knows tank/warrior/nuker
/// and dresses them in a sword and heavy, which would measure the wrong masteries entirely.</summary>
// The archetype IDENTITY floor passive (Evasion Mastery / Precision / Anti-Magic) is
// AUTO-granted in game by AutoLearnCoreSkills — it is not in the class tables, so a synthetic
// character built from those tables alone was missing it entirely. That is why §50h measured the
// rogue's blow gate at a 9.2% crit: his own Evasion Mastery (then worth +20 crit POINTS) was absent
// from the MODEL, not from the game. Never measure a character without it.
// ⚠ No DISCIPLINE is passed, so a rogue here is measured as a MELEE rogue (the full 20/40/76 evade
// ladder). A ranged discipline is capped at rung 1 in game since 2026-08-07 (playtest-19 M7) — if a
// bow rogue at 40+ is ever measured, pass its discipline or the model will over-state its dodge.
static void GrantFloorPassive(Entity e, int level)
{
    if (SkillCatalog.FloorPassiveFor(e.Archetype, level) is { } fp)
        e.LearnedSkills[fp.Id] = Math.Max(e.SkillLevelOf(fp.Id), fp.Level);
    e.RecomputeDerived();
}

static Entity BuildRogue(int level)
{
    var s = StatCalculator.GetBaseStats(Race.Human, BaseClass.Fighter);
    var e = new Entity { Name = "rogue", Kind = EntityKind.Player, Race = Race.Human, BaseClass = BaseClass.Fighter, Level = level };
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Agi = s.Agi; e.Spt = s.Spt;
    if (level >= 20) e.SecondClass = 15;   // Human Assassin

    foreach (var cs in ClassSkills.ForClass(Race.Human, BaseClass.Fighter, null, null))
        if (cs.LearnLevel <= level) e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
    foreach (var cs in ClassSkills.Cumulative(Race.Human, BaseClass.Fighter, e.Archetype, e.Discipline))
        if (cs.LearnLevel <= level) e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
    foreach (var id in e.LearnedSkills.Keys.ToList())
        if (SkillCatalog.Get(id)?.Replaces is { } replaced)
            foreach (var r in replaced) e.LearnedSkills.Remove(r);
    GrantFloorPassive(e, level);

    var rune = SkillCatalog.Get(SkillCatalog.WarRuneBuff);
    if (rune != null)
        e.Buffs.Add(new Game.Server.Simulation.BuffInstance
        {
            Effect = rune.Effect, Magnitudes = rune.Magnitudes,
            TicksRemaining = int.MaxValue, Name = rune.Name, Key = rune.BuffKey,
        });

    int t = GearTier(level);
    Equip(e, $"duals_t{t}");
    Equip(e, $"light_t{t}");
    foreach (var acc in new[] { "helm", "gloves", "boots" }) Equip(e, $"{acc}_t{t}");
    Equip(e, $"necklace_t{t}");
    Equip(e, $"ring_t{t}"); Equip(e, $"ring_t{t}");
    Equip(e, $"earring_t{t}"); Equip(e, $"earring_t{t}");

    e.RecomputeDerived();
    return e;
}

// ----- G3 helpers -----------------------------------------------------------------------------

/// <summary>One creature, spawned from its MobCatalog template the way GameLoopService.BuildMob does
/// it — natural level, the template's own weapon resolution, its MobMod passives via MobTypeId, and,
/// for a player-built one, <see cref="Entity.ApplyMobBuild"/>, which is literally the method the server
/// calls. Normal rank throughout (his B4: *"a elite and bosses will scale with passives out of them"*),
/// so this measures the base creature and nothing else.
///
/// <para>⚠ The HELD RUNE is applied here as a permanent buff, exactly as BuildMob does. The player-side
/// reconciliation loop that normally keeps a rune buff up is player-only and clock-driven; a creature
/// has neither a clock nor a login, so for a mob the rune is not a consumable — it is part of what the
/// creature IS.</para></summary>
static Entity SpawnTemplate(string mobId)
{
    var type = MobCatalog.Get(mobId);
    int level = type.Level;
    var s = StatCalculator.MobStats(level);
    var e = new Entity
    {
        Name = type.Name, Kind = EntityKind.Mob, Level = level, MobTypeId = mobId,
        InnateWeaponType =
            type.Role == MobRole.Archer ? WeaponType.Bow
            : type.Mod is MobMod wm && wm.Weapon != WeaponType.None ? wm.Weapon
            : MobCatalog.DefaultWeaponFor(type.Category),
    };
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Agi = s.Agi; e.Spt = s.Spt;

    if (type.Build is MobBuild build) e.ApplyMobBuild(build);

    if (type.Role == MobRole.Mage)
    {
        e.CasterMob = true;
        int spellLevel = SkillCatalog.MobSpellLevel(level);
        e.LearnedSkills[SkillCatalog.MobNukeSkill] = spellLevel;
        e.LearnedSkills[SkillCatalog.MobBoltSkill] = spellLevel;
    }

    e.RecomputeDerived();

    if (type.Build is MobBuild rb && rb.Held.Length > 0
        && ItemCatalog.Get(rb.Held) is { RuneBuffSkillId: { Length: > 0 } runeBuffId }
        && SkillCatalog.Get(runeBuffId) is SkillDef runeSkill)
    {
        e.Buffs.Add(new Game.Server.Simulation.BuffInstance
        {
            Effect = runeSkill.Effect, Magnitudes = runeSkill.Magnitudes,
            TicksRemaining = int.MaxValue, Name = runeSkill.Name, Key = runeSkill.BuffKey,
        });
    }
    return e;
}

/// <summary>The authored gear-grade ladder, in item-level terms (F, E, D, C, B, A, S).</summary>
static int[] G3GearLadder() => new[] { ItemCatalog.FGradeLevel, 20, 40, 52, 61, 76, ItemCatalog.SGradeLevel };

/// <summary>The tier a mob-player of this level wears: the tier its LEVEL earns, dropped
/// <paramref name="tierDrop"/> grades — "items of lower grade", his words.</summary>
static int G3Tier(int level, int tierDrop)
{
    var ladder = G3GearLadder();
    int idx = 0;
    for (int i = 0; i < ladder.Length; i++) if (level >= ladder[i]) idx = i;
    return ladder[Math.Max(0, idx - tierDrop)];
}

static string G3GearLabel(int level, int tierDrop, ItemRarity q, int enchant) =>
    $"t{G3Tier(level, tierDrop)} {q}{(enchant > 0 ? " +" + enchant : "")}";

/// <summary>Item-id quality suffix. The AUTHORED piece is the Mythic one (bare id); every lesser
/// quality is a generated copy suffixed with its rarity name — see ItemCatalog's DropTiers.</summary>
static string G3Quality(ItemRarity q) =>
    q == ItemRarity.Mythic ? "" : "_" + q.ToString().ToLowerInvariant();

/// <summary>True when the catalogue can actually dress a mob-player in this tier+quality. Not every
/// rung exists: the S grade is Epic-and-up only (<see cref="ItemCatalog.IsTopHalfOnly"/>), so asking
/// for "t80 Common" silently equips nothing at all — and a naked entity flatters any ratio search.
/// Checks one piece of each shape (body / weapon / accessory / jewel) rather than trusting one id.</summary>
static bool G3LoadoutExists(int tier, ItemRarity q)
{
    string s = G3Quality(q);
    foreach (var id in new[] { $"heavy_t{tier}{s}", $"robe_t{tier}{s}", $"sword1h_t{tier}{s}",
                               $"staff_t{tier}{s}", $"helm_t{tier}{s}", $"ring_t{tier}{s}" })
        if (ItemCatalog.Get(id) is null) return false;
    return true;
}

/// <summary>A MOB built entirely through the PLAYER pipeline — Kind=Player, real base stats, a real
/// 2nd class (so it has an Archetype and therefore an HP/MP class modifier), and a real equipped
/// loadout run through the same RecomputeDerived the server runs. NO rune buff: a mob holds no runes,
/// and handing it the player's +100% P.Atk would measure a state that can never exist.</summary>
static Entity BuildMobPlayer(int level, Archetype arch, int tierDrop, ItemRarity quality, int enchant, bool kit) =>
    BuildMobPlayerFixedTier(level, arch, G3Tier(level, tierDrop), quality, enchant, kit);

/// <summary>As <see cref="BuildMobPlayer"/>, but with the gear tier pinned instead of derived from the
/// level — this is what lets G3.3 spawn ONE authored loadout across every zone band.</summary>
static Entity BuildMobPlayerFixedTier(int level, Archetype arch, int tier, ItemRarity quality,
                                      int enchant, bool kit = false, string? weaponOverride = null)
{
    var (race, cls, secondId) = arch switch
    {
        Archetype.Tank    => (Race.Human, BaseClass.Fighter, 13),
        Archetype.Warrior => (Race.Human, BaseClass.Fighter, 14),
        Archetype.Rogue   => (Race.Human, BaseClass.Fighter, 15),
        Archetype.Healer  => (Race.Human, BaseClass.Mage,    17),
        _                 => (Race.Human, BaseClass.Mage,    18),   // Nuker
    };

    var s = StatCalculator.GetBaseStats(race, cls);
    var e = new Entity { Name = "mob-player", Kind = EntityKind.Player, Race = race, BaseClass = cls, Level = level };
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Agi = s.Agi; e.Spt = s.Spt;
    if (level >= 20) e.SecondClass = secondId;

    // The class table is the WRONG source for a mob kit (it drags in masteries) — G3.5(e) measures
    // exactly that. It is offered here only so the cost of getting it wrong is visible.
    if (kit)
    {
        foreach (var cs in ClassSkills.ForClass(race, cls, null, null))
            if (cs.LearnLevel <= level)
                e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
        foreach (var cs in ClassSkills.Cumulative(race, cls, e.Archetype, e.Discipline))
            if (cs.LearnLevel <= level)
                e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
        foreach (var id in e.LearnedSkills.Keys.ToList())
            if (SkillCatalog.Get(id)?.Replaces is { } replaced)
                foreach (var r in replaced) e.LearnedSkills.Remove(r);
    }

    bool caster = arch is Archetype.Nuker or Archetype.Healer;
    string q = G3Quality(quality);
    string body = caster ? "robe" : arch == Archetype.Rogue ? "light" : "heavy";
    string weapon = weaponOverride
                    ?? (caster ? "staff" : arch == Archetype.Warrior ? "sword2h" : "sword1h");

    EquipEnchanted(e, $"{weapon}_t{tier}{q}", enchant);
    EquipEnchanted(e, $"{body}_t{tier}{q}", enchant);
    if (arch == Archetype.Tank) EquipEnchanted(e, $"shield_t{tier}{q}", enchant);
    foreach (var acc in new[] { "helm", "gloves", "boots" }) EquipEnchanted(e, $"{acc}_t{tier}{q}", enchant);
    EquipEnchanted(e, $"necklace_t{tier}{q}", enchant);
    EquipEnchanted(e, $"ring_t{tier}{q}", enchant);    EquipEnchanted(e, $"ring_t{tier}{q}", enchant);
    EquipEnchanted(e, $"earring_t{tier}{q}", enchant); EquipEnchanted(e, $"earring_t{tier}{q}", enchant);

    e.RecomputeDerived();
    return e;
}

/// <summary>HIS loadout shape, playtest 24: the WEAPON and the ARMOR are dressed independently — a
/// high-grade, heavily-enchanted weapon over low-grade armor (*"S grade Mace enchanted to +60 ... and
/// B grade leather"*). G3.2 could not build this and that is its blind spot: it moved grade, quality
/// and enchant on **every slot at once**, so the one shape that can fix the mirror — attack short,
/// defence long — was outside the sweep by construction. Accessories and jewels follow the ARMOR.</summary>
static Entity BuildMobPlayerSplit(int level, Archetype arch,
                                  int armorTier, ItemRarity armorQ, int armorEnch,
                                  int weaponTier, ItemRarity weaponQ, int weaponEnch)
{
    var (race, cls, secondId) = arch switch
    {
        Archetype.Tank    => (Race.Human, BaseClass.Fighter, 13),
        Archetype.Warrior => (Race.Human, BaseClass.Fighter, 14),
        Archetype.Rogue   => (Race.Human, BaseClass.Fighter, 15),
        Archetype.Healer  => (Race.Human, BaseClass.Mage,    17),
        _                 => (Race.Human, BaseClass.Mage,    18),   // Nuker
    };

    var s = StatCalculator.GetBaseStats(race, cls);
    var e = new Entity { Name = "mob-player", Kind = EntityKind.Player, Race = race, BaseClass = cls, Level = level };
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Agi = s.Agi; e.Spt = s.Spt;
    if (level >= 20) e.SecondClass = secondId;

    bool caster = arch is Archetype.Nuker or Archetype.Healer;
    string aq = G3Quality(armorQ), wq = G3Quality(weaponQ);
    string body   = caster ? "robe" : arch == Archetype.Rogue ? "light" : "heavy";
    string weapon = caster ? "staff" : arch == Archetype.Warrior ? "sword2h" : "sword1h";

    EquipEnchanted(e, $"{weapon}_t{weaponTier}{wq}", weaponEnch);
    EquipEnchanted(e, $"{body}_t{armorTier}{aq}", armorEnch);
    if (arch == Archetype.Tank) EquipEnchanted(e, $"shield_t{armorTier}{aq}", armorEnch);
    foreach (var acc in new[] { "helm", "gloves", "boots" }) EquipEnchanted(e, $"{acc}_t{armorTier}{aq}", armorEnch);
    EquipEnchanted(e, $"necklace_t{armorTier}{aq}", armorEnch);
    EquipEnchanted(e, $"ring_t{armorTier}{aq}", armorEnch);    EquipEnchanted(e, $"ring_t{armorTier}{aq}", armorEnch);
    EquipEnchanted(e, $"earring_t{armorTier}{aq}", armorEnch); EquipEnchanted(e, $"earring_t{armorTier}{aq}", armorEnch);

    e.RecomputeDerived();
    return e;
}

/// <summary>True when the catalogue holds this archetype's WEAPON at this tier+quality. The armor
/// side is covered by <see cref="G3LoadoutExists"/>; a weapon needs its own check because G3.7 moves
/// the two independently and a missing weapon measures a bare-handed entity.</summary>
static bool G3WeaponExists(Archetype arch, int tier, ItemRarity q)
{
    bool caster = arch is Archetype.Nuker or Archetype.Healer;
    string weapon = caster ? "staff" : arch == Archetype.Warrior ? "sword2h" : "sword1h";
    return ItemCatalog.Get($"{weapon}_t{tier}{G3Quality(q)}") is not null;
}

static void EquipEnchanted(Entity e, string defId, int enchant)
{
    if (ItemCatalog.Get(defId) is null) { Console.Error.WriteLine($"  !! missing item {defId}"); return; }
    e.Inventory.Add(new InventoryItem { DefId = defId, Equipped = true, Enchant = enchant });
}

static string Ratio(double value, double reference) =>
    reference <= 0 ? "  -  " : "x" + (value / reference).ToString("0.00");

/// <summary>Sustained PHYSICAL dps of `atk` against `def`, through the real resolvers: the best
/// physical skill on its own cycle, autoattacks filling whatever the cast leaves free, crit folded in
/// as an expected multiplier, and the miss chance the accuracy/evasion resolver actually returns.
/// The swing interval is taken from the attacker's KIND, exactly as ResolveAttack does — which is the
/// whole point of G3.5(a). Block is NOT modelled (it needs a roll), so a shielded defender lives
/// longer than this says.</summary>
static float PhysDps(Entity atk, Entity def)
{
    float hit = 1f - Miss(atk, def);
    int pDef = Math.Max(1, (int)def.EffectiveDefence);
    float critMult = StatCalculator.PhysicalCritMult(atk.CritDamageBonus);
    // The FLAT crit-damage add is a bigger factor on a BASIC attack (small P.Atk, no skill power)
    // than on a skill, so the two hits carry their own crit factors.
    float critF = CritFactor(atk.CritChance,
        critMult * StatCalculator.CritFlatFactor(atk.EffectiveBasicAttack, atk.CritDamageFlat));

    int autoHit = StatCalculator.PhysicalDamage((int)atk.EffectiveBasicAttack, 0, pDef, atk.Level);
    int baseInterval = atk.Kind == EntityKind.Player
        ? GameConstants.PlayerAttackIntervalTicks : GameConstants.MobAttackIntervalTicks;
    float autoEvery = Math.Max(2, (int)(baseInterval * atk.EffectiveAttackSpeedMultiplier))
                      * GameConstants.TickSeconds;
    float autoDps = autoHit * critF * hit / autoEvery;

    var (skill, lvl) = TopSkill(atk, SkillEffect.PhysicalDamage);
    if (skill is null) return autoDps;

    float cycle = SkillCycleSeconds(atk, skill);
    float castSecs = Math.Max(2, (int)(skill.CastTicks * atk.EffectiveAttackSpeedMultiplier))
                     * GameConstants.TickSeconds;
    int skillHit = StatCalculator.PhysicalDamage((int)atk.EffectiveAttack, skill.PowerAt(lvl), pDef, atk.Level);
    float skillF = SkillHitFactor(atk, skill, skill.PowerAt(lvl), critMult);
    float autoShare = Math.Max(0f, (cycle - castSecs) / cycle);
    return skillHit * skillF * hit / cycle + autoDps * autoShare;
}

/// <summary>Sustained MAGIC dps: the best nuke on its cycle. Spells are not evaded (they can only
/// "fail", which is a separate roll not modelled here), so no hit term.</summary>
static float MagicDps(Entity atk, Entity def)
{
    var (skill, lvl) = TopSkill(atk, SkillEffect.MagicDamage);
    if (skill is null) return 0f;
    float critF = CritFactor(atk.MagicCritChance, atk.EffectiveMagicCritDamage);
    int hitDmg = StatCalculator.MagicDamage((int)atk.EffectiveMagicAttack, skill.PowerAt(lvl),
        Math.Max(1, (int)def.EffectiveMagicDefence), atk.Level);
    return hitDmg * critF / SkillCycleSeconds(atk, skill);
}

/// <summary>Whichever channel this entity actually fights with — the better of its two. A mob with no
/// skills at all falls through to its autoattack, which is what today's mobs do.</summary>
static float Dps(Entity atk, Entity def) => Math.Max(PhysDps(atk, def), MagicDps(atk, def));

// ---------------------------------------------------------------------------

// The gear tier a character of this level would realistically be wearing.
static int GearTier(int level) =>
    level >= ItemCatalog.SGradeLevel ? ItemCatalog.SGradeLevel
    : level >= 76 ? 76 : level >= 61 ? 61 : level >= 52 ? 52 : level >= 40 ? 40 : 20;

// Highest-power MagicDamage skill the character knows, at the level they know it.
static int TopNukePower(Entity e)
{
    int best = 0;
    string bestName = "-";
    foreach (var (id, lvl) in e.LearnedSkills)
    {
        var def = SkillCatalog.Get(id);
        if (def is null) { Console.Error.WriteLine($"   ?? unresolved skill id '{id}'"); continue; }
        if ((def.Effect & SkillEffect.MagicDamage) == 0) continue;
        if (!string.IsNullOrEmpty(def.ConsumableId)) continue;   // skip reagent ultimates (Elemental Burst)
        if (def.PowerAt(lvl) > best) { best = def.PowerAt(lvl); bestName = $"{def.Name} L{lvl}"; }
    }
    Console.Error.WriteLine($"   [lvl {e.Level}] top nuke = {bestName} ({best})");
    return best;
}

// =====================================================================================================
//  DPS — the only honest way to compare a fighter with a caster.
//
//  "Hits to kill" says nothing: it ignores how long a hit TAKES. A 400-damage skill on a 15s reuse and
//  a 300-damage one on 3s are not comparable by damage, and a fighter filling the gaps with autoattacks
//  is doing work that a hits-count never sees. So model the rotation the server actually runs:
//
//      skill damage / (cast + reuse)  +  autoattack damage / attack interval
//
//  …with crit folded in as an EXPECTED multiplier (chance × (mult − 1)), because over a fight that is
//  what crit is worth. Every timing below comes from the server: PlayerAttackIntervalTicks scaled by
//  EffectiveAttackSpeedMultiplier (CombatTick), CastTicks × the speed multiplier for that skill's
//  CATEGORY (physical skills scale with ATTACK speed, spells with CAST speed — see SkillReuseTicks),
//  and CooldownTicks reduced by CooldownReduction.
// =====================================================================================================

/// <summary>Lay the NPC buffer's FULL set on an entity — the same `SkillCatalog.NewbieBuffSet` the
/// buffer NPC and the debug button use, so "buffed" here means exactly what it means in game.
///
/// This matters because the owner signs off on BUFFED numbers: an unbuffed matrix was measuring a
/// state almost nobody plays in.</summary>
static void ApplyNpcBuffs(Entity e)
{
    // ⚠ The buffer's blessings are CHILD WRAPPERS now (docs/design/BuffLadders.md): the wrapper owns
    // the duration and names a single-buff skill, and carries NO magnitudes of its own. Reading
    // MagnitudesAt off the wrapper therefore yields an EMPTY array — the buffs "apply" and do
    // nothing, and the whole matrix silently reports UNBUFFED numbers under a "buffed" heading.
    // That is exactly the kind of wrong number this tool exists to prevent, so follow the children.
    // (This was already true of the four speed singles from 0.36.0 on.)
    void Add(SkillDef def)
    {
        var kids = def.ChildBuffsAt(1);
        if (kids is { Length: > 0 })
        {
            foreach (var kid in kids)
                if (SkillCatalog.Get(kid) is SkillDef child) Add(child);
            return;
        }
        e.Buffs.Add(new Game.Server.Simulation.BuffInstance
        {
            Effect = def.Effect,
            Magnitudes = def.MagnitudesAt(1) ?? Array.Empty<EffectMagnitude>(),
            TicksRemaining = int.MaxValue, Name = def.Name, Key = def.BuffKey, Level = 1,
        });
    }

    foreach (var id in SkillCatalog.NewbieBuffSet)
        if (SkillCatalog.Get(id) is SkillDef def)
            Add(def);
    e.RecomputeDerived();
}

/// <summary>Apply ONE buff skill and recompute — same child-wrapper unwrapping as ApplyNpcBuffs
/// (a wrapper carries no magnitudes of its own, so reading them off it measures nothing).</summary>
static void ApplyOneBuff(Entity e, string skillId)
{
    void Add(SkillDef def)
    {
        var kids = def.ChildBuffsAt(1);
        if (kids is { Length: > 0 })
        {
            foreach (var kid in kids)
                if (SkillCatalog.Get(kid) is SkillDef child) Add(child);
            return;
        }
        e.Buffs.Add(new Game.Server.Simulation.BuffInstance
        {
            Effect = def.Effect,
            Magnitudes = def.MagnitudesAt(1) ?? Array.Empty<EffectMagnitude>(),
            TicksRemaining = int.MaxValue, Name = def.Name, Key = def.BuffKey, Level = 1,
        });
    }

    if (SkillCatalog.Get(skillId) is SkillDef def) Add(def);
    e.RecomputeDerived();
}

/// <summary>Expected damage multiplier from crit: 1 + chance × (mult − 1).</summary>
static float CritFactor(float chance, float mult) => 1f + chance * (mult - 1f);

/// <summary>Expected damage multiplier on ONE hit of a physical SKILL, running the same three
/// resolutions GameLoopService does (docs/design/CritBlowAndDouble.md):
/// a BLOW crits or falls to its BlowFailFraction floor, and a landed blow takes the crit-damage
/// values and may then [Double]; a [Double] skill is a flat x2 on the ATK curve and nothing else;
/// anything else is the ordinary crit, with the flat crit damage inside it.</summary>
static float SkillHitFactor(Entity atk, SkillDef skill, int power, float critMult)
{
    float flatF = StatCalculator.CritFlatFactor(atk.EffectiveAttack, atk.CritDamageFlat, power);
    float dbl = StatCalculator.PhysicalDoubleChance(atk.AtkStat);

    // A skill's crit roll is the character's rate times the SKILL's own modifier (CritRateMod).
    float skillCrit = Math.Clamp(atk.CritChance * skill.CritRateMod, 0f, 1f);

    if (skill.BlowOnCrit)
    {
        float landed = flatF * critMult * (skill.CanDouble ? 1f + dbl : 1f);
        return skillCrit * landed + (1f - skillCrit) * skill.BlowFailFraction;
    }
    if (skill.CanDouble) return CritFactor(dbl, 2f);
    // Can Crit and Can Double are exclusive OPT-IN flags now (M8): a skill with neither lands flat.
    if (!skill.CanCrit) return 1f;
    return CritFactor(skillCrit, critMult * flatF);
}

/// <summary>Seconds between autoattacks, exactly as CombatTick computes the cooldown.</summary>
static float AutoAttackSeconds(Entity e) =>
    Math.Max(2, (int)(GameConstants.PlayerAttackIntervalTicks * e.EffectiveAttackSpeedMultiplier))
    * GameConstants.TickSeconds;

/// <summary>Seconds one cast of a skill occupies: cast time (scaled by the speed that skill's category
/// uses) plus its reuse. This is SkillReuseTicks' arithmetic.</summary>
static float SkillCycleSeconds(Entity e, SkillDef def)
{
    float speedMult = def.Category == SkillCategory.Physical
        ? e.EffectiveAttackSpeedMultiplier : e.EffectiveCastSpeedMultiplier;
    int castTicks = Math.Max(2, (int)(def.CastTicks * speedMult));
    int cd = def.CooldownTicks;
    if (cd > 0 && e.CooldownReduction > 0f) cd = Math.Max(1, (int)(cd * (1f - e.CooldownReduction)));
    return Math.Max(1, castTicks + cd) * GameConstants.TickSeconds;
}

/// <summary>The strongest damaging skill of a channel, returned with its def so the caller can time it.</summary>
static (SkillDef? Def, int Level) TopSkill(Entity e, SkillEffect channel)
{
    SkillDef? best = null; int bestLvl = 0;
    foreach (var (id, lvl) in e.LearnedSkills)
    {
        var def = SkillCatalog.Get(id);
        if (def is null || (def.Effect & channel) == 0) continue;
        if (!string.IsNullOrEmpty(def.ConsumableId)) continue;
        // The WEAPON gate the server enforces on every cast (GameLoopService: "you need a …").
        // Without it a sword-and-shield tank was measured on Stab — a DUAL-only blow it can never
        // cast — which the blow resolution (full damage only on a crit) makes badly wrong.
        // ⚠ Through WeaponTypes.Satisfies — the SAME helper the server casts through. The raw
        // `(required & equipped) != 0` that stood here until 2026-08-29 predated both the playtest-28
        // fold and the hands gate, so it disagreed with the game in both directions: it refused a maul
        // a skill authored `Blunt` (which the server allows) and it allowed a 2H weapon a one-handed
        // passive (which the server now refuses). A measurement that applies its own rule measures
        // nothing.
        if (!e.WeaponType.Satisfies(def.RequiredWeapon, def.RequiredHands)) continue;
        if (best is null || def.PowerAt(lvl) > best.PowerAt(bestLvl)) { best = def; bestLvl = lvl; }
    }
    return (best, bestLvl);
}

/// <summary>The strongest PHYSICAL-damage skill the character knows, mirroring TopNukePower.
///
/// Without this the fighter row was measured on a power-0 BASIC ATTACK while the mage row used its
/// best nuke — so the two columns were never comparable, and the "fighter needs 24.6 hits vs the
/// mage's 3.8 casts" reading was an artefact of comparing an autoattack to a spell. A fighter's
/// damage comes from its skills exactly as a mage's does.</summary>
static int TopPhysSkillPower(Entity e)
{
    int best = 0;
    string bestName = "-";
    foreach (var (id, lvl) in e.LearnedSkills)
    {
        var def = SkillCatalog.Get(id);
        if (def is null) continue;
        if ((def.Effect & SkillEffect.PhysicalDamage) == 0) continue;
        if (!string.IsNullOrEmpty(def.ConsumableId)) continue;   // reagent ultimates aren't the baseline
        if (def.PowerAt(lvl) > best) { best = def.PowerAt(lvl); bestName = $"{def.Name} L{lvl}"; }
    }
    Console.Error.WriteLine($"   [lvl {e.Level}] top phys skill = {bestName} ({best})");
    return best;
}

// A character at `level` with every skill their class table offers by then, wearing the
// full best-for-tier gear line (weapon + body + accessories + 5 jewels + shield for fighters).
/// <param name="quality">Gear QUALITY suffix: null/"epic" = the authored tier piece, or "mythic" /
/// "legendary" / "rare" / … to measure the six-quality ladder's other rungs.</param>
/// <param name="warrior">Fighters default to the KNIGHT (tank) 2nd class; set this for the CHAMPION
/// (warrior), whose kit is the damage-dealing one.</param>
/// <param name="healer">Mages default to the SORCERER (nuker) 2nd class; set this for the CLERIC,
/// who takes the healer kit and — since the 2026-08-20 mastery fork — a WAND, not a staff.</param>
/// <param name="discipline">BL-13 — take the 3rd class too, where one is authored. Left null by every
/// existing caller ON PURPOSE: only four discipline kits exist, so making it the default would measure
/// four classes at 40+ and the other six at their 2nd-class ceiling, which is worse than measuring all
/// ten at the ceiling. The boss party passes Lightbringer, because a level-40+ healer really is one and
/// his heal ladder above 35 is the whole difference between "the healer can hold" and "he cannot".</param>
static Entity BuildPlayer(Race race, BaseClass cls, int level, string? quality = null, bool warrior = false,
                          bool healer = false, Discipline? discipline = null)
{
    var s = StatCalculator.GetBaseStats(race, cls);
    var e = new Entity { Name = "calc", Kind = EntityKind.Player };
    e.Race = race;
    e.BaseClass = cls;
    e.Level = level;
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Agi = s.Agi; e.Spt = s.Spt;   // 🔴 SPT was MISSING here until 2026-08-26 — every player row ran at SPT 0

    // Second class at 20 (Human Sorcerer / Human Knight) so the archetype kits apply.
    // 18 = Sorcerer (nuker), 13 = Knight (tank), 14 = Champion (warrior).
    // 17 = Human Cleric (healer).
    if (level >= 20) e.SecondClass = cls == BaseClass.Mage ? (healer ? 17 : 18) : warrior ? 14 : 13;
    // The 3rd class, when the caller asked for one and the character is old enough to hold it. It must
    // be set BEFORE the Cumulative loop below: the lookup keys on (race, class, archetype, discipline),
    // so a discipline assigned afterwards teaches nothing.
    if (discipline is { } d && level >= ThirdClassCatalog.ChangeLevel
        && ThirdClassCatalog.Playable.FirstOrDefault(c => c.Race == race && c.Discipline == d) is { } tc)
        e.ThirdClass = tc.Id;

    // Every skill the class table teaches by this level, at the highest level learnable.
    //
    // The BASE-CLASS kit is added separately, because Cumulative does NOT return it once an archetype
    // is set: the base Fighter/Mage skills are registered under archetype=null and the lookup keys on
    // (race, class, archetype, discipline), so asking as a Tank finds only the Tank list. A REAL
    // character keeps what it learned before level 20 — LearnedSkills is persisted — so a synthetic
    // one built from Cumulative alone was a fighter with NO attack skill at all, which is why the
    // fighter row used to be measured on autoattacks.
    foreach (var cs in ClassSkills.ForClass(race, cls, null, null))
    {
        if (cs.LearnLevel > level) continue;
        e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
    }
    foreach (var cs in ClassSkills.Cumulative(race, cls, e.Archetype, e.Discipline))
    {
        if (cs.LearnLevel > level) continue;
        e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
    }
    // Drop anything a later skill supersedes (Smash replaces Strike, etc.) so the "best skill" is the
    // one actually usable, not a retired ladder rung.
    foreach (var id in e.LearnedSkills.Keys.ToList())
        if (SkillCatalog.Get(id)?.Replaces is { } replaced)
            foreach (var r in replaced) e.LearnedSkills.Remove(r);
    GrantFloorPassive(e, level);

    // Shots (2026-07-24): the old training passive is gone — soul/spell runes are now held RUNE items that
    // grant this buff. Apply it directly here so the matrix reflects the EXPECTED play state (runes ON).
    // Its numbers are identical to the old max passive (+100% P.Atk / +41% eff. M.Atk / +40 cast), so the
    // tuned curve is unchanged for a runed player; a rune-LESS player is ~half offence (intended, IG).
    var shot = SkillCatalog.Get(cls == BaseClass.Mage ? SkillCatalog.SpellRuneBuff : SkillCatalog.WarRuneBuff);
    if (shot != null)
        e.Buffs.Add(new Game.Server.Simulation.BuffInstance
        {
            Effect = shot.Effect, Magnitudes = shot.Magnitudes,
            TicksRemaining = int.MaxValue, Name = shot.Name, Key = shot.BuffKey,
        });
    // Mirror AutoLearnCoreSkills: SPELLCASTER MASTERY is the auto-granted one (2026-08-07 restructure)
    // and nothing replaces it — it carries the wrong-weight / wrong-weapon rule for every mage. Robe
    // Armor Mastery is bought off the class table at 7/14 and is already in LearnedSkills above; it
    // must NOT be re-added here, which is exactly the bug that made the level-1 base mastery beat the
    // nuker's own and zeroed his mpWhenRestored.
    if (cls == BaseClass.Mage)
        e.LearnedSkills[SkillCatalog.SpellcasterMastery] = 1;

    // QUALITY suffix. The tiered tables are authored as the EPIC piece, and the six-quality ladder
    // (0.29.1) derives everything else from it — so the bare id IS the Epic, and "_mythic" is the new
    // 100% ceiling at 1/0.7 ≈ +43%. Passing a quality here is what lets the matrix MEASURE that raise
    // instead of asserting it.
    int t = GearTier(level);
    string q = quality is null or "epic" ? "" : "_" + quality;
    Equip(e, (cls == BaseClass.Mage ? (healer ? $"wand_t{t}" : $"staff_t{t}") : $"sword1h_t{t}") + q);
    Equip(e, (cls == BaseClass.Mage ? $"robe_t{t}" : $"heavy_t{t}") + q);
    if (cls == BaseClass.Fighter) Equip(e, $"shield_t{t}{q}");
    foreach (var acc in new[] { "helm", "gloves", "boots" }) Equip(e, $"{acc}_t{t}{q}");
    Equip(e, $"necklace_t{t}{q}");
    Equip(e, $"ring_t{t}{q}"); Equip(e, $"ring_t{t}{q}");
    Equip(e, $"earring_t{t}{q}"); Equip(e, $"earring_t{t}{q}");

    e.RecomputeDerived();
    return e;
}

// ---------------------------------------------------------------------------------------------
//  BL-13 — the party a boss is measured against, and the boss it is measured on.
//
//  🔑 SpawnRanked reads MobRankScale, which is the SAME code GameLoopService.BuildMob spawns with.
//  This tool used to carry a hand-typed copy of the four rank multipliers in two separate places, so
//  the measurement and the game could disagree without either being touched. What the tool must NOT
//  share is the derived arithmetic below it (exp ratios, time-to-kill) — that is what lets it catch
//  the server drifting.
// ---------------------------------------------------------------------------------------------
static Entity SpawnRanked(int level, MobRank rank)
{
    var m = BuildMobEntity(level);
    m.Rank = rank;
    m.MobHpScale = MobRankScale.Hp(rank, level);
    m.MobPAtkScale = m.MobMAtkScale = MobRankScale.Atk(rank);
    m.MobPDefScale = m.MobMDefScale = MobRankScale.Def(rank);
    m.MobAccFlat = MobRankScale.AccFlat(rank);
    // A boss fights with its kit, not with its fists — BuildMob teaches every boss the telegraphed
    // slam when its template has no BossProfile of its own. Leaving it out measured a boss swinging
    // bare-handed and understated what a tank has to survive.
    if (rank == MobRank.Boss) m.LearnedSkills[SkillCatalog.BossSlamSkill] = 1;
    m.RecomputeDerived();   // playtest-20 #7: a rank must survive a recompute
    return m;
}

/// <summary>His party, verbatim: *"A healer, tank and dds in a party are a must"*. Tank, healer and
/// three damage dealers (two champions and a nuker — the mix a real group brings).</summary>
static Entity[] BuildBossParty(int level) => new[]
{
    BuildPlayer(Race.Human, BaseClass.Fighter, level),                  // TANK   (Knight: shield + heavy)
    BuildPlayer(Race.Human, BaseClass.Mage,    level, healer: true,
                discipline: Discipline.Lightbringer),                   // HEALER (Cleric → Lightbringer)
    BuildPlayer(Race.Human, BaseClass.Fighter, level, warrior: true),   // DD
    BuildPlayer(Race.Human, BaseClass.Fighter, level, warrior: true),   // DD
    BuildPlayer(Race.Human, BaseClass.Mage,    level),                  // DD     (Sorcerer)
};

/// <summary>What the party puts INTO the boss. The healer contributes nothing: he is casting heals,
/// which is the entire reason he is standing there — counting his nukes would measure a party that is
/// not being healed, which is the party the old 3-DD ceiling already measured.</summary>
static float PartyDps(Entity[] party, Entity target)
{
    float sum = 0f;
    foreach (var p in party)
        if (p.SecondClass != 17) sum += Dps(p, target);   // 17 = Human Cleric
    return sum;
}

/// <summary>What a SHIELD is worth against this attacker, as a damage multiplier. A crit ignores the
/// shield entirely (GameLoopService.ResolvePhysical), so only the non-crit share can be blocked, and a
/// block removes BlockReduction of the blow. Physical only — magic is never blocked, by design.</summary>
static float BlockFactor(Entity atk, Entity def)
{
    if (!def.HasShield) return 1f;
    float block = Math.Clamp(def.BlockChance, 0f, StatCaps.BlockChance);
    return 1f - (1f - atk.CritChance) * block * def.BlockReduction;
}

/// <summary>ONE basic attack, non-crit. The channel nobody can dodge, walk out of or interrupt — so
/// this, not the telegraphed skill, is what *"not one shooting"* has to be measured on.</summary>
static int BasicHit(Entity atk, Entity def) =>
    StatCalculator.PhysicalDamage((int)atk.EffectiveBasicAttack, 0,
        Math.Max(1, (int)def.EffectiveDefence), atk.Level);

/// <summary>The biggest SINGLE blow this attacker can land — basic or skill. Non-crit on purpose:
/// a crit is variance on top, and a boss that one-shots only on a crit is a different complaint.</summary>
static int BiggestHit(Entity atk, Entity def)
{
    int pDef = Math.Max(1, (int)def.EffectiveDefence);
    int mDef = Math.Max(1, (int)def.EffectiveMagicDefence);
    int best = StatCalculator.PhysicalDamage((int)atk.EffectiveBasicAttack, 0, pDef, atk.Level);
    var (ps, pl) = TopSkill(atk, SkillEffect.PhysicalDamage);
    if (ps is not null)
        best = Math.Max(best, StatCalculator.PhysicalDamage((int)atk.EffectiveAttack, ps.PowerAt(pl), pDef, atk.Level));
    var (ms, ml) = TopSkill(atk, SkillEffect.MagicDamage);
    if (ms is not null)
        best = Math.Max(best, StatCalculator.MagicDamage((int)atk.EffectiveMagicAttack, ms.PowerAt(ml), mDef, atk.Level));
    return best;
}

/// <summary>The healer's sustained output: his best flat heal on its own cycle. A CEILING — it ignores
/// the MP bar, ignores travel and ignores the fact that he also has to move.</summary>
static float HealerHps(Entity healer)
{
    float best = 0f;
    foreach (var (id, lvl) in healer.LearnedSkills)
    {
        var d = SkillCatalog.Get(id);
        if (d is null || (d.Effect & SkillEffect.Heal) == 0 || d.PlacesTotem) continue;
        float amount = SkillMath.HealAmount(d.PowerAt(lvl), healer.HealPowerFlat, healer.HealPowerMod);
        best = Math.Max(best, amount / Math.Max(0.1f, SkillCycleSeconds(healer, d)));
    }
    return best;
}

static void Equip(Entity e, string defId)
{
    if (ItemCatalog.Get(defId) is null) { Console.Error.WriteLine($"  !! missing item {defId}"); return; }
    e.Inventory.Add(new InventoryItem { DefId = defId, Equipped = true });
}

// A REAL low-level player: TRAINING gear (the level 1-10 kit), NO rune buff, learned skills up to
// this level. This is what a new character actually fights with — unlike BuildPlayer, which floors to
// level-20 gear + shots and so hides the low-level one-shot the playtest found.
// ----- hit/miss helpers ---------------------------------------------------------------------

/// <summary>A mob exactly as GameLoopService.BuildMob makes one, stat-wise: MobStats(level)
/// through the same RecomputeDerived. No MobMod — that is the "assume every monster is x1" rule.</summary>
static Entity BuildMobEntity(int level, MobCategory category = MobCategory.Animal)
{
    var s = StatCalculator.MobStats(level);
    var e = new Entity
    {
        Name = "mob", Kind = EntityKind.Mob, Level = level,
        // A mob HOLDS a weapon, and that weapon sets its basic-attack SPEED (owner, 2026-08-10).
        // BuildMob resolves it from the Archer role / the MobMod.Weapon passive / the category
        // default; this tool has no template, so it takes the category default the same way.
        // Leaving it None modelled a bare-handed creature at the WEAPONLESS 300 and understated
        // every mob's DPS by ~30% — the tool would have reported a mob nerf that does not exist.
        // Animal is the default because it is the commonest farm category (claws = Dual, 433).
        InnateWeaponType = MobCatalog.DefaultWeaponFor(category),
    };
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Agi = s.Agi; e.Spt = s.Spt;
    e.RecomputeDerived();
    return e;
}

/// <summary>Base stats and NOTHING else — no gear, no learned skills, so no passive floors.
/// This is the row that shows what the FORMULA does before the character sheet touches it.</summary>
static Entity BuildNaked(Race race, BaseClass cls, int level)
{
    var s = StatCalculator.GetBaseStats(race, cls);
    var e = new Entity { Name = "naked", Kind = EntityKind.Player, Race = race, BaseClass = cls, Level = level };
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Agi = s.Agi; e.Spt = s.Spt;
    e.RecomputeDerived();
    return e;
}

/// <summary>Physical miss chance for attacker → defender, through the one real resolver.</summary>
static float Miss(Entity attacker, Entity defender) =>
    StatCalculator.ResolveAvoidChance(attacker.Accuracy, (int)defender.EffectiveEvasion,
        defender.EvadeFloor, attacker.HitFloor, attacker.Level, defender.Level);

static string Pct(float f) => (f * 100f).ToString("0") + "%";

static Entity BuildStarter(BaseClass cls, int level)
{
    var s = StatCalculator.GetBaseStats(Race.Human, cls);
    var e = new Entity { Name = "starter", Kind = EntityKind.Player, Race = Race.Human, BaseClass = cls, Level = level };
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Agi = s.Agi; e.Spt = s.Spt;   // 🔴 SPT was MISSING here until 2026-08-26 — every player row ran at SPT 0

    foreach (var cs in ClassSkills.Cumulative(Race.Human, cls, e.Archetype, e.Discipline))
        if (cs.LearnLevel <= level)
            e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
    if (cls == BaseClass.Mage) e.LearnedSkills[SkillCatalog.MasteryRobe] = 1;

    // Training kit only — no runes, no jewels (jewels are earned; the point is the FLOOR gear).
    Equip(e, cls == BaseClass.Mage ? ItemCatalog.TrainingWand : ItemCatalog.TrainingSword);
    Equip(e, cls == BaseClass.Mage ? ItemCatalog.TrainingRobe : ItemCatalog.TrainingLeather);

    e.RecomputeDerived();
    return e;
}

/// <summary>A REAL Warchanter of a given race — the 3rd class set, its whole learnable kit, and the
/// weapon/armour ITS OWN masteries train. `BuildPlayer` cannot do this: it stops at the 2nd class and
/// dresses every mage in a wand or a staff, which is exactly the wrong weapon for all three of them.
///
/// <para><paramref name="atkOverride"/> replaces the race's base ATK and nothing else, so a what-if on
/// the power stat can be MEASURED through the real formulas instead of scaled by hand.</para></summary>
static Entity BuildWarchanter(Race race, int level, int? atkOverride = null,
                              Discipline disc = Discipline.Warchanter)
{
    var s = StatCalculator.GetBaseStats(race, BaseClass.Mage);
    var e = new Entity { Name = "warchanter", Kind = EntityKind.Player, Race = race, BaseClass = BaseClass.Mage };
    e.Level = level;
    e.Con = s.Con; e.AtkStat = atkOverride ?? s.Atk; e.Wit = s.Wit; e.Agi = s.Agi; e.Spt = s.Spt;

    // 2nd class = this race's Cleric (the buffer's parent), 3rd = its Warchanter — or, for the
    // side-effect check, its Nuker/Magus. ATK is ONE stat shared by every mage of the race, so a
    // raise aimed at the buffer lands on the nuker too and has to be measured there as well.
    var arch = Disciplines.Parent(disc);
    var second = ClassCatalog.Playable.First(c => c.Race == race && c.Archetype == arch);
    e.SecondClass = second.Id;
    e.ThirdClass = ThirdClassCatalog.Playable
        .First(c => c.Race == race && c.Discipline == disc).Id;

    foreach (var cs in ClassSkills.ForClass(race, BaseClass.Mage, null, null))
        if (cs.LearnLevel <= level)
            e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
    foreach (var cs in ClassSkills.Cumulative(race, BaseClass.Mage, e.Archetype, e.Discipline))
        if (cs.LearnLevel <= level)
            e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
    foreach (var id in e.LearnedSkills.Keys.ToList())
        if (SkillCatalog.Get(id)?.Replaces is { } replaced)
            foreach (var r in replaced) e.LearnedSkills.Remove(r);
    e.LearnedSkills[SkillCatalog.SpellcasterMastery] = 1;
    GrantFloorPassive(e, level);

    var rune = SkillCatalog.Get(SkillCatalog.SpellRuneBuff);
    if (rune != null)
        e.Buffs.Add(new Game.Server.Simulation.BuffInstance
        {
            Effect = rune.Effect, Magnitudes = rune.Magnitudes,
            TicksRemaining = int.MaxValue, Name = rune.Name, Key = rune.BuffKey,
        });

    // HIS race split, verbatim: *"human is tank - 1dmg skill and higher Def, elf is archer -
    // range/evasion 1dmg skill, ork is mele fighter"*. Human alone carries the shield.
    int t = GearTier(level);
    if (disc != Discipline.Warchanter) { Equip(e, $"staff_t{t}"); Equip(e, $"robe_t{t}"); }
    else switch (race)
    {
        case Race.Human: Equip(e, $"blunt1h_t{t}"); Equip(e, $"shield_t{t}"); Equip(e, $"heavy_t{t}"); break;
        case Race.Demon:   Equip(e, $"blunt2h_t{t}"); Equip(e, $"heavy_t{t}"); break;
        default:         Equip(e, $"bow_t{t}");     Equip(e, $"light_t{t}"); break;
    }
    foreach (var acc in new[] { "helm", "gloves", "boots" }) Equip(e, $"{acc}_t{t}");
    Equip(e, $"necklace_t{t}");
    Equip(e, $"ring_t{t}"); Equip(e, $"ring_t{t}");
    Equip(e, $"earring_t{t}"); Equip(e, $"earring_t{t}");

    e.RecomputeDerived();
    return e;
}

// ============================================================================================
//  `--mpregen` — THE MP ECONOMY. Owner, 2026-08-26, opening `BL-92`.
//
//  The question is whether a spamming mage can pay for himself off natural regen. It is answered
//  by MEASURING three things at the same level and putting them side by side:
//      DRAIN   — MP/s of spamming a real spell, on the auto-hunt's own cycle
//                (GameLoopService.AutoCycleTicks: castTicks×castSpeedMult + reduced cooldown).
//      NATURAL — regen with the character's own passives only (robe mastery + weapon mastery).
//      BUFFED  — plus the percent MP-regen buffs a real mage actually carries.
//
//  ⚠ NOTHING HERE CHANGES THE ENGINE. Two models are printed:
//      CURRENT  — what ships: every mpReg is a PERCENT and they all MULTIPLY.
//      PROPOSED — the owner's 2026-08-26 ruling: only the ARMOR mastery's 20% stays a percent;
//                 the weapon mastery's 1.5…3.4 ladder is a FLAT MP/s, added LAST (the global
//                 "flats after percentages" rule from playtest 28), and SPT gets its own linear
//                 regen modifier instead of riding the Max-MP curve.
// ============================================================================================

static void MpEconomy(int[] argLevels)
{
    // The percent MP-regen buffs a mage really carries. Serenity r6 and the Warchanter's Arcane
    // Serenity are the SAME family — the harmony evicts the single, so it is ONE x1.2, not two.
    const float BuffPct = 1.20f * 1.20f;    // Serenity-or-harmony x1.2, Mark x1.2

    int[] levels = argLevels.Length > 0 ? argLevels : new[] { 40, 44, 52, 60, 68, 74, 80, 85 };

    // Mirrors GameLoopService.Regenerate's MP branch EXACTLY, including where the flats sit.
    // If this drifts, the report lies — which is the one thing it exists not to do.
    static float Mp(Entity e, MoveState st, bool moving, float buffPct)
    {
        float stance = MovementTuning.RegenMultiplier(st, moving);
        float calm = st == MoveState.Sitting ? 1f
            : !moving                        ? e.MpRegenStandMult
            : st == MoveState.Walking        ? e.MpRegenWalkMult
                                             : e.MpRegenRunMult;
        return StatCalculator.MpRegenPerSecond(e.EffectiveSpt, e.Level)
                   * stance * calm * e.MpRegenMult * buffPct
               + e.MpRegenBonus;
    }

    Console.WriteLine();
    Console.WriteLine("=== THE MP ECONOMY (BL-92, BUILT 2026-08-26) - spam drain vs regen ===");
    Console.WriteLine();
    Console.WriteLine("  MP/s = [ (2 + 0.08*L) x SptRegenModifier x MpRegenMult x (1+buff%) x stance x calmSpirit ]");
    Console.WriteLine("         + flats     <- the weapon-mastery ladder (+1.1 .. +3.4) lives in the FLAT term now");
    Console.WriteLine("  SptRegenModifier = clamp(1 + (SPT-40)*0.02, 0.70, 1.30)");
    Console.WriteLine("  stance: running 0.70 | walking 0.85 | STANDING STILL 1.00 | sitting 1.50");
    Console.WriteLine($"  'bf' columns model the real buff stack at x{BuffPct:0.00} (Serenity-or-harmony x1.2, Mark x1.2).");
    Console.WriteLine();

    // ---- 1. THE SPT MODIFIER, per race -------------------------------------------------------
    Console.WriteLine("--- 1. SPT REGEN MODIFIER PER RACE (the new curve against the Max-MP one it left) ---");
    Console.WriteLine();
    Console.WriteLine("  race/class        SPT   old(MaxMP)   new(regen)   change");
    foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
        foreach (var cls in new[] { BaseClass.Mage, BaseClass.Fighter })
        {
            int spt = StatCalculator.GetBaseStats(race, cls).Spt;
            float old = StatCalculator.SptModifier(spt) / 1.4733f;
            float now = StatCalculator.SptRegenModifier(spt);
            Console.WriteLine($"  {race,-6} {cls,-9} {spt,4}   {old,10:0.000}   {now,10:0.000}   {(now / old - 1f) * 100,6:+0.0;-0.0}%");
        }
    Console.WriteLine();
    Console.WriteLine("  Every FIGHTER (SPT 25-27) sits on or beside the 0.70 floor - deliberate.");
    Console.WriteLine();

    // ---- 2. THE LADDER -----------------------------------------------------------------------
    Console.WriteLine("--- 2. HUMAN NUKER: DRAIN vs REGEN, LEVEL BY LEVEL ---");
    Console.WriteLine();
    Console.WriteLine("  'main' = the most MP-EFFICIENT spammable damage spell (reuse <= 5s) - the farm rotation.");
    Console.WriteLine("  'ceil' = the COSTLIEST spammable: the most a mage can burn without a burst skill.");
    Console.WriteLine("  'sust' = buffed-STANDING regen / main drain. Under 100% = mana is a real constraint.");
    Console.WriteLine();
    Console.WriteLine("     L   MaxMP  main spell          MP  cycle  drain/s   ceil/s | flat  nat.stnd nat.run  bf.stnd  bf.run  bf.sit | sust");
    Console.WriteLine("  ------------------------------------------------------------------------------------------------------------------------");

    foreach (int L in levels)
    {
        var e = BuildNuker(Race.Human, L);
        var (spell, mp, cycle, _, maxDrain) = BestSpam(e);
        float drain = cycle > 0 ? mp / cycle : 0f;

        float natStand = Mp(e, MoveState.Running, false, 1f);
        float natRun   = Mp(e, MoveState.Running, true,  1f);
        float bfStand  = Mp(e, MoveState.Running, false, BuffPct);
        float bfRun    = Mp(e, MoveState.Running, true,  BuffPct);
        float bfSit    = Mp(e, MoveState.Sitting, false, BuffPct);

        Console.WriteLine($"  {L,4} {e.MaxMp,7}  {Trim(spell, 18),-18} {mp,4} {cycle,5:0.0}s {drain,7:0.0} {maxDrain,8:0.0} | "
                        + $"{e.MpRegenBonus,4:0.0} {natStand,8:0.0} {natRun,7:0.0} {bfStand,8:0.0} {bfRun,7:0.0} {bfSit,7:0.0} | "
                        + $"{(drain > 0 ? bfStand / drain : 0) * 100,4:0}%");
    }
    Console.WriteLine();

    // ---- 3. CALM SPIRIT ----------------------------------------------------------------------
    Console.WriteLine("--- 3. CALM SPIRIT: the walk/stand equality it exists to buy ---");
    Console.WriteLine();
    Console.WriteLine("  A nuker at each rung's learn level, buffed, WITHOUT the passive and WITH it.");
    Console.WriteLine("  The design target is walk == stand at the TOP rung and nowhere before it.");
    Console.WriteLine();
    Console.WriteLine("     L  rung |  no CS: run   walk   stand |  with CS: run   walk   stand | walk/stand");
    Console.WriteLine("  ----------------------------------------------------------------------------------");
    int[] csLevels = { 40, 48, 56, 62, 68, 74 };
    for (int i = 0; i < csLevels.Length; i++)
    {
        int L = csLevels[i];
        var bare = BuildNuker(Race.Human, L);
        bare.LearnedSkills.Remove(SkillCatalog.CalmSpirit);
        bare.RecomputeDerived();

        var cs = BuildNuker(Race.Human, L);
        cs.LearnedSkills[SkillCatalog.CalmSpirit] = i + 1;
        cs.RecomputeDerived();

        float bRun = Mp(bare, MoveState.Running, true, BuffPct);
        float bWalk = Mp(bare, MoveState.Walking, true, BuffPct);
        float bStand = Mp(bare, MoveState.Running, false, BuffPct);
        float cRun = Mp(cs, MoveState.Running, true, BuffPct);
        float cWalk = Mp(cs, MoveState.Walking, true, BuffPct);
        float cStand = Mp(cs, MoveState.Running, false, BuffPct);

        Console.WriteLine($"  {L,4} {i + 1,5} | {bRun,11:0.0} {bWalk,6:0.0} {bStand,7:0.0} | {cRun,13:0.0} {cWalk,6:0.0} {cStand,7:0.0} |"
                        + $" {(cStand > 0 ? cWalk / cStand : 0) * 100,9:0.0}%");
    }
    Console.WriteLine();
    Console.WriteLine("  'walk/stand' reaching 100% is the whole skill: at that rung a kiting mage farms");
    Console.WriteLine("  exactly as well as a parked one. Running is a real cost at every rung.");
    Console.WriteLine();
}
/// <summary>A NUKER with his real class chain. BuildPlayer stops at the 2nd class, which for this
/// report is fatal: the whole `mpReg` ladder and every 40+ spell live on the 3rd/4th tables, so a
/// 2nd-class-only mage measured the level-35 kit at level 85 and reported Vampiric Bolt as his
/// costliest spell at every level. Mirrors BuildWarchanter's chain, on the Magus discipline.</summary>
static Entity BuildNuker(Race race, int level)
{
    var s = StatCalculator.GetBaseStats(race, BaseClass.Mage);
    var e = new Entity { Name = "nuker", Kind = EntityKind.Player, Race = race, BaseClass = BaseClass.Mage };
    e.Level = level;
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Agi = s.Agi; e.Spt = s.Spt;

    if (level >= 20)
        e.SecondClass = ClassCatalog.Playable.First(c => c.Race == race && c.Archetype == Archetype.Nuker).Id;
    if (level >= 40)
        e.ThirdClass = ThirdClassCatalog.Playable.First(c => c.Race == race && c.Discipline == Discipline.Magus).Id;

    foreach (var cs in ClassSkills.ForClass(race, BaseClass.Mage, null, null))
        if (cs.LearnLevel <= level)
            e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
    foreach (var cs in ClassSkills.Cumulative(race, BaseClass.Mage, e.Archetype, e.Discipline))
        if (cs.LearnLevel <= level)
            e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
    foreach (var id in e.LearnedSkills.Keys.ToList())
        if (SkillCatalog.Get(id)?.Replaces is { } replaced)
            foreach (var r in replaced) e.LearnedSkills.Remove(r);
    e.LearnedSkills[SkillCatalog.SpellcasterMastery] = 1;
    GrantFloorPassive(e, level);

    var rune = SkillCatalog.Get(SkillCatalog.SpellRuneBuff);
    if (rune != null)
        e.Buffs.Add(new Game.Server.Simulation.BuffInstance
        {
            Effect = rune.Effect, Magnitudes = rune.Magnitudes,
            TicksRemaining = int.MaxValue, Name = rune.Name, Key = rune.BuffKey,
        });

    int t = GearTier(level);
    Equip(e, $"staff_t{t}");   // TwoHandedBlunt — WeaponType.Base() maps it to Blunt, so the
    Equip(e, $"robe_t{t}");    // caster weapon mastery pays.
    foreach (var acc in new[] { "helm", "gloves", "boots" }) Equip(e, $"{acc}_t{t}");
    Equip(e, $"necklace_t{t}");
    Equip(e, $"ring_t{t}"); Equip(e, $"ring_t{t}");
    Equip(e, $"earring_t{t}"); Equip(e, $"earring_t{t}");

    e.RecomputeDerived();
    return e;
}

/// <summary>The MAIN NUKE — the spell a farming mage actually spams — priced on the auto-hunt's own
/// cycle (GameLoopService.AutoCycleTicks, replicated because it is private). Skills with a long reuse
/// (the 300s bursts) are excluded: they are not part of a sustained drain.
///
/// ⚠ "Main" is the most MP-EFFICIENT spammable at full damage, not the costliest. Ranking by drain
/// picks Vampiric Bolt at every level from 68 up — twice the price of Elemental Blast for the same
/// m.Atk, because it also lifesteals — and that overstates a farm rotation by a factor of two. The
/// costliest is returned alongside it as the CEILING, so both ends of the rotation are visible.</summary>
static (string Name, int Mp, float Cycle, string MaxName, float MaxDrain) BestSpam(Entity e)
{
    string name = "-"; int bestMp = 0; float bestCycle = 0f; float bestEff = -1f;
    string maxName = "-"; float maxDrain = 0f;

    foreach (var (id, lvl) in e.LearnedSkills)
    {
        if (SkillCatalog.Get(id) is not SkillDef d) continue;
        if (d.Category != SkillCategory.Magic) continue;
        int mp = d.MpCostAt(lvl);
        if (mp <= 0) continue;
        int cd = d.CooldownTicksAt(lvl);
        if (cd > 50) continue;                       // 5s+ reuse = a burst, not a spam rotation
        if (cd > 0 && e.CooldownReduction > 0f) cd = Math.Max(1, (int)(cd * (1f - e.CooldownReduction)));
        int castTicks = Math.Max(2, (int)(d.CastTicksAt(lvl) * e.EffectiveCastSpeedMultiplier));
        float cycle = (castTicks + cd) / 10f;
        float drain = mp / cycle;

        if (drain > maxDrain) { maxDrain = drain; maxName = d.Name; }

        // Damage per MP, on the same magic {Flat, Mod} the damage formula reads.
        var (flat, mod) = d.MagicDamageAt(lvl);
        float dmg = flat + mod * 40f;                // Mod is a multiplier on M.Atk; 40 = a scale, not a stat
        if (dmg <= 0f) continue;
        float eff = dmg / mp;
        if (eff <= bestEff) continue;
        bestEff = eff; name = d.Name; bestMp = mp; bestCycle = cycle;
    }
    return (name, bestMp, bestCycle, maxName, maxDrain);
}

static string Trim(string s, int n) => s.Length <= n ? s : s[..n];

// ============================================================================================
//  `--mpnpc` — THE FULLY NPC-BUFFED ELF MAGE AT 74, all three roles. Owner, 2026-08-27:
//  *"show me npc buffed mage (elf - the worst of the 3) at lvl 74 (healer/nuker/buffer-toggles) ->
//  what is the drain (reuse passives/cast speed reduction) vs mp regen and mp pool"*.
//
//  🔑 THIS IS THE HONEST END OF THE LADDER, and it is the opposite of `--mpcase`'s. There the
//  question was whether a bare 43 pays for himself; here everything that can be stacked IS stacked,
//  which cuts BOTH ways and is the whole reason to measure rather than guess:
//      Soul     +35% Max MP        → a bigger bar to spend
//      Serenity +20% MP regen      → a faster refill
//      Alacrity +30% cast speed    → a SHORTER cycle, i.e. MORE drain per second
//  plus every reuse-reduction passive the class owns, which shortens the cycle again. A buff pack
//  sold as "sustain" is, for a caster, mostly an accelerator.
//
//  ⚠ ELF ON PURPOSE — his pick, and the measurement backs it: fastest cast (highest WIT) on the
//  lowest SPT, so he empties fastest and refills slowest. Human and demon are printed beside him so
//  the spread is visible rather than asserted.
// ============================================================================================

static void MpNpc()
{
    // Every `npc_` blessing in the catalog — the full shelf, which is what a player actually walks
    // away with. A NpcSingle carries no magnitudes of its own: it names ONE child (the family's rung)
    // and ApplyBuff resolves it, so the child is what has to land here too.
    static void ApplyNpcBuffs(Entity e)
    {
        foreach (var def in SkillCatalog.AllSkills)
        {
            if (!def.Id.StartsWith("npc_", StringComparison.Ordinal)) continue;
            var kids = def.ChildBuffsAt(1);
            var src = kids is { Length: 1 } && SkillCatalog.Get(kids[0]) is { } kid ? kid : def;
            e.Buffs.Add(new Game.Server.Simulation.BuffInstance
            {
                Effect = src.Effect,
                // ⚠ A child with no magnitudes (a pure flag buff) leaves this null, and BuffInstance
                // .Percent walks it unguarded — an empty array, not null.
                Magnitudes = src.Magnitudes ?? Array.Empty<EffectMagnitude>(),
                TicksRemaining = int.MaxValue, Name = src.Name, Key = src.BuffKey, Rank = src.Rank,
            });
        }
        e.RecomputeDerived();
    }

    static float Mp(Entity e, MoveState st, bool moving)
    {
        float stance = MovementTuning.RegenMultiplier(st, moving);
        float calm = st == MoveState.Sitting ? 1f
            : !moving                        ? e.MpRegenStandMult
            : st == MoveState.Walking        ? e.MpRegenWalkMult
                                             : e.MpRegenRunMult;
        float pct = e.Buffs.Where(b => b.Has(SkillEffect.BuffMpRegen)).Sum(b => b.Percent(SkillEffect.BuffMpRegen));
        float flat = e.Buffs.Where(b => b.Has(SkillEffect.BuffMpRegen)).Sum(b => b.Flat(SkillEffect.BuffMpRegen));
        return StatCalculator.MpRegenPerSecond(e.EffectiveSpt, e.Level)
                   * stance * calm * e.MpRegenMult * (1f + pct)
               + e.MpRegenBonus + flat;
    }

    static int Cost(Entity e, SkillDef d, int lvl) =>
        (int)(d.MpCostAt(lvl) * (1f - (d.Category == SkillCategory.Physical
            ? e.PhysMpCostReduction : e.MagicMpCostReduction)));

    static (float Cast, float Reuse) Cycle(Entity e, SkillDef d, int lvl)
    {
        float castMult = d.Category == SkillCategory.Physical
            ? e.EffectiveAttackSpeedMultiplier : e.EffectiveCastSpeedMultiplier;
        int castTicks = Math.Max(2, (int)(d.CastTicksAt(lvl) * castMult));
        int cd = d.CooldownTicksAt(lvl);
        if (cd > 0 && e.CooldownReduction > 0f) cd = Math.Max(1, (int)(cd * (1f - e.CooldownReduction)));
        return (castTicks / 10f, cd / 10f);
    }

    static (SkillDef? D, int Lvl) Pick(Entity e, params string[] ids)
    {
        foreach (var id in ids)
            if (e.LearnedSkills.TryGetValue(id, out int sl) && SkillCatalog.Get(id) is { } d)
                return (d, sl);
        return (null, 0);
    }

    Console.WriteLine();
    Console.WriteLine("=== FULLY NPC-BUFFED MAGE AT 74 — drain vs regen vs pool (2026-08-27) ===");
    Console.WriteLine();
    Console.WriteLine("  The whole npc_ shelf is applied: Soul +35% Max MP, Serenity +20% MP regen, Alacrity");
    Console.WriteLine("  +30% cast, Force/Ward/Insight and the rest. Regen is the BUFFED number; 'still' is a");
    Console.WriteLine("  parked farmer, 'run' a kiter. Reuse is the base cut by the class's reuse passives.");
    Console.WriteLine();

    foreach (var race in new[] { Race.Elf, Race.Human, Race.Demon })
    {
        Console.WriteLine($"--- {race.ToString().ToUpperInvariant()} ---");
        Console.WriteLine();
        Console.WriteLine("   role     pool   cast(x)  reuse-  spell              MP   cast  reuse  cycle  drain/s | regen still  run |   net/s   empties");
        Console.WriteLine("  ----------------------------------------------------------------------------------------------------------------------------");

        void Line(string role, Entity e, SkillDef? d, int lvl, float extraDrain, string extraLabel)
        {
            float still = Mp(e, MoveState.Running, false);
            float run = Mp(e, MoveState.Running, true);
            float drain = extraDrain;
            string spell = extraLabel; float cast = 0f, reuse = 0f, cyc = 0f; int mp = 0;
            if (d is not null)
            {
                mp = Cost(e, d, lvl);
                (cast, reuse) = Cycle(e, d, lvl);
                cyc = cast + reuse;
                drain += cyc > 0 ? mp / cyc : 0f;
                spell = Trim(d.Name, 17);
            }
            float net = drain - still;
            Console.WriteLine($"   {role,-7} {e.MaxMp,6}   {1f / e.EffectiveCastSpeedMultiplier,6:0.00}  {e.CooldownReduction * 100,5:0}%  {spell,-17}"
                            + $" {mp,4} {cast,5:0.00}s {reuse,5:0.00}s {cyc,5:0.00}s {drain,8:0.00} |"
                            + $" {still,11:0.0} {run,5:0.0} | {net,7:+0.00;-0.00}   {(net <= 0.05f ? "never" : $"{e.MaxMp / net,0:0}s")}");
        }

        // ---- HEALER: his attack spell (the farm rotation), then his workhorse heal (party duty).
        var healer = BuildCasterFor(race, 74, Archetype.Healer, Discipline.Lightbringer);
        ApplyNpcBuffs(healer);
        var (hRay, hRayL) = Pick(healer, SkillCatalog.HolyRay, SkillCatalog.HolyStrike);
        var (hHeal, hHealL) = Pick(healer, SkillCatalog.GreatHeal, SkillCatalog.Heal);
        Line("healer", healer, hRay, hRayL, 0f, "-");
        Line("  heal", healer, hHeal, hHealL, 0f, "-");

        // ---- NUKER: the main nuke.
        var nuker = BuildCasterFor(race, 74, Archetype.Nuker, Discipline.Magus);
        ApplyNpcBuffs(nuker);
        var (nuke, nukeL) = Pick(nuker, SkillCatalog.ElementalBlast, SkillCatalog.ElementalBolt);
        Line("nuker", nuker, nuke, nukeL, 0f, "-");

        // ---- BUFFER: the two toggles alone, then toggles + his sound skill.
        var buffer = BuildCasterFor(race, 74, Archetype.Healer, Discipline.Warchanter);
        ApplyNpcBuffs(buffer);
        var reinf = SkillCatalog.Get(SkillCatalog.WcReinforcement);
        var sharp = SkillCatalog.Get(SkillCatalog.WcSharpening);
        int rr = buffer.SkillLevelOf(SkillCatalog.WcReinforcement);
        int sr = buffer.SkillLevelOf(SkillCatalog.WcSharpening);
        float toggles = (reinf != null && rr > 0 ? reinf.MpPerSecondAt(rr) : 0)
                      + (sharp != null && sr > 0 ? sharp.MpPerSecondAt(sr) : 0);
        Line("toggles", buffer, null, 0, toggles, $"Reinf r{rr} + Sharp r{sr}");

        string bestName = "-"; float bestDrain = 0f; SkillDef? bestDef = null; int bestLvl = 0;
        foreach (var (id, sl) in buffer.LearnedSkills)
        {
            if (SkillCatalog.Get(id) is not SkillDef dd) continue;
            if ((dd.Effect & (SkillEffect.MagicDamage | SkillEffect.PhysicalDamage)) == 0) continue;
            int c = Cost(buffer, dd, sl);
            if (c <= 0) continue;
            var (ca, re) = Cycle(buffer, dd, sl);
            if (re > 5f) continue;
            float cy = ca + re;
            if (cy <= 0f) continue;
            if (c / cy > bestDrain) { bestDrain = c / cy; bestName = dd.Name; bestDef = dd; bestLvl = sl; }
        }
        Line("+attack", buffer, bestDef, bestLvl, toggles, bestName);
        Console.WriteLine();
    }

    Console.WriteLine("  🔑 READ THE 'reuse-' COLUMN. The reuse passives and Alacrity both SHORTEN the cycle, so");
    Console.WriteLine("  the same spell costs more per SECOND the better buffed you are. A full NPC pack raises");
    Console.WriteLine("  the pool 35% and the regen 20%, and raises the drain by more than either.");
    Console.WriteLine();
    Console.WriteLine("  🔑 'empties' is unbroken casting from FULL with no potion. Against it, a potion tier's");
    Console.WriteLine("  SUSTAINED rate (half the sticker - see the table below) is what actually has to cover");
    Console.WriteLine("  the 'net' column - not its sticker number.");
    Console.WriteLine();
    // ---- WHAT ACTUALLY COVERS IT, AND WHAT THAT COSTS -----------------------------------------
    //
    // The 'net' column is the question a potion has to answer, and a potion answers it at its
    // SUSTAINED rate (15s up on a 30s reuse = half the sticker), not its sticker. The gold column is
    // the other half of the decision: a 30s reuse means TWO drinks a minute, for as long as you farm.
    //
    // ⚠ EVERY NUMBER HERE IS READ OFF THE CATALOG, never typed. The table was hard-coded to the
    // 0.92.0 ladder (20/50/100 at 500/1500/4500) and went stale the day he retuned it; the rate
    // lives on the potion's SKILL (the RestoreMp Flat magnitude = MP per second) and the price on
    // the ITEM, so both are looked up.
    Console.WriteLine("--- WHAT COVERS THE NET, AND WHAT IT COSTS TO HOLD ---");
    Console.WriteLine();
    Console.WriteLine("   tier        sticker  sustained   buy   gold/min (2 drinks)   gold/hour");
    Console.WriteLine("  ---------------------------------------------------------------------------");
    foreach (var (name, itemId) in new[] {
        ("Common",   ItemCatalog.MinorManaPotion),
        ("Uncommon", ItemCatalog.ManaPotion),
        ("Rare",     ItemCatalog.GreaterManaPotion) })
    {
        var item = ItemCatalog.Get(itemId)!;
        var skill = SkillCatalog.Get(item.UseSkillId!)!;
        float sticker = skill.Magnitudes.FirstOrDefault(m => m.Effect == SkillEffect.RestoreMp).Value;
        int buy = item.Value;
        Console.WriteLine($"   {name,-10} {sticker,6:0} MP/s {sticker / 2f,7:0} MP/s {buy,6} {buy * 2,15:N0} {buy * 120,13:N0}");
    }
    Console.WriteLine();
    Console.WriteLine("  🔑 A POTION ON COOLDOWN IS NOT ITS SUSTAINED RATE MINUS THE DRAIN — it alternates. Over");
    Console.WriteLine("  one 30s cycle an ELF HEALER at 74 takes 70x15 = 1,050 MP from an Uncommon, 12.8x30 = 384");
    Console.WriteLine("  from regen, and spends 38.33x30 = 1,150: net +284 MP per cycle, i.e. +9.5 MP/s against a");
    Console.WriteLine("  4,605 bar. He farms indefinitely on one potion line. The ladder as authored lands where");
    Console.WriteLine("  it was aimed: UNCOMMON is the healer's and the buffer's (35 sustained vs their 25.6 / 32.0),");
    Console.WriteLine("  COMMON is the low-level and the nuker's, RARE (75) is the raid/economy item.");
    Console.WriteLine();
}

// ============================================================================================
//  `--mpcase` — THE OWNER'S OWN CHARACTER, not a best-geared abstraction. He read section 2 of
//  `--mpdrain` ("below ~45 nobody has an MP problem") against what he actually plays and it did
//  not match: *"Ork healer fight for 1min and mp is depleted ... 43lvl ... E robe + wand/shield"*.
//
//  He is right, and the table was measuring a different character. Three assumptions in it are
//  wrong for a real level-43 healer, and each one costs mana:
//
//    1. IT NEVER HEALS. A healer's rotation is not an attack spell. Heal/Great Heal is the most
//       expensive thing on his bar and the only one he MUST cast, so a bolt-only drain is a floor,
//       not an estimate.
//    2. IT IS FULLY BUFFED. `bf.st` assumes Serenity-or-harmony AND the Mark, x1.44. A soloing
//       43 has neither; the honest column for him is `nat`.
//    3. IT IS PERFECTLY GEARED. `--mpdrain` equips GearTier(43) = the t40 band. He is in E-grade
//       (the t20 band) — one whole grade under-tier — and holding a WAND + SHIELD where the table
//       gives a two-handed staff.
//
//  So this measures HIS build, and prints TIME TO EMPTY against his own "1 minute".
// ============================================================================================

static void MpCase()
{
    Console.WriteLine();
    Console.WriteLine("=== HIS CASE: the level-43 demon healer, measured as he actually plays it ===");
    Console.WriteLine();

    // Mirrors GameLoopService.Regenerate's MP branch (flats outside, 0.88.0).
    static float Mp(Entity e, bool moving, float buffPct)
    {
        float stance = MovementTuning.RegenMultiplier(MoveState.Running, moving);
        float calm = moving ? e.MpRegenRunMult : e.MpRegenStandMult;
        return StatCalculator.MpRegenPerSecond(e.EffectiveSpt, e.Level)
                   * stance * calm * e.MpRegenMult * buffPct
               + e.MpRegenBonus;
    }

    static int Cost(Entity e, SkillDef d, int lvl) =>
        (int)(d.MpCostAt(lvl) * (1f - (d.Category == SkillCategory.Physical
            ? e.PhysMpCostReduction : e.MagicMpCostReduction)));

    static (float Cast, float Reuse) Cycle(Entity e, SkillDef d, int lvl)
    {
        float castMult = d.Category == SkillCategory.Physical
            ? e.EffectiveAttackSpeedMultiplier : e.EffectiveCastSpeedMultiplier;
        int castTicks = Math.Max(2, (int)(d.CastTicksAt(lvl) * castMult));
        int cd = d.CooldownTicksAt(lvl);
        if (cd > 0 && e.CooldownReduction > 0f) cd = Math.Max(1, (int)(cd * (1f - e.CooldownReduction)));
        return (castTicks / 10f, cd / 10f);
    }

    // The healer's real bar at 43: his attack spell and his single-target heal, each at the rung
    // he owns. A rotation is N of the first per 1 of the second — casting is serial, so the mix is
    // (n*bolt + heal) MP over (n*boltCycle + healCycle) seconds. That is the whole model.
    static (SkillDef? D, int Lvl) Best(Entity e, params string[] ids)
    {
        foreach (var id in ids)
            if (e.LearnedSkills.TryGetValue(id, out int sl) && SkillCatalog.Get(id) is { } d)
                return (d, sl);
        return (null, 0);
    }

    // ---- The two builds, side by side: what the table measured, and what he is holding ---------
    var builds = new (string Label, bool Buffed, Func<Entity> Make)[]
    {
        ("--mpdrain's build: t40 staff + robe, BUFFED x1.44", true,
            () => BuildCasterFor(Race.Demon, 43, Archetype.Healer, Discipline.Lightbringer)),
        ("HIS build: E-grade (t20) robe + wand + shield, UNBUFFED", false,
            () => BuildHisHealer(43)),
    };

    foreach (var (label, buffed, make) in builds)
    {
        var e = make();
        float pct = buffed ? 1.20f * 1.20f : 1f;
        var (atk, atkLvl) = Best(e, SkillCatalog.HolyRay, SkillCatalog.HolyStrike);
        var (heal, healLvl) = Best(e, SkillCatalog.GreatHeal, SkillCatalog.Heal);

        Console.WriteLine($"--- {label} ---");
        Console.WriteLine();
        Console.WriteLine($"  WIT {e.EffectiveWit}   SPT {e.EffectiveSpt}   cast x{1f / e.EffectiveCastSpeedMultiplier:0.00}"
                        + $"   MaxMP {e.MaxMp}   MpRegenMult {e.MpRegenMult:0.00}   flats +{e.MpRegenBonus:0.0}");
        Console.WriteLine($"  regen: standing {Mp(e, false, pct):0.0}/s   running {Mp(e, true, pct):0.0}/s");
        // ---- WHY the cast speed is what it is. He gave a LIVE x1.30 against this model's number, and
        //      a cast-speed error is the whole report: it divides the cycle, so it multiplies the drain.
        Console.WriteLine($"    cast chain: classBase {StatCalculator.ClassBaseCastSpeed(e.Race, e.BaseClass)}"
                        + $" x witMod {StatCalculator.CastWitModifier(e.EffectiveWit):0.000}"
                        + $" x gearFactor {1f / Math.Max(0.05f, e.CastSpeedMultiplier):0.000}"
                        + $" (CastSpeedMultiplier {e.CastSpeedMultiplier:0.000})"
                        + $" x penalty {e.CastSpeedPenaltyMult:0.00} + flat {e.CastSpeedFlatBonus:0.0}");
        Console.Write("    masteries/passives held:");
        foreach (var (id, lv) in e.LearnedSkills)
            if (SkillCatalog.Get(id) is { } sd && (sd.ArmorMasteryAt(lv) is not null || id.Contains("mastery")))
                Console.Write($" {id}@{lv}");
        Console.WriteLine();
        if (atk is not null)
        {
            var (c, r) = Cycle(e, atk, atkLvl);
            Console.WriteLine($"  {atk.Name} L{atkLvl}: {Cost(e, atk, atkLvl)} MP, {c:0.0}s cast + {r:0.0}s reuse");
        }
        if (heal is not null)
        {
            var (c, r) = Cycle(e, heal, healLvl);
            Console.WriteLine($"  {heal.Name} L{healLvl}: {Cost(e, heal, healLvl)} MP, {c:0.0}s cast + {r:0.0}s reuse");
        }
        Console.WriteLine();
        Console.WriteLine("   rotation                 drain/s   regen/s     net/s   FULL BAR EMPTIES IN");
        Console.WriteLine("  ------------------------------------------------------------------------------");

        float regen = Mp(e, false, pct);
        void Row(string name, float mp, float secs)
        {
            float drain = secs > 0 ? mp / secs : 0f;
            float net = drain - regen;
            string empty = net <= 0.05f ? "never (regen wins)" : $"{e.MaxMp / net,6:0} s";
            Console.WriteLine($"   {name,-22} {drain,8:0.0} {regen,9:0.0} {net,9:+0.0;-0.0}   {empty}");
        }

        if (atk is not null)
        {
            var (ac, ar) = Cycle(e, atk, atkLvl);
            int amp = Cost(e, atk, atkLvl);
            Row("attack spell only", amp, ac + ar);

            if (heal is not null)
            {
                var (hc, hr) = Cycle(e, heal, healLvl);
                int hmp = Cost(e, heal, healLvl);
                foreach (int n in new[] { 3, 2, 1 })
                    Row($"{n} attack : 1 heal", n * amp + hmp, n * (ac + ar) + (hc + hr));
                Row("heal only (party duty)", hmp, hc + hr);
            }
        }
        Console.WriteLine();
    }


    // 🔑 THE CAST SPEED IS A BUFF STACK, NOT A STAT. First attempt at this section inflated WIT by 6
    // to reproduce his x1.30, which he corrected on the spot: *"Ork have 19 wit but u dont take into
    // the acount alacruty/frenzy/passives"*. He is right — WIT stays at the racial 19; what the model
    // was missing is the buffs a real caster runs. So Alacrity is applied as a REAL buff here, and the
    // remainder is named rather than hidden inside a fudged stat.
    //
    // A cast-speed error is not a detail in this report: it DIVIDES the cycle, so it MULTIPLIES the
    // drain. It is exactly how "no MP problem below 45" came to be written.
    var alacrity = SkillCatalog.Get(SkillCatalog.BuffAlacrityR);   // the Rare rung, +30% cast

    Console.WriteLine("--- PINNED TO LIVE: base WIT + a real Alacrity (Rare) buff ---");
    Console.WriteLine();
    Console.WriteLine("  Holy Ray L1 only. No heals, nothing between casts - his test exactly. Regen UNBUFFED,");
    Console.WriteLine("  because his reading is (his buffed pair is 9.4 / 8.2 / 7.0).");
    Console.WriteLine($"  'his x1.30' recomputes the same character at the cast speed he MEASURED, whatever the");
    Console.WriteLine("  rest of his stack is (Alacrity alone does not reach it - Combo Rush and passives are the");
    Console.WriteLine("  balance). Both rows are shown so the gap between model and client stays visible.");
    Console.WriteLine();
    Console.WriteLine("   race    WIT  SPT   cast(x)   cast  reuse  cycle    MP  drain/s | regen still  walk   run |   net/s");
    Console.WriteLine("  --------------------------------------------------------------------------------------------------");
    foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
    {
        var e = BuildHisHealer(43, race: race);
        if (alacrity != null)
            e.Buffs.Add(new Game.Server.Simulation.BuffInstance
            {
                Effect = alacrity.Effect, Magnitudes = alacrity.Magnitudes,
                TicksRemaining = int.MaxValue, Name = alacrity.Name, Key = alacrity.BuffKey,
            });
        e.RecomputeDerived();

        var (d, l) = Best(e, SkillCatalog.HolyRay, SkillCatalog.HolyStrike);
        if (d is null) continue;
        int mp = Cost(e, d, l);

        float still = Mp(e, false, 1f);
        float walk = StatCalculator.MpRegenPerSecond(e.EffectiveSpt, e.Level)
                        * MovementTuning.RegenMultiplier(MoveState.Walking, true) * e.MpRegenWalkMult
                        * e.MpRegenMult + e.MpRegenBonus;
        float run = Mp(e, true, 1f);

        // Two rows: what the model derives, and the same character at HIS measured cast speed.
        void Row(string tag, float castX)
        {
            float cast = Math.Max(0.2f, d.CastTicksAt(l) / 10f / castX);
            int cd = d.CooldownTicksAt(l);
            if (cd > 0 && e.CooldownReduction > 0f) cd = Math.Max(1, (int)(cd * (1f - e.CooldownReduction)));
            float reuse = cd / 10f;
            float cycle = cast + reuse;
            float drain = mp / cycle;
            Console.WriteLine($"   {tag,-6} {e.EffectiveWit,4} {e.EffectiveSpt,4}    {castX,6:0.00}"
                            + $" {cast,6:0.00}s {reuse,5:0.00}s {cycle,5:0.00}s {mp,5} {drain,8:0.00} |"
                            + $" {still,11:0.0} {walk,5:0.0} {run,5:0.0} | {drain - still,7:+0.00;-0.00}");
        }

        Row(race.ToString(), 1f / e.EffectiveCastSpeedMultiplier);
        if (race == Race.Demon) Row("  his", 1.30f);
    }
    Console.WriteLine();
    Console.WriteLine("  HIS PREDICTION HOLDS: the elf casts fastest AND regenerates slowest, so he is worst off");
    Console.WriteLine("  at every level; the demon is best off and is STILL negative. Standing still, unbuffed, on");
    Console.WriteLine("  the cheapest spell he owns, with nothing between casts, a 43 does not pay for himself.");
    Console.WriteLine("  That is the case for the potion - and the COMMON tier (10 MP/s sustained) covers this");
    Console.WriteLine("  deficit several times over, which is the right shape: a potion that trivialises the");
    Console.WriteLine("  problem at 43 should still be the cheap one at 80.");
    Console.WriteLine();
    // ---- POOL SENSITIVITY. At 43 the bar is mostly JEWELLERY, so what he is NOT wearing moves it
    // further than his level does — and "how many casts a full bar buys" is the number his minute
    // is really made of, not MP/s. Five accessory slots is the difference between a bar that lasts
    // and a bar that is gone.
    Console.WriteLine("--- POOL SENSITIVITY: what the bar is actually made of ---");
    Console.WriteLine();
    Console.WriteLine("  build                                        MaxMP   casts a full bar buys   60s of casting costs");
    Console.WriteLine("  ---------------------------------------------------------------------------------------------------");
    foreach (var (lbl, jw, wp) in new[] {
        ("robe + wand/shield + FULL t20 jewels", true,  true),
        ("robe + wand/shield, NO jewels",        false, true),
        ("robe only, no weapon, no jewels",      false, false) })
    {
        var h = BuildHisHealer(43, jewels: jw, weapon: wp);
        var (d, l) = Best(h, SkillCatalog.HolyRay, SkillCatalog.HolyStrike);
        if (d is null) continue;
        int c = Cost(h, d, l);
        var (ca, re) = Cycle(h, d, l);
        float cyc = ca + re;
        Console.WriteLine($"  {lbl,-42} {h.MaxMp,5}   {(c > 0 ? h.MaxMp / c : 0),13} casts   {(cyc > 0 ? 60f / cyc * c : 0),12:0} MP");
    }
    Console.WriteLine();
    Console.WriteLine("  Read the last column against the first: if 60s of casting costs MORE than the bar holds,");
    Console.WriteLine("  his minute is real and the model's is the number that is wrong.");
    Console.WriteLine();

    Console.WriteLine("  🔑 WHAT THIS RULED OUT. The heal was the obvious suspect and it is not guilty: Great Heal");
    Console.WriteLine("  is 62 MP over a 6.8s cycle against Holy Ray's 30 over 3.3s — 9.1 MP/s either way — so the");
    Console.WriteLine("  rotation mix barely moves the number. Jewellery is not it either: five accessory slots add");
    Console.WriteLine("  ZERO Max MP, the pool is pure level + SPT. And regen is NOT blocked while casting (there is");
    Console.WriteLine("  no cast guard before Regenerate in the tick loop).");
    Console.WriteLine();
    Console.WriteLine("  So the model says Holy Ray spam CANNOT empty a 43 in a minute: 60s of casting costs 545 MP");
    Console.WriteLine("  out of a 1462 bar, and regen very nearly pays for it. Something on the live server differs");
    Console.WriteLine("  from every input above — the two numbers that would settle it are his ACTUAL Max MP and how");
    Console.WriteLine("  many Holy Rays a full bar really buys him.");
    Console.WriteLine();
}

/// <summary>HIS level-43 ork healer, verbatim: *"E robe + wand/shield"*. E-grade is the t20 band
/// (ItemCatalog: level ≥ 20 → E, ≥ 40 → D), so at 43 he is a full grade under-tier — which is the
/// normal way to play, not a mistake, because a grade costs real gold. No rune buff: the report's
/// whole point is what a soloing 43 actually has.</summary>
static Entity BuildHisHealer(int level, bool jewels = true, bool weapon = true,
                             Race race = Race.Demon, int witBonus = 0)
{
    var s = StatCalculator.GetBaseStats(race, BaseClass.Mage);
    var e = new Entity { Name = "his healer", Kind = EntityKind.Player, Race = race, BaseClass = BaseClass.Mage };
    e.Level = level;
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit + witBonus; e.Agi = s.Agi; e.Spt = s.Spt;

    e.SecondClass = ClassCatalog.Playable.First(c => c.Race == race && c.Archetype == Archetype.Healer).Id;
    if (level >= 40)
        e.ThirdClass = ThirdClassCatalog.Playable.First(c => c.Race == race && c.Discipline == Discipline.Lightbringer).Id;

    foreach (var cs in ClassSkills.ForClass(race, BaseClass.Mage, null, null))
        if (cs.LearnLevel <= level)
            e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
    foreach (var cs in ClassSkills.Cumulative(race, BaseClass.Mage, e.Archetype, e.Discipline))
        if (cs.LearnLevel <= level)
            e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
    foreach (var id in e.LearnedSkills.Keys.ToList())
        if (SkillCatalog.Get(id)?.Replaces is { } replaced)
            foreach (var r in replaced) e.LearnedSkills.Remove(r);
    e.LearnedSkills[SkillCatalog.SpellcasterMastery] = 1;
    GrantFloorPassive(e, level);

    // E-grade = the t20 band. Wand + shield, not a staff.
    if (weapon) { Equip(e, "wand_t20"); Equip(e, "shield_t20"); }
    Equip(e, "robe_t20");
    foreach (var acc in new[] { "helm", "gloves", "boots" }) Equip(e, $"{acc}_t20");
    if (jewels)
    {
        Equip(e, "necklace_t20"); Equip(e, "ring_t20"); Equip(e, "ring_t20");
        Equip(e, "earring_t20"); Equip(e, "earring_t20");
    }

    e.RecomputeDerived();
    return e;
}

// ============================================================================================
//  `--mpdrain` — WHAT A CASTER ACTUALLY BURNS, PER SECOND, PER RACE. Owner, 2026-08-27, opening
//  the MP-potion question: *"Just need to measure what is the mp consumption at 20,30,40,50,60,
//  70,80 (healer-holy bolt, Mage-elemental bolt, buffer with reinforcement and sharpening active)
//  vs current mp regen (don't say % I want flat numbers ... 10/s drain vs 5/s regen) but take into
//  an account spt/wit cast speed elf casts faster so 2.5 cast with higher wit and lower mp regen
//  is different from 2.5 cast with lower wit and higher spt"*.
//
//  `--mpregen` (BL-92) answered "is the mage's regen too big" for ONE human nuker on his most
//  EFFICIENT spell. This answers a different question, and it is the one a potion is sized off:
//  the named spell, all three races, the whole level ladder, in FLAT MP/s on both sides.
//
//  ⚠ Race is the entire point, so every term race touches is measured, not assumed:
//      WIT  → cast speed (exponential, x1.63 per +10 WIT) → shorter cycle → HIGHER drain.
//      SPT  → SptRegenModifier (0.70..1.30)               → regen.
//  An elf pays for his speed twice: he empties the bar faster AND refills it slower.
//
//  ⚠ NOTHING HERE CHANGES THE ENGINE. Drain mirrors GameLoopService.AutoCycleTicks +
//  EffectiveMpCost; regen mirrors Regenerate's MP branch, flats outside, exactly as 0.88.0 left it.
// ============================================================================================

static void MpDrain(int[] argLevels)
{
    // The real buff stack a farming caster carries, same as `--mpregen` models it: Serenity-or-
    // harmony x1.2 (ONE family, the harmony evicts the single) and the Mark's x1.2.
    const float BuffPct = 1.20f * 1.20f;

    int[] levels = argLevels.Length > 0 ? argLevels : new[] { 20, 30, 40, 50, 60, 70, 80 };
    var races = new[] { Race.Human, Race.Elf, Race.Demon };

    // Mirrors GameLoopService.Regenerate's MP branch EXACTLY (flats OUTSIDE, 0.88.0).
    static float Mp(Entity e, MoveState st, bool moving, float buffPct)
    {
        float stance = MovementTuning.RegenMultiplier(st, moving);
        float calm = st == MoveState.Sitting ? 1f
            : !moving                        ? e.MpRegenStandMult
            : st == MoveState.Walking        ? e.MpRegenWalkMult
                                             : e.MpRegenRunMult;
        return StatCalculator.MpRegenPerSecond(e.EffectiveSpt, e.Level)
                   * stance * calm * e.MpRegenMult * buffPct
               + e.MpRegenBonus;
    }

    // GameLoopService.EffectiveMpCost, verbatim: authored total x the caster's MP-cost buffs.
    static int Cost(Entity e, SkillDef d, int lvl) =>
        (int)(d.MpCostAt(lvl) * (1f - (d.Category == SkillCategory.Physical
            ? e.PhysMpCostReduction : e.MagicMpCostReduction)));

    // GameLoopService.AutoCycleTicks, verbatim (it is private): cast x castSpeed + reduced reuse.
    static (float Cast, float Reuse) Cycle(Entity e, SkillDef d, int lvl)
    {
        float castMult = d.Category == SkillCategory.Physical
            ? e.EffectiveAttackSpeedMultiplier : e.EffectiveCastSpeedMultiplier;
        int castTicks = Math.Max(2, (int)(d.CastTicksAt(lvl) * castMult));
        int cd = d.CooldownTicksAt(lvl);
        if (cd > 0 && e.CooldownReduction > 0f) cd = Math.Max(1, (int)(cd * (1f - e.CooldownReduction)));
        return (castTicks / 10f, cd / 10f);
    }

    static string Empty(int pool, float drain, float regen)
    {
        float net = drain - regen;
        return net <= 0.05f ? "  never" : $"{pool / net,5:0}s";
    }

    Console.WriteLine();
    Console.WriteLine("=== MP DRAIN vs MP REGEN - FLAT MP/s, ALL THREE RACES (2026-08-27) ===");
    Console.WriteLine();
    Console.WriteLine("  drain/s = EffectiveMpCost / (cast x castSpeed + reuse)      <- AutoCycleTicks");
    Console.WriteLine("  MP/s    = [ (2+0.08L) x SptRegenModifier x MpRegenMult x (1+buff%) x stance ] + flats");
    Console.WriteLine($"  'bf' = the real buff stack, x{BuffPct:0.00} (Serenity-or-harmony x1.2, Mark x1.2).");
    Console.WriteLine("  stance: STANDING 1.00, running 0.70. A farming caster stands to cast, so 'bf.st' is the");
    Console.WriteLine("  honest one; 'bf.run' is what a kiter gets.");
    Console.WriteLine("  'empty' = seconds of unbroken casting from a FULL bar before it hits zero, at bf.st.");
    Console.WriteLine();

    // ---- 0. WHAT RACE IS, IN THE TWO TERMS THAT MATTER ---------------------------------------
    Console.WriteLine("--- 0. THE RACE SPLIT: the two stats this whole report turns on ---");
    Console.WriteLine();
    Console.WriteLine("  race    WIT  castSpeed(x)   SPT  SptRegenMod   verdict");
    foreach (var r in races)
    {
        var s = StatCalculator.GetBaseStats(r, BaseClass.Mage);
        var probe = BuildCasterFor(r, 60, Archetype.Nuker, Discipline.Magus);
        float cx = probe.EffectiveCastSpeedMultiplier;
        Console.WriteLine($"  {r,-6} {s.Wit,4}   {1f / cx,10:0.00}x  {s.Spt,4}   {StatCalculator.SptRegenModifier(s.Spt),10:0.000}"
                        + $"   {(r == Race.Elf ? "casts fastest, regens slowest" : r == Race.Demon ? "casts slowest, regens fastest" : "the middle - the baseline")}");
    }
    Console.WriteLine();
    Console.WriteLine("  (castSpeed measured on a level-60 nuker with real gear - class base x WIT x robe.)");
    Console.WriteLine();

    // ---- 1 & 2. THE TWO SPAMMERS -------------------------------------------------------------
    var tables = new (string Title, Archetype Arch, Discipline Disc, string[] Ids)[]
    {
        ("1. HEALER - Holy Bolt (Holy Ray REPLACES it at 40: the bolt leaves the bar)",
            Archetype.Healer, Discipline.Lightbringer,
            new[] { SkillCatalog.HolyRay, SkillCatalog.HolyStrike }),
        ("2. NUKER - Elemental Bolt (Elemental Blast REPLACES it at 40)",
            Archetype.Nuker, Discipline.Magus,
            new[] { SkillCatalog.ElementalBlast, SkillCatalog.ElementalBolt }),
    };

    foreach (var (title, arch, disc, ids) in tables)
    {
        Console.WriteLine($"--- {title} ---");
        Console.WriteLine();
        Console.WriteLine("     L  race    WIT  SPT  spell             rung  cast  reuse  cycle    MP  drain/s |  nat.st   bf.st  bf.run |   net/s    pool   empty");
        Console.WriteLine("  ------------------------------------------------------------------------------------------------------------------------------------");
        foreach (int L in levels)
        {
            foreach (var race in races)
            {
                var e = BuildCasterFor(race, L, arch, disc);
                SkillDef? d = null; int lvl = 0;
                foreach (var id in ids)
                    if (e.LearnedSkills.TryGetValue(id, out int sl) && SkillCatalog.Get(id) is { } got)
                    { d = got; lvl = sl; break; }

                float natSt = Mp(e, MoveState.Running, false, 1f);
                float bfSt  = Mp(e, MoveState.Running, false, BuffPct);
                float bfRun = Mp(e, MoveState.Running, true,  BuffPct);

                if (d is null)
                {
                    Console.WriteLine($"  {L,4}  {race,-6} {e.EffectiveWit,4} {e.EffectiveSpt,4}  {"(not learned)",-17} {"-",4} {"-",5}  {"-",5}  {"-",5}  {"-",4} {0f,8:0.0} |"
                                    + $" {natSt,7:0.0} {bfSt,7:0.0} {bfRun,7:0.0} | {-bfSt,7:+0.0;-0.0} {e.MaxMp,7}   never");
                    continue;
                }

                int mp = Cost(e, d, lvl);
                var (cast, reuse) = Cycle(e, d, lvl);
                float cycle = cast + reuse;
                float drain = cycle > 0 ? mp / cycle : 0f;

                Console.WriteLine($"  {L,4}  {race,-6} {e.EffectiveWit,4} {e.EffectiveSpt,4}  {Trim(d.Name, 17),-17} {lvl,4} {cast,5:0.0}s {reuse,5:0.0}s {cycle,5:0.0}s {mp,5} {drain,8:0.0} |"
                                + $" {natSt,7:0.0} {bfSt,7:0.0} {bfRun,7:0.0} | {drain - bfSt,7:+0.0;-0.0} {e.MaxMp,7} {Empty(e.MaxMp, drain, bfSt)}");
            }
        }
        Console.WriteLine();
    }

    // ---- 3. THE BUFFER'S TOGGLES -------------------------------------------------------------
    //
    // ⚠ A TOGGLE'S UPKEEP IS NOT A LADDER IN THE ENGINE. BuildStance authors mpPerSec[i] into each
    // SkillLevel's MpCost (the CSV's own convention - *"his MP column carries the same N"*), but
    // TickToggleUpkeep charged `def.MpPerSecond` - the RUNG-1 number - at EVERY rung until 2026-08-27.
    // Both are still printed: 'auth' is what the CSV prices the rung at, 'chg' what the tick takes. They
    // must now MATCH - if they ever diverge again, the ladder has lost its per-rung slot.
    Console.WriteLine("--- 3. BUFFER (Warchanter) - Reinforcement + Sharpening BOTH RUNNING ---");
    Console.WriteLine();
    Console.WriteLine("  A toggle has no cast and no reuse: a FLAT per-second burn for as long as it is lit.");
    Console.WriteLine("  'auth' = the authored rung cost (CSV).  'chg' = what TickToggleUpkeep charges. They must MATCH.");
    Console.WriteLine("  'combat' adds his best spammable damage skill on top - the toggles are not the whole bill.");
    Console.WriteLine();
    Console.WriteLine("     L  race    SPT  rung  Reinf  Sharp   auth/s   chg/s | dmg skill          drain/s | combat/s |  bf.st |   net/s    pool   empty");
    Console.WriteLine("  ------------------------------------------------------------------------------------------------------------------------------------");
    foreach (int L in levels)
    {
        foreach (var race in races)
        {
            var e = BuildCasterFor(race, L, Archetype.Healer, Discipline.Warchanter);
            float bfSt = Mp(e, MoveState.Running, false, BuffPct);

            var reinf = SkillCatalog.Get(SkillCatalog.WcReinforcement);
            var sharp = SkillCatalog.Get(SkillCatalog.WcSharpening);
            int rr = e.SkillLevelOf(SkillCatalog.WcReinforcement);
            int sr = e.SkillLevelOf(SkillCatalog.WcSharpening);

            float auth = 0f, chg = 0f;
            if (reinf != null && rr > 0) { auth += reinf.MpCostAt(rr); chg += reinf.MpPerSecondAt(rr); }
            if (sharp != null && sr > 0) { auth += sharp.MpCostAt(sr); chg += sharp.MpPerSecondAt(sr); }

            // His best spammable DAMAGE skill, any category (the buffer's are physical Sound skills).
            string dmgName = "-"; float dmgDrain = 0f;
            foreach (var (id, sl) in e.LearnedSkills)
            {
                if (SkillCatalog.Get(id) is not SkillDef dd) continue;
                if ((dd.Effect & (SkillEffect.MagicDamage | SkillEffect.PhysicalDamage)) == 0) continue;
                int c = Cost(e, dd, sl);
                if (c <= 0) continue;
                var (ca, re) = Cycle(e, dd, sl);
                if (re > 5f) continue;                      // a burst, not a rotation
                float cy = ca + re;
                if (cy <= 0f) continue;
                float dr = c / cy;
                if (dr > dmgDrain) { dmgDrain = dr; dmgName = dd.Name; }
            }

            float combat = auth + dmgDrain;
            Console.WriteLine($"  {L,4}  {race,-6} {e.EffectiveSpt,4} {(rr > 0 ? rr.ToString() : "-"),5}"
                            + $" {(rr > 0 ? reinf!.MpCostAt(rr).ToString() : "-"),6} {(sr > 0 ? sharp!.MpCostAt(sr).ToString() : "-"),6}"
                            + $" {auth,8:0.0} {chg,7:0.0} | {Trim(dmgName, 17),-17} {dmgDrain,8:0.0} | {combat,8:0.0} | {bfSt,6:0.0} |"
                            + $" {combat - bfSt,7:+0.0;-0.0} {e.MaxMp,7} {Empty(e.MaxMp, combat, bfSt)}");
        }
    }
    Console.WriteLine();
}

/// <summary>A caster of a NAMED discipline, with the class chain gated by level the way the game
/// gates it (2nd at 20, 3rd at 40, the 4th tier's rows arriving by LearnLevel from 76).
/// BuildWarchanter sets the 3rd class unconditionally, which is fine at 40+ and a lie at 20-30 —
/// this report starts at 20, so it needs the gate.</summary>
static Entity BuildCasterFor(Race race, int level, Archetype arch, Discipline disc)
{
    var s = StatCalculator.GetBaseStats(race, BaseClass.Mage);
    var e = new Entity { Name = disc.ToString(), Kind = EntityKind.Player, Race = race, BaseClass = BaseClass.Mage };
    e.Level = level;
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Agi = s.Agi; e.Spt = s.Spt;

    if (level >= 20)
        e.SecondClass = ClassCatalog.Playable.First(c => c.Race == race && c.Archetype == arch).Id;
    if (level >= 40)
        e.ThirdClass = ThirdClassCatalog.Playable.First(c => c.Race == race && c.Discipline == disc).Id;

    foreach (var cs in ClassSkills.ForClass(race, BaseClass.Mage, null, null))
        if (cs.LearnLevel <= level)
            e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
    foreach (var cs in ClassSkills.Cumulative(race, BaseClass.Mage, e.Archetype, e.Discipline))
        if (cs.LearnLevel <= level)
            e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
    foreach (var id in e.LearnedSkills.Keys.ToList())
        if (SkillCatalog.Get(id)?.Replaces is { } replaced)
            foreach (var r in replaced) e.LearnedSkills.Remove(r);
    e.LearnedSkills[SkillCatalog.SpellcasterMastery] = 1;
    GrantFloorPassive(e, level);

    var rune = SkillCatalog.Get(SkillCatalog.SpellRuneBuff);
    if (rune != null)
        e.Buffs.Add(new Game.Server.Simulation.BuffInstance
        {
            Effect = rune.Effect, Magnitudes = rune.Magnitudes,
            TicksRemaining = int.MaxValue, Name = rune.Name, Key = rune.BuffKey,
        });

    // The Warchanter's race split (his own words); every other caster is staff + robe.
    int t = GearTier(level);
    if (disc != Discipline.Warchanter) { Equip(e, $"staff_t{t}"); Equip(e, $"robe_t{t}"); }
    else switch (race)
    {
        case Race.Human: Equip(e, $"blunt1h_t{t}"); Equip(e, $"shield_t{t}"); Equip(e, $"heavy_t{t}"); break;
        case Race.Demon:   Equip(e, $"blunt2h_t{t}"); Equip(e, $"heavy_t{t}"); break;
        default:         Equip(e, $"bow_t{t}");     Equip(e, $"light_t{t}"); break;
    }
    foreach (var acc in new[] { "helm", "gloves", "boots" }) Equip(e, $"{acc}_t{t}");
    Equip(e, $"necklace_t{t}");
    Equip(e, $"ring_t{t}"); Equip(e, $"ring_t{t}");
    Equip(e, $"earring_t{t}"); Equip(e, $"earring_t{t}");

    e.RecomputeDerived();
    return e;
}

// ============================================================================================
//  `--hpregen` — THE HP ECONOMY. `BL-92` part TWO: the half the owner deliberately HELD on
//  2026-08-26 — *"I want to do the same checks for the HP regen as well .. to not over inflate it
//  with multipliers .. but let finish with the MP first"*.
//
//  The MP question was "can a spamming mage pay for himself". The HP question is HIS OWN and it is
//  different: *"the hp regen comes from potions so whatever number it is - its only to save on
//  10-20% potions not more"*. So regen is judged against the two things that actually replace HP:
//      POTION   — the sustained HP/s a potion tier delivers (Common 20 / Uncommon 70 / Rare 150).
//                 'pot%' is regen as a share of the RARE tier: his 10-20% is the target.
//      DAMAGE   — a level-appropriate mob's DPS onto that character. 'dps%' is how much of the
//                 incoming damage regen quietly undoes; at 100% the mob can never kill you.
//
//  ⚠ NOTHING HERE CHANGES THE ENGINE. It mirrors GameLoopService.Regenerate's HP branch exactly,
//  including the two places HP still differs from MP after 0.88.1:
//      1. the flats sit INSIDE the multipliers (MP moved them out),
//      2. the `hpReg x1.1…x2.7` mastery ladder is still a PRODUCT of percents — the exact shape
//         that was measured at x4.84 on the MP side and ended free mana.
// ============================================================================================

static void HpEconomy(int[] argLevels)
{
    // The percent HP-regen buffs a real character carries. Vigor r6 and the Warchanter's harmony are
    // the SAME family — the harmony evicts the single — so it is ONE x1.2, plus the chant's x1.2.
    const float BuffPct = 1.20f * 1.20f;
    const float RarePotion = 150f;   // Rare Healing Potion: 150 HP/s for 30s on a 20s drink cooldown
    const float UncPotion = 70f;     // Uncommon: 70 HP/s for 15s on a 10s cooldown

    int[] levels = argLevels.Length > 0 ? argLevels : new[] { 20, 30, 40, 52, 60, 68, 74, 80, 85 };

    // Mirrors GameLoopService.Regenerate's HP branch EXACTLY. If this drifts, the report lies —
    // which is the one thing it exists not to do.
    //
    // ⚠ THE FLATS ARE OUTSIDE since `BL-92` closed on 2026-08-26. HpRegenBonus now carries the
    // `hpReg` mastery ladder as a flat +1.1…+2.7 HP/s; HpRegenMult keeps only what is genuinely a
    // percent (the armour-SET bonus, the HpRegenPercent gear attribute).
    //
    // ⚠ StatCalculator.HpRegenPerSecond is fed entity.Con — the BASE stat. The MP branch one line
    // below it feeds EffectiveSpt. That asymmetry is real and is reported in section 4.
    static float Hp(Entity e, MoveState st, bool moving, float buffPct)
    {
        float stance = MovementTuning.RegenMultiplier(st, moving);
        return StatCalculator.HpRegenPerSecond(e.EffectiveCon, e.Level) * stance * e.HpRegenMult * buffPct
               + e.HpRegenBonus;
    }

    Console.WriteLine();
    Console.WriteLine("=== THE HP ECONOMY (BL-92 part two) - regen vs potions vs damage taken ===");
    Console.WriteLine();
    Console.WriteLine("  HP/s = ( (3 + 0.1*L) x 1.03^(CON-40) + flats ) x stance x HpRegenMult x (1+buff%)");
    Console.WriteLine("           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ flats INSIDE (MP moved them OUT in 0.88.0)");
    Console.WriteLine("                                                HpRegenMult is still a PRODUCT of hpReg x1.1..x2.7");
    Console.WriteLine("  stance: running 0.70 | walking 0.85 | STANDING STILL 1.00 | sitting 1.50   (shared with MP)");
    Console.WriteLine($"  'bf' columns model the real buff stack at x{BuffPct:0.00} (Vigor-or-harmony x1.2, chant x1.2).");
    Console.WriteLine($"  Potion throughput for scale: Common 20 HP/s | Uncommon {UncPotion:0} HP/s | Rare {RarePotion:0} HP/s.");
    Console.WriteLine();

    // ---- 1. IG vs OURS, IN ABSOLUTE HP/s ------------------------------------------------------
    //
    // His IG reference, given 2026-08-26:
    //     HpRegen = ( base x ConMod x LvlMod + flat ) x buffs
    //     base, without buffs or passives, is PER RACE + CLASS:
    //         Fighters (Human, Demon, Dwarf) 2.5-3.0 | Elven fighters 2.0-2.5
    //         Mages (Human, Elf)           1.5-2.0 | Demon Mystics    2.0-2.2
    //     ConMod anchors: CON 43 -> 1.32, CON 30 -> 1.00.
    //
    // 🔑 OUR formula factors EXACTLY into his shape, which is what makes the comparison honest:
    //     (3 + 0.1*L) x 1.03^(CON-40)  ==  3.00  x  (1 + L/30)  x  1.03^(CON-40)
    //                                      base     LvlMod         ConMod
    // and the differences are then only three numbers, not a change of model:
    //     1. our base is 3.00 for EVERY race and class; IG's splits 1.5 -> 3.0 by race+class,
    //     2. our ConMod is centred on CON 40, IG's on CON 30,
    //     3. our per-point step is 1.03, IG's is 1.0216 (the exact fit through his two anchors).
    Console.WriteLine("--- 1. IG vs OURS, IN HP/s (his reference numbers, 2026-08-26) ---");
    Console.WriteLine();
    Console.WriteLine("  IG:    HpRegen = ( base x ConMod x LvlMod + flat ) x buffs");
    Console.WriteLine("         base is PER RACE+CLASS: fighter 2.5-3.0 | elf fighter 2.0-2.5 | mage 1.5-2.0 | demon mystic 2.0-2.2");
    Console.WriteLine($"         ConMod fitted through HIS anchors CON 30 -> 1.00 and CON 43 -> 1.32, i.e. {IgConStep():0.0000}^(CON-30)");
    Console.WriteLine("  OURS:  (3 + 0.1*L) x 1.03^(CON-40)   ==   3.00 x (1 + L/30) x 1.03^(CON-40)");
    Console.WriteLine("                                            base   LvlMod       ConMod      <- the SAME shape");
    Console.WriteLine("         so only three numbers differ: our base is 3.00 for EVERYONE, our ConMod centres on");
    Console.WriteLine("         CON 40 not 30, and our per-point step is 1.03 not 1.0216.");
    Console.WriteLine();

    Console.WriteLine("  a) THE CON MODIFIER");
    Console.WriteLine();
    Console.WriteLine("     race/class     CON | IG ConMod | OUR ConMod | ours/IG");
    Console.WriteLine("     -----------------------------------------------------");
    foreach (var (race, cls) in RaceClassPairs())
    {
        int con = StatCalculator.GetBaseStats(race, cls).Con;
        float ig = IgConMod(con);
        float our = (float)Math.Pow(StatCalculator.ConRegenBase, con - 40);
        Console.WriteLine($"     {race,-6} {cls,-8} {con,3} | {ig,9:0.000} | {our,10:0.000} | {our / ig,6:0.00}x");
    }
    Console.WriteLine();
    Console.WriteLine("     Ours is the HARSHER curve on mages (centred 10 points higher) and the more generous");
    Console.WriteLine("     one on nobody - every single row is below IG's modifier.");
    Console.WriteLine();

    Console.WriteLine("  b) HP/s AT LEVEL 1 - where LvlMod is ~1 in both, so his base numbers compare DIRECTLY");
    Console.WriteLine();
    Console.WriteLine("     race/class     CON |  IG base   x ConMod =    IG HP/s |  OUR HP/s | verdict");
    Console.WriteLine("     ------------------------------------------------------------------------------------");
    foreach (var (race, cls) in RaceClassPairs())
    {
        int con = StatCalculator.GetBaseStats(race, cls).Con;
        var (lo, hi) = IgBase(race, cls);
        float ig = IgConMod(con);
        float igLo = lo * ig, igHi = hi * ig;
        float our = StatCalculator.HpRegenPerSecond(con, 1);
        string verdict = our < igLo ? $"BELOW IG by {(1 - our / igLo) * 100:0}%"
                       : our > igHi ? $"ABOVE IG by {(our / igHi - 1) * 100:0}%"
                                    : "inside IG's band";
        Console.WriteLine($"     {race,-6} {cls,-8} {con,3} | {lo,4:0.0}-{hi,3:0.0}  x {ig,6:0.000} = {igLo,5:0.00}-{igHi,4:0.00} | {our,9:0.00} | {verdict}");
    }
    Console.WriteLine();

    Console.WriteLine("  c) HP/s BY LEVEL - IG vs OURS, SIDE BY SIDE (natural: no passives, no buffs, standing)");
    Console.WriteLine();
    Console.WriteLine("     IG's LvlMod is L/100 + 0.89 - the SAME expression we already use as the damage lvlMod,");
    Console.WriteLine("     (level+89)/100. It runs x0.90 at L1 to x1.74 at L85: a x1.93 climb across the game.");
    Console.WriteLine("     OUR level term is 1 + L/30, which climbs x3.71 over the same span - nearly DOUBLE.");
    Console.WriteLine("     That single difference is the whole of our divergence; the base and CON are fine.");
    Console.WriteLine();
    Console.Write("     race/class     CON |        |");
    foreach (int L in IgLevels()) Console.Write($"    L{L,-2} |");
    Console.WriteLine();
    Console.Write("     -------------------------------");
    foreach (int _ in IgLevels()) Console.Write("---------");
    Console.WriteLine();
    foreach (var (race, cls) in RaceClassPairs())
    {
        int con = StatCalculator.GetBaseStats(race, cls).Con;
        var (lo, hi) = IgBase(race, cls);
        float igMid = (lo + hi) / 2f * IgConMod(con);

        Console.Write($"     {race,-6} {cls,-8} {con,3} | IG     |");
        foreach (int L in IgLevels()) Console.Write($" {igMid * IgLvlMod(L),6:0.00} |");
        Console.WriteLine();
        Console.Write($"     {"",-19} | OURS   |");
        foreach (int L in IgLevels()) Console.Write($" {StatCalculator.HpRegenPerSecond(con, L),6:0.00} |");
        Console.WriteLine();
        Console.Write($"     {"",-19} | ratio  |");
        foreach (int L in IgLevels())
            Console.Write($" {StatCalculator.HpRegenPerSecond(con, L) / (igMid * IgLvlMod(L)),5:0.00}x |");
        Console.WriteLine();
    }
    Console.WriteLine();
    Console.WriteLine("     We start ON IG at level 1 and end at ~2x IG for fighters and ~2.7x for mages.");
    Console.WriteLine("     Nothing compounds here - it is one linear term outrunning another linear term.");
    Console.WriteLine();

    Console.WriteLine("  d) IF WE SWAP OUR LEVEL TERM FOR IG'S, AND CHANGE NOTHING ELSE");
    Console.WriteLine();
    Console.WriteLine("     i.e.  3.00 x (L+89)/100 x 1.03^(CON-40)   instead of   (3 + 0.1*L) x 1.03^(CON-40)");
    Console.WriteLine("     The ratio to IG then stops moving with level, because the level term is IG's own:");
    Console.WriteLine();
    Console.Write("     race/class     CON |        |");
    foreach (int L in IgLevels()) Console.Write($"    L{L,-2} |");
    Console.WriteLine("  vs IG");
    Console.Write("     -------------------------------");
    foreach (int _ in IgLevels()) Console.Write("---------");
    Console.WriteLine();
    foreach (var (race, cls) in RaceClassPairs())
    {
        int con = StatCalculator.GetBaseStats(race, cls).Con;
        var (lo, hi) = IgBase(race, cls);
        float igMid = (lo + hi) / 2f * IgConMod(con);
        float conMod = (float)Math.Pow(StatCalculator.ConRegenBase, con - 40);

        Console.Write($"     {race,-6} {cls,-8} {con,3} | IG-lvl |");
        foreach (int L in IgLevels()) Console.Write($" {3f * IgLvlMod(L) * conMod,6:0.00} |");
        Console.WriteLine($"  {3f * conMod / igMid,5:0.00}x");
        Console.Write($"     {"",-19} | today  |");
        foreach (int L in IgLevels()) Console.Write($" {StatCalculator.HpRegenPerSecond(con, L),6:0.00} |");
        Console.WriteLine();
    }
    Console.WriteLine();
    Console.WriteLine("     Fighters land 0.90-1.04x IG and mages 1.07-1.24x, at EVERY level - because our single");
    Console.WriteLine("     base of 3.00 cannot reproduce IG's 2x base split between fighter and mage, and our");
    Console.WriteLine("     CON curve only carries the mage down to 62% of the fighter where IG's base puts");
    Console.WriteLine("     him at 45%. Closing THAT gap needs either a race/class base split (IG's own shape)");
    Console.WriteLine("     or a steeper CON step (~1.05 instead of 1.03). The level term is the big rock.");
    Console.WriteLine();
    Console.WriteLine("  ⚠ StatCalculator's own comment says CON 'sits at 36-47'. That is the FIGHTER range only:");
    Console.WriteLine("    mages are 25-31, so the real spread is 25-47 and our exponential spans x1.92 end to end.");
    Console.WriteLine("    IG's spans x1.60 over the same range - a FLATTER curve, off a race/class-split base.");
    Console.WriteLine();

    // ---- 2. THE FLAT LADDER — who actually owns an hpReg rung ---------------------------------
    Console.WriteLine("--- 2. WHO GETS AN hpReg MASTERY AT ALL (a FLAT HP/s since BL-92, at 74) ---");
    Console.WriteLine();
    Console.WriteLine("  class        flat HP/s   x mult   where it comes from");
    foreach (var (name, e, src) in HpSubjects(74))
        Console.WriteLine($"  {name,-12} {e.HpRegenBonus,9:+0.0;-0.0;0.0}   {e.HpRegenMult,6:0.00}   {src}");
    Console.WriteLine();
    Console.WriteLine("  🔴 STILL OPEN (owner, 2026-08-26): the FIGHTER 3rd/4th kits are not authored yet, and");
    Console.WriteLine("     when they are, a fighter's flat must end up HIGHER than a mage's - today the nuker's");
    Console.WriteLine("     +2.7 beats the warrior's +1.6 and the tank has none at all. And the ORK BUFFER should");
    Console.WriteLine("     carry more than the others; how much is NOT decided. Neither is built - do not invent");
    Console.WriteLine("     numbers for either, they arrive with his CSVs.");
    Console.WriteLine();

    // ---- 3. THE LADDER, class by class --------------------------------------------------------
    foreach (var (name, _, _) in HpSubjects(74))
    {
        Console.WriteLine($"--- 3. {name.ToUpperInvariant()}: REGEN vs POOL, MOB DAMAGE AND POTIONS ---");
        Console.WriteLine();
        Console.WriteLine("     L   MaxHP  base/s  flat   mult | nat.stnd  bf.stnd  bf.run   bf.sit | %pool/s  0->full | mobDPS  dps%  pot%");
        Console.WriteLine("  --------------------------------------------------------------------------------------------------------------");
        foreach (int L in levels)
        {
            var subject = HpSubjects(L).First(s => s.Name == name);
            var e = subject.Entity;
            float bas = StatCalculator.HpRegenPerSecond(e.EffectiveCon, e.Level);
            float natStand = Hp(e, MoveState.Running, false, 1f);
            float bfStand = Hp(e, MoveState.Running, false, BuffPct);
            float bfRun = Hp(e, MoveState.Running, true, BuffPct);
            float bfSit = Hp(e, MoveState.Sitting, false, BuffPct);
            float mobDps = Dps(BuildMobEntity(L), e);
            float pool = e.MaxHp > 0 ? bfStand / e.MaxHp : 0f;

            Console.WriteLine($"  {L,4} {e.MaxHp,7} {bas,7:0.0} {e.HpRegenBonus,5:0.0} {e.HpRegenMult,6:0.00} | "
                            + $"{natStand,8:0.0} {bfStand,8:0.0} {bfRun,7:0.0} {bfSit,7:0.0} | "
                            + $"{pool * 100,6:0.00}% {(bfStand > 0 ? e.MaxHp / bfStand : 0),7:0}s | "
                            + $"{mobDps,6:0.0} {(mobDps > 0 ? bfStand / mobDps : 0) * 100,4:0}% {bfStand / RarePotion * 100,4:0}%");
        }
        Console.WriteLine();
    }

    Console.WriteLine("  'dps%'  = buffed-standing regen / one level-appropriate mob's DPS onto you.");
    Console.WriteLine("            Over 100% means natural regen alone outruns that mob - it can never kill you.");
    Console.WriteLine("  'pot%'  = regen as a share of a RARE potion's 150 HP/s. His own framing for why HP regen");
    Console.WriteLine("            is the smaller question: *\"the hp regen comes from potions so whatever number it");
    Console.WriteLine("            is - its only to save on 10-20% potions not more\"*. After BL-92 it reads 5-12%");
    Console.WriteLine("            at 60+, i.e. slightly UNDER the share he described. IG's own sits at 5-7%.");
    Console.WriteLine();

    // ---- 4. WHAT IS STILL OPEN --------------------------------------------------------------
    Console.WriteLine("--- 4. STILL OPEN AFTER BL-92 (not built, not ruled) ---");
    Console.WriteLine();
    Console.WriteLine("  A. CON is read as the BASE stat, SPT as the EFFECTIVE one.");
    Console.WriteLine("     GameLoopService.Regenerate feeds HpRegenPerSecond(entity.Con) and, one line below,");
    Console.WriteLine("     MpRegenPerSecond(entity.EffectiveSpt). So a +CON buff/jewel raises your MAX HP and");
    Console.WriteLine("     changes NOTHING about your regen, while +SPT raises both. One of the two is wrong.");
    Console.WriteLine();
    Console.WriteLine("  B. The LEVEL TERM. Ours is 1 + L/30 (x3.71 across 1-85); IG's is L/100 + 0.89 (x1.93),");
    Console.WriteLine("     which is the same (level+89)/100 our DAMAGE formula already runs on. Swapping it was");
    Console.WriteLine("     measured and explicitly NOT taken - owner, 2026-08-26: *\"Leave out lvl mod just leave");
    Console.WriteLine("     the flat outside ... So we will have x2 more than IG but not as much as we have now");
    Console.WriteLine("     ... Playtest will decide if it stays\"*. Section 5 prices exactly what that x2 is.");
    Console.WriteLine();
    Console.WriteLine("  C. No FLAT HP-regen source exists outside the masteries: the gear attribute");
    Console.WriteLine("     AttributeType.HpRegen is defined but nothing in the tiered tables rolls it, and no");
    Console.WriteLine("     HP-regen buff is authored in Flat mode. The flats-last rule is set; the channel is");
    Console.WriteLine("     otherwise empty, so gear could use it whenever he wants it used.");
    Console.WriteLine();
    // ---- 5. THE BUILT MODEL, AGAINST IG -------------------------------------------------------
    //
    // Owner, 2026-08-26, closing the HP half of `BL-92`:
    //     *"I want to make the passives + not x as the mp .. and buffs to carry the multiplier ..
    //       and the flat is to added at the end"*, and on the level term:
    //     *"Leave out lvl mod just leave the flat outside ... So we will have x2 more than IG but
    //       not as much as we have now ... Playtest will decide if it stays"*.
    //
    //     BUILT:     [ base x ConMod x LvlMod x stance x (1+buff%) ]  +  masteryFlat + gearFlat
    //     IG's own:  ( base x ConMod x LvlMod + flat ) x buffs
    //
    // The two differ in TWO places, both deliberate: IG multiplies its flat by the buffs where we add
    // ours last (the flats-last house rule, playtest 28), and IG's LvlMod is flatter than ours. The
    // second is the whole of the ~2x he accepted, and it is the thing a playtest is meant to judge.
    Console.WriteLine("--- 5. THE BUILT MODEL vs IG (passives FLAT, buffs MULTIPLY, flats LAST) ---");
    Console.WriteLine();
    Console.WriteLine("  BUILT:     [ base x ConMod x LvlMod x stance x (1+buff%) ] + masteryFlat + gearFlat");
    Console.WriteLine("  IG's own:  ( base x ConMod x LvlMod + flat ) x buffs      <- IG multiplies its flat by buffs");
    Console.WriteLine("             we add ours last, by the flats-last house rule. ~1.2 HP/s at a x1.44 stack.");
    Console.WriteLine();
    Console.WriteLine("  'hpReg +2.7' is read as +2.7 HP/s - the whole rung, not its excess, exactly as mpReg is.");
    Console.WriteLine("  All columns are BUFFED and STANDING, with the class's own mastery rung at that level.");
    Console.WriteLine("  'was' = the pre-BL-92 multiplier form, for the record.");
    Console.WriteLine();
    Console.WriteLine("     class      L | mFlat |    was |  BUILT | if IG lvlMod |  IG's own | built/IG");
    Console.WriteLine("  ------------------------------------------------------------------------------");
    foreach (int L in new[] { 40, 52, 60, 68, 74, 85 })
    {
        foreach (var (name, e, _) in HpSubjects(L))
        {
            int con = e.EffectiveCon;
            float conMod = (float)Math.Pow(StatCalculator.ConRegenBase, con - 40);
            float mFlat = e.HpRegenBonus;
            float bas = StatCalculator.HpRegenPerSecond(con, L);

            // The multiplier form this replaced: the rung read as x(1+flat-1) = x(the rung itself).
            float wasMult = mFlat > 0f ? mFlat : 1f;
            float was = bas * wasMult * BuffPct;

            float built = Hp(e, MoveState.Running, false, BuffPct);
            float igLvl = 3f * IgLvlMod(L) * conMod * BuffPct + mFlat;

            var (lo, hi) = IgBase(e.Race, e.BaseClass);
            float igOwn = ((lo + hi) / 2f * IgConMod(con) * IgLvlMod(L) + mFlat) * BuffPct;

            Console.WriteLine($"     {name,-9} {L,2} | {mFlat,5:0.0} | {was,6:0.0} | {built,6:0.0} | {igLvl,12:0.0} | {igOwn,9:0.0} |"
                            + $" {built / igOwn,8:0.00}x");
        }
        Console.WriteLine();
    }
    Console.WriteLine("  READ IT LIKE THIS:");
    Console.WriteLine("   - 'was' -> 'BUILT' is what the ruling bought. The mastery stops multiplying the level");
    Console.WriteLine("     term, so the nuker's x2.7 collapses to a +2.7 and the class order INVERTS BACK: at 74");
    Console.WriteLine("     it becomes warrior > rogue > tank > nuker, which is IG's own intent (a mage's base");
    Console.WriteLine("     regen is half a fighter's). Before it, the nuker held the highest regen in the game.");
    Console.WriteLine("   - 'built/IG' is the ~2x he accepted with his eyes open. It comes entirely from the LEVEL");
    Console.WriteLine("     term, not from the masteries: 1 + L/30 climbs nearly twice as fast as IG's LvlMod.");
    Console.WriteLine("     'if IG lvlMod' is the column that would close it - measured, offered, and DEFERRED to");
    Console.WriteLine("     a playtest. Do not take it without a new ruling.");
    Console.WriteLine("   ⚠ The price of the flats is that the ladder stops being progression: a nuker's six rungs");
    Console.WriteLine("     from +1.1 to +2.7 used to buy +19 HP/s and now buy +1.6 across 34 levels. Same trade");
    Console.WriteLine("     the mpReg ladder took. If he wants those rungs felt, the FLAT numbers get re-authored");
    Console.WriteLine("     bigger in the CSVs - it is not an engine change.");
    Console.WriteLine();
    Console.WriteLine();
}

// ----- IG's HP-REGEN REFERENCE, exactly as the owner supplied it on 2026-08-26 ----------------
//
//     HpRegen = ( base x ConMod x LvlMod + flat ) x buffs
//
// Only two of those four are quoted in his message, so only two are encoded here. IG's LvlMod is
// NOT given and is deliberately NOT invented — section 1c prints the LvlMod our own numbers imply
// instead, which is a thing he can check against IG rather than a thing we made up.

/// <summary>IG's per-point CON step, fitted EXACTLY through his two anchors (CON 30 -> 1.00,
/// CON 43 -> 1.32): 13 stat points across x1.32, so the step is 1.32^(1/13) = 1.0216. Ours is 1.03,
/// a steeper curve, and centred ten points higher.</summary>
static float IgConStep() => (float)Math.Pow(1.32, 1.0 / 13.0);

static float IgConMod(int con) => (float)Math.Pow(1.32, (con - 30) / 13.0);

/// <summary>IG's HP-regen LEVEL modifier, his own expression (owner, 2026-08-26):
/// <c>Level/100 + 0.89</c>. 🔑 That is character-for-character the lvlMod our DAMAGE formula already
/// runs on — <c>(level+89)/100</c>, see StatCalculator's PhysicalK/MagicK path — so adopting it for
/// regen removes a bespoke curve rather than adding one. It climbs x0.90 -> x1.74 across 1-85, where
/// our own <c>1 + L/30</c> climbs x1.03 -> x3.83. That gap IS our divergence from IG.</summary>
static float IgLvlMod(int level) => level / 100f + 0.89f;

/// <summary>IG's base HP/s band, before buffs and passives, per race+class — his four rows. Our
/// Dwarf/Dark-Elf-less roster maps straight onto them: Demon fighters read with the Human fighters,
/// and the Demon mage is his "Demon Mystic" row, which is the one mage band that is raised.</summary>
static (float Lo, float Hi) IgBase(Race race, BaseClass cls) =>
    cls == BaseClass.Fighter
        ? race == Race.Elf ? (2.0f, 2.5f) : (2.5f, 3.0f)
        : race == Race.Demon ? (2.0f, 2.2f) : (1.5f, 2.0f);

static (Race, BaseClass)[] RaceClassPairs() => new[]
{
    (Race.Human, BaseClass.Fighter), (Race.Elf, BaseClass.Fighter), (Race.Demon, BaseClass.Fighter),
    (Race.Human, BaseClass.Mage),    (Race.Elf, BaseClass.Mage),    (Race.Demon, BaseClass.Mage),
};

static int[] IgLevels() => new[] { 1, 20, 40, 60, 74, 85 };

/// <summary>The four characters the HP economy is measured on, each with the class chain that
/// actually carries (or fails to carry) an `hpReg` ladder today.</summary>
static (string Name, Entity Entity, string Source)[] HpSubjects(int level) => new[]
{
    ("nuker",   BuildNuker(Race.Human, level),                                    "Spellcaster Weapon Mastery hpReg x1.1->x2.7 (40-74)"),
    ("warrior", BuildPlayer(Race.Human, BaseClass.Fighter, level, warrior: true), "Body Mastery hpReg x1.1->x1.6, DONE at 32 and never grows"),
    ("tank",    BuildPlayer(Race.Human, BaseClass.Fighter, level),                "Body Mastery only - the class that TAKES the hits has no ladder"),
    ("rogue",   BuildRogue(level),                                                "Armor Mastery hpReg x1.2 @36 (rogue 2nd.csv line 7)"),
};


// ============================================================================================
//  `--buffs` — THE BUFF CENSUS (playtest 27: "we need make max buffs limit ... Tell me how much
//  buffs we have how many harmonies").
//
//  Two questions, answered from the catalog rather than by counting squares on a screenshot:
//    1. How many SQUARES does a fully-buffed character actually carry? Replays what /fullbuff
//       hands out through the real BuffPlan + conflict rules, so groups evict the singles they
//       cover exactly as the server does, and prints what survives.
//    2. What is the CATALOG made of — groups, harmonies, class singles, NPC singles, potion and
//       scroll rungs, self buffs, toggles — so the limit can be set against a real denominator.
// ============================================================================================

static void Stacks()
{
    Console.WriteLine();
    Console.WriteLine("=== STACK CAPS — every item, by the category that decides it ===");
    Console.WriteLine();
    Console.WriteLine($"  bag {GameConstants.InventorySize} rows | private warehouse {GameConstants.WarehouseSize}"
                      + $" | account warehouse {GameConstants.AccountWarehouseSize}");
    Console.WriteLine();

    var groups = ItemCatalog.AllItems
        .GroupBy(d => (d.IsStackable, d.MaxStack, Bucket(d)))
        .OrderByDescending(g => g.Key.IsStackable).ThenBy(g => g.Key.MaxStack)
        .ToList();

    Console.WriteLine("   cap        items  category                    examples");
    Console.WriteLine("  ---------------------------------------------------------------------------------------");
    foreach (var g in groups)
    {
        string cap = !g.Key.IsStackable ? "—" : g.Key.MaxStack >= StackLimits.Uncapped ? "uncapped"
                                                                                       : g.Key.MaxStack.ToString("N0");
        string examples = string.Join(", ", g.Take(3).Select(d => d.Name));
        if (examples.Length > 46) examples = examples.Substring(0, 43) + "...";
        Console.WriteLine($"   {cap,-9} {g.Count(),6}  {g.Key.Item3,-26}  {examples}");
    }

    Console.WriteLine();
    Console.WriteLine("--- WHAT A TRIP COSTS IN ROWS (the argument the caps were settled on) ---");
    Console.WriteLine();
    Console.WriteLine("   consumable                 per hour   1h    4h    8h   24h    of the 250-row bag (8h)");
    Console.WriteLine("  ---------------------------------------------------------------------------------------");

    void Trip(string label, int perHour, int cap)
    {
        int Rows(int hours) => (perHour * hours + cap - 1) / cap;
        Console.WriteLine($"   {label,-26} {perHour,8}  {Rows(1),4}  {Rows(4),4}  {Rows(8),4}  {Rows(24),4}"
                          + $"    {Rows(8) * 100f / GameConstants.InventorySize,20:0.0}%");
    }

    // 120 drinks/hour is the 30s reuse run flat out; 17 blessings is one of every family, each 1 hour.
    Trip("MP potions (120/h)",   120, StackLimits.VitalPotion);
    Trip("+ HP potions (120/h)", 120, StackLimits.VitalPotion);
    Trip("buff scrolls (17/h)",   17, StackLimits.BuffScroll);

    Console.WriteLine();
    Console.WriteLine("  🔑 THIS IS WHY THE TWO CAPS ARE NOT THE SAME NUMBER. Potions drain as loot fills the bag");
    Console.WriteLine("  behind them, so their row count PEAKS AT HOUR ZERO and falls; even a whole day of drinking");
    Console.WriteLine("  is a rounding error against 250 rows, which is why 999 is a sanity bound and GOLD is what");
    Console.WriteLine("  actually prices them. A fully-buffed player's 17 scrolls/hour is FLAT — it does not fall as");
    Console.WriteLine("  he farms — so at 9 a stack the pile is visible within a session and keeps growing. That is");
    Console.WriteLine("  the only cap here that a player will ever feel, and it is the one he meant to be felt.");
    Console.WriteLine();

    static string Bucket(ItemDef d) =>
        !d.IsStackable                  ? "gear / per-instance"
        : d.Slot == EquipSlot.QuestItem ? "quest item"
        : d.Slot == EquipSlot.Material  ? "material"
        : d.Slot == EquipSlot.Box       ? "box / blueprint"
        : ItemCatalog.IsBuffScroll(d)   ? "buff scroll"
        : d.Slot == EquipSlot.Scroll    ? "enchant / attribute scroll"
        : d.PotionCooldownTicks > 0     ? "HP / MP potion"
        : "buff potion / other";
}
// ============================================================================================
//  `--goldflow` — `BL-23`: WHAT AN HOUR OF FARMING EARNS AGAINST WHAT IT BURNS.
//
//  His own re-spec of the coin curve, 2026-08-27: *"i want potion/rune per hour consumation and
//  golddrop/h .. to compare for few lvl rangees - for now at lvl 43 i have 5kk + gold so it dont
//  seem like a problem"*.
//
//  🔑 THE POINT OF THE REPORT IS THE RATIO, NOT THE COIN. `BL-23` had been an assertion — "gear
//  value follows the tier ladder while coin stays linear, so the gap drifts to 51x by 76" — and an
//  assertion is exactly what this tool exists to replace. A drift only matters if it shows up in
//  what an hour of play can BUY, so that is what is measured: income per hour against the two
//  standing costs a farming character actually pays.
//
//  Everything is read from the live catalogs and the live formulas:
//    * income  — the real drop tables x the real vendor sell prices (`KillValue`, the same helper
//                the ECONOMY section runs on) x kills per hour.
//    * kills/h — TTK from the real damage formulas against the real roster mob of that level,
//                plus one PullSeconds of walking to the next one. That constant is the ONLY
//                invented number in the report and it is named, not buried.
//    * HP burn — the deficit E4 measures per kill (mob DPS x TTK, less regen over the same
//                seconds), priced at the cheapest potion tier that can SUSTAIN it.
//    * MP burn — the same, for the rotation.
//    * rune    — the 1h/2h box straight off the Apothecary shelf, per hour.
//
//  ⚠ A POTION TIER HAS A CEILING, NOT JUST A PRICE. Healing Common/Uncommon are 15s on a 10s
//  cooldown, so they are always up: 20 and 70 HP/s sustained. Rare is 30s on 20s: 150 HP/s. Every
//  MANA potion is 15s on a 30s cooldown — HALF uptime — so the mana ladder sustains 10/35/75 MP/s
//  against its 20/70/150 label. Pricing a deficit at a tier that cannot physically deliver it is
//  the mistake this table is built to not make.
// ============================================================================================

static void GoldFlow()
{
    // The one modelled constant: seconds between kills that are NOT spent fighting — walking to the
    // next mob, waiting on a respawn, looting. Farm loops here are short-TTK, so this term is a real
    // share of the hour and moving it moves every row. It is named rather than buried for that reason.
    const float PullSeconds = 5f;

    Console.WriteLine();
    Console.WriteLine("=== BL-23: GOLD PER HOUR vs POTION AND RUNE BURN ===");
    Console.WriteLine();
    Console.WriteLine($"  live rates: exp x{RateConfig.World.Exp:0.##}  gold x{RateConfig.World.Gold:0.##}"
                    + $"  dropChance x{RateConfig.World.DropChance:0.##}  dropAmount x{RateConfig.World.DropAmount:0.##}");
    Console.WriteLine($"  every character is NPC-BUFFED (the way he plays); {PullSeconds:0}s of pull/travel between kills.");
    Console.WriteLine();

    // ---- The consumable shelf, once, so the per-row arithmetic below is checkable by hand. ----
    var healTiers = new[]
    {
        PotionTier(ItemCatalog.MinorPotion,   SkillCatalog.PotHealMinor,   SkillEffect.HealOverTime),
        PotionTier(ItemCatalog.HealingPotion, SkillCatalog.PotHeal,        SkillEffect.HealOverTime),
        PotionTier(ItemCatalog.GreaterPotion, SkillCatalog.PotHealGreater, SkillEffect.HealOverTime),
    };
    var manaTiers = new[]
    {
        PotionTier(ItemCatalog.MinorManaPotion,   SkillCatalog.PotManaMinor,   SkillEffect.RestoreMp),
        PotionTier(ItemCatalog.ManaPotion,        SkillCatalog.PotMana,        SkillEffect.RestoreMp),
        PotionTier(ItemCatalog.GreaterManaPotion, SkillCatalog.PotManaGreater, SkillEffect.RestoreMp),
    };

    Console.WriteLine("--- THE SHELF: what a point of HP or MP costs, and the ceiling each tier can hold ---");
    Console.WriteLine();
    Console.WriteLine($"  {"potion",-24} {"rate",7} {"lasts",6} {"cd",5} {"per drink",10} {"buy",8} {"gold/pt",8} {"sustains",9}");
    Console.WriteLine("  ---------------------------------------------------------------------------------------------");
    foreach (var t in healTiers.Concat(manaTiers))
        Console.WriteLine($"  {t.Name,-24} {t.PerSecond,5:0}/s {t.Seconds,5:0}s {t.CooldownSeconds,4:0}s "
                        + $"{t.PerDrink,10:N0} {t.Buy,8:N0} {t.GoldPerPoint,8:0.000} {t.Sustained,7:0}/s");
    Console.WriteLine();
    Console.WriteLine("  'sustains' = rate x min(1, duration/cooldown) — the most this tier can deliver forever.");
    Console.WriteLine("  🔑 Every MANA tier is 15s on a 30s drink cooldown, so its ceiling is HALF its label.");
    Console.WriteLine("  🔑 Mana costs ~2x healing per point because mana potions DO NOT DROP — the price pays for");
    Console.WriteLine("     the missing faucet (his ruling, 0.92.0), not for potency.");
    Console.WriteLine();

    // The rune shelf: upkeep is the box price divided by the hours it runs.
    Console.WriteLine("--- THE SHELF: rune upkeep per hour (Apothecary boxes; 24h/30d are premium, not buyable) ---");
    Console.WriteLine();
    foreach (var id in new[] { ItemCatalog.BoxWarRune1h, ItemCatalog.BoxWarRune2h,
                               ItemCatalog.BoxSpellRune1h, ItemCatalog.BoxSpellRune2h })
    {
        if (ItemCatalog.Get(id) is not ItemDef box) continue;
        float hours = box.GrantsRuneSeconds / 3600f;
        long buy = ItemCatalog.BuyPrice(box);
        Console.WriteLine($"  {box.Name,-24} {hours,4:0.#}h  buy {buy,10:N0}  =  {buy / Math.Max(0.01f, hours),10:N0} gold/hour");
    }
    Console.WriteLine();

    // ---- The main table: one row per (level, kit). --------------------------------------------
    int[] bands = { 20, 30, 40, 43, 52, 61, 76, 85 };

    Console.WriteLine("--- AN HOUR OF FARMING, BAND BY BAND ---");
    Console.WriteLine();
    Console.WriteLine($"  {"Lvl",3} {"who",-20} {"TTK",6} {"kills/h",8} {"gold/h",11} {"exp/h",13} |"
                    + $" {"HP def/h",9} {"HP potions",16} {"MP def/h",9} {"MP potions",16} | {"burn/h",11} {"NET/h",11} {"burn%",6}");
    Console.WriteLine("  ------------------------------------------------------------------------------------------------------------------------------------------------");
    foreach (int L in bands)
    {
        var roster = FarmRosterBuffed(L);
        var mob = roster[0].E;
        var near = MobsNear(L);
        double perKill = near.Select(m => KillValue(m, L)).Average(x => x.Gear + x.Mats + x.Consumables + x.Gold);
        double mobExp = StatCalculator.MobExpReward(L) * RateConfig.World.Exp;

        foreach (var (name, e) in roster.Skip(1))
        {
            float phys = PhysDps(e, mob), magic = MagicDps(e, mob);
            bool caster = magic > phys;
            float dps = Math.Max(0.01f, Math.Max(phys, magic));
            float ttk = mob.MaxHp / dps;
            float loop = ttk + PullSeconds;
            float killsPerHour = 3600f / loop;

            // HP: what the mob lands over the FIGHT, less what regenerates over the WHOLE loop — the
            // pull seconds regenerate too, which is exactly why a short-TTK farm is cheaper to run
            // than its damage-taken column alone suggests.
            float hpLost = Dps(mob, e) * ttk;
            float hpRegen = (StatCalculator.HpRegenPerSecond(e.EffectiveCon, e.Level) + e.HpRegenBonus)
                            * e.HpRegenMult * loop;
            float hpDeficitHour = Math.Max(0f, hpLost - hpRegen) * killsPerHour;

            // MP: the rotation's drain over the fight, less regen over the loop.
            var (skill, sl) = TopSkill(e, caster ? SkillEffect.MagicDamage : SkillEffect.PhysicalDamage);
            float mpPerKill = 0f;
            if (skill is not null)
            {
                float cycle = Math.Max(0.1f, SkillCycleSeconds(e, skill));
                mpPerKill = ttk / cycle * skill.MpCostAt(sl);
            }
            float mpPct = e.Buffs.Where(b => b.Has(SkillEffect.BuffMpRegen)).Sum(b => b.Percent(SkillEffect.BuffMpRegen));
            float mpRegen = (StatCalculator.MpRegenPerSecond(e.EffectiveSpt, e.Level) + e.MpRegenBonus)
                            * e.MpRegenMult * (1f + mpPct) * loop;
            float mpDeficitHour = Math.Max(0f, mpPerKill - mpRegen) * killsPerHour;

            var hpBuy = CheapestThatSustains(healTiers, hpDeficitHour / 3600f);
            var mpBuy = CheapestThatSustains(manaTiers, mpDeficitHour / 3600f);

            double goldHour = perKill * killsPerHour;
            double burn = hpBuy.CostPerHour + mpBuy.CostPerHour;

            Console.WriteLine($"  {L,3} {name,-20} {ttk,5:F1}s {killsPerHour,8:N0} {goldHour,11:N0} {mobExp * killsPerHour,13:N0} | "
                + $"{hpDeficitHour,9:N0} {hpBuy.Label,16} {mpDeficitHour,9:N0} {mpBuy.Label,16} | "
                + $"{burn,11:N0} {goldHour - burn,11:N0} {(goldHour > 0 ? burn / goldHour : 0),6:P0}");
        }
        Console.WriteLine();
    }

    Console.WriteLine("  'HP/MP potions' = the cheapest tier whose SUSTAINED rate can actually cover that deficit, and");
    Console.WriteLine("           how many drinks an hour it takes. '--' means regen already covers it: no potion at all.");
    Console.WriteLine("  'burn%' is potions ONLY. A rune is a separate, flat decision — add its line from the shelf");
    Console.WriteLine("           above (150,000/h for a 1h box, 140,000/h for the 2h) to any row you want it on.");
    Console.WriteLine();

    // ---- The question BL-23 was actually opened about. -----------------------------------------
    Console.WriteLine("--- WHAT AN HOUR BUYS: the coin curve against the gear ladder (the BL-23 claim itself) ---");
    Console.WriteLine();
    Console.WriteLine("  If coin is linear while gear value follows the tier ladder, an hour of farming buys LESS");
    Console.WriteLine("  gear the higher you go. That is the drift, and this is it in one column.");
    Console.WriteLine();
    Console.WriteLine($"  {"Lvl",3} {"gold/h (champ)",15} {"tier",5} {"cheapest body",14} {"rarity",10} {"bodies/h",9} {"rune hours/h",13}");
    Console.WriteLine("  --------------------------------------------------------------------------------------");
    foreach (int L in bands)
    {
        var roster = FarmRosterBuffed(L);
        var mob = roster[0].E;
        var champ = roster.First(r => r.Name.StartsWith("champion")).E;
        float dps = Math.Max(0.01f, Math.Max(PhysDps(champ, mob), MagicDps(champ, mob)));
        float killsPerHour = 3600f / (mob.MaxHp / dps + PullSeconds);
        var near = MobsNear(L);
        double goldHour = near.Select(m => KillValue(m, L)).Average(x => x.Gear + x.Mats + x.Consumables + x.Gold)
                          * killsPerHour;

        int tier = GearTier(L);
        // ⚠ NOT hardwired to "_common": S grade is TOP HALF ONLY (Epic/Legendary/Mythic — a level-80
        // piece has no Common rung at all, ItemCatalog.IsTopHalfOnly). Asking for one printed a 0 and
        // read as "free", so the column takes the CHEAPEST body that actually exists at that tier and
        // names its rarity. The ladder shifting under you is part of what the drift IS.
        var bodyRungs = new[] { "_common", "_uncommon", "_rare", "_epic", "_legendary", "" }
            .Select(sfx => ItemCatalog.Get($"heavy_t{tier}{sfx}"))
            .Where(d => d is not null && ItemCatalog.BuyPrice(d) > 0)
            .OrderBy(d => ItemCatalog.BuyPrice(d!))
            .ToList();
        long bodyPrice = bodyRungs.Count > 0 ? ItemCatalog.BuyPrice(bodyRungs[0]!) : 0;
        string bodyRarity = bodyRungs.Count > 0 ? bodyRungs[0]!.Rarity.ToString() : "none";
        long runeHour = ItemCatalog.Get(ItemCatalog.BoxWarRune1h) is ItemDef rb ? ItemCatalog.BuyPrice(rb) : 0;

        Console.WriteLine($"  {L,3} {goldHour,15:N0} {tier,5} {bodyPrice,14:N0} "
            + $"{bodyRarity,10} {(bodyPrice > 0 ? goldHour / bodyPrice : 0),9:F2} {(runeHour > 0 ? goldHour / runeHour : 0),13:F2}");
    }
    Console.WriteLine();
    Console.WriteLine("  'bodies/h'     = cheapest chest pieces of the tier you would be wearing, per hour of farm.");
    Console.WriteLine("                   If this column FALLS with level, BL-23's drift is real and measurable.");
    Console.WriteLine("  'rune hours/h' = how many hours of War Rune one hour of farming pays for. Below 1.00 the");
    Console.WriteLine("                   rune costs more than the farm it enables, which is the sharper test.");
    Console.WriteLine();
    Console.WriteLine("  ⚠ HIS OWN DATA POINT, 2026-08-27: *\"for now at lvl 43 i have 5kk + gold so it dont seem");
    Console.WriteLine("    like a problem\"*. Read the 43 row against that before touching a rate.");
    Console.WriteLine();
}

/// <summary>One rung of a potion ladder, read off the ITEM and the SKILL it names — never retyped.
/// <para><c>Sustained</c> is the rate it can hold forever: a 15s potion on a 30s drink cooldown
/// delivers half its label, which is what separates the mana ladder from the healing one.</para></summary>
static (string Name, float PerSecond, float Seconds, float CooldownSeconds, float PerDrink,
        long Buy, float GoldPerPoint, float Sustained)
    PotionTier(string itemId, string skillId, SkillEffect channel)
{
    var item = ItemCatalog.Get(itemId);
    var skill = SkillCatalog.Get(skillId);
    if (item is null || skill is null) return ("MISSING " + itemId, 0, 0, 0, 0, 0, 0, 0);

    float perSec = skill.Magnitudes is null ? 0f
                 : skill.Magnitudes.Where(m => m.Effect == channel).Select(m => m.Value).FirstOrDefault();
    float secs = skill.DurationTicks / 10f;
    float cd = item.PotionCooldownTicks / 10f;
    float perDrink = perSec * secs;
    long buy = ItemCatalog.BuyPrice(item);
    return (item.Name, perSec, secs, cd, perDrink, buy,
            perDrink > 0 ? buy / perDrink : 0f,
            perSec * Math.Min(1f, cd <= 0 ? 1f : secs / cd));
}

/// <summary>The cheapest rung that can actually SUSTAIN this deficit, and what an hour of it costs.
/// <para>Cheapest-per-point is not enough on its own: a tier that cannot deliver the RATE is not an
/// option at any price, which is why the ceiling is checked before the price is.</para></summary>
static (string Label, double CostPerHour) CheapestThatSustains(
    (string Name, float PerSecond, float Seconds, float CooldownSeconds, float PerDrink,
     long Buy, float GoldPerPoint, float Sustained)[] tiers, float pointsPerSecond)
{
    if (pointsPerSecond <= 0.01f) return ("--", 0);

    var able = tiers.Where(t => t.Sustained >= pointsPerSecond && t.PerDrink > 0)
                    .OrderBy(t => t.GoldPerPoint).ToList();
    if (able.Count == 0)
    {
        // Nothing on the shelf can hold this rate. Price the BEST tier anyway and SAY SO — a row that
        // silently printed 0 would read as "free" when it actually means "cannot be sustained at all".
        var best = tiers.OrderByDescending(t => t.Sustained).First();
        double drinksCap = 3600.0 / Math.Max(1f, best.CooldownSeconds);
        return ("OVER CAP", drinksCap * best.Buy);
    }

    var pick = able[0];
    double drinks = pointsPerSecond * 3600.0 / pick.PerDrink;
    string rarity = pick.Name.Split(' ')[0];   // Common / Uncommon / Rare
    return ($"{drinks:N0}x {rarity}", drinks * pick.Buy);
}
static class BuffCensus
{
    public static void Run()
    {
        Console.WriteLine();
        Console.WriteLine("=== 1. THE FULL BAR — what /fullbuff (AdminBuffSet) actually leaves standing ===");
        Console.WriteLine("  Applied in the set's own order, through GameLoopService.BuffPlan + the family");
        Console.WriteLine("  conflict rule. 'EVICTED' = a group covering that family landed first.");
        Console.WriteLine();

        // key -> (label, covered families). Mirrors ApplyBuff's bookkeeping.
        var bar = new List<(string Key, string Label, string[] Covered, int Rank, string Kind, bool Slot)>();
        var evicted = new List<string>();

        foreach (var id in SkillCatalog.AdminBuffSet)
        {
            if (SkillCatalog.Get(id) is not SkillDef def) continue;
            int lvl = def.MaxLevel;
            var (key, rank, covered, _) = GameLoopService.BuffPlan(def, lvl);
            string kind = Classify(def, lvl);

            int hit = bar.FindIndex(b => Conflicts(b.Key, b.Covered, key, covered));
            if (hit >= 0)
            {
                if (rank > bar[hit].Rank) { evicted.Add($"{bar[hit].Label} (out-ranked)"); bar[hit] = (key, def.Name, covered, rank, kind, SlotLabel(def) == "SLOT"); }
                else evicted.Add($"{def.Name} [{kind}] — covered by {bar[hit].Label}");
                continue;
            }
            bar.Add((key, def.Name, covered, rank, kind, SlotLabel(def) == "SLOT"));
        }

        foreach (var kindGroup in bar.GroupBy(b => b.Kind).OrderBy(g => Order(g.Key)))
        {
            Console.WriteLine($"  {kindGroup.Key.ToUpperInvariant()} ({kindGroup.Count()})");
            foreach (var b in kindGroup)
                Console.WriteLine($"      {b.Label,-34} key={b.Key,-24} rank={b.Rank,-4} {(b.Slot ? "SLOT" : "  - ")}");
        }
        Console.WriteLine();
        int slots = bar.Count(b => b.Slot);
        Console.WriteLine($"  >>> SQUARES ON THE BAR: {bar.Count}   (refused/evicted along the way: {evicted.Count})");
        Console.WriteLine($"  >>> OF WHICH COUNT AGAINST THE CAP: {slots} / {GameConstants.MaxBuffSlots}" +
                          $"   — {GameConstants.MaxBuffSlots - slots} free");
        Console.WriteLine();
        foreach (var kindGroup in bar.GroupBy(b => b.Kind).OrderBy(g => Order(g.Key)))
            Console.WriteLine($"      {kindGroup.Key,-22} {kindGroup.Count(),3}");

        Console.WriteLine();
        Console.WriteLine("=== 2. THE CATALOG — every timed buff that exists, by what it is ===");
        Console.WriteLine();
        var all = SkillCatalog.AllSkills
            .Where(s => (s.Effect & SkillEffect.AnyBuff) != 0 || s.Category == SkillCategory.Buff)
            .Where(s => s.DurationTicks > 0 || s.Toggle)
            .ToList();

        foreach (var g in all.GroupBy(s => Classify(s, s.MaxLevel)).OrderBy(g => Order(g.Key)))
        {
            Console.WriteLine($"  {g.Key,-22} {g.Count(),3} skills   {DistinctFamilies(g),3} distinct families/keys");
            foreach (var s in g.OrderBy(s => s.Name))
                Console.WriteLine($"      {s.Name,-34} {(s.MaxLevel > 1 ? $"Lv1-{s.MaxLevel}" : "     "),-8} {DurLabel(s),9} {SlotLabel(s)}  {s.Id}");
            Console.WriteLine();
        }
        Console.WriteLine($"  TOTAL timed-buff skills in the catalog: {all.Count}");
    }

    /// <summary>Exactly the engine rule (GameLoopService.CountsAgainstBuffCap), so the census cannot
    /// claim a buff costs a slot that the server would let through free.</summary>
    private static string SlotLabel(SkillDef d)
    {
        // Resolve a ONE-CHILD wrapper to its child, exactly as ApplyBuff does — a Dash potion never
        // lands, `buff_dash_*` does, and it is the child that carries the flag.
        var landing = d;
        if (d.ChildBuffsAt(d.MaxLevel) is { Length: 1 } kid && SkillCatalog.Get(kid[0]) is SkillDef c)
            landing = c;
        return landing.CountsTowardBuffLimit && !landing.Toggle
            && (d.BuffRow is BuffRow.Buff or BuffRow.Consumable) ? "SLOT" : "  - ";
    }

    private static string DurLabel(SkillDef d)
    {
        if (d.Toggle) return "toggle";
        int t = d.DurationTicks;
        // Duration is per-SkillDef only (no per-level override exists today).
        if (t <= 0) return "-";
        int s = t / 10;
        return s >= 60 ? $"{s / 60}m{(s % 60 == 0 ? "" : $"{s % 60}s")}" : $"{s}s";
    }

    private static int DistinctFamilies(IEnumerable<SkillDef> defs) =>
        defs.Select(d => GameLoopService.BuffPlan(d, d.MaxLevel).Key).Distinct().Count();

    private static bool Conflicts(string keyA, string[] covA, string keyB, string[] covB) =>
        keyA == keyB || covA.Contains(keyB) || covB.Contains(keyA) || covA.Intersect(covB).Any();

    private static int Order(string kind) => kind switch
    {
        "harmony" => 0, "group" => 1, "class single" => 2, "npc single" => 3,
        "self" => 4, "toggle" => 5, "potion/scroll" => 6, _ => 7
    };

    private static string Classify(SkillDef d, int level)
    {
        if (d.Toggle) return "toggle";
        var kids = d.ChildBuffsAt(level);
        bool harmony = d.Name.Contains("Harmony", StringComparison.OrdinalIgnoreCase)
                    || d.Id.Contains("harmony", StringComparison.OrdinalIgnoreCase);
        if (harmony) return "harmony";
        if (kids is { Length: > 1 }) return "group";
        if (d.BuffRow == BuffRow.Consumable) return "potion/scroll";
        if (d.Id.StartsWith("npc_", StringComparison.Ordinal)) return "npc single";
        if (d.TargetMode == TargetMode.SelfOnly) return "self";
        return "class single";
    }
}


// ============================================================================================
//  `--stacks` — WHAT EVERY ITEM STACKS TO, read off the catalog (0.93.0).
//
//  It exists because the caps are DERIVED from an item's category, never authored per row, so the
//  only way to know a def landed in the right bucket is to ask it. A misclassified item is silent
//  otherwise: nothing errors, the scroll simply stacks to 99 instead of 9 and the mechanic he asked
//  for quietly does nothing.
//
//  The second half answers the question the caps were argued over: how many ROWS does a real trip
//  cost? That is the number that decides whether a cap is a mechanic or decoration, and it is worth
//  printing next to the bag size rather than reasoning about.
// ============================================================================================

