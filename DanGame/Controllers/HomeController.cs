using DanGame.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DanGame.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
		private DanGameContext _context;


		public HomeController( DanGameContext dbContext)
		{

			_context = dbContext;

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
			///
			var query = from app in _context.Apps
						where app.AppDetail.AppType == "game"
						select new
						{
							appId = app.AppId,
							appName = app.AppName,
							headerImage = app.AppDetail.HeaderImage,
							appDesc = app.AppDetail.ShortDescription,
							releaseDate = app.AppDetail.ReleaseDate,
							downloaded = app.AppDetail.Downloaded,
							price = app.AppDetail.Price,
							tags = app.Tags
						};
			return View(new { apps = query.ToArray(), subscriptionPlans =  _context.SubscriptionPlans.ToList()
		});
        }

        public IActionResult ranking()
        {

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
