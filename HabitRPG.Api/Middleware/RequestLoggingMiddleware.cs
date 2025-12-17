using System.Diagnostics;

namespace HabitRPG.Api.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsyc(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            var request = context.Request;
            var method = request.Method;
            var path = request.Path.ToString();
            var queryString = request.QueryString.HasValue ? request.QueryString.Value : string.Empty;
            var traceId = context.TraceIdentifier;
            var userId = context.User.FindFirst("userId")?.Value ?? "anonymous";

            _logger.LogInformation(
                "Incoming HTTP request {Method} {Path}{QueryString} (UserId: {UserId}, TraceId: {TraceId})",
                method,
                path,
                queryString,
                userId,
                traceId
            );

            try
            {
                await _next(context);
                stopwatch.Stop();

                var statusCode = context.Response.StatusCode;

                _logger.LogInformation(
                    "HTTP {Method} {Path}{QueryString} responded {StatusCode} in {ElapsedMilliseconds} ms (UserId: {UserId}, TraceId: {TraceId})",
                    method,
                    path,
                    queryString,
                    statusCode,
                    stopwatch.ElapsedMilliseconds,
                    userId,
                    traceId
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "HTTP {Method} {Path}{QueryString} failed in {ElapsedMilliseconds} ms (UserId: {UserId}, TraceId: {TraceId})",
                    method,
                    path,
                    queryString,
                    stopwatch.ElapsedMilliseconds,
                    userId,
                    traceId
                );

                throw;
            }
        }
    }
}