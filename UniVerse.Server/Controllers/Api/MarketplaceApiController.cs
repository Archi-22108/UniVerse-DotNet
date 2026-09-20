using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UniVerse.Server.Data.Repositories;
using UniVerse.Server.Models;

namespace UniVerse.Server.Controllers.Api
{
    [ApiController]
    [Route("api/marketplace")]
    [Produces("application/json")]
    public class MarketplaceApiController : ControllerBase
    {
        private readonly IMarketplaceRepository _marketplaceRepo;

        public MarketplaceApiController(IMarketplaceRepository marketplaceRepo)
        {
            _marketplaceRepo = marketplaceRepo;
        }

        [HttpGet("listings")]
        public async Task<ActionResult<List<MarketplaceListingDetailDto>>> GetListings([FromQuery] string? category)
        {
            var listings = await _marketplaceRepo.GetListingsAsync(category);
            return Ok(listings);
        }

        [HttpGet("listings/{id}")]
        public async Task<ActionResult<MarketplaceListingDetailDto>> GetListingById(string id)
        {
            var listing = await _marketplaceRepo.GetListingByIdAsync(id);
            if (listing == null)
            {
                return NotFound(new { message = "Marketplace listing not found." });
            }
            return Ok(listing);
        }

        [HttpPost("listings")]
        public async Task<ActionResult<MarketplaceListingDetailDto>> CreateListing([FromBody] CreateListingDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var listing = new MarketplaceListing
            {
                SellerId = dto.SellerId,
                Title = dto.Title,
                Description = dto.Description,
                Category = dto.Category,
                Condition = dto.Condition,
                Price = dto.Price,
                OriginalPrice = dto.OriginalPrice,
                Negotiable = dto.Negotiable,
                PickupLocation = dto.PickupLocation,
                ImageUrl = dto.ImageUrl
            };

            var id = await _marketplaceRepo.CreateListingAsync(listing);
            return await GetListingById(id);
        }

        [HttpPost("offers")]
        public async Task<IActionResult> MakeOffer([FromBody] CreateOfferDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var offer = new MarketplaceOffer
            {
                ListingId = dto.ListingId,
                BuyerId = dto.BuyerId,
                OfferPrice = dto.OfferPrice
            };

            var id = await _marketplaceRepo.CreateOfferAsync(offer);
            return Ok(new { message = "Offer submitted successfully via ADO.NET.", offerId = id });
        }
    }
}
