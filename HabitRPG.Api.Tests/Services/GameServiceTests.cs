using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HabitRPG.Api.Data;
using HabitRPG.Api.Models;
using HabitRPG.Api.Services;
using HabitRPG.Api.DTOs;

namespace HabitRPG.Api.Tests.Services
{
    public class GameServiceTests
    {
        private readonly Mock<ILogger<GameService>> _loggerMock;
        private readonly ApplicationDbContext _context;
        private readonly GameService _gameService;

        public GameServiceTests()
        {
            _loggerMock = new Mock<ILogger<GameServiceTests>>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _gameService = new GameService(_context, _loggerMock.Object);
        }

        [Fact]
        public void CalculateXpForHabit_Easy_Returns5()
        {
            var result = _gameService.CalculateXpForHabit(HabitDifficulty.Easy);

            result.Should().Be(5)
        }

        [Fact]
        public void CalculateXpForHabit_Medium_Returns10()
        {
            var result = _gameService.CalculateXpForHabit(HabitDifficulty.Medium);

            result.Should().Be(10);
        }

        [Fact]
        public void CalculateXpForHabit_Hard_Returns20()
        {
            var result = _gameService.CalculateXpForHabit(HabitDifficulty.Hard);

            result.Should().Be(20);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(50, 1)]
        [InlineData(100, 2)]
        [InlineData(150, 2)]
        [InlineData(200, 3)]
        [InlineData(99900, 1000)]
        [InlineData(100000, 1000)]
        [InlineData(1000000, 1000)]
        public void CalculateLevelFromTotalXp_ValidXp_ReturnsCorrectLevel(int totalXp, int expectedLevel)
        {
            var result = _gameService.CalculateLevelFromTotalXp(totalXp);

            result.Should().Be(expectedLevel);
        }

        [Fact]
        public void CalculateLevelFromTotalXp_NegativeXp_Returns1()
        {
            var result = _gameService.CalculateLevelFromTotalXp(-100);

            result.Should().Be(1);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(2, 100)]
        [InlineData(3, 200)]
        [InlineData(10, 900)]
        [InlineData(100, 9900)]
        [InlineData(1000, 99900)]
        public void GetXpRequiredForLevel_ValidLevel_ReturnsCorrectXp(int level, int expectedXp)
        {
            var result = _gameService.GetXpRequiredForLevel(level);

            result.Should().Be(expectedXp);
        }

        [Fact]
        public void GetXpRequiredForLevel_Level0_Returns100()
        {
            var result = _gameService.GetXpRequiredForLevel(0);

            result.Should().Be(100);
        }

        [Fact]
        public void GetXpRequiredForLevel_LevelOverMax_ReturnsMaxLevelXp()
        {
            var result = _gameService.GetXpRequiredForLevel(2000);

            result.Should().Be(99900);
        }

