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

// `BL-300`: a fresh character stands in town, out of combat, so the Blessing is PAUSED and the sheet says so.
Check("the Blessing is paused in town (BL-300)", await a.WaitFor(() => a.Favor?.BlessingPaused == true, 3000),
      a.Favor is null ? "no Favor push" : $"paused={a.Favor.BlessingPaused}");

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
// 1a-1a. `BL-272` part 2 (0.202.0): THE SHOPS. The T52 essence shop (one NPC in Greymarsh, essence only,
//     ¼ of the price in Cobalt + ¾ in Darksteel), the temporary 2-hour Common boxes at the Common price,
//     and the temp pieces' rules (untradeable, unsellable, unbreakable, 2 h of WEARING).
// -------------------------------------------------------------------------------------------
{
    var essShop = ShopCatalog.Get(ShopCatalog.EssenceMerchant);
    var assayer = WorldMap.NpcById(ShopCatalog.EssenceMerchant);
    Check("the essence shop is ONE NPC, in Greymarsh, essence-only, stocking every T52 Mythic slot (8 weapons + 3 bodies + helm/gloves/boots/shield + 3 jewels)",
          essShop is { EssenceOnly: true } && essShop.ItemIds.Length == 18
          && WorldMap.Npcs.Count(n => n.Id.StartsWith(ShopCatalog.EssenceMerchant)) == 1
          && assayer is not null && Towns.All.First(t => t.Id == "town_greymarsh") is var gmTown
          && (assayer.X - gmTown.X) * (assayer.X - gmTown.X) + (assayer.Y - gmTown.Y) * (assayer.Y - gmTown.Y) < gmTown.Radius * gmTown.Radius,
          $"{essShop?.ItemIds.Length} rows");
    Check("a T52 2H costs 750 Cobalt + 6750 Darksteel essence; a ring 63 + 563",
          Crafting.EssenceShopPrice(ItemCatalog.Get("sword2h_t52")) is [{ EssenceId: "essence_c", Qty: 750 }, { EssenceId: "essence_d", Qty: 6750 }]
          && Crafting.EssenceShopPrice(ItemCatalog.Get("ring_t52")) is [{ Qty: 63 }, { Qty: 563 }]);
    Check("the essence price IS ¼ buy ÷ 4500 in C + ¾ buy ÷ 1500 in D (within rounding) on every row",
          essShop!.ItemIds.All(id => ItemCatalog.Get(id) is ItemDef d && Crafting.EssenceShopPrice(d) is [var c, var dk]
              && Math.Abs(c.Qty - 0.25 * ItemCatalog.BuyPrice(d) / 4500) <= 0.51
              && Math.Abs(dk.Qty - 0.75 * ItemCatalog.BuyPrice(d) / 1500) <= 0.51));
    Check("no T40/T61/Common piece has an essence price",
          Crafting.EssenceShopPrice(ItemCatalog.Get("sword2h_t40")) is null
          && Crafting.EssenceShopPrice(ItemCatalog.Get("sword2h_t61")) is null
          && Crafting.EssenceShopPrice(ItemCatalog.Get("sword2h_t52_common")) is null);

    var wBox = ItemCatalog.Get(ItemCatalog.TempWeaponBoxId(40));
    var aBox = ItemCatalog.Get(ItemCatalog.TempArmorBoxId(40));
    Check("the temp boxes cost the COMMON price: weapon T40 214,286 / T52 675,000, armour T40 357,143 / T52 1,125,000",
          ItemCatalog.BuyPrice(wBox!) == 214_286 && ItemCatalog.BuyPrice(aBox!) == 357_143
          && ItemCatalog.BuyPrice(ItemCatalog.Get(ItemCatalog.TempWeaponBoxId(52))!) == 675_000
          && ItemCatalog.BuyPrice(ItemCatalog.Get(ItemCatalog.TempArmorBoxId(52))!) == 1_125_000,
          $"{ItemCatalog.BuyPrice(wBox!)} / {ItemCatalog.BuyPrice(aBox!)}");
    Check("the weapon boxes are on every Armsmaster, the armour boxes on every Outfitter",
          ShopCatalog.Sells("merchant_gear_greymarsh", ItemCatalog.TempWeaponBoxId(52))
          && ShopCatalog.Sells(ShopCatalog.GearMerchant, ItemCatalog.TempWeaponBoxId(40))
          && ShopCatalog.Sells("merchant_armor_frostmere", ItemCatalog.TempArmorBoxId(40)));
    var setBox = BoxCatalog.Get(ItemCatalog.TempSetBoxId("robe", 40));
    Check("every temp armour set (robe too) holds body + helm + gloves + boots + SHIELD, all guaranteed",
          ItemCatalog.TempArmorWeights.All(w => BoxCatalog.Get(ItemCatalog.TempSetBoxId(w, 52)) is { PickCount: 0 } b
              && b.Entries.Length == 5 && b.Entries.All(e => e.Chance >= 1f)
              && b.Entries.Any(e => e.ItemId == ItemCatalog.TempId("shield_t52")))
          && setBox!.Entries.Any(e => e.ItemId == ItemCatalog.TempId("robe_t40")));
    Check("the weapon box is pick-ONE of 8, the armour box pick-ONE of the 3 set boxes",
          BoxCatalog.Get(ItemCatalog.TempWeaponBoxId(40)) is { PickCount: 1, Entries.Length: 8 }
          && BoxCatalog.Get(ItemCatalog.TempArmorBoxId(40)) is { PickCount: 1, Entries.Length: 3 });
    var temp = ItemCatalog.Get(ItemCatalog.TempId("sword2h_t40"));
    var com = ItemCatalog.Get("sword2h_t40_common");
    Check("a temp piece: Common stats, 2 h worn, untradeable, unsellable, unbuyable, unbreakable",
          temp is { Rarity: ItemRarity.Common, WornLifetimeSeconds: 7200, Tradable: false, IsStackable: false }
          && temp.AtkBonus == com!.AtkBonus && !ItemCatalog.IsSellable(temp) && ItemCatalog.BuyPrice(temp) < 0
          && Crafting.BreakYield(temp) is null);
    Check("no temporary jewellery exists",
          ItemCatalog.Get(ItemCatalog.TempId("ring_t40")) is null && ItemCatalog.Get(ItemCatalog.TempId("necklace_t52")) is null);
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

    // `BL-272` (0.199.0): equipment is COMMON + MYTHIC. A Common exists at T40-T61 only, carries the
    // Mythic piece's stats, and is unmodifiable (no set, no attribute; the enchant gate is server-side).
    foreach (int lvl in new[] { 40, 52, 61 })
    {
        var m = ItemCatalog.Get($"sword1h_t{lvl}");
        var cm = ItemCatalog.Get($"sword1h_t{lvl}_common");
        Check($"T{lvl} has a Common with the Mythic's stats, no set, no attributes",
              cm is { Rarity: ItemRarity.Common, SetId: "", NoAttributes: true } && m is not null
                && cm.AtkBonus == m.AtkBonus && cm.MAtkBonus == m.MAtkBonus && ItemCatalog.IsCommonGear(cm),
              $"{cm?.AtkBonus} vs {m?.AtkBonus}");
    }
    Check("no Common outside T40-T61 (T20, T76, S)",
          ItemCatalog.Get("sword1h_t20_common") is null && ItemCatalog.Get("sword1h_t76_common") is null
          && ItemCatalog.Get($"sword1h_t{ItemCatalog.SGradeLevel}_common") is null);
    int midRungs = ItemCatalog.AllItems.Count(d => Crafting.IsGearSlot(d.Slot)
        && d.Rarity is ItemRarity.Uncommon or ItemRarity.Rare or ItemRarity.Epic or ItemRarity.Legendary);
    Check("no Uncommon/Rare/Epic/Legendary EQUIPMENT exists", midRungs == 0, $"{midRungs} found");
    Check("a Mythic piece of gear shows NO rarity word, a Common says 'Common', a potion keeps its own",
          ItemCatalog.RarityLabel(aSword!) == "" && ItemCatalog.RarityLabel(ItemCatalog.Get("sword1h_t40_common")!) == "Common"
          && ItemCatalog.RarityLabel(ItemCatalog.Get(ItemCatalog.HealingPotion)!) != "");

    // The drop groups, read off the same tables the kill roll uses.
    float GroupSum(IEnumerable<DropEntry> rows, int g) => rows.Where(r => r.GroupId == g).Sum(r => r.Chance);
    // `BL-287` (0.201.0): each SLOT rolls its own Common chance (ring > ear/boots/gloves > helm/shield/neck >
    // body > weapon), T40 2%..1% (sum 14%), T52 1%..0.2% (5.8%), T61 0.3%..0.05% (1.7%); an elite x2.
    float SlotSum(int L, MobRank r, Func<string, bool> key) => MobCatalog.GearDrops(L, r)
        .Where(e => e.GroupId == MobCatalog.GroupCommonGear && key(e.ItemId)).Sum(e => e.Chance);
    Check("a normal T40 / T52 / T61 kill rolls Commons at 14% / 5.8% / 1.7% in total, an elite x2",
          Math.Abs(GroupSum(MobCatalog.GearDrops(45, MobRank.Normal), MobCatalog.GroupCommonGear) - 0.14f) < 1e-5
          && Math.Abs(GroupSum(MobCatalog.GearDrops(56, MobRank.Normal), MobCatalog.GroupCommonGear) - 0.058f) < 1e-5
          && Math.Abs(GroupSum(MobCatalog.GearDrops(68, MobRank.Normal), MobCatalog.GroupCommonGear) - 0.017f) < 1e-5
          && Math.Abs(GroupSum(MobCatalog.GearDrops(45, MobRank.Elite), MobCatalog.GroupCommonGear) - 0.28f) < 1e-5);
    Check("per slot at T40: ring 2%, boots 1.75%, helm 1.5%, body 1.25% (3 weights), weapon 1% (8 lines)",
          Math.Abs(SlotSum(45, MobRank.Normal, id => id.StartsWith("ring_")) - 0.02f) < 1e-6
          && Math.Abs(SlotSum(45, MobRank.Normal, id => id.StartsWith("boots_")) - 0.0175f) < 1e-6
          && Math.Abs(SlotSum(45, MobRank.Normal, id => id.StartsWith("helm_")) - 0.015f) < 1e-6
          && Math.Abs(SlotSum(45, MobRank.Normal, id => id.StartsWith("heavy_") || id.StartsWith("light_") || id.StartsWith("robe_")) - 0.0125f) < 1e-6
          && Math.Abs(SlotSum(45, MobRank.Normal, id => ItemCatalog.Get(id)?.Slot == EquipSlot.Weapon) - 0.01f) < 1e-6);
    Check("every Common drop id is a real item",
          new[] { 45, 56, 68 }.All(L => MobCatalog.GearDrops(L, MobRank.Normal).All(e => ItemCatalog.Get(e.ItemId) is not null)));
    Check("no healing potion drops from a mob above level 40 (BL-287)",
          MobCatalog.Templates.Where(m => m.Level > MobCatalog.HealingPotionDropMaxLevel)
              .All(m => (m.Drops ?? Array.Empty<DropEntry>()).All(d => ItemCatalog.Get(d.ItemId) is not ItemDef p || !ItemCatalog.IsHealPotion(p))));
    Check("a normal mob below T40 or from T76 up drops NO equipment",
          !MobCatalog.GearDrops(30, MobRank.Normal).Any() && !MobCatalog.GearDrops(78, MobRank.Normal).Any());
    Check("a boss pays ONE Mythic piece, 100% at T40 down to 70% at T80 (BL-308)",
          new[] { (10, 1f), (30, 1f), (45, 1f), (55, 0.9f), (65, 0.85f), (78, 0.75f), (85, 0.7f) }.All(p =>
              Math.Abs(GroupSum(MobCatalog.GearDrops(p.Item1, MobRank.Boss), MobCatalog.GroupBossGear) - p.Item2) < 1e-5)
          && new[] { 10, 30, 45, 78, 85 }.All(L =>
              MobCatalog.GearDrops(L, MobRank.Boss).All(r => ItemCatalog.Get(r.ItemId) is { Rarity: ItemRarity.Mythic })));

    // `BL-287` (0.201.0): prices + essence, IG-shaped. Mythic T1 x2.2 / T20 x0.65 / T40-T52 x0.5, T61+ as
    // they were; a Common is 0.05 of its Mythic; everything sells for half; break = 0.4 x buy / essence sell.
    Check("Mythic 2H prices: T1 188,571 / T20 1,392,857 / T40 4,285,714 / T52 13.5M / T61 60M / T80 600M",
          ItemCatalog.BuyPrice(ItemCatalog.Get($"sword2h_t{ItemCatalog.FGradeLevel}")!) == 188_571
          && ItemCatalog.BuyPrice(ItemCatalog.Get("sword2h_t20")!) == 1_392_857
          && ItemCatalog.BuyPrice(ItemCatalog.Get("sword2h_t40")!) == 4_285_714
          && ItemCatalog.BuyPrice(ItemCatalog.Get("sword2h_t52")!) == 13_500_000
          && ItemCatalog.BuyPrice(ItemCatalog.Get("sword2h_t61")!) == 60_000_000
          && ItemCatalog.BuyPrice(ItemCatalog.Get($"sword2h_t{ItemCatalog.SGradeLevel}")!) == 600_000_000,
          $"T40 {ItemCatalog.BuyPrice(ItemCatalog.Get("sword2h_t40")!)}");
    Check("a T40 Common 2H is worth 214,286 and sells for 107,143; its Mythic sells for 2,142,857",
          ItemCatalog.Get("sword2h_t40_common")!.Value == 214_286
          && ItemCatalog.SellPrice(ItemCatalog.Get("sword2h_t40_common")!) == 107_143
          && ItemCatalog.SellPrice(ItemCatalog.Get("sword2h_t40")!) == 2_142_857,
          $"common {ItemCatalog.Get("sword2h_t40_common")!.Value} sells {ItemCatalog.SellPrice(ItemCatalog.Get("sword2h_t40_common")!)}");
    Check("a healing potion and a material sell for half their value; a buff potion still sells for 0",
          ItemCatalog.Get(ItemCatalog.HealingPotion) is ItemDef hp && ItemCatalog.SellPrice(hp) == Math.Max(1, hp.Value / 2)
          && ItemCatalog.Get(Crafting.MaterialId(MaterialType.Wood)) is ItemDef wood
          && ItemCatalog.SellPrice(wood) == Math.Max(1, wood.Value / 2)
          && ItemCatalog.AllItems.Where(d => d.SellPriceOverride == 0).All(d => ItemCatalog.SellPrice(d) == 0));

    // Breaking gives the GRADE's essence from an AUTHORED table (literals, generated once); T1/T20 cannot be broken.
    Check("a T40 2H Mythic breaks for 1143 Darksteel Essence, its Common for 57",
          Crafting.BreakYield(ItemCatalog.Get("sword2h_t40")) is { EssenceId: "essence_d", Qty: 1143 }
          && Crafting.BreakYield(ItemCatalog.Get("sword2h_t40_common")) is { EssenceId: "essence_d", Qty: 57 });
    Check("2H Mythic breaks T52 1200 / T61 3200 / T76 3840 / T80 9600 (Soulcrystal)",
          Crafting.BreakYield(ItemCatalog.Get("sword2h_t52"))?.Qty == 1200
          && Crafting.BreakYield(ItemCatalog.Get("sword2h_t61"))?.Qty == 3200
          && Crafting.BreakYield(ItemCatalog.Get("sword2h_t76"))?.Qty == 3840
          && Crafting.BreakYield(ItemCatalog.Get($"sword2h_t{ItemCatalog.SGradeLevel}")) is { EssenceId: "essence_s", Qty: 9600 });
    Check("the break table IS 0.4 x buy / essence sell (within rounding) on every T40+ piece",
          ItemCatalog.AllItems.Where(d => Crafting.IsGearSlot(d.Slot) && d.ItemLevel >= 40 && Crafting.BreakYield(d) is not null)
              .All(d => Math.Abs(Crafting.BreakYield(d)!.Value.Qty
                  - 0.4 * d.Value / Crafting.EssenceSellPrice[Crafting.EssenceGrade(d.ItemLevel)]) <= 0.51));
    Check("T1 and T20 gear cannot be broken (no essence below D)",
          Crafting.BreakYield(ItemCatalog.Get("sword2h_t20")) is null && Crafting.BreakYield(ItemCatalog.Get(ItemCatalog.NewbieSword1H)) is null);
    Check("every T40+ gear piece breaks into something (except the temporary 2-hour gear, BL-272)",
          ItemCatalog.AllItems.Where(d => Crafting.IsGearSlot(d.Slot) && d.ItemLevel >= 40 && d.WornLifetimeSeconds == 0).All(d => Crafting.BreakYield(d) is not null));
    Check("a shattered +3 returns 30% of the break value, a +15 150%",
          Crafting.ShatterYield(ItemCatalog.Get("sword2h_t40"), 3)?.Qty == 342
          && Crafting.ShatterYield(ItemCatalog.Get("sword2h_t40"), 15)?.Qty == 1714);
    Check("essence is unbuyable, worth 2x its sell, and sells for 1500 / 4500 / 7500 / 12500 / 25000",
          Crafting.EssenceIds.Select((id, g) => (ItemCatalog.Get(id), g)).All(x => x.Item1 is { BuyPriceOverride: -1 } e
              && e.Value == 2 * Crafting.EssenceSellPrice[x.g] && ItemCatalog.SellPrice(e) == Crafting.EssenceSellPrice[x.g])
          && Crafting.EssenceSellPrice.SequenceEqual(new[] { 1500, 4500, 7500, 12500, 25000 }));

    // `BL-273` part 2 (0.203.0): BECOMING A CRAFTER — the arithmetic and the catalogue, no server needed.
    Check("craft slots: L0 = 10, +5 per generic level, L10 = 60",
          Crafting.Slots(0) == 10 && Crafting.Slots(1) == 15 && Crafting.Slots(10) == 60);
    Check("craft levels: level N costs 20·N points (L1 at 20, L10 at 1100)",
          Crafting.LevelForPoints(19) == 0 && Crafting.LevelForPoints(20) == 1 && Crafting.LevelForPoints(60) == 2
          && Crafting.LevelForPoints(1099) == 9 && Crafting.LevelForPoints(1100) == 10 && Crafting.LevelForPoints(99999) == 10);
    Check("craft points per attempt: T40 1 · T52 2 · T61 3 · T76 5 · T80 8",
          Crafting.CraftPoints(40) == 1 && Crafting.CraftPoints(52) == 2 && Crafting.CraftPoints(61) == 3
          && Crafting.CraftPoints(76) == 5 && Crafting.CraftPoints(80) == 8);
    // 0.205.0 — STEP 10: the materials and the authored per-slot tables (`BL-273` part 3).
    Check("🔑 step 10: the old mat ladder is gone (one rung per base mat; Iron replaces Ingot)",
          ItemCatalog.Get("mat_ingot_uncommon") is null && ItemCatalog.Get("mat_wood_common") is null
          && Crafting.MaterialTypes.All(t => ItemCatalog.Get(Crafting.MaterialId(t)) is { Rarity: ItemRarity.Common })
          && ItemCatalog.Get(Crafting.MaterialId(MaterialType.Iron))?.Name == "Iron");
    Check("🔑 Nightsilver / Nightsilk: five rungs each, plus Alloy; 18 parts a tier, 90 in all",
          Enumerable.Range(0, 5).All(r => ItemCatalog.Get(Crafting.NightsilverId(r)) is not null && ItemCatalog.Get(Crafting.NightsilkId(r)) is not null)
          && ItemCatalog.Get(Crafting.NightsilverId(4))?.Name == "Legendary Nightsilver" && ItemCatalog.Get(Crafting.AlloyId) is not null
          && ItemCatalog.AllItems.Count(d => d.Id.StartsWith("part_")) == 90
          && ItemCatalog.Get("part_blunt2h_t40")?.Name == "Darksteel Maul Head",
          $"{ItemCatalog.AllItems.Count(d => d.Id.StartsWith("part_"))} parts");
    // `BL-274` part 1 (0.206.0), his rulings: a part = 1% of its full item; a recipe = 10% of its item x its %.
    Check("🔑 a part is worth 1% of its full item (T40 Maul Head = 1% of the T40 maul)",
          ItemCatalog.Get("part_blunt2h_t40")?.Value == (int)Math.Round(ItemCatalog.Get("blunt2h_t40")!.Value * 0.01),
          $"{ItemCatalog.Get("part_blunt2h_t40")?.Value} vs {ItemCatalog.Get("blunt2h_t40")?.Value}");
    Check("🔑 a recipe costs 10% of its item x its % (T61 maul: 100% = 10%, 60% = 6%)",
          ItemCatalog.Get(ItemCatalog.RecipeBookId("craft_blunt2h_t61", 100))?.Value == Crafting.RecipePrice(ItemCatalog.Get("blunt2h_t61")!.Value, 100)
          && ItemCatalog.Get(ItemCatalog.RecipeBookId("craft_blunt2h_t61", 60))?.Value * 10 == ItemCatalog.Get(ItemCatalog.RecipeBookId("craft_blunt2h_t61", 100))?.Value * 6,
          $"{ItemCatalog.Get(ItemCatalog.RecipeBookId("craft_blunt2h_t61", 100))?.Value} / {ItemCatalog.Get(ItemCatalog.RecipeBookId("craft_blunt2h_t61", 60))?.Value}");
    {
        var roster = MobCatalog.Templates.Where(m => !m.Dummy && !m.HandPlaced && !m.Guard && m.Drops is not null && m.Level >= 40).ToList();
        Check("🔑 BL-274: every roster creature of 40+ has a dealt specialty, and every one drops gear of it only",
              roster.All(m => m.Profile is not null)
              && roster.All(m => MobCatalog.KillTable(m, m.Level, MobRank.Normal)
                     .Where(e => e.GroupId == MobCatalog.GroupCommonGear)
                     .All(e => m.Profile!.Keys.Any(k => e.ItemId.StartsWith(k + "_t")))),
              $"{roster.Count} creatures");
        var elite = roster.First(m => m.Profile!.Kind == MobSpecialty.Weapons && m.Level is >= 40 and < 52);
        double Part(MobRank r) => MobCatalog.KillTable(elite, elite.Level, r).Where(e => e.ItemId.StartsWith("part_")).Sum(e => (double)e.Chance);
        Check("🔑 BL-274: an elite drops its parts x4, and a T76 normal drops 20% recipes",
              Math.Abs(Part(MobRank.Elite) - 4 * Part(MobRank.Normal)) < 1e-6
              && MobCatalog.RecipeDrop(76, MobRank.Normal, "sword2h")?.Pct == 20,
              $"{elite.Id}: {Part(MobRank.Normal):0.####} -> {Part(MobRank.Elite):0.####}");
    }
    {
        var t40 = RecipeCatalog.Get("craft_sword2h_t40")!;
        int Q(Recipe r, string id) => r.Inputs.FirstOrDefault(i => i.ItemId == id)?.Qty ?? 0;
        Check("🔑 T40 2H recipe = 400 wood + 400 iron + 10 alloy + 20 Greatsword Blades + 300 Nightsilver + 400 D essence, 400 MP",
              Q(t40, "mat_wood") == 400 && Q(t40, "mat_iron") == 400 && Q(t40, Crafting.AlloyId) == 10
              && Q(t40, "part_sword2h_t40") == 20 && Q(t40, Crafting.NightsilverId(0)) == 300 && Q(t40, "essence_d") == 400
              && t40.Inputs.Length == 6 && t40.MpCost == 400,
              string.Join(" + ", t40.Inputs.Select(i => $"{i.Qty} {i.ItemId}")));
        var ring80 = RecipeCatalog.Get("craft_ring_t80")!;
        var robe61 = RecipeCatalog.Get("craft_robe_t61")!;
        Check("authored cells: T80 ring = 7 bars + 1 Legendary Nightsilver, 50 MP; T61 robe = 1152 thread + 90 Rare Nightsilk",
              Q(ring80, ItemCatalog.VolcanicBar) == 7 && Q(ring80, Crafting.NightsilverId(4)) == 1 && ring80.MpCost == 50
              && Q(robe61, "mat_thread") == 1152 && Q(robe61, Crafting.NightsilkId(2)) == 90);
        Check("🔑 Nightsilver does NOT scale with the recipe %, everything else does (T76 1H at 20%)",
              RecipeCatalog.Get("craft_sword1h_t76") is { } r76
              && r76.Inputs.All(i => Crafting.InputQty(r76, i, 20) == (Crafting.IsFixedInput(i.ItemId) ? i.Qty : Crafting.ScaledQty(i.Qty, 20)))
              && r76.Inputs.Any(i => Crafting.IsFixedInput(i.ItemId)));
        var refines = RecipeCatalog.All.Where(r => r.Refine).ToList();
        Check("🔑 the refines: 8 ladder steps + alloy + bar, 10:1, gated L0/40 · L3/52 · L5/61 · L8/76 (bar L7/76), 0 points, no gold",
              refines.Count == 10 && refines.All(r => r.Type == CraftType.General && r.GoldAt(0) == 0 && Crafting.CraftPoints(r) == 0)
              && RecipeCatalog.Get("refine_nightsilver_1") is { UnlockLevel: 0, LearnLevel: 40, MpCost: 50 }
              && RecipeCatalog.Get("refine_nightsilk_2") is { UnlockLevel: 3, LearnLevel: 52, MpCost: 100 }
              && RecipeCatalog.Get("refine_nightsilver_3") is { UnlockLevel: 5, LearnLevel: 61, MpCost: 150 }
              && RecipeCatalog.Get("refine_nightsilk_4") is { UnlockLevel: 8, LearnLevel: 76, MpCost: 200 }
              && RecipeCatalog.Get("refine_volcanic_bar") is { UnlockLevel: 7, LearnLevel: 76 }
              && RecipeCatalog.Get("refine_nightsilver_1")!.Inputs[0] is { ItemId: "nightsilver_0", Qty: 10 },
              $"{refines.Count} refines");
    }
    // 0.204.0 — THE CRAFTER-POINTS MODEL (his Idea-1) + step 9b.
    Check("🔑 a smith's L9 / L10 add +5% each, and nothing below",
          Crafting.GearSuccessBonus(8) == 0f && Math.Abs(Crafting.GearSuccessBonus(9) - 0.05f) < 1e-6
          && Math.Abs(Crafting.GearSuccessBonus(10) - 0.10f) < 1e-6);
    Check("🔑 tier gates: T40 L0 · T52 L2 · T61 L4 · T76 L6 · T80 L8, and every gear recipe carries its own",
          Crafting.TierGate(40) == 0 && Crafting.TierGate(52) == 2 && Crafting.TierGate(61) == 4
          && Crafting.TierGate(76) == 6 && Crafting.TierGate(80) == 8
          && RecipeCatalog.All.Where(r => r.IsGear).All(r => r.UnlockLevel == Crafting.TierGate(r.GearItemLevel)));
    Check("🔑 Scribe/Apothecary price: x0.90 at L0 → x0.55 at L10, and every batch still costs gold",
          Math.Abs(Crafting.PriceFactor(0) - 0.9f) < 1e-6 && Math.Abs(Crafting.PriceFactor(10) - 0.55f) < 1e-6
          && RecipeCatalog.All.Where(r => r.BatchValue > 0).All(r => r.GoldAt(10) >= 10
              && r.GoldAt(0) > r.GoldAt(10)));
    var nine = RecipeCatalog.All.Where(r => r.Type is CraftType.Apothecary or CraftType.Scribe).ToList();
    Check("🔑 step 9b: every Scribe/Apothecary recipe succeeds 100% and is priced; the OUT list has no recipe",
          nine.Count > 0 && nine.All(r => r.SuccessChance == 1f && r.BatchValue > 0 && r.LearnPrice > 0 && r.LearnLevel >= 40)
          && new[] { ItemCatalog.SkillStone, ItemCatalog.ElementalStone, ItemCatalog.ScrollReturn, ItemCatalog.ScrollResurrect,
                     ItemCatalog.ScrollReturnUltimate, ItemCatalog.ScrollResurrectUltimate, ItemCatalog.InstantPotion,
                     ItemCatalog.DashPotionC, ItemCatalog.DashPotionM, ItemCatalog.ScrollNormalD, ItemCatalog.AttrScrollRare }
                 .All(id => !RecipeCatalog.All.Any(r => r.OutputId == id)),
          $"{nine.Count} recipes");
    Check("step 9b rows: minor HP x100 @40 L0 · rare MP x10 @76 Apothecary L10 · 2h rune x3 @80 Scribe L10",
          RecipeCatalog.Get("craft_" + ItemCatalog.MinorPotion) is { OutputQty: 100, LearnLevel: 40, UnlockLevel: 0, Type: CraftType.Apothecary }
          && RecipeCatalog.Get("craft_" + ItemCatalog.GreaterManaPotion) is { OutputQty: 10, LearnLevel: 76, UnlockLevel: 10 }
          && RecipeCatalog.Get("craft_" + ItemCatalog.BoxWarRune2h) is { OutputQty: 3, LearnLevel: 80, UnlockLevel: 10, Type: CraftType.Scribe });
    Check("🔑 the mat curve: 20 → 30%, 40 → 50%, 60 → 70%, 100 → 100% (20 heads at 20% = 6)",
          Crafting.ScaledQty(20, 20) == 6 && Crafting.ScaledQty(20, 40) == 10 && Crafting.ScaledQty(20, 60) == 14
          && Crafting.ScaledQty(20, 100) == 20 && Crafting.ScaledQty(1, 20) == 1);
    var gearRecipes = RecipeCatalog.All.Where(r => r.IsGear).ToList();
    Check("gear recipes start at T40 (no F/E crafting) and carry their tier + type",
          gearRecipes.Count > 0 && gearRecipes.All(r => r.GearItemLevel >= 40 && r.Type != CraftType.General)
          && RecipeCatalog.Get("craft_sword1h_t20") is null,
          $"{gearRecipes.Count} gear recipes");
    Check("🔑 every gear recipe has a recipe ITEM at exactly its tier's %s (T40/T52 100 · T61 60/100 · T76 20/40/60 · T80 40/60)",
          gearRecipes.All(r => Crafting.RecipePercents.All(p =>
              (ItemCatalog.Get(ItemCatalog.RecipeBookId(r.Id, p)) is { } d && d.RecipePercent == p && d.TeachesRecipeId == r.Id)
              == Crafting.RecipePercentsFor(r.GearItemLevel).Contains(p))));
    var shelfIds = ShopCatalog.Get(WorldMap.CraftMasterId)?.ItemIds ?? Array.Empty<string>();
    Check("🔑 the Master Crafter's shelf sells the T40 and T52 100% gear recipes, and nothing else",
          shelfIds.Length > 0 && shelfIds.All(id => ItemCatalog.Get(id) is { RecipePercent: 100 } d
              && RecipeCatalog.Get(d.TeachesRecipeId) is { GearItemLevel: 40 or 52 })
          && shelfIds.Contains(ItemCatalog.RecipeBookId("craft_sword2h_t52", 100)),
          $"{shelfIds.Length} rows");
    Check("generic recipes: learned at the Master (unlock 0-10, a price), no recipe item; the trial's hammer is not for sale",
          RecipeCatalog.GenericForSale.Any() && RecipeCatalog.GenericForSale.All(r => !r.IsGear && !r.QuestOnly
              && r.UnlockLevel is >= 0 and <= 10 && r.LearnPrice > 0 && r.LearnLevel >= 40)
          && RecipeCatalog.Get(Crafting.HammerRecipeId) is { QuestOnly: true, SuccessChance: 0.4f }
          && !RecipeCatalog.GenericForSale.Any(r => r.Id == Crafting.HammerRecipeId));
    Check("ONE Master Crafter per town (the five profession masters are gone)",
          WorldMap.Npcs.Count(n => n.Role == NpcRole.CraftMaster) == 5
          && WorldMap.Npcs.Where(n => n.Role == NpcRole.CraftMaster).All(n => WorldMap.IsCraftMaster(n.Id)),
          $"{WorldMap.Npcs.Count(n => n.Role == NpcRole.CraftMaster)} masters");
    // `BL-274` step 12: a boss's recipes are rows of its table (group "recipe"), 100% below T76 and 60% at T76/T80;
    // `BL-308`: 2.5 books a kill at T40 down to 0.8 at T80.
    {
        var b44 = MobCatalog.BossDrops(44).ToList();
        var b90 = MobCatalog.BossDrops(90).ToList();
        var books44 = b44.Where(e => e.GroupId == MobCatalog.GroupRecipe).ToList();
        var books90 = b90.Where(e => e.GroupId == MobCatalog.GroupRecipe).ToList();
        Check("bosses: T40 pays 100% recipes, T80 60%, 2.5 books a kill at T40 and 0.8 at T80 (BL-308), a T80 full item, S essence; every id exists",
              books44.All(e => e.ItemId.EndsWith("_100") && e.ItemId.Contains("_t40"))
              && Math.Abs(books44.Sum(e => e.Chance) - 2.5f) < 0.001f
              && books90.All(e => e.ItemId.EndsWith("_60") && e.ItemId.Contains("_t80"))
              && Math.Abs(books90.Sum(e => e.Chance) - 0.8f) < 0.001f
              && b90.Any(e => e.GroupId == MobCatalog.GroupBossGear && e.ItemId == "sword2h_t80")
              && b90.Any(e => e.GroupId == MobCatalog.GroupEssence && e.Chance >= 1f)
              && !b44.Any(e => e.GroupId == MobCatalog.GroupEssence || e.ItemId.EndsWith("_common"))
              && b44.Concat(b90).All(e => ItemCatalog.Get(e.ItemId) is not null),
              $"{books44.Count}/{books90.Count} books");
    }

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
    Check("F grade has NO Common (Commons are T40-T61 only, BL-272)",
          ItemCatalog.Get($"sword1h_t{ItemCatalog.FGradeLevel}_common") is null);

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

    // ---- `BL-272`: EQUIPMENT IS COMMON + MYTHIC, AND ONE SET PER BODY ----
    // The Epic/Legendary set variants went with their rungs. What is left to assert is that nothing
    // but the authored set exists, and that a Common piece carries none of it.
    {
        var mythicBody = ItemCatalog.Get("light_t40");
        var commonBody = ItemCatalog.Get("light_t40_common");
        Check("a Common body carries NO set id (unmodifiable, BL-272)",
              commonBody is { SetId: "" } && mythicBody is not null && mythicBody.SetId.Length > 0,
              $"common '{commonBody?.SetId}', mythic '{mythicBody?.SetId}'");
        Check("the Epic/Legendary set variants are gone",
              ArmorSetCatalog.Get("set_heavy_t20_epic") is null && ArmorSetCatalog.Get("set_heavy_t40_legendary") is null);
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

    // `BL-280` (0.209.0): in a generated NORMAL camp only a spawn of level 80+ attacks on sight. Below 80
    // every field is peaceful; at 80+ every aggressive-capable creature in the roster is.
    var normalCamps = WorldPlan.Fields.SelectMany(f => f.Zones).Where(z => z.Rank == MobRank.Normal).ToArray();
    Check("every generated normal camp gates aggression at level 80",
          normalCamps.All(z => z.AggressiveFromLevel == WorldPlan.AggressiveFromLevel && WorldPlan.AggressiveFromLevel == 80));
    var starter = WorldPlan.Fields.First(f => f.Plan.Id == "field_bracken_hollow");
    Check("the two starter camps are peaceful (no spawn reaches the gate)",
          starter.Zones.All(z => z.MaxLevel < z.AggressiveFromLevel || z.AggressiveTypes is { Length: 0 }));
    // …and the endgame is not: every aggressive-capable creature in an 80+ camp may attack.
    var summit = WorldPlan.Fields.First(f => f.Plan.Id == "field_frost_summit");
    Check("an 80+ camp makes EVERY aggressive-capable creature aggressive",
          summit.Zones.Where(z => z.Rank == MobRank.Normal).All(z => z.MinLevel >= 80 && z.AggressiveTypes is { Length: > 0 } a
              && a.Length == z.MobTypes.Count(id => MobCatalog.Get(id).Aggressive)));
    // `BL-280`: an OwnField creature (the anti-type zones) lives in its own field and nowhere else.
    var leaked = normalCamps.Where(z => z.MobTypes.Any(id => MobCatalog.Get(id).OwnField)
                                        && !z.MobTypes.All(id => MobCatalog.Get(id).OwnField))
                            .Select(z => $"{z.MinLevel}-{z.MaxLevel}").ToArray();
    Check("no anti-type creature is mixed into an ordinary camp", leaked.Length == 0, string.Join(", ", leaked));
    Check("the six anti-type fields exist",
          WorldPlan.Fields.Count(f => f.Zones.All(z => z.MobTypes.All(id => MobCatalog.Get(id).OwnField))) == 6);

    // Every field is OWNED by a city, and that city's gatekeeper can therefore send you there.
    var orphan = RegionMap.Fields.Where(f => f.CityId.Length == 0)
                                 .Select(f => f.Id).ToArray();
    Check("every planned field records a managing city",
          orphan.All(id => id is "field_training" or "field_treant"),
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
// 1a-0b. THE DAILY RUNE QUEST AND /resetlimits (§105.3, 2026-09-26: *"the reset limits don't reset my daily
//     apoth rune quest"*). The recipe dailies are reset-checked in section 5; this one is offered by EVERY
//     town's Apothecary and only at 6-75, so it gets its own run while the protagonist is still low.
// -------------------------------------------------------------------------------------------
{
    await a.Hub.SendAsync("AdminCommand", "lvl", $"{name} 10");
    await a.Settle();
    var apo = WorldMap.Npcs.First(n => n.Id == "merchant_potions");
    await a.Hub.SendAsync("DebugTeleport", apo.X + 60, apo.Y);
    await a.WaitFor(() => a.EntityNames.Any(kv => apo.Name.EndsWith(kv.Value)));
    Guid apoId = a.EntityNames.FirstOrDefault(kv => apo.Name.EndsWith(kv.Value)).Key;
    QuestEntry? Runes() => a.Quests?.Entries.FirstOrDefault(e => e.Id == QuestCatalog.QuestDailyRunes);

    await a.Hub.SendAsync("QuestAction", "accept", QuestCatalog.QuestDailyRunes, apoId);
    await a.Settle();
    await a.Hub.SendAsync("QuestAction", "complete", QuestCatalog.QuestDailyRunes, apoId);
    await a.Settle();
    Check("the daily rune quest, handed in, is closed for today",
          apoId != Guid.Empty && Runes()?.State == QuestAvailability.Completed,
          $"apothecary {apoId}, state {Runes()?.State} '{Runes()?.Status}'");

    await a.Hub.SendAsync("AdminCommand", "resetlimits", "");
    await a.Settle();
    Check("🔑 /resetlimits hands the daily rune quest back", Runes()?.State == QuestAvailability.Available,
          $"state {Runes()?.State} '{Runes()?.Status}', stamps [{string.Join(",", a.Quests?.Completed.Where(c => c.Contains('@')) ?? Array.Empty<string>())}]");
    await a.Hub.SendAsync("QuestAction", "accept", QuestCatalog.QuestDailyRunes, apoId);
    await a.Settle();
    Check("...and the Apothecary really gives it again",
          a.Quests?.Active.Any(q => q.Id == QuestCatalog.QuestDailyRunes) == true,
          $"active [{string.Join(",", a.Quests?.Active.Select(q => q.Id) ?? Array.Empty<string>())}]");

    // §105.2 (2026-09-26): *"the quest window dont allow me to untrack it"*. Accepting pins the quest; the
    // window's Untrack button sends "track", which must toggle the pin OFF, and a second press back on.
    bool? Pinned() => Runes()?.Tracked;
    bool pinnedOnAccept = Pinned() == true;
    await a.Hub.SendAsync("QuestAction", "track", QuestCatalog.QuestDailyRunes, Guid.Empty);
    await a.Settle();
    bool afterUntrack = Pinned() == false;
    await a.Hub.SendAsync("QuestAction", "track", QuestCatalog.QuestDailyRunes, Guid.Empty);
    await a.Settle();
    Check("🔑 an accepted quest is pinned, Untrack unpins it, Track pins it again",
          pinnedOnAccept && afterUntrack && Pinned() == true,
          $"on accept {pinnedOnAccept}, after untrack {afterUntrack}, after re-track {Pinned()}");
    await a.Hub.SendAsync("QuestAction", "abandon", QuestCatalog.QuestDailyRunes, Guid.Empty);
    await a.Settle();
    // Back to level 1: the marker section below asserts a level-1 offer list, and the levelling
    // sections count up from 1.
    await a.Hub.SendAsync("AdminCommand", "lvl", $"{name} 1");
    await a.Settle();
}

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
// 1b-2. THE EXP CURVE ON THE WIRE. The curve moved to the real IG table (ExpCurve), where the
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
// out of order. Tolerance is 1%: real IG pins level 80's cumulative total at exactly 4 200 000 000, a
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
// 4a. CHARISMA (`BL-283`) — a RECOMMENDATION ("Like" on the wire) is +10 from a 20/day budget; the giver
// must be level 20+, never the same account; a target receives one per giver per day, 10 a day.
// It ranks on the charisma board by LIFETIME.
// -------------------------------------------------------------------------------------------
friend.SystemChat.Clear();
a.SystemChat.Clear();
await a.Hub.SendAsync("Like", "Test2");   // the protagonist is still level 1 here
await a.Settle();
Check("a giver below level 20 cannot recommend (`BL-283` rule 1)",
      a.SystemChat.Any(s => s.Contains("must be level 20")), string.Join(" | ", a.SystemChat));
// Lift both givers to 20 for this section only, and put them back after, so the levelling maths
// below (the protagonist lands on 81) and every later Test2 check see the level they always did.
await a.Hub.SendAsync("DebugLevel", 10);   // the hub clamps a step to ±10
await a.Hub.SendAsync("DebugLevel", 9);
await a.Hub.SendAsync("AdminCommand", "lvl", "Test2 20");
await a.Settle();
friend.SystemChat.Clear();
a.SystemChat.Clear();
await a.Hub.SendAsync("Like", "Test2");
await a.Settle();
// Test2 is a SEEDED character that persists between runs, so on the 11th run of one UTC day it has
// already received its 10 — that refusal is the rule working, not a failure.
bool test2Full = a.SystemChat.Any(s => s.Contains("already received 10"));
Check("recommending a player raised their charisma",
      test2Full || friend.SystemChat.Any(s => s.Contains("recommended you") && s.Contains("charisma")),
      test2Full ? "Test2 already had 10 today" : string.Join(" | ", friend.SystemChat));
Check("the giver spent one from the daily budget",
      test2Full || a.SystemChat.Any(s => s.Contains("left today")));
a.SystemChat.Clear();
await a.Hub.SendAsync("Like", "Test2");   // the same giver, the same day
await a.Settle();
Check("one recommendation per giver per target per day",
      a.SystemChat.Any(s => s.Contains("already recommended Test2 today") || s.Contains("already received 10")),
      string.Join(" | ", a.SystemChat));
a.SystemChat.Clear();
await a.Hub.SendAsync("Like", name);   // can't recommend yourself
await a.Settle();
Check("you can't recommend yourself", a.SystemChat.Any(s => s.Contains("can't recommend yourself")));
// The typed `/like` (2026-09-24): an ordinary player reaches HandleLike through the admin path, and its
// staff `-f` form is refused for them; staff force CURRENT charisma, online and offline.
friend.SystemChat.Clear();
await friend.Hub.SendAsync("AdminCommand", "like", "Test2");   // refused (self) — spends no budget on the seeded Test2
await friend.Hub.SendAsync("AdminCommand", "like", $"{name} -f 0");
await a.Settle();
Check("a player's typed /like runs the Recommend rules",
      friend.SystemChat.Any(s => s.Contains("can't recommend yourself")),
      string.Join(" | ", friend.SystemChat));
Check("a player's /like -f is refused (staff only)",
      friend.SystemChat.Any(s => s.Contains("Only staff can force charisma")), string.Join(" | ", friend.SystemChat));
a.SystemChat.Clear();
await a.Hub.SendAsync("AdminCommand", "like", "Test2 -f 0");
await a.Settle();
Check("staff /like <online> -f 0 sets current charisma to 0",
      a.SystemChat.Any(s => s.Contains("Test2: current charisma set to 0")), string.Join(" | ", a.SystemChat));
a.SystemChat.Clear();
await a.Hub.SendAsync("AdminCommand", "like", $"{victimName} -f 300");
for (int attempt = 0; attempt < 10 && !a.SystemChat.Any(s => s.Contains("(offline)")); attempt++)
    await Task.Delay(300);
Check("staff /like <offline> -f 300 sets it in the DB",
      a.SystemChat.Any(s => s.Contains($"{victimName} (offline): current charisma set to 300")), string.Join(" | ", a.SystemChat));
a.SystemChat.Clear();
await a.Hub.SendAsync("Like", victimName);   // offline, and on the protagonist's OWN account
for (int attempt = 0; attempt < 10 && !a.SystemChat.Any(s => s.Contains("own account")); attempt++)
    await Task.Delay(300);   // the offline rules run in the DB on a worker
Check("an OFFLINE same-account recommendation is refused by the DB-side rules",
      a.SystemChat.Any(s => s.Contains("own account")), string.Join(" | ", a.SystemChat));
// Test2 recommends the VICTIM (offline — resolved in the DB), so the later jail has charisma to drain.
// It used to like the protagonist, which stopped working the moment the protagonist became an admin:
// STAFF ARE EXCLUDED FROM THE LEADERBOARDS, which is the answer to the owner's playtest-13 puzzle —
// "my ranking board was never updated ... aaa, my chars are admins".
await friend.Hub.SendAsync("Like", victimName);
await a.Settle();
await a.Hub.SendAsync("DebugLevel", -10);
await a.Hub.SendAsync("DebugLevel", -9);
await a.Hub.SendAsync("AdminCommand", "lvl", "Test2 1");
await a.Settle();
LeaderboardDto chBoard = null!;
for (int attempt = 0; attempt < 10; attempt++)
{
    chBoard = await a.Hub.InvokeAsync<LeaderboardDto>("RequestLeaderboard", "charisma");
    if (chBoard.Entries.Any(e => e.Name == victimName)) break;
    await Task.Delay(300);   // the offline like lands via a background DB write
}
Check("the liked player reached the charisma board",
      chBoard.Entries.Any(e => e.Name == "Test2" && e.Value >= 10),
      string.Join(",", chBoard.Entries.Select(e => $"{e.Name}:{e.Value}")));
Check("an offline recommendation reached the board too, at +10 (the victim)",
      chBoard.Entries.Any(e => e.Name == victimName && e.Value == 10),
      string.Join(",", chBoard.Entries.Select(e => $"{e.Name}:{e.Value}")));
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
// `BL-252` — a subclass is BORN AT 40, not 1. Read off the rule rather than a literal, so the day the
// birth level moves the test moves with it instead of failing as a stale number.
Check($"new class starts at level {ThirdClassCatalog.ChangeLevel}",
      sub?.Level == ThirdClassCatalog.ChangeLevel, $"level {sub?.Level}");
// 🔑 SP, not the skill list. A sub is NOT born with an empty `Learned` — AutoLearnCoreSkills grants
// the *"auto learned like mage etc."* set he carved out, and at 40 that legitimately includes an
// identity floor passive. ZERO SP is the half of his rule that is unconditional, so that is what is
// asserted; asserting an empty list would fail on a mage for being correct.
Check("new class starts with 0 SP (the levels are given, the SP is not)",
      a.Progress?.SkillPoints == 0, $"{a.Progress?.SkillPoints} SP");
Check("new class has the chosen 3rd class pre-approved", sub?.ThirdClass == chosen.Id);

// -------------------------------------------------------------------------------------------
// 3b. `BL-250` — THE SUBCLASS SLOT LADDER. The ticket is an ITEM and the slot count is PERSISTED,
//     which is exactly the shape of bug this harness exists for: a slot that looks open on screen
//     and was never written. The admin path above is deliberately ungated, so this tests the STATE
//     the player path runs on rather than the player path itself (that one needs an NPC in range).
// -------------------------------------------------------------------------------------------
Console.WriteLine("  -- BL-250: the subclass slot ladder --");
Check("the ladder is seven rungs (three earned + four bought)",
      SubclassSlots.MaxSlots == 7 && SubclassSlots.BoughtRungs.Length == 4,
      $"{SubclassSlots.MaxSlots} slots, {SubclassSlots.BoughtRungs.Length} bought");
Check("the bought rungs are 500kk / 5kkk gold then 100 / 1,000 platinum",
      SubclassSlots.PriceOf(4) is { Gold: 500_000_000L, Platinum: 0 }
      && SubclassSlots.PriceOf(5) is { Gold: 5_000_000_000L, Platinum: 0 }
      && SubclassSlots.PriceOf(6) is { Gold: 0L, Platinum: 100 }
      && SubclassSlots.PriceOf(7) is { Gold: 0L, Platinum: 1_000 });
Check("slot 8 is not on the ladder (the 5,000-platinum rung is cut until the summoner)",
      SubclassSlots.PriceOf(8) is null);
Check("an EARNED slot has no price", SubclassSlots.PriceOf(3) is null);

int slotsBefore = a.Subclasses!.SlotsUnlocked;
await a.Hub.SendAsync("DebugGive", ItemCatalog.SubclassTicket, 1);
await a.Settle();
var ticket = a.Inv?.Items.FirstOrDefault(i => i.DefId == ItemCatalog.SubclassTicket);
Check("a Subclass Ticket can be held", ticket is not null);

await a.Hub.SendAsync("UsePotion", ticket!.InstanceId);
await a.Settle();
Check("using the ticket opened ONE slot",
      a.Subclasses!.SlotsUnlocked == slotsBefore + 1,
      $"{slotsBefore} -> {a.Subclasses.SlotsUnlocked}");
Check("...and consumed the ticket",
      (a.Inv?.Items.Count(i => i.DefId == ItemCatalog.SubclassTicket) ?? 0) == 0);
Check("the client is told the ceiling rather than hard-coding it",
      a.Subclasses.MaxSlots == SubclassSlots.MaxSlots, $"{a.Subclasses.MaxSlots}");
int slotsUnlockedForRelog = a.Subclasses.SlotsUnlocked;
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
      a.Subclasses.Classes.First(c => c.Slot == subSlot).Level == ThirdClassCatalog.ChangeLevel + 4,
      $"level {a.Subclasses.Classes.First(c => c.Slot == subSlot).Level}, "
      + $"expected {ThirdClassCatalog.ChangeLevel + 4}");

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

string matId = Crafting.MaterialId(MaterialType.Iron);
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

// `BL-273` part 1: BREAK a real piece on the server and get its essence back.
await a.Hub.SendAsync("DebugGive", "sword2h_t40_common", 1);
await a.Settle();
var toBreak = a.Inv?.Items.FirstOrDefault(i => i.DefId == "sword2h_t40_common" && !i.Equipped);
int essBefore = a.Inv?.Items.Where(i => i.DefId == "essence_d").Sum(i => i.Quantity) ?? 0;
if (toBreak is not null)
{
    await a.Hub.SendAsync("DisassembleItem", toBreak.InstanceId);
    await a.Settle();
}
int essAfter = a.Inv?.Items.Where(i => i.DefId == "essence_d").Sum(i => i.Quantity) ?? 0;
Check("breaking a T40 Common 2H on the server gives 57 Darksteel Essence and consumes it",
      toBreak is not null && essAfter - essBefore == 57
      && a.Inv?.Items.Any(i => i.InstanceId == toBreak.InstanceId) == false,
      $"essence {essBefore} -> {essAfter}");

// `BL-272` part 2 (0.202.0): the SHOPS on the server. Buy a T52 piece for essence at the Assayer, buy a
// temporary weapon box for gold at Greymarsh's Armsmaster, pick from it, wear it and watch the worn clock
// move; then Unequip-all pauses it. The gear `a` wears is parked in preset C and put back afterwards.
{
    int HeldA(string defId) => a.Inv?.Items.Where(i => i.DefId == defId).Sum(i => i.Quantity) ?? 0;
    async Task<Guid> StandAt(string npcId)
    {
        var npc = WorldMap.NpcById(npcId)!;
        string given = npc.Name.Split(' ')[^1];
        await a.Hub.SendAsync("DebugTeleport", npc.X + 40f, npc.Y);
        await a.WaitFor(() => a.EntityNames.Any(kv => kv.Value == given));
        return a.EntityNames.FirstOrDefault(kv => kv.Value == given).Key;
    }

    await a.Hub.SendAsync("SaveEquipPreset", 2);
    await a.Settle();

    var assayerId = await StandAt(ShopCatalog.EssenceMerchant);
    Check("the Assayer is in view in Greymarsh", assayerId != Guid.Empty);
    if (assayerId != Guid.Empty)
    {
        await a.Hub.SendAsync("TalkToNpc", assayerId);
        await a.WaitFor(() => a.Dialog?.Shop?.Items.Any(w => w.DefId == "sword2h_t52") == true);
        var row = a.Dialog?.Shop?.Items.FirstOrDefault(w => w.DefId == "sword2h_t52");
        Check("the Assayer quotes the T52 2H in essence and no gold",
              row is { BuyPrice: 0, Essence.Length: 2 } && row.Essence[0].Qty == 750 && row.Essence[1].Qty == 6750,
              row is null ? "no row" : $"gold {row.BuyPrice}, essence {row.Essence?.Length}");

        // Short of essence: refused, nothing taken.
        await a.Hub.SendAsync("BuyItem", assayerId, "sword2h_t52", 1);
        await a.Settle();
        Check("without the essence the Assayer refuses", HeldA("sword2h_t52") == 0);

        await a.Hub.SendAsync("DebugGive", "essence_c", 750);
        await a.Hub.SendAsync("DebugGive", "essence_d", 6750);
        await a.Settle();
        int cBefore = HeldA("essence_c"), dBefore = HeldA("essence_d");
        long goldBefore = a.Gold;
        await a.Hub.SendAsync("BuyItem", assayerId, "sword2h_t52", 1);
        await a.WaitFor(() => HeldA("sword2h_t52") == 1);
        Check("the Assayer sells the T52 2H for 750 C + 6750 D essence and no gold",
              HeldA("sword2h_t52") == 1 && cBefore - HeldA("essence_c") == 750
              && dBefore - HeldA("essence_d") == 6750 && a.Gold == goldBefore,
              $"C {cBefore}->{HeldA("essence_c")}, D {dBefore}->{HeldA("essence_d")}, gold {goldBefore}->{a.Gold}");
    }

    var smithId = await StandAt("merchant_gear_greymarsh");
    Check("Greymarsh's Armsmaster is in view", smithId != Guid.Empty);
    if (smithId != Guid.Empty)
    {
        await a.Hub.SendAsync("DebugGold", 1_000_000L);
        await a.Settle();
        long g0 = a.Gold;
        string boxId = ItemCatalog.TempWeaponBoxId(40);
        await a.Hub.SendAsync("BuyItem", smithId, boxId, 1);
        await a.WaitFor(() => HeldA(boxId) == 1);
        Check("a T40 temp weapon box costs 214,286 gold", HeldA(boxId) == 1 && g0 - a.Gold == 214_286,
              $"paid {g0 - a.Gold}");

        var box = a.Inv?.Items.FirstOrDefault(i => i.DefId == boxId);
        string tempId = ItemCatalog.TempId("sword2h_t40");
        if (box is not null)
        {
            await a.Hub.SendAsync("SelectBoxItems", box.InstanceId, new[] { tempId });
            await a.WaitFor(() => HeldA(tempId) == 1);
        }
        var piece = a.Inv?.Items.FirstOrDefault(i => i.DefId == tempId);
        Check("picking from the box gives a temp 2H with 7200 s of wearing, tagged temporary + bound",
              piece is { WornSecondsLeft: 7200 } && HeldA(boxId) == 0
              && ItemTag.For(ItemCatalog.Get(tempId)!, piece) == "(temporary, bound)",
              piece is null ? "no piece" : $"worn {piece.WornSecondsLeft}, tag {ItemTag.For(ItemCatalog.Get(tempId)!, piece)}");

        if (piece is not null)
        {
            // In the bag it does not tick.
            await Task.Delay(2500);
            await a.Hub.SendAsync("DisassembleItem", piece.InstanceId);   // refused: temp gear never breaks
            await a.Settle();
            Check("a temp piece cannot be broken", HeldA(tempId) == 1);

            await a.Hub.SendAsync("EquipItem", piece.InstanceId);
            await a.WaitFor(() => a.Inv?.Items.Any(i => i.InstanceId == piece.InstanceId && i.Equipped) == true);
            var bagged = a.Inv?.Items.FirstOrDefault(i => i.InstanceId == piece.InstanceId);
            Check("the clock did not run in the bag (still 7200 when equipped)", bagged?.WornSecondsLeft == 7200,
                  $"{bagged?.WornSecondsLeft}");
            await Task.Delay(3500);   // the clock is 1/s on the server, only while worn
            await a.Hub.SendAsync("UnequipAll");
            await a.WaitFor(() => a.Inv?.Items.Any(i => i.Equipped) == false);
            var worn = a.Inv?.Items.FirstOrDefault(i => i.InstanceId == piece.InstanceId);
            Check("Unequip all takes everything off", a.Inv?.Items.Any(i => i.Equipped) == false);
            Check("wearing it spent the clock (7200 -> ~7197), and it is paused now",
                  worn?.WornSecondsLeft is int w && w < 7200 && w >= 7190, $"{worn?.WornSecondsLeft}");
        }
    }

    await a.Hub.SendAsync("ApplyEquipPreset", 2);   // put `a`'s own gear back
    await a.Settle();
    await a.Hub.SendAsync("DebugTeleport", 24000f, 24000f);
    await a.Settle();
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
// 4f. THE STAT-SWAP BASKET (`BL-03`, the Stats tab). Three failures live here and NOT ONE of them
//     is visible on the screen that causes them:
//       (1) The wrong TOTAL. The price of a rung depends on how many you already own (1/2/3/4/5kk),
//           so a basket of four is not four times the "next rung" price. A tab that summed it the
//           obvious way would show a plausible number and charge a different one.
//       (2) A PARTIAL basket. If the caps were re-checked per line against the SAVED levels instead
//           of the running ones, an over-budget basket would apply its first lines and refuse the
//           rest — committing a build the player never picked, and one only the Mindwriter undoes,
//           a whole pair at a time.
//       (3) Gold taken for a refused basket. The refusal is a chat line; the balance is a number in
//           a corner. Nobody reconciles those by eye.
//     So: assert the exact charge, and assert that a refusal moves NOTHING.
// -------------------------------------------------------------------------------------------
{
    // AGI<->CON is on every class's shelf, so this section does not depend on what `a` rolled.
    string up = SkillCatalog.SwapAgiCon, down = SkillCatalog.SwapConAgi;
    int LevelOf(string id) => a.Learned?.Skills.FirstOrDefault(s => s.Id == id)?.Level ?? 0;

    await a.Hub.SendAsync("DebugGold", 100_000_000L);
    await a.Settle();
    long goldBefore = a.Gold;
    Check("the swap shelf offers +AGI -CON to this class",
          SkillCatalog.StatSwapsFor(BaseClass.Fighter, null).Contains(up));
    Check("no stat rungs owned yet (a fresh character)",
          SkillCatalog.StatSwapRungsOwned(a.Learned!.Skills.ToDictionary(s => s.Id, s => s.Level)) == 0);

    // An ILLEGAL basket first, so the "nothing moved" assertion cannot pass just because the legal
    // one happened to run later: 6 rungs into one pair is one past the +5-per-stat ceiling.
    await a.Hub.SendAsync("BuyStatSwaps", new[] { new StatSwapPurchaseDto(up, 6) });
    await a.Settle();
    Check("a basket over the +5 ceiling is refused ENTIRELY", LevelOf(up) == 0, $"level {LevelOf(up)}");
    Check("...and a refused basket costs nothing", a.Gold == goldBefore, $"{goldBefore} -> {a.Gold}");

    // The legal one: 5 + 4 = his own nine-rung example, which the design says is 35kk however spread.
    await a.Hub.SendAsync("BuyStatSwaps",
        new[] { new StatSwapPurchaseDto(up, 5), new StatSwapPurchaseDto(down, 4) });
    await a.Settle();
    Check("a legal basket applies EVERY line", LevelOf(up) == 5 && LevelOf(down) == 4,
          $"{up}={LevelOf(up)}, {down}={LevelOf(down)}");
    Check("...charged the LADDER total, not 9x the next-rung price",
          goldBefore - a.Gold == 35_000_000L, $"charged {goldBefore - a.Gold:N0}");
    Check("...and that spends the whole 9-rung budget",
          SkillCatalog.StatSwapRungsOwned(a.Learned!.Skills.ToDictionary(s => s.Id, s => s.Level))
              == SkillCatalog.StatSwapMaxTotal);

    // A tenth rung has nowhere to go, and must be refused without taking anything.
    long afterBuy = a.Gold;
    await a.Hub.SendAsync("BuyStatSwaps", new[] { new StatSwapPurchaseDto(SkillCatalog.SwapAgiAtk, 1) });
    await a.Settle();
    Check("a 10th rung is refused, free of charge",
          LevelOf(SkillCatalog.SwapAgiAtk) == 0 && a.Gold == afterBuy);
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
// The stat rungs are a 35kk purchase with no refund. They are stored as skill LEVELS, so a bug that
// saved only the presence of a skill would lose four of the nine and be invisible until someone
// recounted their stats.
Check("all nine stat rungs survived the relog at their bought LEVELS (`BL-03`)",
      b.Learned is not null
      && SkillCatalog.StatSwapRungsOwned(b.Learned.Skills.ToDictionary(s => s.Id, s => s.Level))
             == SkillCatalog.StatSwapMaxTotal,
      b.Learned is null ? "no Learned push" :
      string.Join(",", b.Learned.Skills.Where(s => SkillCatalog.StatSwapOf(s.Id) is not null)
                                       .Select(s => $"{s.Id}={s.Level}")));
Check("the ITEM slot survived the relog (SyncSkillBar kept the item: token, not wiped as a skill)",
      b.Bar is not null && b.Bar.Slots.Contains(itemToken));
Check("the PRESET slot survived the relog (SyncSkillBar kept the preset: token, not wiped as a skill)",
      b.Bar is not null && b.Bar.Slots.Contains(presetToken));
// `BL-250` — the whole point of the slot count being persisted. A slot that opens and is not written
// is the exact failure mode this harness was built for: correct on screen, gone on the next login.
Check("the unlocked SUBCLASS SLOT count survived the relog",
      b.Subclasses!.SlotsUnlocked == slotsUnlockedForRelog,
      $"{b.Subclasses.SlotsUnlocked}, expected {slotsUnlockedForRelog}");
Check($"levels survived the relog (main 81, subclass {ThirdClassCatalog.ChangeLevel + 4})",
      b.Subclasses!.Classes.First(c => c.Slot == mainSlot).Level == 81 &&
      b.Subclasses.Classes.First(c => c.Slot == subSlot).Level == ThirdClassCatalog.ChangeLevel + 4,
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
// 5a. BECOMING A CRAFTER (`BL-273` part 2, 0.203.0) — the Master Crafter's trial, PLAYED FOR REAL.
//
//     🔑 Why it is played and not debug-granted: 0.67.1 found the old profession quests unreachable by
//     normal play (a collect step that never counted) while the whole crafting suite stayed green on a
//     debug-granted profession. So the trial walks every beat through the real protocol: accept, the
//     pitch, the five gathered piles (the collect steps walk in ONE pass), the talk back, LEARNING the
//     recipe from the bag, the craft at the anvil — and, when the 40% roll fails, his *"fail go to 1"*,
//     checked on the wire — then the hand-in that makes you a crafter.
//     The 0.67.1 collect-count assertions live on here too: a partial pile must move the counter.
// -------------------------------------------------------------------------------------------
{
    string trial = QuestCatalog.QuestBecomeCrafter;
    var masterNpc = WorldMap.Npcs.First(n => n.Id == WorldMap.CraftMasterId);
    int Held(string defId) => b.Inv?.Items.Where(i => i.DefId == defId).Sum(i => i.Quantity) ?? 0;
    QuestSummary? Q() => b.Quests?.Active.FirstOrDefault(q => q.Id == trial);
    async Task GiveTrialMats()
    {
        foreach (var (id, n) in new[]
        {
            (ItemCatalog.CrafterQuestWood, 20), (ItemCatalog.CrafterQuestIron, 20), (ItemCatalog.CrafterQuestGem, 20),
            (ItemCatalog.CrafterQuestRecipe, 2), (ItemCatalog.CrafterHammerHead, 1),
        })
        {
            int missing = Math.Max(0, n - Held(id));
            if (missing > 0) await b.Hub.SendAsync("DebugGive", id, missing);
        }
        await b.Settle();
    }

    // ⚠ Section 5 signs off standing on the level-5 SUBCLASS, and the trial is level 40. Back to the
    // level-81 main.
    await b.Hub.SendAsync("SwitchSubclass", mainSlot);
    await b.Settle();

    // A GEAR RECIPE CANNOT BE LEARNED BY A NON-CRAFTER: the trial comes first.
    string t40Recipe = ItemCatalog.RecipeBookId("craft_sword1h_t40", 100);
    await b.Hub.SendAsync("DebugGive", t40Recipe, 1);
    await b.Settle();
    var t40Row = b.Inv?.Items.FirstOrDefault(i => i.DefId == t40Recipe);
    if (t40Row is not null) { await b.Hub.SendAsync("OpenBox", t40Row.InstanceId); await b.Settle(); }
    Check("🔑 a non-crafter cannot learn a gear recipe (the item is kept)",
          Held(t40Recipe) == 1 && !(b.Crafting?.KnownRecipes ?? Array.Empty<string>()).Any(k => k.StartsWith("craft_sword1h_t40:"))
          && b.Crafting is { IsCrafter: false },
          $"held {Held(t40Recipe)}, known [{string.Join(",", b.Crafting?.KnownRecipes ?? Array.Empty<string>())}]");

    // Stand at the Master. ⚠ A spawn carries the BARE name ("Gorran"); the title is split off.
    string masterGivenName = masterNpc.Name.Split(' ')[^1];
    await b.Hub.SendAsync("DebugTeleport", masterNpc.X, masterNpc.Y);
    // 🔑 Poll, never sleep: interest management runs on the SERVER's tick.
    await b.WaitFor(() => b.EntityNames.Any(kv => kv.Value == masterGivenName));
    var masterId = b.EntityNames.FirstOrDefault(kv => kv.Value == masterGivenName).Key;
    Check($"the Master Crafter ({masterNpc.Name}) is in view", masterId != Guid.Empty,
          $"saw [{string.Join(", ", b.EntityNames.Values.Distinct().Take(12))}]");

    if (masterId != Guid.Empty)
    {
        await b.Hub.SendAsync("QuestAction", "accept", trial, masterId);
        await b.Settle();
        await b.Hub.SendAsync("TalkToNpc", masterId);
        await b.Settle();
        var q = Q();
        Check("the trial is active, and his pitch hands over to the first GATHER step",
              q is { StepIndex: QuestCatalog.CrafterQuestGatherStep, CounterNeeded: 20 },
              q is null ? "quest not active at all" : $"step {q.StepIndex}, needs {q.CounterNeeded}");

        // 0.67.1's bug, kept: a PARTIAL pile moves the counter and does not advance the step.
        await b.Hub.SendAsync("DebugGive", ItemCatalog.CrafterQuestWood, 7);
        await b.Settle();
        q = Q();
        Check("🔴 quest wood arriving in the bag MOVES the counter (0/20 forever was the 0.67.1 bug)",
              q is { StepIndex: QuestCatalog.CrafterQuestGatherStep, Counter: 7 },
              $"step {q?.StepIndex}, counter {q?.Counter}");

        // All five piles at once: the collect steps WALK in one pass (wood, iron, gems, recipes, head).
        await GiveTrialMats();
        q = Q();
        Check("🔑 holding all five piles walks every gather step in ONE pass, to the talk-back",
              q is { StepIndex: 6 }, $"step {q?.StepIndex}");
        Check("nothing was consumed in the field — the mats are still carried",
              Held(ItemCatalog.CrafterQuestWood) >= 20 && Held(ItemCatalog.CrafterQuestRecipe) >= 2,
              $"wood {Held(ItemCatalog.CrafterQuestWood)}, recipes {Held(ItemCatalog.CrafterQuestRecipe)}");

        // Crafting BEFORE the craft step is refused (the hammer recipe is not even learned yet).
        await b.Hub.SendAsync("Craft", Crafting.HammerRecipeId, true, Crafting.HammerRecipePercent, 1);
        await b.Settle();
        Check("a hammer craft before the craft step is refused, and costs nothing",
              Held(ItemCatalog.CrafterHammer) == 0 && Held(ItemCatalog.CrafterQuestWood) >= 20,
              $"hammer {Held(ItemCatalog.CrafterHammer)}, wood {Held(ItemCatalog.CrafterQuestWood)}");

        await b.Hub.SendAsync("TalkToNpc", masterId);
        await b.Settle();
        q = Q();
        Check("bringing the mats back hands over to the LEARN step", q is { StepIndex: 7 }, $"step {q?.StepIndex}");

        // Learn one quest recipe from the bag (anywhere; here, at the Master).
        var rRow = b.Inv?.Items.FirstOrDefault(i => i.DefId == ItemCatalog.CrafterQuestRecipe);
        if (rRow is not null) { await b.Hub.SendAsync("OpenBox", rRow.InstanceId); await b.Settle(); }
        q = Q();
        Check("🔑 learning the hammer recipe from the bag credits the learn step, and takes NO slot",
              q is { StepIndex: QuestCatalog.CrafterQuestCraftStep }
              && (b.Crafting?.KnownRecipes ?? Array.Empty<string>()).Contains($"{Crafting.HammerRecipeId}:{Crafting.HammerRecipePercent}")
              && Held(ItemCatalog.CrafterQuestRecipe) == 1,
              $"step {q?.StepIndex}, known [{string.Join(",", b.Crafting?.KnownRecipes ?? Array.Empty<string>())}], "
              + $"recipes held {Held(ItemCatalog.CrafterQuestRecipe)}");

        // Try the craft until the hammer lands. 40% a try, so a fail is LIKELY and is checked when it
        // happens; the cap is a runaway guard. After a fail: back to step 1, mats and the used recipe
        // gone; re-supply, talk back (the learn step then passes on its own), and try again.
        int tries = 0, fails = 0;
        bool failPathOk = true;
        while (Held(ItemCatalog.CrafterHammer) == 0 && tries < 25)
        {
            tries++;
            await b.Hub.SendAsync("Craft", Crafting.HammerRecipeId, true, Crafting.HammerRecipePercent, 1);
            await b.Settle();
            if (Held(ItemCatalog.CrafterHammer) > 0) break;
            fails++;
            q = Q();
            failPathOk &= q is { StepIndex: QuestCatalog.CrafterQuestGatherStep }
                          && Held(ItemCatalog.CrafterQuestWood) == 0 && Held(ItemCatalog.CrafterHammerHead) == 0;
            await GiveTrialMats();
            await b.Hub.SendAsync("TalkToNpc", masterId);
            await b.Settle();
            failPathOk &= Q() is { StepIndex: QuestCatalog.CrafterQuestCraftStep };
        }
        Check("the hammer is forged at the anvil", Held(ItemCatalog.CrafterHammer) == 1,
              $"{tries} tries, {fails} fails");
        if (fails > 0)
            Check($"🔑 a FAILED hammer ({fails}x) sends the trial back to step 1 with the mats gone, and the "
                  + "learn step passes on its own the second time round", failPathOk);
        else
            Console.WriteLine("  (info) the hammer landed first try, so the fail path was not exercised this run");
        q = Q();
        Check("the forged hammer hands over to the last step", q is { CanComplete: true }, $"step {q?.StepIndex}");

        await b.Hub.SendAsync("QuestAction", "complete", trial, masterId);
        await b.Settle();
        var cu5 = b.Crafting;
        Check("🔴 THE TRIAL COMPLETES — a crafter by PLAYING it: 10 slots, generic and types at L0",
              cu5 is { IsCrafter: true, Slots: 10, GenericPoints: 0, FreePoints: 0, Respecs: 0 }
              && (cu5.TypeLevels?.All(v => v == 0) ?? false)
              && (b.Quests?.Completed.Contains(trial) ?? false),
              $"crafter {cu5?.IsCrafter}, slots {cu5?.Slots}, completed={b.Quests?.Completed.Contains(trial)}");
        Check("...he took the hammer, and the trial's recipe is forgotten (it held no slot anyway)",
              Held(ItemCatalog.CrafterHammer) == 0
              && !(cu5?.KnownRecipes ?? Array.Empty<string>()).Any(k => k.StartsWith(Crafting.HammerRecipeId + ":")),
              $"hammer {Held(ItemCatalog.CrafterHammer)}, known [{string.Join(",", cu5?.KnownRecipes ?? Array.Empty<string>())}]");
    }

    // Leave the Master: 5b's first assertion is that a craft away from an anvil is refused.
    await b.Hub.SendAsync("DebugTeleport", masterNpc.X + 3000, masterNpc.Y);
    await b.Settle();
}

// -------------------------------------------------------------------------------------------
// 5b. CRAFTING AS A CRAFTER (`BL-273` part 2) — recipe %, slots, points, forgetting, the shelf.
//
//     ⚠ NOTHING HERE ASSERTS "the item appeared" for a < 100% craft — that is a coin flip. What IS
//     deterministic is the bookkeeping, so that is what is checked:
//       • learning takes ONE slot at the item's %; a higher % overrides in place, a lower one is refused,
//       • every attempt spends ONE recipe item and the inputs SCALED by its % (pass or fail),
//       • 0.204.0: a T76 recipe needs Weaponsmith L6, bought with generic points; every attempt pays
//         tier-weighted points (T76 = 5) to the GENERIC level only, fail included; a respec locks, never forgets,
//       • a % above the learned one is refused; away from a Master is refused,
//       • forgetting frees the slot; the debug levels move the slot count (10 → 60),
//       • the Master sells a T40 100% recipe at 10% of the piece, and teaches a generic recipe for gold.
// -------------------------------------------------------------------------------------------
{
    const string recipeId = "craft_sword1h_t76";      // T76: recipes exist at 20 / 40 / 60 %
    var recipe = RecipeCatalog.Get(recipeId);
    Check("RecipeCatalog initialises without throwing (the static-init bug is fixed)", recipe is not null);
    if (recipe is not null)
    {
        int Count(string defId) => b.Inv?.Items.Where(i => i.DefId == defId).Sum(i => i.Quantity) ?? 0;
        string[] Known() => b.Crafting?.KnownRecipes ?? Array.Empty<string>();
        string r20 = ItemCatalog.RecipeBookId(recipeId, 20), r40 = ItemCatalog.RecipeBookId(recipeId, 40),
               r60 = ItemCatalog.RecipeBookId(recipeId, 60);
        async Task Learn(string itemId)
        {
            var row = b.Inv?.Items.FirstOrDefault(i => i.DefId == itemId);
            if (row is not null) { await b.Hub.SendAsync("OpenBox", row.InstanceId); await b.Settle(); }
        }

        await b.Hub.SendAsync("DebugGive", r20, 1);
        await b.Hub.SendAsync("DebugGive", r40, 4);
        await b.Hub.SendAsync("DebugGive", r60, 1);
        await b.Settle();

        await Learn(r40);
        Check("🔑 0.204.0: a T76 recipe is refused below Weaponsmith L6 (it is kept)",
              !Known().Any(k => k.StartsWith(recipeId + ":")) && Count(r40) == 4, $"r40 {Count(r40)}");
        await b.Hub.SendAsync("DebugSetCraftLevels", 6, 0, 0, 0, 0, 0);
        await b.Settle();
        Check("generic L6 = 6 free points, nothing spent", b.Crafting is { FreePoints: 6 }, $"free {b.Crafting?.FreePoints}");
        for (int i = 0; i < 6; i++) { await b.Hub.SendAsync("SpendCraftPoint", (int)CraftType.Weapon); await b.Settle(); }
        await b.Hub.SendAsync("SpendCraftPoint", (int)CraftType.Scribe);
        await b.Settle();
        Check("🔑 spending: six points make Weaponsmith L6, and a seventh with none free is refused",
              b.Crafting is { FreePoints: 0 } cp && cp.TypeLevels?[(int)CraftType.Weapon] == 6
              && cp.TypeLevels?[(int)CraftType.Scribe] == 0,
              $"free {b.Crafting?.FreePoints}, levels [{string.Join(",", b.Crafting?.TypeLevels ?? Array.Empty<int>())}]");
        int genBase = b.Crafting?.GenericPoints ?? 0;

        await Learn(r40);
        Check("learning a 40% recipe fills ONE slot at 40%", Known().Contains($"{recipeId}:40") && Count(r40) == 3,
              $"known [{string.Join(",", Known())}], r40 {Count(r40)}");
        await Learn(r20);
        Check("🔑 a LOWER % is refused and KEPT (it is what you craft with)",
              Known().Contains($"{recipeId}:40") && Count(r20) == 1, $"r20 {Count(r20)}");
        await Learn(r60);
        Check("🔑 a HIGHER % overrides the slot IN PLACE (still one slot)",
              Known().Contains($"{recipeId}:60") && Known().Count(k => k.StartsWith(recipeId + ":")) == 1 && Count(r60) == 0,
              $"known [{string.Join(",", Known())}]");

        var scaled = recipe.Inputs.Select(i => (i.ItemId, Qty: Crafting.InputQty(recipe, i, 40))).ToArray();
        foreach (var (id, qty) in scaled) await b.Hub.SendAsync("DebugGive", id, qty * 3);
        await b.Settle();
        int mat0 = Count(scaled[0].ItemId);

        await b.Hub.SendAsync("Craft", recipeId, false, 40, 1);
        await b.Settle();
        Check("🔑 a craft AWAY FROM A MASTER is refused, and costs nothing",
              Count(r40) == 3 && Count(scaled[0].ItemId) == mat0, $"r40 {Count(r40)}, mats {Count(scaled[0].ItemId)}/{mat0}");

        var master = WorldMap.Npcs.First(n => n.Id == WorldMap.CraftMasterId);
        await b.Hub.SendAsync("DebugTeleport", master.X, master.Y);
        await b.WaitFor(() => b.Crafting is { AtMaster: true });
        Check("the crafting push says AT MASTER once you stand at one", b.Crafting is { AtMaster: true });

        await b.Hub.SendAsync("Craft", recipeId, false, 100, 1);
        await b.Settle();
        Check("a % ABOVE the learned one (or one that does not exist at this tier) is refused",
              Count(r40) == 3 && Count(scaled[0].ItemId) == mat0);

        int made0 = Count(recipe.OutputId);
        // 0.205.0: a T76 1H costs 300 MP an attempt, so refill between them (/heal, admin) — the MP gate is real.
        for (int i = 0; i < 2; i++)
        {
            await b.Hub.SendAsync("AdminCommand", "heal", "");
            await b.Settle();
            await b.Hub.SendAsync("Craft", recipeId, false, 40, 1);
            await b.Settle();
        }
        int attempts = 2;
        Check("🔑 each attempt spends ONE 40% recipe and the inputs scaled to 50%, pass or fail",
              Count(r40) == 3 - attempts && Count(scaled[0].ItemId) == mat0 - attempts * scaled[0].Qty,
              $"r40 {Count(r40)}, {scaled[0].ItemId} {Count(scaled[0].ItemId)} (expected {mat0 - attempts * scaled[0].Qty})");
        Check("🔑 every attempt pays T76 points (5) to the GENERIC level only — a fail too",
              b.Crafting is { } cu && cu.GenericPoints == genBase + 5 * attempts && cu.TypeLevels?[(int)CraftType.Weapon] == 6,
              $"generic {b.Crafting?.GenericPoints} (from {genBase}), weapon L{b.Crafting?.TypeLevels?[(int)CraftType.Weapon]}");
        Console.WriteLine($"  (info) {Count(recipe.OutputId) - made0} of {attempts} 40% attempts succeeded");

        await b.Hub.SendAsync("DebugSetCraftLevels", 10, 6, 0, 0, 0, 0);
        await b.Settle();
        Check("debug craft levels: generic 10 = 60 slots", b.Crafting is { Slots: 60 }, $"slots {b.Crafting?.Slots}");
        await b.Hub.SendAsync("DebugSetCraftLevels", 0, 0, 0, 0, 0, 0);
        await b.Settle();
        Check("...and generic 0 = 10 slots", b.Crafting is { Slots: 10, GenericPoints: 0 }, $"slots {b.Crafting?.Slots}");

        await b.Hub.SendAsync("ForgetRecipe", recipeId);
        await b.Settle();
        Check("🔑 forgetting frees the slot", !Known().Any(k => k.StartsWith(recipeId + ":")),
              $"known [{string.Join(",", Known())}]");
        int r40Before = Count(r40);
        await b.Hub.SendAsync("Craft", recipeId, false, 40, 1);
        await b.Settle();
        Check("...and a forgotten recipe cannot be crafted", Count(r40) == r40Before);

        // THE SHELF: the Master sells the T40 100% recipe at 10% of the piece.
        string masterGiven = master.Name.Split(' ')[^1];
        await b.WaitFor(() => b.EntityNames.Any(kv => kv.Value == masterGiven));
        var masterId = b.EntityNames.FirstOrDefault(kv => kv.Value == masterGiven).Key;
        string shelf = ItemCatalog.RecipeBookId("craft_sword1h_t40", 100);
        int price = ItemCatalog.BuyPrice(ItemCatalog.Get(shelf)!);
        await b.Hub.SendAsync("DebugGold", 5_000_000L);
        await b.Settle();
        long gold0 = b.Gold;
        int held0 = Count(shelf);
        await b.Hub.SendAsync("BuyItem", masterId, shelf, 1);
        await b.Settle();
        Check("🔑 the Master Crafter SELLS the T40 100% recipe, at 10% of the piece",
              Count(shelf) == held0 + 1 && b.Gold == gold0 - price
              && price > 1 && ItemCatalog.Get(shelf)!.Value == Math.Max(1, (int)Math.Round(ItemCatalog.Get("sword1h_t40")!.Value * 0.10)),
              $"held {held0}->{Count(shelf)}, gold {gold0}->{b.Gold}, price {price}");

        // A GENERIC recipe, taught for gold, crafted with no recipe item, paying 1 generic point only.
        var generic = RecipeCatalog.GenericForSale.First(r => r.UnlockLevel == 0 && r.Id.StartsWith("craft_"));
        long gold1 = b.Gold;
        await b.Hub.SendAsync("LearnRecipeAtMaster", masterId, generic.Id);
        await b.Settle();
        Check($"🔑 the Master TEACHES a generic recipe ({generic.Id}) for gold, into a slot at 100%",
              Known().Contains($"{generic.Id}:100") && b.Gold == gold1 - generic.LearnPrice,
              $"known [{string.Join(",", Known())}], gold {gold1}->{b.Gold} (price {generic.LearnPrice})");
        foreach (var inp in generic.Inputs) await b.Hub.SendAsync("DebugGive", inp.ItemId, inp.Qty);
        await b.Settle();
        int gIn0 = Count(generic.Inputs[0].ItemId);
        await b.Hub.SendAsync("Craft", generic.Id, false, 0, 1);
        await b.Settle();
        Check("a generic craft spends its inputs (no recipe item) and pays 1 GENERIC point, no type point",
              Count(generic.Inputs[0].ItemId) == gIn0 - generic.Inputs[0].Qty
              && b.Crafting is { GenericPoints: 1 },
              $"input {gIn0}->{Count(generic.Inputs[0].ItemId)}, generic {b.Crafting?.GenericPoints}");

        // 0.205.0 — A REFINE WITH A COUNT: one command repeats until something runs out, pays MP, no gold,
        // and NO craft points (*"refines pay 0"*). 50 normal Nightsilver asked x100 = 5 refines, then stops.
        const string refineId = "refine_nightsilver_1";
        await b.Hub.SendAsync("LearnRecipeAtMaster", masterId, refineId);
        await b.Hub.SendAsync("DebugGive", Crafting.NightsilverId(0), 50);
        await b.Settle();
        int ns0 = Count(Crafting.NightsilverId(0)), ns1 = Count(Crafting.NightsilverId(1));
        int gp0 = b.Crafting?.GenericPoints ?? -1;
        await b.Hub.SendAsync("Craft", refineId, false, 0, 100);
        await b.Settle();
        Check("🔑 a refine x100 with 50 normal Nightsilver makes 5 Refined, spends all 50, and pays NO craft point",
              Known().Contains($"{refineId}:100") && Count(Crafting.NightsilverId(0)) == ns0 - 50
              && Count(Crafting.NightsilverId(1)) == ns1 + 5 && b.Crafting?.GenericPoints == gp0,
              $"known {Known().Contains($"{refineId}:100")}, normal {ns0}->{Count(Crafting.NightsilverId(0))}, "
              + $"refined {ns1}->{Count(Crafting.NightsilverId(1))}, generic {gp0}->{b.Crafting?.GenericPoints}");

        // 0.204.0 — A RESPEC LOCKS, IT NEVER FORGETS (*"each repec locks the recipies … only can be removed by
        // hand to free up slot - never crafted if not that lvl of that type"*).
        await b.Hub.SendAsync("DebugSetCraftLevels", 10, 6, 0, 0, 0, 0);
        await b.Hub.SendAsync("DebugGive", r20, 2);
        await b.Hub.SendAsync("DebugGold", 5_000_000L);
        await b.Settle();
        await Learn(r20);
        Check("a Weaponsmith L6 learns the T76 recipe again (at 20%)", Known().Contains($"{recipeId}:20"),
              $"known [{string.Join(",", Known())}]");
        long gold2 = b.Gold;
        await b.Hub.SendAsync("RespecCraft", masterId);
        await b.Settle();
        Check("🔑 RESPEC at the Master: every point back, 1M, 1 of 5 used — and the recipe is KEPT",
              b.Crafting is { FreePoints: 10, Respecs: 1 } rs && (rs.TypeLevels?.All(v => v == 0) ?? false)
              && b.Gold == gold2 - Crafting.RespecPrices[0] && Known().Contains($"{recipeId}:20"),
              $"free {b.Crafting?.FreePoints}, respecs {b.Crafting?.Respecs}, gold {gold2}->{b.Gold}, "
              + $"known [{string.Join(",", Known())}]");
        foreach (var inp in recipe.Inputs) await b.Hub.SendAsync("DebugGive", inp.ItemId, Crafting.InputQty(recipe, inp, 20));
        await b.Settle();
        int r20Before = Count(r20);
        await b.Hub.SendAsync("Craft", recipeId, false, 20, 1);
        await b.Settle();
        Check("🔑 ...and a LOCKED recipe will not craft below its gate (nothing spent)", Count(r20) == r20Before && r20Before > 0,
              $"r20 {r20Before}->{Count(r20)}");
    }
}

// -------------------------------------------------------------------------------------------
// 5c. THE DAILY RECIPE QUESTS (`BL-274` part 3, 0.208.0) — the Frostmere givers, PLAYED on the level-81 main.
//
//     Weaponwright Harrow's T80 quest walked through the real protocol: accept, the talk to Edda, 8 kills, the
//     talk to Ossian, 8 more, the hand-in. The kills are credited by `DebugQuestKill`, which runs the REAL
//     AdvanceKillQuests matching, so a wrong creature must still be refused. Then the ruled rules:
//       • holding the T80 quest bars the T76 one (one errand a day, not two in parallel),
//       • the reward is exactly ONE 40% book, and it is one of the KIND's (the 8 Soulcrystal weapons),
//       • 🔑 the SHARED stamp: after the hand-in the T76 quest is closed for the day too,
//       • the other kinds keep their own stamp (the armourer still offers hers).
// -------------------------------------------------------------------------------------------
{
    int Held(string defId) => b.Inv?.Items.Where(i => i.DefId == defId).Sum(i => i.Quantity) ?? 0;

    // Offline: the three pools are exactly the kind's 40% books at each tier (1/8, 1/7, 1/3 uniform).
    bool poolsOk = true;
    string poolInfo = "";
    foreach (var (kind, keys) in new[]
    {
        ("weapon", new[] { "sword1h", "sword2h", "blunt1h", "blunt2h", "duals", "bow", "wand", "staff" }),
        ("armour", new[] { "heavy", "light", "robe", "helm", "gloves", "boots", "shield" }),
        ("jewel",  new[] { "necklace", "ring", "earring" }),
    })
        foreach (int tier in new[] { 76, 80 })
        {
            var qd = QuestCatalog.Get(QuestCatalog.RecipeQuestId(kind, tier));
            var want = keys.Select(k => ItemCatalog.RecipeBookId($"craft_{k}_t{tier}", 40)).OrderBy(x => x).ToArray();
            var got = (qd?.Reward.RandomItemIds ?? Array.Empty<string>()).OrderBy(x => x).ToArray();
            bool ok = qd is { Daily: true } && qd.DailyGroup == $"daily_recipe_{kind}"
                      && got.SequenceEqual(want) && got.All(id => ItemCatalog.Get(id) is not null)
                      && qd.Reward is { Exp: 0, Gold: 0, SkillPoints: 0, ItemIds: null }
                      && qd.MinLevel == (tier == 76 ? 75 : 80) && qd.MaxLevel == (tier == 76 ? 85 : 0);
            if (!ok) poolInfo += $" {kind}/t{tier}: [{string.Join(",", got)}]";
            poolsOk &= ok;
        }
    Check("the six recipe dailies: each pays ONE book from exactly its kind's 40% set (8/7/3), nothing else, "
          + "T76 75-85, T80 80+", poolsOk, poolInfo);

    var harrow = WorldMap.Npcs.First(n => n.Id == QuestCatalog.RecipeWeaponGiver);
    await b.Hub.SendAsync("DebugTeleport", harrow.X + 100, harrow.Y + 500);
    Guid NpcId(string fullName) => b.EntityNames.FirstOrDefault(kv => kv.Value == fullName.Split(' ')[^1]).Key;
    await b.WaitFor(() => NpcId("Weaponwright Harrow") != Guid.Empty && NpcId("Armourer Edda") != Guid.Empty
                          && NpcId("Jeweller Ossian") != Guid.Empty);
    Guid harrowId = NpcId("Weaponwright Harrow"), eddaId = NpcId("Armourer Edda"), ossianId = NpcId("Jeweller Ossian");
    // 'TalkRange' applies to talks, accepts and hand-ins: stand beside each NPC first (one column, 550 apart).
    async Task At(string npcId) { var n = WorldMap.Npcs.First(x => x.Id == npcId); await b.Hub.SendAsync("DebugTeleport", n.X + 60, n.Y); await b.Settle(); }
    Check("the three Frostmere recipe givers are in view", harrowId != Guid.Empty && eddaId != Guid.Empty && ossianId != Guid.Empty,
          $"harrow {harrowId}, edda {eddaId}, ossian {ossianId}");

    if (harrowId != Guid.Empty && eddaId != Guid.Empty && ossianId != Guid.Empty)
    {
        string t80 = QuestCatalog.RecipeQuestId("weapon", 80), t76 = QuestCatalog.RecipeQuestId("weapon", 76);
        QuestSummary? Q() => b.Quests?.Active.FirstOrDefault(q => q.Id == t80);
        QuestEntry? Entry(string id) => b.Quests?.Entries.FirstOrDefault(e => e.Id == id);

        await At(QuestCatalog.RecipeWeaponGiver);
        await b.Hub.SendAsync("QuestAction", "accept", t80, harrowId);
        await b.Settle();
        await b.Hub.SendAsync("QuestAction", "accept", t76, harrowId);
        await b.Settle();
        Check("🔑 holding the T80 quest BARS the T76 one (one errand a day, not two in parallel)",
              Q() is not null && b.Quests!.Active.All(q => q.Id != t76) && Entry(t76)?.State == QuestAvailability.Locked,
              $"active [{string.Join(",", b.Quests?.Active.Select(q => q.Id) ?? Array.Empty<string>())}], "
              + $"t76 {Entry(t76)?.State} '{Entry(t76)?.Status}'");

        await At(QuestCatalog.RecipeArmourGiver);
        await b.Hub.SendAsync("TalkToNpc", eddaId);
        await b.Settle();
        Check("talking to Edda hands over to her 8 kills", Q() is { StepIndex: 1, CounterNeeded: 8 },
              $"step {Q()?.StepIndex}, needs {Q()?.CounterNeeded}");
        await b.Hub.SendAsync("DebugQuestKill", "radiant_scout", 8);     // Ossian's creature, not Edda's
        await b.Settle();
        Check("...the WRONG creature does not count", Q() is { StepIndex: 1, Counter: 0 }, $"step {Q()?.StepIndex}, {Q()?.Counter}");
        await b.Hub.SendAsync("DebugQuestKill", "wrathborn_demon", 8);
        await b.Settle();
        await At(QuestCatalog.RecipeJewelGiver);
        await b.Hub.SendAsync("TalkToNpc", ossianId);
        await b.Settle();
        await b.Hub.SendAsync("DebugQuestKill", "radiant_scout", 8);
        await b.Settle();
        Check("8 Wrathborn Demons, Ossian, 8 Radiant Scouts: ready to hand in at Harrow", Q() is { StepIndex: 4, CanComplete: true },
              $"step {Q()?.StepIndex}, canComplete {Q()?.CanComplete}");

        var weaponBooks = QuestCatalog.Get(t80)!.Reward.RandomItemIds!;
        var allBooks = ItemCatalog.AllItems.Where(d => d.Id.StartsWith("recipe_craft_")).Select(d => d.Id).ToArray();
        var before = allBooks.ToDictionary(id => id, Held);
        await At(QuestCatalog.RecipeWeaponGiver);
        await b.Hub.SendAsync("QuestAction", "complete", t80, harrowId);
        await b.Settle();
        var gained = allBooks.Where(id => Held(id) > before[id]).ToArray();
        int gainedTotal = allBooks.Sum(id => Held(id) - before[id]);
        Check("🔑 the hand-in pays exactly ONE 40% book, and it is a Soulcrystal WEAPON",
              gainedTotal == 1 && gained.Length == 1 && weaponBooks.Contains(gained[0]) && gained[0].EndsWith("_t80_40"),
              $"gained [{string.Join(",", gained)}] total {gainedTotal}");

        await b.Hub.SendAsync("QuestAction", "accept", t76, harrowId);
        await b.Settle();
        Check("🔴🔑 the SHARED stamp: after the T80 hand-in the T76 quest is closed for the day too",
              b.Quests!.Active.All(q => q.Id != t76) && Entry(t76)?.State == QuestAvailability.Completed
              && Entry(t80)?.State == QuestAvailability.Completed,
              $"t76 {Entry(t76)?.State} '{Entry(t76)?.Status}', t80 {Entry(t80)?.State}");

        string armour76 = QuestCatalog.RecipeQuestId("armour", 76);
        await At(QuestCatalog.RecipeArmourGiver);
        await b.Hub.SendAsync("QuestAction", "accept", armour76, eddaId);
        await b.Settle();
        Check("...and the other kinds keep their OWN stamp: Edda still gives hers",
              b.Quests!.Active.Any(q => q.Id == armour76), $"active [{string.Join(",", b.Quests.Active.Select(q => q.Id))}]");
        await b.Hub.SendAsync("QuestAction", "abandon", armour76, Guid.Empty);
        await b.Settle();

        // `BL-296` / playtest 0.214.1: *"the reset limits don't reset my daily apoth rune quest"*. The
        // weapon group is stamped for today (checked above); /resetlimits must hand it straight back.
        await At(QuestCatalog.RecipeWeaponGiver);
        await b.Hub.SendAsync("AdminCommand", "resetlimits", "");
        await b.Settle();
        Check("🔑 /resetlimits clears today's stamp: the T76 quest is Available again",
              Entry(t76)?.State == QuestAvailability.Available,
              $"t76 {Entry(t76)?.State} '{Entry(t76)?.Status}'");
        await b.Hub.SendAsync("QuestAction", "accept", t76, harrowId);
        await b.Settle();
        Check("...and Harrow really hands it over", b.Quests!.Active.Any(q => q.Id == t76),
              $"active [{string.Join(",", b.Quests.Active.Select(q => q.Id))}]");
        await b.Hub.SendAsync("QuestAction", "abandon", t76, Guid.Empty);
        await b.Settle();
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
var bLeave = await b.LeaveWorldAsync();
Check("the protagonist left cleanly before the moderation section", bLeave is null, bLeave);
await b.DisposeAsync();

var v = await ConnectAsync("test1", "test");
var enteredV = await v.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(victimId));
Check("the plain victim entered the world", enteredV.Success, enteredV.Error);
v.MyId = enteredV.EntityId;
await v.Settle();

// -------------------------------------------------------------------------------------------
// 6a. THE CHAT LOG READER (`BL-89`). Exactly the bug class this harness exists for: the WRITE half
//     shipped in 0.81.0 and looked perfect for weeks, because nothing ever READ it back. A query
//     that SQLite refuses to translate, a name match that misses on case, a page that comes back
//     newest-first — every one of those renders as a tidy, plausible, WRONG page in the System tab.
//
//     It runs here because it needs two people online: a whisper has to have somebody to arrive at,
//     and the private channel is the case the whole feature exists for.
{
    string gmName = gmChars.Characters[0].Name;
    string mark = "smk" + DateTime.UtcNow.ToString("HHmmssff");   // unique per run, so no run sees another's lines

    await gm.Hub.SendAsync("Chat", $"local-{mark}", ChatChannel.Local, null);
    await gm.Hub.SendAsync("Chat", $"whisper-{mark}", ChatChannel.Whisper, victimName);
    await gm.Settle();

    // NO WAIT FOR THE AUTOSAVE. Lines sit in a pending buffer until the 60-second save, and the
    // command flushes it before querying — precisely so a moderator acting on a LIVE report
    // ("he is whispering me right now") does not read an empty page and conclude innocence.
    // If that flush is ever removed, this check fails within 500 ms instead of passing by luck.
    gm.SystemChat.Clear();
    await gm.Hub.SendAsync("AdminCommand", "chatlog", gmName);
    bool sawBoth = await gm.WaitFor(() =>
        gm.SystemChat.Any(s => s.Contains($"local-{mark}")) &&
        gm.SystemChat.Any(s => s.Contains($"whisper-{mark}")));
    Check("/chatlog <name> returns lines said SECONDS ago (the pending buffer is flushed, not waited on)",
          sawBoth, string.Join(" | ", gm.SystemChat));

    // Case-insensitivity, the same lesson `/jail` learned: `=` on TEXT is case-SENSITIVE in SQLite,
    // and an empty page is the most misleading answer this command can give — it reads as "this
    // player never said anything".
    gm.SystemChat.Clear();
    await gm.Hub.SendAsync("AdminCommand", "chatlog", gmName.ToLowerInvariant());
    Check("a lower-case name still finds the lines",
          await gm.WaitFor(() => gm.SystemChat.Any(s => s.Contains($"local-{mark}"))),
          string.Join(" | ", gm.SystemChat));

    // -w is the channel the feature is FOR: it must keep the whisper and drop the public line.
    gm.SystemChat.Clear();
    await gm.Hub.SendAsync("AdminCommand", "chatlog", $"{gmName} -w");
    bool whisperOnly = await gm.WaitFor(() => gm.SystemChat.Any(s => s.Contains($"whisper-{mark}")))
                       && !gm.SystemChat.Any(s => s.Contains($"local-{mark}"));
    Check("/chatlog <name> -w keeps the whisper and drops the public line", whisperOnly,
          string.Join(" | ", gm.SystemChat));

    // A whisper has two ends and a report can name either. Asking about the VICTIM must find what was
    // said TO them — the reporting player is the one whose name a moderator has.
    gm.SystemChat.Clear();
    await gm.Hub.SendAsync("AdminCommand", "chatlog", $"{victimName} -w");
    Check("a name query finds whispers RECEIVED, not only sent",
          await gm.WaitFor(() => gm.SystemChat.Any(s => s.Contains($"whisper-{mark}"))),
          string.Join(" | ", gm.SystemChat));

    // `around <time>`: the relative form, which is what a real report produces ("about ten minutes ago").
    gm.SystemChat.Clear();
    await gm.Hub.SendAsync("AdminCommand", "chatlog", "around 1m");
    Check("/chatlog around 1m reads the window instead of the tail",
          await gm.WaitFor(() => gm.SystemChat.Any(s => s.Contains($"local-{mark}"))),
          string.Join(" | ", gm.SystemChat));

    // A page past the end must say so, not fall over or silently repeat page 1.
    gm.SystemChat.Clear();
    await gm.Hub.SendAsync("AdminCommand", "chatlog", $"{gmName} -p 99");
    Check("a page past the end reports empty rather than repeating page 1",
          await gm.WaitFor(() => gm.SystemChat.Any(s => s.Contains("no lines match")))
          && !gm.SystemChat.Any(s => s.Contains($"local-{mark}")),
          string.Join(" | ", gm.SystemChat));
}

// JAIL the victim live.
v.MyX = 0; v.MyY = 0;
await gm.Hub.SendAsync("AdminCommand", "jail", $"{victimName} 60");
await v.Settle();
// "In the YARD", not "on the jail coordinate": arrivals are spread across the 300x500 room now
// (owner, playtest-20 `61d`), so an exact-centre assertion would fail for the right reason.
bool atJail = WorldDomain.Jail.Contains(v.MyX, v.MyY);
Check("jailing a player teleports them to jail (live)", atJail, $"at ({v.MyX:0},{v.MyY:0})");

// The 60-min jail also DRAINED the player's LIFETIME charisma (−200) below the +10 they'd been recommended for → off the board.
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
    var gmLeave = await gm.LeaveWorldAsync();
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
// 9. THE PREMIUM REWARD RUNES (`BL-01`) — the best rung wins, and the screen tells the truth.
// -------------------------------------------------------------------------------------------
// Three failures live here, and NONE of them is visible in a playtest:
//   (1) Two rungs of one channel both applying. The buff bar would show one square either way (the
//       family key is shared), and the exp would quietly be +105% instead of +100%.
//   (2) The drop list showing the SERVER's rate while the kill rolls the PLAYER's. That is the exact
//       bug CLAUDE.md's rates rule exists to prevent, and a wrong percentage on a popup looks like a
//       percentage.
//   (3) A rune buff saved as an ordinary buff, so login re-applies it on top of the reconciled one —
//       the same shape as the buff double-apply §4d guards, but now driven by a catalog lookup
//       instead of a hardcoded pair of ids.
{
    var expChannel = RewardRunes.All.First(c => c.Key == RewardRunes.KeyExp);
    var dropChannel = RewardRunes.All.First(c => c.Key == RewardRunes.KeyDrop);
    string gmName = gmChars.Characters[0].Name;

    // ⚠ START FROM A KNOWN BAG. This is the one section that runs on the ADMIN's persistent character
    // rather than a fresh Smoke<timestamp> one, and a reward rune lives for 24 HOURS — so the runes the
    // last run handed out are still there, and "the weak rung goes up first" silently became "the 100%
    // rune from an hour ago is still winning". A test that is not idempotent lies to you.
    // (AdminRemoveItem is the only path that can take a rune: the bin refuses one by design.)
    foreach (var stale in (gm.Inv?.Items ?? Array.Empty<InventoryItemDto>())
             .Where(i => ItemCatalog.Get(i.DefId) is { IsRune: true } d
                         && SkillCatalog.Get(d.RuneBuffSkillId) is SkillDef s && !s.RewardsAt(1).IsNeutral)
             .ToList())
        await gm.Hub.SendAsync("AdminRemoveItem", gmName, stale.InstanceId);
    await gm.Settle();
    await Task.Delay(1200);   // the buff goes with the item on the next reconcile pass (~1/s)
    Check("the admin's bag starts with no reward rune from a previous run",
          (gm.Buffs?.Buffs ?? Array.Empty<BuffDto>()).All(b => !SkillCatalog.IsRuneBuff(b.Key)
              || b.Key == SkillCatalog.WarRuneBuff || b.Key == SkillCatalog.SpellRuneBuff),
          string.Join(",", (gm.Buffs?.Buffs ?? Array.Empty<BuffDto>()).Select(b => b.Key)));

    // The WEAK rung first, so the strong one has to evict something that is already running.
    await gm.Hub.SendAsync("AdminCommand", "give", $"{gmName} {expChannel.ItemId(5)}");
    await gm.Settle();
    var weak = gm.Buffs?.Buffs.FirstOrDefault(b => b.Key == expChannel.SkillId);
    Check("a held Rune of Experience puts its buff up (`BL-01`)", weak is not null,
          string.Join(",", (gm.Buffs?.Buffs ?? Array.Empty<BuffDto>()).Select(b => b.Key)));
    Check("...named after the RUNG, so the bar says which one it is",
          weak?.Name == expChannel.NameAt(5), weak?.Name);
    Check("...and its popup states that rung's own number, not the ladder's first",
          weak?.Description.Contains("+5%") == true, weak?.Description);

    await gm.Hub.SendAsync("AdminCommand", "give", $"{gmName} {expChannel.ItemId(100)}");
    await gm.Settle();
    var expBuffs = (gm.Buffs?.Buffs ?? Array.Empty<BuffDto>()).Where(b => b.Key == expChannel.SkillId).ToList();
    Check("🔑 holding TWO rungs runs exactly ONE buff (they never stack into +105%)",
          expBuffs.Count == 1, $"{expBuffs.Count} copies of '{expChannel.SkillId}'");
    Check("...and it is the STRONGER rung, even though the weaker one was already up",
          expBuffs.Count == 1 && expBuffs[0].Name == expChannel.NameAt(100),
          expBuffs.FirstOrDefault()?.Name);

    // (2) The drop list must move with the rune. Read a mob's table before and after.
    // ⚠ Pick a creature that HAS a drop table, by name, from the catalog — "the first entity that isn't
    // me" is a gatekeeper or another player as often as not, and an empty list would make this pass by
    // measuring nothing.
    var dropping = MobCatalog.Templates
        .Where(m => m.Drops is { Length: > 0 })
        .Select(m => m.Name).ToHashSet(StringComparer.Ordinal);
    // 🔴 …AND IN THE GM'S OWN LEVEL BAND. Since playtest 23 the inspect screen applies the LEVEL-GAP
    // penalty the kill roll always applied (his `76e`), so a level-90 admin reading a level-4 fox is now
    // correctly shown 0.00% on every row — and a rune cannot double zero. The old "first zone with
    // drops" pick was a low-level field, which turned this whole check into 0 vs 0 the moment the
    // display became honest. Pick a field this character can actually farm.
    int myLevel = Math.Max(1, gm.Progress?.Level ?? 1);
    var withMobs = WorldMap.SpawnZones
        .Where(z => z.MobTypes.Any(t => MobCatalog.Get(t).Drops is { Length: > 0 }))
        .OrderBy(z => Math.Abs((z.MinLevel + z.MaxLevel) / 2 - myLevel))
        .First();
    await gm.Hub.SendAsync("DebugTeleport", withMobs.X, withMobs.Y);
    await gm.Settle();
    var mob = gm.EntityNames.FirstOrDefault(kv => kv.Key != gm.MyId && dropping.Contains(kv.Value)).Key;
    Check("a creature with a drop table is standing here to inspect", mob != Guid.Empty,
          string.Join(" / ", gm.EntityNames.Values.Take(6)));
    if (mob != Guid.Empty)
    {
        await gm.Hub.SendAsync("InspectTarget", mob, true);
        await gm.Settle();
        var before = gm.Details?.Drops;

        await gm.Hub.SendAsync("AdminCommand", "give", $"{gmName} {dropChannel.ItemId(100)}");
        await gm.Settle();
        await gm.Hub.SendAsync("InspectTarget", mob, true);
        await gm.Settle();
        var after = gm.Details?.Drops;

        // Compare the PERCENTAGES row by row, keyed on the row's label ("Mats", "Armor · Rare",
        // "   Fox Pelt"). The value is everything inside the trailing parentheses.
        static Dictionary<string, double> Rows(string[]? lines)
        {
            var d = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var l in lines ?? Array.Empty<string>())
            {
                int i = l.LastIndexOf('(');
                if (i < 0) continue;
                if (double.TryParse(l.Substring(i + 1).TrimEnd(')', '%', ' '),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var v))
                    d[l.Substring(0, i).TrimEnd()] = v;
            }
            return d;
        }

        var rowsBefore = Rows(before);
        var rowsAfter = Rows(after);
        // ⚠ Only the rows well BELOW the clamp can show a doubling at all: a group already firing at 70%
        // or 100% is pinned there, and its members move by a fraction. Measuring the biggest row — the
        // first thing this test did — measured exactly the row that cannot move, and read as a failure
        // while the rune was working. So: sum the small rows, where the arithmetic is still linear.
        // The MEDIAN ratio across them, not the sum: a handful still belong to a group whose total hits
        // the clamp, and averaging lets those few pull an otherwise perfect x2 down to x1.7. The median
        // says what happened to a TYPICAL row, which is the claim being tested.
        var ratios = rowsBefore
            .Where(kv => kv.Value > 0 && kv.Value < 5 && rowsAfter.ContainsKey(kv.Key))
            .Select(kv => rowsAfter[kv.Key] / kv.Value)
            .OrderBy(r => r).ToList();
        double median = ratios.Count == 0 ? 0 : ratios[ratios.Count / 2];

        Check("the mob's drop list arrived at all", before is { Length: > 0 },
              $"{before?.Length ?? 0} rows");
        Check("🔑 a Rune of Drop moves the numbers ON THE INSPECT SCREEN, not just the kill roll",
              ratios.Count >= 3 && median > 1.9,
              $"a typical unclamped row went x{median:0.##} on a +100% rune ({ratios.Count} rows compared)");
    }

    // (3) Sinister and Sinners: both are ordinary held runes, and Sinners can go nowhere.
    await gm.Hub.SendAsync("AdminCommand", "give", $"{gmName} {ItemCatalog.SinnersRune}");
    await gm.Settle();
    Check("the Rune of Sinners applies its own buff",
          gm.Buffs?.Buffs.Any(b => b.Key == RewardRunes.SinnersId) == true,
          string.Join(",", (gm.Buffs?.Buffs ?? Array.Empty<BuffDto>()).Select(b => b.Key)));

    var sinners = (gm.Inv?.Items ?? Array.Empty<InventoryItemDto>())
        .FirstOrDefault(i => i.DefId == ItemCatalog.SinnersRune);
    if (sinners is not null)
    {
        // ⚠ Back to a TOWN first: the keeper is only reachable in one, and out in the field the refusal
        // would come from "you can only reach your warehouse in a town" — a pass that proves nothing.
        var town = WorldMap.NpcById("gatekeeper_brackenford")!;
        await gm.Hub.SendAsync("DebugTeleport", town.X, town.Y - 40f);
        await gm.Settle();
        await gm.Hub.SendAsync("WarehouseDeposit", sinners.InstanceId);
        await gm.Settle();
        Check("🔑 no keeper will accept the Rune of Sinners — it is bound to the soul, not to a flag",
              (gm.Inv?.Items ?? Array.Empty<InventoryItemDto>()).Any(i => i.InstanceId == sinners.InstanceId),
              "the private keeper took it, so the punishment can be parked");
        await gm.Hub.SendAsync("RemoveItem", sinners.InstanceId, true, 1);
        await gm.Settle();
        Check("...and it cannot be thrown away either",
              (gm.Inv?.Items ?? Array.Empty<InventoryItemDto>()).Any(i => i.InstanceId == sinners.InstanceId),
              "it was destroyed");
    }

    // (3), the half that only a relog can show.
    var runeCharId = gmChars.Characters[0].Id;
    await gm.Hub.InvokeAsync<string?>("LeaveWorld");
    await gm.DisposeAsync();
    gm = await ConnectAsync("admin", "admin");
    var runeBack = await gm.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(runeCharId));
    gm.MyId = runeBack.EntityId;
    await gm.Settle();

    // ⚠ POLL for it. The rune buff is re-applied by the once-a-second reconcile, not in reply to
    // EnterWorld, so a flat Settle() is a race — see Session.WaitFor.
    await gm.WaitFor(() => (gm.Buffs?.Buffs ?? Array.Empty<BuffDto>()).Any(b => b.Key == expChannel.SkillId));
    var afterRelog = (gm.Buffs?.Buffs ?? Array.Empty<BuffDto>()).Where(b => b.Key == expChannel.SkillId).ToList();
    Check("🔑 the rune buff comes back exactly ONCE after a relog (reconciled, never restored)",
          afterRelog.Count == 1,
          $"{afterRelog.Count} copies; all buffs = "
          + string.Join(",", (gm.Buffs?.Buffs ?? Array.Empty<BuffDto>()).Select(x => x.Key)));
    Check("...still at the stronger rung the bag justifies",
          afterRelog.Count == 1 && afterRelog[0].Name == expChannel.NameAt(100),
          afterRelog.FirstOrDefault()?.Name);
}

// -------------------------------------------------------------------------------------------
// 10. THE TRAINING DUMMIES THAT HIT BACK (`56c` / `63h`) — do they actually strike?
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

// ============================================================================================
//  THE GROUND CIRCLES — a totem is not an entity, so it can only be checked HERE.
//
//  This is precisely the bug class this harness exists for. A totem worked perfectly for weeks and
//  was INVISIBLE, because it lives in a plain list on the world and nothing ever put it on the wire.
//  A human playtest cannot tell "the server never sent it" from "the client never drew it", and the
//  owner reported it as the latter. The push either happens or it does not, and that is a fact a
//  headless client can read directly.
// ============================================================================================
{
    Console.WriteLine();
    Console.WriteLine("--- totem + area-effect pushes ---");

    // ORK, because both totems are the Demon Lightbringer's: the race split puts the planted object on
    // his side of the fast heal, and the Mana Totem is his alone.
    // ⚠ `test1`, not a fresh account: ConnectAsync LOGS IN and throws if the account is missing — it
    // does not register — and the seeded accounts are the only ones that exist. An account holds many
    // characters, so the demon below is simply another of test1's, on its own connection.
    var tt = await ConnectAsync("test1", "test");
    string tname = "Totem" + DateTime.UtcNow.ToString("HHmmssff");
    var terr = await tt.Hub.InvokeAsync<string?>("CreateCharacter",
        new CreateCharacterRequest(tname, Race.Demon, BaseClass.Mage));
    Check("created an demon mage for the totem checks", terr is null, terr);

    if (terr is null)
    {
        // ⚠ EVERY Debug* command is an IAdminCommand and a fresh character is a plain player, so
        // without this the level/class/learn steps are all silently refused ("That is an admin-only
        // command.") and the section fails with an empty character rather than a real result. `/role`
        // works on an OFFLINE character, so it goes before EnterWorld.
        await PromoteToAdminAsync(tname);

        var tchars = await tt.Hub.InvokeAsync<CharacterList>("ListCharacters");
        var tpick = tchars.Characters.First(c => c.Name == tname);
        var tin = await tt.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(tpick.Id));
        tt.MyId = tin.EntityId;
        tt.MyX = tin.X; tt.MyY = tin.Y;

        // 1 -> 81, in the +10 steps the debug button allows. Past 52, so the Mana Totem exists too.
        for (int i = 0; i < 8; i++) await tt.Hub.SendAsync("DebugLevel", 10);
        var lb = ThirdClassCatalog.Playable
            .First(t => t.Race == Race.Demon && t.Discipline == Discipline.Lightbringer);
        await tt.Hub.SendAsync("DebugThirdClass", lb.Id);
        await tt.Hub.SendAsync("DebugLearnAll");
        await tt.Settle();

        // 🔑 EVERY WAIT HERE IS GENEROUS, AND THAT IS NOT SLOPPINESS. This character is NAKED and an
        // demon — WIT 19, the worst in the game, no gear — so `EffectiveCastSpeedMultiplier` is ~4.6x
        // and a 1-second totem takes 4.6s to plant. The first cut of this section waited the harness's
        // default 4s and reported eight failures against a feature that worked perfectly; the cast bar
        // was the thing that said so ("Healing Totem 4.6s"). WaitFor returns the instant the condition
        // holds, so a large ceiling costs nothing when things are working.
        const int CastWait = 15000;

        // ---- The HEALING totem: green, and the one he confirmed already worked. ----
        await tt.Hub.SendAsync("UseSkill", SkillCatalog.HealingTotem, tt.MyId);
        bool planted = await tt.WaitFor(() => (tt.Totems?.Totems.Length ?? 0) > 0, CastWait);
        Check("planting a totem PUSHES it to the client (it never used to reach the wire at all)",
              planted, $"{tt.Totems?.Totems.Length ?? 0} totems");

        var hp = tt.Totems?.Totems.FirstOrDefault(t => t.Heals);
        Check("...carrying the HP flag, so the client can colour it green",
              hp is not null);
        Check("...and the server's own radius, which is the whole point of drawing it",
              hp is not null && hp.Radius > 0f, $"radius {hp?.Radius ?? -1f}");

        // ---- The MANA totem: blue, and the one that was REFUSED until the RestoreMp gate was
        //      narrowed to Restore Mana itself. *"only Restore is forbidden other means of mp regen
        //      should work"*. A totem coexists with the healing one — different skills. ----
        await tt.Hub.SendAsync("UseSkill", SkillCatalog.ManaTotem, tt.MyId);
        bool both = await tt.WaitFor(() => (tt.Totems?.Totems.Count(t => t.Restores) ?? 0) > 0, CastWait);
        Check("the MANA totem is accepted and pushed too (the mana-restorer gate no longer eats it)",
              both, $"{tt.Totems?.Totems.Length ?? 0} totems, "
                  + $"{tt.Totems?.Totems.Count(t => t.Restores) ?? 0} restoring");
        Check("...and the two totems COEXIST, one per skill rather than one per owner",
              (tt.Totems?.Totems.Length ?? 0) >= 2, $"{tt.Totems?.Totems.Length ?? 0} totems");

        // ---- The set is WHOLE, so walking away must drop it. ⚠ THIS GOES BEFORE THE AoE CHECK, and
        //      the order is load-bearing: a totem lives 30s and the party heal below takes ~33s to
        //      cast on this naked demon, so run the other way round the totems have already EXPIRED and
        //      this check passes against an empty list — a false pass that proves nothing. ----
        // Away from the map EDGE, not just "+6000": a teleport that clamps would leave him inside
        // view range and fail the check for a reason that is not the one being measured.
        Check("...and both are still standing when the range check runs (not expired)",
              (tt.Totems?.Totems.Length ?? 0) >= 2, $"{tt.Totems?.Totems.Length ?? 0} totems");
        float away = tt.MyX > 12000f ? tt.MyX - GameConstants.ViewRange * 2f
                                     : tt.MyX + GameConstants.ViewRange * 2f;
        await tt.Hub.SendAsync("DebugTeleport", away, tt.MyY);
        bool gone = await tt.WaitFor(() => (tt.Totems?.Totems.Length ?? 0) == 0, 6000);
        Check("walking out of range clears the circles (the push is a WHOLE list, so it self-heals)",
              gone, $"{tt.Totems?.Totems.Length ?? 0} totems still listed");

        // ---- The AREA FLASH. Pick the SHORTEST-cast area skill he actually holds and can afford to
        //      cast: hard-coding Party Heal fails for two unrelated reasons — the Lightbringer's
        //      Ultimate Party Heal `Replaces` the plain one, and the Ultimate then demands 4x Skill
        //      Stone. Reading it off the learned set tests the flash instead of the roster. ----
        var aoeChoice = (tt.Learned?.Skills ?? Array.Empty<SkillRef>())
            .Select(s => (def: SkillCatalog.Get(s.Id), lvl: s.Level))
            .Where(x => x.def is not null
                     && x.def.Passive is null
                     && x.def.AreaRadiusAt(x.lvl) > 0f
                     && string.IsNullOrEmpty(x.def.ConsumableId))
            .OrderBy(x => x.def!.CastTicksAt(x.lvl))
            .FirstOrDefault();

        Check("the healer holds an area skill to flash at all", aoeChoice.def is not null);
        if (aoeChoice.def is not null)
        {
            tt.Areas.Clear();
            await tt.Hub.SendAsync("UseSkill", aoeChoice.def.Id, tt.MyId);
            // ⚠ 45s: see the CastWait note. Party Great Heal is a 7s cast that this naked demon takes
            //    32.6s to finish, and WaitFor returns the moment it lands.
            bool flashed = await tt.WaitFor(() => tt.Areas.Count > 0, 45000);
            Check($"an AoE skill flashes its footprint when it LANDS ({aoeChoice.def.Id})",
                  flashed, $"{tt.Areas.Count} area events");
            Check("...at the skill's real radius, which is what makes it something to stand in",
                  flashed && tt.Areas[0].Radius == aoeChoice.def.AreaRadiusAt(aoeChoice.lvl),
                  flashed ? $"{tt.Areas[0].Radius} vs {aoeChoice.def.AreaRadiusAt(aoeChoice.lvl)}" : "none");
            Check("...coloured by what the skill DOES, not by its id",
                  flashed && tt.Areas[0].Kind == AreaEffectKind.Heal,
                  flashed ? tt.Areas[0].Kind.ToString() : "none");
        }
    }

    await tt.LeaveWorldAsync();
    await tt.DisposeAsync();
}

// -------------------------------------------------------------------------------------------
// 11. THE RUNG YOU CAN ACTUALLY BUY — his playtest-29 find, over the wire.
//
//     *"The harmonist never learns serenity / vigor / vampiric rage / force / insight."* They were on
//     his CSV, they were on the class table, and `LearnSkill` answered "your class cannot learn this".
//     The cause was `owned + 1`: a class shelf may START above rung 1 (rung 1 of Force/Ward/Aim is the
//     POTION, so the cleric's first row is rung 2) and may SKIP rungs as it climbs (Serenity 2 -> 4 -> 6).
//
//     🔑 THIS IS EXACTLY THE KIND OF BUG THE SMOKE TEST EXISTS FOR, and the reason it went unseen for so
//     long is worth stating: `DebugLearnAll` assigns the HIGHEST rung directly and never walks the
//     ladder, so every admin character in every previous run had all five buffs. Only a character
//     BUYING them one at a time can see it. `SkillCsvSeed --learn-audit` guards the tables; this guards
//     the server handler.
// -------------------------------------------------------------------------------------------
{
    var lr = await ConnectAsync("test1", "test");
    string lname = "Rung" + DateTime.UtcNow.ToString("HHmmssff");
    var lerr = await lr.Hub.InvokeAsync<string?>("CreateCharacter",
        new CreateCharacterRequest(lname, Race.Elf, BaseClass.Mage));
    Check("created an elf mage to buy skill rungs", lerr is null, lerr);

    if (lerr is null)
    {
        // Every Debug* command is an IAdminCommand, and `/role` works on an OFFLINE character.
        await PromoteToAdminAsync(lname);

        var lchars = await lr.Hub.InvokeAsync<CharacterList>("ListCharacters");
        var lpick = lchars.Characters.First(c => c.Name == lname);
        var lin = await lr.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(lpick.Id));
        lr.MyId = lin.EntityId;

        // 1 -> 81 in the +10 steps the debug button allows, then the ELF buffer discipline — the
        // Harmonist is the class he was playing when he found this.
        for (int i = 0; i < 8; i++) await lr.Hub.SendAsync("DebugLevel", 10);
        var harmonist = ThirdClassCatalog.Playable
            .First(t => t.Race == Race.Elf && t.Discipline == Discipline.Warchanter);
        await lr.Hub.SendAsync("DebugThirdClass", harmonist.Id);
        await lr.Hub.SendAsync("DebugSp", 100_000_000L);
        await lr.Hub.SendAsync("DebugGold", 100_000_000L);
        await lr.Settle();

        int LearnedLevel(string id) =>
            lr.Learned?.Skills.FirstOrDefault(s => s.Id == id)?.Level ?? 0;

        // ---- Serenity: the shelf starts at rung 2 and then SKIPS — 2, 4, 6. Both halves of the bug
        //      in one ladder, which is why it is the one asserted rung by rung. ----
        string serenity = SkillCatalog.CastId(SkillCatalog.FamMpRegen);
        Check("the buffer starts out NOT knowing Serenity",
              LearnedLevel(serenity) == 0, $"level {LearnedLevel(serenity)}");

        foreach (int expected in new[] { 2, 4, 6 })
        {
            await lr.Hub.SendAsync("LearnSkill", serenity);
            await lr.WaitFor(() => LearnedLevel(serenity) == expected, 5000);
            Check($"...and buys Serenity up to rung {expected} (the shelf's next rung, not owned+1)",
                  LearnedLevel(serenity) == expected, $"level {LearnedLevel(serenity)}");
        }

        // ---- The other four he named. One purchase each: the point is that the FIRST one lands at
        //      all, because for every one of them the first rung on the shelf is above 1. ----
        foreach (var (id, label, first) in new[]
        {
            (SkillCatalog.CastId(SkillCatalog.FamHpRegen),  "Vigor",      2),
            (SkillCatalog.CastId(SkillCatalog.FamVamp),     "Vampirism",  2),
            (SkillCatalog.CastId(SkillCatalog.FamMagAtk),   "Force",      2),
            (SkillCatalog.CastId(SkillCatalog.FamMagCrit),  "Insight",    3),
        })
        {
            int before = LearnedLevel(id);
            await lr.Hub.SendAsync("LearnSkill", id);
            await lr.WaitFor(() => LearnedLevel(id) > before, 5000);
            Check($"...and {label} lands on its shelf's first rung ({first}), not on a rung nobody stocks",
                  LearnedLevel(id) == first, $"level {LearnedLevel(id)} (was {before})");
        }
    }

    await lr.LeaveWorldAsync();
    await lr.DisposeAsync();
}

// -------------------------------------------------------------------------------------------
// 11b. THE CLASS CHANGE'S SPELLS REPLACE THE BASE ONES — his §102.9 / §102.10 (2026-09-23).
//
//     *"as elf cleric i kept my self heal -> heal should have replaced my self heal"* and *"a mages
//     magic bolt is not replaced and left -> elemental bolts and holy bolt should replace it"*.
//
//     The data has said so since 0.163.1 (Heal `Replaces: [elf_self_heal]`, Holy Bolt and Elemental
//     Bolt `Replaces: [magic_bolt]`), so this walks HIS path through the real handlers rather than
//     the debug learn-all: BUY the self-heal at 7, class-change at 20, BUY the 2nd-class spell, and
//     the base one must be gone — from the kit, from a relog, and off the Learn shelf (the server
//     must refuse to sell it back).
// -------------------------------------------------------------------------------------------
{
    // One character per 2nd class, so the block costs two of the account's 36 slots, not three.
    foreach (var (arch, trades) in new[]
    {
        (Archetype.Healer, new[] { (SkillCatalog.Heal, SkillCatalog.ElfSelfHeal),
                                   (SkillCatalog.HolyBolt, SkillCatalog.MagicBolt) }),
        (Archetype.Nuker,  new[] { (SkillCatalog.ElementalBolt, SkillCatalog.MagicBolt) }),
    })
    {
        var rp = await ConnectAsync("test1", "test");
        string rname = "Repl" + DateTime.UtcNow.ToString("HHmmssff");
        var rerr = await rp.Hub.InvokeAsync<string?>("CreateCharacter",
            new CreateCharacterRequest(rname, Race.Elf, BaseClass.Mage));
        Check($"created an elf mage to become a {arch}", rerr is null, rerr);
        if (rerr is not null) { await rp.DisposeAsync(); continue; }

        await PromoteToAdminAsync(rname);
        var rchars = await rp.Hub.InvokeAsync<CharacterList>("ListCharacters");
        var rid = rchars.Characters.First(c => c.Name == rname).Id;
        var rin = await rp.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(rid));
        rp.MyId = rin.EntityId;
        await rp.Hub.SendAsync("DebugSp", 10_000_000L);
        await rp.Hub.SendAsync("DebugLevel", 6);   // 1 -> 7: the self-heal's first rung
        await rp.WaitFor(() => rp.Progress?.Level == 7, 5000);

        bool Knows(string id) => rp.Learned?.Skills.Any(s => s.Id == id) == true;

        foreach (var (_, replaced) in trades)
        {
            if (replaced == SkillCatalog.ElfSelfHeal) await rp.Hub.SendAsync("LearnSkill", replaced);
            await rp.WaitFor(() => Knows(replaced), 5000);
            Check($"...the base mage knows {replaced} before the class change", Knows(replaced));
        }

        await rp.Hub.SendAsync("DebugLevel", 10);
        await rp.Hub.SendAsync("DebugLevel", 3);   // -> 20
        var second = ClassCatalog.OptionsFor(Race.Elf, BaseClass.Mage).First(c => c.Archetype == arch);
        await rp.Hub.SendAsync("DebugSecondClass", second.Id);
        await rp.WaitFor(() => rp.Progress?.Level == 20, 5000);

        foreach (var (spell, replaced) in trades)
        {
            await rp.Hub.SendAsync("LearnSkill", spell);
            await rp.WaitFor(() => Knows(spell) && !Knows(replaced), 5000);
            Check($"...as a {arch}, buying {spell} REPLACES {replaced}",
                  Knows(spell) && !Knows(replaced),
                  $"knows {spell}: {Knows(spell)}, still knows {replaced}: {Knows(replaced)}");

            rp.SystemChat.Clear();
            await rp.Hub.SendAsync("LearnSkill", replaced);
            await rp.WaitFor(() => rp.SystemChat.Count > 0, 5000);
            Check($"...and {replaced} cannot be bought back",
                  !Knows(replaced) && rp.SystemChat.Any(s => s.Contains("superior version")),
                  string.Join(" | ", rp.SystemChat));
        }

        await rp.LeaveWorldAsync();
        rp.Learned = null;
        var again = await rp.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(rid));
        rp.MyId = again.EntityId;
        await rp.WaitFor(() => rp.Learned is not null, 8000);
        foreach (var (spell, replaced) in trades)
            Check($"...and after a relog {replaced} is still gone and {spell} still there",
                  rp.Learned is not null && Knows(spell) && !Knows(replaced),
                  rp.Learned is null ? "no Learned push" : string.Join(",", rp.Learned.Skills.Select(s => s.Id)));

        await rp.LeaveWorldAsync();
        await rp.DisposeAsync();
    }
}

