using FluentValidation;
using HabitRPG.Api.DTOs;

namespace HabitRPG.Api.Validators
{
    public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
    {
        public UpdateProfileRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username cannot be empty.")
                .MinimumLength(3).WithMessage("Username must be between 3 and 50 characters.")
                .MaximumLength(50).WithMessage("Username must be between 3 and 50 characters.")
                .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Username can only contain letters, numbers, hyphens, and underscores.")
                .When(x => !string.IsNullOrEmpty(x.Username));
        }
    }
}