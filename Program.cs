using Game.Database;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging();
builder.Services.AddSingleton<IDatabaseHandler, DatabaseHandler>();
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("DatabaseSettings")
);

var app = builder.Build();
app.UseWebSockets();

var logger = app.Services.GetRequiredService<ILogger<GameWebSocketHandler>>();
var dbHandler = app.Services.GetRequiredService<IDatabaseHandler>();

var gameHandler = new GameWebSocketHandler(logger, dbHandler);

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await gameHandler.HandleWebSocket(context, webSocket);
    }
    else
    {
        await next();
    }
});

app.Run();
