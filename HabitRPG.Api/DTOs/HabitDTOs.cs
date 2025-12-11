using System.ComponentModel.DataAnnotations;
using HabitRPG.Api.Models;

namespace HabitRPG.Api.DTOs
{
    public class HabitDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public HabitFrequency Frequency { get; set; }
        public HabitDifficulty Difficulty { get; set; }
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
        public DateTime? LastCompletedAt { get; set; }
        public bool IsActive { get; set; }
        public bool CanCompleteToday { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateHabitRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Frequency is required")]
        public HabitFrequency Frequency { get; set; } = HabitFrequency.Daily;

        [Required(ErrorMessage = "Difficulty is required")]
        public HabitDifficulty Difficulty { get; set; } = HabitDifficulty.Medium;
    }

    public class UpdateHabitRequest
    {
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string? Title { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        public HabitFrequency? Frequency { get; set; }
        public HabitDifficulty? Difficulty { get; set; }
    }

    public class PermanentDeleteRequest
    {
        [Required(ErrorMessage = "Confirmation text is required")]
        public string ConfirmationText { get; set; } = string.Empty;
    }

    public class BulkDeleteRequest
    {
        [Required]
        public List<int> HabitIds { get; set; } = new();

        public bool IsPermanent { get; set; } = false;

        public string? ConfirmationText { get; set; }
    }

    public class BulkRestoreRequest
    {
        [Required]
        public List<int> HabitIds { get; set; } = new();
    }
}