// -------------------------------------------------------------------------------------------
// 12. A FRESH CHARACTER'S FIRST STATS ARE ALREADY THE RECOMPUTED ONES — his playtest-29 find.
//
//     *"newly created mage (lvl 1) have his first spell cast without penalty of weapon_proficiency ..
//     almost oneshoted a pig being naked ... Then i become lvl 5 and I did 1dmg with ~11s cast so the
//     passive recalculate the stats."*
//
//     `AutoLearnCoreSkills` WRITES the auto-granted skills and never recomputed; on the login path the
//     only recompute runs BEFORE it. So a brand-new character's numbers were the numbers of a
//     character with NO passives, and stayed that way until the first level-up or equip.
//
//     🔑 THE DISCRIMINATOR IS A RELOG, AND IT IS EXACT. On the FIRST login the saved row has none of
//     the auto-granted ids, so the grant is what adds them — the failing case. On the SECOND, the row
//     already carries them, so PersistenceService's own recompute folds them in and the numbers are
//     necessarily right. Same character, same level, same (empty) gear, nothing done in between: the
//     two stat pushes must be identical. With the bug they cannot be, because the first is the stats
//     of a character with no passives and the second is the stats of one with them.
//
//     ⚠ Deliberately NOT "sit down to force a recompute" — SetMoveState pushes stats without calling
//     RecomputeDerived, so that version of this test would have passed with the bug fully present.
//
//     🔑 It is also formula-free, which is what makes it survive re-tuning: it never says what a
//     level-1 mage's cast speed SHOULD be, only that the two logins agree. No future retune of
//     Spellcaster Mastery can turn it red on its own.
// -------------------------------------------------------------------------------------------
{
    var gr = await ConnectAsync("test1", "test");
    string gname = "Grant" + DateTime.UtcNow.ToString("HHmmssff");
    var gerr = await gr.Hub.InvokeAsync<string?>("CreateCharacter",
        new CreateCharacterRequest(gname, Race.Human, BaseClass.Mage));
    Check("created a brand-new level-1 mage", gerr is null, gerr);

    if (gerr is null)
    {
        var gchars = await gr.Hub.InvokeAsync<CharacterList>("ListCharacters");
        var gid = gchars.Characters.First(c => c.Name == gname).Id;

        async Task<StatsUpdate?> EnterAndReadStatsAsync()
        {
            gr.Stats = null;
            var res = await gr.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(gid));
            gr.MyId = res.EntityId;
            await gr.WaitFor(() => gr.Stats is not null, 8000);
            return gr.Stats;
        }

        var firstLogin = await EnterAndReadStatsAsync();
        Check("the fresh mage was sent his stats on his FIRST login", firstLogin is not null);

        // Spellcaster Mastery is auto-granted at level 1 and is the passive his find was about — if
        // the grant did not even land, everything below is measuring the wrong thing.
        // ⚠ WAITED FOR, not read: `Learned` is its own push, and the wait above was for `Stats`. Reading
        //   it straight after failed about one run in five (`§103.2`) with the grant fully in place.
        Check("...and the grant gave him Spellcaster Mastery",
              await gr.WaitFor(() => gr.Learned?.Skills.Any(s => s.Id == SkillCatalog.SpellcasterMastery) == true, 5000));

        var leaveErr = await gr.LeaveWorldAsync();
        Check("...and he logs out cleanly, saving those granted ids", leaveErr is null, leaveErr);

        var secondLogin = await EnterAndReadStatsAsync();
        Check("...and he was sent his stats on the SECOND login", secondLogin is not null);

        // The four numbers a missing passive actually moves: the pools, the cast-speed multiplier the
        // untrained-weapon penalty halves, and magic attack.
        bool same = firstLogin is not null && secondLogin is not null
                 && firstLogin.MaxHp == secondLogin.MaxHp
                 && firstLogin.MaxMp == secondLogin.MaxMp
                 && Math.Abs(firstLogin.CastSpeedMult - secondLogin.CastSpeedMult) < 0.0001f
                 && firstLogin.MagicAttack == secondLogin.MagicAttack;
        Check("...and BOTH logins report the same stats — the grant recomputed, so a brand-new "
            + "character is not playing without his passives",
              same,
              firstLogin is null || secondLogin is null ? "a stats push never arrived" :
              $"1st: hp {firstLogin.MaxHp} mp {firstLogin.MaxMp} cast x{firstLogin.CastSpeedMult:0.###} mAtk {firstLogin.MagicAttack}"
            + $" | 2nd: hp {secondLogin.MaxHp} mp {secondLogin.MaxMp} cast x{secondLogin.CastSpeedMult:0.###} mAtk {secondLogin.MagicAttack}");
    }

    await gr.LeaveWorldAsync();
    await gr.DisposeAsync();
}

