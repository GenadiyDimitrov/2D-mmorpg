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
    // Format: `account/Character` on the first non-blank, non-`#` line (`BL-345`, 2026-10-13). Everything
    // after it is ignored, so "cannot have two" is enforced by READING rather than by validating. A line
    // with no `/` names the character only, and then any account may hold it.
    //
    // 🔑 THE PAIR ALSO OPENS A FRESH SERVER (`BL-345`, his design): until that account holds that
    // character, NOTHING else can be registered or created — see <see cref="OwnerBootstrapped"/>. So a
    // new server is his first, and he lets everyone else in with `/registration open`. Both names are
    // exempt from the reserved-word rule, which is what lets the dev default stay `admin/Owner`.
    //
    // ⚠ Read ONCE. Editing the file mid-session changes nothing until the server restarts — which is
    // the point: it is the one authority a running server cannot be talked out of.
    private static readonly string OwnerFile =
        Path.Combine(AppContext.BaseDirectory, "owner.txt");

    private static string? _ownerName;
    private static string? _ownerAccount;
    private static bool _ownerLoaded;

    private static void LoadOwner()
    {
        if (_ownerLoaded) return;
        _ownerLoaded = true;
        try
        {
            if (!File.Exists(OwnerFile)) return;
            string? line = File.ReadLines(OwnerFile)
                .Select(l => l.Trim())
                .FirstOrDefault(l => l.Length > 0 && !l.StartsWith("#"));
            if (line is null) return;
            int slash = line.IndexOf('/');
            if (slash < 0) { _ownerName = line; return; }
            string acc = line[..slash].Trim(), name = line[(slash + 1)..].Trim();
            _ownerAccount = acc.Length > 0 ? acc : null;
            _ownerName = name.Length > 0 ? name : null;
        }
        catch { /* unreadable file = no owner; the server must still start */ }
    }

    /// <summary>The Owner's character name, or null when no owner.txt names one. Compared
    /// case-insensitively everywhere.</summary>
    internal static string? OwnerCharacterName { get { LoadOwner(); return _ownerName; } }

    /// <summary>The account that must hold the Owner character, or null when owner.txt names none.</summary>
    internal static string? OwnerAccountName { get { LoadOwner(); return _ownerAccount; } }

    /// <summary>Is this the owner.txt ACCOUNT? With no account named there, no username is.</summary>
    internal static bool IsOwnerAccount(string username) =>
        OwnerAccountName is string a && string.Equals(a, username.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>Is this account + character name exactly the owner.txt pair?</summary>
    internal static bool IsOwnerPair(string username, string characterName) =>
        OwnerCharacterName is string n
        && string.Equals(n, characterName.Trim(), StringComparison.OrdinalIgnoreCase)
        && (OwnerAccountName is null || IsOwnerAccount(username));

    // `BL-345` — the owner bootstrap. True once the owner.txt pair exists in the database; until then the
    // server admits only that pair. Set by PersistenceService (which owns the database) and latched: the
    // Owner being deleted later is not a reason to lock everyone else out again.
    private static volatile bool _ownerBootstrapped;
    internal static bool OwnerBootstrapped => _ownerBootstrapped || OwnerCharacterName is null;
    internal static void MarkOwnerBootstrapped() => _ownerBootstrapped = true;

    // Dev convenience: a missing owner.txt is written as `admin/Owner`. Owner, playtest 27: *"each time I
    // remove the GameServer folder it will remove it as well"* — and his deleted folder keeps its database,
    // so the second install would otherwise have no Owner at all and no way to get one.
    //
    // `BL-345` made this safe enough to keep: the file is a PAIR now, and the bootstrap admits only that
    // pair on a fresh database. The one hole left is a stranger registering `admin` on a fresh PUBLIC
    // server before he does, which is why docs/guides/Hosting.md writes owner.txt by hand first.
    internal static void EnsureOwnerFile()
    {
        try
        {
            if (File.Exists(OwnerFile)) return;
            File.WriteAllText(OwnerFile,
                "# The Owner: account/Character. Read at STARTUP only; edit by hand and restart.\n"
                + "# The first non-blank, non-# line wins — there can only ever be one.\n"
                + "# Until this account holds this character, nobody else can register or create one.\n"
                + "#\n"
                + "# This file was AUTO-CREATED because it was missing. Put YOUR account/character below.\n"
                + "admin/Owner\n");
            _ownerLoaded = false;
            LoadOwner();
        }
        catch { /* no owner file = no Owner; every other rank still works */ }
    }

    // ═══ REGISTRATION ═════════════════════════════════════════════════════════════════════════════
    //
    // `BL-345` — `/registration open|close`. CLOSED refuses new accounts AND new characters for everyone
    // but the owner.txt pair (his choice: *"forbids character creations and registrations of any kind"*).
    // Remembered in a file beside game.db (the working directory), so it lives exactly as long as the
    // database does — deleting the server folder in his dev loop does not reopen a closed server.
    //
    // A missing file means CLOSED on a release build (a new public server lets nobody in until he says so)
    // and OPEN on a debug build (the dev seed and the SmokeTest register fresh accounts every run).
    private static readonly string RegistrationFile =
        Path.Combine(Directory.GetCurrentDirectory(), "registration.txt");

    private static bool? _registrationOpen;

    internal static bool RegistrationOpen
    {
        get
        {
            if (_registrationOpen is bool b) return b;
#if DEBUG
            bool open = true;
#else
            bool open = false;
#endif
            try
            {
                if (File.Exists(RegistrationFile))
                    open = File.ReadAllText(RegistrationFile).Trim()
                        .StartsWith("open", StringComparison.OrdinalIgnoreCase);
            }
            catch { /* unreadable = the build's default */ }
            _registrationOpen = open;
            return open;
        }
    }

    internal static void SetRegistration(bool open)
    {
        _registrationOpen = open;
        try { File.WriteAllText(RegistrationFile, open ? "open" : "closed"); }
        catch { /* honoured for this run even if it cannot be remembered */ }
    }

    /// <summary>Why a new account or character is refused right now, or null when it is allowed.</summary>
    internal static string? CreationBlocked(bool ownerPair)
    {
        if (ownerPair) return null;
        if (!OwnerBootstrapped) return "The server is not open yet.";
        if (!RegistrationOpen) return "Registration is closed.";
        return null;
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
    /// The exit code is distinct (66 = "restart me"); under systemd the relaunch is skipped and the
    /// unit's <c>Restart=always</c> does the job (`BL-345`).</summary>
    internal static int Execute(Procedure kind)
    {
        if (kind != Procedure.Reboot) return 0;
        // Under systemd (which sets INVOCATION_ID) the unit's Restart= brings us back; a copy launched
        // from here would be killed along with the old process's cgroup.
        if (Environment.GetEnvironmentVariable("INVOCATION_ID") is { Length: > 0 }) return 66;
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
