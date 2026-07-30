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
    float critF = CritFactor(champ.CritChance, StatCalculator.PhysicalCritMult(champ.CritDamageBonus));
    float dblF  = CritFactor(StatCalculator.PhysicalDoubleChance(Math.Max(champ.Dex, champ.AtkStat)), 2f);

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
    float mCritF = CritFactor(nuker.MagicCritChance, StatCalculator.MagicCritMult(nuker.CritDamageBonus));

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
// LOW LEVEL (1-10): a REAL new player — TRAINING gear, NO shots — vs same-level mob HP. This is the
// band the device playtest flagged ("lvl-1 one-shots a lvl 4-8 mob"). BuildPlayer floors to level-20
// gear + shots, so it can't show this; BuildStarter equips the training kit and no shot buff.
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
        yield return (e, Math.Min(1f, e.Chance * RateConfig.DropChanceRate));
    foreach (var g in applicable.Where(e => e.GroupId != 0).GroupBy(e => e.GroupId))
    {
        float sum = g.Sum(e => e.Chance);
        float trigger = Math.Min(1f, sum * RateConfig.DropChanceRate);
        foreach (var e in g)
            yield return (e, sum <= 0 ? 0 : trigger * (e.Chance / sum));
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
Console.WriteLine($"  live ExpRate = x{RateConfig.ExpRate:0.##}, DropChanceRate = x{RateConfig.DropChanceRate:0.##}");
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
        .Select(g => (Mob: m.Id, Group: g.Key, Sum: g.Sum(d => d.Chance) * RateConfig.DropChanceRate)))
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

static string NameOf(string id) => SkillCatalog.Get(id)?.Name ?? id;

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
    foreach (var id in SkillCatalog.NewbieBuffSet)
        if (SkillCatalog.Get(id) is SkillDef def)
            e.Buffs.Add(new Game.Server.Simulation.BuffInstance
            {
                Effect = def.Effect,
                Magnitudes = def.MagnitudesAt(1) ?? Array.Empty<EffectMagnitude>(),
                TicksRemaining = int.MaxValue, Name = def.Name, Key = def.BuffKey, Level = 1,
            });
    e.RecomputeDerived();
}

/// <summary>Expected damage multiplier from crit: 1 + chance × (mult − 1).</summary>
static float CritFactor(float chance, float mult) => 1f + chance * (mult - 1f);

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

    // Shots (2026-07-24): the old training passive is gone — soul/spiritshots are now held RUNE items that
    // grant this buff. Apply it directly here so the matrix reflects the EXPECTED play state (shots ON).
    // Its numbers are identical to the old max passive (+100% P.Atk / +41% eff. M.Atk / +40 cast), so the
    // tuned curve is unchanged for a shotted player; a shot-LESS player is ~half offence (intended, L2).
    var shot = SkillCatalog.Get(cls == BaseClass.Mage ? SkillCatalog.SpiritshotRuneBuff : SkillCatalog.SoulshotRuneBuff);
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

// A REAL low-level player: TRAINING gear (the level 1-10 kit), NO shot rune buff, learned skills up to
// this level. This is what a new character actually fights with — unlike BuildPlayer, which floors to
// level-20 gear + shots and so hides the low-level one-shot the playtest found.
static Entity BuildStarter(BaseClass cls, int level)
{
    var s = StatCalculator.GetBaseStats(Race.Human, cls);
    var e = new Entity { Name = "starter", Kind = EntityKind.Player, Race = Race.Human, BaseClass = cls, Level = level };
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Dex = s.Dex;

    foreach (var cs in ClassSkills.Cumulative(Race.Human, cls, e.Archetype, e.Discipline))
        if (cs.LearnLevel <= level)
            e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
    if (cls == BaseClass.Mage) e.LearnedSkills[SkillCatalog.MasteryRobe] = 1;

    // Training kit only — no shots, no jewels (jewels are earned; the point is the FLOOR gear).
    Equip(e, cls == BaseClass.Mage ? ItemCatalog.TrainingWand : ItemCatalog.TrainingSword);
    Equip(e, cls == BaseClass.Mage ? ItemCatalog.TrainingRobe : ItemCatalog.TrainingLeather);

    e.RecomputeDerived();
    return e;
}
