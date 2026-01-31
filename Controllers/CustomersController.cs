using Microsoft.AspNetCore.Mvc;

namespace CompuGear.Controllers
{
    public class CustomersController : Controller
    {
        public IActionResult List()
        {
            return View();
        }

        public IActionResult Profile(int? id)
        {
            return View();
        }

        public IActionResult History(int? id)
        {
            return View();
        }
    }
}
