using FluentValidation;
using HabitRPG.Api.DTOs;
using HabitRPG.Api.Models;

namespace HabitRPG.Api.Validators
{
    public class CreateHabitRequestValidator : AbstractValidator<CreateHabitRequest>
    {
        public CreateHabitRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Habit title is required.")
                .MaximumLength(200).WithMessage("Habit title cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Habit description cannot exceed 1000 characters.")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Frequency)
                .IsInEnum().WithMessage("Invalid habit frequency.");

            RuleFor(x => x.Difficulty)
                .IsInEnum().WithMessage("Invalid habit difficulty.");
        }
    }

    public class UpdateHabitRequestValidator : AbstractValidator<UpdateHabitRequest>
    {
        public UpdateHabitRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Habit title cannot be empty.")
                .MaximumLength(200).WithMessage("Habit title cannot exceed 200 characters.")
                .When(x => !string.IsNullOrEmpty(x.Title));

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Habit description cannot exceed 1000 characters.")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Frequency)
                .IsInEnum().WithMessage("Invalid habit frequency.")
                .When(x => x.Frequency.HasValue);

            RuleFor(x => x.Difficulty)
                .IsInEnum().WithMessage("Invalid habit difficulty.")
                .When(x => x.Difficulty.HasValue);
        }
    }

    public class PermanentDeleteRequestValidator : AbstractValidator<PermanentDeleteRequest>
    {
        public PermanentDeleteRequestValidator()
        {
            RuleFor(x => x.ConfirmationText)
                .NotEmpty().WithMessage("Confirmation text is required.")
                .Must(x => x?.Trim().ToUpper() == "DELETE")
                .WithMessage("Permanent deletion must be confirmed with 'DELETE'.");
        }
    }

    public class BulkDeleteRequestValidator : AbstractValidator<BulkDeleteRequest>
    {
        public BulkDeleteRequestValidator()
        {
            RuleFor(x => x.HabitIds)
                .NotEmpty().WithMessage("At least one habit ID is required.")
                .Must(x => x != null && x.Any()).WithMessage("At least one habit ID is required.")
                .Must(x => x != null && x.Count <= 50).WithMessage("Cannot delete more than 50 habits at once.")
                .Must(x => x != null && x.All(id => id > 0)).WithMessage("All habit IDs must be valid positive integers.");

            RuleFor(x => x.ConfirmationText)
                .NotEmpty().WithMessage("Permanent bulk deletion must be confirmed with 'DELETE'.")
                .Must(x => x?.Trim().ToUpper() == "DELETE")
                .WithMessage("Permanent bulk deletion must be confirmed with 'DELETE'.")
                .When(x => x.IsPermanent);
        }
    }

    public class BulkRestoreRequestValidator : AbstractValidator<BulkRestoreRequest>
    {
        public BulkRestoreRequestValidator()
        {
            RuleFor(x => x.HabitIds)
                .NotEmpty().WithMessage("At least one habit ID is required.")
                .Must(x => x != null && x.Any()).WithMessage("At least one habit ID is required.")
                .Must(x => x != null && x.Count <= 50).WithMessage("Cannot restore more than 50 habits at once.");
        }
    }
}