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
