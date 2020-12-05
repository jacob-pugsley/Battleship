using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Security.Policy;
using System.Text.Json;
using System.Threading.Tasks;

namespace Battleship.Models
{
    public class BattleshipModel
    {  
        public static bool gameExists( MySqlConnection dbConnection, int gameId)
        {
            //return true if there is a game with the given id, false otherwise
            dbConnection.Open();

            //string commandString = $"select * from ship_hitpoints where gameId = {gameId};";
            var comm = new MySqlCommand(null, dbConnection);

            comm.CommandText = "select * from ship_hitpoints where gameId = @gameId;";
            MySqlParameter gameIdParam = new MySqlParameter("@gameId", MySqlDbType.Int32);
            gameIdParam.Value = gameId;
            comm.Parameters.Add(gameIdParam);

            var reader = comm.ExecuteReader();

            bool exists = false;
            if(reader.Read())
            {
                exists = true;
            }

            dbConnection.Close();

            return exists;
        }
        public static void createRandomBoard(int gridSize, MySqlConnection dbConnection, int gameId)
        {
            //get both player ids in the game
            int player1Id = 0;
            int player2Id = 0;
            
            dbConnection.Open();

            //string commandString = $"select player1Id, player2Id from game where gameId = {gameId};";

            var comm = new MySqlCommand(null, dbConnection);
            comm.CommandText = "select player1Id, player2Id from game where gameId = @gameId;";
            MySqlParameter gameIdParam = new MySqlParameter("@gameId", MySqlDbType.Int32);
            gameIdParam.Value = gameId;
            comm.Parameters.Add(gameIdParam);

            var reader = comm.ExecuteReader();

            if( reader.Read())
            {
                player1Id = reader.GetInt32(0);
                player2Id = reader.GetInt32(1);
            }

            dbConnection.Close();

            //create ships for each player
            createRandomBoard(gridSize, dbConnection, gameId, player1Id);
            createRandomBoard(gridSize, dbConnection, gameId, player2Id);
        }
        private static void createRandomBoard(int gridSize, MySqlConnection dbConnection, int gameId, int playerId)
        {
            //binary array representing squares that have been taken
            int[,] grid = new int[gridSize, gridSize];

            //create the standard ships
            /*
            1   Carrier     5
            2   Battleship  4
            3   Destroyer   3
            4   Submarine   3
            5   Patrol Boat 2
            */
            Ship carrier = new Ship();
            carrier.Name = "Carrier";
            carrier.HitPoints = new int[5][];
            carrier.DamageIndex = new bool[5];
            Ship battleship = new Ship();
            battleship.Name = "Battleship";
            battleship.HitPoints = new int[4][];
            battleship.DamageIndex = new bool[4];
            Ship destroyer = new Ship();
            destroyer.Name = "Destroyer";
            destroyer.HitPoints = new int[3][];
            destroyer.DamageIndex = new bool[3];
            Ship submarine = new Ship();
            submarine.Name = "Submarine";
            submarine.HitPoints = new int[3][];
            submarine.DamageIndex = new bool[3];
            Ship ptBoat = new Ship();
            ptBoat.Name = "Patrol Boat";
            ptBoat.HitPoints = new int[2][];
            ptBoat.DamageIndex = new bool[2];
            Ship[] ships = new Ship[5] { carrier, battleship, destroyer, submarine, ptBoat };

            //pick random locations and orientations for the ships
            Random rand = new Random();
            int label = 1;
            foreach (Ship s in ships)
            {
                //pick random locations until the ship can be placed
                //accounting for all of its hitpoints
                int hitPoints = s.HitPoints.GetLength(0);
                while (true)
                {
                    int x = rand.Next(1, gridSize - (hitPoints - 1));
                    int y = rand.Next(1, gridSize - (hitPoints - 1));
                    int orientation = rand.Next(0, 2);


                    int[][] test = new int[hitPoints][];
                    bool valid = true;

                    //check if there are enough empty spaces in the direction of the orientation
                    //to fit the ship, starting at the initial random position
                    for (int i = 0; i < hitPoints; i++)
                    {
                        if (grid[y, x] != 0)
                        {
                            valid = false;
                            break;
                        }
                        else
                        {
                            int[] tmp = new int[2];
                            tmp[0] = x;
                            tmp[1] = y;

                            test[i] = tmp;
                        }

                        if (orientation == 0)
                        {
                            //vertical
                            x++;
                        }
                        else
                        {
                            //horizontal
                            y++;
                        }
                    }


                    if (valid)
                    {
                        for (int i = 0; i < test.GetLength(0); i++)
                        {
                            grid[test[i][1], test[i][0]] = label;
                        }
                        label++;
                        s.HitPoints = test;
                        break;
                    }
                }
            }

            //print the grid for testing
            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    Console.Write(grid[j, i] + " ");
                }
                Console.WriteLine();
            }


