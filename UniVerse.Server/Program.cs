using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using UniVerse.Server.Data;
using UniVerse.Server.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ─── 1. Configure Services (MVC & 3-Tier Layer) ──────────────────────────────

// ASP.NET Core MVC with Razor View Engine (.cshtml)
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();

// Swagger Documentation for College Viva Evaluation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "🏛️ UniVerse Campus Super-App API",
        Version = "v1",
        Description = "Enterprise C# ASP.NET Core solution built with pure ADO.NET Data Access Layer (3-Tier Architecture) for Marwadi University.",
        Contact = new OpenApiContact
        {
            Name = "UniVerse Engineering Team",
            Email = "support@marwadiuniversity.ac.in"
        }
    });
});

// ADO.NET Data Access & Repositories
builder.Services.AddSingleton<AdoNetDbHelper>();
builder.Services.AddScoped<DbInitializer>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();
builder.Services.AddScoped<IMarketplaceRepository, MarketplaceRepository>();

// Cookie-Based Authentication & Session (No Node.js/External Auth)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ─── 2. Auto-Initialize Database & Campus Seeds ──────────────────────────────

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.InitializeAsync();
}

// ─── 3. HTTP Request Pipeline ────────────────────────────────────────────────

app.UseCors("AllowAll");
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Swagger UI for interactive API inspection during professor examination
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "UniVerse Campus API v1");
    c.RoutePrefix = "swagger";
});

// MVC Default Routing: / -> HomeController.Index()
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