// -------------------------------------------------------------------------------------------
// 13. THE BUFF BAR IS PUSHED ON ARRIVAL, EVEN WHEN IT IS EMPTY — his playtest-29 find.
//
//     *"after long break when reconnect my buff bar stays with the last snapshot.. But the buffs
//     aren't there ..they don't update as gone"* — and the second half, which is the damaging one:
//     *"if I'm buffed with group buffs and they are 'fake' it looks like I disabled the group buff
//     and can overbuff it with singles"*.
//
//     `PushBuffs` sends an EMPTY bar only once, when the last row goes away, and `_hadBuffs` records
//     what the SERVER last sent — not what any particular CLIENT holds. Those two come apart the
//     moment someone is link-dead or offline-farming: the buffs expire while he is away, the single
//     empty update goes into a dead socket, the id leaves the set, and on reconnect the rule says
//     "already told them" and never speaks again.
//
//     🔑 A FRESH LOGIN IS THE SAME LINE OF CODE AND IS FAR EASIER TO DRIVE. A brand-new character has
//     no buffs, so the bar is empty, so the suppression is exactly the branch under test: before the
//     fix nothing was sent at all and `Buffs` stays null; after it, an empty BuffUpdate arrives. The
//     reconnect case differs only in what the client happened to be holding, which is not something
//     the server can see and therefore not something it may reason about.
//
//     ⚠ It asserts the push EXISTS and is empty — never how many buffs a character "should" have, so
//     no future change to the starter kit or the auto-granted passives can turn it red on its own.
// -------------------------------------------------------------------------------------------
{
    var br = await ConnectAsync("test1", "test");
    string bname = "Bar" + DateTime.UtcNow.ToString("HHmmssff");
    var berr = await br.Hub.InvokeAsync<string?>("CreateCharacter",
        new CreateCharacterRequest(bname, Race.Elf, BaseClass.Fighter));
    Check("created a character to check the arrival buff push", berr is null, berr);

    if (berr is null)
    {
        var bchars = await br.Hub.InvokeAsync<CharacterList>("ListCharacters");
        var bid = bchars.Characters.First(c => c.Name == bname).Id;

        br.Buffs = null;
        var bres = await br.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(bid));
        br.MyId = bres.EntityId;
        await br.WaitFor(() => br.Buffs is not null, 8000);

        // The detail line is printed on a PASS too, so it says what HAPPENED, not what went wrong.
        Check("the buff bar is pushed on ARRIVAL, so a client can never keep a stale one",
              br.Buffs is not null,
              br.Buffs is null ? "no \"Buffs\" push in 8s" : "a \"Buffs\" push arrived");
        Check("...and it is EMPTY for a character carrying nothing",
              br.Buffs is null || br.Buffs.Buffs.Length == 0,
              br.Buffs is null ? null : $"{br.Buffs.Buffs.Length} row(s): "
                  + string.Join(", ", br.Buffs.Buffs.Select(b => b.Name)));
    }

    await br.LeaveWorldAsync();
    await br.DisposeAsync();
}

