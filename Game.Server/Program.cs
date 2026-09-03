using Game.Server.Hubs;
using Game.Server.Persistence;
using Game.Server.Simulation;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("Start!");
try
{
    var builder = WebApplication.CreateBuilder(args);

    // Bind ALL interfaces, not just loopback, so a phone on the same Wi-Fi can reach the dev server
    // (Game.Client.Unity/README.md step 4.1). 0.0.0.0 still serves http://localhost:5238 exactly as before,
    // so the WPF client and the smoke test are unaffected — it only ADDS the LAN address.
    //
    // This used to be a hardcoded UseUrls("http://localhost:5238"), which silently BEAT the
    // ASPNETCORE_URLS the README told you to set: the variable had no effect, the server stayed on
    // loopback, and the phone just failed to connect with nothing in the log to explain why. An explicit
    // override is honoured now.
    //
    // NOTE: `applicationUrl` in Properties/launchSettings.json is ALSO delivered as ASPNETCORE_URLS, so
    // leaving one there would quietly win over this default (it did — F5 came up on a random localhost
    // port instead of 5238). That key is deliberately absent from launchSettings; set the env var
    // explicitly if you want a different address.
    var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    urls = urls?.Length > 0
            ? urls
            : "http://0.0.0.0:5238";
    builder.WebHost.UseUrls(urls);

    // Keep the console READABLE. EF Core logs every command it executes at Information, and this server
    // saves characters constantly (event saves + a 60s autosave over every online player), so the useful
    // lines — who logged in, what the game loop said — were buried under a continuous stream of SQL
    // (owner, playtest-13: "it's overflooding it, just important information"). Warnings and errors from
    // EF still come through; only the per-statement chatter is dropped.
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Query", LogLevel.Warning);
    // ASP.NET's own per-request lines are the same kind of noise on a game server whose real traffic is
    // the SignalR hub, not HTTP.
    builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);

    builder.Services.AddSignalR();

    // Persistence: SQLite file next to the executable. Swap UseSqlite for
    // UseNpgsql/UseSqlServer here to change databases — nothing else changes.
    builder.Services.AddDbContextFactory<GameDbContext>(options =>
        options.UseSqlite("Data Source=game.db"));
    builder.Services.AddSingleton<PersistenceService>();

    builder.Services.AddSingleton<World>();
    builder.Services.AddHostedService<GameLoopService>();

    var app = builder.Build();

    // Create the database/schema on first run.
    using (var scope = app.Services.CreateScope())
    {
        var persistence = scope.ServiceProvider.GetRequiredService<PersistenceService>();
#if DEBUG
        // Dev only, DESTRUCTIVE: EnsureCreated never adds columns to an EXISTING database, so a schema
        // change used to mean deleting game.db by hand — and forgetting produced "table Characters has no
        // column named X" from inside a save, which reads like a bug in whatever you just wrote. While in
        // development no character is worth keeping (owner), so a stale database is simply rebuilt.
        await persistence.ResetIfSchemaStaleAsync(app.Logger);
#endif
        await persistence.EnsureCreatedAsync();
#if DEBUG
        // Dev only: seed admin/admin (a level-90 Warchanter in A-grade gear) + test1..test9/test
        // (plain level-1 fighters) on an empty db.
        await persistence.SeedDebugAccountsAsync();
#endif
    }

    app.MapHub<GameHub>("/game");
    app.MapGet("/", () => Results.Content(
        $"<h2>Game server v{Game.Shared.GameConstants.GameVersion}</h2>" +
        "<p>Hub: <code>/game</code></p>" +
        "<p><a href=\"/apk\">Download the Android client (L2Clone.apk)</a></p>", "text/html"));
    app.MapGet("/version", () => Game.Shared.GameConstants.GameVersion);

    // Serve the built APK so a phone on this LAN/VPN can download it straight from the browser (no adb):
    // open http://<server-ip>:5238/apk. Reuses the already-allowed game port, so no extra firewall rule.
    app.MapGet("/apk", () =>
    {
        var apk = Path.GetFullPath(Path.Combine(
            app.Environment.ContentRootPath, "..", "Game.Client.Unity", "builds", "L2Clone.apk"));
        return File.Exists(apk)
            ? Results.File(apk, "application/vnd.android.package-archive", "L2Clone.apk", enableRangeProcessing: true)
            : Results.NotFound("APK not built yet — check back in a minute.");
    });

    // Fail loudly, at startup, if two skills or consumables ended up sharing a skill-bar label — the same
    // spirit as the skill-id collision guard. An ambiguous square is otherwise invisible until a player
    // squints at their bar mid-fight and can't tell two buffs apart.
    Game.Shared.Abbreviations.Validate();

    // Enforce "no rogue spawners" (owner) — every spawn zone must fall inside a field. Fails startup loudly
    // with the offending coordinates rather than letting a field-less circle onto the map.
    Game.Shared.RegionMap.ValidateSpawnersInFields();

    // The two promises a generated dungeon makes that arithmetic could quietly break: the clear run-up to
    // the boss (his rule — a respawning guard must not be able to pull you off it), and the corridor mouth
    // actually overlapping its entrance safe zone. Neither is visible on a map screenshot.
    Game.Shared.DungeonLayout.Validate();

    // BL-47 — every piece of gear a player-built creature names must exist. A missing id is silent AND
    // flattering: the creature spawns without that slot, and a half-naked entity reads as "the player
    // pipeline under-delivers" when the truth is a typo in a tier.
    Game.Shared.MobCatalog.ValidateBuilds();

    // The generated world layout: camp spacing, elite distance, town clearance, no overlapping fields, and
    // every camp's roster inside its own level band (the "pig next to a werewolf" guard). A bearing is not
    // a picture — none of this is visible in the source, and all of it is obvious only after walking there.
    Game.Shared.WorldPlan.ValidateLayout();
    Game.Shared.WorldPlan.ValidateLevelCoverage();

    // Enforce "no two neighbouring NPCs on the same screen line" (owner, playtest-13) — an overlapping
    // name plate hides the neighbour's quest "!"/"?" and is only visible on a phone.
    Game.Shared.WorldMap.ValidateNpcLabels();

    // Every RUNE must name a real buff skill, and a reward rune must name a real RUNG of it. A rune
    // pointing at a missing skill (or at level 12 of an 11-rung ladder) is silently inert: it sits in
    // the bag looking exactly right and pays nothing. Cheap to check, invisible to a playtest.
    Game.Shared.ItemCatalog.ValidateRunes();

    // No two classes may share a NAME (2026-08-17, per-race 3rd/4th names). The class-change NPC
    // lists what you may become BY NAME, so a duplicate makes two different changes indistinguishable
    // — and with 48 hand-written strings in one table, a repeat is the likeliest typo there is.
    if (Game.Shared.ClassNames.DuplicateNames().ToList() is { Count: > 0 } dupes)
        throw new InvalidOperationException(
            "Duplicate class names in ClassNames.Table:\n  " + string.Join("\n  ", dupes));

    // Every race/class column of GetBaseStats must sum to 153 (owner, 2026-08-28): a race is a
    // REDISTRIBUTION of the same points, never a bigger pile. This drifted unnoticed for weeks —
    // the six columns had reached 153/153/150 and 148/141/162, leaving the elf mage 21 points
    // behind the demon mage — because nothing ever added them up. A one-cell edit here is the
    // easiest possible slip and the hardest to see in a playtest, so the server refuses to boot.
    if (Game.Shared.StatCalculator.BaseStatsNotSummingTo153().ToList() is { Count: > 0 } sums)
        throw new InvalidOperationException(
            "Base stat columns must each sum to 153 (StatCalculator.GetBaseStats):\n  "
            + string.Join("\n  ", sums));

    app.Logger.LogInformation("L2Clone server v{Version} starting.", Game.Shared.GameConstants.GameVersion);

    // (There used to be a LAN-address printout here for the phone. It enumerated every NIC that was up,
    // so on this machine it printed the two virtual-adapter addresses a phone can never reach alongside
    // the real one — three lines of guesswork. Removed with the rest of the boot noise, owner 2026-09-03;
    // `ipconfig` answers the same question without lying about it.)

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    throw;
}
