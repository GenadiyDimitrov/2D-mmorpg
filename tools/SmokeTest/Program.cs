using Game.Shared;
using Microsoft.AspNetCore.SignalR.Client;

// Headless end-to-end smoke test.
//
// A real SignalR client speaking the real protocol, with no window. It exists because the failures
// most likely to be lurking here LOOK CORRECT in the running client while being wrong on the server:
// the skill-bar corruption found by review would have rendered perfectly in-game and only surfaced as
// a mangled bar on the NEXT login. A human playtest cannot reliably catch that. This can.
//
// Requires a server already listening on :5238 (and a DB it may write to).

// TWO MODES, one project. `bot` is a live second PLAYER that stays logged in and takes orders; the
// default is this assert-and-exit smoke test. They share the connect/login/enter plumbing, which is
// the only part that was ever hard to get right.
if (args.Length > 0 && (args[0] == "bot" || args[0] == "--bot"))
    return await Bot.RunAsync(args.Skip(1).ToArray());

const string Url = "http://localhost:5238/game";

int failures = 0;

void Check(string what, bool ok, string? detail = null)
{
    Console.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
    Console.Write(ok ? "  PASS  " : "  FAIL  ");
    Console.ResetColor();
    Console.WriteLine(detail is null ? what : $"{what}  ({detail})");
    if (!ok) failures++;
}

// ---- One connection = one "client". Relogging means a brand-new connection, which is the whole
//      point: it proves the state came back from the DATABASE and not from memory.
async Task<Session> ConnectAsync(string user, string pass)
{
    var s = new Session();
    await s.OpenAsync(Url);
    var auth = await s.Hub.InvokeAsync<AuthResponse>("Login",
        new AuthRequest(user, pass, GameConstants.ProtocolVersion), GameConstants.GameVersion);
    if (!auth.Success) throw new Exception($"login failed: {auth.Error}");
    return s;
}

Console.WriteLine();
Console.WriteLine("=== L2Clone smoke test (headless) ===");
Console.WriteLine();

// -------------------------------------------------------------------------------------------
// 1. Log in, enter the world.
// -------------------------------------------------------------------------------------------
// Version handshake: a client on a different version is rejected (an old client speaks an old protocol).
{
    var vs = new Session();
    await vs.OpenAsync(Url);
    var bad = await vs.Hub.InvokeAsync<AuthResponse>("Login",
        new AuthRequest("test1", "test"), "0.0.0-wrong");
    Check("a client on the wrong version is rejected at login", !bad.Success, bad.Error);
    await vs.DisposeAsync();
}

var a = await ConnectAsync("test1", "test");

// A FRESH character every run. The test mutates the character it plays (adds a subclass, levels it),
// so reusing one would make the run depend on whatever the LAST run left behind — which it did, and
// it cost a debugging detour. A test that is not idempotent lies to you.
string name = "Smoke" + DateTime.UtcNow.ToString("HHmmssff");
var createErr = await a.Hub.InvokeAsync<string?>("CreateCharacter",
    new CreateCharacterRequest(name, Race.Human, BaseClass.Fighter));
Check("created a fresh character", createErr is null, createErr);
if (createErr is not null) return Finish();

var chars = await a.Hub.InvokeAsync<CharacterList>("ListCharacters");
int charId = chars.Characters.First(c => c.Name == name).Id;

// The former "debug menu" is ADMIN-gated now, not `#if DEBUG`-gated (0.33.1) — it used to be compiled
// out, so the release server on the phone accepted every one of those calls and did nothing. This test
// leans on them heavily (levels, items, subclasses, professions), and test1 is an ordinary account, so the
// fresh character has to be promoted first. `/role` works on OFFLINE characters, which is why this can run
// before the character enters the world.
async Task PromoteToAdminAsync(string charName)
{
    var promoter = await ConnectAsync("admin", "admin");
    var pchars = await promoter.Hub.InvokeAsync<CharacterList>("ListCharacters");
    await promoter.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(pchars.Characters[0].Id));
    await promoter.Settle();
    await promoter.Hub.SendAsync("AdminCommand", "role", $"{charName} admin");
    await promoter.Settle();
    await promoter.Hub.SendAsync("LeaveWorld");
    await Task.Delay(300);
    await promoter.DisposeAsync();
}
await PromoteToAdminAsync(name);

// A SECOND fresh character, deliberately left an ordinary player: the moderation section needs a victim
// it can jail, kick and drain the charisma of, and none of that can be done to an admin (an admin may not
// re-rank an equal, by design, so the protagonist cannot be demoted back down once promoted). Keeping the
// two roles as two characters is also just truer to what the test is checking — the protagonist uses the
// admin toolbox, the victim is on the receiving end of moderation.
string victimName = "Vict" + DateTime.UtcNow.ToString("HHmmssff");
var victimErr = await a.Hub.InvokeAsync<string?>("CreateCharacter",
    new CreateCharacterRequest(victimName, Race.Human, BaseClass.Fighter));
Check("created a plain (non-admin) victim character", victimErr is null, victimErr);
if (victimErr is not null) return Finish();
var chars0 = await a.Hub.InvokeAsync<CharacterList>("ListCharacters");
int victimId = chars0.Characters.First(c => c.Name == victimName).Id;

var entered = await a.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(charId));
Check("entered the world", entered.Success, entered.Error);
if (!entered.Success) return Finish();

await a.Settle();
Check("server pushed the subclass list", a.Subclasses is not null);
Check("character starts with exactly one class", a.Subclasses?.Classes.Length == 1,
      $"got {a.Subclasses?.Classes.Length}");
Check("server pushed a skill bar", a.Bar is not null);
Check("server pushed the warehouse on login", a.Ware is not null);

// -------------------------------------------------------------------------------------------
// 1a-0. THE TUTORIAL CANNOT DEAD-END (0.60.1). The owner opened BOTH creation boxes before Cera gave
//     him the quest, so its "open a box" beat had nothing to open and the chain could not continue —
//     a DoAction step is a gate, and a gate whose prop is already consumed is a wall. The fix is that
//     a step SUPPLIES its own props (QuestStep.SupplyItemIds), granted whenever the bag holds none.
//
//     This is asserted here rather than played because the whole failure is invisible on screen: the
//     quest log renders a perfectly good objective either way, and the only difference is whether the
//     server put a box back in the bag. It must also stay IDEMPOTENT — a second push must not hand
//     over a second box — which is likewise invisible until someone has ten of them.
//
//     ⚠ Runs FIRST, while the protagonist is still level 1: the tutorial has a level CEILING of 20 and
//     the sections below level this character to 81.
// -------------------------------------------------------------------------------------------
{
    int Boxes(string defId) => a.Inv?.Items.Where(i => i.DefId == defId).Sum(i => i.Quantity) ?? 0;

    // 63j (2026-08-12): creation grants NO boxes any more. He was getting a set at creation, a set with
    // the quest and a set at the step — three weapons and three armours by the end of part 1.
    Check("a fresh character starts with NO training boxes",
          Boxes(ItemCatalog.BoxTrainingWeapons) == 0 && Boxes(ItemCatalog.BoxTrainingArmorChoice) == 0,
          $"weapons {Boxes(ItemCatalog.BoxTrainingWeapons)}, armor {Boxes(ItemCatalog.BoxTrainingArmorChoice)}");

    // ...and neither does ACCEPTING it. The props belong to the open-a-box step, which is reached only
    // after Pell: "Then so I get the boxes exactly before I need to open them."
    await a.Hub.SendAsync("QuestAction", "accept", QuestCatalog.QuestTutorialWelcome, Guid.Empty);
    await a.Settle();
    Check("accepting the tutorial does NOT hand over a kit up front",
          Boxes(ItemCatalog.BoxTrainingWeapons) == 0 && Boxes(ItemCatalog.BoxTrainingArmorChoice) == 0,
          $"weapons {Boxes(ItemCatalog.BoxTrainingWeapons)}, armor {Boxes(ItemCatalog.BoxTrainingArmorChoice)}");

    // The dead-end guard itself is now a CATALOG invariant rather than something this test can play:
    // reaching the box step needs a walk to Pell and a talk, which a headless client cannot do without
    // faking positions. What must never regress is the pairing — creation grants nothing, so if the
    // step ever stops declaring its props the training kit becomes unreachable for everyone, not just
    // for a player who opened a box early.
    var welcome = QuestCatalog.Get(QuestCatalog.QuestTutorialWelcome);
    var boxStep = welcome?.Steps.FirstOrDefault(s => s.Type == QuestStepType.DoAction
                                                  && s.TargetId == QuestActions.OpenBox);
    Check("the tutorial's open-a-box step SUPPLIES both training boxes",
          boxStep?.SupplyItemIds is { } props
              && props.Contains(ItemCatalog.BoxTrainingWeapons)
              && props.Contains(ItemCatalog.BoxTrainingArmorChoice),
          boxStep is null ? "no open-box step found"
                          : $"supplies [{string.Join(", ", boxStep.SupplyItemIds ?? Array.Empty<string>())}]");

    // Both training boxes are PLAIN now (no picker) and class-conditional: a fighter must see exactly
    // one option in each, and it must not be the mage's.
    foreach (var (boxId, want) in new[]
             {
                 (ItemCatalog.BoxTrainingWeapons, ItemCatalog.TrainingSword),
                 (ItemCatalog.BoxTrainingArmorChoice, ItemCatalog.TrainingLeather),
             })
    {
        var box = BoxCatalog.Get(boxId);
        var forFighter = box?.Entries.Where(e => e.ForClass is null or BaseClass.Fighter).ToArray();
        Check($"{boxId} is a plain box with one fighter entry ({want})",
              box is { PickCount: 0 } && forFighter is { Length: 1 } && forFighter[0].ItemId == want,
              $"pick {box?.PickCount}, fighter entries [{string.Join(", ", forFighter?.Select(e => e.ItemId) ?? Array.Empty<string>())}]");
    }

    // Leave nothing behind for the sections below: this character goes on to be levelled and geared.
    await a.Hub.SendAsync("QuestAction", "abandon", QuestCatalog.QuestTutorialWelcome, Guid.Empty);
    await a.Settle();
}

// -------------------------------------------------------------------------------------------
// 1a-2. QUEST MARKERS. The "!" over an NPC's head is per-PLAYER (level, race, class and what you
//     have already done all decide it), so it is computed server-side and pushed with the quest log.
//     A marker that is right in the client while the server thinks otherwise is exactly the class of
//     bug this test exists for — assert it on the wire.
// -------------------------------------------------------------------------------------------
// -------------------------------------------------------------------------------------------
// 1a-1. EVERY VENDOR NPC RESOLVES TO A SHOP. The ring towns' vendors inherit Brackenford's stock by
//     an id CONVENTION ("merchant_gear_stonewatch" -> "merchant_gear"), and a convention that silently
//     stops matching gives you a vendor who greets you and sells nothing. Cheap to assert, and it is
//     pure catalogue data, so no world state is needed.
// -------------------------------------------------------------------------------------------
{
    int vendors = 0, empty = 0;
    foreach (var npc in WorldMap.Npcs)
    {
        if (npc.Role != NpcRole.Vendor) continue;
        vendors++;
        var shop = ShopCatalog.Get(npc.Id);
        if (shop is null || shop.ItemIds.Length == 0) { empty++; Console.WriteLine($"        [SHOP] {npc.Id} has NO stock"); }
    }
    Check("every vendor NPC in the world resolves to a stocked shop", empty == 0 && vendors > 0,
          $"{vendors} vendors, {empty} empty");
}

