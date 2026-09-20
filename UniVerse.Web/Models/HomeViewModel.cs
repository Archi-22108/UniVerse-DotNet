using System.Collections.Generic;

namespace UniVerse.Web.Models;

public class HomeViewModel
{
    public int DeliveryOrdersCount { get; set; }
    public int ActiveRunnersCount { get; set; }
    public int ActiveRequestsCount { get; set; }
    public int VerifiedStudentsCount { get; set; }
    public List<Product> FeaturedProducts { get; set; } = new();
}