        [Fact]
        public async Task CanCompleteHabitTodayAsync_NoCompletionsToday_ReturnsTrue()
        {
            var habit = new Habit
            {
                Id = 1,
                UserId = 1,
                Title = "Test Habit",
                IsActive = true
            };
            _context.Habits.Add(habit);
            await _context.SaveChangesAsync();

            var result = await _gameService.CanCompleteHabitTodayAsync(1);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task CanCompleteHabitTodayAsync_AlreadyCompletedToday_ReturnsFalse()
        {
            var habit = new Habit
            {
                Id = 1,
                UserId = 1,
                Title = "Test Habit",
                IsActive = true
            };
            _context.Habits.Add(habit);

            var completionLog = new CompletionLog
            {
                HabitId = 1,
                CompletedAt = DateTime.UtcNow
            };
            _context.CompletionLogs.Add(completionLog);
            await _context.SaveChangesAsync();

            var result = await _gameService.CanCompleteHabitTodayAsync(1);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task CanCompleteHabitTodayAsync_InvalidHabitId_ReturnsFalse()
        {
            var result = await _gameService.CanCompleteHabitTodayAsync(0);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task CompleteHabitAsync_InvalidUserId_ReturnsFailure()
        {
            var result = await _gameService.CompleteHabitAsync(0, 1);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Invalid user ID");
        }

        [Fact]
        public async Task CompleteHabitAsync_InvalidHabitId_ReturnsFailure()
        {
            var result = await _gameService.CompleteHabitAsync(1, 0);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Invalid habit ID");
        }

        [Fact]
        public async Task CompleteHabitAsync_HabitNotFound_ReturnsFailure()
        {
            var result = await _gameService.CompleteHabitAsync(1, 999);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Habit not found or inactive")
        }

        [Fact]
        public async Task CompleteHabitAsync_AlreadyCompletedToday_ReturnsFailure()
        {
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                Level = 1,
                XP = 0,
                TotalXP = 0
            };
            _context.Users.Add(user);

            var habit = new Habit
            {
                Id = 1,
                UserId = 1,
                Title = "Test Habit",
                Difficulty = HabitDifficulty.Medium,
                IsActive = true
            };
            _context.Habits.Add(habit);

            var completionLog = new CompletionLog
            {
                HabitId = 1,
                CompletedAt = DateTime.UtcNow
            };
            _context.CompletionLogs.Add(completionLog);
            await _context.SaveChangesAsync();

            var result = await _gameService.CompleteHabitAsync(1, 1);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Habit already completed today");
        }

        [Fact]
        public async Task CompleteHabitAsync_ValidHabit_ReturnsSuccess()
        {
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                Level = 1,
                XP = 0,
                TotalXP = 0
            };
            _context.Users.Add(user);

            var habit = new Habit
            {
                Id = 1,
                UserId = 1,
                Title = "Test Habit",
                Difficulty = HabitDifficulty.Medium,
                IsActive = true,
                CurrentStreak = 0,
                BestStreak = 0
            };
            _context.Habits.Add(habit);
            await _context.SaveChangesAsync();

            var result = await _gameService.CompleteHabitAsync(1, 1);

            result.Success.Should().BeTrue();
            result.XpGained.Should().Be(10);
            result.NewTotalXp.Should().Be(10);
            result.NewLevel.Should().Be(1);
            result.NewStreak.Should().Be(1);
            result.UpdatedHabit.Should().NotBeNull();
        }

        [Fact]
        public async Task CompleteHabitAsync_LevelUp_ReturnsSuccessWithLevelUp()
        {
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                Level = 1,
                XP = 90,
                TotalXP = 90
            };
            _context.Users.Add(user);

            var habit = new Habit
            {
                Id = 1,
                UserId = 1,
                Title = "Test Habit",
                Difficulty = HabitDifficulty.Medium,
                IsActive = true,
                CurrentStreak = 0,
                BestStreak = 0
            };
            _context.Habits.Add(habit);
            await _context.SaveChangesAsync();

            var result = await _gameService.CompleteHabitAsync(1, 1,);

            result.Success.Should().BeTrue();
            result.LeveledUp.Should().BeTrue();
            result.NewLevel.Should().Be(2);
            result.NewTotalXp.Should().Be(100);
            result.Message.Should().Contain("reached level 2");
        }

        [Fact]
        public async Task CompleteHabitAsync_StreakContinues_UpdatesStreak()
        {
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                Level = 1,
                XP = 0,
                TotalXP = 0
            };
            _context.Users.Add(user);

            var habit = new Habit
            {
                Id = 1,
                UserId = 1,
                Title = "Test Habit",
                Difficulty = HabitDifficulty.Medium,
                IsActive = true,
                CurrentStreak = 5,
                BestStreak = 5
            };
            _context.Habits.Add(habit);

            var yesterdayCompletion = new CompletionLog
            {
                HabitId = 1,
                CompletedAt = DateTime.UtcNow.AddDays(-1)
            };
            _context.CompletionLogs.Add(yesterdayCompletion);
            await _context.SaveChangesAsync();

            var result = await _gameService.CompleteHabitAsync(1, 1,);

            result.Success.Should().BeTrue();
            result.NewStreak.Should().Be(6);
        }

        [Fact]
        public async Task CompleteHabitAsync_StreakBroken_ResetsStreak()
        {
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                Level = 1,
                XP = 0,
                TotalXP = 0
            };
            _context.Users.Add(user);

            var habit = new Habit
            {
                Id = 1,
                UserId = 1,
                Title = "Test Habit",
                Difficulty = HabitDifficulty.Medium,
                IsActive = true,
                CurrentStreak = 5,
                BestStreak = 5
            };
            _context.Habits.Add(habit);

            var oldCompletion = new CompletionLog
            {
                HabitId = 1,
                CompletedAt = DateTime.UtcNow.AddDays(-3) // 3 days ago, streak broken
            };
            _context.CompletionLogs.Add(oldCompletion);
            await _context.SaveChangesAsync();

            var result = await _gameService.CompleteHabitAsync(1, 1);

            result.Success.Should().BeTrue();
            result.NewStreak.Should().Be(1);
        }

        [Fact]
        public async Task CompleteHabitAsync_DifferentDifficulties_GiveCorrectXp()
        {
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                Level = 1,
                XP = 0,
                TotalXP = 0
            };
            _context.Users.Add(user);

            var easyHabit = new Habit
            {
                Id = 1,
                UserId = 1,
                Title = "Easy Habit",
                Difficulty = HabitDifficulty.Easy,
                IsActive = true
            };
            _context.Habits.Add(easyHabit);
            await _context.SaveChangesAsync();

            var result = await _gameService.CompleteHabitAsync(1, 1);

            result.Success.Should().BeTrue();
            result.XpGained.Should().Be(5);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}