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
    public class UserControllerTests : IntegrationTestBase
    {
        private string _authToken = string.Empty;
        private User _testUser = null!;

        public UserControllerTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        public async Task InitializeAsync()
        {
            _testUser = await CreateTestUserAsync();
            _authToken = await GetAuthTokenAsync(_testUser.Email);
            SetAuthToken(_authToken);
        }

        [Fact]
        public async Task GetProfile_Unauthenticated_ReturnsUnauthorized()
        {
            Client.DefaultRequestHeaders.Authorization = null;

            var response = await Client.GetAsync("/api/user/profile");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetProfile_Authenticated_ReturnsProfile()
        {
            await InitializeAsync();

            var response = await Client.GetAsync("/api/user/profile");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var profile = await response.Content.ReadFromJsonAsync<JsonElement>();
            profile.GetProperty("username").GetString().Should().Be(_testUser.Username);
            profile.GetProperty("email").GetString().Should().Be(_testUser.Email);
            profile.GetProperty("level").GetInt32().Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetProfile_WithHabits_ReturnsCorrectCount()
        {
            await InitializeAsync();
            await CreateTestHabitAsync(_testUser.Id, "Habit 1");
            await CreateTestHabitAsync(_testUser.Id, "Habit 2");

            var response = await Client.GetAsync("/api/user/profile");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var profile = await response.Content.ReadFromJsonAsync<JsonElement>();
            profile.GetProperty("activeHabitsCount").GetInt32().Should().Be(2);
        }

        [Fact]
        public async Task GetStats_ValidRequest_ReturnsStats()
        {
            await InitializeAsync();
            var habit = await CreateTestHabitAsync(_testUser.Id, "Stats Habit");
            await CreateTestCompletionLogAsync(habit.Id, DateTime.UtcNow.AddDays(-1));
            await CreateTestCompletionLogAsync(habit.Id, DateTime.UtcNow);

            var response = await Client.GetAsync("/api/user/stats?days=30");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var stats = await response.Content.ReadFromJsonAsync<JsonElement>();
            stats.GetProperty("totalCompletions").GetInt32().Should().BeGreaterThan(0);
            stats.GetProperty("dailyCompletions").GetPropertyCount().Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetStats_InvalidDays_ReturnsBadRequest()
        {
            await InitializeAsync();

            var response = await Client.GetAsync("/api/user/stats?days=500");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateProfile_ValidRequest_ReturnsNoContent()
        {
            await InitializeAsync();
            var request = new UpdateProfileRequest
            {
                Username = "newusername"
            };

            var response = await Client.PatchAsJsonAsync("/api/user/profile", request);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify update
            var getResponse = await Client.GetAsync("/api/user/profile");
            var profile = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
            profile.GetProperty("username").GetString().Should().Be("newusername");
        }

        [Fact]
        public async Task UpdateProfile_InvalidUsername_ReturnsBadRequest()
        {
            await InitializeAsync();
            var request = new UpdateProfileRequest
            {
                Username = "ab"
            };

            var response = await Client.PatchAsJsonAsync("/api/user/profile", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateProfile_DuplicateUsername_ReturnsBadRequest()
        {
            await InitializeAsync();
            await CreateTestUserAsync(username: "takenusername");

            var request = new UpdateProfileRequest
            {
                Username = "takenusername"
            };

            var response = await Client.PatchAsJsonAsync("/api/user/profile", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}