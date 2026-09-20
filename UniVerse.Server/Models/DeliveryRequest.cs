using System;
using System.Collections.Generic;

namespace UniVerse.Server.Models
{
    public class DeliveryRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string RequesterId { get; set; } = string.Empty;
        public string? RunnerId { get; set; }
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public double TotalEstimatedAmount { get; set; }
        public double DeliveryFee { get; set; } = 30.0;
        public string Status { get; set; } = "pending"; // pending, accepted, picked_up, in_transit, delivered, cancelled
        public string? DeliveryOtp { get; set; }
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
        public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");
    }

    public class RequestItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string RequestId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public string? Notes { get; set; }
        public double EstimatedPrice { get; set; }
    }

    public class DeliveryRequestDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string RequesterId { get; set; } = string.Empty;
        public string RequesterName { get; set; } = string.Empty;
        public string? RequesterPhone { get; set; }
        public string? RunnerId { get; set; }
        public string? RunnerName { get; set; }
        public string? RunnerPhone { get; set; }
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public double TotalEstimatedAmount { get; set; }
        public double DeliveryFee { get; set; }
        public string Status { get; set; } = "pending";
        public string? DeliveryOtp { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
        public List<RequestItem> Items { get; set; } = new();
    }
}
