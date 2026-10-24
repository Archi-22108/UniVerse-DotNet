using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using UniVerse.Server.Data.Repositories;

namespace UniVerse.Server.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDeliveryRepository _deliveryRepo;
        private readonly IMarketplaceRepository _marketplaceRepo;
        private readonly IUserRepository _userRepo;
        private readonly IMemoryCache _cache;

        public HomeController(
            IDeliveryRepository deliveryRepo, 
            IMarketplaceRepository marketplaceRepo, 
            IUserRepository userRepo,
            IMemoryCache cache)
        {
            _deliveryRepo = deliveryRepo;
            _marketplaceRepo = marketplaceRepo;
            _userRepo = userRepo;
            _cache = cache;
        }

        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Index()
        {
            var stats = await _cache.GetOrCreateAsync("home_stats_cache", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);

                var usersTask = _userRepo.GetAllUsersAsync();
                var requestsTask = _deliveryRepo.GetRequestsAsync();
                var listingsTask = _marketplaceRepo.GetListingsAsync();
                var runnersTask = _userRepo.GetActiveRunnersAsync();

                await Task.WhenAll(usersTask, requestsTask, listingsTask, runnersTask);

                return (
                    UsersCount: usersTask.Result.Count,
                    RequestsCount: requestsTask.Result.Count,
                    ListingsCount: listingsTask.Result.Count,
                    RunnersCount: runnersTask.Result.Count
                );
            });

            ViewBag.TotalUsers = stats.UsersCount;
            ViewBag.TotalRequests = stats.RequestsCount;
            ViewBag.TotalListings = stats.ListingsCount;
            ViewBag.ActiveRunners = stats.RunnersCount;

            return View();
        }
    }
}
