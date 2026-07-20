using Game.Server.Hubs;
using Game.Server.Persistence;
using Game.Server.Simulation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5238");

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
    await persistence.EnsureCreatedAsync();
#if DEBUG
    // Dev only: seed admin/admin + test1..test9/test (one char each) on an empty db.
    await persistence.SeedDebugAccountsAsync();
#endif
}

app.MapHub<GameHub>("/game");
app.MapGet("/", () => $"Game server v{Game.Shared.GameConstants.GameVersion} is running. Hub endpoint: /game");
app.MapGet("/version", () => Game.Shared.GameConstants.GameVersion);

// Fail loudly, at startup, if two skills or consumables ended up sharing a skill-bar label — the same
// spirit as the skill-id collision guard. An ambiguous square is otherwise invisible until a player
// squints at their bar mid-fight and can't tell two buffs apart.
Game.Shared.Abbreviations.Validate();

app.Logger.LogInformation("L2Clone server v{Version} starting.", Game.Shared.GameConstants.GameVersion);
app.Run();
