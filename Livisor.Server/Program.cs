using Livisor.Server.Application.UseCases;
using Livisor.Server.Domain.Cache;
using Livisor.Server.Domain.Time;
using Livisor.Server.Infrastructure;
using Livisor.Server.Logging;
using Livisor.Server.Presentation.Providers;
using Livisor.Server.Presentation.Mapping;
using Livisor.Shared.DTO;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddAppLogging();

// Add services to the container.
builder.Services.AddMagicOnion();

// --- レイヤー配線（Composition Root）---
// Infrastructure: room（Room集約）をメモリにキャッシュ。サーバー時刻の取得。
var defaultActions = DefaultActionSet.Create().Select(ScheduledActionMapper.ToDomain).ToArray();
builder.Services.AddSingleton<IRoomCache>(new RoomCache(defaultActions));
builder.Services.AddSingleton<IClock, SystemClock>();
// Presentation: room ごとの配信グループ。Unary サービスと StreamingHub で共有する。
builder.Services.AddSingleton<RoomGroupProvider>();
// Application: ユースケース。
builder.Services.AddTransient<RoomUseCase>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapMagicOnionService();
app.MapGet(
    "/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909"
);

app.Run();