// -------------------------------------------------------------------------------------------
// 14. FEAR TAKES THE FEET, NOT JUST THE HANDS (`BL-110`, his `BL-123` ruling).
//
//     *"both dont change target like taunt — just act uncontrolably"*, and fear specifically:
//     *"run in place"* — the victim RUNS to random points 100-200 away and cannot steer.
//
//     🔑 WHY THIS BELONGS IN THE SMOKE TEST AND NOT IN A PLAYTEST. Server-driven movement is the one
//     class of effect that a human cannot check by looking: the client draws whatever position it is
//     sent, so a fear that drives nobody and a fear that drives you both render as "a character who is
//     where the server says he is". The old Fear (bit 41) was exactly that — it locked the hands and
//     left the feet entirely free — and it read as working for as long as nobody thought to stand
//     still and watch. Here the assertion is the whole point: NO MOVE ORDER IS EVER SENT, so any
//     displacement at all is the server driving the body.
//
//     ⚠ RUN THIS AGAINST THE OLD CODE AND IT MUST FAIL. That is the only thing that makes it a guard
//     rather than a decoration (the lesson from 0.102.7, where the first version of a guard would
//     have passed against the very bug it was written for). Under the pre-`BL-110` Fear the character
//     given no orders simply stands there and the displacement is zero.
//
//     The second check is the INPUT REFUSAL, and it is deliberately shaped as a "stop" tap: a Move
//     aimed at where you already are. If HandleMove still accepted input while feared, that tap would
//     arrive, the destination would be reached the same tick, and the victim would come to a dead
//     halt — the most visible possible symptom, and one that a tap aimed anywhere ELSE could not
//     distinguish from an unlucky sequence of random hops.
// -------------------------------------------------------------------------------------------
{
    var fe = await ConnectAsync("test1", "test");
    string fname = "Fear" + DateTime.UtcNow.ToString("HHmmssff");
    var ferr = await fe.Hub.InvokeAsync<string?>("CreateCharacter",
        new CreateCharacterRequest(fname, Race.Elf, BaseClass.Fighter));
    Check("created a character to be feared", ferr is null, ferr);

    if (ferr is null)
    {
        await PromoteToAdminAsync(fname);   // `/buff` is admin-gated

        var fchars = await fe.Hub.InvokeAsync<CharacterList>("ListCharacters");
        var fid = fchars.Characters.First(c => c.Name == fname).Id;
        var fres = await fe.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(fid));
        fe.MyId = fres.EntityId;
        await fe.Settle();

        // CONTROL SAMPLE FIRST. Everything below reads "he moved" as proof of the fear, which is only
        // evidence if a character with no orders is otherwise motionless. Assert that, or the test
        // could be passing on nothing more than a spawn drift.
        double idleX = fe.MyX, idleY = fe.MyY;
        await Task.Delay(1200);
        double drift = Math.Sqrt(Math.Pow(fe.MyX - idleX, 2) + Math.Pow(fe.MyY - idleY, 2));
        Check("a character given no orders stands perfectly still (the control sample)",
              drift < 1.0, $"drifted {drift:0.0} units in 1.2s");

        // FEAR HIM. Terrifying Roar is a 5s Fear — long enough to sample twice inside, short enough
        // that the section does not stall. `/buff` reaches it only because `BL-110` gave the command a
        // third search pool; before that no admin could put a CC on anybody at all.
        //
        // 🔴 `5s` IS NOT DECORATION — WITHOUT IT THE FEAR LASTS AN HOUR. `BL-131` gave `/buff` a
        // duration word and a **one-hour default** (`SkillCatalog.NpcBuffTicks`), so this command
        // stopped granting the SKILL's own 5 seconds the day that landed — and the "…and when the fear
        // expires the body stops dead" check below could not pass, because the fear was still running.
        // It read as a control effect that never releases; it was the harness asking for an hour.
        // 🔑 A COMMAND THAT GAINS A DEFAULT CHANGES EVERY CALLER THAT OMITS THE ARGUMENT.
        fe.Buffs = null;
        await fe.Hub.SendAsync("AdminCommand", "buff", "terrifying roar 10s");
        bool landed = await fe.WaitFor(
            () => fe.Buffs is not null && fe.Buffs.Buffs.Any(b => b.Name.Contains("Terrifying")), 4000);
        Check("`/buff` can reach a CONTROL skill, so fear/charm are testable at all",
              landed, landed ? "Terrifying Roar is on the bar" : "no fear buff arrived in 4s");

        // THE SUBJECT. No Move is sent anywhere in this block.
        //
        // 🔴 IT POLLS FOR THE DISTANCE, IT DOES NOT SLEEP FOR IT (found 2026-09-18, while verifying
        //    `BL-84`). This was `Task.Delay(1500)` and one measurement: a single check on a fixed
        //    window, against a five-second buff whose first movement tick can land anywhere inside it.
        //    It FLAKED — one run reported 13 units and failed, the next 100+ and passed, on identical
        //    code. That is the worst possible defect in the tool that verifies everything else: the
        //    next person to see it fail spends an hour looking for a regression that is not there, or
        //    dismisses a real one. `CLAUDE.md` already says it: "SmokeTest must poll, never sleep."
        // ⚠ The 3-second ceiling is still comfortably inside the 5-second fear, so "the fear expires
        //   and the body stops dead" below is unaffected — and a genuinely rooted body still fails,
        //   because polling only ever ends EARLY.
        double fx = fe.MyX, fy = fe.MyY;
        double Ran() => Math.Sqrt(Math.Pow(fe.MyX - fx, 2) + Math.Pow(fe.MyY - fy, 2));
        bool fled = await fe.WaitFor(() => Ran() > 30.0, 3000);
        Check("FEAR DRIVES A BODY THAT WAS GIVEN NO ORDERS",
              fled, $"moved {Ran():0} units with no move command sent");

        // THE REFUSAL. Tap "stop" — a Move to where we already are — repeatedly, and keep watching.
        // Accepted input would park him instantly; refused input leaves the panic running.
        // ⚠ POLLED, AND THE FEAR IS 10s (`§103.2` again, 2026-09-25). With a 5s roar this read ONE
        //   distance after six taps, and the taps could start ~7s after the `/buff` (a 4s wait for the
        //   buff to land + a 3s wait for the flight) — past the fear, so a body that had stopped for the
        //   right reason failed with "moved 15 units". It now taps and watches for up to 3s, and the
        //   fear outlasts the worst case. Accepted input still parks him at once and still fails.
        double sx = fe.MyX, sy = fe.MyY;
        double Despite() => Math.Sqrt(Math.Pow(fe.MyX - sx, 2) + Math.Pow(fe.MyY - sy, 2));
        for (int i = 0; i < 15 && Despite() <= 30.0; i++)
        {
            await fe.Hub.SendAsync("Move", new MoveCommand(fe.MyX, fe.MyY));
            await Task.Delay(200);
        }
        double despite = Despite();
        Check("...and a move order is REFUSED while it runs — a 'stop' tap cannot halt a panic",
              despite > 30.0, $"moved {despite:0} units through repeated stop taps");

        // AND IT HANDS THE BODY BACK. A control effect that never released would be the worse bug of
        // the two, and it is invisible for exactly as long as nobody waits out the duration.
        // Waited for, not slept: the fear leaving the buff bar is the event, then half a second's grace.
        await fe.WaitFor(() => fe.Buffs is not null && !fe.Buffs.Buffs.Any(b => b.Name.Contains("Terrifying")), 12000);
        await Task.Delay(500);
        double rx = fe.MyX, ry = fe.MyY;
        await Task.Delay(1200);
        double after = Math.Sqrt(Math.Pow(fe.MyX - rx, 2) + Math.Pow(fe.MyY - ry, 2));
        Check("...and when the fear expires the body stops dead (control handed back)",
              after < 1.0, $"still moving {after:0.0} units/1.2s after it should have worn off");

        // ---- `BL-111` — THE SLOT FLAG, AND THE BUG THE COUNTER UNCOVERED.
        //
        //      His ask was a number: *"I cannot see if I have 20 or less buffs to not over buff me"*.
        //      The number is only worth having if it is the SAME set the server evicts from, so the
        //      flag is computed server-side and asserted here rather than re-derived on the client.
        //
        //      🔴 AND ASKING FOR IT FOUND A REAL DEFECT. A debuff def carries the default
        //      `BuffRow.Buff` — the Debuff row is a DISPLAY override on the instance — so the cap
        //      predicate answered TRUE for one. A poison or a fear landing on a character at 20
        //      buffs evicted one of his BLESSINGS and then took the slot itself. Nothing said so,
        //      because nothing displayed the count. This is the check that would have.
        fe.Buffs = null;
        await fe.Hub.SendAsync("AdminCommand", "buff", "might");
        await fe.WaitFor(() => fe.Buffs?.Buffs.Any(b => b.Name.Contains("Might")) == true, 5000);
        var might = fe.Buffs?.Buffs.FirstOrDefault(b => b.Name.Contains("Might"));
        Check("a BLESSING reports on the wire that it occupies one of the buff slots",
              might is not null && might.Counts,
              might is null ? "no Might buff arrived" : $"Counts={might.Counts}");

        await fe.Hub.SendAsync("AdminCommand", "buff", "terrifying roar");
        await fe.WaitFor(() => fe.Buffs?.Buffs.Any(b => b.Name.Contains("Terrifying")) == true, 5000);
        var roar = fe.Buffs?.Buffs.FirstOrDefault(b => b.Name.Contains("Terrifying"));
        Check("...and a DEBUFF reports that it does NOT — a poison must never evict a blessing",
              roar is not null && !roar.Counts,
              roar is null ? "no fear buff arrived" : $"Counts={roar.Counts}");
    }

    await fe.LeaveWorldAsync();
    await fe.DisposeAsync();
}

