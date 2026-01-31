using Microsoft.AspNetCore.Mvc;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Customer Portal Controller - For Customer-facing pages
    /// Handles customer dashboard, products, orders, support, and promotions
    /// </summary>
    public class CustomerPortalController : Controller
    {
        // Customer Dashboard
        public IActionResult Index()
        {
            ViewData["Title"] = "My Dashboard";
            return View();
        }

        // Browse Products & Promotions
        public IActionResult Products()
        {
            ViewData["Title"] = "Browse Products";
            return View();
        }

        // Current Promotions & Sales
        public IActionResult Promotions()
        {
            ViewData["Title"] = "Promotions & Deals";
            return View();
        }

        // My Orders
        public IActionResult Orders()
        {
            ViewData["Title"] = "My Orders";
            return View();
        }

        // Order Details
        public IActionResult OrderDetails(int id)
        {
            ViewData["Title"] = "Order Details";
            ViewData["OrderId"] = id;
            return View();
        }

        // Support Center with AI Chatbot
        public IActionResult Support()
        {
            ViewData["Title"] = "Support Center";
            return View();
        }

        // Submit Support Ticket
        public IActionResult SubmitTicket()
        {
            ViewData["Title"] = "Submit Support Ticket";
            return View();
        }

        // My Support Tickets
        public IActionResult MyTickets()
        {
            ViewData["Title"] = "My Support Tickets";
            return View();
        }

        // Ticket Details
        public IActionResult TicketDetails(int id)
        {
            ViewData["Title"] = "Ticket Details";
            ViewData["TicketId"] = id;
            return View();
        }

        // Customer Profile
        public IActionResult Profile()
        {
            ViewData["Title"] = "My Profile";
            return View();
        }

        // Shopping Cart
        public IActionResult Cart()
        {
            ViewData["Title"] = "Shopping Cart";
            return View();
        }

        // Checkout
        public IActionResult Checkout()
        {
            ViewData["Title"] = "Checkout";
            return View();
        }

        // Wishlist
        public IActionResult Wishlist()
        {
            ViewData["Title"] = "My Wishlist";
            return View();
        }
    }
}
