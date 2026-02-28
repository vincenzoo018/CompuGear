using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Billing/Accounting Staff Portal Controller
    /// RoleId: 6 - Accounting & Billing Staff
    /// Access: Invoices, payments, customer accounts/balances, financial reports
    /// </summary>
    public class BillingStaffController : Controller
    {
        // Role-based authorization check
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            
            var roleId = HttpContext.Session.GetInt32("RoleId");
            
            // Allow access for: Super Admin (1), Company Admin (2), Accounting & Billing Staff (6)
            if (roleId == null || (roleId != 1 && roleId != 2 && roleId != 6))
            {
                context.Result = RedirectToAction("Login", "Auth");
            }
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

        // Chart of Accounts
        public IActionResult ChartOfAccounts()
        {
            ViewData["Title"] = "Chart of Accounts";
            return View("~/Views/BillingStaff/ChartOfAccounts.cshtml");
        }

        // Journal Entries
        public IActionResult JournalEntries()
        {
            ViewData["Title"] = "Journal Entries";
            return View("~/Views/BillingStaff/JournalEntries.cshtml");
        }

        // General Ledger
        public IActionResult GeneralLedger()
        {
            ViewData["Title"] = "General Ledger";
            return View("~/Views/BillingStaff/GeneralLedger.cshtml");
        }
    }
}
