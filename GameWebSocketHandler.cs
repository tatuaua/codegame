using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Text;
using System.Net.Http;
using Game.Models;
using Game.Database;

public class GameWebSocketHandler
{
    private static readonly List<Player> loggedInPlayers = [];
    private static readonly List<GameBase> ongoingGames = [];
    private readonly ILogger<GameWebSocketHandler> _logger;
    private readonly IDatabaseHandler _db;

    public GameWebSocketHandler(ILogger<GameWebSocketHandler> logger, IDatabaseHandler db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task HandleWebSocket(HttpContext context, WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];

        try
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Threading.CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                _logger.LogInformation("Received message: {receivedMessage}", receivedMessage);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                Message messageObj;
                try
                {
                    messageObj = JsonSerializer.Deserialize<Message>(receivedMessage, options) ?? throw new Exception("Invalid JSON");
                }
                catch (Exception ex)
                {
                    _logger.LogError("JSON Deserialization Error: {Message}", ex.Message);
                    await SendError("Invalid request format.", webSocket);
                    return;
                }

                var player = await CheckLogin(messageObj.Player.Name, messageObj.Player.PassWord, webSocket);
                if (player == null)
                {
                    _logger.LogWarning("Login failed for player: {PlayerName}", messageObj.Player.Name);
                    await SendError("Bad password", webSocket);
                    return;
                }

                player.Session = webSocket;

                try
                {
                    switch (messageObj.Action)
                    {
                        case "createGame":
                            await CreateGame(player);
                            break;
                        case "findGame":
                            await FindGame(player);
                            break;
                        case "joinGame":
                            await JoinGame(player, messageObj.GameId);
                            break;
                        case "bug":
                            await BugCode(player, messageObj.Code, messageObj.GameId);
                            break;
                        case "fix":
                            await FixCode(player, messageObj.Code, messageObj.GameId);
                            break;
                        default:
                            _logger.LogWarning("Unknown action: {Action}", messageObj.Action);
                            await SendError("Unknown action.", webSocket);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error processing action {Action}: {Message}", messageObj.Action, ex.Message);
                    await SendError("Internal server error.", webSocket);
                }

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Threading.CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("WebSocket error: {Message}", ex.Message);
            _logger.LogError("{StackTrace}", ex.StackTrace);
        }
        finally
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", System.Threading.CancellationToken.None);
            _logger.LogInformation("WebSocket connection closed.");
        }
    }

    private async Task CreateGame(Player player)
    {
        if (player.IsInGame)
        {
            await SendError("Player is already in a game.", player.Session);
            return;
        }

        var game = new GameBase { Id = Guid.NewGuid().ToString(), Player1 = player, State = GameBase.GameState.Created, OriginalCode = GetCode() };
        player.IsInGame = true;
        ongoingGames.Add(game);
        await SendGameId(game.Id, player.Session);
    }

    private async Task FindGame(Player player)
    {
        var game = ongoingGames.FirstOrDefault(g => g.State == GameBase.GameState.Created);
        if (game != null)
        {
            await SendGameId(game.Id, player.Session);
        }
        else
        {
            await SendError("No available games found.", player.Session);
        }
    }

    private async Task JoinGame(Player player, string gameId)
    {
        if (player.IsInGame)
        {
            await SendError("Player is already in a game.", player.Session);
            return;
        }

        var game = ongoingGames.FirstOrDefault(g => g.Id == gameId);
        if (game == null || game.State != GameBase.GameState.Created)
        {
            await SendError("Invalid game ID or game already started.", player.Session);
            return;
        }

        game.Player2 = player;
        game.State = GameBase.GameState.Bugging;
        await SendCode(game.OriginalCode, game.Player1.Session);
    }

    private async Task BugCode(Player player, string code, string gameId)
    {
        var game = ongoingGames.FirstOrDefault(g => g.Id == gameId);
        if (game != null && game.State == GameBase.GameState.Bugging && game.Player2 != null)
        {
            game.BuggedCode = code;
            game.State = GameBase.GameState.Fixing;
            await SendCode(code, game.Player2.Session);
        }
    }

    private async Task FixCode(Player player, string code, string gameId)
    {
        var game = ongoingGames.FirstOrDefault(g => g.Id == gameId);
        if (game != null && game.State == GameBase.GameState.Fixing)
        {
            game.FixedCode = code;
            game.State = GameBase.GameState.Ended;
            ongoingGames.Remove(game);
            try
            {
                await _db.InsertGame(game);
            }
            catch(InvalidOperationException e)
            {
                _logger.LogCritical("Inserting game failed: {message}", e.Message);
            }
        }
    }

    private async Task SendCode(string code, WebSocket session)
    {
        var json = JsonSerializer.Serialize(new { code });
        await session.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
    }

    private async Task SendGameId(string gameId, WebSocket session)
    {
        var json = JsonSerializer.Serialize(new { gameId });
        await session.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
    }

    private async Task SendError(string error, WebSocket session)
    {
        var json = JsonSerializer.Serialize(new { error });
        await session.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        _logger.LogWarning("Sent error response: {Error}", error);
    }
    private string GetCode() => "some code\nwith lines\naaaa";

    private async Task<Player?> CheckLogin(string name, string passWord, WebSocket session)
    {
        var player = loggedInPlayers.FirstOrDefault(p => p.Name == name);
        if (player != null)
        {
            return player.PassWord == passWord ? player : null;
        }
        player = new Player { Name = name, PassWord = passWord, Id = Guid.NewGuid().ToString(), IsInGame = false, Session = session };
        loggedInPlayers.Add(player);
        await _db.InsertPlayer(player);
        return player;
    }
}