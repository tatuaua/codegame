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

public class GameWebSocketHandler
{
    private static readonly List<Player> loggedInPlayers = new List<Player>();
    private static readonly List<GameBase> ongoingGames = new List<GameBase>();

    public async Task HandleWebSocket(HttpContext context, WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Threading.CancellationToken.None);

        while (!result.CloseStatus.HasValue)
        {
            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var messageObj = JsonSerializer.Deserialize<Message>(receivedMessage, options) ?? throw new Exception();

            var player = CheckLogin(messageObj.Player.Name, messageObj.Player.PassWord, webSocket);
            if (player == null)
            {
                await webSocket.SendAsync(Encoding.UTF8.GetBytes("bad password"), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
                return;
            }

            player.Session = webSocket;

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
                    break;
            }

            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Threading.CancellationToken.None);
        }

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, System.Threading.CancellationToken.None);
    }

    private async Task CreateGame(Player player)
    {
        if (player.IsInGame)
            return;

        var game = new GameBase { Id = Guid.NewGuid().ToString(), Player1 = player, State = GameBase.GameState.Created, OriginalCode = GetCode() };
        player.IsInGame = true;
        ongoingGames.Add(game);
        await SendGameId(game.Id, player.Session);
    }

    private async Task FindGame(Player player)
    {
        var game = ongoingGames.FirstOrDefault(g => g.State == GameBase.GameState.Created);
        if (game != null)
            await SendGameId(game.Id, player.Session);
    }

    private async Task JoinGame(Player player, string gameId)
    {
        if (player.IsInGame)
            return;

        var game = ongoingGames.FirstOrDefault(g => g.Id == gameId);
        if (game != null && game.State == GameBase.GameState.Created)
        {
            game.Player2 = player;
            game.State = GameBase.GameState.Bugging;
            await SendCode(game.OriginalCode, game.Player1.Session);
        }
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

    private Task FixCode(Player player, string code, string gameId)
    {
        var game = ongoingGames.FirstOrDefault(g => g.Id == gameId);
        if (game != null && game.State == GameBase.GameState.Fixing)
        {
            game.FixedCode = code;
            game.State = GameBase.GameState.Ended;
            ongoingGames.Remove(game);
        }

        return Task.CompletedTask;
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

    private string GetCode() => "some code\nwith lines\naaaa";

    private Player? CheckLogin(string name, string passWord, WebSocket session)
    {
        var player = loggedInPlayers.FirstOrDefault(p => p.Name == name);
        if (player != null)
        {
            return player.PassWord == passWord ? player : null;
        }
        player = new Player { Name = name, PassWord = passWord, Id = Guid.NewGuid().ToString(), IsInGame = false, Session = session };
        loggedInPlayers.Add(player);
        return player;
    }
}

public class Player
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string PassWord { get; set; }
    public bool IsInGame { get; set; }
    public WebSocket Session { get; set; }
}

public class GameBase
{
    public required string Id { get; set; }
    public required Player Player1 { get; set; }
    public Player? Player2 { get; set; }
    public required string OriginalCode { get; set; }
    public string? BuggedCode { get; set; }
    public string? FixedCode { get; set; }
    public required GameState State { get; set; }

    public enum GameState
    {
        Created,
        Bugging,
        Fixing,
        Ended
    }
}

public class Message
{
    public required string Action { get; set; }
    public required Player Player { get; set; }
    public string GameId { get; set; }
    public string Code { get; set; }
}