// -------------------------------------------------------------------------------------------
// 1a-1b. THE GEAR LADDER'S SHAPE. The authored tier tables are the MYTHIC piece and every lesser
//     quality is derived from it. All of that is arithmetic nobody sees until an item is in hand,
//     so assert it on the catalogue directly.
// -------------------------------------------------------------------------------------------
{
    var aSword = ItemCatalog.Get("sword1h_t76");
    var sSword = ItemCatalog.Get($"sword1h_t{ItemCatalog.SGradeLevel}");
    Check("A-grade sword is MYTHIC (the authored number is the ceiling, not a 70% anchor)",
          aSword is { Rarity: ItemRarity.Mythic }, $"{aSword?.Rarity}");
    // ⚠ This USED to assert S == A × SGradeOverA (1.60). He authored the whole level-80 column by hand
    // on 2026-08-11 and the constant is gone, so the only invariant left is "S exists and beats A" —
    // the exact numbers are data he owns, and re-deriving them here would just re-create the constant.
    Check("S grade exists and is authored ABOVE A (no longer a ×1.60 derivation)",
          sSword is not null && aSword is not null && sSword.AtkBonus > aSword.AtkBonus,
          $"A {aSword?.AtkBonus} -> S {sSword?.AtkBonus}");

    var aEpic = ItemCatalog.Get("sword1h_t76_epic");
    Check("A-Epic is 70% of A-Mythic (the split rung, same stats as Rare)",
          aEpic is not null && aSword is not null
            && aEpic.AtkBonus == (int)(aSword.AtkBonus * 0.70f),
          $"{aEpic?.AtkBonus} vs {aSword?.AtkBonus}");

    // S is TOP HALF ONLY — crafting produces Legendary, so that rung must exist or S can never be
    // crafted; and the sub-Epic rungs must NOT exist or they would be endgame clutter.
    Check("S has a LEGENDARY rung (crafting produces Legendary — without it S is uncraftable)",
          ItemCatalog.Get($"sword1h_t{ItemCatalog.SGradeLevel}_legendary") is not null);
    Check("S has NO common/uncommon/rare rungs",
          ItemCatalog.Get($"sword1h_t{ItemCatalog.SGradeLevel}_common") is null
          && ItemCatalog.Get($"sword1h_t{ItemCatalog.SGradeLevel}_uncommon") is null
          && ItemCatalog.Get($"sword1h_t{ItemCatalog.SGradeLevel}_rare") is null);
    Check("A still HAS the low rungs (only S is top-half)",
          ItemCatalog.Get("sword1h_t76_common") is not null);

    // The "(Lesser)" line is GONE — it became the low QUALITIES of the real ladder.
    int lesser = ItemCatalog.AllItems.Count(d => d.Name.Contains("(Lesser)")
                                                 && d.Slot is EquipSlot.Weapon or EquipSlot.Armor
                                                            or EquipSlot.Shield or EquipSlot.Jewel);
    Check("no '(Lesser)' GEAR exists any more", lesser == 0, $"{lesser} found");

    // The newbie kit IS the F-grade top: same item, "Ferrite" themed, Mythic rung.
    var fSword = ItemCatalog.Get(ItemCatalog.NewbieSword1H);
    Check("the newbie weapon is the F-grade MYTHIC piece",
          fSword is { Rarity: ItemRarity.Mythic, ItemLevel: ItemCatalog.FGradeLevel },
          $"{fSword?.Name} {fSword?.Rarity} lvl {fSword?.ItemLevel}");
    Check("...and it is themed Ferrite (F grade)", fSword?.Name.StartsWith("Ferrite") == true, fSword?.Name);
    Check("F grade has the low rungs too (so the shop has something cheap)",
          ItemCatalog.Get($"sword1h_t{ItemCatalog.FGradeLevel}_common") is not null);

    // A set is joined to its pieces by an id STRING and nothing else, so a mismatch is a bonus that
    // silently never applies — exactly what happened when the newbie kit became the F tier and its set
    // ids were left pointing at the retired items. Assert the join, not just that both halves exist.
    foreach (var (bodyId, setName) in new[]
             {
                 (ItemCatalog.NewbieLightBody, "light"),
                 (ItemCatalog.NewbieRobeBody, "robe"),
                 ($"heavy_t{ItemCatalog.FGradeLevel}", "heavy"),
             })
    {
        var body = ItemCatalog.Get(bodyId);
        var set = body is null ? null : ArmorSetCatalog.Get(body.SetId);
        // No failure-detail on these: Check prints the detail on PASS too, so "PASS … (has no
        // ArmorSetDef)" reads as a contradiction.
        Check($"the F {setName} body's set RESOLVES (id matches a definition)", set is not null);
        // …and the accessory line it names must resolve to the F accessories the pieces carry.
        var helm = ItemCatalog.Get(ItemCatalog.NewbieHelm);
        Check($"the F {setName} set's accessory line matches the F helm's set id",
              set is not null && helm is not null && set.AccessorySetId == helm.SetId);
    }

    // ---- A SET MUST BE FOUR PIECES OF THE SAME QUALITY (owner) ----
    // Previously every Epic/Legendary/Mythic copy carried the SAME set id, so a Mythic body finished
    // by Epic accessories completed the MYTHIC set at full strength — mixing beat matching. Assert the
    // ids now segregate by quality, and that each quality's set exists with a scaled bonus.
    {
        var mythicBody = ItemCatalog.Get("light_t20");            // authored piece = Mythic
        var epicBody   = ItemCatalog.Get("light_t20_epic");
        var legBody    = ItemCatalog.Get("light_t20_legendary");
        var rareBody   = ItemCatalog.Get("light_t20_rare");
        Check("Epic and Mythic bodies do NOT share a set id (no mixing)",
              epicBody is not null && mythicBody is not null && epicBody.SetId != mythicBody.SetId,
              $"epic '{epicBody?.SetId}' vs mythic '{mythicBody?.SetId}'");
        Check("Legendary has its own set id too",
              legBody is not null && legBody.SetId != mythicBody!.SetId && legBody.SetId != epicBody!.SetId);
        Check("below Epic there is no set at all", rareBody is { SetId: "" }, $"rare SetId '{rareBody?.SetId}'");

        var mSet = ArmorSetCatalog.Get(mythicBody!.SetId);
        var eSet = ArmorSetCatalog.Get(epicBody!.SetId);
        Check("both the Mythic and Epic sets exist", mSet is not null && eSet is not null);
        // The Epic set must be WEAKER — 70% of the authored numbers. Asserted on the HEAVY t20 set
        // because it is the one that actually carries MaxHp (135); the light set uses Evasion/MaxMp,
        // and picking a field a set does not use compares 0 against 0 and passes for the wrong reason.
        var mHeavy = ArmorSetCatalog.Get("set_heavy_t20");
        var eHeavy = ArmorSetCatalog.Get("set_heavy_t20_epic");
        Check("the Epic set's bonus is scaled below Mythic's",
              mHeavy is not null && eHeavy is not null
                && eHeavy.Mods.MaxHp > 0 && eHeavy.Mods.MaxHp < mHeavy.Mods.MaxHp,
              $"epic {eHeavy?.Mods.MaxHp} vs mythic {mHeavy?.Mods.MaxHp}");
        Check("the Epic set's SHIELD bonus is scaled too",
              mHeavy is not null && eHeavy is not null
                && eHeavy.ShieldBonus.ShieldDefPct > 0f
                && eHeavy.ShieldBonus.ShieldDefPct < mHeavy.ShieldBonus.ShieldDefPct);
        // An Epic body must want EPIC accessories, not the shared line.
        var epicHelm = ItemCatalog.Get("helm_t20_epic");
        Check("an Epic body's set wants EPIC accessories",
              eSet is not null && epicHelm is not null && eSet.AccessorySetId == epicHelm.SetId,
              $"set wants '{eSet?.AccessorySetId}', epic helm has '{epicHelm?.SetId}'");
        Check("...and a MYTHIC helm would NOT satisfy it",
              eSet is not null && ItemCatalog.Get("helm_t20") is { } mh && eSet.AccessorySetId != mh.SetId);
    }
}

