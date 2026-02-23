using Microsoft.AspNetCore.Http;

namespace CompuGear.Services
{
    public class ActivityAuditMiddleware
    {
        private readonly RequestDelegate _next;

        public ActivityAuditMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            var shouldTrack = ShouldTrackRequest(request);

            await _next(context);

            if (!shouldTrack || context.Response.StatusCode >= 400)
                return;

            try
            {
                var auditService = context.RequestServices.GetRequiredService<IAuditService>();

                var module = ResolveModule(request.Path);
                var action = ResolveAction(request.Method, request.Path);
                var entityId = ResolveEntityId(context);
                var userName = context.Session.GetString("UserName") ?? context.Session.GetString("CustomerName") ?? "System";
                var description = $"{action} via {request.Method} {request.Path} by {userName}";

                await auditService.LogAsync(action, module, module, entityId, description);
            }
            catch
            {
                // Ignore audit failures to avoid breaking normal requests
            }
        }

        private static bool ShouldTrackRequest(HttpRequest request)
        {
            if (!HttpMethods.IsPost(request.Method) &&
                !HttpMethods.IsPut(request.Method) &&
                !HttpMethods.IsPatch(request.Method) &&
                !HttpMethods.IsDelete(request.Method))
                return false;

            var path = request.Path;

            if (path.StartsWithSegments("/Auth/ProcessLogin") ||
                path.StartsWithSegments("/Auth/Logout"))
                return false;

            if (HasFileExtension(path))
                return false;

            if (path.StartsWithSegments("/api/audit-logs") ||
                path.StartsWithSegments("/api/activity-logs") ||
                path.StartsWithSegments("/api/export"))
                return false;

            return true;
        }

        private static bool HasFileExtension(PathString path)
        {
            var value = path.Value;
            if (string.IsNullOrWhiteSpace(value)) return false;

            var lastSegment = value.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
            return lastSegment.Contains('.');
        }

        private static string ResolveModule(PathString path)
        {
            var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            if (segments.Length < 2) return "System";

            var raw = string.Equals(segments[0], "api", StringComparison.OrdinalIgnoreCase)
                ? segments[1]
                : segments[0];

            if (string.IsNullOrWhiteSpace(raw)) return "System";

            return Humanize(raw);
        }

        private static string ResolveAction(string method, PathString path)
        {
            var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            var hasId = segments.Length >= 3 && int.TryParse(segments[2], out _);

            var actionFromPath = ResolveActionFromPath(segments, method);
            if (!string.IsNullOrWhiteSpace(actionFromPath))
                return actionFromPath;

            if (HttpMethods.IsPost(method)) return "Create";
            if (HttpMethods.IsDelete(method)) return "Delete";
            if (HttpMethods.IsPatch(method)) return "Update";
            if (HttpMethods.IsPut(method))
            {
                if (segments.Length >= 4)
                {
                    var suffix = segments[3].Replace("-", " ").Replace("_", " ");
                    return "Update " + suffix;
                }
                return hasId ? "Update" : "Bulk Update";
            }

            return "Activity";
        }

        private static string? ResolveActionFromPath(string[] segments, string method)
        {
            if (segments.Length == 0) return null;

            if (string.Equals(segments[0], "api", StringComparison.OrdinalIgnoreCase))
            {
                if (segments.Length == 2 && !IsLikelyCollection(segments[1]))
                    return Humanize(segments[1]);

                if (HttpMethods.IsPut(method) && segments.Length >= 4 && !int.TryParse(segments[3], out _))
                    return "Update " + Humanize(segments[3]);
            }
            else if (segments.Length >= 2)
            {
                return Humanize(segments[1]);
            }

            return null;
        }

        private static bool IsLikelyCollection(string segment)
        {
            var s = segment.ToLowerInvariant();
            return s.EndsWith("s") || s.Contains("-");
        }

        private static string Humanize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Activity";

            var cleaned = value.Replace("-", " ").Replace("_", " ").Trim();
            if (cleaned.Length == 0) return "Activity";

            var output = new List<char>();
            for (var i = 0; i < cleaned.Length; i++)
            {
                var current = cleaned[i];
                var prev = i > 0 ? cleaned[i - 1] : '\0';

                if (i > 0 && char.IsUpper(current) && (char.IsLower(prev) || char.IsDigit(prev)))
                    output.Add(' ');

                output.Add(current);
            }

            return string.Join(' ', new string(output.ToArray())
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
        }

        private static int? ResolveEntityId(HttpContext context)
        {
            var idFromRoute = context.Request.RouteValues.TryGetValue("id", out var routeId)
                ? routeId?.ToString()
                : null;

            if (int.TryParse(idFromRoute, out var parsedId))
                return parsedId;

            var pathSegments = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            if (pathSegments.Length >= 3 && int.TryParse(pathSegments[2], out parsedId))
                return parsedId;

            return null;
        }
    }
}
