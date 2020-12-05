using System;
using System.Collections.Generic;
using System.Data.Common;
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

        public static void updateUsername(string email, string value, MySqlConnection dbConnection)
        {
            Console.WriteLine(email);
            dbConnection.Open();

            var comm = new MySqlCommand(null, dbConnection);

            comm.CommandText = "update users set username = @value where email = @email";

            MySqlParameter valueParam = new MySqlParameter("@value", MySqlDbType.String, 0);
            valueParam.Value = value;
            MySqlParameter emailParam = new MySqlParameter("@email", MySqlDbType.String, 0);
            emailParam.Value = email;

            comm.Parameters.Add(valueParam);
            comm.Parameters.Add(emailParam);

            comm.ExecuteReader();

            dbConnection.Close();
        }

        public static string[] getUsername(string email, MySqlConnection dbConnection)
        {
            //verify that user with given email exists
            //if not, create new user entry with email as username
            //return username

            dbConnection.Open();

            var comm = new MySqlCommand(null, dbConnection);

            comm.CommandText = "select username, wins, losses from users where email = @email;";

            MySqlParameter emailParam = new MySqlParameter("@email", MySqlDbType.String, 0);
            emailParam.Value = email;

            comm.Parameters.Add(emailParam);

            var reader = comm.ExecuteReader();

            string username = "";
            string wins = "";
            string losses = "";
            if( reader.Read())
            {
                username = reader.GetString(0);
                wins = "" + reader.GetInt32(1);
                losses = "" + reader.GetInt32(2);
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
                wins = "0";
                losses = "0";
            }

            dbConnection.Close();

            string[] ret = new string[] { username, wins, losses };
            return ret;
        }

        public static int getUserIdFromUsername(string username, MySqlConnection dbConnection)
        {
            //return the user id or -1 if user does not exist

            dbConnection.Open();

            var comm = new MySqlCommand(null, dbConnection);

            comm.CommandText = "select playerId from users where username = @username;";

            MySqlParameter usernameParam = new MySqlParameter("@username", MySqlDbType.String, 0);

            usernameParam.Value = username;

            comm.Parameters.Add(usernameParam);

            var reader = comm.ExecuteReader();

            int userId = 0;

            if (reader.Read())
            {
                userId = reader.GetInt32(0);
            }
            else
            {
                userId = -1;
            }

            dbConnection.Close();

            return userId;
        }

        public static string createGuestUser(MySqlConnection dbConnection)
        {
            dbConnection.Open();

            //this query doesn't need to be parameterized because it doesn't take user input
            string commandString = "select ifnull(max(playerId), 0) + 1 into @nextId from users;" +
            "insert into users values(@nextId, concat('Guest#', @nextId), '', 0, 0);" +
            "select username from users where playerId = @nextId;";

            var comm = new MySqlCommand(commandString, dbConnection);

            var reader = comm.ExecuteReader();

            string username = "";

            if( reader.Read())
            {
                username = reader.GetString(0);
            }

            dbConnection.Close();

            return username;
        }

        public static void deleteUser(string username, MySqlConnection dbConnection)
        {
            Console.WriteLine("deleting user with name " + username);
            dbConnection.Open();

            var comm = new MySqlCommand(null, dbConnection);

            comm.CommandText = "delete from users where username = @username;";

            MySqlParameter userParam = new MySqlParameter("@username", MySqlDbType.String);

            userParam.Value = username;

            comm.Parameters.Add(userParam);

            comm.ExecuteReader();

            dbConnection.Close();
        }

    }
}
    