// -------------------------------------------------------------------------------------------
// 1b². THE WORLD LAYOUT — camps, bands, fields, gates, managing cities.
// -------------------------------------------------------------------------------------------
// These are pure CATALOG checks (no protocol), and they exist because every one of them describes a bug
// that a playtest showed only as a bad afternoon. The headline one is the owner's: a hand-listed roster
// spanning levels 1-12 put a level-12 Werewolf in the level-1 camp, because a mob with a natural level
// ignores the zone's band. "How exactly am I supposed to kill a pig next to a werewolf?"
{
    // The pig-and-werewolf guard. ForceZoneLevel camps are exempt BY DESIGN — they deliberately borrow a
    // lower roster and re-level it, which is how 86-90 exists at all.
    var strays = new List<string>();
    foreach (var f in WorldPlan.Fields)
        foreach (var z in f.Zones)
        {
            if (z.ForceZoneLevel) continue;
            foreach (var id in z.MobTypes)
            {
                int lvl = MobCatalog.Get(id).Level;
                if (lvl < z.MinLevel || lvl > z.MaxLevel)
                    strays.Add($"{f.Plan.Name} Lv{z.MinLevel}-{z.MaxLevel} has {id} (Lv{lvl})");
            }
        }
    Check("no camp holds a creature outside its own level band (no pig next to a werewolf)",
          strays.Count == 0, strays.Count == 0 ? null : string.Join("; ", strays.Take(3)));

    // Bands are 4 levels wide (2 at the top) — the owner's "1-4, 4-8, 8-12 …", not the old 5-6-level
    // spans. The boundaries are SHARED in that notation (4-8 follows 1-4), so the span to assert on is
    // Max-Min ≤ 4, and the previous world's 22-28 / 76-80 camps are what this rules out.
    var wide = WorldPlan.Plans.SelectMany(p => p.Bands.Select(b => (p.Name, b)))
                              .Where(t => t.b.Max - t.b.Min > 4).ToArray();
    Check("every band is at most 4 levels wide", wide.Length == 0,
          string.Join(", ", wide.Select(t => $"{t.Name} {t.b.Min}-{t.b.Max}")));

    // Nothing empty: a band whose levels no creature occupies would spawn nothing and read as an empty field.
    int empty = WorldPlan.Fields.SelectMany(f => f.Zones).Count(z => z.MobTypes.Length == 0);
    Check("no camp has an empty roster", empty == 0, $"{empty} empty");

    // Starter camps must be PEACEFUL — nothing should ever jump a level-3 character.
    var starter = WorldPlan.Fields.First(f => f.Plan.Id == "field_bracken_hollow");
    Check("the two starter camps are peaceful (nothing attacks on sight)",
          starter.Zones.All(z => z.AggressiveTypes is { Length: 0 }));
    // …and the endgame is not.
    var summit = WorldPlan.Fields.First(f => f.Plan.Id == "field_frost_summit");
    Check("an endgame camp has three aggressive types",
          summit.Zones.Any(z => z.Rank == MobRank.Normal && z.AggressiveTypes is { Length: 3 }));

    // Every field is OWNED by a city, and that city's gatekeeper can therefore send you there.
    var orphan = RegionMap.Fields.Where(f => f.CityId.Length == 0)
                                 .Select(f => f.Id).ToArray();
    Check("every planned field records a managing city",
          orphan.All(id => id is "field_training" or "field_treant" or "field_dungeon"),
          string.Join(", ", orphan));
    Check("every city owns at least two fields",
          WorldPlan.Cities.All(c => RegionMap.FieldsOf(c.Id).Length >= 2),
          string.Join(", ", WorldPlan.Cities.Select(c => $"{c.Name}:{RegionMap.FieldsOf(c.Id).Length}")));

    // Gates: named, described, uniquely identified, and resolvable back to their field. A gate id is the
    // whole wire contract for travel now — a collision would silently send you to the wrong camp.
    var gates = RegionMap.Regions.SelectMany(r => r.Gates).ToArray();
    Check("gate ids are unique", gates.Select(g => g.Id).Distinct().Count() == gates.Length);
    Check("every gate has a name and a description",
          gates.All(g => g.Name.Length > 0 && g.Description.Length > 0));
    Check("every gate resolves by id back to its own field",
          gates.All(g => RegionMap.GateById(g.Id) is not null));
    // …and every NORMAL camp has one, or a band would be unreachable by gatekeeper.
    int gateless = WorldPlan.Fields.Sum(f => f.Zones.Count(z => z.Rank == MobRank.Normal) - f.Gates.Length);
    Check("every normal camp has a gate (elites deliberately do not)", gateless == 0, $"{gateless} missing");

    // A gate must land you INSIDE its own field — it is stepped back onto the camp's town-facing rim, and
    // an arithmetic slip there would drop you in open ground outside the polygon.
    var outside = RegionMap.Fields
        .Where(f => f.CityId.Length > 0)
        .SelectMany(f => f.Gates.Select(g => (f, g)))
        .Where(t => !t.f.Contains(t.g.At.X, t.g.At.Y))
        .Select(t => t.g.Name).ToArray();
    Check("every gate lands inside its own field", outside.Length == 0, string.Join(", ", outside));

    // The managing-city lookup must agree with the plan at every camp centre — this is what death reads.
    var mismatch = WorldPlan.Fields
        .SelectMany(f => f.Zones.Select(z => (f, z)))
        .Where(t => RegionMap.ManagingCity(t.z.X, t.z.Y)?.Id != t.f.Plan.CityId)
        .Select(t => $"{t.f.Plan.Name} Lv{t.z.MinLevel}").ToArray();
    Check("every camp reports its own city as the managing city (this is where you respawn)",
          mismatch.Length == 0, string.Join(", ", mismatch.Take(3)));

    // No hole in the climb: every level 1..90 must have somewhere to earn it.
    var uncovered = Enumerable.Range(1, GameConstants.MaxPlayerLevel)
        .Where(l => !WorldPlan.Plans.Any(p => p.Bands.Any(b => l >= b.Min && l <= b.Max)))
        .ToArray();
    Check("every level 1-90 is covered by some camp", uncovered.Length == 0,
          string.Join(", ", uncovered));
}

Check("server pushed quest markers on login", a.Marks is not null);
// ⚠ This used to assert ZERO markers at level 1 — "the starter chain opens at 10, the class chains
// at 18, so asserting > 0 would be asserting a bug". That stopped being true in 0.54.0, when the
// TUTORIAL chain landed and deliberately opens at level 1: the assertion then asserted the ABSENCE
// of a shipped feature and failed every run. A brand-new character must now be offered exactly the
// tutorial, and nothing else.
Check("a level-1 character is offered the TUTORIAL and only the tutorial",
      a.Marks is not null && a.Marks.Marks.Length == 1,
      $"{a.Marks?.Marks.Length ?? 0} marks");

// -------------------------------------------------------------------------------------------
// 1c. WAREHOUSE (private bank). Deposit an item in the spawn town, then let the RELOG below prove it
//     came back from the DB in the BANK (not the bag). Done here because the bank is town-gated and the
//     character walks off into the field in later phases.
// -------------------------------------------------------------------------------------------
Guid bankedId = Guid.Empty;
await a.Hub.SendAsync("DebugGive", ItemCatalog.HealingPotion, 1);
await a.Settle();
var toBank = a.Inv?.Items.FirstOrDefault(i => i.DefId == ItemCatalog.HealingPotion);
Check("got a potion to deposit", toBank is not null);
if (toBank is not null)
{
    bankedId = toBank.InstanceId;
    await a.Hub.SendAsync("WarehouseDeposit", bankedId);
    await a.Settle();
    Check("deposit moved the potion INTO the warehouse",
          a.Ware?.Items.Any(i => i.InstanceId == bankedId) == true);
    Check("deposit removed the potion from the BAG",
          a.Inv?.Items.All(i => i.InstanceId != bankedId) == true);
}

// -------------------------------------------------------------------------------------------
// 1b. DELTA SNAPSHOTS (the live world push). The full state is no longer re-sent every tick — an
//     entity is SPAWNED once (full), then only lean UPDATES while it moves, and DESPAWNED on leaving.
//     These would look fine in-client while being wrong on the wire, so assert the protocol directly.
// -------------------------------------------------------------------------------------------
Guid myId = entered.EntityId;
a.MyId = myId;
Check("I was SPAWNED in my own delta (full entity on entry)", a.Spawned.Contains(myId));

// MOVE, then expect a lean UPDATE for myself (position is a dynamic field). Static fields must NOT ride
// updates — so DebugLevel (Level is static) should come back as a re-SPAWN, not an update.
a.ResetDeltas();
await a.Hub.SendAsync("Move", new MoveCommand(entered.X + 400, entered.Y));
await a.Settle();
Check("moving produced a lean UPDATE for me (dynamic field on the wire)", a.Updated.Contains(myId));
Check("a still world doesn't spawn me again (static data isn't re-sent)", !a.Spawned.Contains(myId),
      "spawned again while only moving");

a.ResetDeltas();
await a.Hub.SendAsync("DebugLevel", 1);
await a.Settle();
Check("a STATIC change (level-up) re-SPAWNS me, not a lean update", a.Spawned.Contains(myId));
await a.Hub.SendAsync("DebugLevel", -1);   // back to level 1 so the leveling math below still lands on 81
await a.Settle();

// -------------------------------------------------------------------------------------------
// 1b-2. THE EXP CURVE ON THE WIRE. The curve moved to the real Lineage 2 table (ExpCurve), where the
//     shape is a power law only to level 50 and then SEVEN multiplicative walls — so a plain formula
//     can no longer stand in for it, and an off-by-one in the table shifts every level by one.
//     That is invisible in play: the bar still fills, just against the wrong denominator. It is only
//     visible as the wrong ExpToNext arriving on the wire, which is exactly what this reads.
//     (The off-by-one below is not hypothetical — the first cut of the table had it.)
// -------------------------------------------------------------------------------------------
Check("the server pushes progress at all", a.Progress is not null);
if (a.Progress is not null)
{
    Check($"exp-to-next at level {a.Progress.Level} matches the curve",
          a.Progress.ExpToNext == ExpCurve.ExpToNext(a.Progress.Level),
          $"server says {a.Progress.ExpToNext:N0}, curve says {ExpCurve.ExpToNext(a.Progress.Level):N0}");
}
// Level 1 is the anchor the off-by-one shows up at first: 68, not 295 (which is level 2's cost).
Check("level 1 costs 68 exp (the table is not shifted by one)", ExpCurve.ExpToNext(1) == 68,
      $"got {ExpCurve.ExpToNext(1)}");
// The wall at 79->80 is the loudest feature in the table; if the tail is misaligned this moves.
Check("the level-79 wall is intact (x3.57 step)", ExpCurve.ExpToNext(79) == 2_100_724_166L,
      $"got {ExpCurve.ExpToNext(79):N0}");
// Levels 86-100 are spliced from a second source, where rows 88 and 89 were published TRANSPOSED and
// are swapped back here — so a level costing meaningfully LESS than the one before it means a row is
// out of order. Tolerance is 1%: real L2 pins level 80's cumulative total at exactly 4 200 000 000, a
// deliberately round number, which makes level 80 come out 0.03% cheaper than 79. That dip is in the
// authentic data and is not worth "fixing"; a transposition looks nothing like it (88/89 was 24%).
int transposedAt = 0;
for (int L = 2; L <= ExpCurve.MaxLevel; L++)
    if (ExpCurve.ExpToNext(L) < ExpCurve.ExpToNext(L - 1) * 0.99) { transposedAt = L; break; }
Check("no level is materially cheaper than the one before it (no transposed rows)",
      transposedAt == 0, $"level {transposedAt} costs less than {transposedAt - 1}");
// EXP is long end to end now; int would wrap negative past level 79.
Check("the top of the curve exceeds int range (so long is actually required)",
      ExpCurve.ExpToNext(85) > int.MaxValue);

// -------------------------------------------------------------------------------------------
// 1c. FRIENDS are MUTUAL (owner, 2026-07-20). /fadd is only an invite: until the other side adds you
//     back you are [pending] and get NO presence information at all, and they are deliberately not
//     notified. Once it's reciprocal, both sides see online/offline. Non-admin, per character.
// -------------------------------------------------------------------------------------------
a.SystemChat.Clear();
await a.Hub.SendAsync("FriendCommand", "add", "Test2");
await a.Settle();
Check("adding a friend sends a one-way request",
      a.SystemChat.Any(s => s.Contains("Test2") && s.Contains("pending")),
      string.Join(" | ", a.SystemChat));

a.SystemChat.Clear();
await a.Hub.SendAsync("FriendCommand", "list", "");
await a.Settle();
Check("/flist shows an unreciprocated friend as [pending], with NO online/offline state",
      a.SystemChat.Any(s => s.Contains("Test2") && s.Contains("pending"))
      && !a.SystemChat.Any(s => s.Contains("Test2") && (s.Contains("[online]") || s.Contains("[offline]"))),
      string.Join(" | ", a.SystemChat));

