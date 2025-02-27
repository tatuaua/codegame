var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging();

var app = builder.Build();
app.UseWebSockets();

// Get logger instance from DI
var logger = app.Services.GetRequiredService<ILogger<GameWebSocketHandler>>();
var gameHandler = new GameWebSocketHandler(logger);

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
