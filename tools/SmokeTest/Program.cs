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
var withItem = (string[])mainBar.Clone();
int freeIdx = Array.FindIndex(withItem, string.IsNullOrEmpty);
Check("the bar has a free slot to place an item token", freeIdx >= 0);
if (freeIdx >= 0) withItem[freeIdx] = itemToken;
mainBar = withItem;                       // this is now the canonical main bar the relog must reproduce
await a.Hub.SendAsync("SetSkillBar", mainBar);
await a.Settle();
// SetSkillBar stores without echoing a fresh push, so we don't re-read a.Bar here — the RELOG assertion
// below (SyncSkillBar kept the item: token) is the real proof it was accepted AND persisted.

// -------------------------------------------------------------------------------------------
// 5. THE REAL TEST: log out completely and log back in on a NEW connection. Everything above
//    could still be alive purely in server memory. Only a relog proves it reached the DB.
// -------------------------------------------------------------------------------------------
await a.Hub.SendAsync("LeaveWorld");
await Task.Delay(600);
await a.DisposeAsync();

var b = await ConnectAsync("test1", "test");
var entered2 = await b.Hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(charId));
Check("re-entered the world as the same character", entered2.Success, entered2.Error);
await b.Settle();

Check("both classes survived the relog", b.Subclasses?.Classes.Length == 2,
      $"got {b.Subclasses?.Classes.Length}");
Check("MAIN class's bar survived the relog",
      b.Bar is not null && b.Bar.Slots.SequenceEqual(mainBar));
Check("the ITEM slot survived the relog (SyncSkillBar kept the item: token, not wiped as a skill)",
      b.Bar is not null && b.Bar.Slots.Contains(itemToken));
Check("levels survived the relog (main 81, subclass 5)",
      b.Subclasses!.Classes.First(c => c.Slot == mainSlot).Level == 81 &&
      b.Subclasses.Classes.First(c => c.Slot == subSlot).Level == 5,
      $"main {b.Subclasses!.Classes.First(c => c.Slot == mainSlot).Level}, " +
      $"sub {b.Subclasses.Classes.First(c => c.Slot == subSlot).Level}");

// And the SUBCLASS's own bar must still be its own, after the relog.
b.Bar = null;
await b.Hub.SendAsync("SwitchSubclass", subSlot);
await b.Settle();
Check("SUBCLASS's bar survived the relog too",
      b.Bar is not null && b.Bar.Slots.SequenceEqual(subBar));
b.MyId = entered2.EntityId;

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

    public async Task OpenAsync(string url)
    {
        Hub = new HubConnectionBuilder().WithUrl(url).Build();
        Hub.On<SkillBarDto>("SkillBar", b => Bar = b);
        Hub.On<LearnedSkills>("Learned", l => Learned = l);
        Hub.On<SubclassListDto>("Subclasses", s => Subclasses = s);
        Hub.On<SnapshotDelta>("SnapshotDelta", d =>
        {
            DeltaCount++;
            foreach (var s in d.Spawns) { Spawned.Add(s.Id); if (s.Id == MyId) { MyX = s.X; MyY = s.Y; } }
            foreach (var u in d.Updates) { Updated.Add(u.Id); if (u.Id == MyId) { MyX = u.X; MyY = u.Y; } }
            foreach (var id in d.Despawns) Despawned.Add(id);
        });
        Hub.On<ChatMessage>("Chat", m => { if (m.Channel == ChatChannel.System) { SystemChat.Add(m.Text); Console.WriteLine($"        [SYSTEM] {m.Text}"); } });
        await Hub.StartAsync();
    }

    /// <summary>Let the server tick and its pushes arrive. The game loop runs at 10 Hz and commands
    /// are drained on the tick, so a couple of hundred ms is several ticks' worth of headroom.</summary>
    public Task Settle() => Task.Delay(500);

    public async ValueTask DisposeAsync() => await Hub.DisposeAsync();
}
