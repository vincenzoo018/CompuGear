using Microsoft.AspNetCore.Mvc;

namespace CompuGear.Controllers
{
    public class SalesController : Controller
    {
        public IActionResult Orders()
        {
            return View();
        }

        public IActionResult Leads()
        {
            return View();
        }

        public IActionResult Reports()
        {
            return View();
        }
    }
}
