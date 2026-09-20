using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniVerse.Server.Data.Repositories;
using UniVerse.Server.Models;
using UniVerse.Server.Models.ViewModels;

namespace UniVerse.Server.Controllers
{
    public class MarketplaceController : Controller
    {
        private readonly IMarketplaceRepository _marketplaceRepo;

        public MarketplaceController(IMarketplaceRepository marketplaceRepo)
        {
            _marketplaceRepo = marketplaceRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? category)
        {
            var listings = await _marketplaceRepo.GetListingsAsync(category);
            ViewBag.CurrentCategory = category ?? "All";
            return View(listings);
        }

        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new MarketplaceCreateViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MarketplaceCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var newListing = new MarketplaceListing
            {
                SellerId = userId,
                Title = model.Title.Trim(),
                Description = model.Description?.Trim(),
                Category = model.Category,
                Condition = model.Condition,
                Price = model.Price,
                OriginalPrice = model.OriginalPrice,
                Negotiable = model.Negotiable,
                PickupLocation = model.PickupLocation.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) 
                    ? "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=600" 
                    : model.ImageUrl.Trim(),
                Status = "active"
            };

            var listingId = await _marketplaceRepo.CreateListingAsync(newListing);
            TempData["SuccessMessage"] = "Marketplace listing created successfully!";
            return RedirectToAction("Details", new { id = listingId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var listing = await _marketplaceRepo.GetListingByIdAsync(id);
            if (listing == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.IsSeller = currentUserId == listing.SellerId;

            return View(listing);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeOffer(MakeOfferViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please enter a valid offer price.";
                return RedirectToAction("Details", new { id = model.ListingId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var newOffer = new MarketplaceOffer
            {
                ListingId = model.ListingId,
                BuyerId = userId,
                OfferPrice = model.OfferPrice,
                Status = "pending"
            };

            await _marketplaceRepo.CreateOfferAsync(newOffer);
            TempData["SuccessMessage"] = "Your price offer has been submitted to the seller!";
            return RedirectToAction("Details", new { id = model.ListingId });
        }
    }
}