// Test2 comes online WITHOUT having added us back → still pending, so no presence message at all.
a.SystemChat.Clear();
var friend = await ConnectAsync("test2", "test");
var friendChars = await friend.Hub.InvokeAsync<CharacterList>("ListCharacters");
await friend.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(friendChars.Characters[0].Id));
await a.Settle();
Check("a PENDING friend coming online tells you nothing",
      !a.SystemChat.Any(s => s.Contains("Test2") && s.Contains("is now")),
      string.Join(" | ", a.SystemChat));

// ---- The admin gate on the former DEBUG menu (0.33.1) ----
// These commands SHIP now — they used to be compiled out, so the release server on the phone silently
// ignored them. Shipping them means the gate has to be real, and "real" has to be proven from a plain
// account: a missing check hands any player free gold, levels and a 3rd class. Test2 is an ordinary
// account, and `friend` is the only plain session this test has.
{
    friend.SystemChat.Clear();
    friend.Gold = -1;
    await friend.Hub.SendAsync("DebugGold", 1_000_000L);
    await friend.Settle();
    Check("a NON-admin is refused an admin-only command", friend.Gold == -1,
          friend.Gold == -1 ? null : $"gold arrived: {friend.Gold}");
    Check("...and is TOLD, not silently ignored",
          friend.SystemChat.Any(s => s.Contains("admin-only")),
          string.Join(" | ", friend.SystemChat));
}

// Test2 adds us back → NOW it's a real friendship, and both sides are told.
a.SystemChat.Clear();
await friend.Hub.SendAsync("FriendCommand", "add", name);
await a.Settle();
Check("reciprocating makes it a real friendship, and both sides hear about it",
      a.SystemChat.Any(s => s.Contains("Test2") && s.Contains("now your friend")),
      string.Join(" | ", a.SystemChat));

a.SystemChat.Clear();
await a.Hub.SendAsync("FriendCommand", "list", "");
await a.Settle();
Check("/flist shows a MUTUAL friend's online state",
      a.SystemChat.Any(s => s.Contains("Test2") && s.Contains("[online]")),
      string.Join(" | ", a.SystemChat));

// -------------------------------------------------------------------------------------------
// 4a. CHARISMA — /like gives +1 from a 20/day budget; it ranks on the charisma board.
// -------------------------------------------------------------------------------------------
friend.SystemChat.Clear();
a.SystemChat.Clear();
await a.Hub.SendAsync("Like", "Test2");
await a.Settle();
Check("liking a player raised their charisma",
      friend.SystemChat.Any(s => s.Contains("liked you") && s.Contains("charisma")),
      string.Join(" | ", friend.SystemChat));
Check("the liker spent one from the daily budget",
      a.SystemChat.Any(s => s.Contains("likes left today")));
a.SystemChat.Clear();
await a.Hub.SendAsync("Like", name);   // can't like yourself
await a.Settle();
Check("you can't like yourself", a.SystemChat.Any(s => s.Contains("can't like yourself")));
// Test2 likes the VICTIM (offline — Like resolves offline targets in the DB), so the later jail has
// charisma to drain. It used to like the protagonist, which stopped working the moment the protagonist
// became an admin: STAFF ARE EXCLUDED FROM THE LEADERBOARDS, which is the answer to the owner's
// playtest-13 puzzle — "my ranking board was never updated ... aaa, my chars are admins".
await friend.Hub.SendAsync("Like", victimName);
await a.Settle();
LeaderboardDto chBoard = null!;
for (int attempt = 0; attempt < 10; attempt++)
{
    chBoard = await a.Hub.InvokeAsync<LeaderboardDto>("RequestLeaderboard", "charisma");
    if (chBoard.Entries.Any(e => e.Name == victimName)) break;
    await Task.Delay(300);   // the offline like lands via a background DB write
}
Check("the liked player reached the charisma board",
      chBoard.Entries.Any(e => e.Name == "Test2" && e.Value >= 1),
      string.Join(",", chBoard.Entries.Select(e => $"{e.Name}:{e.Value}")));
Check("an offline like reached the board too (the victim)",
      chBoard.Entries.Any(e => e.Name == victimName && e.Value >= 1));
Check("an ADMIN character is kept OFF the leaderboard (staff don't compete)",
      chBoard.Entries.All(e => e.Name != name));

// -------------------------------------------------------------------------------------------
// 4b. BLOCK / IGNORE. A blocked player's whisper (and world/local chat) is filtered out for you; the
//     SENDER is told it wasn't accepted, the recipient hears nothing. Both players are online here.
// -------------------------------------------------------------------------------------------
a.AllChat.Clear();
await friend.Hub.SendAsync("Chat", "hello before block", ChatChannel.Whisper, name);
await a.Settle();
Check("a whisper is delivered BEFORE blocking",
      a.AllChat.Any(m => m.Channel == ChatChannel.Whisper && m.From == "Test2"));

await a.Hub.SendAsync("BlockCommand", "block", "Test2");
await a.Settle();
a.AllChat.Clear();
friend.SystemChat.Clear();
await friend.Hub.SendAsync("Chat", "still there?", ChatChannel.Whisper, name);
await a.Settle();
Check("a blocked player's whisper is NOT delivered",
      !a.AllChat.Any(m => m.Channel == ChatChannel.Whisper && m.From == "Test2"));
Check("the blocked SENDER is told the message wasn't accepted",
      friend.SystemChat.Any(s => s.Contains("not accepting")));

// ...and going offline now reports, because the friendship is mutual.
a.SystemChat.Clear();
await friend.Hub.SendAsync("LeaveWorld");
await Task.Delay(400);
Check("a MUTUAL friend going offline reports it",
      a.SystemChat.Any(s => s.Contains("Test2") && s.Contains("Offline")),
      string.Join(" | ", a.SystemChat));
await friend.DisposeAsync();

// (FOLLOW/ASSIST are verified in the playtest — a position-convergence smoke check depends on two
//  characters sharing a spawn town, which the seed accounts don't, and the mechanics are simple.)

int mainSlot = a.Subclasses!.Classes[0].Slot;
var mainClass = a.Subclasses.Classes[0].BaseClass;

static string Show(string[]? bar) => bar is null
    ? "<null>"
    : "[" + string.Join(",", bar.Select(s => string.IsNullOrEmpty(s) ? "_" : s)) + "]";

// -------------------------------------------------------------------------------------------
// 2. Arrange the MAIN class's skill bar.
//
//    The PLAYER lays the bar out now — the server stopped auto-placing newly-learned skills
//    (owner, 2026-07-20: it rearranged the bar under you on every level-up, and re-added skills you
//    had deliberately removed). So this test does what the client does: learn the skills, then place
//    them itself. The layout is deliberately one the server would never produce, so if anything
//    re-derives the bar instead of preserving it, the assertions below notice.
// -------------------------------------------------------------------------------------------
// Give the character real skills FIRST. An all-empty bar proves nothing — every assertion below
// would pass trivially by comparing empty to empty, which is exactly how a broken bar could sneak
// through. Level up so skills exist, then learn everything the class can.
// Adding a subclass now requires EVERY owned class to be level 75+ AND hold its 3rd class. The debug
// level step is clamped to +10 (it mirrors the UI buttons), so climb in +10s: 1 -> 81, past the gate.
for (int i = 0; i < 8; i++) await a.Hub.SendAsync("DebugLevel", 10);
// Give the MAIN its 3rd class too (the add-gate now needs it). A Human 3rd class for the Human main; the
// subclass chosen below must be a DIFFERENT discipline (the no-duplicate rule now counts the main).
var mainThird = ThirdClassCatalog.Playable.First(t => t.Race == Race.Human);
await a.Hub.SendAsync("DebugThirdClass", mainThird.Id);
await a.Hub.SendAsync("DebugLearnAll");
await a.Settle();
// The server must NOT have placed anything: auto-placement is gone.
Check("the server does NOT auto-place learned skills on the bar",
      a.Bar!.Slots.All(string.IsNullOrEmpty),
      $"server bar = {Show(a.Bar!.Slots)}");

// Now lay the bar out as the PLAYER would: the learned skills, in an order the server would never
// pick (reverse-alphabetical), plus a built-in ACTION token — the bar must preserve those too.
var toPlace = a.Learned!.Skills
    .Select(s => s.Id)
    .Where(id => SkillCatalog.Get(id) is { } d && d.Category != SkillCategory.Passive)
    .OrderByDescending(id => id, StringComparer.Ordinal)
    .ToList();

Check("the character learned some active skills to place",
      toPlace.Count > 0,
      "an empty bar would make every check below pass for the wrong reason");

string[] mainBar = new string[GameConstants.SkillBarSlots];
for (int i = 0; i < mainBar.Length; i++) mainBar[i] = "";
mainBar[0] = GameConstants.ActionSlotToken(GameConstants.ActionTargetClosest);
for (int i = 0; i < toPlace.Count && i + 1 < mainBar.Length; i++)
    mainBar[i + 1] = toPlace[i];

await a.Hub.SendAsync("SetSkillBar", mainBar);
await a.Settle();
Console.WriteLine($"        main class = {mainClass}, main bar SET to {Show(mainBar)}");

// -------------------------------------------------------------------------------------------
// 3. Add a SUBCLASS of the other base class and switch to it.
// -------------------------------------------------------------------------------------------
// You pick a specific 3rd-class discipline now (pre-approved). It must DIFFER from the main's discipline
// (the no-duplicate rule now counts the main). Take the first catalog entry that differs.
var chosen = ThirdClassCatalog.Playable.First(t => t.Discipline != mainThird.Discipline);
a.Bar = null;
await a.Hub.SendAsync("DebugAddSubclass", chosen.Id);
await a.Settle();

Check("subclass added", a.Subclasses!.Classes.Length == 2,
      $"got {a.Subclasses.Classes.Length}");
var sub = a.Subclasses.Classes.FirstOrDefault(c => c.Slot != mainSlot);
Check("now PLAYING the new class", sub is { Active: true });
Check("new class starts at level 1", sub?.Level == 1, $"level {sub?.Level}");
Check("new class has the chosen 3rd class pre-approved", sub?.ThirdClass == chosen.Id);
Check("new class is the discipline's own race", sub?.Race == chosen.Race);
Check("switching pushed a fresh skill bar", a.Bar is not null);

int subSlot = sub!.Slot;

// The new class's bar must NOT be the main class's bar.
string[] subBarFromServer = a.Bar!.Slots;
Console.WriteLine($"        subclass bar from server = {Show(subBarFromServer)}");
Check("the new class did NOT inherit the main class's bar",
      !subBarFromServer.SequenceEqual(mainBar));

