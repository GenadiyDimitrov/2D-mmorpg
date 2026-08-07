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
Console.WriteLine("=== HIT / MISS vs a SAME-LEVEL mob (accuracy = DEX + level, 1 point = 1%) ===");
Console.WriteLine("  'naked' = base stats only, no gear and no passives. 'geared' = best gear for tier + kit.");
Console.WriteLine("  A same-DEX, same-level pair must sit at the 5% base BOTH ways — that is the whole point of");
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
Console.WriteLine("=== MAGIC CRIT (0.50.1 rework: base 50 x witMod x buffs x passives + flat) ===");
Console.WriteLine("  Rate is WIT-only — no weapon term. Crit damage is a FLAT x3 and takes no buff:");
Console.WriteLine($"  Ferocity and the crit-damage attribute are the PHYSICAL channel now. Cap {StatCaps.MagicCritRate:P0}.");
Console.WriteLine();
Console.WriteLine($"  {"WIT",4} {"witMod",7} {"base",6} {"x1.2 Res",9} {"x2 Insight",11} {"both",7}  who");
foreach ((int wit, string who) in new[]
{
    (5,  "every MOB (flat WIT 5 at all levels)"),
    (10, "ork fighter"),
    (15, "human fighter"),
    (19, "ork mage, bare"),
    (20, "HUMAN MAGE, bare  <- the x1.00 anchor"),
    (23, "elf mage, bare"),
    (26, "ork mage + set +2 + swap +5"),
    (27, "human mage + set +2 + swap +5"),
    (30, "ELF MAGE + set +2 + swap +5  <- tests the cap"),
})
{
    float witMod = StatCalculator.CritWitMod(wit);
    float b = StatCalculator.MagicCritBase(wit);
    // The chain as RecomputeDerived folds it: base x every multiplier, then the single clamp.
    float res = Math.Min(b * 1.2f, StatCaps.MagicCritRate);
    float ins = Math.Min(b * 2.0f, StatCaps.MagicCritRate);
    float both = Math.Min(b * 2.0f * 1.2f, StatCaps.MagicCritRate);
    Console.WriteLine($"  {wit,4} {witMod,7:F2} {b,6:P1} {res,9:P1} {ins,11:P1} {both,7:P1}  {who}");
}
Console.WriteLine();
Console.WriteLine("  MEASURED off real Entities (level 74, best gear) — the chain, not the formula:");
Console.WriteLine($"  {"race",6} {"WIT",4} {"unbuffed",9} {"+Insight x2",12}   (no swap/attribute: BuildPlayer has neither)");
foreach (Race r in new[] { Race.Human, Race.Elf, Race.Ork })
{
    var bare = BuildPlayer(r, BaseClass.Mage, 74);
    var buffed = BuildPlayer(r, BaseClass.Mage, 74);
    ApplyOneBuff(buffed, SkillCatalog.NpcInsight);
    Console.WriteLine($"  {r,6} {(int)bare.EffectiveWit,4} {bare.MagicCritChance,9:P2} {buffed.MagicCritChance,12:P2}");
}
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
//  L2-style power number is tempered rather than explosive — 7600 is not the outlier it looks like next
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
    float mCritF = CritFactor(nuker.MagicCritChance, StatCalculator.MagicCritMult());

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
        e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Dex = s.Dex;
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
// scale 20; "new" ramps scale = min(level, 20) so low levels read close to L2 (a lvl-1 wand mage
// showed ~72 where L2 shows ~8). DAMAGE is untouched (it uses the internal value, printed too).
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
    foreach (var e in applicable.Where(e => e.GroupId == 0))
        yield return (e, Math.Min(1f, MobCatalog.EffectiveChance(e)));
    foreach (var g in applicable.Where(e => e.GroupId != 0).GroupBy(e => e.GroupId))
    {
        // Weights are the PER-ITEM-TUNED chances (MobCatalog.ItemWeight), matching RollDrop exactly —
        // this tool's whole job is to be the same arithmetic, so it reads the same two helpers.
        float sum = g.Sum(MobCatalog.ItemWeight);
        float trigger = Math.Min(1f, g.Sum(MobCatalog.EffectiveChance));
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
        double qty = (e.MinQty + e.MaxQty) / 2.0 * RateConfig.DropAmountRate;
        double value = chance * qty * ItemCatalog.SellPrice(def);
        items += chance;
        if (MobCatalog.IsGearGroup(e.GroupId)) gear += value;
        else if (e.GroupId == MobCatalog.GroupMats || def.Id.StartsWith("mat_")) mats += value;
        else cons += value;
    }
    return (gear, mats, cons, StatCalculator.MobGoldReward(mobLevel) * RateConfig.GoldAmountRate, items);
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
Console.WriteLine($"  live ExpRate = x{RateConfig.ExpRate:0.##}, DropChanceRate = x{RateConfig.DropChanceRate:0.##}"
    + $" (gear groups x{RateConfig.DropGroupRate("armor"):0.##}; mats/scrolls/always are EXEMPT from the global rate)");
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
    double kLive = kX1 / Math.Max(0.01f, RateConfig.ExpRate);
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
        double kills = next / (double)exp / Math.Max(0.01f, RateConfig.ExpRate);
        sold += kills * MobsNear(L).Select(m => KillValue(m, L))
            .Average(x => x.Gear + x.Mats + x.Consumables + x.Gold);
    }
    Console.WriteLine($"{mul,8:0.##} {mul * RateConfig.DropChanceRate,10:0.##}x {sold,14:N0} "
        + $"{sold / 400_000.0,16:0.00}x");
}
for (int i = 0; i < saved.Length; i++)
    RateConfig.DropGroupRates[new[] { "armor", "accessory", "weapon", "jewel" }[i]] = saved[i];
