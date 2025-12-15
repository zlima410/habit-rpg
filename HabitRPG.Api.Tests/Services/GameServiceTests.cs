using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using HabitRPG.Api.Models;
using HabitRPG.Api.Services;
using HabitRPG.Api.DTOs;
using HabitRPG.Api.Repositories;
using HabitRPG.Api.Tests.Helpers;

namespace HabitRPG.Api.Tests.Services
{
    public class GameServiceTests : IDisposable
    {
        private readonly Mock<ILogger<GameService>> _loggerMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IHabitRepository> _habitRepositoryMock;
        private readonly Mock<ICompletionLogRepository> _completionLogRepositoryMock;
        private readonly GameService _gameService;

        public GameServiceTests()
        {
            _loggerMock = new Mock<ILogger<GameService>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _habitRepositoryMock = new Mock<IHabitRepository>();
            _completionLogRepositoryMock = new Mock<ICompletionLogRepository>();

            _unitOfWorkMock.Setup(u => u.Habits).Returns(_habitRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.CompletionLogs).Returns(_completionLogRepositoryMock.Object);

            _gameService = new GameService(_unitOfWorkMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void CalculateXpForHabit_Easy_Returns5()
        {
            var result = _gameService.CalculateXpForHabit(HabitDifficulty.Easy);

            result.Should().Be(5);
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
            _completionLogRepositoryMock
                .Setup(r => r.IsCompletedTodayAsync(1))
                .ReturnsAsync(false);

            var result = await _gameService.CanCompleteHabitTodayAsync(1);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task CanCompleteHabitTodayAsync_AlreadyCompletedToday_ReturnsFalse()
        {
            _completionLogRepositoryMock
                .Setup(r => r.IsCompletedTodayAsync(1))
                .ReturnsAsync(true);

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
            _habitRepositoryMock
                .Setup(r => r.GetByIdWithUserAsync(999))
                .ReturnsAsync((Habit?)null);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            var result = await _gameService.CompleteHabitAsync(1, 999);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Habit not found or inactive");
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CompleteHabitAsync_AlreadyCompletedToday_ReturnsFailure()
        {
            var user = TestHelpers.CreateTestUser(id: 1);
            var habit = TestHelpers.CreateTestHabit(id: 1, userId: 1, difficulty: HabitDifficulty.Medium);
            
            _habitRepositoryMock
                .Setup(r => r.GetByIdWithUserAsync(1))
                .ReturnsAsync(habit);

            _completionLogRepositoryMock
                .Setup(r => r.IsCompletedTodayAsync(1))
                .ReturnsAsync(true);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            var result = await _gameService.CompleteHabitAsync(1, 1);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Habit already completed today");
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CompleteHabitAsync_ValidHabit_ReturnsSuccess()
        {
            var user = TestHelpers.CreateTestUser(id: 1);
            var habit = TestHelpers.CreateTestHabit(id: 1, userId: 1, difficulty: HabitDifficulty.Medium);
            habit.User = user;

            _habitRepositoryMock
                .Setup(r => r.GetByIdWithUserAsync(1))
                .ReturnsAsync(habit);
            
            _completionLogRepositoryMock
                .Setup(r => r.IsCompletedTodayAsync(1))
                .ReturnsAsync(false);

            _completionLogRepositoryMock
                .Setup(r => r.GetLastCompletionBeforeTodayAsync(1))
                .ReturnsAsync((CompletionLog?)null);

            _completionLogRepositoryMock
                .Setup(r => r.GetCompletionsByDateRangeAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<CompletionLog>());

            _completionLogRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<CompletionLog>()))
                .ReturnsAsync((CompletionLog log) => log);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            var result = await _gameService.CompleteHabitAsync(1, 1);

            result.Success.Should().BeTrue();
            result.XpGained.Should().Be(10);
            result.NewTotalXp.Should().Be(10);
            result.NewLevel.Should().Be(1);
            result.NewStreak.Should().Be(1);
            result.UpdatedHabit.Should().NotBeNull();

            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
            _completionLogRepositoryMock.Verify(r => r.AddAsync(It.IsAny<CompletionLog>()), Times.Once);
        }

        [Fact]
        public async Task CompleteHabitAsync_LevelUp_ReturnsSuccessWithLevelUp()
        {
            var user = TestHelpers.CreateTestUser(id: 1);
            user.XP = 90;
            user.TotalXP = 90;
            var habit = TestHelpers.CreateTestHabit(id: 1, userId: 1, difficulty: HabitDifficulty.Medium);
            habit.User = user;

            _habitRepositoryMock
                .Setup(r => r.GetByIdWithUserAsync(1))
                .ReturnsAsync(habit);

            _completionLogRepositoryMock
                .Setup(r => r.IsCompletedTodayAsync(1))
                .ReturnsAsync(false);

            _completionLogRepositoryMock
                .Setup(r => r.GetLastCompletionBeforeTodayAsync(1))
                .ReturnsAsync((CompletionLog?)null);

            _completionLogRepositoryMock
                .Setup(r => r.GetCompletionsByDateRangeAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<CompletionLog>());

            _completionLogRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<CompletionLog>()))
                .ReturnsAsync((CompletionLog log) => log);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            var result = await _gameService.CompleteHabitAsync(1, 1);

            result.Success.Should().BeTrue();
            result.LeveledUp.Should().BeTrue();
            result.NewLevel.Should().Be(2);
            result.NewTotalXp.Should().Be(100);
            result.Message.Should().Contain("reached level 2");
        }

        [Fact]
        public async Task CompleteHabitAsync_StreakContinues_UpdatesStreak()
        {
            var user = TestHelpers.CreateTestUser(id: 1);
            var habit = TestHelpers.CreateTestHabit(id: 1, userId: 1, difficulty: HabitDifficulty.Medium);
            habit.User = user;
            habit.CurrentStreak = 5;
            habit.BestStreak = 5;

            var yesterdayCompletion = TestHelpers.CreateTestCompletionLog(habitId: 1, completedAt: DateTime.UtcNow.AddDays(-1));
            
            _habitRepositoryMock
                .Setup(r => r.GetByIdWithUserAsync(1))
                .ReturnsAsync(habit);

            _completionLogRepositoryMock
                .Setup(r => r.IsCompletedTodayAsync(1))
                .ReturnsAsync(false);

            _completionLogRepositoryMock
                .Setup(r => r.GetLastCompletionBeforeTodayAsync(1))
                .ReturnsAsync(yesterdayCompletion);

            _completionLogRepositoryMock
                .Setup(r => r.GetCompletionsByDateRangeAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<CompletionLog>());

            _completionLogRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<CompletionLog>()))
                .ReturnsAsync((CompletionLog log) => log);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            var result = await _gameService.CompleteHabitAsync(1, 1);

