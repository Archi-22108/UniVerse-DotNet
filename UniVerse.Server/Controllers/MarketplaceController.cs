using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using UniVerse.Server.Data;
using UniVerse.Server.Models;

namespace UniVerse.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class MarketplaceController : ControllerBase
    {
        private readonly AdoNetDbHelper _dbHelper;
        private readonly ILogger<MarketplaceController> _logger;

        public MarketplaceController(AdoNetDbHelper dbHelper, ILogger<MarketplaceController> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        /// <summary>
        /// Browses all active marketplace listings with optional category filtering using ADO.NET.
        /// </summary>
        [HttpGet("listings")]
        public async Task<ActionResult<List<MarketplaceListingDetailDto>>> GetListings([FromQuery] string? category)
        {
            string querySql = @"
SELECT 
    l.id, l.seller_id, u.full_name AS seller_name, u.phone_number AS seller_phone,
    l.title, l.description, l.category, l.condition, l.price, l.original_price,
    l.negotiable, l.pickup_location, l.status, l.image_url, l.created_at,
    (SELECT COUNT(*) FROM marketplace_offers o WHERE o.listing_id = l.id) AS offer_count
FROM marketplace_listings l
INNER JOIN users u ON l.seller_id = u.id
WHERE l.status = 'active'";

            SqliteParameter[] parameters;
            if (!string.IsNullOrEmpty(category))
            {
                querySql += " AND l.category = @category ";
                parameters = new[] { AdoNetDbHelper.CreateParameter("@category", category) };
            }
            else
            {
                parameters = Array.Empty<SqliteParameter>();
            }

            querySql += " ORDER BY l.created_at DESC;";

            var listings = await _dbHelper.ExecuteReaderAsync(querySql, reader => new MarketplaceListingDetailDto
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
            }, parameters);

            return Ok(listings);
        }

        /// <summary>
        /// Retrieves a single marketplace listing with its active negotiation offers using ADO.NET.
        /// </summary>
        [HttpGet("listings/{id}")]
        public async Task<ActionResult<MarketplaceListingDetailDto>> GetListingById(string id)
        {
            const string querySql = @"
SELECT 
    l.id, l.seller_id, u.full_name AS seller_name, u.phone_number AS seller_phone,
    l.title, l.description, l.category, l.condition, l.price, l.original_price,
    l.negotiable, l.pickup_location, l.status, l.image_url, l.created_at,
    (SELECT COUNT(*) FROM marketplace_offers o WHERE o.listing_id = l.id) AS offer_count
FROM marketplace_listings l
INNER JOIN users u ON l.seller_id = u.id
WHERE l.id = @id;";

            var paramId = AdoNetDbHelper.CreateParameter("@id", id);
            var listing = await _dbHelper.ExecuteSingleAsync(querySql, reader => new MarketplaceListingDetailDto
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
            }, paramId);

            if (listing == null)
            {
                return NotFound(new { message = "Marketplace listing not found." });
            }

            // Retrieve offers
            const string offersSql = @"
SELECT o.id, o.listing_id, o.buyer_id, u.full_name AS buyer_name, o.offer_price, o.status, o.created_at
FROM marketplace_offers o
INNER JOIN users u ON o.buyer_id = u.id
WHERE o.listing_id = @listingId
ORDER BY o.created_at DESC;";

            var paramListingId = AdoNetDbHelper.CreateParameter("@listingId", id);
            listing.Offers = await _dbHelper.ExecuteReaderAsync(offersSql, reader => new MarketplaceOfferDto
            {
                Id = reader.GetString(0),
                ListingId = reader.GetString(1),
                BuyerId = reader.GetString(2),
                BuyerName = reader.GetString(3),
                OfferPrice = reader.GetDouble(4),
                Status = reader.GetString(5),
                CreatedAt = reader.GetString(6)
            }, paramListingId);

            return Ok(listing);
        }

        /// <summary>
        /// Posts a new marketplace listing using ADO.NET.
        /// </summary>
        [HttpPost("listings")]
        public async Task<ActionResult<MarketplaceListingDetailDto>> CreateListing([FromBody] CreateListingDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var listingId = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow.ToString("o");

            const string insertSql = @"
INSERT INTO marketplace_listings (id, seller_id, title, description, category, condition, price, original_price, negotiable, pickup_location, status, image_url, created_at, updated_at)
VALUES (@id, @seller_id, @title, @description, @category, @condition, @price, @original_price, @negotiable, @pickup_location, 'active', @image_url, @created_at, @updated_at);";

            var parameters = new[]
            {
                AdoNetDbHelper.CreateParameter("@id", listingId),
                AdoNetDbHelper.CreateParameter("@seller_id", dto.SellerId),
                AdoNetDbHelper.CreateParameter("@title", dto.Title.Trim()),
                AdoNetDbHelper.CreateParameter("@description", dto.Description?.Trim()),
                AdoNetDbHelper.CreateParameter("@category", dto.Category),
                AdoNetDbHelper.CreateParameter("@condition", dto.Condition),
                AdoNetDbHelper.CreateParameter("@price", dto.Price),
                AdoNetDbHelper.CreateParameter("@original_price", dto.OriginalPrice),
                AdoNetDbHelper.CreateParameter("@negotiable", dto.Negotiable ? 1 : 0),
                AdoNetDbHelper.CreateParameter("@pickup_location", dto.PickupLocation.Trim()),
                AdoNetDbHelper.CreateParameter("@image_url", dto.ImageUrl),
                AdoNetDbHelper.CreateParameter("@created_at", now),
                AdoNetDbHelper.CreateParameter("@updated_at", now)
            };

            await _dbHelper.ExecuteNonQueryAsync(insertSql, parameters);
            return await GetListingById(listingId);
        }

        /// <summary>
        /// Submits a price negotiation offer on a listing using ADO.NET.
        /// </summary>
        [HttpPost("offers")]
        public async Task<IActionResult> MakeOffer([FromBody] CreateOfferDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var offerId = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow.ToString("o");

            const string insertSql = @"
INSERT INTO marketplace_offers (id, listing_id, buyer_id, offer_price, status, created_at)
VALUES (@id, @listing_id, @buyer_id, @offer_price, 'pending', @created_at);";

            var parameters = new[]
            {
                AdoNetDbHelper.CreateParameter("@id", offerId),
                AdoNetDbHelper.CreateParameter("@listing_id", dto.ListingId),
                AdoNetDbHelper.CreateParameter("@buyer_id", dto.BuyerId),
                AdoNetDbHelper.CreateParameter("@offer_price", dto.OfferPrice),
                AdoNetDbHelper.CreateParameter("@created_at", now)
            };

            await _dbHelper.ExecuteNonQueryAsync(insertSql, parameters);
            return Ok(new { message = "Offer submitted successfully to seller.", offerId });
        }

        /// <summary>
        /// Updates the status of an offer (accepted, rejected, withdrawn) using ADO.NET.
        /// </summary>
        [HttpPatch("offers/{offerId}/status")]
        public async Task<IActionResult> UpdateOfferStatus(string offerId, [FromBody] string status)
        {
            const string updateSql = "UPDATE marketplace_offers SET status = @status WHERE id = @id;";
            var parameters = new[]
            {
                AdoNetDbHelper.CreateParameter("@id", offerId),
                AdoNetDbHelper.CreateParameter("@status", status.ToLowerInvariant())
            };

            int affected = await _dbHelper.ExecuteNonQueryAsync(updateSql, parameters);
            if (affected == 0)
            {
                return NotFound(new { message = "Offer not found." });
            }

            return Ok(new { message = $"Offer status updated to {status}." });
        }
    }
}
