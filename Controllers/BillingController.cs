using Microsoft.AspNetCore.Mvc;

namespace CompuGear.Controllers
{
    public class BillingController : Controller
    {
        public IActionResult Invoices()
        {
            return View();
        }

        public IActionResult Payments()
        {
            return View();
        }

        public IActionResult Summary()
        {
            return View();
        }

        public IActionResult Records()
        {
            return View();
        }
    }
}
