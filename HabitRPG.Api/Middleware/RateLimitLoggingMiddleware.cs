using Microsoft.AspNetCore.Http;
using System.Net;

namespace HabitRPG.Api.Middleware
{
    public class RateLimitLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitLoggingMiddleware> _logger;

        public RateLimitLoggingMiddleware(RequestDelegate next, ILogger<RateLimitLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            if (context.Response.StatusCode == (int)HttpStatusCode.TooManyRequests)
            {
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var endpoint = $"{context.Request.Method} {context.Request.Path}";
                var userAgent = context.Request.Headers["User-Agent"].ToString();

                _logger.LogWarning(
                    "Rate limit exceeded - IP: {ClientIp}, Endpoint: {Endpoint}, UserAgent: {UserAgent}",
                    clientIp, endpoint, userAgent);
            }
        }
    }
}