Console.WriteLine("  'effective' = the global DropChanceRate x this multiplier — what a gear group really rolls at.");
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
        .Select(g => (Mob: m.Id, Group: g.Key, Sum: g.Sum(MobCatalog.EffectiveChance))))
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
    // Class #0: a Human Mage who became a Sorcerer (Nuker) and then a Tempest.
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

    Console.WriteLine("  …but its 3rd-class DISCIPLINES bar the one already owned:");
    foreach (var tc in ThirdClassCatalog.ForParent(18))
        Console.WriteLine(c.CanTakeThirdClass(tc.Id)
            ? $"    OK      {tc.Name,-14} ({tc.Discipline})"
            : $"    BARRED  {tc.Name,-14} ({tc.Discipline}) — that discipline is already taken");
}
Console.WriteLine();

Console.WriteLine("=== STAT-SWAP DIRECTION RULE ===");

// The owner's worked example: a fighter takes +ATK-MEN, then +WIT-MEN. Every other swap should
// then be banned except the +CON-DEX / +DEX-CON pair, and MEN should stack to -10.
var held = new List<string> { SkillCatalog.SwapAtkMen, SkillCatalog.SwapWitMen };
Console.WriteLine($"  Fighter holds: {string.Join(" + ", held.Select(NameOf))}");
foreach (var id in SkillCatalog.StatSwapsFor(BaseClass.Fighter, null))
{
    if (held.Contains(id)) continue;
    string? clash = SkillCatalog.StatSwapConflict(id, held);
    Console.WriteLine(clash is null
        ? $"    OPEN   {NameOf(id),-22} ({id})"
        : $"    banned {NameOf(id),-22} — {clash}");
}

// The net-zero ring the rule exists to kill: +CON-DEX, +DEX-ATK, +ATK-CON nets to +0 for 45kk.
Console.WriteLine();
Console.WriteLine("  Net-zero ring (+CON-DEX, +DEX-ATK, +ATK-CON) — must be unreachable:");
var ring = new[] { SkillCatalog.SwapConDex, SkillCatalog.SwapDexAtk, SkillCatalog.SwapAtkCon };
var ringHeld = new List<string>();
foreach (var id in ring)
{
    string? clash = SkillCatalog.StatSwapConflict(id, ringHeld);
    Console.WriteLine(clash is null ? $"    taken  {NameOf(id)}" : $"    BLOCKED {NameOf(id)} — {clash}");
    if (clash is null) ringHeld.Add(id);
}

