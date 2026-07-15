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
    var auth = await s.Hub.InvokeAsync<AuthResponse>("Login", new AuthRequest(user, pass));
    if (!auth.Success) throw new Exception($"login failed: {auth.Error}");
    return s;
}

Console.WriteLine();
Console.WriteLine("=== L2Clone smoke test (headless) ===");
Console.WriteLine();

// -------------------------------------------------------------------------------------------
// 1. Log in, enter the world.
// -------------------------------------------------------------------------------------------
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

int mainSlot = a.Subclasses!.Classes[0].Slot;
var mainClass = a.Subclasses.Classes[0].BaseClass;

static string Show(string[]? bar) => bar is null
    ? "<null>"
    : "[" + string.Join(",", bar.Select(s => string.IsNullOrEmpty(s) ? "_" : s)) + "]";

// -------------------------------------------------------------------------------------------
// 2. Arrange the MAIN class's skill bar, deliberately NOT in the server's auto-placed order.
//    (Reversing it is the cheapest way to make a layout the server would never produce itself,
//    so if anything re-derives the bar instead of preserving it, this test notices.)
// -------------------------------------------------------------------------------------------
// Give the character real skills FIRST. An all-empty bar proves nothing — every assertion below
// would pass trivially by comparing empty to empty, which is exactly how a broken bar could sneak
// through. Level up so skills exist, then learn everything the class can.
// Adding a subclass now requires level 76. The debug step is clamped to +10 (it mirrors the UI
// buttons), so climb in +10s: 1 -> 81, comfortably past the gate.
for (int i = 0; i < 8; i++) await a.Hub.SendAsync("DebugLevel", 10);
await a.Hub.SendAsync("DebugLearnAll");
await a.Settle();
Check("the main class has skills on its bar",
      a.Bar!.Slots.Any(s => !string.IsNullOrEmpty(s)),
      "an empty bar would make every check below pass for the wrong reason");
Console.WriteLine($"        main class = {mainClass}, server bar = {Show(a.Bar!.Slots)}");

string[] mainBar = a.Bar!.Slots.Reverse().ToArray();
await a.Hub.SendAsync("SetSkillBar", mainBar);
await a.Settle();
Console.WriteLine($"        main bar SET to {Show(mainBar)}");

// -------------------------------------------------------------------------------------------
// 3. Add a SUBCLASS of the other base class and switch to it.
// -------------------------------------------------------------------------------------------
// You pick a specific 3rd-class discipline now (pre-approved). Any is fine — the main class holds no
// discipline yet, so the no-duplicate rule can't trip. Take the first catalog entry.
var chosen = ThirdClassCatalog.Playable.First();
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

await b.Hub.SendAsync("LeaveWorld");
await Task.Delay(400);
await b.DisposeAsync();

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

    public async Task OpenAsync(string url)
    {
        Hub = new HubConnectionBuilder().WithUrl(url).Build();
        Hub.On<SkillBarDto>("SkillBar", b => Bar = b);
        Hub.On<SubclassListDto>("Subclasses", s => Subclasses = s);
        Hub.On<ChatMessage>("Chat", m => { if (m.Channel == ChatChannel.System) Console.WriteLine($"        [SYSTEM] {m.Text}"); });
        await Hub.StartAsync();
    }

    /// <summary>Let the server tick and its pushes arrive. The game loop runs at 10 Hz and commands
    /// are drained on the tick, so a couple of hundred ms is several ticks' worth of headroom.</summary>
    public Task Settle() => Task.Delay(500);

    public async ValueTask DisposeAsync() => await Hub.DisposeAsync();
}
