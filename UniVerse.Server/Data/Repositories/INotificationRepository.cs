using System.Collections.Generic;
using System.Threading.Tasks;
using UniVerse.Server.Models;

namespace UniVerse.Server.Data.Repositories
{
    public interface INotificationRepository
    {
        Task<List<Notification>> GetByUserIdAsync(string userId, int limit = 30);
        Task<int> GetUnreadCountAsync(string userId);
        Task<int> MarkAsReadAsync(string id, string userId);
        Task<int> MarkAllAsReadAsync(string userId);
        Task<int> DeleteAsync(string id, string userId);
        Task<int> ClearAllAsync(string userId);
        Task<int> CreateAsync(Notification notification);
    }
}