            result.Success.Should().BeTrue();
            result.NewStreak.Should().Be(6);
        }

        [Fact]
        public async Task CompleteHabitAsync_StreakBroken_ResetsStreak()
        {
            var user = TestHelpers.CreateTestUser(id: 1);
            var habit = TestHelpers.CreateTestHabit(id: 1, userId: 1, difficulty: HabitDifficulty.Medium);
            habit.User = user;
            habit.CurrentStreak = 5;
            habit.BestStreak = 5;

            var oldCompletion = TestHelpers.CreateTestCompletionLog(habitId: 1, completedAt: DateTime.UtcNow.AddDays(-3));

            _habitRepositoryMock
                .Setup(r => r.GetByIdWithUserAsync(1))
                .ReturnsAsync(habit);

            _completionLogRepositoryMock
                .Setup(r => r.IsCompletedTodayAsync(1))
                .ReturnsAsync(false);

            _completionLogRepositoryMock
                .Setup(r => r.GetLastCompletionBeforeTodayAsync(1))
                .ReturnsAsync(oldCompletion);

            _completionLogRepositoryMock
                .Setup(r => r.GetCompletionsByDateRangeAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<CompletionLog>());

            _completionLogRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<CompletionLog>()))
                .ReturnsAsync((CompletionLog log) => log);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            var result = await _gameService.CompleteHabitAsync(1, 1);

            result.Success.Should().BeTrue();
            result.NewStreak.Should().Be(1);
        }

        [Fact]
        public async Task CompleteHabitAsync_DifferentDifficulties_GiveCorrectXp()
        {
            var user = TestHelpers.CreateTestUser(id: 1);
            var easyHabit = TestHelpers.CreateTestHabit(id: 1, userId: 1, difficulty: HabitDifficulty.Easy);
            easyHabit.User = user;

            _habitRepositoryMock
                .Setup(r => r.GetByIdWithUserAsync(1))
                .ReturnsAsync(easyHabit);

            _completionLogRepositoryMock
                .Setup(r => r.IsCompletedTodayAsync(1))
                .ReturnsAsync(false);

            _completionLogRepositoryMock
                .Setup(r => r.GetLastCompletionBeforeTodayAsync(1))
                .ReturnsAsync((CompletionLog?)null);

            _completionLogRepositoryMock
                .Setup(r => r.GetCompletionsByDateRangeAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<CompletionLog>());

            _completionLogRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<CompletionLog>()))
                .ReturnsAsync((CompletionLog log) => log);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            var result = await _gameService.CompleteHabitAsync(1, 1);

            result.Success.Should().BeTrue();
            result.XpGained.Should().Be(5);
        }

        public void Dispose()
        {

        }
    }
}