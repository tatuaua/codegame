using Game.Models;
using Npgsql;

namespace Game.Database
{
    public class DatabaseHandler : IDatabaseHandler
    {
        private static readonly string connString = "Host=localhost;Port=5432;Username=postgres;Password=mypassword;Database=postgres";

        NpgsqlConnection conn = new NpgsqlConnection(connString);

        public DatabaseHandler()
        {
            Init();
        }

        public void Init()
        {
            try
            {
                conn.Open();
                Console.WriteLine("Connected to PostgreSQL successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error connecting to PostgreSQL: " + ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        public Task<GameBase> CreateGame(GameBase game)
        {
            throw new NotImplementedException();
        }

        public Task<Player> CreatePlayer(string name, string password)
        {
            throw new NotImplementedException();
        }

        public Task<Player> GetPlayer(string name)
        {
            throw new NotImplementedException();
        }
    }
}