// Give the subclass skills and its OWN bar, and level it by a DIFFERENT amount to the main class —
// if both ended on the same level, a bug that mixed them up would pass unnoticed.
await a.Hub.SendAsync("DebugLearnAll");
await a.Settle();
string[] subBar = a.Bar!.Slots.Reverse().ToArray();
await a.Hub.SendAsync("SetSkillBar", subBar);
await a.Hub.SendAsync("DebugLevel", 4);
await a.Settle();
Console.WriteLine($"        subclass bar SET to {Show(subBar)}");

// -------------------------------------------------------------------------------------------
// 4. Switch BACK to the main class. This is the step that was silently corrupting the bar.
// -------------------------------------------------------------------------------------------
a.Bar = null;
await a.Hub.SendAsync("SwitchSubclass", mainSlot);
await a.Settle();

Check("switched back to the main class",
      a.Subclasses!.Classes.First(c => c.Slot == mainSlot).Active);
Console.WriteLine($"        expected {Show(mainBar)}");
Console.WriteLine($"        got      {Show(a.Bar?.Slots)}");
Check("MAIN class's bar came back exactly as left",
      a.Bar is not null && a.Bar.Slots.SequenceEqual(mainBar),
      "the swap used to overwrite it while still LOOKING right");
Check("main class kept its OWN level (the subclass's levels did not leak into it)",
      a.Subclasses.Classes.First(c => c.Slot == mainSlot).Level == 81,
      $"level {a.Subclasses.Classes.First(c => c.Slot == mainSlot).Level}, expected 81");
Check("subclass kept its own level while parked",
      a.Subclasses.Classes.First(c => c.Slot == subSlot).Level == 5,
      $"level {a.Subclasses.Classes.First(c => c.Slot == subSlot).Level}, expected 5");

// -------------------------------------------------------------------------------------------
// 4b. Bar CAPACITY + ITEM SLOTS. Both are 2026-07-17 changes that live in persistence and would look
//     perfect in the running client while being wrong on the next login — exactly this test's remit.
//       - the bar is now 60 slots (5x12), not 24;
//       - a slot may hold an ITEM ("item:<defId>"), which SyncSkillBar must NOT wipe as an unknown skill.
// -------------------------------------------------------------------------------------------
Check("skill bar is 60 slots (5 rows x 12)",
      a.Bar!.Slots.Length == GameConstants.SkillBarSlots,
      $"got {a.Bar!.Slots.Length}, expected {GameConstants.SkillBarSlots}");

string itemToken = GameConstants.ItemSlotToken(ItemCatalog.HealingPotion);
// Also place an equip-PRESET token. Presets were added AFTER this test, and SyncSkillBar's "forget
// unknown skills" pass did not exempt them — so a preset on the bar was wiped on the very next re-sync
// and vanished on relog (device playtest 0.28.79). This asserts it survives, so that can't regress.
string presetToken = GameConstants.PresetSlotToken(0);   // "preset:0" = the A preset
var withItem = (string[])mainBar.Clone();
int freeIdx = Array.FindIndex(withItem, string.IsNullOrEmpty);
Check("the bar has a free slot to place an item token", freeIdx >= 0);
if (freeIdx >= 0) withItem[freeIdx] = itemToken;
int presetIdx = Array.FindIndex(withItem, string.IsNullOrEmpty);
Check("the bar has a second free slot for a preset token", presetIdx >= 0);
if (presetIdx >= 0) withItem[presetIdx] = presetToken;
mainBar = withItem;                       // this is now the canonical main bar the relog must reproduce
await a.Hub.SendAsync("SetSkillBar", mainBar);
await a.Settle();
// SetSkillBar stores without echoing a fresh push, so we don't re-read a.Bar here — the RELOG assertion
// below (SyncSkillBar kept the item: token) is the real proof it was accepted AND persisted.

// -------------------------------------------------------------------------------------------
// 4c. STACKABLES IN THE BANK. Crafting materials used to land as one warehouse ROW PER DEPOSIT
//     (playtest-13) because deposit moved the whole InventoryItem instead of merging. Deposit the
//     same material twice and assert ONE row holding both.
// -------------------------------------------------------------------------------------------
// Quest markers must now EXIST: the character is far past the level the starter chain opens at, so
// the Armsmaster has something to offer and the server should be saying so.
Check("quest markers appear once the character is old enough to be offered a quest",
      a.Marks is not null && a.Marks.Marks.Length > 0,
      $"{a.Marks?.Marks.Length ?? 0} marks at level {a.Progress?.Level}");

string matId = Crafting.MaterialId(MaterialType.Ingot, ItemRarity.Common);
await a.Hub.SendAsync("DebugGive", matId, 5);
await a.Settle();
var matStack = a.Inv?.Items.FirstOrDefault(i => i.DefId == matId);
Check("materials arrive as ONE stacked bag row", matStack is not null && matStack.Quantity >= 5,
      $"qty {matStack?.Quantity}");
if (matStack is not null)
{
    await a.Hub.SendAsync("WarehouseDeposit", matStack.InstanceId);
    await a.Settle();
    await a.Hub.SendAsync("DebugGive", matId, 3);
    await a.Settle();
    var second = a.Inv?.Items.FirstOrDefault(i => i.DefId == matId);
    if (second is not null)
    {
        await a.Hub.SendAsync("WarehouseDeposit", second.InstanceId);
        await a.Settle();
    }
    int matRows = a.Ware?.Items.Count(i => i.DefId == matId) ?? 0;
    int matTotal = a.Ware?.Items.Where(i => i.DefId == matId).Sum(i => i.Quantity) ?? 0;
    Check("two deposits of one material MERGE into a single bank row", matRows == 1, $"rows {matRows}");
    Check("the merged bank row keeps the full quantity", matTotal >= 8, $"qty {matTotal}");
}

// -------------------------------------------------------------------------------------------
// 4d. BUFFS BEFORE THE RELOG. Buffs used to die on every logout because nothing saved them
//     (playtest-13). Take one here; the relog below proves it came back from the DB with LESS
//     time on it — full time would mean it was re-cast, and none would mean it was lost.
// -------------------------------------------------------------------------------------------
await a.Hub.SendAsync("DebugGive", ItemCatalog.SpeedPotionC, 1);
await a.Settle();
float buffSecondsBefore = 0f;
string? buffKey = null;
var buffPotion = a.Inv?.Items.FirstOrDefault(i => i.DefId == ItemCatalog.SpeedPotionC);
Check("got a buff potion", buffPotion is not null);
if (buffPotion is not null)
{
    await a.Hub.SendAsync("UsePotion", buffPotion.InstanceId);
    await a.Settle();
    var up = a.Buffs?.Buffs.FirstOrDefault(b => !b.IsDebuff && b.SecondsLeft > 0);
    Check("the potion put a timed buff up", up is not null);
    if (up is not null) { buffKey = up.Key; buffSecondsBefore = up.SecondsLeft; }
}

// -------------------------------------------------------------------------------------------
// 4e. A BUFF SCROLL IS PAID FOR (0.42.0). Every buff scroll read for free until then: the cast
//     pipeline consumed a skill's own ConsumableId, which only the Return/Resurrect scrolls
//     declare — so all 48 buff scrolls granted their hour and stayed in the bag. Invisible in a
//     playtest until you notice the stack never shrinks, which is exactly this tool's job.
//     A DIFFERENT family from 4d's potion (cast speed, not move speed) so the relog assertions
//     below still measure the potion they were written for.
// -------------------------------------------------------------------------------------------
await a.Hub.SendAsync("DebugGive", ItemCatalog.CastScrollR, 2);
await a.Settle();
int ScrollsHeld() => a.Inv?.Items.Where(i => i.DefId == ItemCatalog.CastScrollR).Sum(i => i.Quantity) ?? 0;
var scroll = a.Inv?.Items.FirstOrDefault(i => i.DefId == ItemCatalog.CastScrollR);
Check("got two buff scrolls", ScrollsHeld() == 2, $"qty {ScrollsHeld()}");
if (scroll is not null)
{
    await a.Hub.SendAsync("UsePotion", scroll.InstanceId);
    // POLL, don't sleep a fixed span. The scroll's 1s authored channel is scaled by the CASTER's
    // cast-speed multiplier, and this character is a heavy-armour tank with tank WIT — its real
    // channel is ~3.5s, so the old flat 1500ms wait ended while the cast was still running and
    // reported "the scroll is never consumed" against a server that was working correctly.
    for (int i = 0; i < 40 && ScrollsHeld() == 2; i++)
        await Task.Delay(250);
    Check("reading a buff scroll CONSUMES one", ScrollsHeld() == 1, $"{ScrollsHeld()} left");
    // Only a scroll runs an hour — the 20-minute potion from 4d can never clear 2000s, so this
    // cannot pass on the potion's square by accident.
    Check("the scroll's own hour-long buff is up",
          a.Buffs?.Buffs.Any(x => !x.IsDebuff && x.SecondsLeft > 2000f) == true);
}

// -------------------------------------------------------------------------------------------
// 5. THE REAL TEST: log out completely and log back in on a NEW connection. Everything above
//    could still be alive purely in server memory. Only a relog proves it reached the DB.
//
//    INVOKE, not Send: the server completes LeaveWorld only after the character has been SAVED, and
//    answers with a refusal reason when leaving is blocked (in combat / a DoT ticking). SendAsync
//    returns the moment the message is written and waits for neither — which is exactly the bug that
//    left the character-select screen showing the level from before the session.
// -------------------------------------------------------------------------------------------
var leaveRefusal = await a.Hub.InvokeAsync<string?>("LeaveWorld");
Check("LeaveWorld was not refused (out of combat, nothing ticking)", leaveRefusal is null, leaveRefusal);
await a.DisposeAsync();

var b = await ConnectAsync("test1", "test");
var entered2 = await b.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(charId));
Check("re-entered the world as the same character", entered2.Success, entered2.Error);
await b.Settle();

Check("both classes survived the relog", b.Subclasses?.Classes.Length == 2,
      $"got {b.Subclasses?.Classes.Length}");
// Assert by DefId, not InstanceId: a never-saved item is assigned a fresh persistent Guid on its first
// save, so its InstanceId legitimately changes across the relog. The character has exactly one potion.
Check("the warehoused potion survived the relog IN THE BANK",
      bankedId != Guid.Empty && b.Ware?.Items.Any(i => i.DefId == ItemCatalog.HealingPotion) == true);
Check("the warehoused potion did NOT leak back into the bag on relog",
      b.Inv is not null && b.Inv.Items.All(i => i.DefId != ItemCatalog.HealingPotion));
b.SystemChat.Clear();
await b.Hub.SendAsync("BlockCommand", "list", "");
await b.Settle();
Check("the block list survived the relog (BlockedCsv persisted)",
      b.SystemChat.Any(s => s.Contains("Test2")));
Check("MAIN class's bar survived the relog",
      b.Bar is not null && b.Bar.Slots.SequenceEqual(mainBar));
Check("the ITEM slot survived the relog (SyncSkillBar kept the item: token, not wiped as a skill)",
      b.Bar is not null && b.Bar.Slots.Contains(itemToken));
