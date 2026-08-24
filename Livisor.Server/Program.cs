using Livisor.Server.Application.UseCases;
using Livisor.Server.Domain.Cache;
using Livisor.Server.Infrastructure;
using ZLogger;

var builder = WebApplication.CreateBuilder(args);

// ロギング: 既定プロバイダを外し、ZLogger のコンソール出力（JSON）に一本化する。
builder.Logging.ClearProviders();
builder.Logging.AddZLoggerConsole(options =>
{
    // BeginScope で渡した room-id/ConnectionId 等を JSON のトップレベルに出力する。
    options.IncludeScopes = true;
    options.UseJsonFormatter();
});

// Add services to the container.
builder.Services.AddMagicOnion();

// --- レイヤー配線（Composition Root）---
// Infrastructure: room（Room集約）をメモリにキャッシュ（遅延参加者へ再送）。
builder.Services.AddSingleton<IRoomCache, RoomCache>();
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
