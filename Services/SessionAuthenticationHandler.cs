using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CompuGear.Services
{
    public sealed class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public SessionAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var userId = Context.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var roleId = Context.Session.GetInt32("RoleId");
            var roleName = Context.Session.GetString("RoleName") ?? "User";
            var userName = Context.Session.GetString("UserName")
                ?? Context.Session.GetString("CustomerName")
                ?? "User";
            var userEmail = Context.Session.GetString("UserEmail")
                ?? Context.Session.GetString("CustomerEmail")
                ?? string.Empty;

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new(ClaimTypes.Name, userName)
            };

            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                claims.Add(new Claim(ClaimTypes.Email, userEmail));
            }

            if (roleId.HasValue)
            {
                claims.Add(new Claim("role_id", roleId.Value.ToString()));
                claims.Add(new Claim(ClaimTypes.Role, roleName));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            var isApi = Request.Path.StartsWithSegments("/api")
                || Request.Headers.Accept.Any(a => a.Contains("application/json", StringComparison.OrdinalIgnoreCase));

            if (isApi)
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            Response.Redirect("/Auth/Login");
            return Task.CompletedTask;
        }
    }
}