Check("the PRESET slot survived the relog (SyncSkillBar kept the preset: token, not wiped as a skill)",
      b.Bar is not null && b.Bar.Slots.Contains(presetToken));
Check("levels survived the relog (main 81, subclass 5)",
      b.Subclasses!.Classes.First(c => c.Slot == mainSlot).Level == 81 &&
      b.Subclasses.Classes.First(c => c.Slot == subSlot).Level == 5,
      $"main {b.Subclasses!.Classes.First(c => c.Slot == mainSlot).Level}, " +
      $"sub {b.Subclasses.Classes.First(c => c.Slot == subSlot).Level}");

// The merged material row must come back as ONE row, not split again by the save/load round trip.
Check("the merged material row survived the relog as ONE row",
      b.Ware?.Items.Count(i => i.DefId == matId) == 1,
      $"rows {b.Ware?.Items.Count(i => i.DefId == matId)}");

// BUFFS ACROSS THE RELOG (0.28.94). Not "is it still there" — a re-cast would also look like that.
// The timer must have gone DOWN, which is the only evidence the wall-clock expiry was restored rather
// than the buff being freshly applied at full duration.
if (buffKey is not null)
{
    var back = b.Buffs?.Buffs.FirstOrDefault(x => x.Key == buffKey);
    // No failure-detail here: Check prints the detail on PASS too, so "PASS … (buff was lost)" reads
    // like a contradiction.
    Check("the buff survived the relog", back is not null);
    if (back is not null)
        Check("the restored buff kept its REMAINING time (not re-cast at full duration)",
              back.SecondsLeft < buffSecondsBefore && back.SecondsLeft > 0f,
              $"{buffSecondsBefore:0.0}s before -> {back.SecondsLeft:0.0}s after");
    int copies = b.Buffs?.Buffs.Count(x => x.Key == buffKey) ?? 0;
    Check("the restored buff is applied exactly ONCE (no double-apply)", copies == 1, $"copies {copies}");
}

// CHARACTER SELECT freshness (0.28.92/0.28.95): LeaveWorld only completes after the save, so the
// character list must already show the level this session reached — and the class, which the row
// used to ignore entirely.
var afterChars = await b.Hub.InvokeAsync<CharacterList>("ListCharacters");
var mine = afterChars.Characters.First(c => c.Id == charId);
Check("character select shows THIS session's level (the save is awaited, not raced)",
      mine.Level == 81, $"listed level {mine.Level}");

// And the SUBCLASS's own bar must still be its own, after the relog.
b.Bar = null;
await b.Hub.SendAsync("SwitchSubclass", subSlot);
await b.Settle();
Check("SUBCLASS's bar survived the relog too",
      b.Bar is not null && b.Bar.Slots.SequenceEqual(subBar));
b.MyId = entered2.EntityId;

// -------------------------------------------------------------------------------------------
// 5b. CRAFTING + BLUEPRINTS. Crafting had NEVER been exercised end-to-end, which hid a static-init crash
//     (RecipeCatalog threw on first access). This proves it runs, and proves the blueprint economy:
//     1 blueprint to UNLOCK the recipe + 1 consumed per craft (so the first craft costs 2).
// -------------------------------------------------------------------------------------------
{
    const string recipeId = "craft_sword1h_t76";      // A-grade (DropOnly), success 1.0, deterministic
    var recipe = RecipeCatalog.Get(recipeId);
    Check("RecipeCatalog initialises without throwing (the static-init bug is fixed)", recipe is not null);
    if (recipe is not null)
    {
        string bpId = ItemCatalog.RecipeBookId(recipeId);
        int Count(Session s, string defId) => s.Inv?.Items.Where(i => i.DefId == defId).Sum(i => i.Quantity) ?? 0;

        await b.Hub.SendAsync("DebugSetProfession", (int)recipe.Profession);
        await b.Hub.SendAsync("DebugGive", bpId, 2);      // two blueprints: one to learn, one to craft
        await b.Settle();
        Check("got two blueprints", Count(b, bpId) == 2, $"have {Count(b, bpId)}");

        // UNLOCK: open one blueprint to learn the recipe (consumes it → 1 left).
        var oneBp = b.Inv!.Items.First(i => i.DefId == bpId);
        await b.Hub.SendAsync("OpenBox", oneBp.InstanceId);
        await b.Settle();
        Check("unlocking the recipe consumed ONE blueprint (1 of 2 left)", Count(b, bpId) == 1,
              $"have {Count(b, bpId)}");

        // Give the mats, then craft — should succeed and consume the SECOND blueprint.
        foreach (var inp in recipe.Inputs) await b.Hub.SendAsync("DebugGive", inp.ItemId, inp.Qty);
        await b.Settle();
        await b.Hub.SendAsync("Craft", recipeId);
        await b.Settle();
        Check("the craft produced the item", Count(b, recipe.OutputId) == 1, $"have {Count(b, recipe.OutputId)}");
        Check("the craft consumed the second blueprint (0 left → first craft cost 2)", Count(b, bpId) == 0,
              $"have {Count(b, bpId)}");

        // With the recipe still LEARNED and mats re-supplied but NO blueprint, the craft must be blocked.
        foreach (var inp in recipe.Inputs) await b.Hub.SendAsync("DebugGive", inp.ItemId, inp.Qty);
        await b.Settle();
        await b.Hub.SendAsync("Craft", recipeId);
        await b.Settle();
        Check("a craft with no blueprint is blocked (still just one crafted item)",
              Count(b, recipe.OutputId) == 1, $"have {Count(b, recipe.OutputId)}");
    }
}

// -------------------------------------------------------------------------------------------
// 6. ADMIN MODERATION — jail (per-char, live + persists + pins), kick (per-char lockout). These SHIP in
//    release, so they're authorized server-side by the caller's role; verify the behaviour, not the UI.
// -------------------------------------------------------------------------------------------
var gm = await ConnectAsync("admin", "admin");
var gmChars = await gm.Hub.InvokeAsync<CharacterList>("ListCharacters");
var gmEnter = await gm.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(gmChars.Characters[0].Id));
Check("admin account entered the world", gmEnter.Success, gmEnter.Error);
gm.MyId = gmEnter.EntityId;   // without this the session tracks no position and every place check reads (0,0)
await gm.Settle();

// The moderation victim is the PLAIN character, not the protagonist: the protagonist is an admin (it needs
// the admin toolbox), and moderation deliberately refuses to act on staff — an admin can neither jail nor
// re-rank an equal. The protagonist's own session is done with; log the victim in.
var bLeave = await b.Hub.InvokeAsync<string?>("LeaveWorld");
Check("the protagonist left cleanly before the moderation section", bLeave is null, bLeave);
await b.DisposeAsync();

var v = await ConnectAsync("test1", "test");
var enteredV = await v.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(victimId));
Check("the plain victim entered the world", enteredV.Success, enteredV.Error);
v.MyId = enteredV.EntityId;
await v.Settle();

// JAIL the victim live.
v.MyX = 0; v.MyY = 0;
await gm.Hub.SendAsync("AdminCommand", "jail", $"{victimName} 60");
await v.Settle();
// "In the YARD", not "on the jail coordinate": arrivals are spread across the 300x500 room now
// (owner, playtest-20 `61d`), so an exact-centre assertion would fail for the right reason.
bool atJail = WorldDomain.Jail.Contains(v.MyX, v.MyY);
Check("jailing a player teleports them to jail (live)", atJail, $"at ({v.MyX:0},{v.MyY:0})");

// The 60-min jail also DRAINED the player's charisma (−200) below the +1 they'd been liked for → off the board.
//
// POLLED, not read once. The leaderboard comes from the DATABASE, and the charisma drain reaches it via
// a background save (RunSave is fire-and-forget), so a single read races that write — this check failed
// about one run in four while otherwise being correct. A flaky assertion is as misleading as a
// non-idempotent one: it trains you to re-run instead of to look. Poll for up to ~3s and take the first
// answer that reflects the drain.
LeaderboardDto boardAfterJail = null!;
for (int attempt = 0; attempt < 10; attempt++)
{
    boardAfterJail = await v.Hub.InvokeAsync<LeaderboardDto>("RequestLeaderboard", "charisma");
    if (!boardAfterJail.Entries.Any(e => e.Name == victimName)) break;
    await Task.Delay(300);
}
Check("a jail drained the player's charisma (dropped off the board)",
      boardAfterJail.Entries.All(e => e.Name != victimName),
      string.Join(",", boardAfterJail.Entries.Select(e => e.Name)));

// Jailed → may pace around inside the YARD, but can never leave it (owner, 2026-07-20: serving a
// sentence should feel like a room, not paralysis). Walk hard at the wall and confirm we end up
// clamped inside the room rather than either frozen on the spot or out in the world.
double startX = v.MyX, startY = v.MyY;
await v.Hub.SendAsync("Move", new MoveCommand(GameConstants.JailX + 3000, GameConstants.JailY));
for (int i = 0; i < 12; i++) await v.Settle();   // give the walk time to run into the wall
double walked = Math.Sqrt(Math.Pow(v.MyX - startX, 2) + Math.Pow(v.MyY - startY, 2));
Check("a jailed player can MOVE inside the yard",
      walked > 20, $"walked {walked:0} units from ({startX:0},{startY:0})");
Check("a jailed player can NOT walk out of the yard",
      WorldDomain.Jail.Contains(v.MyX, v.MyY),
      $"ended at ({v.MyX:0},{v.MyY:0}); the yard is x[{WorldDomain.Jail.MinX:0},{WorldDomain.Jail.MaxX:0}] " +
      $"y[{WorldDomain.Jail.MinY:0},{WorldDomain.Jail.MaxY:0}]");

// JAIL PERSISTS across a relog: leave, come back, still in jail.
await v.Hub.SendAsync("LeaveWorld");
await Task.Delay(600);
await v.DisposeAsync();
var c = await ConnectAsync("test1", "test");
var enteredC = await c.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(victimId));
c.MyId = enteredC.EntityId;
await c.Settle();
Check("jail SURVIVES a relog (spawns back in jail)",
      WorldDomain.Jail.Contains(c.MyX, c.MyY),
      $"spawned at ({c.MyX:0},{c.MyY:0})");

// RELEASE sends you to the STARTING town, never the nearest one — the jail's location has to stay
// secret, and "nearest" is a map hint (owner, 2026-07-20).
await gm.Hub.SendAsync("AdminCommand", "unjail", victimName);
for (int i = 0; i < 6; i++) await c.Settle();
var startTown = WorldMap.StartingTown;
Check("release from jail teleports to the STARTING town (not the nearest)",
      Math.Abs(c.MyX - startTown.X) < 400 && Math.Abs(c.MyY - startTown.Y) < 400,
      $"released at ({c.MyX:0},{c.MyY:0}), starting town is ({startTown.X:0},{startTown.Y:0})");

