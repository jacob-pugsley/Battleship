using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Threading.Tasks;

namespace Battleship.Models
{
    public class BattleshipModel
    {
        public static void createRandomBoard(int gridSize, MySqlConnection dbConnection, int gameId)
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
            BattleshipModel.pushShipsToDatabase(dbConnection, ships, gameId);
            Console.WriteLine("Done.");

        }

        private static int[] getMaxIds(MySqlConnection dbConnection)
        {
            int[] ret = new int[2];

            dbConnection.Open();

            string commandString = "select MAX(shipId), MAX(hitPointId) from ship_hitpoints";

            using var comm = new MySqlCommand(commandString, dbConnection);
            using var reader = comm.ExecuteReader();

            if(reader.Read())
            {
                ret[0] = reader.GetInt32(0);
                ret[1] = reader.GetInt32(1);
            }
            dbConnection.Close();
            return ret;
        }

        private static void pushShipsToDatabase(MySqlConnection dbConnection, Ship[] ships, int gameId)
        {
            int[] ids = BattleshipModel.getMaxIds(dbConnection);
            //open the connection
            dbConnection.Open();

            int shipId = ids[0];
            int hitPointId = ids[1];
            
            //insert each ship into the database
            foreach (Ship s in ships)
            {
                string commandString = $"insert into shipNames values({shipId}, \"{s.Name}\") as new " +
                    $"on duplicate key update shipName = new.shipName;";

                //push the ship's hit points to the hitPoint table
                foreach (int[] hp in s.HitPoints)
                {
                    commandString += $"insert into hitPoints values({hitPointId}, {gameId}, {hp[0]}, {hp[1]}, false) as new " +
                        $"on duplicate key update xPos = new.xPos, yPos = new.yPos, hit = new.hit;";

                    //insert the hitpoints into the ship_hitpoints join table
                    commandString += $"insert into ship_hitpoints values({gameId}, {shipId}, {hitPointId}) as new " +
                        $"on duplicate key update hitPointId = new.hitPointId;";

                    hitPointId++;


                }
                shipId++;

                //perform all queries with one string to avoid "database connection in use" errors
                using var comm = new MySqlCommand(commandString, dbConnection);

                using var reader = comm.ExecuteReader();
            }
            //close the database connection when finished
            dbConnection.Close();
        }

        public static Ship[] getShips(MySqlConnection dbConnection, int gameId)
        {
            //open the connection
            dbConnection.Open();

            string commandString = "select ship_hitpoints.shipId, shipnames.shipName, " +
                "hitpoints.xPos, hitpoints.yPos, hitpoints.hit from shipnames " +
                "join ship_hitPoints on ship_hitpoints.shipId = shipnames.id " +
                "join hitpoints on ship_hitpoints.hitPointId = hitpoints.id " +
                $"where ship_hitpoinnts.gameId = {gameId};";



            using var comm = new MySqlCommand(commandString, dbConnection);

            using var reader = comm.ExecuteReader();

            Ship[] tmp = new Ship[5];
            List<int[]>[] hpList = new List<int[]>[5];
            List<bool>[] diList = new List<bool>[5];

            while (reader.Read())
            {
                int shipId = reader.GetInt32(0);


                //if the ship doesn't already exist, make a new ship
                if (tmp[shipId - 1] == null)
                {
                    tmp[shipId - 1] = new Ship();
                    hpList[shipId - 1] = new List<int[]>();
                    diList[shipId - 1] = new List<bool>();
                }

                //then update all fields
                tmp[shipId - 1].Name = reader.GetString(1);
                //append to the end of the lists
                int[] hp = new int[2] { reader.GetInt32(2), reader.GetInt32(3) };
                hpList[shipId - 1].Add(hp);
                diList[shipId - 1].Add(reader.GetInt32(4) != 0);
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
        public static bool checkHit(MySqlConnection dbConnection, int gameId, int x, int y)
        {


            //first check if there was a hit
            Ship[] ships = BattleshipModel.getShips(dbConnection, gameId);

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
                string commandString = $"select id into @keyvar from hitpoints where gameId = {gameId} and xPos = {hitPoint[0]} and yPos = {hitPoint[1]}; " +
                    $"update hitpoints set hit = 1 where id = @keyvar";

                using var comm = new MySqlCommand(commandString, dbConnection);
                using var reader = comm.ExecuteReader();
            }

            dbConnection.Close();
            return hit;

        }
    }

    /* Represents a ship with base location and
     * number of hit points.
     */
    public class Ship
    {   
        public string Name { get; set; }
        public int[][] HitPoints { get; set; }
        public bool[] DamageIndex { get; set; }

        public bool Sunk { get; set; }

    }
}
