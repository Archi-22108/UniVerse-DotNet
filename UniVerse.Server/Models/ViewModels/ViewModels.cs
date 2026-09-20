using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UniVerse.Server.Models.ViewModels
{
    // ─── Account ViewModels ──────────────────────────────────────────────────

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Student email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Student Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; } = true;

        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Student email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [Display(Name = "University Email (@marwadiuniversity.ac.in)")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Enrollment Number")]
        public string? EnrollmentNumber { get; set; }

        [Required(ErrorMessage = "Please select your primary campus role.")]
        [Display(Name = "Role")]
        public string Role { get; set; } = "student"; // "student" or "runner"

        [Required(ErrorMessage = "Hostel building is required.")]
        [Display(Name = "Hostel Building")]
        public string HostelName { get; set; } = "Hostel D";

        [Required(ErrorMessage = "Room number is required.")]
        [Display(Name = "Room Number")]
        public string RoomNumber { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Contact Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ─── Delivery ViewModels ─────────────────────────────────────────────────

    public class DeliveryCreateViewModel
    {
        [Required(ErrorMessage = "Pickup location is required.")]
        [Display(Name = "Pickup Spot (Canteen, Stationery, Gate)")]
        public string PickupLocation { get; set; } = "Marwadi Central Canteen";

        [Required(ErrorMessage = "Drop-off hostel is required.")]
        [Display(Name = "Drop-off Location")]
        public string DropoffLocation { get; set; } = "Hostel D, Room 304";

        [Display(Name = "Special Instructions for Runner")]
        public string? Instructions { get; set; }

        [Range(10, 500, ErrorMessage = "Delivery fee tip must be between ₹10 and ₹500.")]
        [Display(Name = "Runner Tip / Delivery Fee (₹)")]
        public double DeliveryFee { get; set; } = 30.0;

        [Required(ErrorMessage = "Item name is required.")]
        [Display(Name = "Items Ordered (e.g. Samosa, Cold Coffee, Drawing Sheet)")]
        public string ItemNames { get; set; } = string.Empty;

        [Range(1, 20, ErrorMessage = "Quantity must be between 1 and 20.")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; } = 1;

        [Range(0, 10000, ErrorMessage = "Estimated price must be valid.")]
        [Display(Name = "Total Estimated Amount (₹)")]
        public double EstimatedAmount { get; set; } = 100.0;
    }

    public class RunnerHubViewModel
    {
        public User CurrentRunner { get; set; } = new();
        public List<DeliveryRequestDetailDto> PendingDeliveries { get; set; } = new();
        public List<DeliveryRequestDetailDto> MyActiveDeliveries { get; set; } = new();
    }

    // ─── Marketplace ViewModels ──────────────────────────────────────────────

    public class MarketplaceCreateViewModel
    {
        [Required(ErrorMessage = "Listing title is required.")]
        [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
        [Display(Name = "Item Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a category.")]
        public string Category { get; set; } = "Books";

        [Required(ErrorMessage = "Please specify item condition.")]
        public string Condition { get; set; } = "Like New";

        [Required(ErrorMessage = "Price is required.")]
        [Range(1, 100000, ErrorMessage = "Price must be greater than ₹0.")]
        [Display(Name = "Selling Price (₹)")]
        public double Price { get; set; }

        [Display(Name = "Original Purchase Price / MRP (₹)")]
        public double? OriginalPrice { get; set; }

        [Display(Name = "Price is Negotiable")]
        public bool Negotiable { get; set; } = true;

        [Required(ErrorMessage = "Campus meetup location is required.")]
        [Display(Name = "Pickup / Exchange Location on Campus")]
        public string PickupLocation { get; set; } = "Central Library or Hostel D";

        [Display(Name = "Detailed Description")]
        public string? Description { get; set; }

        [Display(Name = "Image URL (Optional)")]
        public string? ImageUrl { get; set; }
    }

    public class MakeOfferViewModel
    {
        [Required]
        public string ListingId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Offer amount is required.")]
        [Range(1, 100000, ErrorMessage = "Offer must be greater than ₹0.")]
        [Display(Name = "Your Offer Price (₹)")]
        public double OfferPrice { get; set; }
    }
}
