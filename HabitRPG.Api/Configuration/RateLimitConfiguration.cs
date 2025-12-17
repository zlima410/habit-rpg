using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Mvc;

namespace HabitRPG.Api.Configuration
{
    public static class RateLimitConfiguration
    {
        public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            services.Configure<IpRateLimitOptions>(options =>
            {
                configuration.GetSection("IpRateLimiting").Bind(options);

                if (environment.IsDevelopment())
                {
                    options.IpWhitelist ??= new List<string>();

                    if (!options.IpWhitelist.Contains("127.0.0.1"))
                        options.IpWhitelist.Add("127.0.0.1");

                    if (!options.IpWhitelist.Contains("::1"))
                        options.IpWhitelist.Add("::1");
                }
                else
                {
                    if (options.IpWhitelist != null)
                    {
                        options.IpWhitelist.Remove("127.0.0.1");
                        options.IpWhitelist.Remove("::1");
                    }
                }
            });
            services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));
            services.Configure<ClientRateLimitOptions>(configuration.GetSection("ClientRateLimiting"));
            services.Configure<ClientRateLimitPolicies>(configuration.GetSection("ClientRateLimitPolicies"));

            services.AddMemoryCache();

            services.AddInMemoryRateLimiting();

            services.AddSingleton<IRateLimitConfiguration, AspNetCoreRateLimit.RateLimitConfiguration>();

            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

            return services;
        }

        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
        {
            app.UseIpRateLimiting();

            return app;
        }
    }
}