using System;
using System.Collections.Generic;

namespace UniVerse.Web.Models;

/// <summary>
/// DTO representing an item in the recent activity feed on the Analytics page.
/// </summary>
public class ActivityFeedItemDto
{
    public string Title { get; set; } = "Request Created";
    public string PickupLocation { get; set; } = "Hostel Vending Machine";
    public string DropoffLocation { get; set; } = "Hostel A - Room 400";
    public decimal Amount { get; set; } = 5.00m;
    public string RelativeTime { get; set; } = "about 1 hour ago";
    public string Icon { get; set; } = "bi-box-seam";
}

/// <summary>
/// Strongly-typed ViewModel for the Analytics & Reports page.
/// Conforms to university rules and C# OOP design best practices.
/// </summary>
public class AnalyticsViewModel
{
    public string ActiveRange { get; set; } = "7d"; // "7d", "30d", "90d"
    public string RangeLabel { get; set; } = "7d";

    // Top 3 Stat Cards
    public decimal TotalSpent { get; set; } = 10.00m;
    public string TotalSpentFormatted => $"₹{TotalSpent:N2}";
    public int RequestsMade { get; set; } = 2;
    public string WalletStatus { get; set; } = "Active";
    public string SpendingGrowthText { get; set; } = "100% from previous 7 days";
    public string RequestsGrowthText { get; set; } = "100% from previous 7 days";
    public string WalletNote { get; set; } = "Wallet is in good standing";

    // 5 KPI Metrics Strip
    public decimal AverageSpent { get; set; } = 5.00m;
    public decimal HighestCost { get; set; } = 5.00m;
    public decimal LowestCost { get; set; } = 5.00m;
    public int CompletedCount { get; set; } = 0;
    public int CancelledCount { get; set; } = 0;
    public int ActiveCount { get; set; } = 2;
    public int TotalStatusCount => ActiveCount + CompletedCount + CancelledCount;

    // Daily Timeline Data for Spending Trend & Request Activity
    public List<string> DailyLabels { get; set; } = new() { "Sep 15", "Sep 16", "Sep 17", "Sep 18", "Sep 19", "Sep 20", "Sep 21" };
    public List<decimal> DailySpending { get; set; } = new() { 0m, 0m, 0m, 0m, 0m, 10.00m, 0m };
    public List<int> DailyCreated { get; set; } = new() { 0, 0, 0, 0, 0, 2, 0 };
    public List<int> DailyCompleted { get; set; } = new() { 0, 0, 0, 0, 0, 0, 0 };
    public List<int> DailyCancelled { get; set; } = new() { 0, 0, 0, 0, 0, 0, 0 };

    // Recent Activity Feed
    public List<ActivityFeedItemDto> RecentActivities { get; set; } = new();
}
