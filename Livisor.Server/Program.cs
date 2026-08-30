using Livisor.Server.Domain.Cache;
using Livisor.Server.Infrastructure;
using Livisor.Server.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddAppLogging();

// Add services to the container.
builder.Services.AddMagicOnion();

// --- レイヤー配線（Composition Root）---
// Infrastructure: room（Room集約）をメモリにキャッシュ。
builder.Services.AddSingleton<IRoomCache, RoomCache>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapMagicOnionService();
app.MapGet(
    "/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909"
);

app.Run();
