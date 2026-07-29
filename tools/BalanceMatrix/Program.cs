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
    int cAtk = (int)champ.EffectiveAttack;
    float critF = CritFactor(champ.CritChance, StatCalculator.PhysicalCritMult(champ.CritDamageBonus));
    float dblF  = CritFactor(StatCalculator.PhysicalDoubleChance(Math.Max(champ.Dex, champ.AtkStat)), 2f);

    int crushHit = StatCalculator.PhysicalDamage(cAtk, refPower, mobPDef, refLevel);
    float crushCycle = (refCastTicks + refReuseTicks) * GameConstants.TickSeconds;
    float crushDps = crushHit * critF * dblF / crushCycle;

    int autoHit = StatCalculator.PhysicalDamage(cAtk, 0, mobPDef, refLevel);
    float autoEvery = AutoAttackSeconds(champ);
    // Autoattacks only fill the time the cast is NOT occupying.
    float autoShare = (crushCycle - refCastTicks * GameConstants.TickSeconds) / crushCycle;
    float autoDps = autoHit * critF / autoEvery * autoShare;

    Console.WriteLine($"  CHAMPION  P.Atk {cAtk}  crit x{critF:F2}  double x{dblF:F2}");
    Console.WriteLine($"    Heavenly Crush  {crushHit,6} dmg / {crushCycle:F1}s  = {crushDps,7:F1} dps");
    Console.WriteLine($"    autoattack      {autoHit,6} dmg / {autoEvery:F2}s   = {autoDps,7:F1} dps");
    Console.WriteLine($"    TOTAL                                    = {crushDps + autoDps,7:F1} dps"
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
    int mAtk = (int)nuker.EffectiveMagicAttack;
    float mCritF = CritFactor(nuker.MagicCritChance, StatCalculator.MagicCritMult(nuker.CritDamageBonus));

    int nukeHit = StatCalculator.MagicDamage(mAtk, nukePower, mobMDef, refLevel);
    // Cast time scales with CAST speed exactly as SkillReuseTicks does; reuse does not.
    float nukeCycle = (Math.Max(2, (int)(nukeCastTicks * nuker.EffectiveCastSpeedMultiplier)) + nukeReuseTicks)
                      * GameConstants.TickSeconds;
    float nukeDps = nukeHit * mCritF / nukeCycle;

    Console.WriteLine($"  NUKER     M.Atk {mAtk}  magic crit x{mCritF:F2}");
    Console.WriteLine($"    top nuke (power {nukePower})  {nukeHit,6} dmg / {nukeCycle:F1}s  = {nukeDps,7:F1} dps"
                      + $"   ({mobHp / Math.Max(1f, nukeDps):F1}s to kill)");
    Console.WriteLine($"    -> CHAMPION/NUKER = {(crushDps + autoDps) / Math.Max(1f, nukeDps):F2}x");
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