// -------------------------------------------------------------------------------------------
// 15. WHISPS — THE PUSH-DOWN STACK, THE LEASH AND THE RE-SUMMON WINDOW (`BL-109`).
//
//     A whisp is NOT AN ENTITY, which is the whole reason this section exists: it never appears in
//     the world delta, so a whisp that was summoned but never reached the wire would be invisible in
//     a playtest in the most literal sense — the player casts, pays, and sees nothing, with no way to
//     tell a broken push from a broken summon. The `Whisps` push is the only evidence one exists.
//
//     🔑 THE STACK IS THE THING TO GUARD, and it is his own worked example:
//         [C][B][A] →D→ [D][C][B] →A→ [A][D][C] →D→ [A][D][C] (D refreshed)
//     A whisp you do NOT have is pushed on the front and the tail falls off; a whisp you DO have is
//     refreshed where it stands. With the base limit of ONE slot the eviction happens on the very
//     first extra summon, which makes it cheap to assert here and expensive to notice by playing.
//
//     ⚠ AN ELF BULWARK, deliberately — his race split gives the Elf tank charm + heal, which is the
//     one pair where BOTH whisps exist to push each other off. A Human would do as well; a race with
//     one whisp would not test the stack at all.
// -------------------------------------------------------------------------------------------
{
    var wh = await ConnectAsync("test1", "test");
    string wname = "Whsp" + DateTime.UtcNow.ToString("HHmmssff");
    var werr = await wh.Hub.InvokeAsync<string?>("CreateCharacter",
        new CreateCharacterRequest(wname, Race.Elf, BaseClass.Fighter));
    Check("created an elf fighter to raise whisps", werr is null, werr);

    if (werr is null)
    {
        await PromoteToAdminAsync(wname);   // every Debug* below is admin-only

        var wchars = await wh.Hub.InvokeAsync<CharacterList>("ListCharacters");
        var wpick = wchars.Characters.First(c => c.Name == wname);
        var win = await wh.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(wpick.Id));
        wh.MyId = win.EntityId;
        wh.MyX = win.X; wh.MyY = win.Y;

        // 🔑 LEVEL 51 — ABOVE BOTH WHISPS AND BELOW THE MASTERY, and the order is the point. Whisp
        // Mastery is learned at 60, so a level-71 character starts this section with TWO slots and
        // the eviction never happens — which is exactly how the first cut of this test passed a check
        // it was not making. The stack is tested at one slot, then the character is levelled INTO the
        // mastery and it is tested again. ⚠ 51 and not 41: his two level sets open at 40 and 43, so a
        // level-41 elf can call a Charming Whisp and not yet a Healing one.
        for (int i = 0; i < 5; i++) await wh.Hub.SendAsync("DebugLevel", 10);
        var bulwark = ThirdClassCatalog.Playable
            .First(t => t.Race == Race.Elf && t.Discipline == Discipline.Bulwark);
        await wh.Hub.SendAsync("DebugThirdClass", bulwark.Id);
        await wh.Hub.SendAsync("DebugLearnAll");
        // 🔴 STONES, OR NOTHING BELOW THIS LINE HAPPENS. A summon has cost **4 Skill Stones** since
        //    2026-09-03 (*"whisps to take 4 skillstone each summon"*), and this section was written the
        //    day before — so every check from here down had been failing on `Healing Whisp requires 4x
        //    Skill Stone` ever since, and the run said "12 CHECK(S) FAILED" with nothing in the game
        //    actually wrong. 🔑 THE LESSON IS THE HARNESS'S OWN: a REAGENT added to a skill owes every
        //    test that casts it a stock line, exactly as a new SkillDef field owes SkillText one.
        //    Sixty is generous on purpose — this section calls a whisp at least five times.
        await wh.Hub.SendAsync("DebugGive", ItemCatalog.SkillStone, 60);
        await wh.Settle();

        // Generous, for the same reason section 10 is: this character is naked and a FIGHTER, so his
        // cast multiplier is poor and a 1-second summon is not a 1-second summon.
        const int WhispWait = 15000;

        // ---- THE SUMMON REACHES THE WIRE AT ALL. ----
        wh.Whisps = null;
        await wh.Hub.SendAsync("UseSkill", SkillCatalog.CharmingWhisp, wh.MyId);
        bool called = await wh.WaitFor(() => (wh.Whisps?.Whisps.Length ?? 0) > 0, WhispWait);
        Check("a summoned whisp is PUSHED to the client — the only way one is ever visible",
              called, $"{wh.Whisps?.Whisps.Length ?? 0} whisps");
        Check("...and it is the one that was called",
              wh.Whisps?.Whisps.Any(w => w.SummonSkillId == SkillCatalog.CharmingWhisp) == true,
              string.Join(", ", wh.Whisps?.Whisps.Select(w => w.SummonSkillId) ?? Enumerable.Empty<string>()));
        Check("...carrying its owner, so a client knows whose spirit it is",
              wh.Whisps?.Whisps.All(w => w.OwnerId == wh.MyId) == true);
        Check("...and a countdown, so it can expire on screen the way a buff does",
              wh.Whisps?.Whisps.All(w => w.SecondsLeft > 0) == true,
              $"{wh.Whisps?.Whisps.FirstOrDefault()?.SecondsLeft ?? -1}s left");

        // ---- IT RIDES ITS MASTER. Walk, then check it came along and stayed in his band. His rule
        //      is *"Leashed 100-200 from the master"*, and the number that matters is the one AFTER
        //      he has moved — a whisp parked at the spawn point would pass a static check.
        await wh.Hub.SendAsync("Move", new MoveCommand(win.X + 600, win.Y));
        await Task.Delay(2500);
        var rider = wh.Whisps?.Whisps.FirstOrDefault();
        double gap = rider is null ? -1
                   : Math.Sqrt(Math.Pow(rider.X - wh.MyX, 2) + Math.Pow(rider.Y - wh.MyY, 2));
        Check("a whisp FOLLOWS its master and settles inside his leash band",
              rider is not null && gap >= 0 && gap <= GameConstants.WhispLeashMax + 40,
              $"{gap:0} units away (band is {GameConstants.WhispLeashMin:0}-{GameConstants.WhispLeashMax:0})");

        // ---- THE PUSH-DOWN STACK, at a limit of one: the second whisp evicts the first. ----
        await wh.Hub.SendAsync("UseSkill", SkillCatalog.HealingWhisp, wh.MyId);
        bool swapped = await wh.WaitFor(
            () => wh.Whisps?.Whisps.Any(w => w.SummonSkillId == SkillCatalog.HealingWhisp) == true,
            WhispWait);
        Check("a SECOND whisp is summonable and arrives", swapped,
              string.Join(", ", wh.Whisps?.Whisps.Select(w => w.SummonSkillId) ?? Enumerable.Empty<string>()));
        Check("...and at ONE slot it PUSHES THE FIRST OFF — a stack, not a growing set",
              (wh.Whisps?.Whisps.Length ?? 0) == 1
                  && wh.Whisps?.Whisps.All(w => w.SummonSkillId != SkillCatalog.CharmingWhisp) == true,
              $"{wh.Whisps?.Whisps.Length ?? 0} whisps: "
                  + string.Join(", ", wh.Whisps?.Whisps.Select(w => w.SummonSkillId) ?? Enumerable.Empty<string>()));

        // ---- RE-CALLING A WHISP YOU ALREADY HAVE. It has to be waited for: the 30-second REUSE
        //      answers first ("is not ready"), so an immediate re-cast tests the cooldown and nothing
        //      else — which is what the first cut of this test was in fact reading.
        //
        //      🔴 WHAT IS ASSERTED HERE WAS REVERSED BY HIM AND THE TEST DID NOT FOLLOW. It used to
        //      demand a REFUSAL ("is still with you"), which was `BL-109`'s five-second re-summon
        //      window — and `BL-130` deleted that window the next day on his ruling: *"charming whisp
        //      (and i guess all whisps) resummon on cd not when whisps disapear"*. The message it
        //      waited for no longer exists anywhere in the server, so the check could only ever fail;
        //      it was invisible because the reagent gate above was failing first (see the Skill Stone
        //      note). 🔑 A RULING THAT REVERSES A MECHANIC OWES ITS TEST AN EDIT, exactly as it owes
        //      `SkillText` a line.
        //
        //      So the rule now is: past the reuse a re-call is ALLOWED, and `SummonWhisp` refreshes
        //      the whisp IN PLACE — no duplicate, no reordering of the stack, and the 4 Skill Stones
        //      paid again. That is what is checked.
        wh.SystemChat.Clear();
        await Task.Delay(31000);
        await wh.Hub.SendAsync("UseSkill", SkillCatalog.HealingWhisp, wh.MyId);
        bool renewed = await wh.WaitFor(
            () => wh.SystemChat.Any(s => s.Contains("answers again")), 5000);
        Check("past the reuse, re-calling a whisp you already have RENEWS it (`BL-130`)",
              renewed, renewed ? "refreshed in place, not re-stacked"
                               : string.Join(" | ", wh.SystemChat.TakeLast(3)));
        Check("...and it is a refresh, not a second whisp — still exactly one, and the same one",
              (wh.Whisps?.Whisps.Length ?? 0) == 1
                  && wh.Whisps?.Whisps[0].SummonSkillId == SkillCatalog.HealingWhisp,
              $"{wh.Whisps?.Whisps.Length ?? 0} whisps: "
                  + string.Join(", ", wh.Whisps?.Whisps.Select(w => w.SummonSkillId) ?? Enumerable.Empty<string>()));

        // ---- WHISP MASTERY BUYS THE SECOND SLOT. Level INTO it now, so the difference between one
        //      slot and two is the passive and nothing else. If it were inert this is the only check
        //      in the game that would say so — nothing on a stat panel moves.
        for (int i = 0; i < 2; i++) await wh.Hub.SendAsync("DebugLevel", 10);
        await wh.Hub.SendAsync("DebugLearnAll");
        await wh.Settle();
        await wh.Hub.SendAsync("UseSkill", SkillCatalog.CharmingWhisp, wh.MyId);
        bool bothRide = await wh.WaitFor(() => (wh.Whisps?.Whisps.Length ?? 0) >= 2, WhispWait);
        Check("Whisp Mastery raises the limit to two, and both whisps ride at once",
              bothRide,
              $"{wh.Whisps?.Whisps.Length ?? 0} whisps: "
                  + string.Join(", ", wh.Whisps?.Whisps.Select(w => w.SummonSkillId) ?? Enumerable.Empty<string>()));

        // ---- AND NOW THE HALF THAT MATTERS: DOES A WHISP ACT?
        //
        //      🔑 EVERYTHING ABOVE IS BOOKKEEPING. A whisp that is summoned, drawn, stacked and
        //      expired correctly and never casts anything is a decoration, and it would look
        //      completely healthy in a playtest — the orb is there, the buff-like timer runs down,
        //      and the player has no way to tell that the spirit beside him is doing nothing. This is
        //      the check that says otherwise, and it is the reason this section exists.
        //
        //      A TRAINING DUMMY is the target, because it is the one creature in the game that will
        //      stand still and be attacked without the fight going anywhere. Attacking it is what
        //      puts the master in combat — his first condition on every whisp row (*"Master must be
        //      in combat mode (activley fighting)"*) — and the whisp does the rest by itself.
        var dummyZone = WorldMap.SpawnZones.First(z => z.MobTypes.Contains("dummy_physical"));
        await wh.Hub.SendAsync("DebugTeleport", dummyZone.X, dummyZone.Y);
        await wh.Settle();
        var dummy = wh.EntityNames.FirstOrDefault(kv => kv.Value.StartsWith("Striking Training Dummy"));
        if (dummy.Key != Guid.Empty)
        {
            var at = wh.EntityPos[dummy.Key];
            await wh.Hub.SendAsync("DebugTeleport", at.X, at.Y);
            await wh.Settle();
            wh.Combat.Clear();
            await wh.Hub.SendAsync("Attack", dummy.Key);

            // Its reuse is 10s and it fires the moment the master is engaged and in range, so a
            // handful of seconds is plenty — WaitFor returns the instant it lands.
            bool acted = await wh.WaitFor(
                () => wh.Combat.Any(c => c.Skill != null && c.Skill.Contains("Whisp")), 12000);
            Check("A WHISP ACTS ON ITS OWN once its master is fighting — the whole point of one",
                  acted,
                  acted ? string.Join(", ", wh.Combat.Where(c => c.Skill != null && c.Skill.Contains("Whisp"))
                                                     .Select(c => c.Skill).Distinct())
                        : $"{wh.Combat.Count} combat events, none from a whisp");
            Check("...aimed at the master's own target, not at one it picked for itself",
                  !acted || wh.Combat.Any(c => c.Skill != null && c.Skill.Contains("Whisp")
                                               && c.TargetId == dummy.Key),
                  "a whisp must never start a fight its owner did not");
        }
        else
        {
            Check("found a training dummy to fight so the whisps have something to act on",
                  false, "no 'Striking Training Dummy' in view after teleporting to its zone");
        }

        // ⚠ NOT ASSERTED HERE: "lost on death". There is no admin command that kills you, and the
        //   only other route is being beaten to death by a training dummy at 1 damage per tick — an
        //   hour of test for three lines that sit inside the buff clear in `Kill` and share its
        //   `keptBuffs` test. Stated rather than skipped silently, so nobody reads this section as
        //   covering it.
    }

    // Walk away from the dummy first. A whisp keeps its master ENGAGED for as long as he is swinging,
    // and LeaveWorld refuses to log out mid-fight — without this the run ends in twenty "You can't
    // leave while in combat" lines while the harness retries.
    await wh.Hub.SendAsync("Move", new MoveCommand(wh.MyX + 900, wh.MyY));
    await Task.Delay(1500);
    await wh.LeaveWorldAsync();
    await wh.DisposeAsync();
}

