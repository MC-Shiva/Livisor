using Livisor.Server.Application.UseCases;
using Livisor.Server.Domain.Cache;
using Livisor.Server.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMagicOnion();

// --- レイヤー配線（Composition Root）---
// Infrastructure: room ごとの直近タイムラインをメモリにキャッシュ（遅延参加者へ再送）。
builder.Services.AddSingleton<ITimelineCache, TimelineCache>();
// Application: ユースケース。
builder.Services.AddTransient<JoinRoomUseCase>();
builder.Services.AddTransient<BroadcastTimelineUseCase>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapMagicOnionService();
app.MapGet(
    "/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909"
);

app.Run();
