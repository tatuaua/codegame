using System.Net.WebSockets;

namespace Game.Models
{
    public class Player
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string PassWord { get; set; }
        public bool IsInGame { get; set; }
        public WebSocket Session { get; set; }
        public override string ToString()
        {
            return $"""
            Player ID: {Id}
            Name: {Name}
            IsInGame: {IsInGame}
            Session: {(Session?.State.ToString() ?? "None")}
            """;
        }
    }
}
