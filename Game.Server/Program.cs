using Game.Server.Hubs;
using Game.Server.Simulation;

var builder = WebApplication.CreateBuilder(args);

// Fixed URL so the demo client can connect without configuration.
builder.WebHost.UseUrls("http://localhost:5238");

builder.Services.AddSignalR();
builder.Services.AddSingleton<World>();
builder.Services.AddHostedService<GameLoopService>();

var app = builder.Build();

app.MapHub<GameHub>("/game");
app.MapGet("/", () => "Game server is running. Hub endpoint: /game");

app.Run();
