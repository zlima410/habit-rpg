using HabitRPG.Api.Models;

namespace HabitRPG.Api.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UsernameExistsAsync(string username);
        Task<User?> GetUserWithHabitsAsync(int userId, bool includeInactive = false);
        Task<User?> GetUserWithActiveHabitsAsync(int userId);
    }
}