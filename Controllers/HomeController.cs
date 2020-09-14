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
            //BattleshipModel bm;
            if ( BattleshipModel.getShips(connection, id) == null)
            {
                Console.WriteLine("database is empty, creating new game");
                //bm = new BattleshipModel(11, connection);
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
        public IActionResult Attack(int x, int y, int gameId)
        {

            bool hit = BattleshipModel.checkHit(connection, gameId, x, y);
            //return the board and whether we hit or not as JSON
            string ret = $"{{\"board\": {JsonSerializer.Serialize(BattleshipModel.getShips(connection, gameId))}, \"hit\": {(hit ? 1 : 0)}}}";
            return Ok(ret);
        }

        [HttpPost]
        public IActionResult GetShips(int gameId)
        {
            return Ok(JsonSerializer.Serialize(BattleshipModel.getShips(connection, gameId)));
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
