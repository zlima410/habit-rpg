using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HabitRPG.Api.Models;
using HabitRPG.Api.Services;
using HabitRPG.Api.DTOs;
using HabitRPG.Api.Repositories;
using System.ComponentModel.DataAnnotations;

namespace HabitRPG.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HabitsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGameService _gameService;
        private readonly ILogger<HabitsController> _logger;
        private const int MAX_HABITS_PER_USER = 100;

        public HabitsController(IUnitOfWork unitOfWork, IGameService gameService, ILogger<HabitsController> logger)
        {
            _unitOfWork = unitOfWork;
            _gameService = gameService;
            _logger = logger;
        }

        private Guid? GetCurrentUserId()
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId) || userId == Guid.Empty)
                {
                    return null;
                }
                return userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting user ID from token");
                return null;
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<HabitDto>>> GetHabits([FromQuery] bool includeInactive = false)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Invalid or missing user authentication" });
            }

            try
            {
                var habits = await _unitOfWork.Habits.GetByUserIdAsync(userId.Value, includeInactive);

                var habitDtos = new List<HabitDto>();

                foreach (var habit in habits)
                {
                    try
                    {
                        var canCompleteToday = habit.IsActive && await _gameService.CanCompleteHabitTodayAsync(habit.Id);

                        habitDtos.Add(new HabitDto
                        {
                            Id = habit.Id,
                            Title = habit.Title ?? string.Empty,
                            Description = habit.Description,
                            Frequency = habit.Frequency,
                            Difficulty = habit.Difficulty,
                            CurrentStreak = Math.Max(0, habit.CurrentStreak),
                            BestStreak = Math.Max(0, habit.BestStreak),
                            LastCompletedAt = habit.LastCompletedAt,
                            IsActive = habit.IsActive,
                            CanCompleteToday = canCompleteToday,
                            CreatedAt = habit.CreatedAt
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing habit {HabitId} for user {UserId}", habit.Id, userId);
                    }
                }

                return Ok(habitDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving habits for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving habits" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<HabitDto>> CreateHabit([FromBody] CreateHabitRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid or missing user authentication" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new { message = "Habit title is required" });

            if (request.Title.Length > 200)
                return BadRequest(new { message = "Habit title cannot exceed 200 characters" });

            if (!string.IsNullOrEmpty(request.Description) && request.Description.Length > 1000)
                return BadRequest(new { message = "Habit description cannot exceed 1000 characters" });

            if (!Enum.IsDefined(typeof(HabitFrequency), request.Frequency))
                return BadRequest(new { message = "Invalid habit frequency" });

            if (!Enum.IsDefined(typeof(HabitDifficulty), request.Difficulty))
                return BadRequest(new { message = "Invalid habit difficulty" });

            try
            {
                var activeHabitsCount = await _unitOfWork.Habits.GetActiveCountByUserIdAsync(userId.Value);

                if (activeHabitsCount >= MAX_HABITS_PER_USER)
                    return BadRequest(new { message = $"Maximum number of habits ({MAX_HABITS_PER_USER}) reached" });

                var duplicateExists = await _unitOfWork.Habits.TitleExistsForUserAsync(userId.Value, request.Title.Trim());

                if (duplicateExists)
                    return BadRequest(new { message = "A habit with this title already exists" });

                var habit = new Habit
                {
                    UserId = userId.Value,
                    Title = request.Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                    Frequency = request.Frequency,
                    Difficulty = request.Difficulty,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Habits.AddAsync(habit);
                await _unitOfWork.SaveChangesAsync();

                var canCompleteToday = await _gameService.CanCompleteHabitTodayAsync(habit.Id);

                var habitDto = new HabitDto
                {
                    Id = habit.Id,
                    Title = habit.Title,
                    Description = habit.Description,
                    Frequency = habit.Frequency,
                    Difficulty = habit.Difficulty,
                    CurrentStreak = habit.CurrentStreak,
                    BestStreak = habit.BestStreak,
                    LastCompletedAt = habit.LastCompletedAt,
                    IsActive = habit.IsActive,
                    CanCompleteToday = canCompleteToday,
                    CreatedAt = habit.CreatedAt
                };

                _logger.LogInformation("Created habit {HabitId} for user {UserId}", habit.Id, userId);

                return CreatedAtAction(nameof(GetHabit), new { id = habit.Id }, habitDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating habit for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while creating the habit" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<HabitDto>> GetHabit(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid or missing user authentication" });

            if (id <= 0)
                return BadRequest(new { message = "Invalid habit ID" });

            try
            {
                var habit = await _unitOfWork.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId.Value);

                if (habit == null)
                    return NotFound(new { message = "Habit not found" });

                var canCompleteToday = habit.IsActive && await _gameService.CanCompleteHabitTodayAsync(habit.Id);

                var habitDto = new HabitDto
                {
                    Id = habit.Id,
                    Title = habit.Title ?? string.Empty,
                    Description = habit.Description,
                    Frequency = habit.Frequency,
                    Difficulty = habit.Difficulty,
                    CurrentStreak = Math.Max(0, habit.CurrentStreak),
                    BestStreak = Math.Max(0, habit.BestStreak),
                    LastCompletedAt = habit.LastCompletedAt,
                    IsActive = habit.IsActive,
                    CanCompleteToday = canCompleteToday,
                    CreatedAt = habit.CreatedAt
                };

                return Ok(habitDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving habit {HabitId} for user {UserId}", id, userId);
                return StatusCode(500, new { message = "An error occurred while retrieving the habit" });
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateHabit(int id, [FromBody] UpdateHabitRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid or missing user authentication" });

            if (id <= 0)
                return BadRequest(new { message = "Invalid habit ID" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!string.IsNullOrEmpty(request.Title))
            {
                if (string.IsNullOrWhiteSpace(request.Title))
                    return BadRequest(new { message = "Habit title cannot be empty" });

                if (request.Title.Length > 200)
                    return BadRequest(new { message = "Habit title cannot exceed 200 characters" });
            }

            if (!string.IsNullOrEmpty(request.Description) && request.Description.Length > 1000)
                return BadRequest(new { message = "Habit description cannot exceed 1000 characters" });

            if (request.Frequency.HasValue && !Enum.IsDefined(typeof(HabitFrequency), request.Frequency.Value))
                return BadRequest(new { message = "Invalid habit frequency" });

            if (request.Difficulty.HasValue && !Enum.IsDefined(typeof(HabitDifficulty), request.Difficulty.Value))
                return BadRequest(new { message = "Invalid habit difficulty" });

            try
            {
                var habit = await _unitOfWork.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId.Value);

                if (habit == null)
                    return NotFound(new { message = "Habit not found" });

                if (!habit.IsActive)
                    return BadRequest(new { message = "Cannot update inactive habit" });

                var hasChanges = false;

                if (!string.IsNullOrEmpty(request.Title) && request.Title.Trim() != habit.Title)
                {
                    var duplicateExists = await _unitOfWork.Habits.TitleExistsForUserAsync(userId.Value, request.Title.Trim(), id);

                    if (duplicateExists)
                        return BadRequest(new { message = "A habit with this title already exists" });

                    habit.Title = request.Title.Trim();
                    hasChanges = true;
                }

                if (request.Description != null && request.Description.Trim() != (habit.Description ?? string.Empty))
                {
                    habit.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
                    hasChanges = true;
                }

                if (request.Frequency.HasValue && request.Frequency.Value != habit.Frequency)
                {
                    habit.Frequency = request.Frequency.Value;
                    hasChanges = true;
                }

                if (request.Difficulty.HasValue && request.Difficulty.Value != habit.Difficulty)
                {
                    habit.Difficulty = request.Difficulty.Value;
                    hasChanges = true;
                }

                if (!hasChanges)
                {
                    return NoContent();
                }

                _unitOfWork.Habits.Update(habit);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Updated habit {HabitId} for user {UserId}", id, userId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating habit {HabitId} for user {UserId}", id, userId);
                return StatusCode(500, new { message = "An error occurred while updating the habit" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHabit(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid or missing user authentication" });

            if (id <= 0)
                return BadRequest(new { message = "Invalid habit ID" });

            try
            {
                var habit = await _unitOfWork.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId.Value);

                if (habit == null)
                    return NotFound(new { message = "Habit not found" });

                if (!habit.IsActive)
                    return BadRequest(new { message = "Habit is already deleted" });

                habit.IsActive = false;
                _unitOfWork.Habits.Update(habit);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Soft deleted habit {HabitId} for user {UserId}", id, userId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting habit {HabitId} for user {UserId}", id, userId);
                return StatusCode(500, new { message = "An error occurred while deleting the habit" });
            }
        }

        [HttpDelete("{id}/permanent")]
        public async Task<IActionResult> PermanentlyDeleteHabit(int id, [FromBody] PermanentDeleteRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid or missing user authentication" });

            if (id <= 0)
                return BadRequest(new { message = "Invalid habit ID" });

            if (string.IsNullOrWhiteSpace(request.ConfirmationText) ||
                request.ConfirmationText.Trim().ToUpper() != "DELETE")
                return BadRequest(new { message = "Permanent deletion must be confirmed with 'DELETE'" });

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var habit = await _unitOfWork.Habits.GetByIdWithUserAndCompletionsAsync(id);

                if (habit == null || habit.UserId != userId.Value)
                    return NotFound(new { message = "Habit not found" });

                var completionCount = habit.CompletionLogs?.Count ?? 0;

                if (completionCount > 0)
                {
                    var completionLogs = habit.CompletionLogs!.ToList();
                    _unitOfWork.CompletionLogs.RemoveRange(completionLogs);
                }

                _unitOfWork.Habits.Remove(habit);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Permanently deleted habit {HabitId} and {CompletionCount} completion logs for user {UserId}",
                    id, completionCount, userId);

                return Ok(new
                {
                    message = "Habit permanently deleted",
                    deletedCompletions = completionCount
                });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error permanently deleting habit {HabitId} for user {UserId}", id, userId);
                return StatusCode(500, new { message = "An error occurred while permanently deleting the habit" });
            }
        }

        [HttpDelete("bulk")]
        public async Task<IActionResult> BulkDeleteHabits([FromBody] BulkDeleteRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid or missing user authentication" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!request.HabitIds?.Any() == true)
                return BadRequest(new { message = "At least one habit ID is required" });

            if (request.HabitIds.Count > 50)
                return BadRequest(new { message = "Cannot delete more than 50 habits at once" });

            if (request.HabitIds.Any(id => id <= 0))
                return BadRequest(new { message = "All habit IDs must be valid positive integers" });

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var habits = await _unitOfWork.Habits.GetByIdsForUserAsync(userId.Value, request.HabitIds);

                if (!habits.Any())
                    return NotFound(new { message = "No habits found to delete" });

                var notFoundIds = request.HabitIds.Except(habits.Select(h => h.Id)).ToList();

                if (request.IsPermanent)
                {
                    if (string.IsNullOrWhiteSpace(request.ConfirmationText) ||
                        request.ConfirmationText.Trim().ToUpper() != "DELETE")
                        return BadRequest(new { message = "Permanent bulk deletion must be confirmed with 'DELETE'" });

                    var habitIds = habits.Select(h => h.Id).ToList();
                    var completionLogs = await _unitOfWork.CompletionLogs
                        .FindAsync(cl => habitIds.Contains(cl.HabitId));

                    _unitOfWork.CompletionLogs.RemoveRange(completionLogs);
                    _unitOfWork.Habits.RemoveRange(habits);

                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    _logger.LogInformation("Permanently bulk deleted {HabitCount} habits and {CompletionCount} completion logs for user {UserId}",
                        habits.Count(), completionLogs.Count(), userId);

                    return Ok(new
                    {
                        message = $"Successfully deleted {habits.Count()} habits permanently",
                        deletedHabits = habits.Count(),
                        deletedCompletions = completionLogs.Count(),
                        notFound = notFoundIds
                    });
                }
                else
                {
                    foreach (var habit in habits)
                        habit.IsActive = false;

                    _unitOfWork.Habits.UpdateRange(habits);
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    _logger.LogInformation("Soft bulk deleted {HabitCount} habits for user {UserId}", habits.Count(), userId);

                    return Ok(new
                    {
                        message = $"Successfully deactivated {habits.Count()} habits",
                        deactivatedHabits = habits.Count(),
                        notFound = notFoundIds
                    });
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error bulk deleting habits for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred during bulk deletion" });
            }
        }

        [HttpPost("{id}/complete")]
        public async Task<ActionResult<GameReward>> CompleteHabit(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid or missing user authentication" });

            if (id <= 0)
                return BadRequest(new { message = "Invalid habit ID" });

            try
            {
                var habitExists = await _unitOfWork.Habits.IsActiveForUserAsync(id, userId.Value);

                if (!habitExists)
                {
                    return NotFound(new { message = "Habit not found or inactive" });
                }

                var result = await _gameService.CompleteHabitAsync(userId.Value, id);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to complete habit {HabitId} for user {UserId}: {Message}",
                        id, userId, result.Message);

                    return BadRequest(new { message = result.Message });
                }

                _logger.LogInformation("User {UserId} completed habit {HabitId}, gained {XP} XP",
                    userId, id, result.XpGained);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing habit {HabitId} for user {UserId}", id, userId);
                return StatusCode(500, new { message = "An error occurred while completing the habit" });
            }
        }

        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreHabit(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid or missing user authentication" });

            if (id <= 0)
                return BadRequest(new { message = "Invalid habit ID" });

            try
            {
                var habit = await _unitOfWork.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId.Value);

                if (habit == null)
                    return NotFound(new { message = "Habit not found" });

                if (habit.IsActive)
                    return BadRequest(new { message = "Habit is already active" });

                var activeHabitsCount = await _unitOfWork.Habits.GetActiveCountByUserIdAsync(userId.Value);

                if (activeHabitsCount >= MAX_HABITS_PER_USER)
                {
                    return BadRequest(new { message = $"Maximum number of habits ({MAX_HABITS_PER_USER}) reached" });
                }

                habit.IsActive = true;
                _unitOfWork.Habits.Update(habit);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Restored habit {HabitId} for user {UserId}", id, userId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring habit {HabitId} for user {UserId}", id, userId);
                return StatusCode(500, new { message = "An error occurred while restoring the habit" });
            }
        }

        [HttpPatch("bulk/restore")]
        public async Task<IActionResult> BulkRestoreHabits([FromBody] BulkRestoreRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid or missing user authentication" });

            if (!request.HabitIds?.Any() == true)
                return BadRequest(new { message = "At least one habit ID is required" });

            if (request.HabitIds.Count > 50)
                return BadRequest(new { message = "Cannot restore more than 50 habits at once" });

            try
            {
                var activeHabitsCount = await _unitOfWork.Habits.GetActiveCountByUserIdAsync(userId.Value);

                var habitsToRestore = await _unitOfWork.Habits.GetInactiveByUserIdAsync(userId.Value);
                habitsToRestore = habitsToRestore.Where(h => request.HabitIds.Contains(h.Id)).ToList();

                if (activeHabitsCount + habitsToRestore.Count() > MAX_HABITS_PER_USER)
                    return BadRequest(new { message = $"Restoring these habits would exceed the maximum limit of {MAX_HABITS_PER_USER} active habits" });

                foreach (var habit in habitsToRestore)
                    habit.IsActive = true;

                _unitOfWork.Habits.UpdateRange(habitsToRestore);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Bulk restored {HabitCount} habits for user {UserId}", habitsToRestore.Count(), userId);

                return Ok(new
                {
                    message = $"Successfully restored {habitsToRestore.Count()} habits",
                    restoredHabits = habitsToRestore.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk restoring habits for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred during bulk restoration" });
            }
        }

        [HttpGet("deleted")]
        public async Task<ActionResult<IEnumerable<HabitDto>>> GetDeletedHabits()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid or missing user authentication" });

            try
            {
                var deletedHabits = await _unitOfWork.Habits.GetInactiveByUserIdAsync(userId.Value);

                var habitDtos = deletedHabits.Select(habit => new HabitDto
                {
                    Id = habit.Id,
                    Title = habit.Title ?? string.Empty,
                    Description = habit.Description,
                    Frequency = habit.Frequency,
                    Difficulty = habit.Difficulty,
                    CurrentStreak = Math.Max(0, habit.CurrentStreak),
                    BestStreak = Math.Max(0, habit.BestStreak),
                    LastCompletedAt = habit.LastCompletedAt,
                    IsActive = habit.IsActive,
                    CanCompleteToday = false,
                    CreatedAt = habit.CreatedAt
                }).ToList();

                return Ok(habitDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deleted habits for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving deleted habits" });
            }
        }
    }
}