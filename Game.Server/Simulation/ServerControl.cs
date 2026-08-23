using Game.Shared;

namespace Game.Server.Simulation;

/// <summary>
/// THE SHUTDOWN / REBOOT PROCEDURE, and the admin-only lockdown that can follow it.
/// His playtest-26 design, built as specified:
///
///   /server shutdown|stop    [minutes] [adminOnlyAfterStart]
///   /server reboot|restart   [minutes] [adminOnlyAfterStart]
///   /server on|online        [minutes]
///
/// • <b>minutes</b> — `-`, empty or `0` means INSTANT; anything unparseable means 30 minutes. That is
///   his rule verbatim, and it is the safe way round: a typo delays the server rather than killing it
///   under a full population.
/// • <b>adminOnlyAfterStart</b> — `true`/`1` writes a flag file, and the server that comes back up
///   admits STAFF ONLY until an admin types `/server on`.
/// • <b>on</b> — cancels any countdown INSTANTLY, and lifts the admin-only lock either now (no time
///   given) or after the minutes given. *"the idea in the ON is to stop the procedures of
///   shutdown/reboot in an instant and allow normal players to enter after the time"*.
/// • Each command REPLACES the one before it — there is only ever one procedure running.
///
/// 🔑 The countdown announces on HIS ladder, not on a fixed interval: whole hours while over an hour,
/// then every 10 minutes, then every minute under 10, then every second for the last 60. So a 117-minute
/// shutdown says 1:57h at the moment it is typed, then 1:00h, 50, 40, 30, 20, 10, 9…1, then counts the
/// last minute down second by second.
///
/// ⚠ State is deliberately STATIC and process-wide: the hub's login gate has to read the lockdown on the
/// connection thread, while the countdown ticks on the game loop. Nothing here mutates world entities,
/// so it needs none of the single-writer discipline the rest of the simulation does.
/// </summary>
internal static class ServerControl
{
    internal enum Procedure { None, Shutdown, Reboot }

    /// <summary>What is scheduled, if anything.</summary>
    internal static Procedure Kind { get; private set; } = Procedure.None;

    /// <summary>When it fires. Null when nothing is scheduled.</summary>
    internal static DateTime? DueUtc { get; private set; }

    /// <summary>Whether the server that comes back up should admit staff only.</summary>
    internal static bool AdminOnlyAfterStart { get; private set; }

    /// <summary>Admin-only is in force until this moment. <see cref="DateTime.MaxValue"/> = until an
    /// admin lifts it by hand. Null = the server is open.</summary>
    internal static DateTime? AdminOnlyUntilUtc { get; private set; }

    /// <summary>Is a non-staff character refused entry right now?</summary>
    internal static bool LockedToStaff =>
        AdminOnlyUntilUtc is DateTime until && DateTime.UtcNow < until;

    /// <summary>What to tell a player who is refused. Reads the remaining time when there is one.</summary>
    internal static string LockedMessage()
    {
        if (AdminOnlyUntilUtc is not DateTime until) return "The server is open.";
        if (until == DateTime.MaxValue)
            return "The server is in maintenance — staff only.";
        var left = until - DateTime.UtcNow;
        return $"The server is in maintenance — staff only for another {Format((int)left.TotalSeconds)}.";
    }

    // ─── THE FLAG FILE ────────────────────────────────────────────────────────────────────────────
    // Beside the exe, like debug-config.json and owner.txt. It exists ONLY to carry `admin only` across
    // the restart the shutdown itself performs — its whole life is "written just before we die, read
    // just after we come back, deleted the moment an admin says /server on".
    private static readonly string LockFile =
        Path.Combine(AppContext.BaseDirectory, "admin-only.flag");

    /// <summary>Read the flag left by a previous run. Call once at startup.</summary>
    internal static void LoadLockdown()
    {
        try
        {
            if (File.Exists(LockFile)) AdminOnlyUntilUtc = DateTime.MaxValue;
        }
        catch { /* unreadable = open; the server must still start */ }
    }

    private static void WriteLockFile(bool on)
    {
        try
        {
            if (on) File.WriteAllText(LockFile, "Staff-only login. Delete this file, or type /server on.");
            else if (File.Exists(LockFile)) File.Delete(LockFile);
        }
        catch { /* a lock we cannot persist is still honoured for this run */ }
    }

    // ═══ THE OWNER FILE ═══════════════════════════════════════════════════════════════════════════
    //
    // ONE character is the Owner, and which one is decided by a text file beside the exe — his design,
    // verbatim: *"maybe a file in the directory that can be altered only by hand .. read only at start
    // .. no db no nothing"*. Deliberately NOT in the database and NOT reachable from any command, so the
    // top of the hierarchy cannot be granted, stolen, or lost to a careless `/role`.
    //
    // Format: one character NAME on the first non-blank, non-`#` line. Everything after it is ignored,
    // so "cannot have two" is enforced by READING rather than by validating.
    //
    // ⚠ Read ONCE. Editing the file mid-session changes nothing until the server restarts — which is
    // the point: it is the one authority a running server cannot be talked out of.
    private static readonly string OwnerFile =
        Path.Combine(AppContext.BaseDirectory, "owner.txt");

