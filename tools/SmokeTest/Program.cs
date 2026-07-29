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
//     quality is derived from it; S grade is derived from A. All of that is arithmetic nobody sees
//     until an item is in hand, so assert it on the catalogue directly.
// -------------------------------------------------------------------------------------------
{
    var aSword = ItemCatalog.Get("sword1h_t76");
    var sSword = ItemCatalog.Get($"sword1h_t{ItemCatalog.SGradeLevel}");
    Check("A-grade sword is MYTHIC (the authored number is the ceiling, not a 70% anchor)",
          aSword is { Rarity: ItemRarity.Mythic }, $"{aSword?.Rarity}");
    Check("S grade exists and is ~60% above A",
          sSword is not null && aSword is not null
            && sSword.AtkBonus == (int)Math.Round(aSword.AtkBonus * ItemCatalog.SGradeOverA),
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

Check("server pushed quest markers on login", a.Marks is not null);
// A level-1 character legitimately has NO markers: the starter chain opens at 10 and the class
// chains at 18. Asserting "> 0" here would be asserting a bug. The real check is after the level-up
// below — markers must APPEAR once the character is old enough to be offered something.
Check("a level-1 character has no quest markers yet (nothing opens before level 10)",
      a.Marks is not null && a.Marks.Marks.Length == 0,
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
await friend.Hub.SendAsync("Like", name);   // Test2 likes the smoke char, so a later jail has charisma to drain
await a.Settle();
var chBoard = await a.Hub.InvokeAsync<LeaderboardDto>("RequestLeaderboard", "charisma");
Check("the liked player reached the charisma board",
      chBoard.Entries.Any(e => e.Name == "Test2" && e.Value >= 1),
      string.Join(",", chBoard.Entries.Select(e => $"{e.Name}:{e.Value}")));
Check("the smoke char is on the charisma board after being liked",
      chBoard.Entries.Any(e => e.Name == name && e.Value >= 1));

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
await gm.Settle();

// JAIL the victim (b) live.
await b.Settle();   // make sure b's position is current
b.MyX = 0; b.MyY = 0;
await gm.Hub.SendAsync("AdminCommand", "jail", $"{name} 60");
await b.Settle();
bool atJail = Math.Abs(b.MyX - GameConstants.JailX) < 50 && Math.Abs(b.MyY - GameConstants.JailY) < 50;
Check("jailing a player teleports them to jail (live)", atJail, $"at ({b.MyX:0},{b.MyY:0})");

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
    boardAfterJail = await b.Hub.InvokeAsync<LeaderboardDto>("RequestLeaderboard", "charisma");
    if (!boardAfterJail.Entries.Any(e => e.Name == name)) break;
    await Task.Delay(300);
}
Check("a jail drained the player's charisma (dropped off the board)",
      boardAfterJail.Entries.All(e => e.Name != name),
      string.Join(",", boardAfterJail.Entries.Select(e => e.Name)));

// Jailed → may pace around inside the CELL, but can never leave it (owner, 2026-07-20: serving a
// sentence should feel like a cell, not paralysis). Walk hard at the wall and confirm we end up
// clamped to the jail radius rather than either frozen on the spot or out in the world.
await b.Hub.SendAsync("Move", new MoveCommand(GameConstants.JailX + 3000, GameConstants.JailY));
for (int i = 0; i < 12; i++) await b.Settle();   // give the walk time to run into the wall
double fromJail = Math.Sqrt(
    Math.Pow(b.MyX - GameConstants.JailX, 2) + Math.Pow(b.MyY - GameConstants.JailY, 2));
Check("a jailed player can MOVE inside the cell",
      fromJail > 20, $"{fromJail:0} units from the jail centre");
Check("a jailed player can NOT walk out of the cell",
      fromJail <= GameConstants.JailRadius + 40,
      $"{fromJail:0} units out, cell radius is {GameConstants.JailRadius:0}");

// JAIL PERSISTS across a relog: leave, come back, still in jail.
await b.Hub.SendAsync("LeaveWorld");
await Task.Delay(600);
await b.DisposeAsync();
var c = await ConnectAsync("test1", "test");
var enteredC = await c.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(charId));
c.MyId = enteredC.EntityId;
await c.Settle();
Check("jail SURVIVES a relog (spawns back in jail)",
      Math.Abs(c.MyX - GameConstants.JailX) < 50 && Math.Abs(c.MyY - GameConstants.JailY) < 50,
      $"spawned at ({c.MyX:0},{c.MyY:0})");

// RELEASE sends you to the STARTING town, never the nearest one — the jail's location has to stay
// secret, and "nearest" is a map hint (owner, 2026-07-20).
await gm.Hub.SendAsync("AdminCommand", "unjail", name);
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
await gm.Hub.SendAsync("AdminCommand", "jail", $"{name.ToLowerInvariant()} 60");
await gm.Settle();
Check("a lower-case name resolves, and does NOT report 'no character'",
      gm.SystemChat.Any(s => s.Contains("jailed for")) &&
      !gm.SystemChat.Any(s => s.Contains("No character")),
      string.Join(" | ", gm.SystemChat));
await gm.Hub.SendAsync("AdminCommand", "unjail", name);
await Task.Delay(300);

// KICK must remove the entity SERVER-SIDE, without the kicked client's cooperation.
//
// The smoke client is a raw connection: it ignores ForceDisconnect and never calls LeaveWorld. That
// is exactly the case that used to break — the server only asked the client to leave, so the entity
// stayed behind as a GHOST (targetable, killable, and still holding the name), and the account was
// then refused re-entry with "character is already online". So re-entering here WITHOUT leaving
// first is the whole test: the error must be the kick lockout, never "already online".
await gm.Hub.SendAsync("AdminCommand", "kick", $"{name} 60");
await Task.Delay(600);
var d = await ConnectAsync("test1", "test");
var enteredD = await d.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(charId));
Check("a KICKED character can't re-enter until the lockout passes",
      !enteredD.Success && (enteredD.Error?.Contains("locked") ?? false), enteredD.Error);
Check("kick leaves NO ghost entity behind (re-entry is blocked by the kick, not by 'already online')",
      !(enteredD.Error?.Contains("already online") ?? false), enteredD.Error);
await d.DisposeAsync();
await c.DisposeAsync();

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
            foreach (var s in d.Spawns) { Spawned.Add(s.Id); if (s.Id == MyId) { MyX = s.X; MyY = s.Y; } }
            foreach (var u in d.Updates) { Updated.Add(u.Id); if (u.Id == MyId) { MyX = u.X; MyY = u.Y; } }
            foreach (var id in d.Despawns) Despawned.Add(id);
        });
        Hub.On<ProgressUpdate>("Progress", p => Progress = p);
        Hub.On<BuffUpdate>("Buffs", b => Buffs = b);
        Hub.On<QuestMarks>("QuestMarks", m => Marks = m);
        Hub.On<ChatMessage>("Chat", m => { AllChat.Add(m); if (m.Channel == ChatChannel.System) { SystemChat.Add(m.Text); Console.WriteLine($"        [SYSTEM] {m.Text}"); } });
        await Hub.StartAsync();
    }

    /// <summary>Let the server tick and its pushes arrive. The game loop runs at 10 Hz and commands
    /// are drained on the tick, so a couple of hundred ms is several ticks' worth of headroom.</summary>
    public Task Settle() => Task.Delay(500);

    public async ValueTask DisposeAsync() => await Hub.DisposeAsync();
}