            //push the ships to the database
            Console.WriteLine("Adding ships to database...");
            BattleshipModel.pushShipsToDatabase(dbConnection, ships, gameId, playerId);
            Console.WriteLine("Done.");

        }

        private static int[] getMaxIds(MySqlConnection dbConnection)
        {
            int[] ret = new int[2];

            dbConnection.Open();

            //this query doesn't need to be parameterized because it doesn't take user input
            string commandString = "select MAX(shipId), MAX(hitPointId) from ship_hitpoints";

            using var comm = new MySqlCommand(commandString, dbConnection);
            using var reader = comm.ExecuteReader();

            if(reader.Read())
            {
                try
                {
                    ret[0] = reader.GetInt32(0) + 1;
                }catch( InvalidCastException)
                {
                    Console.WriteLine("rdr 0: " + reader.GetValue(0));
                    ret[0] = 1;
                }
                try
                {
                    ret[1] = reader.GetInt32(1) + 1;
                }
                catch (InvalidCastException)
                {
                    Console.WriteLine("rdr 1: " + reader.GetValue(1));
                    ret[1] = 1;
                }
            }
            dbConnection.Close();
            return ret;
        }

        private static void pushShipsToDatabase(MySqlConnection dbConnection, Ship[] ships, int gameId, int playerId)
        {
            int[] ids = BattleshipModel.getMaxIds(dbConnection);
            Console.WriteLine(gameId);
            //open the connection
            dbConnection.Open();

            int shipId = ids[0];
            int hitPointId = ids[1];

            //gameId and playerId need to be sanitized because they are taken from user input in the calling function
            MySqlParameter gameIdParam = new MySqlParameter("@gameId", MySqlDbType.Int32);
            gameIdParam.Value = gameId;

            MySqlParameter playerIdParam = new MySqlParameter("@playerId", MySqlDbType.Int32);
            playerIdParam.Value = playerId;

            string commandString = "";
            //insert each ship into the database
            foreach (Ship s in ships)
            {
                commandString += $"insert into shipNames values({shipId}, \"{s.Name}\", @gameId) as new " +
                    $"on duplicate key update shipName = new.shipName;\n";

                //push the ship's hit points to the hitPoint table
                foreach (int[] hp in s.HitPoints)
                {
                    //insert each hitpoint with its player id so that there can be a seperate board for each player in a game
                    commandString += $"insert into hitPoints values({hitPointId}, @gameId, @playerId, {hp[0]}, {hp[1]}, false) as new " +
                        $"on duplicate key update xPos = new.xPos, yPos = new.yPos, hit = new.hit;\n";

                    //insert the hitpoints into the ship_hitpoints join table
                    commandString += $"insert into ship_hitpoints values(@gameId, {shipId}, {hitPointId}) as new " +
                        $"on duplicate key update hitPointId = new.hitPointId;\n";

                    hitPointId++;


                }
                shipId++;
            }
            //close the database connection when finished
            var comm = new MySqlCommand(null, dbConnection);
            comm.CommandText = commandString;
            comm.Parameters.Add(gameIdParam);
            comm.Parameters.Add(playerIdParam);

            var reader = comm.ExecuteReader();
            dbConnection.Close();
        }

        private static int getIndexOfShipWithId(Ship[] ships, int shipId)
        {
            for (int i = 0; i < ships.GetLength(0); i++)
            {
                if( ships[i] == null)
                {
                    break;
                }
                else if (ships[i].Id == shipId)
                {
                    return i;
                }
            }
            return -1;
        }

        private static int getFirstEmptyIndex(object[] arr)
        {
            //return the last empty (null) index
            //return -1 if there are no empty indexes

            for( int i = 0; i < arr.GetLength(0); i++)
            {
                if( arr[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }

        public static Ship[] getShips(MySqlConnection dbConnection, int gameId, int playerId)
        {
            //open the connection
            dbConnection.Open();

            var comm = new MySqlCommand(null, dbConnection);

            comm.CommandText = "select ship_hitpoints.shipId, shipnames.shipName, " +
                "hitpoints.xPos, hitpoints.yPos, hitpoints.hit from shipnames " +
                "join ship_hitPoints on ship_hitpoints.shipId = shipnames.id " +
                $"join hitpoints on ship_hitpoints.hitPointId = hitpoints.id and hitpoints.playerId = @playerId " +
                $"where ship_hitpoints.gameId = @gameId;";

            MySqlParameter gameIdParam = new MySqlParameter("@gameId", MySqlDbType.Int32);
            gameIdParam.Value = gameId;

            MySqlParameter playerIdParam = new MySqlParameter("@playerId", MySqlDbType.Int32);
            playerIdParam.Value = playerId;

            comm.Parameters.Add(gameIdParam);
            comm.Parameters.Add(playerIdParam);

            var reader = comm.ExecuteReader();

            Ship[] tmp = new Ship[5];
            //int indexInTmp = 0;
            List<int[]>[] hpList = new List<int[]>[5];
            List<bool>[] diList = new List<bool>[5];

            while (reader.Read())
            {
                int shipId = reader.GetInt32(0);

                int indexInTmp = getIndexOfShipWithId(tmp, shipId);

                //if the ship doesn't already exist, make a new ship
                if ( indexInTmp == -1 )
                {
                    indexInTmp = getFirstEmptyIndex(tmp);

                    if( indexInTmp == -1)
                    {
                        throw new Exception("Error: tmp array is full");
                    }

                    tmp[indexInTmp] = new Ship();
                    hpList[indexInTmp] = new List<int[]>();
                    diList[indexInTmp] = new List<bool>();
                }

                //then update all fields
                tmp[indexInTmp].Id = shipId;
                tmp[indexInTmp].Name = reader.GetString(1);
                //append to the end of the lists
                int[] hp = new int[2] { reader.GetInt32(2), reader.GetInt32(3) };
                hpList[indexInTmp].Add(hp);
                diList[indexInTmp].Add(reader.GetInt32(4) != 0);
            }

            //update the sunk status and hitpoints of all ships
            //if the first ship is null, the list is empty
            if (tmp[0] == null)
            {
                dbConnection.Close();
                return null;
            }
            for (int i = 0; i < tmp.Length; i++)
            {
                if (tmp[i] == null)
                {
                    break;
                }
                tmp[i].HitPoints = hpList[i].ToArray();
                tmp[i].DamageIndex = diList[i].ToArray();

                tmp[i].Sunk = true;
                foreach (bool hit in tmp[i].DamageIndex)
                {
                    if (!hit)
                    {
                        tmp[i].Sunk = false;
                    }
                }

            }

            dbConnection.Close();
            return tmp;
        }



        /* Update the database if the given coordinate was a hit.
        * Return true if there was a hit, false otherwise.
        */
        public static bool checkHit(MySqlConnection dbConnection, int gameId, int playerId, int x, int y)
        {


            //first check if there was a hit
            Ship[] ships = BattleshipModel.getShips(dbConnection, gameId, playerId);

            //open the connection
            dbConnection.Open();

            bool hit = false;
            int[] hitPoint = new int[2];

            foreach (Ship s in ships)
            {
                foreach (int[] hp in s.HitPoints)
                {
                    if (hp[0] == x && hp[1] == y)
                    {
                        hitPoint[0] = hp[0];
                        hitPoint[1] = hp[1];
                        hit = true;
                        break;
                    }
                }
                if (hit)
                {
                    break;
                }
            }

            //update database
            if (hit)
            {
                //perform a safe update by first getting the hitpoint id to be changed

                var comm = new MySqlCommand(null, dbConnection);
                comm.CommandText = $"select id into @keyvar from hitpoints where gameId = @gameId and playerId = @playerId " +
                    $"and xPos = {hitPoint[0]} and yPos = {hitPoint[1]}; " +
                    $"update hitpoints set hit = 1 where id = @keyvar";

                MySqlParameter gameIdParam = new MySqlParameter("@gameId", MySqlDbType.Int32);
                gameIdParam.Value = gameId;

                MySqlParameter playerIdParam = new MySqlParameter("@playerId", MySqlDbType.Int32);
                playerIdParam.Value = playerId;

                comm.Parameters.Add(gameIdParam);
                comm.Parameters.Add(playerIdParam);

                using var reader = comm.ExecuteReader();
            }

            dbConnection.Close();
            return hit;

        }

        public static void addMiss(MySqlConnection dbConnection, int gameId, int playerId, int x, int y)
        {
            dbConnection.Open();

            var comm = new MySqlCommand(null, dbConnection);

            comm.CommandText = $"insert into misses values(@gameId, @playerId, {x}, {y});";

            MySqlParameter gameIdParam = new MySqlParameter("@gameId", MySqlDbType.Int32);
            gameIdParam.Value = gameId;

            MySqlParameter playerIdParam = new MySqlParameter("@playerId", MySqlDbType.Int32);
            playerIdParam.Value = playerId;

            comm.Parameters.Add(gameIdParam);
            comm.Parameters.Add(playerIdParam);

            

            comm.ExecuteReader();

            dbConnection.Close();
        }
        
        public static int[][] getMisses(MySqlConnection dbConnection, int gameId, int playerId)
        {
            dbConnection.Open();

            var comm = new MySqlCommand(null, dbConnection);

            comm.CommandText = $"select xPos, yPos from misses where gameId = @gameId and playerId = @playerId;";

            MySqlParameter gameIdParam = new MySqlParameter("@gameId", MySqlDbType.Int32);
            gameIdParam.Value = gameId;

            MySqlParameter playerIdParam = new MySqlParameter("@playerId", MySqlDbType.Int32);
            playerIdParam.Value = playerId;

            comm.Parameters.Add(gameIdParam);
            comm.Parameters.Add(playerIdParam);

            var reader = comm.ExecuteReader();

            List<int[]> res = new List<int[]>();

            while( reader.Read())
            {
                res.Add(new int[2] { reader.GetInt32(0), reader.GetInt32(1) });
            }

            dbConnection.Close();
            return res.ToArray();
        }

        public static bool myTurn(MySqlConnection dbConnection, int gameId, int playerId)
        {
            dbConnection.Open();
            
            var comm = new MySqlCommand(null, dbConnection);
            comm.CommandText = $"select currentPlayer from game where gameId = @gameId;";
            MySqlParameter gameIdParam = new MySqlParameter("@gameId", MySqlDbType.Int32);
            gameIdParam.Value = gameId;
            comm.Parameters.Add(gameIdParam);

            var reader = comm.ExecuteReader();

            if( reader.Read())
            {
                bool ret = reader.GetInt32(0) == playerId;
                dbConnection.Close();
                return ret;
            }
            else{
                Console.WriteLine("Error: no game found with given gameId");
                dbConnection.Close();
                return false;
            }
        }

        public static void changeTurn(MySqlConnection dbConnection, int gameId)
        {
            dbConnection.Open();

            var comm = new MySqlCommand(null, dbConnection);
            comm.CommandText = $"select currentPlayer into @cp from game where gameId = @gameId;" +
                $"select player1Id into @p1 from game where gameId = @gameId;" +
                $"select player2Id into @p2 from game where gameId = @gameId;" +
                $"update game set currentPlayer = if (@cp = @p1, @p2, @p1 ) where gameId = @gameId;";

            MySqlParameter gameIdParam = new MySqlParameter("@gameId", MySqlDbType.Int32);
            gameIdParam.Value = gameId;
            comm.Parameters.Add(gameIdParam);

            comm.ExecuteReader();

            dbConnection.Close();
        }

        public static bool isVictory(MySqlConnection dbConnection, int gameId)
        {
            dbConnection.Open();
            var comm = new MySqlCommand(null, dbConnection);

            comm.CommandText = $"select victory from game where gameId =  @gameId;";
            MySqlParameter gameIdParam = new MySqlParameter("@gameId", MySqlDbType.Int32);
            gameIdParam.Value = gameId;
            comm.Parameters.Add(gameIdParam);

            var reader = comm.ExecuteReader();

            bool result = false;

            if( reader.Read())
            {
                result = reader.GetBoolean(0);
            }

            dbConnection.Close();

            return result;
        }

        public static void setVictory(MySqlConnection dbConnection, int gameId, bool victory)
        {
            dbConnection.Open();
            var comm = new MySqlCommand(null, dbConnection);
            comm.CommandText = $"update game set victory = {victory} where gameId = @gameId;";
            MySqlParameter gameIdParam = new MySqlParameter("@gameId", MySqlDbType.Int32);
            gameIdParam.Value = gameId;
            comm.Parameters.Add(gameIdParam);

            

            comm.ExecuteReader();

            dbConnection.Close();
        }

        public static void deleteGame(MySqlConnection dbConnection, int gameId)
        {
            dbConnection.Open();

            //only delete games that have been won so that concurrent uses of this function do not cause problems
            var comm = new MySqlCommand(null, dbConnection);
            comm.CommandText = $"delete from game where gameId = @gameId and victory = true;";
            MySqlParameter gameIdParam = new MySqlParameter("@gameId", MySqlDbType.Int32);
            gameIdParam.Value = gameId;
            comm.Parameters.Add(gameIdParam);

            comm.ExecuteReader();

            dbConnection.Close();
        }

        public static void updateStats(MySqlConnection dbConnection, int playerId, bool won)
        {
            dbConnection.Open();

            var comm = new MySqlCommand(null, dbConnection);

            comm.CommandText = won ? $"update users set wins = wins + 1 where playerId = @playerId;" :
                $"update users set losses = losses + 1 where playerId = @playerId;";
            MySqlParameter playerIdParam = new MySqlParameter("@playerId", MySqlDbType.Int32);
            playerIdParam.Value = playerId;
            comm.Parameters.Add(playerIdParam);

            comm.ExecuteReader();

            dbConnection.Close();
        }
    }

    /* Represents a ship with base location and
     * number of hit points.
     */
    public class Ship
    {   
        public int Id { get; set; }
        public string Name { get; set; }
        public int[][] HitPoints { get; set; }
        public bool[] DamageIndex { get; set; }

        public bool Sunk { get; set; }

    }
}