    private static string? _ownerName;
    private static bool _ownerLoaded;

    /// <summary>The Owner's character name, or null when no owner.txt names one. Compared
    /// case-insensitively everywhere.</summary>
    internal static string? OwnerCharacterName
    {
        get
        {
            if (_ownerLoaded) return _ownerName;
            _ownerLoaded = true;
            try
            {
                if (File.Exists(OwnerFile))
                    _ownerName = File.ReadLines(OwnerFile)
                        .Select(l => l.Trim())
                        .FirstOrDefault(l => l.Length > 0 && !l.StartsWith("#"));
            }
            catch { /* unreadable file = no owner; the server must still start */ }
            return _ownerName;
        }
    }

    /// <summary>Name the Owner on a FRESH server — called once from the debug seed, which only runs on an
    /// empty database. Never overwrites an existing file, so a real deployment's owner.txt is safe and
    /// the seeded `Admin` character simply becomes the Owner on a dev machine, which is what makes the
    /// whole hierarchy testable the moment the DB is deleted.</summary>
    internal static void SeedOwnerFile(string characterName)
    {
        try
        {
            if (File.Exists(OwnerFile)) return;
            File.WriteAllText(OwnerFile,
                "# The one Owner character. Read at STARTUP only; edit by hand and restart.\n"
                + "# The first non-blank, non-# line wins — there can only ever be one.\n"
                + characterName + "\n");
            _ownerName = characterName;
            _ownerLoaded = true;
        }
        catch { /* no owner file = no Owner; every other rank still works */ }
    }


    // ⚠⚠ DEV CONVENIENCE — DELETE THIS METHOD AND ITS CALL BEFORE THE GAME GOES PUBLIC. ⚠⚠
    //
    // Owner, playtest 27: *"can u make it for time being if now owner.txt is missing at start to create
    // it with name Owner - each time I remove the GameServer folder it will remove it as well - make a
    // comment to delete the file creation when game going public."*
    //
    // SeedOwnerFile above only runs on a fresh DATABASE, and his loop is the other one: he deletes the
    // deployed server FOLDER on every build, which takes owner.txt with it while the database he keeps
    // survives — so the second install had no Owner at all and no way to get one, the rank being
    // deliberately unreachable from any command.
    //
    // On a public server this is exactly the wrong behaviour: an owner.txt that writes itself is an
    // owner.txt an attacker can predict, and "the file was missing" would silently appoint whoever
    // holds that name. The real deployment wants the file placed by hand, ONCE, and a missing file to
    // mean NO owner. Hence the shouting above.
    internal static void EnsureOwnerFileForDev()
    {
        try
        {
            if (File.Exists(OwnerFile)) return;
            File.WriteAllText(OwnerFile,
                "# The one Owner character. Read at STARTUP only; edit by hand and restart.\n"
                + "# The first non-blank, non-# line wins — there can only ever be one.\n"
                + "#\n"
                + "# This file was AUTO-CREATED because it was missing (dev convenience, see\n"
                + "# ServerControl.EnsureOwnerFileForDev). Put YOUR character's name below and restart.\n"
                + "Owner\n");
            _ownerName = "Owner";
            _ownerLoaded = true;
        }
        catch { /* no owner file = no Owner; every other rank still works */ }
    }
    /// <summary>The rank a character actually holds: whatever the DB row says, unless owner.txt names
    /// them, in which case Owner and nothing else. Applied on every load, so appointing the Owner costs
    /// one line in a file and one relog.</summary>
    internal static AccountRole EffectiveRole(string characterName, AccountRole stored) =>
        OwnerCharacterName is string owner
        && string.Equals(owner, characterName, StringComparison.OrdinalIgnoreCase)
            ? AccountRole.Owner : stored;

    // ─── SCHEDULING ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Parse his minutes token: `-`, empty or `0` = instant; a number = that many minutes;
    /// anything else = 30 minutes.</summary>
    internal static int ParseMinutes(string? token)
    {
        string t = (token ?? "").Trim();
        if (t.Length == 0 || t == "-" || t == "0") return 0;
        return int.TryParse(t, out int m) && m >= 0 ? m : 30;
    }

