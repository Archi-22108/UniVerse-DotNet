using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using UniVerse.Server.Models;

namespace UniVerse.Server.Data.Repositories
{
    /// <summary>
    /// Student P2P Marketplace Repository implemented strictly with raw ADO.NET.
    /// Demonstrates subqueries, parameterization, and relational mapping.
    /// </summary>
    public class MarketplaceRepository : IMarketplaceRepository
    {
        private readonly AdoNetDbHelper _db;
        private readonly ILogger<MarketplaceRepository> _logger;

        public MarketplaceRepository(AdoNetDbHelper db, ILogger<MarketplaceRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<MarketplaceListingDetailDto>> GetListingsAsync(string? category = null)
        {
            string sql = @"
SELECT 
    l.id, l.seller_id, u.full_name AS seller_name, u.phone_number AS seller_phone,
    l.title, l.description, l.category, l.condition, l.price, l.original_price,
    l.negotiable, l.pickup_location, l.status, l.image_url, l.created_at,
    (SELECT COUNT(*) FROM marketplace_offers o WHERE o.listing_id = l.id) AS offer_count
FROM marketplace_listings l
INNER JOIN users u ON l.seller_id = u.id
WHERE l.status = 'active'";

            SqliteParameter[] parameters;
            if (!string.IsNullOrEmpty(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                sql += " AND LOWER(l.category) = LOWER(@category) ";
                parameters = new[] { AdoNetDbHelper.CreateParameter("@category", category) };
            }
            else
            {
                parameters = Array.Empty<SqliteParameter>();
            }

            sql += " ORDER BY l.created_at DESC;";

            return await _db.ExecuteReaderAsync(sql, MapListingFromReader, parameters);
        }

        public async Task<MarketplaceListingDetailDto?> GetListingByIdAsync(string id)
        {
            const string sql = @"
SELECT 
    l.id, l.seller_id, u.full_name AS seller_name, u.phone_number AS seller_phone,
    l.title, l.description, l.category, l.condition, l.price, l.original_price,
    l.negotiable, l.pickup_location, l.status, l.image_url, l.created_at,
    (SELECT COUNT(*) FROM marketplace_offers o WHERE o.listing_id = l.id) AS offer_count
FROM marketplace_listings l
INNER JOIN users u ON l.seller_id = u.id
WHERE l.id = @id;";

            var param = AdoNetDbHelper.CreateParameter("@id", id);
            var listing = await _db.ExecuteSingleAsync(sql, MapListingFromReader, param);
            if (listing != null)
            {
                listing.Offers = await GetOffersForListingAsync(listing.Id);
            }
            return listing;
        }

        public async Task<string> CreateListingAsync(MarketplaceListing listing)
        {
            var id = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow.ToString("o");

            const string sql = @"
INSERT INTO marketplace_listings (id, seller_id, title, description, category, condition, price, original_price, negotiable, pickup_location, status, image_url, created_at, updated_at)
VALUES (@id, @seller_id, @title, @description, @category, @condition, @price, @original_price, @negotiable, @pickup_location, 'active', @image_url, @created_at, @updated_at);";

            var parameters = new[]
            {
                AdoNetDbHelper.CreateParameter("@id", id),
                AdoNetDbHelper.CreateParameter("@seller_id", listing.SellerId),
                AdoNetDbHelper.CreateParameter("@title", listing.Title.Trim()),
                AdoNetDbHelper.CreateParameter("@description", listing.Description?.Trim()),
                AdoNetDbHelper.CreateParameter("@category", listing.Category),
                AdoNetDbHelper.CreateParameter("@condition", listing.Condition),
                AdoNetDbHelper.CreateParameter("@price", listing.Price),
                AdoNetDbHelper.CreateParameter("@original_price", listing.OriginalPrice),
                AdoNetDbHelper.CreateParameter("@negotiable", listing.Negotiable ? 1 : 0),
                AdoNetDbHelper.CreateParameter("@pickup_location", listing.PickupLocation.Trim()),
                AdoNetDbHelper.CreateParameter("@image_url", listing.ImageUrl),
                AdoNetDbHelper.CreateParameter("@created_at", now),
                AdoNetDbHelper.CreateParameter("@updated_at", now)
            };

            await _db.ExecuteNonQueryAsync(sql, parameters);
            return id;
        }

        public async Task<string> CreateOfferAsync(MarketplaceOffer offer)
        {
            var id = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow.ToString("o");

            const string sql = @"
INSERT INTO marketplace_offers (id, listing_id, buyer_id, offer_price, status, created_at)
VALUES (@id, @id_listing, @buyer_id, @offer_price, 'pending', @created_at);";

            var parameters = new[]
            {
                AdoNetDbHelper.CreateParameter("@id", id),
                AdoNetDbHelper.CreateParameter("@id_listing", offer.ListingId),
                AdoNetDbHelper.CreateParameter("@buyer_id", offer.BuyerId),
                AdoNetDbHelper.CreateParameter("@offer_price", offer.OfferPrice),
                AdoNetDbHelper.CreateParameter("@created_at", now)
            };

            await _db.ExecuteNonQueryAsync(sql, parameters);
            return id;
        }

        public async Task<bool> UpdateOfferStatusAsync(string offerId, string status)
        {
            const string sql = "UPDATE marketplace_offers SET status = @status WHERE id = @id;";
            var parameters = new[]
            {
                AdoNetDbHelper.CreateParameter("@id", offerId),
                AdoNetDbHelper.CreateParameter("@status", status.ToLowerInvariant())
            };

            int rows = await _db.ExecuteNonQueryAsync(sql, parameters);
            return rows > 0;
        }

        private async Task<List<MarketplaceOfferDto>> GetOffersForListingAsync(string listingId)
        {
            const string sql = @"
SELECT o.id, o.listing_id, o.buyer_id, u.full_name AS buyer_name, o.offer_price, o.status, o.created_at
FROM marketplace_offers o
INNER JOIN users u ON o.buyer_id = u.id
WHERE o.listing_id = @listingId
ORDER BY o.created_at DESC;";

            var param = AdoNetDbHelper.CreateParameter("@listingId", listingId);
            return await _db.ExecuteReaderAsync(sql, reader => new MarketplaceOfferDto
            {
                Id = reader.GetString(0),
                ListingId = reader.GetString(1),
                BuyerId = reader.GetString(2),
                BuyerName = reader.GetString(3),
                OfferPrice = reader.GetDouble(4),
                Status = reader.GetString(5),
                CreatedAt = reader.GetString(6)
            }, param);
        }

        private static MarketplaceListingDetailDto MapListingFromReader(SqliteDataReader reader)
        {
            return new MarketplaceListingDetailDto
            {
                Id = reader.GetString(0),
                SellerId = reader.GetString(1),
                SellerName = reader.GetString(2),
                SellerPhone = reader.IsDBNull(3) ? null : reader.GetString(3),
                Title = reader.GetString(4),
                Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                Category = reader.GetString(6),
                Condition = reader.GetString(7),
                Price = reader.GetDouble(8),
                OriginalPrice = reader.IsDBNull(9) ? null : reader.GetDouble(9),
                Negotiable = reader.GetInt32(10) == 1,
                PickupLocation = reader.GetString(11),
                Status = reader.GetString(12),
                ImageUrl = reader.IsDBNull(13) ? null : reader.GetString(13),
                CreatedAt = reader.GetString(14),
                OfferCount = reader.GetInt32(15)
            };
        }
    }
}
