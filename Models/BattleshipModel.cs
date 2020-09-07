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
        Ship[] ships { get; }
        MySqlConnection dbConnection { get; set; }
        public BattleshipModel(int gridSize, MySqlConnection connection)
        {
            dbConnection = connection;
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
            ships = new Ship[5] { carrier, battleship, destroyer, submarine, ptBoat };

            //pick random locations and orientations for the ships
            Random rand = new Random();
            int label = 1;
            foreach( Ship s in ships)
            {
                //pick random locations until the ship can be placed
                //accounting for all of its hitpoints
                int hitPoints = s.HitPoints.GetLength(0);
                while ( true )
                {
                    int x = rand.Next(1, gridSize - (hitPoints - 1) );
                    int y = rand.Next(1, gridSize - (hitPoints - 1) );
                    int orientation = rand.Next(0, 2);

                    
                    int[][] test = new int[hitPoints][];
                    bool valid = true;

                    //check if there are enough empty spaces in the direction of the orientation
                    //to fit the ship, starting at the initial random position
                    for( int i = 0; i < hitPoints; i++)
                    {
                        if( grid[y, x] != 0)
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

                        if( orientation == 0)
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

                    
                    if( valid )
                    {
                        for(int i = 0; i < test.GetLength(0); i++)
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
            for( int i = 0; i < grid.GetLength(0); i++)
            {
                for( int j = 0; j < grid.GetLength(1); j++)
                {
                    Console.Write(grid[j, i] + " ");
                }
                Console.WriteLine();
            }


            //push the ships to the database
            Console.WriteLine("Adding ships to database...");
            pushShipsToDatabase();
            Console.WriteLine("Done.");

        }

        private void pushShipsToDatabase()
        {
            //open the connection
            dbConnection.Open();

            int shipId = 1;
            int hitPointId = 1;
            //insert each ship into the database
            foreach(Ship s in ships)
            {
                string commandString = $"insert into shipNames values({shipId}, \"{s.Name}\") as new " +
                    $"on duplicate key update shipName = new.shipName;";

                //push the ship's hit points to the hitPoint table
                foreach( int[] hp in s.HitPoints)
                {
                    commandString += $"insert into hitPoints values({hitPointId}, {hp[0]}, {hp[1]}, false) as new " +
                        $"on duplicate key update xPos = new.xPos, yPos = new.yPos, hit = new.hit;";

                    //insert the hitpoints into the ship_hitpoints join table
                    commandString += $"insert into ship_hitpoints values({shipId}, {hitPointId}) as new " +
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
        
        /* Check if any ship has been hit or sunk.
         * Sets the sunk property of a hit ship to true as appropriate.
         */
        public static bool isHit(Ship[] ships, int x, int y)
        {
            bool exit = false;
            bool hit = false;
            foreach (Ship s in ships)
            {
                for (int i = 0; i < s.HitPoints.GetLength(0); i++)
                {
                    if (s.HitPoints[i][0] == x && s.HitPoints[i][1] == y)
                    {
                        if (!s.DamageIndex[i])
                        {
                            s.DamageIndex[i] = true;
                            if( isSunk(s))
                            {
                                s.Sunk = true;
                            }
                            hit = true;
                        }
                        exit = true;
                        break;
                    }
                }
                if (exit)
                {
                    break;
                }
            }
            return hit;
        }

        private static bool isSunk(Ship s)
        {
            for( int i = 0; i < s.DamageIndex.GetLength(0); i++)
            {
                if( s.DamageIndex[i] == false)
                {
                    return false;
                }
            }
            return true;
        } 

        /* Return the ship list as a string
         */
        public string asString()
        {
            return JsonSerializer.Serialize(ships);
        }

        public static string getShips(MySqlConnection dbConnection)
        {
            dbConnection.Open();

            string commandString = "select ship_hitpoints.shipId, shipnames.shipName, " +
                "hitpoints.xPos, hitpoints.yPos, hitpoints.hit from shipnames " +
                "join ship_hitPoints on ship_hitpoints.shipId = shipnames.id " +
                "join hitpoints on ship_hitpoints.hitPointId = hitpoints.id;";

            

            using var comm = new MySqlCommand(commandString, dbConnection);

            using var reader = comm.ExecuteReader();

            //TODO: return reader output to the client as a JSON string

            Ship[] tmp = new Ship[5];
            List<int[]>[] hpList = new List<int[]>[5];
            List<bool>[] diList = new List<bool>[5];

            while ( reader.Read())
            {
                //Console.WriteLine(reader.GetValue(1));
                int shipId = (int) reader.GetValue(0);
                
                
                //if the ship doesn't already exist, make a new ship
                if( tmp[shipId - 1] == null)
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
                //tmp[shipId - 1].HitPoints.Append(hp);
                //tmp[shipId - 1].DamageIndex.Append(reader.GetInt32(4) != 0);
                diList[shipId - 1].Add(reader.GetInt32(4) != 0);
            }

            //update the sunk status and hitpoints of all ships
            for(int i = 0; i < tmp.Length; i++)
            {
                tmp[i].HitPoints = hpList[i].ToArray();
                tmp[i].DamageIndex = diList[i].ToArray();

                tmp[i].Sunk = true;
                foreach ( bool hit in tmp[i].DamageIndex)
                {
                    if( !hit)
                    {
                        tmp[i].Sunk = false;
                    }
                }

            }

            return JsonSerializer.Serialize(tmp);
        }
    }

    /* Represents a ship with base location and
     * number of hit points.
     */
    public class Ship
    {   
        /*
        public Ship(string name, int hitPoints)
        {
            Name = name;

            HitPoints = new int[hitPoints][];

            DamageIndex = new bool[hitPoints];
        }
        */
        

        public string Name { get; set; }
        public int[][] HitPoints { get; set; }
        public bool[] DamageIndex { get; set; }

        public bool Sunk { get; set; }

    }
}
