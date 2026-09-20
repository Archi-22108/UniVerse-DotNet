using System;
using System.Collections.Generic;

namespace UniVerse.Web.Models;

/// <summary>
/// ViewModel encapsulating all data needed for Student Dashboard.
/// Follows ASP.NET Core MVC design pattern.
/// </summary>
public class DashboardViewModel
{
    // Student Identity
    public string DisplayName { get; set; } = "Archi.kumari126697";
    public string Email { get; set; } = "archi.kumari126697@marwadiuniversity.ac.in";
    public string? PhoneNumber { get; set; }
    public string Initial => !string.IsNullOrEmpty(DisplayName) ? DisplayName[0].ToString().ToUpperInvariant() : "A";
    public string UniversityName { get; set; } = "Marwadi University";
    public bool IsVerified { get; set; } = true;

    // Metric Summary Counters
    public int TotalRequests { get; set; } = 0;
    public int ActiveRequests { get; set; } = 0;
    public int CompletedRequests { get; set; } = 0;
    public int CancelledRequests { get; set; } = 0;

    // Percentage Breakdown
    public int CompletedPercentage => TotalRequests > 0 ? (int)Math.Round((double)CompletedRequests / TotalRequests * 100) : 0;
    public int InProgressPercentage => TotalRequests > 0 ? (int)Math.Round((double)ActiveRequests / TotalRequests * 100) : 0;
    public int CancelledPercentage => TotalRequests > 0 ? (int)Math.Round((double)CancelledRequests / TotalRequests * 100) : 0;

    // Delivery Requests
    public List<DeliveryRequest> ActiveDeliveries { get; set; } = new();
    public List<DeliveryRequest> RecentCompleted { get; set; } = new();

    // Activity Feed
    public List<DashboardActivityItem> Activities { get; set; } = new();

    // Overview Analytics Sparkline Data
    public List<string> WeeklyDays { get; set; } = new() { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
    public List<int> WeeklyActivityCounts { get; set; } = new() { 0, 0, 0, 0, 0, 0, 0 };
    public string SelectedTimeRange { get; set; } = "This Week";
}

public class DashboardActivityItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
    public string IconType { get; set; } = "bi-box-seam";
    public string ColorHex { get; set; } = "#00e599";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