// ADMINS ARE IMMUNE. `/jail admin` used to jail the OWNER in their own jail.
gm.SystemChat.Clear();
await gm.Hub.SendAsync("AdminCommand", "jail", "Admin 60");
await gm.Settle();
Check("an admin can't jail themselves (or any other admin)",
      !gm.SystemChat.Any(s => s.Contains("jailed for")),
      string.Join(" | ", gm.SystemChat));

// Case-INSENSITIVE lookup: the action and the message must agree. `/jail test1` on "Test1" used to
// jail them for real and then report "No character 'test1'" — the online lookup ignored case, the
// database lookup did not.
gm.SystemChat.Clear();
await gm.Hub.SendAsync("AdminCommand", "jail", $"{victimName.ToLowerInvariant()} 60");
await gm.Settle();
Check("a lower-case name resolves, and does NOT report 'no character'",
      gm.SystemChat.Any(s => s.Contains("jailed for")) &&
      !gm.SystemChat.Any(s => s.Contains("No character")),
      string.Join(" | ", gm.SystemChat));
await gm.Hub.SendAsync("AdminCommand", "unjail", victimName);
await Task.Delay(300);

// KICK must remove the entity SERVER-SIDE, without the kicked client's cooperation.
//
// The smoke client is a raw connection: it ignores ForceDisconnect and never calls LeaveWorld. That
// is exactly the case that used to break — the server only asked the client to leave, so the entity
// stayed behind as a GHOST (targetable, killable, and still holding the name), and the account was
// then refused re-entry with "character is already online". So re-entering here WITHOUT leaving
// first is the whole test: the error must be the kick lockout, never "already online".
await gm.Hub.SendAsync("AdminCommand", "kick", $"{victimName} 60");
await Task.Delay(600);
var d = await ConnectAsync("test1", "test");
var enteredD = await d.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(victimId));
Check("a KICKED character can't re-enter until the lockout passes",
      !enteredD.Success && (enteredD.Error?.Contains("locked") ?? false), enteredD.Error);
Check("kick leaves NO ghost entity behind (re-entry is blocked by the kick, not by 'already online')",
      !(enteredD.Error?.Contains("already online") ?? false), enteredD.Error);
await d.DisposeAsync();
await c.DisposeAsync();

// -------------------------------------------------------------------------------------------
// 7. THE GATEKEEPER, END TO END — a named field gate, over the wire.
// -------------------------------------------------------------------------------------------
// The catalog checks above prove the gates EXIST and are well-formed. They cannot prove the wire path
// works, and that path is new in three places at once: the dialog now carries this city's own field
// gates, the destination id is a GATE id rather than a safe-zone id, and the handler has to reject a gate
// belonging to a different city. Travel is also the kind of thing that "works" while silently landing you
// somewhere else, which no amount of catalog assertion catches.
{
    var pell = WorldMap.NpcById("gatekeeper_brackenford")!;
    await gm.Hub.SendAsync("DebugTeleport", pell.X, pell.Y - 40f);   // within TalkRange
    await gm.Hub.SendAsync("DebugGold", 200_000L);
    await gm.Settle();

    // ⚠ Match on the PERSONAL name, not the catalog name. Since 0.55.0 ("NPCs wear their role") the
    // server splits "Gatekeeper Pell" into Name="Pell" + Title="Gatekeeper", so a comparison against
    // the full catalog name never matched and this section failed — then threw at the `First` below
    // and took the whole rest of the run with it. The catalog deliberately keeps the full name.
    var (_, pellPersonal) = TitleCatalog.SplitNpcName(pell.Name);
    var pellId = gm.EntityNames.FirstOrDefault(kv => kv.Value == pellPersonal).Key;
    Check("the Brackenford gatekeeper is visible after teleporting to him", pellId != Guid.Empty, pellPersonal);

    gm.Dialog = null;
    await gm.Hub.SendAsync("TalkToNpc", pellId);
    await gm.Settle();

    var menu = gm.Dialog?.Teleport?.Destinations ?? Array.Empty<TeleportDest>();
    // Brackenford owns Bracken Hollow + Bracken Downs = 4 camps = 4 gates, then the other cities.
    var local = menu.Where(t => t.Group.Length > 0).ToArray();
    Check("the gatekeeper lists its OWN city's field gates", local.Length == 4,
          $"{local.Length} local gates of {menu.Length} destinations");
    Check("...grouped under their field, with the band and roster in the description",
          local.Any(t => t.Group == "Bracken Hollow" && t.Description.Contains("Lv 1-4")),
          local.FirstOrDefault(t => t.Group == "Bracken Hollow")?.Description);
    Check("...and the other cities are still offered (Group empty)",
          menu.Any(t => t.Group.Length == 0 && t.DestId == "town_frostmere"));

    // Travel to the level 1-4 gate and land ON it. This is the assertion that the "random teleport
    // factor" is gone: one named gate, one landing spot (±150 of scatter).
    var target = local.First(t => t.Group == "Bracken Hollow" && t.Description.Contains("Lv 1-4"));
    var gate = RegionMap.GateById(target.DestId)!.Value.Gate;
    long goldBefore = gm.Gold;
    await gm.Hub.SendAsync("Teleport", pellId, target.DestId);
    await gm.Settle();

    double off = Math.Sqrt(Math.Pow(gm.MyX - gate.At.X, 2) + Math.Pow(gm.MyY - gate.At.Y, 2));
    Check("teleporting to a named gate lands you AT that gate", off < 250,
          $"{off:0} from ({gate.At.X:0},{gate.At.Y:0})");
    Check("...and charged the fee", gm.Gold == goldBefore - target.Fee,
          $"{goldBefore} -> {gm.Gold}, fee {target.Fee}");
    Check("...and the gate is inside the field it belongs to",
          RegionMap.At(gm.MyX, gm.MyY)?.Id == "field_bracken_hollow",
          RegionMap.At(gm.MyX, gm.MyY)?.Name);

    // A gate belonging to a DIFFERENT city must be refused — the gatekeeper knows its own grounds and the
    // roads out, nothing further. Without this the id becomes a free warp anywhere in the world.
    await gm.Hub.SendAsync("DebugTeleport", pell.X, pell.Y - 40f);
    await gm.Settle();
    var foreign = RegionMap.FieldsOf("town_frostmere")[0].Gates[0];
    long goldBeforeDenied = gm.Gold;
    await gm.Hub.SendAsync("Teleport", pellId, foreign.Id);
    await gm.Settle();
    Check("a gate in ANOTHER city's field is refused (no free warp across the world)",
          gm.Gold == goldBeforeDenied
            && Math.Abs(gm.MyX - pell.X) < 400 && Math.Abs(gm.MyY - pell.Y) < 400,
          $"at ({gm.MyX:0},{gm.MyY:0}), gold {gm.Gold}");
}

// -------------------------------------------------------------------------------------------
// 8. `/give` AND THE PER-INSTANCE TAGS (`58d`) — a REAL item carrying tags, not a cloned def.
// -------------------------------------------------------------------------------------------
// The failure this guards is invisible in play: a bound item that persists as an ORDINARY one looks
// perfectly right until the next login, when it can suddenly be sold or banked. That is the same shape
// as the skill-bar corruption this tool was built for, so the relog is the assertion that matters — and
// the Rune of Sinners, whose entire point is that you cannot get rid of it, rides on exactly this.
{
    var gmCharId = gmChars.Characters[0].Id;

    // His own worked example, with both storage flags: unsellable, untradable, one day, renamed, +5.
    await gm.Hub.SendAsync("AdminCommand", "give",
        $"{gmChars.Characters[0].Name} {ItemCatalog.WarRune} -1 0 1d \"Soulbound Rune\" 5 0 0");
    await gm.Settle();

    var tagged = (gm.Inv?.Items ?? Array.Empty<InventoryItemDto>())
        .FirstOrDefault(i => i.CustomName == "Soulbound Rune");
    Check("/give spawned a tagged instance with the written name (`58d`)", tagged is not null,
          string.Join(",", (gm.Inv?.Items ?? Array.Empty<InventoryItemDto>()).Select(i => i.CustomName ?? i.DefId)));

    if (tagged is not null)
    {
        Check("...carrying its own sell price, tradability and storage rules",
              tagged.SellPriceOverride == -1 && tagged.TradableOverride == false
              && tagged.CanStorePrivate == false && tagged.CanStoreAccount == false,
              $"sell {tagged.SellPriceOverride} trade {tagged.TradableOverride} priv {tagged.CanStorePrivate} acct {tagged.CanStoreAccount}");
        Check("...and the enchant and the clock it was given", tagged.Enchant == 5 && tagged.ExpiresAtUtc is not null,
              $"+{tagged.Enchant}, expires {tagged.ExpiresAtUtc}");

        var def = ItemCatalog.Get(tagged.DefId)!;
        Check("...so it reads as (temporary, bound)", ItemTag.For(def, tagged) == "(temporary, bound)",
              ItemTag.For(def, tagged));

        // The keeper must refuse it — the private warehouse had NO instance gate before `58d`.
        await gm.Hub.SendAsync("WarehouseDeposit", tagged.InstanceId);
        await gm.Settle();
        Check("the private keeper refuses an item bound to your soul",
              (gm.Inv?.Items ?? Array.Empty<InventoryItemDto>()).Any(i => i.InstanceId == tagged.InstanceId),
              "it went into the bank anyway");
    }

    // THE ONE THAT MATTERS: does the tag survive being written to SQLite and read back?
    var gmLeave = await gm.Hub.InvokeAsync<string?>("LeaveWorld");
    Check("the admin left cleanly (so the save is awaited, not raced)", gmLeave is null, gmLeave);
    await gm.DisposeAsync();

    gm = await ConnectAsync("admin", "admin");
    var gmBack = await gm.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(gmCharId));
    Check("the admin re-entered the world", gmBack.Success, gmBack.Error);
    gm.MyId = gmBack.EntityId;
    await gm.Settle();

    var after = (gm.Inv?.Items ?? Array.Empty<InventoryItemDto>())
        .FirstOrDefault(i => i.CustomName == "Soulbound Rune");
    Check("🔑 the per-instance tags SURVIVED THE RELOG (`58d` persists)",
          after is not null && after.SellPriceOverride == -1 && after.TradableOverride == false
          && after.CanStorePrivate == false && after.CanStoreAccount == false && after.Enchant == 5,
          after is null ? "the item came back untagged or not at all"
                        : $"sell {after.SellPriceOverride} trade {after.TradableOverride} priv {after.CanStorePrivate} acct {after.CanStoreAccount} +{after.Enchant}");
}