// -------------------------------------------------------------------------------------------
// 16. THE HUMAN RAVAGER'S FOCUS POOL (`BL-237`) — gather, the cap, the relog, the spend, the proc.
//
//     🔑 WHY OVER THE WIRE: every part of this is invisible bookkeeping. A pool that is refilled past
//     its cap, spent per slash instead of per cast, or quietly reset to 1 by a relog all LOOK fine on
//     the buff bar for exactly as long as nobody counts. Level 49 is chosen for its numbers: Focus's
//     cap there is 3, which is also the Double Slash's spend limit, so one full pool must vanish in
//     one cast and a per-slash bug would show up as a second "spent" line.
// -------------------------------------------------------------------------------------------
{
    var fs = await ConnectAsync("test1", "test");
    string fname = "Fcs" + DateTime.UtcNow.ToString("HHmmssff");
    var ferr = await fs.Hub.InvokeAsync<string?>("CreateCharacter",
        new CreateCharacterRequest(fname, Race.Human, BaseClass.Fighter));
    Check("created a human fighter to raise a Ravager", ferr is null, ferr);

    if (ferr is null)
    {
        await PromoteToAdminAsync(fname);
        async Task EnterAsFocusChar(Session s)
        {
            var list = await s.Hub.InvokeAsync<CharacterList>("ListCharacters");
            var inWorld = await s.Hub.InvokeAsync<LoginResult>("EnterWorld",
                new EnterWorldRequest(list.Characters.First(c => c.Name == fname).Id));
            s.MyId = inWorld.EntityId;
            s.MyX = inWorld.X; s.MyY = inWorld.Y;
        }
        int FocusOf(Session s) =>
            s.Buffs?.Buffs.FirstOrDefault(b => b.Key == SkillCatalog.WarriorFocus)?.Stacks ?? 0;
        bool HasFocus(Session s) =>
            s.Buffs?.Buffs.Any(b => b.Key == SkillCatalog.WarriorFocus) == true;

        await EnterAsFocusChar(fs);
        for (int i = 0; i < 4; i++) await fs.Hub.SendAsync("DebugLevel", 10);
        await fs.Hub.SendAsync("DebugLevel", 8);   // 1 + 48 = 49
        var ravager = ThirdClassCatalog.Playable
            .First(t => t.Race == Race.Human && t.Discipline == Discipline.Ravager);
        await fs.Hub.SendAsync("DebugThirdClass", ravager.Id);
        await fs.Hub.SendAsync("DebugLearnAll");
        await fs.Hub.SendAsync("DebugGive", ItemCatalog.NewbieSword2H, 1);
        await fs.WaitFor(() => fs.Inv?.Items.Any(i => i.DefId == ItemCatalog.NewbieSword2H) == true, 5000);
        var sword = fs.Inv?.Items.FirstOrDefault(i => i.DefId == ItemCatalog.NewbieSword2H);
        if (sword is not null) await fs.Hub.SendAsync("EquipItem", sword.InstanceId);
        await fs.Settle();

        // ---- GATHER TO THE CAP, ONE CHARGE A USE. ----
        //
        // ⚠ `BL-256` GAVE FOCUS A 0.5s REUSE (*"focus and focus force to have 0.5 cd... Now I spam it as
        //   crazy"*), so three presses back to back are two charges and a refusal — which is exactly what
        //   this section caught the day the reuse landed. PRESS UNTIL THE POOL MOVES, which is what a
        //   player's thumb does; never a sleep (a fixed wait either flakes or hides a real stall).
        async Task<bool> GatherTo(int n)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                await fs.Hub.SendAsync("UseSkill", SkillCatalog.WarriorFocus, fs.MyId);
                if (await fs.WaitFor(() => FocusOf(fs) >= n, 800)) return true;
            }
            return false;
        }
        for (int n = 1; n <= 3; n++) await GatherTo(n);
        Check("Focus gathers one charge per use, up to its rung's cap (3 at level 49)",
              FocusOf(fs) == 3, $"pool reads {FocusOf(fs)}");

        // The cap refusal is raised in ExecuteSkill, PAST the reuse gate — so a press sent while the
        // 0.5s is still running is refused for the wrong reason and never reaches it. Press until one
        // of the two answers comes back, and assert it was the cap's.
        fs.SystemChat.Clear();
        bool refused = false;
        for (int attempt = 0; attempt < 20 && !refused; attempt++)
        {
            await fs.Hub.SendAsync("UseSkill", SkillCatalog.WarriorFocus, fs.MyId);
            refused = await fs.WaitFor(() => fs.SystemChat.Any(s => s.Contains("already at its limit")), 800);
        }
        await Task.Delay(1200);   // long enough for a gather that WRONGLY went through to land
        Check("...and at the cap it is REFUSED (*\"Cannot be used if maximum is reached\"*), not overfilled",
              refused && FocusOf(fs) == 3, $"refused={refused}, pool reads {FocusOf(fs)}");

        // ---- THE POOL SURVIVES A RELOG AT ITS REAL COUNT. RestorePersistedBuffs clamps stacks to the
        //      DEF's MaxStacks, so a pool def left at 1 would come back as a single charge. ----
        await fs.LeaveWorldAsync();
        await fs.DisposeAsync();
        fs = await ConnectAsync("test1", "test");
        await EnterAsFocusChar(fs);
        await fs.WaitFor(() => fs.Buffs is not null, 5000);
        Check("a relog restores the Focus pool at its real count, not at 1",
              FocusOf(fs) == 3, $"pool reads {FocusOf(fs)} after relog");

        // ---- SPENT ONCE PER CAST: a two-hit Double Slash takes the whole 3 in ONE spend. ----
        // ⚠ THE PLAIN LEVEL-40 DUMMY, not a striking one. The first cut of this section used the
        //   Striking Training Dummy — which is LEVEL 80, and killed the level-49 Ravager mid-test, so the
        //   proc check below read "pool 0" off a corpse. This one never hits back and sits nine levels
        //   under him, so his swings actually land. A dummy's plate carries its level
        //   ("Training Dummy (Lv 40)"), which is what tells the four plain ones apart.
        const string Dummy40 = "Training Dummy (Lv 40)";
        var dz = WorldMap.SpawnZones.First(z => z.MobTypes.Contains("training_dummy") && z.MinLevel == 40);
        await fs.Hub.SendAsync("DebugTeleport", dz.X, dz.Y);
        await fs.Settle();
        await fs.WaitFor(() => fs.EntityNames.Any(kv => kv.Value == Dummy40), 4000);
        var fdummy = fs.EntityNames.FirstOrDefault(kv => kv.Value == Dummy40);
        if (fdummy.Key != Guid.Empty)
        {
            var at = fs.EntityPos[fdummy.Key];
            await fs.Hub.SendAsync("DebugTeleport", at.X, at.Y);
            await fs.Settle();
            fs.SystemChat.Clear();
            fs.Combat.Clear();
            await fs.Hub.SendAsync("UseSkill", SkillCatalog.FocusedDoubleSlash, fdummy.Key);
            bool slashed = await fs.WaitFor(
                () => fs.Combat.Count(c => c.Skill == "Focused Double Slash" && c.AttackerId == fs.MyId) >= 2, 8000);
            await Task.Delay(500);
            var spends = fs.SystemChat.Where(s => s.Contains("spent") && s.Contains("Focus")).ToList();
            Check("Focused Double Slash lands BOTH slashes", slashed,
                  $"{fs.Combat.Count(c => c.Skill == "Focused Double Slash")} slash event(s)");
            Check("...and spends the pool ONCE for the cast — 3 Focus, +45% power, a single spend line",
                  spends.Count == 1 && spends[0].Contains("spent 3 Focus") && spends[0].Contains("+45%"),
                  string.Join(" | ", spends));
            Check("...leaving the pool empty and GONE from the bar, not sitting at x0",
                  !HasFocus(fs), $"pool reads {FocusOf(fs)}");

            // ---- FOCUS MASTERY: basic swings gather it. 15% a hit, so ~20 swings is 96% sure;
            //      WaitFor returns the moment it procs. ----
            await fs.Hub.SendAsync("Attack", fdummy.Key);
            bool procced = await fs.WaitFor(() => FocusOf(fs) > 0, 30000);
            Check("Focus Mastery gathers Focus from basic attacks", procced, $"pool reads {FocusOf(fs)}");
        }
        else
        {
            Check("found a training dummy to spend Focus on", false,
                  "no level-40 'Training Dummy' in view after teleporting to its zone");
        }

        await fs.Hub.SendAsync("Move", new MoveCommand(fs.MyX + 900, fs.MyY));
        await Task.Delay(1500);
        await fs.LeaveWorldAsync();
    }
    await fs.DisposeAsync();
}

