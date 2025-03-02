using Game.Models;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Game.Database
{
    public class DatabaseHandler : IDatabaseHandler
    {
        private static bool initialized = false;
        private readonly ILogger<DatabaseHandler> _logger;
        private readonly IOptions<DatabaseSettings> _settings;

        NpgsqlConnection conn;

        public DatabaseHandler(ILogger<DatabaseHandler> logger, IOptions<DatabaseSettings> settings)
        {
            if (initialized) throw new InvalidOperationException("Database already initialized, inject the service instead");
            _logger = logger;
            _settings = settings;
            conn = new NpgsqlConnection(_settings.Value.ConnectionString ?? "Host=localhost;Port=5432;Username=postgres;Password=mypassword;Database=postgres");
            Init();
        }

        public void Init()
        {
            try
            {
                initialized = true;
                CreateTables();
                _logger.LogInformation("Initialized PostgreSQL database.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to initialize the database. Application will now exit.");
                throw; // Rethrow the exception to crash the app
            }
        }

        private void CreateTables()
        {
            const string dropGames = @"DROP TABLE IF EXISTS games;";
            const string dropPlayers = @"DROP TABLE IF EXISTS players;";

            const string createPlayersTableQuery = @"CREATE TABLE players (
                id TEXT PRIMARY KEY,
                name TEXT UNIQUE NOT NULL,
                password TEXT NOT NULL
            );";

            const string createGamesTableQuery = @"CREATE TABLE games (
                id TEXT PRIMARY KEY,
                player1 TEXT NOT NULL REFERENCES players(id),
                player2 TEXT NOT NULL REFERENCES players(id),
                original_code TEXT NOT NULL,
                bugged_code TEXT,
                fixed_code TEXT
            );";

            using var command = new NpgsqlCommand(dropGames, conn);
            conn.Open();
            command.ExecuteNonQuery();
            command.CommandText = dropPlayers;
            command.ExecuteNonQuery();
            command.CommandText = createPlayersTableQuery;
            command.ExecuteNonQuery();
            command.CommandText = createGamesTableQuery;
            command.ExecuteNonQuery();
            conn.Close();
        }

        public async Task<GameBase> InsertGame(GameBase game)
        {
            _logger.LogInformation("Inserting game: {game}", game.ToString());
            const string query = "INSERT INTO games (id, player1, player2, original_code, bugged_code, fixed_code) " +
                                 "VALUES (@id, @player1, @player2, @original_code, @bugged_code, @fixed_code)";

            await using var command = new NpgsqlCommand(query, conn);
            command.Parameters.AddWithValue("@id", game.Id);
            command.Parameters.AddWithValue("@player1", game.Player1.Id);
            command.Parameters.AddWithValue("@player2", game.Player2?.Id ?? throw new InvalidOperationException("Player2 cannot be null."));
            command.Parameters.AddWithValue("@original_code", game.OriginalCode);
            command.Parameters.AddWithValue("@bugged_code", game.BuggedCode ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@fixed_code", game.FixedCode ?? (object)DBNull.Value);

            await conn.OpenAsync();
            await command.ExecuteNonQueryAsync();
            await conn.CloseAsync();

            return game;
        }

        public async Task InsertPlayer(Player player)
        {
            const string query = "INSERT INTO players (id, name, password) VALUES (@id, @name, @password)";

            await using var command = new NpgsqlCommand(query, conn);
            command.Parameters.AddWithValue("@id", player.Id);
            command.Parameters.AddWithValue("@name", player.Name);
            command.Parameters.AddWithValue("@password", player.PassWord);

            await conn.OpenAsync();
            await command.ExecuteNonQueryAsync();
            await conn.CloseAsync();
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