// -------------------------------------------------------------------------------------------
// 9. THE TRAINING DUMMIES THAT HIT BACK (`56c` / `63h`) — do they actually strike?
// -------------------------------------------------------------------------------------------
// They shipped in 0.58.x and did NOTHING for two builds, and the owner could only report *"both
// dummies act as the old"* — which is exactly the shape of bug this tool exists for. Two causes, both
// invisible without standing there: the strike radius was 50 while a melee attacker is walked to
// MeleeRange (80) and STOPS, so nobody was ever inside it; and every dummy was hard-named "Training
// Dummy (Lv N)", so the three of them were indistinguishable plates in a row.
//
// So: teleport ONTO the magic dummy, hold still, and count the combat events on the wire. A dummy
// that does not reach you produces zero, which is precisely what could not be seen before.
{
    var magicZone = WorldMap.SpawnZones.First(z => z.MobTypes.Contains("dummy_magic"));
    var physZone  = WorldMap.SpawnZones.First(z => z.MobTypes.Contains("dummy_physical"));

    await gm.Hub.SendAsync("DebugTeleport", magicZone.X, magicZone.Y);
    await gm.Settle();

    var magic = gm.EntityNames.FirstOrDefault(kv => kv.Value.StartsWith("Magic Training Dummy"));
    Check("the MAGIC dummy keeps its own name (not the generic 'Training Dummy')",
          magic.Key != Guid.Empty, string.Join(" / ", gm.EntityNames.Values.Where(n => n.Contains("Dummy"))));
    if (magic.Key != Guid.Empty)
        Check("...and wears the title 'Magic' (`63h`)", gm.EntityTitles[magic.Key] == "Magic",
              $"title '{gm.EntityTitles.GetValueOrDefault(magic.Key)}'");

    // Stand still inside the strike radius for a couple of seconds: 10 ticks/s, one strike per tick.
    // ⚠ Teleport onto the DUMMY, not the zone centre — a zone places its mob anywhere inside its
    // 200-unit radius, which is wider than the strike radius on purpose.
    if (magic.Key != Guid.Empty)
    {
        var at = gm.EntityPos[magic.Key];
        await gm.Hub.SendAsync("DebugTeleport", at.X, at.Y);
        await gm.Settle();
    }
    gm.Combat.Clear();
    await Task.Delay(2000);
    var onMe = gm.Combat.Where(c => c.TargetId == gm.MyId && c.Skill == "Practice Bolt").ToList();
    Check("the MAGIC dummy actually strikes someone standing on it (`63h`)", onMe.Count > 5,
          $"{onMe.Count} strikes in 2s (expected ~20)");
    // The whole point of the instrument: the OUTCOME is resolved, not a flat number. Over ~20 samples
    // a fail or a crit may or may not appear, so only assert that a real outcome came through.
    Check("...through the real magic resolution (Hit / Fail / Crit, never Miss)",
          onMe.Count == 0 || onMe.All(c => c.Outcome != CombatOutcome.Miss),
          string.Join(",", onMe.Select(c => c.Outcome).Distinct()));

    // The physical one is a separate template, a separate resolver and a separate title.
    await gm.Hub.SendAsync("DebugTeleport", physZone.X, physZone.Y);
    await gm.Settle();
    var phys = gm.EntityNames.FirstOrDefault(kv => kv.Value.StartsWith("Striking Training Dummy"));
    Check("the PHYSICAL dummy is its own creature, titled 'Physical' (`63h`)",
          phys.Key != Guid.Empty && gm.EntityTitles.GetValueOrDefault(phys.Key) == "Physical",
          $"'{phys.Value}' title '{gm.EntityTitles.GetValueOrDefault(phys.Key)}'");

    if (phys.Key != Guid.Empty)
    {
        var at = gm.EntityPos[phys.Key];
        await gm.Hub.SendAsync("DebugTeleport", at.X, at.Y);
        await gm.Settle();
    }
    gm.Combat.Clear();
    await Task.Delay(2000);
    int hits = gm.Combat.Count(c => c.TargetId == gm.MyId && c.Skill == "Practice Strike");
    Check("the PHYSICAL dummy actually strikes back", hits > 5, $"{hits} strikes in 2s");

    // And a step OUT ends it — the short radius is the design ("you have to choose to stand in it"),
    // and a dummy that reaches across the training ground would be its own bug.
    await gm.Hub.SendAsync("DebugTeleport", physZone.X + 1200f, physZone.Y);
    await gm.Settle();
    gm.Combat.Clear();
    await Task.Delay(1000);
    Check("...and stops the moment you step out of range",
          gm.Combat.Count(c => c.TargetId == gm.MyId) == 0,
          $"{gm.Combat.Count(c => c.TargetId == gm.MyId)} strikes at 1200 units");

    // ⚠ Standing in a dummy sets the combat flag, which decays over 30s (CombatDecayTicks), so the
    // LeaveWorld below prints "You can't leave while in combat" and the run ends on a disconnect
    // instead. That is CORRECT behaviour being observed, not a failure — and waiting it out would add
    // half a minute to every smoke run for nothing. Every character here is a fresh throwaway.
}

// (No cleanup needed — every run creates a fresh Smoke<timestamp> character, so the jailed/kicked
//  throwaway char is never reused.)
await gm.Hub.SendAsync("LeaveWorld");
await Task.Delay(300);
await gm.DisposeAsync();

return Finish();

int Finish()
{
    Console.WriteLine();
    if (failures == 0)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("ALL CHECKS PASSED");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{failures} CHECK(S) FAILED");
    }
    Console.ResetColor();
    Console.WriteLine();
    return failures == 0 ? 0 : 1;
}

/// <summary>One connected client. Captures the server pushes we assert on.</summary>
sealed class Session : IAsyncDisposable
{
    public HubConnection Hub { get; private set; } = null!;
    public SkillBarDto? Bar;
    public SubclassListDto? Subclasses;
    public InventoryUpdate? Inv;
    public WarehouseUpdate? Ware;

    /// <summary>The last "Learned" push. The test needs it to lay the bar out ITSELF now that the
    /// server no longer auto-places skills.</summary>
    public LearnedSkills? Learned;

    // Delta-snapshot capture — the live world push. Accumulated so a test can assert an entity was
    // SPAWNED (full), UPDATED (lean), or DESPAWNED, and reset the tallies between phases.
    public readonly HashSet<Guid> Spawned = new();
    public readonly HashSet<Guid> Updated = new();
    public readonly HashSet<Guid> Despawned = new();
    public int DeltaCount;
    public void ResetDeltas() { Spawned.Clear(); Updated.Clear(); Despawned.Clear(); DeltaCount = 0; }

    // Self position, tracked from delta spawns + lean updates (for jail-teleport checks). MyId is set by
    // the test after EnterWorld.
    public Guid MyId;
    public float MyX, MyY;

    /// <summary>Entity id → name, from full spawns. The only way to address an NPC over the protocol is by
    /// its runtime Guid, and a test that wants "the Brackenford gatekeeper" has nothing else to go on.</summary>
    public readonly Dictionary<Guid, string> EntityNames = new();

    /// <summary>Entity id → its TITLE line ("Gatekeeper", "Elite", "Field Boss", …), from full spawns.
    /// A title is a STATIC field of the spawn DTO, so it only ever arrives on a full spawn.</summary>
    public readonly Dictionary<Guid, string> EntityTitles = new();

    /// <summary>Entity id → where it spawned. A zone places its mob anywhere inside its radius, so
    /// "teleport to the zone centre" is not the same as "stand next to the thing".</summary>
    public readonly Dictionary<Guid, (float X, float Y)> EntityPos = new();

    /// <summary>Every combat event the server sent us. The training dummies are the only thing in the
    /// game whose entire purpose is to PRODUCE these at a known rate, and "it does nothing" was
    /// invisible in a playtest for two builds — so counting them on the wire is the only honest test.</summary>
    public readonly List<CombatEvent> Combat = new();

    /// <summary>The last "Dialog" push — what an NPC offered when talked to.</summary>
    public NpcDialog? Dialog;

    /// <summary>The last "Gold" push. Needed to assert a teleport actually CHARGED, and to know whether a
    /// fee is affordable before asserting the travel succeeded.</summary>
    public long Gold;

    // System-chat lines captured (for friend-list / "back online" assertions).
    public readonly List<string> SystemChat = new();

    /// <summary>Every chat message received (all channels) — for whisper / block assertions.</summary>
    public readonly List<ChatMessage> AllChat = new();

    /// <summary>The last Progress push (level / exp / exp-to-next). The exp CURVE is the thing this
    /// captures: a wrong curve looks perfectly fine on screen and is only visible as the wrong
    /// exp-to-next arriving on the wire.</summary>
    public ProgressUpdate? Progress;

    /// <summary>The last "Buffs" push. Buffs are pushed CONDITIONALLY (about once a second while any
    /// are running), which is exactly why they need asserting on the wire: a buff bar can look right
    /// while the server holds something else entirely.</summary>
    public BuffUpdate? Buffs;

    /// <summary>The last "QuestMarks" push — which NPCs have a marker for this character.</summary>
    public QuestMarks? Marks;

    public async Task OpenAsync(string url)
    {
        Hub = new HubConnectionBuilder().WithUrl(url).Build();
        Hub.On<SkillBarDto>("SkillBar", b => Bar = b);
        Hub.On<InventoryUpdate>("Inventory", i => Inv = i);
        Hub.On<WarehouseUpdate>("Warehouse", w => Ware = w);
        Hub.On<LearnedSkills>("Learned", l => Learned = l);
        Hub.On<SubclassListDto>("Subclasses", s => Subclasses = s);
        Hub.On<SnapshotDelta>("SnapshotDelta", d =>
        {
            DeltaCount++;
            foreach (var s in d.Spawns) { Spawned.Add(s.Id); EntityNames[s.Id] = s.Name; EntityTitles[s.Id] = s.Title ?? ""; EntityPos[s.Id] = (s.X, s.Y); if (s.Id == MyId) { MyX = s.X; MyY = s.Y; } }
            foreach (var u in d.Updates) { Updated.Add(u.Id); if (u.Id == MyId) { MyX = u.X; MyY = u.Y; } }
            foreach (var id in d.Despawns) Despawned.Add(id);
        });
        Hub.On<ProgressUpdate>("Progress", p => Progress = p);
        Hub.On<BuffUpdate>("Buffs", b => Buffs = b);
        Hub.On<QuestMarks>("QuestMarks", m => Marks = m);
        Hub.On<NpcDialog>("Dialog", d => Dialog = d);
        Hub.On<GoldUpdate>("Gold", g => Gold = g.Gold);
        Hub.On<CombatEvent>("Combat", c => Combat.Add(c));
        Hub.On<ChatMessage>("Chat", m => { AllChat.Add(m); if (m.Channel == ChatChannel.System) { SystemChat.Add(m.Text); Console.WriteLine($"        [SYSTEM] {m.Text}"); } });
        await Hub.StartAsync();
    }

    /// <summary>Let the server tick and its pushes arrive. The game loop runs at 10 Hz and commands
    /// are drained on the tick, so a couple of hundred ms is several ticks' worth of headroom.</summary>
    public Task Settle() => Task.Delay(500);

    public async ValueTask DisposeAsync() => await Hub.DisposeAsync();
}