// -------------------------------------------------------------------------------------------
// 17. THE 4th TIER'S TWO GATHERERS (`BL-237`) — Focus Limit and Focus Force.
//
//     🔑 WHY OVER THE WIRE, and it is a different reason from section 16's. Both of these are NEW
//     SHAPES of the gather rule and both are refusals waiting to happen:
//       • FOCUS LIMIT gathers TEN against a cap of ten, which is how "set Focus to max" is expressed
//         without a new mechanic. If the clamp were wrong it would fill to one and cost 80 HP.
//       • FOCUS FORCE is the only gatherer in the game that also DEALS DAMAGE, and `IsChargePoolFull`
//         has to let it through at a full pool — refusing an ATTACK because a resource is full is the
//         kind of wall that reads as a broken button, and it is invisible until the pool happens to
//         be full. The gather itself sits AFTER the damage arm, which is the half a playtest cannot see.
// -------------------------------------------------------------------------------------------
{
    var f4 = await ConnectAsync("test1", "test");
    string f4name = "Fc4" + DateTime.UtcNow.ToString("HHmmssff");
    var f4err = await f4.Hub.InvokeAsync<string?>("CreateCharacter",
        new CreateCharacterRequest(f4name, Race.Human, BaseClass.Fighter));
    Check("created a human fighter to raise an ascended Ravager", f4err is null, f4err);

    if (f4err is null)
    {
        await PromoteToAdminAsync(f4name);
        var list4 = await f4.Hub.InvokeAsync<CharacterList>("ListCharacters");
        var world4 = await f4.Hub.InvokeAsync<LoginResult>("EnterWorld",
            new EnterWorldRequest(list4.Characters.First(c => c.Name == f4name).Id));
        f4.MyId = world4.EntityId; f4.MyX = world4.X; f4.MyY = world4.Y;

        int Focus4() => f4.Buffs?.Buffs.FirstOrDefault(b => b.Key == SkillCatalog.WarriorFocus)?.Stacks ?? 0;

        for (int i = 0; i < 8; i++) await f4.Hub.SendAsync("DebugLevel", 10);   // 1 + 80 = 81
        var rav4 = ThirdClassCatalog.Playable
            .First(t => t.Race == Race.Human && t.Discipline == Discipline.Ravager);
        await f4.Hub.SendAsync("DebugThirdClass", rav4.Id);
        await f4.Hub.SendAsync("DebugFourthClass");
        await f4.Hub.SendAsync("DebugLearnAll");
        await f4.Hub.SendAsync("DebugGive", ItemCatalog.NewbieSword2H, 1);
        await f4.WaitFor(() => f4.Inv?.Items.Any(i => i.DefId == ItemCatalog.NewbieSword2H) == true, 5000);
        var sw4 = f4.Inv?.Items.FirstOrDefault(i => i.DefId == ItemCatalog.NewbieSword2H);
        if (sw4 is not null) await f4.Hub.SendAsync("EquipItem", sw4.InstanceId);
        await f4.Settle();

        // ---- FOCUS LIMIT: one press, a full pool of TEN. ----
        await f4.Hub.SendAsync("UseSkill", SkillCatalog.FocusLimit, f4.MyId);
        await f4.WaitFor(() => Focus4() >= 10, 6000);
        Check("Focus Limit fills the pool to its ceiling of 10 in one cast", Focus4() == 10,
              $"pool reads {Focus4()}");

        // ---- FOCUS FORCE against a FULL pool: it must still SWING. ----
        const string Dummy80 = "Training Dummy (Lv 80)";
        var dz4 = WorldMap.SpawnZones.FirstOrDefault(z => z.MobTypes.Contains("training_dummy") && z.MinLevel == 40);
        if (dz4 is not null)
        {
            await f4.Hub.SendAsync("DebugTeleport", dz4.X, dz4.Y);
            await f4.Settle();
            await f4.WaitFor(() => f4.EntityNames.Any(kv => kv.Value.StartsWith("Training Dummy")), 4000);
            var d4 = f4.EntityNames.FirstOrDefault(kv => kv.Value.StartsWith("Training Dummy") && kv.Value != Dummy80);
            if (d4.Key != Guid.Empty)
            {
                var at4 = f4.EntityPos[d4.Key];
                await f4.Hub.SendAsync("DebugTeleport", at4.X, at4.Y);
                await f4.Settle();
                f4.SystemChat.Clear();
                f4.Combat.Clear();
                await f4.Hub.SendAsync("UseSkill", SkillCatalog.FocusForce, d4.Key);
                bool struck = await f4.WaitFor(
                    () => f4.Combat.Any(c => c.Skill == "Focus Force" && c.AttackerId == f4.MyId), 8000);
                Check("Focus Force strikes even with the pool already full — a gatherer that DAMAGES is "
                    + "never refused", struck,
                      string.Join(" | ", f4.SystemChat.Take(3)));
                Check("...and the full pool is neither overfilled nor emptied by it", Focus4() == 10,
                      $"pool reads {Focus4()}");

                // ---- ...and the Triple Slash spends its four. ----
                f4.SystemChat.Clear();
                await f4.Hub.SendAsync("UseSkill", SkillCatalog.FocusedTrippleSlash, d4.Key);
                await f4.WaitFor(() => f4.SystemChat.Any(x => x.Contains("spent") && x.Contains("Focus")), 8000);
                var spend4 = f4.SystemChat.FirstOrDefault(x => x.Contains("spent") && x.Contains("Focus"));
                Check("Focused Tripple Slash spends exactly 4 of the ten, leaving 6",
                      spend4 is not null && spend4.Contains("spent 4 Focus") && Focus4() == 6,
                      $"{spend4} / pool reads {Focus4()}");
            }
            else
            {
                Check("found a training dummy for the ascended Focus checks", false, "none in view");
            }
        }

        await f4.Hub.SendAsync("Move", new MoveCommand(f4.MyX + 900, f4.MyY));
        await Task.Delay(1500);
        await f4.LeaveWorldAsync();
    }
    await f4.DisposeAsync();
}


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

    /// <summary>The last "Stats" push — §12's whole subject. A character's numbers are the only place
    /// an unapplied passive is visible, and the client is sent them; nothing else on the wire says
    /// whether a granted skill was ever folded in.</summary>
    public StatsUpdate? Stats;

    /// <summary>The last "Favor" push (the Wayfarer's Favor and Blessing sheet).</summary>
    public FavorUpdate? Favor;

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

    /// <summary>The last "QuestLog" push. A quest's live COUNTER only exists here — the dialog carries a
    /// summary only while you stand at the NPC, and the whole 0.67.1 bug (a collect step that never
    /// counted) is a number on this message that never moved.</summary>
    public QuestLog? Quests;

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

    /// <summary>The last "Crafting" push — profession, crafting level/exp and whether the character is
    /// standing at his master. `BL-05` put three gates on a craft and this is where two of them surface.</summary>
    public CraftingUpdate? Crafting;

    /// <summary>The last "QuestMarks" push — which NPCs have a marker for this character.</summary>
    public QuestMarks? Marks;

    /// <summary>The last "TargetDetails" push — the expanded target window, including a mob's DROP
    /// list. The drop percentages are computed per LOOKER (a Rune of Drop moves them), and the whole
    /// point of that rule is that the shown number is the rolled number — which can only be checked
    /// where the number is actually sent.</summary>
    public TargetDetails? Details;

    /// <summary>The last "Totems" push — the whole visible set, so this IS the current truth rather
    /// than a running tally. Null until the server first has something to say.</summary>
    public TotemList? Totems;

    /// <summary>The last "Whisps" push (`BL-109`). A whisp is not an entity, so this is the ONLY
    /// place one appears on the wire — if this stays null the client has no way to know one exists.</summary>
    public WhispList? Whisps;

    /// <summary>Every area-effect flash seen. A list because they are one-shot events with no state:
    /// missing one is only visible as an absence, which is exactly what these tests check.</summary>
    public readonly List<AreaEffectEvent> Areas = new();

    public async Task OpenAsync(string url)
    {
        Hub = new HubConnectionBuilder().WithUrl(url).Build();
        Hub.On<SkillBarDto>("SkillBar", b => Bar = b);
        Hub.On<InventoryUpdate>("Inventory", i => Inv = i);
        Hub.On<WarehouseUpdate>("Warehouse", w => Ware = w);
        Hub.On<LearnedSkills>("Learned", l => Learned = l);
        Hub.On<StatsUpdate>("Stats", s => Stats = s);
        Hub.On<FavorUpdate>("Favor", f => Favor = f);
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
        Hub.On<CraftingUpdate>("Crafting", c => Crafting = c);
        Hub.On<QuestMarks>("QuestMarks", m => Marks = m);
        Hub.On<TargetDetails>("TargetDetails", d => Details = d);
        Hub.On<NpcDialog>("Dialog", d => Dialog = d);
        Hub.On<QuestLog>("QuestLog", q => Quests = q);
        Hub.On<GoldUpdate>("Gold", g => Gold = g.Gold);
        Hub.On<CombatEvent>("Combat", c => Combat.Add(c));
        Hub.On<TotemList>("Totems", t => Totems = t);
        Hub.On<WhispList>("Whisps", w => Whisps = w);
        Hub.On<AreaEffectEvent>("AreaEffect", a => Areas.Add(a));
        Hub.On<ChatMessage>("Chat", m => { AllChat.Add(m); if (m.Channel == ChatChannel.System) { SystemChat.Add(m.Text); Console.WriteLine($"        [SYSTEM] {m.Text}"); } });
        await Hub.StartAsync();
    }

    /// <summary>Let the server tick and its pushes arrive. The game loop runs at 10 Hz and commands
    /// are drained on the tick, so a couple of hundred ms is several ticks' worth of headroom.</summary>
    public Task Settle() => Task.Delay(500);

    /// <summary>Wait until a condition on the captured pushes holds, or give up after
    /// <paramref name="timeoutMs"/>. Returns whether it held.
    ///
    /// 🔑 **Poll, never sleep** — the rule this harness learned once already and then broke again.
    /// <see cref="Settle"/> is a flat 500 ms, and anything the server does on its OWN clock rather than
    /// in reply to a message is a coin flip against it. The rune-relog pair was exactly that: rune buffs
    /// are re-applied by <c>ReconcileTimedItems</c>, which runs ONCE A SECOND, so whether the push landed
    /// inside the 500 ms depended on nothing but where the tick happened to be. It passed for months and
    /// then started failing when an unrelated section got longer and shifted the phase — the test was
    /// always a coin flip, it had simply been winning. A poll makes it a fact.</summary>
    public async Task<bool> WaitFor(Func<bool> condition, int timeoutMs = 4000)
    {
        var until = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < until)
        {
            if (condition()) return true;
            await Task.Delay(50);
        }
        return condition();
    }

    /// <summary>Leave the world, retrying while the server says "in combat". Returns null on success,
    /// or the server's last refusal.
    ///
    /// 🔑 The SAME lesson as <see cref="WaitFor"/>, found again on a different clock. `LeaveWorld` is
    /// refused for <c>CombatDecayTicks</c> — THIRTY SECONDS — after the character's last combat tick,
    /// and that timer runs on the server's clock, not in reply to anything this harness sends. So
    /// whether a single attempt succeeded depended on how long the sections before it happened to
    /// take. Measured against the build BEFORE this batch, it failed roughly one run in three: a coin
    /// flip that had mostly been winning, exactly like the rune pair.
    ///
    /// A retry makes it a fact. It costs nothing in the normal case (the first attempt returns null)
    /// and it turns "the save is raced" from a maybe into a measurement.</summary>
    public async Task<string?> LeaveWorldAsync(int timeoutMs = 40000)
    {
        var until = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        string? last;
        while (true)
        {
            last = await Hub.InvokeAsync<string?>("LeaveWorld");
            if (last is null || DateTime.UtcNow >= until) return last;
            await Task.Delay(500);
        }
    }

    public async ValueTask DisposeAsync() => await Hub.DisposeAsync();
}
