using System.Collections.Generic;
using System.Threading.Tasks;
using UniVerse.Server.Models;

namespace UniVerse.Server.Data.Repositories
{
    public interface IMarketplaceRepository
    {
        Task<List<MarketplaceListingDetailDto>> GetListingsAsync(string? category = null);
        Task<MarketplaceListingDetailDto?> GetListingByIdAsync(string id);
        Task<string> CreateListingAsync(MarketplaceListing listing);
        Task<bool> DeleteListingAsync(string id, string sellerId);
        Task<string> CreateOfferAsync(MarketplaceOffer offer);
        Task<bool> UpdateOfferStatusAsync(string offerId, string status);
    }
}
