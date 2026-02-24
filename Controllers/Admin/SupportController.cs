using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;
using System.Security.Claims;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Support Controller for Admin - Uses Views/Admin/Support folder
    /// RoleId: 1 - Super Admin, 2 - Company Admin
    /// </summary>
    public class SupportController : Controller
    {
        private readonly CompuGearDbContext _context;

        public SupportController(CompuGearDbContext context)
        {
            _context = context;
        }

        // Admin authorization check - only Super Admin (1) and Company Admin (2)
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            
            var roleId = HttpContext.Session.GetInt32("RoleId");
            
            if (roleId == null || (roleId != 1 && roleId != 2))
            {
                context.Result = RedirectToAction("Login", "Auth");
            }
        }

        // Helper to get current support staff user
        private async Task<User?> GetCurrentUserAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return await _context.Users.FindAsync(userId);
            }
            // For demo, return first support staff
            return await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 4);
        }

        // Helper: returns CompanyId from session. Super Admin (RoleId=1) gets null → sees all data.
        private int? GetCompanyId()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId == 1) return null; // Super Admin sees everything
            return HttpContext.Session.GetInt32("CompanyId");
        }

        #region Views

        public IActionResult Tickets()
        {
            ViewData["Title"] = "Support Tickets";
            return View("~/Views/Admin/Support/Tickets.cshtml");
        }

        public IActionResult Status()
        {
            ViewData["Title"] = "Ticket Status";
            return View("~/Views/Admin/Support/Status.cshtml");
        }

        public IActionResult Knowledge()
        {
            ViewData["Title"] = "Knowledge Base";
            return View("~/Views/Admin/Support/Knowledge.cshtml");
        }

        public IActionResult Reports()
        {
            ViewData["Title"] = "Support Reports";
            return View("~/Views/Admin/Support/Reports.cshtml");
        }

        // New: Live Chat view for support staff
        public IActionResult LiveChat()
        {
            ViewData["Title"] = "Live Chat";
            return View("~/Views/Admin/Support/LiveChat.cshtml");
        }

        #endregion

        #region Ticket API Endpoints

        // GET: /Support/GetTickets
        [HttpGet]
        public async Task<IActionResult> GetTickets(string? status = null, string? priority = null, int? categoryId = null, string? search = null)
        {
            var query = _context.SupportTickets
                .Include(t => t.Customer)
                .Include(t => t.Category)
                .Include(t => t.AssignedUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(t => t.Status == status);

            if (!string.IsNullOrEmpty(priority))
                query = query.Where(t => t.Priority == priority);

            if (categoryId.HasValue)
                query = query.Where(t => t.CategoryId == categoryId);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(t => t.TicketNumber.Contains(search) || 
                                         t.Subject.Contains(search) || 
                                         t.ContactEmail.Contains(search));

            var tickets = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.TicketId,
                    t.TicketNumber,
                    t.Subject,
                    t.Description,
                    t.Priority,
                    t.Status,
                    t.Source,
                    Category = t.Category != null ? t.Category.CategoryName : null,
                    CategoryId = t.CategoryId,
                    t.CustomerId,
                    CustomerName = t.Customer != null ? t.Customer.FullName : t.ContactName,
                    CustomerEmail = t.Customer != null ? t.Customer.Email : t.ContactEmail,
                    CustomerPhone = t.Customer != null ? t.Customer.Phone : t.ContactPhone,
                    AssignedToId = t.AssignedTo,
                    AssignedToName = t.AssignedUser != null ? $"{t.AssignedUser.FirstName} {t.AssignedUser.LastName}" : null,
                    t.DueDate,
                    t.SLABreached,
                    t.CreatedAt,
                    t.UpdatedAt,
                    t.ResolvedAt,
                    MessageCount = t.Messages.Count
                })
                .ToListAsync();

            return Json(tickets);
        }

        // GET: /Support/GetTicket/{id}
        [HttpGet]
        public async Task<IActionResult> GetTicket(int id)
        {
            var ticket = await _context.SupportTickets
                .Include(t => t.Customer)
                .Include(t => t.Category)
                .Include(t => t.AssignedUser)
                .Include(t => t.Order)
                .Where(t => t.TicketId == id)
                .Select(t => new
                {
                    t.TicketId,
                    t.TicketNumber,
                    t.Subject,
                    t.Description,
                    t.Priority,
                    t.Status,
                    t.Source,
                    Category = t.Category != null ? t.Category.CategoryName : null,
                    CategoryId = t.CategoryId,
                    t.CustomerId,
                    CustomerName = t.Customer != null ? t.Customer.FullName : t.ContactName,
                    CustomerEmail = t.Customer != null ? t.Customer.Email : t.ContactEmail,
                    CustomerPhone = t.Customer != null ? t.Customer.Phone : t.ContactPhone,
                    CustomerCode = t.Customer != null ? t.Customer.CustomerCode : null,
                    t.OrderId,
                    OrderNumber = t.Order != null ? t.Order.OrderNumber : null,
                    AssignedToId = t.AssignedTo,
                    AssignedToName = t.AssignedUser != null ? $"{t.AssignedUser.FirstName} {t.AssignedUser.LastName}" : null,
                    t.DueDate,
                    t.SLABreached,
                    t.Resolution,
                    t.SatisfactionRating,
                    t.Feedback,
                    t.CreatedAt,
                    t.UpdatedAt,
                    t.ResolvedAt,
                    t.ClosedAt
                })
                .FirstOrDefaultAsync();

            if (ticket == null)
                return NotFound(new { message = "Ticket not found" });

            // Get ticket messages
            var messages = await _context.TicketMessages
                .Include(m => m.Sender)
                .Where(m => m.TicketId == id)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.MessageId,
                    m.Message,
                    m.SenderType,
                    SenderName = m.Sender != null ? $"{m.Sender.FirstName} {m.Sender.LastName}" : (m.SenderType == "Customer" ? "Customer" : "System"),
                    m.IsInternal,
                    m.CreatedAt
                })
                .ToListAsync();

            return Json(new { ticket, messages });
        }

        // POST: /Support/CreateTicket
        [HttpPost]
        public async Task<IActionResult> CreateTicket([FromBody] CreateSupportTicketModel model)
        {
            var user = await GetCurrentUserAsync();
            var ticketCount = await _context.SupportTickets.CountAsync() + 1;
            var ticketNumber = $"TKT-{DateTime.Now:yyyy}-{ticketCount:D4}";

            // Get SLA from category
            var category = await _context.TicketCategories.FindAsync(model.CategoryId);
            var slaHours = category?.SLAHours ?? 24;

            var ticket = new SupportTicket
            {
                TicketNumber = ticketNumber,
                CustomerId = model.CustomerId,
                CategoryId = model.CategoryId,
                OrderId = model.OrderId,
                ContactName = model.ContactName,
                ContactEmail = model.ContactEmail ?? "",
                ContactPhone = model.ContactPhone,
                Subject = model.Subject,
                Description = model.Description,
                Priority = model.Priority ?? "Medium",
                Status = "Open",
                AssignedTo = model.AssignedTo,
                AssignedAt = model.AssignedTo.HasValue ? DateTime.UtcNow : null,
                DueDate = DateTime.UtcNow.AddHours(slaHours),
                Source = model.Source ?? "Web",
                CreatedAt = DateTime.UtcNow
            };

            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            return Json(new { success = true, ticketId = ticket.TicketId, ticketNumber = ticket.TicketNumber });
        }

        // PUT: /Support/UpdateTicket/{id}
        [HttpPut]
        public async Task<IActionResult> UpdateTicket(int id, [FromBody] UpdateSupportTicketModel model)
        {
            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket == null)
                return NotFound(new { message = "Ticket not found" });

            var user = await GetCurrentUserAsync();

            // Track status change
            var oldStatus = ticket.Status;

            if (!string.IsNullOrEmpty(model.Subject))
                ticket.Subject = model.Subject;
            if (!string.IsNullOrEmpty(model.Description))
                ticket.Description = model.Description;
            if (!string.IsNullOrEmpty(model.Priority))
                ticket.Priority = model.Priority;
            if (!string.IsNullOrEmpty(model.Status))
            {
                ticket.Status = model.Status;
                if (model.Status == "Resolved" && ticket.ResolvedAt == null)
                {
                    ticket.ResolvedAt = DateTime.UtcNow;
                    ticket.ResolvedBy = user?.UserId;
                }
                if (model.Status == "Closed" && ticket.ClosedAt == null)
                {
                    ticket.ClosedAt = DateTime.UtcNow;
                }
            }
            if (model.CategoryId.HasValue)
                ticket.CategoryId = model.CategoryId;
            if (model.AssignedTo.HasValue)
            {
                ticket.AssignedTo = model.AssignedTo == 0 ? null : model.AssignedTo;
                if (model.AssignedTo > 0)
                    ticket.AssignedAt = DateTime.UtcNow;
            }
            if (!string.IsNullOrEmpty(model.Resolution))
                ticket.Resolution = model.Resolution;

            ticket.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: /Support/ReplyToTicket
        [HttpPost]
        public async Task<IActionResult> ReplyToTicket([FromBody] SupportTicketReplyModel model)
        {
            var ticket = await _context.SupportTickets.FindAsync(model.TicketId);
            if (ticket == null)
                return NotFound(new { message = "Ticket not found" });

            var user = await GetCurrentUserAsync();

            var message = new TicketMessage
            {
                TicketId = model.TicketId,
                SenderType = "Agent",
                SenderId = user?.UserId,
                Message = model.Message,
                IsInternal = model.IsInternal,
                CreatedAt = DateTime.UtcNow
            };

            _context.TicketMessages.Add(message);

            // Update ticket status if specified
            if (!string.IsNullOrEmpty(model.NewStatus))
            {
                ticket.Status = model.NewStatus;
                if (model.NewStatus == "Resolved")
                {
                    ticket.ResolvedAt = DateTime.UtcNow;
                    ticket.ResolvedBy = user?.UserId;
                }
            }
            else if (ticket.Status == "Open")
            {
                ticket.Status = "In Progress";
            }

            ticket.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, messageId = message.MessageId });
        }

        // GET: /Support/GetTicketMessages/{ticketId}
        [HttpGet]
        public async Task<IActionResult> GetTicketMessages(int ticketId)
        {
            var messages = await _context.TicketMessages
                .Include(m => m.Sender)
                .Where(m => m.TicketId == ticketId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.MessageId,
                    m.Message,
                    m.SenderType,
                    SenderName = m.Sender != null ? $"{m.Sender.FirstName} {m.Sender.LastName}" : (m.SenderType == "Customer" ? "Customer" : "System"),
                    m.IsInternal,
                    m.CreatedAt
                })
                .ToListAsync();

            return Json(messages);
        }

        // DELETE: /Support/DeleteTicket/{id}
        [HttpDelete]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket == null)
                return NotFound(new { message = "Ticket not found" });

            _context.SupportTickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        #endregion

        #region Live Chat API Endpoints

        // GET: /Support/GetActiveChatSessions - Get all active chat sessions for support staff
        [HttpGet]
        public async Task<IActionResult> GetActiveChatSessions()
        {
            var user = await GetCurrentUserAsync();
            
            var sessions = await _context.ChatSessions
                .Include(s => s.Customer)
                .Where(s => s.Status == "Active" || s.Status == "Transferred" || s.Status == "Waiting")
                .OrderByDescending(s => s.StartedAt)
                .Select(s => new
                {
                    s.SessionId,
                    s.SessionToken,
                    s.CustomerId,
                    CustomerName = s.Customer != null ? s.Customer.FullName : "Guest Visitor",
                    CustomerEmail = s.Customer != null ? s.Customer.Email : null,
                    s.VisitorId,
                    s.Status,
                    s.Source,
                    s.AgentId,
                    IsAssignedToMe = s.AgentId == user!.UserId,
                    s.TotalMessages,
                    s.StartedAt,
                    LastMessageAt = s.Messages.Any() ? s.Messages.Max(m => m.CreatedAt) : s.StartedAt,
                    UnreadCount = s.Messages.Count(m => m.SenderType == "Customer" && !m.IsRead)
                })
                .ToListAsync();

            return Json(new { success = true, data = sessions });
        }

        // GET: /Support/GetPendingAgentRequests - Get customers waiting for agent
        [HttpGet]
        public async Task<IActionResult> GetPendingAgentRequests()
        {
            var requests = await _context.ChatSessions
                .Include(s => s.Customer)
                .Where(s => s.Status == "Transferred" && s.AgentId == null)
                .OrderBy(s => s.StartedAt)
                .Select(s => new
                {
                    s.SessionId,
                    s.CustomerId,
                    CustomerName = s.Customer != null ? s.Customer.FullName : "Guest Visitor",
                    CustomerEmail = s.Customer != null ? s.Customer.Email : null,
                    s.VisitorId,
                    s.Source,
                    WaitingSince = s.StartedAt,
                    WaitingMinutes = (int)(DateTime.UtcNow - s.StartedAt).TotalMinutes
                })
                .ToListAsync();

            return Json(new { success = true, data = requests });
        }

        // POST: /Support/AcceptChat - Agent accepts a chat request
        [HttpPost]
        public async Task<IActionResult> AcceptChat([FromBody] AcceptChatModel model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            var session = await _context.ChatSessions.FindAsync(model.SessionId);
            if (session == null)
                return NotFound(new { message = "Chat session not found" });

            session.AgentId = user.UserId;
            session.Status = "Active";

            // Add system message
            var systemMessage = new ChatMessage
            {
                SessionId = session.SessionId,
                SenderType = "System",
                Message = $"{user.FirstName} {user.LastName} has joined the chat.",
                MessageType = "Text",
                CreatedAt = DateTime.UtcNow
            };
            _context.ChatMessages.Add(systemMessage);

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Chat accepted" });
        }

        // GET: /Support/GetChatMessages/{sessionId}
        [HttpGet]
        public async Task<IActionResult> GetChatMessages(int sessionId)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
                
            // Get sender names separately
            var agentIds = messages.Where(m => m.SenderType == "Agent" && m.SenderId.HasValue).Select(m => m.SenderId!.Value).Distinct().ToList();
            var agents = await _context.Users.Where(u => agentIds.Contains(u.UserId)).ToDictionaryAsync(u => u.UserId, u => $"{u.FirstName} {u.LastName}");
            
            var result = messages.Select(m => new
                {
                    m.MessageId,
                    m.SenderType,
                    SenderName = m.SenderType == "Agent" && m.SenderId.HasValue && agents.ContainsKey(m.SenderId.Value) 
                        ? agents[m.SenderId.Value] 
                        : (m.SenderType == "Customer" ? "Customer" : m.SenderType),
                    m.Message,
                    m.MessageType,
                    m.CreatedAt
                }).ToList();

            return Json(new { success = true, data = result });
        }

        // POST: /Support/SendChatMessage - Agent sends a message
        [HttpPost]
        public async Task<IActionResult> SendChatMessage([FromBody] AgentChatMessageModel model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            var session = await _context.ChatSessions.FindAsync(model.SessionId);
            if (session == null)
                return NotFound(new { message = "Chat session not found" });

            var message = new ChatMessage
            {
                SessionId = model.SessionId,
                SenderType = "Agent",
                SenderId = user.UserId,
                Message = model.Message,
                MessageType = "Text",
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            session.TotalMessages += 1;
            await _context.SaveChangesAsync();

            return Json(new { success = true, messageId = message.MessageId });
        }

        // POST: /Support/EndChat - Agent ends the chat
        [HttpPost]
        public async Task<IActionResult> EndChat([FromBody] EndChatModel model)
        {
            var user = await GetCurrentUserAsync();
            var session = await _context.ChatSessions.FindAsync(model.SessionId);
            if (session == null)
                return NotFound(new { message = "Chat session not found" });

            session.Status = "Ended";
            session.EndedAt = DateTime.UtcNow;

            // Add system message
            var systemMessage = new ChatMessage
            {
                SessionId = session.SessionId,
                SenderType = "System",
                Message = "Chat session has ended. Thank you for contacting support!",
                MessageType = "Text",
                CreatedAt = DateTime.UtcNow
            };
            _context.ChatMessages.Add(systemMessage);

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: /Support/TransferChat - Transfer chat to another agent
        [HttpPost]
        public async Task<IActionResult> TransferChat([FromBody] TransferChatModel model)
        {
            var session = await _context.ChatSessions.FindAsync(model.SessionId);
            if (session == null)
                return NotFound(new { message = "Chat session not found" });

            var newAgent = await _context.Users.FindAsync(model.NewAgentId);
            if (newAgent == null)
                return NotFound(new { message = "Agent not found" });

            session.AgentId = model.NewAgentId;

            var systemMessage = new ChatMessage
            {
                SessionId = session.SessionId,
                SenderType = "System",
                Message = $"Chat transferred to {newAgent.FirstName} {newAgent.LastName}.",
                MessageType = "Text",
                CreatedAt = DateTime.UtcNow
            };
            _context.ChatMessages.Add(systemMessage);

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // GET: /Support/GetAvailableAgents
        [HttpGet]
        public async Task<IActionResult> GetAvailableAgents()
        {
            var agents = await _context.Users
                .Where(u => u.RoleId == 4 && u.IsActive) // Customer Support Staff
                .Select(u => new
                {
                    u.UserId,
                    FullName = $"{u.FirstName} {u.LastName}",
                    u.Email,
                    ActiveChats = _context.ChatSessions.Count(s => s.AgentId == u.UserId && s.Status == "Active")
                })
                .ToListAsync();

            return Json(new { success = true, data = agents });
        }

        #endregion

        #region Knowledge Base API Endpoints

        // GET: /Support/GetKnowledgeCategories
        [HttpGet]
        public async Task<IActionResult> GetKnowledgeCategories()
        {
            var categories = await _context.KnowledgeCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName,
                    c.Description,
                    ArticleCount = c.Articles.Count(a => a.Status == "Published")
                })
                .ToListAsync();

            return Json(new { success = true, data = categories });
        }

        // GET: /Support/GetKnowledgeArticles
        [HttpGet]
        public async Task<IActionResult> GetKnowledgeArticles(int? categoryId = null, string? search = null, string? status = null)
        {
            var query = _context.KnowledgeArticles
                .Include(a => a.Category)
                .Include(a => a.CreatedByUser)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(a => a.CategoryId == categoryId);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(a => a.Title.Contains(search) || a.Content.Contains(search) || (a.Tags != null && a.Tags.Contains(search)));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);

            var articles = await query
                .OrderByDescending(a => a.UpdatedAt)
                .Select(a => new
                {
                    a.ArticleId,
                    a.Title,
                    a.Slug,
                    a.Summary,
                    a.Tags,
                    a.Status,
                    a.ViewCount,
                    a.HelpfulCount,
                    a.NotHelpfulCount,
                    CategoryId = a.CategoryId,
                    CategoryName = a.Category != null ? a.Category.CategoryName : null,
                    CreatedByName = a.CreatedByUser != null ? $"{a.CreatedByUser.FirstName} {a.CreatedByUser.LastName}" : null,
                    a.PublishedAt,
                    a.CreatedAt,
                    a.UpdatedAt
                })
                .ToListAsync();

            return Json(new { success = true, data = articles });
        }

        // GET: /Support/GetKnowledgeArticle/{id}
        [HttpGet]
        public async Task<IActionResult> GetKnowledgeArticle(int id)
        {
            var article = await _context.KnowledgeArticles
                .Include(a => a.Category)
                .Include(a => a.CreatedByUser)
                .Where(a => a.ArticleId == id)
                .Select(a => new
                {
                    a.ArticleId,
                    a.Title,
                    a.Slug,
                    a.Content,
                    a.Summary,
                    a.Tags,
                    a.Status,
                    a.ViewCount,
                    a.HelpfulCount,
                    a.NotHelpfulCount,
                    a.CategoryId,
                    CategoryName = a.Category != null ? a.Category.CategoryName : null,
                    CreatedByName = a.CreatedByUser != null ? $"{a.CreatedByUser.FirstName} {a.CreatedByUser.LastName}" : null,
                    a.PublishedAt,
                    a.CreatedAt,
                    a.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (article == null)
                return NotFound(new { message = "Article not found" });

            return Json(new { success = true, data = article });
        }

        // POST: /Support/CreateKnowledgeArticle
        [HttpPost]
        public async Task<IActionResult> CreateKnowledgeArticle([FromBody] KnowledgeArticleModel model)
        {
            var user = await GetCurrentUserAsync();

            var article = new KnowledgeArticle
            {
                CategoryId = model.CategoryId,
                Title = model.Title,
                Slug = model.Title.ToLower().Replace(" ", "-"),
                Content = model.Content,
                Summary = model.Summary,
                Tags = model.Tags,
                Status = model.Status ?? "Draft",
                PublishedAt = model.Status == "Published" ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = user?.UserId
            };

            _context.KnowledgeArticles.Add(article);
            await _context.SaveChangesAsync();

            return Json(new { success = true, articleId = article.ArticleId });
        }

        // PUT: /Support/UpdateKnowledgeArticle/{id}
        [HttpPut]
        public async Task<IActionResult> UpdateKnowledgeArticle(int id, [FromBody] KnowledgeArticleModel model)
        {
            var user = await GetCurrentUserAsync();
            var article = await _context.KnowledgeArticles.FindAsync(id);
            if (article == null)
                return NotFound(new { message = "Article not found" });

            if (model.CategoryId.HasValue)
                article.CategoryId = model.CategoryId;
            if (!string.IsNullOrEmpty(model.Title))
            {
                article.Title = model.Title;
                article.Slug = model.Title.ToLower().Replace(" ", "-");
            }
            if (!string.IsNullOrEmpty(model.Content))
                article.Content = model.Content;
            if (model.Summary != null)
                article.Summary = model.Summary;
            if (model.Tags != null)
                article.Tags = model.Tags;
            if (!string.IsNullOrEmpty(model.Status))
            {
                var wasPublished = article.Status == "Published";
                article.Status = model.Status;
                if (model.Status == "Published" && !wasPublished)
                    article.PublishedAt = DateTime.UtcNow;
            }

            article.UpdatedAt = DateTime.UtcNow;
            article.UpdatedBy = user?.UserId;

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // DELETE: /Support/DeleteKnowledgeArticle/{id}
        [HttpDelete]
        public async Task<IActionResult> DeleteKnowledgeArticle(int id)
        {
            var article = await _context.KnowledgeArticles.FindAsync(id);
            if (article == null)
                return NotFound(new { message = "Article not found" });

            _context.KnowledgeArticles.Remove(article);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: /Support/CreateKnowledgeCategory
        [HttpPost]
        public async Task<IActionResult> CreateKnowledgeCategory([FromBody] KnowledgeCategoryModel model)
        {
            var category = new KnowledgeCategory
            {
                CategoryName = model.CategoryName,
                Description = model.Description,
                ParentCategoryId = model.ParentCategoryId,
                DisplayOrder = model.DisplayOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.KnowledgeCategories.Add(category);
            await _context.SaveChangesAsync();

            return Json(new { success = true, categoryId = category.CategoryId });
        }

        #endregion

        #region Reports API Endpoints

        // GET: /Support/GetTicketStats
        [HttpGet]
        public async Task<IActionResult> GetTicketStats()
        {
            var tickets = await _context.SupportTickets.ToListAsync();
            var now = DateTime.UtcNow;
            var today = now.Date;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);
            var thisMonth = new DateTime(now.Year, now.Month, 1);

            var stats = new
            {
                TotalTickets = tickets.Count,
                OpenTickets = tickets.Count(t => t.Status == "Open"),
                InProgressTickets = tickets.Count(t => t.Status == "In Progress"),
                PendingTickets = tickets.Count(t => t.Status == "Pending Customer"),
                ResolvedTickets = tickets.Count(t => t.Status == "Resolved"),
                ClosedTickets = tickets.Count(t => t.Status == "Closed"),
                
                TodayTickets = tickets.Count(t => t.CreatedAt.Date == today),
                ThisWeekTickets = tickets.Count(t => t.CreatedAt >= thisWeek),
                ThisMonthTickets = tickets.Count(t => t.CreatedAt >= thisMonth),
                
                HighPriorityOpen = tickets.Count(t => (t.Priority == "High" || t.Priority == "Critical") && t.Status != "Resolved" && t.Status != "Closed"),
                SLABreached = tickets.Count(t => t.SLABreached),
                
                ByPriority = new
                {
                    Low = tickets.Count(t => t.Priority == "Low"),
                    Medium = tickets.Count(t => t.Priority == "Medium"),
                    High = tickets.Count(t => t.Priority == "High"),
                    Critical = tickets.Count(t => t.Priority == "Critical")
                },
                
                ByStatus = new
                {
                    Open = tickets.Count(t => t.Status == "Open"),
                    InProgress = tickets.Count(t => t.Status == "In Progress"),
                    Pending = tickets.Count(t => t.Status == "Pending Customer"),
                    Resolved = tickets.Count(t => t.Status == "Resolved"),
                    Closed = tickets.Count(t => t.Status == "Closed")
                },
                
                ResolutionRate = tickets.Count > 0 
                    ? Math.Round((decimal)tickets.Count(t => t.Status == "Resolved" || t.Status == "Closed") / tickets.Count * 100, 1) 
                    : 0,
                    
                AverageResolutionHours = tickets.Where(t => t.ResolvedAt.HasValue).Any()
                    ? Math.Round(tickets.Where(t => t.ResolvedAt.HasValue).Average(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours), 1)
                    : 0
            };

            return Json(new { success = true, data = stats });
        }

        // GET: /Support/GetAgentPerformance
        [HttpGet]
        public async Task<IActionResult> GetAgentPerformance()
        {
            var agents = await _context.Users
                .Where(u => u.RoleId == 4 && u.IsActive)
                .Select(u => new
                {
                    u.UserId,
                    FullName = $"{u.FirstName} {u.LastName}",
                    AssignedTickets = _context.SupportTickets.Count(t => t.AssignedTo == u.UserId),
                    ResolvedTickets = _context.SupportTickets.Count(t => t.ResolvedBy == u.UserId),
                    OpenTickets = _context.SupportTickets.Count(t => t.AssignedTo == u.UserId && t.Status != "Resolved" && t.Status != "Closed"),
                    ActiveChats = _context.ChatSessions.Count(s => s.AgentId == u.UserId && s.Status == "Active")
                })
                .ToListAsync();

            return Json(new { success = true, data = agents });
        }

        // GET: /Support/GetTicketCategories
        [HttpGet]
        public async Task<IActionResult> GetTicketCategories()
        {
            var categories = await _context.TicketCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName,
                    c.Description,
                    c.SLAHours,
                    c.Priority,
                    TicketCount = c.Tickets.Count
                })
                .ToListAsync();

            return Json(new { success = true, data = categories });
        }

        // GET: /Support/GetCustomers - For ticket creation dropdown
        [HttpGet]
        public async Task<IActionResult> GetCustomers(string? search = null)
        {
            var query = _context.Customers.Where(c => c.Status == "Active");

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.FirstName.Contains(search) || c.LastName.Contains(search) || c.Email.Contains(search));

            var customers = await query
                .Take(50)
                .Select(c => new
                {
                    c.CustomerId,
                    FullName = c.FirstName + " " + c.LastName,
                    c.Email,
                    c.Phone,
                    c.CustomerCode
                })
                .ToListAsync();

            return Json(new { success = true, data = customers });
        }

        // GET: /Support/GetSupportStats - For Reports page
        [HttpGet]
        public async Task<IActionResult> GetSupportStats()
        {
            var tickets = await _context.SupportTickets.ToListAsync();
            var activeSessions = await _context.ChatSessions.CountAsync(s => s.Status == "Active");
            
            // Agent stats
            var agents = await _context.Users
                .Where(u => u.RoleId == 4 && u.IsActive)
                .Select(a => new
                {
                    a.UserId,
                    Name = a.FirstName + " " + a.LastName,
                    AssignedTickets = _context.SupportTickets.Count(t => t.AssignedTo == a.UserId && t.Status != "Resolved" && t.Status != "Closed"),
                    ResolvedTickets = _context.SupportTickets.Count(t => t.AssignedTo == a.UserId && (t.Status == "Resolved" || t.Status == "Closed"))
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                totalTickets = tickets.Count,
                openTickets = tickets.Count(t => t.Status == "Open"),
                inProgressTickets = tickets.Count(t => t.Status == "In Progress"),
                resolvedTickets = tickets.Count(t => t.Status == "Resolved" || t.Status == "Closed"),
                highPriorityTickets = tickets.Count(t => (t.Priority == "High" || t.Priority == "Critical") && t.Status != "Resolved" && t.Status != "Closed"),
                activeChatSessions = activeSessions,
                avgResponseTime = "< 2h", // Placeholder - would calculate from actual data
                agentStats = agents
            });
        }

        #endregion

        #region Admin API Endpoints (migrated from ApiController)

        // ===== Ticket API Endpoints =====

        [HttpGet]
        [Route("api/tickets")]
        public async Task<IActionResult> ApiGetTickets()
        {
            try
            {
                var companyId = GetCompanyId();
                var tickets = await _context.SupportTickets
                    .Where(t => companyId == null || t.CompanyId == companyId)
                    .Include(t => t.Customer)
                    .Include(t => t.Category)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new
                    {
                        t.TicketId,
                        t.TicketNumber,
                        t.CustomerId,
                        t.AssignedTo,
                        CustomerName = t.Customer != null ? t.Customer.FirstName + " " + t.Customer.LastName : t.ContactName,
                        t.ContactEmail,
                        t.CategoryId,
                        CategoryName = t.Category != null ? t.Category.CategoryName : "",
                        t.Subject,
                        t.Description,
                        t.Priority,
                        t.Status,
                        t.CreatedAt,
                        t.ResolvedAt,
                        t.ClosedAt
                    })
                    .ToListAsync();

                return Ok(tickets);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet]
        [Route("api/tickets/{id}")]
        public async Task<IActionResult> ApiGetTicket(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var ticket = await _context.SupportTickets
                    .Include(t => t.Customer)
                    .Include(t => t.Category)
                    .Include(t => t.Messages)
                    .FirstOrDefaultAsync(t => t.TicketId == id && (companyId == null || t.CompanyId == companyId));

                if (ticket == null) return NotFound();
                return Ok(ticket);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("api/tickets")]
        public async Task<IActionResult> ApiCreateTicket([FromBody] SupportTicket ticket)
        {
            try
            {
                var companyId = GetCompanyId();
                ticket.CompanyId = companyId;
                ticket.TicketNumber = $"TKT-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                ticket.CreatedAt = DateTime.UtcNow;
                ticket.UpdatedAt = DateTime.UtcNow;
                ticket.Status = "Open";

                _context.SupportTickets.Add(ticket);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Ticket created successfully", data = ticket });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut]
        [Route("api/tickets/{id}")]
        public async Task<IActionResult> ApiUpdateTicket(int id, [FromBody] SupportTicket ticket)
        {
            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.SupportTickets.FindAsync(id);
                if (existing == null) return NotFound();
                if (companyId != null && existing.CompanyId != null && existing.CompanyId != companyId) return NotFound();

                // Assign CompanyId if not set (legacy data migration)
                if (existing.CompanyId == null && companyId != null)
                    existing.CompanyId = companyId;

                existing.Subject = ticket.Subject;
                existing.Description = ticket.Description;
                existing.Priority = ticket.Priority;
                existing.Status = ticket.Status;
                existing.CategoryId = ticket.CategoryId;
                existing.AssignedTo = ticket.AssignedTo;
                existing.UpdatedAt = DateTime.UtcNow;

                if (ticket.Status == "Resolved" && !existing.ResolvedAt.HasValue)
                    existing.ResolvedAt = DateTime.UtcNow;
                if (ticket.Status == "Closed" && !existing.ClosedAt.HasValue)
                    existing.ClosedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Ticket updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/tickets/{id}/reply")]
        public async Task<IActionResult> ApiReplyToTicket(int id, [FromBody] TicketMessage message)
        {
            try
            {
                var companyId = GetCompanyId();
                var ticket = await _context.SupportTickets.FindAsync(id);
                if (ticket == null) return NotFound();
                if (companyId != null && ticket.CompanyId != null && ticket.CompanyId != companyId) return NotFound();

                message.TicketId = id;
                message.CreatedAt = DateTime.UtcNow;
                message.SenderType = "Staff";

                _context.TicketMessages.Add(message);

                ticket.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Reply sent successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // Support Staff respond endpoint - sends response and optionally updates status
        [HttpPost]
        [Route("api/tickets/{id}/respond")]
        public async Task<IActionResult> ApiRespondToTicket(int id, [FromBody] TicketResponseRequest request)
        {
            try
            {
                var companyId = GetCompanyId();
                var ticket = await _context.SupportTickets.FindAsync(id);
                if (ticket == null) return NotFound();
                if (companyId != null && ticket.CompanyId != null && ticket.CompanyId != companyId) return NotFound();

                // Get current user info from session
                var userId = HttpContext.Session.GetInt32("UserId");
                var userName = HttpContext.Session.GetString("UserName") ?? "Support Staff";

                // Create the message
                var ticketMessage = new TicketMessage
                {
                    TicketId = id,
                    Message = request.Message,
                    SenderType = "Staff",
                    SenderId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TicketMessages.Add(ticketMessage);

                // Update status if provided
                if (!string.IsNullOrEmpty(request.Status))
                {
                    ticket.Status = request.Status;
                    if (request.Status == "Resolved" && !ticket.ResolvedAt.HasValue)
                        ticket.ResolvedAt = DateTime.UtcNow;
                    if (request.Status == "Closed" && !ticket.ClosedAt.HasValue)
                        ticket.ClosedAt = DateTime.UtcNow;
                }

                ticket.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Response sent successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // Support Staff escalate ticket to admin
        [HttpPost]
        [Route("api/tickets/{id}/escalate")]
        public async Task<IActionResult> ApiEscalateTicket(int id, [FromBody] TicketEscalationRequest request)
        {
            try
            {
                var companyId = GetCompanyId();
                var ticket = await _context.SupportTickets.FindAsync(id);
                if (ticket == null) return NotFound();
                if (companyId != null && ticket.CompanyId != null && ticket.CompanyId != companyId) return NotFound();

                var userId = HttpContext.Session.GetInt32("UserId");

                // Update ticket status to Pending Approval
                ticket.Status = "Pending Approval";
                ticket.UpdatedAt = DateTime.UtcNow;

                // Add an internal note/message about escalation
                var escalationNote = new TicketMessage
                {
                    TicketId = id,
                    Message = $"[ESCALATED TO ADMIN]\nReason: {request.Reason}\nNotes: {request.Notes ?? "N/A"}",
                    SenderType = "System",
                    SenderId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TicketMessages.Add(escalationNote);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Ticket escalated to Admin for approval" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete]
        [Route("api/tickets/{id}")]
        public async Task<IActionResult> ApiDeleteTicket(int id)
        {
            var companyId = GetCompanyId();
            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket == null) return NotFound();
            if (companyId != null && ticket.CompanyId != null && ticket.CompanyId != companyId) return NotFound();

            _context.SupportTickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Ticket deleted successfully" });
        }

        [HttpGet]
        [Route("api/ticket-categories")]
        public async Task<IActionResult> ApiGetTicketCategories()
        {
            try
            {
                var categories = await _context.TicketCategories.Where(c => c.IsActive).ToListAsync();
                return Ok(categories);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        // ===== Live Chat Endpoints =====

        [HttpGet]
        [Route("api/GetCurrentUser")]
        public IActionResult ApiGetCurrentUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");
            var userName = HttpContext.Session.GetString("UserName");
            var fullName = HttpContext.Session.GetString("FullName");

            if (userId == null)
                return Ok(new { success = false, message = "Not logged in" });

            return Ok(new { 
                success = true, 
                data = new { 
                    userId = userId, 
                    roleId = roleId, 
                    userName = userName, 
                    fullName = fullName 
                } 
            });
        }

        [HttpGet]
        [Route("api/GetActiveChatSessions")]
        public async Task<IActionResult> ApiGetActiveChatSessions()
        {
            try
            {
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                
                var sessions = await _context.ChatSessions
                    .Include(s => s.Customer)
                    .Where(s => s.Status == "Active" || s.Status == "Transferred" || s.Status == "Pending")
                    .OrderByDescending(s => s.StartedAt)
                    .Select(s => new
                    {
                        s.SessionId,
                        CustomerName = s.Customer != null ? s.Customer.FullName : "Guest",
                        CustomerEmail = s.Customer != null ? s.Customer.Email : "",
                        s.CustomerId,
                        s.Status,
                        s.TotalMessages,
                        s.StartedAt,
                        s.AgentId,
                        LastMessageAt = _context.ChatMessages
                            .Where(m => m.SessionId == s.SessionId)
                            .OrderByDescending(m => m.CreatedAt)
                            .Select(m => m.CreatedAt)
                            .FirstOrDefault(),
                        UnreadCount = _context.ChatMessages
                            .Count(m => m.SessionId == s.SessionId && m.SenderType == "Customer" && !m.IsRead)
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = sessions });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message, data = new List<object>() });
            }
        }

        [HttpGet]
        [Route("api/GetPendingAgentRequests")]
        public async Task<IActionResult> ApiGetPendingAgentRequests()
        {
            try
            {
                var pendingRequests = await _context.ChatSessions
                    .Include(s => s.Customer)
                    .Where(s => s.Status == "Pending" || s.Status == "Transferred")
                    .OrderBy(s => s.StartedAt)
                    .Select(s => new
                    {
                        s.SessionId,
                        CustomerName = s.Customer != null ? s.Customer.FullName : "Guest",
                        CustomerEmail = s.Customer != null ? s.Customer.Email : "",
                        s.CustomerId,
                        s.StartedAt,
                        WaitingMinutes = (int)(DateTime.UtcNow - s.StartedAt).TotalMinutes
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = pendingRequests });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message, data = new List<object>() });
            }
        }

        [HttpGet]
        [Route("api/GetChatMessages")]
        public async Task<IActionResult> ApiGetChatMessages([FromQuery] int sessionId)
        {
            try
            {
                var messages = await _context.ChatMessages
                    .Where(m => m.SessionId == sessionId)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new
                    {
                        m.MessageId,
                        m.SenderType,
                        m.Message,
                        m.MessageType,
                        m.CreatedAt
                    })
                    .ToListAsync();

                // Mark messages as read
                var unreadMessages = await _context.ChatMessages
                    .Where(m => m.SessionId == sessionId && m.SenderType == "Customer" && !m.IsRead)
                    .ToListAsync();
                
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();

                return Ok(new { success = true, data = messages });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message, data = new List<object>() });
            }
        }

        [HttpPost]
        [Route("api/AcceptChat")]
        public async Task<IActionResult> ApiAcceptChat([FromBody] ChatSessionRequest request)
        {
            try
            {
                var agentId = HttpContext.Session.GetInt32("UserId");
                var agentName = HttpContext.Session.GetString("FullName") ?? "Support Agent";

                var session = await _context.ChatSessions
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
                if (session == null)
                    return Ok(new { success = false, message = "Chat session not found" });

                session.AgentId = agentId;
                session.Status = "Active";

                // Add system message
                var systemMessage = new ChatMessage
                {
                    SessionId = session.SessionId,
                    SenderType = "System",
                    Message = $"{agentName} has joined the chat. How can we assist you today?",
                    MessageType = "Text",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(systemMessage);

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Chat accepted", agentName });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/DeclineChat")]
        public async Task<IActionResult> ApiDeclineChat([FromBody] ChatSessionRequest request)
        {
            try
            {
                var session = await _context.ChatSessions
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
                if (session == null)
                    return Ok(new { success = false, message = "Chat session not found" });

                // Add system message
                var systemMessage = new ChatMessage
                {
                    SessionId = session.SessionId,
                    SenderType = "System",
                    Message = "All agents are currently busy. Please try again later or submit a support ticket.",
                    MessageType = "Text",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(systemMessage);

                session.Status = "Ended";
                session.EndedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Chat declined" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/SendAgentChatMessage")]
        public async Task<IActionResult> ApiSendAgentChatMessage([FromBody] AgentChatMessageRequest request)
        {
            try
            {
                var agentId = HttpContext.Session.GetInt32("UserId") ?? 0;

                var session = await _context.ChatSessions
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
                if (session == null)
                    return Ok(new { success = false, message = "Chat session not found" });

                var message = new ChatMessage
                {
                    SessionId = request.SessionId,
                    SenderType = "Agent",
                    SenderId = agentId,
                    Message = request.Message,
                    MessageType = "Text",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(message);

                session.TotalMessages += 1;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Message sent" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/EndChatSession")]
        public async Task<IActionResult> ApiEndChatSession([FromBody] ChatSessionRequest request)
        {
            try
            {
                var agentName = HttpContext.Session.GetString("FullName") ?? "Support Agent";

                var session = await _context.ChatSessions
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
                if (session == null)
                    return Ok(new { success = false, message = "Chat session not found" });

                // Add system message
                var systemMessage = new ChatMessage
                {
                    SessionId = session.SessionId,
                    SenderType = "System",
                    Message = $"Chat ended by {agentName}. Thank you for contacting CompuGear Support!",
                    MessageType = "Text",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(systemMessage);

                session.Status = "Ended";
                session.EndedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Chat ended" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/TransferChat")]
        public async Task<IActionResult> ApiTransferChat([FromBody] TransferChatRequest request)
        {
            try
            {
                var fromAgentId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var fromAgentName = HttpContext.Session.GetString("FullName") ?? "Support Agent";

                var session = await _context.ChatSessions
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
                if (session == null)
                    return Ok(new { success = false, message = "Chat session not found" });

                var toAgent = await _context.Users.FindAsync(request.ToAgentId);
                if (toAgent == null)
                    return Ok(new { success = false, message = "Target agent not found" });

                // Create transfer record
                var transfer = new ChatTransfer
                {
                    ChatSessionId = session.SessionId,
                    FromUserId = fromAgentId,
                    ToUserId = request.ToAgentId,
                    Reason = request.Reason,
                    TransferredAt = DateTime.UtcNow
                };
                _context.ChatTransfers.Add(transfer);

                // Update session
                session.AgentId = request.ToAgentId;
                session.Status = "Transferred";

                // Add system message
                var toAgentName = $"{toAgent.FirstName} {toAgent.LastName}".Trim();
                var systemMessage = new ChatMessage
                {
                    SessionId = session.SessionId,
                    SenderType = "System",
                    Message = $"Chat transferred from {fromAgentName} to {toAgentName}." + 
                              (!string.IsNullOrEmpty(request.Reason) ? $" Reason: {request.Reason}" : ""),
                    MessageType = "Text",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(systemMessage);

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Chat transferred to {toAgentName}" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/RequestLiveAgent")]
        public async Task<IActionResult> ApiRequestLiveAgent([FromBody] RequestLiveAgentRequest request)
        {
            try
            {
                var customerId = HttpContext.Session.GetInt32("CustomerId");

                // Find existing pending session or create new one
                var session = await _context.ChatSessions
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.CustomerId == customerId && 
                        (s.Status == "Active" || s.Status == "Pending"));

                if (session == null)
                {
                    session = new ChatSession
                    {
                        CustomerId = customerId,
                        VisitorId = request.VisitorId ?? Guid.NewGuid().ToString(),
                        SessionToken = Guid.NewGuid().ToString(),
                        Status = "Pending",
                        StartedAt = DateTime.UtcNow,
                        Source = "Website"
                    };
                    _context.ChatSessions.Add(session);
                }
                else
                {
                    session.Status = "Pending";
                }

                // Add system message
                var systemMessage = new ChatMessage
                {
                    SessionId = session.SessionId,
                    SenderType = "System",
                    Message = "You have requested to speak with a live agent. Please wait while we connect you...",
                    MessageType = "Text",
                    CreatedAt = DateTime.UtcNow
                };
                
                await _context.SaveChangesAsync();
                
                // Add system message after session is saved (to get SessionId)
                systemMessage.SessionId = session.SessionId;
                _context.ChatMessages.Add(systemMessage);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Agent request submitted", sessionId = session.SessionId });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/CustomerSendChatMessage")]
        public async Task<IActionResult> ApiCustomerSendChatMessage([FromBody] CustomerChatMessageRequest request)
        {
            try
            {
                var customerId = HttpContext.Session.GetInt32("CustomerId");

                var session = await _context.ChatSessions
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
                if (session == null)
                    return Ok(new { success = false, message = "Chat session not found" });

                // Prevent customer from sending messages until agent accepts
                if (session.Status == "Pending")
                    return Ok(new { success = false, message = "Please wait for an agent to accept the chat before sending messages.", waitingForAgent = true });

                var message = new ChatMessage
                {
                    SessionId = request.SessionId,
                    SenderType = "Customer",
                    SenderId = customerId,
                    Message = request.Message,
                    MessageType = "Text",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(message);

                session.TotalMessages += 1;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Message sent" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/GetCustomerChatUpdates")]
        public async Task<IActionResult> ApiGetCustomerChatUpdates([FromQuery] int sessionId, [FromQuery] int? lastMessageId)
        {
            try
            {
                var session = await _context.ChatSessions
                    .Where(s => s.SessionId == sessionId)
                    .Select(s => new { s.Status, s.AgentId })
                    .FirstOrDefaultAsync();

                if (session == null)
                    return Ok(new { success = false, message = "Session not found" });

                var messagesQuery = _context.ChatMessages
                    .Where(m => m.SessionId == sessionId);

                if (lastMessageId.HasValue && lastMessageId > 0)
                {
                    messagesQuery = messagesQuery.Where(m => m.MessageId > lastMessageId);
                }

                var messages = await messagesQuery
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new
                    {
                        m.MessageId,
                        m.SenderType,
                        m.Message,
                        m.CreatedAt
                    })
                    .ToListAsync();

                // Get agent name if assigned
                string? agentName = null;
                if (session.AgentId.HasValue)
                {
                    var agent = await _context.Users
                        .Where(u => u.UserId == session.AgentId)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefaultAsync();
                    agentName = agent;
                }

                return Ok(new { 
                    success = true, 
                    data = new {
                        status = session.Status,
                        agentName = agentName,
                        messages = messages
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/CustomerEndChat")]
        public async Task<IActionResult> ApiCustomerEndChat([FromBody] ChatSessionRequest request)
        {
            try
            {
                var session = await _context.ChatSessions
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
                if (session == null)
                    return Ok(new { success = false, message = "Chat session not found" });

                // Add system message
                var systemMessage = new ChatMessage
                {
                    SessionId = session.SessionId,
                    SenderType = "System",
                    Message = "Customer ended the chat. Thank you for contacting CompuGear Support!",
                    MessageType = "Text",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(systemMessage);

                session.Status = "Ended";
                session.EndedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Chat ended" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        // ===== Knowledge Base Endpoints =====

        [HttpGet]
        [Route("api/knowledge-categories")]
        public async Task<IActionResult> ApiGetKnowledgeCategories()
        {
            try
            {
                var categories = await _context.KnowledgeCategories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.CategoryName)
                    .Select(c => new
                    {
                        c.CategoryId,
                        c.CategoryName,
                        c.Description,
                        c.DisplayOrder,
                        c.IsActive
                    })
                    .ToListAsync();

                return Ok(categories);
            }
            catch
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet]
        [Route("api/knowledge-articles")]
        public async Task<IActionResult> ApiGetKnowledgeArticles([FromQuery] int? categoryId = null, [FromQuery] string? search = null, [FromQuery] string? status = null)
        {
            try
            {
                var query = _context.KnowledgeArticles
                    .AsNoTracking()
                    .Include(a => a.Category)
                    .AsQueryable();

                if (categoryId.HasValue)
                    query = query.Where(a => a.CategoryId == categoryId.Value);

                if (!string.IsNullOrWhiteSpace(status))
                    query = query.Where(a => a.Status == status);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(a =>
                        a.Title.Contains(search) ||
                        (a.Content != null && a.Content.Contains(search)) ||
                        (a.Tags != null && a.Tags.Contains(search))
                    );
                }

                var articles = await query
                    .OrderByDescending(a => a.UpdatedAt)
                    .Select(a => new
                    {
                        a.ArticleId,
                        a.CategoryId,
                        CategoryName = a.Category != null ? a.Category.CategoryName : null,
                        a.Title,
                        a.Content,
                        a.Summary,
                        a.Tags,
                        a.ViewCount,
                        a.HelpfulCount,
                        a.NotHelpfulCount,
                        a.Status,
                        a.CreatedAt,
                        a.UpdatedAt,
                        a.CreatedBy,
                        a.UpdatedBy
                    })
                    .ToListAsync();

                return Ok(articles);
            }
            catch
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet]
        [Route("api/knowledge-articles/{id}")]
        public async Task<IActionResult> ApiGetKnowledgeArticle(int id)
        {
            try
            {
                var article = await _context.KnowledgeArticles
                    .AsTracking()
                    .FirstOrDefaultAsync(a => a.ArticleId == id);

                if (article == null)
                    return NotFound(new { success = false, message = "Article not found" });

                article.ViewCount += 1;
                article.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    article.ArticleId,
                    article.CategoryId,
                    article.Title,
                    article.Content,
                    article.Summary,
                    article.Tags,
                    article.ViewCount,
                    article.HelpfulCount,
                    article.NotHelpfulCount,
                    article.Status,
                    article.CreatedAt,
                    article.UpdatedAt,
                    article.CreatedBy,
                    article.UpdatedBy
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/knowledge-articles")]
        public async Task<IActionResult> ApiCreateKnowledgeArticle([FromBody] KnowledgeArticleCreateRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");

                var article = new KnowledgeArticle
                {
                    CategoryId = request.CategoryId,
                    Title = request.Title,
                    Content = request.Content,
                    Summary = request.Summary,
                    Tags = request.Tags,
                    Status = string.IsNullOrWhiteSpace(request.Status) ? "Pending Approval" : request.Status,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.KnowledgeArticles.Add(article);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Knowledge article submitted", articleId = article.ArticleId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion
    }

    #region Request Models

    public class CreateSupportTicketModel
    {
        public int? CustomerId { get; set; }
        public int? CategoryId { get; set; }
        public int? OrderId { get; set; }
        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Priority { get; set; }
        public int? AssignedTo { get; set; }
        public string? Source { get; set; }
    }

    public class UpdateSupportTicketModel
    {
        public string? Subject { get; set; }
        public string? Description { get; set; }
        public string? Priority { get; set; }
        public string? Status { get; set; }
        public int? CategoryId { get; set; }
        public int? AssignedTo { get; set; }
        public string? Resolution { get; set; }
    }

    public class SupportTicketReplyModel
    {
        public int TicketId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
        public string? NewStatus { get; set; }
    }

    public class AcceptChatModel
    {
        public int SessionId { get; set; }
    }

    public class AgentChatMessageModel
    {
        public int SessionId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class EndChatModel
    {
        public int SessionId { get; set; }
    }

    public class TransferChatModel
    {
        public int SessionId { get; set; }
        public int NewAgentId { get; set; }
    }

    public class KnowledgeArticleModel
    {
        public int? CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? Tags { get; set; }
        public string? Status { get; set; }
    }

    public class KnowledgeCategoryModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentCategoryId { get; set; }
        public int DisplayOrder { get; set; }
    }

    #endregion
}
