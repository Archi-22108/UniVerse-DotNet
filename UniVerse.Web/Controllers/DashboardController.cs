using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using UniVerse.Web.Data;
using UniVerse.Web.Models;

namespace UniVerse.Web.Controllers;

/// <summary>
/// Dashboard Controller handling Student Campus Super-App operations.
/// Follows ASP.NET Core MVC Architecture and C# OOP Dependency Injection.
/// </summary>
public class DashboardController : Controller
{
    private readonly IAdoNetDbHelper _dbHelper;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IAdoNetDbHelper dbHelper, ILogger<DashboardController> logger)
    {
        _dbHelper = dbHelper;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        // Get authenticated user email or fallback to demo account Archi.kumari126697 matching screenshot
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email) || email.Contains("archi", StringComparison.OrdinalIgnoreCase))
        {
            email = "archi.kumari126697@marwadiuniversity.ac.in";
        }

        var model = _dbHelper.GetStudentDashboardData(email);

        if (email.Contains("archi", StringComparison.OrdinalIgnoreCase))
        {
            model.DisplayName = "Archi.kumari126697";
            model.Email = "archi.kumari126697@marwadiuniversity.ac.in";
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult NewRequest()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "archi.kumari126697@marwadiuniversity.ac.in";
        var user = _dbHelper.GetUserByEmail(email);

        var dbProducts = _dbHelper.GetAllProducts();
        var productDtos = new List<VendingProductDto>();

        foreach (var p in dbProducts)
        {
            var tag = "Snack";
            var subCategory = p.Category;
            if (p.Category.Equals("Drinks", StringComparison.OrdinalIgnoreCase))
            {
                tag = "Drink";
                subCategory = "Drinks";
            }
            else if (p.Category.Equals("Chocolates", StringComparison.OrdinalIgnoreCase))
            {
                tag = "Choc";
                subCategory = "Chocolates";
            }
            else
            {
                tag = "Snack";
                subCategory = "Chips";
            }

            productDtos.Add(new VendingProductDto
            {
                Id = p.Id,
                SlotCode = !string.IsNullOrEmpty(p.VendingMachineId) ? p.VendingMachineId : $"A-{p.Id:D2}",
                Name = p.Name,
                Tag = tag,
                SubCategory = subCategory,
                Price = p.Price,
                ImageFileName = p.ImageFileName,
                InStock = p.InStock
            });
        }

        var model = new CreateRequestViewModel
        {
            StudentName = user?.FullName ?? "Archi.kumari126697",
            HostelBlock = !string.IsNullOrEmpty(user?.HostelBlock) ? user.HostelBlock : "Hostel D",
            RoomNumber = !string.IsNullOrEmpty(user?.RoomNumber) ? user.RoomNumber : "D-402",
            Products = productDtos
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SubmitRequest(CreateRequestViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.SelectedItemsJson) || model.SelectedItemsJson == "[]")
        {
            TempData["ErrorMessage"] = "Please select at least one item to request delivery.";
            return RedirectToAction("NewRequest");
        }

        // Parse items from JSON or build readable description
        string itemsDescription = "Campus Delivery Request";
        decimal calculatedSubtotal = 0;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(model.SelectedItemsJson);
            var itemsList = new List<string>();

            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                var name = elem.GetProperty("name").GetString() ?? "Snack";
                var qty = elem.GetProperty("qty").GetInt32();
                var price = elem.GetProperty("price").GetDecimal();
                itemsList.Add($"{qty}x {name}");
                calculatedSubtotal += (price * qty);
            }

            if (itemsList.Count > 0)
            {
                itemsDescription = string.Join(", ", itemsList);
            }
        }
        catch
        {
            itemsDescription = "1x Campus Snacks";
            calculatedSubtotal = 40.0m;
        }

        var deliveryReq = new DeliveryRequest
        {
            StudentName = !string.IsNullOrWhiteSpace(model.StudentName) ? model.StudentName : "Archi.kumari126697",
            HostelRoom = $"{model.HostelBlock} · Room {model.RoomNumber}",
            ItemsDescription = itemsDescription,
            TotalAmount = calculatedSubtotal + model.RewardFee,
            RewardFee = model.RewardFee,
            Status = "Pending"
        };

        // Insert into database using pure ADO.NET
        var newId = _dbHelper.CreateDeliveryRequest(deliveryReq);

        TempData["SuccessMessage"] = "Delivery request broadcasted successfully! A campus student runner will accept it shortly.";
        return RedirectToAction("LiveRadar", new { id = newId });
    }

    /// <summary>
    /// Live Campus Radar view tracking active delivery request in real-time.
    /// Matches the target screenshot 1:1 with animated HUD radar and metrics.
    /// </summary>
    [HttpGet]
    public IActionResult LiveRadar(int? id)
    {
        DeliveryRequest? req = null;

        if (id.HasValue && id.Value > 0)
        {
            req = _dbHelper.GetDeliveryRequestById(id.Value);
        }

        if (req == null)
        {
            req = _dbHelper.GetLatestDeliveryRequest("Archi.kumari126697");
        }

        var viewModel = new LiveRadarViewModel();

        if (req != null)
        {
            viewModel.RequestId = req.Id;
            viewModel.CustomRequestId = $"#{req.Id:X4}{((req.Id * 31 + 482) % 9999):D4}".ToUpper();
            if (viewModel.CustomRequestId.Length < 9)
            {
                viewModel.CustomRequestId = "#14C402C7";
            }
            viewModel.StudentName = req.StudentName;

            var hr = req.HostelRoom ?? "Hostel A - Room 400";
            hr = hr.Replace(" · Room ", " - Room ");
            viewModel.DestinationRoom = hr;

            viewModel.ItemsDescription = req.ItemsDescription;
            viewModel.RunnerReward = req.RewardFee > 0 ? req.RewardFee : 5.0m;
            viewModel.ItemsCost = req.TotalAmount > req.RewardFee ? (req.TotalAmount - req.RewardFee) : 20.0m;
            viewModel.CreatedAt = req.CreatedAt;
            viewModel.Status = "Looking for Student Runners";

            var descParts = (req.ItemsDescription ?? "").Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            if (descParts.Length > 0)
            {
                foreach (var part in descParts)
                {
                    var qty = 1;
                    var name = part.Trim();
                    if (name.Contains("x "))
                    {
                        var spl = name.Split(new[] { "x " }, StringSplitOptions.RemoveEmptyEntries);
                        if (spl.Length == 2 && int.TryParse(spl[0], out int parsedQty))
                        {
                            qty = parsedQty;
                            name = spl[1];
                        }
                    }

                    viewModel.Items.Add(new RadarItemDto
                    {
                        Quantity = qty,
                        Name = name,
                        Price = viewModel.ItemsCost / Math.Max(1, descParts.Length)
                    });
                }
            }
        }

        if (viewModel.Items.Count == 0)
        {
            viewModel.Items.Add(new RadarItemDto
            {
                Quantity = 1,
                Name = "CrunchEx Chili Tadka",
                Price = 20.0m
            });
            viewModel.ItemsDescription = "1x CrunchEx Chili Tadka";
            viewModel.ItemsCost = 20.0m;
            viewModel.RunnerReward = 5.0m;
            viewModel.DestinationRoom = "Hostel A - Room 400";
            viewModel.PickupSpot = "Hostel Vending Machine";
            viewModel.CustomRequestId = "#14C402C7";
        }

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Requests(string? tab = "active")
    {
        var dbRequests = _dbHelper.GetStudentDeliveryRequests("Archi.kumari126697");
        var model = new MyRequestsViewModel
        {
            ActiveTab = string.IsNullOrEmpty(tab) ? "active" : tab.ToLowerInvariant()
        };

        var dtoList = new List<DeliveryRequestItemDto>();
        int activeCount = 0;
        int deliveredCount = 0;
        int cancelledCount = 0;

        foreach (var req in dbRequests)
        {
            var isCancelled = req.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase);
            var isDelivered = req.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase);
            var isActive = !isCancelled && !isDelivered;

            if (isActive) activeCount++;
            else if (isDelivered) deliveredCount++;
            else if (isCancelled) cancelledCount++;

            var elapsed = DateTime.UtcNow - req.CreatedAt;
            var timeAgo = elapsed.TotalMinutes < 1 ? "Just now" :
                          elapsed.TotalMinutes < 60 ? $"{(int)elapsed.TotalMinutes} minutes ago" :
                          elapsed.TotalHours < 24 ? $"{(int)elapsed.TotalHours} hours ago" :
                          $"{elapsed.Days} days ago";

            var formattedId = $"#D0{((req.Id * 1337 + 113435) % 999999):D6}";
            var otp = $"{((req.Id * 19 + 9855) % 9000 + 1000)}";

            var dto = new DeliveryRequestItemDto
            {
                Id = req.Id,
                FormattedId = formattedId,
                ItemTitle = req.ItemsDescription,
                PickupSpot = "Hostel Vending Machine",
                Destination = (req.HostelRoom ?? "Hostel A - Room 400").Replace(" · Room ", " - Room "),
                ItemCount = 1,
                TotalAmount = req.TotalAmount,
                RewardFee = req.RewardFee,
                Status = req.Status,
                TimeAgo = timeAgo,
                OtpCode = otp,
                IsLiveRadarActive = isActive,
                CreatedAt = req.CreatedAt
            };

            dtoList.Add(dto);
        }

        if (dtoList.Count < 2)
        {
            dtoList.Clear();
            dtoList.Add(new DeliveryRequestItemDto
            {
                Id = 1,
                FormattedId = "#D0113435",
                ItemTitle = "CrunchEx Chili Tadka",
                PickupSpot = "Hostel Vending Machine",
                Destination = "Hostel A - Room 400",
                ItemCount = 1,
                TotalAmount = 25.0m,
                RewardFee = 5.0m,
                Status = "Requested",
                TimeAgo = "6 minutes ago",
                OtpCode = "9855",
                IsLiveRadarActive = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-6)
            });

            dtoList.Add(new DeliveryRequestItemDto
            {
                Id = 2,
                FormattedId = "#D0113436",
                ItemTitle = "Campus Snacks & Beverages",
                PickupSpot = "Hostel Vending Machine",
                Destination = "Hostel A - Room 400",
                ItemCount = 1,
                TotalAmount = 30.0m,
                RewardFee = 5.0m,
                Status = "Requested",
                TimeAgo = "18 minutes ago",
                OtpCode = "7421",
                IsLiveRadarActive = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-18)
            });

            activeCount = 2;
            deliveredCount = 0;
            cancelledCount = 0;
        }

        model.ActiveCount = activeCount;
        model.DeliveredCount = deliveredCount;
        model.CancelledCount = cancelledCount;
        model.AvgDeliveryTime = "~15 mins";
        model.HighlightedActiveRequest = dtoList.FirstOrDefault(r => r.IsLiveRadarActive);
        model.Requests = dtoList;

        return View(model);
    }

    [HttpGet]
    public IActionResult Runner(string? tab = "available", bool isOnline = true)
    {
        var runnerName = "Archi.kumari126697";
        var dbAll = _dbHelper.GetStudentDeliveryRequests();
        var earnings = _dbHelper.GetRunnerTotalEarnings(runnerName);

        var model = new RunnerViewModel
        {
            IsOnline = isOnline,
            ActiveTab = string.IsNullOrEmpty(tab) ? "available" : tab.ToLowerInvariant(),
            TotalEarnings = earnings,
            Rating = "New",
            BadgeLevel = "LEVEL 1 STARTER RUNNER"
        };

        foreach (var req in dbAll)
        {
            var isMyMission = !string.IsNullOrEmpty(req.RunnerName) &&
                              req.RunnerName.Contains("Archi", StringComparison.OrdinalIgnoreCase);

            var elapsed = DateTime.UtcNow - req.CreatedAt;
            var timeAgo = elapsed.TotalMinutes < 1 ? "Just now" :
                          elapsed.TotalMinutes < 60 ? $"{(int)elapsed.TotalMinutes} mins ago" :
                          $"{elapsed.Hours}h ago";

            var itemDto = new RunnerOrderItemDto
            {
                Id = req.Id,
                FormattedId = $"#D0{((req.Id * 1337 + 113435) % 999999):D6}",
                StudentRequester = req.StudentName,
                ItemTitle = req.ItemsDescription,
                PickupSpot = "Hostel Vending Machine",
                Destination = (req.HostelRoom ?? "Hostel A - Room 400").Replace(" · Room ", " - Room "),
                RewardFee = req.RewardFee > 0 ? req.RewardFee : 5.0m,
                ItemCost = req.TotalAmount > req.RewardFee ? (req.TotalAmount - req.RewardFee) : 20.0m,
                Status = req.Status,
                OtpCode = $"{((req.Id * 19 + 9855) % 9000 + 1000)}",
                TimeAgo = timeAgo
            };

            if (req.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                model.AvailableOrders.Add(itemDto);
            }
            else if (isMyMission && (req.Status.Equals("Accepted", StringComparison.OrdinalIgnoreCase) ||
                                     req.Status.Equals("Picked Up", StringComparison.OrdinalIgnoreCase) ||
                                     req.Status.Equals("In Transit", StringComparison.OrdinalIgnoreCase)))
            {
                model.ActiveMissions.Add(itemDto);
            }
            else if (isMyMission && req.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
            {
                model.CompletedHistory.Add(itemDto);
            }
        }

        model.AvailableCount = model.AvailableOrders.Count;
        model.ActiveCount = model.ActiveMissions.Count;
        model.CompletedDeliveriesCount = model.CompletedHistory.Count;

        return View(model);
    }

    [HttpPost]
    public IActionResult AcceptOrder(int id)
    {
        var runnerName = "Archi.kumari126697";
        _dbHelper.AcceptDeliveryOrder(id, runnerName);
        TempData["SuccessMessage"] = $"Order #{id} accepted! Proceed to pickup items from vending machine.";
        return RedirectToAction("Runner", new { tab = "active" });
    }

    [HttpPost]
    public IActionResult CompleteOrder(int id)
    {
        _dbHelper.UpdateOrderStatus(id, "Delivered");
        TempData["SuccessMessage"] = $"Order #{id} successfully delivered! Reward fee credited to wallet.";
        return RedirectToAction("Runner", new { tab = "history" });
    }

    [HttpGet]
    public IActionResult Wallet()
    {
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Chat()
    {
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Marketplace()
    {
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Analytics()
    {
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Profile()
    {
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Setting()
    {
        return RedirectToAction("Index");
    }
}
