using System.Collections.Generic;
using UniVerse.Web.Models;

namespace UniVerse.Web.Data;

/// <summary>
/// Data Access Contract demonstrating C# OOP Abstraction & Interfaces.
/// Injected into Controllers via ASP.NET Core Dependency Injection.
/// </summary>
public interface IAdoNetDbHelper
{
    void InitializeDatabase();
    User? ValidateUser(string email, string password);
    User? GetUserByEmail(string email);
    bool RegisterUser(string fullName, string email, string password, out string errorMessage);
    List<Product> GetFeaturedProducts(int limit = 6);
    HomeViewModel GetHomeMetrics();
    DashboardViewModel GetStudentDashboardData(string studentEmail);
    List<Product> GetAllProducts(string? category = null);
    int CreateDeliveryRequest(DeliveryRequest request);
    DeliveryRequest? GetDeliveryRequestById(int id);
    DeliveryRequest? GetLatestDeliveryRequest(string? studentName = null);
    List<DeliveryRequest> GetStudentDeliveryRequests(string? studentName = null);
    List<DeliveryRequest> GetAvailableRunnerOrders();
    bool AcceptDeliveryOrder(int requestId, string runnerName);
    bool UpdateOrderStatus(int requestId, string newStatus);
    decimal GetRunnerTotalEarnings(string runnerName);
    List<MarketplaceItem> GetMarketplaceItems(string? category, string? search, string? sort, string? viewFilter, string currentUser);
    int CreateMarketplaceItem(MarketplaceItem item);
    bool ToggleSaveMarketplaceItem(int itemId);
    bool MarkMarketplaceItemSold(int itemId);
    bool DeleteMarketplaceItem(int itemId);
    AnalyticsViewModel GetAnalyticsData(string? studentName = null, string range = "7d");
    ProfileViewModel GetUserProfile(string email);
    bool UpdateUserProfile(ProfileViewModel profile);
    bool UpdateUserHostelInfo(string email, string hostelBlock, string roomNumber);
    bool UpdateUserPassword(string email, string newPassword);
    WalletViewModel GetWalletData(string email);
    bool TopUpWallet(string email, decimal amount, string paymentMethod);
    bool WithdrawWallet(string email, decimal amount, string upiId);
}
