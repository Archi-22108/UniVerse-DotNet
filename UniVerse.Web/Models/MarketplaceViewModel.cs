using System;
using System.Collections.Generic;

namespace UniVerse.Web.Models;

public class MarketplaceItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Books";
    public decimal Price { get; set; }
    public string Condition { get; set; } = "New"; // "New", "Used - Like New", "Good", "Fair"
    public bool IsNegotiable { get; set; } = true;
    public string? Description { get; set; }
    public string SellerName { get; set; } = "Archi.kumari126697";
    public string SellerHostel { get; set; } = "Hostel D · Room 304";
    public string? ImageUrl { get; set; }
    public string Rating { get; set; } = "No ratings yet";
    public bool IsSold { get; set; }
    public bool IsSaved { get; set; }

    public string FormattedDate
    {
        get
        {
            var diff = DateTime.UtcNow - CreatedAt;
            if (diff.TotalHours < 1) return "Just now";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return CreatedAt.ToString("d MMM");
            return CreatedAt.ToString("d MMM");
        }
    }
}

public class MarketplaceViewModel
{
    public List<MarketplaceItem> Items { get; set; } = new();
    public string ActiveCategory { get; set; } = "All Items";
    public string? SearchQuery { get; set; }
    public string ActiveSort { get; set; } = "Newest";
    public string ActiveView { get; set; } = "all"; // "all", "my", "saved"

    public int TotalListingsCount { get; set; }
    public int SavedCount { get; set; }
    public int MyListingsCount { get; set; }

    public List<MarketplaceCategoryPill> Categories { get; set; } = new()
    {
        new() { Key = "All Items", Name = "All Items", Icon = "bi bi-stars" },
        new() { Key = "Books", Name = "Books", Icon = "bi bi-book" },
        new() { Key = "Electronics", Name = "Electronics", Icon = "bi bi-laptop" },
        new() { Key = "Study Notes", Name = "Study Notes", Icon = "bi bi-file-earmark-text" },
        new() { Key = "Hostel Life", Name = "Hostel Life", Icon = "bi bi-house-door" },
        new() { Key = "Sports", Name = "Sports", Icon = "bi bi-trophy" },
        new() { Key = "Furniture", Name = "Furniture", Icon = "bi bi-lamp" },
        new() { Key = "Clothing", Name = "Clothing", Icon = "bi bi-person-standing" },
        new() { Key = "Gaming", Name = "Gaming", Icon = "bi bi-controller" },
        new() { Key = "Other", Name = "Other", Icon = "bi bi-three-dots" }
    };
}

public class MarketplaceCategoryPill
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}
