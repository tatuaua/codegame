using Game.Models;
using Npgsql;

namespace Game.Database
{
    public class DatabaseHandler : IDatabaseHandler
    {
        private static readonly string connString = "Host=localhost;Port=5432;Username=postgres;Password=mypassword;Database=postgres";
        private static bool initialized = false;

        NpgsqlConnection conn = new NpgsqlConnection(connString);

        public DatabaseHandler()
        {
            if (initialized) throw new InvalidOperationException("Database already initialized, inject the service instead");
            Init();
            initialized = true;
        }

        public void Init()
        {
            try
            {
                CreateTablesIfNotExist();
                Console.WriteLine("Initialized PostgreSQL database.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error initializing database: " + ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private void CreateTablesIfNotExist()
        {
            const string createPlayersTableQuery = @"CREATE TABLE IF NOT EXISTS players (
                id UUID PRIMARY KEY,
                name TEXT UNIQUE NOT NULL,
                password TEXT NOT NULL
            );";

            const string createGamesTableQuery = @"CREATE TABLE IF NOT EXISTS games (
                id UUID PRIMARY KEY,
                player1 UUID NOT NULL REFERENCES players(id),
                player2 UUID REFERENCES players(id),
                original_code TEXT NOT NULL,
                bugged_code TEXT,
                fixed_code TEXT
            );";

            using var command = new NpgsqlCommand(createPlayersTableQuery, conn);
            conn.Open();
            command.ExecuteNonQuery();
            command.CommandText = createGamesTableQuery;
            command.ExecuteNonQuery();
            conn.Close();
        }

        public async Task<GameBase> CreateGame(GameBase game)
        {
            const string query = "INSERT INTO games (id, player1, player2, original_code, bugged_code, fixed_code) " +
                                 "VALUES (@id, @player1, @player2, @original_code, @bugged_code, @fixed_code)";

            await using var command = new NpgsqlCommand(query, conn);
            command.Parameters.AddWithValue("@id", game.Id);
            command.Parameters.AddWithValue("@player1", game.Player1.Id);
            command.Parameters.AddWithValue("@player2", game.Player2?.Id ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@original_code", game.OriginalCode);
            command.Parameters.AddWithValue("@bugged_code", game.BuggedCode ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@fixed_code", game.FixedCode ?? (object)DBNull.Value);

            await conn.OpenAsync();
            await command.ExecuteNonQueryAsync();
            await conn.CloseAsync();

            return game;
        }

        public async Task<Player> CreatePlayer(string name, string password)
        {
            var player = new Player { Id = Guid.NewGuid().ToString(), Name = name, PassWord = password, IsInGame = false, Session = null };
            const string query = "INSERT INTO players (id, name, password) VALUES (@id, @name, @password)";

            await using var command = new NpgsqlCommand(query, conn);
            command.Parameters.AddWithValue("@id", player.Id);
            command.Parameters.AddWithValue("@name", player.Name);
            command.Parameters.AddWithValue("@password", player.PassWord);

            await conn.OpenAsync();
            await command.ExecuteNonQueryAsync();
            await conn.CloseAsync();

            return player;
        }

        public async Task<Player?> GetPlayer(string name)
        {
            const string query = "SELECT id, name, password FROM players WHERE name = @name";

            await using var command = new NpgsqlCommand(query, conn);
            command.Parameters.AddWithValue("@name", name);

            await conn.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();

            Player? player = null;
            if (await reader.ReadAsync())
            {
                player = new Player
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1),
                    PassWord = reader.GetString(2),
                };
            }

            await conn.CloseAsync();
            return player;
        }
    }
}
