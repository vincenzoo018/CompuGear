using Microsoft.AspNetCore.Mvc;

namespace CompuGear.Controllers
{
    public class UsersController : Controller
    {
        public IActionResult Accounts()
        {
            return View();
        }

        public IActionResult Roles()
        {
            return View();
        }

        public IActionResult Activity()
        {
            return View();
        }
    }
}
