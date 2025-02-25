var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();
var gameHandler = new GameWebSocketHandler();

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
