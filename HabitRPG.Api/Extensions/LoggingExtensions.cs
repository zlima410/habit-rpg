using HabitRPG.Api.Middleware;

namespace HabitRPG.Api.Extensions
{
    public static class LoggingExtensions
    {
        public static IServiceCollection AddRequestLogging(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            return services;
        }

        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}