    /// <summary>Start (or replace) a shutdown/reboot. Returns the announcement to broadcast.</summary>
    internal static string Schedule(Procedure kind, int minutes, bool adminOnlyAfterStart)
    {
        Kind = kind;
        AdminOnlyAfterStart = adminOnlyAfterStart;
        DueUtc = DateTime.UtcNow.AddMinutes(minutes);
        int seconds = minutes * 60;
        // The opening line IS the first announcement, so seed the ladder with it. Without this, a round
        // `/server shutdown 60` says "in 1:00h" twice — once here and once on the very next tick, when
        // the ladder sees 3600 seconds for the first time.
        _lastAnnounced = seconds;
        return seconds <= 0
            ? $"The server is {Verb(kind)} NOW!"
            : $"The server is {Verb(kind)} in {Format(seconds)}!";
    }

    /// <summary>Cancel any procedure, and lift the staff-only lock now or after <paramref name="minutes"/>.
    /// Returns the announcement, or null if there was nothing to say.</summary>
    internal static string Online(int minutes)
    {
        string? cancelled = Kind == Procedure.None ? null
            : $"Server {(Kind == Procedure.Reboot ? "reboot" : "shutdown")} cancelled — carry on.";
        Kind = Procedure.None;
        DueUtc = null;
        AdminOnlyAfterStart = false;
        _lastAnnounced = -1;

        if (minutes <= 0)
        {
            AdminOnlyUntilUtc = null;
            WriteLockFile(false);
            return cancelled ?? "The server is open to everyone.";
        }
        AdminOnlyUntilUtc = DateTime.UtcNow.AddMinutes(minutes);
        WriteLockFile(false);   // the timer below lives in memory; a restart in between opens the server
        return (cancelled is null ? "" : cancelled + " ")
            + $"Staff only for another {Format(minutes * 60)}.";
    }

    // ─── THE COUNTDOWN ────────────────────────────────────────────────────────────────────────────

    private static int _lastAnnounced = -1;

    /// <summary>Called every tick. Returns the line to broadcast this tick, or null for silence; sets
    /// <paramref name="fire"/> when the countdown has run out and the process must go down.</summary>
    internal static string? Tick(out bool fire)
    {
        fire = false;

        // The admin-only window can expire on its own.
        if (AdminOnlyUntilUtc is DateTime until && until != DateTime.MaxValue && DateTime.UtcNow >= until)
            AdminOnlyUntilUtc = null;

        if (Kind == Procedure.None || DueUtc is not DateTime due) return null;

        int remaining = (int)Math.Ceiling((due - DateTime.UtcNow).TotalSeconds);
        if (remaining <= 0)
        {
            fire = true;
            if (AdminOnlyAfterStart) WriteLockFile(true);
            var kind = Kind;
            Kind = Procedure.None;
            DueUtc = null;
            return $"The server is {Verb(kind)} NOW!";
        }

        if (remaining == _lastAnnounced || !ShouldAnnounce(remaining)) return null;
        _lastAnnounced = remaining;
        return $"The server is {Verb(Kind)} in {Format(remaining)}!";
    }

    /// <summary>His announcement ladder, as one predicate: whole hours above an hour · every 10 minutes
    /// from 60 down to 10 · every minute from 9 down to 1 · every second for the last 60.</summary>
    internal static bool ShouldAnnounce(int remainingSeconds) =>
        remainingSeconds <= 60                                          // 60…1, one a second
        || (remainingSeconds <= 600 && remainingSeconds % 60 == 0)      // 10…1 minutes
        || (remainingSeconds <= 3600 && remainingSeconds % 600 == 0)    // 60…10 minutes
        || remainingSeconds % 3600 == 0;                                // whole hours

    /// <summary>"1:57h" · "45 min" · "30 sec" — his own format, hours first.</summary>
    internal static string Format(int seconds)
    {
        if (seconds >= 3600) return $"{seconds / 3600}:{seconds % 3600 / 60:00}h";
        if (seconds >= 60) return $"{seconds / 60} min";
        return $"{Math.Max(seconds, 0)} sec";
    }

    private static string Verb(Procedure kind) =>
        kind == Procedure.Reboot ? "rebooting" : "shutting down";

    /// <summary>Take the process down, relaunching it first when this is a REBOOT. The relaunch is a
    /// detached copy of our own executable with our own arguments — the only restart mechanism that
    /// needs no supervisor, which matters because he runs the server under termux-ubuntu on a phone.
    ///
    /// The exit code is still distinct (66 = "restart me"), so a supervisor CAN take over the job and
    /// the self-relaunch can be dropped the day one exists.</summary>
    internal static int Execute(Procedure kind)
    {
        if (kind != Procedure.Reboot) return 0;
        try
        {
            if (Environment.ProcessPath is string exe)
            {
                var psi = new System.Diagnostics.ProcessStartInfo(exe) { UseShellExecute = false };
                foreach (var a in Environment.GetCommandLineArgs().Skip(1)) psi.ArgumentList.Add(a);
                psi.WorkingDirectory = AppContext.BaseDirectory;
                System.Diagnostics.Process.Start(psi);
            }
        }
        catch { /* the relaunch failed; the exit code still tells a supervisor what we wanted */ }
        return 66;
    }
}
