using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;
using System.Text.Json;

namespace CompuGear.Controllers
{
    /// <summary>
    /// API Controller for Approval Requests workflow
    /// Handles staff requests that need admin approval
    /// </summary>
    [Route("api/approval-requests")]
    [ApiController]
    public class ApprovalRequestsController : ControllerBase
    {
        private readonly CompuGearDbContext _context;

        public ApprovalRequestsController(CompuGearDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all approval requests (Admin only)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllRequests(
            [FromQuery] string? module = null,
            [FromQuery] string? status = null,
            [FromQuery] string? requestType = null)
        {
            // Check if user is admin
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1 && roleId != 2)
            {
                return Forbid("Only admins can view all requests");
            }

            var query = _context.ApprovalRequests
                .Include(r => r.Requester)
                .Include(r => r.Approver)
                .AsQueryable();

            if (!string.IsNullOrEmpty(module))
            {
                query = query.Where(r => r.Module == module);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            if (!string.IsNullOrEmpty(requestType))
            {
                var types = requestType.Split(',');
                query = query.Where(r => types.Contains(r.RequestType));
            }

            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ApprovalRequestListDto
                {
                    RequestId = r.RequestId,
                    RequestCode = r.RequestCode,
                    RequestType = r.RequestType,
                    Module = r.Module,
                    Title = r.Title,
                    Description = r.Description,
                    Reason = r.Reason,
                    EntityType = r.EntityType,
                    EntityId = r.EntityId,
                    EntityName = r.EntityName,
                    RequestData = r.RequestData,
                    Status = r.Status,
                    Priority = r.Priority,
                    RequesterName = r.Requester != null ? r.Requester.FirstName + " " + r.Requester.LastName : "Unknown",
                    RequesterEmail = r.Requester != null ? r.Requester.Email : "",
                    RequesterRole = r.Requester != null && r.Requester.Role != null ? r.Requester.Role.RoleName : "",
                    RequestedAt = r.RequestedAt,
                    ApproverName = r.Approver != null ? r.Approver.FirstName + " " + r.Approver.LastName : null,
                    ApprovedAt = r.ApprovedAt,
                    ApprovalNotes = r.ApprovalNotes,
                    IsRead = r.IsRead
                })
                .ToListAsync();

            return Ok(requests);
        }

        /// <summary>
        /// Get pending requests count (Admin only)
        /// </summary>
        [HttpGet("pending-count")]
        public async Task<IActionResult> GetPendingCount()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1 && roleId != 2)
            {
                return Ok(new { count = 0 });
            }

            var count = await _context.ApprovalRequests
                .Where(r => r.Status == ApprovalStatus.Pending)
                .CountAsync();

