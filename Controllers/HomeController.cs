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

namespace Battleship.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            BattleshipModel bm = new BattleshipModel(11);
            //HttpContext.Session.SetString("test", "test value");
            HttpContext.Session.SetString("shipJson", bm.asString());
            return View(bm);
        }

        [HttpPost]
        public IActionResult Attack(int x, int y)
        {
            Ship[] ships = JsonSerializer.Deserialize<Ship[]>(HttpContext.Session.GetString("shipJson"));

            bool exit = false;
            bool hit = false;
            foreach( Ship s in ships)
            {
                for( int i = 0; i < s.HitPoints.GetLength(0); i++)
                {
                    if( s.HitPoints[i][0] == x && s.HitPoints[i][1] == y )
                    {
                        if (!s.DamageIndex[i])
                        {
                            s.DamageIndex[i] = true;
                            hit = true;
                        }
                        HttpContext.Session.SetString("shipJson", JsonSerializer.Serialize(ships));
                        exit = true;
                        break;
                    }
                }
                if( exit )
                {
                    break;
                }
            }

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
