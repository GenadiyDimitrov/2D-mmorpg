using System.Globalization;
using Game.Shared;
using Microsoft.AspNetCore.SignalR.Client;

/// <summary>
/// A HEADLESS SECOND PLAYER, on the real protocol, with no window.
///
/// Why this exists: everything social in this game needs two people — party, trade, resurrect, PvP,
/// buffing someone else, kill-stealing, loot rules. The owner has one phone, so all of it was
/// untestable except by describing it. This is the other player.
///
/// It is NOT the smoke test. The smoke test asserts and exits; this logs in and STAYS logged in,
/// taking orders. Both live in one project because the connect/login/enter plumbing is the same and
/// had already been debugged once here.
///
/// ── How it takes orders ───────────────────────────────────────────────────────────────────────
/// It reads commands from a FILE and from stdin, whichever produces a line first. The file is the
/// important one: it means an operator with no interactive terminal (a coding agent driving this
/// through tool calls) can steer a live character by APPENDING a line, while the bot keeps its
/// connection, its buffs and its party membership. A REPL alone would have forced a reconnect per
/// command, which loses exactly the state worth testing.
///
///   dotnet run --project tools/SmokeTest -- bot [user] [pass] [commandfile]
///   echo "target Admin" >> bot-commands.txt
///
/// Everything it sees — chat, damage, deaths, party invites, resurrect offers — is printed with a
/// timestamp, so the console doubles as a transcript of what the SERVER thought happened.
/// </summary>
static class Bot
{
    const string Url = "http://localhost:5238/game";

    static HubConnection _hub = null!;
    static Guid _myId;
    static string _myName = "";
    static Guid? _target;

    // The world as this client knows it. Kept from the delta feed exactly as a real client does, so
    // "target the thing called X" resolves against what the server actually says is nearby.
    static readonly Dictionary<Guid, Known> _world = new();
    static float _x, _y;
    static bool _dead;
    static bool _autoAcceptParty = true;
    static bool _autoAcceptRes = true;
    static bool _running = true;

    sealed class Known
    {
        public string Name = "";
        public EntityKind Kind;
        public int Level, Hp, MaxHp;
        public float X, Y;
        public bool Dead;
    }

    static void Log(string s) =>
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {s}");

    public static async Task<int> RunAsync(string[] args)
    {
        string user = args.Length > 0 ? args[0] : "test2";
        string pass = args.Length > 1 ? args[1] : "test";
        string file = args.Length > 2 ? args[2] : "bot-commands.txt";

        Console.WriteLine();
        Console.WriteLine("=== L2Clone BOT — a headless second player ===");
        Console.WriteLine($"    account   : {user}");
        Console.WriteLine($"    order file: {Path.GetFullPath(file)}");
        Console.WriteLine("    type 'help' for commands (or append them to the file)");
        Console.WriteLine();

        _hub = new HubConnectionBuilder().WithUrl(Url).WithAutomaticReconnect().Build();
        Wire();
        await _hub.StartAsync();

        var auth = await _hub.InvokeAsync<AuthResponse>("Login",
            new AuthRequest(user, pass, GameConstants.ProtocolVersion), GameConstants.GameVersion);
        if (!auth.Success) { Log($"LOGIN FAILED: {auth.Error}"); return 1; }

        // Reuse a character if the account has one — the point is a PERSISTENT second player whose
        // level and gear survive between sessions, not a fresh throwaway like the smoke test wants.
        var chars = await _hub.InvokeAsync<CharacterList>("ListCharacters");
        if (chars.Characters.Length == 0)
        {
            string name = "Bot" + DateTime.UtcNow.ToString("HHmmss");
            var err = await _hub.InvokeAsync<string?>("CreateCharacter",
                new CreateCharacterRequest(name, Race.Human, BaseClass.Fighter));
            if (err is not null) { Log($"CREATE FAILED: {err}"); return 1; }
            chars = await _hub.InvokeAsync<CharacterList>("ListCharacters");
        }

        var pick = chars.Characters[0];
        var entered = await _hub.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(pick.Id));
        if (!entered.Success) { Log($"ENTER FAILED: {entered.Error}"); return 1; }

        _myId = entered.EntityId;
        _myName = pick.Name;
        _x = entered.X; _y = entered.Y;
        Log($"in world as {_myName} (Lv {pick.Level}) at {_x:0},{_y:0}");

        // Start the order file empty so a stale queue from the last run cannot execute itself.
        try { File.WriteAllText(file, ""); } catch { /* the console still works without it */ }

