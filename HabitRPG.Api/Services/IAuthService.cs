using System.ComponentModel.DataAnnotations;
using HabitRPG.Api.Models;
using HabitRPG.Api.DTOs;

namespace HabitRPG.Api.Services
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(RegisterRequest request);
        Task<AuthResult> LoginAsync(LoginRequest request);
        string GenerateJwtToken(User user);
    }
}