using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HabitRPG.Api.DTOs;
using HabitRPG.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HabitRPG.Api.Tests.Integration
{
    public class HabitsControllerTests : IntegrationTestBase
    {
        private string _authToken = string.Empty;
        private User _testUser = null!;

        public HabitsControllerTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        public async Task InitializeAsync()
        {
            _testUser = await CreateTestUserAsync();
            _authToken = await GetAuthTokenAsync(_testUser.Email);
            SetAuthToken(_authToken);
        }

        [Fact]
        public async Task GetHabits_Unauthenticated_ReturnsUnauthorized()
        {
            Client.DefaultRequestHeaders.Authorization = null;

            var response = await Client.GetAsync("/api/habits");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetHabits_NoHabits_ReturnsEmptyList()
        {
            await InitializeAsync();

            var response = await Client.GetAsync("/api/habits");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var habits = await response.Content.ReadFromJsonAsync<List<HabitDto>>();
            habits.Should().BeEmpty();
        }

        [Fact]
        public async Task GetHabits_WithHabits_ReturnsHabits()
        {
            await InitializeAsync();
            await CreateTestHabitAsync(_testUser.Id, "Habit 1");
            await CreateTestHabitAsync(_testUser.Id, "Habit 2");

            var response = await Client.GetAsync("/api/habits");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var habits = await response.Content.ReadFromJsonAsync<List<HabitDto>>();
            habits.Should().HaveCount(2);
            habits!.Any(h => h.Title == "Habit 1").Should().BeTrue();
            habits.Any(h => h.Title == "Habit 2").Should().BeTrue();
        }

        [Fact]
        public async Task GetHabits_IncludeInactive_ReturnsAllHabits()
        {
            await InitializeAsync();
            var activeHabit = await CreateTestHabitAsync(_testUser.Id, "Active Habit");
            var inactiveHabit = await CreateTestHabitAsync(_testUser.Id, "Inactive Habit");

            inactiveHabit.IsActive = false;
            UnitOfWork.Habits.Update(inactiveHabit);
            await UnitOfWork.SaveChangesAsync();

            var response = await Client.GetAsync("/api/habits?includeInactive=true");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var habits = await response.Content.ReadFromJsonAsync<List<HabitDto>>();
            habits.Should().HaveCount(2);
        }

        [Fact]
        public async Task CreateHabit_ValidRequest_ReturnsCreated()
        {
            await InitializeAsync();
            var request = new CreateHabitRequest
            {
                Title = "New Habit",
                Description = "Test Description",
                Frequency = HabitFrequency.Daily,
                Difficulty = HabitDifficulty.Medium
            };

            var response = await Client.PostAsJsonAsync("/api/habits", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var habit = await response.Content.ReadFromJsonAsync<HabitDto>();
            habit.Should().NotBeNull();
            habit!.Title.Should().Be("New Habit");
            habit.Difficulty.Should().Be(HabitDifficulty.Medium);
        }

        [Fact]
        public async Task CreateHabit_InvalidRequest_ReturnsBadRequest()
        {
            await InitializeAsync();
            var request = new CreateHabitRequest
            {
                Title = "",
                Frequency = HabitFrequency.Daily,
                Difficulty = HabitDifficulty.Medium
            };

            var response = await Client.PostAsJsonAsync("/api/habits", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateHabit_DuplicateTitle_ReturnsBadRequest()
        {
            await InitializeAsync();
            await CreateTestHabitAsync(_testUser.Id, "Duplicate Title");

            var request = new CreateHabitRequest
            {
                Title = "Duplicate Title",
                Frequency = HabitFrequency.Daily,
                Difficulty = HabitDifficulty.Medium
            };

            var response = await Client.PostAsJsonAsync("/api/habits", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetHabit_ValidId_ReturnsHabit()
        {
            await InitializeAsync();
            var habit = await CreateTestHabitAsync(_testUser.Id, "Test Habit");

            var response = await Client.GetAsync($"/api/habits/{habit.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<HabitDto>();
            result.Should().NotBeNull();
            result!.Title.Should().Be("Test Habit");
        }

        [Fact]
        public async Task GetHabit_InvalidId_ReturnsNotFound()
        {
            await InitializeAsync();

            var response = await Client.GetAsync("/api/habits/999");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateHabit_ValidRequest_ReturnsNoContent()
        {
            await InitializeAsync();
            var habit = await CreateTestHabitAsync(_testUser.Id, "Original Title");

            var request = new UpdateHabitRequest
            {
                Title = "Updated Title",
                Description = "Updated Description"
            };

            var response = await Client.PatchAsJsonAsync($"/api/habits/{habit.Id}", request);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify update
            var getResponse = await Client.GetAsync($"/api/habits/{habit.Id}");
            var updatedHabit = await getResponse.Content.ReadFromJsonAsync<HabitDto>();
            updatedHabit!.Title.Should().Be("Updated Title");
        }

        [Fact]
        public async Task DeleteHabit_ValidId_ReturnsNoContent()
        {
            await InitializeAsync();
            var habit = await CreateTestHabitAsync(_testUser.Id, "To Delete");

            var response = await Client.DeleteAsync($"/api/habits/{habit.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify deletion (soft delete)
            var getResponse = await Client.GetAsync($"/api/habits/{habit.Id}");
            var deletedHabit = await getResponse.Content.ReadFromJsonAsync<HabitDto>();
            deletedHabit!.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task CompleteHabit_ValidHabit_ReturnsSuccess()
        {
            await InitializeAsync();
            var habit = await CreateTestHabitAsync(_testUser.Id, "Complete Me", HabitDifficulty.Medium);

            var response = await Client.PostAsync($"/api/habits/{habit.Id}/complete", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            result.GetProperty("success").GetBoolean().Should().BeTrue();
            result.GetProperty("xpGained").GetInt32().Should().Be(10);
        }

        [Fact]
        public async Task CompleteHabit_AlreadyCompletedToday_ReturnsBadRequest()
        {
            await InitializeAsync();
            var habit = await CreateTestHabitAsync(_testUser.Id, "Already Done");
            await CreateTestCompletionLogAsync(habit.Id);

            var response = await Client.PostAsync($"/api/habits/{habit.Id}/complete", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CompleteHabit_LevelUp_ReturnsSuccessWithLevelUp()
        {
            await InitializeAsync();

            _testUser.TotalXP = 90;
            UnitOfWork.Users.Update(_testUser);
            await UnitOfWork.SaveChangesAsync();

            var habit = await CreateTestHabitAsync(_testUser.Id, "Level Up", HabitDifficulty.Medium);

            var response = await Client.PostAsync($"/api/habits/{habit.Id}/complete", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            result.GetProperty("leveledUp").GetBoolean().Should().BeTrue();
            result.GetProperty("newLevel").GetInt32().Should().Be(2);
        }

        [Fact]
        public async Task RestoreHabit_ValidId_ReturnsNoContent()
        {
            await InitializeAsync();
            var habit = await CreateTestHabitAsync(_testUser.Id, "To Restore");

            habit.IsActive = false;
            UnitOfWork.Habits.Update(habit);
            await UnitOfWork.SaveChangesAsync();

            var response = await Client.PostAsync($"/api/habits/{habit.Id}/restore", null);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            await DbContext.Entry(habit).ReloadAsync();
            habit.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task GetDeletedHabits_ReturnsDeletedHabits()
        {
            await InitializeAsync();
            var activeHabit = await CreateTestHabitAsync(_testUser.Id, "Active");
            var deletedHabit = await CreateTestHabitAsync(_testUser.Id, "Deleted");

            deletedHabit.IsActive = false;
            UnitOfWork.Habits.Update(deletedHabit);
            await UnitOfWork.SaveChangesAsync();

            var response = await Client.GetAsync("/api/habits/deleted");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var habits = await response.Content.ReadFromJsonAsync<List<HabitDto>>();
            habits.Should().HaveCount(1);
            habits!.First().Title.Should().Be("Deleted");
        }
    }
}