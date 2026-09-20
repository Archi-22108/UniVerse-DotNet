using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UniVerse.Server.Models
{
    // ─── Auth DTOs ─────────────────────────────────────────────────────────────

    public class LoginRequestDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequestDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        public string? EnrollmentNumber { get; set; }
        public string Role { get; set; } = "student"; // "student" or "runner"
        public string? HostelName { get; set; }
        public string? RoomNumber { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public UserDto? User { get; set; }
    }

    // ─── Request DTOs ──────────────────────────────────────────────────────────

    public class CreateDeliveryRequestDto
    {
        [Required]
        public string RequesterId { get; set; } = string.Empty;

        [Required]
        public string PickupLocation { get; set; } = string.Empty;

        [Required]
        public string DropoffLocation { get; set; } = string.Empty;

        public string? Instructions { get; set; }
        public double DeliveryFee { get; set; } = 30.0;

        public List<CreateRequestItemDto> Items { get; set; } = new();
    }

    public class CreateRequestItemDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public string? Notes { get; set; }
        public double EstimatedPrice { get; set; }
    }

    public class UpdateDeliveryStatusDto
    {
        [Required]
        public string Status { get; set; } = string.Empty; // "accepted", "picked_up", "in_transit", "delivered", "cancelled"

        public string? RunnerId { get; set; }
        public string? DeliveryOtp { get; set; }
    }

    // ─── Runner DTOs ───────────────────────────────────────────────────────────

    public class ToggleRunnerDutyDto
    {
        [Required]
        public string RunnerId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class AcceptDeliveryDto
    {
        [Required]
        public string RunnerId { get; set; } = string.Empty;
    }

    public class CompleteDeliveryDto
    {
        [Required]
        public string RunnerId { get; set; } = string.Empty;

        [Required]
        public string DeliveryOtp { get; set; } = string.Empty;
    }

    // ─── Marketplace DTOs ──────────────────────────────────────────────────────

    public class CreateListingDto
    {
        [Required]
        public string SellerId { get; set; } = string.Empty;

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string Category { get; set; } = "Books";

        [Required]
        public string Condition { get; set; } = "Good";

        [Range(0, 100000)]
        public double Price { get; set; }

        public double? OriginalPrice { get; set; }
        public bool Negotiable { get; set; } = true;

        [Required]
        public string PickupLocation { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }
    }

    public class CreateOfferDto
    {
        [Required]
        public string ListingId { get; set; } = string.Empty;

        [Required]
        public string BuyerId { get; set; } = string.Empty;

        [Range(1, 100000)]
        public double OfferPrice { get; set; }
    }
}
