using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HabitRPG.Api.Data;
using HabitRPG.Api.Services;
using HabitRPG.Api.DTOs;
using System.ComponentModel.DataAnnotations;

namespace HabitRPG.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IGameService _gameService;
        private readonly ILogger<UserController> _logger;

        public UserController(ApplicationDbContext db, IGameService gameService, ILogger<UserController> logger)
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

        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileDto>> GetProfile()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid or missing user authentication" });

            try
            {
                var user = await _db.Users
                    .Include(u => u.Habits.Where(h => h.IsActive))
                    .FirstOrDefaultAsync(u => u.Id == userId.Value);

                if (user == null)
                {
                    _logger.LogWarning("User profile not found for ID {UserId}", userId.Value);
                    return NotFound(new { message = "User profile not found" });
                }

                if (user.Level < 1)
                {
                    _logger.LogWarning("User {UserId} has invalid level {Level}, correcting to 1", userId.Value, user.Level);
                    user.Level = 1;
                    await _db.SaveChangesAsync();
                }

                if (user.TotalXP < 0)
                {
                    _logger.LogWarning("User {UserId} has negative total XP {TotalXP}, correcting to 0", userId.Value, user.TotalXP);
                    user.TotalXP = 0;
                    user.XP = 0;
                    await _db.SaveChangesAsync();
                }

                var calculatedLevel = _gameService.CalculateLevelFromTotalXp(user.TotalXP);
                if (calculatedLevel != user.Level)
                {
                    _logger.LogWarning("User {UserId} level mismatch. Stored: {StoredLevel}, Calculated: {CalculatedLevel}", 
                        userId.Value, user.Level, calculatedLevel);
                    user.Level = calculatedLevel;
                    await _db.SaveChangesAsync();
                }

                var currentLevelXpRequired = _gameService.GetXpRequiredForLevel(user.Level);
                var nextLevelXpRequired = _gameService.GetXpRequiredForLevel(user.Level + 1);
                
                var xpProgress = Math.Max(0, user.TotalXP - currentLevelXpRequired);
                var xpToNextLevel = Math.Max(0, nextLevelXpRequired - user.TotalXP);
                var xpRequiredForNextLevel = nextLevelXpRequired - currentLevelXpRequired;

                var correctCurrentXp = user.TotalXP - currentLevelXpRequired;
                if (user.XP != correctCurrentXp)
                {
                    user.XP = correctCurrentXp;
                    await _db.SaveChangesAsync();
                }

                var activeHabitsCount = user.Habits?.Count ?? 0;

                var habitIds = user.Habits?.Select(h => h.Id).ToList() ?? new List<int>();
                var totalCompletions = 0;

                if (habitIds.Any())
                    totalCompletions = await _db.CompletionLogs
                        .CountAsync(cl => habitIds.Contains(cl.HabitId));

                var longestStreak = 0;
                var currentActiveStreaks = 0;

                if (user.Habits?.Any() == true)
                {
                    longestStreak = user.Habits.Max(h => h.BestStreak);
                    currentActiveStreaks = user.Habits.Count(h => h.CurrentStreak > 0);
                }

                return Ok(new UserProfileDto
                {
                    Id = user.Id,
                    Username = user.Username ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Level = user.Level,
                    XP = user.XP,
                    TotalXP = user.TotalXP,
                    XpToNextLevel = xpToNextLevel,
                    XpProgress = xpProgress,
                    XpRequiredForNextLevel = xpRequiredForNextLevel,
                    ActiveHabitsCount = activeHabitsCount,
                    TotalCompletions = totalCompletions,
                    LongestStreak = longestStreak,
                    CurrentActiveStreaks = currentActiveStreaks,
                    CreatedAt = user.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile for user {UserId}", userId.Value);
                return StatusCode(500, new { message = "An error occurred while retrieving your profile" });
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<UserStatsDto>> GetStats([FromQuery] int days = 30)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid or missing user authentication" });

            if (days < 1 || days > 365)
                return BadRequest(new { message = "Days parameter must be between 1 and 365" });

            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                var startDate = DateTime.UtcNow.AddDays(-days).Date;
                var endDate = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

                var habitIds = await _db.Habits
                    .Where(h => h.UserId == userId.Value)
                    .Select(h => h.Id)
                    .ToListAsync();

                var completions = new List<DateTime>();
                if (habitIds.Any())
                    completions = await _db.CompletionLogs
                        .Where(cl => habitIds.Contains(cl.HabitId) && 
                                    cl.CompletedAt >= startDate && 
                                    cl.CompletedAt <= endDate)
                        .Select(cl => cl.CompletedAt.Date)
                        .ToListAsync();

                var completionsByDay = completions
                    .GroupBy(d => d)
                    .ToDictionary(g => g.Key, g => g.Count());

                var dailyCompletions = new List<(DateTime Date, int Count)>();
                for (var date = startDate; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
                {
                    var count = completionsByDay.GetValueOrDefault(date, 0);
                    dailyCompletions.Add((date, count));
                }

                var currentStreak = CalculateCurrentStreak(dailyCompletions);
                var longestStreakInPeriod = CalculateLongestStreak(dailyCompletions);

                return Ok(new UserStatsDto
                {
                    TotalCompletions = completions.Count,
                    CompletionRate = habitIds.Any() ? Math.Round((double)completions.Count / (days * habitIds.Count) * 100, 1) : 0,
                    CurrentStreak = currentStreak,
                    LongestStreakInPeriod = longestStreakInPeriod,
                    AverageCompletionsPerDay = Math.Round((double)completions.Count / days, 1),
                    DailyCompletions = dailyCompletions.ToDictionary(dc => dc.Date.ToString("yyyy-MM-dd"), dc => dc.Count),
                    PeriodStart = startDate,
                    PeriodEnd = DateTime.UtcNow.Date
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stats for user {UserId}", userId.Value);
                return StatusCode(500, new { message = "An error occurred while retrieving your statistics" });
            }
        }

        [HttpPatch("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid or missing user authentication" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!string.IsNullOrEmpty(request.Username))
            {
                if (string.IsNullOrWhiteSpace(request.Username))
                    return BadRequest(new { message = "Username cannot be empty" });

                if (request.Username.Length < 3 || request.Username.Length > 50)
                    return BadRequest(new { message = "Username must be between 3 and 50 characters" });

                if (!System.Text.RegularExpressions.Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_-]+$"))
                    return BadRequest(new { message = "Username can only contain letters, numbers, hyphens, and underscores" });
            }

            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                var hasChanges = false;

                if (!string.IsNullOrEmpty(request.Username) && request.Username.Trim() != user.Username)
                {
                    var usernameExists = await _db.Users
                        .AnyAsync(u => u.Id != userId.Value && u.Username.ToLower() == request.Username.Trim().ToLower());

                    if (usernameExists)
                        return BadRequest(new { message = "Username is already taken" });

                    user.Username = request.Username.Trim();
                    hasChanges = true;
                }

                if (!hasChanges)
                    return NoContent(); // No changes to save

                await _db.SaveChangesAsync();

                _logger.LogInformation("Updated profile for user {UserId}", userId.Value);

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating profile for user {UserId}", userId.Value);
                return StatusCode(500, new { message = "An error occurred while updating your profile" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId.Value);
                return StatusCode(500, new { message = "An unexpected error occurred" });
            }
        }

        private static int CalculateCurrentStreak(List<(DateTime Date, int Count)> dailyCompletions)
        {
            var streak = 0;
            var today = DateTime.UtcNow.Date;

            for (var i = dailyCompletions.Count - 1; i >= 0; i--)
            {
                var completion = dailyCompletions[i];
                var expectedDate = today.AddDays(-(dailyCompletions.Count - 1 - i));

                if (completion.Date == expectedDate && completion.Count > 0)
                {
                    streak++;
                }
                else if (completion.Date == expectedDate)
                {
                    break;
                }
            }

            return streak;
        }

        private static int CalculateLongestStreak(List<(DateTime Date, int Count)> dailyCompletions)
        {
            var maxStreak = 0;
            var currentStreak = 0;

            foreach (var completion in dailyCompletions)
            {
                if (completion.Count > 0)
                {
                    currentStreak++;
                    maxStreak = Math.Max(maxStreak, currentStreak);
                }
                else
                    currentStreak = 0;
            }

            return maxStreak;
        }
    }
}