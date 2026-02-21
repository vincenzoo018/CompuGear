using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Billing/Accounting Staff Portal Controller
    /// RoleId: 6 - Accounting & Billing Staff
    /// Access: Invoices, payments, customer accounts/balances, financial reports
    /// </summary>
    public class BillingStaffController : Controller
    {
        private readonly CompuGearDbContext _context;

        public BillingStaffController(CompuGearDbContext context)
        {
            _context = context;
        }

        // Role-based authorization check
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            
            var roleId = HttpContext.Session.GetInt32("RoleId");
            
            // Allow access for: Super Admin (1), Company Admin (2), Accounting & Billing Staff (6)
            if (roleId == null || (roleId != 1 && roleId != 2 && roleId != 6))
            {
                context.Result = RedirectToAction("Login", "Auth");
                return;
            }

            if (roleId == 6)
            {
                var companyId = HttpContext.Session.GetInt32("CompanyId");
                if (!companyId.HasValue || !HasModuleAccess(companyId.Value, roleId.Value, "BILLING"))
                {
                    context.Result = RedirectToAction("Index", "Home");
                }
            }
        }

        private bool HasModuleAccess(int companyId, int roleId, string moduleCode)
        {
            var companyHasModule = _context.CompanyModuleAccess
                .Include(a => a.Module)
                .Any(a => a.CompanyId == companyId && a.IsEnabled && a.Module.ModuleCode == moduleCode);

            if (!companyHasModule)
            {
                return false;
            }

            var roleAccessRows = _context.RoleModuleAccess
                .Where(r => r.CompanyId == companyId && r.RoleId == roleId)
                .ToList();

            if (!roleAccessRows.Any())
            {
                return true;
            }

            return roleAccessRows.Any(r => r.ModuleCode == moduleCode && r.HasAccess);
        }

        // Dashboard
        public IActionResult Index()
        {
            ViewData["Title"] = "Billing Dashboard";
            return View("~/Views/BillingStaff/Index.cshtml");
        }

        // Invoices
        public IActionResult Invoices()
        {
            ViewData["Title"] = "Invoices";
            return View("~/Views/BillingStaff/Invoices.cshtml");
        }

        public IActionResult InvoiceDetails(int id)
        {
            ViewData["Title"] = "Invoice Details";
            ViewData["InvoiceId"] = id;
            return View("~/Views/BillingStaff/InvoiceDetails.cshtml");
        }

        public IActionResult CreateInvoice()
        {
            ViewData["Title"] = "Create Invoice";
            return View("~/Views/BillingStaff/CreateInvoice.cshtml");
        }

        public IActionResult PrintInvoice(int id)
        {
            ViewData["Title"] = "Print Invoice";
            ViewData["InvoiceId"] = id;
            return View("~/Views/BillingStaff/PrintInvoice.cshtml");
        }

        // Payments
        public IActionResult Payments()
        {
            ViewData["Title"] = "Payments";
            return View("~/Views/BillingStaff/Payments.cshtml");
        }

        public IActionResult PaymentDetails(int id)
        {
            ViewData["Title"] = "Payment Details";
            ViewData["PaymentId"] = id;
            return View("~/Views/BillingStaff/PaymentDetails.cshtml");
        }

        // Customer Accounts
        public IActionResult Accounts()
        {
            ViewData["Title"] = "Customer Accounts";
            return View("~/Views/BillingStaff/Accounts.cshtml");
        }

        public IActionResult CustomerStatement(int id)
        {
            ViewData["Title"] = "Customer Statement";
            ViewData["CustomerId"] = id;
            return View("~/Views/BillingStaff/CustomerStatement.cshtml");
        }

        // Financial Reports
        public IActionResult Reports()
        {
            ViewData["Title"] = "Financial Reports";
            return View("~/Views/BillingStaff/Reports.cshtml");
        }

        // Refunds (Requires Admin Approval)
        public IActionResult Refunds()
        {
            ViewData["Title"] = "Refund Requests";
            return View("~/Views/BillingStaff/Refunds.cshtml");
        }

        // Credit Notes (Requires Admin Approval)
        public IActionResult CreditNotes()
        {
            ViewData["Title"] = "Credit Notes";
            return View("~/Views/BillingStaff/CreditNotes.cshtml");
        }
    }
}
