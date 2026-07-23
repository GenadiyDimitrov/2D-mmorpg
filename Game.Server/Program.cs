using Game.Server.Hubs;
using Game.Server.Persistence;
using Game.Server.Simulation;
using Microsoft.EntityFrameworkCore;

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
builder.WebHost.UseUrls(
    Environment.GetEnvironmentVariable("ASPNETCORE_URLS") is { Length: > 0 } urls
        ? urls
        : "http://0.0.0.0:5238");

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

app.Logger.LogInformation("L2Clone server v{Version} starting.", Game.Shared.GameConstants.GameVersion);

// Print the LAN address the phone should use. "Now listening on: http://0.0.0.0:5238" is technically
// correct and completely useless when you're standing there with a phone — 0.0.0.0 is not something you
// can type into the client. This resolves the actual address instead of sending you to ipconfig.
foreach (var ip in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
             .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
             .Where(n => n.NetworkInterfaceType is System.Net.NetworkInformation.NetworkInterfaceType.Ethernet
                                                or System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211)
             .SelectMany(n => n.GetIPProperties().UnicastAddresses)
             .Select(a => a.Address)
             .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                         && !System.Net.IPAddress.IsLoopback(a)))
{
    app.Logger.LogInformation("  Unity/phone clients on this LAN: http://{Ip}:5238/game", ip);
}

app.Run();
