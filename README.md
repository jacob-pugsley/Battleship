# Battleship

An online multiplayer version of Battleship built with ASP.NET Core MVC.

In Battleship, the player must guess the locations of enemy ships in order to sink them all and win. Each turn consists of picking a grid square to fire on. 
After each attack, the player is notified of the results- whether a ship was hit, sunk, or if the shot missed -and can use that information to deduce the position
of the enemy ships.

The project will have online multiplayer functionality including user profiles that display player statistics and match hosting between arbitrary users or a computer player. 

See the develop branch for the latest updates!

# Features in Version 2
<ul>
  <li>Moved game data storage from browser session to an external database</li>
  <li>Added a second player with different ships</li>
  <li>Added a second grid display to the main screen</li>
  <li>Players now compete with each other and can win or lose</li>
</ul>

# Features in Version 1

<ul>
  <li>Basic game of Battleship without an opponent or ship placement</li>
  <li>Each new game board is stored in the browser session</li>
</ul>
