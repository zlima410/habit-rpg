using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HabitRPG.Api.DTOs;
using HabitRPG.Api.Models;
using HabitRPG.Api.Data;
using HabitRPG.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HabitRPG.Api.Tests.Integration
{
    public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
    {
        protected readonly CustomWebApplicationFactory<Program> Factory;
        protected readonly HttpClient Client;
        protected readonly IServiceScope Scope;
        protected readonly IUnitOfWork UnitOfWork;
        protected readonly ApplicationDbContext DbContext;

        protected IntegrationTestBase(CustomWebApplicationFactory<Program> factory)
        {
            Factory = factory;
            Client = Factory.CreateClient();

            // Create a scope that will be disposed properly
            Scope = Factory.Services.CreateScope();
            UnitOfWork = Scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        protected async Task<string> GetAuthTokenAsync(string email = "test@example.com", string password = "password123")
        {
            var existingUser = await UnitOfWork.Users.GetByEmailAsync(email);
            if (existingUser == null)
            {
                var user = new User
                {
                    Username = "testuser",
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    Level = 1,
                    XP = 0,
                    TotalXP = 0,
                    CreatedAt = DateTime.UtcNow
                };
                await UnitOfWork.Users.AddAsync(user);
                await UnitOfWork.SaveChangesAsync();
            }

            var loginRequest = new LoginRequest
            {
                Email = email,
                Password = password
            };

            var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
    
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Login failed with status {response.StatusCode}: {errorContent}");
            }
    
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            return result.GetProperty("token").GetString() ?? string.Empty;
        }

        protected void SetAuthToken(string token)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        protected async Task<User> CreateTestUserAsync(string username = "testuser", string email = "test@example.com", string password = "password123")
        {
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Level = 1,
                XP = 0,
                TotalXP = 0,
                CreatedAt = DateTime.UtcNow
            };

            await UnitOfWork.Users.AddAsync(user);
            await UnitOfWork.SaveChangesAsync();

            return user;
        }

        protected async Task<Habit> CreateTestHabitAsync(int userId, string title = "Test Habit", HabitDifficulty difficulty = HabitDifficulty.Medium)
        {
            var habit = new Habit
            {
                UserId = userId,
                Title = title,
                Description = "Test Description",
                Difficulty = difficulty,
                Frequency = HabitFrequency.Daily,
                IsActive = true,
                CurrentStreak = 0,
                BestStreak = 0,
                CreatedAt = DateTime.UtcNow
            };

            await UnitOfWork.Habits.AddAsync(habit);
            await UnitOfWork.SaveChangesAsync();
            
            return habit;
        }

        protected async Task<CompletionLog> CreateTestCompletionLogAsync(int habitId, DateTime? completedAt = null)
        {
            var log = new CompletionLog
            {
                HabitId = habitId,
                CompletedAt = completedAt ?? DateTime.UtcNow
            };

            await UnitOfWork.CompletionLogs.AddAsync(log);
            await UnitOfWork.SaveChangesAsync();
            
            return log;
        }

        // Helper method to reload an entity from the database after API changes
        protected async Task<T> ReloadEntityAsync<T>(T entity) where T : class
        {
            await DbContext.Entry(entity).ReloadAsync();
            return entity;
        }

        public void Dispose()
        {
            DbContext.Database.EnsureDeleted();
            UnitOfWork.Dispose();
            Scope.Dispose();
            DbContext.Dispose();
            Client.Dispose();
        }
    }
}