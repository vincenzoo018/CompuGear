using CompuGear.Data;
using CompuGear.Models;
using System.Text.Json;

namespace CompuGear.Services
{
    /// <summary>
    /// Service for logging audit trail activities
    /// </summary>
    public interface IAuditService
    {
        Task LogAsync(string action, string module, string? entityType = null, int? entityId = null, 
            string? description = null, object? oldValues = null, object? newValues = null);
        Task LogLoginAsync(int userId, string userName, bool success, string? reason = null);
        Task LogLogoutAsync(int userId, string userName);
    }

    public class AuditService : IAuditService
    {
        private readonly CompuGearDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(CompuGearDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string action, string module, string? entityType = null, int? entityId = null,
            string? description = null, object? oldValues = null, object? newValues = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userId = httpContext?.Session.GetInt32("UserId");
            var userName = httpContext?.Session.GetString("UserName");

            var log = new ActivityLog
            {
                UserId = userId,
                UserName = userName ?? "System",
                Action = action,
                Module = module,
                EntityType = entityType,
                EntityId = entityId,
                Description = description,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                IPAddress = GetClientIP(),
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                SessionId = httpContext?.Session.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task LogLoginAsync(int userId, string userName, bool success, string? reason = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var action = success ? "Login" : "Login Failed";
            var description = success 
                ? $"User {userName} logged in successfully" 
                : $"Login attempt failed for {userName}: {reason}";

            var log = new ActivityLog
            {
                UserId = success ? userId : null,
                UserName = userName,
                Action = action,
                Module = "Authentication",
                Description = description,
                IPAddress = GetClientIP(),
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                SessionId = httpContext?.Session.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task LogLogoutAsync(int userId, string userName)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var log = new ActivityLog
            {
                UserId = userId,
                UserName = userName,
                Action = "Logout",
                Module = "Authentication",
                Description = $"User {userName} logged out",
                IPAddress = GetClientIP(),
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                SessionId = httpContext?.Session.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        private string? GetClientIP()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            // Check for forwarded IP (when behind proxy/load balancer)
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',').FirstOrDefault()?.Trim();
            }

            return httpContext.Connection.RemoteIpAddress?.ToString();
        }
    }
}
