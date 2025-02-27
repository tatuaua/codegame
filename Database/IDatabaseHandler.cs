using Game.Models;

namespace Game.Database
{
    public interface IDatabaseHandler
    {
        public void Init();
        public Task<Player> GetPlayer(string name);
        public Task<Player> CreatePlayer(string name, string password);
        public Task<GameBase> CreateGame(GameBase game);
    }
}
