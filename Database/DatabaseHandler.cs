using Game.Models;
using System;
using Microsoft.Data.SqlClient;

namespace Game.Database
{
    public class DatabaseHandler : IDatabaseHandler
    {
        static readonly string connString = "Server=myServerAddress;Database=myDataBase;Uid=myUsername;Pwd=myPassword;";

        SqlConnection conn = new SqlConnection(connString);

        void IDatabaseHandler.Init()
        {
            throw new NotImplementedException();
        }

        Task<GameBase> IDatabaseHandler.CreateGame(GameBase game)
        {
            throw new NotImplementedException();
        }

        Task<Player> IDatabaseHandler.CreatePlayer(string name, string password)
        {
            throw new NotImplementedException();
        }

        Task<Player> IDatabaseHandler.GetPlayer(string name)
        {
            throw new NotImplementedException();
        }
    }
}
