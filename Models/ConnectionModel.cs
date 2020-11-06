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

            //we don't know which player id is considered player 1 in the database, so we have to account for both
            //possibilities

            //use a prepared statement for derived user input
            using var comm = new MySqlCommand(null, dbConnection);
            comm.CommandText = "select gameId from game where (game.player1Id = @user1 and game.player2Id = @user2) " +
                $"or (game.player1Id = @user2 and game.player2Id = @user1);";

            MySqlParameter user1 = new MySqlParameter("@user1", MySqlDbType.Int32, 0);
            MySqlParameter user2 = new MySqlParameter("@user2", MySqlDbType.Int32, 0);

            user1.Value = userId1;
            user2.Value = userId2;

            comm.Parameters.Add(user1);
            comm.Parameters.Add(user2);
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
                dbConnection.Close();
                gameIds.Add(ConnectionModel.createGame(dbConnection, userId1, userId2));
            }
            else 
            {
                dbConnection.Close();
            }

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

            //use prepared statement for derived user input
            using var comm = new MySqlCommand(null, dbConnection);

            comm.CommandText = "select ifnull(max(gameId), 0) + 1 into @nextId from game;" +
                $"insert into game values(@nextId, @user1, @user2, @user1, false);" +
                $"select @nextId;";

            MySqlParameter user1 = new MySqlParameter("@user1", MySqlDbType.Int32, 0);
            MySqlParameter user2 = new MySqlParameter("@user2", MySqlDbType.Int32, 0);

            user1.Value = userId1;
            user2.Value = userId2;

            comm.Parameters.Add(user1);
            comm.Parameters.Add(user2);
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

        public static string getUsername(string email, MySqlConnection dbConnection)
        {
                //verify that user with given email exists
                //if not, create new user entry with email as username
                //return username

                dbConnection.Open();

                var comm = new MySqlCommand(null, dbConnection);

                comm.CommandText = "select username from users where email = @email;";

                MySqlParameter emailParam = new MySqlParameter("@email", MySqlDbType.String, 0);
                emailParam.Value = email;

                comm.Parameters.Add(emailParam);

                var reader = comm.ExecuteReader();

                string username = "";
                if( reader.Read())
                {
                    username = reader.GetString(0);
                }
                else
                {
                    //no username found
                    dbConnection.Close();
                    dbConnection.Open();

                    //create a new user entry with a new user id
                    
                    var comm2 = new MySqlCommand(null, dbConnection);


                    comm2.CommandText = "select ifnull(max(playerId), 0) + 1 into @nextId from users;" +
                        "insert into users values(@nextId, @email, @email, 0, 0);";

                    comm2.Parameters.Add(emailParam);

                    comm2.ExecuteReader();

                    username = (string) emailParam.Value;
                }

                dbConnection.Close();

                return username;
        }
    }
}
    