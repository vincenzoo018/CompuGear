using Microsoft.AspNetCore.Mvc;

namespace CompuGear.Controllers
{
    public class MarketingController : Controller
    {
        public IActionResult Campaigns()
        {
            return View();
        }

        public IActionResult Promotions()
        {
            return View();
        }

        public IActionResult Segments()
        {
            return View();
        }

        public IActionResult Analytics()
        {
            return View();
        }
    }
}
