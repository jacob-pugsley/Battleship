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
            BattleshipModel bm = new BattleshipModel(11, connection);
            //HttpContext.Session.SetString("test", "test value");
            HttpContext.Session.SetString("shipJson", bm.asString());
            return View(bm);
        }

        [HttpPost]
        public IActionResult Attack(int x, int y)
        {
            string tmp = HttpContext.Session.GetString("shipJson");
            Ship[] ships;
            if (tmp != null)
            {
                ships = JsonSerializer.Deserialize<Ship[]>(tmp);
            }
            else
            {
                Console.WriteLine("Error: session variable shipJson is null");
                return Error();
            }

            bool hit = BattleshipModel.isHit(ships, x, y);

            HttpContext.Session.SetString("shipJson", JsonSerializer.Serialize(ships));

            //return the board and whether we hit or not as JSON
            string ret = $"{{\"board\": {HttpContext.Session.GetString("shipJson")}, \"hit\": {(hit ? 1 : 0)}}}";
            Console.WriteLine(ret);
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
