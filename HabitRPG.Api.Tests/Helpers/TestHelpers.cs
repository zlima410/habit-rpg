using HabitRPG.Api.Data;
using HabitRPG.Api.Models;
using HabitRPG.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HabitRPG.Api.Tests.Helpers
{
    public static class TestHelpers
    {
        public static ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ApplicationDbContext(options);
        }

        public static User CreateTestUser(int id = 1, string username = "testuser", string email = "test@example.com")
        {
            return new User
            {
                Id = id,
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Level = 1,
                XP = 0,
                TotalXP = 0,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static Habit CreateTestHabit(int id = 1, int userId = 1, HabitDifficulty difficulty = HabitDifficulty.Medium)
        {
            return new Habit
            {
                Id = id,
                UserId = userId,
                Title = "Test Habit",
                Description = "Test Description",
                Difficulty = difficulty,
                Frequency = HabitFrequency.Daily,
                IsActive = true,
                CurrentStreak = 0,
                BestStreak = 0,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static CompletionLog CreateTestCompletionLog(int habitId, DateTime? completedAt = null)
        {
            return new CompletionLog
            {
                HabitId = habitId,
                CompletedAt = completedAt ?? DateTime.UtcNow
            };
        }

        public static RegisterRequest CreateTestRegisterRequest(string username, string email, string password)
        {
            return new RegisterRequest
            {
                Username = username,
                Email = email,
                Password = password
            };
        }

        public static LoginRequest CreateTestLoginRequest(string email, string password)
        {
            return new LoginRequest
            {
                Email = email,
                Password = password
            };  
        }
    }
}