            return Ok(new { count });
        }

        /// <summary>
        /// Get a single request by ID (Admin only)
        /// </summary>
        [HttpGet("details/{id:int}")]
        public async Task<IActionResult> GetRequestById(int id)
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1 && roleId != 2)
            {
                return Forbid("Only admins can view request details");
            }

            var request = await _context.ApprovalRequests
                .Include(r => r.Requester)
                    .ThenInclude(u => u!.Role)
                .Include(r => r.Approver)
                .Where(r => r.RequestId == id)
                .Select(r => new ApprovalRequestListDto
                {
                    RequestId = r.RequestId,
                    RequestCode = r.RequestCode,
                    RequestType = r.RequestType,
                    Module = r.Module,
                    Title = r.Title,
                    Description = r.Description,
                    Reason = r.Reason,
                    EntityType = r.EntityType,
                    EntityId = r.EntityId,
                    EntityName = r.EntityName,
                    RequestData = r.RequestData,
                    Status = r.Status,
                    Priority = r.Priority,
                    RequesterName = r.Requester != null ? r.Requester.FirstName + " " + r.Requester.LastName : "Unknown",
                    RequesterEmail = r.Requester != null ? r.Requester.Email : "",
                    RequesterRole = r.Requester != null && r.Requester.Role != null ? r.Requester.Role.RoleName : "",
                    RequestedAt = r.RequestedAt,
                    ApproverName = r.Approver != null ? r.Approver.FirstName + " " + r.Approver.LastName : null,
                    ApprovedAt = r.ApprovedAt,
                    ApprovalNotes = r.ApprovalNotes,
                    IsRead = r.IsRead
                })
                .FirstOrDefaultAsync();

            if (request == null)
            {
                return NotFound(new { success = false, message = "Request not found" });
            }

            // Mark as read
            var entity = await _context.ApprovalRequests.FindAsync(id);
            if (entity != null && !entity.IsRead)
            {
                entity.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Ok(request);
        }

        /// <summary>
        /// Get my requests (for staff members)
        /// </summary>
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests(
            [FromQuery] string? module = null,
            [FromQuery] string? requestType = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var query = _context.ApprovalRequests
                .Where(r => r.RequestedBy == userId.Value)
                .AsQueryable();

            if (!string.IsNullOrEmpty(module))
            {
                query = query.Where(r => r.Module == module);
            }

            if (!string.IsNullOrEmpty(requestType))
            {
                var types = requestType.Split(',');
                query = query.Where(r => types.Contains(r.RequestType));
            }

            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ApprovalRequestListDto
                {
                    RequestId = r.RequestId,
                    RequestCode = r.RequestCode,
                    RequestType = r.RequestType,
                    Module = r.Module,
                    Title = r.Title,
                    Description = r.Description,
                    Reason = r.Reason,
                    EntityType = r.EntityType,
                    EntityId = r.EntityId,
                    EntityName = r.EntityName,
                    RequestData = r.RequestData,
                    Status = r.Status,
                    Priority = r.Priority,
                    RequestedAt = r.RequestedAt,
                    ApprovedAt = r.ApprovedAt,
                    ApprovalNotes = r.ApprovalNotes,
                    IsRead = r.IsRead
                })
                .ToListAsync();

            return Ok(requests);
        }

        /// <summary>
        /// Create a new approval request (Staff)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateApprovalRequestDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Unauthorized(new { success = false, message = "Please log in" });
            }

            try
            {
                // Generate request code
                var requestCode = $"REQ-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";

                var request = new ApprovalRequest
                {
                    RequestCode = requestCode,
                    RequestType = dto.RequestType,
                    Module = dto.Module,
                    Title = dto.Title,
                    Description = dto.Description,
                    Reason = dto.Reason,
                    EntityType = dto.EntityType,
                    EntityId = dto.EntityId,
                    EntityName = dto.EntityName,
                    RequestData = dto.RequestData,
                    Priority = dto.Priority,
                    Status = ApprovalStatus.Pending,
                    RequestedBy = userId.Value,
                    RequestedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ApprovalRequests.Add(request);
                await _context.SaveChangesAsync();

                // Create notification for admins
                await CreateAdminNotification(request);

                return Ok(new { 
                    success = true, 
                    message = "Request submitted successfully",
                    requestId = request.RequestId,
                    requestCode = request.RequestCode
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Error creating request: " + ex.Message });
            }
        }

        /// <summary>
        /// Process approval request (Admin only - Approve/Reject)
        /// </summary>
        [HttpPost("process")]
        public async Task<IActionResult> ProcessRequest([FromBody] ProcessApprovalDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (!userId.HasValue || (roleId != 1 && roleId != 2))
            {
                return Forbid("Only admins can process requests");
            }

            var request = await _context.ApprovalRequests.FindAsync(dto.RequestId);
            if (request == null)
            {
                return NotFound(new { success = false, message = "Request not found" });
            }

            if (request.Status != ApprovalStatus.Pending)
            {
                return BadRequest(new { success = false, message = "Request has already been processed" });
            }

            try
            {
                if (dto.Action.ToLower() == "approve")
                {
                    request.Status = ApprovalStatus.Approved;
                    
                    // Execute the approved action based on request type
                    await ExecuteApprovedAction(request);
                }
                else if (dto.Action.ToLower() == "reject")
                {
                    request.Status = ApprovalStatus.Rejected;
                }
                else
                {
                    return BadRequest(new { success = false, message = "Invalid action. Use 'approve' or 'reject'" });
                }

                request.ApprovedBy = userId.Value;
                request.ApprovedAt = DateTime.UtcNow;
                request.ApprovalNotes = dto.ApprovalNotes;
                request.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Notify the requester
                await NotifyRequester(request);

                return Ok(new { 
                    success = true, 
                    message = $"Request {request.Status.ToLower()} successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Error processing request: " + ex.Message });
            }
        }

        /// <summary>
        /// Get request details (for staff viewing their own requests)
        /// </summary>
        [HttpGet("view/{id:int}")]
        public async Task<IActionResult> GetRequest(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            var request = await _context.ApprovalRequests
                .Include(r => r.Requester)
                .Include(r => r.Approver)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null)
            {
                return NotFound();
            }

            // Only allow admin or the requester to view
            if (roleId != 1 && roleId != 2 && request.RequestedBy != userId)
            {
                return Forbid();
            }

            // Mark as read if admin
            if ((roleId == 1 || roleId == 2) && !request.IsRead)
            {
                request.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Ok(new ApprovalRequestListDto
            {
                RequestId = request.RequestId,
                RequestCode = request.RequestCode,
                RequestType = request.RequestType,
                Module = request.Module,
                Title = request.Title,
                Description = request.Description,
                Reason = request.Reason,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                EntityName = request.EntityName,
                RequestData = request.RequestData,
                Status = request.Status,
                Priority = request.Priority,
                RequesterName = request.Requester != null ? request.Requester.FirstName + " " + request.Requester.LastName : "Unknown",
                RequesterEmail = request.Requester?.Email ?? "",
                RequestedAt = request.RequestedAt,
                ApproverName = request.Approver != null ? request.Approver.FirstName + " " + request.Approver.LastName : null,
                ApprovedAt = request.ApprovedAt,
                ApprovalNotes = request.ApprovalNotes,
                IsRead = request.IsRead
            });
        }

        /// <summary>
        /// Cancel a pending request (Staff can cancel their own)
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelRequest(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var request = await _context.ApprovalRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound(new { success = false, message = "Request not found" });
            }

            if (request.RequestedBy != userId.Value)
            {
                return Forbid("You can only cancel your own requests");
            }

            if (request.Status != ApprovalStatus.Pending)
            {
                return BadRequest(new { success = false, message = "Only pending requests can be cancelled" });
            }

            request.Status = ApprovalStatus.Cancelled;
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Request cancelled" });
        }

        /// <summary>
        /// Execute the approved action based on request type
        /// </summary>
        private async Task ExecuteApprovedAction(ApprovalRequest request)
        {
            if (string.IsNullOrEmpty(request.RequestData)) return;

            try
            {
                var data = JsonDocument.Parse(request.RequestData);
                var root = data.RootElement;

                switch (request.RequestType)
                {
                    case ApprovalRequestTypes.StockAdjustment:
                        await ExecuteStockAdjustment(request, root);
                        break;

                    case ApprovalRequestTypes.ProductCreate:
                        // Product creation would be handled here
                        break;

                    case ApprovalRequestTypes.OrderCancel:
                        await ExecuteOrderCancel(request, root);
                        break;

                    case ApprovalRequestTypes.OrderRefund:
                    case ApprovalRequestTypes.PaymentRefund:
                        await ExecuteRefund(request, root);
                        break;

                    case ApprovalRequestTypes.InvoiceVoid:
                        await ExecuteInvoiceVoid(request, root);
                        break;

                    // Add more cases as needed
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the approval
                Console.WriteLine($"Error executing approved action: {ex.Message}");
            }
        }

        private async Task ExecuteStockAdjustment(ApprovalRequest request, JsonElement data)
        {
            if (!request.EntityId.HasValue) return;

            var product = await _context.Products.FindAsync(request.EntityId.Value);
            if (product == null) return;

            var adjustmentType = data.GetProperty("adjustmentType").GetString();
            var quantity = data.GetProperty("quantity").GetInt32();
            var previousStock = product.StockQuantity;

            if (adjustmentType == "Add")
            {
                product.StockQuantity += quantity;
            }
            else
            {
                product.StockQuantity = Math.Max(0, product.StockQuantity - quantity);
            }

            product.UpdatedAt = DateTime.UtcNow;

            // Create inventory transaction
            var transaction = new InventoryTransaction
            {
                ProductId = product.ProductId,
                TransactionType = adjustmentType == "Add" ? "Stock In" : "Stock Out",
                Quantity = adjustmentType == "Add" ? quantity : -quantity,
                PreviousStock = previousStock,
                NewStock = product.StockQuantity,
                ReferenceType = "ApprovalRequest",
                ReferenceId = request.RequestId,
                Notes = $"Approved request: {request.RequestCode}. Reason: {request.Reason}",
                TransactionDate = DateTime.UtcNow,
                CreatedBy = request.ApprovedBy
            };

            _context.InventoryTransactions.Add(transaction);
            await _context.SaveChangesAsync();
        }

        private async Task ExecuteOrderCancel(ApprovalRequest request, JsonElement data)
        {
            if (!request.EntityId.HasValue) return;

            var order = await _context.Orders.FindAsync(request.EntityId.Value);
            if (order == null) return;

            order.OrderStatus = "Cancelled";
            order.CancelledAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            order.InternalNotes = $"{order.InternalNotes}\nCancelled via approval request: {request.RequestCode}";

            await _context.SaveChangesAsync();
        }

        private async Task ExecuteRefund(ApprovalRequest request, JsonElement data)
        {
            // Refund processing logic
            if (!request.EntityId.HasValue) return;

            var payment = await _context.Payments.FindAsync(request.EntityId.Value);
            if (payment == null) return;

            var refundAmount = data.TryGetProperty("refundAmount", out var amt) ? amt.GetDecimal() : payment.Amount;

            var refund = new Refund
            {
                PaymentId = payment.PaymentId,
                Amount = refundAmount,
                Reason = $"{request.Reason}. Approved request: {request.RequestCode}",
                Status = "Completed",
                ProcessedAt = DateTime.UtcNow,
                ProcessedBy = request.ApprovedBy
            };

            _context.Refunds.Add(refund);
            await _context.SaveChangesAsync();
        }

        private async Task ExecuteInvoiceVoid(ApprovalRequest request, JsonElement data)
        {
            if (!request.EntityId.HasValue) return;

            var invoice = await _context.Invoices.FindAsync(request.EntityId.Value);
            if (invoice == null) return;

            invoice.Status = "Cancelled";
            invoice.Notes = $"{invoice.Notes}\nVoided via approval request: {request.RequestCode}. Reason: {request.Reason}";
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private async Task CreateAdminNotification(ApprovalRequest request)
        {
            try
            {
                // Get all admin users
                var admins = await _context.Users
                    .Where(u => u.RoleId == 1 || u.RoleId == 2)
                    .Select(u => u.UserId)
                    .ToListAsync();

                foreach (var adminId in admins)
                {
                    var notification = new Notification
                    {
                        UserId = adminId,
                        Title = $"New Approval Request: {request.RequestType}",
                        Message = $"{request.Title} - Priority: {request.Priority}",
                        Type = "ApprovalRequest",
                        Link = $"/Home/Approvals",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating notification: {ex.Message}");
            }
        }

        private async Task NotifyRequester(ApprovalRequest request)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = request.RequestedBy,
                    Title = $"Request {request.Status}: {request.RequestCode}",
                    Message = $"Your request '{request.Title}' has been {request.Status.ToLower()}.",
                    Type = "ApprovalResponse",
                    Link = null,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error notifying requester: {ex.Message}");
            }
        }
    }
}
