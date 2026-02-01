using Microsoft.AspNetCore.Mvc;

namespace CompuGear.Controllers
{
    public class InventoryController : Controller
    {
        public IActionResult Products()
        {
            return View();
        }

        public IActionResult Categories()
        {
            return View();
        }

        public IActionResult Stock()
        {
            return View();
        }

        public IActionResult Alerts()
        {
            return View();
        }

        public IActionResult Reports()
        {
            return View();
        }
    }
}