        _ = Task.Run(StdinLoop);
        await FileLoop(file);

        try { await _hub.SendAsync("LeaveWorld"); await Task.Delay(300); } catch { }
        await _hub.DisposeAsync();
        return 0;
    }

    /// <summary>Poll the order file for APPENDED lines. Polling (not a FileSystemWatcher) because the
    /// writer may be `echo >>` from any shell, and a watcher's event coalescing loses lines when two
    /// arrive in the same tick — a lost order in a live playtest reads as "the bot ignored me".</summary>
    static async Task FileLoop(string file)
    {
        // Track a BYTE OFFSET, not a line count. Counting lines looked equivalent and was not: an empty
        // file splits into one empty element, so the counter started at 1 and the first real order was
        // silently swallowed. An offset has no such edge case — and a swallowed order in a live
        // playtest is indistinguishable from the bot ignoring you.
        long offset = 0;
        var pending = new System.Text.StringBuilder();

        while (_running)
        {
            try
            {
                if (File.Exists(file))
                {
                    // Share-read so appending from any other process never throws in here.
                    using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    if (fs.Length < offset) offset = 0;   // truncated/replaced — start over
                    fs.Seek(offset, SeekOrigin.Begin);
                    using var sr = new StreamReader(fs);
                    pending.Append(await sr.ReadToEndAsync());
                    offset = fs.Length;

                    // Execute only COMPLETE lines: a write caught mid-append would otherwise run half
                    // an order. Whatever follows the last newline stays buffered for the next poll.
                    string buffered = pending.ToString();
                    int nl;
                    while ((nl = buffered.IndexOf('\n')) >= 0)
                    {
                        string line = buffered[..nl].Trim();
                        buffered = buffered[(nl + 1)..];
                        if (line.Length > 0) await ExecuteAsync(line);
                    }
                    pending.Clear();
                    pending.Append(buffered);
                }
            }
            catch (Exception ex) { Log("order file: " + ex.Message); }
            await Task.Delay(250);
        }
    }

    static async Task StdinLoop()
    {
        while (_running)
        {
            string? line = await Console.In.ReadLineAsync();
            if (line is null) { await Task.Delay(500); continue; }   // no console attached
            if (line.Trim().Length > 0) await ExecuteAsync(line.Trim());
        }
    }

    // ---- the world feed -------------------------------------------------------------------------

    static void Wire()
    {
        _hub.On<SnapshotDelta>("SnapshotDelta", d =>
        {
            foreach (var s in d.Spawns)
            {
                _world[s.Id] = new Known
                {
                    Name = s.Name, Kind = s.Kind, Level = s.Level,
                    Hp = s.Hp, MaxHp = s.MaxHp, X = s.X, Y = s.Y,
                };
                if (s.Id == _myId) { _x = s.X; _y = s.Y; }
            }
            foreach (var u in d.Updates)
            {
                if (_world.TryGetValue(u.Id, out var k))
                {
                    k.X = u.X; k.Y = u.Y; k.Hp = u.Hp; k.Dead = u.Dead;
                }
                if (u.Id == _myId)
                {
                    _x = u.X; _y = u.Y;
                    if (u.Dead != _dead)
                    {
                        _dead = u.Dead;
                        Log(_dead ? "*** I DIED *** ('respawn' to go to town, or wait for a resurrect)"
                                  : "*** I am alive again ***");
                    }
                }
            }
            foreach (var id in d.Despawns) _world.Remove(id);
        });

        _hub.On<ChatMessage>("Chat", m =>
            Log($"<{m.Channel}> {m.From}: {m.Text}"));

        // Only MY fights, or the console drowns in every mob in the zone hitting every other mob.
        _hub.On<CombatEvent>("Combat", c =>
        {
            if (c.AttackerId != _myId && c.TargetId != _myId) return;
            string what = c.Skill is null ? "" : $" [{c.Skill}]";
            Log($"combat: {c.AttackerName} → {c.TargetName}  {c.Outcome} {c.Damage}{what}");
        });

        _hub.On<PartyInviteDto>("PartyInvite", async p =>
        {
            Log($"party invite from {p.InviterName} (loot {p.LootMode})");
            if (_autoAcceptParty)
            {
                await _hub.SendAsync("PartyRespond", true);
                Log("  → accepted automatically ('autoparty off' to stop)");
            }
        });

        _hub.On<PartyUpdate>("Party", p =>
            Log(p.Members.Length == 0
                ? "party: disbanded/left"
                : "party: " + string.Join(", ", p.Members.Select(m =>
                    $"{m.Name} Lv{m.Level} {m.Hp}/{m.MaxHp}" + (m.IsLeader ? " (leader)" : "")))));

        _hub.On<ResurrectOffer>("ResurrectOffer", async r =>
        {
            Log($"resurrect offered by {r.FromName} ({r.ExpPct:0.#}% exp back)");
            if (_autoAcceptRes)
            {
                await _hub.SendAsync("ResurrectResponse", true);
                Log("  → accepted automatically ('autores off' to stop)");
            }
        });

        _hub.On<ProgressUpdate>("Progress", p => Log($"progress: Lv {p.Level}  exp {p.Exp}"));
        _hub.On<string>("ForceDisconnect", m => Log("KICKED: " + m));
        _hub.Closed += _ => { Log("connection closed"); return Task.CompletedTask; };
    }

    // ---- orders ---------------------------------------------------------------------------------

    static Known? Find(string name)
    {
        foreach (var kv in _world)
            if (kv.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return kv.Value;
        return null;
    }

    static Guid? FindId(string name)
    {
        foreach (var kv in _world)
            if (kv.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return kv.Key;
        return null;
    }

    static float Num(string s) =>
        float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0f;

    static async Task ExecuteAsync(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts[0].ToLowerInvariant();
        string Rest(int from) => string.Join(' ', parts.Skip(from));

        try
        {
            switch (cmd)
            {
                case "help":
                    Console.WriteLine("""
                      who                    nearby entities
                      status                 my hp/position/target
                      target <name>          select by name
                      attack [name]          attack target (or name)
                      skill <id> [name]      cast on target/name (e.g. skill magic_bolt)
                      move <x> <y>           walk to world coordinates
                      goto <name>            walk to an entity
                      follow <name>          keep walking to them until 'stop'
                      stop                   stop following
                      sit / stand / run / walk
                      say <text> / world <text> / w <name> <text>
                      party invite <name> | accept | decline | leave | kick <name>
                      autoparty on|off       auto-accept invites (default on)
                      res                    accept a resurrect offer
                      autores on|off         auto-accept resurrects (default on)
                      respawn                respawn in town after death
                      pvp on|off
                      buff                   give myself the full buff set (debug)
                      level <+n|-n> | gold | sp | learnall | give <itemId> [qty]
                      tp <x> <y> | tpto <name>
                      quit
                      """);
                    break;

                case "who":
                    foreach (var kv in _world.OrderBy(k => k.Value.Kind).ThenBy(k => k.Value.Name))
                        Console.WriteLine($"    {kv.Value.Kind,-6} {kv.Value.Name,-20} Lv{kv.Value.Level,-3} " +
                                          $"{kv.Value.Hp}/{kv.Value.MaxHp} at {kv.Value.X:0},{kv.Value.Y:0}" +
                                          (kv.Value.Dead ? "  DEAD" : "") +
                                          (kv.Key == _myId ? "  ← me" : "") +
                                          (kv.Key == _target ? "  ← target" : ""));
                    break;

                case "status":
                    Log($"{_myName} at {_x:0},{_y:0} {(_dead ? "DEAD" : "alive")}, " +
                        $"target={(_target is Guid t && _world.TryGetValue(t, out var tk) ? tk.Name : "none")}, " +
                        $"{_world.Count} entities in view");
                    break;

                case "target":
                {
                    var id = FindId(Rest(1));
                    if (id is null) { Log($"no '{Rest(1)}' in view — try 'who'"); break; }
                    _target = id;
                    Log($"target = {_world[id.Value].Name}");
                    break;
                }

                case "attack":
                {
                    if (parts.Length > 1) _target = FindId(Rest(1)) ?? _target;
                    if (_target is null) { Log("no target"); break; }
                    await _hub.SendAsync("Attack", _target.Value);
                    Log($"attacking {_world[_target.Value].Name}");
                    break;
                }

                case "skill":
                {
                    if (parts.Length < 2) { Log("skill <id> [name]"); break; }
                    if (parts.Length > 2) _target = FindId(Rest(2)) ?? _target;
                    await _hub.SendAsync("UseSkill", parts[1], _target);
                    Log($"casting {parts[1]}");
                    break;
                }

                case "move":
                    if (parts.Length < 3) { Log("move <x> <y>"); break; }
                    await _hub.SendAsync("Move", new MoveCommand(Num(parts[1]), Num(parts[2])));
                    break;

                case "goto":
                {
                    var k = Find(Rest(1));
                    if (k is null) { Log($"no '{Rest(1)}' in view"); break; }
                    await _hub.SendAsync("Move", new MoveCommand(k.X, k.Y));
                    Log($"walking to {k.Name} at {k.X:0},{k.Y:0}");
                    break;
                }

                case "follow":
                    _followName = Rest(1);
                    Log($"following {_followName} — 'stop' to quit");
                    _ = FollowLoop();
                    break;

                case "stop":
                    _followName = null;
                    Log("stopped following");
                    break;

                case "sit":   await _hub.SendAsync("SetMoveState", (int)MoveState.Sitting); break;
                case "stand":
                case "run":   await _hub.SendAsync("SetMoveState", (int)MoveState.Running); break;
                case "walk":  await _hub.SendAsync("SetMoveState", (int)MoveState.Walking); break;

                case "say":   await _hub.SendAsync("Chat", Rest(1), ChatChannel.Local, null); break;
                case "world": await _hub.SendAsync("Chat", Rest(1), ChatChannel.World, null); break;
                case "w":     await _hub.SendAsync("Chat", Rest(2), ChatChannel.Whisper, parts[1]); break;

                case "party":
                {
                    string sub = parts.Length > 1 ? parts[1].ToLowerInvariant() : "";
                    switch (sub)
                    {
                        case "invite":
                        {
                            var id = FindId(Rest(2));
                            if (id is null) { Log($"no '{Rest(2)}' in view"); break; }
                            await _hub.SendAsync("PartyInvite", id.Value);
                            Log($"invited {Rest(2)}");
                            break;
                        }
                        case "accept":  await _hub.SendAsync("PartyRespond", true); break;
                        case "decline": await _hub.SendAsync("PartyRespond", false); break;
                        case "leave":   await _hub.SendAsync("PartyLeave"); break;
                        case "kick":
                        {
                            var id = FindId(Rest(2));
                            if (id is not null) await _hub.SendAsync("PartyKick", id.Value);
                            break;
                        }
                        default: Log("party invite|accept|decline|leave|kick <name>"); break;
                    }
                    break;
                }

                case "autoparty": _autoAcceptParty = Rest(1) != "off"; Log($"autoparty {_autoAcceptParty}"); break;
                case "autores":   _autoAcceptRes   = Rest(1) != "off"; Log($"autores {_autoAcceptRes}"); break;
                case "res":       await _hub.SendAsync("ResurrectResponse", true); break;
                case "respawn":   await _hub.SendAsync("Respawn"); break;
                case "pvp":       await _hub.SendAsync("TogglePvp", Rest(1) != "off"); break;

                case "buff":     await _hub.SendAsync("DebugBuff"); break;
                case "learnall": await _hub.SendAsync("DebugLearnAll"); break;
                case "gold":     await _hub.SendAsync("DebugGold", 10_000_000L); break;
                case "sp":       await _hub.SendAsync("DebugSp", 1_000_000L); break;
                case "level":
                    await _hub.SendAsync("DebugLevel",
                        int.TryParse(parts.ElementAtOrDefault(1), out var dl) ? dl : 1);
                    break;
                case "give":
                    await _hub.SendAsync("DebugGive", parts[1],
                        int.TryParse(parts.ElementAtOrDefault(2), out var q) ? q : 1);
                    break;
                case "tp":
                    await _hub.SendAsync("DebugTeleport", Num(parts[1]), Num(parts[2]));
                    break;
                case "tpto":
                {
                    var k = Find(Rest(1));
                    if (k is null) { Log($"no '{Rest(1)}' in view"); break; }
                    await _hub.SendAsync("DebugTeleport", k.X, k.Y);
                    break;
                }

                case "quit": _running = false; Log("leaving"); break;

                default: Log($"? {cmd} — 'help' for the list"); break;
            }
        }
        catch (Exception ex) { Log($"'{line}' failed: {ex.Message}"); }
    }

    static string? _followName;

    /// <summary>Re-issue a walk toward someone every second. Deliberately a WALK, not a server-side
    /// follow: the point is to exercise the same move path the phone uses, and to be visible on the
    /// owner's screen as another player moving normally.</summary>
    static async Task FollowLoop()
    {
        string? who = _followName;
        while (_running && _followName == who && who is not null)
        {
            var k = Find(who);
            if (k is not null)
            {
                float dx = k.X - _x, dy = k.Y - _y;
                if (dx * dx + dy * dy > 120f * 120f)
                    await _hub.SendAsync("Move", new MoveCommand(k.X, k.Y));
            }
            await Task.Delay(1000);
        }
    }
}
