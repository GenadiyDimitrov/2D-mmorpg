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
app.MapGet("/", () => "Game server is running. Hub endpoint: /game");

app.Run();
