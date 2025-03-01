using Game.Models;

namespace Game.Database
{
    public interface IDatabaseHandler
    {
        public void Init();
        public Task<Player?> GetPlayer(string name);
        public Task InsertPlayer(Player player);
        public Task<GameBase> InsertGame(GameBase game);
    }
}
