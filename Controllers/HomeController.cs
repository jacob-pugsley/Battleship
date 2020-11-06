using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Battleship.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using MySqlConnector;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Google.Apis.Auth;

namespace Battleship.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MySqlConnection connection;

        public HomeController(ILogger<HomeController> logger, MySqlConnection connection)
        {
            _logger = logger;
            this.connection = connection;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Game(int id)
        {
            if ( !BattleshipModel.gameExists(connection, id) )
            {
                Console.WriteLine("database is empty, creating new game");
                BattleshipModel.createRandomBoard(11, connection, id);
            }
            return View();
        }

        [HttpPost]
        public IActionResult GetGame(int userId1, int userId2)
        {
            //get the id of the game between the two users
            //TODO: support multiple ids
            int[] ids = ConnectionModel.getGameIds(connection, userId1, userId2);

            string ret = $"{{\"id\": {ids[0]}}}";

            return Ok(ret);
        }

        [HttpPost]
        public IActionResult Attack(int x, int y, int gameId, int playerId)
        {
            //check if there was a hit against the given player
            bool hit = BattleshipModel.checkHit(connection, gameId, playerId, x, y);

            //if there wasn't a hit, add a miss to the database for the given player
            if( !hit)
            {
                BattleshipModel.addMiss(connection, gameId, playerId, x, y);
            }

            //return the board for the given player and whether we hit or not as JSON
            string ret = $"{{\"board\": {JsonSerializer.Serialize(BattleshipModel.getShips(connection, gameId, playerId))}, \"hit\": {(hit ? 1 : 0)}}}";
            return Ok(ret);
        }

        [HttpPost]
        public IActionResult GetShips(int gameId, int playerId)
        {
            //get the ships owned by the given player
            return Ok(JsonSerializer.Serialize(BattleshipModel.getShips(connection, gameId, playerId)));
        }

        [HttpPost]
        public IActionResult GetMisses(int gameId, int playerId)
        {
            int[][] res = BattleshipModel.getMisses(connection, gameId, playerId);

            return Ok(JsonSerializer.Serialize(res));
        }

        public IActionResult MyTurn(int gameId, int playerId)
        {
            bool isMyTurn = BattleshipModel.myTurn(connection, gameId, playerId);

            string ret = $"{{\"myTurn\": {(isMyTurn ? 1 : 0)}}}";

            return Ok(ret);
        }

        [HttpPost]
        public IActionResult ChangeTurn(int gameId)
        {
            BattleshipModel.changeTurn(connection, gameId);

            return Ok();
        }

        [HttpPost]
        public IActionResult CheckVictory(int gameId)
        {
            bool victory = BattleshipModel.isVictory(connection, gameId);

            return Ok(JsonSerializer.Serialize(victory));
        }

        [HttpPost]
        public IActionResult SetVictory(int gameId, bool value)
        {
            BattleshipModel.setVictory(connection, gameId, value);

            return Ok();
        }

        [HttpPost]
        public IActionResult DeleteGame(int gameId)
        {
            BattleshipModel.deleteGame(connection, gameId);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyToken(string token)
        {
            try
            {
               var res = await GoogleJsonWebSignature.ValidateAsync(token);
            }
            catch(InvalidJwtException)
            {
                return Error();
            }

            return Ok();
        }

        public IActionResult GetUsername(string email)
        {
            //check if a user with given email address exists in the database
            //if not, create them
            //return username so the frontend can display it

            string ret = $"{{\"username\": \"{ConnectionModel.getUsername(email, connection)}\"}}";

            return Ok(ret);

        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
