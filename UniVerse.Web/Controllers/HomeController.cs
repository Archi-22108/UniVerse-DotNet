using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UniVerse.Web.Data;
using UniVerse.Web.Models;

namespace UniVerse.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AdoNetDbHelper _dbHelper;

    public HomeController(ILogger<HomeController> logger, AdoNetDbHelper dbHelper)
    {
        _logger = logger;
        _dbHelper = dbHelper;
    }

    public IActionResult Index()
    {
        // ADO.NET query to get live metrics from SQLite database
        var model = _dbHelper.GetHomeMetrics();
        return View(model);
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult HowItWorks()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
