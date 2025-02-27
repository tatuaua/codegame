namespace Game.Models
{
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
}
