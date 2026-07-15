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

int[] levels = { 20, 40, 52, 61, 76, 85 };

Console.WriteLine();
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
Console.WriteLine($"{"Lvl",4} {"P.Atk",7} {"MaxHP",7} {"P.Def",7} {"M.Def",7} | {"basic",7} {"mobHP",7} {"hits",6}");

foreach (int L in levels)
{
    var f = BuildPlayer(Race.Human, BaseClass.Fighter, L);
    int pAtk = (int)f.EffectiveAttack;
    int mobPDef = MobBaseStats.PDef(L);
    int mobHp = MobBaseStats.Hp(L);
    int hit = StatCalculator.PhysicalDamage(pAtk, 0, mobPDef, L);
    float hits = hit > 0 ? mobHp / (float)hit : 0;

    Console.WriteLine($"{L,4} {pAtk,7} {f.MaxHp,7} {(int)f.EffectiveDefence,7} {(int)f.EffectiveMagicDefence,7} | " +
                      $"{hit,7} {mobHp,7} {hits,6:F1}");
}
Console.WriteLine();
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

Console.WriteLine("=== PROGRESSION (x1 rates; a NORMAL x1-toughness mob) ===");
Console.WriteLine($"{"Lvl",4} {"exp/kill",9} {"expToNext",10} {"mobs/level",11}");
foreach (int L in levels)
{
    int exp = StatCalculator.MobExpReward(L);
    long next = StatCalculator.ExpToNext(L);
    Console.WriteLine($"{L,4} {exp,9} {next,10} {next / (float)exp,11:F0}");
}
Console.WriteLine("  (a mob that buys bulk with an HP-multiplier passive now pays that multiple in EXP)");
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
    level >= 76 ? 76 : level >= 61 ? 61 : level >= 52 ? 52 : level >= 40 ? 40 : 20;

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

// A character at `level` with every skill their class table offers by then, wearing the
// full best-for-tier gear line (weapon + body + accessories + 5 jewels + shield for fighters).
static Entity BuildPlayer(Race race, BaseClass cls, int level)
{
    var s = StatCalculator.GetBaseStats(race, cls);
    var e = new Entity { Name = "calc", Kind = EntityKind.Player };
    e.Race = race;
    e.BaseClass = cls;
    e.Level = level;
    e.Con = s.Con; e.AtkStat = s.Atk; e.Wit = s.Wit; e.Dex = s.Dex;

    // Second class at 20 (Human Sorcerer / Human Knight) so the archetype kits apply.
    if (level >= 20) e.SecondClass = cls == BaseClass.Mage ? 18 : 13;

    // Every skill the class table teaches by this level, at the highest level learnable.
    foreach (var cs in ClassSkills.Cumulative(race, cls, e.Archetype, e.Discipline))
    {
        if (cs.LearnLevel > level) continue;
        e.LearnedSkills[cs.SkillId] = Math.Max(e.SkillLevelOf(cs.SkillId), cs.SkillLevel);
    }

    int train = StatCalculator.TrainingLevelFor(level);
    if (train > 0)
        e.LearnedSkills[cls == BaseClass.Mage ? SkillCatalog.SpiritTraining : SkillCatalog.PhysicalTraining] = train;
    if (cls == BaseClass.Mage)
        e.LearnedSkills[SkillCatalog.MasteryRobe] = 1;

    int t = GearTier(level);
    Equip(e, cls == BaseClass.Mage ? $"staff_t{t}" : $"sword1h_t{t}");
    Equip(e, cls == BaseClass.Mage ? $"robe_t{t}" : $"heavy_t{t}");
    if (cls == BaseClass.Fighter) Equip(e, $"shield_t{t}");
    foreach (var acc in new[] { "helm", "gloves", "boots" }) Equip(e, $"{acc}_t{t}");
    Equip(e, $"necklace_t{t}");
    Equip(e, $"ring_t{t}"); Equip(e, $"ring_t{t}");
    Equip(e, $"earring_t{t}"); Equip(e, $"earring_t{t}");

    e.RecomputeDerived();
    return e;
}

static void Equip(Entity e, string defId)
{
    if (ItemCatalog.Get(defId) is null) { Console.Error.WriteLine($"  !! missing item {defId}"); return; }
    e.Inventory.Add(new InventoryItem { DefId = defId, Equipped = true });
}
