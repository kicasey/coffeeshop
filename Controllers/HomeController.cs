using Microsoft.AspNetCore.Mvc;
using CoffeeShopSimulation.Models;

namespace CoffeeShopSimulation.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // The drag-and-drop ingredients are hardcoded in the view,
            // so we don't need to pass a model. The view can work without one.
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new Models.ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}


