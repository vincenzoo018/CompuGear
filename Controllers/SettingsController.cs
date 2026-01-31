using Microsoft.AspNetCore.Mvc;

namespace CompuGear.Controllers
{
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
