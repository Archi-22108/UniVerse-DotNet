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
}