Console.WriteLine();
Console.WriteLine("  (debug \"learn all skills\" now grants NO swaps — a swap is a permanent build");
Console.WriteLine("   choice, and the greedy legal pick lands on four -ATK swaps = -20 ATK.)");
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
foreach (int L in g3Levels)
{
    var player = BuildPlayer(Race.Human, BaseClass.Fighter, L, warrior: true);

    var opponents = new List<(string Label, Entity E)> { ("today's mob (Kind=Mob)", BuildMobEntity(L)) };
    foreach (var arch in g3Archs)
        opponents.Add(($"mob-player {arch}", BuildMobPlayer(L, arch, tierDrop: 1, ItemRarity.Common, 0, kit: true)));

    foreach (var (label, opp) in opponents)
    {
        float pDps = Dps(player, opp);
        float oDps = Dps(opp, player);
        Console.WriteLine($"{L,4} {label,22} {opp.MaxHp,8} {pDps,8:F0} " +
            $"{opp.MaxHp / Math.Max(0.01f, pDps),7:F1}s | {oDps,8:F0} " +
            $"{player.MaxHp / Math.Max(0.01f, oDps),9:F1}s " +
            $"{Pct(Miss(player, opp)),10} {Pct(Miss(opp, player)),10}");
    }
    Console.WriteLine();
}

// -----------------------------------------------------------------------------------------------
// 5. THE DIVERGENCES that flipping Kind would cause, each one MEASURED rather than asserted. These
//    are the things "just make it a player" changes by side effect — the reason this is an audit and
//    not a one-line flag flip.
// -----------------------------------------------------------------------------------------------
Console.WriteLine("=== G3.5: what flipping Kind actually changes (measured side effects) ===");
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
    Console.WriteLine($"  swing interval   Kind=Mob {oldSwing:F2}s -> Kind=Player {newSwing:F2}s "
        + $"(x{oldSwing / newSwing:0.00} swings/sec, unauthored)");

    // (b) THE NEUTRAL-OPPONENT BENCHMARK. Mob DEX is flat 30 on purpose (owner 2026-08-02) so a
    //     same-level pair sits at the 5% floor both ways, and MobDexReference IS the human-fighter
    //     base — so a fighter-shaped mob-player inherits the same number by construction. The mage
    //     archetypes are the ones to watch: their class base DEX is not the reference.
    var refPlayer = BuildPlayer(Race.Human, BaseClass.Fighter, L, warrior: true);
    Console.WriteLine($"  DEX benchmark    MobStats DEX is flat {StatCalculator.MobDexReference} "
        + $"(Kind=Mob: acc {oldMob.Accuracy} eva {(int)oldMob.EffectiveEvasion}); a geared player misses it "
        + $"{Pct(Miss(refPlayer, oldMob))}");
    foreach (var arch in g3Archs)
    {
        var a = BuildMobPlayer(L, arch, 1, ItemRarity.Common, 0, kit: false);
        Console.WriteLine($"                     {arch,-8} DEX {a.Dex,3} acc {a.Accuracy,4} eva {(int)a.EffectiveEvasion,4}"
            + $"  player misses it {Pct(Miss(refPlayer, a)),4}, it misses the player {Pct(Miss(a, refPlayer)),4}"
            + $"{(a.Dex == StatCalculator.MobDexReference ? "" : "   << OFF THE BENCHMARK")}");
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
        + $"classMod {StatCalculator.HpClassLevelModifier(BaseClass.Fighter, Archetype.Warrior):0.00}) = {newMob.MaxHp}");
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

Console.WriteLine("=== G3: VERDICT INPUTS (read the tables, not this line) ===");
Console.WriteLine("  * NO gear combination closes all three gaps at once (G3.2): the player pipeline is the");
Console.WriteLine("    MIRROR of the mob curve — armor over-delivers P.Def/M.Def while the weapon under-delivers");
Console.WriteLine("    attack by 2-5x, at every level and every grade. Today's mob is a glass cannon; a");
Console.WriteLine("    player-shaped entity of the same level is the opposite. Gear cannot flip that sign.");
Console.WriteLine("  * so the TYPE PASSIVES carry the reconciliation, not the gear — and G3.6 says the");
Console.WriteLine("    multipliers they must supply DRIFT with level, so each passive needs a per-band table.");
Console.WriteLine("    That is his 'levelled passive with a name per level' — the design already assumes it.");
Console.WriteLine("  * HP is the cleanest case: archetype + level alone lands within x0.96-x1.16 for Tank and");
Console.WriteLine("    Warrior. Rogue (x1.4) and especially Nuker (x3.3) need real HP passives.");
Console.WriteLine("  * a FROZEN per-template loadout rots across zone bands (G3.3) — a level->grade function is");
Console.WriteLine("    mandatory, not optional, if 'the zone assigns the level' survives.");
Console.WriteLine("  * the FIGHTS are already playable (G3.4): mob-player TTKs land 4-16s against today's 2-16s.");
Console.WriteLine("    Their damage OUT is the weak side — at L80 a mob-player deals 13-33 dps where today's mob");
Console.WriteLine("    deals 46, which is the same attack gap seen in G3.1/G3.2 showing up in the fight.");
Console.WriteLine("  * flipping Kind moves the SWING CLOCK by side effect (G3.5a). The DEX benchmark survives for");
Console.WriteLine("    fighter archetypes by construction — MobDexReference IS the human-fighter base.");
Console.WriteLine();

