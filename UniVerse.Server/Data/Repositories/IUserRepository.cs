using System.Collections.Generic;
using System.Threading.Tasks;
using UniVerse.Server.Models;

namespace UniVerse.Server.Data.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(string id);
        Task<List<User>> GetAllUsersAsync();
        Task<List<User>> GetActiveRunnersAsync();
        Task<int> CreateUserAsync(User user);
        Task<int> ToggleRunnerDutyAsync(string runnerId, bool isActive);
        Task<int> AddRewardBalanceAsync(string userId, double amount);
    }
}
