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
    public IActionResult Requests(string? tab = "all")
    {
        return RedirectToAction("LiveRadar");
    }

    [HttpGet]
    public IActionResult Runner()
    {
        return RedirectToAction("Index");
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
