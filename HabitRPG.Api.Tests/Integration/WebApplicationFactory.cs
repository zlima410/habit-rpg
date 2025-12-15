using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HabitRPG.Api.Data;

namespace HabitRPG.Api.Tests.Integration
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        private readonly string _databaseName;

        public CustomWebApplicationFactory()
        {
            _databaseName = "TestDb_" + Guid.NewGuid().ToString();
        }

        static CustomWebApplicationFactory()
        {
            Environment.SetEnvironmentVariable("DEV_DATABASE_CONNECTION", "dummy-connection-string-for-testing");
            Environment.SetEnvironmentVariable("JWT_SECRET_KEY", "test-secret-key-that-is-at-least-32-characters-long-for-testing-purposes");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);

                    options.ConfigureWarnings(warnings =>
                        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });

                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                    db.Database.EnsureCreated();
                }
            });
        }
    }
}