using System;
using System.Collections.Generic;

namespace UniVerse.Web.Models;

public class WalletTransactionItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = "Credit"; // "Credit" or "Debit"
    public string Category { get; set; } = "Topup"; // "Topup", "RunnerReward", "DeliveryPayment", "Withdrawal"
    public string ReferenceId { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string FormattedTime => CreatedAt.ToString("MMM dd, yyyy · hh:mm tt");
}

public class WalletViewModel
{
    public string StudentEmail { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string Initial { get; set; } = "A";
    public string? ProfilePictureUrl { get; set; }
    public decimal Balance { get; set; } = 250.00m;
    public decimal TotalEarned { get; set; } = 155.00m;
    public decimal TotalSpent { get; set; } = 180.00m;
    public int TotalDeliveriesDone { get; set; } = 6;
    public string LinkedUpiId { get; set; } = "archi.kumari@mu-upi";
    public List<WalletTransactionItem> Transactions { get; set; } = new();
}
