using System;
using System.Collections.Generic;

namespace UniVerse.Server.Models
{
    public class MarketplaceListing
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SellerId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = "Books";
        public string Condition { get; set; } = "Good";
        public double Price { get; set; }
        public double? OriginalPrice { get; set; }
        public bool Negotiable { get; set; } = true;
        public string PickupLocation { get; set; } = string.Empty;
        public string Status { get; set; } = "active"; // active, reserved, sold
        public string? ImageUrl { get; set; }
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
        public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");
    }

    public class MarketplaceOffer
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ListingId { get; set; } = string.Empty;
        public string BuyerId { get; set; } = string.Empty;
        public double OfferPrice { get; set; }
        public string Status { get; set; } = "pending"; // pending, accepted, rejected, withdrawn
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
    }

    public class MarketplaceListingDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string SellerId { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public string? SellerPhone { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public double Price { get; set; }
        public double? OriginalPrice { get; set; }
        public bool Negotiable { get; set; }
        public string PickupLocation { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
        public string? ImageUrl { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public int OfferCount { get; set; }
        public List<MarketplaceOfferDto> Offers { get; set; } = new();
    }

    public class MarketplaceOfferDto
    {
        public string Id { get; set; } = string.Empty;
        public string ListingId { get; set; } = string.Empty;
        public string BuyerId { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public double OfferPrice { get; set; }
        public string Status { get; set; } = "pending";
        public string CreatedAt { get; set; } = string.Empty;
    }
}
