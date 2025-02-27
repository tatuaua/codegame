namespace Game.Models
{
    public class Message
    {
        public required string Action { get; set; }
        public required Player Player { get; set; }
        public string GameId { get; set; }
        public string Code { get; set; }
    }
}
