using DanGame.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DanGame.Controllers
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
            //// �ˬd�O�_���w�n�J��session
            //if (HttpContext.Session.GetString("UserId") != null)
            //{
            //    // �p�G�w�n�J�A�ɦVUserController��UserIndex����
            //    return RedirectToAction("UserIndex", "User");
            //}

            //// �_�h���Index����
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult RedirectToUserIndex()
        {
            return RedirectToAction("UserIndex", "User");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
