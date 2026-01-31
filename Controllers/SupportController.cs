using Microsoft.AspNetCore.Mvc;

namespace CompuGear.Controllers
{
    public class SupportController : Controller
    {
        public IActionResult Tickets()
        {
            return View();
        }

        public IActionResult Status()
        {
            return View();
        }

        public IActionResult Knowledge()
        {
            return View();
        }

        public IActionResult Reports()
        {
            return View();
        }
    }
}
