using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;

namespace Battleship.Models
{
    /* Models the connection logic between two users.
     * Creates a game entry in the database if one does not exist
     * between the given users.
     */
    public class ConnectionModel
    {
        /* Return the id of all games between the two users.
         * Create one if none exist
         */
        public static int[] getGameIds(MySqlConnection dbConnection, int userId1, int userId2)
        {
            dbConnection.Open();

            string commandString = $"select id from game where game.player1Id = {userId1} and game.player2Id = {userId2};";

            using var comm = new MySqlCommand(commandString, dbConnection);
            using var reader = comm.ExecuteReader();

            //get all relevant game ids from the database
            List<int> gameIds = new List<int>();
            while( reader.Read())
            {
                gameIds.Add(reader.GetInt32(0));
            }
            //if no games were found, create a new one
            if( gameIds.Count() == 0)
            {
                Console.WriteLine("No games found, creating a new one");
                gameIds.Add(ConnectionModel.createGame(dbConnection, userId1, userId2));
            }

            dbConnection.Close();
            return gameIds.ToArray();
        }

        /* Create a game entry in the database for the two users.
         * Note that this function does not place any ships, only creates the entry.
         */
        private static int createGame(MySqlConnection dbConnection, int userId1, int userId2)
        {
            dbConnection.Open();

            //passing 0 for the primary key will auto-increment the field
            //this command will insert the new game and return its id
            string commandString = $"insert into game values(0, {userId1}, {userId2}, {userId1});" +
                $"select id from game where id = LAST_INSERT_ID();";

            using var comm = new MySqlCommand(commandString, dbConnection);
            using var reader = comm.ExecuteReader();

            int id = 0;

            if( reader.Read())
            {
                id = reader.GetInt32(0);
            }
            else
            {
                Console.WriteLine("Error: game not found");
            }

            dbConnection.Close();
            return id;
        }
    }
}
