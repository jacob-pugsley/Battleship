using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Battleship.Models
{
    public class BattleshipModel
    {
        Ship[] ships { get; }
        public BattleshipModel(int gridSize)
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
            Ship carrier = new Ship("Carrier", 5);
            Ship battleship = new Ship("Battleship", 4);
            Ship destroyer = new Ship("Destroyer", 3);
            Ship submarine = new Ship("Submarine", 3);
            Ship ptBoat = new Ship("Patrol Boat", 2);

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

                    
                    int[,] test = new int[hitPoints, 2];
                    bool valid = true;

                    //check if there are enough empty spaces in the direction of the orientation
                    //to fit the ship, starting at the initial random position
                    for( int i = 0; i < hitPoints; i++)
                    {
                        if( grid[x, y] != 0)
                        {
                            valid = false;
                            break;
                        }
                        else
                        {
                            test[i, 0] = x;
                            test[i, 1] = y;
                        }

                        if( orientation == 0)
                        {
                            //vertical
                            y++;
                        }
                        else
                        {
                            //horizontal
                            x++;
                        }
                    }

                    
                    if( valid )
                    {
                        for(int i = 0; i < test.GetLength(0); i++)
                        {
                            grid[test[i, 0], test[i, 1]] = label;
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
                    Console.Write(grid[i, j] + " ");
                }
                Console.WriteLine();
            }


        }
    }

    /* Represents a ship with base location and
     * number of hit points.
     */
    public class Ship
    {
        
        public Ship(string name, int hitPoints)
        {
            Name = name;

            //list of hit point grid locations
            HitPoints = new int[hitPoints, 2];
        }

        public string Name { get; }
        public int[,] HitPoints { get; set; }

    }
}
