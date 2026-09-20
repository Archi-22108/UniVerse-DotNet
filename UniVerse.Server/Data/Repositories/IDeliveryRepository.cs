using System.Collections.Generic;
using System.Threading.Tasks;
using UniVerse.Server.Models;

namespace UniVerse.Server.Data.Repositories
{
    public interface IDeliveryRepository
    {
        Task<List<DeliveryRequestDetailDto>> GetRequestsAsync(string? status = null);
        Task<DeliveryRequestDetailDto?> GetRequestByIdAsync(string id);
        Task<List<DeliveryRequestDetailDto>> GetRequestsByRequesterAsync(string requesterId);
        Task<List<DeliveryRequestDetailDto>> GetRequestsByRunnerAsync(string runnerId);
        Task<string> CreateRequestAsync(DeliveryRequest request, List<RequestItem> items);
        Task<bool> AssignRunnerAsync(string requestId, string runnerId);
        Task<bool> UpdateStatusAsync(string requestId, string status);
        Task<(bool Success, string Message, double Reward)> CompleteDeliveryWithOtpAsync(string requestId, string runnerId, string otp);
    }
}