// =====================================================================================================
//  C1 — CRIT DAMAGE (flat), BLOWS and [Double]                    docs/design/CritBlowAndDouble.md
//
//  Three changes measured against what they replaced, because all three moved at once (2026-08-05):
//    * crit damage "+80" in the CSVs is FLAT ATTACK added inside the ratio on a crit, not "x2.8";
//    * a landed BLOW is now computed WITH the crit-damage values (it used to return base damage,
//      so a dagger's whole crit-damage ladder did nothing at all);
//    * [Double] chance is a pure ATK curve capped 25%, not max(DEX,ATK)/1000 capped 30%.
//  OLD columns re-create the previous arithmetic here so the magnitude of the swing is visible.
// =====================================================================================================
Console.WriteLine("=== C1: [Double] chance — ATK curve (new) vs max(DEX,ATK)/1000 cap 30% (old) ===");
{
    int[] atks = { 30, 35, 40, 45, 50, 55, 60, 70 };
    Console.Write("  ATK stat ");   foreach (int a in atks) Console.Write($"{a,8}");
    Console.WriteLine();
    Console.Write("  new      ");
    foreach (int a in atks) Console.Write($"{StatCalculator.PhysicalDoubleChance(a) * 100,7:F1}%");
    Console.WriteLine();
    Console.Write("  old      ");
    foreach (int a in atks) Console.Write($"{Math.Clamp(a * 0.001f, 0f, 0.30f) * 100,7:F1}%");
    Console.WriteLine("     (old ALSO read DEX, so a rogue sat far higher than this row)");
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
            // OLD: a landed blow returned base damage untouched, then doubled off max(DEX,ATK).
            float oldDbl = Math.Clamp(Math.Max(r.EffectiveDex, r.AtkStat) * 0.001f, 0f, 0.30f);
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
                ? CritFactor(Math.Clamp(Math.Max(w.EffectiveDex, w.AtkStat) * 0.001f, 0f, 0.30f), 2f)
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

Console.WriteLine("=== C2: CRIT RATE — his L2 model, decomposed (docs/design/CritBlowAndDouble.md §5) ===");
{
    Console.WriteLine("  crit = (110 x weaponFactor x dexMod  x  passives x buffs  +  flat) x debuffs x enemyLightArmor");
    Console.WriteLine("  numbers on HIS 0-1000 scale (1000 = 100%), cap 500. mult = every passive AND buff folded.");
    Console.WriteLine("  build                     lvl | DEX dexMod | weapon    base | mult  | flat | FINAL      %");
    void CritRow(string label, int lvl, Entity e)
    {
        int dex = (int)e.EffectiveDex;
        Console.WriteLine($"  {label,-25} {lvl,3} | {dex,3} x{StatCalculator.CritDexMod(dex),4:F2} |"
            + $" {e.WeaponType.Base(),-8} {StatCalculator.PhysicalCritBase(dex, e.WeaponType) * 1000f,4:F0} |"
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
    Console.WriteLine("   - flat is 0 on every row: our only flat crit-rate source is a RANDOM WEAPON ATTRIBUTE");
    Console.WriteLine("     (sword/dual/bow only). His model's flat 'heavy set +127' — the term that is supposed");
    Console.WriteLine("     to carry the BLUNT warrior, who cannot multiply his way anywhere — does not exist yet.");
    Console.WriteLine("   - DEX is 30 on every row because DEX is per RACE+BASE CLASS: only an ELF fighter (35)");
    Console.WriteLine("     moves it, and no armor set in these tiers carries a Dex line. See the elf row below.");
    // The one build that actually exercises dexMod today.
    var elf = BuildRogue(36);
    elf.Dex = StatCalculator.GetBaseStats(Race.Elf, BaseClass.Fighter).Dex;
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
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Dex = s.Dex; e.Spt = s.Spt;
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
                                      int enchant, bool kit = false)
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
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Dex = s.Dex; e.Spt = s.Spt;
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
    string weapon = caster ? "staff" : arch == Archetype.Warrior ? "sword2h" : "sword1h";

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
    float critF = CritFactor(atk.MagicCritChance, StatCalculator.MagicCritMult());
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
        if (def.RequiredWeapon != WeaponType.None && (def.RequiredWeapon & e.WeaponType) == 0) continue;
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
static Entity BuildPlayer(Race race, BaseClass cls, int level, string? quality = null, bool warrior = false)
{
    var s = StatCalculator.GetBaseStats(race, cls);
    var e = new Entity { Name = "calc", Kind = EntityKind.Player };
    e.Race = race;
    e.BaseClass = cls;
    e.Level = level;
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Dex = s.Dex;

    // Second class at 20 (Human Sorcerer / Human Knight) so the archetype kits apply.
    // 18 = Sorcerer (nuker), 13 = Knight (tank), 14 = Champion (warrior).
    if (level >= 20) e.SecondClass = cls == BaseClass.Mage ? 18 : warrior ? 14 : 13;

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
    // tuned curve is unchanged for a runed player; a rune-LESS player is ~half offence (intended, L2).
    var shot = SkillCatalog.Get(cls == BaseClass.Mage ? SkillCatalog.SpellRuneBuff : SkillCatalog.WarRuneBuff);
    if (shot != null)
        e.Buffs.Add(new Game.Server.Simulation.BuffInstance
        {
            Effect = shot.Effect, Magnitudes = shot.Magnitudes,
            TicksRemaining = int.MaxValue, Name = shot.Name, Key = shot.BuffKey,
        });
    if (cls == BaseClass.Mage)
        e.LearnedSkills[SkillCatalog.MasteryRobe] = 1;

    // QUALITY suffix. The tiered tables are authored as the EPIC piece, and the six-quality ladder
    // (0.29.1) derives everything else from it — so the bare id IS the Epic, and "_mythic" is the new
    // 100% ceiling at 1/0.7 ≈ +43%. Passing a quality here is what lets the matrix MEASURE that raise
    // instead of asserting it.
    int t = GearTier(level);
    string q = quality is null or "epic" ? "" : "_" + quality;
    Equip(e, (cls == BaseClass.Mage ? $"staff_t{t}" : $"sword1h_t{t}") + q);
    Equip(e, (cls == BaseClass.Mage ? $"robe_t{t}" : $"heavy_t{t}") + q);
    if (cls == BaseClass.Fighter) Equip(e, $"shield_t{t}{q}");
    foreach (var acc in new[] { "helm", "gloves", "boots" }) Equip(e, $"{acc}_t{t}{q}");
    Equip(e, $"necklace_t{t}{q}");
    Equip(e, $"ring_t{t}{q}"); Equip(e, $"ring_t{t}{q}");
    Equip(e, $"earring_t{t}{q}"); Equip(e, $"earring_t{t}{q}");

    e.RecomputeDerived();
    return e;
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
static Entity BuildMobEntity(int level)
{
    var s = StatCalculator.MobStats(level);
    var e = new Entity { Name = "mob", Kind = EntityKind.Mob, Level = level };
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Dex = s.Dex; e.Spt = s.Spt;
    e.RecomputeDerived();
    return e;
}

/// <summary>Base stats and NOTHING else — no gear, no learned skills, so no passive floors.
/// This is the row that shows what the FORMULA does before the character sheet touches it.</summary>
static Entity BuildNaked(Race race, BaseClass cls, int level)
{
    var s = StatCalculator.GetBaseStats(race, cls);
    var e = new Entity { Name = "naked", Kind = EntityKind.Player, Race = race, BaseClass = cls, Level = level };
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Dex = s.Dex; e.Spt = s.Spt;
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
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Dex = s.Dex;

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
