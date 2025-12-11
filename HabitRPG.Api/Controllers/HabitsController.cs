using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HabitRPG.Api.Data;
using HabitRPG.Api.Models;
using HabitRPG.Api.Services;
using HabitRPG.Api.DTOs;
using System.ComponentModel.DataAnnotations;

namespace HabitRPG.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HabitsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IGameService _gameService;
        private readonly ILogger<HabitsController> _logger;
        private const int MAX_HABITS_PER_USER = 100;

        public HabitsController(ApplicationDbContext db, IGameService gameService, ILogger<HabitsController> logger)
        {
            _db = db;
            _gameService = gameService;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId) || userId <= 0)
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
                var query = _db.Habits.Where(h => h.UserId == userId.Value);

                if (!includeInactive)
                {
                    query = query.Where(h => h.IsActive);
                }

                var habits = await query
                    .OrderByDescending(h => h.CreatedAt)
                    .Take(1000)
                    .ToListAsync();

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
                var activeHabitsCount = await _db.Habits
                    .CountAsync(h => h.UserId == userId.Value && h.IsActive);

                if (activeHabitsCount >= MAX_HABITS_PER_USER)
                    return BadRequest(new { message = $"Maximum number of habits ({MAX_HABITS_PER_USER}) reached" });

                var duplicateExists = await _db.Habits
                    .AnyAsync(h => h.UserId == userId.Value &&
                                  h.IsActive &&
                                  h.Title.ToLower() == request.Title.Trim().ToLower());

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

                _db.Habits.Add(habit);
                await _db.SaveChangesAsync();

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
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating habit for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while creating the habit" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating habit for user {UserId}", userId);
                return StatusCode(500, new { message = "An unexpected error occurred" });
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
                var habit = await _db.Habits
                    .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId.Value);

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
                var habit = await _db.Habits
                    .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId.Value);

                if (habit == null)
                    return NotFound(new { message = "Habit not found" });

                if (!habit.IsActive)
                    return BadRequest(new { message = "Cannot update inactive habit" });

                var originalTitle = habit.Title;
                var hasChanges = false;

                if (!string.IsNullOrEmpty(request.Title) && request.Title.Trim() != habit.Title)
                {
                    var duplicateExists = await _db.Habits
                        .AnyAsync(h => h.UserId == userId.Value &&
                                      h.IsActive &&
                                      h.Id != id &&
                                      h.Title.ToLower() == request.Title.Trim().ToLower());

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

                await _db.SaveChangesAsync();

                _logger.LogInformation("Updated habit {HabitId} for user {UserId}", id, userId);

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating habit {HabitId} for user {UserId}", id, userId);
                return StatusCode(500, new { message = "An error occurred while updating the habit" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating habit {HabitId} for user {UserId}", id, userId);
                return StatusCode(500, new { message = "An unexpected error occurred" });
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
                var habit = await _db.Habits
                    .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId.Value);

                if (habit == null)
                    return NotFound(new { message = "Habit not found" });

                if (!habit.IsActive)
                    return BadRequest(new { message = "Habit is already deleted" });

                habit.IsActive = false;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Soft deleted habit {HabitId} for user {UserId}", id, userId);

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting habit {HabitId} for user {UserId}", id, userId);
                return StatusCode(500, new { message = "An error occurred while deleting the habit" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting habit {HabitId} for user {UserId}", id, userId);
                return StatusCode(500, new { message = "An unexpected error occurred" });
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
                using var transaction = await _db.Database.BeginTransactionAsync();

                var habit = await _db.Habits
                    .Include(h => h.CompletionLogs)
                    .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId.Value);

                if (habit == null)
                    return NotFound(new { message = "Habit not found" });

                if (habit.CompletionLogs.Any())
                    _db.CompletionLogs.RemoveRange(habit.CompletionLogs);

                _db.Habits.Remove(habit);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Permanently deleted habit {HabitId} and {CompletionCount} completion logs for user {UserId}",
                    id, habit.CompletionLogs.Count, userId);

                return Ok(new
                {
                    message = "Habit permanently deleted",
                    deletedCompletions = habit.CompletionLogs.Count
                });
            }
            catch (Exception ex)
            {
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
                using var transaction = await _db.Database.BeginTransactionAsync();

                var habits = await _db.Habits
                    .Where(h => request.HabitIds.Contains(h.Id) && h.UserId == userId.Value)
                    .ToListAsync();

                if (!habits.Any())
                    return NotFound(new { message = "No habits found to delete" });

                var notFoundIds = request.HabitIds.Except(habits.Select(h => h.Id)).ToList();

                if (request.IsPermanent)
                {
                    if (string.IsNullOrWhiteSpace(request.ConfirmationText) ||
                        request.ConfirmationText.Trim().ToUpper() != "DELETE")
                        return BadRequest(new { message = "Permanent bulk deletion must be confirmed with 'DELETE'" });

                    var habitIds = habits.Select(h => h.Id).ToList();
                    var completionLogs = await _db.CompletionLogs
                        .Where(cl => habitIds.Contains(cl.HabitId))
                        .ToListAsync();

                    _db.CompletionLogs.RemoveRange(completionLogs);
                    _db.Habits.RemoveRange(habits);

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Permanently bulk deleted {HabitCount} habits and {CompletionCount} completion logs for user {UserId}",
                        habits.Count, completionLogs.Count, userId);

                    return Ok(new
                    {
                        message = $"Successfully deleted {habits.Count} habits permanently",
                        deletedHabits = habits.Count,
                        deletedCompletions = completionLogs.Count,
                        notFound = notFoundIds
                    });
                }
                else
                {
                    foreach (var habit in habits)
                        habit.IsActive = false;

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Soft bulk deleted {HabitCount} habits for user {UserId}", habits.Count, userId);

                    return Ok(new
                    {
                        message = $"Successfully deactivated {habits.Count} habits",
                        deactivatedHabits = habits.Count,
                        notFound = notFoundIds
                    });
                }
            }
            catch (Exception ex)
            {
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
                var habitExists = await _db.Habits
                    .AnyAsync(h => h.Id == id && h.UserId == userId.Value && h.IsActive);

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
                var habit = await _db.Habits
                    .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId.Value);

                if (habit == null)
                    return NotFound(new { message = "Habit not found" });

                if (habit.IsActive)
                    return BadRequest(new { message = "Habit is already active" });

                var activeHabitsCount = await _db.Habits
                    .CountAsync(h => h.UserId == userId.Value && h.IsActive);

                if (activeHabitsCount >= MAX_HABITS_PER_USER)
                {
                    return BadRequest(new { message = $"Maximum number of habits ({MAX_HABITS_PER_USER}) reached" });
                }

                habit.IsActive = true;
                await _db.SaveChangesAsync();

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
                var activeHabitsCount = await _db.Habits
                    .CountAsync(h => h.UserId == userId.Value && h.IsActive);

                var habitsToRestore = await _db.Habits
                    .Where(h => request.HabitIds.Contains(h.Id) && h.UserId == userId.Value && !h.IsActive)
                    .ToListAsync();

                if (activeHabitsCount + habitsToRestore.Count > MAX_HABITS_PER_USER)
                    return BadRequest(new { message = $"Restoring these habits would exceed the maximum limit of {MAX_HABITS_PER_USER} active habits" });

                foreach (var habit in habitsToRestore)
                    habit.IsActive = true;

                await _db.SaveChangesAsync();

                _logger.LogInformation("Bulk restored {HabitCount} habits for user {UserId}", habitsToRestore.Count, userId);

                return Ok(new
                {
                    message = $"Successfully restored {habitsToRestore.Count} habits",
                    restoredHabits = habitsToRestore.Count
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
                var deletedHabits = await _db.Habits
                    .Where(h => h.UserId == userId.Value && !h.IsActive)
                    .OrderByDescending(h => h.CreatedAt)
                    .Take(100)
                    .ToListAsync();

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