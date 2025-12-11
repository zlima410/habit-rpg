using Microsoft.AspNetCore.Mvc;
using HabitRPG.Api.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using HabitRPG.Api.DTOs;

namespace HabitRPG.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            _logger.LogInformation("Attempting to register new user.");

            try
            {
                if (request == null)
                    return Problem(
                        statusCode: 400,
                        title: "Invalid Request",
                        detail: "Request body is required.",
                        type: "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                    );

                if (!ModelState.IsValid)
                    return ValidationProblem(
                        title: "Validation Failed",
                        detail: "One or more validation errors occurred.",
                        statusCode: 400
                    );

                var validationResult = ValidateRegisterRequest(request);
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _authService.RegisterAsync(request);

                if (!result.Success)
                {
                    var statusCode = result.Message.Contains("already exists") || result.Message.Contains("already taken")
                        ? 409 : 400;

                    return Problem(
                        statusCode: statusCode,
                        title: "Registration Failed",
                        detail: result.Message,
                        type: statusCode == 409
                            ? "https://tools.ietf.org/html/rfc9110#section-15.5.10"
                            : "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                    );
                }

                _logger.LogInformation($"User: {result.User}, registered successfully.");
                return Ok(new
                {
                    message = result.Message,
                    token = result.Token,
                    user = result.User
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during registration");
                
                return Problem(
                    statusCode: 500,
                    title: "Database Error",
                    detail: "A database error occurred while processing your request.",
                    type: "https://tools.ietf.org/html/rfc9110#section-15.6.1"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during user registration");

                return Problem(
                    statusCode: 500,
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred while processing your request.",
                    type: "https://tools.ietf.org/html/rfc9110#section-15.6.1"
                );
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("Attempting user login.");

            try
            {
                if (request == null)
                    return Problem(
                        statusCode: 400,
                        title: "Invalid Request",
                        detail: "Request body is required.",
                        type: "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                    );

                if (!ModelState.IsValid)
                    return ValidationProblem(
                        title: "Validation Failed",
                        detail: "One or more validation errors occurred.",
                        statusCode: 400
                    );

                var validationResult = ValidateLoginRequest(request);
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _authService.LoginAsync(request);

                if (!result.Success)
                    return Problem(
                        statusCode: 401,
                        title: "Authentication Failed",
                        detail: result.Message,
                        type: "https://tools.ietf.org/html/rfc9110#section-15.5.2"
                    );

                return Ok(new
                {
                    message = result.Message,
                    token = result.Token,
                    user = result.User
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during user login");

                return Problem(
                    statusCode: 500,
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred while processing your request.",
                    type: "https://tools.ietf.org/html/rfc9110#section-15.6.1"
                );
            }
        }

        private IActionResult? ValidateRegisterRequest(RegisterRequest request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Username))
                errors.Add("Username is required.");
            else if (request.Username.Length < 3)
                errors.Add("Username must be at least 3 characters long.");
            else if (request.Username.Length > 50)
                errors.Add("Username cannot exceed 50 characters.");
            else if (!IsValidUsername(request.Username))
                errors.Add("Username can only contain letters, numbers, and underscores.");

            if (string.IsNullOrWhiteSpace(request.Email))
                errors.Add("Email is required.");
            else if (!new EmailAddressAttribute().IsValid(request.Email))
                errors.Add("Please provide a valid email address.");

            if (string.IsNullOrWhiteSpace(request.Password))
                errors.Add("Password is required.");
            else if (request.Password.Length < 6)
                errors.Add("Password must be at least 6 characters long.");
            else if (request.Password.Length > 100)
                errors.Add("Password cannot exceed 100 characters");
            else if (!IsValidPassword(request.Password))
                errors.Add("Password must contain at least one letter and one number.");

            if (errors.Any())
            {
                var problemDetails = new ValidationProblemDetails
                {
                    Title = "Validation Failed",
                    Detail = "One or more validatoin errors occurred.",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                };

                foreach (var error in errors)
                    problemDetails.Errors.Add("ValidationError", new[] { error });

                return new BadRequestObjectResult(problemDetails);
            }

            return null;
        }

        private IActionResult? ValidateLoginRequest(LoginRequest request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Email))
                errors.Add("Email is required.");
            else if (!new EmailAddressAttribute().IsValid(request.Email))
                errors.Add("Please provide a valid email address.");

            if (string.IsNullOrWhiteSpace(request.Password))
                errors.Add("Password is required.");

            if (errors.Any())
            {
                var problemDetails = new ValidationProblemDetails
                {
                    Title = "Validation Failed",
                    Detail = "One or more validation errors occurred.",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                };

                foreach (var error in errors)
                    problemDetails.Errors.Add("ValidationError", new[] { error });

                return new BadRequestObjectResult(problemDetails);
            }

            return null;
        }

        private static bool IsValidUsername(string username)
        {
            return username.All(c => char.IsLetterOrDigit(c) || c == '_');
        }

        private static bool IsValidPassword(string password)
        {
            return password.Any(char.IsLetter) && password.Any(char.IsDigit);
        }
    }
}