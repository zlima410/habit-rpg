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
    public class HabitsControllerBulkAndEdgeCaseTests : IntegrationTestBase
    {
        private string _authToken = string.Empty;
        private User _testUser = null!;

        public HabitsControllerBulkAndEdgeCaseTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        public async Task InitializeAsync()
        {
            _testUser = await CreateTestUserAsync();
            _authToken = await GetAuthTokenAsync(_testUser.Email);
            SetAuthToken(_authToken);
        }

        #region Permanent Delete Tests

        [Fact]
        public async Task PermanentlyDeleteHabit_ValidRequest_ReturnsSuccess()
        {
            await InitializeAsync();
            var habit = await CreateTestHabitAsync(_testUser.Id, "To Permanently Delete");
            await CreateTestCompletionLogAsync(habit.Id);
            await CreateTestCompletionLogAsync(habit.Id, DateTime.UtcNow.AddDays(-1));

            var request = new PermanentDeleteRequest
            {
                ConfirmationText = "DELETE"
            };

            var response = await Client.DeleteAsJsonAsync($"/api/habits/{habit.Id}/permanent", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            result.GetProperty("message").GetString().Should().Contain("permanently deleted");
            result.GetProperty("deletedCompletions").GetInt32().Should().Be(2);

            var getResponse = await Client.GetAsync($"/api/habits/{habit.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task PermanentlyDeleteHabit_InvalidConfirmation_ReturnsBadRequest()
        {
            await InitializeAsync();
            var habit = await CreateTestHabitAsync(_testUser.Id, "To Delete");

            var request = new PermanentDeleteRequest
            {
                ConfirmationText = "WRONG"
            };

            var response = await Client.DeleteAsJsonAsync($"/api/habits/{habit.Id}/permanent", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task PermanentlyDeleteHabit_MissingConfirmation_ReturnsBadRequest()
        {
            await InitializeAsync();
            var habit = await CreateTestHabitAsync(_testUser.Id, "To Delete");

            var request = new PermanentDeleteRequest
            {
                ConfirmationText = ""
            };

            var response = await Client.DeleteAsJsonAsync($"/api/habits/{habit.Id}/permanent", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task PermanentlyDeleteHabit_InvalidHabitId_ReturnsBadRequest()
        {
            await InitializeAsync();

            var request = new PermanentDeleteRequest
            {
                ConfirmationText = "DELETE"
            };

            var response = await Client.DeleteAsJsonAsync("/api/habits/0/permanent", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task PermanentlyDeleteHabit_NonExistentHabit_ReturnsNotFound()
        {
            await InitializeAsync();

            var request = new PermanentDeleteRequest
            {
                ConfirmationText = "DELETE"
            };

            var response = await Client.DeleteAsJsonAsync("/api/habits/99999/permanent", request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task PermanentlyDeleteHabit_DeletesAllCompletionLogs()
        {
            await InitializeAsync();
            var habit = await CreateTestHabitAsync(_testUser.Id, "Habit With Logs");
            
            await CreateTestCompletionLogAsync(habit.Id, DateTime.UtcNow.AddDays(-5));
            await CreateTestCompletionLogAsync(habit.Id, DateTime.UtcNow.AddDays(-3));
            await CreateTestCompletionLogAsync(habit.Id, DateTime.UtcNow.AddDays(-1));

            var completionCountBefore = await UnitOfWork.CompletionLogs
                .CountAsync(cl => cl.HabitId == habit.Id);

            var request = new PermanentDeleteRequest
            {
                ConfirmationText = "DELETE"
            };

            var response = await Client.DeleteAsJsonAsync($"/api/habits/{habit.Id}/permanent", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var completionCountAfter = await UnitOfWork.CompletionLogs
                .CountAsync(cl => cl.HabitId == habit.Id);
            completionCountAfter.Should().Be(0);
            completionCountBefore.Should().Be(3);
        }

        #endregion

        #region Bulk Delete Tests

        [Fact]
        public async Task BulkDeleteHabits_SoftDelete_ReturnsSuccess()
        {
            await InitializeAsync();
            var habit1 = await CreateTestHabitAsync(_testUser.Id, "Habit 1");
            var habit2 = await CreateTestHabitAsync(_testUser.Id, "Habit 2");
            var habit3 = await CreateTestHabitAsync(_testUser.Id, "Habit 3");

            var request = new BulkDeleteRequest
            {
                HabitIds = new List<int> { habit1.Id, habit2.Id },
                IsPermanent = false
            };

            var response = await Client.DeleteAsJsonAsync("/api/habits/bulk", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            result.GetProperty("message").GetString().Should().Contain("deactivated");
            result.GetProperty("deactivatedHabits").GetInt32().Should().Be(2);

            var getResponse1 = await Client.GetAsync($"/api/habits/{habit1.Id}");
            var deletedHabit1 = await getResponse1.Content.ReadFromJsonAsync<HabitDto>();
            deletedHabit1!.IsActive.Should().BeFalse();

            var getResponse2 = await Client.GetAsync($"/api/habits/{habit2.Id}");
            var deletedHabit2 = await getResponse2.Content.ReadFromJsonAsync<HabitDto>();
            deletedHabit2!.IsActive.Should().BeFalse();

            var getResponse3 = await Client.GetAsync($"/api/habits/{habit3.Id}");
            var activeHabit = await getResponse3.Content.ReadFromJsonAsync<HabitDto>();
            activeHabit!.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task BulkDeleteHabits_PermanentDelete_ReturnsSuccess()
        {
            await InitializeAsync();
            var habit1 = await CreateTestHabitAsync(_testUser.Id, "Habit 1");
            var habit2 = await CreateTestHabitAsync(_testUser.Id, "Habit 2");
            await CreateTestCompletionLogAsync(habit1.Id);
            await CreateTestCompletionLogAsync(habit2.Id);

            var request = new BulkDeleteRequest
            {
                HabitIds = new List<int> { habit1.Id, habit2.Id },
                IsPermanent = true,
                ConfirmationText = "DELETE"
            };

            var response = await Client.DeleteAsJsonAsync("/api/habits/bulk", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            result.GetProperty("message").GetString().Should().Contain("permanently");
            result.GetProperty("deletedHabits").GetInt32().Should().Be(2);
            result.GetProperty("deletedCompletions").GetInt32().Should().Be(2);

            var getResponse1 = await Client.GetAsync($"/api/habits/{habit1.Id}");
            getResponse1.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var getResponse2 = await Client.GetAsync($"/api/habits/{habit2.Id}");
            getResponse2.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task BulkDeleteHabits_PermanentDeleteWithoutConfirmation_ReturnsBadRequest()
        {
            await InitializeAsync();
            var habit1 = await CreateTestHabitAsync(_testUser.Id, "Habit 1");

            var request = new BulkDeleteRequest
            {
                HabitIds = new List<int> { habit1.Id },
                IsPermanent = true,
                ConfirmationText = "WRONG"
            };

            var response = await Client.DeleteAsJsonAsync("/api/habits/bulk", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task BulkDeleteHabits_EmptyHabitIds_ReturnsBadRequest()
        {
            await InitializeAsync();

            var request = new BulkDeleteRequest
            {
                HabitIds = new List<int>(),
                IsPermanent = false
            };

            var response = await Client.DeleteAsJsonAsync("/api/habits/bulk", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task BulkDeleteHabits_TooManyHabits_ReturnsBadRequest()
        {
            await InitializeAsync();
            var habitIds = Enumerable.Range(1, 51).ToList();

            var request = new BulkDeleteRequest
            {
                HabitIds = habitIds,
                IsPermanent = false
            };

            var response = await Client.DeleteAsJsonAsync("/api/habits/bulk", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task BulkDeleteHabits_InvalidHabitIds_ReturnsBadRequest()
        {
            // Arrange
            await InitializeAsync();

            var request = new BulkDeleteRequest
            {
                HabitIds = new List<int> { 1, 0, -1 },
                IsPermanent = false
            };

            var response = await Client.DeleteAsJsonAsync("/api/habits/bulk", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task BulkDeleteHabits_NonExistentHabits_ReturnsNotFound()
        {
            await InitializeAsync();

            var request = new BulkDeleteRequest
            {
                HabitIds = new List<int> { 99999, 99998 },
                IsPermanent = false
            };

            var response = await Client.DeleteAsJsonAsync("/api/habits/bulk", request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task BulkDeleteHabits_PartialMatch_ReturnsSuccessWithNotFound()
        {
            await InitializeAsync();
            var habit1 = await CreateTestHabitAsync(_testUser.Id, "Habit 1");
            var habit2 = await CreateTestHabitAsync(_testUser.Id, "Habit 2");

            var request = new BulkDeleteRequest
            {
                HabitIds = new List<int> { habit1.Id, habit2.Id, 99999 },
                IsPermanent = false
            };

            var response = await Client.DeleteAsJsonAsync("/api/habits/bulk", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            result.GetProperty("deactivatedHabits").GetInt32().Should().Be(2);
            var notFoundArray = result.GetProperty("notFound").EnumerateArray().Select(e => e.GetInt32()).ToList();
            notFoundArray.Should().HaveCount(1);
            notFoundArray.Should().Contain(99999);
        }

        [Fact]
        public async Task BulkDeleteHabits_OtherUserHabits_OnlyDeletesOwnHabits()
        {
            await InitializeAsync();
            var otherUser = await CreateTestUserAsync(username: "otheruser", email: "other@example.com");
            var myHabit = await CreateTestHabitAsync(_testUser.Id, "My Habit");
            var otherHabit = await CreateTestHabitAsync(otherUser.Id, "Other Habit");

            var request = new BulkDeleteRequest
            {
                HabitIds = new List<int> { myHabit.Id, otherHabit.Id },
                IsPermanent = false
            };

            var response = await Client.DeleteAsJsonAsync("/api/habits/bulk", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            result.GetProperty("deactivatedHabits").GetInt32().Should().Be(1);
            var notFoundArray = result.GetProperty("notFound").EnumerateArray().Select(e => e.GetInt32()).ToList();
            notFoundArray.Should().Contain(otherHabit.Id);

            var otherUserToken = await GetAuthTokenAsync(otherUser.Email);
            SetAuthToken(otherUserToken);
            var getResponse = await Client.GetAsync($"/api/habits/{otherHabit.Id}");
            var otherHabitResult = await getResponse.Content.ReadFromJsonAsync<HabitDto>();
            otherHabitResult!.IsActive.Should().BeTrue();
        }

        #endregion

        #region Bulk Restore Tests

        [Fact]
        public async Task BulkRestoreHabits_ValidRequest_ReturnsSuccess()
        {
            await InitializeAsync();
            var habit1 = await CreateTestHabitAsync(_testUser.Id, "Habit 1");
            var habit2 = await CreateTestHabitAsync(_testUser.Id, "Habit 2");
            var habit3 = await CreateTestHabitAsync(_testUser.Id, "Habit 3");

            habit1.IsActive = false;
            habit2.IsActive = false;
            UnitOfWork.Habits.UpdateRange(new[] { habit1, habit2 });
            await UnitOfWork.SaveChangesAsync();

            var request = new BulkRestoreRequest
            {
                HabitIds = new List<int> { habit1.Id, habit2.Id }
            };

            var response = await Client.PatchAsJsonAsync("/api/habits/bulk/restore", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            result.GetProperty("message").GetString().Should().Contain("restored");
            result.GetProperty("restoredHabits").GetInt32().Should().Be(2);

            var getResponse1 = await Client.GetAsync($"/api/habits/{habit1.Id}");
            var restoredHabit1 = await getResponse1.Content.ReadFromJsonAsync<HabitDto>();
            restoredHabit1!.IsActive.Should().BeTrue();

            var getResponse2 = await Client.GetAsync($"/api/habits/{habit2.Id}");
            var restoredHabit2 = await getResponse2.Content.ReadFromJsonAsync<HabitDto>();
            restoredHabit2!.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task BulkRestoreHabits_EmptyHabitIds_ReturnsBadRequest()
        {
            await InitializeAsync();

            var request = new BulkRestoreRequest
            {
                HabitIds = new List<int>()
            };

            var response = await Client.PatchAsJsonAsync("/api/habits/bulk/restore", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task BulkRestoreHabits_TooManyHabits_ReturnsBadRequest()
        {
            await InitializeAsync();
            var habitIds = Enumerable.Range(1, 51).ToList();

            var request = new BulkRestoreRequest
            {
                HabitIds = habitIds
            };

            var response = await Client.PatchAsJsonAsync("/api/habits/bulk/restore", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task BulkRestoreHabits_ExceedsMaxLimit_ReturnsBadRequest()
        {
            await InitializeAsync();
            
            for (int i = 0; i < 99; i++)
            {
                await CreateTestHabitAsync(_testUser.Id, $"Habit {i}");
            }

            var deletedHabit1 = await CreateTestHabitAsync(_testUser.Id, "Deleted 1");
            var deletedHabit2 = await CreateTestHabitAsync(_testUser.Id, "Deleted 2");
            deletedHabit1.IsActive = false;
            deletedHabit2.IsActive = false;
            UnitOfWork.Habits.UpdateRange(new[] { deletedHabit1, deletedHabit2 });
            await UnitOfWork.SaveChangesAsync();

            var request = new BulkRestoreRequest
            {
                HabitIds = new List<int> { deletedHabit1.Id, deletedHabit2.Id }
            };

            var response = await Client.PatchAsJsonAsync("/api/habits/bulk/restore", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task BulkRestoreHabits_AlreadyActiveHabits_IgnoresThem()
        {
            await InitializeAsync();
            var activeHabit = await CreateTestHabitAsync(_testUser.Id, "Active Habit");
            var deletedHabit = await CreateTestHabitAsync(_testUser.Id, "Deleted Habit");
            deletedHabit.IsActive = false;
            UnitOfWork.Habits.Update(deletedHabit);
            await UnitOfWork.SaveChangesAsync();

            var request = new BulkRestoreRequest
            {
                HabitIds = new List<int> { activeHabit.Id, deletedHabit.Id }
            };

            var response = await Client.PatchAsJsonAsync("/api/habits/bulk/restore", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            result.GetProperty("restoredHabits").GetInt32().Should().Be(1);
        }

        #endregion

        #region Edge Cases - Max Habits Limit

        [Fact]
        public async Task CreateHabit_MaxHabitsReached_ReturnsBadRequest()
        {
            await InitializeAsync();
            
            for (int i = 0; i < 100; i++)
            {
                await CreateTestHabitAsync(_testUser.Id, $"Habit {i}");
            }

            var request = new CreateHabitRequest
            {
                Title = "Should Fail",
                Frequency = HabitFrequency.Daily,
                Difficulty = HabitDifficulty.Medium
            };

            var response = await Client.PostAsJsonAsync("/api/habits", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var errorContent = await response.Content.ReadAsStringAsync();
            errorContent.Should().Contain("Maximum number of habits");
        }

        [Fact]
        public async Task CreateHabit_JustUnderMaxLimit_Succeeds()
        {
            await InitializeAsync();
            
            for (int i = 0; i < 99; i++)
            {
                await CreateTestHabitAsync(_testUser.Id, $"Habit {i}");
            }

            var request = new CreateHabitRequest
            {
                Title = "Should Succeed",
                Frequency = HabitFrequency.Daily,
                Difficulty = HabitDifficulty.Medium
            };

            var response = await Client.PostAsJsonAsync("/api/habits", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task RestoreHabit_MaxHabitsReached_ReturnsBadRequest()
        {
            await InitializeAsync();
            
            for (int i = 0; i < 100; i++)
            {
                await CreateTestHabitAsync(_testUser.Id, $"Habit {i}");
            }

            var deletedHabit = await CreateTestHabitAsync(_testUser.Id, "Deleted Habit");
            deletedHabit.IsActive = false;
            UnitOfWork.Habits.Update(deletedHabit);
            await UnitOfWork.SaveChangesAsync();

            var response = await Client.PostAsync($"/api/habits/{deletedHabit.Id}/restore", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Edge Cases - Invalid Inputs

        [Fact]
        public async Task GetHabit_InvalidId_ReturnsBadRequest()
        {
            await InitializeAsync();

            var response = await Client.GetAsync("/api/habits/0");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateHabit_InvalidId_ReturnsBadRequest()
        {
            await InitializeAsync();
            var request = new UpdateHabitRequest
            {
                Title = "Updated Title"
            };

            var response = await Client.PatchAsJsonAsync("/api/habits/0", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task DeleteHabit_InvalidId_ReturnsBadRequest()
        {
            await InitializeAsync();

            var response = await Client.DeleteAsync("/api/habits/0");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CompleteHabit_InvalidId_ReturnsBadRequest()
        {
            await InitializeAsync();

            var response = await Client.PostAsync("/api/habits/0/complete", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateHabit_TooLongTitle_ReturnsBadRequest()
        {
            await InitializeAsync();
            var habit = await CreateTestHabitAsync(_testUser.Id, "Test Habit");

            var request = new UpdateHabitRequest
            {
                Title = new string('A', 201)
            };

            var response = await Client.PatchAsJsonAsync($"/api/habits/{habit.Id}", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateHabit_TooLongDescription_ReturnsBadRequest()
        {
            await InitializeAsync();
            var habit = await CreateTestHabitAsync(_testUser.Id, "Test Habit");

            var request = new UpdateHabitRequest
            {
                Description = new string('A', 1001)
            };

            var response = await Client.PatchAsJsonAsync($"/api/habits/{habit.Id}", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateHabit_InactiveHabit_ReturnsBadRequest()
        {
            await InitializeAsync();
            var habit = await CreateTestHabitAsync(_testUser.Id, "Inactive Habit");
            habit.IsActive = false;
            UnitOfWork.Habits.Update(habit);
            await UnitOfWork.SaveChangesAsync();

            var request = new UpdateHabitRequest
            {
                Title = "Updated Title"
            };

            var response = await Client.PatchAsJsonAsync($"/api/habits/{habit.Id}", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task DeleteHabit_AlreadyDeleted_ReturnsBadRequest()
        {
            await InitializeAsync();
            var habit = await CreateTestHabitAsync(_testUser.Id, "Already Deleted");
            habit.IsActive = false;
            UnitOfWork.Habits.Update(habit);
            await UnitOfWork.SaveChangesAsync();

            var response = await Client.DeleteAsync($"/api/habits/{habit.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RestoreHabit_AlreadyActive_ReturnsBadRequest()
        {
            await InitializeAsync();
            var habit = await CreateTestHabitAsync(_testUser.Id, "Active Habit");

            var response = await Client.PostAsync($"/api/habits/{habit.Id}/restore", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Edge Cases - Cross-User Isolation

        [Fact]
        public async Task GetHabit_OtherUserHabit_ReturnsNotFound()
        {
            await InitializeAsync();
            var otherUser = await CreateTestUserAsync(username: "otheruser", email: "other@example.com");
            var otherHabit = await CreateTestHabitAsync(otherUser.Id, "Other User Habit");

            var response = await Client.GetAsync($"/api/habits/{otherHabit.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateHabit_OtherUserHabit_ReturnsNotFound()
        {
            await InitializeAsync();
            var otherUser = await CreateTestUserAsync(username: "otheruser", email: "other@example.com");
            var otherHabit = await CreateTestHabitAsync(otherUser.Id, "Other User Habit");

            var request = new UpdateHabitRequest
            {
                Title = "Hacked Title"
            };

            var response = await Client.PatchAsJsonAsync($"/api/habits/{otherHabit.Id}", request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteHabit_OtherUserHabit_ReturnsNotFound()
        {
            await InitializeAsync();
            var otherUser = await CreateTestUserAsync(username: "otheruser", email: "other@example.com");
            var otherHabit = await CreateTestHabitAsync(otherUser.Id, "Other User Habit");

            var response = await Client.DeleteAsync($"/api/habits/{otherHabit.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CompleteHabit_OtherUserHabit_ReturnsNotFound()
        {
            await InitializeAsync();
            var otherUser = await CreateTestUserAsync(username: "otheruser", email: "other@example.com");
            var otherHabit = await CreateTestHabitAsync(otherUser.Id, "Other User Habit");

            var response = await Client.PostAsync($"/api/habits/{otherHabit.Id}/complete", null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion
    }
}