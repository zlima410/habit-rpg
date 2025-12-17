using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HabitRPG.Api.Models;
using HabitRPG.Api.DTOs;
using HabitRPG.Api.Repositories;

namespace HabitRPG.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUnitOfWork unitOfWork, IConfiguration config, ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _config = config;
            _logger = logger;
        }

        public async Task<AuthResult> RegisterAsync(RegisterRequest request)
        {
            var trimmedUsername = request.Username?.Trim() ?? string.Empty;
            var trimmedEmail = request.Email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedUsername))
                return new AuthResult { Success = false, Message = "Username is required" };

            if (string.IsNullOrWhiteSpace(trimmedEmail))
                return new AuthResult { Success = false, Message = "Email is required" };

            if (string.IsNullOrWhiteSpace(request.Password))
                return new AuthResult { Success = false, Message = "Password is required" };

            if (trimmedUsername.Length < 3 || trimmedUsername.Length > 50)
                return new AuthResult { Success = false, Message = "Username must be between 3 and 50 characters" };

            if (!System.Text.RegularExpressions.Regex.IsMatch(trimmedUsername, @"^[a-zA-Z0-9_-]+$"))
                return new AuthResult { Success = false, Message = "Username can only contain letters, numbers, hyphens, and underscores" };

            if (!IsValidEmail(trimmedEmail))
                return new AuthResult { Success = false, Message = "Please provide a valid email address" };

            if (request.Password.Length < 6)
                return new AuthResult { Success = false, Message = "Password must be at least 6 characters long" };

            if (request.Password.Length > 100)
                return new AuthResult { Success = false, Message = "Password cannot exceed 100 characters" };

            try
            {
                var lowerEmail = trimmedEmail.ToLower();
                if (await _unitOfWork.Users.EmailExistsAsync(lowerEmail))
                    return new AuthResult { Success = false, Message = "An account with this email already exists" };

                if (await _unitOfWork.Users.UsernameExistsAsync(trimmedUsername))
                    return new AuthResult { Success = false, Message = "This username is already taken" };

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = trimmedUsername,
                    Email = lowerEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                var token = GenerateJwtToken(user);

                return new AuthResult
                {
                    Success = true,
                    Message = "Registration successful! Welcome to HabitRPG!",
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Level = user.Level,
                        XP = user.XP,
                        TotalXP = user.TotalXP
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return new AuthResult { Success = false, Message = "An error occurred during registration. Please try again." };
            }
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);

                if (user == null)
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };

                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };

                var token = GenerateJwtToken(user);

                return new AuthResult
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Level = user.Level,
                        XP = user.XP,
                        TotalXP = user.TotalXP
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                return new AuthResult
                {
                    Success = false,
                    Message = "An error occurred during login. Please try again later."
                };
            }
        }

        public string GenerateJwtToken(User user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
            var issuer = jwtSettings["Issuer"]!;
            var audience = jwtSettings["Audience"]!;
            var expirationHours = int.Parse(jwtSettings["ExpirationHours"]!);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("userId", user.Id.ToString()),
                    new Claim("username", user.Username),
                    new Claim(ClaimTypes.Email, user.Email)
                }),
                Expires = DateTime.UtcNow.AddHours(expirationHours),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}