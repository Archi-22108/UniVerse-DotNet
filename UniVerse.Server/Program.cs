using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using UniVerse.Server.Data;

var builder = WebApplication.CreateBuilder(args);

// ─── 1. Configure Services ───────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger for Academic Presentation & API Testing
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "🏛️ UniVerse Campus Super-App API",
        Version = "v1",
        Description = "Enterprise C# ASP.NET Core solution built with pure ADO.NET Data Access Layer, supporting Campus Delivery, Runners, and Peer-to-Peer Marketplace for Marwadi University.",
        Contact = new OpenApiContact
        {
            Name = "UniVerse Enterprise Architecture Team",
            Email = "support@marwadiuniversity.ac.in"
        }
    });
});

// Configure ADO.NET Services
builder.Services.AddSingleton<AdoNetDbHelper>();
builder.Services.AddScoped<DbInitializer>();

// CORS configuration to allow local frontend access
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

// ─── 2. Database Auto-Initialization (ADO.NET) ──────────────────────────────

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.InitializeAsync();
}

// ─── 3. Configure HTTP Request Pipeline ──────────────────────────────────────

app.UseCors("AllowAll");

// Serve Default Files (index.html) and Static Web Assets from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// Enable Swagger UI across all environments for professor grading
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "UniVerse Campus API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthorization();

app.MapControllers();

app.Run();
